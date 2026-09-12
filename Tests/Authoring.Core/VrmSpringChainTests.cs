using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;

internal static partial class Program
{
    static void RunVrmSpringChainTests()
    {
        Test("VRM1 chain pairs retain skipped joints endpoints and raw settings", () =>
        {
            string tip = Guid.NewGuid().ToString("D");
            var skeleton = new SkeletonDefinition(new[] {
                new BoneDefinition(RootBone, "head", "", new Vec3(), new Vec3(0, .1f, 0)),
                new BoneDefinition(ChildBone, "middle", RootBone, new Vec3(0, 1, 0), new Vec3(0, 1.1f, 0)),
                new BoneDefinition(tip, "tail", ChildBone, new Vec3(0, 2, 0), new Vec3(0, 2.1f, 0)) });
            string sid = Guid.NewGuid().ToString("D"), mid = Guid.NewGuid().ToString("D"), output = Guid.NewGuid().ToString("D"), hash = new string('a', 64);
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] { GraphNode.SkeletonNode(sid, skeleton), GraphNode.Source(mid, AuthoringFixtures.Panel(1), new RestTransform(1, new Vec3())), GraphNode.Output(output) }, new[] { new GraphEdge(mid, "mesh", output, "mesh") }, output);
            var rig = new ImportedRigSession(hash, skeleton.ContentHash, graph.GraphId, sid,
                new Dictionary<int, string> { [0] = RootBone, [1] = ChildBone, [2] = tip }, new Dictionary<string, int>(),
                new Dictionary<int, Vec3> { [0] = new Vec3(.3f, 0, 0), [1] = new Vec3(.3f, 1, 0), [2] = new Vec3(.3f, 2, 0) });
            VrmSpringJoint Joint(int node) => new VrmSpringJoint(node, .1f, 25, 2, .5f, new Vec3(0, -1, 0));
            VrmSpringBoneGroup Chain(int[] nodes, int center = -1) => new VrmSpringBoneGroup("fixture", System.Array.ConvertAll(nodes, Joint), null, null, center);
            VrmSpringSession Session(params VrmSpringBoneGroup[] chains) => new VrmSpringSession(hash, "vrm1", "fixture", "", chains, null);
            var input = VrmSpringSessionCodec.Read(VrmSpringSessionCodec.Write(Session(Chain(new[] { 0, 2 }, 0))));
            var resolved = Vrm1SpringChainResolver.Resolve(input, ImportedRigSessionCodec.Read(ImportedRigSessionCodec.Write(rig)), graph);
            Equal(1, resolved.Count); Equal(1, resolved[0].Pairs.Count);
            Equal(RootBone, resolved[0].Pairs[0].HeadBoneId); Equal(tip, resolved[0].Pairs[0].TailBoneId); Equal(RootBone, resolved[0].CenterBoneId);
            Near(25, resolved[0].Pairs[0].Settings.Stiffness);
            SpringPointNear(new Vec3(.3f, 0, 0), resolved[0].Pairs[0].SourceHeadOrigin);
            SpringPointNear(new Vec3(.3f, 2, 0), resolved[0].Pairs[0].SourceTailOrigin);
            var pose = PoseSet.Create(skeleton, skeleton.Bones.Select(b => new BonePose(b.BoneId, PoseTransform.FromTranslation(b.Head))));
            var executable = Vrm1SpringRuntimeAdapter.CreateChains(input, rig, graph, pose);
            Equal(1, executable[0].Joints.Count); Near(25, executable[0].Joints[0].Stiffness);
            True(executable[0].Joints[0].IntegrationMode == SpringIntegrationMode.VrmReference);
            var state = SpringBoneSimulator.CreateInitialState(skeleton, pose, executable);
            for (int i = 0; i < 12; i++)
            {
                var step = SpringBoneSimulator.Step(skeleton, pose, executable, null, state, 1f / 60f); state = step.State;
                SpringPointNear(new Vec3(.3f, 0, 0), step.Pose.ByBoneId[RootBone].Transform.TransformPoint(new Vec3(.3f, 0, 0)));
                SpringPointNear(state.CurrentTails[RootBone], step.Pose.ByBoneId[RootBone].Transform.TransformPoint(new Vec3(.3f, 2, 0)));
            }
            var three = Vrm1SpringChainResolver.Resolve(Session(Chain(new[] { 0, 1, 2 })), rig, graph);
            Equal(2, three[0].Pairs.Count); Equal(ChildBone, three[0].Pairs[1].HeadBoneId);
            Expect("DUPLICATE_SPRING_JOINT", () => Vrm1SpringChainResolver.Resolve(Session(Chain(new[] { 0, 2 }), Chain(new[] { 1, 2 })), rig, graph));
            Expect("INVALID_VRM_SPRING_CHAIN", () => Vrm1SpringChainResolver.Resolve(Session(Chain(new[] { 2, 0 })), rig, graph));
            Expect("INVALID_VRM_SPRING_CHAIN", () => Vrm1SpringChainResolver.Resolve(Session(Chain(new[] { 0, 2 }, 1)), rig, graph));
            Expect("INVALID_VRM_SPRING_CHAIN", () => Vrm1SpringChainResolver.Resolve(Session(Chain(new[] { 0 })), rig, graph));
            Expect("IMPORT_BONE_UNMAPPED", () => Vrm1SpringChainResolver.Resolve(Session(Chain(new[] { 0, 3 })), rig, graph));
            Expect("IMPORT_SOURCE_CHANGED", () => Vrm1SpringChainResolver.Resolve(new VrmSpringSession(new string('b', 64), "vrm1", "", "", null, null), rig, graph));
        });
    }
}
