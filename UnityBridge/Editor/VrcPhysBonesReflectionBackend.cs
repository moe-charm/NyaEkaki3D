using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Simulation;
using UnityEditor;
using UnityEngine;

namespace NyaForge.UnityBridge.Editor
{
    /// <summary>
    /// Reflection adapter for the optional VRChat SDK. The public package remains buildable without the SDK assembly.
    /// </summary>
    public sealed class VrcPhysBonesReflectionBackend : IPhysBonesComponentBackend, IPhysBonesComponentPreflight
    {
        public const string Target = "vrchat.physbones";
        public const string DefaultComponentType = PhysBonesTargetPackage.DefaultComponentTypeName;

        sealed class Snapshot
        {
            internal readonly Dictionary<string, object> Values = new Dictionary<string, object>(StringComparer.Ordinal);
        }

        readonly Type componentType;
        readonly Dictionary<string, VrcPhysBonesReflectionMember> members;
        readonly PhysBonesCapabilities capabilities;

        public Type ComponentType { get { return componentType; } }
        public PhysBonesCapabilities Capabilities { get { return capabilities; } }

        VrcPhysBonesReflectionBackend(Type type, string sdkVersion)
        {
            componentType = type;
            members = VrcPhysBonesReflectionMemberCatalog.Discover(type);
            capabilities = new PhysBonesCapabilities(Target, string.IsNullOrEmpty(sdkVersion) ? "unknown" : sdkVersion, Features());
        }

        public static bool TryCreate(out VrcPhysBonesReflectionBackend backend, string sdkVersion = "unknown",
            string componentTypeName = DefaultComponentType)
        {
            string diagnostic;
            return TryCreate(out backend, sdkVersion, componentTypeName, out diagnostic);
        }

        public static bool TryCreate(out VrcPhysBonesReflectionBackend backend, string sdkVersion,
            string componentTypeName, out string diagnostic)
        {
            backend = null;
            var resolution = VrcPhysBonesReflectionResolver.Resolve(componentTypeName);
            diagnostic = resolution.Diagnostic;
            if (!resolution.IsResolved) return false;
            backend = new VrcPhysBonesReflectionBackend(resolution.ComponentType, sdkVersion);
            return true;
        }

        public Component Create(Transform owner)
        {
            if (owner == null) throw new ArgumentNullException("owner");
            try { return Undo.AddComponent(owner.gameObject, componentType); }
            catch (Exception error) { throw new PhysBonesBridgeException("SDK_CREATE_FAILED", error.Message); }
        }

        public object Capture(Component component)
        {
            if (component == null || !componentType.IsInstanceOfType(component)) throw new ArgumentException("Unexpected PhysBones component.");
            var snapshot = new Snapshot();
            foreach (var pair in members) snapshot.Values[pair.Key] = CopyValue(pair.Value.Get(component));
            return snapshot;
        }

        public void Validate(PhysBonesChain chain, PhysBonesBridgeContext context)
        {
            if (chain == null) throw new ArgumentNullException("chain");
            if (context == null) throw new ArgumentNullException("context");
            ValidateParameters(chain.Parameters);
            ValidateAdvancedBool(new[] { "allowPosing", "AllowPosing" });
            ValidateAdvancedBool(new[] { "allowCollision", "AllowCollision" });
            ValidateAdvancedBool(new[] { "allowGrabbing", "AllowGrabbing" });
            ValidateBool(new[] { "snapToHand", "SnapToHand" });
            if (chain.Branches.Count == 0) return;
            var member = Find(new[] { "multiChildType", "MultiChildType" });
            if (member == null) throw new PhysBonesBridgeException("SDK_MEMBER_MISSING", "PhysBones branch mode is unavailable.");
            if (chain.MultiChildType == PhysBonesMultiChildType.Ignore)
                throw new PhysBonesBridgeException("UNSUPPORTED_BRANCH_MAPPING", "A target branch list cannot be represented with Ignore multi-child mode.");
            foreach (var branch in chain.Branches)
            {
                var parent = context.Bone(branch.ParentBoneId);
                var selected = branch.ChildBoneIds.Select(context.Bone).ToArray();
                if (selected.Any(child => child.parent != parent))
                    throw new PhysBonesBridgeException("UNSUPPORTED_BRANCH_MAPPING", "A target branch contains a non-direct child: " + branch.ParentBoneId + ".");
                if (chain.MultiChildType == PhysBonesMultiChildType.First)
                {
                    if (parent.childCount == 0 || selected.Length != 1 || selected[0] != parent.GetChild(0))
                        throw new PhysBonesBridgeException("UNSUPPORTED_BRANCH_MAPPING", "PhysBones First mode can represent only the first direct child.");
                }
                else if (selected.Length != parent.childCount || !selected.SequenceEqual(Enumerable.Range(0, parent.childCount).Select(parent.GetChild)))
                    throw new PhysBonesBridgeException("UNSUPPORTED_BRANCH_MAPPING", "PhysBones All mode requires every direct child in scene order.");
            }
        }

