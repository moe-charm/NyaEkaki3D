using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring
{
    internal sealed class ProjectManifestV2
    {
        [JsonProperty(Required = Required.Always)] public int SchemaVersion;
        [JsonProperty(Required = Required.Always)] public string Profile, Units, Coordinates;
        [JsonProperty(Required = Required.Always)] public string DocumentId, Name, StateHash;
        [JsonProperty(Required = Required.Always)] public long DocumentRevision, SaveVersion;
        [JsonProperty(Required = Required.Always)] public StaticObjectManifest[] Objects;
    }
    internal sealed class StaticObjectManifest
    {
        [JsonProperty(Required = Required.Always)] public string ObjectId;
        [JsonProperty(Required = Required.Always)] public TransformDto Transform;
        [JsonProperty(Required = Required.Always)] public string BaselineHash, TopologyHash, DeltaHash, NormalPolicy;
        [JsonProperty(Required = Required.Always)] public bool LayerEnabled;
    }

    internal static class ProjectCodec
    {
        const string Profile = "static-project-v2";
        internal static ProjectManifestV2 Encode(AuthoringDocument doc, long saveVersion)
        {
            return new ProjectManifestV2
            {
                SchemaVersion = 2, Profile = Profile, Units = "meters", Coordinates = Storage.Coordinates,
                DocumentId = doc.DocumentId, Name = doc.Name, StateHash = doc.StateHash,
                DocumentRevision = doc.DocumentRevision, SaveVersion = saveVersion,
                Objects = doc.Objects.Select(item => new StaticObjectManifest
                {
                    ObjectId = item.ObjectId, Transform = TransformDto.From(item.Transform),
                    BaselineHash = item.BaselineMesh.ContentHash, TopologyHash = item.BaselineMesh.TopologyHash,
                    DeltaHash = Checks.Hash(DeltaBinary.Write(item.Offsets)), LayerEnabled = item.LayerEnabled,
                    NormalPolicy = "preserve"
                }).ToArray()
            };
        }
        internal static ProjectManifestV2 ReadManifest(JObject token)
        {
            var value = Storage.Decode<ProjectManifestV2>(token);
            Checks.Require(value.SchemaVersion == 2, "UNSUPPORTED_FORMAT", "Unsupported project version.");
            Checks.Require(value.Profile == Profile && value.Units == "meters" && value.Coordinates == Storage.Coordinates,
                "EXPORT_UNSUPPORTED_FEATURE", "Unsupported project profile.");
            Checks.Id(value.DocumentId); Checks.Name(value.Name); Checks.HashText(value.StateHash);
            Checks.Require(value.DocumentRevision >= 0 && value.DocumentRevision < long.MaxValue && value.SaveVersion > 0 && value.SaveVersion < long.MaxValue,
                "INVALID_REVISION", "Invalid saved revision.");
            Checks.Require(value.Objects.Length <= 1, "OBJECT_LIMIT", "This profile currently supports zero or one static object.");
            foreach (var item in value.Objects)
            {
                Checks.Id(item.ObjectId); item.Transform.Value();
                Checks.HashText(item.BaselineHash); Checks.HashText(item.TopologyHash); Checks.HashText(item.DeltaHash);
                Checks.Require(item.NormalPolicy == "preserve", "EXPORT_UNSUPPORTED_FEATURE", "Unsupported normal policy.");
            }
            return value;
        }
        internal static AuthoringDocument Decode(string directory, ProjectManifestV2 manifest)
        {
            AuthoringObject item = null;
            if (manifest.Objects.Length != 0)
            {
                var data = manifest.Objects[0];
                var mesh = MeshBinary.Read(Storage.ReadBlob(directory, data.BaselineHash));
                Checks.Require(mesh.ContentHash == data.BaselineHash && mesh.TopologyHash == data.TopologyHash, "HASH_MISMATCH", "Mesh identity differs.");
                item = new AuthoringObject(data.ObjectId, data.Transform.Value(), mesh, data.LayerEnabled,
                    DeltaBinary.Read(Storage.ReadBlob(directory, data.DeltaHash), mesh.VertexCount));
            }
            var doc = new AuthoringDocument(manifest.DocumentId, manifest.Name, manifest.DocumentRevision, item);
            Checks.Require(doc.StateHash == manifest.StateHash, "HASH_MISMATCH", "Project state hash differs.");
            doc.Evaluate();
            return doc;
        }
        internal static void WriteBlobs(string directory, AuthoringDocument doc)
        {
            foreach (var item in doc.Objects)
            {
                Storage.WriteBlob(directory, item.BaselineMesh.ContentHash, MeshBinary.Write(item.BaselineMesh));
                var delta = DeltaBinary.Write(item.Offsets);
                Storage.WriteBlob(directory, Checks.Hash(delta), delta);
            }
        }
    }
}
