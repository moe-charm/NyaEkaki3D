using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;
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
        readonly List<string> morphTargetIds = new List<string>();

        void BuildMorph(VisualElement parent)
        {
            morphPanel = new Foldout { text = "5  Morph / 表情差分", value = false, name = "morph-panel" };
            morphStatus = new Label { name = "morph-status" }; morphStatus.style.whiteSpace = WhiteSpace.Normal; morphPanel.Add(morphStatus);
            morphTargetChoice = new DropdownField("対象target", new List<string> { "なし" }, 0) { name = "morph-target-choice" }; morphPanel.Add(morphTargetChoice);
            morphWeight = new FloatField("weight (0〜1)") { value = 0, name = "morph-weight" }; morphWeight.style.minHeight = 36; morphPanel.Add(morphWeight);
            applyMorphWeight = Button("Morph weightを適用", SetSelectedMorphWeight, "morph-set-weight"); morphPanel.Add(applyMorphWeight);
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
                morphTargetChoice.choices = new List<string> { "なし" }; morphTargetChoice.SetValueWithoutNotify("なし"); applyMorphWeight.SetEnabled(false); morphWeight.SetEnabled(false); return;
            }
            var graph = workspace.Document.Objects[0].Graph;
            var morphNode = graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.MorphSet && node.Morphs != null);
            var deformNode = graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.MorphDeform);
            if (morphNode == null || deformNode == null)
            {
                morphStatus.text = "Morph nodeがありません。ノード表示の「Morphサンプル」から始められます。";
                morphTargetChoice.choices = new List<string> { "なし" }; morphTargetChoice.SetValueWithoutNotify("なし"); applyMorphWeight.SetEnabled(false); morphWeight.SetEnabled(false); return;
            }
            morphTargetIds.AddRange(morphNode.Morphs.Targets.Select(target => target.TargetId));
            var labels = morphNode.Morphs.Targets.Select(target => target.Name + " · " + target.TargetId.Substring(0, 8)).ToList();
            int selected = morphTargetIds.IndexOf(SelectedMorphTargetId(deformNode)); if (selected < 0) selected = 0;
            morphTargetChoice.choices = labels; morphTargetChoice.SetValueWithoutNotify(labels[selected]);
            string targetId = morphTargetIds[selected];
            morphWeight.SetValueWithoutNotify(deformNode.MorphWeights.TryGetValue(targetId, out var value) ? value : 0);
            bool fresh = true;
            try
            {
                var meshNode = graph.Edges.FirstOrDefault(edge => edge.ToNode == deformNode.NodeId && edge.ToPort == "mesh");
                if (meshNode == null || !workspace.Preview.Evaluation.MeshOutputs.TryGetValue(meshNode.FromNode, out var input) || input.Mesh == null) fresh = false;
                else morphNode.Morphs.ValidateFor(input.Mesh);
            }
            catch (AuthoringException) { fresh = false; }
            morphStatus.text = "target " + morphNode.Morphs.Targets.Count + "個 · 差分 " + morphNode.Morphs.Targets.Sum(target => target.Deltas.Count) + "頂点" + (fresh ? "" : " · stale: mesh topologyを確認");
            bool editable = !workspace.Document.Objects[0].IsStaticProfile && fresh;
            applyMorphWeight.SetEnabled(editable); morphWeight.SetEnabled(editable);
        }

        string SelectedMorphTargetId(GraphNode deformNode)
        {
            int index = morphTargetChoice == null ? -1 : morphTargetChoice.index;
            if (index >= 0 && index < morphTargetIds.Count) return morphTargetIds[index];
            return deformNode.MorphWeights.Keys.OrderBy(id => id, StringComparer.Ordinal).FirstOrDefault() ?? "";
        }

        void SetSelectedMorphWeight()
        {
            Try(() =>
            {
                if (workspace == null || workspace.Document.IsEmpty) throw new InvalidOperationException("Morphを含むgraphを開いてください。");
                var graph = workspace.Document.Objects[0].Graph;
                var deformNode = graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.MorphDeform);
                if (deformNode == null) throw new InvalidOperationException("morph-deform nodeが見つかりません。");
                string targetId = SelectedMorphTargetId(deformNode); if (targetId == "") throw new InvalidOperationException("対象targetを選択してください。");
                var weights = deformNode.MorphWeights.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal); weights[targetId] = morphWeight.value;
                Execute(AuthoringOperation.UpdateNode(GraphNode.MorphDeformNode(deformNode.NodeId, weights)));
            });
        }
    }
}
