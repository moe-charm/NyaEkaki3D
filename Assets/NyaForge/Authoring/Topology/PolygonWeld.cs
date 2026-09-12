using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    public static class PolygonWeld
    {
        /// <summary>Merge selected vertices at their centroid, retaining the smallest vertex ID.
        /// Each surviving face keeps its own corner attributes; collapsed runs keep their smallest corner ID.</summary>
        public static PolygonMesh AtCenter(PolygonMesh mesh,IEnumerable<ulong> vertices)
        {
            Checks.Require(mesh!=null && vertices!=null,"INVALID_SELECTION","Select vertices to weld.");
            var ids=vertices.Take(AuthoringLimits.MaxVertices+1).ToArray();var selected=new HashSet<ulong>(ids);
            Checks.Require(ids.Length>=2 && ids.Length<=AuthoringLimits.MaxVertices && selected.Count==ids.Length && selected.All(mesh.Vertices.ContainsKey),"INVALID_SELECTION","Select distinct existing vertices.");
            ulong target=ids.Min();double x=0,y=0,z=0;
            foreach(ulong id in ids.OrderBy(v=>v)) { var p=mesh.Vertices[id].Position;x+=p.X;y+=p.Y;z+=p.Z; }
            var center=new Vec3((float)(x/ids.Length),(float)(y/ids.Length),(float)(z/ids.Length));
            var faces=new List<CageFace>();
            foreach(var face in mesh.Faces)
            {
                if(!face.Corners.Any(c=>selected.Contains(c.VertexId))) { faces.Add(face);continue; }
                var corners=new List<CageCorner>();
                foreach(var c in face.Corners)
                {
                    var mapped=selected.Contains(c.VertexId) ? new CageCorner(c.Id,target,c.Uv0,c.Normal,c.Tangent) : c;
                    if(corners.Count>0 && corners[corners.Count-1].VertexId==mapped.VertexId)
                    { if(mapped.Id<corners[corners.Count-1].Id) corners[corners.Count-1]=mapped; }
                    else corners.Add(mapped);
                }
                if(corners.Count>1 && corners[0].VertexId==corners[corners.Count-1].VertexId)
                { if(corners[corners.Count-1].Id<corners[0].Id) corners[0]=corners[corners.Count-1];corners.RemoveAt(corners.Count-1); }
                Checks.Require(corners.Select(c=>c.VertexId).Distinct().Count()==corners.Count,"INVALID_WELD","Weld would make a face revisit a vertex.");
                if(corners.Count>=3) faces.Add(new CageFace(face.Id,face.Material,corners));
            }
            Checks.Require(faces.Count>0,"EMPTY_POLYGON","Weld would remove all faces.");
            var keys=new HashSet<string>();
            foreach(var f in faces) Checks.Require(keys.Add(string.Join(",",f.Corners.Select(c=>c.VertexId).OrderBy(v=>v))),"DUPLICATE_FACE","Weld would duplicate a face.");
            var used=new HashSet<ulong>(faces.SelectMany(f=>f.Corners).Select(c=>c.VertexId));
            var removed=new HashSet<ulong>(mesh.Faces.SelectMany(f=>f.Corners).Select(c=>c.VertexId));
            removed.UnionWith(selected);removed.ExceptWith(used);
            var result=new PolygonMesh(mesh.DomainId,mesh.Vertices.Values.Where(v=>!removed.Contains(v.Id)).Select(v=>v.Id==target ? new CageVertex(target,center) : v),faces,mesh.IdWatermarks);
            ValidateJunction(result,target);
            PolygonRenderAdapter.Build(result);return result;
        }

        static void ValidateJunction(PolygonMesh mesh,ulong target)
        {
            Checks.Require(mesh.EdgeFaces.All(e=>e.Value.Count<=2),"NONMANIFOLD_WELD","Weld would overuse an edge.");
            var incident=mesh.Faces.Where(f=>f.Corners.Any(c=>c.VertexId==target)).ToArray();
            var edges=mesh.EdgeFaces.Where(e=>e.Key.A==target || e.Key.B==target).ToArray();
            int boundary=edges.Count(e=>e.Value.Count==1);
            Checks.Require(boundary==0 || boundary==2,"NONMANIFOLD_WELD","Weld would create a branched boundary.");
            var reached=new HashSet<ulong>();if(incident.Length>0) reached.Add(incident[0].Id);
            bool changed;
            do { changed=false;foreach(var edge in edges) if(edge.Value.Any(reached.Contains)) foreach(ulong face in edge.Value) changed|=reached.Add(face); } while(changed);
            Checks.Require(reached.Count==incident.Length,"NONMANIFOLD_WELD","Weld would create disconnected face fans.");
            foreach(var edge in edges.Where(e=>e.Value.Count==2))
            {
                int forward=incident.Where(f=>edge.Value.Contains(f.Id)).Sum(f=>Enumerable.Range(0,f.Corners.Count).Count(i=>f.Corners[i].VertexId==edge.Key.A && f.Corners[(i+1)%f.Corners.Count].VertexId==edge.Key.B));
                Checks.Require(forward==1,"INVALID_WELD","Weld would reverse adjacent face winding.");
            }
        }
    }
}
