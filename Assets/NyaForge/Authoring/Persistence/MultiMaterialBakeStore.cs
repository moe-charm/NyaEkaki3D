using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace NyaForge.Authoring
{
    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class MultiMaterialBakeManifest
    {
        [JsonProperty(Required=Required.Always)] public int SchemaVersion=1;
        [JsonProperty(Required=Required.Always)] public string Profile=MultiMaterialBakeStore.Profile;
        [JsonProperty(Required=Required.Always)] public BakeManifest Mesh;
        [JsonProperty(Required=Required.Always)] public int[] SubmeshSlots;
        [JsonProperty(Required=Required.Always)] public BakedMaterialSlot[] Slots;
    }
    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class BakedMaterialSlot
    {
        [JsonProperty(Required=Required.Always)] public int Slot;
        [JsonProperty(Required=Required.Always)] public string MaterialNodeId;
        [JsonProperty(Required=Required.Always)] public string MaterialHash;
        [JsonProperty(Required=Required.Always)] public BakeImagePayload[] BaseColor;
    }
    public sealed class MultiMaterialBakeSlot
    {
        public int Slot { get; }
        public string MaterialNodeId { get; }
        public MaterialBakeDocument Surface { get; }
        internal MultiMaterialBakeSlot(int slot,string node,MaterialBakeDocument surface)
        { Slot=slot;MaterialNodeId=node;Surface=surface; }
    }
    public sealed class MultiMaterialBakeDocument
    {
        public BakeDocument Geometry { get; }
        public IReadOnlyList<int> SubmeshSlots { get; }
        public IReadOnlyList<MultiMaterialBakeSlot> Slots { get; }
        internal MultiMaterialBakeDocument(BakeDocument geometry,int[] map,MultiMaterialBakeSlot[] slots)
        { Geometry=geometry;SubmeshSlots=Array.AsReadOnly((int[])map.Clone());Slots=Array.AsReadOnly(slots); }
    }
    /// <summary>Preserves authored slot keys and material identities separately from dense render submeshes.</summary>
    public static class MultiMaterialBakeStore
    {
        public const string Profile="static-standard-pbr-linear-straight-fade-material-slots-v1";
        public const string ManifestName="materials.nyaforge-bake.json";
        public static string Export(string directory,AuthoringWorkspace workspace)
        {
            if(workspace==null) throw new ArgumentNullException(nameof(workspace));
            directory=Storage.DirectoryPath(directory);
            lock(workspace.Gate)
            {
                Checks.Require(!workspace.Executing,"REENTRANT_EXPORT","Export requires a committed document.");
                var source=BakeSource.CaptureMaterials(workspace.Document);
                Checks.Require(source.SlotOutput!=null,"MATERIAL_REQUIRED","Assign per-slot materials before this export.");
                var output=source.SlotOutput;
                var slots=output.SlotMaterials.OrderBy(p=>p.Key).Select(p=>new BakedMaterialSlot {
                    Slot=p.Key,MaterialNodeId=p.Value.MaterialNodeId,MaterialHash=p.Value.Material.Parameters.ContentHash,
                    BaseColor=p.Value.Material.BaseColor==null ? Array.Empty<BakeImagePayload>() : new[]{BakeImagePayload.Describe(p.Value.Material.BaseColor)} }).ToArray();
                var manifest=new MultiMaterialBakeManifest { Mesh=BakeStore.CreateManifest(workspace.Document,source),
                    SubmeshSlots=(output.PolygonRendering?.MaterialSlotMap ?? Enumerable.Range(0,source.Mesh.Submeshes.Count).ToArray()).ToArray(),Slots=slots };
                var bytes=Storage.JsonBytes(manifest);
                using(Storage.Lock(directory))
                {
                    Storage.WriteBlob(directory,source.Mesh.ContentHash,MeshBinary.Write(source.Mesh));
                    foreach(var slot in slots)
                    {
                        var material=output.SlotMaterials[slot.Slot].Material;
                        Storage.WriteBlob(directory,slot.MaterialHash,MaterialParametersCodec.Write(material.Parameters));
                        if(slot.BaseColor.Length==1) slot.BaseColor[0].Write(directory,material.BaseColor.Image);
                    }
                    string path=Path.Combine(directory,ManifestName);BakeOutputIdentity.Publish(path,bytes,workspace.Document);return path;
                }
            }
        }
        public static MultiMaterialBakeDocument Read(string manifestPath)
        {
            manifestPath=Path.GetFullPath(manifestPath);
            var manifest=Storage.ReadJson<MultiMaterialBakeManifest>(manifestPath);
            Checks.Require(manifest.SchemaVersion==1,"UNSUPPORTED_FORMAT","Unsupported multi-material Bake version.");
            Checks.Require(manifest.Profile==Profile,"EXPORT_UNSUPPORTED_FEATURE","Unsupported multi-material Bake profile.");
            Checks.Require(manifest.Slots.Length>0 && manifest.Slots.Length<=AuthoringLimits.MaxSubmeshes,"INVALID_MANIFEST","Invalid material slot count.");
            int previous=-1;var identities=new Dictionary<string,string>();
            foreach(var slot in manifest.Slots)
            {
                Checks.Require(slot!=null && slot.Slot>previous && slot.Slot<AuthoringLimits.MaxSubmeshes,"INVALID_MANIFEST","Slots must be bounded, unique and ascending.");
                previous=slot.Slot;Checks.Id(slot.MaterialNodeId);Checks.HashText(slot.MaterialHash);
                Checks.Require(slot.BaseColor.Length<=1 && slot.BaseColor.All(p=>p!=null),"INVALID_MANIFEST","Invalid base-color descriptors.");
                // A shared node cannot describe different parameters or image bindings in different slots.
                string identity=Checks.Hash(Storage.JsonBytes(new { slot.MaterialHash,slot.BaseColor }));
                Checks.Require(!identities.TryGetValue(slot.MaterialNodeId,out var existing) || existing==identity,"INVALID_MANIFEST","Shared material identity has conflicting payloads.");
                identities[slot.MaterialNodeId]=identity;
            }
            var keys=new HashSet<int>(manifest.Slots.Select(s=>s.Slot));
            Checks.Require(manifest.SubmeshSlots.Length>0 && manifest.SubmeshSlots.Length<=AuthoringLimits.MaxSubmeshes &&
                manifest.SubmeshSlots.Distinct().Count()==manifest.SubmeshSlots.Length && manifest.SubmeshSlots.All(keys.Contains),"INVALID_MANIFEST","Every dense submesh needs one unique authored slot.");
            string directory=Path.GetDirectoryName(manifestPath);var geometry=BakeStore.ReadManifest(manifest.Mesh,directory);
            Checks.Require(geometry.Mesh.Submeshes.Count==manifest.SubmeshSlots.Length,"INVALID_MANIFEST","Submesh map count differs from mesh.");
            var result=new List<MultiMaterialBakeSlot>();
            foreach(var slot in manifest.Slots)
            {
                var parameters=MaterialParametersCodec.Read(Storage.ReadBlob(directory,slot.MaterialHash));
                Checks.Require(parameters.ContentHash==slot.MaterialHash,"HASH_MISMATCH","Noncanonical material parameters.");
                var descriptor=slot.BaseColor.FirstOrDefault();
                MaterialBakeDocument surface;
                if(descriptor==null) surface=new MaterialBakeDocument(geometry,parameters,null,null,null);
                else { var image=descriptor.Read(directory,geometry.Mesh);surface=new MaterialBakeDocument(geometry,parameters,descriptor,image.image,image.png); }
                result.Add(new MultiMaterialBakeSlot(slot.Slot,slot.MaterialNodeId,surface));
            }
            return new MultiMaterialBakeDocument(geometry,manifest.SubmeshSlots,result.ToArray());
        }
    }
}
