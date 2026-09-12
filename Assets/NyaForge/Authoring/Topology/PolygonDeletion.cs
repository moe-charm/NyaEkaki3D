using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    public static class PolygonDeletion
    {
        /// <summary>Removes faces and newly unused vertices, retaining surviving corner attributes and IDs.</summary>
        public static PolygonMesh DeleteFaces(PolygonMesh mesh,IEnumerable<ulong> faceIds)
        {
            Checks.Require(mesh!=null && faceIds!=null,"INVALID_SELECTION","Mesh and face selection are required.");
            var ids=faceIds.Take(AuthoringLimits.MaxIndices/3+1).ToArray();var selected=new HashSet<ulong>(ids);
            Checks.Require(ids.Length>0 && selected.Count==ids.Length && selected.IsSubsetOf(mesh.Faces.Select(f=>f.Id)),"INVALID_SELECTION","Select distinct existing faces.");
            var faces=mesh.Faces.Where(f=>!selected.Contains(f.Id)).ToArray();
            var removedVertices=new HashSet<ulong>(mesh.Faces.Where(f=>selected.Contains(f.Id)).SelectMany(f=>f.Corners).Select(c=>c.VertexId));
            removedVertices.ExceptWith(faces.SelectMany(f=>f.Corners).Select(c=>c.VertexId));
            var result=new PolygonMesh(mesh.DomainId,mesh.Vertices.Values.Where(v=>!removedVertices.Contains(v.Id)),faces,mesh.IdWatermarks);
            if(result.Faces.Count>0) PolygonRenderAdapter.Build(result);return result;
        }
    }
}

