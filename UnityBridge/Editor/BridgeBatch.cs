using System;
using System.Collections.Generic;
using System.IO;
using NyaForge.Authoring;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NyaForge.UnityBridge.Editor
{
    /// <summary>Receiver-side checks. Run in a separate, disposable Unity project.</summary>
    public static partial class BridgeBatch
    {
        const float ToleranceMetres = 0.000001f;

        [Serializable]
        sealed class Report
        {
            public string suite = "NyaForge NF-0 Unity Bridge";
            public string unityVersion;
            public string status;
            public string error;
            public float toleranceMetres = ToleranceMetres;
            public string[] checks;
            public string[] importedAssetDirectories;
        }

        // -executeMethod NyaForge.UnityBridge.Editor.BridgeBatch.VerifyRoundTrip
        // --nyaforge-bake-scale1 <manifest> --nyaforge-bake-scale100 <manifest>
        // --nyaforge-report <absolute report path>
        public static void VerifyRoundTrip()
        {
            var checks = new List<string>();
            var folders = new List<string>();
            var report = new Report { unityVersion = Application.unityVersion, status = "failed" };
            string reportPath = null;
            int exitCode = 1;
            try
            {
                var args = Environment.GetCommandLineArgs();
                reportPath = RequiredArgument(args, "--nyaforge-report");
                string scale1Path = RequiredArgument(args, "--nyaforge-bake-scale1");
                string scale100Path = RequiredArgument(args, "--nyaforge-bake-scale100");
                var bake1 = BakeStore.Read(scale1Path);
                var bake100 = BakeStore.Read(scale100Path);
                Near(bake1.Transform.Scale, 1f, "First fixture source scale");
                Near(bake100.Transform.Scale, 100f, "Second fixture source scale");
                var result1 = BakeImporter.Import(scale1Path);
                folders.Add(result1.AssetDirectory);
                var result100 = BakeImporter.Import(scale100Path);
                folders.Add(result100.AssetDirectory);
                var mesh1 = VerifyAssets(result1, bake1, checks, "scale1");
                var mesh100 = VerifyAssets(result100, bake100, checks, "scale100");
                var positions1 = mesh1.vertices;
                var positions100 = mesh100.vertices;
                Require(positions1.Length == positions100.Length, "Scale fixtures vertex counts differ.");
                for (int i = 0; i < positions1.Length; i++)
                    Near(positions1[i], positions100[i], "Scale-independent metre position " + i);
                checks.Add("Scale 1 and scale 100 produce identical metre geometry.");
                VerifyKnownCentimetreEdit(mesh1, bake1, "scale1");
                VerifyKnownCentimetreEdit(mesh100, bake100, "scale100");
                checks.Add("Both fixtures move only vertex 0 by exactly +0.01 metre on X.");
                VerifyExplicitAttachment(scale1Path, mesh1.vertices, folders);
                checks.Add("Explicit scene parent preserves world geometry; prefab uses ordinary components only.");
                VerifyRejectedOutputDoesNotCreateAssets(scale1Path);
                checks.Add("Output traversal is rejected before asset creation.");
                VerifyPhysBonesBridge(checks);
                if (Array.IndexOf(args,"--nyaforge-surface") >= 0)
                    VerifySurface(RequiredArgument(args,"--nyaforge-surface"),checks,folders);
                if (Array.IndexOf(args,"--nyaforge-material") >= 0)
                    VerifyMaterial(RequiredArgument(args,"--nyaforge-material"),checks,folders);
                if (Array.IndexOf(args,"--nyaforge-materials") >= 0)
                    VerifyMaterials(RequiredArgument(args,"--nyaforge-materials"),checks,folders);
                report.status = "passed";
                exitCode = 0;
                Debug.Log("NYAFORGE_BRIDGE_ROUNDTRIP_PASSED");
            }
            catch (Exception error)
            {
                report.error = error.ToString();
                Debug.LogException(error);
            }
            finally
            {
                report.checks = checks.ToArray();
                report.importedAssetDirectories = folders.ToArray();
                if (!string.IsNullOrEmpty(reportPath))
                {
                    try
                    {
                        string full = Path.GetFullPath(reportPath);
                        Directory.CreateDirectory(Path.GetDirectoryName(full));
                        File.WriteAllText(full, JsonUtility.ToJson(report, true));
                    }
                    catch (Exception error) { Debug.LogException(error); exitCode = 1; }
                }
                if (Application.isBatchMode) EditorApplication.Exit(exitCode);
            }
        }

        static Mesh VerifyAssets(BakeImportResult result, BakeDocument bake, List<string> checks, string label, bool generatedNormals=false)
        {
            // Force a disk import after saving the owned assets, exercising Unity serialization.
            AssetDatabase.ImportAsset(result.MeshPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(result.PrefabPath, ImportAssetOptions.ForceUpdate);
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(result.MeshPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(result.PrefabPath);
            Require(mesh != null && prefab != null, label + " serialized assets missing.");
            Require(mesh.vertexCount == bake.Mesh.VertexCount, label + " vertex count changed.");
            var positions = mesh.vertices;
            for (int i = 0; i < positions.Length; i++)
            {
                var expected = bake.Transform.ToAvatarPoint(bake.Mesh.Positions[i]);
                Near(positions[i], Vector(expected), label + " position " + i);
            }
            var normals = mesh.normals;
            if (generatedNormals && bake.Mesh.Normals.Count==0)
                Require(normals.Length==mesh.vertexCount,label+" derived normals missing.");
            else
            {
                Require(normals.Length == bake.Mesh.Normals.Count, label + " normal attribute count changed.");
                for (int i = 0; i < normals.Length; i++) Near(normals[i], Vector(bake.Mesh.Normals[i]), label + " normal " + i);
            }
            var tangents = mesh.tangents;
            Require(tangents.Length == bake.Mesh.Tangents.Count, label + " tangent attribute count changed.");
            for (int i = 0; i < tangents.Length; i++)
            {
                var expected = bake.Mesh.Tangents[i];
                Near(tangents[i].x, expected.X, label + " tangent x");
                Near(tangents[i].y, expected.Y, label + " tangent y");
                Near(tangents[i].z, expected.Z, label + " tangent z");
                Near(tangents[i].w, expected.W, label + " tangent handedness");
            }
            var uv = mesh.uv;
            Require(uv.Length == bake.Mesh.Uv0.Count, label + " UV count changed.");
            for (int i = 0; i < uv.Length; i++)
            {
                Near(uv[i].x, bake.Mesh.Uv0[i].X, label + " UV x");
                Near(uv[i].y, bake.Mesh.Uv0[i].Y, label + " UV y");
            }
            var submeshes = bake.Mesh.Submeshes;
            Require(mesh.subMeshCount == submeshes.Count, label + " submesh count changed.");
            for (int submesh = 0; submesh < submeshes.Count; submesh++)
            {
                var actual = mesh.GetTriangles(submesh);
                var expected = submeshes[submesh];
                Require(actual.Length == expected.Length, label + " index count changed.");
                for (int i = 0; i < actual.Length; i++) Require(actual[i] == expected[i], label + " index order changed.");
            }
            Near(prefab.transform.localPosition, Vector3.zero, label + " prefab root translation");
            Near(prefab.transform.localScale, Vector3.one, label + " prefab root scale");
            Require(Quaternion.Angle(prefab.transform.localRotation, Quaternion.identity) < .0001f, label + " prefab root rotation changed.");
            Require(prefab.GetComponent<MeshFilter>().sharedMesh == mesh, label + " prefab mesh reference is wrong.");
            Require(prefab.GetComponent<MeshRenderer>().sharedMaterials.Length == submeshes.Count, label + " material slots lost.");
            foreach (var component in prefab.GetComponentsInChildren<Component>(true))
                Require(component is Transform || component is MeshFilter || component is MeshRenderer,
                    label + " unexpected runtime component.");
            Require(mesh.blendShapeCount == 0 && mesh.bindposes.Length == 0, label + " static profile gained skin or morph data.");
            checks.Add(label + ": serialized positions, UV0, authored normals, tangents, submeshes and prefab references preserved" + (generatedNormals && bake.Mesh.Normals.Count==0 ? "; missing normals generated on derived asset." : "."));
            return mesh;
        }

        static void VerifyKnownCentimetreEdit(Mesh mesh, BakeDocument bake, string label)
        {
            // Independent expected coordinates for the eight-vertex front/back fixture.
            // Testing these constants prevents producer and importer from sharing a scale bug.
            var baseline = new[]
            {
                new Vector3(-.1f,-.05f,-.02f), new Vector3(.1f,-.05f,-.02f),
                new Vector3(.1f,.05f,-.02f), new Vector3(-.1f,.05f,-.02f),
                new Vector3(-.1f,-.05f,-.02f), new Vector3(.1f,-.05f,-.02f),
                new Vector3(.1f,.05f,-.02f), new Vector3(-.1f,.05f,-.02f)
            };
            var actual = mesh.vertices;
            Require(actual.Length == 8 && mesh.subMeshCount == 2, "Expected the original NF-0 split-corner fixture.");
            for (int i = 0; i < baseline.Length; i++)
            {
                var expected = baseline[i] + Vector(bake.Transform.Translation);
                if (i == 0) expected.x += .01f;
                Near(actual[i], expected, label + " independent 1 cm edit vertex " + i);
            }
            Require(mesh.normals[0] == -mesh.normals[4], label + " front/back normal seam was merged.");
        }

        static void VerifyExplicitAttachment(string manifestPath, Vector3[] positions, List<string> folders)
        {
            var parent = new GameObject("Explicit fixture parent");
            BakeImportResult result = null;
            try
            {
                parent.transform.position = new Vector3(.4f, .7f, -.2f);
                parent.transform.rotation = Quaternion.Euler(0, 30, 0);
                parent.transform.localScale = Vector3.one * 2f;
                result = BakeImporter.Import(manifestPath, "Assets", parent.transform, true);
                folders.Add(result.AssetDirectory);
                Require(result.SceneInstance != null && result.SceneInstance.transform.parent == parent.transform,
                    "Explicit parent was not used.");
                var attached = result.SceneInstance.GetComponent<MeshFilter>().sharedMesh.vertices;
                for (int i = 0; i < attached.Length; i++)
                    Near(result.SceneInstance.transform.TransformPoint(attached[i]), positions[i], "Attachment world vertex " + i);
            }
            finally
            {
                if (result != null && result.SceneInstance != null) Object.DestroyImmediate(result.SceneInstance);
                Object.DestroyImmediate(parent);
            }
        }

        static void VerifyRejectedOutputDoesNotCreateAssets(string manifestPath)
        {
            int count = Directory.GetDirectories(Application.dataPath, "NyaForgeImport-*").Length;
            bool rejected = false;
            try { BakeImporter.Import(manifestPath, "Assets/../Outside"); }
            catch (ArgumentException) { rejected = true; }
            Require(rejected, "Traversal output was accepted.");
            Require(count == Directory.GetDirectories(Application.dataPath, "NyaForgeImport-*").Length,
                "Rejected output created an asset folder.");
        }

        static string RequiredArgument(string[] args, string name)
        {
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == name) return args[i + 1];
            throw new ArgumentException("Missing argument: " + name);
        }
        static Vector3 Vector(Vec3 value) { return new Vector3(value.X, value.Y, value.Z); }
        static void Near(Vector3 actual, Vector3 expected, string message)
        { Near(actual.x, expected.x, message + " X"); Near(actual.y, expected.y, message + " Y"); Near(actual.z, expected.z, message + " Z"); }
        static void Near(float actual, float expected, string message)
        { Require(!float.IsNaN(actual) && !float.IsInfinity(actual) && Mathf.Abs(actual - expected) <= ToleranceMetres, message + ": " + actual + " != " + expected); }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
