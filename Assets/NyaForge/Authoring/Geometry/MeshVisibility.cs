using System;
namespace NyaForge.Authoring.Geometry
{
    /// <summary>Opaque, double-sided geometry occlusion up to a candidate point. The target surface itself is allowed.</summary>
    public static class MeshVisibility
    {
        public static bool IsVisible(MeshRaycast geometry,Vec3 rayOrigin,Vec3 target)
        {
            Checks.Require(geometry!=null,"MESH_MISSING","Visibility geometry required.");Checks.Finite(rayOrigin);Checks.Finite(target);
            double x=(double)target.X-rayOrigin.X,y=(double)target.Y-rayOrigin.Y,z=(double)target.Z-rayOrigin.Z;
            double distance=Math.Sqrt(x*x+y*y+z*z);if(distance==0) return true;
            double tolerance=Math.Max(1e-6,distance*1e-5);
            var direction=new Vec3((float)(x/distance),(float)(y/distance),(float)(z/distance));
            var hit=geometry.Raycast(rayOrigin,direction,false,distance);
            return hit==null || hit.Distance>=distance-tolerance;
        }
    }
}
