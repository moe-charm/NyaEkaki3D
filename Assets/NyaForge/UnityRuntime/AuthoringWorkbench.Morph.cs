using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Foldout morphPanel;
        Label morphStatus;
        DropdownField morphTargetChoice;
        FloatField morphWeight;
        Button applyMorphWeight;
        DropdownField vrmExpressionChoice;
        Button applyVrmExpression;
        readonly List<string> morphTargetIds = new List<string>();
        IReadOnlyList<MappedVrmExpression> importedVrmExpressions { get { return importedVrmSession == null ? Array.Empty<MappedVrmExpression>() : importedVrmSession.Expressions; } }

        void BuildMorph(VisualElement parent)
        {
            morphPanel = new Foldout { text = "5  Morph / 表情差分", value = false, name = "morph-panel" };
            morphStatus = new Label { name = "morph-status" }; morphStatus.style.whiteSpace = WhiteSpace.Normal; morphPanel.Add(morphStatus);
            morphTargetChoice = new DropdownField("対象target", new List<string> { "なし" }, 0) { name = "morph-target-choice" };
            morphTargetChoice.tooltip = "Morph targetを選びます。選択後にマウスを載せると完全なtarget IDを確認できます。";
            morphTargetChoice.RegisterValueChangedCallback(_ => RefreshMorphTargetTooltip());
            morphPanel.Add(morphTargetChoice);
            morphWeight = new FloatField("weight (0〜1)") { value = 0, name = "morph-weight" }; morphWeight.style.minHeight = 36; morphPanel.Add(morphWeight);
            applyMorphWeight = Button("Morph weightを適用", SetSelectedMorphWeight, "morph-set-weight"); morphPanel.Add(applyMorphWeight);
            vrmExpressionChoice = new DropdownField("VRM表情", new List<string> { "なし" }, 0) { name = "vrm-expression-choice" }; morphPanel.Add(vrmExpressionChoice);
            applyVrmExpression = Button("選択したVRM表情を適用", ApplySelectedVrmExpression, "vrm-expression-apply"); morphPanel.Add(applyVrmExpression);
            morphPanel.Add(new Label("Morphはrest meshの頂点差分です。mesh topologyが変わった場合は自動適用しません。"));
            parent.Add(morphPanel);
        }

        void RefreshMorph()
        {
            if (morphPanel == null) return;
            morphTargetIds.Clear();
            if (workspace == null || workspace.Document.IsEmpty)
            {
                morphStatus.text = "Morphサンプルまたはmorph nodeを追加すると表情差分を編集できます。";
                morphTargetChoice.choices = new List<string> { "なし" }; morphTargetChoice.SetValueWithoutNotify("なし"); morphTargetChoice.tooltip = "Morph targetを選びます。Morph nodeがあると完全なtarget IDを表示します。"; applyMorphWeight.SetEnabled(false); morphWeight.SetEnabled(false); ResetVrmExpressionUi(); return;
            }
            var graph = workspace.Document.ActiveObject.Graph;
            var morphNode = graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.MorphSet && node.Morphs != null);
            var deformNode = graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.MorphDeform);
            if (morphNode == null || deformNode == null)
            {
                morphStatus.text = "Morph nodeがありません。ノード表示の「Morphサンプル」から始められます。";
                morphTargetChoice.choices = new List<string> { "なし" }; morphTargetChoice.SetValueWithoutNotify("なし"); morphTargetChoice.tooltip = "Morph targetを選びます。Morph nodeがあると完全なtarget IDを表示します。"; applyMorphWeight.SetEnabled(false); morphWeight.SetEnabled(false); ResetVrmExpressionUi(); return;
            }
            morphTargetIds.AddRange(morphNode.Morphs.Targets.Select(target => target.TargetId));
            var labels = morphNode.Morphs.Targets.Select(target => target.Name + " · " + target.TargetId.Substring(0, 8)).ToList();
            int selected = morphTargetIds.IndexOf(SelectedMorphTargetId(deformNode)); if (selected < 0) selected = 0;
            morphTargetChoice.choices = labels; morphTargetChoice.SetValueWithoutNotify(labels[selected]);
            string targetId = morphTargetIds[selected];
            string activeObjectLabel = ObjectDisplayName(workspace.Document.ActiveObject);
            morphTargetChoice.tooltip = CurrentMorphTargetTooltip(morphNode.Morphs.Targets[selected].Name, targetId);
            morphWeight.SetValueWithoutNotify(deformNode.MorphWeights.TryGetValue(targetId, out var value) ? value : 0);
            bool fresh = true;
            try
            {
                var meshNode = graph.Edges.FirstOrDefault(edge => edge.ToNode == deformNode.NodeId && edge.ToPort == "mesh");
                if (meshNode == null || !workspace.Preview.Evaluation.MeshOutputs.TryGetValue(meshNode.FromNode, out var input) || input.Mesh == null) fresh = false;
                else morphNode.Morphs.ValidateFor(input.Mesh);
            }
            catch (AuthoringException) { fresh = false; }
            morphStatus.text = "対象: " + activeObjectLabel + " · target " + morphNode.Morphs.Targets.Count + "個 · 差分 " + morphNode.Morphs.Targets.Sum(target => target.Deltas.Count) + "頂点" + (fresh ? "" : " · stale: mesh topologyを確認");
            bool editable = !workspace.Document.ActiveObject.IsStaticProfile && fresh;
            applyMorphWeight.SetEnabled(editable); morphWeight.SetEnabled(editable);
            var expressionLabels = importedVrmExpressions.Count == 0 ? new List<string> { "なし" } : importedVrmExpressions.Select(expression => expression.Name + (expression.IsCustom ? " · custom" : " · preset")).ToList();
            vrmExpressionChoice.choices = expressionLabels; if (vrmExpressionChoice.index < 0 || vrmExpressionChoice.index >= expressionLabels.Count) vrmExpressionChoice.SetValueWithoutNotify(expressionLabels[0]);
            applyVrmExpression.SetEnabled(editable && importedVrmExpressions.Count > 0);
            if (importedVrmExpressions.Count > 0) morphStatus.text += " · VRM表情 " + importedVrmExpressions.Count + "件";
        }

        string SelectedMorphTargetId(GraphNode deformNode)
        {
            int index = morphTargetChoice == null ? -1 : morphTargetChoice.index;
            if (index >= 0 && index < morphTargetIds.Count) return morphTargetIds[index];
            return deformNode.MorphWeights.Keys.OrderBy(id => id, StringComparer.Ordinal).FirstOrDefault() ?? "";
        }

        static string MorphTargetTooltip(string name, string targetId)
        {
            return "Morph target: " + (string.IsNullOrWhiteSpace(name) ? "(名称なし)" : name) + "\n完全なtarget ID: " + targetId +
                "\nこのIDで現在のMorph差分とweightを対応付けます。";
        }

        string CurrentMorphTargetTooltip(string name, string targetId)
        {
            string result = MorphTargetTooltip(name, targetId);
            if (workspace != null && !workspace.Document.IsEmpty && workspace.Document.ActiveObject != null)
                result += "\n現在の制作対象: " + ObjectDisplayName(workspace.Document.ActiveObject) +
                    "\nobject ID: " + workspace.Document.ActiveObjectId;
            return result;
        }

        void RefreshMorphTargetTooltip()
        {
            int index = morphTargetChoice == null ? -1 : morphTargetChoice.index;
            if (index < 0 || index >= morphTargetIds.Count)
            {
                if (morphTargetChoice != null)
                    morphTargetChoice.tooltip = "Morph targetを選びます。Morph nodeがあると完全なtarget IDを表示します。";
                return;
            }
            string name = morphTargetChoice.value;
            int separator = name.IndexOf(" · ", StringComparison.Ordinal);
            if (separator >= 0) name = name.Substring(0, separator);
            morphTargetChoice.tooltip = CurrentMorphTargetTooltip(name, morphTargetIds[index]);
        }

        void SetSelectedMorphWeight()
        {
            Try(() =>
            {
                if (workspace == null || workspace.Document.IsEmpty) throw new InvalidOperationException("Morphを含むgraphを開いてください。");
                var graph = workspace.Document.ActiveObject.Graph;
                var deformNode = graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.MorphDeform);
                if (deformNode == null) throw new InvalidOperationException("morph-deform nodeが見つかりません。");
                string targetId = SelectedMorphTargetId(deformNode); if (targetId == "") throw new InvalidOperationException("対象targetを選択してください。");
                var weights = deformNode.MorphWeights.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal); weights[targetId] = morphWeight.value;
                Execute(AuthoringOperation.UpdateNode(GraphNode.MorphDeformNode(deformNode.NodeId, weights)));
            });
        }

        void ApplySelectedVrmExpression()
        {
            Try(() =>
            {
                if (workspace == null || workspace.Document.IsEmpty || importedVrmExpressions.Count == 0) throw new InvalidOperationException("VRM表情を含むgraphを開いてください。");
                int index = vrmExpressionChoice.index; if (index < 0 || index >= importedVrmExpressions.Count) throw new InvalidOperationException("VRM表情を選択してください。");
                var graph = workspace.Document.ActiveObject.Graph; var morphNode = graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.MorphSet && node.Morphs != null); var deformNode = graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.MorphDeform);
                if (morphNode == null || deformNode == null) throw new InvalidOperationException("morph nodeが見つかりません。");
                var selected = importedVrmExpressions[index]; ChecksMappedTargets(selected, morphNode.Morphs); var weights = morphNode.Morphs.Targets.ToDictionary(target => target.TargetId, target => selected.Weights.TryGetValue(target.TargetId, out var weight) ? weight : 0f, StringComparer.Ordinal);
                Execute(AuthoringOperation.UpdateNode(GraphNode.MorphDeformNode(deformNode.NodeId, weights)));
                SetStatus("VRM表情を適用しました: " + selected.Name + "。元に戻す・やり直すで確認できます。");
            });
        }

        static VrmExpressionSession PrepareImportedExpressions(byte[] bytes, VrmMetadata metadata, MorphSet morphs)
        {
            if (metadata == null || morphs == null) return null;
            try { return VrmExpressionSession.Create(metadata, VrmExpressionMapper.ResolveForImportedMesh(bytes, metadata, morphs)); }
            catch (AuthoringException error) { Debug.LogWarning("[NyaForge VRM] expression mapping unavailable: " + error.Code + ": " + error.Message); return null; }
        }

        void ClearImportedVrmExpressions() { ClearImportedVrmExpressionTable(); }
        static void ChecksMappedTargets(MappedVrmExpression expression, MorphSet morphs)
        {
            foreach (var targetId in expression.Weights.Keys) if (!morphs.ById.ContainsKey(targetId)) throw new InvalidOperationException("VRM表情が現在のMorphSetと一致しません。再取り込みしてください。");
        }
        void ResetVrmExpressionUi()
        {
            if (vrmExpressionChoice == null) return;
            vrmExpressionChoice.choices = new List<string> { "なし" }; vrmExpressionChoice.SetValueWithoutNotify("なし"); applyVrmExpression.SetEnabled(false);
        }
    }
}
