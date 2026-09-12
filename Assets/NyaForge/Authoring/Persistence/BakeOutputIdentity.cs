using System;
using System.IO;
using Newtonsoft.Json;

namespace NyaForge.Authoring
{
    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class BakeOutputIdentityManifest
    {
        [JsonProperty(Required=Required.Always)] public int SchemaVersion=1;
        [JsonProperty(Required=Required.Always)] public string DocumentId;
        [JsonProperty(Required=Required.Always)] public string ObjectId;
        [JsonProperty(Required=Required.Always)] public string OutputId;
        [JsonProperty(Required=Required.Always)] public string OutputKind;
        [JsonProperty(Required=Required.Always)] public long Revision;
        [JsonProperty(Required=Required.Always)] public string ManifestHash;
    }
    /// <summary>Stable export target identity, bound to exact Bake bytes. Missing means legacy, never inferred identity.</summary>
    public sealed class BakeOutputIdentity
    {
        public const string Suffix=".identity.json";
        public string DocumentId { get; }
        public string ObjectId { get; }
        public string OutputId { get; }
        public string OutputKind { get; }
        public long Revision { get; }
        public string ManifestHash { get; }
        BakeOutputIdentity(BakeOutputIdentityManifest value)
        { DocumentId=value.DocumentId;ObjectId=value.ObjectId;OutputId=value.OutputId;OutputKind=value.OutputKind;Revision=value.Revision;ManifestHash=value.ManifestHash; }
        internal static void Publish(string path,byte[] bytes,AuthoringDocument document)
        {
            var item=document.Objects[0];
            var identity=new BakeOutputIdentityManifest { DocumentId=document.DocumentId,ObjectId=document.ObjectId,
                OutputId=item.IsStaticProfile ? document.ObjectId : item.Graph.OutputNodeId,
                OutputKind=item.IsStaticProfile ? "static-object" : "graph-output",Revision=document.DocumentRevision,ManifestHash=Checks.Hash(bytes) };
            byte[] descriptor=Storage.JsonBytes(identity);
            // A crash between writes leaves an absent/mismatched descriptor; update readers must reject it.
            Storage.AtomicWrite(path,bytes,true);
            Storage.AtomicWrite(path+Suffix,descriptor,true);
        }
        public static BakeOutputIdentity Read(string manifestPath,BakeDocument geometry)
        {
            if(geometry==null) throw new ArgumentNullException(nameof(geometry));
            manifestPath=Path.GetFullPath(manifestPath);string path=manifestPath+Suffix;
            if(!File.Exists(path)) return null;
            var value=Storage.ReadJson<BakeOutputIdentityManifest>(path);
            Checks.Require(value.SchemaVersion==1,"UNSUPPORTED_FORMAT","Unsupported Bake output identity version.");
            Checks.Id(value.DocumentId);Checks.Id(value.ObjectId);Checks.Id(value.OutputId);Checks.HashText(value.ManifestHash);
            Checks.Require(value.OutputKind=="graph-output" || value.OutputKind=="static-object","INVALID_MANIFEST","Unknown output identity kind.");
            Checks.Require(value.OutputKind!="static-object" || value.OutputId==value.ObjectId,"INVALID_MANIFEST","Static output must identify its object.");
            Checks.Require(value.DocumentId==geometry.DocumentId && value.ObjectId==geometry.ObjectId && value.Revision==geometry.DocumentRevision,
                "OUTPUT_IDENTITY_MISMATCH","Identity descriptor belongs to a different document snapshot.");
            Checks.Require(Checks.Hash(Storage.ReadBounded(manifestPath,AuthoringLimits.MaxManifestBytes))==value.ManifestHash,
                "HASH_MISMATCH","Output identity does not describe these Bake manifest bytes.");
            return new BakeOutputIdentity(value);
        }
    }
}
