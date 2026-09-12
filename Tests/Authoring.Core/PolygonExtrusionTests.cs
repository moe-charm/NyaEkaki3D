using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunPolygonExtrusionTests()
    {
        Test("quad extrusion retains cap identity and creates boundary walls", () =>
        {
            var original = PolygonPrimitives.Plane(GraphId());
            var mesh = PolygonExtrusion.Extrude(original, new ulong[] { 1 }, new Vec3(0, 0, -.05f));
            Equal(8, mesh.Vertices.Count); Equal(5, mesh.Faces.Count); Equal(10, PolygonRenderAdapter.Build(mesh).Mesh.TriangleCount);
            var cap = mesh.Faces.Single(f => f.Id == 1); True(cap.Corners.Select(c => c.Id).SequenceEqual(original.Faces[0].Corners.Select(c => c.Id)));
            foreach (var corner in cap.Corners) Near(-.05f, mesh.Vertices[corner.VertexId].Position.Z);
            Near(0, original.Vertices[1].Position.Z); Equal(1, original.Faces.Count);
            Equal(4, mesh.EdgeFaces.Count(p => p.Value.Count == 1)); True(mesh.EdgeFaces.All(p => p.Value.Count <= 2));
            Expect("INVALID_EXTRUSION", () => PolygonExtrusion.Extrude(original, new ulong[] { 1 }, new Vec3(.1f, 0, 0)));
        });
        Test("region extrusion omits internal wall between selected triangles", () =>
        {
            var source = PolygonPrimitives.Plane(GraphId()); var c = source.Faces[0].Corners;
            var faces = new[] { new CageFace(1, 0, new[] { c[0], c[1], c[2] }), new CageFace(2, 0, new[] {
                new CageCorner(5, c[0].VertexId, c[0].Uv0, c[0].Normal, c[0].Tangent),
                new CageCorner(6, c[2].VertexId, c[2].Uv0, c[2].Normal, c[2].Tangent), c[3] }) };
            var mesh = PolygonExtrusion.Extrude(new PolygonMesh(source.DomainId, source.Vertices.Values, faces), new ulong[] { 1, 2 }, new Vec3(0, 0, -.1f));
            Equal(6, mesh.Faces.Count); Equal(8, mesh.Vertices.Count); Equal(10, PolygonRenderAdapter.Build(mesh).Mesh.TriangleCount);
        });
        foreach (float scale in new[] { 1f, 100f })
        Test("extrusion command Undo save and invalid delta protection scale" + scale, () =>
        {
            string source = GraphId(), edit = GraphId(), output = GraphId(); var polygon = PolygonPrimitives.Plane(GraphId());
            var graph = new AuthoringGraph(GraphId(), new[] { GraphNode.Polygon(source, polygon, new RestTransform(scale, new Vec3())), GraphNode.PolygonEdit(edit), GraphNode.Output(output) },
                new[] { new GraphEdge(source, "mesh", edit, "mesh"), new GraphEdge(edit, "mesh", output, "mesh") }, output);
            var w = AuthoringWorkspace.CreateEmpty(); var commands = new AuthoringCommandService(w); Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph))));
            var context = GraphEditing.Context(graph, edit); var command = w.NewCommand(AuthoringOperation.ExtrudePolygonFaces(context, new ulong[] { 1 }, new Vec3(0, 0, -.05f)));
            Ok(commands.Execute(command)); string state = w.Document.StateHash; Ok(commands.Execute(command)); Equal(state, w.Document.StateHash);
            Equal(5, w.Preview.Output.Polygon.Faces.Count);
            var cap = w.Preview.Output.Polygon.Faces.Single(f => f.Id == 1);
            foreach (var corner in cap.Corners) Near(-.05f / scale, w.Preview.Output.Polygon.Vertices[corner.VertexId].Position.Z);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Undo()))); Equal(1, w.Preview.Output.Polygon.Faces.Count);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Redo()))); Equal(state, w.Document.StateHash);
            string dir = Dir("extrude-save-" + scale); ProjectStore.Save(dir, w, 0); Equal(state, ProjectStore.Open(dir).Document.StateHash);
            Code("INVALID_EXTRUSION", commands.Execute(w.NewCommand(AuthoringOperation.ExtrudePolygonFaces(context, new ulong[] { 1 }, new Vec3()))));
            Equal(state, w.Document.StateHash);
        });
    }
}
