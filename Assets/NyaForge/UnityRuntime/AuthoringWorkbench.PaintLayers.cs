using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Button migratePaint;
        Foldout layerPanel;
        DropdownField paintLayerChoice;
        FloatField layerOpacity;
        Toggle layerVisible;
        string selectedPaintLayer="";
        string[] paintLayerIds=Array.Empty<string>();
        LayerEditContext strokeLayerContext;
        string strokeLayerId;

        void BuildPaintLayers(VisualElement parent)
        {
            migratePaint=Button("レイヤー編集へ移行",()=>Try(()=>
            {
                string id=Guid.NewGuid().ToString("D");
                Execute(AuthoringOperation.MigratePaintLayers(PaintEditing.Context(workspace.Document.ActiveObject.Graph,selectedPaint),id));
                selectedPaintLayer=id;RefreshPaint();
            }),"migrate-paint-layers");parent.Add(migratePaint);
            layerPanel=new Foldout { text="レイヤー",value=true,name="paint-layers" };parent.Add(layerPanel);
            paintLayerChoice=new DropdownField("編集する層");paintLayerChoice.style.flexDirection=FlexDirection.Column;layerPanel.Add(paintLayerChoice);
            paintLayerChoice.RegisterValueChangedCallback(_=>
            {
                paintCanvas.CancelStroke();selectedPaintLayer=paintLayerChoice.index>=0 && paintLayerChoice.index<paintLayerIds.Length ? paintLayerIds[paintLayerChoice.index] : "";RefreshPaint();
            });
            layerPanel.Add(Button("透明レイヤーを追加",()=>Try(()=>
            {
                var node=workspace.Document.ActiveObject.Graph.Nodes[selectedPaint];string id=Guid.NewGuid().ToString("D");
                EditLayer(PaintLayerChange.Add(new PaintLayer(id,"レイヤー "+(node.LayerStack.Layers.Count+1),new PaintImage(node.PaintWidth,node.PaintHeight,new Rgba32())),node.LayerStack.Layers.Count));
                selectedPaintLayer=id;RefreshPaint();
            }),"paint-layer-add"));
            var order=Row(layerPanel);
            order.Add(Button("下へ",()=>MovePaintLayer(-1),"paint-layer-down"));order.Add(Button("上へ",()=>MovePaintLayer(1),"paint-layer-up"));
            layerPanel.Add(Button("選択レイヤーを削除",()=>Try(()=>EditLayer(PaintLayerChange.Remove(selectedPaintLayer))),"paint-layer-remove"));
            layerVisible=new Toggle("選択レイヤーを表示");layerPanel.Add(layerVisible);
            layerOpacity=Number(layerPanel,"層の不透明度",1,"paint-layer-opacity");
            layerPanel.Add(Button("表示・不透明度を適用",()=>Try(()=>EditLayer(PaintLayerChange.Appearance(selectedPaintLayer,layerOpacity.value,layerVisible.value))),"paint-layer-appearance"));
            BuildPaintMasks(layerPanel);
            BuildPaintImport(layerPanel);
        }
        void MovePaintLayer(int delta) => Try(()=>
        {
            int index=Array.IndexOf(paintLayerIds,selectedPaintLayer),next=index+delta;
            if(index>=0 && next>=0 && next<paintLayerIds.Length) EditLayer(PaintLayerChange.Move(selectedPaintLayer,next));
        });
        void EditLayer(PaintLayerChange change)
        {
            paintCanvas.CancelStroke();
            Execute(AuthoringOperation.EditPaintLayers(LayerEditing.Context(workspace.Document.ActiveObject.Graph,selectedPaint),change));
        }
        PaintImage RefreshPaintLayers(GraphNode node,GraphImageValue value)
        {
            bool layered=node?.LayerStack!=null;
            bool importedImage=node?.TypeId==BuiltinNodes.Paint && node.PaintImage!=null && node.PaintUvHash=="" && node.ExpectedDomain=="";
            migratePaint.style.display=node?.TypeId==BuiltinNodes.Paint ? DisplayStyle.Flex : DisplayStyle.None;
            migratePaint.SetEnabled(value!=null && !importedImage);
            layerPanel.style.display=layered ? DisplayStyle.Flex : DisplayStyle.None;
            layerPanel.SetEnabled(value!=null);
            paintLayerIds=layered ? node.LayerStack.Layers.Select(l=>l.Id).ToArray() : Array.Empty<string>();
            if(!paintLayerIds.Contains(selectedPaintLayer)) selectedPaintLayer=paintLayerIds.FirstOrDefault() ?? "";
            paintLayerChoice.choices=layered && paintLayerIds.Length>0 ? node.LayerStack.Layers.Select((l,i)=>(i+1)+": "+l.Name).ToList() : new System.Collections.Generic.List<string>{"レイヤーなし"};
            int index=Array.IndexOf(paintLayerIds,selectedPaintLayer);
            paintLayerChoice.SetValueWithoutNotify(paintLayerChoice.choices[Math.Max(0,index)]);
            var layer=index<0 ? null : node.LayerStack.Layers[index];
            paintLayerChoice.SetEnabled(layer!=null);
            layerVisible.SetEnabled(layer!=null);layerOpacity.SetEnabled(layer!=null);
            layerPanel.Q<Button>("paint-layer-remove").SetEnabled(layer!=null);
            layerPanel.Q<Button>("paint-layer-appearance").SetEnabled(layer!=null);
            layerPanel.Q<Button>("paint-layer-down").SetEnabled(index>0);
            layerPanel.Q<Button>("paint-layer-up").SetEnabled(index>=0 && index<paintLayerIds.Length-1);
            layerPanel.Q<Button>("paint-layer-add").SetEnabled(layered && paintLayerIds.Length<16);
            paintImportPanel.SetEnabled(layered && paintLayerIds.Length<16);
            layerVisible.SetValueWithoutNotify(layer?.Visible ?? true);layerOpacity.SetValueWithoutNotify(layer?.Opacity ?? 1);
            var image=RefreshPaintMask(layer);
            return layered ? image : value?.Image ?? node?.PaintImage;
        }
    }
}
