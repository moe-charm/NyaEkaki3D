using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;
using NyaForge.Authoring.Paint;

namespace NyaForge.Authoring
{
    public enum GlbExportProfile { StaticGeometry, SkinnedGeometry, SkinnedGeometryExtended }

    public sealed class GlbExportResult
    {
        public string Path { get; }
        /// <summary>Bounded sidecar describing the exact native snapshot and profile used for this GLB.</summary>
        public string ReportPath { get; }
        public GlbExportProfile Profile { get; }
        public int ObjectCount { get; }
        public GlbExportNodeMap NodeMap { get; }
        internal GlbExportResult(string path, string reportPath, GlbExportProfile profile, int objectCount, GlbExportNodeMap nodeMap)
        { Path = path; ReportPath = reportPath; Profile = profile; ObjectCount = objectCount; NodeMap = nodeMap; }
    }

    /// <summary>Actual glTF node indices emitted for each authored graph object.</summary>
    public sealed class GlbExportNodeMap
    {
        public IReadOnlyDictionary<string, int> MeshNodes { get; }
        public IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> BoneNodes { get; }
        public IReadOnlyDictionary<string, IReadOnlyList<int>> SkeletonNodes { get; }
        internal GlbExportNodeMap(IDictionary<string, int> meshes,
            IDictionary<string, IReadOnlyDictionary<string, int>> bones,
            IDictionary<string, IReadOnlyList<int>> skeletons)
        {
            MeshNodes = new System.Collections.ObjectModel.ReadOnlyDictionary<string, int>(meshes);
            BoneNodes = new System.Collections.ObjectModel.ReadOnlyDictionary<string, IReadOnlyDictionary<string, int>>(bones);
            SkeletonNodes = new System.Collections.ObjectModel.ReadOnlyDictionary<string, IReadOnlyList<int>>(skeletons);
        }
    }

    /// <summary>Writes standard glTF 2.0 GLB geometry without changing the native project.</summary>
    public static class GlbExportService
    {
        public const string FileName = "model.glb";
        public const string ReportFileName = "export-report.json";

        public static GlbExportResult ExportStatic(AuthoringWorkspace workspace, string instance, string document, long revision, string directory)
            => ExportStaticWithOverrides(workspace, instance, document, revision, directory, null);

        /// <summary>Writes static geometry while allowing the Workbench to supply its display-corrected mesh per object.</summary>
        public static GlbExportResult ExportStaticWithOverrides(AuthoringWorkspace workspace, string instance, string document, long revision, string directory,
            IReadOnlyDictionary<string, GraphMeshValue> meshOverrides)
        {
            ValidateRequest(workspace, instance, document, revision, directory);
            lock (workspace.Gate)
            {
                var objects = workspace.Document.Objects.Select(item =>
                {
                    GraphMeshValue overrideValue = null;
                    if (meshOverrides != null) meshOverrides.TryGetValue(item.ObjectId, out overrideValue);
                    return BuildStaticObject(item, overrideValue);
                }).ToArray();
                var paths = Write(directory, objects, (SkinnedObject[])null, GlbExportProfile.StaticGeometry,
                    workspace.Document.DocumentId, workspace.Document.DocumentRevision, workspace.Document.StateHash,
                    ReadSourceDiagnostics(workspace));
                return new GlbExportResult(paths.GlbPath, paths.ReportPath, GlbExportProfile.StaticGeometry, objects.Length, paths.NodeMap);
            }
        }

        public static GlbExportResult ExportSkinned(AuthoringWorkspace workspace, string instance, string document, long revision, string directory)
            => ExportSkinnedCore(workspace, instance, document, revision, directory, GlbExportProfile.SkinnedGeometry, null, false);

        /// <summary>Writes a skinned GLB while retaining a selected source node instance affine on the mesh node.</summary>
        public static GlbExportResult ExportSkinned(AuthoringWorkspace workspace, string instance, string document, long revision, string directory, SourceAffine instanceWorldTransform)
            => ExportSkinnedCore(workspace, instance, document, revision, directory, GlbExportProfile.SkinnedGeometry, _ => instanceWorldTransform, true);

        /// <summary>Writes a skinned GLB while retaining one explicit source node affine per graph object.</summary>
        public static GlbExportResult ExportSkinnedWithTransforms(AuthoringWorkspace workspace, string instance, string document, long revision, string directory, IReadOnlyDictionary<string, SourceAffine> instanceWorldTransforms)
            => ExportSkinnedCore(workspace, instance, document, revision, directory, GlbExportProfile.SkinnedGeometry,
                item => instanceWorldTransforms != null && instanceWorldTransforms.TryGetValue(item.ObjectId, out var value) ? value : null, false, instanceWorldTransforms, null);

        /// <summary>Writes skinned GLB while retaining source inverse-bind matrices per graph object.</summary>
        public static GlbExportResult ExportSkinnedWithTransforms(AuthoringWorkspace workspace, string instance, string document, long revision, string directory,
            IReadOnlyDictionary<string, SourceAffine> instanceWorldTransforms,
            IReadOnlyDictionary<string, IReadOnlyList<SourceAffine>> inverseBindMatrices,
            IReadOnlyDictionary<string, IReadOnlyList<SourceAffine>> jointLocalTransforms = null)
            => ExportSkinnedCore(workspace, instance, document, revision, directory, GlbExportProfile.SkinnedGeometry,
                item => instanceWorldTransforms != null && instanceWorldTransforms.TryGetValue(item.ObjectId, out var value) ? value : null, false,
                instanceWorldTransforms, inverseBindMatrices, jointLocalTransforms);

        static GlbExportResult ExportSkinnedCore(AuthoringWorkspace workspace, string instance, string document, long revision, string directory, GlbExportProfile profile, Func<AuthoringObject, SourceAffine> transformResolver, bool singleTransformMode, IReadOnlyDictionary<string, SourceAffine> transformMap = null, IReadOnlyDictionary<string, IReadOnlyList<SourceAffine>> inverseBindMap = null, IReadOnlyDictionary<string, IReadOnlyList<SourceAffine>> jointTransformMap = null)
        {
            ValidateRequest(workspace, instance, document, revision, directory);
            lock (workspace.Gate)
            {
                Checks.Require(!singleTransformMode || !workspace.Document.Objects.Any() || transformResolver(workspace.Document.Objects[0]) == null || workspace.Document.Objects.Count == 1,
                    "GLB_SKIN_MULTI_INSTANCE_TRANSFORM", "A selected node instance transform is only valid for a single-object export.");
                if (transformMap != null)
                    Checks.Require(transformMap.Keys.All(id => workspace.Document.Objects.Any(item => item.ObjectId == id)), "GLB_SKIN_INSTANCE_TRANSFORM", "A skinned instance transform references an unknown graph object.");
                var skinned = workspace.Document.Objects.Select(item => BuildSkinnedObject(item, transformResolver == null ? null : transformResolver(item),
                    inverseBindMap != null && inverseBindMap.TryGetValue(item.ObjectId, out var inverseBind) ? inverseBind : null,
                    jointTransformMap != null && jointTransformMap.TryGetValue(item.ObjectId, out var joints) ? joints : null, profile)).ToArray();
                ValidateSharedSkeleton(skinned);
                var paths = Write(directory, skinned.Select(item => item.Mesh).ToArray(), skinned, profile,
                    workspace.Document.DocumentId, workspace.Document.DocumentRevision, workspace.Document.StateHash,
                    ReadSourceDiagnostics(workspace));
                return new GlbExportResult(paths.GlbPath, paths.ReportPath, profile, skinned.Length, paths.NodeMap);
            }
        }

