using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunPolygonUvTests()
    {
        Test("UV face projection packs shell islands and preserves geometry identities", () =>
        {
            var source = PolygonSolidify.Apply(PolygonPrimitives.Plane(GraphId()), .02f);
            var result = PolygonUvProjection.Apply(source);
            Equal(source.DomainId, result.DomainId); Equal(source.Vertices.Count, result.Vertices.Count);
            foreach (var face in source.Faces)
            {
                var updated = result.Faces.Single(f => f.Id == face.Id);
                True(face.Corners.Select(c => c.Id).SequenceEqual(updated.Corners.Select(c => c.Id)));
                foreach (var c in updated.Corners)
                {
                    var uv = c.Uv0.Value; True(uv.X > 0 && uv.X < 1 && uv.Y > 0 && uv.Y < 1);
                    var n = c.Normal.Value; var t = c.Tangent.Value;
                    Near(0, n.X*t.X+n.Y*t.Y+n.Z*t.Z); Near(1,t.X*t.X+t.Y*t.Y+t.Z*t.Z);
                }
            }
            var boxes = result.Faces.Select(f => (minU:f.Corners.Min(c=>c.Uv0.Value.X),maxU:f.Corners.Max(c=>c.Uv0.Value.X),minV:f.Corners.Min(c=>c.Uv0.Value.Y),maxV:f.Corners.Max(c=>c.Uv0.Value.Y))).ToArray();
            for(int i=0;i<boxes.Length;i++) for(int j=i+1;j<boxes.Length;j++)
                True(boxes[i].maxU<boxes[j].minU || boxes[j].maxU<boxes[i].minU || boxes[i].maxV<boxes[j].minV || boxes[j].maxV<boxes[i].minV);
            foreach(var p in source.Vertices) Equal(p.Value.Position,result.Vertices[p.Key].Position);
            Equal(PolygonRenderAdapter.Build(result).Mesh.ContentHash,PolygonRenderAdapter.Build(PolygonUvProjection.Apply(result)).Mesh.ContentHash);
        });
        Test("UV command Undo native and Bake retains new UV coordinates", () =>
        {
            string source=GraphId(),edit=GraphId(),output=GraphId();
            var graph=new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,PolygonSolidify.Apply(PolygonPrimitives.Plane(GraphId()),.02f),new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(output)},
                new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",output,"mesh")},output);
            var w=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(w);Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph))));
            string before=w.Document.StateHash;Ok(commands.Execute(w.NewCommand(AuthoringOperation.ProjectPolygonUv(GraphEditing.Context(graph,edit)))));string after=w.Document.StateHash;True(before!=after);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Undo())));Equal(before,w.Document.StateHash);Ok(commands.Execute(w.NewCommand(AuthoringOperation.Redo())));Equal(after,w.Document.StateHash);
            string dir=Dir("uv-project");ProjectStore.Save(dir,w,0);var reopened=ProjectStore.Open(dir);Equal(after,reopened.Document.StateHash);
            var bake=BakeStore.Read(BakeStore.Export(System.IO.Path.Combine(dir,"bake"),reopened));Equal(w.Evaluate().ContentHash,bake.Mesh.ContentHash);
        });
    }
}
