using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;

internal static partial class Program
{
    static void RunVrm0SpringPreviewTests()
    {
        Test("VRM0 sessions drive source-node preview with legacy gravity and external frames", () =>
        {
            var bytes = BuildMappedVrm(true); var json = JObject.Parse(ReadJsonChunk(bytes));
            json["nodes"][1]["children"] = new JArray(3); json["nodes"][2]["children"] = new JArray(4);
            var nodes = (JArray)json["nodes"];
            nodes.Add(new JObject { ["translation"] = new JArray(0, .2, 0), ["children"] = new JArray(2) });
            nodes.Add(new JObject { ["translation"] = new JArray(0, .1, 0) });
            json["extensions"]["VRM"]["secondaryAnimation"] = new JObject {
                ["colliderGroups"] = new JArray(new JObject { ["node"] = 3, ["colliders"] = new JArray(new JObject { ["radius"] = .1, ["offset"] = new JObject { ["x"] = 10, ["y"] = 0, ["z"] = 1 } }) }),
                ["boneGroups"] = new JArray(new JObject { ["bones"] = new JArray(2), ["center"] = 3, ["colliderGroups"] = new JArray(0), ["stiffiness"] = 2, ["gravityPower"] = .2,
                    ["gravityDir"] = new JObject { ["x"] = 0, ["y"] = 0, ["z"] = 1 } }) };
            bytes = ReplaceJsonChunk(bytes, json.ToString()); var skin = GlbSkinImporter.Read(bytes); var metadata = VrmMetadataReader.Read(bytes);
            string sid = Guid.NewGuid().ToString("D"), mid = Guid.NewGuid().ToString("D"), output = Guid.NewGuid().ToString("D");
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] { GraphNode.SkeletonNode(sid, skin.Skeleton), GraphNode.Source(mid, skin.Mesh, new RestTransform(1, new Vec3())), GraphNode.Output(output) }, new[] { new GraphEdge(mid, "mesh", output, "mesh") }, output);
            var rig = ImportedRigSessionCodec.Read(ImportedRigSessionCodec.Write(ImportedRigSession.Create(skin, metadata, graph.GraphId, sid)));
            var source = VrmSpringSessionCodec.Read(VrmSpringSessionCodec.Write(VrmSpringSession.Create(metadata)));
            var pose = PoseSet.Create(skin.Skeleton, skin.Skeleton.Bones.Select(b => new BonePose(b.BoneId, PoseTransform.FromTranslation(b.Head))));
            var runtime = new Vrm0SpringRuntime(source, rig, graph); var input = runtime.Rig.FromAuthored(graph, pose);
            Equal(4, runtime.Rig.ByNode.Count); True(runtime.Rig.ByNode.ContainsKey(3) && runtime.Rig.ByNode.ContainsKey(4));
            SpringPointNear(new Vec3(0, 0, -1), runtime.Chains(input)[0].Joints[0].GravityDirection);
            SpringPointNear(new Vec3(10, .2f, -1), runtime.Colliders(input)[0].Colliders[0].Center);
            SpringPointNear(new Vec3(0, .2f, 0), runtime.Centers(input)[runtime.Rig.ByNode[2]].Translation);
            var owner = new Vrm0SpringPreview(source, rig, graph, pose); owner.Play();
            owner.Advance(graph, pose, .2f); Equal(12L, owner.CompletedSteps);
            True(owner.Pose.ContentHash != pose.ContentHash); Equal(pose.SkeletonHash, owner.Pose.SkeletonHash);
            owner.Pause(); var paused = owner.State; owner.Advance(graph, pose, .1f); True(ReferenceEquals(paused, owner.State));
            owner.Play(); owner.Advance(graph, pose, 1f / 60); Equal(13L, owner.CompletedSteps);
            var prior = owner.State; var priorPose = owner.Pose;
            Expect("INVALID_DELTA_TIME", () => owner.Advance(graph, pose, 1));
            True(ReferenceEquals(prior, owner.State) && ReferenceEquals(priorPose, owner.Pose));
            var moved = PoseSet.Create(skin.Skeleton, skin.Skeleton.Bones.Select(b => new BonePose(b.BoneId,
                PoseTransform.FromTranslation(b.Head + new Vec3(.5f, 0, 0)))));
            var movedInput = runtime.Rig.FromAuthored(graph, moved);
            SpringPointNear(new Vec3(.5f, .2f, 0), runtime.Centers(movedInput)[runtime.Rig.ByNode[2]].Translation);
            SpringPointNear(new Vec3(10.5f, .2f, -1), runtime.Colliders(movedInput)[0].Colliders[0].Center);
            owner.Pause(); var beforeCenter = owner.State; owner.Advance(graph, moved, .1f); True(ReferenceEquals(beforeCenter, owner.State));
            owner.Play(); owner.Advance(graph, moved, 1f / 60); Equal(14L, owner.CompletedSteps);
            var scaled = PoseSet.Create(skin.Skeleton, skin.Skeleton.Bones.Select(b => new BonePose(b.BoneId,
                new PoseTransform(new Vec3(2, 0, 0), new Vec3(0, 2, 0), new Vec3(0, 0, 2), b.Head))));
            var beforeScale = owner.State;
            Expect("SPRING_SCALE_CHANGED", () => owner.Advance(graph, scaled, 1f / 60)); True(ReferenceEquals(beforeScale, owner.State));
            owner.Reset(graph, scaled); Equal(0L, owner.CompletedSteps);
            owner.Reset(graph, pose); Equal(0L, owner.CompletedSteps); True(!owner.IsPlaying);
            SpringPointNear(pose.ByBoneId[rig.NodeToBone[2]].Transform.Translation, owner.Pose.ByBoneId[rig.NodeToBone[2]].Transform.Translation);
        });
    }
}
