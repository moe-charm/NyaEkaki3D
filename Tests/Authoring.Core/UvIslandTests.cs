using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunUvIslandTests()
    {
        Test("UV connectivity follows shared endpoints and respects projected seams", () =>
        {
            var polygon = TriangleMeshAdapter.Import(GraphId(), PrimitiveGeometry.Plane(.2f,.1f));
            var ids = UvIslands.Expand(polygon,new[]{polygon.Faces[0].Id}); Equal(2,ids.Count);
            var separated = PolygonUvProjection.Apply(polygon);
            Equal(1,UvIslands.Expand(separated,new[]{separated.Faces[0].Id}).Count);
            var moved = UvIslandTransform.Apply(polygon,new[]{polygon.Faces[0].Id},new UvTransformSettings(new Vec2(.1f,.2f)));
            foreach(var face in polygon.Faces)
                foreach(var c in face.Corners)
                {
                    var changed=moved.Faces.SelectMany(f=>f.Corners).Single(v=>v.Id==c.Id);
                    Near(c.Uv0.Value.X+.1f,changed.Uv0.Value.X);Near(c.Uv0.Value.Y+.2f,changed.Uv0.Value.Y);
                    Equal(polygon.Vertices[c.VertexId].Position,moved.Vertices[c.VertexId].Position);
                }
        });
        Test("UV island rotation rescales UVs and reconstructs anisotropic tangents", () =>
        {
            var p=PolygonPrimitives.Plane(GraphId(),.2f,.1f);
            var changed=UvIslandTransform.Apply(p,new ulong[]{1},new UvTransformSettings(new Vec2(),45,.5f));
            var t=changed.Faces[0].Corners[0].Tangent.Value;
            Near(.8944272f,Math.Abs(t.X));Near(.4472136f,Math.Abs(t.Y));Near(0,t.Z);
            var old=p.Faces[0].Corners[0].Uv0.Value;var next=changed.Faces[0].Corners[0].Uv0.Value;
            Near((float)Math.Sqrt((old.X-.5f)*(old.X-.5f)+(old.Y-.5f)*(old.Y-.5f))*.5f,
                (float)Math.Sqrt((next.X-.5f)*(next.X-.5f)+(next.Y-.5f)*(next.Y-.5f)));
            Expect("INVALID_UV_SCALE",()=>new UvTransformSettings(new Vec2(),0,0));
        });
        Test("UV island command retry Undo native and Bake", () =>
        {
            string source=GraphId(),edit=GraphId(),output=GraphId();
            var graph=new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,PolygonPrimitives.Plane(GraphId()),new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(output)},
                new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",output,"mesh")},output);
            var w=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(w);Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph))));string before=w.Document.StateHash;
            var command=w.NewCommand(AuthoringOperation.TransformUvIslands(GraphEditing.Context(graph,edit),new ulong[]{1},new UvTransformSettings(new Vec2(.05f,0),30,.7f)));
            Ok(commands.Execute(command));string after=w.Document.StateHash;Ok(commands.Execute(command));Equal(after,w.Document.StateHash);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Undo())));Equal(before,w.Document.StateHash);Ok(commands.Execute(w.NewCommand(AuthoringOperation.Redo())));Equal(after,w.Document.StateHash);
            string dir=Dir("uv-island");ProjectStore.Save(dir,w,0);var reopened=ProjectStore.Open(dir);Equal(after,reopened.Document.StateHash);
            Equal(w.Evaluate().ContentHash,BakeStore.Read(BakeStore.Export(System.IO.Path.Combine(dir,"bake"),reopened)).Mesh.ContentHash);
        });
    }
}
