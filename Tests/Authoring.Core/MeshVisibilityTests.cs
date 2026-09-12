using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Geometry;
using NyaForge.Authoring.Paint;
using NyaForge.Authoring.Topology;
internal static partial class Program
{
    static void RunMeshVisibilityTests()
    {
        Test("edge visibility skips nearest hidden edge and finds visible alternative",()=>
        {
            var positions=new[]{new Vec3(-.5f,0,2),new Vec3(.5f,0,2),new Vec3(.5f,.5f,2),new Vec3(-.5f,.5f,2),new Vec3(-.6f,-.05f,1),new Vec3(.6f,-.05f,1),new Vec3(.6f,.6f,1),new Vec3(-.6f,.6f,1)};
            ulong corner=0;
            CageFace Face(ulong id,ulong[] ids)=>new CageFace(id,0,ids.Select(v=>new CageCorner(++corner,v)));
            var polygon=new PolygonMesh(GraphId(),positions.Select((p,i)=>new CageVertex((ulong)i+1,p)),new[]{Face(1,new ulong[]{1,2,3,4}),Face(2,new ulong[]{5,6,7,8})});
            var transform=new RestTransform(1,new Vec3());var geometry=new MeshRaycast(PolygonRenderAdapter.Build(polygon).Mesh,transform);
            var camera=new SurfaceCameraSnapshot(new Vec4(1,0,0,0),new Vec4(0,1,0,0),new Vec4(0,0,0,1),new Vec4(0,0,1,0),new Vec2(),new Vec2(200,200),.1f,10);
            var point=new Vec2(100,100);
            Equal(new CageEdgeId(1,2),PolygonEdgeScreenPicker.Pick(polygon,transform,camera,point,10).Location.Edge);
            var hit=PolygonEdgeScreenPicker.Pick(polygon,transform,camera,point,10,p=>MeshVisibility.IsVisible(geometry,new Vec3(p.X,p.Y,.1f),p));
            True(hit!=null);Equal(new CageEdgeId(5,6),hit.Location.Edge);Near(.5f,hit.Location.Fraction);
            True(PolygonEdgeScreenPicker.Pick(polygon,transform,camera,point,2,p=>MeshVisibility.IsVisible(geometry,new Vec3(p.X,p.Y,.1f),p))==null);
            True(MeshVisibility.IsVisible(geometry,new Vec3(0,0,.1f),new Vec3(0,0,1)));
            True(!MeshVisibility.IsVisible(geometry,new Vec3(0,0,.1f),new Vec3(0,0,2)));
            True(MeshVisibility.IsVisible(geometry,new Vec3(0,0,1),new Vec3(0,0,1)));
        });
    }
}
