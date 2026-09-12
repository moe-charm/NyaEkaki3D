using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;

internal static partial class Program
{
    static void RunSourceSkinPosePaletteTests()
    {
        Test("source pose palette cancels rest and drives graph source skin adapter", () =>
        {
            var bytes = BuildMappedVrm(false); var imported = GlbSkinImporter.Read(bytes);
            var candidate = GlbSourceSkinImporter.Read(bytes); var metadata = VrmMetadataReader.Read(bytes);
            string sourceId = Guid.NewGuid().ToString("D"), skeletonId = Guid.NewGuid().ToString("D"), outputId = Guid.NewGuid().ToString("D");
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] {
                GraphNode.Source(sourceId, imported.Mesh, new RestTransform(1, new Vec3())),
                GraphNode.SkeletonNode(skeletonId, imported.Skeleton), GraphNode.Output(outputId) },
                new[] { new GraphEdge(sourceId, "mesh", outputId, "mesh") }, outputId);
            var session = ImportedRigSession.Create(imported, metadata, graph.GraphId, skeletonId)
                .WithSourceSkin(candidate.Skin, candidate.Binding);
            var restPose = PoseSet.Create(imported.Skeleton, imported.Skeleton.Bones.Select(b => new BonePose(b.BoneId, PoseTransform.FromTranslation(b.Head))));
            var evaluated = GraphEvaluator.Evaluate(graph);
            var restOutput = SourceSkinGraphAdapter.Apply(evaluated.Output, session, graph, restPose);
            for (int i = 0; i < imported.Mesh.VertexCount; i++) SpringPointNear(imported.Mesh.Positions[i], restOutput.Mesh.Positions[i]);
            var moved = imported.Skeleton.Bones.Select((bone, index) => new BonePose(bone.BoneId,
                index == 0 ? PoseTransform.RotationZ(25, bone.Head) : PoseTransform.FromTranslation(bone.Head))).ToArray();
            var posedOutput = SourceSkinGraphAdapter.Apply(evaluated.Output, session, graph, PoseSet.Create(imported.Skeleton, moved));
            True(posedOutput.Mesh.ContentHash != restOutput.Mesh.ContentHash);
            Equal(restOutput.DomainId, posedOutput.DomainId); Equal(restOutput.Mesh.TopologyHash, posedOutput.Mesh.TopologyHash);
        });

        Test("source pose palette rejects a legacy session without complete source skin", () =>
        {
            var bytes = BuildMappedVrm(false); var imported = GlbSkinImporter.Read(bytes);
            string skeletonId = Guid.NewGuid().ToString("D");
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] { GraphNode.SkeletonNode(skeletonId, imported.Skeleton) }, Array.Empty<GraphEdge>(), "");
            var session = ImportedRigSession.Create(imported, null, graph.GraphId, skeletonId);
            var pose = PoseSet.Create(imported.Skeleton, imported.Skeleton.Bones.Select(b => new BonePose(b.BoneId, PoseTransform.FromTranslation(b.Head))));
            Expect("IMPORT_SOURCE_SKIN_MISSING", () => SourceSkinPosePalette.Build(session, graph, pose));
        });
    }
}
