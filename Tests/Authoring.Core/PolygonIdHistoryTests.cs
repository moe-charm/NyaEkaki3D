using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunPolygonIdHistoryTests()
    {
        Test("deleted ID reservation survives native save Undo redo and cap command",()=>
        {
            var open=PolygonExtrusion.Extrude(PolygonPrimitives.Plane(GraphId()),new ulong[]{1},new Vec3(0,0,-.05f));
            var closed=PolygonCap.Fill(open,PolygonBoundaries.Find(open)[0]);ulong removed=closed.IdWatermarks.Face;
            string source=GraphId(),edit=GraphId(),output=GraphId();
            var graph=new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,closed,new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(output)},
                new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",output,"mesh")},output);
            var workspace=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(workspace);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph))));
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.DeletePolygonFaces(GraphEditing.Context(graph,edit),new[]{removed}))));
            string deletedState=workspace.Document.StateHash;
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.Undo())));
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.Redo())));Equal(deletedState,workspace.Document.StateHash);
            string directory=Dir("id-history-native");ProjectStore.Save(directory,workspace,0);workspace=ProjectStore.Open(directory);
            Equal(deletedState,workspace.Document.StateHash);Equal(removed,workspace.Preview.Output.Polygon.IdWatermarks.Face);
            commands=new AuthoringCommandService(workspace);
            var restoredGraph=workspace.Document.Objects[0].Graph;
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.FillPolygonBoundary(GraphEditing.Context(restoredGraph,edit),PolygonBoundaries.Find(workspace.Preview.Output.Polygon)[0]))));
            True(workspace.Preview.Output.Polygon.Faces.Max(f=>f.Id)>removed);
        });
        Test("deleted IDs survive ordinary edits binary reopen and subsequent extrusion",()=>
        {
            var original=PolygonExtrusion.Extrude(PolygonPrimitives.Plane(GraphId()),new ulong[]{1},new Vec3(0,0,-.05f));
            var keep=original.Faces.First(f=>f.Corners.All(c=>c.VertexId<original.IdWatermarks.Vertex));
            var deleted=PolygonDeletion.DeleteFaces(original,original.Faces.Where(f=>f.Id!=keep.Id).Select(f=>f.Id));
            True(deleted.Vertices.Keys.Max()<deleted.IdWatermarks.Vertex);
            Equal(original.IdWatermarks.Vertex,deleted.IdWatermarks.Vertex);Equal(original.IdWatermarks.Face,deleted.IdWatermarks.Face);Equal(original.IdWatermarks.Corner,deleted.IdWatermarks.Corner);
            deleted=deleted.MoveVertices(deleted.Vertices.Keys.Take(1),new Vec3(.001f,0,0));
            deleted=PolygonUvProjection.Apply(deleted);
            deleted=UvIslandTransform.Apply(deleted,new[]{keep.Id},new UvTransformSettings(new Vec2(.1f,0),0,1));
            deleted=PolygonMaterialAssignment.Assign(deleted,new[]{keep.Id},2);
            var bytes=PolygonBinaryCodec.Write(deleted);Equal(2,BitConverter.ToInt32(bytes,4));
            var reopened=PolygonBinaryCodec.Read(bytes);True(bytes.SequenceEqual(PolygonBinaryCodec.Write(reopened)));
            var result=PolygonExtrusion.Extrude(reopened,new[]{keep.Id},PolygonExtrusion.FaceNormal(reopened,reopened.Faces[0])*.01f);
            True(result.Vertices.Keys.Except(reopened.Vertices.Keys).All(id=>id>original.IdWatermarks.Vertex));
            True(result.Faces.Where(f=>f.Id!=keep.Id).All(f=>f.Id>original.IdWatermarks.Face));
            var oldCorners=reopened.Faces.SelectMany(f=>f.Corners).Select(c=>c.Id).ToArray();
            True(result.Faces.SelectMany(f=>f.Corners).Select(c=>c.Id).Except(oldCorners).All(id=>id>original.IdWatermarks.Corner));
            var mirrored=PolygonMirror.Apply(reopened,GraphId(),0,0);
            True(mirrored.Vertices.Keys.Except(reopened.Vertices.Keys).All(id=>id>original.IdWatermarks.Vertex));
            var thick=PolygonSolidify.Apply(reopened,.01f);
            True(thick.Vertices.Keys.Except(reopened.Vertices.Keys).All(id=>id>original.IdWatermarks.Vertex));
        });
        Test("cap allocation skips deleted cap IDs and codec rejects lost history",()=>
        {
            var original=PolygonExtrusion.Extrude(PolygonPrimitives.Plane(GraphId()),new ulong[]{1},new Vec3(0,0,-.05f));
            var capped=PolygonCap.Fill(original,PolygonBoundaries.Find(original)[0]);ulong removed=capped.Faces.Max(f=>f.Id);
            var open=PolygonDeletion.DeleteFaces(capped,new[]{removed});
            var restored=PolygonCap.Fill(open,PolygonBoundaries.Find(open)[0]);
            True(restored.Faces.Max(f=>f.Id)>removed);True(restored.IdWatermarks.Corner>capped.IdWatermarks.Corner);
            var bytes=PolygonBinaryCodec.Write(open);Equal(2,BitConverter.ToInt32(bytes,4));
            var bad=(byte[])bytes.Clone();Array.Clear(bad,56,24);Expect("INVALID_BLOB",()=>PolygonBinaryCodec.Read(bad));
            Expect("INVALID_BLOB",()=>PolygonBinaryCodec.Read(bytes.Take(65).ToArray()));
            var legacy=PolygonBinaryCodec.Write(original);Equal(1,BitConverter.ToInt32(legacy,4));True(legacy.SequenceEqual(PolygonBinaryCodec.Write(PolygonBinaryCodec.Read(legacy))));
            var exhausted=new PolygonMesh(open.DomainId,open.Vertices.Values,open.Faces,new PolygonIdWatermarks(open.IdWatermarks.Vertex,ulong.MaxValue,open.IdWatermarks.Corner));
            Expect("ELEMENT_ID_EXHAUSTED",()=>PolygonCap.Fill(exhausted,PolygonBoundaries.Find(exhausted)[0]));
        });
    }
}
