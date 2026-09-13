using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.UnityBridge;
using UnityEditor;
using UnityEngine;

namespace NyaForge.UnityBridge.Editor
{
    /// <summary>
    /// Receiver-side clothing package loader. Bone assignment is explicit and
    /// persistent; a package is never matched to an avatar by display name.
    /// </summary>
    public sealed class SkinnedClothingPackageWindow : EditorWindow
    {
        string manifestPath = "";
        SkinnedClothingPackage package;
        Transform avatarRoot;
        NyaForgeSkinnedClothingBinding binding;
        readonly Dictionary<string, Transform> boneBindings = new Dictionary<string, Transform>(StringComparer.Ordinal);
        Vector2 boneScroll;
        bool managedOnly;
        string status = "";
        MessageType statusType = MessageType.Info;

        [MenuItem("Tools/NyaForge/Import Skinned Clothing Package...")]
        static void Open()
        {
            GetWindow<SkinnedClothingPackageWindow>("NyaForge Clothing").minSize = new Vector2(620, 520);
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("NyaForge skinned clothing", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("衣装packageを検証し、avatar rootとstable BoneIdを明示対応してから適用します。名前による自動対応は行いません。", MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            string previousManifest = manifestPath;
            manifestPath = EditorGUILayout.TextField("Clothing manifest", manifestPath);
            if (manifestPath != previousManifest)
            {
                package = null;
                boneBindings.Clear();
                binding = null;
                status = "";
            }
            if (GUILayout.Button("選択", GUILayout.Width(56)))
            {
                string chosen = EditorUtility.OpenFilePanel("NyaForge skinned clothing manifest", "", "json");
                if (!string.IsNullOrEmpty(chosen)) { manifestPath = chosen; package = null; boneBindings.Clear(); status = ""; }
            }
            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(!File.Exists(manifestPath)))
            {
                if (GUILayout.Button("packageを検証して読み込む", GUILayout.Height(28))) LoadPackage();
            }

            if (package == null)
            {
                ShowStatus();
                return;
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Object", package.ObjectId);
            EditorGUILayout.LabelField("Graph", package.GraphId);
            EditorGUILayout.LabelField("Geometry", package.Mesh.VertexCount + " vertices · " + package.Mesh.TriangleCount + " triangles");
            EditorGUILayout.LabelField("Skeleton", package.Skeleton.Bones.Count + " bones · " + package.Skeleton.ContentHash.Substring(0, 12));

            Transform previousRoot = avatarRoot;
            avatarRoot = (Transform)EditorGUILayout.ObjectField("Avatar root", avatarRoot, typeof(Transform), true);
            if (avatarRoot != previousRoot)
            {
                boneBindings.Clear();
                binding = FindBindingForPackage();
            }
            managedOnly = EditorGUILayout.ToggleLeft("管理対象だけを更新する（既存が無ければ停止）", managedOnly);

            bool exact = binding != null && binding.Matches(package.ObjectId, package.StateHash,
                package.Skeleton.ContentHash, package.BindingHash);
            bool reusable = binding != null && binding.MatchesObject(package.ObjectId);
            bool sameAssignment = binding != null && binding.MatchesAssignment(package.ObjectId);
            if (binding == null)
                EditorGUILayout.HelpBox("このavatar rootには保存済みの衣装割当がありません。", MessageType.Info);
            else if (!exact && reusable)
                EditorGUILayout.HelpBox("同じObjectIdの新しいpackageです。保存済み骨対応を検証して更新できます。", MessageType.Info);
            else if (!exact)
                EditorGUILayout.HelpBox("保存済み割当は別の衣装です。既存管理objectを置き換える前に、正しいpackageを選んでください。", MessageType.Warning);

            EditorGUILayout.LabelField("Stable bone bindings", EditorStyles.boldLabel);
            boneScroll = EditorGUILayout.BeginScrollView(boneScroll, GUILayout.MinHeight(210));
            foreach (var bone in package.Skeleton.Bones)
            {
                Transform current;
                boneBindings.TryGetValue(bone.BoneId, out current);
                Transform assigned = (Transform)EditorGUILayout.ObjectField(
                    bone.Name + "  [" + bone.BoneId.Substring(0, 8) + "]", current, typeof(Transform), true);
                if (assigned == null) boneBindings.Remove(bone.BoneId);
                else boneBindings[bone.BoneId] = assigned;
            }
            EditorGUILayout.EndScrollView();

            var validation = ValidateCurrentBindings();
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!validation.IsValid))
            {
                if (GUILayout.Button("現在の割当を保存")) SaveBindings();
            }
            using (new EditorGUI.DisabledScope(binding == null || !binding.MatchesAssignment(package.ObjectId)))
            {
                if (GUILayout.Button("保存済み割当を読み込む")) LoadBindings();
            }
            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.DisabledScope(!validation.IsValid))
            {
                if (GUILayout.Button("事前診断（書き込みなし）")) InspectPackage();
            }
            using (new EditorGUI.DisabledScope(!validation.IsValid || managedOnly && !reusable || binding != null && !sameAssignment && !exact))
            {
                if (GUILayout.Button(managedOnly ? "管理対象へ更新" : "衣装を作成／更新", GUILayout.Height(32))) ApplyPackage();
            }
            using (new EditorGUI.DisabledScope(!bindingMatchesObject()))
            {
                if (GUILayout.Button("管理対象の衣装を削除（Undo可）")) RemovePackage();
            }
            if (!validation.IsValid)
                EditorGUILayout.HelpBox("割当を保存／適用できません: " + validation.Message, MessageType.Warning);
            ShowStatus();
        }

