using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;

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

        Test("source skin graph adapter projects the skin input instead of authored SkinDeform output", () =>
        {
            var bytes = BuildMappedVrm(false); var root = Newtonsoft.Json.Linq.JObject.Parse(ReadJsonChunk(bytes));
            ((Newtonsoft.Json.Linq.JObject)root["nodes"]![0]!)!["translation"] = new Newtonsoft.Json.Linq.JArray(1, 2, 3);
            bytes = ReplaceJsonChunk(bytes, root.ToString());
            var instance = GlbSceneInventoryReader.Read(bytes).Instances.Single();
            var imported = GlbSkinImporter.Read(bytes, 0, 0, instance.WorldTransform); var source = GlbSourceSkinImporter.Read(bytes);
            string meshId = Guid.NewGuid().ToString("D"), skeletonId = Guid.NewGuid().ToString("D"), bindId = Guid.NewGuid().ToString("D"), poseId = Guid.NewGuid().ToString("D"), deformId = Guid.NewGuid().ToString("D"), outputId = Guid.NewGuid().ToString("D");
            var pose = PoseSet.Create(imported.Skeleton, imported.Skeleton.Bones.Select(b => new BonePose(b.BoneId, PoseTransform.FromTranslation(b.Head))));
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] {
                GraphNode.Source(meshId, imported.Mesh, new RestTransform(1, new Vec3())), GraphNode.SkeletonNode(skeletonId, imported.Skeleton),
                GraphNode.SkinBindNode(bindId, imported.Binding), GraphNode.PoseNode(poseId, pose), GraphNode.SkinDeformNode(deformId), GraphNode.Output(outputId) },
                new[] { new GraphEdge(meshId, "mesh", bindId, "mesh"), new GraphEdge(skeletonId, "skeleton", bindId, "skeleton"),
                    new GraphEdge(skeletonId, "skeleton", poseId, "skeleton"), new GraphEdge(meshId, "mesh", deformId, "mesh"),
                    new GraphEdge(skeletonId, "skeleton", deformId, "skeleton"), new GraphEdge(bindId, "binding", deformId, "binding"),
                    new GraphEdge(poseId, "pose", deformId, "pose"), new GraphEdge(deformId, "mesh", outputId, "mesh") }, outputId);
            var session = ImportedRigSession.Create(imported, null, graph.GraphId, skeletonId).WithSourceSkin(source.Skin, source.Binding);
            var evaluation = GraphEvaluator.Evaluate(graph); True(evaluation.IsComplete);
            var projected = SourceSkinGraphAdapter.ApplyToEvaluation(evaluation, graph, session);
            for (int i = 0; i < imported.Mesh.VertexCount; i++) SpringPointNear(instance.WorldTransform.TransformPoint(imported.Mesh.Positions[i]), projected.Mesh.Positions[i]);
            Equal(evaluation.Output.DomainId, projected.DomainId); Equal(evaluation.Output.Transform.Scale, projected.Transform.Scale);
            var moved = PoseSet.Create(imported.Skeleton, imported.Skeleton.Bones.Select((b, i) => new BonePose(b.BoneId, i == 0 ? PoseTransform.RotationZ(25, b.Head) : PoseTransform.FromTranslation(b.Head))));
            var movedGraph = graph.ReplaceNode(GraphNode.PoseNode(poseId, moved)); var movedEvaluation = GraphEvaluator.Evaluate(movedGraph);
            var movedSession = ImportedRigSession.Create(imported, null, movedGraph.GraphId, skeletonId).WithSourceSkin(source.Skin, source.Binding);
            var movedProjected = SourceSkinGraphAdapter.ApplyToEvaluation(movedEvaluation, movedGraph, movedSession);
            True(movedProjected.Mesh.ContentHash != projected.Mesh.ContentHash);

            string secondDeformId = Guid.NewGuid().ToString("D"), secondOutputId = Guid.NewGuid().ToString("D");
            var multiGraph = new AuthoringGraph(Guid.NewGuid().ToString("D"),
                graph.Nodes.Values.Concat(new[] { GraphNode.SkinDeformNode(secondDeformId), GraphNode.Output(secondOutputId) }),
                graph.Edges.Concat(new[] {
                    new GraphEdge(meshId, "mesh", secondDeformId, "mesh"), new GraphEdge(skeletonId, "skeleton", secondDeformId, "skeleton"),
                    new GraphEdge(bindId, "binding", secondDeformId, "binding"), new GraphEdge(poseId, "pose", secondDeformId, "pose"),
                    new GraphEdge(secondDeformId, "mesh", secondOutputId, "mesh") }), secondOutputId);
            var multiEvaluation = GraphEvaluator.Evaluate(multiGraph); True(multiEvaluation.IsComplete);
            var multiSession = ImportedRigSession.Create(imported, null, multiGraph.GraphId, skeletonId).WithSourceSkin(source.Skin, source.Binding);
            var multiProjected = SourceSkinGraphAdapter.ApplyToEvaluation(multiEvaluation, multiGraph, multiSession);
            Equal(multiEvaluation.Output.DomainId, multiProjected.DomainId); True(multiProjected.Mesh != null);
        });
    }
}
