using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace NyaForge.Authoring
{
    public sealed class CommandEnvelope
    {
        public string ExpectedInstanceId { get; set; }
        public string DocumentId { get; set; }
        public long ExpectedDocumentRevision { get; set; }
        public string CommandId { get; set; }
        public string ObjectId { get; set; }
        public string ExpectedBaselineHash { get; set; }
        public AuthoringOperation[] Operations { get; set; }
    }
    public sealed partial class AuthoringOperation
    {
        public string Kind { get; private set; }
        public IReadOnlyList<int> VertexIds { get; private set; }
        public Vec3 Delta { get; private set; }
        public bool Enabled { get; private set; }
        public MeshData Mesh { get; private set; }
        public RestTransform Transform { get; private set; }
        public string NewObjectId { get; private set; }
        public NyaForge.Authoring.Graph.AuthoringGraph Graph { get; private set; }
        public static AuthoringOperation ReplaceGraph(NyaForge.Authoring.Graph.AuthoringGraph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            return new AuthoringOperation("graph.replace", Array.Empty<int>(), new Vec3(), false) { Graph = graph };
        }
        private AuthoringOperation(string kind, int[] ids, Vec3 delta, bool enabled) { Kind = kind; VertexIds = Array.AsReadOnly((int[])ids.Clone()); Delta = delta; Enabled = enabled; }
        public static AuthoringOperation TranslateVertices(int[] vertexIds, Vec3 restDelta) { if (vertexIds == null) throw new ArgumentNullException("vertexIds"); return new AuthoringOperation("vertices.translate",vertexIds,restDelta,false); }
        public static AuthoringOperation SetLayerEnabled(bool value) { return new AuthoringOperation("layer.enabled",new int[0],new Vec3(),value); }
        public static AuthoringOperation Undo() { return new AuthoringOperation("history.undo",new int[0],new Vec3(),false); }
        public static AuthoringOperation Redo() { return new AuthoringOperation("history.redo",new int[0],new Vec3(),false); }
        public static AuthoringOperation AddMesh(MeshData mesh, RestTransform transform)
        {
            if (mesh == null) throw new ArgumentNullException("mesh");
            transform.Validate();
            return new AuthoringOperation("object.add_mesh", Array.Empty<int>(), new Vec3(), false)
            { Mesh = mesh, Transform = transform, NewObjectId = Guid.NewGuid().ToString("D") };
        }
    }
    public sealed class CommandResult
    {
        public bool Success { get; internal set; }
        public string Code { get; internal set; }
        public string Message { get; internal set; }
        public long DocumentRevision { get; internal set; }
        public string MeshContentHash { get; internal set; }
        public bool EvaluationComplete { get; internal set; }
        public long? PreviewRevision { get; internal set; }
    }
    internal sealed class CachedCommand { internal string Fingerprint; internal CommandResult Result; }
}
