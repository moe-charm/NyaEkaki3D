using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Rig;
using NyaForge.Authoring.Simulation;
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
            manifestPath = EditorGUILayout.TextField("Target manifest", manifestPath);
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
            EditorGUILayout.LabelField("Skeleton", package.Skeleton.Bones.Count + " bones · " + package.Skeleton.ContentHash.Substring(0, 12));

            avatarRoot = (Transform)EditorGUILayout.ObjectField("Avatar root", avatarRoot, typeof(Transform), true);
            managedOnly = EditorGUILayout.ToggleLeft("管理対象だけを更新する（既存が無ければ停止）", managedOnly);
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

            bool ready = avatarRoot != null && RequiredBones().All(bone => boneBindings.ContainsKey(bone.BoneId) && boneBindings[bone.BoneId] != null)
                && groups.All(group => colliderBindings.ContainsKey(group) && colliderBindings[group].Any(component => component != null));
            using (new EditorGUI.DisabledScope(!ready))
            {
                if (GUILayout.Button(managedOnly ? "管理対象へ更新" : "PhysBones componentを作成／更新", GUILayout.Height(32))) ApplyPackage();
            }
            if (!ready) EditorGUILayout.HelpBox("Avatar root、stable bone、collider group（参照時）をすべて指定してください。", MessageType.Warning);
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

        void ApplyPackage()
        {
            try
            {
                if (package == null || avatarRoot == null) throw new InvalidOperationException("target packageとavatar rootが必要です。");
                var bones = new Dictionary<string, Transform>(boneBindings, StringComparer.Ordinal);
                VrcPhysBonesReflectionBackend backend;
                if (!VrcPhysBonesReflectionBackend.TryCreate(out backend, package.Target.SdkVersion))
                    throw new InvalidOperationException("VRChat PhysBones SDK component typeが見つかりません。SDKを導入したUnity projectで実行してください。");
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
