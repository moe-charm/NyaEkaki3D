using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace NyaForge.UnityBridge.Editor
{
    internal static class SurfaceRenderVerification
    {
        internal static void Write(GameObject prefab,string path,bool requireRed=true)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) throw new InvalidOperationException("Surface capture requires graphics.");
            GameObject instance = null, cameraObject = null, lightObject = null;
            RenderTexture target = null; Texture2D image = null;
            var active = RenderTexture.active; var ambient = RenderSettings.ambientLight; var mode = RenderSettings.ambientMode;
            try
            {
                instance = Object.Instantiate(prefab); instance.transform.rotation = Quaternion.Euler(12,-25,0);
                foreach (var child in instance.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
                var bounds = instance.GetComponentInChildren<MeshRenderer>().bounds;
                cameraObject = new GameObject("NyaForge capture camera");
                var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false;
                camera.orthographic = true; camera.orthographicSize = Mathf.Max(bounds.extents.y,bounds.extents.x)*1.3f;
                camera.transform.position = bounds.center + Vector3.back*(bounds.size.magnitude+1);
                camera.nearClipPlane = .01f; camera.farClipPlane = bounds.size.magnitude+3;
                camera.cullingMask = 1<<31; camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.035f,.045f,.065f,1);
                lightObject = new GameObject("NyaForge capture light"); var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional; light.intensity = 1; light.cullingMask = 1<<31;
                light.transform.rotation = Quaternion.Euler(35,-25,0);
                RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.35f,.35f,.35f);
                target = new RenderTexture(512,512,24); target.Create(); camera.targetTexture = target; camera.Render();
                RenderTexture.active = target; image = new Texture2D(512,512,TextureFormat.RGBA32,false);
                image.ReadPixels(new Rect(0,0,512,512),0,0); image.Apply();
                int colored = 0;
                foreach (var pixel in image.GetPixels32()) if (requireRed ? pixel.r > 100 && pixel.r > pixel.b*1.3f : Mathf.Max(pixel.r,pixel.g,pixel.b)>100) colored++;
                if (colored < 1000) throw new InvalidOperationException("Surface capture has no expected colored object.");
                File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = active; RenderSettings.ambientMode = mode; RenderSettings.ambientLight = ambient;
                if (cameraObject) Object.DestroyImmediate(cameraObject); if (lightObject) Object.DestroyImmediate(lightObject);
                if (instance) Object.DestroyImmediate(instance); if (image) Object.DestroyImmediate(image);
                if (target) { target.Release(); Object.DestroyImmediate(target); }
            }
        }
    }
}
