using System;
using System.IO;
using NyaForge.Authoring.Import;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IntegerField modelImportMeshIndex;
        IntegerField modelImportSkinIndex;
        IntegerField modelImportInstanceIndex;
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
            parent.Add(modelImportInstanceIndex); parent.Add(modelImportMeshIndex); parent.Add(modelImportSkinIndex);
            parent.Add(Button("候補を確認", () => Try(() => InspectModelSelection(modelImportPath.value)), "model-import-inspect"));
            modelImportMeshIndex.RegisterValueChangedCallback(_ => ClearModelImportSelectionStatus());
            modelImportSkinIndex.RegisterValueChangedCallback(_ => ClearModelImportSelectionStatus());
            modelImportInstanceIndex.RegisterValueChangedCallback(_ => ClearModelImportSelectionStatus());
        }

        void ClearModelImportSelectionStatus()
        {
            if (modelImportSelectionStatus != null) modelImportSelectionStatus.text = "";
        }

        void InspectModelSelection(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("GLBファイルを選択してください。");
            var inventory = GlbSceneInventoryReader.Read(File.ReadAllBytes(Path.GetFullPath(path)));
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
    }
}
