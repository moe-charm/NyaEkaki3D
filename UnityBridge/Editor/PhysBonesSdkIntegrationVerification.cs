using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NyaForge.Authoring;
using NyaForge.Authoring.Rig;
using NyaForge.Authoring.Simulation;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NyaForge.UnityBridge.Editor
{
    /// <summary>
    /// Explicit batch probe for a real VRChat PhysBones SDK installation.
    /// This is never run by a normal editor load; invoke Run from Unity batchmode.
    /// </summary>
    public static class PhysBonesSdkIntegrationVerification
    {
        public static void Run()
        {
            string reportPath = Argument("--nyaforge-report");
            var checks = new List<string>();
            GameObject avatar = null;
            int exitCode = 1;
            try
            {
                var resolution = VrcPhysBonesReflectionResolver.Resolve();
                Require(resolution.IsResolved, "VRCPhysBone type did not resolve: " + resolution.Diagnostic);
                Require(typeof(Component).IsAssignableFrom(resolution.ComponentType), "Resolved PhysBones type is not a Unity Component.");
                Require(resolution.Members.Contains("rootTransform"), "Resolved PhysBones type has no rootTransform member.");
                checks.Add("resolved VRCPhysBone runtime type: " + resolution.AssemblyQualifiedTypeName);

                VrcPhysBonesReflectionBackend backend;
                string diagnostic;
                Require(VrcPhysBonesReflectionBackend.TryCreate(out backend, "probe", resolution.AssemblyQualifiedTypeName, out diagnostic),
                    "reflection backend resolution failed: " + diagnostic);
                checks.Add("capabilities: " + string.Join(",", backend.Capabilities.SupportedFeatures));

                avatar = new GameObject("NyaForge PhysBones SDK probe avatar");
                var child = new GameObject("tail");
                child.transform.SetParent(avatar.transform, false);
                child.transform.localPosition = Vector3.up;
                string rootId = Guid.NewGuid().ToString("D");
                string childId = Guid.NewGuid().ToString("D");
                var skeleton = new SkeletonDefinition(new[]
                {
                    new BoneDefinition(rootId, "root", "", new Vec3(0, 0, 0), new Vec3(0, 1, 0)),
                    new BoneDefinition(childId, "tail", rootId, new Vec3(0, 1, 0), new Vec3(0, 2, 0))
                });
                var chain = new PhysBonesChain("tail", rootId, new[] { rootId, childId },
                    PhysBonesEndpointMode.Auto, "", null, PhysBonesMultiChildType.Ignore, null, null, null,
                    PhysBonesParameters.Default, PhysBonesInteraction.Default, null);
                var profile = new PhysBonesTargetProfile(VrcPhysBonesReflectionBackend.Target, "probe", "probe",
                    skeleton.ContentHash, "", new[] { chain });
                var context = new PhysBonesBridgeContext(avatar.transform,
                    new Dictionary<string, Transform> { [rootId] = avatar.transform, [childId] = child.transform });

                var inspection = PhysBonesBridge.Inspect(profile, skeleton, context, backend);
                Require(inspection.CreatedCount == 1 && !avatar.GetComponentsInChildren<Component>(true)
                    .Any(component => component.GetType() == resolution.ComponentType),
                    "preflight changed the scene or did not plan exactly one component");
                checks.Add("preflight is non-mutating");

                var applied = PhysBonesBridge.Apply(profile, skeleton, context, backend);
                var component = applied.Components.Single();
                Require(component.GetType() == resolution.ComponentType, "applied component type differs from resolved SDK type");
                var rootField = resolution.ComponentType.GetField("rootTransform",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                Require(rootField != null && (Transform)rootField.GetValue(component) == avatar.transform,
                    "rootTransform was not configured on the real SDK component");
                Require(avatar.GetComponentsInChildren<NyaForgePhysBonesManaged>(true).Length == 1,
                    "managed ownership marker was not created");
                checks.Add("real SDK component created and configured with stable root/bone mapping");
                exitCode = 0;
            }
            catch (Exception error)
            {
                Debug.LogException(error);
            }
            finally
            {
                if (avatar != null) Object.DestroyImmediate(avatar);
                string componentType = "";
                try
                {
                    var resolution = VrcPhysBonesReflectionResolver.Resolve();
                    if (resolution.IsResolved) componentType = resolution.AssemblyQualifiedTypeName;
                }
                catch { }
                string payload = "{\"schemaVersion\":1,\"status\":\"" + (exitCode == 0 ? "passed" : "failed") +
                    "\",\"componentType\":" + Json(componentType) + ",\"checks\":[" +
                    string.Join(",", checks.Select(Json)) + "]}";
                try { File.WriteAllText(reportPath, payload); }
                catch (Exception error) { Debug.LogException(error); exitCode = 1; }
                EditorApplication.Exit(exitCode);
            }
        }

        static string Argument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == name) return args[i + 1];
            throw new ArgumentException("missing " + name);
        }

        static string Json(string value)
        {
            return "\"" + (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
        }

        static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }
    }
}
