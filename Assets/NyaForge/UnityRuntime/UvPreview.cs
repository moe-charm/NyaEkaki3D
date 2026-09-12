using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Topology;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    /// <summary>UV corner display and face picking. Geometry changes are dispatched by the workbench.</summary>
    sealed partial class UvPreview : VisualElement
    {
        public const int FaceLimit = 2048;
        PolygonMesh mesh;
        readonly HashSet<ulong> selected = new HashSet<ulong>();
        public int VisibleFaceCount { get; private set; }
        public event Action<ulong?, bool> FacePicked;

        public UvPreview()
        {
            name = "uv-preview";
            style.height = 260;
            style.backgroundColor = new Color(.055f, .07f, .1f);
            BuildNavigation();
            BuildIslandDrag();
            generateVisualContent += Draw;
            RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0 || mesh == null || IsDragging || panPointer!=-1) return;
                float size = Mathf.Min(contentRect.width, contentRect.height) - 24;
                if (size <= 0) return;
                var local = this.WorldToLocal(e.position);
                var p = LocalToUv(local);
                ulong? hit = null;
                foreach (var face in mesh.Faces.Take(FaceLimit).Reverse())
                {
                    if (!IsVisible(face)) continue;
                    bool inside = false;
                    for (int i = 0, j = face.Corners.Count - 1; i < face.Corners.Count; j = i++)
                    {
                        var a = face.Corners[i].Uv0.Value; var b = face.Corners[j].Uv0.Value;
                        if ((a.Y > p.y) != (b.Y > p.y) && p.x < (b.X-a.X)*(p.y-a.Y)/(b.Y-a.Y)+a.X) inside = !inside;
                    }
                    if (inside) { hit = face.Id; break; }
                }
                if(e.shiftKey || !hit.HasValue || !selected.Contains(hit.Value)) FacePicked?.Invoke(hit,e.shiftKey);
                if(hit.HasValue && selected.Contains(hit.Value)) StartIslandDrag(e,local);
                e.StopPropagation();
            });
        }

        public void Bind(PolygonMesh value, IEnumerable<ulong> faceIds)
        {
            CancelIslandDrag();
            if(mesh?.DomainId!=value?.DomainId) ResetView();
            mesh = value; selected.Clear();
            foreach (ulong id in faceIds) selected.Add(id);
            VisibleFaceCount = mesh?.Faces.Take(FaceLimit).Count(IsVisible) ?? 0;
            MarkDirtyRepaint();
        }

        static bool IsVisible(CageFace face) => face.Corners.All(c => c.Uv0.HasValue);

        void Draw(MeshGenerationContext context)
        {
            float size = Mathf.Min(contentRect.width, contentRect.height) - 24;
            if (size <= 0) return;
            var painter = context.painter2D;
            Vector2 Point(float u, float v) => UvToLocal(new Vector2(u,v));
            painter.lineWidth = 1; painter.strokeColor = new Color(.25f, .3f, .36f);
            for (int i = 0; i <= 4; i++)
            {
                float t = i / 4f;
                painter.BeginPath(); painter.MoveTo(Point(t, 0)); painter.LineTo(Point(t, 1)); painter.Stroke();
                painter.BeginPath(); painter.MoveTo(Point(0, t)); painter.LineTo(Point(1, t)); painter.Stroke();
            }
            if (mesh == null) return;
            painter.lineWidth = 2;
            foreach (var face in mesh.Faces.Take(FaceLimit))
            {
                if (!IsVisible(face)) continue;
                painter.strokeColor = selected.Contains(face.Id) ? new Color(1, .59f, .16f) : new Color(.3f, .85f, .8f);
                painter.BeginPath();
                for (int i = 0; i < face.Corners.Count; i++)
                {
                    var uv = face.Corners[i].Uv0.Value;
                    var offset=selected.Contains(face.Id) ? DragOffset : Vector2.zero;
                    if (i == 0) painter.MoveTo(Point(uv.X+offset.x, uv.Y+offset.y)); else painter.LineTo(Point(uv.X+offset.x, uv.Y+offset.y));
                }
                painter.ClosePath(); painter.Stroke();
            }
        }
    }
}
