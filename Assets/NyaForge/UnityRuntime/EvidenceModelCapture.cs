using System;
using NyaForge.Authoring.Evidence;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace NyaForge.UnityRuntime
{
    /// <summary>Unity-thread-only model capture in its own scene. Never reads live workbench projection.</summary>
    internal static class EvidenceModelCapture
    {
        internal static EvidenceImage Capture(EvaluatedSnapshot snapshot,EvidenceView view)
        {
            if(snapshot==null || view==null || snapshot.State!=EvidenceState.Ready) throw new InvalidOperationException("Model capture requires a ready mesh snapshot.");
            var scene=SceneManager.CreateScene("Evidence-"+Guid.NewGuid().ToString("N"));
            GameObject root=null,cameraRoot=null;Mesh mesh=null;Material template=null;MaterialSurfaceSet materials=null;RenderTexture target=null;Texture2D pixels=null;
            var previous=RenderTexture.active;
            try
            {
                root=new GameObject("Evidence model") { layer=31 };SceneManager.MoveGameObjectToScene(root,scene);
                var value=snapshot.Value;root.transform.position=OwnedMeshProjection.ToUnity(value.Transform.Translation);root.transform.localScale=Vector3.one*value.Transform.Scale;
                mesh=OwnedMeshProjection.CreateMesh(value.Mesh);root.AddComponent<MeshFilter>().sharedMesh=mesh;
                var shader=Resources.Load<Shader>("AuthoringSurface");if(!shader) throw new InvalidOperationException("Evidence shader unavailable.");
                template=new Material(shader) { color=new Color(.22f,.72f,.69f) };
                materials=new MaterialSurfaceSet(value.Mesh.Submeshes.Count,template,value.BaseColor?.Image,value.Material?.Parameters,value);
                root.AddComponent<MeshRenderer>().sharedMaterials=materials.Materials;
                cameraRoot=new GameObject("Evidence camera");SceneManager.MoveGameObjectToScene(cameraRoot,scene);var camera=cameraRoot.AddComponent<Camera>();
                camera.enabled=false;camera.scene=scene;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.04f,.06f,.08f,1);
                camera.orthographic=true;camera.orthographicSize=view.OrthographicSize;camera.nearClipPlane=view.Near;camera.farClipPlane=view.Far;camera.allowHDR=false;camera.allowMSAA=false;
                camera.transform.position=OwnedMeshProjection.ToUnity(view.Position);camera.transform.rotation=Quaternion.LookRotation(OwnedMeshProjection.ToUnity(view.Target)-camera.transform.position,OwnedMeshProjection.ToUnity(view.Up));
                target=new RenderTexture(view.Width,view.Height,24,RenderTextureFormat.ARGB32);target.Create();camera.targetTexture=target;camera.aspect=(float)view.Width/view.Height;camera.Render();
                pixels=new Texture2D(view.Width,view.Height,TextureFormat.RGBA32,false);RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,view.Width,view.Height),0,0);pixels.Apply();
                return new EvidenceImage(snapshot,view,pixels.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=previous;
                if(cameraRoot) cameraRoot.SetActive(false);if(root) root.SetActive(false);
                materials?.Dispose();if(template) UnityEngine.Object.Destroy(template);if(mesh) UnityEngine.Object.Destroy(mesh);if(pixels) UnityEngine.Object.Destroy(pixels);
                if(target) { target.Release();UnityEngine.Object.Destroy(target); }
                SceneManager.UnloadSceneAsync(scene);
            }
        }
    }
}