        public void Restore(Component component, object value)
        {
            var snapshot = value as Snapshot;
            if (snapshot == null) throw new ArgumentException("Unexpected PhysBones snapshot.");
            foreach (var pair in snapshot.Values)
            {
                VrcPhysBonesReflectionMember member;
                if (members.TryGetValue(pair.Key, out member)) member.Set(component, CopyValue(pair.Value));
            }
            EditorUtility.SetDirty(component);
        }

        public void Configure(Component component, PhysBonesChain chain, PhysBonesBridgeContext context)
        {
            if (component == null || !componentType.IsInstanceOfType(component)) throw new ArgumentException("Unexpected PhysBones component.");
            SetRequired(component, new[] { "rootTransform", "RootTransform" }, context.Bone(chain.RootBoneId));
            if (chain.EndpointMode == PhysBonesEndpointMode.Auto) SetOptional(component, new[] { "endpointPosition", "EndpointPosition" }, Vector3.zero);
            else
            {
                Vector3 endpoint = chain.EndpointMode == PhysBonesEndpointMode.Position ? ToVector(chain.EndpointPosition.Value) :
                    context.Bone(chain.RootBoneId).InverseTransformPoint(context.Bone(chain.EndBoneId).position);
                SetRequired(component, new[] { "endpointPosition", "EndpointPosition" }, endpoint);
            }
            if (chain.MultiChildType == PhysBonesMultiChildType.Ignore) SetOptionalEnum(component, new[] { "multiChildType", "MultiChildType" }, MultiChildName(chain.MultiChildType));
            else SetEnum(component, new[] { "multiChildType", "MultiChildType" }, MultiChildName(chain.MultiChildType));
            SetTransforms(component, new[] { "ignoreTransforms", "IgnoreTransforms", "exclusions", "Exclusions" },
                chain.ExcludedBoneIds.Select(context.Bone));
            SetComponents(component, new[] { "colliders", "Colliders" }, context.Colliders(chain));

            var p = chain.Parameters;
            SetEnum(component, new[] { "limitType", "LimitType" }, LimitName(p.LimitType));
            // VRC SDK 3.7 exposes separate X/Z angle limits, while older
            // compatible components expose one maxAngle field. Set every
            // available representation so the conversion is deterministic.
            SetMappedNumber(component, new[] { "maxAngle", "MaxAngle", "maxAngleX", "MaxAngleX", "maxAngleZ", "MaxAngleZ" }, p.MaxAngle);
            SetMappedNumber(component, new[] { "radius", "Radius" }, p.Radius);
            SetMappedNumber(component, new[] { "stiffness", "Stiffness" }, p.Stiffness);
            SetMappedNumber(component, new[] { "pull", "Pull" }, p.Pull);
            SetMappedNumber(component, new[] { "spring", "Spring" }, p.Spring);
            SetMappedNumber(component, new[] { "immobile", "Immobile" }, p.Immobile);
            SetMappedNumber(component, new[] { "gravity", "Gravity" }, p.Gravity);
            SetMappedNumber(component, new[] { "gravityFalloff", "GravityFalloff" }, p.GravityFalloff);
            SetMappedNumber(component, new[] { "damping", "Damping" }, p.Damping);
            SetMappedNumber(component, new[] { "elasticity", "Elasticity" }, p.Elasticity);
            SetMappedNumber(component, new[] { "inert", "Inert" }, p.Inert);
            SetMappedNumber(component, new[] { "friction", "Friction" }, p.Friction);
            SetMappedNumber(component, new[] { "stretchMotion", "StretchMotion" }, p.StretchMotion);
            // Newer VRC SDKs call this limit maxSquish; preserve the generic
            // name for older adapters and reject a nonzero value if neither is
            // present rather than silently dropping it.
            SetMappedNumber(component, new[] { "squish", "Squish", "maxSquish", "MaxSquish" }, p.Squish);
            SetMappedGravityDirection(component, p);

            var interaction = chain.Interaction;
            SetAdvancedBool(component, new[] { "allowPosing", "AllowPosing" }, interaction.AllowPosing);
            SetAdvancedBool(component, new[] { "allowCollision", "AllowCollision" }, interaction.AllowCollision);
            SetAdvancedBool(component, new[] { "allowGrabbing", "AllowGrabbing" }, interaction.AllowGrabbing);
            SetBool(component, new[] { "snapToHand", "SnapToHand" }, interaction.SnapToHand);
            SetOptionalBool(component, new[] { "resetWhenDisabled", "ResetWhenDisabled" }, interaction.ResetWhenDisabled);
            SetOptionalBool(component, new[] { "isAnimated", "IsAnimated" }, interaction.IsAnimated);
            if (interaction.Parameter == "") SetOptionalString(component, new[] { "parameter", "Parameter" }, interaction.Parameter);
            else SetString(component, new[] { "parameter", "Parameter" }, interaction.Parameter);
            var authoredCurves = new HashSet<PhysBonesCurveChannel>(chain.Curves.Select(curve => curve.Channel));
            // A managed component may have curves from an earlier profile. Clear
            // channels omitted by the new profile so re-apply cannot leave stale
            // SDK AnimationCurves behind.
            foreach (PhysBonesCurveChannel channel in Enum.GetValues(typeof(PhysBonesCurveChannel)))
            {
                if (authoredCurves.Contains(channel)) continue;
                var member = Find(new[] { CurveName(channel), Upper(CurveName(channel)) });
                if (member != null && member.ValueType == typeof(AnimationCurve)) member.Set(component, new AnimationCurve());
            }
            foreach (var curve in chain.Curves) SetCurve(component, curve);
        }

