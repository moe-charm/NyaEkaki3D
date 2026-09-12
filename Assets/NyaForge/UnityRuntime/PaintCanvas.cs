using System;
using System.Collections.Generic;
using NyaForge.Authoring;
using NyaForge.Authoring.Paint;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    /// <summary>Owns only an uncommitted 2D stroke preview. The workbench commits one document command.</summary>
    sealed class PaintCanvas : VisualElement, IDisposable
    {
        readonly Image display;
        readonly IVisualElementScheduledItem captureWatch;
        readonly List<Vec2> points = new List<Vec2>();
        PaintImage committed;
        Texture2D texture;
        string binding;
        int pointer;
        bool drawing;
        float strokeRadius;
        Rgba32 strokeColor;
        Func<PaintImage,IEnumerable<Vec2>,float,Rgba32,PaintImage> strokePreview;
        public Func<PaintImage,IEnumerable<Vec2>,float,Rgba32,PaintImage> PreviewOperation { get; set; } = PaintStroke.Apply;
        public float Radius { get; set; } = 8;
        public Rgba32 Color { get; set; } = new Rgba32(245,70,145);
        public bool IsDrawing => drawing;
        public event Action Started;
        public event Action<Vec2[],float,Rgba32> Completed;
        public event Action<string> Failed;

        public PaintCanvas()
        {
            name = "paint-canvas"; style.height = 256; style.flexShrink = 0;
            focusable = true;
            display = new Image { pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.StretchToFill };
            display.style.width = Length.Percent(100); display.style.height = Length.Percent(100); Add(display);
            // A release may update capture ownership before UI Toolkit dispatches CaptureOut.
            // Observe ownership while drawing so an interrupted gesture cannot remain pending.
            captureWatch = schedule.Execute(() =>
            {
                if (drawing && !this.HasPointerCapture(pointer)) CancelStroke();
            }).Every(16);
            captureWatch.Pause();
            RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0 || committed == null || drawing) return;
                try
                {
                    Started?.Invoke(); points.Clear(); strokeRadius = Radius; strokeColor = Color;
                    strokePreview=PreviewOperation;
                    pointer = e.pointerId; drawing = true; Focus(); this.CapturePointer(pointer);
                    captureWatch.Resume();
                    Append(e.position); Preview(); e.StopPropagation();
                }
                catch (Exception error) { CancelStroke(); Failed?.Invoke(error.Message); }
            });
            RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!drawing || e.pointerId != pointer) return;
                try { Append(e.position); Preview(); e.StopPropagation(); }
                catch (Exception error) { CancelStroke(); Failed?.Invoke(error.Message); }
            });
            RegisterCallback<PointerUpEvent>(e =>
            {
                if (!drawing || e.pointerId != pointer || e.button != 0) return;
                try
                {
                    Append(e.position); var stroke = points.ToArray();
                    drawing = false; captureWatch.Pause(); this.ReleasePointer(pointer); points.Clear();
                    Completed?.Invoke(stroke,strokeRadius,strokeColor);
                    Show(committed); e.StopPropagation();
                }
                catch (Exception error) { CancelStroke(); Failed?.Invoke(error.Message); }
            });
            RegisterCallback<PointerCaptureOutEvent>(_ => { if (drawing) CancelStroke(); });
            RegisterCallback<PointerCancelEvent>(e => { if (drawing && e.pointerId == pointer) CancelStroke(); });
            RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Escape) { CancelStroke(); e.StopPropagation(); } });
            RegisterCallback<DetachFromPanelEvent>(_ => CancelStroke());
        }

        public void Bind(PaintImage image,string key)
        {
            if (binding == key) return;
            CancelStroke(); binding = key; committed = image; Show(image);
        }
        void Append(Vector2 panelPoint)
        {
            var local = this.WorldToLocal(panelPoint);
            var point = new Vec2(Mathf.Clamp01(local.x/contentRect.width),Mathf.Clamp01(1-local.y/contentRect.height));
            if (points.Count > 0)
            {
                var previous = points[points.Count-1];
                if (Mathf.Abs(previous.X-point.X)<.0001f && Mathf.Abs(previous.Y-point.Y)<.0001f) return;
            }
            if (points.Count >= PaintStroke.MaxPoints) throw new InvalidOperationException("ストロークが長すぎます。短い線に分けてください。");
            points.Add(point);
        }
        void Preview() => Show(strokePreview(committed,points,strokeRadius,strokeColor));
        void Show(PaintImage image)
        {
            var next = image == null ? null : PaintTextureAdapter.Create(image);
            display.image = next;
            if (texture != null) UnityEngine.Object.Destroy(texture);
            texture = next;
        }
        public void CancelStroke()
        {
            if (!drawing) return;
            drawing = false; captureWatch.Pause(); this.ReleasePointer(pointer); points.Clear(); Show(committed);
        }
        public void Dispose()
        {
            CancelStroke(); captureWatch.Pause(); display.image = null;
            if (texture != null) UnityEngine.Object.Destroy(texture);
            texture = null; committed = null;
        }
    }
}