        /// <summary>Writes a skinned GLB with every authored influence split into JOINTS_n/WEIGHTS_n sets.</summary>
        public static GlbExportResult ExportSkinnedExtended(AuthoringWorkspace workspace, string instance, string document, long revision, string directory)
            => ExportSkinnedExtendedCore(workspace, instance, document, revision, directory, null, false);

        /// <summary>Writes an extended skinned GLB while retaining a selected source node instance affine.</summary>
        public static GlbExportResult ExportSkinnedExtended(AuthoringWorkspace workspace, string instance, string document, long revision, string directory, SourceAffine instanceWorldTransform)
            => ExportSkinnedExtendedCore(workspace, instance, document, revision, directory, _ => instanceWorldTransform, true);

        /// <summary>Writes an extended skinned GLB with one explicit source node affine per graph object.</summary>
        public static GlbExportResult ExportSkinnedExtendedWithTransforms(AuthoringWorkspace workspace, string instance, string document, long revision, string directory, IReadOnlyDictionary<string, SourceAffine> instanceWorldTransforms)
            => ExportSkinnedExtendedCore(workspace, instance, document, revision, directory,
                item => instanceWorldTransforms != null && instanceWorldTransforms.TryGetValue(item.ObjectId, out var value) ? value : null, false, instanceWorldTransforms, null);

        /// <summary>Writes extended skinned GLB while retaining source inverse-bind matrices per graph object.</summary>
        public static GlbExportResult ExportSkinnedExtendedWithTransforms(AuthoringWorkspace workspace, string instance, string document, long revision, string directory,
            IReadOnlyDictionary<string, SourceAffine> instanceWorldTransforms,
            IReadOnlyDictionary<string, IReadOnlyList<SourceAffine>> inverseBindMatrices,
            IReadOnlyDictionary<string, IReadOnlyList<SourceAffine>> jointLocalTransforms = null)
            => ExportSkinnedExtendedCore(workspace, instance, document, revision, directory,
                item => instanceWorldTransforms != null && instanceWorldTransforms.TryGetValue(item.ObjectId, out var value) ? value : null, false,
                instanceWorldTransforms, inverseBindMatrices, jointLocalTransforms);

        static GlbExportResult ExportSkinnedExtendedCore(AuthoringWorkspace workspace, string instance, string document, long revision, string directory, Func<AuthoringObject, SourceAffine> transformResolver, bool singleTransformMode, IReadOnlyDictionary<string, SourceAffine> transformMap = null, IReadOnlyDictionary<string, IReadOnlyList<SourceAffine>> inverseBindMap = null, IReadOnlyDictionary<string, IReadOnlyList<SourceAffine>> jointTransformMap = null)
        {
            ValidateRequest(workspace, instance, document, revision, directory);
            lock (workspace.Gate)
            {
                Checks.Require(!singleTransformMode || !workspace.Document.Objects.Any() || transformResolver(workspace.Document.Objects[0]) == null || workspace.Document.Objects.Count == 1,
                    "GLB_SKIN_MULTI_INSTANCE_TRANSFORM", "A selected node instance transform is only valid for a single-object export.");
                if (transformMap != null)
                    Checks.Require(transformMap.Keys.All(id => workspace.Document.Objects.Any(item => item.ObjectId == id)), "GLB_SKIN_INSTANCE_TRANSFORM", "A skinned instance transform references an unknown graph object.");
                var skinned = workspace.Document.Objects.Select(item => BuildSkinnedObject(item, transformResolver == null ? null : transformResolver(item),
                    inverseBindMap != null && inverseBindMap.TryGetValue(item.ObjectId, out var inverseBind) ? inverseBind : null,
                    jointTransformMap != null && jointTransformMap.TryGetValue(item.ObjectId, out var joints) ? joints : null, GlbExportProfile.SkinnedGeometryExtended)).ToArray();
                ValidateSharedSkeleton(skinned);
                var paths = Write(directory, skinned.Select(item => item.Mesh).ToArray(), skinned, GlbExportProfile.SkinnedGeometryExtended,
                    workspace.Document.DocumentId, workspace.Document.DocumentRevision, workspace.Document.StateHash,
                    ReadSourceDiagnostics(workspace));
                return new GlbExportResult(paths.GlbPath, paths.ReportPath, GlbExportProfile.SkinnedGeometryExtended, skinned.Length, paths.NodeMap);
            }
        }

        static void ValidateSharedSkeleton(IReadOnlyList<SkinnedObject> objects)
        {
            Checks.Require(objects != null && objects.Count > 0, "NO_EXPORTABLE_OBJECT", "No skinned graph objects were provided.");
            string skeletonHash = objects[0].Skeleton.ContentHash;
            if (objects.All(item => item.Skeleton.ContentHash == skeletonHash)) return;

            // A source GLB may contain several skin resources that use
            // different subsets (or bind frames) of one hierarchy. Keep those
            // as separate glTF skins, but require a shared stable BoneId so
            // unrelated avatars cannot be combined merely because their bone
            // names happen to match. Stable IDs include the source hash.
            var common = new HashSet<string>(objects[0].Skeleton.Bones.Select(bone => bone.BoneId), StringComparer.Ordinal);
            for (int i = 1; i < objects.Count; i++)
                common.IntersectWith(objects[i].Skeleton.Bones.Select(bone => bone.BoneId));
            Checks.Require(common.Count > 0, "GLB_SKIN_SHARED_SKELETON", "Skinned multi-object GLB export requires a shared source skeleton or explicit attachment conversion.");
        }

        static void ValidateRequest(AuthoringWorkspace workspace, string instance, string document, long revision, string directory)
        {
            if (workspace == null) throw new ArgumentNullException(nameof(workspace));
            lock (workspace.Gate)
            {
                Checks.Require(!workspace.Executing, "REENTRANT_EXPORT", "Cannot export during a projection transaction.");
                Checks.Require(workspace.InstanceId == instance, "STALE_INSTANCE", "Export targets another instance.");
                Checks.Require(workspace.Document.DocumentId == document, "DOCUMENT_CHANGED", "Export targets another document.");
                Checks.Require(workspace.Document.DocumentRevision == revision, "REVISION_CONFLICT", "Document changed before export.");
                Checks.Require(!workspace.Document.IsEmpty, "NO_EXPORTABLE_OBJECT", "Add a mesh before exporting.");
                Checks.Require(workspace.Preview.IsComplete && !workspace.Preview.IsStale, "GRAPH_INCOMPLETE", "Export requires complete current evaluation.");
                Checks.Require(!workspace.Document.Objects.Any(item => !item.IsStaticProfile && item.Graph.Nodes.Values.Any(node => node.TypeId == BuiltinNodes.Attachment)),
                    "GLB_ATTACHMENT_METADATA_UNSUPPORTED", "Standard GLB does not preserve object attachment metadata; use native project export.");
                Checks.Require(!Directory.Exists(directory) && !File.Exists(directory), "EXPORT_DESTINATION_EXISTS", "Export destination already exists.");
            }
        }

        internal sealed class MeshObject
        {
            public MeshData Mesh;
            public RestTransform Transform;
            public MorphSet Morphs;
            public IReadOnlyDictionary<string, float> MorphWeights;
            public string Name;
            public SourceAffine Affine;
            public GraphMaterialValue Material;
            public GraphImageValue BaseColor;
            public IReadOnlyDictionary<int, MaterialSlotBinding> SlotMaterials;
        }

