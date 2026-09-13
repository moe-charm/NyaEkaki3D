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

    /// <summary>Explicit metadata required to publish a VRM 1.0 humanoid package.</summary>
    public sealed class VrmExportMetadata
    {
        public string Name { get; }
        public string Version { get; }
        public IReadOnlyList<string> Authors { get; }
        public string LicenseUrl { get; }
        /// <summary>VRM human bone name to the node index in the exported glTF.</summary>
        public IReadOnlyDictionary<string, int> HumanoidNodes { get; }
        public IReadOnlyList<VrmExpressionExport> Expressions { get; }

        internal static readonly string[] RequiredHumanBones =
        {
            "hips", "spine", "head", "leftUpperLeg", "leftLowerLeg", "leftFoot",
            "rightUpperLeg", "rightLowerLeg", "rightFoot", "leftUpperArm", "leftLowerArm",
            "leftHand", "rightUpperArm", "rightLowerArm", "rightHand"
        };

        public VrmExportMetadata(string name, IEnumerable<string> authors, string licenseUrl,
            IReadOnlyDictionary<string, int> humanoidNodes, string version = "1.0", IEnumerable<VrmExpressionExport> expressions = null)
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
            Name = name; Version = version; Authors = Array.AsReadOnly(authorValues); LicenseUrl = licenseUrl; HumanoidNodes = new System.Collections.ObjectModel.ReadOnlyDictionary<string, int>(mapping); Expressions = Array.AsReadOnly(expressionValues);
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
            IReadOnlyDictionary<string, IReadOnlyList<SourceAffine>> inverseBindMatrices = null)
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

            string sourceDirectory = directory + ".source-glb-" + Guid.NewGuid().ToString("N");
            string staging = directory + ".staging-" + Guid.NewGuid().ToString("N");
            try
            {
                var glb = GlbExportService.ExportSkinnedWithTransforms(workspace, instance, document, revision, sourceDirectory, instanceWorldTransforms, inverseBindMatrices);
                byte[] vrmBytes = Package( File.ReadAllBytes(glb.Path), metadata);
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
                    ["metadata"] = new JObject { ["name"] = metadata.Name, ["version"] = metadata.Version, ["authors"] = new JArray(metadata.Authors), ["licenseUrl"] = metadata.LicenseUrl, ["humanoidBoneCount"] = metadata.HumanoidNodes.Count, ["expressionCount"] = metadata.Expressions.Count },
                    ["limitations"] = new JArray("VRM 1.0 humanoid/meta and resolved morphTargetBinds only", "material binds, texture transforms, lookAt, firstPerson and SpringBone are not emitted by this profile", "rest pose only", "graph and native metadata are not embedded")
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

            var extension = new JObject
            {
                ["specVersion"] = "1.0",
                ["meta"] = new JObject
                {
                    ["name"] = metadata.Name, ["version"] = metadata.Version, ["authors"] = new JArray(metadata.Authors), ["licenseUrl"] = metadata.LicenseUrl,
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
            var extensions = source.Root["extensions"] as JObject ?? new JObject(); extensions["VRMC_vrm"] = extension; source.Root["extensions"] = extensions;
            var used = source.Root["extensionsUsed"] as JArray ?? new JArray(); if (!used.Values<string>().Contains("VRMC_vrm", StringComparer.Ordinal)) used.Add("VRMC_vrm"); source.Root["extensionsUsed"] = used;
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
