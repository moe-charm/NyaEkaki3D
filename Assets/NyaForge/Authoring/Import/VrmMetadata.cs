using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring.Import
{
    /// <summary>A source VRM morph bind. OwnerIndex is a glTF node in VRM 1.0 and a mesh in VRM 0.x.</summary>
    public sealed class VrmMorphBinding
    {
        public int OwnerIndex { get; }
        public int MorphIndex { get; }
        public float Weight { get; }

        internal VrmMorphBinding(int ownerIndex, int morphIndex, float weight)
        {
            Checks.Require(ownerIndex >= 0 && morphIndex >= 0, "INVALID_VRM", "VRM morph bind index is invalid."); Checks.Finite(weight); Checks.Require(weight >= 0 && weight <= 1, "INVALID_VRM", "VRM morph bind weight must be between 0 and 1.");
            OwnerIndex = ownerIndex; MorphIndex = morphIndex; Weight = weight == 0 ? 0 : weight;
        }
    }

    /// <summary>Bounded VRM expression inventory. Binding payloads stay in the source adapter until a morph/material adapter exists.</summary>
    public sealed class VrmExpression
    {
        public string Name { get; }
        public string Preset { get; }
        public bool IsCustom { get; }
        public int MorphTargetBindCount { get; }
        public int MaterialBindCount { get; }
        public IReadOnlyList<VrmMorphBinding> MorphBindings { get; }

        internal VrmExpression(string name, string preset, bool isCustom, IEnumerable<VrmMorphBinding> morphBindings, int materialBindCount)
        {
            Checks.Name(name); Checks.Require(morphBindings != null && materialBindCount >= 0, "INVALID_VRM", "VRM expression bind count is invalid.");
            var bindings = morphBindings.ToArray(); Checks.Require(bindings.Length <= VrmMetadata.MaxExpressions, "BUDGET_EXCEEDED", "VRM morph bind count exceeds capacity.");
            Name = name; Preset = preset ?? ""; IsCustom = isCustom; MorphBindings = Array.AsReadOnly(bindings); MorphTargetBindCount = bindings.Length; MaterialBindCount = materialBindCount;
        }
    }

    /// <summary>Bounded source SpringBone joint settings. This is inventory only; it does not simulate dynamics.</summary>
    public sealed class VrmSpringJoint
    {
        public int NodeIndex { get; }
        public float HitRadius { get; }
        public float Stiffness { get; }
        public float GravityPower { get; }
        public float DragForce { get; }
        public Vec3? GravityDirection { get; }

        internal VrmSpringJoint(int nodeIndex, float hitRadius, float stiffness, float gravityPower, float dragForce, Vec3? gravityDirection = null)
        {
            Checks.Require(nodeIndex >= 0, "INVALID_VRM", "VRM spring joint node is invalid.");
            NodeIndex = nodeIndex; HitRadius = hitRadius; Stiffness = stiffness; GravityPower = gravityPower; DragForce = dragForce; if (gravityDirection.HasValue) Checks.Finite(gravityDirection.Value); GravityDirection = gravityDirection;
        }
    }

    /// <summary>One VRM SpringBone chain, normalized across VRM 0.x and 1.0.</summary>
    public sealed class VrmSpringBoneGroup
    {
        public string Name { get; }
        public IReadOnlyList<VrmSpringJoint> Joints { get; }
        public IReadOnlyList<int> RootBoneNodes { get; }
        public IReadOnlyList<int> ColliderGroupIndices { get; }
        public int CenterNodeIndex { get; }

        internal VrmSpringBoneGroup(string name, IEnumerable<VrmSpringJoint> joints, IEnumerable<int> rootBoneNodes, IEnumerable<int> colliderGroupIndices, int centerNodeIndex)
        {
            Checks.Require(name != null && name.Length <= 128, "INVALID_VRM", "VRM spring name is invalid.");
            var jointValues = (joints ?? Array.Empty<VrmSpringJoint>()).ToArray(); var roots = (rootBoneNodes ?? Array.Empty<int>()).ToArray(); var colliders = (colliderGroupIndices ?? Array.Empty<int>()).ToArray();
            Checks.Require(jointValues.Length <= VrmMetadata.MaxSpringJoints && roots.Length <= VrmMetadata.MaxSpringJoints && colliders.Length <= VrmMetadata.MaxSpringColliderGroups, "BUDGET_EXCEEDED", "VRM spring group exceeds capacity.");
            Name = name; Joints = Array.AsReadOnly(jointValues); RootBoneNodes = Array.AsReadOnly(roots); ColliderGroupIndices = Array.AsReadOnly(colliders); CenterNodeIndex = centerNodeIndex;
        }
    }

    /// <summary>Source collider group with optional detailed shapes; null shapes identify older incomplete sessions.</summary>
    public sealed class VrmSpringColliderGroup
    {
        public int NodeIndex { get; }
        public int ColliderCount { get; }
        public IReadOnlyList<int> ColliderNodeIndices { get; }
        public IReadOnlyList<VrmSpringColliderShape> Shapes { get; }

        internal VrmSpringColliderGroup(int nodeIndex, int colliderCount, IEnumerable<int> colliderNodeIndices = null, IEnumerable<VrmSpringColliderShape> shapes = null)
        {
            Checks.Require(nodeIndex >= -1 && colliderCount >= 0, "INVALID_VRM", "VRM spring collider group is invalid.");
            var nodes = colliderNodeIndices == null ? (nodeIndex < 0 ? System.Array.Empty<int>() : Enumerable.Repeat(nodeIndex, colliderCount)) : colliderNodeIndices;
            var values = nodes.ToArray(); Checks.Require(values.Length == colliderCount && (nodeIndex >= 0 || values.Length == 0 || values.All(item => item >= 0)), "INVALID_VRM", "VRM spring collider node list is invalid.");
            NodeIndex = nodeIndex; ColliderCount = colliderCount; ColliderNodeIndices = Array.AsReadOnly(values);
            if (shapes != null) { var shapeValues = shapes.Take(VrmMetadata.MaxSpringColliders + 1).ToArray(); Checks.Require(shapeValues.Length == colliderCount && shapeValues.All(shape => shape != null), "INVALID_VRM", "Spring collider shape count differs."); Shapes = Array.AsReadOnly(shapeValues); }
        }
    }

    /// <summary>Small, source-pinned VRM identity and humanoid mapping independent of UniVRM.</summary>
    public sealed class VrmMetadata
    {
        public const int MaxExpressions = 256;
        public const int MaxSpringGroups = 256;
        public const int MaxSpringJoints = 1024;
        public const int MaxSpringColliderGroups = 256;
        public const int MaxSpringColliders = 1024;
        public string SourceHash { get; }
        public string Format { get; }
        public string SpecVersion { get; }
        public string Title { get; }
        public string Author { get; }
        public IReadOnlyList<string> Authors { get; }
        public IReadOnlyDictionary<string, int> HumanoidNodes { get; }
        public IReadOnlyList<VrmExpression> Expressions { get; }
        public IReadOnlyList<VrmSpringBoneGroup> SpringBones { get; }
        public IReadOnlyList<VrmSpringColliderGroup> SpringColliderGroups { get; }
        public IReadOnlyList<string> Warnings { get; }

        internal VrmMetadata(string sourceHash, string format, string specVersion, string title, string author, IDictionary<string, int> humanoidNodes, IEnumerable<VrmExpression> expressions, IEnumerable<VrmSpringBoneGroup> springBones, IEnumerable<VrmSpringColliderGroup> springColliderGroups, IEnumerable<string> warnings, IEnumerable<string> authors = null)
        {
            Checks.HashText(sourceHash); Checks.Name(format); Checks.Name(specVersion); Checks.Require(humanoidNodes != null, "INVALID_VRM", "Humanoid mapping is required.");
            SourceHash = sourceHash; Format = format; SpecVersion = specVersion; Title = title ?? ""; Authors = authors == null ? VrmAuthorNames.Legacy(author) : VrmAuthorNames.Copy(authors); Author = string.Join(", ", Authors);
            var expressionValues = (expressions ?? Array.Empty<VrmExpression>()).ToArray(); Checks.Require(expressionValues.Length <= MaxExpressions, "BUDGET_EXCEEDED", "VRM expression count exceeds capacity.");
            Expressions = Array.AsReadOnly(expressionValues); HumanoidNodes = new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(humanoidNodes, StringComparer.Ordinal)); Warnings = Array.AsReadOnly((warnings ?? Array.Empty<string>()).ToArray());
            var springValues = (springBones ?? Array.Empty<VrmSpringBoneGroup>()).ToArray(); var colliderValues = (springColliderGroups ?? Array.Empty<VrmSpringColliderGroup>()).ToArray();
            Checks.Require(springValues.Length <= MaxSpringGroups && colliderValues.Length <= MaxSpringColliderGroups, "BUDGET_EXCEEDED", "VRM spring inventory exceeds capacity."); SpringBones = Array.AsReadOnly(springValues); SpringColliderGroups = Array.AsReadOnly(colliderValues);
        }
    }

    /// <summary>Reads only VRM 0.x/1.0 metadata from the GLB JSON extension.</summary>
    public static class VrmMetadataReader
    {
        public static bool ContainsVrm(byte[] bytes)
        {
            var extensions = GlbDocumentReader.Read(bytes).Root["extensions"] as JObject;
            return extensions != null && (extensions["VRMC_vrm"] is JObject || extensions["VRM"] is JObject);
        }

        public static VrmMetadata Read(byte[] bytes)
        {
            var document = GlbDocumentReader.Read(bytes); var extensions = document.Root["extensions"] as JObject;
            Checks.Require(extensions != null, "UNSUPPORTED_FORMAT", "The GLB does not contain a VRM extension.");
            var modern = extensions["VRMC_vrm"] as JObject; var legacy = extensions["VRM"] as JObject;
            Checks.Require(modern != null || legacy != null, "UNSUPPORTED_FORMAT", "The GLB does not contain a supported VRM extension.");
            if (modern != null) return ParseModern(document, modern);
            return ParseLegacy(document, legacy);
        }

        static VrmMetadata ParseModern(GlbDocument document, JObject extension)
        {
            string spec = StringProperty(extension, "specVersion", 64, "specVersion"); Checks.Require(spec == "1.0", "UNSUPPORTED_FORMAT", "Only VRM 1.0 metadata is supported.");
            var meta = extension["meta"] as JObject; Checks.Require(meta != null, "INVALID_VRM", "VRMC_vrm meta is required.");
            var map = ParseModernHumanoid(document.Root, extension["humanoid"] as JObject);
            var expressions = ParseModernExpressions(document.Root, extension);
            var spring = ParseModernSpring(document.Root, extensions: document.Root["extensions"] as JObject);
            var warnings = new List<string> { "VRM 1.0 identity, humanoid, expression and SpringBone inventory were read; expression application, spring simulation, look-at and material conversion remain separate adapters." };
            return new VrmMetadata(document.SourceHash, "vrm1", spec, OptionalString(meta, "name", 256), "", map, expressions, spring.Bones, spring.Colliders, warnings, VrmAuthorNames.Read(meta, true));
        }

        static VrmMetadata ParseLegacy(GlbDocument document, JObject extension)
        {
            string spec = OptionalString(extension, "specVersion", 64); Checks.Require(spec == "0.0" || spec == "0.0.0" || spec == "", "UNSUPPORTED_FORMAT", "Only VRM 0.x metadata is supported.");
            var meta = extension["meta"] as JObject; Checks.Require(meta != null, "INVALID_VRM", "VRM meta is required.");
            var map = ParseLegacyHumanoid(document.Root, extension["humanoid"] as JObject);
            var expressions = ParseLegacyExpressions(document.Root, extension);
            var spring = ParseLegacySpring(document.Root, extension);
            var warnings = new List<string> { "VRM 0.x identity, humanoid, expression and SpringBone inventory were read; VRM 1.0 conversion, expression application, spring simulation and material conversion remain separate adapters." };
            return new VrmMetadata(document.SourceHash, "vrm0", spec == "" ? "0.0" : spec, OptionalString(meta, "title", 256), OptionalString(meta, "author", 256), map, expressions, spring.Bones, spring.Colliders, warnings);
        }

        static (IReadOnlyList<VrmSpringBoneGroup> Bones, IReadOnlyList<VrmSpringColliderGroup> Colliders) ParseModernSpring(JObject root, JObject extensions)
        {
            var extension = extensions?["VRMC_springBone"] as JObject; if (extension == null) return (System.Array.Empty<VrmSpringBoneGroup>(), System.Array.Empty<VrmSpringColliderGroup>());
            string spec = StringProperty(extension, "specVersion", 64, "VRMC_springBone specVersion"); Checks.Require(spec == "1.0", "UNSUPPORTED_FORMAT", "Only VRMC_springBone 1.0 is supported."); int nodeCount = Array(root, "nodes").Count;
            var colliderArray = OptionalArray(extension, "colliders"); Checks.Require(colliderArray.Count <= VrmMetadata.MaxSpringColliders, "BUDGET_EXCEEDED", "VRM spring collider count exceeds capacity.");
            var sourceShapes = new List<VrmSpringColliderShape>();
            foreach (var token in colliderArray)
            {
                var value = token as JObject; Checks.Require(value != null, "INVALID_VRM", "VRM spring collider is invalid.");
                IntProperty(value, "node", 0, nodeCount - 1, "VRM spring collider node");
                sourceShapes.Add(VrmSpringDetailJson.ModernShape(value["shape"] as JObject));
            }
            var groupArray = OptionalArray(extension, "colliderGroups"); Checks.Require(groupArray.Count <= VrmMetadata.MaxSpringColliderGroups, "BUDGET_EXCEEDED", "VRM spring collider group count exceeds capacity.");
            var colliderNodes = colliderArray.Select(token => IntProperty((JObject)token, "node", 0, nodeCount - 1, "VRM spring collider node")).ToArray(); var colliders = new List<VrmSpringColliderGroup>(); foreach (var token in groupArray) { var value = token as JObject; Checks.Require(value != null, "INVALID_VRM", "VRM spring collider group is invalid."); var indices = IntArray(value, "colliders", 0, colliderArray.Count - 1, "VRM spring collider group index", true); var nodes = indices.Select(index => colliderNodes[index]).ToArray(); int commonNode = nodes.Length == 0 || nodes.All(node => node == nodes[0]) ? (nodes.Length == 0 ? -1 : nodes[0]) : -1; colliders.Add(new VrmSpringColliderGroup(commonNode, nodes.Length, nodes, indices.Select(index => sourceShapes[index]))); }
            var springs = OptionalArray(extension, "springs"); Checks.Require(springs.Count <= VrmMetadata.MaxSpringGroups, "BUDGET_EXCEEDED", "VRM spring count exceeds capacity."); var bones = new List<VrmSpringBoneGroup>(); int totalJoints = 0;
            foreach (var token in springs) { var value = token as JObject; Checks.Require(value != null, "INVALID_VRM", "VRM spring is invalid."); string name = OptionalString(value, "name", 128); var joints = value["joints"] as JArray; Checks.Require(joints != null, "INVALID_VRM", "VRM spring joints are invalid."); var jointValues = new List<VrmSpringJoint>(); var seen = new HashSet<int>(); foreach (var jointToken in joints) { var joint = jointToken as JObject; Checks.Require(joint != null, "INVALID_VRM", "VRM spring joint is invalid."); int node = IntProperty(joint, "node", 0, nodeCount - 1, "VRM spring joint node"); Checks.Require(seen.Add(node), "INVALID_VRM", "VRM spring joint repeats."); jointValues.Add(new VrmSpringJoint(node, OptionalNumber(joint, "hitRadius", 0), OptionalNumber(joint, "stiffness", 0, defaultValue: 1), OptionalNumber(joint, "gravityPower", 0), OptionalNumber(joint, "dragForce", 0, 1, defaultValue: .5f), VrmSpringDetailJson.ModernVector(joint, "gravityDir", new Vec3(0, -1, 0)))); } totalJoints += jointValues.Count; Checks.Require(totalJoints <= VrmMetadata.MaxSpringJoints, "BUDGET_EXCEEDED", "VRM spring joint count exceeds capacity."); int center = OptionalInt(value, "center", -1, nodeCount - 1); var colliderGroups = IntArray(value, "colliderGroups", 0, groupArray.Count - 1, "VRM spring collider group index", true); bones.Add(new VrmSpringBoneGroup(name, jointValues, System.Array.Empty<int>(), colliderGroups, center)); }
            return (bones.AsReadOnly(), colliders.AsReadOnly());
        }

        static (IReadOnlyList<VrmSpringBoneGroup> Bones, IReadOnlyList<VrmSpringColliderGroup> Colliders) ParseLegacySpring(JObject root, JObject extension)
        {
            var secondary = extension["secondaryAnimation"] as JObject; if (secondary == null) return (System.Array.Empty<VrmSpringBoneGroup>(), System.Array.Empty<VrmSpringColliderGroup>()); int nodeCount = Array(root, "nodes").Count;
            var colliderArray = OptionalArray(secondary, "colliderGroups"); Checks.Require(colliderArray.Count <= VrmMetadata.MaxSpringColliderGroups, "BUDGET_EXCEEDED", "VRM spring collider group count exceeds capacity."); var colliders = new List<VrmSpringColliderGroup>();
            foreach (var token in colliderArray) { var value = token as JObject; Checks.Require(value != null, "INVALID_VRM", "VRM spring collider group is invalid."); int node = IntProperty(value, "node", 0, nodeCount - 1, "VRM spring collider node"); var shapes = value["colliders"] as JArray; Checks.Require(shapes != null && shapes.Count <= VrmMetadata.MaxSpringColliders, "INVALID_VRM", "VRM spring collider list is invalid."); foreach (var shapeToken in shapes) { var shape = shapeToken as JObject; Checks.Require(shape != null, "INVALID_VRM", "VRM spring collider is invalid."); NumberProperty(shape, "radius", 0, float.MaxValue, "VRM spring collider radius"); } colliders.Add(new VrmSpringColliderGroup(node, shapes.Count, shapes: shapes.Select(token => new VrmSpringColliderShape("sphere", VrmSpringDetailJson.LegacyVector((JObject)token, "offset"), VrmSpringDetailJson.Number(token["radius"]), null)))); }
            var groups = OptionalArray(secondary, "boneGroups"); Checks.Require(groups.Count <= VrmMetadata.MaxSpringGroups, "BUDGET_EXCEEDED", "VRM spring count exceeds capacity."); var bones = new List<VrmSpringBoneGroup>(); int totalRoots = 0;
            foreach (var token in groups) { var value = token as JObject; Checks.Require(value != null, "INVALID_VRM", "VRM spring bone group is invalid."); string name = OptionalString(value, "comment", 128); var roots = IntArray(value, "bones", 0, nodeCount - 1, "VRM spring root node", true); totalRoots += roots.Count; Checks.Require(totalRoots <= VrmMetadata.MaxSpringJoints, "BUDGET_EXCEEDED", "VRM spring root count exceeds capacity."); var colliderGroups = IntArray(value, "colliderGroups", 0, colliders.Count - 1, "VRM spring collider group index", true); int center = OptionalInt(value, "center", -1, nodeCount - 1); float hit = OptionalNumber(value, "hitRadius", 0); float stiffness = OptionalNumber(value, "stiffiness", 0); float gravity = OptionalNumber(value, "gravityPower", 0); float drag = OptionalNumber(value, "dragForce", 0, 1); var joints = roots.Select(node => new VrmSpringJoint(node, hit, stiffness, gravity, drag, VrmSpringDetailJson.LegacyVector(value, "gravityDir"))); bones.Add(new VrmSpringBoneGroup(name, joints, roots, colliderGroups, center)); }
            return (bones.AsReadOnly(), colliders.AsReadOnly());
        }

        static IReadOnlyList<VrmExpression> ParseModernExpressions(JObject root, JObject extension)
        {
            var owner = extension["expressions"] as JObject; if (owner == null) return System.Array.Empty<VrmExpression>();
            var result = new List<VrmExpression>(); var names = new HashSet<string>(StringComparer.Ordinal);
            ParseModernExpressionGroup(owner["preset"], false, result, names, Array(root, "nodes").Count);
            ParseModernExpressionGroup(owner["custom"], true, result, names, Array(root, "nodes").Count);
            Checks.Require(result.Count <= VrmMetadata.MaxExpressions, "BUDGET_EXCEEDED", "VRM expression count exceeds capacity."); return result.AsReadOnly();
        }

        static void ParseModernExpressionGroup(JToken token, bool custom, IList<VrmExpression> result, ISet<string> names, int nodeCount)
        {
            if (token == null || token.Type == JTokenType.Null) return;
            var group = token as JObject; Checks.Require(group != null, "INVALID_VRM", "VRM expression group is invalid.");
            foreach (var property in group.Properties())
            {
                var value = property.Value as JObject; Checks.Require(value != null, "INVALID_VRM", "VRM expression entry is invalid.");
                string name = StringPropertyName(property.Name, "expression name"); Checks.Require(names.Add(name), "INVALID_VRM", "VRM expression name repeats.");
                result.Add(new VrmExpression(name, custom ? "" : name, custom, ModernMorphBindings(value, nodeCount), BindCount(value, "materialColorBinds")));
            }
        }

        static IReadOnlyList<VrmExpression> ParseLegacyExpressions(JObject root, JObject extension)
        {
            var master = extension["blendShapeMaster"] as JObject; if (master == null) return System.Array.Empty<VrmExpression>();
            var groups = master["blendShapeGroups"] as JArray; Checks.Require(groups != null, "INVALID_VRM", "VRM blendShapeGroups is invalid.");
            int meshCount = Array(root, "meshes").Count;
            Checks.Require(groups.Count <= VrmMetadata.MaxExpressions, "BUDGET_EXCEEDED", "VRM expression count exceeds capacity.");
            var result = new List<VrmExpression>(); var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var token in groups)
            {
                var value = token as JObject; Checks.Require(value != null, "INVALID_VRM", "VRM blendShapeGroup is invalid.");
                string preset = OptionalString(value, "presetName", 128); string name = OptionalString(value, "name", 128); if (name.Length == 0) name = preset;
                Checks.Require(name.Length > 0, "INVALID_VRM", "VRM blendShapeGroup needs a name or presetName."); Checks.Require(names.Add(name), "INVALID_VRM", "VRM expression name repeats.");
                result.Add(new VrmExpression(name, preset, preset.Length == 0, LegacyMorphBindings(value, meshCount), BindCount(value, "materialValues")));
            }
            return result.AsReadOnly();
        }

        static IReadOnlyList<VrmMorphBinding> ModernMorphBindings(JObject owner, int nodeCount)
        {
            var value = owner["morphTargetBinds"]; if (value == null || value.Type == JTokenType.Null) return System.Array.Empty<VrmMorphBinding>();
            var array = value as JArray; Checks.Require(array != null && array.Count <= VrmMetadata.MaxExpressions, "INVALID_VRM", "VRM expression bind list is invalid: morphTargetBinds");
            var result = new List<VrmMorphBinding>(array.Count);
            foreach (var token in array) { var bind = token as JObject; Checks.Require(bind != null, "INVALID_VRM", "VRM morph bind is invalid."); int node = IntProperty(bind, "node", 0, nodeCount - 1, "VRM morph bind node"); int index = IntProperty(bind, "index", 0, int.MaxValue, "VRM morph bind index"); float weight = NumberProperty(bind, "weight", 0, 1, "VRM morph bind weight"); result.Add(new VrmMorphBinding(node, index, weight)); }
            return result.AsReadOnly();
        }

        static IReadOnlyList<VrmMorphBinding> LegacyMorphBindings(JObject owner, int meshCount)
        {
            var value = owner["binds"]; if (value == null || value.Type == JTokenType.Null) return System.Array.Empty<VrmMorphBinding>();
            var array = value as JArray; Checks.Require(array != null && array.Count <= VrmMetadata.MaxExpressions, "INVALID_VRM", "VRM expression bind list is invalid: binds");
            var result = new List<VrmMorphBinding>(array.Count);
            foreach (var token in array) { var bind = token as JObject; Checks.Require(bind != null, "INVALID_VRM", "VRM morph bind is invalid."); int mesh = IntProperty(bind, "mesh", 0, meshCount - 1, "VRM morph bind mesh"); int index = IntProperty(bind, "index", 0, int.MaxValue, "VRM morph bind index"); float sourceWeight = NumberProperty(bind, "weight", 0, 100, "VRM morph bind weight"); result.Add(new VrmMorphBinding(mesh, index, sourceWeight / 100f)); }
            return result.AsReadOnly();
        }

        static int BindCount(JObject owner, string property)
        {
            var value = owner[property]; if (value == null || value.Type == JTokenType.Null) return 0;
            var array = value as JArray; Checks.Require(array != null && array.Count <= VrmMetadata.MaxExpressions, "INVALID_VRM", "VRM expression bind list is invalid: " + property); return array.Count;
        }

        static JArray OptionalArray(JObject owner, string property)
        {
            var value = owner[property]; if (value == null || value.Type == JTokenType.Null) return new JArray(); var array = value as JArray; Checks.Require(array != null, "INVALID_VRM", "VRM array property is invalid: " + property); return array;
        }

        static IReadOnlyList<int> IntArray(JObject owner, string property, int minimum, int maximum, string label, bool optional = false)
        {
            var value = owner[property]; if (value == null || value.Type == JTokenType.Null) { Checks.Require(optional, "INVALID_VRM", label + " list is missing."); return System.Array.Empty<int>(); }
            var array = value as JArray; Checks.Require(array != null && array.Count <= VrmMetadata.MaxSpringJoints, "INVALID_VRM", label + " list is invalid."); var result = new List<int>(array.Count); var seen = new HashSet<int>();
            foreach (var token in array) { Checks.Require(token.Type == JTokenType.Integer, "INVALID_VRM", label + " is invalid."); int number = (int)token; Checks.Require(number >= minimum && number <= maximum, "INVALID_VRM", label + " is out of range."); Checks.Require(seen.Add(number), "INVALID_VRM", label + " repeats."); result.Add(number); }
            return result.AsReadOnly();
        }

        static int OptionalInt(JObject owner, string property, int defaultValue, int maximum)
        {
            var value = owner[property]; if (value == null || value.Type == JTokenType.Null) return defaultValue; Checks.Require(value.Type == JTokenType.Integer, "INVALID_VRM", "VRM integer property is invalid: " + property); int number = (int)value; Checks.Require(number >= -1 && number <= maximum, "INVALID_VRM", "VRM integer property is out of range: " + property); return number;
        }

        static float OptionalNumber(JObject owner, string property, float minimum, float maximum = float.MaxValue, float defaultValue = 0)
        {
            var value = owner[property]; if (value == null || value.Type == JTokenType.Null) return defaultValue; Checks.Require(value.Type == JTokenType.Float || value.Type == JTokenType.Integer, "INVALID_VRM", "VRM number property is invalid: " + property); float number = (float)value; Checks.Finite(number); Checks.Require(number >= minimum && number <= maximum, "INVALID_VRM", "VRM number property is out of range: " + property); return number;
        }

        static IDictionary<string, int> ParseModernHumanoid(JObject root, JObject humanoid)
        {
            Checks.Require(humanoid != null, "INVALID_VRM", "VRMC_vrm humanoid is required."); var bones = humanoid["humanBones"] as JObject; Checks.Require(bones != null, "INVALID_VRM", "VRMC_vrm humanBones is required.");
            var nodes = Array(root, "nodes"); var map = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var pair in bones.Properties()) { var value = pair.Value as JObject; Checks.Require(value != null, "INVALID_VRM", "VRMC_vrm human bone entry is invalid."); AddBone(map, pair.Name, IntProperty(value, "node", 0, nodes.Count - 1, "human bone node")); }
            return map;
        }

        static IDictionary<string, int> ParseLegacyHumanoid(JObject root, JObject humanoid)
        {
            Checks.Require(humanoid != null, "INVALID_VRM", "VRM humanoid is required."); var bones = humanoid["humanBones"] as JArray; Checks.Require(bones != null, "INVALID_VRM", "VRM humanBones is required.");
            var nodes = Array(root, "nodes"); var map = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var token in bones) { var value = token as JObject; Checks.Require(value != null, "INVALID_VRM", "VRM human bone entry is invalid."); string bone = StringProperty(value, "bone", 128, "human bone name"); AddBone(map, bone, IntProperty(value, "node", 0, nodes.Count - 1, "human bone node")); }
            return map;
        }

        static void AddBone(IDictionary<string, int> map, string name, int node) { Checks.Require(map.TryAdd(name, node), "INVALID_VRM", "VRM humanoid mapping repeats a bone."); }
        static JArray Array(JObject owner, string property) { var value = owner[property] as JArray; Checks.Require(value != null, "INVALID_VRM", "VRM property is missing: " + property); return value; }
        static int IntProperty(JObject owner, string property, int minimum, int maximum, string label) { var token = owner[property]; Checks.Require(token != null && token.Type == JTokenType.Integer, "INVALID_VRM", label + " is invalid."); int value = (int)token; Checks.Require(value >= minimum && value <= maximum, "INVALID_VRM", label + " is out of range."); return value; }
        static float NumberProperty(JObject owner, string property, float minimum, float maximum, string label) { var token = owner[property]; Checks.Require(token != null && (token.Type == JTokenType.Float || token.Type == JTokenType.Integer), "INVALID_VRM", label + " is invalid."); float value = (float)token; Checks.Finite(value); Checks.Require(value >= minimum && value <= maximum, "INVALID_VRM", label + " is out of range."); return value; }
        static string StringProperty(JObject owner, string property, int maximum, string label) { var token = owner[property]; Checks.Require(token != null && token.Type == JTokenType.String && !string.IsNullOrWhiteSpace((string)token) && ((string)token).Length <= maximum, "INVALID_VRM", label + " is invalid."); return (string)token; }
        static string OptionalString(JObject owner, string property, int maximum) { var token = owner[property]; if (token == null || token.Type == JTokenType.Null) return ""; Checks.Require(token.Type == JTokenType.String && ((string)token).Length <= maximum, "INVALID_VRM", "VRM text property is invalid: " + property); return (string)token; }
        static string StringPropertyName(string name, string label) { Checks.Require(!string.IsNullOrWhiteSpace(name) && name.Length <= 128 && name.IndexOf('\0') < 0, "INVALID_VRM", label + " is invalid."); return name; }
    }
}
