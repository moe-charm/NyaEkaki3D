using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;
using NyaForge.Authoring.Paint;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    static void RunMaterialFaceTests()
    {
        Test("wire face material assignment updates slot and preserves identity",()=>
        {
            var fixture=MaterialFixture();string edit=GraphId();
            var source=fixture.graph.Nodes.Values.Single(n=>n.TypeId==BuiltinNodes.PolygonSource);
            var graph=new AuthoringGraph(fixture.graph.GraphId,fixture.graph.Nodes.Values.Append(GraphNode.PolygonEdit(edit)),
                fixture.graph.Edges.Select(e=>e.FromNode==source.NodeId ? new GraphEdge(edit,e.FromPort,e.ToNode,e.ToPort) : e).Append(new GraphEdge(source.NodeId,"mesh",edit,"mesh")),fixture.graph.OutputNodeId);
            graph=MaterialSlotEditing.ConvertOutput(graph);var w=AuthoringWorkspace.CreateEmpty();Ok(Execute(w,AuthoringOperation.AddGraph(graph)));
            var context=GraphEditing.Context(graph,edit);var face=w.Preview.Output.Polygon.Faces[0].Id;
            var inspected=new JObject {["graphId"]=graph.GraphId,["nodeId"]=edit,["inputSnapshot"]=context.InputSnapshot,["domainId"]=context.DomainId};
            var op=new JObject {["kind"]="polygon.faces.material",["context"]=inspected,["elementIds"]=new JArray(face.ToString()),["materialSlot"]=7,["materialNodeId"]=fixture.material};
            var wire=Wire(w,op);var service=new AuthoringCommandService(w);Ok(service.Execute(CommandWireReader.Read(wire)));string after=w.Document.StateHash;
            Equal(7,w.Preview.Output.Polygon.Faces.First(f=>f.Id==face).Material);Equal(fixture.material,w.Preview.Output.SlotMaterials[7].MaterialNodeId);
            long revision=w.Document.DocumentRevision;Ok(service.Execute(CommandWireReader.Read(wire)));Equal(revision,w.Document.DocumentRevision);
            Ok(Execute(w,AuthoringOperation.Undo()));Ok(Execute(w,AuthoringOperation.Redo()));Equal(after,w.Document.StateHash);
            op.Remove("materialNodeId");Expect("INVALID_COMMAND_WIRE",()=>CommandWireReader.Read(Wire(w,op)));
        });
        Test("face slot transaction rebinds only unchanged painted mapping and commits as one Undo",()=>
        {
            var fixture=MaterialFixture();string edit=GraphId();
            var source=fixture.graph.Nodes.Values.Single(n=>n.TypeId==BuiltinNodes.PolygonSource).NodeId;
            var paint=fixture.graph.Nodes.Values.Single(n=>n.TypeId==BuiltinNodes.Paint).NodeId;
            var graph=new AuthoringGraph(fixture.graph.GraphId,fixture.graph.Nodes.Values.Append(GraphNode.PolygonEdit(edit)),
                fixture.graph.Edges.Select(e=>e.FromNode==source ? new GraphEdge(edit,e.FromPort,e.ToNode,e.ToPort) : e).Append(new GraphEdge(source,"mesh",edit,"mesh")),fixture.graph.OutputNodeId);
            var mapping=PaintEditing.Context(graph,paint);var image=new PaintImage(64,64,new Rgba32(50,120,210,180));
            graph=graph.ReplaceNode(GraphNode.Paint(paint,64,64,image,mapping.UvHash,mapping.MeshDomain));
            graph=MaterialSlotEditing.ConvertOutput(graph);
            var workspace=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(workspace);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph))));string before=workspace.Document.StateHash;
            var context=GraphEditing.Context(graph,edit);var face=workspace.Preview.Output.Polygon.Faces[0].Id;
            var changed=MaterialFaceEditing.Assign(graph,context,new[]{face},7,fixture.material);
            var result=GraphEvaluator.Evaluate(changed);True(result.IsComplete);
            Equal(fixture.material,result.Output.SlotMaterials[7].MaterialNodeId);
            True(ReferenceEquals(image,changed.Nodes[paint].PaintImage));False(mapping.UvHash==changed.Nodes[paint].PaintUvHash);
            Equal(PaintUvBinding.Hash(result.Output.Polygon),changed.Nodes[paint].PaintUvHash);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.ReplaceGraph(changed))));
            string after=workspace.Document.StateHash;
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.Undo())));Equal(before,workspace.Document.StateHash);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.Redo())));Equal(after,workspace.Document.StateHash);
            string directory=Dir("face-slot-paint-native");ProjectStore.Save(directory,workspace,0);var reopened=ProjectStore.Open(directory);
            Equal(after,reopened.Document.StateHash);True(reopened.Preview.Output.SlotMaterials[7].Material.BaseColor.Image.CopyRgba().SequenceEqual(image.CopyRgba()));
            var stale=graph.ReplaceNode(GraphNode.Paint(paint,64,64,image,new string('a',64),mapping.MeshDomain));
            Expect("MATERIAL_REQUIRED",()=>MaterialFaceEditing.Assign(stale,context,new[]{face},7,fixture.material));
            True(ReferenceEquals(image,stale.Nodes[paint].PaintImage));
        });
        Test("face material command keeps geometry and corner identity through Undo and native",()=>
        {
            var polygon=TriangleMeshAdapter.Import(GraphId(),AuthoringFixtures.Panel(1));
            string source=GraphId(),edit=GraphId(),output=GraphId();
            var graph=new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,polygon,new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(output)},
                new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",output,"mesh")},output);
            var workspace=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(workspace);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph))));string before=workspace.Document.StateHash;
            var context=GraphEditing.Context(graph,edit);var selection=new[]{polygon.Faces[0].Id};
            var operation=AuthoringOperation.AssignPolygonMaterial(context,selection,7);selection[0]=99999;
            Ok(commands.Execute(workspace.NewCommand(operation)));
            var changed=workspace.Preview.Output.Polygon;
            Equal(7,changed.Faces.First(f=>f.Id==polygon.Faces[0].Id).Material);
            foreach(var face in polygon.Faces)
            {
                var actual=changed.Faces.Single(f=>f.Id==face.Id);
                if(face.Id!=polygon.Faces[0].Id) Equal(face.Material,actual.Material);
                True(face.Corners.SequenceEqual(actual.Corners));
            }
            foreach(var vertex in polygon.Vertices) Equal(vertex.Value.Position,changed.Vertices[vertex.Key].Position);
            Equal(polygon.DomainId,changed.DomainId);
            True(workspace.Preview.Output.PolygonRendering.MaterialSlotMap.SequenceEqual(new[]{0,1,7}));
            string after=workspace.Document.StateHash;
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.Undo())));Equal(before,workspace.Document.StateHash);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.Redo())));Equal(after,workspace.Document.StateHash);
            string directory=Dir("material-face-native");ProjectStore.Save(directory,workspace,0);Equal(after,ProjectStore.Open(directory).Document.StateHash);
            Code("INVALID_SELECTION",commands.Execute(workspace.NewCommand(AuthoringOperation.AssignPolygonMaterial(context,new ulong[]{9999},7))));Equal(after,workspace.Document.StateHash);
            Code("INVALID_MATERIAL_SLOTS",commands.Execute(workspace.NewCommand(AuthoringOperation.AssignPolygonMaterial(context,new[]{polygon.Faces[0].Id},-1))));Equal(after,workspace.Document.StateHash);
        });
    }
}
