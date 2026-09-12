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
                var inspection = PhysBonesBridge.Inspect(profile, skeleton, context, backend);
                Require(inspection.ChainCount == 1 && inspection.CreatedCount == 1 && inspection.UpdatedCount == 0
                    && avatar.GetComponentsInChildren<VerificationComponent>(true).Length == 0
                    && avatar.GetComponentsInChildren<NyaForgePhysBonesManaged>(true).Length == 0,
                    "PhysBones Bridge inspection mutated the scene or reported incorrect create/update counts.");
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
                string fixtureType = typeof(PhysBonesReflectionFixtureComponent).AssemblyQualifiedName;
                var resolution = VrcPhysBonesReflectionResolver.Resolve(fixtureType);
                Require(resolution.IsResolved && resolution.AssemblyQualifiedTypeName == fixtureType && resolution.Members.Contains("stiffness"),
                    "Reflection resolver did not retain the exact fixture type and member catalog.");
                var missing = VrcPhysBonesReflectionResolver.Resolve("NyaForge.MissingPhysBonesComponent, MissingPhysBonesAssembly");
                Require(!missing.IsResolved && missing.Diagnostic.Contains("No loaded assembly"), "Reflection resolver did not diagnose an unavailable SDK type.");
                var nonComponent = VrcPhysBonesReflectionResolver.Resolve(typeof(string).AssemblyQualifiedName);
                Require(!nonComponent.IsResolved && nonComponent.Diagnostic.Contains("not a Unity Component"), "Reflection resolver accepted a non-Component type.");
                Require(VrcPhysBonesReflectionBackend.TryCreate(out backend, "verification-sdk", fixtureType), "Reflection PhysBones backend did not find the shape-compatible fixture type.");
                string packageDirectory = Path.Combine("Temp", "NyaForgePhysBonesBridgePackage-" + Guid.NewGuid().ToString("N"));
                string manifest = PhysBonesTargetPackage.Export(packageDirectory, profile, skeleton, fixtureType);
                var package = PhysBonesTargetPackage.Read(manifest);
                Require(package.ComponentTypeName == fixtureType, "PhysBones target package did not retain the explicit component type name.");
                var packageInspection = PhysBonesBridge.InspectPackage(manifest, context, backend);
                Require(packageInspection.ChainCount == 1 && packageInspection.CreatedCount == 1
                    && avatar.GetComponentsInChildren<PhysBonesReflectionFixtureComponent>(true).Length == 0,
                    "PhysBones package inspection did not remain non-mutating.");
                var result = PhysBonesBridge.ApplyPackage(manifest, context, backend);
                var component = (PhysBonesReflectionFixtureComponent)result.Components.Single();
                Require(component.rootTransform == avatar.transform && Mathf.Abs(component.stiffness - .5f) < .0001f && component.allowCollision, "Reflection PhysBones backend did not map the target fields.");
                Require(backend.Capabilities.Supports(PhysBonesFeatures.Limits) && backend.Capabilities.Supports(PhysBonesFeatures.Interaction), "Reflection PhysBones capability scan missed required fields.");
                Require(result.ProfileHash == profile.ContentHash, "PhysBones package apply changed the target identity.");
                if (Directory.Exists(packageDirectory)) Directory.Delete(packageDirectory, true);
                VrcPhysBonesReflectionBackend inheritedBackend;
                Require(VrcPhysBonesReflectionBackend.TryCreate(out inheritedBackend, "verification-sdk", typeof(PhysBonesReflectionInheritedFixtureComponent).AssemblyQualifiedName), "Reflection backend did not find inherited SDK members.");
                var inheritedAvatar = new GameObject("NyaForge PhysBones inherited reflection fixture");
                try
                {
                    var inheritedChild = new GameObject("tail"); inheritedChild.transform.SetParent(inheritedAvatar.transform, false); inheritedChild.transform.localPosition = Vector3.up;
                    var inheritedContext = new PhysBonesBridgeContext(inheritedAvatar.transform, new Dictionary<string, Transform> { [rootId] = inheritedAvatar.transform, [childId] = inheritedChild.transform });
                    var inheritedResult = PhysBonesBridge.Apply(profile, skeleton, inheritedContext, inheritedBackend);
                    var inherited = (PhysBonesReflectionInheritedFixtureComponent)inheritedResult.Components.Single();
                    Require(inherited.RootTransform == inheritedAvatar.transform && Mathf.Abs(inherited.Stiffness - .5f) < .0001f, "Reflection backend did not map private inherited SDK members.");
                }
                finally { Object.DestroyImmediate(inheritedAvatar); }
                checks.Add("PhysBones reflection backend: target package read, optional SDK type discovery, field mapping and inherited private-member compatibility pass");
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

        static void VerifyPhysBonesBinding(List<string> checks)
        {
            var avatar = new GameObject("NyaForge PhysBones binding fixture");
            try
            {
                var child = new GameObject("tail");
                child.transform.SetParent(avatar.transform, false);
                var collider = avatar.AddComponent<BoxCollider>();
                string rootId = Guid.NewGuid().ToString("D"), childId = Guid.NewGuid().ToString("D");
                var validMapping = PhysBonesBindingValidator.Validate(avatar.transform,
                    new[]
                    {
                        new KeyValuePair<string, Transform>(childId, child.transform),
                        new KeyValuePair<string, Transform>(rootId, avatar.transform)
                    },
                    new[] { new KeyValuePair<int, IEnumerable<Component>>(3, new Component[] { collider }) },
                    new[] { rootId, childId }, new[] { 3 });
                Require(validMapping.IsValid, "PhysBones binding validator rejected a valid descendant mapping.");
                var outside = new GameObject("NyaForge PhysBones binding outside fixture");
                try
                {
                    var invalidMapping = PhysBonesBindingValidator.Validate(avatar.transform,
                        new[]
                        {
                            new KeyValuePair<string, Transform>(rootId, avatar.transform),
                            new KeyValuePair<string, Transform>(childId, outside.transform)
                        },
                        Array.Empty<KeyValuePair<int, IEnumerable<Component>>>(),
                        new[] { rootId, childId }, new[] { 3 });
                    Require(!invalidMapping.IsValid && invalidMapping.Errors.Any(error => error.Contains("outside the avatar root"))
                        && invalidMapping.Errors.Any(error => error.Contains("collider group is missing")),
                        "PhysBones binding validator did not reject an outside transform and missing collider group.");
                }
                finally { Object.DestroyImmediate(outside); }
                var binding = (NyaForgePhysBonesBinding)Undo.AddComponent(avatar, typeof(NyaForgePhysBonesBinding));
                binding.Capture("exports/physbones/test.json", "manifest-hash", "vrchat.physbones", "verification-sdk",
                    "profile-hash", "skeleton-hash",
                    new[]
                    {
                        new KeyValuePair<string, Transform>(childId, child.transform),
                        new KeyValuePair<string, Transform>(rootId, avatar.transform)
                    },
                    new[]
                    {
                        new KeyValuePair<int, IEnumerable<Component>>(3, new Component[] { collider })
                    });
                Require(binding.Matches("manifest-hash", "vrchat.physbones", "verification-sdk", "profile-hash", "skeleton-hash"),
                    "PhysBones binding did not retain the package identity.");
                var expectedIds = new[] { childId, rootId }.OrderBy(id => id, StringComparer.Ordinal).ToArray();
                Require(binding.Bones.Count == 2 && binding.Bones[0].BoneId == expectedIds[0] && binding.Bones[1].BoneId == expectedIds[1],
                    "PhysBones binding did not use deterministic stable-ID ordering.");
                Transform resolved;
                Require(binding.TryGetBone(rootId, out resolved) && resolved == avatar.transform,
                    "PhysBones binding did not resolve an explicit stable BoneId.");
                Require(binding.ColliderGroups.Count == 1 && binding.ColliderGroups[0].GroupIndex == 3
                    && binding.ColliderGroups[0].Colliders.Count == 1 && binding.ColliderGroups[0].Colliders[0] == collider,
                    "PhysBones binding did not retain the explicit collider group.");
                Require(!binding.Matches("changed-manifest", "vrchat.physbones", "verification-sdk", "profile-hash", "skeleton-hash"),
                    "PhysBones binding accepted a stale package identity.");
                checks.Add("PhysBones binding: explicit stable bones and collider groups persist with package identity and reject stale manifests");
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