        IEnumerable<string> Features()
        {
            var result = new List<string>();
            if (Find(new[] { "rootTransform", "RootTransform" }) != null) result.Add(PhysBonesFeatures.Root);
            if (Find(new[] { "endpointPosition", "EndpointPosition" }) != null) result.Add(PhysBonesFeatures.Endpoint);
            if (Find(new[] { "ignoreTransforms", "IgnoreTransforms", "exclusions", "Exclusions" }) != null) result.Add(PhysBonesFeatures.Exclusions);
            if (Find(new[] { "multiChildType", "MultiChildType" }) != null) result.Add(PhysBonesFeatures.Branches);
            if (Find(new[] { "colliders", "Colliders" }) != null) result.Add(PhysBonesFeatures.Colliders);
            // VRC SDK 3.7 uses maxAngleX/maxAngleZ and omits several older
            // generic parameters. Value-level validation below rejects any
            // nonzero parameter for which the installed type has no member.
            if (Find(new[] { "limitType", "LimitType" }) != null &&
                HasAny(new[] { "maxAngle", "MaxAngle", "maxAngleX", "MaxAngleX", "maxAngleZ", "MaxAngleZ" }) &&
                HasAll(new[] { "radius", "stiffness", "pull", "spring", "immobile", "gravity", "gravityFalloff", "stretchMotion" })) result.Add(PhysBonesFeatures.Limits);
            if (HasAll(CurveMemberNames())) result.Add(PhysBonesFeatures.Curves);
            if (HasAll(new[] { "allowPosing", "allowCollision", "allowGrabbing", "snapToHand" })) result.Add(PhysBonesFeatures.Interaction);
            if (Find(new[] { "parameter", "Parameter" }) != null) result.Add(PhysBonesFeatures.Parameter);
            return result;
        }

        bool HasAll(IEnumerable<string> names) { return names.All(name => Find(new[] { name, Upper(name) }) != null); }
        bool HasAny(IEnumerable<string> names) { return names.Any(name => Find(new[] { name, Upper(name) }) != null); }

        void SetTransforms(Component component, string[] names, IEnumerable<Transform> values)
        {
            var member = Find(names);
            if (member == null)
            {
                if (values.Any()) throw new PhysBonesBridgeException("SDK_MEMBER_MISSING", "PhysBones component is missing " + names[0] + ".");
                return;
            }
            SetCollection(member, component, values.Cast<UnityEngine.Object>().ToArray(), typeof(Transform), names[0], "BONE_TYPE_MISMATCH");
        }

