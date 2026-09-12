using System.IO;
using Newtonsoft.Json;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;

namespace NyaForge.Authoring
{
    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class BakeImagePayload
    {
        [JsonProperty(Required=Required.Always)] public string ImageHash;
        [JsonProperty(Required=Required.Always)] public string PngHash;
        [JsonProperty(Required=Required.Always)] public string UvHash;
        [JsonProperty(Required=Required.Always)] public string MeshDomain;
        [JsonProperty(Required=Required.Always)] public int Width;
        [JsonProperty(Required=Required.Always)] public int Height;
        internal static BakeImagePayload Describe(GraphImageValue image)=>new BakeImagePayload {
            ImageHash=image.ImageHash,PngHash=Checks.Hash(PaintPng.Encode(image.Image)),UvHash=image.UvHash,MeshDomain=image.MeshDomain,
            Width=image.Image.Width,Height=image.Image.Height };
        // Called inside the export directory lock. Only the final manifest publishes the complete set.
        internal void Write(string directory,PaintImage image)
        {
            var encoded=PaintImageCodec.Write(image);var png=PaintPng.Encode(image);
            Checks.Require(Checks.Hash(encoded)==ImageHash && Checks.Hash(png)==PngHash,"HASH_MISMATCH","Export image changed before writing.");
            Storage.WriteBlob(directory,ImageHash,encoded);
            string path=Path.Combine(directory,PngHash+".png");
            if(File.Exists(path)) Checks.Require(Checks.Hash(Storage.ReadBounded(path,AuthoringLimits.MaxBlobBytes))==PngHash,"HASH_MISMATCH","Existing PNG was modified.");
            else Storage.AtomicWrite(path,png,false);
        }
        internal (PaintImage image,byte[] png) Read(string directory,MeshData mesh)
        {
            Checks.HashText(ImageHash);Checks.HashText(PngHash);Checks.HashText(UvHash);Checks.HashText(MeshDomain);
            PaintImage.ValidateDimensions(Width,Height);
            Checks.Require(mesh.Uv0.Count==mesh.VertexCount,"UV_MISSING","Textured mesh requires UV0.");
            var image=PaintImageCodec.Read(Storage.ReadBlob(directory,ImageHash));
            Checks.Require(image.Width==Width && image.Height==Height,"HASH_MISMATCH","Image dimensions differ from manifest.");
            var png=Storage.ReadBounded(Path.Combine(directory,PngHash+".png"),AuthoringLimits.MaxBlobBytes);
            Checks.Require(Checks.Hash(png)==PngHash && Checks.Hash(PaintPng.Encode(image))==PngHash,"HASH_MISMATCH","PNG differs from retained image pixels or encoding profile.");
            return (image,png);
        }
    }
}
