using System;
using System.Collections.Generic;
using NyaForge.Authoring;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    internal static class SurfaceCameraVerification
    {
        internal static void Verify(List<string> checks)
        {
            var host=new GameObject("Surface camera snapshot fixture");var camera=host.AddComponent<Camera>();camera.enabled=false;
            try
            {
                camera.nearClipPlane=.05f;camera.farClipPlane=20;camera.aspect=1.37f;camera.fieldOfView=55;camera.orthographicSize=1.2f;
                camera.transform.SetPositionAndRotation(new Vector3(1,2,3),Quaternion.Euler(15,123,0));
                var bounds=new Rect(40,50,900,600);
                foreach(bool orthographic in new[]{false,true})
                {
                    camera.orthographic=orthographic;camera.ResetProjectionMatrix();
                    var matrix=camera.projectionMatrix;matrix.m02+=.12f;matrix.m12-=.07f;camera.projectionMatrix=matrix;
                    var snapshot=SurfaceCameraCapture.Capture(camera,bounds);
                    foreach(var local in new[]{new Vector3(.1f,.2f,.2f),new Vector3(-.3f,.25f,2),new Vector3(1,-1,10)})
                    {
                        var world=camera.transform.TransformPoint(local);var point=new Vec3(world.x,world.y,world.z);
                        var expected=camera.WorldToViewportPoint(world);var actual=snapshot.Project(point);
                        if(Math.Abs(actual.X-(bounds.x+expected.x*bounds.width))>.005f || Math.Abs(actual.Y-(bounds.y+(1-expected.y)*bounds.height))>.005f || Math.Abs(snapshot.Depth(point)-expected.z)>.00001f)
                            throw new InvalidOperationException("Core camera snapshot differs from Unity projection");
                    }
                    var sample=camera.transform.TransformPoint(new Vector3(0,0,2));var p=new Vec3(sample.x,sample.y,sample.z);var frozen=snapshot.Project(p);
                    camera.transform.position+=new Vector3(.1f,0,0);
                    if(!snapshot.Project(p).Equals(frozen) || snapshot.Equals(SurfaceCameraCapture.Capture(camera,bounds)))
                        throw new InvalidOperationException("Captured camera changed with live camera");
                }
                checks.Add("Camera snapshot: perspective/orthographic and offset projection agree with Unity; immutable after live-camera movement");
            }
            finally { UnityEngine.Object.Destroy(host); }
        }
    }
}
