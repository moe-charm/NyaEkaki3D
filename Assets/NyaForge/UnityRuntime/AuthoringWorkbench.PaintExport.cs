using System;
using System.IO;
using NyaForge.Authoring;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Button exportPaintPng;
        void BuildPaintExport(VisualElement parent)
        {
            exportPaintPng = Button("確定画像をPNGで書き出す", () => Try(() =>
            {
                string directory = Path.Combine(Path.GetFullPath(projectPath.value),"exports","paint-"+Guid.NewGuid().ToString("N"));
                string path = PaintPngExport.Write(directory,workspace,selectedPaint);
                SetStatus("PNGを書き出しました: " + path);
            }), "export-paint-png");
            parent.Add(exportPaintPng);
        }
    }
}
