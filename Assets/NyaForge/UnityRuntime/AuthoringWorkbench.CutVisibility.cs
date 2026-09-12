using NyaForge.Authoring;
using NyaForge.Authoring.Geometry;
using NyaForge.Authoring.Topology;
using UnityEngine;
using UnityEngine.UIElements;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Toggle cutVisibleOnly;
        MeshData cutVisibilitySource;
        MeshRaycast cutVisibilityGeometry;
        void BuildCutVisibility(VisualElement parent)
        {
            cutVisibleOnly=new Toggle("隠れた位置を除外") { value=true,name="cut-visible-only" };parent.Add(cutVisibleOnly);
            var help=new Label("ON：編集中の形状の面で隠れた位置を除外。透明な材質も面として扱います。OFF：裏側も候補。");help.style.whiteSpace=WhiteSpace.Normal;parent.Add(help);
            cutVisibleOnly.RegisterValueChangedCallback(_=>ClearCutPathHover());
        }
        PolygonEdgeScreenHit FindCutPathHit(Vector2 position)
        {
            var value=DisplayedGraphValue();if(value?.Polygon==null) return null;
            System.Func<Vec3,bool> accept=null;
            if(cutVisibleOnly.value && value.Mesh!=null)
            {
                if(!ReferenceEquals(cutVisibilitySource,value.Mesh) || cutVisibilityGeometry==null || !cutVisibilityGeometry.Transform.Equals(value.Transform))
                { cutVisibilitySource=value.Mesh;cutVisibilityGeometry=new MeshRaycast(value.Mesh,value.Transform); }
                accept=point=>
                {
                    var world=new Vector3(point.X,point.Y,point.Z);
                    var viewport=camera.WorldToViewportPoint(world);var ray=camera.ViewportPointToRay(viewport);
                    return MeshVisibility.IsVisible(cutVisibilityGeometry,new Vec3(ray.origin.x,ray.origin.y,ray.origin.z),point);
                };
            }
            return PolygonEdgeScreenPicker.Pick(value.Polygon,value.Transform,SurfaceCameraCapture.Capture(camera,view.worldBound),new Vec2(position.x,position.y),12,accept);
        }
    }
}
