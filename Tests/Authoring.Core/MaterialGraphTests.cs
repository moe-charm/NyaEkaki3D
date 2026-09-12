using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;

internal static partial class Program
{
    static (AuthoringGraph graph,string material,string assign) MaterialFixture()
    {
        var source=PaintFixture();string material=GraphId(),assign=GraphId();
        var outputEdge=source.graph.Edges.Single(e=>e.ToNode==source.graph.OutputNodeId && e.ToPort=="mesh");
        var edges=source.graph.Edges.Where(e=>e.ToNode!=source.graph.OutputNodeId).Concat(new[]{
            new GraphEdge(outputEdge.FromNode,outputEdge.FromPort,assign,"mesh"),new GraphEdge(source.paint,"image",material,"baseColor"),
            new GraphEdge(material,"material",assign,"material"),new GraphEdge(assign,"mesh",source.graph.OutputNodeId,"mesh")});
        return (new AuthoringGraph(source.graph.GraphId,source.graph.Nodes.Values.Concat(new[]{GraphNode.StandardMaterial(material),GraphNode.AssignMaterial(assign)}),edges,source.graph.OutputNodeId),material,assign);
    }
    static void RunMaterialGraphTests()
    {
        Test("output material migration preserves paint ownership and routes new paint before assignment",()=>
        {
            var f=PaintFixture();var before=GraphEvaluator.Evaluate(f.graph);string material=GraphId();
            var migrated=OutputSurfaceConnections.AddMaterial(f.graph,material,GraphId());var result=GraphEvaluator.Evaluate(migrated);
            True(result.IsComplete);True(ReferenceEquals(f.graph.Nodes[f.paint],migrated.Nodes[f.paint]));
            Equal(before.Output.Mesh.ContentHash,result.Output.Mesh.ContentHash);Equal(before.Output.BaseColor.ImageHash,result.Output.BaseColor.ImageHash);
            var route=OutputSurfaceConnections.Resolve(migrated);Equal(f.paint,route.ImageNodeId);Equal(material,route.ImageTargetNodeId);Equal(f.source,route.Geometry.FromNode);
            Equal(f.output,OutputSurfaceConnections.Resolve(f.graph).ImageTargetNodeId);
            Expect("MATERIAL_ALREADY_ASSIGNED",()=>OutputSurfaceConnections.AddMaterial(migrated,GraphId(),GraphId()));
            var noImage=f.graph.WithEdges(f.graph.Edges.Where(e=>e.ToPort!="baseColor"));
            var plain=OutputSurfaceConnections.AddMaterial(noImage,GraphId(),GraphId());route=OutputSurfaceConnections.Resolve(plain);Equal("",route.ImageNodeId);
            string paint=GraphId();var painted=new AuthoringGraph(plain.GraphId,plain.Nodes.Values.Concat(new[]{GraphNode.Paint(paint)}),plain.Edges.Concat(new[]{
                new GraphEdge(route.Geometry.FromNode,route.Geometry.FromPort,paint,"mesh"),new GraphEdge(paint,"image",route.ImageTargetNodeId,"baseColor")}),plain.OutputNodeId);
            True(GraphEvaluator.Evaluate(painted).IsComplete);Equal(paint,OutputSurfaceConnections.Resolve(painted).ImageNodeId);
        });
        Test("material parameters validate ranges and preserve canonical immutable identities",()=>
        {
            var original=MaterialParameters.Default;
            var same=new MaterialParameters(new Vec4(1,1,1,1),-0f,.5f,new Vec3());Equal(original.ContentHash,same.ContentHash);
            var other=new MaterialParameters(new Vec4(.2f,.3f,.4f,.5f),.8f,.1f,new Vec3(2,1,0),MaterialAlphaMode.Blend,.25f);
            False(original.ContentHash==other.ContentHash);Equal(1f,original.BaseColor.X);
            Expect("INVALID_MATERIAL",()=>new MaterialParameters(new Vec4(2,0,0,1),0,1,new Vec3()));
            Expect("INVALID_MATERIAL",()=>new MaterialParameters(new Vec4(1,1,1,1),float.NaN,1,new Vec3()));
            Expect("INVALID_MATERIAL",()=>new MaterialParameters(new Vec4(1,1,1,1),0,-1,new Vec3()));
            Expect("INVALID_MATERIAL",()=>new MaterialParameters(new Vec4(1,1,1,1),0,1,new Vec3(65,0,0)));
            Expect("INVALID_MATERIAL",()=>new MaterialParameters(new Vec4(1,1,1,1),0,1,new Vec3(),(MaterialAlphaMode)99));
        });
        Test("typed material graph binds image domain and rejects conflicting assignments",()=>
        {
            var f=MaterialFixture();var result=GraphEvaluator.Evaluate(f.graph);True(result.IsComplete);
            Equal(MaterialParameters.Default.ContentHash,result.Output.Material.Parameters.ContentHash);
            True(ReferenceEquals(result.Output.BaseColor,result.MaterialOutputs[f.material].BaseColor));
            var tint=f.graph.ReplaceNode(GraphNode.StandardMaterial(f.material,new MaterialParameters(new Vec4(.5f,1,1,1),.7f,.2f,new Vec3())));
            var changed=GraphEvaluator.Evaluate(tint);True(changed.IsComplete);
            Equal(result.Output.Mesh.ContentHash,changed.Output.Mesh.ContentHash);Equal(result.Output.BaseColor.ImageHash,changed.Output.BaseColor.ImageHash);
            False(result.Output.SnapshotHash==changed.Output.SnapshotHash);
            var image=f.graph.Edges.Single(e=>e.ToNode==f.material);
            var conflict=f.graph.WithEdges(f.graph.Edges.Concat(new[]{new GraphEdge(image.FromNode,image.FromPort,f.graph.OutputNodeId,"baseColor")}));
            True(GraphEvaluator.Evaluate(conflict).Diagnostics.Any(d=>d.Code=="MATERIAL_ALREADY_ASSIGNED"));
            var source=f.graph.Nodes.Values.Single(n=>n.SourcePolygon!=null);string otherSource=GraphId();
            var wrong=new AuthoringGraph(f.graph.GraphId,f.graph.Nodes.Values.Concat(new[]{GraphNode.Polygon(otherSource,source.SourcePolygon,source.Transform)}),
                f.graph.Edges.Where(e=>e.ToNode!=f.assign || e.ToPort!="mesh").Concat(new[]{new GraphEdge(otherSource,"mesh",f.assign,"mesh")}),f.graph.OutputNodeId);
            True(GraphEvaluator.Evaluate(wrong).Diagnostics.Any(d=>d.Code=="PAINT_UV_CHANGED"));
            string mirror=GraphId();
            var afterMaterial=new AuthoringGraph(f.graph.GraphId,f.graph.Nodes.Values.Concat(new[]{GraphNode.Mirror(mirror)}),
                f.graph.Edges.Where(e=>e.ToNode!=f.graph.OutputNodeId).Concat(new[]{new GraphEdge(f.assign,"mesh",mirror,"mesh"),new GraphEdge(mirror,"mesh",f.graph.OutputNodeId,"mesh")}),f.graph.OutputNodeId);
            True(GraphEvaluator.Evaluate(afterMaterial).Diagnostics.Any(d=>d.Code=="MATERIAL_ORDER_UNSUPPORTED"));
            Expect("PORT_TYPE_MISMATCH",()=>f.graph.WithEdges(f.graph.Edges.Where(e=>e.ToNode!=f.assign || e.ToPort!="material").Concat(new[]{new GraphEdge(image.FromNode,"image",f.assign,"material")})));
            var untextured=f.graph.WithEdges(f.graph.Edges.Where(e=>e.ToNode!=f.material));True(GraphEvaluator.Evaluate(untextured).IsComplete);
        });
        Test("material native roundtrip and common command Undo preserve appearance and reject lossy export",()=>
        {
            var f=MaterialFixture();var workspace=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(workspace);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.AddGraph(f.graph))));string before=workspace.Document.StateHash;
            var parameters=new MaterialParameters(new Vec4(.2f,.4f,.8f,.6f),.8f,.3f,new Vec3(1,2,3),MaterialAlphaMode.Cutout,.4f);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.UpdateNode(GraphNode.StandardMaterial(f.material,parameters)))));
            string after=workspace.Document.StateHash;False(before==after);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.Undo())));Equal(before,workspace.Document.StateHash);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.Redo())));Equal(after,workspace.Document.StateHash);
            string directory=Dir("material-native");ProjectStore.Save(directory,workspace,0);var reopened=ProjectStore.Open(directory);
            Equal(after,reopened.Document.StateHash);Equal(parameters.ContentHash,reopened.Preview.Output.Material.Parameters.ContentHash);
            Equal(workspace.Preview.Output.SnapshotHash,reopened.Preview.Output.SnapshotHash);
            Expect("EXPORT_UNSUPPORTED_FEATURE",()=>BakeSource.Capture(reopened.Document));
            Expect("EXPORT_UNSUPPORTED_FEATURE",()=>BakeSource.CaptureSurface(reopened.Document));
            var blobs=new Dictionary<string,byte[]>();var wire=GraphBinaryCodec.Encode(f.graph,data=> { string hash=Checks.Hash(data);blobs[hash]=data;return hash; });
            var restored=GraphBinaryCodec.Decode(wire,key=>blobs[key]);True(GraphEvaluator.Evaluate(restored).IsComplete);
            True(wire.SequenceEqual(GraphBinaryCodec.Encode(restored,data=>Checks.Hash(data))));
        });
    }
}
