using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class GraphCanvas
    {
        void BuildCard(GraphNode node, int index)
        {
            var card = new VisualElement { name = "graph-node-" + node.NodeId }; card.style.position = Position.Absolute;
            card.style.width = 250; card.style.paddingLeft = card.style.paddingRight = 8; card.style.paddingBottom = 8;
            card.style.backgroundColor = new Color(.13f, .18f, .23f);
            string key = LayoutKey(node.NodeId);
            if (!positions.TryGetValue(key, out var position)) positions[key] = position = new Vector2(20 + index % 6 * 290, 20 + index / 6 * 540);
            world.style.height = Mathf.Max(world.style.height.value.value, position.y + 600);
            card.style.left = position.x; card.style.top = position.y; cards.Add(card);
            var title = new Label(node.TypeId + " · " + node.NodeId.Substring(0, 6)) { name = "node-title" }; title.style.fontSize = 16; title.style.paddingTop = title.style.paddingBottom = 8;
            card.Add(title);
            bool dragging = false; Vector2 start = default, origin = default;
            title.RegisterCallback<PointerDownEvent>(e => { if (e.button != 0) return; dragging = true; start = e.position; origin = positions[key]; title.CapturePointer(e.pointerId); e.StopPropagation(); });
            title.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!dragging) return; var p = origin + (Vector2)e.position - start; p.x = Mathf.Clamp(p.x, 0, 1920); p.y = Mathf.Clamp(p.y, 0, world.resolvedStyle.height - 500);
                positions[key] = p; card.style.left = p.x; card.style.top = p.y; wires.MarkDirtyRepaint(); e.StopPropagation();
            });
            title.RegisterCallback<PointerUpEvent>(e => { if (dragging) SaveLayout(); dragging = false; title.ReleasePointer(e.pointerId); });
            title.RegisterCallback<PointerCaptureOutEvent>(_ => { if (dragging) SaveLayout(); dragging = false; });
            card.RegisterCallback<GeometryChangedEvent>(_ => wires.MarkDirtyRepaint());
            bool editable = !workspace.Document.ActiveObject.IsStaticProfile;
            var definition = BuiltinNodes.Find(node);
            if (definition != null)
            {
                foreach (var port in definition.Inputs)
                {
                    var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; card.Add(row);
                    var input = ActionButton("● " + port.Id + " ← " + port.Type, () => Input(node.NodeId, port.Id)); input.SetEnabled(editable); row.Add(input);
                    ports["in:" + node.NodeId + ":" + port.Id] = input;
                    if (Graph.Edges.Any(e => e.ToNode == node.NodeId && e.ToPort == port.Id))
                    { var cut = ActionButton("切断", () => submit(new[] { AuthoringOperation.Disconnect(node.NodeId, port.Id) })); cut.SetEnabled(editable); row.Add(cut); }
                }
                foreach (var port in definition.Outputs)
                {
                    var output = ActionButton(port.Type + " → " + port.Id + " ●", () => { sourceNode = node.NodeId; sourcePort = port.Id; ShowState(); }); output.SetEnabled(editable); card.Add(output);
                    ports["out:" + node.NodeId + ":" + port.Id] = output;
                }
                BuildParameters(card, node, editable);
            }
            else card.Add(new Label("未対応ノード / payloadを保持"));
            foreach (var diagnostic in workspace.Preview.Evaluation.Diagnostics.Where(d => d.NodeId == node.NodeId))
            { var label = new Label(diagnostic.Code); label.style.whiteSpace = WhiteSpace.Normal; label.style.color = new Color(1, .7f, .4f); card.Add(label); }
            var remove = ActionButton("ノードを削除", () => submit(new[] { AuthoringOperation.RemoveNode(node.NodeId) })); remove.SetEnabled(editable); card.Add(remove);
        }

        void BuildParameters(VisualElement card, GraphNode node, bool enabled)
        {
            var fields = new VisualElement(); fields.SetEnabled(enabled); card.Add(fields);
            if (node.TypeId == BuiltinNodes.Plane)
            {
                var width = Field(fields, "幅 (m)", node.Width); var height = Field(fields, "高さ (m)", node.Height);
                fields.Add(ActionButton("寸法を適用", () => submit(new[] { AuthoringOperation.UpdateNode(GraphNode.Plane(node.NodeId, width.value, height.value)) })));
            }
            else if (node.TypeId == BuiltinNodes.Scalar)
            {
                var value = Field(fields, "値", node.Scalar);
                fields.Add(ActionButton("値を適用", () => submit(new[] { AuthoringOperation.UpdateNode(GraphNode.Number(node.NodeId, value.value)) })));
            }
            else if (node.TypeId == BuiltinNodes.Mirror) BuildMirrorParameters(fields,node);
            else if (node.TypeId == BuiltinNodes.EditMesh || node.TypeId == BuiltinNodes.PolygonEdit)
            {
                fields.Add(ActionButton("この段を表示・編集", () => edit(node.NodeId)));
                fields.Add(ActionButton(node.Enabled ? "処理を無効化" : "処理を有効化", () => submit(new[] { AuthoringOperation.UpdateNode(node.TypeId == BuiltinNodes.PolygonEdit ? GraphNode.PolygonEdit(node.NodeId, node.SourcePolygon, node.ExpectedInputSnapshot, node.ExpectedDomain, !node.Enabled) : GraphNode.Edit(node.NodeId, !node.Enabled, new Dictionary<int, Vec3>(node.Offsets), node.ExpectedInputSnapshot, node.ExpectedDomain)) })));
            }
            else if (node.TypeId == BuiltinNodes.Output)
                fields.Add(ActionButton(Graph.OutputNodeId == node.NodeId ? "現在の最終出力" : "最終出力に指定", () => submit(new[] { AuthoringOperation.SetOutput(node.NodeId) })));
            else if (node.TypeId == BuiltinNodes.Skeleton)
                fields.Add(new Label("rest骨 " + node.Skeleton.Bones.Count + "本 · hash " + node.Skeleton.ContentHash.Substring(0, 8)));
            else if (node.TypeId == BuiltinNodes.SkinBind)
            {
                var evaluated = workspace.Preview.Evaluation.SkinBindingOutputs.TryGetValue(node.NodeId, out var value) ? value.Binding : null;
                fields.Add(new Label(evaluated == null ? "mesh＋skeleton入力を接続してください" : "weight " + evaluated.Weights.Count + "頂点 · 最大" + evaluated.Weights.Max(p => p.Value.Count) + "本"));
            }
            else if (node.TypeId == BuiltinNodes.Pose)
            {
                var pose = workspace.Preview.Evaluation.PoseOutputs.TryGetValue(node.NodeId, out var value) ? value.Pose : node.Pose;
                fields.Add(new Label("pose " + pose.Poses.Count + "本 · hash " + pose.ContentHash.Substring(0, 8)));
            }
            else if (node.TypeId == BuiltinNodes.SkinDeform)
                fields.Add(new Label("skin deformation · rest-relative"));
        }
        static FloatField Field(VisualElement parent, string label, float value)
        {
            var field = new FloatField(label) { value = value }; field.style.flexDirection = FlexDirection.Column; parent.Add(field); return field;
        }
    }
}

