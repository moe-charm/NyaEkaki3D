using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring.Import;

namespace NyaForge.Authoring
{
    public sealed class VrmMorphBind
    {
        public int Node { get; }
        public int Index { get; }
        public float Weight { get; }
        public VrmMorphBind(int node, int index, float weight)
        {
            Checks.Require(node >= 0 && index >= 0, "VRM_EXPRESSION_INVALID", "VRM morph bind index is invalid."); Checks.Finite(weight); Checks.Require(weight >= 0 && weight <= 1, "VRM_EXPRESSION_INVALID", "VRM morph bind weight must be between 0 and 1.");
            Node = node; Index = index; Weight = weight;
        }
    }

    public sealed class VrmExpressionExport
    {
        public string Name { get; }
        public string Preset { get; }
        public bool IsCustom { get; }
        public IReadOnlyList<VrmMorphBind> Binds { get; }
        public VrmExpressionExport(string name, string preset, bool isCustom, IEnumerable<VrmMorphBind> binds)
        {
            Checks.Require(!string.IsNullOrWhiteSpace(name) && name.Length <= 128, "VRM_EXPRESSION_INVALID", "VRM expression name is invalid.");
            var values = (binds ?? Array.Empty<VrmMorphBind>()).ToArray(); Checks.Require(values.Length > 0 && values.Length <= 256, "VRM_EXPRESSION_INVALID", "VRM expression requires at least one morph bind.");
            Name = name; Preset = preset ?? ""; IsCustom = isCustom; Binds = Array.AsReadOnly(values);
        }
    }

    public sealed class VrmSpringColliderExport
    {
        public int Node { get; }
        public string Kind { get; }
        public Vec3 Offset { get; }
        public float Radius { get; }
        public Vec3? Tail { get; }
        public VrmSpringColliderExport(int node, string kind, Vec3 offset, float radius, Vec3? tail = null)
        {
            Checks.Require(node >= 0, "VRM_SPRING_INVALID", "VRM SpringBone collider node is invalid.");
            Checks.Require(kind == "sphere" || kind == "capsule", "VRM_SPRING_INVALID", "VRM SpringBone collider shape is invalid.");
            Checks.Finite(offset); Checks.Finite(radius); Checks.Require(radius >= 0, "VRM_SPRING_INVALID", "VRM SpringBone collider radius is invalid.");
            if (tail.HasValue) Checks.Finite(tail.Value);
            Checks.Require(kind == "capsule" || !tail.HasValue, "VRM_SPRING_INVALID", "Sphere collider cannot have a tail.");
            Node = node; Kind = kind; Offset = offset; Radius = radius; Tail = tail;
        }
    }

    public sealed class VrmSpringColliderGroupExport
    {
        public string Name { get; }
        public IReadOnlyList<int> ColliderIndices { get; }
        public VrmSpringColliderGroupExport(string name, IEnumerable<int> colliderIndices)
        {
            Checks.Require(name != null && name.Length <= 128, "VRM_SPRING_INVALID", "VRM SpringBone collider group name is invalid.");
            var values = (colliderIndices ?? Array.Empty<int>()).ToArray();
            Checks.Require(values.Length > 0 && values.Length <= 1024 && values.All(value => value >= 0), "VRM_SPRING_INVALID", "VRM SpringBone collider group is invalid.");
            Name = name; ColliderIndices = Array.AsReadOnly(values);
        }
    }

    public sealed class VrmSpringJointExport
    {
        public int Node { get; }
        public float HitRadius { get; }
        public float Stiffness { get; }
        public float GravityPower { get; }
        public Vec3 GravityDirection { get; }
        public float DragForce { get; }
        public VrmSpringJointExport(int node, float hitRadius, float stiffness, float gravityPower, Vec3 gravityDirection, float dragForce)
        {
            Checks.Require(node >= 0, "VRM_SPRING_INVALID", "VRM SpringBone joint node is invalid.");
            Checks.Finite(hitRadius); Checks.Finite(stiffness); Checks.Finite(gravityPower); Checks.Finite(gravityDirection); Checks.Finite(dragForce);
            Checks.Require(hitRadius >= 0 && stiffness >= 0 && gravityPower >= 0 && dragForce >= 0 && dragForce <= 1, "VRM_SPRING_INVALID", "VRM SpringBone joint parameter is invalid.");
            Node = node; HitRadius = hitRadius; Stiffness = stiffness; GravityPower = gravityPower; GravityDirection = gravityDirection; DragForce = dragForce;
        }
    }

