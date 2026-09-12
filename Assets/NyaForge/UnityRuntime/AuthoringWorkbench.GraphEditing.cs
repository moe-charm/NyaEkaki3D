using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        DropdownField editStage;
        Label editStageInfo;
        readonly List<string> editStageIds = new List<string>();
        GraphEditContext activeEditContext;
        bool IsGraph => workspace != null && !workspace.Document.IsEmpty && !workspace.Document.Objects[0].IsStaticProfile;

        void BuildGraphEditing(VisualElement side)
        {
            side.Add(Button("Planeグラフから始める", CreatePlaneGraph, "graph-create-plane"));
            side.Add(Button("空の形状から始める",()=>CreatePolygonGraph(true),"graph-create-empty-polygon"));
            side.Add(Button("四角面から始める", CreatePolygonGraph, "graph-create-polygon"));
            side.Add(Button("左右対称で始める", CreateMirrorGraph, "graph-create-mirror"));
            editStage = new DropdownField("表示・編集段", new List<string> { "最終出力（確認）" }, 0) { name = "graph-edit-stage" };
            editStage.style.flexDirection = FlexDirection.Column;
            editStage.RegisterValueChangedCallback(_ => Try(() => SelectEditStage(editStage.index)));
            side.Add(editStage);
            BuildFinalPreview(side);
            editStageInfo = new Label { name = "graph-edit-stage-info" };
            editStageInfo.style.whiteSpace = WhiteSpace.Normal; side.Add(editStageInfo);BuildFacelessRecovery(side);
        }

        void CreatePlaneGraph()
        {
            string plane = Guid.NewGuid().ToString("D"), edit = Guid.NewGuid().ToString("D"), output = Guid.NewGuid().ToString("D");
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] { GraphNode.Plane(plane), GraphNode.Edit(edit), GraphNode.Output(output) },
                new[] { new GraphEdge(plane, "mesh", edit, "mesh"), new GraphEdge(edit, "mesh", output, "mesh") }, output);
            Execute(AuthoringOperation.AddGraph(graph));
            if (IsGraph && workspace.Document.Objects[0].Graph.GraphId == graph.GraphId)
            {
                SelectEditStage(editStageIds.IndexOf(edit)); Frame();
            }
        }

        void CreatePolygonGraph()=>CreatePolygonGraph(false);
        void CreatePolygonGraph(bool empty)
        {
            string source = Guid.NewGuid().ToString("D"), edit = Guid.NewGuid().ToString("D"), output = Guid.NewGuid().ToString("D");
            var polygon = empty ? new NyaForge.Authoring.Topology.PolygonMesh(Guid.NewGuid().ToString("D"),Array.Empty<NyaForge.Authoring.Topology.CageVertex>(),Array.Empty<NyaForge.Authoring.Topology.CageFace>()) : NyaForge.Authoring.Topology.PolygonPrimitives.Plane(Guid.NewGuid().ToString("D"));
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] { GraphNode.Polygon(source, polygon, new RestTransform(1, new Vec3())), GraphNode.PolygonEdit(edit), GraphNode.Output(output) },
                new[] { new GraphEdge(source, "mesh", edit, "mesh"), new GraphEdge(edit, "mesh", output, "mesh") }, output);
            Execute(AuthoringOperation.AddGraph(graph));
            if (IsGraph && workspace.Document.Objects[0].Graph.GraphId == graph.GraphId) { SelectEditStage(editStageIds.IndexOf(edit)); Frame(); }
        }

        void SelectEditStage(int index)
        {
            if (index < 0 || index >= editStageIds.Count) return;
            string previous = projection.PreviewNodeId;
            projection.PreviewNodeId = editStageIds[index];
            try { using (var prepared = projection.PrepareGraph(workspace.Document, workspace.Preview)) prepared.Commit(); }
            catch { projection.PreviewNodeId = previous; throw; }
            activeEditContext = null; selection.Clear(); selectedFaces.Clear(); projection.Select(selection); Refresh();
        }

        GraphMeshValue DisplayedGraphValue()
        {
            if (IsGraph && projection.PreviewNodeId != "")
            {
                workspace.Preview.Evaluation.MeshOutputs.TryGetValue(projection.PreviewNodeId, out var value); return value;
            }
            return workspace.Preview.Output;
        }

        void RefreshGraphEditing()
        {
            editStageIds.Clear(); editStageIds.Add("");
            var labels = new List<string> { "最終出力（確認）" };
            if (IsGraph)
                foreach (var node in workspace.Document.Objects[0].Graph.Nodes.Values.OrderBy(n => n.NodeId, StringComparer.Ordinal))
                    if ((node.TypeId == BuiltinNodes.EditMesh || node.TypeId == BuiltinNodes.PolygonEdit) && BuiltinNodes.Find(node) != null)
                    { editStageIds.Add(node.NodeId); labels.Add((node.TypeId == BuiltinNodes.PolygonEdit ? "PolygonEdit" : "EditMesh") + " · " + node.NodeId.Substring(0, 8)); }
            int index = editStageIds.IndexOf(projection.PreviewNodeId);
            if (index < 0)
            {
                index = 0; projection.PreviewNodeId = "";
                using (var prepared = projection.PrepareGraph(workspace.Document, workspace.Preview)) prepared.Commit();
            }
            editStage.choices = labels; editStage.SetValueWithoutNotify(labels[index]); editStage.SetEnabled(IsGraph);
            finalPreview.SetEnabled(IsGraph && index > 0);
            root.Q<Button>("graph-create-plane").SetEnabled(workspace.Document.IsEmpty);
            root.Q<Button>("graph-create-polygon").SetEnabled(workspace.Document.IsEmpty);
            root.Q<Button>("graph-create-empty-polygon").SetEnabled(workspace.Document.IsEmpty);
            root.Q<Button>("graph-create-mirror").SetEnabled(workspace.Document.IsEmpty);
            var previous = activeEditContext; activeEditContext = null;
            editStageInfo.text = IsGraph ? "編集するEditMeshを選んでください。最終出力は確認用です。" : "プレートは従来の頂点編集を使用します。";
            if (IsGraph && index > 0)
            {
                try
                {
                    var graph = workspace.Document.Objects[0].Graph;
                    var value = DisplayedGraphValue();
                    if (value == null) throw new InvalidOperationException("編集段が未解決です。接続または上流の変更を確認してください。");
                    activeEditContext = GraphEditing.Context(graph, projection.PreviewNodeId);
                    var node = graph.Nodes[projection.PreviewNodeId];
                    if ((node.Offsets.Count > 0 || node.SourcePolygon != null) && (node.ExpectedInputSnapshot != activeEditContext.InputSnapshot || node.ExpectedDomain != activeEditContext.DomainId))
                        throw new InvalidOperationException("編集差分の入力が変わっています。元の入力に戻して確認してください。");
                    editStageInfo.text = "選択したEditMeshの結果を表示・編集しています。単位: mm / 文書 rev " + workspace.Document.DocumentRevision;
                    if (projection.FinalMesh != null) editStageInfo.text += "\n青緑と点が編集対象。灰色は最終結果です。";
                    else if (projection.ShowFinalResult && !workspace.Preview.IsComplete) editStageInfo.text += "\n最終評価が未完了のため灰色の結果は非表示です。";
                }
                catch (Exception e) { activeEditContext = null; editStageInfo.text = e.Message; }
            }
            if (previous?.NodeId != activeEditContext?.NodeId || previous?.InputSnapshot != activeEditContext?.InputSnapshot || previous?.DomainId != activeEditContext?.DomainId)
            { selection.Clear(); selectedFaces.Clear(); projection.Select(selection); }
            RefreshFacelessRecovery();
        }
    }
}


