using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Graph;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    static void RunVrm0SpringExpansionTests()
    {
        VrmSpringSession Session(params int[] roots) => new VrmSpringSession(new string('a', 64), "vrm0", "fixture", "author",
            new[] { new VrmSpringBoneGroup("hair", roots.Select(n => new VrmSpringJoint(n, .02f, 25, 2, .4f, new Vec3(2, -3, 4))), roots, Array.Empty<int>(), 0) }, Array.Empty<VrmSpringColliderGroup>());
        var hierarchy = new ImportedSourceHierarchy(new[] { -1, 0, 1, 1 },
            new[] { new Vec3(), new Vec3(0, 1, 0), new Vec3(1, 1, 0), new Vec3(0, 1, -2) },
            new int[][] { new[] { 1 }, new[] { 3, 2 }, Array.Empty<int>(), Array.Empty<int>() });
        Test("VRM0 expands every branch in source order with first-child and virtual tails", () =>
        {
            var group = Vrm0SpringExpansion.Expand(Session(1), hierarchy).Single();
            Equal(3, group.Targets.Count); Equal(1, group.Targets[0].NodeIndex); Equal(3, group.Targets[1].NodeIndex); Equal(2, group.Targets[2].NodeIndex);
            Equal(3, group.Targets[0].TailNodeIndex); Equal(-1, group.Targets[1].TailNodeIndex);
            SpringPointNear(new Vec3(0, 1, -2), group.Targets[0].Tail);
            SpringPointNear(new Vec3(0, 1, -2.07f), group.Targets[1].Tail);
            SpringPointNear(new Vec3(1.07f, 1, 0), group.Targets[2].Tail);
            foreach (var target in group.Targets) { Equal(target.NodeIndex, target.Settings.NodeIndex); Equal(25f, target.Settings.Stiffness); SpringPointNear(new Vec3(2, -3, 4), target.Settings.GravityDirection.Value); }
            Equal(0, group.CenterNodeIndex);
        });
        Test("VRM0 imported metadata and persisted rig expand nonjoint terminals", () =>
        {
            var bytes = BuildMappedVrm(true); var json = JObject.Parse(ReadJsonChunk(bytes));
            json["nodes"][2]["children"] = new JArray(3);
            ((JArray)json["nodes"]).Add(new JObject { ["translation"] = new JArray(0, .2, 0) });
            json["extensions"]["VRM"]["secondaryAnimation"] = new JObject {
                ["colliderGroups"] = new JArray(),
                ["boneGroups"] = new JArray(new JObject { ["bones"] = new JArray(1), ["stiffiness"] = 25,
                    ["gravityDir"] = new JObject { ["x"] = 0, ["y"] = -1, ["z"] = 0 } }) };
            bytes = ReplaceJsonChunk(bytes, json.ToString()); var skin = GlbSkinImporter.Read(bytes); var metadata = VrmMetadataReader.Read(bytes);
            string sid = Guid.NewGuid().ToString("D"), mid = Guid.NewGuid().ToString("D"), output = Guid.NewGuid().ToString("D");
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] { GraphNode.SkeletonNode(sid, skin.Skeleton), GraphNode.Source(mid, skin.Mesh, new RestTransform(1, new Vec3())), GraphNode.Output(output) }, new[] { new GraphEdge(mid, "mesh", output, "mesh") }, output);
            var rig = ImportedRigSessionCodec.Read(ImportedRigSessionCodec.Write(ImportedRigSession.Create(skin, metadata, graph.GraphId, sid)));
            var spring = VrmSpringSession.Create(metadata);
            var targets = Vrm0SpringExpansion.Resolve(spring, rig, graph).Single().Targets;
            Equal(3, targets.Count); Equal(3, targets[2].NodeIndex); True(!rig.NodeToBone.ContainsKey(3));
            SpringPointNear(new Vec3(0, .37f, 0), targets[2].Tail);
            Expect("IMPORT_SOURCE_CHANGED", () => Vrm0SpringExpansion.Resolve(Session(1), rig, graph));
        });
        Test("VRM0 expansion rejects unknown hierarchy overlapping roots and degenerate endpoints", () =>
        {
            Expect("IMPORT_NODE_HIERARCHY_MISSING", () => Vrm0SpringExpansion.Expand(Session(1), null));
            Expect("DUPLICATE_SPRING_JOINT", () => Vrm0SpringExpansion.Expand(Session(1, 3), hierarchy));
            Expect("INVALID_VRM_SPRING_CHAIN", () => Vrm0SpringExpansion.Expand(Session(8), hierarchy));
            Expect("INVALID_VRM_SPRING_ENDPOINT", () => Vrm0SpringExpansion.Expand(Session(0), new ImportedSourceHierarchy(new[] { -1 }, new[] { new Vec3() })));
            Expect("INVALID_VRM_SPRING_ENDPOINT", () => Vrm0SpringExpansion.Expand(Session(1), new ImportedSourceHierarchy(new[] { -1, 0 }, new[] { new Vec3(), new Vec3() })));
        });
    }
}
