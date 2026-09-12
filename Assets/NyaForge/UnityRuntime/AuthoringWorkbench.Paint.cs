using System;
using System.Collections.Generic;
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
        Foldout paintPanel;
        DropdownField paintStage, paintPalette;
        FloatField paintRadius, paintOpacity;
        Label paintInfo;
        Button addPaint;
        PaintCanvas paintCanvas;
        PaintEditContext strokeContext;
        readonly List<string> paintIds = new List<string>();
        string selectedPaint = "";
        static readonly Rgba32[] Palette = { new Rgba32(245,70,145),new Rgba32(40,160,230),new Rgba32(255,210,50),new Rgba32(40,190,120),new Rgba32(255,255,255),new Rgba32(20,20,25) };

        void BuildPaint(VisualElement parent)
        {
            paintPanel = new Foldout { text = "色塗り", value = false, name = "paint-panel" }; parent.Add(paintPanel);
            addPaint = Button("最終出力に色塗りを追加", () => Try(AddPaintToOutput), "add-paint"); paintPanel.Add(addPaint);
            BuildSlotPaint();
            paintStage = new DropdownField("Paint段", new List<string>{"なし"},0); paintStage.style.flexDirection = FlexDirection.Column;
            paintStage.RegisterValueChangedCallback(_ => { paintCanvas.CancelStroke(); selectedPaint = paintStage.index >= 0 && paintStage.index < paintIds.Count ? paintIds[paintStage.index] : ""; RefreshPaint(); });
            paintPanel.Add(paintStage);
            BuildSurfacePaint(paintPanel);
            BuildPaintLayers(paintPanel);
            paintPalette = new DropdownField("色",new List<string>{"ピンク","青","黄","緑","白","黒"},0); paintPanel.Add(paintPalette);
            paintRadius = Number(paintPanel,"半径 (px)",8,"paint-radius");
            paintOpacity = Number(paintPanel,"不透明度 0〜1",1,"paint-opacity");
            paintInfo = new Label(); paintInfo.style.whiteSpace = WhiteSpace.Normal; paintPanel.Add(paintInfo);
            BuildPaintRebind(paintPanel);
            paintCanvas = new PaintCanvas(); paintPanel.Add(paintCanvas);
            paintPanel.Add(Button("描きかけを取り消す",paintCanvas.CancelStroke,"cancel-paint-stroke"));
            BuildPaintExport(paintPanel);
            paintPanel.RegisterValueChangedCallback(e => { if (!e.newValue) { paintCanvas.CancelStroke();CancelSurfaceStroke(); } });
            paintCanvas.Started += () =>
            {
                CancelSurfaceStroke();
                if (!float.IsFinite(paintOpacity.value) || paintOpacity.value < 0 || paintOpacity.value > 1) throw new InvalidOperationException("不透明度は0〜1で指定してください。");
                var graph=workspace.Document.ActiveObject.Graph;
                strokeLayerContext=null;
                if(graph.Nodes[selectedPaint].LayerStack!=null) { strokeLayerContext=LayerEditing.Context(graph,selectedPaint);strokeLayerId=selectedPaintLayer; }
                else strokeContext = PaintEditing.Context(graph,selectedPaint);
                var color = Palette[paintPalette.index];
                paintCanvas.Color = new Rgba32(color.R,color.G,color.B,(byte)Mathf.RoundToInt(paintOpacity.value*255));
                paintCanvas.Radius = paintRadius.value;
                ConfigureMaskStroke();
            };
            paintCanvas.Completed += (points,radius,color) => Execute(strokeLayerContext==null ?
                AuthoringOperation.PaintImageStroke(strokeContext,points,radius,color) :
                AuthoringOperation.EditPaintLayers(strokeLayerContext,strokeIsMask ? PaintLayerChange.MaskStroke(strokeLayerId,points,radius,strokeMaskTarget,strokeMaskStrength) : PaintLayerChange.Stroke(strokeLayerId,points,radius,color)));
            paintCanvas.Failed += SetStatus;
        }
        void AddPaintToOutput()
        {
            var graph = workspace.Document.ActiveObject.Graph;
            string id = Guid.NewGuid().ToString("D");
            var route=OutputSurfaceConnections.Resolve(graph);if(route==null) throw new InvalidOperationException("最終出力の接続を確認してください。");
            var mesh=route.Geometry;
            Execute(AuthoringOperation.AddNode(GraphNode.Paint(id)),AuthoringOperation.Connect(new GraphEdge(mesh.FromNode,mesh.FromPort,id,"mesh")),AuthoringOperation.Connect(new GraphEdge(id,"image",route.ImageTargetNodeId,"baseColor")));
            selectedPaint = id; SelectEditStage(0); RefreshPaint(); Frame();
        }
        void RefreshPaint()
        {
            var graph = IsGraph ? workspace.Document.ActiveObject.Graph : null;
            addPaint.SetEnabled(graph != null && workspace.Preview.IsComplete && workspace.Preview.Output?.Polygon?.Faces.Count > 0 && workspace.Preview.Output.BaseColor == null && workspace.Preview.Output.SlotMaterials==null);
            RefreshSlotPaint();
            paintIds.Clear();
            if (graph != null) paintIds.AddRange(graph.Nodes.Values.Where(n=>n.Version==1 && (n.TypeId==BuiltinNodes.Paint || n.TypeId==BuiltinNodes.LayeredPaint)).Select(n=>n.NodeId).OrderBy(id=>id,StringComparer.Ordinal));
            if (!paintIds.Contains(selectedPaint)) selectedPaint = paintIds.FirstOrDefault() ?? "";
            paintStage.choices = paintIds.Count == 0 ? new List<string>{"なし"} : paintIds.Select(id=>"Paint · "+id.Substring(0,8)).ToList();
            paintStage.SetValueWithoutNotify(paintIds.Count == 0 ? "なし" : paintStage.choices[paintIds.IndexOf(selectedPaint)]);
            paintStage.SetEnabled(paintIds.Count > 0);
            GraphImageValue value = null;
            if (selectedPaint != "") workspace.Preview.Evaluation.ImageOutputs.TryGetValue(selectedPaint,out value);
            var node = selectedPaint == "" ? null : graph.Nodes[selectedPaint];
            RefreshPaintRebind(node,value);
            var editableImage=RefreshPaintLayers(node,value);
            string key = workspace.InstanceId + ":" + selectedPaint + ":" + (node?.LayerStack!=null ? workspace.Document.StateHash+":"+selectedPaintLayer : value?.ImageHash ?? workspace.Document.StateHash) + ":" + value?.UvHash + ":" + value?.MeshDomain;
            paintCanvas.Bind(editableImage,key+":"+EditingMask); paintCanvas.SetEnabled(value != null && editableImage!=null);
            paintPalette.style.display=EditingMask ? DisplayStyle.None : DisplayStyle.Flex;
            paintOpacity.label=EditingMask ? "筆の強さ 0〜1" : "不透明度 0〜1";
            exportPaintPng.SetEnabled(value != null);
            paintInfo.text = value == null ? (node == null ? "四角面などを作り、色塗りを追加してください。" : "UV対応が未解決です。旧画像は保持し、描画を停止しています。") :
                value.Image.Width + "×" + value.Image.Height + "。画像をドラッグして描画。離すと確定、Escapeで取消。3Dは確定後に更新。";
            if(node?.LayerStack!=null && value!=null) paintInfo.text="2Dは選択レイヤー、3D・PNG・Unity出力は全層の合成。下から上の順に重ねます。";
            if(EditingMask && value!=null) paintInfo.text="マスク：白は表示、黒は非表示。色画像は保持します。離すと確定、Escapeで取消。3Dと出力は全層の合成です。";
            RefreshSurfacePaint(node);
        }
    }
}

