using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    public static class PolygonEdgeInsertion
    {
        /// <summary>Fraction runs from the smaller vertex ID to the larger, independent of face winding.</summary>
        public static PolygonMesh Insert(PolygonMesh mesh,IEnumerable<ulong> vertexIds,float fraction)
        {
            Checks.Require(mesh!=null && vertexIds!=null,"INVALID_SELECTION","Select an edge's two endpoints.");
            Checks.Finite(fraction);Checks.Require(fraction>0 && fraction<1,"INVALID_EDGE_POSITION","Use an interior fraction between zero and one.");
            var ids=vertexIds.Take(3).ToArray();
            Checks.Require(ids.Length==2 && ids[0]!=ids[1],"INVALID_SELECTION","Select two distinct edge endpoints.");
            var edge=new CageEdgeId(ids[0],ids[1]);
            Checks.Require(mesh.EdgeFaces.TryGetValue(edge,out var incident),"EDGE_NOT_FOUND","Selected vertices do not form an edge.");
            Checks.Require(incident.Count<=2,"NONMANIFOLD_EDGE","Cannot insert into a nonmanifold edge.");
            Checks.Require(mesh.IdWatermarks.Vertex<ulong.MaxValue && mesh.IdWatermarks.Corner<=ulong.MaxValue-(ulong)incident.Count,"ELEMENT_ID_EXHAUSTED","No element IDs remain.");
            ulong vertex=mesh.IdWatermarks.Vertex+1,corner=mesh.IdWatermarks.Corner;
            var position=CornerInterpolation.Position(mesh.Vertices[edge.A].Position,mesh.Vertices[edge.B].Position,fraction);
            var faces=new List<CageFace>();
            foreach(var face in mesh.Faces)
            {
                if(!incident.Contains(face.Id)) { faces.Add(face);continue; }
                Checks.Require(face.Corners.Count<256,"BUDGET_EXCEEDED","Face already has the maximum corner count.");
                var corners=new List<CageCorner>();
                for(int i=0;i<face.Corners.Count;i++)
                {
                    var a=face.Corners[i];var b=face.Corners[(i+1)%face.Corners.Count];corners.Add(a);
                    if(new CageEdgeId(a.VertexId,b.VertexId).Equals(edge))
                        corners.Add(CornerInterpolation.Between(++corner,vertex,a,b,a.VertexId==edge.A ? fraction : 1-fraction));
                }
                faces.Add(new CageFace(face.Id,face.Material,corners));
            }
            var result=new PolygonMesh(mesh.DomainId,mesh.Vertices.Values.Concat(new[]{new CageVertex(vertex,position)}),faces,mesh.IdWatermarks);
            PolygonRenderAdapter.Build(result);return result;
        }
    }
}
