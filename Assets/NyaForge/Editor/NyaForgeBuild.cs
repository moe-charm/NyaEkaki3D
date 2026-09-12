using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;
using Viewer.Contracts;
using Viewer.Runtime;

namespace NyaForge.EditorTools
{
    /// <summary>Builds a public, self-generated fixture and the existing viewer without avatar source assets.</summary>
    public static class NyaForgeBuild
    {
        const string AssetRoot = "Assets/LocalContent/NyaForgeFixture";
        const string PackId = "nyaforge-synthetic-fixture";
        const string BundleId = "fixture";
        const string ScenePath = "Assets/Viewer/Scenes/Viewer.unity";
        static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        [MenuItem("NyaForge/Build/Fixture and Windows player")]
        public static void BuildAll()
        {
            BuildFixture();
            BuildPlayer();
        }

        [MenuItem("NyaForge/Build/Public synthetic fixture")]
        public static void BuildFixture()
        {
            RequireWindows();
            string token = Resources.Load<TextAsset>("ViewerCompatibility")?.text.Trim();
            if (string.IsNullOrEmpty(token)) throw new InvalidOperationException("ViewerCompatibility resource is missing.");
            Directory.CreateDirectory(Path.Combine(ProjectRoot, AssetRoot));
            AssetDatabase.Refresh();
            var assetPaths = CreateFixtureAssets();
            AssetDatabase.SaveAssets();

            // Revisions are immutable once published. A failed build leaves the previous current pointer usable.
            string revision = "r-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            string library = Path.Combine(ProjectRoot, "GeneratedPacks", "NyaForgeFixture");
            string revisionRoot = Path.Combine(library, "revisions", revision);
            Directory.CreateDirectory(revisionRoot);
            var built = BuildPipeline.BuildAssetBundles(revisionRoot, new[]
            {
                new AssetBundleBuild { assetBundleName = BundleId, assetNames = assetPaths }
            }, BuildAssetBundleOptions.ChunkBasedCompression | BuildAssetBundleOptions.StrictMode,
                BuildTarget.StandaloneWindows64);
            if (built == null) throw new InvalidOperationException("Synthetic fixture AssetBundle build failed.");

            string bundlePath = Path.Combine(revisionRoot, BundleId);
            var manifest = new PackManifest
            {
                schemaVersion = 1, packId = PackId, revision = revision,
                displayName = "NyaForge synthetic fixture", avatarId = "nyaforge-mannequin-v1",
                avatarSourceVersion = "synthetic-1", rigProfileId = "nyaforge-neck-rig-v1",
                toolchain = new Toolchain
                {
                    unityVersion = Application.unityVersion, buildTarget = "StandaloneWindows64", architecture = "x86_64",
                    graphicsApi = "Direct3D11", renderPipeline = "BuiltIn", colorSpace = PlayerSettings.colorSpace.ToString(),
                    lilToonVersion = "none", packageLockSha256 = JsonFiles.Sha256(Path.Combine(ProjectRoot, "Packages/packages-lock.json")),
                    shaderProfileId = "nyaforge-built-in-fixture-v1", compatibilityToken = token,
                    importRecipeVersion = 1, runtimeContractVersion = 1
                },
                bundles = new[] { new BundleRecord { id = BundleId, path = BundleId, sha256 = JsonFiles.Sha256(bundlePath),
                    sizeBytes = new FileInfo(bundlePath).Length, dependsOn = Array.Empty<string>() } },
                avatarPrefab = new AssetReference { bundleId = BundleId, assetName = (AssetRoot + "/Fixture.prefab").ToLowerInvariant() },
                renderers = new[]
                {
                    RendererRow("fixture-body", "Body", "Body", "SkinnedMeshRenderer", "body"),
                    RendererRow("fixture-head", "Head", "Rig/Hips/Neck/Head", "MeshRenderer", "body"),
                    RendererRow("fixture-collar", "Collar", "Rig/Hips/Neck/Collar", "MeshRenderer", "accessory")
                },
                morphBindings = Array.Empty<MorphBinding>(),
                clips = new[] { ClipRow("pose-rest", "Rest", "Rest.anim", 1), ClipRow("pose-neck-tilt", "Neck tilt", "NeckTilt.anim", 2) },
                defaultClipId = "pose-rest", excludedFeatures = new[] { "humanoid-retargeting", "blend-shapes", "third-party-avatar-content" }
            };
            Validation.Manifest(manifest, token);
            string manifestPath = Path.Combine(revisionRoot, "manifest.json");
            string manifestHash = JsonFiles.AtomicWrite(manifestPath, manifest);
            PackStore.Verify(manifestPath, token, manifestHash);
            ValidateBundle(manifest, revisionRoot);
            JsonFiles.AtomicWrite(Path.Combine(library, "current.StandaloneWindows64.json"), new CurrentReference
            {
                schemaVersion = 1, packId = PackId, revision = revision, buildTarget = "StandaloneWindows64",
                manifestPath = "revisions/" + revision + "/manifest.json", manifestSha256 = manifestHash
            }, allowReplace: true);
            File.WriteAllText(Path.Combine(library, "README.txt"),
                "Generated entirely from NyaForge source. No external avatar or licensed asset is included.\n" +
                "Body: CPU-readable skinned mesh, meter units, hips/neck bind poses. Head and collar: rigid neck children.\n" +
                "This is a technical fixture, not a Humanoid avatar or a clothing-fit quality test.\n" +
                "Launch NyaForge.exe --library \"" + library + "\"\n");
            Debug.Log("NYAFORGE_FIXTURE_OK " + manifestPath);
        }

