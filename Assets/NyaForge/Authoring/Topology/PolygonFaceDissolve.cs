using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    public static class PolygonFaceDissolve
    {
        public static PolygonMesh Dissolve(PolygonMesh mesh,IEnumerable<ulong> faceIds)
        {
            Checks.Require(mesh!=null && faceIds!=null,"INVALID_SELECTION","Select connected coplanar faces.");
            var ids=faceIds.Take(AuthoringLimits.MaxIndices/3+1).ToArray();
            Checks.Require(ids.Length>=2 && ids.Length<=AuthoringLimits.MaxIndices/3 && ids.Distinct().Count()==ids.Length,"INVALID_SELECTION","Select distinct faces.");
            var faces=mesh.Faces.Where(f=>ids.Contains(f.Id)).OrderBy(f=>f.Id).ToArray();
            Checks.Require(faces.Length==ids.Length,"INVALID_SELECTION","Selected face is missing.");
            Checks.Require(faces.All(f=>f.Material==faces[0].Material),"MATERIAL_SEAM","Faces use different material slots.");
            var selected=new HashSet<ulong>(ids);
            var shared=mesh.EdgeFaces.Where(p=>p.Value.Count(selected.Contains)>1).ToArray();
            var internalEdges=new HashSet<CageEdgeId>(shared.Select(p=>p.Key));
            var reached=new HashSet<ulong>{ids[0]};bool changed;
            do { changed=false;foreach(var edge in shared) if(edge.Value.Any(reached.Contains)) foreach(ulong id in edge.Value.Where(selected.Contains)) changed|=reached.Add(id); } while(changed);
            Checks.Require(reached.Count==ids.Length,"INVALID_FACE_MERGE","Selected faces must form one connected region.");
            foreach(var edge in shared)
            {
                Checks.Require(edge.Value.Count==2,"INVALID_FACE_MERGE","Internal edges must be manifold.");
                var pair=faces.Where(f=>edge.Value.Contains(f.Id)).ToArray();
                foreach(ulong vertex in new[]{edge.Key.A,edge.Key.B})
                {
                    var a=pair[0].Corners.Single(c=>c.VertexId==vertex);var b=pair[1].Corners.Single(c=>c.VertexId==vertex);
                    Checks.Require(a.Uv0.Equals(b.Uv0) && a.Normal.Equals(b.Normal) && a.Tangent.Equals(b.Tangent),"ATTRIBUTE_SEAM","Internal edge has a UV or shading seam.");
                }
                int forward=pair.Sum(f=>Enumerable.Range(0,f.Corners.Count).Count(i=>f.Corners[i].VertexId==edge.Key.A && f.Corners[(i+1)%f.Corners.Count].VertexId==edge.Key.B));
                Checks.Require(forward==1,"INVALID_FACE_MERGE","Internal edge winding differs.");
            }
            var normal=PolygonExtrusion.FaceNormal(mesh,faces[0]);
            Checks.Require(faces.All(f=>Dot(normal,PolygonExtrusion.FaceNormal(mesh,f))>1-1e-5),"NONPLANAR_MERGE","Faces must have the same planar orientation.");
            var origin=mesh.Vertices[faces[0].Corners[0].VertexId].Position;
            var offsets=faces.SelectMany(f=>f.Corners).Select(c=>mesh.Vertices[c.VertexId].Position-origin).ToArray();
            double extent=Math.Sqrt(offsets.Max(p=>Dot(p,p)));
            Checks.Require(offsets.All(p=>Math.Abs(Dot(normal,p))<=Math.Max(1e-10,extent*1e-6)),"NONPLANAR_MERGE","Faces are not coplanar.");
            var next=new Dictionary<ulong,(ulong end,CageCorner corner)>();
            foreach(var face in faces)
                for(int i=0;i<face.Corners.Count;i++)
                {
                    var a=face.Corners[i];var b=face.Corners[(i+1)%face.Corners.Count];
                    if(internalEdges.Contains(new CageEdgeId(a.VertexId,b.VertexId))) continue;
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
            var remaining=mesh.Faces.Where(f=>!selected.Contains(f.Id)).Concat(new[]{merged}).OrderBy(f=>f.Id).ToArray();
            var removed=new HashSet<ulong>(faces.SelectMany(f=>f.Corners).Select(c=>c.VertexId));
            removed.ExceptWith(remaining.SelectMany(f=>f.Corners).Select(c=>c.VertexId));
            var result=new PolygonMesh(mesh.DomainId,mesh.Vertices.Values.Where(v=>!removed.Contains(v.Id)),remaining,mesh.IdWatermarks);
            PolygonRenderAdapter.Build(result);return result;
        }
        static double Dot(Vec3 a,Vec3 b)=>(double)a.X*b.X+(double)a.Y*b.Y+(double)a.Z*b.Z;
    }
}