        void SetComponents(Component component, string[] names, IEnumerable<Component> values)
        {
            var member = Find(names);
            if (member == null)
            {
                if (values.Any()) throw new PhysBonesBridgeException("SDK_MEMBER_MISSING", "PhysBones component is missing " + names[0] + ".");
                return;
            }
            SetCollection(member, component, values.Cast<UnityEngine.Object>().ToArray(), typeof(Component), names[0], "COLLIDER_TYPE_MISMATCH");
        }

        static void SetCollection(VrcPhysBonesReflectionMember member, Component component, UnityEngine.Object[] values, Type expectedBase, string name, string mismatchCode)
        {
            Type element = member.ValueType.IsArray ? member.ValueType.GetElementType() : member.ValueType.IsGenericType ? member.ValueType.GetGenericArguments()[0] : null;
            if (element == null || !expectedBase.IsAssignableFrom(element))
                throw new PhysBonesBridgeException("SDK_MEMBER_TYPE", "PhysBones member has an incompatible type: " + name + ".");
            var items = values ?? Array.Empty<UnityEngine.Object>();
            if (member.ValueType.IsArray)
            {
                var array = Array.CreateInstance(element, items.Length);
                for (int i = 0; i < items.Length; i++) { if (!element.IsInstanceOfType(items[i])) throw new PhysBonesBridgeException(mismatchCode, "PhysBones collection item type does not match the installed SDK."); array.SetValue(items[i], i); }
                member.Set(component, array);
                return;
            }
            if (!typeof(IList).IsAssignableFrom(member.ValueType)) throw new PhysBonesBridgeException("SDK_MEMBER_TYPE", "PhysBones collection is not assignable: " + name + ".");
            var list = Activator.CreateInstance(member.ValueType) as IList;
            if (list == null) throw new PhysBonesBridgeException("SDK_MEMBER_TYPE", "PhysBones collection cannot be constructed: " + name + ".");
            foreach (var item in items) { if (!element.IsInstanceOfType(item)) throw new PhysBonesBridgeException(mismatchCode, "PhysBones collection item type does not match the installed SDK."); list.Add(item); }
            member.Set(component, list);
        }

        void SetCurve(Component component, PhysBonesCurve curve)
        {
            string name = CurveName(curve.Channel);
            var member = Find(new[] { name, Upper(name) });
            if (member == null || member.ValueType != typeof(AnimationCurve)) throw new PhysBonesBridgeException("SDK_MEMBER_MISSING", "PhysBones curve member is unavailable: " + name + ".");
            member.Set(component, new AnimationCurve(curve.Keys.Select(key => new Keyframe(key.Time, key.Value)).ToArray()));
        }

        void SetEnum(Component component, string[] names, string value)
        {
            var member = Find(names);
            if (member == null) throw new PhysBonesBridgeException("SDK_MEMBER_MISSING", "PhysBones component is missing " + names[0] + ".");
            if (!member.ValueType.IsEnum) throw new PhysBonesBridgeException("SDK_MEMBER_TYPE", "PhysBones member is not an enum: " + names[0] + ".");
            try { member.Set(component, Enum.Parse(member.ValueType, value, true)); }
            catch { throw new PhysBonesBridgeException("SDK_ENUM_MISMATCH", "PhysBones enum does not contain " + value + "."); }
        }

