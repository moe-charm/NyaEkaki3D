using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring
{
    internal sealed class GraphProjectManifest
    {
        [JsonProperty(Required = Required.Always)] public int SchemaVersion;
        [JsonProperty(Required = Required.Always)] public string Profile, Units, Coordinates;
        [JsonProperty(Required = Required.Always)] public string DocumentId, Name, StateHash;
        [JsonProperty(Required = Required.Always)] public long DocumentRevision, SaveVersion;
        [JsonProperty(Required = Required.Always)] public GraphObjectManifest[] Objects;
    }
    internal sealed class GraphObjectManifest
    {
        [JsonProperty(Required = Required.Always)] public string ObjectId, GraphHash;
    }

    /// <summary>Native graph profile. Incomplete evaluation does not invalidate stored authoring data.</summary>
    internal static class GraphProjectCodec
    {
        internal static GraphProjectManifest Encode(AuthoringDocument doc, long version)
        {
            Checks.Require(doc.Objects.All(x => !x.IsStaticProfile), "GRAPH_PROFILE_REQUIRED", "Graph profile requires graph objects.");
            return new GraphProjectManifest
            {
                SchemaVersion = 3, Profile = "graph-project-v1", Units = "meters", Coordinates = Storage.Coordinates,
                DocumentId = doc.DocumentId, Name = doc.Name, StateHash = doc.StateHash,
                DocumentRevision = doc.DocumentRevision, SaveVersion = version,
                Objects = doc.Objects.Select(x => new GraphObjectManifest { ObjectId = x.ObjectId, GraphHash = GraphContentIdentity.Hash(x.Graph) }).ToArray()
            };
        }

        internal static GraphProjectManifest ReadManifest(JObject token)
        {
            var data = Storage.Decode<GraphProjectManifest>(token);
            Checks.Require(data.SchemaVersion == 3, "UNSUPPORTED_FORMAT", "Unsupported graph project version.");
            Checks.Require(data.Profile == "graph-project-v1" && data.Units == "meters" && data.Coordinates == Storage.Coordinates,
                "EXPORT_UNSUPPORTED_FEATURE", "Unsupported graph project profile.");
            Checks.Id(data.DocumentId); Checks.Name(data.Name); Checks.HashText(data.StateHash);
            Checks.Require(data.DocumentRevision >= 0 && data.DocumentRevision < long.MaxValue && data.SaveVersion > 0 && data.SaveVersion < long.MaxValue,
                "INVALID_REVISION", "Invalid saved revision.");
            Checks.Require(data.Objects.Length <= 1, "OBJECT_LIMIT", "This profile currently supports zero or one graph object.");
            foreach (var item in data.Objects) { Checks.Id(item.ObjectId); Checks.HashText(item.GraphHash); }
            return data;
        }

        internal static AuthoringDocument Decode(string directory, GraphProjectManifest data)
        {
            AuthoringObject item = data.Objects.Length == 0 ? null : new AuthoringObject(data.Objects[0].ObjectId,
                GraphBlobStore.Read(directory, data.Objects[0].GraphHash));
            var doc = new AuthoringDocument(data.DocumentId, data.Name, data.DocumentRevision, item);
            Checks.Require(doc.StateHash == data.StateHash, "HASH_MISMATCH", "Project state hash differs.");
            return doc;
        }
    }
}
