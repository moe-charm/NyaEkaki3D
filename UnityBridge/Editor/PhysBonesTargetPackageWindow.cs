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

namespace NyaForge.UnityBridge.Editor
{
    /// <summary>
    /// Receiver-side package loader. Bone assignment is deliberately explicit: a name match is never used as a binding.
    /// </summary>
    public sealed class PhysBonesTargetPackageWindow : EditorWindow
    {
        string manifestPath = "";
        PhysBonesTargetPackage package;
        Transform avatarRoot;
        NyaForgePhysBonesBinding binding;
        readonly Dictionary<string, Transform> boneBindings = new Dictionary<string, Transform>(StringComparer.Ordinal);
        readonly Dictionary<int, List<Component>> colliderBindings = new Dictionary<int, List<Component>>();
        bool managedOnly;
        Vector2 boneScroll;
        string status = "";
        MessageType statusType = MessageType.Info;

        [MenuItem("Tools/NyaForge/Import PhysBones Target...")]
        static void Open()
        {
            GetWindow<PhysBonesTargetPackageWindow>("NyaForge PhysBones").minSize = new Vector2(620, 520);
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("NyaForge PhysBones target", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("target packageを読み込み、avatar rootとstable BoneIdを明示対応してからSDK componentへ適用します。名前による自動対応は行いません。", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            string previousManifest = manifestPath;
            manifestPath = EditorGUILayout.TextField("Target manifest", manifestPath);
            if (manifestPath != previousManifest)
            {
                package = null;
                boneBindings.Clear();
                colliderBindings.Clear();
                binding = null;
                status = "";
            }
            if (GUILayout.Button("選択", GUILayout.Width(56)))
            {
                string chosen = EditorUtility.OpenFilePanel("NyaForge PhysBones target manifest", "", "json");
                if (!string.IsNullOrEmpty(chosen)) { manifestPath = chosen; package = null; status = ""; }
            }
            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(!File.Exists(manifestPath)))
            {
                if (GUILayout.Button("packageを検証して読み込む", GUILayout.Height(28))) LoadPackage();
            }

            if (package == null)
            {
                if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, statusType);
                return;
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Target", package.Target.TargetId);
            EditorGUILayout.LabelField("SDK version", package.Target.SdkVersion);
            EditorGUILayout.LabelField("Package version", string.IsNullOrEmpty(package.Target.PackageVersion) ? "(なし)" : package.Target.PackageVersion);
            EditorGUILayout.LabelField("Component type", package.ComponentTypeName);
            EditorGUILayout.LabelField("Skeleton", package.Skeleton.Bones.Count + " bones · " + package.Skeleton.ContentHash.Substring(0, 12));

            Transform previousRoot = avatarRoot;
            avatarRoot = (Transform)EditorGUILayout.ObjectField("Avatar root", avatarRoot, typeof(Transform), true);
            if (avatarRoot != previousRoot)
            {
                boneBindings.Clear();
                colliderBindings.Clear();
                binding = avatarRoot == null ? null : avatarRoot.GetComponent<NyaForgePhysBonesBinding>();
            }
            managedOnly = EditorGUILayout.ToggleLeft("管理対象だけを更新する（既存が無ければ停止）", managedOnly);
            bool bindingMatches = binding != null && binding.Matches(package.ManifestHash, package.Target.TargetId,
                package.Target.SdkVersion, package.Target.ContentHash, package.Skeleton.ContentHash);
            if (binding == null)
                EditorGUILayout.HelpBox("このavatar rootには保存済みの割当がありません。", MessageType.Info);
            else if (!bindingMatches)
                EditorGUILayout.HelpBox("保存済み割当はこのtarget packageと一致しません。読み込まず、現在の割当を保存し直してください。", MessageType.Warning);
            EditorGUILayout.LabelField("Stable bone bindings", EditorStyles.boldLabel);
            boneScroll = EditorGUILayout.BeginScrollView(boneScroll, GUILayout.MinHeight(190));
            foreach (var bone in RequiredBones())
            {
                Transform current;
                boneBindings.TryGetValue(bone.BoneId, out current);
                Transform assigned = (Transform)EditorGUILayout.ObjectField(
                    bone.Name + "  [" + bone.BoneId.Substring(0, 8) + "]", current, typeof(Transform), true);
                if (assigned == null) boneBindings.Remove(bone.BoneId);
                else boneBindings[bone.BoneId] = assigned;
            }
            EditorGUILayout.EndScrollView();

            var groups = RequiredColliderGroups().ToArray();
            if (groups.Length > 0)
            {
                EditorGUILayout.LabelField("Collider group bindings", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox("各groupへavatar root配下のcollider componentを1つ以上、明示指定してください。", MessageType.Info);
                foreach (int group in groups)
                {
                    List<Component> values;
                    if (!colliderBindings.TryGetValue(group, out values)) colliderBindings[group] = values = new List<Component>();
                    EditorGUILayout.LabelField("Group " + group);
                    for (int i = 0; i < values.Count; i++)
                    {
                        EditorGUILayout.BeginHorizontal();
                        values[i] = (Component)EditorGUILayout.ObjectField("Collider " + (i + 1), values[i], typeof(Component), true);
                        if (GUILayout.Button("削除", GUILayout.Width(48))) { values.RemoveAt(i); i--; }
                        EditorGUILayout.EndHorizontal();
                    }
                    if (GUILayout.Button("このgroupへcolliderを追加")) values.Add(null);
                }
            }

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!readyForBinding()))
            {
                if (GUILayout.Button("現在の割当を保存")) SaveBindings();
            }
            using (new EditorGUI.DisabledScope(!bindingMatches))
            {
                if (GUILayout.Button("保存済み割当を読み込む")) LoadBindings();
            }
            EditorGUILayout.EndHorizontal();

            var validation = ValidateCurrentBindings();
            bool ready = validation.IsValid;
            using (new EditorGUI.DisabledScope(!ready))
            {
                if (GUILayout.Button(managedOnly ? "管理対象へ更新" : "PhysBones componentを作成／更新", GUILayout.Height(32))) ApplyPackage();
            }
            if (!ready) EditorGUILayout.HelpBox("割当を保存／適用できません: " + validation.Summary, MessageType.Warning);
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, statusType);
        }