        void SetVector(Component component, string[] names, Vector3 value) { SetTyped(component, names, value, "vector"); }
        void SetNumber(Component component, string[] names, float value) { SetTyped(component, names, value, "number"); }
        void SetMappedNumber(Component component, string[] names, float value)
        {
            bool assigned = false;
            foreach (var name in names)
            {
                var member = Find(new[] { name });
                if (member == null) continue;
                Assign(member, component, value, name); assigned = true;
            }
            if (!assigned && Mathf.Abs(value) > 0.000001f)
                throw new PhysBonesBridgeException("SDK_MEMBER_MISSING", "PhysBones member is unavailable for a nonzero value: " + names[0] + ".");
        }
        void SetMappedGravityDirection(Component component, PhysBonesParameters parameters)
        {
            var member = Find(new[] { "gravityDir", "GravityDir", "gravityDirection", "GravityDirection" });
            if (member != null) { Assign(member, component, ToVector(parameters.GravityDirection), "gravityDir"); return; }
            // VRC SDK 3.7 uses a fixed world gravity direction. A zero gravity
            // value makes the authored direction irrelevant; any active force
            // must stop before mutation because it cannot be represented.
            if (Mathf.Abs(parameters.Gravity) > 0.000001f)
                throw new PhysBonesBridgeException("SDK_MEMBER_MISSING", "The installed PhysBones SDK has no gravity direction member.");
        }
        void ValidateParameters(PhysBonesParameters parameters)
        {
            if (parameters == null) throw new PhysBonesBridgeException("INVALID_PHYSBONES", "PhysBones parameters are missing.");
            ValidateMappedNumber(parameters.MaxAngle, new[] { "maxAngle", "MaxAngle", "maxAngleX", "MaxAngleX", "maxAngleZ", "MaxAngleZ" });
            ValidateMappedNumber(parameters.Radius, new[] { "radius", "Radius" });
            ValidateMappedNumber(parameters.Stiffness, new[] { "stiffness", "Stiffness" });
            ValidateMappedNumber(parameters.Pull, new[] { "pull", "Pull" });
            ValidateMappedNumber(parameters.Spring, new[] { "spring", "Spring" });
            ValidateMappedNumber(parameters.Immobile, new[] { "immobile", "Immobile" });
            ValidateMappedNumber(parameters.Gravity, new[] { "gravity", "Gravity" });
            ValidateMappedNumber(parameters.GravityFalloff, new[] { "gravityFalloff", "GravityFalloff" });
            ValidateMappedNumber(parameters.Damping, new[] { "damping", "Damping" });
            ValidateMappedNumber(parameters.Elasticity, new[] { "elasticity", "Elasticity" });
            ValidateMappedNumber(parameters.Inert, new[] { "inert", "Inert" });
            ValidateMappedNumber(parameters.Friction, new[] { "friction", "Friction" });
            ValidateMappedNumber(parameters.StretchMotion, new[] { "stretchMotion", "StretchMotion" });
            ValidateMappedNumber(parameters.Squish, new[] { "squish", "Squish", "maxSquish", "MaxSquish" });
            if (Find(new[] { "gravityDir", "GravityDir", "gravityDirection", "GravityDirection" }) == null && Mathf.Abs(parameters.Gravity) > 0.000001f)
                throw new PhysBonesBridgeException("SDK_MEMBER_MISSING", "The installed PhysBones SDK has no gravity direction member.");
        }
        void ValidateMappedNumber(float value, string[] names)
        {
            if (HasAny(names)) return;
            if (Mathf.Abs(value) > 0.000001f)
                throw new PhysBonesBridgeException("SDK_MEMBER_MISSING", "PhysBones member is unavailable for a nonzero value: " + names[0] + ".");
        }
        void SetAdvancedBool(Component component, string[] names, bool value)
        {
            var member = Find(names);
            if (member == null) throw new PhysBonesBridgeException("SDK_MEMBER_MISSING", "PhysBones component is missing " + names[0] + ".");
            if (member.ValueType == typeof(bool)) { member.Set(component, value); return; }
            if (!member.ValueType.IsEnum)
                throw new PhysBonesBridgeException("SDK_MEMBER_TYPE", "PhysBones member has an incompatible type: " + names[0] + ".");
            try
            {
                // VRC SDK 3.7 uses AdvancedBool { False, True, Other }.
                // Map authored booleans to explicit values and never select Other.
                member.Set(component, Enum.Parse(member.ValueType, value ? "True" : "False", true));
            }
            catch (Exception error)
            {
                throw new PhysBonesBridgeException("SDK_ENUM_MISMATCH", "PhysBones boolean enum does not contain the required value for " + names[0] + " (" + error.Message + ").");
            }
        }
        void ValidateAdvancedBool(string[] names)
        {
            var member = Find(names);
            if (member == null) throw new PhysBonesBridgeException("SDK_MEMBER_MISSING", "PhysBones component is missing " + names[0] + ".");
            if (member.ValueType == typeof(bool)) return;
            if (!member.ValueType.IsEnum)
                throw new PhysBonesBridgeException("SDK_MEMBER_TYPE", "PhysBones member has an incompatible type: " + names[0] + ".");
            try
            {
                Enum.Parse(member.ValueType, "False", true);
                Enum.Parse(member.ValueType, "True", true);
            }
            catch (Exception error)
            {
                throw new PhysBonesBridgeException("SDK_ENUM_MISMATCH", "PhysBones boolean enum does not contain False/True for " + names[0] + " (" + error.Message + ").");
            }
        }
        void ValidateBool(string[] names)
        {
            var member = Find(names);
            if (member == null) throw new PhysBonesBridgeException("SDK_MEMBER_MISSING", "PhysBones component is missing " + names[0] + ".");
            if (member.ValueType != typeof(bool))
                throw new PhysBonesBridgeException("SDK_MEMBER_TYPE", "PhysBones member has an incompatible type: " + names[0] + ".");
        }
        void SetBool(Component component, string[] names, bool value) { SetTyped(component, names, value, "bool"); }
        void SetString(Component component, string[] names, string value) { SetTyped(component, names, value, "string"); }
        void SetOptionalBool(Component component, string[] names, bool value) { SetOptional(component, names, value); }
        void SetOptionalString(Component component, string[] names, string value) { SetOptional(component, names, value); }
        void SetOptionalEnum(Component component, string[] names, string value)
        {
            var member = Find(names); if (member == null) return;
            if (!member.ValueType.IsEnum) throw new PhysBonesBridgeException("SDK_MEMBER_TYPE", "PhysBones member is not an enum: " + names[0] + ".");
            try { member.Set(component, Enum.Parse(member.ValueType, value, true)); }
            catch { throw new PhysBonesBridgeException("SDK_ENUM_MISMATCH", "PhysBones enum does not contain " + value + "."); }
        }
        void SetOptional(Component component, string[] names, object value) { var member = Find(names); if (member != null) Assign(member, component, value, names[0]); }
        void SetRequired(Component component, string[] names, object value) { var member = Find(names); if (member == null) throw new PhysBonesBridgeException("SDK_MEMBER_MISSING", "PhysBones component is missing " + names[0] + "."); Assign(member, component, value, names[0]); }
        void SetTyped(Component component, string[] names, object value, string kind) { SetRequired(component, names, value); }

