using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Paint
{
    /// <summary>Bounded Douglas-Peucker reduction, independently per section; authored paths remain immutable.</summary>
    public static class PaintPathSimplifier
    {
        public const int MaxInputPoints=4096;
        // At every currently supported image size, the UV deviation is at most 1/8 texture pixel.
        public const float UvTolerance=1f/(8*PaintImage.MaxDimension);
        public static PaintStrokePath Reduce(IEnumerable<IEnumerable<Vec2>> sections)
        {
            Checks.Require(sections!=null,"INVALID_STROKE","Stroke sections required.");
            var output=new List<IEnumerable<Vec2>>();int count=0,work=0;
            foreach(var section in sections.Take(MaxInputPoints+1))
            {
                Checks.Require(section!=null,"INVALID_STROKE","Stroke section required.");
                var points=section.Take(MaxInputPoints-count+1).ToArray();count+=points.Length;
                Checks.Require(points.Length>0 && count<=MaxInputPoints,"STROKE_BUDGET_EXCEEDED","Raw stroke exceeds sample budget.");
                foreach(var p in points)
                {
                    Checks.Finite(p.X);Checks.Finite(p.Y);
                    Checks.Require(p.X>=0 && p.X<=1 && p.Y>=0 && p.Y<=1,"INVALID_STROKE","Stroke UV must be in 0..1.");
                }
                var keep=new bool[points.Length];keep[0]=keep[points.Length-1]=true;
                var pending=new Stack<(int Start,int End)>();pending.Push((0,points.Length-1));
                while(pending.Count>0)
                {
                    var range=pending.Pop();int farthest=-1;double distance=(double)UvTolerance*UvTolerance;
                    for(int i=range.Start+1;i<range.End;i++)
                    {
                        Checks.Require(++work<=8388608,"STROKE_BUDGET_EXCEEDED","Path simplification work budget exceeded.");
                        double value=DistanceSquared(points[i],points[range.Start],points[range.End]);
                        if(value>distance) { distance=value;farthest=i; }
                    }
                    if(farthest<0) continue;
                    keep[farthest]=true;pending.Push((farthest,range.End));pending.Push((range.Start,farthest));
                }
                output.Add(points.Where((_,i)=>keep[i]).ToArray());
            }
            return new PaintStrokePath(output);
        }
        static double DistanceSquared(Vec2 p,Vec2 a,Vec2 b)
        {
            double dx=(double)b.X-a.X,dy=(double)b.Y-a.Y,px=(double)p.X-a.X,py=(double)p.Y-a.Y;
            double length=dx*dx+dy*dy,t=length==0 ? 0 : Math.Max(0,Math.Min(1,(px*dx+py*dy)/length));
            double x=px-t*dx,y=py-t*dy;return x*x+y*y;
        }
    }
}
