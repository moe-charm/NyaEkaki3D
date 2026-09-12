using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace NyaForge.Authoring
{
    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class TransformDto
    {
        [JsonProperty(Required = Required.Always)] public float Scale;
        [JsonProperty(Required = Required.Always)] public float TranslationX;
        [JsonProperty(Required = Required.Always)] public float TranslationY;
        [JsonProperty(Required = Required.Always)] public float TranslationZ;
        internal RestTransform Value() { return new RestTransform(Scale,new Vec3(TranslationX,TranslationY,TranslationZ)); }
        internal static TransformDto From(RestTransform value) { return new TransformDto { Scale = value.Scale, TranslationX = value.Translation.X, TranslationY = value.Translation.Y, TranslationZ = value.Translation.Z }; }
    }
    [JsonObject(MemberSerialization.OptIn)]
    // Read-only legacy DTO: fields are assigned by the strict JSON decoder.
#pragma warning disable CS0649
    internal sealed class ProjectManifest
    {
        [JsonProperty(Required = Required.Always)] public int SchemaVersion;
        [JsonProperty(Required = Required.Always)] public string Profile;
        [JsonProperty(Required = Required.Always)] public string Units;
        [JsonProperty(Required = Required.Always)] public string Coordinates;
        [JsonProperty(Required = Required.Always)] public string DocumentId;
        [JsonProperty(Required = Required.Always)] public string ObjectId;
        [JsonProperty(Required = Required.Always)] public string Name;
        [JsonProperty(Required = Required.Always)] public long DocumentRevision;
        [JsonProperty(Required = Required.Always)] public long SaveVersion;
        [JsonProperty(Required = Required.Always)] public TransformDto Transform;
        [JsonProperty(Required = Required.Always)] public string BaselineHash;
        [JsonProperty(Required = Required.Always)] public string TopologyHash;
        [JsonProperty(Required = Required.Always)] public string DeltaHash;
        [JsonProperty(Required = Required.Always)] public string StateHash;
        [JsonProperty(Required = Required.Always)] public bool LayerEnabled;
        [JsonProperty(Required = Required.Always)] public string NormalPolicy;
    }
#pragma warning restore CS0649
    internal static class LegacyProjectCodec
    {
        internal static AuthoringWorkspace Read(string directory, JObject token)
        {
            directory = Storage.DirectoryPath(directory); var manifest = Storage.Decode<ProjectManifest>(token); Validate(manifest);
            var baseline = MeshBinary.Read(Storage.ReadBlob(directory,manifest.BaselineHash));
            Checks.Require(baseline.ContentHash == manifest.BaselineHash && baseline.TopologyHash == manifest.TopologyHash,"HASH_MISMATCH","Baseline mesh identity mismatch.");
            var offsets = DeltaBinary.Read(Storage.ReadBlob(directory,manifest.DeltaHash),baseline.VertexCount);
            var doc = new AuthoringDocument(manifest.DocumentId,manifest.ObjectId,manifest.Name,manifest.DocumentRevision,manifest.Transform.Value(),baseline,manifest.LayerEnabled,offsets);
            Checks.Require(doc.StateHash == manifest.StateHash,"HASH_MISMATCH","Document state does not match its recorded hash."); doc.Evaluate();
            // History and request identities intentionally begin fresh after reopen.
            return new AuthoringWorkspace(doc) { SaveVersion = manifest.SaveVersion, SavedDirectory = directory, SavedStateHash = doc.StateHash };
        }
        private static void Validate(ProjectManifest value)
        {
            Checks.Require(value.SchemaVersion == 1,"UNSUPPORTED_FORMAT","Unsupported native project version.");
            Storage.ValidateProfile(value.Profile,value.Units,value.Coordinates,value.NormalPolicy);
            Checks.Id(value.DocumentId); Checks.Id(value.ObjectId); Checks.Name(value.Name); value.Transform.Value();
            Checks.Require(value.SaveVersion > 0 && value.SaveVersion < long.MaxValue && value.DocumentRevision >= 0 && value.DocumentRevision < long.MaxValue,"INVALID_REVISION","Invalid saved revision.");
            Checks.HashText(value.BaselineHash); Checks.HashText(value.TopologyHash); Checks.HashText(value.DeltaHash); Checks.HashText(value.StateHash);
        }
    }
}
