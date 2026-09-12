using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;

internal static partial class Program
{
    static void RunAttachmentGraphTests()
    {
        Test("object attachment node pins stable target and survives native graph storage", () =>
        {
            string plane = GraphId(), output = GraphId(), attachment = GraphId();
            string targetObject = GraphId(), boneId = GraphId();
            string skeletonHash = Checks.Hash(new byte[] { 1, 2, 3 });
            var graph = new AuthoringGraph(GraphId(),
                new[] { GraphNode.Plane(plane), GraphNode.Output(output), GraphNode.AttachmentNode(attachment, targetObject, boneId, skeletonHash, new Vec3(.01f, -.02f, .03f)) },
                new[] { new GraphEdge(plane, "mesh", output, "mesh") }, output);
            True(GraphEvaluator.Evaluate(graph).IsComplete);
            var node = graph.Nodes[attachment]; Equal(targetObject, node.AttachmentTargetObjectId); Equal(boneId, node.AttachmentBoneId); Equal(skeletonHash, node.AttachmentSkeletonHash); Near(.01f, node.AttachmentOffset.X);
            string dir = Dir("attachment-graph"); string hash = GraphBlobStore.Write(dir, graph); var loaded = GraphBlobStore.Read(dir, hash).Nodes[attachment];
            Equal(targetObject, loaded.AttachmentTargetObjectId); Equal(boneId, loaded.AttachmentBoneId); Equal(skeletonHash, loaded.AttachmentSkeletonHash); Near(.03f, loaded.AttachmentOffset.Z);
            Equal(hash, GraphBlobStore.Write(dir, GraphBlobStore.Read(dir, hash)));
        });
        Test("object attachment node rejects missing stable identities", () =>
        {
            string bone = GraphId(), target = GraphId(), hash = Checks.Hash(new byte[] { 9 });
            Expect("INVALID_ID", () => GraphNode.AttachmentNode(GraphId(), "", bone, hash, new Vec3()));
            Expect("INVALID_ID", () => GraphNode.AttachmentNode(GraphId(), target, "", hash, new Vec3()));
            Expect("INVALID_HASH", () => GraphNode.AttachmentNode(GraphId(), target, bone, "bad", new Vec3()));
            Expect("NON_FINITE", () => GraphNode.AttachmentNode(GraphId(), target, bone, hash, new Vec3(float.NaN, 0, 0)));
        });
    }
}
