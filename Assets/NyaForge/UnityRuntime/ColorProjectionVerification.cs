using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;
using NyaForge.Authoring.Topology;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    internal static class ColorProjectionVerification
    {
        internal static void Verify(List<string> checks)
        {
            var parent=new GameObject("Color projection transaction fixture");var projection=new OwnedMeshProjection(parent.transform);
            void Require(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
            try
            {
                string source=Guid.NewGuid().ToString("D"),paint=Guid.NewGuid().ToString("D"),output=Guid.NewGuid().ToString("D");
                var polygon=PolygonPrimitives.Plane(Guid.NewGuid().ToString("D"));
                var graph=new AuthoringGraph(Guid.NewGuid().ToString("D"),new[]{GraphNode.Polygon(source,polygon,new RestTransform(1,new Vec3())),GraphNode.Paint(paint),GraphNode.Output(output)},
                    new[]{new GraphEdge(source,"mesh",paint,"mesh"),new GraphEdge(source,"mesh",output,"mesh"),new GraphEdge(paint,"image",output,"baseColor")},output);
                var context=PaintEditing.Context(graph,paint);
                graph=graph.ReplaceNode(GraphNode.Paint(paint,8,8,new PaintImage(8,8,new Rgba32(255,0,0)),context.UvHash,context.MeshDomain));
                var workspace=AuthoringWorkspace.CreateEmpty();var service=new AuthoringCommandService(workspace);
                Require(service.Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph))).Success,"Color fixture graph failed");
                using(var prepared=projection.PrepareGraph(workspace.Document,workspace.Preview)) prepared.Commit();
                var mesh=projection.DisplayMesh;var root=projection.DisplayObject;
                var renderer=root.transform.Find("Evaluated mesh").GetComponent<MeshRenderer>();var original=renderer.sharedMaterial;
                projection.ShowPaintPreview(new PaintImage(8,8,new Rgba32(255,255,0)));
                var preview=original.mainTexture;
                graph=graph.ReplaceNode(GraphNode.Paint(paint,8,8,new PaintImage(8,8,new Rgba32(0,0,255)),context.UvHash,context.MeshDomain));
                Require(service.Execute(workspace.NewCommand(AuthoringOperation.ReplaceGraph(graph))).Success,"Color fixture replacement failed");
                using(var prepared=projection.PrepareGraph(workspace.Document,workspace.Preview))
                {
                    Require(renderer.sharedMaterial==original && original.mainTexture==preview,"Prepare changed visible material");
                    prepared.Commit();
                    Require(projection.DisplayMesh==mesh && projection.DisplayObject==root && renderer.sharedMaterial!=original && !projection.HasPaintPreview,"Color swap rebuilt geometry or retained preview");
                    Require(((Texture2D)renderer.sharedMaterial.mainTexture).GetPixels32()[0].b==255,"Color swap did not display candidate image");
                    prepared.Rollback();Require(renderer.sharedMaterial==original && original.mainTexture==preview && projection.HasPaintPreview,"Rollback lost original material or pending preview");
                }
                using(var abandoned=projection.PrepareGraph(workspace.Document,workspace.Preview)) { }
                Require(renderer.sharedMaterial==original,"Abandoned preparation changed visible material");
                using(var first=projection.PrepareGraph(workspace.Document,workspace.Preview))
                using(var stale=projection.PrepareGraph(workspace.Document,workspace.Preview))
                {
                    first.Commit();bool rejected=false;
                    try { stale.Commit(); } catch(InvalidOperationException) { rejected=true; }
                    Require(rejected,"Concurrent stale color preparation was accepted");
                }
                var blue=renderer.sharedMaterial;
                var noColor=graph.WithEdges(graph.Edges.Where(e=>e.ToPort!="baseColor"));
                Require(service.Execute(workspace.NewCommand(AuthoringOperation.ReplaceGraph(noColor))).Success,"Color removal fixture failed");
                using(var prepared=projection.PrepareGraph(workspace.Document,workspace.Preview))
                {
                    prepared.Commit();Require(projection.DisplayMesh==mesh && renderer.sharedMaterial!=blue,"Color removal rebuilt mesh or retained material");
                    prepared.Rollback();Require(renderer.sharedMaterial==blue,"Color removal rollback failed");
                }
                var moved=polygon.MoveVertices(new[]{polygon.Vertices.Keys.First()},new Vec3(0,0,.01f));
                graph=graph.ReplaceNode(GraphNode.Polygon(source,moved,new RestTransform(1,new Vec3())));
                Require(service.Execute(workspace.NewCommand(AuthoringOperation.ReplaceGraph(graph))).Success,"Geometry replacement fixture failed");
                using(var prepared=projection.PrepareGraph(workspace.Document,workspace.Preview))
                {
                    prepared.Commit();Require(projection.DisplayMesh!=mesh,"Changed geometry incorrectly reused mesh");
                    prepared.Rollback();Require(projection.DisplayMesh==mesh && renderer.sharedMaterial==blue,"Geometry rollback failed after color swap");
                }
                checks.Add("Color-only projection transaction: prepare isolation, mesh/root reuse, rollback with pending preview, abandoned/stale candidates, color removal and geometry replacement");
            }
            finally { projection.Dispose();UnityEngine.Object.Destroy(parent); }
        }
    }
}
