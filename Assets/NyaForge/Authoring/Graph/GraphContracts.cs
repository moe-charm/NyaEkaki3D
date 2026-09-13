using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NyaForge.Authoring.Graph
{
    public enum PortType { Mesh, Scalar, Image, Material, Skeleton, SkinBinding, Pose, MorphSet }

    public sealed class PortDefinition
    {
        public string Id { get; }
        public PortType Type { get; }
        public bool Required { get; }
        public PortDefinition(string id, PortType type, bool required = true)
        { Checks.Name(id); Id = id; Type = type; Required = required; }
    }

    public sealed class NodeDefinition
    {
        public string TypeId { get; }
        public int Version { get; }
        public IReadOnlyList<PortDefinition> Inputs { get; }
        public IReadOnlyList<PortDefinition> Outputs { get; }
        internal NodeDefinition(string id, PortDefinition[] inputs, PortDefinition[] outputs)
        {
            TypeId = id; Version = 1;
            Inputs = Array.AsReadOnly((PortDefinition[])inputs.Clone());
            Outputs = Array.AsReadOnly((PortDefinition[])outputs.Clone());
        }
    }

    public static class BuiltinNodes
    {
        public const string MeshSource = "mesh.source";
        public const string PolygonSource = "mesh.polygon-source";
        public const string PolygonEdit = "mesh.polygon-edit";
        public const string Mirror = "mesh.mirror";
        public const string Paint = "image.paint";
        /// <summary>Owned source bytes retained separately from the bounded Paint preview.</summary>
        public const string OriginalImage = "image.original-source";
        public const string LayeredPaint = "image.paint-layers";
        public const string Plane = "primitive.plane";
        public const string EditMesh = "mesh.edit";
        public const string Output = "mesh.output";
        public const string Scalar = "value.scalar";
        public const string StandardMaterial = "material.standard";
        public const string AssignMaterial = "mesh.assign-material";
        public const string AssignMaterials = "mesh.assign-materials";
        public const string Skeleton = "rig.skeleton";
        public const string SkinBind = "rig.skin-bind";
        public const string Pose = "rig.pose";
        public const string SkinDeform = "rig.skin-deform";
        /// <summary>Metadata node that records the explicit avatar pose-copy source.</summary>
        public const string PoseSource = "rig.pose-source";
        public const string MorphSet = "rig.morph-set";
        public const string MorphDeform = "rig.morph-deform";
        /// <summary>Metadata node that pins an accessory object to a stable avatar bone.</summary>
        public const string Attachment = "object.attachment";
        /// <summary>Metadata node that records the polygon graph used to derive a skin graph.</summary>
        public const string DerivedSource = "mesh.derived-source";
        static readonly IReadOnlyDictionary<string, NodeDefinition> definitions =
            new ReadOnlyDictionary<string, NodeDefinition>(new Dictionary<string, NodeDefinition>
            {
                [MeshSource] = new NodeDefinition(MeshSource, Array.Empty<PortDefinition>(), new[] { new PortDefinition("mesh", PortType.Mesh) }),
                [PolygonSource] = new NodeDefinition(PolygonSource, Array.Empty<PortDefinition>(), new[] { new PortDefinition("mesh", PortType.Mesh) }),
                [PolygonEdit] = new NodeDefinition(PolygonEdit, new[] { new PortDefinition("mesh", PortType.Mesh) }, new[] { new PortDefinition("mesh", PortType.Mesh) }),
                [Mirror] = new NodeDefinition(Mirror, new[] { new PortDefinition("mesh", PortType.Mesh) }, new[] { new PortDefinition("mesh", PortType.Mesh) }),
                [Paint] = new NodeDefinition(Paint, new[] { new PortDefinition("mesh", PortType.Mesh) }, new[] { new PortDefinition("image", PortType.Image) }),
                [OriginalImage] = new NodeDefinition(OriginalImage, Array.Empty<PortDefinition>(), Array.Empty<PortDefinition>()),
                [LayeredPaint] = new NodeDefinition(LayeredPaint, new[] { new PortDefinition("mesh", PortType.Mesh) }, new[] { new PortDefinition("image", PortType.Image) }),
                [Plane] = new NodeDefinition(Plane, new[] { new PortDefinition("width", PortType.Scalar, false), new PortDefinition("height", PortType.Scalar, false) }, new[] { new PortDefinition("mesh", PortType.Mesh) }),
                [EditMesh] = new NodeDefinition(EditMesh, new[] { new PortDefinition("mesh", PortType.Mesh) }, new[] { new PortDefinition("mesh", PortType.Mesh) }),
                [Output] = new NodeDefinition(Output, new[] { new PortDefinition("mesh", PortType.Mesh), new PortDefinition("baseColor", PortType.Image, false) }, Array.Empty<PortDefinition>()),
                [Scalar] = new NodeDefinition(Scalar, Array.Empty<PortDefinition>(), new[] { new PortDefinition("value", PortType.Scalar) }),
                [StandardMaterial] = new NodeDefinition(StandardMaterial,new[] { new PortDefinition("baseColor",PortType.Image,false) },new[] { new PortDefinition("material",PortType.Material) }),
                [AssignMaterial] = new NodeDefinition(AssignMaterial,new[] { new PortDefinition("mesh",PortType.Mesh),new PortDefinition("material",PortType.Material) },new[] { new PortDefinition("mesh",PortType.Mesh) }),
                [AssignMaterials] = new NodeDefinition(AssignMaterials,new[] { new PortDefinition("mesh",PortType.Mesh) },new[] { new PortDefinition("mesh",PortType.Mesh) })
                ,[Skeleton] = new NodeDefinition(Skeleton, Array.Empty<PortDefinition>(), new[] { new PortDefinition("skeleton", PortType.Skeleton) })
                ,[SkinBind] = new NodeDefinition(SkinBind, new[] { new PortDefinition("mesh", PortType.Mesh), new PortDefinition("skeleton", PortType.Skeleton) }, new[] { new PortDefinition("binding", PortType.SkinBinding) })
                ,[Pose] = new NodeDefinition(Pose, new[] { new PortDefinition("skeleton", PortType.Skeleton) }, new[] { new PortDefinition("pose", PortType.Pose) })
                ,[SkinDeform] = new NodeDefinition(SkinDeform, new[] { new PortDefinition("mesh", PortType.Mesh), new PortDefinition("skeleton", PortType.Skeleton), new PortDefinition("binding", PortType.SkinBinding), new PortDefinition("pose", PortType.Pose) }, new[] { new PortDefinition("mesh", PortType.Mesh) })
                ,[PoseSource] = new NodeDefinition(PoseSource, Array.Empty<PortDefinition>(), Array.Empty<PortDefinition>())
                ,[MorphSet] = new NodeDefinition(MorphSet, Array.Empty<PortDefinition>(), new[] { new PortDefinition("morphs", PortType.MorphSet) })
                ,[MorphDeform] = new NodeDefinition(MorphDeform, new[] { new PortDefinition("mesh", PortType.Mesh), new PortDefinition("morphs", PortType.MorphSet) }, new[] { new PortDefinition("mesh", PortType.Mesh) })
                ,[Attachment] = new NodeDefinition(Attachment, Array.Empty<PortDefinition>(), Array.Empty<PortDefinition>())
                ,[DerivedSource] = new NodeDefinition(DerivedSource, Array.Empty<PortDefinition>(), Array.Empty<PortDefinition>())
            });
        public static IReadOnlyDictionary<string, NodeDefinition> Definitions { get { return definitions; } }
        public static NodeDefinition Find(GraphNode node)
        {
            if(node.TypeId==AssignMaterials && node.Version==1)
                return new NodeDefinition(AssignMaterials,new[]{new PortDefinition("mesh",PortType.Mesh)}.Concat((node.MaterialSlots ?? Array.Empty<int>()).Select(s=>new PortDefinition(GraphNode.MaterialSlotPort(s),PortType.Material))).ToArray(),new[]{new PortDefinition("mesh",PortType.Mesh)});
            NodeDefinition definition;
            return definitions.TryGetValue(node.TypeId, out definition) && node.Version == definition.Version ? definition : null;
        }
    }

    public sealed class GraphEdge
    {
        public string FromNode { get; }
        public string FromPort { get; }
        public string ToNode { get; }
        public string ToPort { get; }
        public GraphEdge(string fromNode, string fromPort, string toNode, string toPort)
        {
            Checks.Id(fromNode); Checks.Id(toNode); Checks.Name(fromPort); Checks.Name(toPort);
            FromNode = fromNode; FromPort = fromPort; ToNode = toNode; ToPort = toPort;
        }
    }

    public sealed class AuthoringGraph
    {
        public const int MaxNodes = 128, MaxEdges = 512;
        public string GraphId { get; }
        /// <summary>Canonical content identity used by native persistence and delivery sidecars.</summary>
        public string ContentHash { get { return GraphContentIdentity.Hash(this); } }
        public string OutputNodeId { get; }
        public IReadOnlyDictionary<string, GraphNode> Nodes { get; }
        public IReadOnlyList<GraphEdge> Edges { get; }
        public AuthoringGraph(string graphId, IEnumerable<GraphNode> nodes, IEnumerable<GraphEdge> edges, string outputNodeId)
        {
            Checks.Id(graphId);
            Checks.Require(nodes != null && edges != null, "INVALID_GRAPH", "Graph collections are required.");
            var items = nodes.Take(MaxNodes + 1).ToArray(); var links = edges.Take(MaxEdges + 1).ToArray();
            Checks.Require(items.Length <= MaxNodes && links.Length <= MaxEdges, "BUDGET_EXCEEDED", "Graph capacity exceeded.");
            var byId = new Dictionary<string, GraphNode>(StringComparer.Ordinal);
            foreach (var node in items)
            {
                Checks.Require(node != null && !byId.ContainsKey(node.NodeId), "DUPLICATE_NODE", "Node identity must be unique.");
                byId.Add(node.NodeId, node);
            }
            foreach (var link in links) Checks.Require(link != null, "INVALID_GRAPH", "Null graph edge.");
            if (!string.IsNullOrEmpty(outputNodeId)) Checks.Id(outputNodeId);
            GraphId = graphId; OutputNodeId = outputNodeId ?? "";
            Nodes = new ReadOnlyDictionary<string, GraphNode>(byId); Edges = Array.AsReadOnly(links);
            GraphValidator.Order(this);
        }
        public AuthoringGraph ReplaceNode(GraphNode node)
        {
            Checks.Require(node != null && Nodes.ContainsKey(node.NodeId), "NODE_NOT_FOUND", "Cannot replace a missing node.");
            return new AuthoringGraph(GraphId, Nodes.Values.Select(n => n.NodeId == node.NodeId ? node : n), Edges, OutputNodeId);
        }
        public AuthoringGraph WithEdges(IEnumerable<GraphEdge> edges)
        { return new AuthoringGraph(GraphId, Nodes.Values, edges, OutputNodeId); }
    }
}
