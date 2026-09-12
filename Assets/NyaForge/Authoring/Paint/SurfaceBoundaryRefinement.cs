using System;

namespace NyaForge.Authoring.Paint
{
    /// <summary>Refines observed hit transitions; does not claim to detect hidden geometry between equal hits.</summary>
    internal static class SurfaceBoundaryRefinement
    {
        internal const float Tolerance=1f/64;
        internal static void Append(Vec2 a,SurfacePaintHit left,Vec2 b,SurfacePaintHit right,
            Func<Vec2,SurfacePaintHit> resolve,Action<SurfacePaintHit> emit,int depth=0)
        {
            double dx=(double)b.X-a.X,dy=(double)b.Y-a.Y;
            bool same=left==null ? right==null : right!=null && left.TriangleIndex==right.TriangleIndex && left.IsUvInRange==right.IsUvInRange;
            if(same || depth>=12 || dx*dx+dy*dy<=Tolerance*Tolerance) { emit(right);return; }
            var middle=new Vec2((float)((double)a.X+dx*.5),(float)((double)a.Y+dy*.5));
            if(middle.Equals(a) || middle.Equals(b)) { emit(right);return; }
            var hit=resolve(middle);
            Append(a,left,middle,hit,resolve,emit,depth+1);
            Append(middle,hit,b,right,resolve,emit,depth+1);
        }
    }
}