    public sealed class VrmSpringExport
    {
        public IReadOnlyList<VrmSpringColliderExport> Colliders { get; }
        public IReadOnlyList<VrmSpringColliderGroupExport> ColliderGroups { get; }
        public IReadOnlyList<VrmSpringExport.SpringExportGroup> Springs { get; }
        public VrmSpringExport(IEnumerable<VrmSpringColliderExport> colliders, IEnumerable<VrmSpringColliderGroupExport> colliderGroups, IEnumerable<SpringExportGroup> springs)
        {
            var colliderValues = (colliders ?? Array.Empty<VrmSpringColliderExport>()).ToArray();
            var groupValues = (colliderGroups ?? Array.Empty<VrmSpringColliderGroupExport>()).ToArray();
            var springValues = (springs ?? Array.Empty<SpringExportGroup>()).ToArray();
            Checks.Require(springValues.Length > 0 && springValues.Length <= 256, "VRM_SPRING_INVALID", "VRM SpringBone export requires at least one spring.");
            Checks.Require(colliderValues.Length <= 1024 && groupValues.Length <= 256, "VRM_SPRING_INVALID", "VRM SpringBone export exceeds capacity.");
            foreach (var group in groupValues) foreach (var index in group.ColliderIndices) Checks.Require(index < colliderValues.Length, "VRM_SPRING_INVALID", "VRM SpringBone collider index is out of range.");
            Colliders = Array.AsReadOnly(colliderValues); ColliderGroups = Array.AsReadOnly(groupValues); Springs = Array.AsReadOnly(springValues);
        }

        public sealed class SpringExportGroup
        {
            public string Name { get; }
            public IReadOnlyList<VrmSpringJointExport> Joints { get; }
            public IReadOnlyList<int> ColliderGroupIndices { get; }
            public int? Center { get; }
            public SpringExportGroup(string name, IEnumerable<VrmSpringJointExport> joints, IEnumerable<int> colliderGroupIndices, int? center)
            {
                Checks.Require(name != null && name.Length <= 128, "VRM_SPRING_INVALID", "VRM SpringBone spring name is invalid.");
                var jointValues = (joints ?? Array.Empty<VrmSpringJointExport>()).ToArray(); var colliderValues = (colliderGroupIndices ?? Array.Empty<int>()).ToArray();
                Checks.Require(jointValues.Length > 0 && jointValues.Length <= 1024 && colliderValues.All(value => value >= 0), "VRM_SPRING_INVALID", "VRM SpringBone spring is invalid.");
                Name = name; Joints = Array.AsReadOnly(jointValues); ColliderGroupIndices = Array.AsReadOnly(colliderValues); Center = center;
            }
        }
    }

    /// <summary>Explicit metadata required to publish a VRM 1.0 humanoid package.</summary>
    public sealed class VrmExportMetadata
    {
        public string Name { get; }
        public string Version { get; }
        public IReadOnlyList<string> Authors { get; }
        public string LicenseUrl { get; }
        public bool UsesAuthoredNodeTokens { get; }
        /// <summary>VRM human bone name to the node index in the exported glTF.</summary>
        public IReadOnlyDictionary<string, int> HumanoidNodes { get; }
        public IReadOnlyList<VrmExpressionExport> Expressions { get; }
        public VrmSpringExport Springs { get; }

        internal static readonly string[] RequiredHumanBones =
        {
            "hips", "spine", "head", "leftUpperLeg", "leftLowerLeg", "leftFoot",
            "rightUpperLeg", "rightLowerLeg", "rightFoot", "leftUpperArm", "leftLowerArm",
            "leftHand", "rightUpperArm", "rightLowerArm", "rightHand"
        };

