using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    static class UvNavigationVerification
    {
        internal static void Verify(UvPreview preview)
        {
            void Require(bool value,string message) { if(!value) throw new InvalidOperationException(message); }
            preview.ResetView();
            var anchor=new Vector2(.4f,.6f);var local=preview.UvToLocal(anchor);var world=preview.LocalToWorld(local);
            float initialDistance=Vector2.Distance(preview.UvToLocal(Vector2.zero),preview.UvToLocal(Vector2.one));
            using(var wheel=WheelEvent.GetPooled(new Event { type=EventType.ScrollWheel,mousePosition=world,delta=new Vector2(0,-4) })) preview.SendEvent(wheel);
            Require(Vector2.Distance(preview.LocalToUv(local),anchor)<.00001f,"UV wheel moved its cursor anchor");
            Require(Vector2.Distance(preview.UvToLocal(Vector2.zero),preview.UvToLocal(Vector2.one))>initialDistance,"UV wheel did not zoom");
            Vector2 before=preview.UvToLocal(anchor),delta=new Vector2(13,9);
            PointerProbe.Down(preview,world,1);PointerProbe.Move(preview,world+delta,1);PointerProbe.Up(preview,world+delta,1);
            Require(Vector2.Distance(preview.UvToLocal(anchor),before+delta)<.0001f,"UV right drag did not pan");
            Require(Vector2.Distance(preview.LocalToUv(preview.UvToLocal(anchor)),anchor)<.00001f,"UV drawing and picking mappings disagree");
            PointerProbe.ClickAt(preview,preview.LocalToWorld(preview.UvToLocal(new Vector2(.5f,.5f))));
            preview.ResetView();
            Require(Vector2.Distance(preview.UvToLocal(Vector2.zero),preview.UvToLocal(Vector2.one))-initialDistance<.0001f,"UV reset did not restore scale");
        }
    }
}
