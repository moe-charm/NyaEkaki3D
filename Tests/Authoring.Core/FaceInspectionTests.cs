using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Inspection;
using NyaForge.Authoring.Topology;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    static void RunFaceInspectionTests()
    {
        Test("polygon inspection preserves sparse 64-bit identities winding and transform",()=>
        {
            ulong high=9007199254740993UL;
            var vertices=new[]{new CageVertex(high,new Vec3(0,0,0)),new CageVertex(high+7,new Vec3(1,0,0)),new CageVertex(high+19,new Vec3(0,1,0))};
            var face=new CageFace(high+100,0,vertices.Select((v,i)=>new CageCorner(high+200+(ulong)i,v.Id,new Vec2(i==1 ? 1 : 0,i==2 ? 1 : 0))));
            var polygon=new PolygonMesh(GraphId(),vertices,new[]{face});
            string source=GraphId(),output=GraphId();var transform=new RestTransform(2,new Vec3(3,4,5));
            var graph=new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,polygon,transform),GraphNode.Output(output)},new[]{new GraphEdge(source,"mesh",output,"mesh")},output);
            var w=AuthoringWorkspace.CreateEmpty();Ok(Execute(w,AuthoringOperation.AddGraph(graph)));
            var value=w.Preview.Evaluation.MeshOutputs[source];
            JObject Read(int offset,int count)=>AuthoringFaceReader.Read(w,w.InstanceId,w.Document.DocumentId,w.Document.DocumentRevision,source,false,value.SnapshotHash,offset,count);
            var page=Read(0,1);var read=page["faces"][0];
            Equal((high+100).ToString(),(string)read["id"]);Equal(3,read["corners"].Count());
            for(int i=0;i<3;i++)
            {
                Equal(vertices[i].Id.ToString(),(string)read["corners"][i]["vertexId"]);
                Equal((high+200+(ulong)i).ToString(),(string)read["corners"][i]["id"]);
            }
            Equal(1f,(float)read["corners"][1]["uv0"][0]);True(page["nextOffset"].Type==JTokenType.Null);
            Equal(0,Read(1,1)["faces"].Count());Expect("INVALID_PAGE",()=>Read(2,1));Expect("INVALID_PAGE",()=>Read(0,65));
            var positions=AuthoringVertexReader.Read(w,w.InstanceId,w.Document.DocumentId,w.Document.DocumentRevision,source,false,value.SnapshotHash,1,1);
            Equal((high+7).ToString(),(string)positions["vertices"][0]["id"]);
            var expected=transform.ToAvatarPoint(vertices[1].Position);Near(expected.X,(float)positions["vertices"][0]["avatarPosition"][0]);Near(expected.Y,(float)positions["vertices"][0]["avatarPosition"][1]);
            read["corners"][0]["vertexId"]="bad";Equal(high.ToString(),(string)Read(0,1)["faces"][0]["corners"][0]["vertexId"]);
            var empty=new PolygonMesh(polygon.DomainId,vertices,System.Array.Empty<CageFace>());
            Ok(Execute(w,AuthoringOperation.UpdateNode(GraphNode.Polygon(source,empty,transform))));
            value=w.Preview.Evaluation.MeshOutputs[source];Equal(0,Read(0,1)["faces"].Count());
        });
    }
}
