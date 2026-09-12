using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    /// <summary>Runtime-only node view. Layout is local view state; all content changes are commands.</summary>
    public sealed partial class GraphCanvas : VisualElement
    {
        readonly VisualElement world, wires, cards, toolbar;
        readonly Label message;
        readonly ScrollView scroller;
        readonly Dictionary<string, Vector2> positions = new Dictionary<string, Vector2>();
        readonly Dictionary<string, VisualElement> ports = new Dictionary<string, VisualElement>();
        AuthoringWorkspace workspace;
        Action<AuthoringOperation[]> submit;
        Action<string> edit;
        string binding, sourceNode, sourcePort;
        AuthoringGraph Graph => workspace.Document.IsEmpty ? null : workspace.Document.Objects[0].Graph;

        public GraphCanvas()
        {
            name = "graph-canvas"; style.height = 330; style.flexShrink = 0; style.backgroundColor = new Color(.065f, .085f, .11f);
            toolbar = new VisualElement(); toolbar.style.flexDirection = FlexDirection.Row; toolbar.style.flexWrap = Wrap.Wrap; Add(toolbar);
            foreach (var type in new[] { "Plane", "EditMesh", "PolygonEdit", "Mirror", "Output", "Scalar" })
            {
                string captured = type; toolbar.Add(ActionButton("＋ " + type, () => AddNode(captured), "graph-add-" + type));
            }
            toolbar.Add(ActionButton("＋ Rigサンプル", AddRigSample, "graph-add-rig-sample"));
            toolbar.Add(ActionButton("接続をキャンセル", () => { sourceNode = sourcePort = null; ShowState(); }, "graph-cancel-link"));
            message = new Label("出力ポート → 入力ポートの順にクリック。見出しをドラッグして配置。");
            message.style.whiteSpace = WhiteSpace.Normal; Add(message);
            var scroll = scroller = new ScrollView(ScrollViewMode.VerticalAndHorizontal); scroll.style.flexGrow = 1; Add(scroll);
            world = new VisualElement(); world.style.width = 2200; world.style.height = 1600; scroll.Add(world);
            wires = new VisualElement { pickingMode = PickingMode.Ignore }; Fill(wires); world.Add(wires);
            cards = new VisualElement { pickingMode = PickingMode.Ignore }; Fill(cards); world.Add(cards);
            wires.generateVisualContent += DrawWires;
        }

        static void Fill(VisualElement element)
        { element.style.position = Position.Absolute; element.style.left = element.style.top = element.style.right = element.style.bottom = 0; }
        Button ActionButton(string label, Action action, string id = null)
        { return new Button(() => { try { action(); } catch (Exception e) { message.text = e.Message; } }) { text = label, name = id }; }

        public void Bind(AuthoringWorkspace value, Action<AuthoringOperation[]> dispatch, Action<string> selectEdit)
        {
            workspace = value; submit = dispatch; edit = selectEdit;
            LoadLayout();
            string key = value.InstanceId + ":" + value.Document.StateHash;
            if (key == binding) return;
            binding = key; sourceNode = sourcePort = null; cards.Clear(); ports.Clear();
            toolbar.SetEnabled(value.Document.IsEmpty || !value.Document.Objects[0].IsStaticProfile);
            var graph = Graph;
            world.style.height = Math.Max(1600, ((graph?.Nodes.Count ?? 0) + 5) / 6 * 540 + 600);
            if (graph != null)
            {
                int index = 0;
                foreach (var node in graph.Nodes.Values.OrderBy(n => n.TypeId == BuiltinNodes.Output ? 2 : n.TypeId == BuiltinNodes.EditMesh ? 1 : 0).ThenBy(n => n.NodeId, StringComparer.Ordinal)) BuildCard(node, index++);
            }
            ShowState(); wires.MarkDirtyRepaint();
        }

        string LayoutKey(string node) => workspace.Document.DocumentId + ":" + Graph.GraphId + ":" + node;
        void ShowState()
        {
            message.text = sourceNode != null ? "接続先の入力ポートをクリックしてください。" :
                workspace.Document.IsEmpty ? "ノードを追加して始めます。" : workspace.Document.Objects[0].IsStaticProfile ? "プレートは確認表示です。新しい空プロジェクトからノードを追加できます。" :
                "出力 → 入力で接続。見出しをドラッグで移動。 " + (workspace.Preview.IsComplete ? "評価完了" : "評価未完了");
            if (layoutWarning != null) message.text += "\n" + layoutWarning;
        }

        void AddNode(string type)
        {
            string id = Guid.NewGuid().ToString("D");
            GraphNode node = type == "Plane" ? GraphNode.Plane(id) : type == "EditMesh" ? GraphNode.Edit(id) : type == "PolygonEdit" ? GraphNode.PolygonEdit(id) : type == "Mirror" ? GraphNode.Mirror(id) : type == "Output" ? GraphNode.Output(id) : GraphNode.Number(id, .2f);
            if (workspace.Document.IsEmpty)
                submit(new[] { AuthoringOperation.AddGraph(new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] { node }, Array.Empty<GraphEdge>(), type == "Output" ? id : "")) });
            else submit(new[] { AuthoringOperation.AddNode(node) });
        }

        void AddRigSample()
        {
            if (!workspace.Document.IsEmpty) { message.text = "Rigサンプルは空のプロジェクトで追加してください。"; return; }
            string root = Guid.NewGuid().ToString("D"), child = Guid.NewGuid().ToString("D"), skeletonId = Guid.NewGuid().ToString("D"), planeId = Guid.NewGuid().ToString("D"), bindId = Guid.NewGuid().ToString("D"), poseId = Guid.NewGuid().ToString("D"), deformId = Guid.NewGuid().ToString("D"), outputId = Guid.NewGuid().ToString("D");
            var skeleton = new NyaForge.Authoring.Rig.SkeletonDefinition(new[] {
                new NyaForge.Authoring.Rig.BoneDefinition(root, "Root", "", new Vec3(), new Vec3(0, .1f, 0)),
                new NyaForge.Authoring.Rig.BoneDefinition(child, "Child", root, new Vec3(.1f, 0, 0), new Vec3(.1f, .1f, 0)) });
            var mesh = PrimitiveGeometry.Plane(.2f, .1f);
            var bindingWeights = new List<NyaForge.Authoring.Rig.SkinBinding.VertexWeightInput>();
            for (int i = 0; i < mesh.VertexCount; i++) bindingWeights.Add(new NyaForge.Authoring.Rig.SkinBinding.VertexWeightInput(i, i < 2 ? root : child, 1));
            var binding = NyaForge.Authoring.Rig.SkinBinding.Create(mesh, skeleton, bindingWeights);
            var pose = NyaForge.Authoring.Rig.PoseSet.Create(skeleton, new[] {
                new NyaForge.Authoring.Rig.BonePose(root, NyaForge.Authoring.Rig.PoseTransform.Identity),
                new NyaForge.Authoring.Rig.BonePose(child, NyaForge.Authoring.Rig.PoseTransform.RotationZ(25, new Vec3())) });
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] { GraphNode.Plane(planeId), GraphNode.SkeletonNode(skeletonId, skeleton), GraphNode.SkinBindNode(bindId, binding), GraphNode.PoseNode(poseId, pose), GraphNode.SkinDeformNode(deformId), GraphNode.Output(outputId) },
                new[] { new GraphEdge(planeId, "mesh", bindId, "mesh"), new GraphEdge(skeletonId, "skeleton", bindId, "skeleton"), new GraphEdge(skeletonId, "skeleton", poseId, "skeleton"), new GraphEdge(planeId, "mesh", deformId, "mesh"), new GraphEdge(skeletonId, "skeleton", deformId, "skeleton"), new GraphEdge(bindId, "binding", deformId, "binding"), new GraphEdge(poseId, "pose", deformId, "pose"), new GraphEdge(deformId, "mesh", outputId, "mesh") }, outputId);
            submit(new[] { AuthoringOperation.AddGraph(graph) });
        }

        // Runtime verification uses the same command path without depending on toolbar scroll position.
        internal void AddRigSampleForVerification() => AddRigSample();

        void Input(string node, string port)
        {
            if (sourceNode == null) { message.text = "先に出力ポートをクリックしてください。"; return; }
            var operations = new List<AuthoringOperation>();
            if (Graph.Edges.Any(e => e.ToNode == node && e.ToPort == port)) operations.Add(AuthoringOperation.Disconnect(node, port));
            operations.Add(AuthoringOperation.Connect(new GraphEdge(sourceNode, sourcePort, node, port)));
            submit(operations.ToArray());
        }

        void DrawWires(MeshGenerationContext context)
        {
            if (workspace == null || Graph == null) return;
            var painter = context.painter2D; painter.lineWidth = 2; painter.strokeColor = new Color(.3f, .85f, .8f);
            foreach (var edge in Graph.Edges)
            {
                if (!ports.TryGetValue("out:" + edge.FromNode + ":" + edge.FromPort, out var from) || !ports.TryGetValue("in:" + edge.ToNode + ":" + edge.ToPort, out var to)) continue;
                Vector2 a = wires.WorldToLocal(from.worldBound.center), b = wires.WorldToLocal(to.worldBound.center);
                if (!float.IsFinite(a.x) || !float.IsFinite(b.x)) continue;
                painter.BeginPath(); painter.MoveTo(a); painter.BezierCurveTo(a + Vector2.right * 70, b - Vector2.right * 70, b); painter.Stroke();
            }
        }
    }
}
