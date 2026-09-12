using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    public static class PolygonCap
    {
        public static PolygonMesh Fill(PolygonMesh mesh,IEnumerable<ulong> vertexIds)
        {
            Checks.Require(vertexIds!=null,"INVALID_SELECTION","Choose a boundary loop.");
            var ids=vertexIds.Take(257).ToArray();var selected=new HashSet<ulong>(ids);
            Checks.Require(ids.Length>=3 && ids.Length<=256 && selected.Count==ids.Length,"INVALID_SELECTION","Boundary requires 3..256 distinct vertices.");
            var loop=PolygonBoundaries.Find(mesh).SingleOrDefault(l=>l.Length==ids.Length && l.All(selected.Contains));
            Checks.Require(loop!=null,"INVALID_SELECTION","Selection is not one complete boundary loop.");
            Checks.Require(!mesh.Faces.Any(f=>f.Corners.Count==loop.Length && f.Corners.All(c=>selected.Contains(c.VertexId))),"DUPLICATE_FACE","A face already covers this boundary.");
            ulong faceId=mesh.IdWatermarks.Face,cornerId=mesh.IdWatermarks.Corner;
            Checks.Require(faceId<ulong.MaxValue && cornerId<=ulong.MaxValue-(ulong)loop.Length,"ELEMENT_ID_EXHAUSTED","No element IDs remain.");
            ulong adjacent=mesh.EdgeFaces[new CageEdgeId(loop[0],loop[1])][0];
            int material=mesh.Faces.Single(f=>f.Id==adjacent).Material;
            var bare=new CageFace(faceId+1,material,loop.Select((v,i)=>new CageCorner(cornerId+1+(ulong)i,v)));
            var projected=PolygonNewFace.Project(mesh,bare);
            var result=new PolygonMesh(mesh.DomainId,mesh.Vertices.Values,mesh.Faces.Concat(new[]{projected}),mesh.IdWatermarks);
            PolygonRenderAdapter.Build(result);return result;
        }
    }
}
