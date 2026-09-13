using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Graph
{
    /// <summary>Immutable typed parameters and payload. No Unity objects or UI layout.</summary>
    public sealed partial class GraphNode
    {
        public string NodeId { get; }
        public string TypeId { get; }
        public int Version { get; }
        public MeshData SourceMesh { get; }
        public NyaForge.Authoring.Topology.PolygonMesh SourcePolygon { get; private set; }
        public SkeletonDefinition Skeleton { get; private set; }
        public SkinBinding Binding { get; private set; }
        public PoseSet Pose { get; private set; }
        /// <summary>Avatar object identity used by the last explicit pose copy.</summary>
        public string PoseSourceObjectId { get; private set; } = "";
        /// <summary>Source graph identity retained by a materialized clothing graph.</summary>
        public string DerivedFromGraphId { get; private set; } = "";
        /// <summary>Source graph content hash observed during materialization.</summary>
        public string DerivedFromGraphHash { get; private set; } = "";
        public MorphSet Morphs { get; private set; }
        public IReadOnlyDictionary<string, float> MorphWeights { get; private set; }
        /// <summary>Raw imported image kept separately from the bounded editable Paint preview.</summary>
        public GraphOriginalImage OriginalImage { get; private set; }
        /// <summary>Target avatar object identity for an object.attachment node.</summary>
        public string AttachmentTargetObjectId { get; private set; } = "";
        /// <summary>Stable target bone identity; names are never used as a fallback.</summary>
        public string AttachmentBoneId { get; private set; } = "";
        /// <summary>Skeleton content hash observed when the attachment was authored.</summary>
        public string AttachmentSkeletonHash { get; private set; } = "";
        /// <summary>Accessory root offset in the target bone's rest-local coordinates.</summary>
        public Vec3 AttachmentOffset { get; private set; }
        public static GraphNode PolygonEdit(string id, NyaForge.Authoring.Topology.PolygonMesh payload = null, string inputSnapshot = "", string domain = "", bool enabled = true)
        {
            if (payload != null) { Checks.HashText(inputSnapshot); Checks.HashText(domain); }
            return new GraphNode(id, BuiltinNodes.PolygonEdit, 1, null, Identity, 0, 0, 0, enabled, inputSnapshot, domain, Empty, "") { SourcePolygon = payload };
        }
        public static GraphNode Polygon(string id, NyaForge.Authoring.Topology.PolygonMesh mesh, RestTransform transform)
        {
            Checks.Require(mesh != null, "INVALID_MESH", "Polygon source is required.");
            return new GraphNode(id, BuiltinNodes.PolygonSource, 1, null, transform, 0, 0, 0, true, "", "", Empty, "") { SourcePolygon = mesh };
        }
        public RestTransform Transform { get; }
        public float Width { get; }
        public float Height { get; }
        public float Scalar { get; }
        public bool Enabled { get; }
        public string ExpectedInputSnapshot { get; }
        public string ExpectedDomain { get; }
        public IReadOnlyDictionary<int, Vec3> Offsets { get; }
        public string UnknownPayload { get; private set; }
        public bool UnknownPayloadIsText { get; private set; }
        public IReadOnlyList<byte> UnknownPayloadBytes { get; private set; }

        GraphNode(string id, string type, int version, MeshData mesh, RestTransform transform,
            float width, float height, float scalar, bool enabled, string inputSnapshot,
            string domain, IDictionary<int, Vec3> offsets, string unknown)
        {
            Checks.Id(id); Checks.Name(type);
            Checks.Require(version > 0, "INVALID_NODE_VERSION", "Node version must be positive.");
            transform.Validate(); Checks.Finite(width); Checks.Finite(height); Checks.Finite(scalar);
            Checks.Require(offsets != null && offsets.Count <= AuthoringLimits.MaxVertices, "BUDGET_EXCEEDED", "Edit payload exceeds capacity.");
            var copy = new Dictionary<int, Vec3>();
            foreach (var pair in offsets)
            {
                Checks.Require(pair.Key >= 0, "INVALID_VERTEX", "Negative vertex identity."); Checks.Finite(pair.Value); copy.Add(pair.Key, pair.Value);
            }
            NodeId = id; TypeId = type; Version = version; SourceMesh = mesh; Transform = transform;
            Width = width; Height = height; Scalar = scalar; Enabled = enabled;
            ExpectedInputSnapshot = inputSnapshot ?? ""; ExpectedDomain = domain ?? "";
            if (ExpectedInputSnapshot != "") Checks.HashText(ExpectedInputSnapshot);
            if (ExpectedDomain != "") Checks.HashText(ExpectedDomain);
            Checks.Require(unknown != null && unknown.Length <= 8192, "BUDGET_EXCEEDED", "Unknown node payload exceeds capacity.");
            UnknownPayload = unknown; UnknownPayloadIsText = true;
            UnknownPayloadBytes = Array.AsReadOnly(new UTF8Encoding(false,true).GetBytes(unknown));
            Offsets = new ReadOnlyDictionary<int, Vec3>(copy);
            MorphWeights = new ReadOnlyDictionary<string, float>(new Dictionary<string, float>(StringComparer.Ordinal));
        }
        static RestTransform Identity { get { return new RestTransform(1, new Vec3()); } }
        static IDictionary<int, Vec3> Empty { get { return new Dictionary<int, Vec3>(); } }
        public static GraphNode Source(string id, MeshData mesh, RestTransform transform)
        {
            Checks.Require(mesh != null, "INVALID_MESH", "A source needs mesh data.");
            return new GraphNode(id, BuiltinNodes.MeshSource, 1, mesh, transform, 0, 0, 0, true, "", "", Empty, "");
        }
        public static GraphNode Plane(string id, float width = .2f, float height = .1f)
        {
            PrimitiveGeometry.ValidatePlane(width, height);
            return new GraphNode(id, BuiltinNodes.Plane, 1, null, Identity, width, height, 0, true, "", "", Empty, "");
        }
        public static GraphNode SkeletonNode(string id, SkeletonDefinition skeleton)
        {
            Checks.Require(skeleton != null, "INVALID_SKELETON", "Skeleton payload is required.");
            return new GraphNode(id, BuiltinNodes.Skeleton, 1, null, Identity, 0, 0, 0, true, "", "", Empty, "") { Skeleton = skeleton };
        }
        public static GraphNode SkinBindNode(string id, SkinBinding binding)
        {
            Checks.Require(binding != null, "INVALID_SKIN", "Skin binding payload is required.");
            return new GraphNode(id, BuiltinNodes.SkinBind, 1, null, Identity, 0, 0, 0, true, "", "", Empty, "") { Binding = binding };
        }
        public static GraphNode PoseNode(string id, PoseSet pose)
        {
            Checks.Require(pose != null, "INVALID_POSE", "Pose payload is required.");
            return new GraphNode(id, BuiltinNodes.Pose, 1, null, Identity, 0, 0, 0, true, "", "", Empty, "") { Pose = pose };
        }
        public static GraphNode SkinDeformNode(string id)
        { return new GraphNode(id, BuiltinNodes.SkinDeform, 1, null, Identity, 0, 0, 0, true, "", "", Empty, ""); }
        public static GraphNode PoseSourceNode(string id, string objectId)
        {
            Checks.Id(objectId);
            var node = new GraphNode(id, BuiltinNodes.PoseSource, 1, null, Identity, 0, 0, 0, true, "", "", Empty, "");
            node.PoseSourceObjectId = objectId;
            return node;
        }
        public static GraphNode DerivedSourceNode(string id, string sourceGraphId, string sourceGraphHash)
        {
            Checks.Id(sourceGraphId); Checks.HashText(sourceGraphHash);
            var node = new GraphNode(id, BuiltinNodes.DerivedSource, 1, null, Identity, 0, 0, 0, true, "", "", Empty, "");
            node.DerivedFromGraphId = sourceGraphId; node.DerivedFromGraphHash = sourceGraphHash;
            return node;
        }
        public static GraphNode Edit(string id, bool enabled = true, IDictionary<int, Vec3> offsets = null, string inputSnapshot = "", string domain = "")
        { return new GraphNode(id, BuiltinNodes.EditMesh, 1, null, Identity, 0, 0, 0, enabled, inputSnapshot, domain, offsets ?? Empty, ""); }
        public static GraphNode Output(string id)
        { return new GraphNode(id, BuiltinNodes.Output, 1, null, Identity, 0, 0, 0, true, "", "", Empty, ""); }
        public static GraphNode Number(string id, float value)
        { return new GraphNode(id, BuiltinNodes.Scalar, 1, null, Identity, 0, 0, value, true, "", "", Empty, ""); }
        public static GraphNode AttachmentNode(string id, string targetObjectId, string boneId, string skeletonHash, Vec3 offset)
        {
            Checks.Id(targetObjectId); Checks.Id(boneId); Checks.HashText(skeletonHash); Checks.Finite(offset);
            var node = new GraphNode(id, BuiltinNodes.Attachment, 1, null, Identity, 0, 0, 0, true, "", "", Empty, "");
            node.AttachmentTargetObjectId = targetObjectId; node.AttachmentBoneId = boneId;
            node.AttachmentSkeletonHash = skeletonHash; node.AttachmentOffset = offset;
            return node;
        }
        public static GraphNode Unknown(string id, string type, int version, string payload)
        {
            var node = new GraphNode(id, type, version, null, Identity, 0, 0, 0, true, "", "", Empty, payload);
            Checks.Require(BuiltinNodes.Find(node) == null, "INVALID_NODE", "Known nodes require their typed factory.");
            return node;
        }
        public static GraphNode UnknownBinary(string id, string type, int version, byte[] payload)
        {
            Checks.Require(payload != null && payload.Length <= 32768, "BUDGET_EXCEEDED", "Opaque node payload exceeds capacity.");
            var node = Unknown(id,type,version,"");
            node.UnknownPayloadBytes = Array.AsReadOnly((byte[])payload.Clone());
            try { node.UnknownPayload = new UTF8Encoding(false,true).GetString(payload); }
            catch (DecoderFallbackException)
            {
                node.UnknownPayloadIsText = false;
                node.UnknownPayload = Convert.ToBase64String(payload);
            }
            return node;
        }
    }
}
