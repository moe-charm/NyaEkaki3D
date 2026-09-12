using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Geometry;
internal static partial class Program
{
    static void RunMeshRaycastTests()
    {
        Test("geometry BVH accepts UV-free mesh and preserves barycentrics indices and distance",()=>
        {
            var original=RayTriangle();var mesh=new MeshData(original.Positions.ToArray(),Array.Empty<Vec3>(),Array.Empty<Vec4>(),Array.Empty<Vec2>(),original.Submeshes.ToArray());
            foreach(float scale in new[]{1f,100f,.000001f})
            {
                var geometry=new MeshRaycast(mesh,new RestTransform(scale,new Vec3(0,0,2)));
                var hit=geometry.Raycast(new Vec3(.2f*scale,.3f*scale,5),new Vec3(0,0,-12));
                True(hit!=null && hit.IsFrontFace);Equal(0,hit.A);Equal(1,hit.B);Equal(2,hit.C);
                Near(.2f,(float)hit.U);Near(.3f,(float)hit.V);Near(3,(float)hit.Distance);
                True(geometry.Raycast(new Vec3(.2f*scale,.3f*scale,5),new Vec3(0,0,-1),false,2.99)==null);
            }
        });
    }
}