        IEnumerable<BoneDefinition> RequiredBones()
        {
            if (package == null) yield break;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var chain in package.Target.Chains)
            {
                foreach (string id in chain.BoneIds) ids.Add(id);
                foreach (string id in chain.ExcludedBoneIds) ids.Add(id);
                if (chain.EndBoneId != "") ids.Add(chain.EndBoneId);
                foreach (var branch in chain.Branches)
                {
                    ids.Add(branch.ParentBoneId);
                    foreach (string id in branch.ChildBoneIds) ids.Add(id);
                }
            }
            foreach (var bone in package.Skeleton.Bones) if (ids.Contains(bone.BoneId)) yield return bone;
        }

        void LoadPackage()
        {
            try
            {
                package = PhysBonesTargetPackage.Read(manifestPath);
                boneBindings.Clear();
                colliderBindings.Clear();
                binding = avatarRoot == null ? null : avatarRoot.GetComponent<NyaForgePhysBonesBinding>();
                status = "読み込みました。stable boneを手動対応してください。";
                statusType = MessageType.Info;
            }
            catch (Exception error)
            {
                package = null;
                status = "読み込めませんでした: " + error.Message;
                statusType = MessageType.Error;
                Debug.LogException(error);
            }
        }

        bool readyForBinding()
        {
            return ValidateCurrentBindings().IsValid;
        }

        PhysBonesBindingValidationResult ValidateCurrentBindings()
        {
            if (package == null || avatarRoot == null)
                return PhysBonesBindingValidator.Validate(avatarRoot, boneBindings, ColliderBindingsForValidation());
            return PhysBonesBindingValidator.Validate(avatarRoot, boneBindings, ColliderBindingsForValidation(),
                RequiredBones().Select(bone => bone.BoneId), RequiredColliderGroups());
        }

        IEnumerable<KeyValuePair<int, IEnumerable<Component>>> ColliderBindingsForValidation()
        {
            foreach (var pair in colliderBindings)
                yield return new KeyValuePair<int, IEnumerable<Component>>(pair.Key, pair.Value);
        }

        void SaveBindings()
        {
            try
            {
                if (!readyForBinding()) throw new InvalidOperationException("完全なstable bone／collider group割当が必要です。");
                PhysBonesBindingValidator.RequireValid(avatarRoot, boneBindings, ColliderBindingsForValidation(),
                    RequiredBones().Select(bone => bone.BoneId), RequiredColliderGroups());
                if (binding == null) binding = (NyaForgePhysBonesBinding)Undo.AddComponent(avatarRoot.gameObject, typeof(NyaForgePhysBonesBinding));
                var groups = colliderBindings.Select(pair => new KeyValuePair<int, IEnumerable<Component>>(pair.Key, pair.Value));
                binding.Capture(manifestPath, package.ManifestHash, package.Target.TargetId, package.Target.SdkVersion,
                    package.Target.ContentHash, package.Skeleton.ContentHash,
                    boneBindings, groups);
                EditorUtility.SetDirty(binding);
                status = "stable bone／collider group割当をavatar rootへ保存しました。";
                statusType = MessageType.Info;
            }
            catch (Exception error)
            {
                status = "割当を保存できませんでした: " + error.Message;
                statusType = MessageType.Error;
                Debug.LogException(error);
            }
        }

        void LoadBindings()
        {
            try
            {
                if (binding == null || !binding.Matches(package.ManifestHash, package.Target.TargetId,
                    package.Target.SdkVersion, package.Target.ContentHash, package.Skeleton.ContentHash))
                    throw new InvalidOperationException("保存済み割当がtarget packageと一致しません。");
                boneBindings.Clear();
                colliderBindings.Clear();
                foreach (var bone in binding.Bones)
                {
                    if (bone == null || string.IsNullOrEmpty(bone.BoneId) || bone.Transform == null)
                        throw new InvalidOperationException("保存済みstable bone割当が不完全です。");
                    boneBindings.Add(bone.BoneId, bone.Transform);
                }
                foreach (var group in binding.ColliderGroups)
                {
                    if (group == null) continue;
                    colliderBindings[group.GroupIndex] = group.Colliders.Where(component => component != null).ToList();
                }
                PhysBonesBindingValidator.RequireValid(avatarRoot, boneBindings, ColliderBindingsForValidation(),
                    RequiredBones().Select(bone => bone.BoneId), RequiredColliderGroups());
                status = "保存済み割当を読み込みました。";
                statusType = MessageType.Info;
            }
            catch (Exception error)
            {
                status = "割当を読み込めませんでした: " + error.Message;
                statusType = MessageType.Error;
                Debug.LogException(error);
            }
        }

        void ApplyPackage()
        {
            try
            {
                if (package == null || avatarRoot == null) throw new InvalidOperationException("target packageとavatar rootが必要です。");
                PhysBonesBindingValidator.RequireValid(avatarRoot, boneBindings, ColliderBindingsForValidation(),
                    RequiredBones().Select(bone => bone.BoneId), RequiredColliderGroups());
                var bones = new Dictionary<string, Transform>(boneBindings, StringComparer.Ordinal);
                VrcPhysBonesReflectionBackend backend;
                string resolverDiagnostic;
                if (!VrcPhysBonesReflectionBackend.TryCreate(out backend, package.Target.SdkVersion, package.ComponentTypeName, out resolverDiagnostic))
                    throw new InvalidOperationException("VRChat PhysBones SDK component typeを解決できません: " + resolverDiagnostic);
                var colliderGroups = new Dictionary<int, IReadOnlyList<Component>>();
                foreach (var pair in colliderBindings) colliderGroups[pair.Key] = pair.Value.Where(component => component != null).ToArray();
                var result = PhysBonesBridge.ApplyPackage(manifestPath,
                    new PhysBonesBridgeContext(avatarRoot, bones, colliderGroups), backend,
                    managedOnly ? PhysBonesApplyMode.UpdateManagedOnly : PhysBonesApplyMode.CreateOrUpdateManaged);
                status = "適用しました: created " + result.CreatedCount + " · updated " + result.UpdatedCount + " · profile " + result.ProfileHash.Substring(0, 12);
                statusType = MessageType.Info;
            }
            catch (PhysBonesBridgeException error)
            {
                status = "適用できませんでした [" + error.Code + "]: " + error.Message;
                if (error.LossReport != null && error.LossReport.Unsupported.Count > 0)
                    status += " 未対応: " + string.Join(", ", error.LossReport.Unsupported.Select(entry => entry.Path).ToArray());
                statusType = MessageType.Error;
                Debug.LogException(error);
            }
            catch (Exception error)
            {
                status = "適用できませんでした: " + error.Message;
                statusType = MessageType.Error;
                Debug.LogException(error);
            }
        }

        IEnumerable<int> RequiredColliderGroups()
        {
            if (package == null) yield break;
            foreach (int group in package.Target.Chains.SelectMany(chain => chain.ColliderGroupIndices).Distinct().OrderBy(value => value)) yield return group;
        }
    }
}
