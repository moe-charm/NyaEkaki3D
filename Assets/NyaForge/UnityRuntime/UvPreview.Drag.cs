using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    sealed partial class UvPreview
    {
        public Func<Action<Vector2>> BeginIslandDrag { get; set; }
        int dragPointer=-1;
        Vector2 dragStartLocal,dragStartUv;
        Action<Vector2> commitDrag;
        internal Vector2 DragOffset { get; private set; }
        internal bool IsDragging=>dragPointer!=-1;

        void BuildIslandDrag()
        {
            focusable=true;
            RegisterCallback<PointerMoveEvent>(e=>
            {
                if(e.pointerId!=dragPointer) return;
                if(!this.HasPointerCapture(dragPointer)) { CancelIslandDrag();return; }
                var local=this.WorldToLocal(e.position);
                DragOffset=(local-dragStartLocal).sqrMagnitude<9 ? Vector2.zero : LocalToUv(local)-dragStartUv;
                MarkDirtyRepaint();e.StopPropagation();
            });
            RegisterCallback<PointerUpEvent>(e=>
            {
                if(e.pointerId!=dragPointer || e.button!=0) return;
                if(!this.HasPointerCapture(dragPointer)) { CancelIslandDrag();e.StopPropagation();return; }
                var local=this.WorldToLocal(e.position);
                var delta=(local-dragStartLocal).sqrMagnitude<9 ? Vector2.zero : LocalToUv(local)-dragStartUv;
                var commit=commitDrag;CancelIslandDrag();
                if(delta.sqrMagnitude>0) commit?.Invoke(delta);
                e.StopPropagation();
            });
            RegisterCallback<KeyDownEvent>(e=> { if(e.keyCode==KeyCode.Escape && IsDragging) { CancelIslandDrag();e.StopPropagation(); } });
            RegisterCallback<PointerCancelEvent>(e=> { if(e.pointerId==dragPointer) CancelIslandDrag(); });
            RegisterCallback<PointerCaptureOutEvent>(e=> { if(e.pointerId==dragPointer) CancelIslandDrag(); });
            RegisterCallback<BlurEvent>(_=>CancelIslandDrag());
            RegisterCallback<DetachFromPanelEvent>(_=>CancelIslandDrag());
            RegisterCallback<GeometryChangedEvent>(_=>CancelIslandDrag());
        }
        void StartIslandDrag(PointerDownEvent e,Vector2 local)
        {
            if(panPointer!=-1 || e.shiftKey) return;
            var commit=BeginIslandDrag?.Invoke();if(commit==null) return;
            Focus();commitDrag=commit;dragStartLocal=local;dragStartUv=LocalToUv(local);dragPointer=e.pointerId;
            DragOffset=Vector2.zero;this.CapturePointer(dragPointer);
        }
        public void CancelIslandDrag()
        {
            int pointer=dragPointer;dragPointer=-1;commitDrag=null;DragOffset=Vector2.zero;
            if(pointer!=-1 && this.HasPointerCapture(pointer)) this.ReleasePointer(pointer);
            MarkDirtyRepaint();
        }
    }
}
