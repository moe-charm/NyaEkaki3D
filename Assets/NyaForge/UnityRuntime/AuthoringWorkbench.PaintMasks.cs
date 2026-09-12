using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Paint;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Foldout layerDetails;
        TextField layerName;
        DropdownField paintTarget,maskBrush;
        bool strokeIsMask;
        byte strokeMaskTarget;
        float strokeMaskStrength;
        PaintMask displayedMask;
        PaintImage maskDisplay;
        bool EditingMask=>paintTarget.index==1;

        void BuildPaintMasks(VisualElement parent)
        {
            layerDetails=new Foldout { text="名前・マスク設定",value=false,name="paint-layer-details" };parent.Add(layerDetails);
            layerName=new TextField("レイヤー名") { name="paint-layer-name" };layerName.style.flexDirection=FlexDirection.Column;layerDetails.Add(layerName);
            layerDetails.Add(Button("名前を適用",()=>Try(()=>EditLayer(PaintLayerChange.Rename(selectedPaintLayer,layerName.value))),"paint-layer-rename"));
            layerDetails.Add(Button("白マスクを追加",()=>Try(()=>
            {
                var layer=CurrentPaintLayer();
                EditLayer(PaintLayerChange.Mask(layer.Id,new PaintMask(layer.Image.Width,layer.Image.Height,Enumerable.Repeat((byte)255,layer.Image.Width*layer.Image.Height).ToArray())));
                paintTarget.value=paintTarget.choices[1];
            }),"paint-mask-add"));
            layerDetails.Add(Button("マスクを削除",()=>Try(()=>EditLayer(PaintLayerChange.Mask(selectedPaintLayer,null))),"paint-mask-remove"));
            paintTarget=new DropdownField("描く対象",new List<string>{"色画像","マスク"},0) { name="paint-target" };
            paintTarget.style.flexDirection=FlexDirection.Column;parent.Add(paintTarget);
            paintTarget.RegisterValueChangedCallback(_=> { paintCanvas.CancelStroke();RefreshPaint(); });
            maskBrush=new DropdownField("マスク筆",new List<string>{"黒：隠す","白：表示する"},0) { name="paint-mask-brush" };
            maskBrush.style.flexDirection=FlexDirection.Column;parent.Add(maskBrush);
            maskBrush.RegisterValueChangedCallback(_=> { paintCanvas.CancelStroke();CancelSurfaceStroke(); });
        }
        PaintLayer CurrentPaintLayer()=>workspace.Document.ActiveObject.Graph.Nodes[selectedPaint].LayerStack.Layers.Single(l=>l.Id==selectedPaintLayer);
        PaintImage RefreshPaintMask(PaintLayer layer)
        {
            layerDetails.SetEnabled(layer!=null);layerName.SetValueWithoutNotify(layer?.Name ?? "");
            layerDetails.Q<Button>("paint-mask-add").SetEnabled(layer!=null && layer.Mask==null);
            layerDetails.Q<Button>("paint-mask-remove").SetEnabled(layer?.Mask!=null);
            if(layer?.Mask==null) paintTarget.SetValueWithoutNotify(paintTarget.choices[0]);
            paintTarget.SetEnabled(layer?.Mask!=null);
            maskBrush.style.display=EditingMask ? DisplayStyle.Flex : DisplayStyle.None;
            if(!EditingMask) { displayedMask=null;maskDisplay=null;return layer?.Image; }
            if(!ReferenceEquals(displayedMask,layer.Mask)) { displayedMask=layer.Mask;maskDisplay=PaintMaskDisplay.Create(displayedMask); }
            return maskDisplay;
        }
        void ConfigureMaskStroke()
        {
            strokeIsMask=strokeLayerContext!=null && EditingMask;
            paintCanvas.PreviewOperation=PaintStroke.Apply;
            if(!strokeIsMask) return;
            var mask=CurrentPaintLayer().Mask;
            if(mask==null) throw new InvalidOperationException("マスクを追加してから描画してください。");
            strokeMaskTarget=maskBrush.index==0 ? (byte)0 : (byte)255;
            strokeMaskStrength=paintOpacity.value;
            byte target=strokeMaskTarget;float strength=strokeMaskStrength;
            paintCanvas.PreviewOperation=(_,points,radius,color)=>PaintMaskDisplay.Create(PaintMaskStroke.Apply(mask,points,radius,target,strength));
        }
    }
}
