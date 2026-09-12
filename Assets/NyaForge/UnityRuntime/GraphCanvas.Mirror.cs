using System.Collections.Generic;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class GraphCanvas
    {
        void BuildMirrorParameters(VisualElement fields,GraphNode node)
        {
            var axis=new DropdownField("軸",new List<string>{"X","Y","Z"},node.MirrorAxis);fields.Add(axis);
            var plane=Field(fields,"対称面の位置 (m)",node.MirrorPlane);
            fields.Add(ActionButton("Mirrorを適用",()=>submit(new[]{AuthoringOperation.UpdateNode(GraphNode.Mirror(node.NodeId,axis.index,plane.value,node.Enabled))})));
            fields.Add(ActionButton(node.Enabled ? "処理を無効化" : "処理を有効化",()=>submit(new[]{AuthoringOperation.UpdateNode(GraphNode.Mirror(node.NodeId,node.MirrorAxis,node.MirrorPlane,!node.Enabled))})));
            var note=new Label("元形状＋反転コピー。中心の頂点は未結合。編集は手前のPolygonEditで行います。");note.style.whiteSpace=WhiteSpace.Normal;fields.Add(note);
        }
    }
}
