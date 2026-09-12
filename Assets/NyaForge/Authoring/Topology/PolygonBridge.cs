using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    public static class PolygonBridge
    {
        public static PolygonMesh Connect(PolygonMesh mesh,IEnumerable<ulong> first,IEnumerable<ulong> second,int offset=-1)
        {
            Checks.Require(mesh!=null && first!=null && second!=null,"INVALID_SELECTION","Choose two boundary loops.");
            var a=first.Take(257).ToArray();var b=second.Take(257).ToArray();
            Checks.Require(a.Length>=3 && a.Length<=256 && a.Length==b.Length && a.Distinct().Count()==a.Length && b.Distinct().Count()==b.Length && !a.Intersect(b).Any(),"INVALID_BRIDGE","Choose two disjoint loops with the same vertex count (3..256).");
            var boundaries=PolygonBoundaries.Find(mesh);
            var firstSet=new HashSet<ulong>(a);var secondSet=new HashSet<ulong>(b);
            a=boundaries.SingleOrDefault(loop=>loop.Length==a.Length && loop.All(firstSet.Contains));
            b=boundaries.SingleOrDefault(loop=>loop.Length==secondSet.Count && loop.All(secondSet.Contains));
            Checks.Require(a!=null && b!=null,"INVALID_BRIDGE","Each selection must be one complete boundary.");
            int n=a.Length;Checks.Require(offset>=-1 && offset<n,"INVALID_BRIDGE","Offset must be -1 (automatic) or a loop index.");
            if(offset==-1)
            {
                double best=double.PositiveInfinity;
                for(int candidate=0;candidate<n;candidate++)
                {
                    double distance=0;
                    for(int i=0;i<n;i++)
                    {
                        var x=mesh.Vertices[a[i]].Position;var y=mesh.Vertices[b[(candidate-i+n)%n]].Position;
                        double dx=(double)x.X-y.X,dy=(double)x.Y-y.Y,dz=(double)x.Z-y.Z;distance+=dx*dx+dy*dy+dz*dz;
                    }
                    if(distance<best) { best=distance;offset=candidate; }
                }
            }
            Checks.Require(mesh.IdWatermarks.Face<=ulong.MaxValue-(ulong)n && mesh.IdWatermarks.Corner<=ulong.MaxValue-(ulong)n*4,"ELEMENT_ID_EXHAUSTED","No element IDs remain.");
            ulong faceId=mesh.IdWatermarks.Face,cornerId=mesh.IdWatermarks.Corner;var faces=mesh.Faces.ToList();
            for(int i=0;i<n;i++)
            {
                int next=(i+1)%n;
                var loop=new[]{a[i],a[next],b[(offset-next+n)%n],b[(offset-i+n)%n]};
                var set=new HashSet<ulong>(loop);
                Checks.Require(!mesh.Faces.Any(f=>f.Corners.Count==4 && f.Corners.All(c=>set.Contains(c.VertexId))),"DUPLICATE_FACE","Bridge would duplicate an existing face.");
                ulong adjacent=mesh.EdgeFaces[new CageEdgeId(a[i],a[next])][0];int material=mesh.Faces.Single(f=>f.Id==adjacent).Material;
                var bare=new CageFace(++faceId,material,loop.Select(v=>new CageCorner(++cornerId,v)).ToArray());
                faces.Add(PolygonNewFace.Project(mesh,bare));
            }
            var result=new PolygonMesh(mesh.DomainId,mesh.Vertices.Values,faces,mesh.IdWatermarks);
            Checks.Require(result.EdgeFaces.All(p=>p.Value.Count<=2),"NONMANIFOLD_BRIDGE","Bridge would overuse an existing edge.");
            PolygonRenderAdapter.Build(result);return result;
        }
    }
}
