using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Paint;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Graph
{
    /// <summary>Result of turning a polygon authoring graph into a skin-ready derivative.</summary>
    public sealed class AccessorySkinMaterialization
    {
        public string SourceGraphId { get; }
        public string SourceGraphHash { get; }
        public AuthoringGraph Graph { get; }

        internal AccessorySkinMaterialization(string sourceGraphId, string sourceGraphHash, AuthoringGraph graph)
        {
            SourceGraphId = sourceGraphId;
            SourceGraphHash = sourceGraphHash;
            Graph = graph;
        }
    }

    /// <summary>
    /// Creates a skin-bindable derivative while leaving the editable polygon
    /// graph untouched. The derived graph keeps the evaluated UV/material
    /// topology and can be added as a second project object in one command.
    /// </summary>
    public static class AccessorySkinMaterializer
    {
        public static AccessorySkinMaterialization Materialize(
            AuthoringGraph polygonGraph, SkeletonDefinition skeleton, string rootBoneId,
            string poseSourceObjectId = "", string derivedGraphId = "")
        {
            Checks.Require(polygonGraph != null && skeleton != null, "INVALID_SKIN", "Polygon graph and avatar skeleton are required.");
            if (string.IsNullOrEmpty(derivedGraphId)) derivedGraphId = Guid.NewGuid().ToString("D");
            Checks.Id(derivedGraphId);
            Checks.Require(derivedGraphId != polygonGraph.GraphId, "GRAPH_ID_CONFLICT", "A materialized graph needs a new graph identity.");
            Checks.Id(rootBoneId);
            if (!string.IsNullOrEmpty(poseSourceObjectId)) Checks.Id(poseSourceObjectId);

            var sources = polygonGraph.Nodes.Values.Where(node => node.TypeId == BuiltinNodes.PolygonSource).ToArray();
            var edits = polygonGraph.Nodes.Values.Where(node => node.TypeId == BuiltinNodes.PolygonEdit).ToArray();
            Checks.Require(sources.Length == 1 && edits.Length == 1, "POLYGON_MATERIALIZE_SHAPE", "Materialization needs exactly one PolygonSource and one PolygonEdit stage.");
            var source = sources[0]; var edit = edits[0];
            Checks.Require(polygonGraph.Edges.Any(edge => edge.FromNode == source.NodeId && edge.FromPort == "mesh" && edge.ToNode == edit.NodeId && edge.ToPort == "mesh"),
                "POLYGON_MATERIALIZE_SHAPE", "PolygonSource must feed PolygonEdit directly.");
            Checks.Require(!polygonGraph.Nodes.Values.Any(node => node.TypeId == BuiltinNodes.Skeleton ||
                node.TypeId == BuiltinNodes.SkinBind || node.TypeId == BuiltinNodes.SkinDeform || node.TypeId == BuiltinNodes.Pose ||
                node.TypeId == BuiltinNodes.Attachment), "ACCESSORY_ALREADY_SKINNED", "The polygon graph already contains a rig or attachment.");

            // Geometry-altering stages need a topology-aware conversion. The
            // first v1 materializer is deliberately limited to polygon editing
            // plus appearance nodes so that no later modifier is silently baked.
            var allowed = new HashSet<string>(new[] {
                BuiltinNodes.PolygonSource, BuiltinNodes.PolygonEdit, BuiltinNodes.Output,
                BuiltinNodes.Paint, BuiltinNodes.StandardMaterial, BuiltinNodes.AssignMaterial,
                BuiltinNodes.AssignMaterials
            }, StringComparer.Ordinal);
            Checks.Require(polygonGraph.Nodes.Values.All(node => allowed.Contains(node.TypeId)),
                "POLYGON_MATERIALIZE_UNSUPPORTED", "Materialization supports polygon editing and appearance nodes only.");
            var evaluation = GraphEvaluator.Evaluate(polygonGraph);
            Checks.Require(evaluation.IsComplete && evaluation.Output != null && evaluation.Output.Mesh != null,
                "POLYGON_MATERIALIZE_INCOMPLETE", "Resolve the polygon output before creating the skin derivative.");
            Checks.Require(evaluation.MeshOutputs.TryGetValue(edit.NodeId, out var editValue) && editValue != null && editValue.Mesh != null,
                "POLYGON_MATERIALIZE_INCOMPLETE", "PolygonEdit must produce a renderable mesh.");

            var materializedSource = GraphNode.Source(source.NodeId, editValue.Mesh, editValue.Transform);
            var sourceValue = GraphMeshValue.Source(source.NodeId, editValue.Mesh, editValue.Transform);
            var materializedEdit = GraphNode.Edit(edit.NodeId, true, null, sourceValue.SnapshotHash, sourceValue.DomainId);
            var sourceMarker = GraphNode.DerivedSourceNode(Guid.NewGuid().ToString("D"), polygonGraph.GraphId, GraphContentIdentity.Hash(polygonGraph));
            var nodes = polygonGraph.Nodes.Values.Select(node =>
            {
                if (node.NodeId == source.NodeId) return materializedSource;
                if (node.NodeId == edit.NodeId) return materializedEdit;
                // A polygon-bound paint image has the same UV layout in the
                // rendered mesh. Clearing its polygon domain makes it an
                // immutable imported image while retaining every pixel.
                if (node.TypeId == BuiltinNodes.Paint)
                {
                    var image = node.PaintImage ?? new PaintImage(node.PaintWidth, node.PaintHeight, new Rgba32(255, 255, 255, 255));
                    return GraphNode.Paint(node.NodeId, node.PaintWidth, node.PaintHeight, image);
                }
                Checks.Require(node.TypeId != BuiltinNodes.LayeredPaint, "POLYGON_MATERIALIZE_UNSUPPORTED", "Layered paint needs an explicit image rebinding step before materialization.");
                return node;
            }).Concat(new[] { sourceMarker }).ToArray();
            var derived = new AuthoringGraph(derivedGraphId, nodes, polygonGraph.Edges, polygonGraph.OutputNodeId);
            var derivedEvaluation = GraphEvaluator.Evaluate(derived);
            Checks.Require(derivedEvaluation.IsComplete && derivedEvaluation.Output != null && derivedEvaluation.Output.Mesh != null,
                "POLYGON_MATERIALIZE_INCOMPLETE", "The materialized appearance graph could not be evaluated.");
            Checks.Require(derivedEvaluation.Output.Mesh.ContentHash == evaluation.Output.Mesh.ContentHash,
                "POLYGON_MATERIALIZE_MISMATCH", "Materialization changed the evaluated geometry.");
            var rootBound = AccessorySkinBindingAdapter.BindToSkeleton(derived,
                derivedEvaluation.MeshOutputs[edit.NodeId].Mesh, skeleton, rootBoneId, poseSourceObjectId);
            return new AccessorySkinMaterialization(polygonGraph.GraphId, GraphContentIdentity.Hash(polygonGraph), rootBound);
        }
    }

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
            Checks.Require(downstream.Length > 0, "ACCESSORY_OUTPUT_UNRESOLVED", "The accessory EditMesh stage must feed a downstream mesh stage.");
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
            var edges = graph.Edges.Where(edge => !downstream.Contains(edge)).ToList();
            edges.Add(new GraphEdge(edit.NodeId, "mesh", bindingId, "mesh"));
            edges.Add(new GraphEdge(skeletonId, "skeleton", bindingId, "skeleton"));
            edges.Add(new GraphEdge(edit.NodeId, "mesh", deformId, "mesh"));
            edges.Add(new GraphEdge(skeletonId, "skeleton", deformId, "skeleton"));
            edges.Add(new GraphEdge(bindingId, "binding", deformId, "binding"));
            edges.Add(new GraphEdge(poseId, "pose", deformId, "pose"));
            foreach (var next in downstream)
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
