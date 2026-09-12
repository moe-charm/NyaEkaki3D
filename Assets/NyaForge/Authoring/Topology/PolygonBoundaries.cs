using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    public static class PolygonBoundaries
    {
        /// <summary>Directed loops wound opposite to incident faces, suitable for caps.</summary>
        public static IReadOnlyList<ulong[]> Find(PolygonMesh mesh)
        {
            Checks.Require(mesh!=null,"INVALID_MESH","Polygon is required.");
            Checks.Require(mesh.EdgeFaces.All(p=>p.Value.Count<=2),"NONMANIFOLD_BOUNDARY","Nonmanifold edges cannot be capped.");
            var next=new Dictionary<ulong,ulong>();var incoming=new HashSet<ulong>();
            foreach(var face in mesh.Faces)
                for(int i=0;i<face.Corners.Count;i++)
                {
                    ulong a=face.Corners[i].VertexId,b=face.Corners[(i+1)%face.Corners.Count].VertexId;
                    if(mesh.EdgeFaces[new CageEdgeId(a,b)].Count!=1) continue;
                    Checks.Require(!next.ContainsKey(b) && incoming.Add(a),"AMBIGUOUS_BOUNDARY","Boundary branches at a vertex.");
                    next.Add(b,a);
                }
            Checks.Require(next.Keys.All(incoming.Contains),"AMBIGUOUS_BOUNDARY","Boundary is not a closed loop.");
            var remaining=new SortedSet<ulong>(next.Keys);var loops=new List<ulong[]>();
            while(remaining.Count>0)
            {
                ulong start=remaining.Min,current=start;var loop=new List<ulong>();
                do
                {
                    Checks.Require(remaining.Remove(current),"AMBIGUOUS_BOUNDARY","Boundary intersects another loop.");
                    loop.Add(current);current=next[current];
                } while(current!=start);
                loops.Add(loop.ToArray());
            }
            return loops.AsReadOnly();
        }
    }
}
