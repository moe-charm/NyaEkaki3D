using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunPolygonSolidifyTests()
    {
        Test("solidify quad creates closed shell with original identity and reversed inner attributes", () =>
        {
            var original = PolygonPrimitives.Plane(GraphId()); var shell = PolygonSolidify.Apply(original, .02f);
            Equal(8, shell.Vertices.Count); Equal(6, shell.Faces.Count); Equal(12, PolygonRenderAdapter.Build(shell).Mesh.TriangleCount);
            True(shell.EdgeFaces.Values.All(e => e.Count == 2)); True(ReferenceEquals(original.Faces[0], shell.Faces[0]));
            var inside = shell.Faces[1]; var source = original.Faces[0].Corners.Reverse().ToArray();
            for (int i = 0; i < inside.Corners.Count; i++)
            {
                Near(.02f, shell.Vertices[inside.Corners[i].VertexId].Position.Z);
                Equal(source[i].Uv0.Value, inside.Corners[i].Uv0.Value);
                Near(-source[i].Normal.Value.Z, inside.Corners[i].Normal.Value.Z);
                Near(-source[i].Tangent.Value.W, inside.Corners[i].Tangent.Value.W);
            }
            foreach (var face in shell.Faces)
                for (int i = 0; i < face.Corners.Count; i++)
                {
                    ulong a = face.Corners[i].VertexId, b = face.Corners[(i + 1) % face.Corners.Count].VertexId;
                    var other = shell.Faces.Single(f => f.Id == shell.EdgeFaces[new CageEdgeId(a, b)].Single(id => id != face.Id));
                    True(Enumerable.Range(0, other.Corners.Count).Any(j => other.Corners[j].VertexId == b && other.Corners[(j + 1) % other.Corners.Count].VertexId == a));
                }
            Expect("INVALID_THICKNESS", () => PolygonSolidify.Apply(original, 0));
            Expect("INVALID_THICKNESS", () => PolygonSolidify.Apply(original, -.01f));
        });
        Test("solidify bent surface closes all boundaries without inventing absent attributes", () =>
        {
            var vertices = new[] { new CageVertex(1,new Vec3()),new CageVertex(2,new Vec3(1,0,0)),new CageVertex(3,new Vec3(1,1,0)),new CageVertex(4,new Vec3(0,1,0)),new CageVertex(5,new Vec3(1,0,1)),new CageVertex(6,new Vec3(1,1,1)) };
            var faces = new[] { new CageFace(1,0,new[] { new CageCorner(1,1),new CageCorner(2,2),new CageCorner(3,3),new CageCorner(4,4) }),
                new CageFace(2,1,new[] { new CageCorner(5,3),new CageCorner(6,2),new CageCorner(7,5),new CageCorner(8,6) }) };
            var shell = PolygonSolidify.Apply(new PolygonMesh(GraphId(),vertices,faces), .05f);
            Equal(10,shell.Faces.Count); True(shell.EdgeFaces.Values.All(e => e.Count == 2));
            True(shell.Faces.SelectMany(f => f.Corners).All(c => !c.Normal.HasValue && !c.Uv0.HasValue && !c.Tangent.HasValue));
            True(shell.Faces.Any(f => f.Material == 1));
        });
        Test("solidify rejects inconsistent winding and nonmanifold edges", () =>
        {
            var original = PolygonPrimitives.Plane(GraphId()); var face = original.Faces[0];
            CageFace Copy(ulong id, ulong offset) => new CageFace(id,0,face.Corners.Select(c => new CageCorner(c.Id+offset,c.VertexId,c.Uv0,c.Normal,c.Tangent)));
            Expect("INCONSISTENT_WINDING", () => PolygonSolidify.Apply(new PolygonMesh(original.DomainId,original.Vertices.Values,new[] { face,Copy(2,4) }),.01f));
            Expect("NONMANIFOLD_SELECTION", () => PolygonSolidify.Apply(new PolygonMesh(original.DomainId,original.Vertices.Values,new[] { face,Copy(2,4),Copy(3,8) }),.01f));
        });
        Test("solidify rejects surfaces touching only at a shared vertex", () =>
        {
            var vertices = new[] { new CageVertex(1,new Vec3()),new CageVertex(2,new Vec3(1,0,0)),new CageVertex(3,new Vec3(0,1,0)),new CageVertex(4,new Vec3(-1,0,0)),new CageVertex(5,new Vec3(0,-1,0)) };
            var faces = new[] { new CageFace(1,0,new[] { new CageCorner(1,1),new CageCorner(2,2),new CageCorner(3,3) }),new CageFace(2,0,new[] { new CageCorner(4,1),new CageCorner(5,4),new CageCorner(6,5) }) };
            Expect("NONMANIFOLD_SELECTION", () => PolygonSolidify.Apply(new PolygonMesh(GraphId(),vertices,faces),.01f));
        });
        foreach (float scale in new[] { 1f, 100f })
        Test("solidify common command scale Undo save Bake " + scale, () =>
        {
            string source = GraphId(), edit = GraphId(), output = GraphId();
            var graph = new AuthoringGraph(GraphId(), new[] { GraphNode.Polygon(source, PolygonPrimitives.Plane(GraphId()), new RestTransform(scale, new Vec3())), GraphNode.PolygonEdit(edit), GraphNode.Output(output) },
                new[] { new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",output,"mesh") },output);
            var w = AuthoringWorkspace.CreateEmpty(); var commands = new AuthoringCommandService(w);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph)))); var context = GraphEditing.Context(graph,edit);
            var command = w.NewCommand(AuthoringOperation.SolidifyPolygon(context,.02f)); Ok(commands.Execute(command));
            string state = w.Document.StateHash; Ok(commands.Execute(command)); Equal(state,w.Document.StateHash);
            Near(.02f/scale,w.Preview.Output.Polygon.Vertices.Values.Max(v => v.Position.Z));
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Undo()))); Equal(1,w.Preview.Output.Polygon.Faces.Count);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Redo()))); Equal(state,w.Document.StateHash);
            Code("INVALID_THICKNESS",commands.Execute(w.NewCommand(AuthoringOperation.SolidifyPolygon(context,0)))); Equal(state,w.Document.StateHash);
            string dir = Dir("solidify-"+scale); ProjectStore.Save(dir,w,0); var reopened = ProjectStore.Open(dir); Equal(state,reopened.Document.StateHash);
            string manifest = BakeStore.Export(System.IO.Path.Combine(dir,"bake"),reopened); Equal(12,BakeStore.Read(manifest).Mesh.TriangleCount);
        });
    }
}
