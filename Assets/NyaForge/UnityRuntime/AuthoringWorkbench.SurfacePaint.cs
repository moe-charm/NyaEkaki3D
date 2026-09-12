using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Toggle surfacePaintMode;
        IVisualElementScheduledItem surfaceCaptureWatch;
        SurfaceStrokeSampler surfaceStroke;
        LayerEditContext surfaceContext;
        PaintLayers surfaceOriginal;
        PaintLayer surfaceLayer;
        bool surfaceMask;
        float surfaceRadius,surfaceStrength;
        byte surfaceMaskTarget;
        Rgba32 surfaceColor;
        int surfacePointer;
        int[] surfacePaintSlots;

        void BuildSurfacePaint(VisualElement parent)
        {
            surfacePaintMode=new Toggle("3Dで塗る") { name="surface-paint-mode" };parent.Add(surfacePaintMode);
            BuildSurfacePreparation(parent);
            var help=new Label("レイヤー編集で使用。左ドラッグで描画、Alt+左で回転、右で移動。筆の半径はテクスチャpx。");
            help.style.whiteSpace=WhiteSpace.Normal;parent.Add(help);
            surfacePaintMode.RegisterValueChangedCallback(e=>
            {
                CancelSurfaceStroke();paintCanvas.CancelStroke();if(e.newValue) SelectEditStage(0);Refresh();
            });
            view.focusable=true;
            surfaceCaptureWatch=view.schedule.Execute(()=>
            {
                if(surfaceStroke!=null && !view.HasPointerCapture(surfacePointer)) CancelSurfaceStroke();
            }).Every(16);surfaceCaptureWatch.Pause();
            view.RegisterCallback<PointerDownEvent>(e=>
            {
                if(surfaceStroke!=null) CancelSurfaceStroke();
                if(!active || !surfacePaintMode.value || e.button!=0 || e.altKey) return;
                SurfacePaintAction(()=>BeginSurfaceStroke(e.pointerId,e.position));e.StopImmediatePropagation();
            },TrickleDown.TrickleDown);
            view.RegisterCallback<PointerMoveEvent>(e=>
            {
                if(surfaceStroke==null || e.pointerId!=surfacePointer) return;
                SurfacePaintAction(()=>AppendSurfaceStroke(e.position));e.StopImmediatePropagation();
            },TrickleDown.TrickleDown);
            view.RegisterCallback<PointerUpEvent>(e=>
            {
                if(surfaceStroke==null || e.pointerId!=surfacePointer || e.button!=0) return;
                SurfacePaintAction(()=>
                {
                    AppendSurfaceStroke(e.position);var path=surfaceStroke.Snapshot();
                    var operation=path==null ? null : AuthoringOperation.EditPaintLayers(surfaceContext,surfaceMask ?
                        PaintLayerChange.MaskPathStroke(surfaceLayer.Id,path,surfaceRadius,surfaceMaskTarget,surfaceStrength) :
                        PaintLayerChange.PathStroke(surfaceLayer.Id,path,surfaceRadius,surfaceColor));
                    CancelSurfaceStroke();if(operation!=null) Execute(operation);
                });e.StopImmediatePropagation();
            },TrickleDown.TrickleDown);
            view.RegisterCallback<PointerCaptureOutEvent>(_=>CancelSurfaceStroke());
            view.RegisterCallback<PointerCancelEvent>(_=>CancelSurfaceStroke());
            view.RegisterCallback<DetachFromPanelEvent>(_=>ClearSurfacePreparation());
            view.RegisterCallback<GeometryChangedEvent>(_=>CancelSurfaceStroke());
            view.RegisterCallback<WheelEvent>(_=>CancelSurfaceStroke(),TrickleDown.TrickleDown);
            view.RegisterCallback<KeyDownEvent>(e=> { if(e.keyCode==KeyCode.Escape && surfaceStroke!=null) { CancelSurfaceStroke();e.StopImmediatePropagation(); } });
        }
        void SurfacePaintAction(Action action)
        {
            try { action(); } catch(Exception e) { CancelSurfaceStroke();SetStatus(e.Message); }
        }
        void RefreshSurfacePaint(GraphNode node)
        {
            CancelSurfaceStroke();
            bool wasEnabled=surfacePaintMode.value;
            var graph=IsGraph ? workspace.Document.Objects[0].Graph : null;
            bool enabled=node?.LayerStack!=null && node.LayerStack.Layers.Count>0 && workspace.Preview.IsComplete &&
                (OutputSurfaceConnections.Resolve(graph)?.ImageNodeId==selectedPaint || OutputSurfaceConnections.ImageSlots(graph,selectedPaint).Count>0);
            surfacePaintMode.SetEnabled(enabled);
            if(!enabled || projection.PreviewNodeId!="") surfacePaintMode.SetValueWithoutNotify(false);
            if(wasEnabled && !surfacePaintMode.value) RefreshFaceEditing();
            RefreshViewportHint();
            RefreshSurfacePreparation();
        }
        void BeginSurfaceStroke(int pointer,Vector2 point)
        {
            paintCanvas.CancelStroke();
            var prepared=ReadySurface();if(prepared==null) return;
            if(!float.IsFinite(paintOpacity.value) || paintOpacity.value<0 || paintOpacity.value>1) throw new InvalidOperationException("筆の強さは0〜1で指定してください。");
            if(!float.IsFinite(paintRadius.value) || paintRadius.value<.5f || paintRadius.value>512) throw new InvalidOperationException("半径は0.5〜512pxで指定してください。");
            var graph=workspace.Document.Objects[0].Graph;
            var output=workspace.Preview.Output;
            surfacePaintSlots=output.SlotMaterials==null ? null : OutputSurfaceConnections.ImageSlots(graph,selectedPaint).ToArray();
            var slotMap=output.PolygonRendering?.MaterialSlotMap ?? Enumerable.Range(0,output.Mesh.Submeshes.Count).ToArray();
            var allowed=surfacePaintSlots==null ? null : new System.Collections.Generic.HashSet<int>(surfacePaintSlots);
            if(allowed!=null && allowed.Count==0) throw new InvalidOperationException("選択したPaintを使用する部位がありません。");
            surfaceContext=LayerEditing.Context(graph,selectedPaint);surfaceOriginal=graph.Nodes[selectedPaint].LayerStack;surfaceLayer=CurrentPaintLayer();
            surfaceMask=EditingMask;surfaceStrength=paintOpacity.value;surfaceRadius=paintRadius.value;surfaceMaskTarget=maskBrush.index==0 ? (byte)0 : (byte)255;
            var color=Palette[paintPalette.index];surfaceColor=new Rgba32(color.R,color.G,color.B,(byte)Mathf.RoundToInt(surfaceStrength*255));
            surfaceStroke=new SurfaceStrokeSampler(prepared.RayMesh,p=>
            {
                var rect=view.worldBound;if(!rect.Contains(new Vector2(p.X,p.Y))) return null;
                var ray=camera.ViewportPointToRay(new Vector3((p.X-rect.x)/rect.width,1-(p.Y-rect.y)/rect.height,0));
                float forward=Vector3.Dot(ray.direction,camera.transform.forward);
                float remaining=camera.farClipPlane-Vector3.Dot(ray.origin-camera.transform.position,camera.transform.forward);
                if(forward<=0 || remaining<0) return null;
                var hit=prepared.RayMesh.Raycast(new Vec3(ray.origin.x,ray.origin.y,ray.origin.z),new Vec3(ray.direction.x,ray.direction.y,ray.direction.z),false,remaining/forward);
                // Keep the nearest surface as an occluder even when it belongs to another Paint.
                return hit!=null && allowed!=null && !allowed.Contains(slotMap[hit.SubmeshIndex]) ? null : hit;
            },screenCoverage:prepared.Coverage);
            surfacePointer=pointer;orbiting=false;view.Focus();view.CapturePointer(pointer);surfaceCaptureWatch.Resume();AppendSurfaceStroke(point);
        }
        void AppendSurfaceStroke(Vector2 point)
        {
            surfaceStroke.Append(new Vec2(point.x,point.y));var path=surfaceStroke.Snapshot();if(path==null) return;
            var layer=surfaceMask ? surfaceLayer.WithMask(PaintMaskStroke.ApplyPaths(surfaceLayer.Mask,path,surfaceRadius,surfaceMaskTarget,surfaceStrength)) :
                surfaceLayer.WithImage(PaintStroke.ApplyPaths(surfaceLayer.Image,path,surfaceRadius,surfaceColor));
            var image=surfaceOriginal.Replace(layer).Composite();
            if(surfacePaintSlots==null) projection.ShowPaintPreview(image);else projection.ShowPaintPreviews(image,surfacePaintSlots);
        }
        void CancelSurfaceStroke()
        {
            surfaceCaptureWatch?.Pause();bool drawing=surfaceStroke!=null;
            surfaceStroke=null;surfaceOriginal=null;surfaceLayer=null;surfaceContext=null;surfacePaintSlots=null;
            if(drawing) { view.ReleasePointer(surfacePointer);projection?.ShowPaintPreview(null); }
        }
    }
}

