using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Graph
{
    /// <summary>
    /// Turns the imported accessory graph into a regular skin graph using a
    /// supplied avatar skeleton.  The initial binding is deliberately simple:
    /// every vertex is assigned to the selected root bone so the artist can
    /// paint useful influences afterwards.
    /// </summary>
    public static class AccessorySkinBindingAdapter
    {
        public static AuthoringGraph BindToSkeleton(AuthoringGraph graph, MeshData editMesh,
            SkeletonDefinition skeleton, string rootBoneId, string poseSourceObjectId = "")
        {
            Checks.Require(graph != null && editMesh != null && skeleton != null,
                "INVALID_SKIN", "Accessory graph, evaluated mesh and avatar skeleton are required.");
            Checks.Id(rootBoneId);
            Checks.Require(skeleton.ById.ContainsKey(rootBoneId), "BONE_NOT_FOUND", "The selected root bone is not in the avatar skeleton.");
            if (!string.IsNullOrEmpty(poseSourceObjectId)) Checks.Id(poseSourceObjectId);
            Checks.Require(graph.Nodes.Values.All(node => node.TypeId != BuiltinNodes.Skeleton &&
                node.TypeId != BuiltinNodes.SkinBind && node.TypeId != BuiltinNodes.SkinDeform &&
                node.TypeId != BuiltinNodes.Pose), "ACCESSORY_ALREADY_SKINNED", "This accessory already has a skin graph.");
            Checks.Require(!graph.Nodes.Values.Any(node => node.TypeId == BuiltinNodes.Attachment),
                "ACCESSORY_ATTACHMENT_CONFLICT", "Remove the rigid attachment before creating a skin binding.");

            var editNodes = graph.Nodes.Values.Where(node => node.TypeId == BuiltinNodes.EditMesh).ToArray();
            Checks.Require(editNodes.Length == 1, "ACCESSORY_EDIT_STAGE_REQUIRED", "The accessory needs exactly one EditMesh stage before skin binding.");
            var edit = editNodes[0];
            var downstream = graph.Edges.Where(edge => edge.FromNode == edit.NodeId && edge.FromPort == "mesh").ToArray();
            Checks.Require(downstream.Length == 1, "ACCESSORY_OUTPUT_UNRESOLVED", "The accessory EditMesh stage must feed one downstream mesh stage.");
            Checks.Require(editMesh.TopologyHash != "", "INVALID_MESH", "The evaluated accessory mesh has no topology identity.");

            var raw = Enumerable.Range(0, editMesh.VertexCount)
                .Select(index => new SkinBinding.VertexWeightInput(index, rootBoneId, 1f));
            var binding = SkinBinding.Create(editMesh, skeleton, raw);
            var skeletonId = NewId(graph, "skeleton");
            var bindingId = NewId(graph, "binding");
            var poseId = NewId(graph, "pose");
            var deformId = NewId(graph, "deform");
            var pose = PoseSet.Create(skeleton, skeleton.Bones.Select(bone =>
                new BonePose(bone.BoneId, PoseTransform.FromTranslation(bone.Head))));

            var nodes = graph.Nodes.Values.ToList();
            nodes.Add(GraphNode.SkeletonNode(skeletonId, skeleton));
            nodes.Add(GraphNode.SkinBindNode(bindingId, binding));
            nodes.Add(GraphNode.PoseNode(poseId, pose));
            nodes.Add(GraphNode.SkinDeformNode(deformId));
            if (!string.IsNullOrEmpty(poseSourceObjectId)) nodes.Add(GraphNode.PoseSourceNode(NewId(graph, "pose-source"), poseSourceObjectId));

            // Keep all material/paint stages downstream of deformation.  The
            // imported static graph normally has EditMesh -> assignment/output,
            // and this also preserves that path for future accessory imports.
            var edges = graph.Edges.Where(edge => edge != downstream[0]).ToList();
            var next = downstream[0];
            edges.Add(new GraphEdge(edit.NodeId, "mesh", bindingId, "mesh"));
            edges.Add(new GraphEdge(skeletonId, "skeleton", bindingId, "skeleton"));
            edges.Add(new GraphEdge(edit.NodeId, "mesh", deformId, "mesh"));
            edges.Add(new GraphEdge(skeletonId, "skeleton", deformId, "skeleton"));
            edges.Add(new GraphEdge(bindingId, "binding", deformId, "binding"));
            edges.Add(new GraphEdge(poseId, "pose", deformId, "pose"));
            edges.Add(new GraphEdge(deformId, "mesh", next.ToNode, next.ToPort));
            edges.Add(new GraphEdge(skeletonId, "skeleton", poseId, "skeleton"));
            return new AuthoringGraph(graph.GraphId, nodes, edges, graph.OutputNodeId);
        }

        static string NewId(AuthoringGraph graph, string label)
        {
            string id;
            do { id = Guid.NewGuid().ToString("D"); }
            while (graph.Nodes.ContainsKey(id));
            return id;
        }
    }
}
