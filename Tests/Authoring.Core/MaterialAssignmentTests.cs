using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static (AuthoringGraph graph,string a,string b,string assign,string output) SlotFixture()
    {
        var original=TriangleMeshAdapter.Import(GraphId(),AuthoringFixtures.Panel(1));
        var polygon=new PolygonMesh(original.DomainId,original.Vertices.Values,original.Faces.Select(f=>new CageFace(f.Id,f.Material==0 ? 3 : 9,f.Corners)));
        string source=GraphId(),a=GraphId(),b=GraphId(),assign=GraphId(),output=GraphId();
        return (new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,polygon,new RestTransform(1,new Vec3())),
            GraphNode.StandardMaterial(a),GraphNode.StandardMaterial(b,new MaterialParameters(new Vec4(1,0,0,1),.8f,.2f,new Vec3())),GraphNode.AssignMaterials(assign,new[]{9,3}),GraphNode.Output(output)},
            new[]{new GraphEdge(source,"mesh",assign,"mesh"),new GraphEdge(a,"material",assign,"material-3"),new GraphEdge(b,"material",assign,"material-9"),new GraphEdge(assign,"mesh",output,"mesh")},output),a,b,assign,output);
    }
    static void RunMaterialAssignmentTests()
    {
        Test("material slot wire and inspection preserve sparse slot identities",()=>
        {
            var f=SlotFixture();var w=AuthoringWorkspace.CreateEmpty();Ok(Execute(w,AuthoringOperation.AddGraph(f.graph)));
            var inspected=NyaForge.Authoring.Inspection.AuthoringGraphReader.Read(w,w.InstanceId);
            var assign=inspected["graph"]["nodes"].Single(n=>(string)n["nodeId"]==f.assign);
            True(assign["materialSlots"].Values<int>().SequenceEqual(new[]{3,9}));
            True(assign["assignedMaterials"]["3"]["baseColorLinear"]!=null);
            True(assign["assignedMaterials"]["9"]["baseColorLinear"]!=null);
            var wire=new Newtonsoft.Json.Linq.JObject {["kind"]="graph.node.update",["node"]=new Newtonsoft.Json.Linq.JObject {["nodeId"]=f.assign,["typeId"]=BuiltinNodes.AssignMaterials,["version"]=1,["parameters"]=new Newtonsoft.Json.Linq.JObject {["slots"]=new Newtonsoft.Json.Linq.JArray(9,3)}}};
            var parsed=CommandWireReader.Read(Wire(w,wire));True(parsed.Operations[0].Node.MaterialSlots.SequenceEqual(new[]{3,9}));
            wire["node"]["parameters"]["slots"]=new Newtonsoft.Json.Linq.JArray("3");Expect("INVALID_COMMAND_WIRE",()=>CommandWireReader.Read(Wire(w,wire)));
        });
        Test("blank slot Paint isolates one slot while retaining original image nodes and history",()=>
        {
            var f=SlotFixture();string material=GraphId(),paint=GraphId();
            var initial=MaterialSlotPaintEditing.AddBlank(f.graph,3,material,paint);
            string nextMaterial=GraphId(),nextPaint=GraphId();
            var changed=MaterialSlotPaintEditing.AddBlank(initial,3,nextMaterial,nextPaint);
            True(changed.Nodes.ContainsKey(material) && changed.Nodes.ContainsKey(paint));
            Equal(nextPaint,OutputSurfaceConnections.Resolve(changed,3).ImageNodeId);
            Equal(f.b,OutputSurfaceConnections.Resolve(changed,9).MaterialNodeId);
            Equal("",OutputSurfaceConnections.Resolve(changed,9).ImageNodeId);
            var workspace=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(workspace);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.AddGraph(initial))));string before=workspace.Document.StateHash;
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.ReplaceGraph(changed))));string after=workspace.Document.StateHash;
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.Undo())));Equal(before,workspace.Document.StateHash);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.Redo())));Equal(after,workspace.Document.StateHash);
            string directory=Dir("slot-blank-paint");ProjectStore.Save(directory,workspace,0);Equal(after,ProjectStore.Open(directory).Document.StateHash);
        });
        Test("shared image slot routing follows node identity, not identical pixels",()=>
        {
            var f=SlotFixture();string source=f.graph.Nodes.Values.Single(n=>n.TypeId==BuiltinNodes.PolygonSource).NodeId;
            string paint=GraphId(),other=GraphId();
            var graph=new AuthoringGraph(f.graph.GraphId,f.graph.Nodes.Values.Concat(new[]{GraphNode.Paint(paint,4,4),GraphNode.Paint(other,4,4)}),
                f.graph.Edges.Concat(new[]{new GraphEdge(source,"mesh",paint,"mesh"),new GraphEdge(source,"mesh",other,"mesh"),new GraphEdge(paint,"image",f.a,"baseColor"),new GraphEdge(paint,"image",f.b,"baseColor")}),f.output);
            True(OutputSurfaceConnections.ImageSlots(graph,paint).SequenceEqual(new[]{3,9}));
            Equal(0,OutputSurfaceConnections.ImageSlots(graph,other).Count);
            graph=graph.WithEdges(graph.Edges.Where(e=>!(e.ToNode==f.b && e.ToPort=="baseColor")).Append(new GraphEdge(other,"image",f.b,"baseColor")));
            var result=GraphEvaluator.Evaluate(graph);True(result.IsComplete);
            Equal(result.ImageOutputs[paint].ImageHash,result.ImageOutputs[other].ImageHash);
            True(OutputSurfaceConnections.ImageSlots(graph,paint).SequenceEqual(new[]{3}));
            True(OutputSurfaceConnections.ImageSlots(graph,other).SequenceEqual(new[]{9}));
        });
        Test("per-slot migration and independent copy preserve image connections and Undo",()=>
        {
            var f=MaterialFixture();var workspace=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(workspace);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.AddGraph(f.graph))));
            var before=workspace.Preview.Output;string state=workspace.Document.StateHash;
            var graph=MaterialSlotEditing.ConvertOutput(f.graph);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.ReplaceGraph(graph))));
            int slot=workspace.Preview.Output.SlotMaterials.Keys.First();var route=OutputSurfaceConnections.Resolve(graph,slot);
            True(OutputSurfaceConnections.Resolve(graph)==null);Equal(f.material,route.MaterialNodeId);
            Equal(before.BaseColor.ImageHash,workspace.Preview.Output.SlotMaterials[slot].Material.BaseColor.ImageHash);
            string copy=GraphId();graph=MaterialSlotEditing.MakeIndependent(graph,slot,copy);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.ReplaceGraph(graph))));
            Equal(copy,workspace.Preview.Output.SlotMaterials[slot].MaterialNodeId);
            Equal(route.ImageNodeId,OutputSurfaceConnections.Resolve(graph,slot).ImageNodeId);
            Equal(before.Mesh.ContentHash,workspace.Preview.Output.Mesh.ContentHash);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.Undo())));
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.Undo())));Equal(state,workspace.Document.StateHash);
        });
        Test("multiple material graph preserves slot identity through native save, node update and Undo",()=>
        {
            var f=SlotFixture();var workspace=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(workspace);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.AddGraph(f.graph))));
            var output=workspace.Preview.Output;True(output.Material==null && output.BaseColor==null);
            Equal(f.a,output.SlotMaterials[3].MaterialNodeId);Equal(f.b,output.SlotMaterials[9].MaterialNodeId);
            True(f.graph.Nodes[f.assign].MaterialSlots.SequenceEqual(new[]{3,9}));
            var reordered=new AuthoringGraph(f.graph.GraphId,f.graph.Nodes.Values.Reverse(),f.graph.Edges.Reverse(),f.output);
            Equal(output.SnapshotHash,GraphEvaluator.Evaluate(reordered).Output.SnapshotHash);
            var changed=GraphNode.StandardMaterial(f.b,new MaterialParameters(new Vec4(0,0,1,.5f),0,.8f,new Vec3(),MaterialAlphaMode.Blend));
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.UpdateNode(changed))));
            Equal(output.Mesh.ContentHash,workspace.Preview.Output.Mesh.ContentHash);False(output.SnapshotHash==workspace.Preview.Output.SnapshotHash);
            Equal(f.a,workspace.Preview.Output.SlotMaterials[3].MaterialNodeId);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.Undo())));Equal(output.SnapshotHash,workspace.Preview.Output.SnapshotHash);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.Redo())));
            string directory=Dir("slot-assignment-native");ProjectStore.Save(directory,workspace,0);var reopened=ProjectStore.Open(directory);
            Equal(workspace.Document.StateHash,reopened.Document.StateHash);Equal(workspace.Preview.Output.SnapshotHash,reopened.Preview.Output.SnapshotHash);
            Equal(f.b,reopened.Preview.Output.SlotMaterials[9].MaterialNodeId);
            Expect("EXPORT_UNSUPPORTED_FEATURE",()=>BakeStore.Export(Dir("slot-old-bake"),reopened));
            Expect("EXPORT_UNSUPPORTED_FEATURE",()=>SurfaceBakeStore.Export(Dir("slot-old-surface"),reopened));
            Expect("EXPORT_UNSUPPORTED_FEATURE",()=>MaterialBakeStore.Export(Dir("slot-old-material"),reopened));
        });
        Test("multiple assignment requires valid keys, connected used slots and no conflicting assignment",()=>
        {
            Expect("INVALID_MATERIAL_SLOTS",()=>GraphNode.AssignMaterials(GraphId(),new[]{3,3}));
            Expect("INVALID_MATERIAL_SLOTS",()=>GraphNode.AssignMaterials(GraphId(),Array.Empty<int>()));
            Expect("INVALID_MATERIAL_SLOTS",()=>GraphNode.AssignMaterials(GraphId(),new[]{-1}));
            var f=SlotFixture();
            var missing=GraphEvaluator.Evaluate(f.graph.WithEdges(f.graph.Edges.Where(e=>e.ToPort!="material-9")));
            False(missing.IsComplete);True(missing.Diagnostics.Any(d=>d.Code=="INPUT_MISSING"));
            var absent=f.graph.WithEdges(f.graph.Edges.Where(e=>e.ToPort!="material-9")).ReplaceNode(GraphNode.AssignMaterials(f.assign,new[]{3}));
            var unresolved=GraphEvaluator.Evaluate(absent);False(unresolved.IsComplete);True(unresolved.Diagnostics.Any(d=>d.Code=="MATERIAL_SLOT_UNASSIGNED"));
            string repeated=GraphId();var graph=new AuthoringGraph(f.graph.GraphId,f.graph.Nodes.Values.Append(GraphNode.AssignMaterial(repeated)),
                f.graph.Edges.Where(e=>e.ToNode!=f.output).Concat(new[]{new GraphEdge(f.assign,"mesh",repeated,"mesh"),new GraphEdge(f.a,"material",repeated,"material"),new GraphEdge(repeated,"mesh",f.output,"mesh")}),f.output);
            True(GraphEvaluator.Evaluate(graph).Diagnostics.Any(d=>d.Code=="MATERIAL_ALREADY_ASSIGNED"));
        });
    }
}
