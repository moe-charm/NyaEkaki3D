using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring
{
    /// <summary>Immutable static mesh asset and its rest-space editing payload.</summary>
    public sealed class AuthoringObject
    {
        public string ObjectId { get; private set; }
        public AuthoringGraph Graph { get; private set; }
        public bool IsStaticProfile { get; private set; }
        readonly string sourceId, editId;
        public RestTransform Transform { get { RequireStaticProfile(); return Graph.Nodes[sourceId].Transform; } }
        public MeshData BaselineMesh { get { RequireStaticProfile(); return Graph.Nodes[sourceId].SourceMesh; } }
        public bool LayerEnabled { get { RequireStaticProfile(); return Graph.Nodes[editId].Enabled; } }
        public IReadOnlyDictionary<int, Vec3> Offsets { get { RequireStaticProfile(); return Graph.Nodes[editId].Offsets; } }

        internal AuthoringObject(string id, AuthoringGraph graph)
        {
            Checks.Id(id);
            Checks.Require(graph != null, "INVALID_DOCUMENT", "Missing object graph.");
            ObjectId = id; Graph = graph;
        }

        void RequireStaticProfile()
        {
            Checks.Require(IsStaticProfile, "GRAPH_PROFILE_REQUIRED", "Use graph evaluation and an explicit edit context for this object.");
        }

        public GraphEvaluation EvaluateGraph() { return GraphEvaluator.Evaluate(Graph); }

        internal AuthoringObject(string id, RestTransform transform, MeshData mesh, bool enabled, IDictionary<int, Vec3> offsets)
        {
            Checks.Id(id); transform.Validate();
            Checks.Require(mesh != null && offsets != null && offsets.Count <= mesh.VertexCount, "INVALID_DOCUMENT", "Missing geometry or invalid offsets.");
            foreach (var pair in offsets)
            {
                Checks.Require(pair.Key >= 0 && pair.Key < mesh.VertexCount, "INVALID_VERTEX", "Offset is outside its baseline domain.");
                Checks.Finite(pair.Value);
            }
            ObjectId = id; sourceId = StaticMeshGraph.Id(id, "source"); editId = StaticMeshGraph.Id(id, "edit");
            Graph = StaticMeshGraph.Create(id, mesh, transform, enabled, offsets);
            IsStaticProfile = true;
        }

        public MeshData Evaluate()
        {
            var result = GraphEvaluator.Evaluate(Graph);
            if (!result.IsComplete)
            {
                var failure = result.Diagnostics.First();
                throw new AuthoringException(failure.Code, failure.Message);
            }
            return result.Output.Mesh;
        }
    }
}
