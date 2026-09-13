using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Foldout materialPanel;
        VisualElement materialFields;
        Button addMaterial;
        DropdownField materialChoice,materialAlphaMode;
        TextField materialTint,materialEmission;
        Label semanticTextureInfo;
        FloatField materialOpacity,materialMetallic,materialRoughness,materialEmissionStrength,materialCutoff;
        readonly List<string> materialIds=new List<string>();
        string selectedMaterial="",loadedTint,loadedEmission;
        float loadedEmissionStrength;
        MaterialParameters loadedMaterial;

        void BuildMaterials(VisualElement parent)
        {
            materialPanel=new Foldout { text="材質",value=false,name="material-panel" };parent.Add(materialPanel);
            addMaterial=Button("材質を追加（画像を引継ぐ）",()=>Try(()=>
            {
                var graph=workspace.Document.ActiveObject.Graph;string id=Guid.NewGuid().ToString("D");
                Execute(AuthoringOperation.ReplaceGraph(OutputSurfaceConnections.AddMaterial(graph,id,Guid.NewGuid().ToString("D"))));
                if(workspace.Document.ActiveObject.Graph.Nodes.ContainsKey(id)) selectedMaterial=id;
                SelectEditStage(0);RefreshMaterials();
            }),"material-add");materialPanel.Add(addMaterial);
            materialChoice=new DropdownField("編集する材質",new List<string>{"なし"},0);materialChoice.style.flexDirection=FlexDirection.Column;materialPanel.Add(materialChoice);
            materialChoice.RegisterValueChangedCallback(_=> { CancelMaterialGesture();selectedMaterial=materialChoice.index>=0 && materialChoice.index<materialIds.Count ? materialIds[materialChoice.index] : "";RefreshMaterials(); });
            BuildMaterialSlots();
            materialFields=new VisualElement();materialPanel.Add(materialFields);
            materialTint=MaterialColorField("基本色 #RRGGBB","material-tint");
            materialOpacity=Number(materialFields,"不透明度 0〜1",1,"material-opacity");
            materialMetallic=Number(materialFields,"金属感 0〜1",0,"material-metallic");
            materialRoughness=Number(materialFields,"粗さ 0〜1",.5f,"material-roughness");
            materialEmission=MaterialColorField("発光色 #RRGGBB","material-emission");
            materialEmissionStrength=Number(materialFields,"発光の強さ 0〜64",0,"material-emission-strength");
            materialAlphaMode=new DropdownField("透明方式",new List<string>{"不透明","切り抜き","半透明"},0) { name="material-alpha-mode" };materialFields.Add(materialAlphaMode);
            materialCutoff=Number(materialFields,"切り抜き閾値 0〜1",.5f,"material-cutoff");
            materialAlphaMode.RegisterValueChangedCallback(_=> { CancelMaterialGesture();materialCutoff.SetEnabled(materialAlphaMode.index==1); });
            foreach(var field in new[]{materialOpacity,materialMetallic,materialRoughness,materialEmissionStrength,materialCutoff})
            {
                field.style.flexDirection=FlexDirection.Column;
                field.RegisterValueChangedCallback(_=>CancelMaterialGesture());
            }
            materialFields.Add(Button("材質の変更を適用",()=>Try(ApplyMaterial),"material-apply"));
            semanticTextureInfo=new Label { name="material-semantic-textures" }; semanticTextureInfo.style.whiteSpace=WhiteSpace.Normal; materialFields.Add(semanticTextureInfo);
            var help=new Label("基本色と発光色はsRGB。画像に基本色を掛けて表示します。材質は作品へ保存でき、対応するUnity用profileへ書き出せます。");
            help.style.whiteSpace=WhiteSpace.Normal;materialFields.Add(help);
        }
        TextField MaterialColorField(string label,string name)
        {
            var field=new TextField(label) { name=name };field.style.flexDirection=FlexDirection.Column;materialFields.Add(field);
            field.RegisterValueChangedCallback(_=>CancelMaterialGesture());return field;
        }
        void CancelMaterialGesture() { paintCanvas?.CancelStroke();CancelSurfaceStroke(); }
        static string MaterialHex(Vec3 linear)=>"#"+ColorUtility.ToHtmlStringRGB(new Color(linear.X,linear.Y,linear.Z).gamma);
        static Vec3 MaterialLinear(string hex)
        {
            if(hex==null || hex.Length!=7 || hex[0]!='#' || !ColorUtility.TryParseHtmlString(hex,out var color)) throw new InvalidOperationException("色は #RRGGBB の6桁で指定してください。");
            color=color.linear;return new Vec3(color.r,color.g,color.b);
        }
        void ApplyMaterial()
        {
            var graph=workspace.Document.ActiveObject.Graph;
            if(loadedMaterial==null || !graph.Nodes.TryGetValue(selectedMaterial,out var node) || node.Material?.ContentHash!=loadedMaterial.ContentHash)
                throw new InvalidOperationException("材質が更新されました。選び直してから編集してください。");
            var tint=materialTint.value==loadedTint ? new Vec3(loadedMaterial.BaseColor.X,loadedMaterial.BaseColor.Y,loadedMaterial.BaseColor.Z) : MaterialLinear(materialTint.value);
            float strength=materialEmissionStrength.value;
            if(!float.IsFinite(strength) || strength<0 || strength>64) throw new InvalidOperationException("発光の強さは0〜64で指定してください。");
            Vec3 emission;
            if(materialEmission.value==loadedEmission)
                emission=strength==loadedEmissionStrength ? loadedMaterial.Emission : loadedEmissionStrength>0 ? loadedMaterial.Emission*(strength/loadedEmissionStrength) : MaterialLinear(materialEmission.value)*strength;
            else emission=MaterialLinear(materialEmission.value)*strength;
            // Editing scalar material values must retain imported semantic maps.
            // They belong to the material node, not to the UI fields being edited.
            var parameters=new MaterialParameters(new Vec4(tint.X,tint.Y,tint.Z,materialOpacity.value),materialMetallic.value,materialRoughness.value,emission,(MaterialAlphaMode)materialAlphaMode.index,materialCutoff.value,loadedMaterial.Textures);
            if(parameters.ContentHash==loadedMaterial.ContentHash) return;
            Execute(AuthoringOperation.UpdateNode(GraphNode.StandardMaterial(selectedMaterial,parameters)));
        }
        void RefreshMaterials()
        {
            if(materialPanel==null) return;
            RefreshMaterialSlots();
            var graph=IsGraph ? workspace.Document.ActiveObject.Graph : null;var route=OutputSurfaceConnections.Resolve(graph);
            addMaterial.SetEnabled(route!=null && route.MaterialNodeId=="" && workspace.Preview.IsComplete && workspace.Preview.Output?.Mesh!=null);
            materialIds.Clear();if(graph!=null) materialIds.AddRange(graph.Nodes.Values.Where(n=>n.Material!=null).Select(n=>n.NodeId).OrderBy(id=>id,StringComparer.Ordinal));
            if(!materialIds.Contains(selectedMaterial)) selectedMaterial=route?.MaterialNodeId!="" && materialIds.Contains(route?.MaterialNodeId) ? route.MaterialNodeId : materialIds.FirstOrDefault() ?? "";
            materialChoice.choices=materialIds.Count==0 ? new List<string>{"なし"} : materialIds.Select(id=>"材質 · "+id.Substring(0,8)).ToList();
            materialChoice.SetValueWithoutNotify(materialIds.Count==0 ? "なし" : materialChoice.choices[materialIds.IndexOf(selectedMaterial)]);materialChoice.SetEnabled(materialIds.Count>0);
            materialFields.style.display=selectedMaterial=="" ? DisplayStyle.None : DisplayStyle.Flex;
            materialFields.SetEnabled(selectedMaterial!="");loadedMaterial=selectedMaterial=="" ? null : graph.Nodes[selectedMaterial].Material;if(loadedMaterial==null) return;
            var p=loadedMaterial;loadedTint=MaterialHex(new Vec3(p.BaseColor.X,p.BaseColor.Y,p.BaseColor.Z));materialTint.SetValueWithoutNotify(loadedTint);
            materialOpacity.SetValueWithoutNotify(p.BaseColor.W);materialMetallic.SetValueWithoutNotify(p.Metallic);materialRoughness.SetValueWithoutNotify(p.Roughness);
            loadedEmissionStrength=Mathf.Max(p.Emission.X,p.Emission.Y,p.Emission.Z);
            loadedEmission=MaterialHex(loadedEmissionStrength>0 ? p.Emission*(1/loadedEmissionStrength) : new Vec3(1,1,1));
            materialEmission.SetValueWithoutNotify(loadedEmission);materialEmissionStrength.SetValueWithoutNotify(loadedEmissionStrength);
            materialAlphaMode.SetValueWithoutNotify(materialAlphaMode.choices[(int)p.AlphaMode]);materialCutoff.SetValueWithoutNotify(p.AlphaCutoff);materialCutoff.SetEnabled(p.AlphaMode==MaterialAlphaMode.Cutout);
            semanticTextureInfo.text=SemanticTextureSummary(p.Textures);
        }

        static string SemanticTextureSummary(MaterialTextureSet textures)
        {
            if (textures==null || textures.IsEmpty) return "semantic map: なし（normal / metallic-roughness）";
            var entries=new List<string>();
            if (textures.Normal!=null) entries.Add("normal: "+textures.Normal.MimeType+" / "+textures.Normal.EncodedByteCount.ToString("N0")+" bytes / UV"+textures.Normal.TexCoord+" / scale "+textures.Normal.NormalScale.ToString("0.###"));
            if (textures.MetallicRoughness!=null) entries.Add("metallic-roughness: "+textures.MetallicRoughness.MimeType+" / "+textures.MetallicRoughness.EncodedByteCount.ToString("N0")+" bytes / UV"+textures.MetallicRoughness.TexCoord);
            return "保持中のsemantic map\n"+string.Join("\n",entries)+"\nnormal: tangent-space / MR: B=metallic, G=roughness";
        }
    }
}

