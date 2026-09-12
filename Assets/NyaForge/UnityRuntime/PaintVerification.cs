using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Paint;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    internal static class PaintVerification
    {
        internal static void Verify(string output, List<string> checks)
        {
            var blank = new PaintImage(256,256,new Rgba32(235,235,245));
            var image = PaintStroke.Apply(blank,new[]{new Vec2(.2f,.25f),new Vec2(.8f,.75f)},12,new Rgba32(245,70,145));
            image = PaintStroke.Apply(image,new[]{new Vec2(.2f,.75f),new Vec2(.8f,.25f)},12,new Rgba32(40,160,230,180));
            string dir = Path.Combine(output,"paint-assets");
            string hash = PaintImageStore.Write(dir,image); var reopened = PaintImageStore.Read(dir,hash);
            if (!image.CopyRgba().SequenceEqual(reopened.CopyRgba())) throw new InvalidOperationException("Paint Player blob roundtrip failed");
            var texture = PaintTextureAdapter.Create(reopened);
            var decoded = new Texture2D(2,2,TextureFormat.RGBA32,false,false);
            try
            {
                byte[] png = PaintPng.Encode(reopened); File.WriteAllBytes(Path.Combine(output,"paint-core.png"),png);
                if (!decoded.LoadImage(png,false) || decoded.width != image.Width || decoded.height != image.Height)
                    throw new InvalidOperationException("Paint PNG decode dimensions differ");
                var pixels = decoded.GetPixels32();
                for(int y=0;y<image.Height;y++) for(int x=0;x<image.Width;x++)
                {
                    var expected=image.GetPixel(x,y);var actual=pixels[y*image.Width+x];
                    if(actual.r!=expected.R || actual.g!=expected.G || actual.b!=expected.B || actual.a!=expected.A)
                        throw new InvalidOperationException("Paint PNG changed pixels or row orientation");
                }
            }
            finally { UnityEngine.Object.Destroy(texture); UnityEngine.Object.Destroy(decoded); }
            checks.Add("paint foundation: tile stroke image/blob roundtrip, sRGB texture adapter and exact PNG pixel roundtrip; no authoring GUI or project binding yet");
        }
    }
}
