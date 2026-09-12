using System;
using System.Linq;
using NyaForge.Authoring.Paint;
namespace NyaForge.Authoring.Topology
{
    public sealed class PolygonEdgeScreenHit
    {
        public EdgeCutLocation Location { get; }
        public float Distance { get; }
        public float Depth { get; }
        internal PolygonEdgeScreenHit(CageEdgeId edge,float fraction,float distance,float depth)
        { Location=new EdgeCutLocation(edge.A,edge.B,fraction);Distance=distance;Depth=depth; }
    }
    /// <summary>Screen-space edge proximity, with depth clipping and perspective-correct fractions.
    /// Optional predicate filters candidate world points (e.g. face occlusion). Equal distances prefer nearer depth then stable edge IDs.</summary>
    public static class PolygonEdgeScreenPicker
    {
        public static PolygonEdgeScreenHit Pick(PolygonMesh mesh,RestTransform transform,SurfaceCameraSnapshot camera,Vec2 point,float radius,Func<Vec3,bool> acceptPoint=null)
        {
            Checks.Require(mesh!=null && camera!=null,"INVALID_EDGE_PICK","Mesh and camera required.");
            Checks.Finite(point.X);Checks.Finite(point.Y);Checks.Finite(radius);transform.Validate();
            Checks.Require(radius>=0,"INVALID_EDGE_PICK","Pick radius cannot be negative.");
            PolygonEdgeScreenHit best=null;double bestSquared=(double)radius*radius;
            foreach(var edge in mesh.EdgeFaces.Keys.OrderBy(e=>e.A).ThenBy(e=>e.B))
            {
                var a=transform.ToAvatarPoint(mesh.Vertices[edge.A].Position);var b=transform.ToAvatarPoint(mesh.Vertices[edge.B].Position);
                double da=camera.Depth(a),db=camera.Depth(b),start=0,end=1;
                if(da==db) { if(da<camera.Near || da>camera.Far) continue; }
                else
                {
                    double near=(camera.Near-da)/(db-da),far=(camera.Far-da)/(db-da);
                    start=Math.Max(0,Math.Min(near,far));end=Math.Min(1,Math.Max(near,far));
                    if(start>end) continue;
                }
                var ca=Lerp(a,b,start);var cb=Lerp(a,b,end);
                double wa=camera.ProjectionWeight(ca),wb=camera.ProjectionWeight(cb);if(wa<=0 || wb<=0) continue;
                var pa=camera.Project(ca);var pb=camera.Project(cb);
                double dx=(double)pb.X-pa.X,dy=(double)pb.Y-pa.Y,length=dx*dx+dy*dy;
                double q=length==0 ? (camera.Depth(ca)<=camera.Depth(cb) ? 0 : 1) : Math.Max(0,Math.Min(1,(((double)point.X-pa.X)*dx+((double)point.Y-pa.Y)*dy)/length));
                double x=pa.X+q*dx-point.X,y=pa.Y+q*dy-point.Y,squared=x*x+y*y;
                if(squared>bestSquared) continue;
                double local=q*wa/((1-q)*wb+q*wa),fraction=start+(end-start)*local;
                float depth=(float)(da+(db-da)*fraction);
                if(best!=null && squared==bestSquared && depth>=best.Depth) continue;
                if(acceptPoint!=null && !acceptPoint(Lerp(a,b,fraction))) continue;
                best=new PolygonEdgeScreenHit(edge,(float)fraction,(float)Math.Sqrt(squared),depth);bestSquared=squared;
            }
            return best;
        }
        static Vec3 Lerp(Vec3 a,Vec3 b,double t)=>new Vec3((float)(a.X+((double)b.X-a.X)*t),(float)(a.Y+((double)b.Y-a.Y)*t),(float)(a.Z+((double)b.Z-a.Z)*t));
    }
}

