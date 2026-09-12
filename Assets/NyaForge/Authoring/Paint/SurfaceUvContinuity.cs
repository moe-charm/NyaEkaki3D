using System.Collections.Generic;

namespace NyaForge.Authoring.Paint
{
    /// <summary>Shared logical edge plus identical endpoint UVs; coincident positions alone never join surfaces.</summary>
    internal sealed class SurfaceUvContinuity
    {
        readonly int[,] groups;
        internal SurfaceUvContinuity(MeshData mesh,IReadOnlyList<ulong> vertexIds)
        {
            Checks.Require(vertexIds==null || vertexIds.Count==mesh.VertexCount,"INVALID_VERTEX_MAP","Logical vertex IDs must match rendered vertices.");
            ulong Id(int index)=>vertexIds==null ? (ulong)index+1 : vertexIds[index];
            if(vertexIds!=null)
            {
                var positions=new Dictionary<ulong,Vec3>();
                for(int i=0;i<vertexIds.Count;i++)
                {
                    ulong id=Id(i);Checks.Require(id>0,"INVALID_VERTEX_MAP","Logical vertex ID must be nonzero.");
                    if(positions.TryGetValue(id,out var p)) Checks.Require(p.Equals(mesh.Positions[i]),"INVALID_VERTEX_MAP","One logical vertex ID has inconsistent positions.");
                    else positions.Add(id,mesh.Positions[i]);
                }
            }
            groups=new int[mesh.TriangleCount,3];int triangle=0;
            var lookup=new Dictionary<(ulong,ulong,Vec2,Vec2),int>();
            foreach(var indices in mesh.Submeshes)
                for(int at=0;at<indices.Length;at+=3,triangle++)
                    for(int edge=0;edge<3;edge++)
                    {
                        int a=indices[at+edge],b=indices[at+(edge+1)%3];ulong ia=Id(a),ib=Id(b);
                        var key=ia<ib ? (ia,ib,mesh.Uv0[a],mesh.Uv0[b]) : (ib,ia,mesh.Uv0[b],mesh.Uv0[a]);
                        if(!lookup.TryGetValue(key,out int group)) { group=lookup.Count;lookup.Add(key,group); }
                        groups[triangle,edge]=group;
                    }
        }
        internal bool CanJoin(int a,int b)
        {
            if(a==b) return true;
            for(int i=0;i<3;i++) for(int j=0;j<3;j++) if(groups[a,i]==groups[b,j]) return true;
            return false;
        }
    }
}
