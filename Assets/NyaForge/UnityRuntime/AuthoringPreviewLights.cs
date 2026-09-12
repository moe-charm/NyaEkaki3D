using UnityEngine;

namespace NyaForge.UnityRuntime
{
    internal static class AuthoringPreviewLights
    {
        // Owned by the workbench stage; closing the workbench disables the lights together with its camera.
        public static void Create(Transform parent)
        {
            Add(parent,"Authoring key light",new Vector3(30,-35,0),1.1f);
            Add(parent,"Authoring fill light",new Vector3(-20,145,0),.45f);
        }
        static void Add(Transform parent,string name,Vector3 angles,float intensity)
        {
            var root=new GameObject(name);root.transform.SetParent(parent,false);root.transform.localRotation=Quaternion.Euler(angles);
            var light=root.AddComponent<Light>();light.type=LightType.Directional;light.color=Color.white;light.intensity=intensity;
            light.cullingMask=1<<OwnedMeshProjection.PreviewLayer;light.shadows=LightShadows.None;
        }
    }
}
