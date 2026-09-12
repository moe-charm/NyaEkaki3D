using System;
using System.Collections.Generic;
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
    /// <summary>Controls whether the Bridge may create a missing managed component.</summary>
    public enum PhysBonesApplyMode
    {
        CreateOrUpdateManaged = 0,
        UpdateManagedOnly = 1
    }

    /// <summary>Explicit stable-ID to scene Transform and collider-group bindings.</summary>
    public sealed class PhysBonesBridgeContext
    {
        public Transform AvatarRoot { get; private set; }
        public IReadOnlyDictionary<string, Transform> Bones { get; private set; }
        public IReadOnlyDictionary<int, IReadOnlyList<Component>> ColliderGroups { get; private set; }

        public PhysBonesBridgeContext(Transform avatarRoot, IReadOnlyDictionary<string, Transform> bones,
            IReadOnlyDictionary<int, IReadOnlyList<Component>> colliderGroups = null)
        {
            if (avatarRoot == null) throw new ArgumentNullException("avatarRoot");
            if (bones == null) throw new ArgumentNullException("bones");
            AvatarRoot = avatarRoot;
            var boneCopy = new Dictionary<string, Transform>(StringComparer.Ordinal);
            foreach (var pair in bones) boneCopy.Add(pair.Key, pair.Value);
            Bones = boneCopy;
            var groups = new Dictionary<int, IReadOnlyList<Component>>();
            if (colliderGroups != null)
                foreach (var pair in colliderGroups)
                    groups.Add(pair.Key, (pair.Value ?? Array.Empty<Component>()).ToArray());
            ColliderGroups = groups;
        }

        public Transform Bone(string id)
        {
            Transform value;
            if (!Bones.TryGetValue(id, out value) || value == null)
                throw new PhysBonesBridgeException("BONE_BINDING_MISSING", "Bone binding is missing: " + id);
            return value;
        }

        public IReadOnlyList<Component> Colliders(PhysBonesChain chain)
        {
            if (chain == null) throw new ArgumentNullException("chain");
            var result = new List<Component>();
            foreach (int group in chain.ColliderGroupIndices)
            {
                IReadOnlyList<Component> values;
                if (!ColliderGroups.TryGetValue(group, out values))
                    throw new PhysBonesBridgeException("COLLIDER_BINDING_MISSING", "Collider group binding is missing: " + group);
                result.AddRange(values);
            }
            return result.AsReadOnly();
        }
    }

    /// <summary>Vendor-neutral component operations. Implementations may use reflection or a vendor SDK.</summary>
    public interface IPhysBonesComponentBackend
    {
        Type ComponentType { get; }
        PhysBonesCapabilities Capabilities { get; }
        Component Create(Transform owner);
        object Capture(Component component);
        void Configure(Component component, PhysBonesChain chain, PhysBonesBridgeContext context);
        void Restore(Component component, object snapshot);
    }

    /// <summary>Optional backend-specific checks that must complete before any component is mutated.</summary>
    public interface IPhysBonesComponentPreflight
    {
        void Validate(PhysBonesChain chain, PhysBonesBridgeContext context);
    }

    /// <summary>Failure raised before or during a target write. A loss report is available for capability failures.</summary>
    public sealed class PhysBonesBridgeException : InvalidOperationException
    {
        public string Code { get; private set; }
        public PhysBonesLossReport LossReport { get; private set; }

        internal PhysBonesBridgeException(string code, string message, PhysBonesLossReport report = null)
            : base(message)
        {
            Code = code;
            LossReport = report;
        }
    }

    /// <summary>Result of an atomic target apply. Runtime simulation state is never part of this result.</summary>
    public sealed class PhysBonesBridgeResult
    {
        public string ProfileHash { get; private set; }
        public PhysBonesLossReport LossReport { get; private set; }
        public int CreatedCount { get; private set; }
        public int UpdatedCount { get; private set; }
        public IReadOnlyList<Component> Components { get; private set; }

        internal PhysBonesBridgeResult(string profileHash, PhysBonesLossReport report, int created, int updated,
            IEnumerable<Component> components)
        {
            ProfileHash = profileHash;
            LossReport = report;
            CreatedCount = created;
            UpdatedCount = updated;
            Components = Array.AsReadOnly(components.ToArray());
        }
    }

    /// <summary>
    /// Applies a PhysBones target only after a complete stable-ID preflight. Existing unmarked components are never changed.
    /// </summary>
    public static class PhysBonesBridge
    {
        sealed class Plan
        {
            internal int Index;
            internal PhysBonesChain Chain;
            internal Transform Root;
            internal Component Existing;
            internal NyaForgePhysBonesManaged Marker;
        }

        sealed class Applied
        {
            internal Plan Plan;
            internal Component Component;
            internal object Snapshot;
            internal NyaForgePhysBonesManaged Marker;
            internal bool CreatedComponent;
            internal bool CreatedMarker;
            internal MarkerState MarkerState;
        }

        struct MarkerState
        {
            internal Component Component;
            internal string Target;
            internal int Index;
            internal string Name;
            internal string Root;
            internal string Hash;
        }

        public static PhysBonesBridgeResult Apply(PhysBonesTargetProfile profile, SkeletonDefinition skeleton,
            PhysBonesBridgeContext context, IPhysBonesComponentBackend backend,
            PhysBonesApplyMode mode = PhysBonesApplyMode.CreateOrUpdateManaged,
            SecondaryMotionAsset source = null)
        {
            if (profile == null) throw new ArgumentNullException("profile");
            if (skeleton == null) throw new ArgumentNullException("skeleton");
            if (context == null) throw new ArgumentNullException("context");
            if (backend == null) throw new ArgumentNullException("backend");
            if (backend.ComponentType == null) throw new PhysBonesBridgeException("SDK_UNAVAILABLE", "PhysBones component type is unavailable.");
            try { profile.ValidateFor(source, skeleton); }
            catch (AuthoringException error) { throw new PhysBonesBridgeException(error.Code, error.Message); }

            if (profile.TargetId != backend.Capabilities.TargetId)
                throw new PhysBonesBridgeException("TARGET_MISMATCH", "PhysBones target does not match the installed component backend.");
            var report = PhysBonesLossReport.Compare(profile, backend.Capabilities);
            if (report.Unsupported.Count != 0)
                throw new PhysBonesBridgeException("UNSUPPORTED_FEATURE", "PhysBones target contains features unsupported by this backend.", report);

            var plans = Preflight(profile, skeleton, context, backend, mode);
            var applied = new List<Applied>();
            int created = 0, updated = 0;
            try
            {
                foreach (var plan in plans)
                {
                    var item = new Applied { Plan = plan, Marker = plan.Marker };
                    if (plan.Existing != null)
                    {
                        item.Component = plan.Existing;
                        item.Snapshot = backend.Capture(plan.Existing);
                        item.MarkerState = Capture(plan.Marker);
                        updated++;
                    }
                    else
                    {
                        item.Component = backend.Create(plan.Root);
                        if (item.Component == null || !backend.ComponentType.IsInstanceOfType(item.Component))
                            throw new PhysBonesBridgeException("SDK_CREATE_FAILED", "PhysBones backend did not create the expected component.");
                        item.CreatedComponent = true;
                        created++;
                    }
                    // Register the item before any mutating call so a late configure/marker failure
                    // also restores an existing component or removes a newly created one.
                    applied.Add(item);
                    backend.Configure(item.Component, plan.Chain, context);
                    if (item.Marker == null)
                    {
                        item.Marker = (NyaForgePhysBonesManaged)Undo.AddComponent(plan.Root.gameObject, typeof(NyaForgePhysBonesManaged));
                        if (item.Marker == null) throw new PhysBonesBridgeException("MARKER_CREATE_FAILED", "Could not create the NyaForge ownership marker.");
                        item.CreatedMarker = true;
                    }
                    item.Marker.Bind(item.Component, profile.TargetId, plan.Index, plan.Chain.Name, plan.Chain.RootBoneId, profile.ContentHash);
                    EditorUtility.SetDirty(item.Component);
                    EditorUtility.SetDirty(item.Marker);
                }
            }
            catch
            {
                for (int i = applied.Count - 1; i >= 0; i--) Rollback(applied[i], backend);
                throw;
            }
            return new PhysBonesBridgeResult(profile.ContentHash, report, created, updated,
                applied.Select(item => item.Component));
        }

        /// <summary>Reads a self-contained target package and applies it after the same stable-ID preflight.</summary>
        public static PhysBonesBridgeResult ApplyPackage(string manifestPath, PhysBonesBridgeContext context,
            IPhysBonesComponentBackend backend, PhysBonesApplyMode mode = PhysBonesApplyMode.CreateOrUpdateManaged,
            SecondaryMotionAsset source = null)
        {
            if (string.IsNullOrWhiteSpace(manifestPath)) throw new ArgumentException("PhysBones package manifest is required.", "manifestPath");
            var package = PhysBonesTargetPackage.Read(manifestPath);
            return Apply(package.Target, package.Skeleton, context, backend, mode, source);
        }

        static List<Plan> Preflight(PhysBonesTargetProfile profile, SkeletonDefinition skeleton,
            PhysBonesBridgeContext context, IPhysBonesComponentBackend backend, PhysBonesApplyMode mode)
        {
            var markers = context.AvatarRoot.GetComponentsInChildren<NyaForgePhysBonesManaged>(true);
            var plans = new List<Plan>();
            for (int index = 0; index < profile.Chains.Count; index++)
            {
                var chain = profile.Chains[index];
                var root = context.Bone(chain.RootBoneId);
                RequireDescendant(context.AvatarRoot, root);
                for (int i = 1; i < chain.BoneIds.Count; i++)
                {
                    var previous = context.Bone(chain.BoneIds[i - 1]);
                    var current = context.Bone(chain.BoneIds[i]);
                    if (current.parent != previous)
                        throw new PhysBonesBridgeException("UNSUPPORTED_CHAIN_TOPOLOGY", "PhysBones chain skips a scene parent: " + chain.Name);
                }
                if (chain.EndpointMode == PhysBonesEndpointMode.Bone) RequireDescendant(context.AvatarRoot, context.Bone(chain.EndBoneId));
                foreach (var id in chain.ExcludedBoneIds) RequireDescendant(context.AvatarRoot, context.Bone(id));
                foreach (var branch in chain.Branches)
                {
                    RequireDescendant(context.AvatarRoot, context.Bone(branch.ParentBoneId));
                    foreach (var id in branch.ChildBoneIds) RequireDescendant(context.AvatarRoot, context.Bone(id));
                }
                IReadOnlyList<Component> colliders;
                colliders = context.Colliders(chain);
                foreach (var value in colliders)
                {
                    if (value == null) throw new PhysBonesBridgeException("COLLIDER_BINDING_MISSING", "Collider binding contains a null component.");
                    RequireDescendant(context.AvatarRoot, value.transform);
                }
                var preflight = backend as IPhysBonesComponentPreflight;
                if (preflight != null) preflight.Validate(chain, context);
                var matching = markers.Where(marker => marker.Matches(profile.TargetId, index, chain.RootBoneId)).ToArray();
                if (matching.Length > 1) throw new PhysBonesBridgeException("MANAGED_COMPONENT_DUPLICATE", "More than one managed PhysBones marker matches chain " + index + ".");
                var marker = matching.SingleOrDefault();
                Component existing = null;
                if (marker != null)
                {
                    existing = marker.ManagedComponent;
                    if (existing == null || !backend.ComponentType.IsInstanceOfType(existing) || existing.gameObject != root.gameObject)
                        throw new PhysBonesBridgeException("MANAGED_COMPONENT_INVALID", "Managed PhysBones marker does not point to a valid component.");
                }
                else if (mode == PhysBonesApplyMode.UpdateManagedOnly)
                    throw new PhysBonesBridgeException("MANAGED_COMPONENT_MISSING", "Managed-only update found no owned component for chain " + index + ".");
                plans.Add(new Plan { Index = index, Chain = chain, Root = root, Existing = existing, Marker = marker });
            }
            return plans;
        }

        static void RequireDescendant(Transform root, Transform value)
        {
            if (value == null) throw new PhysBonesBridgeException("BONE_BINDING_MISSING", "A PhysBones bone binding is null.");
            if (value != root && !value.IsChildOf(root))
                throw new PhysBonesBridgeException("BONE_OUTSIDE_AVATAR", "A PhysBones binding is outside the explicit avatar root.");
        }

        static MarkerState Capture(NyaForgePhysBonesManaged marker)
        {
            return new MarkerState { Component = marker.ManagedComponent, Target = marker.TargetId, Index = marker.ChainIndex,
                Name = marker.ChainName, Root = marker.RootBoneId, Hash = marker.ProfileHash };
        }

        static void Rollback(Applied item, IPhysBonesComponentBackend backend)
        {
            try
            {
                if (item.CreatedMarker && item.Marker != null) Object.DestroyImmediate(item.Marker);
                else if (item.Marker != null)
                    item.Marker.Bind(item.MarkerState.Component, item.MarkerState.Target, item.MarkerState.Index,
                        item.MarkerState.Name, item.MarkerState.Root, item.MarkerState.Hash);
                if (item.CreatedComponent && item.Component != null) Object.DestroyImmediate(item.Component);
                else if (item.Component != null && item.Snapshot != null) backend.Restore(item.Component, item.Snapshot);
            }
            catch (Exception error) { Debug.LogException(error); }
        }
    }
}
