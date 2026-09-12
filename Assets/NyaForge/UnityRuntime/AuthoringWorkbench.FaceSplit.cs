using System;
using System.Linq;
using NyaForge.Authoring;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Button splitFaceButton;
        void BuildFaceSplit(VisualElement parent)
        {
            splitFaceButton=Button("2頂点を結んで面を分割",()=>Try(()=>
            {
                long before=workspace.Document.DocumentRevision;
                Execute(AuthoringOperation.SplitPolygonFace(activeEditContext,SelectedPolygonVertices()));
                if(workspace.Document.DocumentRevision!=before) { selection.Clear();Refresh(); }
            }),"split-face");parent.Add(splitFaceButton);
            splitFaceButton.tooltip="点モードで同じ面の隣り合わない2頂点を選びます。平面を対角線で2面に分割します。";
        }
    }
}
