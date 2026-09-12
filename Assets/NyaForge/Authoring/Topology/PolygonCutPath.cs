using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    public sealed class EdgeCutLocation
    {
        public CageEdgeId Edge { get; }
        public float Fraction { get; }
        public EdgeCutLocation(ulong a,ulong b,float fraction)
        {
            Checks.Finite(fraction);Checks.Require(fraction>=0 && fraction<=1,"INVALID_EDGE_POSITION","Edge position must be between zero and one.");
            Edge=new CageEdgeId(a,b);Fraction=fraction;
        }
    }
    public static class PolygonCutPath
    {
        /// <summary>Ordered edge crossings; each original face is traversed at most once.</summary>
        public static PolygonMesh Cut(PolygonMesh mesh,IEnumerable<EdgeCutLocation> path)
        {
            Checks.Require(mesh!=null && path!=null,"INVALID_SELECTION","Specify a cut path.");
            var points=path.Take(257).ToArray();
            Checks.Require(points.Length>=2 && points.Length<=256 && points.All(p=>p!=null),"INVALID_CUT_PATH","A path requires 2..256 edge locations.");
            Checks.Require(points.Select(p=>p.Edge).Distinct().Count()==points.Length,"INVALID_CUT_PATH","A path cannot revisit an edge.");
            Checks.Require(points.All(p=>mesh.EdgeFaces.ContainsKey(p.Edge)),"EDGE_NOT_FOUND","Path edge is missing.");
            var visited=new HashSet<ulong>();
            for(int i=1;i<points.Length;i++)
            {
                var common=mesh.EdgeFaces[points[i-1].Edge].Intersect(mesh.EdgeFaces[points[i].Edge]).ToArray();
                Checks.Require(common.Length==1 && visited.Add(common[0]),"INVALID_CUT_PATH","Consecutive edges must cross a distinct common face.");
            }
            var candidate=mesh;var vertices=new ulong[points.Length];
            for(int i=0;i<points.Length;i++) vertices[i]=PolygonEdgeCut.Resolve(ref candidate,points[i].Edge,points[i].Fraction);
            for(int i=1;i<vertices.Length;i++) candidate=PolygonFaceSplit.Split(candidate,new[]{vertices[i-1],vertices[i]});
            return candidate;
        }
    }
}
