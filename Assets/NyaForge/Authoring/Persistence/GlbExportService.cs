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
        public GlbExportProfile Profile { get; }
        public int ObjectCount { get; }
        internal GlbExportResult(string path, GlbExportProfile profile, int objectCount)
        { Path = path; Profile = profile; ObjectCount = objectCount; }
    }

    /// <summary>Writes standard glTF 2.0 GLB geometry without changing the native project.</summary>
    public static class GlbExportService
    {
        public const string FileName = "model.glb";

        public static GlbExportResult ExportStatic(AuthoringWorkspace workspace, string instance, string document, long revision, string directory)
        {
            ValidateRequest(workspace, instance, document, revision, directory);
            lock (workspace.Gate)
            {
                var objects = workspace.Document.Objects.Select(BuildStaticObject).ToArray();
                string path = Write(directory, objects, null, GlbExportProfile.StaticGeometry);
                return new GlbExportResult(path, GlbExportProfile.StaticGeometry, objects.Length);
            }
        }

        public static GlbExportResult ExportSkinned(AuthoringWorkspace workspace, string instance, string document, long revision, string directory)
            => ExportSkinned(workspace, instance, document, revision, directory, null);

        /// <summary>Writes a skinned GLB while retaining a selected source node instance affine on the mesh node.</summary>
        public static GlbExportResult ExportSkinned(AuthoringWorkspace workspace, string instance, string document, long revision, string directory, SourceAffine instanceWorldTransform)
        {
            ValidateRequest(workspace, instance, document, revision, directory);
            lock (workspace.Gate)
            {
                Checks.Require(workspace.Document.Objects.Count == 1, "GLB_SKIN_OBJECT_COUNT", "Skinned GLB export supports exactly one graph object.");
                var skinned = BuildSkinnedObject(workspace.Document.Objects[0], instanceWorldTransform, GlbExportProfile.SkinnedGeometry);
                string path = Write(directory, new[] { skinned.Mesh }, skinned, GlbExportProfile.SkinnedGeometry);
                return new GlbExportResult(path, GlbExportProfile.SkinnedGeometry, 1);
            }
        }

        /// <summary>Writes a skinned GLB with every authored influence split into JOINTS_n/WEIGHTS_n sets.</summary>
        public static GlbExportResult ExportSkinnedExtended(AuthoringWorkspace workspace, string instance, string document, long revision, string directory)
            => ExportSkinnedExtended(workspace, instance, document, revision, directory, null);

        /// <summary>Writes an extended skinned GLB while retaining a selected source node instance affine.</summary>
        public static GlbExportResult ExportSkinnedExtended(AuthoringWorkspace workspace, string instance, string document, long revision, string directory, SourceAffine instanceWorldTransform)
        {
            ValidateRequest(workspace, instance, document, revision, directory);
            lock (workspace.Gate)
            {
                Checks.Require(workspace.Document.Objects.Count == 1, "GLB_SKIN_OBJECT_COUNT", "Skinned GLB export supports exactly one graph object.");
                var skinned = BuildSkinnedObject(workspace.Document.Objects[0], instanceWorldTransform, GlbExportProfile.SkinnedGeometryExtended);
                string path = Write(directory, new[] { skinned.Mesh }, skinned, GlbExportProfile.SkinnedGeometryExtended);
                return new GlbExportResult(path, GlbExportProfile.SkinnedGeometryExtended, 1);
            }
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
        }

        static MeshObject BuildStaticObject(AuthoringObject item)
        {
            var evaluation = item.EvaluateGraph();
            Checks.Require(evaluation.IsComplete && evaluation.Output != null && evaluation.Output.Mesh != null,
                "GRAPH_INCOMPLETE", "Static GLB export requires a complete renderable graph.");
            return new MeshObject { Mesh = evaluation.Output.Mesh, Transform = evaluation.Output.Transform, Name = item.ObjectId,
                Material = evaluation.Output.Material, BaseColor = evaluation.Output.BaseColor, SlotMaterials = evaluation.Output.SlotMaterials };
        }

        static SkinnedObject BuildSkinnedObject(AuthoringObject item, SourceAffine instanceWorldTransform, GlbExportProfile profile)
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
            var allowed = new HashSet<string>(new[] { BuiltinNodes.MeshSource, BuiltinNodes.EditMesh, BuiltinNodes.MorphSet, BuiltinNodes.MorphDeform, BuiltinNodes.Skeleton, BuiltinNodes.SkinBind, BuiltinNodes.Pose, BuiltinNodes.SkinDeform, BuiltinNodes.Output }, StringComparer.Ordinal);
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
            };
        }

        static PoseSet DefaultPose(SkeletonDefinition skeleton)
        { return PoseSet.Create(skeleton, skeleton.Bones.Select(bone => new BonePose(bone.BoneId, PoseTransform.FromTranslation(bone.Head)))); }

        static string Write(string directory, MeshObject[] objects, SkinnedObject skinned, GlbExportProfile profile)
        {
            Checks.Require(objects != null && objects.Length > 0, "NO_EXPORTABLE_OBJECT", "No mesh objects were provided.");
            string staging = directory + ".staging-" + Guid.NewGuid().ToString("N");
            try
            {
                Directory.CreateDirectory(staging);
                byte[] bytes = GlbWriter.Build(objects, skinned, profile);
                Checks.Require(bytes.Length <= AuthoringLimits.MaxGlbExportBytes, "BUDGET_EXCEEDED", "GLB output exceeds the 128 MiB budget.");
                string path = Path.Combine(staging, FileName); File.WriteAllBytes(path, bytes);
                string parent = Path.GetDirectoryName(directory); if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent); Directory.Move(staging, directory);
                return Path.Combine(directory, FileName);
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
                        ["baseColorFactor"] = new JArray(LinearToSrgb(parameters.BaseColor.X), LinearToSrgb(parameters.BaseColor.Y), LinearToSrgb(parameters.BaseColor.Z), parameters.BaseColor.W),
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

        static float LinearToSrgb(float value)
        { return value <= .0031308f ? value * 12.92f : 1.055f * (float)Math.Pow(value, 1f / 2.4f) - .055f; }

        public static byte[] Build(GlbExportService.MeshObject[] objects, GlbExportService.SkinnedObject skinned, GlbExportProfile profile)
        {
            var binary = new BinaryBuffer(); var views = new JArray(); var accessors = new JArray(); var meshes = new JArray(); var nodes = new JArray(); var skins = new JArray(); var sceneNodes = new JArray();
            var materials = new JArray(); var images = new JArray(); var textures = new JArray();
            var materialRegistry = new MaterialRegistry(binary, views, materials, images, textures);
            for (int i = 0; i < objects.Length; i++)
            {
                var item = objects[i]; var meshObject = item; var mesh = item.Mesh; var primitiveTemplates = new JArray();
                int position = AddVec3(binary, views, accessors, mesh.Positions, ArrayBuffer, true);
                int normal = mesh.Normals.Count == 0 ? -1 : AddVec3(binary, views, accessors, mesh.Normals, ArrayBuffer, false);
                int tangent = mesh.Tangents.Count == 0 ? -1 : AddVec4(binary, views, accessors, mesh.Tangents, ArrayBuffer);
                int uv = mesh.Uv0.Count == 0 ? -1 : AddVec2(binary, views, accessors, mesh.Uv0, ArrayBuffer);
                int joints = -1, weights = -1, skinIndex = -1; int[] jointSets = null, weightSets = null;
                if (IsSkinned(profile))
                {
                    jointSets = AddJointSets(binary, views, accessors, mesh.VertexCount, skinned.Binding, skinned.Skeleton, profile == GlbExportProfile.SkinnedGeometryExtended);
                    weightSets = AddWeightSets(binary, views, accessors, mesh.VertexCount, skinned.Binding, profile == GlbExportProfile.SkinnedGeometryExtended);
                    joints = jointSets[0]; weights = weightSets[0];
                    skinIndex = AddSkeleton(binary, views, accessors, nodes, skins, sceneNodes, skinned);
                }
                var morphTargets = new JArray();
                if (meshObject.Morphs != null)
                    foreach (var target in meshObject.Morphs.Targets)
                    {
                        var deltas = new Vec3[mesh.VertexCount]; foreach (var pair in target.Deltas) deltas[pair.Key] = pair.Value;
                        var targetJson = new JObject { ["POSITION"] = AddVec3(binary, views, accessors, deltas, ArrayBuffer, false) };
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
                        var primitive = new JObject { ["attributes"] = attrs.DeepClone(), ["indices"] = AddIndices(binary, views, accessors, mesh.Submeshes[slot]), ["mode"] = 4 };
                        if (meshObject.SlotMaterials.TryGetValue(slot, out var binding)) primitive["material"] = materialRegistry.Get(binding.Material, null);
                        if (morphTargets.Count > 0) primitive["targets"] = morphTargets.DeepClone();
                        primitiveTemplates.Add(primitive);
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
                nodes.Add(node); sceneNodes.Add(meshNode);
            }
            var root = new JObject { ["asset"] = new JObject { ["version"] = "2.0", ["generator"] = "NyaForge" }, ["scene"] = 0, ["scenes"] = new JArray(new JObject { ["nodes"] = sceneNodes }), ["nodes"] = nodes, ["meshes"] = meshes, ["buffers"] = new JArray(new JObject { ["byteLength"] = binary.ToArray().Length }), ["bufferViews"] = views, ["accessors"] = accessors };
            if (skins.Count > 0) root["skins"] = skins;
            if (materials.Count > 0) root["materials"] = materials;
            if (images.Count > 0) { root["images"] = images; root["textures"] = textures; }
            byte[] json = PadJson(System.Text.Encoding.UTF8.GetBytes(root.ToString(Newtonsoft.Json.Formatting.None)), 0x20); byte[] bin = binary.ToArray();
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(0x46546c67); writer.Write(2); writer.Write(checked(12 + 8 + json.Length + 8 + bin.Length)); writer.Write(json.Length); writer.Write(JsonChunk); writer.Write(json); writer.Write(bin.Length); writer.Write(BinChunk); writer.Write(bin); return stream.ToArray();
            }
        }

        static void ApplyTransform(JObject node, RestTransform transform)
        {
            if (transform.Scale != 1f) node["scale"] = new JArray(transform.Scale, transform.Scale, transform.Scale);
            if (transform.Translation.X != 0f || transform.Translation.Y != 0f || transform.Translation.Z != 0f) node["translation"] = new JArray(transform.Translation.X, transform.Translation.Y, transform.Translation.Z);
        }

        static int AddSkeleton(BinaryBuffer binary, JArray views, JArray accessors, JArray nodes, JArray skins, JArray sceneNodes, GlbExportService.SkinnedObject skinned)
        {
            var jointNodes = new int[skinned.Skeleton.Bones.Count]; var byId = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < skinned.Skeleton.Bones.Count; i++) { jointNodes[i] = nodes.Count; byId.Add(skinned.Skeleton.Bones[i].BoneId, jointNodes[i]); nodes.Add(new JObject { ["name"] = skinned.Skeleton.Bones[i].Name }); }
            var roots = new List<int>();
            for (int i = 0; i < skinned.Skeleton.Bones.Count; i++)
            {
                var bone = skinned.Skeleton.Bones[i]; var node = (JObject)nodes[jointNodes[i]]; var parent = bone.ParentBoneId == "" ? (BoneDefinition)null : skinned.Skeleton.ById[bone.ParentBoneId];
                var origin = parent == null ? bone.Head : bone.Head - parent.Head; node["translation"] = new JArray(origin.X, origin.Y, origin.Z);
                if (parent == null) roots.Add(jointNodes[i]);
                else
                {
                    // children is a JArray after the first child; casting the
                    // token itself to JObject breaks any skeleton with siblings.
                    var parentNode = (JObject)nodes[byId[parent.BoneId]];
                    parentNode["children"] = Append(parentNode["children"], jointNodes[i]);
                }
            }
            foreach (var root in roots) sceneNodes.Add(root);
            var ibm = new Vec4[skinned.Skeleton.Bones.Count * 4];
            int ibmOffset = binary.Write(writer => { foreach (var bone in skinned.Skeleton.Bones) { writer.Write(1f); writer.Write(0f); writer.Write(0f); writer.Write(0f); writer.Write(0f); writer.Write(1f); writer.Write(0f); writer.Write(0f); writer.Write(0f); writer.Write(0f); writer.Write(1f); writer.Write(0f); writer.Write(-bone.Head.X); writer.Write(-bone.Head.Y); writer.Write(-bone.Head.Z); writer.Write(1f); } });
            int ibmView = AddView(views, ibmOffset, skinned.Skeleton.Bones.Count * 64, ArrayBuffer); int ibmAccessor = AddAccessor(accessors, ibmView, 5126, skinned.Skeleton.Bones.Count, "MAT4", false, null, null);
            var skin = new JObject { ["joints"] = new JArray(jointNodes), ["inverseBindMatrices"] = ibmAccessor }; if (roots.Count > 0) skin["skeleton"] = roots[0]; skins.Add(skin); return skins.Count - 1;
        }

        static JArray Append(JToken existing, int value) { var array = existing as JArray ?? new JArray(); array.Add(value); return array; }
        static bool IsSkinned(GlbExportProfile profile) => profile == GlbExportProfile.SkinnedGeometry || profile == GlbExportProfile.SkinnedGeometryExtended;
        static int[] AddJointSets(BinaryBuffer binary, JArray views, JArray accessors, int vertexCount, SkinBinding binding, SkeletonDefinition skeleton, bool extended)
        {
            int setCount = extended ? (binding.Weights.Values.Max(values => values.Count) + 3) / 4 : 1;
            var byId = skeleton.Bones.Select((bone, index) => new { bone.BoneId, index }).ToDictionary(x => x.BoneId, x => x.index, StringComparer.Ordinal);
            var result = new int[setCount];
            for (int set = 0; set < setCount; set++)
            {
                int current = set;
                int offset = binary.Write(writer => { for (int vertex = 0; vertex < vertexCount; vertex++) { var values = binding.Weights[vertex]; for (int i = 0; i < 4; i++) { int index = current * 4 + i; writer.Write((ushort)(index < values.Count ? byId[values[index].BoneId] : 0)); } } });
                result[set] = AddAccessor(accessors, AddView(views, offset, checked(vertexCount * 8), ArrayBuffer), 5123, vertexCount, "VEC4", false, null, null);
            }
            return result;
        }
        static int[] AddWeightSets(BinaryBuffer binary, JArray views, JArray accessors, int vertexCount, SkinBinding binding, bool extended)
        {
            int setCount = extended ? (binding.Weights.Values.Max(values => values.Count) + 3) / 4 : 1;
            var result = new int[setCount];
            for (int set = 0; set < setCount; set++)
            {
                int current = set;
                int offset = binary.Write(writer => { for (int vertex = 0; vertex < vertexCount; vertex++) { var values = binding.Weights[vertex]; float total = values.Sum(value => value.Weight); for (int i = 0; i < 4; i++) { int index = current * 4 + i; writer.Write(index < values.Count ? values[index].Weight / total : 0f); } } });
                result[set] = AddAccessor(accessors, AddView(views, offset, checked(vertexCount * 16), ArrayBuffer), 5126, vertexCount, "VEC4", false, null, null);
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
