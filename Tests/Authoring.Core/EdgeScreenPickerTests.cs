using System;
using NyaForge.Authoring;
using NyaForge.Authoring.Paint;
using NyaForge.Authoring.Topology;
internal static partial class Program
{
    static void RunEdgeScreenPickerTests()
    {
        PolygonMesh Triangle(Vec3 a,Vec3 b,Vec3 c)=>new PolygonMesh(GraphId(),new[]{new CageVertex(1,a),new CageVertex(2,b),new CageVertex(3,c)},new[]{new CageFace(1,0,new[]{new CageCorner(1,1),new CageCorner(2,2),new CageCorner(3,3)})});
        SurfaceCameraSnapshot Camera(bool perspective,float near=.1f,float far=10)=>new SurfaceCameraSnapshot(new Vec4(1,0,0,0),new Vec4(0,1,0,0),perspective ? new Vec4(0,0,1,0) : new Vec4(0,0,0,1),new Vec4(0,0,1,0),new Vec2(),new Vec2(200,200),near,far);
        var identity=new RestTransform(1,new Vec3());
        Test("screen edge picker uses perspective correct fractions and transform",()=>
        {
            var mesh=Triangle(new Vec3(-1,0,1),new Vec3(2,0,2),new Vec3(0,2,1));
            var hit=PolygonEdgeScreenPicker.Pick(mesh,identity,Camera(true),new Vec2(100,100),5);
            True(hit!=null);Equal(new CageEdgeId(1,2),hit.Location.Edge);Near(1f/3,hit.Location.Fraction);Near(0,hit.Distance);
            var scaled=Triangle(new Vec3(-.01f,0,.01f),new Vec3(.02f,0,.02f),new Vec3(0,.02f,.01f));
            var other=PolygonEdgeScreenPicker.Pick(scaled,new RestTransform(100,new Vec3()),Camera(true),new Vec2(100,100),5);
            Near(hit.Location.Fraction,other.Location.Fraction);
            var ortho=PolygonEdgeScreenPicker.Pick(mesh,identity,Camera(false),new Vec2(100,100),5);Near(1f/3,ortho.Location.Fraction);
            True(PolygonEdgeScreenPicker.Pick(mesh,identity,Camera(true),new Vec2(100,180),5)==null);
            Expect("INVALID_EDGE_PICK",()=>PolygonEdgeScreenPicker.Pick(mesh,identity,Camera(true),new Vec2(),-1));
        });
        Test("screen edge picker clips camera depth and returns original segment fraction",()=>
        {
            var mesh=Triangle(new Vec3(-.05f,0,.05f),new Vec3(1,0,1),new Vec3(0,2,1));var camera=Camera(true,.1f,.8f);
            var hit=PolygonEdgeScreenPicker.Pick(mesh,identity,camera,new Vec2(150,100),2);
            True(hit!=null);Equal(new CageEdgeId(1,2),hit.Location.Edge);
            var p=mesh.Vertices[1].Position*(1-hit.Location.Fraction)+mesh.Vertices[2].Position*hit.Location.Fraction;
            Near(150,camera.Project(p).X);True(hit.Depth>=camera.Near && hit.Depth<=camera.Far);
            var hidden=Triangle(new Vec3(-1,0,-2),new Vec3(1,0,-2),new Vec3(0,1,-2));
            True(PolygonEdgeScreenPicker.Pick(hidden,identity,camera,new Vec2(100,100),20)==null);
        });
    }
}
