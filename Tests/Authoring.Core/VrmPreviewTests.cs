using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;

internal static partial class Program
{
    static (VrmSpringSession Spring, ImportedRigSession Rig, AuthoringGraph Graph, PoseSet Pose) VrmPreviewFixture()
    {
        var bytes = BuildMappedVrm(false); var json = JObject.Parse(ReadJsonChunk(bytes));
        json["nodes"][1]["translation"] = new JArray(1, 0, 0);
        json["nodes"][2]["translation"] = new JArray(0, .3, 0);
        json["extensions"]["VRMC_springBone"] = new JObject {
            ["specVersion"] = "1.0",
            ["colliders"] = new JArray(new JObject { ["node"] = 2, ["shape"] = new JObject { ["sphere"] = new JObject { ["radius"] = .05 } } }),
            ["colliderGroups"] = new JArray(new JObject { ["colliders"] = new JArray(0) }),
            ["springs"] = new JArray(new JObject { ["center"] = 1, ["colliderGroups"] = new JArray(0), ["joints"] = new JArray(
                new JObject { ["node"] = 1, ["stiffness"] = 2, ["gravityPower"] = .2, ["gravityDir"] = new JArray(1, 0, 0) }, new JObject { ["node"] = 2 }) }) };
        bytes = ReplaceJsonChunk(bytes, json.ToString()); var source = GlbSkinImporter.Read(bytes); var metadata = VrmMetadataReader.Read(bytes);
        string sid = Guid.NewGuid().ToString("D"), mid = Guid.NewGuid().ToString("D"), output = Guid.NewGuid().ToString("D");
        var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] { GraphNode.SkeletonNode(sid, source.Skeleton), GraphNode.Source(mid, source.Mesh, new RestTransform(1, new Vec3())), GraphNode.Output(output) }, new[] { new GraphEdge(mid, "mesh", output, "mesh") }, output);
        var rig = ImportedRigSessionCodec.Read(ImportedRigSessionCodec.Write(ImportedRigSession.Create(source, metadata, graph.GraphId, sid)));
        var springs = VrmSpringSessionCodec.Read(VrmSpringSessionCodec.Write(VrmSpringSession.Create(metadata)));
        var pose = PoseSet.Create(source.Skeleton, source.Skeleton.Bones.Select(b => new BonePose(b.BoneId, PoseTransform.FromTranslation(b.Head))));
        return (springs, rig, graph, pose);
    }

    static void RunVrmPreviewTests()
    {
        Test("VRM file sessions drive center-aware preview without changing authored data", () =>
        {
            var f = VrmPreviewFixture(); var preview = new Vrm1SpringPreview(f.Spring, f.Rig, f.Graph, f.Pose);
            var comparison = new Vrm1SpringPreview(f.Spring, f.Rig, f.Graph, f.Pose);
            string sourcePose = f.Pose.ContentHash;
            preview.Play(); comparison.Play(); preview.Advance(f.Graph, f.Pose, .1f); comparison.Advance(f.Graph, f.Pose, .1f);
            Equal(6L, preview.CompletedSteps);
            var state = preview.State; preview.Pause();
            var skeleton = f.Graph.Nodes[f.Rig.SkeletonNodeId].Skeleton; var offset = new Vec3(2, 1, 0);
            var moved = PoseSet.Create(skeleton, f.Pose.Poses.Select(b => new BonePose(b.BoneId, PoseTransform.FromTranslation(b.Transform.Translation + offset))));
            preview.Advance(f.Graph, moved, .2f); preview.Advance(f.Graph, moved, .2f);
            True(ReferenceEquals(state, preview.State));
            preview.Play(); preview.Advance(f.Graph, moved, 1f / 60); comparison.Advance(f.Graph, moved, 1f / 60);
            Equal(comparison.Pose.ContentHash, preview.Pose.ContentHash);
            var head = f.Rig.NodeToBone[1];
            SpringPointNear(new Vec3(3, 1, 0), preview.Pose.ByBoneId[head].Transform.TransformPoint(new Vec3(1, 0, 0)));
            Equal(sourcePose, f.Pose.ContentHash);
        });
        Test("VRM preview refreshes collider poses and requires reset for scale changes", () =>
        {
            var f = VrmPreviewFixture(); var normal = new Vrm1SpringPreview(f.Spring, f.Rig, f.Graph, f.Pose);
            var movedCollider = new Vrm1SpringPreview(f.Spring, f.Rig, f.Graph, f.Pose);
            var skeleton = f.Graph.Nodes[f.Rig.SkeletonNodeId].Skeleton; var colliderBone = f.Rig.NodeToBone[2];
            var moved = PoseSet.Create(skeleton, f.Pose.Poses.Select(b => new BonePose(b.BoneId, PoseTransform.FromTranslation(b.Transform.Translation + (b.BoneId == colliderBone ? new Vec3(10, 0, 0) : new Vec3())))));
            normal.Play(); movedCollider.Play(); normal.Advance(f.Graph, f.Pose, .02f); movedCollider.Advance(f.Graph, moved, .02f);
            True(Distance(normal.State.CurrentTails[f.Rig.NodeToBone[1]], movedCollider.State.CurrentTails[f.Rig.NodeToBone[1]]) > .01f);
            var state = normal.State; var output = normal.Pose;
            var scaled = PoseSet.Create(skeleton, f.Pose.Poses.Select(b => new BonePose(b.BoneId, new PoseTransform(new Vec3(2, 0, 0), new Vec3(0, 2, 0), new Vec3(0, 0, 2), b.Transform.Translation * 2))));
            Expect("SPRING_SCALE_CHANGED", () => normal.Advance(f.Graph, scaled, .02f));
            True(ReferenceEquals(state, normal.State) && ReferenceEquals(output, normal.Pose));
            normal.Reset(f.Graph, scaled); Equal(0L, normal.CompletedSteps); False(normal.IsPlaying);
            var reset = normal.State;
            var changedSkeleton = SkeletonEditing.MoveBone(skeleton, f.Rig.NodeToBone[1], new Vec3(.1f, 0, 0), new Vec3(.1f, 0, 0));
            var changedGraph = f.Graph.ReplaceNode(GraphNode.SkeletonNode(f.Rig.SkeletonNodeId, changedSkeleton));
            Expect("IMPORT_SKELETON_CHANGED", () => normal.Reset(changedGraph, scaled));
            True(ReferenceEquals(reset, normal.State));
        });
    }
}
