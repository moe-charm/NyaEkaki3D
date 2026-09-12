using System;
using System.Collections.Generic;
using System.Reflection;

namespace NyaForge.UnityBridge.Editor
{
    /// <summary>Deterministic field/property discovery for optional PhysBones SDK components.</summary>
    internal sealed class VrcPhysBonesReflectionMember
    {
        internal readonly FieldInfo Field;
        internal readonly PropertyInfo Property;
        internal Type ValueType { get { return Field != null ? Field.FieldType : Property.PropertyType; } }

        internal VrcPhysBonesReflectionMember(FieldInfo field) { Field = field; }
        internal VrcPhysBonesReflectionMember(PropertyInfo property) { Property = property; }

        internal object Get(object owner)
        { return Field != null ? Field.GetValue(owner) : Property.GetValue(owner, null); }

        internal void Set(object owner, object value)
        { if (Field != null) Field.SetValue(owner, value); else Property.SetValue(owner, value, null); }
    }

    internal static class VrcPhysBonesReflectionMemberCatalog
    {
        internal static Dictionary<string, VrcPhysBonesReflectionMember> Discover(Type componentType)
        {
            if (componentType == null) throw new ArgumentNullException("componentType");
            var result = new Dictionary<string, VrcPhysBonesReflectionMember>(StringComparer.Ordinal);
            // Walk from the concrete SDK type towards Component so a derived member
            // wins when a vendor version shadows a base member with the same name.
            for (var type = componentType; type != null && typeof(UnityEngine.Component).IsAssignableFrom(type); type = type.BaseType)
            {
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                foreach (var field in type.GetFields(flags))
                {
                    if (field.IsStatic || field.IsLiteral || field.IsInitOnly || result.ContainsKey(field.Name)) continue;
                    result.Add(field.Name, new VrcPhysBonesReflectionMember(field));
                }
                foreach (var property in type.GetProperties(flags))
                {
                    if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length != 0 || result.ContainsKey(property.Name)) continue;
                    result.Add(property.Name, new VrcPhysBonesReflectionMember(property));
                }
            }
            return result;
        }
    }
}
