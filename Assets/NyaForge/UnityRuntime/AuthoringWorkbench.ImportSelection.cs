using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring.Import;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IntegerField modelImportMeshIndex;
        IntegerField modelImportSkinIndex;
        IntegerField modelImportInstanceIndex;
        DropdownField modelImportMeshChoice;
        DropdownField modelImportSkinChoice;
        DropdownField modelImportInstanceChoice;
        Label modelImportSelectionStatus;

        void BuildModelImportSelection(VisualElement parent)
        {
            modelImportSelectionStatus = new Label { name = "model-import-selection-status" };
            modelImportSelectionStatus.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(modelImportSelectionStatus);
            modelImportMeshIndex = new IntegerField("mesh index") { value = 0, name = "model-import-mesh-index" };
            modelImportSkinIndex = new IntegerField("skin index") { value = 0, name = "model-import-skin-index" };
            modelImportInstanceIndex = new IntegerField("node instance index (-1=resource)") { value = -1, name = "model-import-instance-index" };
            modelImportMeshIndex.style.flexDirection = FlexDirection.Column;
            modelImportSkinIndex.style.flexDirection = FlexDirection.Column;
            modelImportInstanceIndex.style.flexDirection = FlexDirection.Column;
            // Keep the numeric fields in the tree for scripted checks and older
            // automation, but make the normal Windows path name-driven.
            modelImportMeshIndex.style.display = DisplayStyle.None;
            modelImportSkinIndex.style.display = DisplayStyle.None;
            modelImportInstanceIndex.style.display = DisplayStyle.None;
            parent.Add(modelImportInstanceIndex); parent.Add(modelImportMeshIndex); parent.Add(modelImportSkinIndex);
            modelImportInstanceChoice = new DropdownField("node instance", new List<string> { "候補を確認してください" }, 0) { name = "model-import-instance-choice" };
            modelImportMeshChoice = new DropdownField("mesh resource", new List<string> { "候補を確認してください" }, 0) { name = "model-import-mesh-choice" };
            modelImportSkinChoice = new DropdownField("skin resource", new List<string> { "候補を確認してください" }, 0) { name = "model-import-skin-choice" };
            modelImportInstanceChoice.tooltip = "node instanceを選ぶと、そのmesh・skin・配置を使います。先頭を選ぶと下のresource指定を使います。";
            modelImportMeshChoice.tooltip = "取り込むmesh resource。候補を確認してから選びます。";
            modelImportSkinChoice.tooltip = "取り込むskin resource。node instanceを選んだ場合は自動で決まります。";
            parent.Add(modelImportInstanceChoice); parent.Add(modelImportMeshChoice); parent.Add(modelImportSkinChoice);
            parent.Add(Button("候補を確認", () => Try(() => InspectModelSelection(modelImportPath.value)), "model-import-inspect"));
            modelImportMeshIndex.RegisterValueChangedCallback(_ => ClearModelImportSelectionStatus());
            modelImportSkinIndex.RegisterValueChangedCallback(_ => ClearModelImportSelectionStatus());
            modelImportInstanceIndex.RegisterValueChangedCallback(_ => ClearModelImportSelectionStatus());
            modelImportInstanceChoice.RegisterValueChangedCallback(change =>
            {
                int index = modelImportInstanceChoice.index - 1;
                modelImportInstanceIndex.SetValueWithoutNotify(index);
                ClearModelImportSelectionStatus();
            });
            modelImportMeshChoice.RegisterValueChangedCallback(change =>
            {
                if (change.newValue != null && change.newValue.StartsWith("mesh ", StringComparison.Ordinal))
                    modelImportMeshIndex.SetValueWithoutNotify(ParseChoiceIndex(change.newValue));
                ClearModelImportSelectionStatus();
            });
            modelImportSkinChoice.RegisterValueChangedCallback(change =>
            {
                if (change.newValue != null && change.newValue.StartsWith("skin ", StringComparison.Ordinal))
                    modelImportSkinIndex.SetValueWithoutNotify(ParseChoiceIndex(change.newValue));
                ClearModelImportSelectionStatus();
            });
        }

        void ClearModelImportSelectionStatus()
        {
            if (modelImportSelectionStatus != null) modelImportSelectionStatus.text = "";
        }

        void InspectModelSelection(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("GLBファイルを選択してください。");
            var inventory = GlbSceneInventoryReader.Read(File.ReadAllBytes(Path.GetFullPath(path)));
            SetModelImportChoices(inventory);
            int instanceIndex = modelImportInstanceIndex.value;
            if (instanceIndex >= 0 && instanceIndex >= inventory.Instances.Count) throw new InvalidOperationException("node instance index が範囲外です。候補を確認してください。");
            if (instanceIndex >= 0)
            {
                var instance = inventory.Instances[instanceIndex];
                modelImportSelectionStatus.text = "候補: node " + instanceIndex + " (" + instance.Name + ") → mesh " + instance.MeshIndex + (instance.SkinIndex.HasValue ? " · skin " + instance.SkinIndex.Value : " · skinなし") + " · instances " + inventory.Instances.Count + " · source " + inventory.SourceHash.Substring(0, 12);
                return;
            }
            int meshIndex = modelImportMeshIndex.value, skinIndex = modelImportSkinIndex.value;
            if (meshIndex < 0 || meshIndex >= inventory.Meshes.Count) throw new InvalidOperationException("mesh index が範囲外です。候補を確認してください。");
            if (inventory.Skins.Count > 0 && (skinIndex < 0 || skinIndex >= inventory.Skins.Count)) throw new InvalidOperationException("skin index が範囲外です。候補を確認してください。");
            var mesh = inventory.Meshes[meshIndex];
            string instances = " instances " + inventory.Instances.Count;
            modelImportSelectionStatus.text = "候補: mesh " + meshIndex + " (" + mesh.Name + ", " + mesh.PrimitiveCount + " primitive) · skins " + inventory.Skins.Count + instances + " · source " + inventory.SourceHash.Substring(0, 12);
        }

        int SelectedModelMeshIndex => modelImportMeshIndex == null ? 0 : modelImportMeshIndex.value;
        int SelectedModelSkinIndex => modelImportSkinIndex == null ? 0 : modelImportSkinIndex.value;
        int SelectedModelInstanceIndex => modelImportInstanceIndex == null ? -1 : modelImportInstanceIndex.value;

        void SetModelImportChoices(GlbSceneInventory inventory)
        {
            if (inventory == null) return;
            if (modelImportMeshChoice != null)
            {
                modelImportMeshChoice.choices = inventory.Meshes.Select(mesh => "mesh " + mesh.MeshIndex + " · " + mesh.Name + " · " + mesh.PrimitiveCount + " primitive").ToList();
                if (modelImportMeshChoice.choices.Count == 0) modelImportMeshChoice.choices.Add("mesh候補なし");
                modelImportMeshChoice.index = Math.Max(0, Math.Min(SelectedModelMeshIndex, modelImportMeshChoice.choices.Count - 1));
            }
            if (modelImportSkinChoice != null)
            {
                modelImportSkinChoice.choices = inventory.Skins.Select(skin => "skin " + skin.SkinIndex + " · " + skin.Joints.Count + " bone").ToList();
                if (modelImportSkinChoice.choices.Count == 0) modelImportSkinChoice.choices.Add("skinなし");
                modelImportSkinChoice.index = Math.Max(0, Math.Min(SelectedModelSkinIndex, modelImportSkinChoice.choices.Count - 1));
            }
            if (modelImportInstanceChoice != null)
            {
                var choices = new List<string> { "resourceを指定（nodeなし）" };
                choices.AddRange(inventory.Instances.Select((instance, index) =>
                    "node " + index + " · " + instance.Name + " → mesh " + instance.MeshIndex + (instance.SkinIndex.HasValue ? " · skin " + instance.SkinIndex.Value : " · skinなし")));
                modelImportInstanceChoice.choices = choices;
                modelImportInstanceChoice.index = SelectedModelInstanceIndex < 0 ? 0 : Math.Min(SelectedModelInstanceIndex + 1, choices.Count - 1);
            }
        }

        static int ParseChoiceIndex(string choice)
        {
            int separator = choice.IndexOf(' ');
            int end = choice.IndexOf(' ', separator + 1);
            if (separator < 0) return 0;
            string value = end < 0 ? choice.Substring(separator + 1) : choice.Substring(separator + 1, end - separator - 1);
            return int.TryParse(value, out var index) ? index : 0;
        }
    }
}
