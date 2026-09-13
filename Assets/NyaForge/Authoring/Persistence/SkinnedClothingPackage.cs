using System;
using System.IO;
using Newtonsoft.Json;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring
{
    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class SkinnedClothingManifest
    {
        [JsonProperty(Required = Required.Always)] public int SchemaVersion = 1;
        [JsonProperty(Required = Required.Always)] public string Profile = SkinnedClothingPackage.Profile;
        [JsonProperty(Required = Required.Always)] public string Units = "meters";
        [JsonProperty(Required = Required.Always)] public string Coordinates = Storage.Coordinates;
        [JsonProperty(Required = Required.Always)] public string DocumentId;
        [JsonProperty(Required = Required.Always)] public string ObjectId;
        [JsonProperty(Required = Required.Always)] public string GraphId;
        [JsonProperty(Required = Required.Always)] public string StateHash;
        [JsonProperty(Required = Required.Always)] public string GraphHash;
        [JsonProperty(Required = Required.Always)] public string GlbFile = SkinnedClothingPackage.GlbFileName;
        [JsonProperty(Required = Required.Always)] public string SkeletonFile = SkinnedClothingPackage.SkeletonFileName;
        [JsonProperty(Required = Required.Always)] public string BindingFile = SkinnedClothingPackage.BindingFileName;
        [JsonProperty(Required = Required.Always)] public string GlbHash;
        [JsonProperty(Required = Required.Always)] public string SkeletonHash;
        [JsonProperty(Required = Required.Always)] public string BindingHash;
        [JsonProperty(Required = Required.Always)] public string MeshContentHash;
        [JsonProperty(Required = Required.Always)] public string MeshTopologyHash;
        [JsonProperty(Required = Required.Always)] public int VertexCount;
        [JsonProperty(Required = Required.Always)] public int TriangleCount;
    }

    /// <summary>Self-contained clothing-only package for an explicit Unity receiver.</summary>
    public sealed class SkinnedClothingPackage
    {
        public const string Profile = "skinned-clothing-v1";
        public const string ManifestFileName = "skinned-clothing.nyaforge.json";
        public const string GlbFileName = "clothing.glb";
        public const string SkeletonFileName = "skeleton.nyaforge.bin";
        public const string BindingFileName = "binding.nyaforge.bin";

        public string ManifestPath { get; private set; }
        public string DocumentId { get; private set; }
        public string ObjectId { get; private set; }
        public string GraphId { get; private set; }
        public string StateHash { get; private set; }
        public string GraphHash { get; private set; }
        public string GlbHash { get; private set; }
        public MeshData Mesh { get; private set; }
        public SkeletonDefinition Skeleton { get; private set; }
        public SkinBinding Binding { get; private set; }
        public byte[] Glb { get; private set; }

        SkinnedClothingPackage(SkinnedClothingManifest manifest, string manifestPath, byte[] glb,
            MeshData mesh, SkeletonDefinition skeleton, SkinBinding binding)
        {
            ManifestPath = manifestPath; DocumentId = manifest.DocumentId; ObjectId = manifest.ObjectId;
            GraphId = manifest.GraphId; StateHash = manifest.StateHash; GraphHash = manifest.GraphHash;
            GlbHash = manifest.GlbHash; Glb = glb; Mesh = mesh; Skeleton = skeleton; Binding = binding;
        }

        /// <summary>Writes a clothing-only package after validating the exact GLB geometry.</summary>
        public static string Export(string directory, byte[] glb, MeshData mesh, SkeletonDefinition skeleton,
            SkinBinding binding, string documentId, string objectId, string graphId, string stateHash, string graphHash)
        {
            if (glb == null) throw new ArgumentNullException("glb");
            if (mesh == null || skeleton == null || binding == null) throw new ArgumentNullException("mesh/skeleton/binding");
            directory = Storage.DirectoryPath(directory);
            Checks.Id(documentId); Checks.Id(objectId); Checks.Id(graphId);
            Checks.HashText(stateHash); Checks.HashText(graphHash);
            binding.ValidateFor(mesh, skeleton);
            Checks.Require(glb.Length > 0 && glb.Length <= AuthoringLimits.MaxGlbImportBytes, "BUDGET_EXCEEDED", "Clothing GLB exceeds the import budget.");
            var imported = GlbSkinImporter.Read(glb);
            Checks.Require(imported.Mesh.ContentHash == mesh.ContentHash && imported.Mesh.TopologyHash == mesh.TopologyHash &&
                imported.Mesh.VertexCount == mesh.VertexCount && imported.Mesh.TriangleCount == mesh.TriangleCount,
                "CLOTHING_GLB_MISMATCH", "Clothing GLB geometry does not match the authored mesh.");
            byte[] skeletonBytes = RigCodec.WriteSkeleton(skeleton);
            byte[] bindingBytes = RigCodec.WriteBinding(binding);
            var manifest = new SkinnedClothingManifest
            {
                DocumentId = documentId, ObjectId = objectId, GraphId = graphId, StateHash = stateHash, GraphHash = graphHash,
                GlbHash = Checks.Hash(glb), SkeletonHash = Checks.Hash(skeletonBytes), BindingHash = Checks.Hash(bindingBytes),
                MeshContentHash = mesh.ContentHash, MeshTopologyHash = mesh.TopologyHash,
                VertexCount = mesh.VertexCount, TriangleCount = mesh.TriangleCount
            };
            Checks.Require(!Directory.Exists(directory) && !File.Exists(directory), "EXPORT_DESTINATION_EXISTS", "Clothing package destination already exists.");
            string staging = directory + ".staging-" + Guid.NewGuid().ToString("N");
            try
            {
                Directory.CreateDirectory(staging);
                Storage.AtomicWrite(Path.Combine(staging, GlbFileName), glb, false);
                Storage.AtomicWrite(Path.Combine(staging, SkeletonFileName), skeletonBytes, false);
                Storage.AtomicWrite(Path.Combine(staging, BindingFileName), bindingBytes, false);
                string manifestPath = Path.Combine(staging, ManifestFileName);
                Storage.AtomicWrite(manifestPath, Storage.JsonBytes(manifest), false);
                // Read the staged package before publication so a malformed
                // sidecar or an unreadable GLB can never appear as an export.
                Read(manifestPath);
                Directory.CreateDirectory(Path.GetDirectoryName(directory));
                Directory.Move(staging, directory);
                return Path.Combine(directory, ManifestFileName);
            }
            catch
            {
                if (Directory.Exists(staging)) Directory.Delete(staging, true);
                throw;
            }
        }

        public static SkinnedClothingPackage Read(string manifestPath)
        {
            manifestPath = Path.GetFullPath(manifestPath);
            var manifest = Storage.ReadJson<SkinnedClothingManifest>(manifestPath);
            Checks.Require(manifest.SchemaVersion == 1 && manifest.Profile == Profile && manifest.Units == "meters" && manifest.Coordinates == Storage.Coordinates,
                "UNSUPPORTED_FORMAT", "Unsupported skinned clothing package.");
            Checks.Require(manifest.GlbFile == GlbFileName && manifest.SkeletonFile == SkeletonFileName && manifest.BindingFile == BindingFileName,
                "INVALID_MANIFEST", "Clothing package file names are invalid.");
            Checks.Id(manifest.DocumentId); Checks.Id(manifest.ObjectId); Checks.Id(manifest.GraphId);
            Checks.HashText(manifest.StateHash); Checks.HashText(manifest.GraphHash); Checks.HashText(manifest.GlbHash);
            Checks.HashText(manifest.SkeletonHash); Checks.HashText(manifest.BindingHash);
            Checks.HashText(manifest.MeshContentHash); Checks.HashText(manifest.MeshTopologyHash);
            Checks.Require(manifest.VertexCount >= 3 && manifest.VertexCount <= AuthoringLimits.MaxVertices && manifest.TriangleCount > 0,
                "BUDGET_EXCEEDED", "Clothing package mesh counts exceed the budget.");
            string directory = Path.GetDirectoryName(manifestPath);
            byte[] glb = Storage.ReadBounded(Path.Combine(directory, GlbFileName), AuthoringLimits.MaxGlbImportBytes);
            byte[] skeletonBytes = Storage.ReadBounded(Path.Combine(directory, SkeletonFileName), AuthoringLimits.MaxBlobBytes);
            byte[] bindingBytes = Storage.ReadBounded(Path.Combine(directory, BindingFileName), AuthoringLimits.MaxBlobBytes);
            Checks.Require(Checks.Hash(glb) == manifest.GlbHash && Checks.Hash(skeletonBytes) == manifest.SkeletonHash && Checks.Hash(bindingBytes) == manifest.BindingHash,
                "HASH_MISMATCH", "Clothing package payload hash differs from its manifest.");
            var imported = GlbSkinImporter.Read(glb);
            var skeleton = RigCodec.ReadSkeleton(skeletonBytes);
            Checks.Require(imported.Mesh.ContentHash == manifest.MeshContentHash && imported.Mesh.TopologyHash == manifest.MeshTopologyHash &&
                imported.Mesh.VertexCount == manifest.VertexCount && imported.Mesh.TriangleCount == manifest.TriangleCount,
                "CLOTHING_GLB_MISMATCH", "Clothing package GLB geometry differs from its manifest.");
            var binding = RigCodec.ReadBinding(bindingBytes, imported.Mesh, skeleton);
            Checks.Require(binding.MeshTopologyHash == manifest.MeshTopologyHash && binding.SkeletonHash == skeleton.ContentHash,
                "CLOTHING_RIG_MISMATCH", "Clothing package binding does not match its skeleton or mesh.");
            return new SkinnedClothingPackage(manifest, manifestPath, glb, imported.Mesh, skeleton, binding);
        }
    }
}
