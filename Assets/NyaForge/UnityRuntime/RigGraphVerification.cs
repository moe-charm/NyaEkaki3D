using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Inspection;
using NyaForge.Authoring.Rig;

namespace NyaForge.UnityRuntime
{
    internal static class RigGraphVerification
    {
        internal static void Verify(string output, List<string> checks)
        {
            string root = Guid.NewGuid().ToString("D"), child = Guid.NewGuid().ToString("D"), skeletonId = Guid.NewGuid().ToString("D"), planeId = Guid.NewGuid().ToString("D"), bindId = Guid.NewGuid().ToString("D"), poseId = Guid.NewGuid().ToString("D"), deformId = Guid.NewGuid().ToString("D"), outputId = Guid.NewGuid().ToString("D");
            var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(root, "Root", "", new Vec3(), new Vec3(0, .1f, 0)), new BoneDefinition(child, "Child", root, new Vec3(.1f, 0, 0), new Vec3(.1f, .1f, 0)) });
            var mesh = PrimitiveGeometry.Plane(.2f, .1f);
            var binding = SkinBinding.Create(mesh, skeleton, Enumerable.Range(0, mesh.VertexCount).Select(i => new SkinBinding.VertexWeightInput(i, i < 2 ? root : child, 1)));
            var pose = PoseSet.Create(skeleton, new[] { new BonePose(root, PoseTransform.Identity), new BonePose(child, PoseTransform.RotationZ(25, new Vec3())) });
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] { GraphNode.Plane(planeId), GraphNode.SkeletonNode(skeletonId, skeleton), GraphNode.SkinBindNode(bindId, binding), GraphNode.PoseNode(poseId, pose), GraphNode.SkinDeformNode(deformId), GraphNode.Output(outputId) },
                new[] { new GraphEdge(planeId, "mesh", bindId, "mesh"), new GraphEdge(skeletonId, "skeleton", bindId, "skeleton"), new GraphEdge(skeletonId, "skeleton", poseId, "skeleton"), new GraphEdge(planeId, "mesh", deformId, "mesh"), new GraphEdge(skeletonId, "skeleton", deformId, "skeleton"), new GraphEdge(bindId, "binding", deformId, "binding"), new GraphEdge(poseId, "pose", deformId, "pose"), new GraphEdge(deformId, "mesh", outputId, "mesh") }, outputId);
            var workspace = AuthoringWorkspace.CreateEmpty(); var result = new AuthoringCommandService(workspace).Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph)));
            if (!result.Success || !workspace.Preview.Evaluation.SkinBindingOutputs.ContainsKey(bindId) || !workspace.Preview.Evaluation.PoseOutputs.ContainsKey(poseId) || !workspace.Preview.Evaluation.MeshOutputs.ContainsKey(deformId)) throw new InvalidOperationException("Rig graph evaluation failed in Player");
            if (workspace.Preview.Evaluation.MeshOutputs[deformId].Mesh.ContentHash == mesh.ContentHash) throw new InvalidOperationException("Skin deformation did not change mesh in Player");
            var inspection = AuthoringGraphReader.Read(workspace, workspace.InstanceId);
            var node = inspection["graph"]["nodes"].Children<Newtonsoft.Json.Linq.JObject>().Single(n => (string)n["nodeId"] == bindId);
            if ((string)node["skinBindingOutput"]["meshTopologyHash"] != mesh.TopologyHash || (int)node["skinBindingOutput"]["vertexCount"] != mesh.VertexCount)
                throw new InvalidOperationException("Skin binding inspection differs in Player");
            string directory = Path.Combine(output, "rig-graph-project"); ProjectStore.Save(directory, workspace, 0);
            var loaded = ProjectStore.Open(directory);
            if (!loaded.Preview.Evaluation.SkinBindingOutputs.ContainsKey(bindId) || !loaded.Preview.Evaluation.PoseOutputs.ContainsKey(poseId) || !loaded.Preview.Evaluation.MeshOutputs.ContainsKey(deformId) || loaded.Preview.Output.Mesh.ContentHash != workspace.Preview.Output.Mesh.ContentHash)
                throw new InvalidOperationException("Rig graph save/reopen differs in Player");
            checks.Add("rig graph: typed skeleton/skin-bind/pose/skin-deform evaluation, inspection and native schema3 reopen");
        }
    }
}