        public VrmExportMetadata(string name, IEnumerable<string> authors, string licenseUrl,
            IReadOnlyDictionary<string, int> humanoidNodes, string version = "1.0", IEnumerable<VrmExpressionExport> expressions = null, VrmSpringExport springs = null, bool usesAuthoredNodeTokens = false)
        {
            Checks.Require(!string.IsNullOrWhiteSpace(name) && name.Length <= 256, "VRM_METADATA_REQUIRED", "VRM name is required.");
            Checks.Require(!string.IsNullOrWhiteSpace(version) && version.Length <= 64, "VRM_METADATA_REQUIRED", "VRM version is required.");
            var authorValues = (authors ?? Array.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToArray();
            Checks.Require(authorValues.Length > 0 && authorValues.Length <= 64 && authorValues.All(value => value.Length <= 256), "VRM_METADATA_REQUIRED", "At least one VRM author is required.");
            Checks.Require(Uri.TryCreate(licenseUrl, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps), "VRM_METADATA_REQUIRED", "VRM licenseUrl must be an absolute HTTP(S) URL.");
            Checks.Require(humanoidNodes != null, "VRM_HUMANOID_REQUIRED", "VRM humanoid mapping is required.");
            var mapping = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var pair in humanoidNodes)
            {
                Checks.Require(!string.IsNullOrWhiteSpace(pair.Key) && pair.Key.Length <= 128 && pair.Value >= 0, "VRM_HUMANOID_INVALID", "VRM humanoid node mapping is invalid.");
                Checks.Require(mapping.TryAdd(pair.Key, pair.Value), "VRM_HUMANOID_INVALID", "VRM humanoid bone mapping repeats a name.");
            }
            foreach (var bone in RequiredHumanBones)
                Checks.Require(mapping.ContainsKey(bone), "VRM_HUMANOID_REQUIRED", "VRM humanoid mapping is missing: " + bone);
            var expressionValues = (expressions ?? Array.Empty<VrmExpressionExport>()).ToArray(); var expressionNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var expression in expressionValues) Checks.Require(expression != null && expressionNames.Add(expression.Name), "VRM_EXPRESSION_INVALID", "VRM expression name repeats.");
            Checks.Require(expressionValues.Length <= 256, "VRM_EXPRESSION_INVALID", "VRM expression count exceeds capacity.");
            Name = name; Version = version; Authors = Array.AsReadOnly(authorValues); LicenseUrl = licenseUrl; UsesAuthoredNodeTokens = usesAuthoredNodeTokens; HumanoidNodes = new System.Collections.ObjectModel.ReadOnlyDictionary<string, int>(mapping); Expressions = Array.AsReadOnly(expressionValues); Springs = springs;
        }
    }

    public sealed class VrmExportResult
    {
        public string Path { get; }
        public string ReportPath { get; }
        public int ObjectCount { get; }
        internal VrmExportResult(string path, string reportPath, int objectCount) { Path = path; ReportPath = reportPath; ObjectCount = objectCount; }
    }

    /// <summary>Creates a bounded VRM 1.0 package from NyaForge's rest-pose skinned GLB profile.</summary>
    public static class VrmExportService
    {
        public const string FileName = "model.vrm";
        public const string ReportFileName = "export-report.json";

        public static VrmExportResult ExportVrm1(AuthoringWorkspace workspace, string instance, string document, long revision,
            string directory, VrmExportMetadata metadata,
            IReadOnlyDictionary<string, SourceAffine> instanceWorldTransforms = null,
            IReadOnlyDictionary<string, IReadOnlyList<SourceAffine>> inverseBindMatrices = null,
            IReadOnlyDictionary<string, IReadOnlyList<SourceAffine>> jointLocalTransforms = null)
        {
            Checks.Require(metadata != null, "VRM_METADATA_REQUIRED", "VRM export metadata is required.");
            if (workspace == null) throw new ArgumentNullException(nameof(workspace));
            lock (workspace.Gate)
            {
                Checks.Require(workspace.Document.Objects.Count == 1, "VRM_OBJECT_COUNT", "VRM 1.0 export currently requires exactly one avatar graph object.");
                Checks.Require(!workspace.Document.Objects[0].IsStaticProfile, "VRM_GRAPH_REQUIRED", "VRM 1.0 export requires a skinned avatar graph.");
                Checks.Require(!workspace.Document.Objects[0].Graph.Nodes.Values.Any(node => node.TypeId == Graph.BuiltinNodes.Attachment), "VRM_ATTACHMENT_UNSUPPORTED", "VRM output cannot silently discard attachment metadata; convert the accessory to skin first.");
                Checks.Require(!Directory.Exists(directory) && !File.Exists(directory), "EXPORT_DESTINATION_EXISTS", "Export destination already exists.");
            }

            // Keep temporary paths beside the requested destination. Appending
            // two long suffixes below a user/fixture path can exceed Windows'
            // legacy MAX_PATH limit before the final package is moved.
            string parentDirectory = Path.GetDirectoryName(directory) ?? ".";
            string token = Guid.NewGuid().ToString("N").Substring(0, 12);
            string sourceDirectory = Path.Combine(parentDirectory, ".nyaforge-vrm-src-" + token);
            string staging = Path.Combine(parentDirectory, ".nyaforge-vrm-stage-" + token);
            try
            {
                var glb = GlbExportService.ExportSkinnedWithTransforms(workspace, instance, document, revision, sourceDirectory, instanceWorldTransforms, inverseBindMatrices, jointLocalTransforms);
                if (metadata.UsesAuthoredNodeTokens)
                    metadata = ResolveAuthoredNodeTokens(metadata, glb.NodeMap, workspace.Document.Objects[0].ObjectId);
                byte[] vrmBytes = Package(File.ReadAllBytes(glb.Path), metadata);
                Checks.Require(vrmBytes.Length <= AuthoringLimits.MaxGlbExportBytes, "BUDGET_EXCEEDED", "VRM output exceeds the 128 MiB budget.");
                Directory.CreateDirectory(staging);
                string path = System.IO.Path.Combine(staging, FileName); File.WriteAllBytes(path, vrmBytes);
                string reportPath = System.IO.Path.Combine(staging, ReportFileName);
                var report = new JObject
                {
                    ["version"] = 1,
                    ["profile"] = "Vrm1Humanoid",
                    ["units"] = "meters",
                    ["coordinates"] = Storage.Coordinates,
                    ["documentId"] = workspace.Document.DocumentId,
                    ["documentRevision"] = workspace.Document.DocumentRevision,
                    ["stateHash"] = workspace.Document.StateHash,
                    ["vrmHash"] = Checks.Hash(vrmBytes),
                    ["objectCount"] = 1,
                    ["metadata"] = new JObject { ["name"] = metadata.Name, ["version"] = metadata.Version, ["authors"] = new JArray(metadata.Authors), ["licenseUrl"] = "other", ["otherLicenseUrl"] = metadata.LicenseUrl, ["humanoidBoneCount"] = metadata.HumanoidNodes.Count, ["expressionCount"] = metadata.Expressions.Count, ["springBone"] = metadata.Springs != null },
                    ["limitations"] = new JArray("VRM 1.0 humanoid/meta, resolved morphTargetBinds and optional VRMC_springBone 1.0 are emitted", "material binds, texture transforms, lookAt, firstPerson and animation are not emitted by this profile", "rest pose only", "graph and native metadata are not embedded")
                };
                File.WriteAllText(reportPath, report.ToString(Newtonsoft.Json.Formatting.Indented) + "\n", new UTF8Encoding(false));
                string parent = System.IO.Path.GetDirectoryName(directory); if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
                Directory.Move(staging, directory);
                return new VrmExportResult(System.IO.Path.Combine(directory, FileName), System.IO.Path.Combine(directory, ReportFileName), 1);
            }
            catch
            {
                if (Directory.Exists(staging)) Directory.Delete(staging, true);
                throw;
            }
            finally
            {
                if (Directory.Exists(sourceDirectory)) Directory.Delete(sourceDirectory, true);
            }
        }

        static VrmExportMetadata ResolveAuthoredNodeTokens(VrmExportMetadata source, GlbExportNodeMap map, string objectId)
        {
            Checks.Require(map != null && map.MeshNodes.ContainsKey(objectId) && map.SkeletonNodes.ContainsKey(objectId), "VRM_NODE_MAP_REQUIRED", "GLB export did not return authored node mapping.");
            var skeleton = map.SkeletonNodes[objectId];
            int Resolve(int token)
            {
                Checks.Require(token >= 0, "VRM_NODE_MAP_REQUIRED", "Authored node token is invalid.");
                if (token == 0) return map.MeshNodes[objectId];
                Checks.Require(token - 1 < skeleton.Count, "VRM_NODE_MAP_REQUIRED", "Authored skeleton node token is outside the exported skeleton.");
                return skeleton[token - 1];
            }
            var humanoid = source.HumanoidNodes.ToDictionary(pair => pair.Key, pair => Resolve(pair.Value), StringComparer.Ordinal);
            var expressions = source.Expressions.Select(expression => new VrmExpressionExport(expression.Name, expression.Preset, expression.IsCustom, expression.Binds.Select(bind => new VrmMorphBind(Resolve(bind.Node), bind.Index, bind.Weight))));
            VrmSpringExport springs = null;
            if (source.Springs != null)
            {
                var colliders = source.Springs.Colliders.Select(c => new VrmSpringColliderExport(Resolve(c.Node), c.Kind, c.Offset, c.Radius, c.Tail));
                var springGroups = source.Springs.Springs.Select(s => new VrmSpringExport.SpringExportGroup(s.Name, s.Joints.Select(j => new VrmSpringJointExport(Resolve(j.Node), j.HitRadius, j.Stiffness, j.GravityPower, j.GravityDirection, j.DragForce)), s.ColliderGroupIndices, s.Center.HasValue ? Resolve(s.Center.Value) : (int?)null));
                springs = new VrmSpringExport(colliders, source.Springs.ColliderGroups, springGroups);
            }
            return new VrmExportMetadata(source.Name, source.Authors, source.LicenseUrl, humanoid, source.Version, expressions, springs, false);
        }

        /// <summary>Packages an already validated GLB, retaining its BIN chunk byte-for-byte.</summary>
        public static byte[] Package(byte[] glb, VrmExportMetadata metadata)
        {
            Checks.Require(metadata != null, "VRM_METADATA_REQUIRED", "VRM export metadata is required.");
            var source = GlbDocumentReader.Read(glb);
            var nodes = source.Root["nodes"] as JArray;
            Checks.Require(nodes != null, "VRM_GRAPH_REQUIRED", "VRM output requires a glTF nodes array.");
            foreach (var pair in metadata.HumanoidNodes)
                Checks.Require(pair.Value < nodes.Count, "VRM_HUMANOID_INVALID", "VRM humanoid node is outside the exported node array: " + pair.Key);
            var meshes = source.Root["meshes"] as JArray;
            foreach (var expression in metadata.Expressions)
                foreach (var bind in expression.Binds)
                {
                    Checks.Require(bind.Node < nodes.Count, "VRM_EXPRESSION_INVALID", "VRM expression node is outside the exported node array.");
                    var node = nodes[bind.Node] as JObject; var meshToken = node?["mesh"]; Checks.Require(meshToken != null && meshToken.Type == JTokenType.Integer && meshes != null && (int)meshToken >= 0 && (int)meshToken < meshes.Count, "VRM_EXPRESSION_INVALID", "VRM expression bind node has no exported mesh.");
                    var primitives = (meshes[(int)meshToken] as JObject)?["primitives"] as JArray; Checks.Require(primitives != null && primitives.Count > 0 && primitives.OfType<JObject>().All(primitive => primitive["targets"] is JArray && bind.Index < ((JArray)primitive["targets"]).Count), "VRM_EXPRESSION_INVALID", "VRM expression morph index is outside the exported mesh targets.");
                }
            if (metadata.Springs != null)
            {
                foreach (var collider in metadata.Springs.Colliders) Checks.Require(collider.Node < nodes.Count, "VRM_SPRING_INVALID", "VRM SpringBone collider node is outside the exported node array.");
                foreach (var group in metadata.Springs.ColliderGroups) foreach (var index in group.ColliderIndices) Checks.Require(index < metadata.Springs.Colliders.Count, "VRM_SPRING_INVALID", "VRM SpringBone collider index is outside the exported collider array.");
                foreach (var spring in metadata.Springs.Springs)
                {
                    foreach (var joint in spring.Joints) Checks.Require(joint.Node < nodes.Count, "VRM_SPRING_INVALID", "VRM SpringBone joint node is outside the exported node array.");
                    foreach (var index in spring.ColliderGroupIndices) Checks.Require(index < metadata.Springs.ColliderGroups.Count, "VRM_SPRING_INVALID", "VRM SpringBone collider group index is outside the exported array.");
                    if (spring.Center.HasValue) Checks.Require(spring.Center.Value < nodes.Count, "VRM_SPRING_INVALID", "VRM SpringBone center node is outside the exported node array.");
                }
            }

            var extension = new JObject
            {
                ["specVersion"] = "1.0",
                ["meta"] = new JObject
                {
                    ["name"] = metadata.Name, ["version"] = metadata.Version, ["authors"] = new JArray(metadata.Authors), ["licenseUrl"] = "other", ["otherLicenseUrl"] = metadata.LicenseUrl,
                    ["avatarPermission"] = "onlyAuthor", ["commercialUsage"] = "personalNonProfit", ["creditNotation"] = "required", ["modification"] = "prohibited"
                },
                ["humanoid"] = new JObject { ["humanBones"] = new JObject(metadata.HumanoidNodes.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => new JProperty(pair.Key, new JObject { ["node"] = pair.Value }))) }
            };
            if (metadata.Expressions.Count > 0)
            {
                var preset = new JObject(); var custom = new JObject();
                foreach (var expression in metadata.Expressions)
                {
                    var value = new JObject { ["morphTargetBinds"] = new JArray(expression.Binds.Select(bind => new JObject { ["node"] = bind.Node, ["index"] = bind.Index, ["weight"] = bind.Weight })) };
                    if (expression.IsCustom) custom[expression.Name] = value; else preset[string.IsNullOrWhiteSpace(expression.Preset) ? expression.Name : expression.Preset] = value;
                }
                extension["expressions"] = new JObject { ["preset"] = preset, ["custom"] = custom };
            }
            var extensions = source.Root["extensions"] as JObject ?? new JObject();
            var used = source.Root["extensionsUsed"] as JArray ?? new JArray();
            if (metadata.Springs != null)
            {
                var spring = new JObject { ["specVersion"] = "1.0" };
                if (metadata.Springs.Colliders.Count > 0)
                    spring["colliders"] = new JArray(metadata.Springs.Colliders.Select(collider => new JObject { ["node"] = collider.Node, ["shape"] = collider.Kind == "sphere" ? new JObject { ["sphere"] = new JObject { ["offset"] = new JArray(collider.Offset.X, collider.Offset.Y, collider.Offset.Z), ["radius"] = collider.Radius } } : new JObject { ["capsule"] = new JObject { ["offset"] = new JArray(collider.Offset.X, collider.Offset.Y, collider.Offset.Z), ["radius"] = collider.Radius, ["tail"] = new JArray(collider.Tail.Value.X, collider.Tail.Value.Y, collider.Tail.Value.Z) } } }));
                if (metadata.Springs.ColliderGroups.Count > 0)
                    spring["colliderGroups"] = new JArray(metadata.Springs.ColliderGroups.Select(group => new JObject { ["name"] = group.Name, ["colliders"] = new JArray(group.ColliderIndices) }));
                spring["springs"] = new JArray(metadata.Springs.Springs.Select(value =>
                {
                    var item = new JObject { ["name"] = value.Name, ["joints"] = new JArray(value.Joints.Select(joint => new JObject { ["node"] = joint.Node, ["hitRadius"] = joint.HitRadius, ["stiffness"] = joint.Stiffness, ["gravityPower"] = joint.GravityPower, ["gravityDir"] = new JArray(joint.GravityDirection.X, joint.GravityDirection.Y, joint.GravityDirection.Z), ["dragForce"] = joint.DragForce })) };
                    if (value.ColliderGroupIndices.Count > 0) item["colliderGroups"] = new JArray(value.ColliderGroupIndices);
                    if (value.Center.HasValue) item["center"] = value.Center.Value;
                    return item;
                }));
                extensions["VRMC_springBone"] = spring;
                if (!used.Values<string>().Contains("VRMC_springBone", StringComparer.Ordinal)) used.Add("VRMC_springBone");
            }
            extensions["VRMC_vrm"] = extension; source.Root["extensions"] = extensions;
            if (!used.Values<string>().Contains("VRMC_vrm", StringComparer.Ordinal)) used.Add("VRMC_vrm"); source.Root["extensionsUsed"] = used;
            return Build(source.Root, source.Bin);
        }

        static byte[] Build(JObject root, byte[] bin)
        {
            byte[] jsonSource = Encoding.UTF8.GetBytes(root.ToString(Newtonsoft.Json.Formatting.None));
            int jsonLength = checked((jsonSource.Length + 3) / 4 * 4); var json = new byte[jsonLength]; Buffer.BlockCopy(jsonSource, 0, json, 0, jsonSource.Length); for (int i = jsonSource.Length; i < json.Length; i++) json[i] = 0x20;
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(0x46546c67); writer.Write(2); writer.Write(checked(12 + 8 + json.Length + 8 + bin.Length)); writer.Write(json.Length); writer.Write(0x4e4f534a); writer.Write(json); writer.Write(bin.Length); writer.Write(0x004e4942); writer.Write(bin); return stream.ToArray();
            }
        }
    }
}