        void LoadPackage()
        {
            try
            {
                package = SkinnedClothingPackage.Read(manifestPath);
                boneBindings.Clear();
                binding = FindBindingForPackage();
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

        void SaveBindings()
        {
            try
            {
                var validation = ValidateCurrentBindings();
                if (!validation.IsValid) throw new InvalidOperationException(validation.Message);
                if (binding == null) binding = (NyaForgeSkinnedClothingBinding)Undo.AddComponent(avatarRoot.gameObject, typeof(NyaForgeSkinnedClothingBinding));
                Undo.RecordObject(binding, "Save NyaForge clothing bone bindings");
                // Bone assignments are reusable across package revisions. Keep
                // the existing managed object reference so an update can replace
                // it deterministically; exact state hashes describe the package
                // revision, not ownership identity.
                var generated = binding.MatchesAssignment(package.ObjectId) &&
                    binding.GeneratedObject != null && binding.GeneratedObject.transform.parent == avatarRoot
                    ? binding.GeneratedObject : null;
                binding.Capture(manifestPath, package.ObjectId, package.GraphId, package.StateHash, package.GraphHash,
                    package.GlbHash, package.Skeleton.ContentHash, package.BindingHash, boneBindings, generated);
                EditorUtility.SetDirty(binding);
                status = "stable BoneId割当をavatar rootへ保存しました。";
                statusType = MessageType.Info;
            }
            catch (Exception error) { SetError("割当を保存できませんでした: ", error); }
        }

        void LoadBindings()
        {
            try
            {
                if (binding == null || !binding.MatchesAssignment(package.ObjectId))
                    throw new InvalidOperationException("保存済み割当のObjectIdがこのpackageと一致しません。");
                boneBindings.Clear();
                foreach (var bone in binding.Bones)
                {
                    if (bone == null || string.IsNullOrEmpty(bone.BoneId) || bone.Transform == null)
                        throw new InvalidOperationException("保存済みstable bone割当が不完全です。");
                    boneBindings.Add(bone.BoneId, bone.Transform);
                }
                var validation = ValidateCurrentBindings();
                if (!validation.IsValid) throw new InvalidOperationException(validation.Message);
                status = "保存済み割当を読み込みました。";
                statusType = MessageType.Info;
            }
            catch (Exception error) { SetError("割当を読み込めませんでした: ", error); }
        }

        void InspectPackage()
        {
            try
            {
                var validation = ValidateCurrentBindings();
                if (!validation.IsValid) throw new InvalidOperationException(validation.Message);
                // Read again immediately before the write path so a changed sidecar
                // is reported as a package error instead of being applied partially.
                var checkedPackage = SkinnedClothingPackage.Read(manifestPath);
                if (checkedPackage.StateHash != package.StateHash || checkedPackage.BindingHash != package.BindingHash)
                    throw new InvalidOperationException("packageが読み込み後に変更されました。再読込してください。");
                status = "事前診断OK（sceneへの書き込みなし）。";
                statusType = MessageType.Info;
            }
            catch (Exception error) { SetError("事前診断で停止しました: ", error); }
        }

        void ApplyPackage()
        {
            SkinnedClothingReceiver.Result created = null;
            try
            {
                var validation = ValidateCurrentBindings();
                if (!validation.IsValid) throw new InvalidOperationException(validation.Message);
                var previous = binding == null ? null : binding.GeneratedObject;
                if (managedOnly && !bindingMatchesObject())
                    throw new InvalidOperationException("管理対象の一致する衣装objectがありません。");
                if (previous != null)
                {
                    if (!bindingMatchesObject() || previous.transform.parent != avatarRoot)
                        throw new InvalidOperationException("置き換え対象の管理objectがこのavatar rootにありません。");
                }
                created = SkinnedClothingReceiver.ApplyPackage(manifestPath, avatarRoot, boneBindings,
                    "NyaForge Clothing " + package.ObjectId.Substring(0, 8));
                if (previous != null) DestroyManagedObjectWithUndo(previous, "Update NyaForge clothing package");
                if (binding == null) binding = (NyaForgeSkinnedClothingBinding)Undo.AddComponent(avatarRoot.gameObject, typeof(NyaForgeSkinnedClothingBinding));
                Undo.RecordObject(binding, "Apply NyaForge clothing package");
                binding.Capture(manifestPath, package.ObjectId, package.GraphId, package.StateHash, package.GraphHash,
                    package.GlbHash, package.Skeleton.ContentHash, package.BindingHash, boneBindings, created.GameObject);
                EditorUtility.SetDirty(binding);
                status = previous == null ? "衣装を適用しました。" : "管理対象の衣装を更新しました。";
                statusType = MessageType.Info;
            }
            catch (Exception error)
            {
                if (created != null && created.GameObject != null) DestroyManagedObjectImmediately(created.GameObject);
                SetError("衣装を適用できませんでした: ", error);
            }
        }

        void RemovePackage()
        {
            try
            {
                if (!bindingMatchesObject())
                    throw new InvalidOperationException("このpackageに一致する管理対象の衣装objectがありません。");
                var previous = binding.GeneratedObject;
                if (previous.transform.parent != avatarRoot)
                    throw new InvalidOperationException("削除対象の管理objectがこのavatar rootにありません。");
                Undo.SetCurrentGroupName("Remove NyaForge clothing package");
                Undo.RecordObject(binding, "Remove NyaForge clothing binding");
                DestroyManagedObjectWithUndo(previous, "Remove NyaForge clothing package");
                binding.ClearGeneratedObject();
                EditorUtility.SetDirty(binding);
                status = "管理対象の衣装を削除しました。Undoで元の関連付けへ戻せます。";
                statusType = MessageType.Info;
            }
            catch (Exception error) { SetError("衣装を削除できませんでした: ", error); }
        }

        internal static void DestroyManagedObjectWithUndo(GameObject generated, string undoName)
        {
            if (generated == null) return;
            var marker = generated.GetComponent<NyaForgeSkinnedClothingManaged>();
            if (marker == null)
            {
                Undo.DestroyObjectImmediate(generated);
                return;
            }
            var mesh = marker.Mesh;
            var materials = marker.Materials.Where(material => material != null).Distinct().ToArray();
            var textures = new HashSet<Texture>();
            foreach (var material in materials)
            {
                if (material == null) continue;
                AddOwnedTexture(textures, material.mainTexture);
                if (material.HasProperty("_BumpMap")) AddOwnedTexture(textures, material.GetTexture("_BumpMap"));
                if (material.HasProperty("_MetallicGlossMap")) AddOwnedTexture(textures, material.GetTexture("_MetallicGlossMap"));
            }
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName(undoName);
            // Capture the references before destroying the GameObject. Each
            // generated asset is destroyed through Undo as well, so one Undo
            // restores the complete managed object and its material resources.
            Undo.DestroyObjectImmediate(generated);
            foreach (var texture in textures) Undo.DestroyObjectImmediate(texture);
            foreach (var material in materials) Undo.DestroyObjectImmediate(material);
            if (mesh != null) Undo.DestroyObjectImmediate(mesh);
            Undo.CollapseUndoOperations(group);
        }

        static void AddOwnedTexture(HashSet<Texture> textures, Texture texture)
        { if (texture != null && texture != Texture2D.whiteTexture) textures.Add(texture); }

        internal static void DestroyManagedObjectImmediately(GameObject generated)
        {
            if (generated == null) return;
            var marker = generated.GetComponent<NyaForgeSkinnedClothingManaged>();
            if (marker != null) marker.ReleaseOwnedAssets();
            UnityEngine.Object.DestroyImmediate(generated);
        }

        bool bindingMatchesObject()
        {
            return binding != null && binding.MatchesObject(package.ObjectId) && binding.GeneratedObject != null;
        }

        NyaForgeSkinnedClothingBinding FindBindingForPackage()
        {
            if (avatarRoot == null || package == null) return null;
            return avatarRoot.GetComponents<NyaForgeSkinnedClothingBinding>()
                .Where(candidate => candidate != null && candidate.ObjectId == package.ObjectId)
                .OrderByDescending(candidate => candidate.GeneratedObject != null)
                .FirstOrDefault();
        }

        Validation ValidateCurrentBindings()
        {
            if (package == null) return Validation.Invalid("packageを読み込んでください。");
            if (avatarRoot == null) return Validation.Invalid("avatar rootを指定してください。");
            foreach (var bone in package.Skeleton.Bones)
            {
                Transform target;
                if (!boneBindings.TryGetValue(bone.BoneId, out target) || target == null)
                    return Validation.Invalid("BoneId " + bone.BoneId.Substring(0, 8) + " の割当がありません。");
                if (target != avatarRoot && !target.IsChildOf(avatarRoot))
                    return Validation.Invalid("BoneId " + bone.BoneId.Substring(0, 8) + " がavatar rootの外です。");
            }
            return Validation.Valid();
        }

        void ShowStatus()
        {
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, statusType);
        }

        void SetError(string prefix, Exception error)
        {
            status = prefix + error.Message;
            statusType = MessageType.Error;
            Debug.LogException(error);
        }

        readonly struct Validation
        {
            public readonly bool IsValid;
            public readonly string Message;
            Validation(bool valid, string message) { IsValid = valid; Message = message; }
            public static Validation Valid() { return new Validation(true, ""); }
            public static Validation Invalid(string message) { return new Validation(false, message); }
        }
    }
}
