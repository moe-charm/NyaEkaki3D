using NyaForge.Authoring.Paint;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    /// <summary>Creates an owned sRGB texture from the immutable paint image; caller destroys it.</summary>
    public static class PaintTextureAdapter
    {
        public static Texture2D Create(PaintImage image)
        {
            var texture = new Texture2D(image.Width, image.Height, TextureFormat.RGBA32, false, false)
            { name = "NyaForge base color", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            try { texture.LoadRawTextureData(image.CopyRgba()); texture.Apply(false, false); return texture; }
            catch { Object.Destroy(texture); throw; }
        }
    }
}
