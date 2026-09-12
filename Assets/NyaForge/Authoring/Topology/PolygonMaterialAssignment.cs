using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    public static class PolygonMaterialAssignment
    {
        public static PolygonMesh Assign(PolygonMesh mesh,IEnumerable<ulong> faces,int slot)
        {
            Checks.Require(mesh!=null && faces!=null,"INVALID_SELECTION","Mesh and selected faces are required.");
            Checks.Require(slot>=0 && slot<AuthoringLimits.MaxSubmeshes,"INVALID_MATERIAL_SLOTS","Material slot is outside budget.");
            var ids=faces.Take(mesh.Faces.Count+1).ToArray();var selected=new HashSet<ulong>(ids);
            var existing=new HashSet<ulong>(mesh.Faces.Select(f=>f.Id));
            Checks.Require(ids.Length>0 && ids.Length<=mesh.Faces.Count && selected.Count==ids.Length && selected.All(existing.Contains),"INVALID_SELECTION","Select distinct existing face IDs.");
            return new PolygonMesh(mesh.DomainId,mesh.Vertices.Values,mesh.Faces.Select(f=>selected.Contains(f.Id) ? new CageFace(f.Id,slot,f.Corners) : f),mesh.IdWatermarks);
        }
    }
}
