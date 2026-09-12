using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    public static class PolygonFaceCreation
    {
        /// <summary>The supplied order is the perimeter and winding, never sorted by ID.</summary>
        public static PolygonMesh Create(PolygonMesh mesh,IEnumerable<ulong> perimeter,int material=0)
        {
            Checks.Require(mesh!=null && perimeter!=null,"INVALID_SELECTION","Specify an ordered face perimeter.");
            var ids=perimeter.Take(257).ToArray();var selected=new HashSet<ulong>(ids);
            Checks.Require(ids.Length>=3 && ids.Length<=256 && selected.Count==ids.Length && selected.All(mesh.Vertices.ContainsKey),"INVALID_SELECTION","Choose 3..256 distinct existing vertices in perimeter order.");
            Checks.Require(!mesh.Faces.Any(f=>f.Corners.Count==ids.Length && f.Corners.All(c=>selected.Contains(c.VertexId))),"DUPLICATE_FACE","A face already uses these vertices.");
            for(int i=0;i<ids.Length;i++)
            {
                ulong a=ids[i],b=ids[(i+1)%ids.Length];
                if(!mesh.EdgeFaces.TryGetValue(new CageEdgeId(a,b),out var adjacent)) continue;
                Checks.Require(adjacent.Count==1,"NONMANIFOLD_FACE","The edge already has two incident faces.");
                var face=mesh.Faces.Single(f=>f.Id==adjacent[0]);
                bool opposite=Enumerable.Range(0,face.Corners.Count).Any(j=>face.Corners[j].VertexId==b && face.Corners[(j+1)%face.Corners.Count].VertexId==a);
                Checks.Require(opposite,"FACE_WINDING","Reverse the perimeter to match the adjacent face winding.");
            }
            Checks.Require(mesh.IdWatermarks.Face<ulong.MaxValue && mesh.IdWatermarks.Corner<=ulong.MaxValue-(ulong)ids.Length,"ELEMENT_ID_EXHAUSTED","No element IDs remain.");
            var bare=new CageFace(mesh.IdWatermarks.Face+1,material,ids.Select((v,i)=>new CageCorner(mesh.IdWatermarks.Corner+1+(ulong)i,v)));
            var faceWithAttributes=PolygonNewFace.Project(mesh,bare);
            var result=new PolygonMesh(mesh.DomainId,mesh.Vertices.Values,mesh.Faces.Concat(new[]{faceWithAttributes}),mesh.IdWatermarks);
            PolygonRenderAdapter.Build(result);return result;
        }
    }
}
