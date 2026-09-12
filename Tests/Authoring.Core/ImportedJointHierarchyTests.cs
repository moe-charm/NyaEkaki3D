using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;

internal static partial class Program
{
    static void RunImportedJointHierarchyTests()
    {
        foreach (int count in new[] { 1, 3 })
        foreach (bool reverse in new[] { false, true })
            Test("Imported joints preserve intervening nodes: " + count + "/" + reverse, () =>
            {
                var bytes = BuildMappedVrm(false); var json = JObject.Parse(ReadJsonChunk(bytes));
                var nodes = (JArray)json["nodes"];
                nodes[1]["children"] = new JArray(3);
                for (int i = 0; i < count; i++) nodes.Add(new JObject {
                    ["translation"] = new JArray(.2, 0, 0),
                    ["children"] = new JArray(i + 1 == count ? 2 : 4 + i) });
                // Without inverse binds, the accumulated source translations define rest heads.
                ((JObject)json["skins"][0]).Remove("inverseBindMatrices");
                if (reverse) json["skins"][0]["joints"] = new JArray(2, 1);
                var source = GlbSkinImporter.Read(ReplaceJsonChunk(bytes, json.ToString()));
                var parentId = source.BoneMap.Resolve(1); var childId = source.BoneMap.Resolve(2);
                var child = source.Skeleton.ById[childId];
                Equal(parentId, child.ParentBoneId);
                SpringPointNear(new Vec3(.2f * count, .1f, 0), child.Head);
                SpringPointNear(child.Head, source.Skeleton.ById[parentId].Tail);
                var pose = PoseSet.Create(source.Skeleton, source.Skeleton.Bones.Select(b => new BonePose(b.BoneId, PoseTransform.FromTranslation(b.Head))));
                var moved = new Dictionary<string, BonePose> { [parentId] = new BonePose(parentId, PoseTransform.RotationZ(90, new Vec3(1, 2, 3))) };
                var followed = SpringPoseHierarchy.Inherit(child, pose, moved);
                SpringPointNear(new Vec3(.9f, 2 + .2f * count, 3), followed.Transform.Translation);
                string meshId = Guid.NewGuid().ToString("D"), skeletonId = Guid.NewGuid().ToString("D"), outputId = Guid.NewGuid().ToString("D");
                var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] {
                    GraphNode.Source(meshId, source.Mesh, new RestTransform(1, new Vec3())),
                    GraphNode.SkeletonNode(skeletonId, source.Skeleton), GraphNode.Output(outputId)
                }, new[] { new GraphEdge(meshId, "mesh", outputId, "mesh") }, outputId);
                var workspace = AuthoringWorkspace.CreateEmpty(); Ok(Execute(workspace, AuthoringOperation.AddGraph(graph)));
                var directory = Dir("joint-hierarchy-" + count + "-" + reverse); ProjectStore.Save(directory, workspace, 0);
                var restored = ProjectStore.Open(directory).Document.Objects[0].Graph.Nodes[skeletonId].Skeleton;
                Equal(source.Skeleton.ContentHash, restored.ContentHash);
                Equal(parentId, restored.ById[childId].ParentBoneId);
            });
    }
}
