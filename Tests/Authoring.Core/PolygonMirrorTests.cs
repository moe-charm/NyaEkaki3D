using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunPolygonMirrorTests()
    {
        foreach (int axis in new[] { 0,1,2 })
        Test("mirror preserves source and reflects corner frame axis"+axis, () =>
        {
            var p = PolygonPrimitives.Plane(GraphId()); var m = PolygonMirror.Apply(p,GraphId(),axis,.3f);
            Equal(8,m.Vertices.Count); Equal(2,m.Faces.Count); Equal(4,PolygonRenderAdapter.Build(m).Mesh.TriangleCount);
            True(ReferenceEquals(p.Faces[0],m.Faces[0]));
            foreach (var v in p.Vertices.Values)
            {
                var a = v.Position; var b = m.Vertices[v.Id+4].Position;
                Near(axis == 0 ? .6f-a.X : a.X,b.X); Near(axis == 1 ? .6f-a.Y : a.Y,b.Y); Near(axis == 2 ? .6f-a.Z : a.Z,b.Z);
            }
            var corners = p.Faces[0].Corners.Reverse().ToArray(); var reflected = m.Faces[1];
            for (int i=0;i<4;i++) { Equal(corners[i].VertexId+4,reflected.Corners[i].VertexId); Equal(corners[i].Uv0.Value,reflected.Corners[i].Uv0.Value); Near(-corners[i].Tangent.Value.W,reflected.Corners[i].Tangent.Value.W); }
            var normal = PolygonExtrusion.FaceNormal(m,reflected); var attribute = reflected.Corners[0].Normal.Value;
            Near(normal.X,attribute.X); Near(normal.Y,attribute.Y); Near(normal.Z,attribute.Z);
        });
        foreach (float scale in new[] { 1f,100f })
        Test("mirror graph updates downstream and native Bake roundtrip scale"+scale, () =>
        {
            string source=GraphId(),edit=GraphId(),mirror=GraphId(),output=GraphId(); var polygon=PolygonPrimitives.Plane(GraphId());
            var graph=new AuthoringGraph(GraphId(),new[] { GraphNode.Polygon(source,polygon,new RestTransform(scale,new Vec3(.3f,0,0))),GraphNode.PolygonEdit(edit),GraphNode.Mirror(mirror,0,.1f),GraphNode.Output(output) },
                new[] {new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",mirror,"mesh"),new GraphEdge(mirror,"mesh",output,"mesh")},output);
            var w=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(w);Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph))));
            void CheckSymmetry()
            {
                var value=w.Preview.Output;
                for(ulong id=1;id<=4;id++) Near(.2f,value.Transform.ToAvatarPoint(value.Polygon.Vertices[id].Position).X+value.Transform.ToAvatarPoint(value.Polygon.Vertices[id+4].Position).X);
            }
            CheckSymmetry();string before=w.Document.StateHash;
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.TranslatePolygonVertices(GraphEditing.Context(graph,edit),new ulong[]{1},new Vec3(.01f,0,0)))));
            CheckSymmetry();string after=w.Document.StateHash;True(before!=after);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Undo())));Equal(before,w.Document.StateHash);Ok(commands.Execute(w.NewCommand(AuthoringOperation.Redo())));Equal(after,w.Document.StateHash);
            string dir=Dir("mirror-"+scale);ProjectStore.Save(dir,w,0);var reopened=ProjectStore.Open(dir);Equal(after,reopened.Document.StateHash);
            Equal(4,BakeStore.Read(BakeStore.Export(System.IO.Path.Combine(dir,"bake"),reopened)).Mesh.TriangleCount);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.UpdateNode(GraphNode.Mirror(mirror,0,.1f,false)))));Equal(1,w.Preview.Output.Polygon.Faces.Count);
        });
    }
}
