using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Rig;
using NyaForge.Authoring.Simulation;
using NyaForge.UnityBridge;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NyaForge.UnityBridge.Editor
{
    public static partial class BridgeBatch
    {
        sealed class VerificationComponent : MonoBehaviour
        {
            public Transform rootTransform;
            public float stiffness;
            public int configured;
        }

        sealed class VerificationBackend : IPhysBonesComponentBackend
        {
            sealed class State { internal Transform root; internal float stiffness; internal int configured; }
            readonly PhysBonesCapabilities capabilities;
            readonly bool supportLimits;
            readonly bool failConfigure;
            internal VerificationBackend(bool supportLimits, bool failConfigure = false)
            {
                this.supportLimits = supportLimits;
                this.failConfigure = failConfigure;
                var features = new List<string> { PhysBonesFeatures.Root, PhysBonesFeatures.Interaction };
                if (supportLimits) features.Add(PhysBonesFeatures.Limits);
                capabilities = new PhysBonesCapabilities(VrcPhysBonesReflectionBackend.Target, "verification-sdk", features);
            }
            public Type ComponentType { get { return typeof(VerificationComponent); } }
            public PhysBonesCapabilities Capabilities { get { return capabilities; } }
            public Component Create(Transform owner) { return Undo.AddComponent(owner.gameObject, typeof(VerificationComponent)); }
            public object Capture(Component component)
            {
                var value = (VerificationComponent)component;
                return new State { root = value.rootTransform, stiffness = value.stiffness, configured = value.configured };
            }
            public void Configure(Component component, PhysBonesChain chain, PhysBonesBridgeContext context)
            {
                var value = (VerificationComponent)component;
                value.rootTransform = context.Bone(chain.RootBoneId);
                value.stiffness = chain.Parameters.Stiffness;
                value.configured++;
                if (failConfigure) throw new PhysBonesBridgeException("CONFIGURE_FAILED", "Synthetic backend failure after mutation.");
            }
            public void Restore(Component component, object snapshot)
            {
                var value = (VerificationComponent)component; var state = (State)snapshot;
                value.rootTransform = state.root; value.stiffness = state.stiffness; value.configured = state.configured;
            }
        }

        static void VerifyPhysBonesBridge(List<string> checks)
        {
            var avatar = new GameObject("NyaForge PhysBones bridge fixture");
            try
            {
                var child = new GameObject("tail"); child.transform.SetParent(avatar.transform, false); child.transform.localPosition = Vector3.up;
                string rootId = Guid.NewGuid().ToString("D"), childId = Guid.NewGuid().ToString("D");
                var skeleton = new SkeletonDefinition(new[]
                {
                    new BoneDefinition(rootId, "root", "", new Vec3(0, 0, 0), new Vec3(0, 1, 0)),
                    new BoneDefinition(childId, "tail", rootId, new Vec3(0, 1, 0), new Vec3(0, 2, 0))
                });
                var chain = Chain(rootId, childId, PhysBonesParameters.Default);
                var profile = new PhysBonesTargetProfile(VrcPhysBonesReflectionBackend.Target, "verification-sdk", "", skeleton.ContentHash, "", new[] { chain });
                var bones = new Dictionary<string, Transform> { [rootId] = avatar.transform, [childId] = child.transform };
                var context = new PhysBonesBridgeContext(avatar.transform, bones);
                var backend = new VerificationBackend(true);
                var first = PhysBonesBridge.Apply(profile, skeleton, context, backend);
                Require(first.CreatedCount == 1 && first.UpdatedCount == 0 && first.Components.Count == 1, "PhysBones Bridge did not create one managed component.");
                var managed = avatar.GetComponentsInChildren<NyaForgePhysBonesManaged>(true).Single();
                var component = (VerificationComponent)managed.ManagedComponent;
                Require(component.rootTransform == avatar.transform && component.configured == 1, "PhysBones Bridge did not configure the created component.");
                var unmanaged = (VerificationComponent)Undo.AddComponent(avatar, typeof(VerificationComponent)); unmanaged.configured = 17;
                var changed = new PhysBonesTargetProfile(VrcPhysBonesReflectionBackend.Target, "verification-sdk", "", skeleton.ContentHash, "", new[] { Chain(rootId, childId, new PhysBonesParameters(PhysBonesLimitType.None, 0, 0, .8f, .5f, .5f, 0, 0, .5f, .5f, 0, 0, 0, 0, 0, new Vec3(0, -1, 0))) });
                var update = PhysBonesBridge.Apply(changed, skeleton, context, backend, PhysBonesApplyMode.UpdateManagedOnly);
                Require(update.CreatedCount == 0 && update.UpdatedCount == 1 && managed.ProfileHash == changed.ContentHash, "Managed-only PhysBones update did not update the owned component.");
                Require(unmanaged.configured == 17, "Managed-only PhysBones update changed an unowned component.");
                bool blocked = false;
                try { PhysBonesBridge.Apply(profile, skeleton, context, new VerificationBackend(false)); }
                catch (PhysBonesBridgeException error) { blocked = error.Code == "UNSUPPORTED_FEATURE" && error.LossReport != null && error.LossReport.Unsupported.Count > 0; }
                Require(blocked && avatar.GetComponentsInChildren<VerificationComponent>(true).Length == 2, "Unsupported PhysBones data was mutated before the loss report stopped the write.");
                int configuredBeforeFailure = component.configured;
                float stiffnessBeforeFailure = component.stiffness;
                bool failed = false;
                try { PhysBonesBridge.Apply(profile, skeleton, context, new VerificationBackend(true, true)); }
                catch (PhysBonesBridgeException error) { failed = error.Code == "CONFIGURE_FAILED"; }
                Require(failed && component.configured == configuredBeforeFailure && Mathf.Abs(component.stiffness - stiffnessBeforeFailure) < .0001f,
                    "A configure failure did not roll back the managed component.");
                Require(avatar.GetComponentsInChildren<NyaForgePhysBonesManaged>(true).Length == 1 && avatar.GetComponentsInChildren<VerificationComponent>(true).Length == 2,
                    "A configure failure left an extra managed component or marker.");
                checks.Add("PhysBones Bridge: explicit stable bone bindings create/update only marked components, preserve unowned components, and stop on unsupported capability before mutation");
            }
            finally { Object.DestroyImmediate(avatar); }
            VerifyPhysBonesReflectionBackend(checks);
        }

        static void VerifyPhysBonesReflectionBackend(List<string> checks)
        {
            var avatar = new GameObject("NyaForge PhysBones reflection fixture");
            try
            {
                var child = new GameObject("tail"); child.transform.SetParent(avatar.transform, false); child.transform.localPosition = Vector3.up;
                string rootId = Guid.NewGuid().ToString("D"), childId = Guid.NewGuid().ToString("D");
                var skeleton = new SkeletonDefinition(new[]
                {
                    new BoneDefinition(rootId, "root", "", new Vec3(0, 0, 0), new Vec3(0, 1, 0)),
                    new BoneDefinition(childId, "tail", rootId, new Vec3(0, 1, 0), new Vec3(0, 2, 0))
                });
                var profile = new PhysBonesTargetProfile(VrcPhysBonesReflectionBackend.Target, "verification-sdk", "", skeleton.ContentHash, "", new[] { Chain(rootId, childId, PhysBonesParameters.Default) });
                var context = new PhysBonesBridgeContext(avatar.transform, new Dictionary<string, Transform> { [rootId] = avatar.transform, [childId] = child.transform });
                VrcPhysBonesReflectionBackend backend;
                Require(VrcPhysBonesReflectionBackend.TryCreate(out backend, "verification-sdk", typeof(PhysBonesReflectionFixtureComponent).AssemblyQualifiedName), "Reflection PhysBones backend did not find the shape-compatible fixture type.");
                string packageDirectory = Path.Combine("Temp", "NyaForgePhysBonesBridgePackage-" + Guid.NewGuid().ToString("N"));
                string manifest = PhysBonesTargetPackage.Export(packageDirectory, profile, skeleton);
                var result = PhysBonesBridge.ApplyPackage(manifest, context, backend);
                var component = (PhysBonesReflectionFixtureComponent)result.Components.Single();
                Require(component.rootTransform == avatar.transform && Mathf.Abs(component.stiffness - .5f) < .0001f && component.allowCollision, "Reflection PhysBones backend did not map the target fields.");
                Require(backend.Capabilities.Supports(PhysBonesFeatures.Limits) && backend.Capabilities.Supports(PhysBonesFeatures.Interaction), "Reflection PhysBones capability scan missed required fields.");
                Require(result.ProfileHash == profile.ContentHash, "PhysBones package apply changed the target identity.");
                if (Directory.Exists(packageDirectory)) Directory.Delete(packageDirectory, true);
                checks.Add("PhysBones reflection backend: target package read, optional SDK type discovery and field mapping pass against a shape-compatible fixture");
            }
            finally { Object.DestroyImmediate(avatar); }
            VerifyPhysBonesBranchPreflight(checks);
        }

        static void VerifyPhysBonesBranchPreflight(List<string> checks)
        {
            var avatar = new GameObject("NyaForge PhysBones branch fixture");
            try
            {
                var first = new GameObject("tail-a"); first.transform.SetParent(avatar.transform, false);
                var second = new GameObject("tail-b"); second.transform.SetParent(avatar.transform, false);
                string rootId = Guid.NewGuid().ToString("D"), firstId = Guid.NewGuid().ToString("D"), secondId = Guid.NewGuid().ToString("D");
                var skeleton = new SkeletonDefinition(new[]
                {
                    new BoneDefinition(rootId, "root", "", new Vec3(0, 0, 0), new Vec3(0, 1, 0)),
                    new BoneDefinition(firstId, "tail-a", rootId, new Vec3(0, 1, 0), new Vec3(0, 2, 0)),
                    new BoneDefinition(secondId, "tail-b", rootId, new Vec3(0, 1, 0), new Vec3(0, 2, 0))
                });
                var branch = new PhysBonesBranch(rootId, new[] { firstId });
                var chain = new PhysBonesChain("branch", rootId, new[] { rootId }, PhysBonesEndpointMode.Auto, "", null,
                    PhysBonesMultiChildType.All, null, new[] { branch }, null, PhysBonesParameters.Default, PhysBonesInteraction.Default, null);
                var profile = new PhysBonesTargetProfile(VrcPhysBonesReflectionBackend.Target, "verification-sdk", "", skeleton.ContentHash, "", new[] { chain });
                var context = new PhysBonesBridgeContext(avatar.transform, new Dictionary<string, Transform>
                {
                    [rootId] = avatar.transform, [firstId] = first.transform, [secondId] = second.transform
                });
                VrcPhysBonesReflectionBackend backend;
                Require(VrcPhysBonesReflectionBackend.TryCreate(out backend, "verification-sdk", typeof(PhysBonesReflectionFixtureComponent).AssemblyQualifiedName), "Reflection branch backend was not found.");
                bool rejected = false;
                try { PhysBonesBridge.Apply(profile, skeleton, context, backend); }
                catch (PhysBonesBridgeException error) { rejected = error.Code == "UNSUPPORTED_BRANCH_MAPPING"; }
                Require(rejected && avatar.GetComponentsInChildren<PhysBonesReflectionFixtureComponent>(true).Length == 0,
                    "Unrepresentable explicit branch mapping was not rejected before mutation.");
                checks.Add("PhysBones reflection backend: explicit branch lists that cannot be represented by SDK multi-child mode are rejected before mutation");
            }
            finally { Object.DestroyImmediate(avatar); }
        }

        static PhysBonesChain Chain(string root, string child, PhysBonesParameters parameters)
        {
            return new PhysBonesChain("tail", root, new[] { root, child }, PhysBonesEndpointMode.Auto, "", null,
                PhysBonesMultiChildType.Ignore, null, null, null, parameters, PhysBonesInteraction.Default, null);
        }

    }
}
