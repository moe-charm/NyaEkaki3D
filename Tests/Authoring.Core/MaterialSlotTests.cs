using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunMaterialSlotTests()
    {
        Test("sparse authored material slots survive rendering order, native storage and removal",()=>
        {
            var original=TriangleMeshAdapter.Import(GraphId(),AuthoringFixtures.Panel(1));
            var faces=original.Faces.Select(f=>new CageFace(f.Id,f.Material==0 ? 3 : 9,f.Corners)).ToArray();
            var polygon=new PolygonMesh(original.DomainId,original.Vertices.Values,faces);
            var render=PolygonRenderAdapter.Build(polygon);
            True(render.MaterialSlotMap.SequenceEqual(new[]{3,9}));Equal(2,render.Mesh.Submeshes.Count);
            int triangle=0;
            for(int i=0;i<render.Mesh.Submeshes.Count;i++)
                for(int t=0;t<render.Mesh.Submeshes[i].Length/3;t++)
                {
                    ulong faceId=render.RenderTriangleMap[triangle++];
                    Equal(render.MaterialSlotMap[i],faces.Single(f=>f.Id==faceId).Material);
                }
            var reordered=PolygonRenderAdapter.Build(new PolygonMesh(polygon.DomainId,polygon.Vertices.Values.Reverse(),faces.Reverse()));
            Equal(render.Mesh.ContentHash,reordered.Mesh.ContentHash);True(render.MaterialSlotMap.SequenceEqual(reordered.MaterialSlotMap));
            var removed=new PolygonMesh(polygon.DomainId,polygon.Vertices.Values,faces.Where(f=>f.Material==9));
            var remaining=PolygonRenderAdapter.Build(removed);
            Equal(1,remaining.Mesh.Submeshes.Count);Equal(9,remaining.MaterialSlotMap[0]);True(removed.Faces.All(f=>f.Material==9));
            string source=GraphId(),output=GraphId();
            var graph=new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,polygon,new RestTransform(1,new Vec3())),GraphNode.Output(output)},
                new[]{new GraphEdge(source,"mesh",output,"mesh")},output);
            var workspace=AuthoringWorkspace.CreateEmpty();Ok(new AuthoringCommandService(workspace).Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph))));
            string directory=Dir("sparse-material-native");ProjectStore.Save(directory,workspace,0);
            var loaded=ProjectStore.Open(directory);Equal(workspace.Document.StateHash,loaded.Document.StateHash);
            True(loaded.Preview.Output.PolygonRendering.MaterialSlotMap.SequenceEqual(new[]{3,9}));
            Equal(render.Mesh.ContentHash,loaded.Preview.Output.Mesh.ContentHash);
        });
        Test("new geometry inherits authored material slot without requiring slot zero",()=>
        {
            var plane=PolygonPrimitives.Plane(GraphId());
            var source=new PolygonMesh(plane.DomainId,plane.Vertices.Values,plane.Faces.Select(f=>new CageFace(f.Id,7,f.Corners)));
            var extruded=PolygonExtrusion.Extrude(source,new[]{source.Faces[0].Id},new Vec3(0,0,.02f));
            var mirrored=PolygonMirror.Apply(source,GraphId(),0,.3f);
            foreach(var result in new[]{extruded,mirrored})
            {
                True(result.Faces.Count>source.Faces.Count);True(result.Faces.All(f=>f.Material==7));
                True(PolygonRenderAdapter.Build(result).MaterialSlotMap.SequenceEqual(new[]{7}));
            }
        });
    }
}