        internal sealed class SkinnedObject
        {
            public MeshObject Mesh;
            public SkeletonDefinition Skeleton;
            public SkinBinding Binding;
            public PoseSet Pose;
            public IReadOnlyList<SourceAffine> InverseBindMatrices;
            public IReadOnlyList<SourceAffine> JointLocalTransforms;
            public IReadOnlyDictionary<string, SourceAffine> InverseBindByBone;
            public IReadOnlyDictionary<string, SourceAffine> JointLocalByBone;
        }

        static IReadOnlyDictionary<string, ImportedGlbDiagnostics> ReadSourceDiagnostics(AuthoringWorkspace workspace)
        {
            var bytes = workspace.Attachments.Read(ProjectAttachments.ImportDiagnostics);
            return bytes == null
                ? new Dictionary<string, ImportedGlbDiagnostics>(StringComparer.Ordinal)
                : ImportedGlbDiagnosticsCodec.Read(bytes);
        }

        static MeshObject BuildStaticObject(AuthoringObject item, GraphMeshValue meshOverride = null)
        {
            var evaluation = item.EvaluateGraph();
            Checks.Require(evaluation.IsComplete && evaluation.Output != null && evaluation.Output.Mesh != null,
                "GRAPH_INCOMPLETE", "Static GLB export requires a complete renderable graph.");
            var output = meshOverride ?? evaluation.Output;
            Checks.Require(output.Mesh != null, "GRAPH_INCOMPLETE", "Static GLB export requires a complete renderable graph.");
            return new MeshObject { Mesh = output.Mesh, Transform = output.Transform, Name = item.ObjectId,
                Material = output.Material, BaseColor = output.BaseColor, SlotMaterials = output.SlotMaterials };
        }

