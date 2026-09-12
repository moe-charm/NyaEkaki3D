using System;
using System.IO;
using Newtonsoft.Json;
using NyaForge.Authoring.Paint;

namespace NyaForge.Authoring
{
    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class PaintPngManifest
    {
        [JsonProperty(Required = Required.Always)] public int SchemaVersion = 1;
        [JsonProperty(Required = Required.Always)] public string Profile = "base-color-rgba8-srgb-straight-v1";
        [JsonProperty(Required = Required.Always)] public string DocumentId;
        [JsonProperty(Required = Required.Always)] public long DocumentRevision;
        [JsonProperty(Required = Required.Always)] public string GraphId;
        [JsonProperty(Required = Required.Always)] public string PaintNodeId;
        [JsonProperty(Required = Required.Always)] public string ImageHash;
        [JsonProperty(Required = Required.Always)] public string UvHash;
        [JsonProperty(Required = Required.Always)] public string MeshDomain;
        [JsonProperty(Required = Required.Always)] public string PngHash;
        [JsonProperty(Required = Required.Always)] public int Width;
        [JsonProperty(Required = Required.Always)] public int Height;
    }

    public static class PaintPngExport
    {
        public const string ManifestName = "paint.nyaforge-image.json";
        /// <summary>Exports a resolved Paint node from a committed document, never an in-progress stroke.</summary>
        public static string Write(string directory, AuthoringWorkspace workspace, string paintNode)
        {
            if (workspace == null) throw new ArgumentNullException(nameof(workspace));
            directory = Storage.DirectoryPath(directory);
            lock (workspace.Gate)
            {
                Checks.Require(!workspace.Executing,"REENTRANT_EXPORT","Export requires a committed document.");
                var doc = workspace.Document;
                Checks.Require(!doc.IsEmpty && !doc.Objects[0].IsStaticProfile,"PAINT_NODE_REQUIRED","Select a graph Paint node.");
                var graph = doc.Objects[0].Graph;
                var evaluation = doc.Objects[0].EvaluateGraph();
                Checks.Require(paintNode != null && evaluation.ImageOutputs.TryGetValue(paintNode,out _),"PAINT_UNRESOLVED","Resolve the selected Paint image before exporting.");
                var value = evaluation.ImageOutputs[paintNode];
                byte[] png = PaintPng.Encode(value.Image);
                var manifest = new PaintPngManifest {
                    DocumentId = doc.DocumentId, DocumentRevision = doc.DocumentRevision, GraphId = graph.GraphId,
                    PaintNodeId = paintNode, ImageHash = value.ImageHash, UvHash = value.UvHash, MeshDomain = value.MeshDomain,
                    PngHash = Checks.Hash(png), Width = value.Image.Width, Height = value.Image.Height };
                byte[] json = Storage.JsonBytes(manifest);
                using (Storage.Lock(directory))
                {
                    string path = Path.Combine(directory,manifest.PngHash+".png");
                    if (File.Exists(path))
                        Checks.Require(Checks.Hash(Storage.ReadBounded(path,AuthoringLimits.MaxBlobBytes)) == manifest.PngHash,"HASH_MISMATCH","Existing PNG was modified.");
                    else Storage.AtomicWrite(path,png,false);
                    Storage.AtomicWrite(Path.Combine(directory,ManifestName),json,true);
                    return path;
                }
            }
        }
    }
}
