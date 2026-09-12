using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    public static class PolygonFaceMerge
    {
        public static PolygonMesh Merge(PolygonMesh mesh,IEnumerable<ulong> faceIds)
        {
            Checks.Require(mesh!=null && faceIds!=null,"INVALID_SELECTION","Select two adjacent faces.");
            var ids=faceIds.Take(3).ToArray();
            Checks.Require(ids.Length==2 && ids[0]!=ids[1],"INVALID_SELECTION","Select exactly two distinct faces.");
            var faces=mesh.Faces.Where(f=>ids.Contains(f.Id)).OrderBy(f=>f.Id).ToArray();
            Checks.Require(faces.Length==2,"INVALID_SELECTION","Selected face is missing.");
            Checks.Require(faces[0].Material==faces[1].Material,"MATERIAL_SEAM","Faces use different material slots.");
            var shared=mesh.EdgeFaces.Where(p=>p.Value.Contains(ids[0]) && p.Value.Contains(ids[1])).ToArray();
            Checks.Require(shared.Length==1 && shared[0].Value.Count==2,"INVALID_FACE_MERGE","Faces must share exactly one manifold edge.");
            var edge=shared[0].Key;
            foreach(ulong vertex in new[]{edge.A,edge.B})
            {
                var a=faces[0].Corners.Single(c=>c.VertexId==vertex);var b=faces[1].Corners.Single(c=>c.VertexId==vertex);
                Checks.Require(a.Uv0.Equals(b.Uv0) && a.Normal.Equals(b.Normal) && a.Tangent.Equals(b.Tangent),"ATTRIBUTE_SEAM","Shared edge has a UV or shading seam.");
            }
            var normal=PolygonExtrusion.FaceNormal(mesh,faces[0]);var other=PolygonExtrusion.FaceNormal(mesh,faces[1]);
            Checks.Require(Dot(normal,other)>1-1e-5,"NONPLANAR_MERGE","Faces must have the same planar orientation.");
            var origin=mesh.Vertices[faces[0].Corners[0].VertexId].Position;
            var offsets=faces.SelectMany(f=>f.Corners).Select(c=>mesh.Vertices[c.VertexId].Position-origin).ToArray();
            double extent=Math.Sqrt(offsets.Max(p=>Dot(p,p)));
            Checks.Require(offsets.All(p=>Math.Abs(Dot(normal,p))<=Math.Max(1e-10,extent*1e-6)),"NONPLANAR_MERGE","Faces are not coplanar.");
            var next=new Dictionary<ulong,(ulong end,CageCorner corner)>();
            foreach(var face in faces)
                for(int i=0;i<face.Corners.Count;i++)
                {
                    var a=face.Corners[i];var b=face.Corners[(i+1)%face.Corners.Count];
                    if(new CageEdgeId(a.VertexId,b.VertexId).Equals(edge)) continue;
                    Checks.Require(!next.ContainsKey(a.VertexId),"INVALID_FACE_MERGE","Merged boundary branches or has inconsistent winding.");
                    next.Add(a.VertexId,(b.VertexId,a));
                }
            Checks.Require(next.Count>=3 && next.Count<=256,"INVALID_FACE_MERGE","Merged face requires 3..256 corners.");
            ulong start=next.Keys.Min(),current=start;var corners=new List<CageCorner>();var visited=new HashSet<ulong>();
            do
            {
                Checks.Require(visited.Add(current) && next.ContainsKey(current),"INVALID_FACE_MERGE","Merged boundary is not a simple loop.");
                var step=next[current];corners.Add(step.corner);current=step.end;
            } while(current!=start);
            Checks.Require(corners.Count==next.Count,"INVALID_FACE_MERGE","Merged boundary contains disconnected loops.");
            var merged=new CageFace(faces[0].Id,faces[0].Material,corners);
            var result=new PolygonMesh(mesh.DomainId,mesh.Vertices.Values,mesh.Faces.Where(f=>f.Id!=faces[1].Id).Select(f=>f.Id==merged.Id ? merged : f),mesh.IdWatermarks);
            PolygonRenderAdapter.Build(result);return result;
        }
        static double Dot(Vec3 a,Vec3 b)=>(double)a.X*b.X+(double)a.Y*b.Y+(double)a.Z*b.Z;
    }
}
