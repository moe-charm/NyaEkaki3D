using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Paint
{
    /// <summary>Camera snapshot of projected triangles. Coverage cuts are hints; raycast decides visibility.</summary>
    public sealed class SurfaceScreenCoverage
    {
        readonly Triangle[] triangles;
        readonly int[] order;
        readonly List<Node> nodes=new List<Node>();
        struct Triangle { internal Vec2 A,B,C;internal Bounds Box; }
        struct Bounds
        {
            internal double X0,Y0,X1,Y1;
            internal Bounds Merge(Bounds b)=>new Bounds { X0=Math.Min(X0,b.X0),Y0=Math.Min(Y0,b.Y0),X1=Math.Max(X1,b.X1),Y1=Math.Max(Y1,b.Y1) };
            internal bool Overlaps(Bounds b)=>X0<=b.X1 && X1>=b.X0 && Y0<=b.Y1 && Y1>=b.Y0;
        }
        struct Node { internal Bounds Box;internal int Start,Count,Left,Right; }
        public SurfaceScreenCoverage(IEnumerable<Vec2> triangleVertices)
        {
            Checks.Require(triangleVertices!=null,"INVALID_SCREEN_MESH","Projected triangles required.");
            var points=triangleVertices.Take(3000001).ToArray();
            Checks.Require(points.Length<=3000000 && points.Length%3==0,"SCREEN_MESH_BUDGET_EXCEEDED","Projected triangle budget exceeded.");
            var items=new List<Triangle>();
            for(int i=0;i<points.Length;i+=3)
            {
                var a=points[i];var b=points[i+1];var c=points[i+2];
                foreach(var p in new[]{a,b,c}) { Checks.Finite(p.X);Checks.Finite(p.Y); }
                if(Cross(a,b,c)==0) continue;
                items.Add(new Triangle { A=a,B=b,C=c,Box=new Bounds { X0=Math.Min(a.X,Math.Min(b.X,c.X)),X1=Math.Max(a.X,Math.Max(b.X,c.X)),Y0=Math.Min(a.Y,Math.Min(b.Y,c.Y)),Y1=Math.Max(a.Y,Math.Max(b.Y,c.Y)) } });
            }
            triangles=items.ToArray();order=Enumerable.Range(0,triangles.Length).ToArray();if(order.Length>0) Build(0,order.Length);
        }
        int Build(int start,int count)
        {
            var box=triangles[order[start]].Box;for(int i=1;i<count;i++) box=box.Merge(triangles[order[start+i]].Box);
            int index=nodes.Count;nodes.Add(new Node { Box=box,Start=start,Count=count });
            if(count<=8) return index;
            bool x=box.X1-box.X0>=box.Y1-box.Y0;
            Array.Sort(order,start,count,Comparer<int>.Create((a,b)=>
            {
                var p=triangles[a].Box;var q=triangles[b].Box;
                int value=(x ? p.X0+p.X1 : p.Y0+p.Y1).CompareTo(x ? q.X0+q.X1 : q.Y0+q.Y1);
                return value!=0 ? value : a.CompareTo(b);
            }));
            int half=count/2,left=Build(start,half),right=Build(start+half,count-half);
            nodes[index]=new Node { Box=box,Count=0,Left=left,Right=right };return index;
        }
        public IReadOnlyList<double> SampleParameters(Vec2 a,Vec2 b,int budget)
        {
            Checks.Finite(a.X);Checks.Finite(a.Y);Checks.Finite(b.X);Checks.Finite(b.Y);
            Checks.Require(budget>=0 && budget<=SurfaceStrokeSampler.MaxRaySamples,"STROKE_BUDGET_EXCEEDED","Invalid screen sample budget.");
            if(a.Equals(b) || nodes.Count==0) return Array.Empty<double>();
            var cuts=new SortedSet<double> { 0,1 };
            var box=new Bounds { X0=Math.Min(a.X,b.X),X1=Math.Max(a.X,b.X),Y0=Math.Min(a.Y,b.Y),Y1=Math.Max(a.Y,b.Y) };
            void Visit(int index)
            {
                var node=nodes[index];if(!node.Box.Overlaps(box)) return;
                if(node.Count==0) { Visit(node.Left);Visit(node.Right);return; }
                for(int i=0;i<node.Count;i++)
                {
                    var tri=triangles[order[node.Start+i]];if(!tri.Box.Overlaps(box)) continue;
                    double lo=0,hi=1,sign=Cross(tri.A,tri.B,tri.C)>0 ? 1 : -1;
                    bool Clip(Vec2 p,Vec2 q)
                    {
                        double first=sign*Cross(p,q,a),last=sign*Cross(p,q,b),delta=last-first;
                        if(delta==0) return first>=0;
                        double crossing=-first/delta;if(delta>0) lo=Math.Max(lo,crossing);else hi=Math.Min(hi,crossing);
                        return lo<=hi;
                    }
                    if(Clip(tri.A,tri.B) && Clip(tri.B,tri.C) && Clip(tri.C,tri.A))
                    {
                        cuts.Add(lo);cuts.Add(hi);
                        Checks.Require(cuts.Count<=budget+2,"STROKE_BUDGET_EXCEEDED","Too many projected surface boundaries.");
                    }
                }
            }
            Visit(0);var sorted=cuts.ToArray();var result=new List<double>();
            for(int i=1;i<sorted.Length;i++)
            {
                double mid=(sorted[i-1]+sorted[i])*.5;
                if(mid>sorted[i-1] && mid<sorted[i]) result.Add(mid);
                result.Add(sorted[i]);
                Checks.Require(result.Count<=budget,"STROKE_BUDGET_EXCEEDED","Projected interval samples exceed gesture budget.");
            }
            return result.AsReadOnly();
        }
        static double Cross(Vec2 a,Vec2 b,Vec2 p)=>((double)b.X-a.X)*((double)p.Y-a.Y)-((double)b.Y-a.Y)*((double)p.X-a.X);
    }
}
