using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NyaForge.UnityBridge.Editor
{
    /// <summary>Read-only diagnostics for the explicit scene mapping used by the PhysBones receiver.</summary>
    public sealed class PhysBonesBindingValidationResult
    {
        readonly IReadOnlyList<string> errors;

        internal PhysBonesBindingValidationResult(IEnumerable<string> values)
        {
            errors = Array.AsReadOnly((values ?? Array.Empty<string>()).Where(value => !string.IsNullOrEmpty(value)).ToArray());
        }

        public bool IsValid { get { return errors.Count == 0; } }
        public IReadOnlyList<string> Errors { get { return errors; } }
        public string Summary { get { return string.Join(" ", errors.ToArray()); } }
    }

    /// <summary>
    /// Validates editor-owned explicit bindings before they are persisted or sent to an SDK backend.
    /// It intentionally does not inspect vendor component types: that remains the backend's job.
    /// </summary>
    public static class PhysBonesBindingValidator
    {
        public static PhysBonesBindingValidationResult Validate(Transform avatarRoot,
            IEnumerable<KeyValuePair<string, Transform>> bones,
            IEnumerable<KeyValuePair<int, IEnumerable<Component>>> colliderGroups,
            IEnumerable<string> requiredBoneIds = null,
            IEnumerable<int> requiredColliderGroups = null)
        {
            var errors = new List<string>();
            if (avatarRoot == null)
            {
                errors.Add("Avatar root is required.");
                return new PhysBonesBindingValidationResult(errors);
            }

            var boneValues = (bones ?? Array.Empty<KeyValuePair<string, Transform>>()).ToArray();
            var requiredBones = new HashSet<string>((requiredBoneIds ?? Array.Empty<string>()).Where(id => !string.IsNullOrWhiteSpace(id)), StringComparer.Ordinal);
            var seenBoneIds = new HashSet<string>(StringComparer.Ordinal);
            var seenTransforms = new HashSet<Transform>();
            foreach (var pair in boneValues)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    errors.Add("Every PhysBones bone binding needs a stable BoneId.");
                    continue;
                }
                if (!seenBoneIds.Add(pair.Key)) errors.Add("PhysBones bone binding is duplicated: " + pair.Key);
                requiredBones.Remove(pair.Key);
                if (pair.Value == null)
                {
                    errors.Add("PhysBones bone binding is null: " + pair.Key);
                    continue;
                }
                if (pair.Value != avatarRoot && !pair.Value.IsChildOf(avatarRoot))
                    errors.Add("PhysBones bone binding is outside the avatar root: " + pair.Key);
                else if (!seenTransforms.Add(pair.Value))
                    errors.Add("The same scene Transform is assigned to multiple PhysBones BoneIds: " + pair.Key);
            }
            foreach (string id in requiredBones.OrderBy(value => value, StringComparer.Ordinal))
                errors.Add("Required PhysBones bone binding is missing: " + id);

            var groupValues = (colliderGroups ?? Array.Empty<KeyValuePair<int, IEnumerable<Component>>>()).ToArray();
            var requiredGroups = new HashSet<int>(requiredColliderGroups ?? Array.Empty<int>());
            var seenGroups = new HashSet<int>();
            foreach (var pair in groupValues)
            {
                if (!seenGroups.Add(pair.Key)) errors.Add("PhysBones collider group is duplicated: " + pair.Key);
                requiredGroups.Remove(pair.Key);
                var values = (pair.Value ?? Array.Empty<Component>()).Where(value => value != null).ToArray();
                if (values.Length == 0)
                {
                    errors.Add("PhysBones collider group has no component: " + pair.Key);
                    continue;
                }
                var seenGroupColliders = new HashSet<Component>();
                foreach (var component in values)
                {
                    // Reusing one collider in multiple groups is valid; duplicates within one group are not.
                    if (!seenGroupColliders.Add(component))
                    {
                        errors.Add("The same collider component is assigned more than once: " + component.GetInstanceID());
                        continue;
                    }
                    if (component.transform == null || (component.transform != avatarRoot && !component.transform.IsChildOf(avatarRoot)))
                        errors.Add("PhysBones collider component is outside the avatar root: " + pair.Key);
                }
            }
            foreach (int group in requiredGroups.OrderBy(value => value))
                errors.Add("Required PhysBones collider group is missing: " + group);
            return new PhysBonesBindingValidationResult(errors);
        }

        public static void RequireValid(Transform avatarRoot,
            IEnumerable<KeyValuePair<string, Transform>> bones,
            IEnumerable<KeyValuePair<int, IEnumerable<Component>>> colliderGroups,
            IEnumerable<string> requiredBoneIds = null,
            IEnumerable<int> requiredColliderGroups = null)
        {
            var result = Validate(avatarRoot, bones, colliderGroups, requiredBoneIds, requiredColliderGroups);
            if (!result.IsValid) throw new InvalidOperationException(result.Summary);
        }
    }
}