        [MenuItem("NyaForge/Build/Windows player")]
        public static void BuildPlayer()
        {
            RequireWindows();
            if (!File.Exists(ScenePath)) throw new FileNotFoundException("Viewer scene is missing.", ScenePath);
            if (GraphicsSettings.currentRenderPipeline != null) throw new InvalidOperationException("The viewer requires the Built-in Render Pipeline.");
            foreach (string shaderName in new[] { "Standard", "Viewer/UnlitColor", "Viewer/BodyDiagnostic" })
                if (Shader.Find(shaderName) == null) throw new InvalidOperationException("Required shader is missing: " + shaderName);
            var arguments = Environment.GetCommandLineArgs();
            int nameIndex = Array.IndexOf(arguments, "--nyaforge-build-name");
            string buildName = nameIndex >= 0 && nameIndex + 1 < arguments.Length ? arguments[nameIndex + 1] : "Windows";
            if (!System.Text.RegularExpressions.Regex.IsMatch(buildName, "^[A-Za-z0-9_-]+$"))
                throw new InvalidOperationException("Invalid build directory name");
            string output = Path.Combine(ProjectRoot, "Builds", buildName, "NyaForge.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            bool splash = PlayerSettings.SplashScreen.show;
            bool unityLogo = PlayerSettings.SplashScreen.showUnityLogo;
            bool autoGraphics = PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64);
            var graphics = PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);
            try
            {
                PlayerSettings.SplashScreen.show = false;
                PlayerSettings.SplashScreen.showUnityLogo = false;
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
                PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { ScenePath }, locationPathName = output, target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.StrictMode
                });
                if (report.summary.result != BuildResult.Succeeded)
                    throw new InvalidOperationException("Windows player build failed: " + report.summary.result + " (errors: " + report.summary.totalErrors + ")");
                Debug.Log("NYAFORGE_PLAYER_OK " + output + " bytes=" + report.summary.totalSize);
            }
            finally
            {
                PlayerSettings.SplashScreen.show = splash;
                PlayerSettings.SplashScreen.showUnityLogo = unityLogo;
                PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, graphics);
                PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, autoGraphics);
            }
        }

        static void RequireWindows()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor)
                throw new PlatformNotSupportedException("This first build recipe supports the Windows Editor only.");
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64))
                throw new InvalidOperationException("Install Windows Build Support for this exact Unity Editor version.");
        }

        static RendererRecord RendererRow(string id, string name, string path, string type, string category) =>
            new RendererRecord { rendererId = id, displayName = name, path = path, rendererType = type,
                category = category, defaultVisible = true, required = true };

        static ClipRecord ClipRow(string id, string name, string asset, double duration) =>
            new ClipRecord { clipId = id, displayName = name, bundleId = BundleId,
                assetName = (AssetRoot + "/" + asset).ToLowerInvariant(), durationSeconds = duration, sampleRate = 30, required = true };

        static string[] CreateFixtureAssets()
        {
            var root = new GameObject("NyaForgeFixture");
            try
            {
                root.AddComponent<Animator>();
                var rig = Child(root.transform, "Rig", Vector3.zero);
                var hips = Child(rig, "Hips", new Vector3(0, .9f, 0));
                var neck = Child(hips, "Neck", new Vector3(0, .5f, 0));
                var body = Child(root.transform, "Body", Vector3.zero).gameObject.AddComponent<SkinnedMeshRenderer>();
                var bodyMesh = Box("Fixture body", new Vector3(.4f, .75f, .24f), new Vector3(0, .95f, 0));
                bodyMesh.boneWeights = bodyMesh.vertices.Select(v => new BoneWeight { boneIndex0 = 0, weight0 = 1 }).ToArray();
                bodyMesh.bindposes = new[] { hips.worldToLocalMatrix * body.transform.localToWorldMatrix, neck.worldToLocalMatrix * body.transform.localToWorldMatrix };
                body.sharedMesh = WriteAsset(bodyMesh, "Body.asset");
                body.bones = new[] { hips, neck }; body.rootBone = hips; body.updateWhenOffscreen = true;
                body.localBounds = body.sharedMesh.bounds;
                body.sharedMaterial = WriteAsset(Material("Body", new Color(.48f, .68f, .78f)), "Body.mat");
                var head = Child(neck, "Head", new Vector3(0, .16f, 0)).gameObject;
                head.AddComponent<MeshFilter>().sharedMesh = WriteAsset(Box("Fixture head", new Vector3(.25f, .28f, .23f), Vector3.zero), "Head.asset");
                head.AddComponent<MeshRenderer>().sharedMaterial = WriteAsset(Material("Head", new Color(.7f, .79f, .83f)), "Head.mat");
                var collar = Child(neck, "Collar", Vector3.zero).gameObject;
                collar.AddComponent<MeshFilter>().sharedMesh = WriteAsset(Ring(), "Collar.asset");
                collar.AddComponent<MeshRenderer>().sharedMaterial = WriteAsset(Material("Collar", new Color(.88f, .38f, .5f)), "Collar.mat");
                PrefabUtility.SaveAsPrefabAsset(root, AssetRoot + "/Fixture.prefab");
                WriteAsset(Pose("Rest", AnimationCurve.Linear(0, 0, 1, 0)), "Rest.anim");
                WriteAsset(Pose("NeckTilt", new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 20), new Keyframe(2, 0))), "NeckTilt.anim");
                return new[] { AssetRoot + "/Fixture.prefab", AssetRoot + "/Rest.anim", AssetRoot + "/NeckTilt.anim" };
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static Transform Child(Transform parent, string name, Vector3 position)
        {
            var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = position; return t;
        }

        static Material Material(string name, Color color)
        {
            var shader = Shader.Find("Viewer/UnlitColor");
            if (!shader) throw new InvalidOperationException("Fixture shader is missing.");
            return new Material(shader) { name = name, color = color };
        }

        static AnimationClip Pose(string name, AnimationCurve curve)
        {
            var clip = new AnimationClip { name = name, frameRate = 30, legacy = false };
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Rig/Hips/Neck", typeof(Transform), "localEulerAnglesRaw.z"), curve);
            clip.EnsureQuaternionContinuity();
            return clip;
        }

        static T WriteAsset<T>(T asset, string name) where T : UnityEngine.Object
        {
            string path = AssetRoot + "/" + name;
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing)
            {
                EditorUtility.CopySerialized(asset, existing);
                UnityEngine.Object.DestroyImmediate(asset);
                EditorUtility.SetDirty(existing);
                return existing;
            }
            if (File.Exists(path)) throw new IOException("Generated asset path is occupied by a different asset type: " + path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // Face-split cube provides deliberate UV/normal seams for later authoring probes.
        static Mesh Box(string name, Vector3 size, Vector3 center)
        {
            Vector3[] corners =
            {
                new Vector3(-1,-1,-1), new Vector3(1,-1,-1), new Vector3(1,1,-1), new Vector3(-1,1,-1),
                new Vector3(-1,-1,1), new Vector3(1,-1,1), new Vector3(1,1,1), new Vector3(-1,1,1)
            };
            int[][] faces = { new[] { 0,3,2,1 }, new[] { 4,5,6,7 }, new[] { 0,4,7,3 },
                new[] { 1,2,6,5 }, new[] { 0,1,5,4 }, new[] { 3,7,6,2 } };
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            foreach (var face in faces)
            {
                int start = vertices.Count;
                foreach (int i in face) vertices.Add(center + Vector3.Scale(corners[i], size * .5f));
                uv.AddRange(new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right });
                triangles.AddRange(new[] { start, start+1, start+2, start, start+2, start+3 });
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            return mesh;
        }

        static Mesh Ring()
        {
            const int segments = 16;
            var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                for (int corner = 0; corner < 4; corner++)
                {
                    float radius = corner < 2 ? .12f : .105f;
                    float y = corner == 0 || corner == 3 ? -.018f : .018f;
                    vertices.Add(new Vector3(Mathf.Sin(angle) * radius, y, Mathf.Cos(angle) * radius));
                    uv.Add(new Vector2((float)i / segments, corner / 3f));
                }
            }
            for (int i = 0; i < segments; i++)
                for (int j = 0; j < 4; j++)
                {
                    int a = i*4+j, b = i*4+(j+1)%4, c = a+4, d = b+4;
                    triangles.AddRange(new[] { a,c,b, b,c,d });
                }
            var mesh = new Mesh { name = "Fixture collar" };
            mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            return mesh;
        }

        static void ValidateBundle(PackManifest manifest, string directory)
        {
            var bundle = AssetBundle.LoadFromFile(Path.Combine(directory, BundleId));
            if (!bundle) throw new InvalidOperationException("Generated fixture bundle could not be reopened.");
            try
            {
                var prefab = bundle.LoadAsset<GameObject>(manifest.avatarPrefab.assetName);
                if (!prefab) throw new InvalidOperationException("Generated prefab is missing from bundle.");
                foreach (var row in manifest.renderers)
                {
                    var t = prefab.transform.Find(row.path);
                    Mesh mesh = row.rendererType == "SkinnedMeshRenderer"
                        ? t?.GetComponent<SkinnedMeshRenderer>()?.sharedMesh : t?.GetComponent<MeshFilter>()?.sharedMesh;
                    if (!mesh || !mesh.isReadable || mesh.vertexCount == 0)
                        throw new InvalidOperationException("Generated renderer mesh is missing or CPU-unreadable: " + row.rendererId);
                    if (mesh.triangles.Any(i => i < 0 || i >= mesh.vertexCount)) throw new InvalidOperationException("Invalid fixture mesh indices.");
                }
                foreach (var clip in manifest.clips)
                    if (!bundle.LoadAsset<AnimationClip>(clip.assetName)) throw new InvalidOperationException("Generated clip is missing: " + clip.clipId);
            }
            finally { bundle.Unload(true); }
        }
    }
}
