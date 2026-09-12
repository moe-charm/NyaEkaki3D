using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    /// <summary>One atomic cut between two boundary-edge locations on a common planar face.</summary>
    public static class PolygonEdgeCut
    {
        public static PolygonMesh Cut(PolygonMesh mesh,IEnumerable<ulong> endpoints,float first,float second)
        {
            Checks.Require(mesh!=null && endpoints!=null,"INVALID_SELECTION","Choose two face edges.");
            var ids=endpoints.Take(5).ToArray();
            Checks.Require(ids.Length==4 && ids[0]!=ids[1] && ids[2]!=ids[3],"INVALID_SELECTION","Specify two edge endpoint pairs.");
            Checks.Finite(first);Checks.Finite(second);
            Checks.Require(first>=0 && first<=1 && second>=0 && second<=1,"INVALID_EDGE_POSITION","Edge positions must be between zero and one.");
            var a=new CageEdgeId(ids[0],ids[1]);var b=new CageEdgeId(ids[2],ids[3]);
            Checks.Require(!a.Equals(b),"INVALID_CUT","Choose two different edges.");
            Checks.Require(mesh.EdgeFaces.ContainsKey(a) && mesh.EdgeFaces.ContainsKey(b),"EDGE_NOT_FOUND","Cut edge is missing.");
            Checks.Require(mesh.EdgeFaces[a].Intersect(mesh.EdgeFaces[b]).Count()==1,"AMBIGUOUS_FACE","Edges must share exactly one face.");
            var candidate=mesh;
            ulong start=Resolve(ref candidate,a,first),end=Resolve(ref candidate,b,second);
            return PolygonFaceSplit.Split(candidate,new[]{start,end});
        }
        internal static ulong Resolve(ref PolygonMesh mesh,CageEdgeId edge,float fraction)
        {
            if(fraction==0) return edge.A;
            if(fraction==1) return edge.B;
            mesh=PolygonEdgeInsertion.Insert(mesh,new[]{edge.A,edge.B},fraction);
            return mesh.IdWatermarks.Vertex;
        }
    }
}
