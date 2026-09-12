using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Import
{
    public static class ImportedRigSessionCodec
    {
        public static byte[] Write(ImportedRigSession session)
        {
            Checks.Require(session != null, "INVALID_IMPORT", "Imported rig session is required.");
            if (session.MeshInstanceTransform != null)
                Checks.Require(session.SourceSkin != null && session.SourceSkinBinding != null, "INVALID_IMPORT", "A skinned node instance transform requires a complete source skin package.");
            var root = new JObject { ["version"] = session.MeshInstanceTransform != null ? 6 : session.SourceSkinBinding != null ? 5 : session.SourceSkin == null ? 3 : 4, ["sourceHash"] = session.SourceHash, ["skeletonHash"] = session.SkeletonHash,
                ["graphId"] = session.GraphId, ["skeletonNodeId"] = session.SkeletonNodeId,
                ["origins"] = ImportedNodeOriginsJson.Write(session.SourceNodeOrigins),
                ["hierarchy"] = ImportedSourceHierarchyJson.Write(session.Hierarchy),
                ["nodes"] = new JArray(session.NodeToBone.OrderBy(p => p.Key).Select(p => new JObject { ["node"] = p.Key, ["boneId"] = p.Value })),
                ["humanoid"] = new JArray(session.HumanoidNodes.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => new JObject { ["name"] = p.Key, ["node"] = p.Value })) };
            if (session.MeshInstanceTransform != null) root["meshInstanceTransform"] = new JArray(session.MeshInstanceTransform.ToColumnMajor());
            if (session.SourceSkin != null) root["sourceSkin"] = Convert.ToBase64String(SourceSkinCodec.Write(session.SourceSkin));
            if (session.SourceSkinBinding != null)
            {
                root.Remove("sourceSkin"); root["sourceSkinPackage"] = Convert.ToBase64String(SourceSkinPackageCodec.Write(new SourceSkinPackage(session.SourceSkin, session.SourceSkinBinding)));
            }
            var bytes = new UTF8Encoding(false).GetBytes(root.ToString(Formatting.Indented) + "\n");
            Checks.Require(bytes.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Imported rig session exceeds capacity.");
            return bytes;
        }

        public static ImportedRigSession Read(byte[] bytes)
        {
            Checks.Require(bytes != null && bytes.Length > 0 && bytes.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Imported rig session exceeds capacity.");
            try
            {
                JObject root;
                using (var text = new StringReader(new UTF8Encoding(false, true).GetString(bytes)))
                using (var reader = new JsonTextReader(text) { MaxDepth = 8, DateParseHandling = DateParseHandling.None })
                {
                    root = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                    Checks.Require(!reader.Read(), "INVALID_IMPORT", "Trailing imported rig data is not allowed.");
                }
                int version = Integer(root, "version");
                Checks.Require(version >= 1 && version <= 6, "UNSUPPORTED_FORMAT", "Imported rig session version is unsupported.");
                Fields(root, version == 6 ? new[] { "version", "sourceHash", "skeletonHash", "graphId", "skeletonNodeId", "nodes", "humanoid", "origins", "hierarchy", "sourceSkinPackage", "meshInstanceTransform" } : version == 5 ? new[] { "version", "sourceHash", "skeletonHash", "graphId", "skeletonNodeId", "nodes", "humanoid", "origins", "hierarchy", "sourceSkinPackage" } : version == 4 ? new[] { "version", "sourceHash", "skeletonHash", "graphId", "skeletonNodeId", "nodes", "humanoid", "origins", "hierarchy", "sourceSkin" } : version == 3 ? new[] { "version", "sourceHash", "skeletonHash", "graphId", "skeletonNodeId", "nodes", "humanoid", "origins", "hierarchy" } : version == 1 ? new[] { "version", "sourceHash", "skeletonHash", "graphId", "skeletonNodeId", "nodes", "humanoid" } : new[] { "version", "sourceHash", "skeletonHash", "graphId", "skeletonNodeId", "nodes", "humanoid", "origins" });
                var nodes = new Dictionary<int, string>(); var humanoid = new Dictionary<string, int>(StringComparer.Ordinal);
                foreach (var token in Array(root, "nodes"))
                {
                    var value = token as JObject; Fields(value, "node", "boneId");
                    Checks.Require(nodes.TryAdd(Integer(value, "node"), Text(value, "boneId")), "INVALID_IMPORT", "Imported node repeats.");
                }
                foreach (var token in Array(root, "humanoid"))
                {
                    var value = token as JObject; Fields(value, "name", "node");
                    Checks.Require(humanoid.TryAdd(Text(value, "name"), Integer(value, "node")), "INVALID_IMPORT", "Imported humanoid name repeats.");
                }
                SourceSkin sourceSkin = null;
                SourceSkinBinding sourceSkinBinding = null;
                if (version == 4)
                {
                    Checks.Require(root["sourceSkin"]?.Type == JTokenType.String, "INVALID_IMPORT", "Complete source skin payload is required in v4.");
                    sourceSkin = SourceSkinCodec.Read(Convert.FromBase64String((string)root["sourceSkin"]));
                }
                if (version == 5)
                {
                    Checks.Require(root["sourceSkinPackage"]?.Type == JTokenType.String, "INVALID_IMPORT", "Complete source skin package is required in v5.");
                    var package = SourceSkinPackageCodec.Read(Convert.FromBase64String((string)root["sourceSkinPackage"])); sourceSkin = package.Skin; sourceSkinBinding = package.Binding;
                }
                SourceAffine meshInstanceTransform = version == 6 ? ReadAffine(root["meshInstanceTransform"]) : null;
                if (version == 6)
                {
                    Checks.Require(root["sourceSkinPackage"]?.Type == JTokenType.String, "INVALID_IMPORT", "Complete source skin package is required in v6.");
                    if (sourceSkin == null)
                    {
                        var package = SourceSkinPackageCodec.Read(Convert.FromBase64String((string)root["sourceSkinPackage"])); sourceSkin = package.Skin; sourceSkinBinding = package.Binding;
                    }
                }
                return new ImportedRigSession(Text(root, "sourceHash"), Text(root, "skeletonHash"), Text(root, "graphId"), Text(root, "skeletonNodeId"), nodes, humanoid, version == 1 ? null : ImportedNodeOriginsJson.Read(root["origins"]), version < 3 ? null : ImportedSourceHierarchyJson.Read(root["hierarchy"]), sourceSkin, sourceSkinBinding, meshInstanceTransform);
            }
            catch (JsonException error) { throw new AuthoringException("INVALID_IMPORT", error.Message); }
            catch (DecoderFallbackException error) { throw new AuthoringException("INVALID_IMPORT", error.Message); }
            catch (OverflowException error) { throw new AuthoringException("INVALID_IMPORT", error.Message); }
            catch (FormatException error) { throw new AuthoringException("INVALID_IMPORT", error.Message); }
        }

        static JArray Array(JObject owner, string name)
        {
            var value = owner[name] as JArray;
            Checks.Require(value != null && value.Count <= SkeletonDefinition.MaxBones, "INVALID_IMPORT", "Imported rig list exceeds capacity."); return value;
        }
        static void Fields(JObject value, params string[] fields)
        {
            Checks.Require(value != null && value.Count == fields.Length && fields.All(name => value[name] != null), "INVALID_IMPORT", "Imported rig fields are invalid.");
        }
        static string Text(JObject value, string name)
        {
            Checks.Require(value[name]?.Type == JTokenType.String && ((string)value[name]).Length <= 128, "INVALID_IMPORT", "Imported rig text is invalid."); return (string)value[name];
        }
        static int Integer(JObject value, string name)
        {
            Checks.Require(value[name]?.Type == JTokenType.Integer, "INVALID_IMPORT", "Imported rig integer is invalid.");
            var number = (long)value[name]; Checks.Require(number >= 0 && number <= int.MaxValue, "INVALID_IMPORT", "Imported rig integer is out of range."); return (int)number;
        }

        static SourceAffine ReadAffine(JToken token)
        {
            var array = token as JArray; Checks.Require(array != null && array.Count == 16, "INVALID_IMPORT", "Mesh instance transform must contain 16 numbers.");
            var values = new double[16];
            for (int i = 0; i < values.Length; i++)
            {
                Checks.Require(array[i].Type == JTokenType.Float || array[i].Type == JTokenType.Integer, "INVALID_IMPORT", "Mesh instance transform contains a non-number.");
                values[i] = (double)array[i];
            }
            return new SourceAffine(values);
        }
    }
}
