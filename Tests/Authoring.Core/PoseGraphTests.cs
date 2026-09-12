using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Inspection;
using NyaForge.Authoring.Rig;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    static void RunPoseGraphTests()
    {
        Test("pose and skin deformation graph nodes evaluate and survive native storage", () =>
        {
            string root = GraphId(), child = GraphId(), skeletonId = GraphId(), planeId = GraphId(), bindId = GraphId(), poseId = GraphId(), deformId = GraphId(), outputId = GraphId();
            var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(root, "Root", "", new Vec3(), new Vec3(0, .1f, 0)), new BoneDefinition(child, "Child", root, new Vec3(.1f, 0, 0), new Vec3(.1f, .1f, 0)) });
            var mesh = PrimitiveGeometry.Plane(.2f, .1f);
            var binding = SkinBinding.Create(mesh, skeleton, Enumerable.Range(0, mesh.VertexCount).Select(i => new SkinBinding.VertexWeightInput(i, i < 2 ? root : child, 1)));
            var pose = PoseSet.Create(skeleton, new[] { new BonePose(root, PoseTransform.Identity), new BonePose(child, PoseTransform.RotationZ(25, new Vec3())) });
            var graph = new AuthoringGraph(GraphId(), new[] { GraphNode.Plane(planeId), GraphNode.SkeletonNode(skeletonId, skeleton), GraphNode.SkinBindNode(bindId, binding), GraphNode.PoseNode(poseId, pose), GraphNode.SkinDeformNode(deformId), GraphNode.Output(outputId) },
                new[] { new GraphEdge(planeId, "mesh", bindId, "mesh"), new GraphEdge(skeletonId, "skeleton", bindId, "skeleton"), new GraphEdge(skeletonId, "skeleton", poseId, "skeleton"), new GraphEdge(planeId, "mesh", deformId, "mesh"), new GraphEdge(skeletonId, "skeleton", deformId, "skeleton"), new GraphEdge(bindId, "binding", deformId, "binding"), new GraphEdge(poseId, "pose", deformId, "pose"), new GraphEdge(deformId, "mesh", outputId, "mesh") }, outputId);
            var workspace = AuthoringWorkspace.CreateEmpty(); Ok(new AuthoringCommandService(workspace).Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph))));
            True(workspace.Preview.Evaluation.PoseOutputs.ContainsKey(poseId)); True(workspace.Preview.Evaluation.MeshOutputs.ContainsKey(deformId));
            True(!workspace.Preview.Evaluation.MeshOutputs[deformId].Mesh.Positions.SequenceEqual(mesh.Positions));
            var info = AuthoringGraphReader.Read(workspace, workspace.InstanceId)["graph"]["nodes"].Children<JObject>().Single(n => (string)n["nodeId"] == poseId)["poseOutput"];
            Equal(pose.ContentHash, (string)info["poseHash"]); Equal(2, (int)info["boneCount"]);
            string directory = Dir("pose-graph-native"); ProjectStore.Save(directory, workspace, 0); var reopened = ProjectStore.Open(directory);
            True(reopened.Preview.Evaluation.PoseOutputs.ContainsKey(poseId)); True(reopened.Preview.Evaluation.MeshOutputs.ContainsKey(deformId));
            True(reopened.Preview.Evaluation.MeshOutputs[deformId].Mesh.Positions.SequenceEqual(workspace.Preview.Evaluation.MeshOutputs[deformId].Mesh.Positions));
            var poseBytes = PoseCodec.Write(pose); True(poseBytes.SequenceEqual(PoseCodec.Write(PoseCodec.Read(poseBytes, skeleton))));
        });
    }
}
