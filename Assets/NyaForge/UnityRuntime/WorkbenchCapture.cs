using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    // UI verification capture, separate from future model-only evidence snapshots.
    internal static class WorkbenchCapture
    {
        internal static IEnumerator Write(UIDocument document, Camera camera, string path, Action<string> completed)
        {
            var settings = document.panelSettings;
            var previousTarget = settings.targetTexture;
            var capture = new RenderTexture(Screen.width, Screen.height, 24);
            capture.Create(); settings.targetTexture = capture;
            camera.Render();
            // UI layout may resize the viewport target after changing the panel target.
            // Render the current target again after each layout frame, not only before it.
            for (int i = 0; i < 8; i++) { yield return null; camera.Render(); }
            yield return new WaitForEndOfFrame();
            string failure = null;
            Texture2D texture = null;
            var previousActive = RenderTexture.active;
            try
            {
                texture = new Texture2D(capture.width, capture.height, TextureFormat.RGBA32, false);
                RenderTexture.active = capture;
                texture.ReadPixels(new Rect(0, 0, capture.width, capture.height), 0, 0); texture.Apply();
                var colors = new HashSet<Color32>(); var pixels = texture.GetPixels32();
                for (int i = 0; i < pixels.Length; i += 97) colors.Add(pixels[i]);
                if (colors.Count <= 8) throw new InvalidOperationException("No rendered UI content in capture");
                var bytes = texture.EncodeToPNG();
                if (bytes.Length <= 5000) throw new InvalidOperationException("Capture is empty");
                File.WriteAllBytes(path, bytes);
            }
            catch (Exception e) { failure = e.ToString(); }
            finally
            {
                RenderTexture.active = previousActive; settings.targetTexture = previousTarget;
                if (texture) UnityEngine.Object.Destroy(texture);
                capture.Release(); UnityEngine.Object.Destroy(capture);
            }
            completed(failure);
        }
    }
}
