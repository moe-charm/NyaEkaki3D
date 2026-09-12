using System;
using System.IO;
using Newtonsoft.Json;
using NyaForge.Authoring.Paint;

namespace NyaForge.Authoring
{
    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class SurfaceBakeManifest
    {
        [JsonProperty(Required = Required.Always)] public int SchemaVersion = 1;
        [JsonProperty(Required = Required.Always)] public string Profile = SurfaceBakeStore.Profile;
        [JsonProperty(Required = Required.Always)] public BakeManifest Mesh;
        [JsonProperty(Required = Required.Always)] public string Material = "opaque-basecolor-srgb-straight-all-slots-v1";
        [JsonProperty(Required = Required.Always)] public string ImageHash;
        [JsonProperty(Required = Required.Always)] public string PngHash;
        [JsonProperty(Required = Required.Always)] public string UvHash;
        [JsonProperty(Required = Required.Always)] public string MeshDomain;
        [JsonProperty(Required = Required.Always)] public int Width;
        [JsonProperty(Required = Required.Always)] public int Height;
    }

    public sealed class SurfaceBakeDocument
    {
        public BakeDocument Geometry { get; }
        public PaintImage BaseColor { get; }
        public string UvHash { get; }
        public string MeshDomain { get; }
        public string PngHash { get; }
        readonly byte[] png;
        public byte[] CopyPng() => (byte[])png.Clone();
        internal SurfaceBakeDocument(BakeDocument geometry,PaintImage image,SurfaceBakeManifest manifest,byte[] bytes)
        { Geometry=geometry; BaseColor=image; UvHash=manifest.UvHash; MeshDomain=manifest.MeshDomain; PngHash=manifest.PngHash; png=bytes; }
    }

    /// <summary>Static mesh plus one opaque base-color material assigned to all submeshes.
    /// Image alpha is retained, while the rendering profile remains explicitly opaque.</summary>
    public static class SurfaceBakeStore
    {
        public const string Profile = "static-surface-basecolor-v1";
        public const string ManifestName = "surface.nyaforge-bake.json";
        public static string Export(string directory,AuthoringWorkspace workspace)
        {
            if (workspace == null) throw new ArgumentNullException(nameof(workspace));
            directory = Storage.DirectoryPath(directory);
            lock (workspace.Gate)
            {
                Checks.Require(!workspace.Executing,"REENTRANT_EXPORT","Export requires a committed document.");
                var source = BakeSource.CaptureSurface(workspace.Document);
                Checks.Require(source.BaseColor != null,"PAINT_IMAGE_REQUIRED","The surface profile requires a resolved base-color image.");
                var image = source.BaseColor;
                var payload = BakeImagePayload.Describe(image);
                var manifest = new SurfaceBakeManifest { Mesh=BakeStore.CreateManifest(workspace.Document,source),
                    ImageHash=payload.ImageHash, PngHash=payload.PngHash, UvHash=image.UvHash, MeshDomain=image.MeshDomain,
                    Width=image.Image.Width, Height=image.Image.Height };
                var bytes = Storage.JsonBytes(manifest);
                using (Storage.Lock(directory))
                {
                    Storage.WriteBlob(directory,source.Mesh.ContentHash,MeshBinary.Write(source.Mesh));
                    payload.Write(directory,image.Image);
                    string path = Path.Combine(directory,ManifestName);
                    BakeOutputIdentity.Publish(path,bytes,workspace.Document); return path;
                }
            }
        }
        public static SurfaceBakeDocument Read(string manifestPath)
        {
            manifestPath = Path.GetFullPath(manifestPath);
            var manifest = Storage.ReadJson<SurfaceBakeManifest>(manifestPath);
            Checks.Require(manifest.SchemaVersion == 1,"UNSUPPORTED_FORMAT","Unsupported surface Bake version.");
            Checks.Require(manifest.Profile == Profile && manifest.Material == "opaque-basecolor-srgb-straight-all-slots-v1",
                "EXPORT_UNSUPPORTED_FEATURE","Unsupported surface material profile.");
            string directory = Path.GetDirectoryName(manifestPath);
            var mesh = BakeStore.ReadManifest(manifest.Mesh,directory);
            var payload=new BakeImagePayload { ImageHash=manifest.ImageHash,PngHash=manifest.PngHash,UvHash=manifest.UvHash,MeshDomain=manifest.MeshDomain,Width=manifest.Width,Height=manifest.Height };
            var decoded=payload.Read(directory,mesh.Mesh);
            return new SurfaceBakeDocument(mesh,decoded.image,manifest,decoded.png);
        }
    }
}

