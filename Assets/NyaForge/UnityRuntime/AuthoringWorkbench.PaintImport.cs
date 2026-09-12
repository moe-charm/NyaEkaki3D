using System;
using System.Collections;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Paint;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Foldout paintImportPanel;
        TextField paintImportPath;
        Toggle fitImportedImage;
        bool importPickerOpen;
        void BuildPaintImport(VisualElement parent)
        {
            paintImportPanel=new Foldout { text="PNGを新しい層へ",value=false,name="paint-import" };parent.Add(paintImportPanel);
            var help=new Label("PNG・最大1024×1024 / 16 MiB。sRGBとして読み込みます。ICC・HDR・アニメーションは未対応。");
            help.style.whiteSpace=WhiteSpace.Normal;paintImportPanel.Add(help);
            fitImportedImage=new Toggle("サイズを合わせる") { value=true };paintImportPanel.Add(fitImportedImage);
            var fitHelp=new Label("縦横比を保って収め、余白を透明にします。オフの場合は同じ寸法の画像のみ追加します。");
            fitHelp.style.whiteSpace=WhiteSpace.Normal;paintImportPanel.Add(fitHelp);
            paintImportPanel.Add(Button("PNGを選んで追加",()=> { if(!importPickerOpen) StartCoroutine(PickPaintImage()); },"paint-import-browse"));
            paintImportPath=new TextField("ファイルパス") { name="paint-import-path" };paintImportPath.style.flexDirection=FlexDirection.Column;paintImportPanel.Add(paintImportPath);
            paintImportPanel.Add(Button("このPNGを追加",()=>Try(()=>ImportPaintImage(paintImportPath.value)),"paint-import-apply"));
        }
        void ImportPaintImage(string path)
        {
            paintCanvas.CancelStroke();var node=workspace.Document.Objects[0].Graph.Nodes[selectedPaint];
            var image=PaintPngImporter.Read(path);
            string name=Path.GetFileNameWithoutExtension(path);
            if(string.IsNullOrWhiteSpace(name) || name.Length>128) name="取り込み画像";
            string id=Guid.NewGuid().ToString("D");
            EditLayer(PaintLayerImport.Prepare(image,node.PaintWidth,node.PaintHeight,fitImportedImage.value,id,name,node.LayerStack.Layers.Count));
            if(!workspace.Document.Objects[0].Graph.Nodes[selectedPaint].LayerStack.Layers.Any(l=>l.Id==id)) return;
            selectedPaintLayer=id;RefreshPaint();
        }
        IEnumerator PickPaintImage()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var previous=workspace;string state=workspace.Document.StateHash,node=selectedPaint;
            importPickerOpen=true;
            try
            {
                var picker=Platform.WindowsFilePicker.Open(Platform.WindowsFilePicker.GetActiveWindow(),PaintImportDirectory(paintImportPath.value),
                    "PNG画像 (*.png)\0*.png\0\0","NyaForge — PNGを新しい層に追加","png");
                while(!picker.IsCompleted) yield return null;
                if(picker.IsFaulted) { SetStatus(picker.Exception.GetBaseException().Message);yield break; }
                if(string.IsNullOrEmpty(picker.Result)) yield break;
                if(!ReferenceEquals(previous,workspace) || state!=workspace.Document.StateHash || node!=selectedPaint)
                { SetStatus("選択中に作品が変わったため、画像の追加を取り消しました。");yield break; }
                paintImportPath.SetValueWithoutNotify(picker.Result);Try(()=>ImportPaintImage(picker.Result));
            }
            finally { importPickerOpen=false; }
#else
            SetStatus("ファイル選択はWindows版に対応しています。パスを指定して追加してください。");yield break;
#endif
        }
        static string PaintImportDirectory(string path)
        {
            try { return Path.GetDirectoryName(path); }
            catch(ArgumentException) { return null; }
        }
    }
}
