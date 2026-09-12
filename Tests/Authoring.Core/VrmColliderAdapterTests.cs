using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;

internal static partial class Program
{
    static void RunVrmColliderAdapterTests()
    {
        foreach (bool legacy in new[] { false, true })
            Test("VRM collider adapter preserves source space and scales geometry: " + legacy, () =>
            {
                var bytes = BuildMappedVrm(legacy); var json = JObject.Parse(ReadJsonChunk(bytes));
                json["nodes"][2]["translation"] = new JArray(1, .3, 0);
                bytes = ReplaceJsonChunk(bytes, json.ToString()); var source = GlbSkinImporter.Read(bytes);
                string skeletonId = Guid.NewGuid().ToString("D"), meshId = Guid.NewGuid().ToString("D"), output = Guid.NewGuid().ToString("D");
                var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] { GraphNode.SkeletonNode(skeletonId, source.Skeleton), GraphNode.Source(meshId, source.Mesh, new RestTransform(1, new Vec3())), GraphNode.Output(output) }, new[] { new GraphEdge(meshId, "mesh", output, "mesh") }, output);
                var rig = ImportedRigSessionCodec.Read(ImportedRigSessionCodec.Write(ImportedRigSession.Create(source, VrmMetadataReader.Read(bytes), graph.GraphId, skeletonId)));
                var shapes = new[] { new VrmSpringColliderShape("sphere", new Vec3(0, .2f, .4f), .1f, null), new VrmSpringColliderShape("capsule", new Vec3(0, .2f, .4f), .2f, new Vec3(0, .5f, .6f)) };
                VrmSpringSession Session(int node, bool details = true, string hash = null) => new VrmSpringSession(hash ?? source.SourceHash, legacy ? "vrm0" : "vrm1", "fixture", "", null, new[] { new VrmSpringColliderGroup(node, 2, shapes: details ? shapes : null) });
                var session = VrmSpringSessionCodec.Read(VrmSpringSessionCodec.Write(Session(2)));
                var rotation = new PoseTransform(new Vec3(0, 2, 0), new Vec3(-2, 0, 0), new Vec3(0, 0, 2), new Vec3(10, 20, 30));
                PoseSet Pose(PoseTransform transform) => PoseSet.Create(source.Skeleton, source.Skeleton.Bones.Select(b => new BonePose(b.BoneId, b.BoneId == source.BoneMap.Resolve(2) ? transform : PoseTransform.FromTranslation(b.Head))));
                var pose = Pose(rotation); var converted = VrmSpringColliderAdapter.Convert(session, rig, graph, pose);
                Equal(1, converted.Count); Equal(2, converted[0].Colliders.Count);
                float sign = legacy ? -1 : 1;
                SpringPointNear(new Vec3(9.2f, 22, 30 + sign * .8f), converted[0].Colliders[0].Center);
                SpringPointNear(new Vec3(8.6f, 22, 30 + sign * 1.2f), converted[0].Colliders[1].Tail.Value);
                Near(.2f, converted[0].Colliders[0].Radius); Near(.4f, converted[0].Colliders[1].Radius);
                False(converted[0].Colliders[0].Tail.HasValue);
                var reflected = VrmSpringColliderAdapter.Convert(session, rig, graph, Pose(new PoseTransform(new Vec3(-2, 0, 0), new Vec3(0, 2, 0), new Vec3(0, 0, 2), new Vec3())));
                Near(.2f, reflected[0].Colliders[0].Radius);
                Expect("IMPORT_COLLIDER_DETAILS_MISSING", () => VrmSpringColliderAdapter.Convert(Session(2, false), rig, graph, pose));
                Expect("IMPORT_BONE_UNMAPPED", () => VrmSpringColliderAdapter.Convert(Session(0), rig, graph, pose));
                Expect("IMPORT_SOURCE_CHANGED", () => VrmSpringColliderAdapter.Convert(Session(2, hash: new string('f', 64)), rig, graph, pose));
                Expect("IMPORT_COLLIDER_SCALE_UNSUPPORTED", () => VrmSpringColliderAdapter.Convert(session, rig, graph, Pose(new PoseTransform(new Vec3(2, 0, 0), new Vec3(0, 1, 0), new Vec3(0, 0, 1), new Vec3()))));
                Expect("IMPORT_COLLIDER_SCALE_UNSUPPORTED", () => VrmSpringColliderAdapter.Convert(session, rig, graph, Pose(new PoseTransform(new Vec3(1, 0, 0), new Vec3(.2f, 1, 0), new Vec3(0, 0, 1), new Vec3()))));
            });
    }
}
