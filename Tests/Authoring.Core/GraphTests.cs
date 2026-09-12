using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;

internal static partial class Program
{
    static string GraphId() { return Guid.NewGuid().ToString("D"); }
    static AuthoringGraph PlaneGraph(out string plane, out string edit)
    {
        plane = GraphId(); edit = GraphId(); string output = GraphId();
        return new AuthoringGraph(GraphId(), new[] { GraphNode.Plane(plane), GraphNode.Edit(edit), GraphNode.Output(output) },
            new[] { new GraphEdge(plane,"mesh",edit,"mesh"), new GraphEdge(edit,"mesh",output,"mesh") }, output);
    }
    static void RunGraphTests()
    {
        Test("typed graph plane and edit preserve attributes and source", () =>
        {
            string plane, edit; var graph = PlaneGraph(out plane, out edit);
            var original = GraphEvaluator.Evaluate(graph); True(original.IsComplete);
            Equal(4, original.Output.Mesh.VertexCount); Equal(2, original.Output.Mesh.TriangleCount);
            var context = GraphEditing.Context(graph, edit);
            var changed = GraphEditing.Translate(graph, context, new[] { 0 }, new Vec3(.01f,0,0));
            var result = GraphEvaluator.Evaluate(changed); True(result.IsComplete);
            Near(-.09f, result.Output.Mesh.Positions[0].X); Near(-.1f, original.Output.Mesh.Positions[0].X);
            Equal(original.Output.Mesh.TopologyHash, result.Output.Mesh.TopologyHash);
            Equal(4, result.Output.Mesh.Uv0.Count); Equal(-1f, result.Output.Mesh.Normals[0].Z);
            Equal(original.Output.Mesh.ContentHash, GraphEvaluator.Evaluate(graph).Output.Mesh.ContentHash);
        });
        Test("unbound imported image can feed a material on a render-index mesh", () =>
        {
            string source = GraphId(), image = GraphId(), red = GraphId(), green = GraphId(), assign = GraphId(), output = GraphId();
            var mesh = AuthoringFixtures.Panel(1); var imagePayload = new PaintImage(2, 2, new Rgba32(220, 30, 60, 255));
            var nodes = new[] { GraphNode.Source(source, mesh, new RestTransform(1, new Vec3())), GraphNode.Paint(image, 2, 2, imagePayload), GraphNode.StandardMaterial(red), GraphNode.StandardMaterial(green), GraphNode.AssignMaterials(assign, new[] { 0, 1 }), GraphNode.Output(output) };
            var edges = new[]
            {
                new GraphEdge(source, "mesh", image, "mesh"), new GraphEdge(image, "image", red, "baseColor"), new GraphEdge(image, "image", green, "baseColor"),
                new GraphEdge(source, "mesh", assign, "mesh"), new GraphEdge(red, "material", assign, GraphNode.MaterialSlotPort(0)), new GraphEdge(green, "material", assign, GraphNode.MaterialSlotPort(1)),
                new GraphEdge(assign, "mesh", output, "mesh")
            };
            var evaluation = GraphEvaluator.Evaluate(new AuthoringGraph(GraphId(), nodes, edges, output));
            True(evaluation.IsComplete); Equal(2, evaluation.Output.SlotMaterials.Count); Equal(imagePayload.Width, evaluation.ImageOutputs[image].Image.Width); True(evaluation.Output.SlotMaterials[0].Material.BaseColor.ImageHash == evaluation.Output.SlotMaterials[1].Material.BaseColor.ImageHash);
        });
        Test("scalar mesh ports reject wrong types and evaluate parameters", () =>
        {
            string number = GraphId(), plane = GraphId(), output = GraphId();
            var nodes = new[] { GraphNode.Number(number, .4f), GraphNode.Plane(plane), GraphNode.Output(output) };
            var graph = new AuthoringGraph(GraphId(), nodes, new[] { new GraphEdge(number,"value",plane,"width"), new GraphEdge(plane,"mesh",output,"mesh") }, output);
            var result = GraphEvaluator.Evaluate(graph); True(result.IsComplete); Near(.2f, result.Output.Mesh.Positions[1].X);
            Expect("PORT_TYPE_MISMATCH", () => new AuthoringGraph(GraphId(), nodes, new[] { new GraphEdge(number,"value",output,"mesh") }, output));
            Expect("PORT_NOT_FOUND", () => new AuthoringGraph(GraphId(), nodes, new[] { new GraphEdge(plane,"imaginary",output,"mesh") }, output));
        });
        Test("cycles are rejected even outside selected output", () =>
        {
            string plane, edit; var good = PlaneGraph(out plane, out edit);
            string a = GraphId(), b = GraphId();
            var nodes = good.Nodes.Values.Concat(new[] { GraphNode.Edit(a), GraphNode.Edit(b) });
            var edges = good.Edges.Concat(new[] { new GraphEdge(a,"mesh",b,"mesh"), new GraphEdge(b,"mesh",a,"mesh") });
            Expect("GRAPH_CYCLE", () => new AuthoringGraph(good.GraphId,nodes,edges,good.OutputNodeId));
            True(GraphEvaluator.Evaluate(good).IsComplete);
        });
        Test("graph identity and duplicate input validation", () =>
        {
            string plane, edit; var graph = PlaneGraph(out plane, out edit);
            Expect("INPUT_ALREADY_CONNECTED", () => graph.WithEdges(graph.Edges.Concat(new[] { new GraphEdge(plane,"mesh",edit,"mesh") })));
            Expect("DUPLICATE_NODE", () => new AuthoringGraph(GraphId(), graph.Nodes.Values.Concat(new[] { graph.Nodes[plane] }), graph.Edges, graph.OutputNodeId));
            Expect("NODE_NOT_FOUND", () => graph.WithEdges(new[] { new GraphEdge(GraphId(),"mesh",edit,"mesh") }));
        });
        Test("missing input stays incomplete while independent node preview remains", () =>
        {
            string plane, edit; var graph = PlaneGraph(out plane, out edit);
            var disconnected = graph.WithEdges(graph.Edges.Where(e => e.ToNode != edit));
            var result = GraphEvaluator.Evaluate(disconnected);
            False(result.IsComplete); True(result.Output == null);
            True(result.MeshOutputs.ContainsKey(plane)); True(result.Diagnostics.Any(d => d.Code == "INPUT_MISSING"));
            True(GraphEvaluator.Evaluate(graph).IsComplete);
        });
        Test("unknown type and future version retain payload without execution", () =>
        {
            foreach (var type in new[] { "vendor.future", BuiltinNodes.Plane })
            {
                string node = GraphId(), output = GraphId(); string raw = "{\"parameter\":42,\"preserved\":true}";
                var unknown = GraphNode.Unknown(node, type, 99, raw);
                var graph = new AuthoringGraph(GraphId(), new[] { unknown, GraphNode.Output(output) }, new[] { new GraphEdge(node,"mesh",output,"mesh") }, output);
                var result = GraphEvaluator.Evaluate(graph); False(result.IsComplete);
                Equal(raw, graph.Nodes[node].UnknownPayload); True(result.Diagnostics.Any(d => d.Code == "UNKNOWN_NODE"));
                Equal(2, graph.Nodes.Count);
            }
        });
        Test("upstream parameter change invalidates payload and stale edit context", () =>
        {
            string plane, edit; var graph = PlaneGraph(out plane, out edit);
            var context = GraphEditing.Context(graph, edit);
            var edited = GraphEditing.Translate(graph, context, new[] { 0 }, new Vec3(.01f,0,0));
            var changed = edited.ReplaceNode(GraphNode.Plane(plane,.4f,.1f));
            var result = GraphEvaluator.Evaluate(changed);
            False(result.IsComplete); True(result.Diagnostics.Any(d => d.Code == "EDIT_INPUT_CHANGED"));
            Equal(1, changed.Nodes[edit].Offsets.Count);
            Expect("EDIT_CONTEXT_STALE", () => GraphEditing.Translate(changed, context, new[] { 0 }, new Vec3(.01f,0,0)));
            var freshContext = GraphEditing.Context(changed, edit);
            Expect("EDIT_INPUT_CHANGED", () => GraphEditing.Translate(changed, freshContext, new[] { 0 }, new Vec3(.01f,0,0)));
        });
        Test("same geometry from different source domain cannot inherit offsets", () =>
        {
            string plane, edit; var graph = PlaneGraph(out plane, out edit);
            var edited = GraphEditing.Translate(graph, GraphEditing.Context(graph,edit), new[] { 0 }, new Vec3(.01f,0,0));
            string replacement = GraphId();
            var nodes = edited.Nodes.Values.Where(n => n.NodeId != plane).Concat(new[] { GraphNode.Plane(replacement) });
            var swapped = new AuthoringGraph(graph.GraphId,nodes,new[] { new GraphEdge(replacement,"mesh",edit,"mesh"), new GraphEdge(edit,"mesh",graph.OutputNodeId,"mesh") },graph.OutputNodeId);
            True(GraphEvaluator.Evaluate(swapped).Diagnostics.Any(d => d.Code == "EDIT_DOMAIN_CHANGED"));
        });
        Test("graph invalid edit candidates leave original intact", () =>
        {
            string plane, edit; var graph = PlaneGraph(out plane,out edit); var context = GraphEditing.Context(graph,edit);
            string hash = GraphEvaluator.Evaluate(graph).Output.Mesh.ContentHash;
            Expect("INVALID_VERTEX", () => GraphEditing.Translate(graph,context,new[] { -1 },new Vec3()));
            Expect("INVALID_SELECTION", () => GraphEditing.Translate(graph,context,new[] { 0,0 },new Vec3()));
            Expect("DEGENERATE_TRIANGLE", () => GraphEditing.Translate(graph,context,new[] { 0 },new Vec3(.2f,0,0)));
            Equal(hash,GraphEvaluator.Evaluate(graph).Output.Mesh.ContentHash); Equal(0,graph.Nodes[edit].Offsets.Count);
        });
        Test("legacy static profile uses deterministic graph and rest-space deltas", () =>
        {
            foreach (float scale in new[] { 1f,100f })
            {
                var w = AuthoringWorkspace.CreateFixture(scale); Ok(Edit(w,0,new Vec3(.01f,0,0)));
                var graph = w.Document.Objects[0].Graph; var result = GraphEvaluator.Evaluate(graph);
                True(result.IsComplete); Near(-.09f,result.Output.Transform.ToAvatarPoint(result.Output.Mesh.Positions[0]).X);
                Equal(w.Evaluate().ContentHash,result.Output.Mesh.ContentHash);
                string dir = Dir("graph-profile" + scale); ProjectStore.Save(dir,w,0);
                var reopened = ProjectStore.Open(dir).Document.Objects[0].Graph;
                Equal(graph.GraphId,reopened.GraphId); Equal(graph.OutputNodeId,reopened.OutputNodeId);
                Equal(result.Output.SnapshotHash,GraphEvaluator.Evaluate(reopened).Output.SnapshotHash);
            }
        });
        Test("graph budgets and invalid primitive parameters fail before allocation", () =>
        {
            Expect("PARAMETER_RANGE", () => GraphNode.Plane(GraphId(),0,.1f));
            Expect("NON_FINITE", () => GraphNode.Plane(GraphId(),float.NaN,.1f));
            Expect("BUDGET_EXCEEDED", () => new AuthoringGraph(GraphId(),Enumerable.Range(0,AuthoringGraph.MaxNodes+1).Select(_ => GraphNode.Number(GraphId(),1)),Array.Empty<GraphEdge>(),""));
        });
    }
}
