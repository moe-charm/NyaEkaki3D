using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Inspection;
using NyaForge.Authoring.Rig;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    static void RunMorphGraphTests()
    {
        Test("morph graph node evaluates, inspects and survives native storage", () =>
        {
            string planeId = GraphId(), morphId = GraphId(), deformId = GraphId(), outputId = GraphId(), targetId = GraphId();
            var mesh = PrimitiveGeometry.Plane(.2f, .1f);
            var morphs = MorphSet.Create(mesh, new[] { MorphTarget.Create(mesh, targetId, "Smile", new[] { new MorphDelta(0, new Vec3(.1f, 0, 0)) }) });
            var graph = new AuthoringGraph(GraphId(), new[] { GraphNode.Plane(planeId), GraphNode.MorphSetNode(morphId, morphs), GraphNode.MorphDeformNode(deformId, new Dictionary<string, float> { [targetId] = .5f }), GraphNode.Output(outputId) },
                new[] { new GraphEdge(planeId, "mesh", deformId, "mesh"), new GraphEdge(morphId, "morphs", deformId, "morphs"), new GraphEdge(deformId, "mesh", outputId, "mesh") }, outputId);
            var workspace = AuthoringWorkspace.CreateEmpty(); Ok(new AuthoringCommandService(workspace).Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph))));
            True(workspace.Preview.Evaluation.MorphSetOutputs.ContainsKey(morphId)); True(workspace.Preview.Evaluation.MeshOutputs.ContainsKey(deformId));
            Near(mesh.Positions[0].X + .05f, workspace.Preview.Evaluation.MeshOutputs[deformId].Mesh.Positions[0].X);
            var info = AuthoringGraphReader.Read(workspace, workspace.InstanceId)["graph"]["nodes"].Children<JObject>().Single(n => (string)n["nodeId"] == morphId)["morphOutput"];
            Equal(morphs.ContentHash, (string)info["morphHash"]); Equal(1, (int)info["targetCount"]); Equal(1, (int)info["targets"][0]["deltaCount"]);
            string directory = Dir("morph-graph-native"); ProjectStore.Save(directory, workspace, 0); var reopened = ProjectStore.Open(directory);
            True(reopened.Preview.Evaluation.MorphSetOutputs.ContainsKey(morphId)); True(reopened.Preview.Evaluation.MeshOutputs.ContainsKey(deformId));
            True(reopened.Preview.Evaluation.MeshOutputs[deformId].Mesh.Positions.SequenceEqual(workspace.Preview.Evaluation.MeshOutputs[deformId].Mesh.Positions));
        });

        Test("morph graph refuses a changed mesh topology", () =>
        {
            string planeId = GraphId(), morphId = GraphId(), deformId = GraphId(), outputId = GraphId(), targetId = GraphId();
            var mesh = PrimitiveGeometry.Plane(.2f, .1f); var morphs = MorphSet.Create(mesh, new[] { MorphTarget.Create(mesh, targetId, "Blink", new[] { new MorphDelta(0, new Vec3(0, .1f, 0)) }) });
            var changed = new MeshData(mesh.Positions.ToArray(), mesh.Normals.ToArray(), mesh.Tangents.ToArray(), mesh.Uv0.ToArray(), new[] { new[] { 0, 1, 2, 0, 2, 3 } });
            var source = GraphNode.Source(planeId, changed, new RestTransform(1, new Vec3()));
            var graph = new AuthoringGraph(GraphId(), new[] { source, GraphNode.MorphSetNode(morphId, morphs), GraphNode.MorphDeformNode(deformId, new Dictionary<string, float> { [targetId] = 1 }), GraphNode.Output(outputId) },
                new[] { new GraphEdge(planeId, "mesh", deformId, "mesh"), new GraphEdge(morphId, "morphs", deformId, "morphs"), new GraphEdge(deformId, "mesh", outputId, "mesh") }, outputId);
            var evaluation = GraphEvaluator.Evaluate(graph); True(evaluation.Diagnostics.Any(d => d.Code == "MORPH_TOPOLOGY_CHANGED")); True(!evaluation.MeshOutputs.ContainsKey(deformId));
        });
    }
}
