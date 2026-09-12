using System;
using System.IO;
using NyaForge.Authoring;
using NyaForge.Rendering;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace NyaForge.UnityBridge.Editor
{
    public sealed class BakeImportResult
    {
        public string AssetDirectory { get; internal set; }
        public string MeshPath { get; internal set; }
        public string PrefabPath { get; internal set; }
        public string[] MaterialPaths { get; internal set; }
        public GameObject SceneInstance { get; internal set; }
    }

    /// <summary>Converts the restricted NF-0 static profile to independent Unity assets.</summary>
    public static partial class BakeImporter
    {
        public static BakeImportResult Import(string manifestPath, string assetParent = "Assets",
            Transform sceneParent = null, bool createSceneInstance = false)
        {
            // Read and validate every blob before creating any Unity asset.
            var bake = BakeStore.Read(manifestPath);
            return ImportValidated(bake,null,assetParent,sceneParent,createSceneInstance,identity:BakeOutputIdentity.Read(manifestPath,bake));
        }
        static BakeImportResult ImportValidated(BakeDocument bake,SurfaceBakeDocument surface,string assetParent,
            Transform sceneParent,bool createSceneInstance,MaterialBakeDocument authored=null,Action<string> verificationCheckpoint=null,MultiMaterialBakeDocument multiple=null,BakeOutputIdentity identity=null)
        {
            ValidateAssetParent(assetParent);
            if (sceneParent != null && EditorUtility.IsPersistent(sceneParent))
                throw new ArgumentException("Scene parent must be an object in an open scene, not a prefab asset.");
            if (sceneParent != null && !createSceneInstance)
                throw new ArgumentException("A parent requires an explicit scene instance.");
            if (sceneParent != null)
            {
                var scale = sceneParent.lossyScale;
                if (scale.x <= 0 || Mathf.Abs(scale.x - scale.y) > 0.00001f ||
                    Mathf.Abs(scale.x - scale.z) > 0.00001f)
                    throw new ArgumentException("NF-0 attachment requires a positive uniform parent scale.");
            }

            if(multiple!=null) foreach(var slot in multiple.Slots) StandardMaterialAdapter.RequireShader(slot.Surface.Material);
            var shader = multiple!=null ? StandardMaterialAdapter.RequireShader(multiple.Slots[0].Surface.Material) : authored==null ? Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit") : StandardMaterialAdapter.RequireShader(authored.Material);
            if (shader == null)
                throw new InvalidOperationException("A Standard or Universal Render Pipeline/Lit shader is required.");

            string folder = null;
            Mesh mesh = null;
            GameObject temporary = null;
            GameObject instance = null;
            var ownedMaterials = new Material[bake.Mesh.Submeshes.Count];
            try
            {
                // A unique, newly owned folder is the only cleanup target. Existing content is never replaced.
                var folderName = "NyaForgeImport-" + Guid.NewGuid().ToString("N");
                var candidate = assetParent.TrimEnd('/') + "/" + folderName;
                if (Directory.Exists(AbsoluteAssetPath(candidate)) || File.Exists(AbsoluteAssetPath(candidate)))
                    throw new IOException("Import folder already exists.");
                string guid = AssetDatabase.CreateFolder(assetParent, folderName);
                if (string.IsNullOrEmpty(guid)) throw new IOException("Could not create the import folder.");
                folder = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.Equals(folder, candidate, StringComparison.Ordinal))
                    throw new IOException("Unity created an unexpected import folder.");

                mesh = CreateMesh(bake);
                // Material lighting needs normals. Only the derived asset is completed;
                // legacy profiles and the content-addressed source remain unchanged.
                if (authored != null || multiple!=null) DisplayMeshNormals.Ensure(mesh);
                string meshPath = folder + "/Mesh.asset";
                AssetDatabase.CreateAsset(mesh, meshPath);
                var materialPaths = new string[ownedMaterials.Length];
                var baseColor = authored?.BaseColor!=null ? ImportBaseColor(folder,authored.CopyPng(),authored.BaseColor.Width,authored.BaseColor.Height) : surface == null ? null : ImportBaseColor(folder,surface);
                if(multiple!=null) ImportSlotMaterials(folder,multiple,ownedMaterials,materialPaths);
                for (int i = 0; multiple==null && i < ownedMaterials.Length; i++)
                {
                    var material = authored==null ? new Material(shader) { name = "NyaForge Preview " + i } : StandardMaterialAdapter.Create(authored.Material);
                    ownedMaterials[i] = material;
                    if (material.HasProperty("_Color")) material.SetColor("_Color", new Color(0.68f, 0.74f, 0.84f, 1f));
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", new Color(0.68f, 0.74f, 0.84f, 1f));
                    if (baseColor != null) { if(authored==null) ApplyBaseColor(material,baseColor);else material.mainTexture=baseColor; }
                    materialPaths[i] = folder + "/Material-" + i + ".mat";
                    AssetDatabase.CreateAsset(material, materialPaths[i]);
                }
                verificationCheckpoint?.Invoke(folder);
                temporary = new GameObject("NyaForge Static Mesh");
                temporary.AddComponent<MeshFilter>().sharedMesh = mesh;
                temporary.AddComponent<MeshRenderer>().sharedMaterials = ownedMaterials;
                string prefabPath = folder + "/StaticMesh.prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(temporary, prefabPath, out bool success);
                if (!success || prefab == null) throw new IOException("Could not save the imported prefab.");
                // Do not flush unrelated dirty assets in the receiving project.
                AssetDatabase.SaveAssetIfDirty(mesh);
                foreach (var material in ownedMaterials) AssetDatabase.SaveAssetIfDirty(material);
                AssetDatabase.SaveAssetIfDirty(prefab);
                ImportOwnership.Capture(folder,bake,identity);

                if (createSceneInstance)
                {
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    if (instance == null) throw new InvalidOperationException("Could not instantiate the prefab.");
                    // The bake uses avatar-rest metres. Parenting retains that world placement;
                    // it is not an inferred neck offset or a skinned binding conversion.
                    if (sceneParent != null) instance.transform.SetParent(sceneParent, true);
                    Undo.RegisterCreatedObjectUndo(instance, "Import NyaForge mesh");
                }

                return new BakeImportResult
                {
                    AssetDirectory = folder, MeshPath = meshPath, PrefabPath = prefabPath,
                    MaterialPaths = materialPaths, SceneInstance = instance
                };
            }
            catch
            {
                if (instance != null) Object.DestroyImmediate(instance);
                if (temporary != null) { Object.DestroyImmediate(temporary); temporary = null; }
                // Only non-persistent temporaries need explicit destruction. AssetDatabase owns saved assets.
                if (mesh != null && !EditorUtility.IsPersistent(mesh)) Object.DestroyImmediate(mesh);
                foreach (var material in ownedMaterials)
                    if (material != null && !EditorUtility.IsPersistent(material)) Object.DestroyImmediate(material);
                if (folder != null) DeleteOwnedFolder(folder, assetParent);
                throw;
            }
            finally
            {
                if (temporary != null) Object.DestroyImmediate(temporary);
            }
        }

        static Mesh CreateMesh(BakeDocument bake)
        {
            var data = bake.Mesh;
            var positions = new Vector3[data.VertexCount];
            var normals = new Vector3[data.Normals.Count];
            var tangents = new Vector4[data.Tangents.Count];
            var uv = new Vector2[data.Uv0.Count];
            for (int i = 0; i < positions.Length; i++)
            {
                var value = bake.Transform.ToAvatarPoint(data.Positions[i]);
                if (!Finite(value.X) || !Finite(value.Y) || !Finite(value.Z))
                    throw new InvalidDataException("The transformed mesh contains a non-finite position.");
                positions[i] = new Vector3(value.X, value.Y, value.Z);
            }
            for (int i = 0; i < normals.Length; i++)
                normals[i] = new Vector3(data.Normals[i].X, data.Normals[i].Y, data.Normals[i].Z);
            for (int i = 0; i < tangents.Length; i++)
                tangents[i] = new Vector4(data.Tangents[i].X, data.Tangents[i].Y, data.Tangents[i].Z, data.Tangents[i].W);
            for (int i = 0; i < uv.Length; i++) uv[i] = new Vector2(data.Uv0[i].X, data.Uv0[i].Y);
            var mesh = new Mesh
            {
                name = "NyaForge Static Mesh",
                indexFormat = positions.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
            };
            try
            {
                mesh.vertices = positions;
                mesh.normals = normals;
                mesh.tangents = tangents;
                mesh.uv = uv;
                var submeshes = data.Submeshes;
                mesh.subMeshCount = submeshes.Count;
                for (int i = 0; i < submeshes.Count; i++) mesh.SetTriangles(submeshes[i], i, false);
                mesh.RecalculateBounds();
                var size = mesh.bounds.size;
                var center = mesh.bounds.center;
                if (!Finite(size.x) || !Finite(size.y) || !Finite(size.z) ||
                    !Finite(center.x) || !Finite(center.y) || !Finite(center.z))
                    throw new InvalidDataException("The transformed mesh bounds exceed Unity's numeric range.");
                return mesh;
            }
            catch { Object.DestroyImmediate(mesh); throw; }
        }

        static void ValidateAssetParent(string assetParent)
        {
            if (string.IsNullOrEmpty(assetParent) || assetParent.Contains("\\") ||
                (assetParent != "Assets" && !assetParent.StartsWith("Assets/", StringComparison.Ordinal)))
                throw new ArgumentException("Output parent must be an existing Assets folder.");
            AbsoluteAssetPath(assetParent);
            if (!AssetDatabase.IsValidFolder(assetParent))
                throw new ArgumentException("Output parent must be an existing Assets folder.");
        }

        static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }

        static string AbsoluteAssetPath(string assetPath)
        {
            string root = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string relative = assetPath == "Assets" ? "" : assetPath.Substring("Assets/".Length);
            if (relative.Split('/').Length > 0)
                foreach (string part in relative.Split('/'))
                    if (part == "." || part == "..") throw new ArgumentException("Relative traversal is not allowed.");
            string result = Path.GetFullPath(Path.Combine(root, relative));
            if (!string.Equals(result, root, StringComparison.OrdinalIgnoreCase) &&
                !result.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Output must remain under Assets.");
            return result;
        }

        static void DeleteOwnedFolder(string folder, string assetParent)
        {
            string prefix = assetParent.TrimEnd('/') + "/NyaForgeImport-";
            if (!folder.StartsWith(prefix, StringComparison.Ordinal) ||
                folder.Substring(prefix.Length).Length != 32 ||
                !Guid.TryParseExact(folder.Substring(prefix.Length), "N", out _))
                throw new IOException("Refused cleanup of a folder not owned by this import.");
            AbsoluteAssetPath(folder);
            if (!AssetDatabase.DeleteAsset(folder))
                Debug.LogError("NyaForge import failed; the owned incomplete folder could not be removed: " + folder);
        }
    }
}
