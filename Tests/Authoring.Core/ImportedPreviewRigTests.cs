using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;

internal static partial class Program
{
    static void RunImportedPreviewRigTests()
    {
        var skeleton = new SkeletonDefinition(new[] {
            new BoneDefinition(RootBone, "root", "", new Vec3(), new Vec3(0, .1f, 0)),
            new BoneDefinition(ChildBone, "child", RootBone, new Vec3(0, 2, 0), new Vec3(0, 2.1f, 0)) });
        string sid = Guid.NewGuid().ToString("D"), mid = Guid.NewGuid().ToString("D"), output = Guid.NewGuid().ToString("D");
        var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] { GraphNode.SkeletonNode(sid, skeleton), GraphNode.Source(mid, AuthoringFixtures.Panel(1), new RestTransform(1, new Vec3())), GraphNode.Output(output) }, new[] { new GraphEdge(mid, "mesh", output, "mesh") }, output);
        var hierarchy = new ImportedSourceHierarchy(new[] { -1, 0, 1, -1 }, new[] { new Vec3(1, 0, 0), new Vec3(1, 1, 0), new Vec3(1, 2, 0), new Vec3(10, 0, 0) });
        var rig = new ImportedRigSession(new string('b', 64), skeleton.ContentHash, graph.GraphId, sid,
            new Dictionary<int, string> { [0] = RootBone, [2] = ChildBone }, new Dictionary<string, int>(),
            new Dictionary<int, Vec3> { [0] = hierarchy.Origins[0], [2] = hierarchy.Origins[2] }, hierarchy);
        Test("Preview rig preserves inverse-bind offsets and adds only required source ancestry", () =>
        {
            var preview = new ImportedPreviewRig(rig, graph, new[] { 1 });
            Equal(3, preview.Skeleton.Bones.Count); True(!preview.ByNode.ContainsKey(3));
            Equal(preview.ByNode[1], preview.Skeleton.ById[ChildBone].ParentBoneId);
            Equal(preview.Skeleton.ContentHash, new ImportedPreviewRig(rig, graph, new[] { 1, 1 }).Skeleton.ContentHash);
            var pose = PoseSet.Create(skeleton, skeleton.Bones.Select(b => new BonePose(b.BoneId, PoseTransform.RotationZ(90, b.Head))));
            var transformed = preview.FromAuthored(graph, pose); var restored = preview.ToAuthored(graph, transformed);
            foreach (var bone in skeleton.Bones)
            {
                SpringPointNear(pose.ByBoneId[bone.BoneId].Transform.Translation, restored.ByBoneId[bone.BoneId].Transform.Translation);
                SpringPointNear(pose.ByBoneId[bone.BoneId].Transform.XAxis, restored.ByBoneId[bone.BoneId].Transform.XAxis);
            }
            SpringPointNear(new Vec3(-1, 1, 0), transformed.ByBoneId[preview.ByNode[1]].Transform.Translation);
            var withSceneNode = new ImportedPreviewRig(rig, graph, new[] { 3 });
            SpringPointNear(new Vec3(10, 0, 0), withSceneNode.FromAuthored(graph, pose).ByBoneId[withSceneNode.ByNode[3]].Transform.Translation);
            var changed = graph.ReplaceNode(GraphNode.SkeletonNode(sid, SkeletonEditing.MoveBone(skeleton, RootBone, new Vec3(.1f, 0, 0), new Vec3(.1f, 0, 0))));
            Expect("IMPORT_SKELETON_CHANGED", () => preview.FromAuthored(changed, pose));
            Expect("IMPORT_SKELETON_CHANGED", () => preview.ToAuthored(changed, transformed));
        });
        Test("Simulating a nonjoint helper moves the authored child without changing its skeleton", () =>
        {
            var preview = new ImportedPreviewRig(rig, graph, new[] { 1 });
            var authoredPose = PoseSet.Create(skeleton, skeleton.Bones.Select(b => new BonePose(b.BoneId, PoseTransform.FromTranslation(b.Head))));
            var input = preview.FromAuthored(graph, authoredPose); string helper = preview.ByNode[1];
            var chains = new[] { new SpringBoneChain("helper", new[] { new SpringBoneJointSettings(helper, 0, 0, 1, new Vec3(1, 0, 0), .2f, new Vec3(0, 1, 0), new Vec3(), SpringIntegrationMode.VrmReference) }, Array.Empty<int>()) };
            var colliders = Array.Empty<SpringBoneColliderGroup>();
            var state = SpringBoneSimulator.CreateInitialState(preview.Skeleton, input, chains, colliders);
            PoseSet simulated = input;
            for (int i = 0; i < 12; i++) { var step = SpringBoneSimulator.Step(preview.Skeleton, input, chains, colliders, state, 1f / 60); state = step.State; simulated = step.Pose; }
            var projected = preview.ToAuthored(graph, simulated);
            True(Math.Abs(projected.ByBoneId[ChildBone].Transform.XAxis.Y) > .01f);
            SpringPointNear(new Vec3(1, 1, 0), simulated.ByBoneId[helper].Transform.Translation);
            SpringPointNear(new Vec3(0, 2, 0), authoredPose.ByBoneId[ChildBone].Transform.Translation);
            Equal(skeleton.ContentHash, projected.SkeletonHash);
        });
    }
}
