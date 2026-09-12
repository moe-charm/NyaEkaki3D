using NyaForge.Authoring;
using NyaForge.Authoring.Paint;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    internal static class SurfaceCameraCapture
    {
        // Call on the Unity thread. Returned snapshot contains only Core value types.
        internal static SurfaceCameraSnapshot Capture(Camera camera,Rect bounds)
        {
            var matrix=camera.projectionMatrix*camera.worldToCameraMatrix;
            var forward=camera.transform.forward;var position=camera.transform.position;
            Vec4 Row(int index) { var row=matrix.GetRow(index);return new Vec4(row.x,row.y,row.z,row.w); }
            return new SurfaceCameraSnapshot(Row(0),Row(1),Row(3),new Vec4(forward.x,forward.y,forward.z,-Vector3.Dot(forward,position)),
                new Vec2(bounds.x,bounds.y),new Vec2(bounds.width,bounds.height),camera.nearClipPlane,camera.farClipPlane);
        }
    }
}