        static void Assign(VrcPhysBonesReflectionMember member, Component component, object value, string name)
        {
            try
            {
                if (value is float && member.ValueType == typeof(double)) value = (double)(float)value;
                else if (value is float && member.ValueType == typeof(int)) value = Mathf.RoundToInt((float)value);
                else if (value is float && member.ValueType == typeof(float)) { }
                else if (value is bool && member.ValueType != typeof(bool)) throw new InvalidCastException();
                else if (value is string && member.ValueType != typeof(string)) throw new InvalidCastException();
                else if (value is Vector3 && member.ValueType != typeof(Vector3)) throw new InvalidCastException();
                else if (value != null && !member.ValueType.IsInstanceOfType(value)) throw new InvalidCastException();
                member.Set(component, value);
            }
            catch (Exception error) { throw new PhysBonesBridgeException("SDK_MEMBER_TYPE", "PhysBones member has an incompatible type: " + name + " (" + error.Message + ")."); }
        }

        VrcPhysBonesReflectionMember Find(IEnumerable<string> names)
        {
            foreach (var name in names)
            {
                VrcPhysBonesReflectionMember value;
                if (members.TryGetValue(name, out value)) return value;
                if (members.TryGetValue("m_" + name, out value)) return value;
            }
            return null;
        }

        static object CopyValue(object value)
        {
            var array = value as Array;
            if (array != null)
            {
                var copy = Array.CreateInstance(array.GetType().GetElementType(), array.Length);
                Array.Copy(array, copy, array.Length); return copy;
            }
            var list = value as IList;
            if (list != null)
            {
                var copy = Activator.CreateInstance(value.GetType()) as IList;
                if (copy != null) { foreach (var item in list) copy.Add(item); return copy; }
            }
            return value;
        }
        static Vector3 ToVector(NyaForge.Authoring.Vec3 value) { return new Vector3(value.X, value.Y, value.Z); }
        static string Upper(string value) { return char.ToUpperInvariant(value[0]) + value.Substring(1); }
        static string MultiChildName(PhysBonesMultiChildType value) { return value == PhysBonesMultiChildType.All ? "All" : value == PhysBonesMultiChildType.First ? "First" : "Ignore"; }
        static string LimitName(PhysBonesLimitType value) { return value == PhysBonesLimitType.Angle ? "Angle" : value == PhysBonesLimitType.Hinge ? "Hinge" : value == PhysBonesLimitType.Polar ? "Polar" : "None"; }
        static IEnumerable<string> CurveMemberNames() { return Enum.GetValues(typeof(PhysBonesCurveChannel)).Cast<PhysBonesCurveChannel>().Select(CurveName); }
        static string CurveName(PhysBonesCurveChannel value) { return value.ToString().Substring(0, 1).ToLowerInvariant() + value.ToString().Substring(1) + "Curve"; }
    }
}
