using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NyaForge.UnityBridge.Editor
{
    /// <summary>Read-only result of resolving the component type named by a PhysBones target package.</summary>
    public sealed class VrcPhysBonesReflectionResolution
    {
        readonly IReadOnlyList<string> members;

        internal VrcPhysBonesReflectionResolution(string requestedTypeName, Type componentType,
            IEnumerable<string> memberNames, string diagnostic)
        {
            RequestedTypeName = requestedTypeName ?? "";
            ComponentType = componentType;
            members = Array.AsReadOnly((memberNames ?? Array.Empty<string>()).OrderBy(name => name, StringComparer.Ordinal).ToArray());
            Diagnostic = diagnostic ?? "";
        }

        public string RequestedTypeName { get; private set; }
        public Type ComponentType { get; private set; }
        public bool IsResolved { get { return ComponentType != null; } }
        public string AssemblyQualifiedTypeName { get { return ComponentType == null ? "" : ComponentType.AssemblyQualifiedName ?? ""; } }
        public string AssemblyName { get { return ComponentType == null ? "" : ComponentType.Assembly.GetName().Name ?? ""; } }
        public string AssemblyVersion { get { return ComponentType == null ? "" : ComponentType.Assembly.GetName().Version == null ? "" : ComponentType.Assembly.GetName().Version.ToString(); } }
        public IReadOnlyList<string> Members { get { return members; } }
        public string Diagnostic { get; private set; }
    }

    /// <summary>Deterministic optional-SDK type resolver. It performs no Unity scene mutation.</summary>
    public static class VrcPhysBonesReflectionResolver
    {
        public static VrcPhysBonesReflectionResolution Resolve(string componentTypeName = VrcPhysBonesReflectionBackend.DefaultComponentType)
        {
            string requested = string.IsNullOrWhiteSpace(componentTypeName) ? VrcPhysBonesReflectionBackend.DefaultComponentType : componentTypeName.Trim();
            Type type = ResolveType(requested, out string diagnostic);
            if (type == null) return new VrcPhysBonesReflectionResolution(requested, null, null, diagnostic);
            if (!typeof(Component).IsAssignableFrom(type))
                return new VrcPhysBonesReflectionResolution(requested, null, null, "Resolved type is not a Unity Component: " + type.FullName + ".");
            var members = VrcPhysBonesReflectionMemberCatalog.Discover(type).Keys;
            return new VrcPhysBonesReflectionResolution(requested, type, members, "Resolved " + type.AssemblyQualifiedName + ".");
        }

        static Type ResolveType(string name, out string diagnostic)
        {
            try
            {
                var exact = Type.GetType(name, false);
                if (exact != null) { diagnostic = ""; return exact; }
            }
            catch (Exception error)
            {
                diagnostic = "Type.GetType failed: " + error.Message;
                return null;
            }

            var matches = new List<Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(name, false);
                    if (type != null && !matches.Contains(type)) matches.Add(type);
                }
                catch (ReflectionTypeLoadException) { }
            }
            if (matches.Count == 1) { diagnostic = ""; return matches[0]; }
            if (matches.Count == 0)
            {
                diagnostic = "No loaded assembly contains the requested component type: " + name + ".";
                return null;
            }
            diagnostic = "The requested component type is ambiguous across loaded assemblies: " + string.Join(", ", matches.Select(type => type.Assembly.FullName).ToArray()) + ". Use an assembly-qualified name.";
            return null;
        }
    }
}
