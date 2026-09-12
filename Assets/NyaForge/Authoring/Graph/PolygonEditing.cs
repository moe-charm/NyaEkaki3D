using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Topology;

namespace NyaForge.Authoring.Graph
{
    public static class PolygonEditing
    {
        public static AuthoringGraph CutPath(AuthoringGraph graph,GraphEditContext context,IEnumerable<EdgeCutLocation> path)
        { return Apply(graph,context,(polygon,_)=>PolygonCutPath.Cut(polygon,path)); }
        public static AuthoringGraph CutEdges(AuthoringGraph graph,GraphEditContext context,IEnumerable<ulong> endpoints,float first,float second)
        { return Apply(graph,context,(polygon,_)=>PolygonEdgeCut.Cut(polygon,endpoints,first,second)); }
        public static AuthoringGraph DissolveFaces(AuthoringGraph graph,GraphEditContext context,IEnumerable<ulong> faces)
        { return Apply(graph,context,(polygon,_)=>PolygonFaceDissolve.Dissolve(polygon,faces)); }
        public static AuthoringGraph AddVertex(AuthoringGraph graph,GraphEditContext context,Vec3 position)
        { return Apply(graph,context,(polygon,_)=>PolygonVertexCreation.Add(polygon,position),true); }
        public static AuthoringGraph CreateFace(AuthoringGraph graph,GraphEditContext context,IEnumerable<ulong> perimeter,int material)
        { return Apply(graph,context,(polygon,_)=>PolygonFaceCreation.Create(polygon,perimeter,material),true); }
        public static AuthoringGraph Weld(AuthoringGraph graph,GraphEditContext context,IEnumerable<ulong> vertices)
        { return Apply(graph,context,(polygon,_)=>PolygonWeld.AtCenter(polygon,vertices)); }
        public static AuthoringGraph Bridge(AuthoringGraph graph,GraphEditContext context,IEnumerable<ulong> vertices,int offset)
        {
            var ids=vertices.ToArray();
            return Apply(graph,context,(polygon,_)=>PolygonBridge.Connect(polygon,ids.Take(ids.Length/2),ids.Skip(ids.Length/2),offset));
        }
        public static AuthoringGraph InsertEdgeVertex(AuthoringGraph graph,GraphEditContext context,IEnumerable<ulong> vertices,float fraction)
        { return Apply(graph,context,(polygon,_)=>PolygonEdgeInsertion.Insert(polygon,vertices,fraction)); }
        public static AuthoringGraph SplitFace(AuthoringGraph graph,GraphEditContext context,IEnumerable<ulong> vertices)
        { return Apply(graph,context,(polygon,_)=>PolygonFaceSplit.Split(polygon,vertices)); }
        public static AuthoringGraph MergeFaces(AuthoringGraph graph,GraphEditContext context,IEnumerable<ulong> faces)
        { return Apply(graph,context,(polygon,_)=>PolygonFaceMerge.Merge(polygon,faces)); }
        public static AuthoringGraph FillBoundary(AuthoringGraph graph,GraphEditContext context,IEnumerable<ulong> vertices)
        { return Apply(graph,context,(polygon,_)=>PolygonCap.Fill(polygon,vertices)); }
        public static AuthoringGraph DeleteFaces(AuthoringGraph graph,GraphEditContext context,IEnumerable<ulong> faces)
        { return Apply(graph,context,(polygon,_)=>PolygonDeletion.DeleteFaces(polygon,faces)); }
        public static AuthoringGraph AssignMaterial(AuthoringGraph graph,GraphEditContext context,IEnumerable<ulong> faces,int slot)
        { return Apply(graph,context,(polygon,_)=>PolygonMaterialAssignment.Assign(polygon,faces,slot)); }
        public static AuthoringGraph Translate(AuthoringGraph graph, GraphEditContext context, IEnumerable<ulong> ids, Vec3 restDelta)
        { return Apply(graph, context, (polygon, transform) => polygon.MoveVertices(ids, transform.ToLocalVector(restDelta)),true); }

        public static AuthoringGraph Extrude(AuthoringGraph graph, GraphEditContext context, IEnumerable<ulong> faceIds, Vec3 restDelta)
        { return Apply(graph, context, (polygon, transform) => PolygonExtrusion.Extrude(polygon, faceIds, transform.ToLocalVector(restDelta))); }

        public static AuthoringGraph Solidify(AuthoringGraph graph, GraphEditContext context, float restThickness)
        { return Apply(graph, context, (polygon, transform) => PolygonSolidify.Apply(polygon, restThickness / transform.Scale)); }

        public static AuthoringGraph ProjectUv(AuthoringGraph graph, GraphEditContext context)
        { return Apply(graph, context, (polygon, _) => PolygonUvProjection.Apply(polygon)); }

        public static AuthoringGraph TransformUv(AuthoringGraph graph, GraphEditContext context, IEnumerable<ulong> faces, UvTransformSettings settings)
        { return Apply(graph, context, (polygon, _) => UvIslandTransform.Apply(polygon, faces, settings)); }

        static AuthoringGraph Apply(AuthoringGraph graph, GraphEditContext context, System.Func<PolygonMesh, RestTransform, PolygonMesh> change,bool allowFaceless=false)
        {
            Checks.Require(context != null && context.GraphId == graph.GraphId, "EDIT_CONTEXT_STALE", "Edit context belongs to another graph.");
            var current = GraphEditing.Context(graph, context.NodeId); var node = graph.Nodes[context.NodeId];
            Checks.Require(node.TypeId == BuiltinNodes.PolygonEdit, "EDIT_MODE_UNSUPPORTED", "Choose PolygonEdit for stable-ID editing.");
            Checks.Require(current.InputSnapshot == context.InputSnapshot && current.DomainId == context.DomainId, "EDIT_CONTEXT_STALE", "Reacquire the changed input context.");
            Checks.Require(node.SourcePolygon == null || node.ExpectedInputSnapshot == current.InputSnapshot && node.ExpectedDomain == current.DomainId, "EDIT_INPUT_CHANGED", "Retain or explicitly rebase the old polygon edit.");
            var input = GraphEvaluator.Evaluate(graph).MeshInputs[context.NodeId];
            var polygon = node.SourcePolygon ?? input.Polygon;
            Checks.Require(allowFaceless || polygon.Faces.Count>0,"NO_RENDERABLE_FACES","Create a face before this operation.");
            var changed = change(polygon, input.Transform);
            if(changed.Faces.Count>0) PolygonRenderAdapter.Build(changed); // Reject invalid geometry before creating a committable candidate.
            return graph.ReplaceNode(GraphNode.PolygonEdit(node.NodeId, changed, current.InputSnapshot, current.DomainId));
        }
    }
}



