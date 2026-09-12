using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NyaForge.Authoring.Simulation;
using UnityEditor;
using UnityEngine;

namespace NyaForge.UnityBridge.Editor
{
    /// <summary>
    /// Reflection adapter for the optional VRChat SDK. The public package remains buildable without the SDK assembly.
    /// </summary>
    public sealed class VrcPhysBonesReflectionBackend : IPhysBonesComponentBackend
    {
        public const string Target = "vrchat.physbones";
        public const string DefaultComponentType = "VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone";

        sealed class Member
        {
            internal readonly FieldInfo Field;
            internal readonly PropertyInfo Property;
            internal Type ValueType { get { return Field != null ? Field.FieldType : Property.PropertyType; } }
            internal Member(FieldInfo field) { Field = field; }
            internal Member(PropertyInfo property) { Property = property; }
            internal object Get(object owner) { return Field != null ? Field.GetValue(owner) : Property.GetValue(owner, null); }
            internal void Set(object owner, object value)
            {
                if (Field != null) Field.SetValue(owner, value);
                else Property.SetValue(owner, value, null);
            }
        }

        sealed class Snapshot
        {
            internal readonly Dictionary<string, object> Values = new Dictionary<string, object>(StringComparer.Ordinal);
        }

        readonly Type componentType;
        readonly Dictionary<string, Member> members;
        readonly PhysBonesCapabilities capabilities;

        public Type ComponentType { get { return componentType; } }
        public PhysBonesCapabilities Capabilities { get { return capabilities; } }

        VrcPhysBonesReflectionBackend(Type type, string sdkVersion)
        {
            componentType = type;
            members = new Dictionary<string, Member>(StringComparer.Ordinal);
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var field in type.GetFields(flags)) if (!field.IsStatic && !members.ContainsKey(field.Name)) members.Add(field.Name, new Member(field));
            foreach (var property in type.GetProperties(flags)) if (property.CanRead && property.CanWrite && property.GetIndexParameters().Length == 0 && !members.ContainsKey(property.Name)) members.Add(property.Name, new Member(property));
            capabilities = new PhysBonesCapabilities(Target, string.IsNullOrEmpty(sdkVersion) ? "unknown" : sdkVersion, Features());
        }

        public static bool TryCreate(out VrcPhysBonesReflectionBackend backend, string sdkVersion = "unknown",
            string componentTypeName = DefaultComponentType)
        {
            backend = null;
            var type = FindType(componentTypeName);
            if (type == null || !typeof(Component).IsAssignableFrom(type)) return false;
            backend = new VrcPhysBonesReflectionBackend(type, sdkVersion);
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

        public void Restore(Component component, object value)
        {
            var snapshot = value as Snapshot;
            if (snapshot == null) throw new ArgumentException("Unexpected PhysBones snapshot.");
            foreach (var pair in snapshot.Values)
            {
                Member member;
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
            SetNumber(component, new[] { "maxAngle", "MaxAngle" }, p.MaxAngle);
            SetNumber(component, new[] { "radius", "Radius" }, p.Radius);
            SetNumber(component, new[] { "stiffness", "Stiffness" }, p.Stiffness);
            SetNumber(component, new[] { "pull", "Pull" }, p.Pull);
            SetNumber(component, new[] { "spring", "Spring" }, p.Spring);
            SetNumber(component, new[] { "immobile", "Immobile" }, p.Immobile);
            SetNumber(component, new[] { "gravity", "Gravity" }, p.Gravity);
            SetNumber(component, new[] { "gravityFalloff", "GravityFalloff" }, p.GravityFalloff);
            SetNumber(component, new[] { "damping", "Damping" }, p.Damping);
            SetNumber(component, new[] { "elasticity", "Elasticity" }, p.Elasticity);
            SetNumber(component, new[] { "inert", "Inert" }, p.Inert);
            SetNumber(component, new[] { "friction", "Friction" }, p.Friction);
            SetNumber(component, new[] { "stretchMotion", "StretchMotion" }, p.StretchMotion);
            SetNumber(component, new[] { "squish", "Squish" }, p.Squish);
            SetVector(component, new[] { "gravityDir", "GravityDir", "gravityDirection", "GravityDirection" }, ToVector(p.GravityDirection));

            var interaction = chain.Interaction;
            SetBool(component, new[] { "allowPosing", "AllowPosing" }, interaction.AllowPosing);
            SetBool(component, new[] { "allowCollision", "AllowCollision" }, interaction.AllowCollision);
            SetBool(component, new[] { "allowGrabbing", "AllowGrabbing" }, interaction.AllowGrabbing);
            SetBool(component, new[] { "snapToHand", "SnapToHand" }, interaction.SnapToHand);
            SetOptionalBool(component, new[] { "resetWhenDisabled", "ResetWhenDisabled" }, interaction.ResetWhenDisabled);
            SetOptionalBool(component, new[] { "isAnimated", "IsAnimated" }, interaction.IsAnimated);
            if (interaction.Parameter == "") SetOptionalString(component, new[] { "parameter", "Parameter" }, interaction.Parameter);
            else SetString(component, new[] { "parameter", "Parameter" }, interaction.Parameter);
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
            if (HasAll(new[] { "limitType", "maxAngle", "radius", "stiffness", "pull", "spring", "immobile", "gravity", "gravityFalloff", "damping", "elasticity", "inert", "friction", "stretchMotion", "squish" }) &&
                (Find(new[] { "gravityDir", "GravityDir", "gravityDirection", "GravityDirection" }) != null)) result.Add(PhysBonesFeatures.Limits);
            if (HasAll(CurveMemberNames())) result.Add(PhysBonesFeatures.Curves);
            if (HasAll(new[] { "allowPosing", "allowCollision", "allowGrabbing", "snapToHand" })) result.Add(PhysBonesFeatures.Interaction);
            if (Find(new[] { "parameter", "Parameter" }) != null) result.Add(PhysBonesFeatures.Parameter);
            return result;
        }

        bool HasAll(IEnumerable<string> names) { return names.All(name => Find(new[] { name, Upper(name) }) != null); }

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

        static void SetCollection(Member member, Component component, UnityEngine.Object[] values, Type expectedBase, string name, string mismatchCode)
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

        static void Assign(Member member, Component component, object value, string name)
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

        Member Find(IEnumerable<string> names)
        {
            foreach (var name in names)
            {
                Member value;
                if (members.TryGetValue(name, out value)) return value;
                if (members.TryGetValue("m_" + name, out value)) return value;
            }
            return null;
        }

        static Type FindType(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { var type = assembly.GetType(name, false); if (type != null) return type; }
                catch (ReflectionTypeLoadException) { }
            }
            return Type.GetType(name, false);
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
