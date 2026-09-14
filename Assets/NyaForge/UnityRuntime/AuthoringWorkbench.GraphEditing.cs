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
        Foldout graphDetailsPanel;
        readonly List<string> editStageIds = new List<string>();
        GraphEditContext activeEditContext;
        bool IsGraph => workspace != null && !workspace.Document.IsEmpty && !workspace.Document.ActiveObject.IsStaticProfile;

        void BuildGraphEditing(VisualElement side)
        {
            BuildShapeCreationPanel(side);
            // Keep the normal graph/edit-stage controls reachable for both a
            // first-time user and the existing pointer-based acceptance
            // harness. The developer-only template group below remains
            // collapsed so test fixtures do not dominate the production path.
            graphDetailsPanel = new Foldout { text = "ノード・編集段（詳細）", value = true, name = "graph-details" };
            var graphPanel = graphDetailsPanel;
            graphPanel.Add(new Label("基本形状を追加した後、処理段やノードを細かく確認できます。通常の作業では開かなくても編集できます。"));
            graphPanel.Add(Button("Planeグラフから始める", CreatePlaneGraph, "graph-create-plane"));
            graphPanel.Add(Button("空の形状から始める",()=>CreatePolygonGraph(true),"graph-create-empty-polygon"));
            graphPanel.Add(Button("四角面から始める", CreatePolygonGraph, "graph-create-polygon"));
            graphPanel.Add(Button("左右対称で始める", CreateMirrorGraph, "graph-create-mirror"));
            // Keep stable automation IDs for the verification harness while the
            // user-facing path lives in the reusable shape-creation panel.
            var developerTemplates = new Foldout { text = "検証用テンプレート", value = false, name = "developer-shape-templates" };
            developerTemplates.Add(new Label("自動検証と互換操作用。通常の衣装制作では「基本形状を追加」を使います。"));
            developerTemplates.Add(Button("検証: チョーカー", CreateChokerGraph, "graph-create-choker"));
            developerTemplates.Add(Button("検証: 手首カフ", CreateCuffGraph, "graph-create-cuff"));
            graphPanel.Add(developerTemplates);
            editStage = new DropdownField("表示・編集段", new List<string> { "最終出力（確認）" }, 0) { name = "graph-edit-stage" };
            editStage.style.flexDirection = FlexDirection.Column;
            editStage.RegisterValueChangedCallback(_ => Try(() => SelectEditStage(editStage.index)));
            graphPanel.Add(editStage);
            BuildFinalPreview(graphPanel);
            editStageInfo = new Label { name = "graph-edit-stage-info" };
            editStageInfo.style.whiteSpace = WhiteSpace.Normal; graphPanel.Add(editStageInfo);BuildFacelessRecovery(graphPanel);
            side.Add(graphPanel);
        }

        void CreatePlaneGraph()
        {
            string plane = Guid.NewGuid().ToString("D"), edit = Guid.NewGuid().ToString("D"), output = Guid.NewGuid().ToString("D");
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] { GraphNode.Plane(plane), GraphNode.Edit(edit), GraphNode.Output(output) },
                new[] { new GraphEdge(plane, "mesh", edit, "mesh"), new GraphEdge(edit, "mesh", output, "mesh") }, output);
            Execute(AuthoringOperation.AddGraph(graph));
            if (IsGraph && workspace.Document.ActiveObject.Graph.GraphId == graph.GraphId)
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
            if (IsGraph && workspace.Document.ActiveObject.Graph.GraphId == graph.GraphId) { SelectEditStage(editStageIds.IndexOf(edit)); Frame(); }
        }

        void CreateChokerGraph() => CreateChokerGraph(.06f, .008f, 24);

        void CreateChokerGraph(float radius, float tubeRadius, int segments)
        {
            string source = Guid.NewGuid().ToString("D"), edit = Guid.NewGuid().ToString("D"), output = Guid.NewGuid().ToString("D");
            var polygon = NyaForge.Authoring.Topology.PolygonPrimitives.Choker(Guid.NewGuid().ToString("D"), radius, tubeRadius, segments, 8);
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] { GraphNode.Polygon(source, polygon, new RestTransform(1, new Vec3())), GraphNode.PolygonEdit(edit), GraphNode.Output(output) },
                new[] { new GraphEdge(source, "mesh", edit, "mesh"), new GraphEdge(edit, "mesh", output, "mesh") }, output);
            Execute(AuthoringOperation.AddGraph(graph));
            if (IsGraph && workspace.Document.ActiveObject.Graph.GraphId == graph.GraphId)
            {
                SelectEditStage(editStageIds.IndexOf(edit)); Frame();
                SetStatus("チョーカー形状を追加しました。頂点編集・厚み・UV・材質を調整してください。");
            }
        }

        void CreateCuffGraph() => CreateCuffGraph(.04f, .035f, .004f, 32);

        void CreateCuffGraph(float radius, float width, float thickness, int segments)
        {
            string source = Guid.NewGuid().ToString("D"), edit = Guid.NewGuid().ToString("D"), output = Guid.NewGuid().ToString("D");
            var polygon = NyaForge.Authoring.Topology.PolygonPrimitives.Cuff(Guid.NewGuid().ToString("D"), radius, width, thickness, segments);
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] { GraphNode.Polygon(source, polygon, new RestTransform(1, new Vec3())), GraphNode.PolygonEdit(edit), GraphNode.Output(output) },
                new[] { new GraphEdge(source, "mesh", edit, "mesh"), new GraphEdge(edit, "mesh", output, "mesh") }, output);
            Execute(AuthoringOperation.AddGraph(graph));
            if (IsGraph && workspace.Document.ActiveObject.Graph.GraphId == graph.GraphId)
            {
                SelectEditStage(editStageIds.IndexOf(edit)); Frame();
                SetStatus("手首カフ形状を追加しました。頂点編集・厚み・UV・材質を調整してください。");
            }
        }

        void SelectEditStage(int index)
        {
            if (index < 0 || index >= editStageIds.Count) return;
            string previous = projection.PreviewNodeId;
            projection.PreviewNodeId = editStageIds[index];
            try { using (var prepared = projection.PrepareGraph(workspace.Document, workspace.Preview)) prepared.Commit(); }
            catch { projection.PreviewNodeId = previous; throw; }
            activeEditContext = null; selectionContext.SetEditNode(""); selection.Clear(); selectedFaces.Clear(); selectionContext.NotifyChanged(); projection.Select(selection); Refresh();
        }

        GraphMeshValue DisplayedGraphValue()
        {
            if (IsGraph && projection.PreviewNodeId != "")
            {
                workspace.Preview.Evaluation.MeshOutputs.TryGetValue(projection.PreviewNodeId, out var value); return value;
            }
            return SourceSkinDisplayValue() ?? workspace.Preview.Output;
        }

        void RefreshGraphEditing()
        {
            editStageIds.Clear(); editStageIds.Add("");
            var labels = new List<string> { "最終出力（確認）" };
            if (IsGraph)
                foreach (var node in workspace.Document.ActiveObject.Graph.Nodes.Values.OrderBy(n => n.NodeId, StringComparer.Ordinal))
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
            root.Q<Button>("graph-create-choker").SetEnabled(workspace.Document.IsEmpty || IsGraph);
            root.Q<Button>("graph-create-cuff").SetEnabled(workspace.Document.IsEmpty || IsGraph);
            var previous = activeEditContext; activeEditContext = null;
            editStageInfo.text = IsGraph ? "編集するEditMeshを選んでください。最終出力は確認用です。" : "プレートは従来の頂点編集を使用します。";
            if (IsGraph && index > 0)
            {
                try
                {
                    var graph = workspace.Document.ActiveObject.Graph;
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
            selectionContext.SetEditNode(activeEditContext?.NodeId);
            if (previous?.NodeId != activeEditContext?.NodeId || previous?.InputSnapshot != activeEditContext?.InputSnapshot || previous?.DomainId != activeEditContext?.DomainId)
            { selection.Clear(); selectedFaces.Clear(); selectionContext.NotifyChanged(); projection.Select(selection); }
            RefreshFacelessRecovery();
        }
    }
}


