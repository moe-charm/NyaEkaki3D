using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring.Import
{
    /// <summary>Persistable SpringBone inventory. It contains settings, not runtime state or source model bytes.</summary>
    public sealed class VrmSpringSession
    {
        public string SourceHash { get; }
        public string Format { get; }
        public string Title { get; }
        public string Author { get; }
        public IReadOnlyList<string> Authors { get; }
        public IReadOnlyList<VrmSpringBoneGroup> SpringBones { get; }
        public IReadOnlyList<VrmSpringColliderGroup> ColliderGroups { get; }

        internal VrmSpringSession(string sourceHash, string format, string title, string author, IEnumerable<VrmSpringBoneGroup> springBones, IEnumerable<VrmSpringColliderGroup> colliderGroups, IEnumerable<string> authors = null)
        {
            Checks.HashText(sourceHash); Checks.Name(format);
            var bones = (springBones ?? System.Array.Empty<VrmSpringBoneGroup>()).ToArray(); var colliders = (colliderGroups ?? System.Array.Empty<VrmSpringColliderGroup>()).ToArray();
            Checks.Require(bones.Length <= VrmMetadata.MaxSpringGroups && colliders.Length <= VrmMetadata.MaxSpringColliderGroups, "BUDGET_EXCEEDED", "VRM SpringBone session exceeds capacity.");
            SourceHash = sourceHash; Format = format; Title = title ?? ""; Authors = authors == null ? VrmAuthorNames.Legacy(author) : VrmAuthorNames.Copy(authors); Author = string.Join(", ", Authors); SpringBones = System.Array.AsReadOnly(bones); ColliderGroups = System.Array.AsReadOnly(colliders);
        }

        public static VrmSpringSession Create(VrmMetadata metadata)
        {
            Checks.Require(metadata != null, "INVALID_VRM", "VRM metadata is required.");
            return new VrmSpringSession(metadata.SourceHash, metadata.Format, metadata.Title, metadata.Author, metadata.SpringBones, metadata.SpringColliderGroups, metadata.Authors);
        }
    }

    /// <summary>Strict bounded deterministic JSON codec for the SpringBone session sidecar.</summary>
    public static class VrmSpringSessionCodec
    {
        const int Version = 2;

        public static byte[] Write(VrmSpringSession session)
        {
            Checks.Require(session != null, "INVALID_VRM", "VRM SpringBone session is required.");
            var groups = new JArray(session.SpringBones.Select(group => new JObject
            {
                ["name"] = group.Name, ["centerNode"] = group.CenterNodeIndex,
                ["rootBoneNodes"] = new JArray(group.RootBoneNodes), ["colliderGroupIndices"] = new JArray(group.ColliderGroupIndices),
                ["joints"] = new JArray(group.Joints.Select(joint => new JObject { ["node"] = joint.NodeIndex, ["hitRadius"] = joint.HitRadius, ["stiffness"] = joint.Stiffness, ["gravityPower"] = joint.GravityPower, ["dragForce"] = joint.DragForce }))
            }));
            var colliderGroups = new JArray(session.ColliderGroups.Select(group => new JObject { ["node"] = group.NodeIndex, ["colliderCount"] = group.ColliderCount, ["nodes"] = new JArray(group.ColliderNodeIndices) }));
            var root = new JObject { ["version"] = Version, ["sourceHash"] = session.SourceHash, ["format"] = session.Format, ["title"] = session.Title, ["authors"] = new JArray(session.Authors), ["springBones"] = groups, ["colliderGroups"] = colliderGroups };
            var bytes = new UTF8Encoding(false).GetBytes(root.ToString(Formatting.Indented) + "\n"); Checks.Require(bytes.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "VRM SpringBone session exceeds capacity."); return bytes;
        }

        public static VrmSpringSession Read(byte[] bytes)
        {
            Checks.Require(bytes != null && bytes.Length > 0 && bytes.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "VRM SpringBone session exceeds capacity.");
            try
            {
                JObject root;
                using (var text = new StringReader(new UTF8Encoding(false, true).GetString(bytes))) using (var reader = new JsonTextReader(text) { MaxDepth = 16, DateParseHandling = DateParseHandling.None })
                { root = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error }); Checks.Require(!reader.Read(), "INVALID_VRM", "Trailing VRM SpringBone session data is not allowed."); }
                int version = Int(root, "version");
                RequireFields(root, "version", "sourceHash", "format", "title", version == 1 ? "author" : "authors", "springBones", "colliderGroups"); Checks.Require(version == 1 || version == Version, "UNSUPPORTED_FORMAT", "VRM SpringBone session version is unsupported.");
                string sourceHash = String(root, "sourceHash", 64); string format = String(root, "format", 32); string title = String(root, "title", 256, true); var authors = version == 1 ? VrmAuthorNames.Legacy(String(root, "author", 256, true)) : VrmAuthorNames.Read(root, false);
                var colliderArray = root["colliderGroups"] as JArray; Checks.Require(colliderArray != null && colliderArray.Count <= VrmMetadata.MaxSpringColliderGroups, "INVALID_VRM", "VRM SpringBone collider groups are invalid."); var colliders = new List<VrmSpringColliderGroup>();
                foreach (var token in colliderArray) { var value = token as JObject; Checks.Require(value != null, "INVALID_VRM", "VRM SpringBone collider group is invalid."); RequireFields(value, "node", "colliderCount", "nodes"); int node = Int(value, "node"); Checks.Require(node >= -1 && node < AuthoringLimits.MaxVertices, "INVALID_VRM", "VRM SpringBone collider node is out of range."); int count = Int(value, "colliderCount"); Checks.Require(count >= 0 && count <= VrmMetadata.MaxSpringColliders, "BUDGET_EXCEEDED", "VRM SpringBone collider count exceeds capacity."); var nodes = IntArray(value, "nodes", VrmMetadata.MaxSpringColliders, unique: false); Checks.Require(nodes.Count == count, "INVALID_VRM", "VRM SpringBone collider node count differs."); Checks.Require(nodes.All(item => item < AuthoringLimits.MaxVertices), "INVALID_VRM", "VRM SpringBone collider node is out of range."); colliders.Add(new VrmSpringColliderGroup(node, count, nodes)); }
                var springArray = root["springBones"] as JArray; Checks.Require(springArray != null && springArray.Count <= VrmMetadata.MaxSpringGroups, "INVALID_VRM", "VRM SpringBone groups are invalid."); var bones = new List<VrmSpringBoneGroup>(); int totalJoints = 0;
                foreach (var token in springArray) { var value = token as JObject; Checks.Require(value != null, "INVALID_VRM", "VRM SpringBone group is invalid."); RequireFields(value, "name", "centerNode", "rootBoneNodes", "colliderGroupIndices", "joints"); string name = String(value, "name", 128, true); int center = Int(value, "centerNode"); Checks.Require(center >= -1 && center < AuthoringLimits.MaxVertices, "INVALID_VRM", "VRM SpringBone center node is invalid."); var roots = IntArray(value, "rootBoneNodes", VrmMetadata.MaxSpringJoints); Checks.Require(roots.All(item => item < AuthoringLimits.MaxVertices), "INVALID_VRM", "VRM SpringBone root node is out of range."); var colliderIndices = IntArray(value, "colliderGroupIndices", VrmMetadata.MaxSpringColliderGroups); foreach (var index in colliderIndices) Checks.Require(index < colliders.Count, "INVALID_VRM", "VRM SpringBone collider group index is out of range."); var jointArray = value["joints"] as JArray; Checks.Require(jointArray != null && jointArray.Count <= VrmMetadata.MaxSpringJoints, "INVALID_VRM", "VRM SpringBone joints are invalid."); var joints = new List<VrmSpringJoint>(); var seen = new HashSet<int>(); foreach (var jointToken in jointArray) { var joint = jointToken as JObject; Checks.Require(joint != null, "INVALID_VRM", "VRM SpringBone joint is invalid."); RequireFields(joint, "node", "hitRadius", "stiffness", "gravityPower", "dragForce"); int node = Int(joint, "node"); Checks.Require(node >= 0 && node < AuthoringLimits.MaxVertices && seen.Add(node), "INVALID_VRM", "VRM SpringBone joint node is invalid or repeated."); joints.Add(new VrmSpringJoint(node, Number(joint, "hitRadius", 0, float.MaxValue), Number(joint, "stiffness", 0, float.MaxValue), Number(joint, "gravityPower", 0, float.MaxValue), Number(joint, "dragForce", 0, 1))); } totalJoints += joints.Count; Checks.Require(totalJoints <= VrmMetadata.MaxSpringJoints, "BUDGET_EXCEEDED", "VRM SpringBone joint count exceeds capacity."); bones.Add(new VrmSpringBoneGroup(name, joints, roots, colliderIndices, center)); }
                return new VrmSpringSession(sourceHash, format, title, "", bones, colliders, authors);
            }
            catch (JsonException error) { throw new AuthoringException("INVALID_VRM", error.Message); }
            catch (DecoderFallbackException error) { throw new AuthoringException("INVALID_VRM", error.Message); }
        }

        static void RequireFields(JObject owner, params string[] names) { var expected = new HashSet<string>(names, StringComparer.Ordinal); foreach (var name in names) Checks.Require(owner[name] != null, "INVALID_VRM", "VRM SpringBone session field is missing: " + name); foreach (var property in owner.Properties()) Checks.Require(expected.Contains(property.Name), "INVALID_VRM", "VRM SpringBone session field is unknown: " + property.Name); }
        static int Int(JObject owner, string name) { var token = owner[name]; Checks.Require(token != null && token.Type == JTokenType.Integer, "INVALID_VRM", "VRM SpringBone session integer is invalid: " + name); return (int)token; }
        static string String(JObject owner, string name, int maximum, bool optional = false) { var token = owner[name]; Checks.Require(token != null && token.Type == JTokenType.String && ((string)token).Length <= maximum && (optional || !string.IsNullOrWhiteSpace((string)token)), "INVALID_VRM", "VRM SpringBone session text is invalid: " + name); return (string)token; }
        static float Number(JObject owner, string name, float minimum, float maximum) { var token = owner[name]; Checks.Require(token != null && (token.Type == JTokenType.Float || token.Type == JTokenType.Integer), "INVALID_VRM", "VRM SpringBone session number is invalid: " + name); float value = (float)token; Checks.Finite(value); Checks.Require(value >= minimum && value <= maximum, "INVALID_VRM", "VRM SpringBone session number is out of range: " + name); return value; }
        static IReadOnlyList<int> IntArray(JObject owner, string name, int maximumCount, bool unique = true) { var token = owner[name] as JArray; Checks.Require(token != null && token.Count <= maximumCount, "INVALID_VRM", "VRM SpringBone session array is invalid: " + name); var result = new List<int>(); var seen = new HashSet<int>(); foreach (var item in token) { Checks.Require(item.Type == JTokenType.Integer, "INVALID_VRM", "VRM SpringBone session array item is invalid: " + name); int value = (int)item; Checks.Require(value >= 0 && (!unique || seen.Add(value)), "INVALID_VRM", "VRM SpringBone session array item is out of range: " + name); result.Add(value); } return result.AsReadOnly(); }
    }
}
