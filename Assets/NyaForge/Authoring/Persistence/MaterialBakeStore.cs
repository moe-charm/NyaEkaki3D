using System;
using System.IO;
using Newtonsoft.Json;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;

namespace NyaForge.Authoring
{
    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class MaterialBakeManifest
    {
        [JsonProperty(Required=Required.Always)] public int SchemaVersion=1;
        [JsonProperty(Required=Required.Always)] public string Profile=MaterialBakeStore.Profile;
        [JsonProperty(Required=Required.Always)] public BakeManifest Mesh;
        [JsonProperty(Required=Required.Always)] public string MaterialHash;
        [JsonProperty(Required=Required.Always)] public BakeImagePayload[] BaseColor=Array.Empty<BakeImagePayload>();
    }
    public sealed class MaterialBakeDocument
    {
        public BakeDocument Geometry { get; }
        public MaterialParameters Material { get; }
        public PaintImage BaseColor { get; }
        public string UvHash { get; }
        public string MeshDomain { get; }
        public string PngHash { get; }
        readonly byte[] png;
        public byte[] CopyPng()=>png==null ? null : (byte[])png.Clone();
        internal MaterialBakeDocument(BakeDocument geometry,MaterialParameters material,BakeImagePayload descriptor,PaintImage image,byte[] bytes)
        { Geometry=geometry;Material=material;BaseColor=image;UvHash=descriptor?.UvHash;MeshDomain=descriptor?.MeshDomain;PngHash=descriptor?.PngHash;png=bytes; }
    }
    /// <summary>Static geometry, one explicit standard-PBR material for all slots, optional bound base-color image.</summary>
    public static class MaterialBakeStore
    {
        public const string Profile="static-standard-pbr-linear-straight-fade-all-slots-v1";
        public const string ManifestName="material.nyaforge-bake.json";
        public static string Export(string directory,AuthoringWorkspace workspace)
        {
            if(workspace==null) throw new ArgumentNullException(nameof(workspace));directory=Storage.DirectoryPath(directory);
            lock(workspace.Gate)
            {
                Checks.Require(!workspace.Executing,"REENTRANT_EXPORT","Export requires a committed document.");
                var source=BakeSource.CaptureMaterial(workspace.Document);
                Checks.Require(source.Material!=null,"MATERIAL_REQUIRED","Assign an explicit material before material export.");
                var descriptor=source.BaseColor==null ? null : BakeImagePayload.Describe(source.BaseColor);
                var manifest=new MaterialBakeManifest { Mesh=BakeStore.CreateManifest(workspace.Document,source),MaterialHash=source.Material.ContentHash,BaseColor=descriptor==null ? Array.Empty<BakeImagePayload>() : new[]{descriptor} };
                var bytes=Storage.JsonBytes(manifest);
                using(Storage.Lock(directory))
                {
                    Storage.WriteBlob(directory,source.Mesh.ContentHash,MeshBinary.Write(source.Mesh));
                    Storage.WriteBlob(directory,manifest.MaterialHash,MaterialParametersCodec.Write(source.Material));
                    descriptor?.Write(directory,source.BaseColor.Image);
                    string path=Path.Combine(directory,ManifestName);BakeOutputIdentity.Publish(path,bytes,workspace.Document);return path;
                }
            }
        }
        public static MaterialBakeDocument Read(string manifestPath)
        {
            manifestPath=Path.GetFullPath(manifestPath);var manifest=Storage.ReadJson<MaterialBakeManifest>(manifestPath);
            Checks.Require(manifest.SchemaVersion==1,"UNSUPPORTED_FORMAT","Unsupported material Bake version.");
            Checks.Require(manifest.Profile==Profile,"EXPORT_UNSUPPORTED_FEATURE","Unsupported material Bake profile.");
            Checks.Require(manifest.BaseColor.Length<=1,"EXPORT_UNSUPPORTED_FEATURE","This profile supports at most one base-color image.");
            Checks.HashText(manifest.MaterialHash);string directory=Path.GetDirectoryName(manifestPath);
            var geometry=BakeStore.ReadManifest(manifest.Mesh,directory);
            var parameters=MaterialParametersCodec.Read(Storage.ReadBlob(directory,manifest.MaterialHash));
            Checks.Require(parameters.ContentHash==manifest.MaterialHash,"HASH_MISMATCH","Material parameters differ from canonical identity.");
            if(manifest.BaseColor.Length==0) return new MaterialBakeDocument(geometry,parameters,null,null,null);
            var image=manifest.BaseColor[0].Read(directory,geometry.Mesh);
            return new MaterialBakeDocument(geometry,parameters,manifest.BaseColor[0],image.image,image.png);
        }
    }
}
