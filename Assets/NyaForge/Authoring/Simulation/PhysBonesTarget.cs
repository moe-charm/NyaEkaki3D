using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Simulation
{
    public enum PhysBonesEndpointMode { Auto = 0, Bone = 1, Position = 2 }
    public enum PhysBonesMultiChildType { Ignore = 0, First = 1, All = 2 }
    public enum PhysBonesLimitType { None = 0, Angle = 1, Hinge = 2, Polar = 3 }
    public enum PhysBonesCurveChannel
    {
        Radius = 0, MaxAngle = 1, Stiffness = 2, Pull = 3, Spring = 4, Immobile = 5,
        Gravity = 6, Damping = 7, Elasticity = 8, Inert = 9, Friction = 10,
        StretchMotion = 11, Squish = 12
    }

    /// <summary>A bounded, Unity-independent key for a PhysBone value curve.</summary>
    public struct PhysBonesCurveKey
    {
        public float Time { get; }
        public float Value { get; }

        public PhysBonesCurveKey(float time, float value)
        {
            Checks.Finite(time); Checks.Finite(value);
            Checks.Require(time >= 0f && time <= 1f, "INVALID_PHYSBONES", "PhysBones curve time must be between 0 and 1.");
            Time = Checks.Canonical(time); Value = Checks.Canonical(value);
        }
    }

    /// <summary>Curve data is kept as bounded keyframes so the Core does not depend on AnimationCurve.</summary>
    public sealed class PhysBonesCurve
    {
        public const int MaxKeys = 32;
        public PhysBonesCurveChannel Channel { get; }
        public IReadOnlyList<PhysBonesCurveKey> Keys { get; }

        public PhysBonesCurve(PhysBonesCurveChannel channel, IEnumerable<PhysBonesCurveKey> keys)
        {
            ValidateChannel(channel); Checks.Require(keys != null, "INVALID_PHYSBONES", "PhysBones curve keys are required.");
            var values = keys.ToArray(); Checks.Require(values.Length > 0 && values.Length <= MaxKeys, "BUDGET_EXCEEDED", "PhysBones curve key count exceeds capacity.");
            float previous = -1f;
            foreach (var key in values)
            {
                Checks.Require(key.Time >= previous, "INVALID_PHYSBONES", "PhysBones curve keys must be sorted by time.");
                ValidateValue(channel, key.Value); previous = key.Time;
            }
            Channel = channel; Keys = Array.AsReadOnly(values);
        }

        internal static void ValidateChannel(PhysBonesCurveChannel channel)
        {
            Checks.Require(channel >= PhysBonesCurveChannel.Radius && channel <= PhysBonesCurveChannel.Squish, "INVALID_PHYSBONES", "Unknown PhysBones curve channel.");
        }

        internal static void ValidateValue(PhysBonesCurveChannel channel, float value)
        {
            Checks.Finite(value);
            if (channel == PhysBonesCurveChannel.Radius) Checks.Require(value >= 0f && value <= 10f, "INVALID_PHYSBONES", "PhysBones radius curve value is outside 0 to 10.");
            else if (channel == PhysBonesCurveChannel.MaxAngle) Checks.Require(value >= 0f && value <= 180f, "INVALID_PHYSBONES", "PhysBones angle curve value is outside 0 to 180.");
            else Checks.Require(value >= 0f && value <= 1f, "INVALID_PHYSBONES", "PhysBones normalized curve value is outside 0 to 1.");
        }
    }

    /// <summary>Numeric PhysBone limits and forces. Values use the target's documented normalized ranges.</summary>
    public sealed class PhysBonesParameters
    {
        public PhysBonesLimitType LimitType { get; }
        public float MaxAngle { get; }
        public float Radius { get; }
        public float Stiffness { get; }
        public float Pull { get; }
        public float Spring { get; }
        public float Immobile { get; }
        public float Gravity { get; }
        public float GravityFalloff { get; }
        public float Damping { get; }
        public float Elasticity { get; }
        public float Inert { get; }
        public float Friction { get; }
        public float StretchMotion { get; }
        public float Squish { get; }
        public Vec3 GravityDirection { get; }

        public PhysBonesParameters(PhysBonesLimitType limitType, float maxAngle, float radius, float stiffness, float pull,
            float spring, float immobile, float gravity, float gravityFalloff, float damping, float elasticity, float inert,
            float friction, float stretchMotion, float squish, Vec3 gravityDirection)
        {
            Checks.Require(limitType >= PhysBonesLimitType.None && limitType <= PhysBonesLimitType.Polar, "INVALID_PHYSBONES", "Unknown PhysBones limit type.");
            Range(maxAngle, 0f, 180f, "max angle"); Range(radius, 0f, 10f, "radius");
            Range(stiffness, 0f, 1f, "stiffness"); Range(pull, 0f, 1f, "pull"); Range(spring, 0f, 1f, "spring");
            Range(immobile, 0f, 1f, "immobile"); Range(gravity, 0f, 1f, "gravity"); Range(gravityFalloff, 0f, 1f, "gravity falloff");
            Range(damping, 0f, 1f, "damping"); Range(elasticity, 0f, 1f, "elasticity"); Range(inert, 0f, 1f, "inert");
            Range(friction, 0f, 1f, "friction"); Range(stretchMotion, 0f, 1f, "stretch motion"); Range(squish, 0f, 1f, "squish");
            Checks.Finite(gravityDirection); Checks.Require(LengthSquared(gravityDirection) > 1e-12f || gravity == 0f, "INVALID_PHYSBONES", "A nonzero gravity direction is required when gravity is enabled.");
            LimitType = limitType; MaxAngle = Checks.Canonical(maxAngle); Radius = Checks.Canonical(radius); Stiffness = Checks.Canonical(stiffness); Pull = Checks.Canonical(pull); Spring = Checks.Canonical(spring); Immobile = Checks.Canonical(immobile); Gravity = Checks.Canonical(gravity); GravityFalloff = Checks.Canonical(gravityFalloff); Damping = Checks.Canonical(damping); Elasticity = Checks.Canonical(elasticity); Inert = Checks.Canonical(inert); Friction = Checks.Canonical(friction); StretchMotion = Checks.Canonical(stretchMotion); Squish = Checks.Canonical(squish); GravityDirection = gravityDirection;
        }

        public static PhysBonesParameters Default { get { return new PhysBonesParameters(PhysBonesLimitType.None, 0f, 0f, .5f, .5f, .5f, 0f, 0f, .5f, 0f, 0f, 0f, 0f, 0f, 0f, new Vec3(0, -1, 0)); } }

        static void Range(float value, float min, float max, string label) { Checks.Finite(value); Checks.Require(value >= min && value <= max, "INVALID_PHYSBONES", "PhysBones " + label + " is outside its bounded range."); }
        static float LengthSquared(Vec3 value) { return value.X * value.X + value.Y * value.Y + value.Z * value.Z; }
    }

    /// <summary>Interaction flags are target data, separate from the simulator-neutral chain topology.</summary>
    public sealed class PhysBonesInteraction
    {
        public bool AllowPosing { get; }
        public bool AllowCollision { get; }
        public bool AllowGrabbing { get; }
        public bool SnapToHand { get; }
        public bool ResetWhenDisabled { get; }
        public bool IsAnimated { get; }
        public string Parameter { get; }

        public PhysBonesInteraction(bool allowPosing, bool allowCollision, bool allowGrabbing, bool snapToHand,
            bool resetWhenDisabled, bool isAnimated, string parameter)
        {
            Checks.Require(parameter != null && parameter.Length <= 128 && parameter.IndexOf('\0') < 0, "INVALID_PHYSBONES", "PhysBones parameter is invalid.");
            AllowPosing = allowPosing; AllowCollision = allowCollision; AllowGrabbing = allowGrabbing; SnapToHand = snapToHand; ResetWhenDisabled = resetWhenDisabled; IsAnimated = isAnimated; Parameter = parameter;
        }

        public static PhysBonesInteraction Default { get { return new PhysBonesInteraction(false, true, true, false, true, false, ""); } }
    }

    /// <summary>Explicit branch mapping retained when a root has multiple child paths.</summary>
    public sealed class PhysBonesBranch
    {
        public string ParentBoneId { get; }
        public IReadOnlyList<string> ChildBoneIds { get; }

        public PhysBonesBranch(string parentBoneId, IEnumerable<string> childBoneIds)
        {
            Checks.Id(parentBoneId); Checks.Require(childBoneIds != null, "INVALID_PHYSBONES", "PhysBones branch children are required.");
            var values = childBoneIds.ToArray(); Checks.Require(values.Length > 0 && values.Length <= 64, "BUDGET_EXCEEDED", "PhysBones branch child count exceeds capacity.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in values) { Checks.Id(id); Checks.Require(seen.Add(id), "INVALID_PHYSBONES", "PhysBones branch repeats a child bone."); }
            ParentBoneId = parentBoneId; ChildBoneIds = Array.AsReadOnly(values);
        }
    }

    /// <summary>One target PhysBone component described entirely with stable authoring IDs.</summary>
    public sealed class PhysBonesChain
    {
        public const int MaxBones = 1024;
        public const int MaxExcluded = 1024;
        public const int MaxBranches = 256;
        public const int MaxCurves = 32;
        public string Name { get; }
        public string RootBoneId { get; }
        public IReadOnlyList<string> BoneIds { get; }
        public PhysBonesEndpointMode EndpointMode { get; }
        public string EndBoneId { get; }
        public Vec3? EndpointPosition { get; }
        public PhysBonesMultiChildType MultiChildType { get; }
        public IReadOnlyList<string> ExcludedBoneIds { get; }
        public IReadOnlyList<PhysBonesBranch> Branches { get; }
        public IReadOnlyList<int> ColliderGroupIndices { get; }
        public PhysBonesParameters Parameters { get; }
        public PhysBonesInteraction Interaction { get; }
        public IReadOnlyList<PhysBonesCurve> Curves { get; }

        public PhysBonesChain(string name, string rootBoneId, IEnumerable<string> boneIds, PhysBonesEndpointMode endpointMode,
            string endBoneId, Vec3? endpointPosition, PhysBonesMultiChildType multiChildType, IEnumerable<string> excludedBoneIds,
            IEnumerable<PhysBonesBranch> branches, IEnumerable<int> colliderGroupIndices, PhysBonesParameters parameters,
            PhysBonesInteraction interaction, IEnumerable<PhysBonesCurve> curves)
        {
            Checks.Name(name); Checks.Id(rootBoneId); Checks.Require(boneIds != null, "INVALID_PHYSBONES", "PhysBones chain bones are required.");
            Checks.Require(endpointMode >= PhysBonesEndpointMode.Auto && endpointMode <= PhysBonesEndpointMode.Position, "INVALID_PHYSBONES", "Unknown PhysBones endpoint mode.");
            Checks.Require(multiChildType >= PhysBonesMultiChildType.Ignore && multiChildType <= PhysBonesMultiChildType.All, "INVALID_PHYSBONES", "Unknown PhysBones branch mode.");
            var bones = boneIds.ToArray(); Checks.Require(bones.Length > 0 && bones.Length <= MaxBones, "BUDGET_EXCEEDED", "PhysBones chain bone count exceeds capacity.");
            Checks.Require(bones[0] == rootBoneId, "INVALID_PHYSBONES", "PhysBones root must be the first ordered bone."); UniqueIds(bones, "chain bone");
            var excluded = (excludedBoneIds ?? Array.Empty<string>()).ToArray(); Checks.Require(excluded.Length <= MaxExcluded, "BUDGET_EXCEEDED", "PhysBones exclusion count exceeds capacity."); UniqueIds(excluded, "excluded bone");
            var boneSet = new HashSet<string>(bones, StringComparer.Ordinal); foreach (var id in excluded) Checks.Require(!boneSet.Contains(id), "INVALID_PHYSBONES", "A PhysBones bone cannot also be excluded.");
            if (endBoneId == null) endBoneId = ""; Checks.Require(endBoneId == "" || Guid.TryParseExact(endBoneId, "D", out _), "INVALID_PHYSBONES", "PhysBones endpoint bone identity is invalid.");
            if (endpointPosition.HasValue) Checks.Finite(endpointPosition.Value);
            Checks.Require(endpointMode != PhysBonesEndpointMode.Bone || endBoneId != "", "INVALID_PHYSBONES", "Bone endpoint mode requires an endpoint bone.");
            Checks.Require(endpointMode != PhysBonesEndpointMode.Position || endpointPosition.HasValue, "INVALID_PHYSBONES", "Position endpoint mode requires an endpoint position.");
            Checks.Require(endpointMode == PhysBonesEndpointMode.Bone || endBoneId == "", "INVALID_PHYSBONES", "Endpoint bone is only valid for bone endpoint mode.");
            Checks.Require(endpointMode == PhysBonesEndpointMode.Position || !endpointPosition.HasValue, "INVALID_PHYSBONES", "Endpoint position is only valid for position endpoint mode.");
            var branchValues = (branches ?? Array.Empty<PhysBonesBranch>()).ToArray(); Checks.Require(branchValues.Length <= MaxBranches, "BUDGET_EXCEEDED", "PhysBones branch count exceeds capacity.");
            var branchParents = new HashSet<string>(StringComparer.Ordinal); foreach (var branch in branchValues) { Checks.Require(branch != null, "INVALID_PHYSBONES", "PhysBones branch cannot be null."); Checks.Require(boneSet.Contains(branch.ParentBoneId) && branchParents.Add(branch.ParentBoneId), "INVALID_PHYSBONES", "PhysBones branch parent must be a unique chain bone."); foreach (var id in branch.ChildBoneIds) Checks.Require(!boneSet.Contains(id) && !excluded.Contains(id), "INVALID_PHYSBONES", "PhysBones branch child overlaps the chain or exclusion list."); }
            var colliders = (colliderGroupIndices ?? Array.Empty<int>()).ToArray(); foreach (var index in colliders) Checks.Require(index >= 0, "INVALID_PHYSBONES", "PhysBones collider group index cannot be negative.");
            var curveValues = (curves ?? Array.Empty<PhysBonesCurve>()).ToArray(); Checks.Require(curveValues.Length <= MaxCurves, "BUDGET_EXCEEDED", "PhysBones curve count exceeds capacity."); var channels = new HashSet<PhysBonesCurveChannel>(); foreach (var curve in curveValues) { Checks.Require(curve != null && channels.Add(curve.Channel), "INVALID_PHYSBONES", "PhysBones curve channel repeats or is null."); }
            Checks.Require(parameters != null && interaction != null, "INVALID_PHYSBONES", "PhysBones parameters and interaction settings are required.");
            Name = name; RootBoneId = rootBoneId; BoneIds = Array.AsReadOnly(bones); EndpointMode = endpointMode; EndBoneId = endBoneId; EndpointPosition = endpointPosition; MultiChildType = multiChildType; ExcludedBoneIds = Array.AsReadOnly(excluded); Branches = Array.AsReadOnly(branchValues); ColliderGroupIndices = Array.AsReadOnly(colliders); Parameters = parameters; Interaction = interaction; Curves = Array.AsReadOnly(curveValues);
        }

        static void UniqueIds(IEnumerable<string> ids, string label)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal); foreach (var id in ids) { Checks.Id(id); Checks.Require(seen.Add(id), "INVALID_PHYSBONES", "PhysBones " + label + " repeats an identity."); }
        }
    }

    /// <summary>Versioned target profile for VRChat PhysBones. SDK data never enters the common asset fields.</summary>
    public sealed class PhysBonesTargetProfile
    {
        public const int SchemaVersion = 1;
        public const int MaxChains = 256;
        public string TargetId { get; }
        public string SdkVersion { get; }
        public string PackageVersion { get; }
        public string SkeletonHash { get; }
        public string SourceSecondaryMotionHash { get; }
        public IReadOnlyList<PhysBonesChain> Chains { get; }
        public string ContentHash { get; }

        public PhysBonesTargetProfile(string targetId, string sdkVersion, string packageVersion, string skeletonHash,
            string sourceSecondaryMotionHash, IEnumerable<PhysBonesChain> chains)
        {
            SecondaryMotionCapabilities.Text(targetId, "PhysBones target identity"); SecondaryMotionCapabilities.Text(sdkVersion, "PhysBones SDK version"); SecondaryMotionCapabilities.OptionalText(packageVersion, "PhysBones package version");
            Hash(skeletonHash, "PhysBones skeleton hash"); HashOptional(sourceSecondaryMotionHash, "PhysBones source asset hash"); Checks.Require(chains != null, "INVALID_PHYSBONES", "PhysBones chains are required.");
            var values = chains.ToArray(); Checks.Require(values.Length > 0 && values.Length <= MaxChains, "BUDGET_EXCEEDED", "PhysBones chain count exceeds capacity."); foreach (var chain in values) Checks.Require(chain != null, "INVALID_PHYSBONES", "PhysBones chain cannot be null.");
            TargetId = targetId; SdkVersion = sdkVersion; PackageVersion = packageVersion ?? ""; SkeletonHash = skeletonHash; SourceSecondaryMotionHash = sourceSecondaryMotionHash ?? ""; Chains = Array.AsReadOnly(values); ContentHash = Checks.Hash(PhysBonesTargetCodec.Write(this));
        }

        public void ValidateFor(SecondaryMotionAsset source, SkeletonDefinition skeleton)
        {
            Checks.Require(skeleton != null && skeleton.ContentHash == SkeletonHash, "SIMULATION_SKELETON_CHANGED", "PhysBones profile skeleton identity changed; rebind the setup.");
            if (SourceSecondaryMotionHash != "") Checks.Require(source != null && source.ContentHash == SourceSecondaryMotionHash, "SIMULATION_ASSET_CHANGED", "PhysBones profile source asset changed; rebind the setup.");
            foreach (var chain in Chains)
            {
                foreach (var id in chain.BoneIds) Checks.Require(skeleton.ById.ContainsKey(id), "SIMULATION_BONE_MISSING", "PhysBones references a missing chain bone.");
                foreach (var id in chain.ExcludedBoneIds) Checks.Require(skeleton.ById.ContainsKey(id), "SIMULATION_BONE_MISSING", "PhysBones exclusion references a missing bone.");
                if (chain.EndBoneId != "") Checks.Require(skeleton.ById.ContainsKey(chain.EndBoneId), "SIMULATION_BONE_MISSING", "PhysBones endpoint references a missing bone.");
                foreach (var branch in chain.Branches) { Checks.Require(skeleton.ById.ContainsKey(branch.ParentBoneId), "SIMULATION_BONE_MISSING", "PhysBones branch parent is missing."); foreach (var id in branch.ChildBoneIds) Checks.Require(skeleton.ById.ContainsKey(id), "SIMULATION_BONE_MISSING", "PhysBones branch child is missing."); }
                foreach (var index in chain.ColliderGroupIndices) Checks.Require(source != null && index < source.ColliderGroups.Count, "INVALID_PHYSBONES", "PhysBones references a missing collider group.");
            }
        }

        static void Hash(string value, string label) { Checks.Require(value != null, "INVALID_PHYSBONES", label + " is required."); Checks.HashText(value); }
        static void HashOptional(string value, string label) { Checks.Require(value != null, "INVALID_PHYSBONES", label + " is invalid."); if (value != "") Checks.HashText(value); }
    }

    /// <summary>Stable feature names understood by the target Bridge and loss-report generator.</summary>
    public static class PhysBonesFeatures
    {
        public const string Root = "root"; public const string Endpoint = "endpoint"; public const string Exclusions = "exclusions"; public const string Branches = "branches"; public const string Colliders = "colliders"; public const string Limits = "limits"; public const string Curves = "curves"; public const string Interaction = "interaction"; public const string Parameter = "parameter";
        public static readonly IReadOnlyList<string> All = Array.AsReadOnly(new[] { Root, Endpoint, Exclusions, Branches, Colliders, Limits, Curves, Interaction, Parameter });
    }

    /// <summary>Target capability declaration. The Bridge supplies this from the installed SDK package.</summary>
    public sealed class PhysBonesCapabilities
    {
        public string TargetId { get; }
        public string SdkVersion { get; }
        public IReadOnlyList<string> SupportedFeatures { get; }

        public PhysBonesCapabilities(string targetId, string sdkVersion, IEnumerable<string> supportedFeatures)
        {
            SecondaryMotionCapabilities.Text(targetId, "PhysBones capability target identity"); SecondaryMotionCapabilities.Text(sdkVersion, "PhysBones capability SDK version"); Checks.Require(supportedFeatures != null, "INVALID_PHYSBONES", "PhysBones supported features are required.");
            var values = supportedFeatures.ToArray(); Checks.Require(values.Length <= PhysBonesFeatures.All.Count, "BUDGET_EXCEEDED", "PhysBones feature count exceeds capacity."); var seen = new HashSet<string>(StringComparer.Ordinal); foreach (var feature in values) { SecondaryMotionCapabilities.Text(feature, "PhysBones feature"); Checks.Require(PhysBonesFeatures.All.Contains(feature), "INVALID_PHYSBONES", "Unknown PhysBones feature."); Checks.Require(seen.Add(feature), "INVALID_PHYSBONES", "PhysBones feature repeats."); }
            TargetId = targetId; SdkVersion = sdkVersion; SupportedFeatures = Array.AsReadOnly(values);
        }

        public bool Supports(string feature) { return SupportedFeatures.Contains(feature); }
    }

    /// <summary>One explicit supported/unsupported item produced before any SDK component is mutated.</summary>
    public sealed class PhysBonesLossEntry
    {
        public string Code { get; }
        public string Path { get; }
        public string Message { get; }
        public PhysBonesLossEntry(string code, string path, string message)
        {
            SecondaryMotionCapabilities.Text(code, "PhysBones loss code"); SecondaryMotionCapabilities.Text(path, "PhysBones loss path"); SecondaryMotionCapabilities.Text(message, "PhysBones loss message"); Code = code; Path = path; Message = message;
        }
    }

    /// <summary>Deterministic target mapping result. Unsupported data is reported and never silently dropped.</summary>
    public sealed class PhysBonesLossReport
    {
        public string TargetId { get; }
        public string SdkVersion { get; }
        public string SourceProfileHash { get; }
        public IReadOnlyList<PhysBonesLossEntry> Supported { get; }
        public IReadOnlyList<PhysBonesLossEntry> Unsupported { get; }
        public IReadOnlyList<PhysBonesLossEntry> Warnings { get; }
        public bool IsLossless { get { return Unsupported.Count == 0; } }
        public string ContentHash { get; }

        private PhysBonesLossReport(string targetId, string sdkVersion, string sourceProfileHash, IEnumerable<PhysBonesLossEntry> supported, IEnumerable<PhysBonesLossEntry> unsupported, IEnumerable<PhysBonesLossEntry> warnings)
        {
            SecondaryMotionCapabilities.Text(targetId, "PhysBones report target identity"); SecondaryMotionCapabilities.Text(sdkVersion, "PhysBones report SDK version"); Checks.HashText(sourceProfileHash);
            Supported = Entries(supported); Unsupported = Entries(unsupported); Warnings = Entries(warnings); TargetId = targetId; SdkVersion = sdkVersion; SourceProfileHash = sourceProfileHash; ContentHash = Checks.Hash(WriteCanonical());
        }

        public static PhysBonesLossReport Compare(PhysBonesTargetProfile profile, PhysBonesCapabilities capabilities)
        {
            Checks.Require(profile != null && capabilities != null, "INVALID_PHYSBONES", "PhysBones loss report inputs are required."); Checks.Require(profile.TargetId == capabilities.TargetId, "INVALID_PHYSBONES", "PhysBones target identity differs from capability declaration.");
            var supported = new List<PhysBonesLossEntry>(); var unsupported = new List<PhysBonesLossEntry>(); var warnings = new List<PhysBonesLossEntry>();
            foreach (var feature in RequiredFeatures(profile))
            {
                var entry = new PhysBonesLossEntry(capabilities.Supports(feature) ? "SUPPORTED" : "UNSUPPORTED_FEATURE", feature, capabilities.Supports(feature) ? "Target capability covers " + feature + "." : "Target capability does not cover " + feature + "; Bridge must stop before writing this value.");
                (capabilities.Supports(feature) ? supported : unsupported).Add(entry);
            }
            if (profile.SdkVersion != capabilities.SdkVersion) warnings.Add(new PhysBonesLossEntry("SDK_VERSION_MISMATCH", "sdkVersion", "Profile SDK version differs from the installed target capability version."));
            return new PhysBonesLossReport(profile.TargetId, capabilities.SdkVersion, profile.ContentHash, supported, unsupported, warnings);
        }

        static IEnumerable<string> RequiredFeatures(PhysBonesTargetProfile profile)
        {
            var result = new List<string> { PhysBonesFeatures.Root };
            foreach (var chain in profile.Chains)
            {
                if (chain.EndpointMode != PhysBonesEndpointMode.Auto) result.Add(PhysBonesFeatures.Endpoint);
                if (chain.ExcludedBoneIds.Count > 0) result.Add(PhysBonesFeatures.Exclusions);
                if (chain.Branches.Count > 0 || chain.MultiChildType != PhysBonesMultiChildType.Ignore) result.Add(PhysBonesFeatures.Branches);
                if (chain.ColliderGroupIndices.Count > 0) result.Add(PhysBonesFeatures.Colliders);
                if (chain.Parameters != null) result.Add(PhysBonesFeatures.Limits);
                if (chain.Curves.Count > 0) result.Add(PhysBonesFeatures.Curves);
                if (chain.Interaction != null) { result.Add(PhysBonesFeatures.Interaction); if (chain.Interaction.Parameter != "") result.Add(PhysBonesFeatures.Parameter); }
            }
            return result.Distinct(StringComparer.Ordinal);
        }

        static IReadOnlyList<PhysBonesLossEntry> Entries(IEnumerable<PhysBonesLossEntry> values)
        {
            var list = (values ?? Array.Empty<PhysBonesLossEntry>()).ToArray(); Checks.Require(list.Length <= 256, "BUDGET_EXCEEDED", "PhysBones loss report entry count exceeds capacity."); foreach (var value in list) Checks.Require(value != null, "INVALID_PHYSBONES", "PhysBones loss report entry cannot be null."); return Array.AsReadOnly(list);
        }

        byte[] WriteCanonical()
        {
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(TargetId); writer.Write(SdkVersion); writer.Write(SourceProfileHash); WriteEntries(writer, Supported); WriteEntries(writer, Unsupported); WriteEntries(writer, Warnings); writer.Flush(); return stream.ToArray();
            }
        }
        static void WriteEntries(BinaryWriter writer, IReadOnlyList<PhysBonesLossEntry> entries) { writer.Write(entries.Count); foreach (var entry in entries) { writer.Write(entry.Code); writer.Write(entry.Path); writer.Write(entry.Message); } }
    }
}
