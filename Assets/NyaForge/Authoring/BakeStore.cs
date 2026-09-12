using System;
using System.IO;
using Newtonsoft.Json;

namespace NyaForge.Authoring
{
    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class BakeManifest
    {
        [JsonProperty(Required = Required.Always)] public int SchemaVersion;
        [JsonProperty(Required = Required.Always)] public string Profile;
        [JsonProperty(Required = Required.Always)] public string Units;
        [JsonProperty(Required = Required.Always)] public string Coordinates;
        [JsonProperty(Required = Required.Always)] public string DocumentId;
        [JsonProperty(Required = Required.Always)] public long DocumentRevision;
        [JsonProperty(Required = Required.Always)] public string ObjectId;
        [JsonProperty(Required = Required.Always)] public string Name;
        [JsonProperty(Required = Required.Always)] public TransformDto Transform;
        [JsonProperty(Required = Required.Always)] public string MeshContentHash;
        [JsonProperty(Required = Required.Always)] public string TopologyHash;
        [JsonProperty(Required = Required.Always)] public string BaselineHash;
        [JsonProperty(Required = Required.Always)] public int VertexCount;
        [JsonProperty(Required = Required.Always)] public int TriangleCount;
        [JsonProperty(Required = Required.Always)] public string NormalPolicy;
        [JsonProperty(Required = Required.Always)] public bool NormalsPreservedAfterPositionEdit;
        [JsonProperty(Required = Required.Always)] public bool HasSkin;
        [JsonProperty(Required = Required.Always)] public bool HasBlendShapes;
    }

    public sealed class BakeDocument
    {
        public int SchemaVersion { get { return 1; } }
        public string DocumentId { get; private set; }
        public long DocumentRevision { get; private set; }
        public string ObjectId { get; private set; }
        public string Name { get; private set; }
        public MeshData Mesh { get; private set; }
        public RestTransform Transform { get; private set; }
        public string MeshContentHash { get { return Mesh.ContentHash; } }
        public string BaselineHash { get; private set; }
        public bool NormalsPreservedAfterPositionEdit { get; private set; }
        internal BakeDocument(BakeManifest manifest, MeshData mesh) { DocumentId = manifest.DocumentId; DocumentRevision = manifest.DocumentRevision; ObjectId = manifest.ObjectId; Name = manifest.Name; Mesh = mesh; Transform = manifest.Transform.Value(); BaselineHash = manifest.BaselineHash; NormalsPreservedAfterPositionEdit = manifest.NormalsPreservedAfterPositionEdit; }
    }

    public static class BakeStore
    {
        public const string ManifestName = "mesh.nyaforge-bake.json";
        public static string Export(string directory, AuthoringWorkspace workspace)
        {
            if (workspace == null) throw new ArgumentNullException("workspace"); directory = Storage.DirectoryPath(directory);
            lock (workspace.Gate)
            {
                Checks.Require(!workspace.Executing,"REENTRANT_EXPORT","Export requires a committed document.");
                Checks.Require(!workspace.Document.IsEmpty, "NO_EXPORTABLE_OBJECT", "Add a mesh before exporting.");
                var doc = workspace.Document; var source = BakeSource.Capture(doc); var mesh = source.Mesh;
                using (Storage.Lock(directory))
                {
                    var manifest = CreateManifest(doc,source);
                    var bytes = Storage.JsonBytes(manifest); Storage.WriteBlob(directory,mesh.ContentHash,MeshBinary.Write(mesh));
                    string path = Path.Combine(directory,ManifestName); BakeOutputIdentity.Publish(path,bytes,doc); return path;
                }
            }
        }
        internal static BakeManifest CreateManifest(AuthoringDocument doc, BakeSource source)
        {
            var mesh = source.Mesh;
            return new BakeManifest { SchemaVersion = 1, Profile = Storage.Profile, Units = "meters", Coordinates = Storage.Coordinates, DocumentId = doc.DocumentId, DocumentRevision = doc.DocumentRevision, ObjectId = doc.ObjectId, Name = doc.Name, Transform = TransformDto.From(source.Transform), MeshContentHash = mesh.ContentHash, TopologyHash = mesh.TopologyHash, BaselineHash = source.BaselineHash, VertexCount = mesh.VertexCount, TriangleCount = mesh.TriangleCount, NormalPolicy = "preserve", NormalsPreservedAfterPositionEdit = source.PositionsEdited, HasSkin = false, HasBlendShapes = false };
        }
        public static BakeDocument Read(string manifestPath)
        {
            manifestPath = Path.GetFullPath(manifestPath); var manifest = Storage.ReadJson<BakeManifest>(manifestPath);
            return ReadManifest(manifest,Path.GetDirectoryName(manifestPath));
        }
        internal static BakeDocument ReadManifest(BakeManifest manifest,string directory)
        {
            Checks.Require(manifest.SchemaVersion == 1,"UNSUPPORTED_FORMAT","Unsupported Bake format version.");
            Storage.ValidateProfile(manifest.Profile,manifest.Units,manifest.Coordinates,manifest.NormalPolicy);
            Checks.Require(!manifest.HasSkin && !manifest.HasBlendShapes,"EXPORT_UNSUPPORTED_FEATURE","This Bake reader cannot import skin or morphs.");
            Checks.Id(manifest.DocumentId); Checks.Id(manifest.ObjectId); Checks.Name(manifest.Name); manifest.Transform.Value(); Checks.HashText(manifest.MeshContentHash); Checks.HashText(manifest.BaselineHash); Checks.HashText(manifest.TopologyHash);
            Checks.Require(manifest.DocumentRevision >= 0 && manifest.DocumentRevision < long.MaxValue,"INVALID_REVISION","Invalid document revision.");
            Checks.Require(manifest.VertexCount >= 3 && manifest.VertexCount <= AuthoringLimits.MaxVertices && manifest.TriangleCount > 0 && manifest.TriangleCount <= AuthoringLimits.MaxIndices / 3,"BUDGET_EXCEEDED","Bake counts exceed budget.");
            var mesh = MeshBinary.Read(Storage.ReadBlob(directory,manifest.MeshContentHash));
            Checks.Require(mesh.ContentHash == manifest.MeshContentHash && mesh.TopologyHash == manifest.TopologyHash && mesh.VertexCount == manifest.VertexCount && mesh.TriangleCount == manifest.TriangleCount,"HASH_MISMATCH","Bake metadata does not match its mesh.");
            foreach (var position in mesh.Positions) Checks.Finite(manifest.Transform.Value().ToAvatarPoint(position));
            return new BakeDocument(manifest,mesh);
        }
    }
}