        static SkinnedObject BuildSkinnedObject(AuthoringObject item, SourceAffine instanceWorldTransform, IReadOnlyList<SourceAffine> inverseBindMatrices, IReadOnlyList<SourceAffine> jointLocalTransforms, GlbExportProfile profile)
        {
            Checks.Require(!item.IsStaticProfile, "GLB_SKIN_PROFILE", "Skinned GLB export requires a graph object.");
            var graph = item.Graph; var evaluation = item.EvaluateGraph();
            Checks.Require(evaluation.IsComplete && evaluation.Output?.Mesh != null, "GRAPH_INCOMPLETE", "Skinned GLB export requires a complete renderable graph.");
            var sourceNodes = graph.Nodes.Values.Where(node => node.TypeId == BuiltinNodes.MeshSource).ToArray();
            var skeletonNodes = graph.Nodes.Values.Where(node => node.TypeId == BuiltinNodes.Skeleton && node.Skeleton != null).ToArray();
            var bindingNodes = graph.Nodes.Values.Where(node => node.TypeId == BuiltinNodes.SkinBind && node.Binding != null).ToArray();
            var poseNodes = graph.Nodes.Values.Where(node => node.TypeId == BuiltinNodes.Pose && node.Pose != null).ToArray();
            Checks.Require(sourceNodes.Length == 1 && skeletonNodes.Length == 1 && bindingNodes.Length == 1 && poseNodes.Length <= 1,
                "GLB_SKIN_GRAPH", "Skinned GLB export requires one source, skeleton, skin binding and optional pose.");
            var source = sourceNodes[0]; var skeleton = skeletonNodes[0].Skeleton;
            Checks.Require(source.Transform.Scale == 1f && source.Transform.Translation.X == 0f && source.Transform.Translation.Y == 0f && source.Transform.Translation.Z == 0f,
                "GLB_SKIN_TRANSFORM", "Skinned GLB export requires an identity source transform.");
            var authoredOutput = evaluation.Output.Mesh;
            Checks.Require(evaluation.Output.Transform.Scale == 1f && evaluation.Output.Transform.Translation.X == 0f && evaluation.Output.Transform.Translation.Y == 0f && evaluation.Output.Transform.Translation.Z == 0f,
                "GLB_SKIN_TRANSFORM", "Skinned GLB export requires an identity output transform.");
            // Material and paint nodes are evaluated as part of the graph and
            // their result is already carried by evaluation.Output.  Keep the
            // whitelist explicit so export never guesses an arbitrary node,
            // while allowing an imported skinned GLB to retain its appearance.
            var allowed = new HashSet<string>(new[] {
                BuiltinNodes.MeshSource, BuiltinNodes.EditMesh, BuiltinNodes.MorphSet,
                BuiltinNodes.MorphDeform, BuiltinNodes.Skeleton, BuiltinNodes.SkinBind,
                BuiltinNodes.Pose, BuiltinNodes.SkinDeform, BuiltinNodes.Output,
                BuiltinNodes.PoseSource,
                BuiltinNodes.StandardMaterial, BuiltinNodes.AssignMaterial,
                BuiltinNodes.AssignMaterials, BuiltinNodes.Paint, BuiltinNodes.LayeredPaint
            }, StringComparer.Ordinal);
            Checks.Require(graph.Nodes.Values.All(node => allowed.Contains(node.TypeId)), "GLB_SKIN_GRAPH", "Skinned GLB export does not guess unsupported graph nodes.");
            var morphNode = graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.MorphSet && node.Morphs != null);
            var morphDeform = graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.MorphDeform);
            var weights = morphDeform?.MorphWeights ?? new Dictionary<string, float>(StringComparer.Ordinal);
            Checks.Require(weights.Count == 0 || weights.Values.All(value => value == 0f), "GLB_MORPH_EDIT_UNSUPPORTED", "Skinned GLB export requires morph weights to be zero; bake a posed morph into static GLB or native project export.");
            var binding = bindingNodes[0].Binding.ValidateFor(authoredOutput, skeleton);
            // This interchange profile writes one JOINTS_0/WEIGHTS_0 set. Do
            // not silently drop fifth-and-later influences while producing a
            // file that claims to preserve the skin.
            if (profile == GlbExportProfile.SkinnedGeometry)
                Checks.Require(binding.Weights.Values.All(values => values.Count <= 4),
                    "GLB_SKIN_INFLUENCES", "Skinned GLB export supports at most four influences per vertex; use extended or native export for higher influence counts.");
            MorphSet morphs = morphNode == null ? null : morphNode.Morphs.ValidateFor(source.SourceMesh);
            var pose = poseNodes.Length == 0 ? DefaultPose(skeleton) : poseNodes[0].Pose.ValidateFor(skeleton);
            foreach (var bone in skeleton.Bones)
            {
                var value = pose.ByBoneId[bone.BoneId].Transform;
                Checks.Require(value.XAxis.X == 1f && value.XAxis.Y == 0f && value.XAxis.Z == 0f && value.YAxis.X == 0f && value.YAxis.Y == 1f && value.YAxis.Z == 0f && value.ZAxis.X == 0f && value.ZAxis.Y == 0f && value.ZAxis.Z == 1f && value.Translation.X == bone.Head.X && value.Translation.Y == bone.Head.Y && value.Translation.Z == bone.Head.Z,
                    "GLB_SKIN_POSE_UNSUPPORTED", "Skinned GLB export currently supports the rest pose only.");
            }
            if (morphs == null) Checks.Require(weights.Count == 0, "GLB_MORPH_UNRESOLVED", "Morph weights require a retained morph set.");
            if (morphs != null) foreach (var pair in weights) Checks.Require(morphs.ById.ContainsKey(pair.Key), "GLB_MORPH_UNRESOLVED", "Morph weight references an unknown target.");
            return new SkinnedObject
            {
                Mesh = new MeshObject { Mesh = authoredOutput, Transform = evaluation.Output.Transform, Morphs = morphs, MorphWeights = weights, Name = item.ObjectId, Affine = instanceWorldTransform,
                    Material = evaluation.Output.Material, BaseColor = evaluation.Output.BaseColor, SlotMaterials = evaluation.Output.SlotMaterials },
                Skeleton = skeleton, Binding = binding, Pose = pose
                , InverseBindMatrices = inverseBindMatrices, JointLocalTransforms = jointLocalTransforms,
                InverseBindByBone = MatrixMap(skeleton, inverseBindMatrices, "GLB_SKIN_BIND"),
                JointLocalByBone = MatrixMap(skeleton, jointLocalTransforms, "GLB_SKIN_SKELETON")
            };
        }

        static IReadOnlyDictionary<string, SourceAffine> MatrixMap(SkeletonDefinition skeleton, IReadOnlyList<SourceAffine> values, string code)
        {
            if (values == null) return null;
            Checks.Require(values.Count == skeleton.Bones.Count, code, "Retained skeleton matrices must cover every exported joint.");
            return skeleton.Bones.Select((bone, index) => new { bone.BoneId, Value = values[index] }).ToDictionary(x => x.BoneId, x => x.Value, StringComparer.Ordinal);
        }

        static PoseSet DefaultPose(SkeletonDefinition skeleton)
        { return PoseSet.Create(skeleton, skeleton.Bones.Select(bone => new BonePose(bone.BoneId, PoseTransform.FromTranslation(bone.Head)))); }

        static (string GlbPath, string ReportPath, GlbExportNodeMap NodeMap) Write(string directory, MeshObject[] objects, SkinnedObject[] skinned, GlbExportProfile profile,
            string documentId, long documentRevision, string stateHash,
            IReadOnlyDictionary<string, ImportedGlbDiagnostics> sourceDiagnostics)
        {
            Checks.Require(objects != null && objects.Length > 0, "NO_EXPORTABLE_OBJECT", "No mesh objects were provided.");
            if (profile == GlbExportProfile.SkinnedGeometry || profile == GlbExportProfile.SkinnedGeometryExtended)
                Checks.Require(skinned != null && skinned.Length == objects.Length, "GLB_SKIN_OBJECT_COUNT", "Each skinned mesh must have a matching binding.");
            string staging = directory + ".staging-" + Guid.NewGuid().ToString("N");
            try
            {
                Directory.CreateDirectory(staging);
                var built = GlbWriter.BuildMany(objects, skinned, profile); byte[] bytes = built.Bytes;
                Checks.Require(bytes.Length <= AuthoringLimits.MaxGlbExportBytes, "BUDGET_EXCEEDED", "GLB output exceeds the 128 MiB budget.");
                string path = Path.Combine(staging, FileName); File.WriteAllBytes(path, bytes);
                string reportPath = Path.Combine(staging, ReportFileName);
                var report = new JObject
                {
                    ["version"] = 1,
                    ["profile"] = profile.ToString(),
                    ["units"] = "meters",
                    ["coordinates"] = Storage.Coordinates,
                    ["documentId"] = documentId,
                    ["documentRevision"] = documentRevision,
                    ["stateHash"] = stateHash,
                    ["glbHash"] = Checks.Hash(bytes),
                    ["objectCount"] = objects.Length,
                    ["objects"] = new JArray(objects.Select(item => new JObject
                    {
                        ["objectId"] = item.Name,
                        ["vertexCount"] = item.Mesh.VertexCount,
                        ["triangleCount"] = item.Mesh.Submeshes.Sum(values => values.Length / 3),
                        ["submeshCount"] = item.Mesh.Submeshes.Count,
                        ["materialSlotCount"] = item.SlotMaterials?.Count ?? (item.Material == null && item.BaseColor == null ? 0 : 1)
                    })),
                    ["sourceDiagnostics"] = new JArray((sourceDiagnostics ?? new Dictionary<string, ImportedGlbDiagnostics>(StringComparer.Ordinal)).Values
                        .OrderBy(item => item.GraphId, StringComparer.Ordinal).Select(item => new JObject
                        {
                            ["graphId"] = item.GraphId,
                            ["sourceHash"] = item.SourceHash,
                            ["meshIndex"] = item.MeshIndex,
                            ["skinIndex"] = item.SkinIndex.HasValue ? (JToken)new JValue(item.SkinIndex.Value) : JValue.CreateNull(),
                            ["nodeIndex"] = item.NodeIndex.HasValue ? (JToken)new JValue(item.NodeIndex.Value) : JValue.CreateNull(),
                            ["diagnostics"] = new JArray(item.Diagnostics.Select(d => new JObject
                            {
                                ["code"] = d.Code, ["path"] = d.Path, ["isBlocking"] = d.IsBlocking, ["message"] = d.Message
                            }))
                        })),
                    ["limitations"] = new JArray(profile == GlbExportProfile.StaticGeometry
                        ? new[] { "graph and native metadata are not embedded", "VRM extensions are not emitted" }
                        : profile == GlbExportProfile.SkinnedGeometry
                            ? new[] { "rest pose only", "maximum four influences per vertex", "graph and native metadata are not embedded", "VRM extensions are not emitted" }
                            : new[] { "rest pose only", "graph and native metadata are not embedded", "VRM extensions are not emitted" })
                };
                File.WriteAllText(reportPath, report.ToString(Newtonsoft.Json.Formatting.Indented) + "\n", new System.Text.UTF8Encoding(false));
                string parent = Path.GetDirectoryName(directory); if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent); Directory.Move(staging, directory);
                return (Path.Combine(directory, FileName), Path.Combine(directory, ReportFileName), built.NodeMap);
            }
            catch
            {
                if (Directory.Exists(staging)) Directory.Delete(staging, true);
                throw;
            }
        }
    }

    static class GlbWriter
    {
        const int JsonChunk = 0x4e4f534a, BinChunk = 0x004e4942;
        const int ArrayBuffer = 34962, ElementArrayBuffer = 34963;
        readonly struct AccessorRef { public readonly int Id; public AccessorRef(int id) { Id = id; } }
        sealed class BinaryBuffer
        {
            readonly MemoryStream stream = new MemoryStream();
            public int Offset { get { return checked((int)stream.Length); } }
            public int Write(Action<BinaryWriter> action)
            {
                Align(); int start = Offset; using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true)) action(writer); return start;
            }
            public byte[] ToArray() { Align(); return stream.ToArray(); }
            void Align() { while (stream.Length % 4 != 0) stream.WriteByte(0); }
        }

        sealed class MaterialRegistry
        {
            readonly BinaryBuffer binary; readonly JArray views, materials, images, textures;
            readonly Dictionary<string, int> materialIds = new Dictionary<string, int>(StringComparer.Ordinal);
            readonly Dictionary<string, int> imageIds = new Dictionary<string, int>(StringComparer.Ordinal);
            public MaterialRegistry(BinaryBuffer binary, JArray views, JArray materials, JArray images, JArray textures)
            { this.binary = binary; this.views = views; this.materials = materials; this.images = images; this.textures = textures; }

            public int Get(GraphMaterialValue material, GraphImageValue fallbackImage)
            {
                if (material == null && fallbackImage == null) return -1;
                var parameters = material == null ? MaterialParameters.Default : material.Parameters;
                var image = material?.BaseColor ?? fallbackImage;
                string key = parameters.ContentHash + ":" + (image?.ImageHash ?? "");
                if (materialIds.TryGetValue(key, out var existing)) return existing;
                var json = new JObject { ["name"] = "NyaForgeMaterial-" + materials.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["pbrMetallicRoughness"] = new JObject {
                        // MaterialParameters stores linear RGB and glTF's
                        // baseColorFactor is also linear. Do not apply the sRGB
                        // transfer function used for texture pixels here.
                        ["baseColorFactor"] = new JArray(parameters.BaseColor.X, parameters.BaseColor.Y, parameters.BaseColor.Z, parameters.BaseColor.W),
                        ["metallicFactor"] = parameters.Metallic, ["roughnessFactor"] = parameters.Roughness } };
                var pbr = (JObject)json["pbrMetallicRoughness"];
                if (image != null)
                {
                    int imageIndex = AddImage(image);
                    pbr["baseColorTexture"] = new JObject { ["index"] = imageIndex };
                }
                if (parameters.Emission.X != 0f || parameters.Emission.Y != 0f || parameters.Emission.Z != 0f)
                    json["emissiveFactor"] = new JArray(parameters.Emission.X, parameters.Emission.Y, parameters.Emission.Z);
                if (parameters.AlphaMode == MaterialAlphaMode.Cutout) { json["alphaMode"] = "MASK"; json["alphaCutoff"] = parameters.AlphaCutoff; }
                else if (parameters.AlphaMode == MaterialAlphaMode.Blend) json["alphaMode"] = "BLEND";
                int id = materials.Count; materials.Add(json); materialIds.Add(key, id); return id;
            }

            int AddImage(GraphImageValue image)
            {
                if (imageIds.TryGetValue(image.ImageHash, out var existing)) return existing;
                byte[] png = PaintPng.Encode(image.Image);
                int offset = binary.Write(writer => writer.Write(png));
                int view = AddRawView(views, offset, png.Length);
                int imageId = images.Count; images.Add(new JObject { ["bufferView"] = view, ["mimeType"] = "image/png" });
                textures.Add(new JObject { ["source"] = imageId }); imageIds.Add(image.ImageHash, imageId); return imageId;
            }
        }

        public static byte[] Build(GlbExportService.MeshObject[] objects, GlbExportService.SkinnedObject skinned, GlbExportProfile profile)
            => BuildMany(objects, skinned == null ? null : new[] { skinned }, profile).Bytes;

        public sealed class BuildResult
        {
            public byte[] Bytes { get; }
            public GlbExportNodeMap NodeMap { get; }
            internal BuildResult(byte[] bytes, GlbExportNodeMap nodeMap) { Bytes = bytes; NodeMap = nodeMap; }
        }

        public static BuildResult BuildMany(GlbExportService.MeshObject[] objects, GlbExportService.SkinnedObject[] skinnedObjects, GlbExportProfile profile)
        {
            var binary = new BinaryBuffer(); var views = new JArray(); var accessors = new JArray(); var meshes = new JArray(); var nodes = new JArray(); var skins = new JArray(); var sceneNodes = new JArray();
            var materials = new JArray(); var images = new JArray(); var textures = new JArray();
            var materialRegistry = new MaterialRegistry(binary, views, materials, images, textures);
            var meshNodeMap = new Dictionary<string, int>(StringComparer.Ordinal);
            var boneNodeMap = new Dictionary<string, IReadOnlyDictionary<string, int>>(StringComparer.Ordinal);
            var skeletonNodeMap = new Dictionary<string, IReadOnlyList<int>>(StringComparer.Ordinal);
            if (IsSkinned(profile))
            {
                Checks.Require(skinnedObjects != null && skinnedObjects.Length == objects.Length, "GLB_SKIN_OBJECT_COUNT", "Each skinned mesh must have a matching binding.");
                Checks.Require(skinnedObjects.Length > 0, "NO_EXPORTABLE_OBJECT", "No skinned graph objects were provided.");
            }
            var skinIndices = new Dictionary<string, int>(StringComparer.Ordinal);
            var skinBoneMaps = new Dictionary<string, IReadOnlyDictionary<string, int>>(StringComparer.Ordinal);
            for (int i = 0; i < objects.Length; i++)
            {
                var item = objects[i]; var meshObject = item; var mesh = item.Mesh; var primitiveTemplates = new JArray();
                var currentSkinned = skinnedObjects == null ? null : skinnedObjects[i];
                int position = AddVec3(binary, views, accessors, mesh.Positions, ArrayBuffer, true);
                int normal = mesh.Normals.Count == 0 ? -1 : AddVec3(binary, views, accessors, mesh.Normals, ArrayBuffer, false);
                int tangent = mesh.Tangents.Count == 0 ? -1 : AddVec4(binary, views, accessors, mesh.Tangents, ArrayBuffer);
                int uv = mesh.Uv0.Count == 0 ? -1 : AddVec2(binary, views, accessors, mesh.Uv0, ArrayBuffer);
                int joints = -1, weights = -1, skinIndex = -1; int[] jointSets = null, weightSets = null;
                if (IsSkinned(profile))
                {
                    Checks.Require(currentSkinned != null, "GLB_SKIN_OBJECT_COUNT", "Skinned mesh binding is missing.");
                    jointSets = AddJointSets(binary, views, accessors, mesh.VertexCount, currentSkinned.Binding, currentSkinned.Skeleton, profile == GlbExportProfile.SkinnedGeometryExtended);
                    weightSets = AddWeightSets(binary, views, accessors, mesh.VertexCount, currentSkinned.Binding, profile == GlbExportProfile.SkinnedGeometryExtended);
                    joints = jointSets[0]; weights = weightSets[0];
                    string skinKey = SkinIdentity(currentSkinned);
                    if (!skinIndices.TryGetValue(skinKey, out skinIndex))
                    {
                        var skeleton = AddSkeleton(binary, views, accessors, nodes, skins, sceneNodes, currentSkinned);
                        skinIndex = skeleton.SkinIndex;
                        boneNodeMap[meshObject.Name] = skeleton.BoneNodes; skinBoneMaps[skinKey] = skeleton.BoneNodes;
                        skeletonNodeMap[meshObject.Name] = currentSkinned.Skeleton.Bones.Select(bone => skeleton.BoneNodes[bone.BoneId]).ToArray();
                        skinIndices.Add(skinKey, skinIndex);
                    }
                    else if (!boneNodeMap.ContainsKey(meshObject.Name))
                    {
                        // Shared skins retain the same actual node map.
                        var source = skinBoneMaps[skinKey];
                        boneNodeMap[meshObject.Name] = source; skeletonNodeMap[meshObject.Name] = currentSkinned.Skeleton.Bones.Select(bone => source[bone.BoneId]).ToArray();
                    }
                }
                var morphTargets = new JArray();
                if (meshObject.Morphs != null)
                    foreach (var target in meshObject.Morphs.Targets)
                    {
                        var deltas = new Vec3[mesh.VertexCount]; foreach (var pair in target.Deltas) deltas[pair.Key] = pair.Value;
                        // glTF requires POSITION morph accessors to carry min/max
                        // bounds. Keep zero deltas in the range so the accessor
                        // remains valid even when only a subset of vertices moves.
                        var targetJson = new JObject { ["POSITION"] = AddVec3(binary, views, accessors, deltas, ArrayBuffer, true) };
                        if (target.NormalDeltas.Count > 0)
                        {
                            var normalDeltas = new Vec3[mesh.VertexCount]; foreach (var pair in target.NormalDeltas) normalDeltas[pair.Key] = pair.Value;
                            targetJson["NORMAL"] = AddVec3(binary, views, accessors, normalDeltas, ArrayBuffer, false);
                        }
                        if (target.TangentDeltas.Count > 0)
                        {
                            var tangentDeltas = new Vec3[mesh.VertexCount]; foreach (var pair in target.TangentDeltas) tangentDeltas[pair.Key] = pair.Value;
                            targetJson["TANGENT"] = AddVec3(binary, views, accessors, tangentDeltas, ArrayBuffer, false);
                        }
                        morphTargets.Add(targetJson);
                    }
                var attrs = new JObject { ["POSITION"] = position };
                if (normal >= 0) attrs["NORMAL"] = normal; if (tangent >= 0) attrs["TANGENT"] = tangent; if (uv >= 0) attrs["TEXCOORD_0"] = uv;
                if (joints >= 0)
                {
                    attrs["JOINTS_0"] = joints; attrs["WEIGHTS_0"] = weights;
                    if (profile == GlbExportProfile.SkinnedGeometryExtended)
                    {
                        for (int set = 1; set < jointSets.Length; set++)
                        {
                            attrs["JOINTS_" + set.ToString(System.Globalization.CultureInfo.InvariantCulture)] = jointSets[set];
                            attrs["WEIGHTS_" + set.ToString(System.Globalization.CultureInfo.InvariantCulture)] = weightSets[set];
                        }
                    }
                }
                // Preserve material slots when the graph assigned them. Geometry
                // without appearance data keeps the historical single primitive.
                if (meshObject.SlotMaterials != null)
                {
                    for (int slot = 0; slot < mesh.Submeshes.Count; slot++)
                    {
                        // Keep each slot primitive's POSITION count local to
                        // its index buffer. Sharing the full mesh accessor for
                        // every slot makes concatenating readers count the same
                        // vertices repeatedly and can exceed the authoring cap.
                        meshObject.SlotMaterials.TryGetValue(slot, out var binding);
                        primitiveTemplates.Add(BuildSlotPrimitive(binary, views, accessors, mesh, mesh.Submeshes[slot],
                            binding?.Material, materialRegistry, currentSkinned, profile, meshObject.Morphs));
                    }
                }
                else
                {
                    var allIndices = mesh.Submeshes.SelectMany(values => values).ToArray();
                    int indices = AddIndices(binary, views, accessors, allIndices);
                    var primitive = new JObject { ["attributes"] = attrs, ["indices"] = indices, ["mode"] = 4 };
                    var materialIndex = materialRegistry.Get(meshObject.Material, meshObject.BaseColor);
                    if (materialIndex >= 0) primitive["material"] = materialIndex;
                    if (morphTargets.Count > 0) primitive["targets"] = morphTargets.DeepClone();
                    primitiveTemplates.Add(primitive);
                }
                var meshJson = new JObject { ["primitives"] = primitiveTemplates };
                if (meshObject.Morphs != null)
                {
                    meshJson["extras"] = new JObject { ["targetNames"] = new JArray(meshObject.Morphs.Targets.Select(target => target.Name)) };
                    var deform = meshObject.MorphWeights ?? new Dictionary<string, float>(StringComparer.Ordinal);
                    meshJson["weights"] = new JArray(meshObject.Morphs.Targets.Select(target => deform.TryGetValue(target.TargetId, out var value) ? value : 0f));
                }
                meshes.Add(meshJson);
                int meshNode = nodes.Count; var node = new JObject { ["name"] = "NyaForgeObject-" + i, ["mesh"] = i };
                if (IsSkinned(profile))
                {
                    node["skin"] = skinIndex;
                    if (item.Affine != null) node["matrix"] = new JArray(item.Affine.ToColumnMajor());
                }
                else ApplyTransform(node, item.Transform);
                nodes.Add(node); sceneNodes.Add(meshNode); meshNodeMap[meshObject.Name] = meshNode;
            }
            var root = new JObject { ["asset"] = new JObject { ["version"] = "2.0", ["generator"] = "NyaForge" }, ["scene"] = 0, ["scenes"] = new JArray(new JObject { ["nodes"] = sceneNodes }), ["nodes"] = nodes, ["meshes"] = meshes, ["buffers"] = new JArray(new JObject { ["byteLength"] = binary.ToArray().Length }), ["bufferViews"] = views, ["accessors"] = accessors };
            if (skins.Count > 0) root["skins"] = skins;
            if (materials.Count > 0) root["materials"] = materials;
            if (images.Count > 0) { root["images"] = images; root["textures"] = textures; }
            byte[] json = PadJson(System.Text.Encoding.UTF8.GetBytes(root.ToString(Newtonsoft.Json.Formatting.None)), 0x20); byte[] bin = binary.ToArray();
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(0x46546c67); writer.Write(2); writer.Write(checked(12 + 8 + json.Length + 8 + bin.Length)); writer.Write(json.Length); writer.Write(JsonChunk); writer.Write(json); writer.Write(bin.Length); writer.Write(BinChunk); writer.Write(bin);
                return new BuildResult(stream.ToArray(), new GlbExportNodeMap(meshNodeMap, boneNodeMap, skeletonNodeMap));
            }
        }

        static JObject BuildSlotPrimitive(BinaryBuffer binary, JArray views, JArray accessors, MeshData mesh,
            int[] sourceIndices, GraphMaterialValue material, MaterialRegistry materialRegistry,
            GlbExportService.SkinnedObject skinned, GlbExportProfile profile, MorphSet morphs)
        {
            var remap = new Dictionary<int, int>(); var sourceVertices = new List<int>(); var indices = new int[sourceIndices.Length];
            for (int i = 0; i < sourceIndices.Length; i++)
            {
                int source = sourceIndices[i]; Checks.Require(source >= 0 && source < mesh.VertexCount, "INVALID_MESH", "Material slot index is outside the mesh domain.");
                if (!remap.TryGetValue(source, out int local)) { local = sourceVertices.Count; remap.Add(source, local); sourceVertices.Add(source); }
                indices[i] = local;
            }
            var attrs = new JObject { ["POSITION"] = AddVec3(binary, views, accessors, sourceVertices.Select(index => mesh.Positions[index]).ToArray(), ArrayBuffer, true) };
            if (mesh.Normals.Count > 0) attrs["NORMAL"] = AddVec3(binary, views, accessors, sourceVertices.Select(index => mesh.Normals[index]).ToArray(), ArrayBuffer, false);
            if (mesh.Tangents.Count > 0) attrs["TANGENT"] = AddVec4(binary, views, accessors, sourceVertices.Select(index => mesh.Tangents[index]).ToArray(), ArrayBuffer);
            if (mesh.Uv0.Count > 0) attrs["TEXCOORD_0"] = AddVec2(binary, views, accessors, sourceVertices.Select(index => mesh.Uv0[index]).ToArray(), ArrayBuffer);
            if (skinned != null)
            {
                var jointSets = AddJointSets(binary, views, accessors, sourceVertices, skinned.Binding, skinned.Skeleton, profile == GlbExportProfile.SkinnedGeometryExtended);
                var weightSets = AddWeightSets(binary, views, accessors, sourceVertices, skinned.Binding, profile == GlbExportProfile.SkinnedGeometryExtended);
                for (int set = 0; set < jointSets.Length; set++)
                {
                    attrs["JOINTS_" + set.ToString(System.Globalization.CultureInfo.InvariantCulture)] = jointSets[set];
                    attrs["WEIGHTS_" + set.ToString(System.Globalization.CultureInfo.InvariantCulture)] = weightSets[set];
                }
            }
            var primitive = new JObject { ["attributes"] = attrs, ["indices"] = AddIndices(binary, views, accessors, indices), ["mode"] = 4 };
            int materialIndex = materialRegistry.Get(material, null); if (materialIndex >= 0) primitive["material"] = materialIndex;
            if (morphs != null)
            {
                var targets = new JArray();
                foreach (var target in morphs.Targets)
                {
                    var deltas = new Vec3[sourceVertices.Count]; foreach (var pair in target.Deltas) if (remap.TryGetValue(pair.Key, out int local)) deltas[local] = pair.Value;
                    var targetJson = new JObject { ["POSITION"] = AddVec3(binary, views, accessors, deltas, ArrayBuffer, true) };
                    if (target.NormalDeltas.Count > 0) { var values = new Vec3[sourceVertices.Count]; foreach (var pair in target.NormalDeltas) if (remap.TryGetValue(pair.Key, out int local)) values[local] = pair.Value; targetJson["NORMAL"] = AddVec3(binary, views, accessors, values, ArrayBuffer, false); }
                    if (target.TangentDeltas.Count > 0) { var values = new Vec3[sourceVertices.Count]; foreach (var pair in target.TangentDeltas) if (remap.TryGetValue(pair.Key, out int local)) values[local] = pair.Value; targetJson["TANGENT"] = AddVec3(binary, views, accessors, values, ArrayBuffer, false); }
                    targets.Add(targetJson);
                }
                if (targets.Count > 0) primitive["targets"] = targets;
            }
            return primitive;
        }

        static void ApplyTransform(JObject node, RestTransform transform)
        {
            if (transform.Scale != 1f) node["scale"] = new JArray(transform.Scale, transform.Scale, transform.Scale);
            if (transform.Translation.X != 0f || transform.Translation.Y != 0f || transform.Translation.Z != 0f) node["translation"] = new JArray(transform.Translation.X, transform.Translation.Y, transform.Translation.Z);
        }

        sealed class SkeletonBuild { public int SkinIndex; public IReadOnlyDictionary<string, int> BoneNodes; }
        static SkeletonBuild AddSkeleton(BinaryBuffer binary, JArray views, JArray accessors, JArray nodes, JArray skins, JArray sceneNodes, GlbExportService.SkinnedObject skinned)
        {
            var inverseByBone = skinned.InverseBindByBone ?? MatrixMapFallback(skinned.Skeleton, skinned.InverseBindMatrices);
            var jointByBone = skinned.JointLocalByBone ?? MatrixMapFallback(skinned.Skeleton, skinned.JointLocalTransforms);
            Checks.Require(skinned.JointLocalTransforms == null || skinned.JointLocalTransforms.Count == skinned.Skeleton.Bones.Count,
                "GLB_SKIN_SKELETON", "Retained joint local transforms must cover every exported joint.");
            var orderedBones = skinned.Skeleton.Bones.OrderBy(bone => bone.BoneId, StringComparer.Ordinal).ToArray();
            var jointNodes = new int[orderedBones.Length]; var byId = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < orderedBones.Length; i++) { jointNodes[i] = nodes.Count; byId.Add(orderedBones[i].BoneId, jointNodes[i]); nodes.Add(new JObject { ["name"] = orderedBones[i].Name }); }
            var roots = new List<int>();
            for (int i = 0; i < orderedBones.Length; i++)
            {
                var bone = orderedBones[i]; var node = (JObject)nodes[jointNodes[i]]; var parent = bone.ParentBoneId == "" ? (BoneDefinition)null : skinned.Skeleton.ById[bone.ParentBoneId];
                if (jointByBone != null)
                    node["matrix"] = new JArray(jointByBone[bone.BoneId].ToColumnMajor());
                else
                {
                    var origin = parent == null ? bone.Head : bone.Head - parent.Head; node["translation"] = new JArray(origin.X, origin.Y, origin.Z);
                }
                if (parent == null) roots.Add(jointNodes[i]);
                else
                {
                    // children is a JArray after the first child; casting the
                    // token itself to JObject breaks any skeleton with siblings.
                    var parentNode = (JObject)nodes[byId[parent.BoneId]];
                    parentNode["children"] = Append(parentNode["children"], jointNodes[i]);
                }
            }
            // A glTF skin's optional skeleton property names one common root.
            // When the authored rig has several parentless bones, create a
            // synthetic root so every joint remains reachable from that
            // property instead of silently leaving later roots outside the
            // exported skeleton hierarchy.
            int skeletonRoot;
            if (roots.Count == 1)
            {
                skeletonRoot = roots[0];
                sceneNodes.Add(skeletonRoot);
            }
            else
            {
                skeletonRoot = nodes.Count;
                nodes.Add(new JObject { ["name"] = "NyaForgeSkeletonRoot", ["children"] = new JArray(roots) });
                sceneNodes.Add(skeletonRoot);
            }
            Checks.Require(skinned.InverseBindMatrices == null || skinned.InverseBindMatrices.Count >= skinned.Skeleton.Bones.Count,
                "GLB_SKIN_BIND", "Retained inverse-bind matrices must cover every exported joint.");
            int ibmOffset = binary.Write(writer =>
            {
                for (int index = 0; index < orderedBones.Length; index++)
                {
                    var bone = orderedBones[index];
                    var values = inverseByBone == null
                        ? DefaultInverseBind(bone).ToColumnMajor()
                        : inverseByBone[bone.BoneId].ToColumnMajor();
                    foreach (double value in values) writer.Write((float)value);
                }
            });
            int ibmView = AddView(views, ibmOffset, orderedBones.Length * 64, ArrayBuffer); int ibmAccessor = AddAccessor(accessors, ibmView, 5126, orderedBones.Length, "MAT4", false, null, null);
            var skin = new JObject { ["joints"] = new JArray(jointNodes), ["inverseBindMatrices"] = ibmAccessor, ["skeleton"] = skeletonRoot }; skins.Add(skin); return new SkeletonBuild { SkinIndex = skins.Count - 1, BoneNodes = byId };
        }

        static JArray Append(JToken existing, int value) { var array = existing as JArray ?? new JArray(); array.Add(value); return array; }
        static string SkinIdentity(GlbExportService.SkinnedObject skinned)
        {
            var inverseByBone = skinned.InverseBindByBone ?? MatrixMapFallback(skinned.Skeleton, skinned.InverseBindMatrices);
            var jointByBone = skinned.JointLocalByBone ?? MatrixMapFallback(skinned.Skeleton, skinned.JointLocalTransforms);
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(skinned.Skeleton.ContentHash);
                foreach (var bone in skinned.Skeleton.Bones.OrderBy(bone => bone.BoneId, StringComparer.Ordinal)) writer.Write(bone.BoneId);
                if (inverseByBone == null) writer.Write(0);
                else
                {
                    writer.Write(skinned.Skeleton.Bones.Count);
                    foreach (var bone in skinned.Skeleton.Bones.OrderBy(bone => bone.BoneId, StringComparer.Ordinal))
                    {
                        var matrix = inverseByBone[bone.BoneId]; Checks.Require(matrix != null, "GLB_SKIN_BIND", "Retained inverse-bind matrix is missing.");
                        foreach (double value in matrix.ToColumnMajor()) writer.Write(value);
                    }
                }
                if (jointByBone == null) writer.Write(0);
                else foreach (var bone in skinned.Skeleton.Bones.OrderBy(bone => bone.BoneId, StringComparer.Ordinal)) foreach (double value in jointByBone[bone.BoneId].ToColumnMajor()) writer.Write(value);
                return Checks.Hash(stream.ToArray());
            }
        }
        static IReadOnlyDictionary<string, SourceAffine> MatrixMapFallback(SkeletonDefinition skeleton, IReadOnlyList<SourceAffine> values)
        {
            if (values == null) return null;
            Checks.Require(values.Count == skeleton.Bones.Count, "GLB_SKIN_BIND", "Retained skeleton matrices must cover every exported joint.");
            return skeleton.Bones.Select((bone, index) => new { bone.BoneId, Value = values[index] }).ToDictionary(x => x.BoneId, x => x.Value, StringComparer.Ordinal);
        }
        static SourceAffine DefaultInverseBind(BoneDefinition bone)
            => SourceAffine.FromTrs(new Vec3(-bone.Head.X, -bone.Head.Y, -bone.Head.Z), new Vec4(0, 0, 0, 1), new Vec3(1, 1, 1));
        static bool IsSkinned(GlbExportProfile profile) => profile == GlbExportProfile.SkinnedGeometry || profile == GlbExportProfile.SkinnedGeometryExtended;
        static int[] AddJointSets(BinaryBuffer binary, JArray views, JArray accessors, int vertexCount, SkinBinding binding, SkeletonDefinition skeleton, bool extended)
            => AddJointSets(binary, views, accessors, Enumerable.Range(0, vertexCount).ToArray(), binding, skeleton, extended);
        static int[] AddJointSets(BinaryBuffer binary, JArray views, JArray accessors, IReadOnlyList<int> vertices, SkinBinding binding, SkeletonDefinition skeleton, bool extended)
        {
            int setCount = extended ? (binding.Weights.Values.Max(values => values.Count) + 3) / 4 : 1;
            var byId = skeleton.Bones.OrderBy(bone => bone.BoneId, StringComparer.Ordinal).Select((bone, index) => new { bone.BoneId, index }).ToDictionary(x => x.BoneId, x => x.index, StringComparer.Ordinal);
            var result = new int[setCount];
            for (int set = 0; set < setCount; set++)
            {
                int current = set;
                int offset = binary.Write(writer => { foreach (int vertex in vertices) { var values = binding.Weights[vertex]; for (int i = 0; i < 4; i++) { int index = current * 4 + i; writer.Write((ushort)(index < values.Count ? byId[values[index].BoneId] : 0)); } } });
                result[set] = AddAccessor(accessors, AddView(views, offset, checked(vertices.Count * 8), ArrayBuffer), 5123, vertices.Count, "VEC4", false, null, null);
            }
            return result;
        }
        static int[] AddWeightSets(BinaryBuffer binary, JArray views, JArray accessors, int vertexCount, SkinBinding binding, bool extended)
            => AddWeightSets(binary, views, accessors, Enumerable.Range(0, vertexCount).ToArray(), binding, extended);
        static int[] AddWeightSets(BinaryBuffer binary, JArray views, JArray accessors, IReadOnlyList<int> vertices, SkinBinding binding, bool extended)
        {
            int setCount = extended ? (binding.Weights.Values.Max(values => values.Count) + 3) / 4 : 1;
            var result = new int[setCount];
            for (int set = 0; set < setCount; set++)
            {
                int current = set;
                int offset = binary.Write(writer => { foreach (int vertex in vertices) { var values = binding.Weights[vertex]; float total = values.Sum(value => value.Weight); for (int i = 0; i < 4; i++) { int index = current * 4 + i; writer.Write(index < values.Count ? values[index].Weight / total : 0f); } } });
                result[set] = AddAccessor(accessors, AddView(views, offset, checked(vertices.Count * 16), ArrayBuffer), 5126, vertices.Count, "VEC4", false, null, null);
            }
            return result;
        }
        static int AddIndices(BinaryBuffer binary, JArray views, JArray accessors, int[] values)
        { int offset = binary.Write(writer => { foreach (var value in values) writer.Write((uint)value); }); return AddAccessor(accessors, AddView(views, offset, checked(values.Length * 4), ElementArrayBuffer), 5125, values.Length, "SCALAR", false, null, null); }
        static int AddVec2(BinaryBuffer binary, JArray views, JArray accessors, IReadOnlyList<Vec2> values, int target)
        { int offset = binary.Write(writer => { foreach (var value in values) { writer.Write(value.X); writer.Write(value.Y); } }); return AddAccessor(accessors, AddView(views, offset, checked(values.Count * 8), target), 5126, values.Count, "VEC2", false, null, null); }
        static int AddVec3(BinaryBuffer binary, JArray views, JArray accessors, IReadOnlyList<Vec3> values, int target, bool minMax)
        { int offset = binary.Write(writer => { foreach (var value in values) { writer.Write(value.X); writer.Write(value.Y); writer.Write(value.Z); } }); JArray min = null, max = null; if (minMax && values.Count > 0) { min = new JArray(values.Min(v => v.X), values.Min(v => v.Y), values.Min(v => v.Z)); max = new JArray(values.Max(v => v.X), values.Max(v => v.Y), values.Max(v => v.Z)); } return AddAccessor(accessors, AddView(views, offset, checked(values.Count * 12), target), 5126, values.Count, "VEC3", false, min, max); }
        static int AddVec4(BinaryBuffer binary, JArray views, JArray accessors, IReadOnlyList<Vec4> values, int target)
        { int offset = binary.Write(writer => { foreach (var value in values) { writer.Write(value.X); writer.Write(value.Y); writer.Write(value.Z); writer.Write(value.W); } }); return AddAccessor(accessors, AddView(views, offset, checked(values.Count * 16), target), 5126, values.Count, "VEC4", false, null, null); }
        static int AddView(JArray views, int offset, int length, int target) { int id = views.Count; views.Add(new JObject { ["buffer"] = 0, ["byteOffset"] = offset, ["byteLength"] = length, ["target"] = target }); return id; }
        static int AddRawView(JArray views, int offset, int length) { int id = views.Count; views.Add(new JObject { ["buffer"] = 0, ["byteOffset"] = offset, ["byteLength"] = length }); return id; }
        static int AddAccessor(JArray accessors, int view, int component, int count, string type, bool normalized, JArray min, JArray max)
        { int id = accessors.Count; var value = new JObject { ["bufferView"] = view, ["componentType"] = component, ["count"] = count, ["type"] = type }; if (normalized) value["normalized"] = true; if (min != null) value["min"] = min; if (max != null) value["max"] = max; accessors.Add(value); return id; }
        static byte[] PadJson(byte[] bytes, byte pad) { int length = (bytes.Length + 3) / 4 * 4; var result = new byte[length]; Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length); for (int i = bytes.Length; i < result.Length; i++) result[i] = pad; return result; }
    }
}
