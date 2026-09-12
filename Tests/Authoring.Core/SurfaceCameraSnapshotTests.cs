using System;
using System.Threading.Tasks;
using NyaForge.Authoring;
using NyaForge.Authoring.Paint;

internal static partial class Program
{
    static void RunSurfaceCameraSnapshotTests()
    {
        SurfaceCameraSnapshot Camera()=>new SurfaceCameraSnapshot(new Vec4(1,0,0,0),new Vec4(0,1,0,0),new Vec4(0,0,1,0),new Vec4(0,0,1,0),new Vec2(10,20),new Vec2(200,100),.1f,10);
        Test("immutable camera snapshot projects perspective coordinates and rejects invalid depth",()=>
        {
            var camera=Camera();Equal(new Vec2(135,45),camera.Project(new Vec3(1,2,4)));Equal(4f,camera.Depth(new Vec3(1,2,4)));
            True(camera.Equals(Camera()));Equal(camera.GetHashCode(),Camera().GetHashCode());
            Expect("INVALID_PAINT_CAMERA",()=>camera.Project(new Vec3(0,0,-1)));
            Expect("INVALID_PAINT_CAMERA",()=>new SurfaceCameraSnapshot(new Vec4(),new Vec4(),new Vec4(),new Vec4(),new Vec2(),new Vec2(0,100),.1f,10));
        });
        Test("camera snapshot builds clipped coverage on a worker without changing results",()=>
        {
            var camera=Camera();var mesh=new MeshData(new[]{new Vec3(-1,-1,-1),new Vec3(1,-1,2),new Vec3(0,1,2)},Array.Empty<Vec3>(),Array.Empty<Vec4>(),Array.Empty<Vec2>(),new[]{new[]{0,1,2}});
            var placement=new RestTransform(1,new Vec3());var before=camera.Project(new Vec3(1,2,4));
            var local=camera.BuildCoverage(mesh,placement);var worker=Task.Run(()=>camera.BuildCoverage(mesh,placement)).GetAwaiter().GetResult();
            var a=new Vec2(-100,70);var b=new Vec2(300,70);
            var expected=local.SampleParameters(a,b,4096);var actual=worker.SampleParameters(a,b,4096);
            Equal(expected.Count,actual.Count);for(int i=0;i<expected.Count;i++) Equal(expected[i],actual[i]);Equal(before,camera.Project(new Vec3(1,2,4)));
        });
    }
}
