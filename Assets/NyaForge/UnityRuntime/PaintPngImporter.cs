using System;
using System.IO;
using NyaForge.Authoring;
using NyaForge.Authoring.Paint;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    public static class PaintPngImporter
    {
        public static PaintImage Read(string path,string expectedHash=null)
        {
            byte[] bytes;
            using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))
            {
                if(stream.Length<45 || stream.Length>PaintPngInput.MaxFileBytes) throw new AuthoringException("INVALID_PNG","PNGは16 MiB以内で指定してください。");
                bytes=new byte[(int)stream.Length];int offset=0;
                while(offset<bytes.Length) { int n=stream.Read(bytes,offset,bytes.Length-offset);if(n==0) throw new EndOfStreamException();offset+=n; }
                if(stream.ReadByte()!=-1) throw new IOException("読み込み中にファイルの大きさが変わりました。");
            }
            if(expectedHash!=null) PaintPngInput.RequireSourceHash(bytes,expectedHash);
            return Decode(PaintPngInput.Read(bytes));
        }
        public static PaintImage Decode(PaintPngInput input)
        {
            if(input==null) throw new ArgumentNullException(nameof(input));
            Texture2D texture=null;
            try
            {
                texture=new Texture2D(2,2,TextureFormat.RGBA32,false,false);
                if(!texture.LoadImage(input.CopyBytes(),false) || texture.width!=input.Width || texture.height!=input.Height)
                    throw new AuthoringException("INVALID_PNG","PNG画像を読み込めませんでした。");
                var colors=texture.GetPixels32();var rgba=new byte[colors.Length*4];
                for(int i=0;i<colors.Length;i++) { var c=colors[i];int p=i*4;rgba[p]=c.r;rgba[p+1]=c.g;rgba[p+2]=c.b;rgba[p+3]=c.a; }
                return PaintImage.FromRgbaBottomLeft(input.Width,input.Height,rgba);
            }
            finally { if(texture!=null) UnityEngine.Object.Destroy(texture); }
        }
    }
}
