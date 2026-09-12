using System;
using NyaForge.Authoring.Paint;
using NyaForge.Authoring.Graph;
using NyaForge.Rendering;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    /// <summary>Owns the preview texture and material for a committed base-color image.</summary>
    sealed class BaseColorSurface : IDisposable
    {
        readonly Texture2D texture;
        Texture2D preview;
        public Material Material { get; }
        public bool HasPreview=>preview!=null;
        public BaseColorSurface(PaintImage image, Material template, MaterialParameters parameters=null)
        {
            texture = image==null ? null : PaintTextureAdapter.Create(image);
            try
            {
                Material=parameters==null ? new Material(template) { color=Color.white } : StandardMaterialAdapter.Create(parameters,true);
                Material.mainTexture=texture!=null ? texture : Texture2D.whiteTexture;
            }
            catch { UnityEngine.Object.Destroy(Material);UnityEngine.Object.Destroy(texture); throw; }
        }
        public void ShowPreview(PaintImage image)
        {
            var next=image==null ? null : PaintTextureAdapter.Create(image);
            SetPreview(next);
        }
        internal void SetPreview(Texture2D next)
        {
            Material.mainTexture=next!=null ? next : texture!=null ? texture : Texture2D.whiteTexture;
            if(preview!=null) UnityEngine.Object.Destroy(preview);preview=next;
        }
        public void Dispose() { UnityEngine.Object.Destroy(Material); UnityEngine.Object.Destroy(texture);if(preview!=null) UnityEngine.Object.Destroy(preview); }
    }
}
