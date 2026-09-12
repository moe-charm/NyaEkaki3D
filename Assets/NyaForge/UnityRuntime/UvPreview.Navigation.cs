using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    sealed partial class UvPreview
    {
        readonly UvViewportTransform navigation=new UvViewportTransform();
        int panPointer=-1;
        Vector2 panLast;
        float ViewSize=>Mathf.Min(contentRect.width,contentRect.height)-24;
        internal Vector2 UvToLocal(Vector2 uv)=>navigation.ToLocal(uv,ViewSize);
        internal Vector2 LocalToUv(Vector2 local)=>navigation.ToUv(local,ViewSize);
        public void ResetView() { CancelIslandDrag();EndPan();navigation.Reset();MarkDirtyRepaint(); }

        void BuildNavigation()
        {
            style.overflow=Overflow.Hidden;
            RegisterCallback<WheelEvent>(e=>
            {
                if(ViewSize<=0) return;
                CancelIslandDrag();
                navigation.ZoomAt(this.WorldToLocal(e.mousePosition),ViewSize,e.delta.y);
                MarkDirtyRepaint();e.StopPropagation();
            });
            RegisterCallback<PointerDownEvent>(e=>
            {
                if(e.button!=1 && e.button!=2 || panPointer!=-1) return;
                CancelIslandDrag();
                panPointer=e.pointerId;panLast=e.position;this.CapturePointer(panPointer);e.StopPropagation();
            });
            RegisterCallback<PointerMoveEvent>(e=>
            {
                if(e.pointerId!=panPointer) return;
                Vector2 next=e.position;navigation.Translate(next-panLast);panLast=next;
                MarkDirtyRepaint();e.StopPropagation();
            });
            RegisterCallback<PointerUpEvent>(e=> { if(e.pointerId==panPointer && (e.button==1 || e.button==2)) { EndPan();e.StopPropagation(); } });
            RegisterCallback<PointerCaptureOutEvent>(e=> { if(e.pointerId==panPointer) panPointer=-1; });
            RegisterCallback<PointerCancelEvent>(e=> { if(e.pointerId==panPointer) EndPan(); });
            RegisterCallback<DetachFromPanelEvent>(_=>EndPan());
        }
        void EndPan()
        {
            int pointer=panPointer;panPointer=-1;
            if(pointer!=-1 && this.HasPointerCapture(pointer)) this.ReleasePointer(pointer);
        }
    }
}
