using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        /// <summary>
        /// Owns the UI event wiring around the viewport. Camera math lives in
        /// Viewport.cs and picking/commands live in ViewportInput.cs; this
        /// adapter only translates UI Toolkit events into those operations.
        /// </summary>
        void BindViewportInteraction(Label hint)
        {
            view.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                UpdateCamera();
                // The viewport becomes narrow on a DPI-scaled 1080px window.
                // Keep the interaction hint and the empty-project callout
                // readable without overlapping each other.
                float width = view.layout.width, height = view.layout.height;
                if (width <= 0 || height <= 0) return;

                bool narrow = width < 180f;
                // Refresh owns the normal visibility decision. A narrow
                // viewport hides the callout so it cannot cover the surface.
                emptyHint.style.display = narrow
                    ? DisplayStyle.None
                    : DisplayedGraphValue()?.Mesh == null ? DisplayStyle.Flex : DisplayStyle.None;
                if (!narrow)
                {
                    emptyHint.style.fontSize = Mathf.Clamp(width / 18f, 14f, 22f);
                    emptyHint.style.top = Mathf.Clamp(hint.layout.height + 24f, 80f, Mathf.Max(80f, height - 80f));
                }
            });

            view.RegisterCallback<PointerDownEvent>(e =>
            {
                if (!active || e.button > 1) return;
                if (RigWeightPaintActive && e.button == 0)
                {
                    BeginRigWeightStroke(e.position);
                    view.CapturePointer(e.pointerId);
                    e.StopPropagation();
                    return;
                }
                if (CutPathPicking) view.Focus();
                orbiting = true;
                orbitButton = e.button;
                pointerStart = pointerLast = e.position;
                view.CapturePointer(e.pointerId);
                e.StopPropagation();
            });

            view.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (RigWeightPaintActive && rigPainting)
                {
                    UpdateRigWeightStroke(e.position);
                    e.StopPropagation();
                    return;
                }
                if (!orbiting) return;
                var delta = (Vector2)e.position - pointerLast;
                pointerLast = e.position;
                if (orbitButton == 0)
                    orbit = Quaternion.AngleAxis(delta.x * .3f, Vector3.up) * orbit * Quaternion.AngleAxis(delta.y * .3f, Vector3.right);
                else
                    target += orbit * new Vector3(-delta.x, delta.y, 0) * distance * .0012f;
                UpdateCamera();
            });

            view.RegisterCallback<PointerUpEvent>(e =>
            {
                if (RigWeightPaintActive && rigPainting && e.button == 0)
                {
                    EndRigWeightStroke();
                    view.ReleasePointer(e.pointerId);
                    e.StopPropagation();
                    return;
                }
                if (orbiting && orbitButton == 0 && Vector2.Distance(pointerStart, e.position) < 4)
                {
                    if (SurfaceTrianglePickingActive) PickAvatarSurfaceTriangle(e.position, e.shiftKey);
                    else if (CutPathPicking) PickCutPathPoint(e.position);
                    else PickVertex(e.position, e.shiftKey);
                }
                orbiting = false;
                view.ReleasePointer(e.pointerId);
            });

            view.RegisterCallback<PointerCaptureOutEvent>(_ =>
            {
                orbiting = false;
                if (rigPainting) EndRigWeightStroke();
            });

            view.RegisterCallback<WheelEvent>(e =>
            {
                distance = Mathf.Clamp(distance * Mathf.Exp(e.delta.y * .045f), .02f, 20f);
                UpdateCamera();
                e.StopPropagation();
            });
        }
    }
}
