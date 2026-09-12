using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;

internal static partial class Program
{
    static void RunSourceSkinGraphAdapterTests()
    {
        Test("source skin graph adapter replaces mesh and retains graph value identity", () =>
        {
            var value = SimpleSourceSkin();
            string sourceId = Guid.NewGuid().ToString("D"), outputId = Guid.NewGuid().ToString("D");
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"),
                new[] { GraphNode.Source(sourceId, value.mesh, new RestTransform(2, new Vec3(3, 4, 5))), GraphNode.Output(outputId) },
                new[] { new GraphEdge(sourceId, "mesh", outputId, "mesh") }, outputId);
            var evaluated = GraphEvaluator.Evaluate(graph);
            True(evaluated.IsComplete);
            var posed = new[] { value.skin.Nodes.World[0],
                SourceAffine.FromTrs(new Vec3(1, .2f, 0), new Vec4(0, 0, 0, 1), new Vec3(1, 1, 1)) };
            var deformed = SourceSkinGraphAdapter.Apply(evaluated.Output, value.skin, value.binding, posed);
            True(deformed.Mesh.ContentHash != evaluated.Output.Mesh.ContentHash);
            Equal(evaluated.Output.DomainId, deformed.DomainId);
            Equal(evaluated.Output.Transform.Scale, deformed.Transform.Scale);
            Equal(evaluated.Output.Transform.Translation.X, deformed.Transform.Translation.X);
            Near(value.mesh.Positions[0].X + 1, deformed.Mesh.Positions[0].X);
            Equal(value.mesh.TopologyHash, deformed.Mesh.TopologyHash);
            Equal(value.mesh.ContentHash, evaluated.Output.Mesh.ContentHash);
        });

        Test("source skin graph adapter rejects polygon values and stale topology", () =>
        {
            var value = SimpleSourceSkin();
            var polygonId = Guid.NewGuid().ToString("D");
            var polygon = NyaForge.Authoring.Topology.PolygonPrimitives.Plane(Guid.NewGuid().ToString("D"));
            var polygonGraph = new AuthoringGraph(Guid.NewGuid().ToString("D"),
                new[] { GraphNode.Polygon(polygonId, polygon, new RestTransform(1, new Vec3())) },
                Array.Empty<GraphEdge>(), "");
            var polygonValue = GraphEvaluator.Evaluate(polygonGraph).MeshOutputs[polygonId];
            Expect("EDIT_MODE_UNSUPPORTED", () => SourceSkinGraphAdapter.Apply(polygonValue, value.skin, value.binding, new[] { value.skin.Nodes.World[0], value.skin.Nodes.World[1] }));
            var changed = new MeshData(value.mesh.Positions.ToArray(), value.mesh.Normals.ToArray(), value.mesh.Tangents.ToArray(), value.mesh.Uv0.ToArray(), new[] { new[] { 0, 1, 2, 0, 3, 2 }, new[] { 4, 6, 5, 4, 7, 6 } });
            var staleId = Guid.NewGuid().ToString("D");
            var staleGraph = new AuthoringGraph(Guid.NewGuid().ToString("D"),
                new[] { GraphNode.Source(staleId, changed, new RestTransform(1, new Vec3())) }, Array.Empty<GraphEdge>(), "");
            var staleValue = GraphEvaluator.Evaluate(staleGraph).MeshOutputs[staleId];
            Expect("SKIN_SOURCE_CHANGED", () => SourceSkinGraphAdapter.Apply(staleValue, value.skin, value.binding, new[] { value.skin.Nodes.World[0], value.skin.Nodes.World[1] }));
        });
    }
}
