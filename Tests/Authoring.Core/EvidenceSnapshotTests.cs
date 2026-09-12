using System;
using NyaForge.Authoring;
using NyaForge.Authoring.Evidence;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;
internal static partial class Program
{
    static void RunEvidenceSnapshotTests()
    {
        Test("evidence snapshot stays fixed across edit undo and distinguishes identity from content",()=>
        {
            var w=AuthoringWorkspace.CreateFixture();var snapshot=EvaluatedSnapshot.Acquire(w);var again=EvaluatedSnapshot.Acquire(w);
            Equal(EvidenceState.Ready,snapshot.State);True(snapshot.SnapshotId!=again.SnapshotId);Equal(snapshot.OutputContentHash,again.OutputContentHash);
            Equal(w.Document.StateHash,snapshot.StateHash);Equal(w.Evaluate().TriangleCount,snapshot.Metrics.TriangleCount);
            string meshHash=snapshot.Value.Mesh.ContentHash;var bounds=snapshot.Metrics.BoundsMin;
            Ok(Execute(w,AuthoringOperation.TranslateVertices(new[]{0},new Vec3(1,0,0))));
            True(EvaluatedSnapshot.Acquire(w).OutputContentHash!=snapshot.OutputContentHash);Equal(meshHash,snapshot.Value.Mesh.ContentHash);Equal(bounds,snapshot.Metrics.BoundsMin);
            Ok(Execute(w,AuthoringOperation.Undo()));var undone=EvaluatedSnapshot.Acquire(w);Equal(snapshot.OutputContentHash,undone.OutputContentHash);True(undone.DocumentRevision>snapshot.DocumentRevision);
            True(snapshot.Metrics.LogicalVertexCount==null);True(snapshot.Diagnostics.Count==0);
        });
        Test("evidence never labels stale preview as current geometry",()=>
        {
            string plane,edit;var graph=PlaneGraph(out plane,out edit);var w=AuthoringWorkspace.CreateEmpty();Ok(Execute(w,AuthoringOperation.AddGraph(graph)));
            var ready=EvaluatedSnapshot.Acquire(w);Ok(Execute(w,AuthoringOperation.Disconnect(edit,"mesh")));var incomplete=EvaluatedSnapshot.Acquire(w);
            Equal(EvidenceState.Incomplete,incomplete.State);True(incomplete.Value==null && incomplete.Metrics==null && incomplete.OutputContentHash==null);
            Equal((long?)ready.DocumentRevision,incomplete.StalePreviewRevision);True(incomplete.Diagnostics.Count>0);True(ready.Value!=null);
        });
        Test("evidence empty and faceless are distinct and loose point bounds use world transform",()=>
        {
            var w=AuthoringWorkspace.CreateEmpty();var empty=EvaluatedSnapshot.Acquire(w);Equal(EvidenceState.Empty,empty.State);True(empty.Value==null && empty.Metrics==null && empty.GraphId==null);
            string source=GraphId(),output=GraphId();var polygon=new PolygonMesh(GraphId(),Array.Empty<CageVertex>(),Array.Empty<CageFace>());
            AuthoringGraph Graph(PolygonMesh p)=>new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,p,new RestTransform(100,new Vec3(1,2,3))),GraphNode.Output(output)},new[]{new GraphEdge(source,"mesh",output,"mesh")},output);
            Ok(Execute(w,AuthoringOperation.AddGraph(Graph(polygon))));var zero=EvaluatedSnapshot.Acquire(w);
            Equal(EvidenceState.Faceless,zero.State);Equal((int?)0,zero.Metrics.LogicalVertexCount);True(zero.Metrics.BoundsMin==null);
            var one=new PolygonMesh(polygon.DomainId,new[]{new CageVertex(1,new Vec3(.01f,.02f,.03f))},Array.Empty<CageFace>());
            Ok(Execute(w,AuthoringOperation.ReplaceGraph(Graph(one))));var point=EvaluatedSnapshot.Acquire(w);
            Equal(EvidenceState.Faceless,point.State);Equal(0,point.Metrics.RenderVertexCount);Equal((int?)1,point.Metrics.LogicalVertexCount);
            Near(2,point.Metrics.BoundsMin.Value.X);Near(4,point.Metrics.BoundsMin.Value.Y);Near(6,point.Metrics.BoundsMin.Value.Z);
            True(zero.Metrics.BoundsMin==null);True(point.Metrics.ImageHashes.Count==0);
        });
    }
}
