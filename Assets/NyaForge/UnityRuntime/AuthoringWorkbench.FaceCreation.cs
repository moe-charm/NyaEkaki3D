using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Topology;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Foldout faceCreatePanel;
        TextField facePerimeter;
        IntegerField faceCreateMaterial;
        Label faceCreateStatus;
        Button faceCreateButton,faceAppendButton;
        BoundaryHighlightProjection faceCreateOutline;
        string faceDraftContext;

        void BuildFaceCreation(VisualElement parent)
        {
            faceCreatePanel=new Foldout { text="頂点から面を作る",value=false,name="face-create-panel" };parent.Add(faceCreatePanel);
            faceCreatePanel.Add(new Label("周囲の順に頂点を1つずつ登録"));
            faceAppendButton=Button("選択した1頂点を末尾へ",()=>Try(()=>
            {
                var selected=SelectedPolygonVertices();
                if(selected.Length!=1) throw new InvalidOperationException("頂点を1つ選択してください。");
                var ids=ReadFacePerimeter();
                if(ids.Contains(selected[0])) throw new InvalidOperationException("この頂点は登録済みです。");
                facePerimeter.value=string.Join(", ",ids.Concat(selected));
            }),"face-append-vertex");faceCreatePanel.Add(faceAppendButton);
            faceCreatePanel.Add(new Label("頂点IDの順番（カンマ区切り）"));
            facePerimeter=new TextField { name="face-perimeter",multiline=true };faceCreatePanel.Add(facePerimeter);
            faceCreatePanel.Add(Button("順番を反転",()=>Try(()=>facePerimeter.value=string.Join(", ",ReadFacePerimeter().Reverse())),"face-reverse"));
            faceCreatePanel.Add(Button("末尾を削除",()=>Try(()=> { var ids=ReadFacePerimeter();facePerimeter.value=string.Join(", ",ids.Take(Math.Max(0,ids.Length-1))); }),"face-pop"));
            faceCreatePanel.Add(Button("登録をクリア",()=>facePerimeter.value="","face-clear"));
            faceCreatePanel.Add(new Label("材質スロット"));
            faceCreateMaterial=new IntegerField { value=0,name="face-create-material" };faceCreatePanel.Add(faceCreateMaterial);
            faceCreateStatus=new Label { name="face-create-status" };faceCreateStatus.style.whiteSpace=WhiteSpace.Normal;faceCreatePanel.Add(faceCreateStatus);
            faceCreateButton=Button("この順番で面を作る",()=>Try(()=>
            {
                Execute(AuthoringOperation.CreatePolygonFace(activeEditContext,ReadFacePerimeter(),faceCreateMaterial.value));
            }),"create-polygon-face");faceCreatePanel.Add(faceCreateButton);
            facePerimeter.RegisterValueChangedCallback(_=>RefreshFaceCreation());
            faceCreateMaterial.RegisterValueChangedCallback(_=>RefreshFaceCreation());
            faceCreatePanel.RegisterValueChangedCallback(_=>RefreshFaceCreation());
        }
        ulong[] ReadFacePerimeter()
        {
            var words=(facePerimeter.value??"").Split(new[]{',',' ','\r','\n','\t'},StringSplitOptions.RemoveEmptyEntries);
            if(words.Length>256) throw new InvalidOperationException("頂点は256個までです。");
            return words.Select(w=> { if(!ulong.TryParse(w,out var id) || id==0) throw new InvalidOperationException("頂点IDは正の整数で指定してください。");return id; }).ToArray();
        }
        void RefreshFaceCreation()
        {
            if(faceCreateButton==null) return;
            string context=activeEditContext==null ? null : activeEditContext.GraphId+"/"+activeEditContext.NodeId+"/"+workspace.Document.StateHash;
            if(context!=faceDraftContext) { faceDraftContext=context;facePerimeter.SetValueWithoutNotify(""); }
            bool editable=activeEditContext!=null && DisplayedGraphValue()?.Polygon!=null;
            faceAppendButton.SetEnabled(editable && !faceMode.value && SelectedPolygonVertices().Length==1);
            faceCreateButton.SetEnabled(false);faceCreateOutline?.Clear();
            if(!faceCreatePanel.value) return;
            if(!editable) { faceCreateStatus.text="PolygonEdit段で操作してください。";return; }
            try
            {
                var ids=ReadFacePerimeter();
                if(ids.Length<3) { faceCreateStatus.text="3頂点以上を周囲の順に登録してください。";return; }
                var value=DisplayedGraphValue();
                PolygonFaceCreation.Create(value.Polygon,ids,faceCreateMaterial.value);
                if(faceCreateOutline==null) faceCreateOutline=new BoundaryHighlightProjection(stage.transform,new Color(.4f,1,.5f));
                faceCreateOutline.Show(value.Polygon,value.Transform,ids);
                faceCreateStatus.text=ids.Length+"頂点の輪郭を表示中。面を作成できます。";faceCreateButton.SetEnabled(true);
            }
            catch(Exception e) { faceCreateStatus.text=e.Message; }
        }
    }
}
