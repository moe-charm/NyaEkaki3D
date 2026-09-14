using System;
using System.Linq;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        /// <summary>
        /// Frames the camera around the currently displayed mesh and attached
        /// objects. Projection ownership remains with the feature panels; this
        /// partial only translates their framing points into camera state.
        /// </summary>
        void Frame()
        {
            var points = projection.FramingPoints
                .Concat(objectProjection?.FramingPoints ?? Enumerable.Empty<Vector3>())
                .ToArray();
            if (points.Length == 0)
            {
                target = Vector3.zero;
                distance = .5f;
                orbit = Quaternion.Euler(0, 180, 0);
                UpdateCamera();
                return;
            }

            var bounds = new Bounds(points[0], Vector3.zero);
            foreach (var point in points) bounds.Encapsulate(point);
            target = bounds.center;
            distance = Mathf.Max(.1f, bounds.size.magnitude * 2.4f);
            orbit = Quaternion.Euler(0, 180, 0);
            UpdateCamera();
        }

        /// <summary>
        /// Recreates the preview render target when the UI viewport changes,
        /// then applies the orbit pose and lets surface preparation consume it.
        /// </summary>
        void UpdateCamera()
        {
            ClearCutPathHover();
            if (!camera || view?.panel == null) return;
            CancelSurfaceStroke();

            var panel = root.panel.visualTree;
            float sx = Screen.width / Math.Max(1, panel.resolvedStyle.width);
            float sy = Screen.height / Math.Max(1, panel.resolvedStyle.height);
            var rect = view.worldBound;
            int width = Mathf.Clamp(Mathf.RoundToInt(rect.width * sx), 1, 4096);
            int height = Mathf.Clamp(Mathf.RoundToInt(rect.height * sy), 1, 4096);
            if (!previewTexture || previewTexture.width != width || previewTexture.height != height)
            {
                camera.targetTexture = null;
                if (previewTexture)
                {
                    previewTexture.Release();
                    Destroy(previewTexture);
                }
                previewTexture = new RenderTexture(width, height, 24) { name = "NyaForge authoring viewport" };
                previewTexture.Create();
                camera.targetTexture = previewTexture;
                previewImage.image = previewTexture;
            }

            camera.transform.position = target + orbit * new Vector3(0, 0, -distance);
            camera.transform.rotation = orbit;
            RefreshSurfacePreparation();
        }
    }
}
