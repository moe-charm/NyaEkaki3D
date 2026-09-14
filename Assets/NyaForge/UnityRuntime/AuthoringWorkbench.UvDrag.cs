using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Topology;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Action<Vector2> BeginUvIslandDrag()
        {
            if(activeEditContext==null || selectedFaces.Count==0) return null;
            var expanded=UvIslands.Expand(DisplayedGraphValue().Polygon,selectedFaces);
            if(!selectedFaces.SetEquals(expanded)) { selectedFaces.Clear();selectedFaces.UnionWith(expanded);selectionContext.NotifyChanged();Refresh(); }
            var context=activeEditContext;var faces=selectedFaces.OrderBy(id=>id).ToArray();
            // Capture the revision now, not at release: edits or project replacement invalidate this gesture.
            var envelope=workspace.NewCommand();
            return delta=>Try(()=>
            {
                envelope.Operations=new[]{AuthoringOperation.TransformUvIslands(context,faces,new UvTransformSettings(new Vec2(delta.x,delta.y),0,1))};
                if (ReferenceProtectionBlocks(envelope.Operations)) throw new InvalidOperationException(ReferenceProtectionMessage);
                var result=commands.Execute(envelope,projection);
                Refresh();
                if(!result.Success) throw new InvalidOperationException(result.Code+": "+result.Message);
                SetStatus("UV島を移動しました。Undoで戻せます。");
            });
        }
    }
}
