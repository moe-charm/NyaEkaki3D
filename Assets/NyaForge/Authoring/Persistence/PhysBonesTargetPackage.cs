using System;
using System.IO;
using Newtonsoft.Json;
using NyaForge.Authoring.Rig;
using NyaForge.Authoring.Simulation;

namespace NyaForge.Authoring
{
    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class PhysBonesTargetManifest
    {
        [JsonProperty(Required = Required.Always)] public int SchemaVersion = 1;
        [JsonProperty(Required = Required.Always)] public string Profile = "physbones-target-v1";
        [JsonProperty(Required = Required.Always)] public string TargetId;
        [JsonProperty(Required = Required.Always)] public string SdkVersion;
        [JsonProperty(Required = Required.Always)] public string PackageVersion;
        [JsonProperty(Required = Required.Always)] public string ProfileHash;
        [JsonProperty(Required = Required.Always)] public string SkeletonHash;
        [JsonProperty(Required = Required.Always)] public string ProfileFile = PhysBonesTargetPackage.ProfileFileName;
        [JsonProperty(Required = Required.Always)] public string SkeletonFile = PhysBonesTargetPackage.SkeletonFileName;
        [JsonProperty] public string SourceFile = "";
        // Optional for compatibility with packages exported before the receiver type was recorded.
        [JsonProperty] public string ComponentTypeName;
    }

    /// <summary>Self-contained PhysBones target package for a Unity Bridge receiver.</summary>
    public sealed class PhysBonesTargetPackage
    {
        public const string Profile = "physbones-target-v1";
        public const string ManifestName = "physbones.nyaforge-target.json";
        public const string ProfileFileName = "physbones-target.nyaforge.bin";
        public const string SkeletonFileName = "skeleton.nyaforge.bin";
        public const string SourceFileName = "secondary-motion.nyaforge.bin";
        public const string DefaultComponentTypeName = "VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone";

        public PhysBonesTargetProfile Target { get; private set; }
        public SkeletonDefinition Skeleton { get; private set; }
        public string ManifestHash { get; private set; }
        public string ComponentTypeName { get; private set; }
        public SecondaryMotionAsset Source { get; private set; }

        PhysBonesTargetPackage(PhysBonesTargetProfile target, SkeletonDefinition skeleton, string manifestHash, string componentTypeName, SecondaryMotionAsset source)
        { Target = target; Skeleton = skeleton; ManifestHash = manifestHash; ComponentTypeName = componentTypeName; Source = source; }

        public static string Export(string directory, PhysBonesTargetProfile target, SkeletonDefinition skeleton, string componentTypeName = null)
        { return Export(directory, target, skeleton, null, componentTypeName); }

        /// <summary>Exports a target package and embeds the source asset when the profile pins one.</summary>
        public static string Export(string directory, PhysBonesTargetProfile target, SkeletonDefinition skeleton, SecondaryMotionAsset source, string componentTypeName = null)
        {
            if (target == null) throw new ArgumentNullException("target");
            if (skeleton == null) throw new ArgumentNullException("skeleton");
            Checks.Require(target.SkeletonHash == skeleton.ContentHash, "SIMULATION_SKELETON_CHANGED", "PhysBones target does not match the exported skeleton.");
            if (target.SourceSecondaryMotionHash != "")
                Checks.Require(source != null && source.ContentHash == target.SourceSecondaryMotionHash, "SIMULATION_ASSET_CHANGED", "PhysBones target requires its source asset to be embedded in the package.");
            componentTypeName = NormalizeComponentTypeName(componentTypeName);
            directory = Storage.DirectoryPath(directory);
            byte[] profileBytes = PhysBonesTargetCodec.Write(target);
            byte[] skeletonBytes = RigCodec.WriteSkeleton(skeleton);
            var manifest = new PhysBonesTargetManifest
            {
                TargetId = target.TargetId, SdkVersion = target.SdkVersion, PackageVersion = target.PackageVersion,
                ProfileHash = Checks.Hash(profileBytes), SkeletonHash = Checks.Hash(skeletonBytes), ComponentTypeName = componentTypeName,
                SourceFile = source == null ? "" : SourceFileName
            };
            byte[] manifestBytes = Storage.JsonBytes(manifest);
            using (Storage.Lock(directory))
            {
                Storage.AtomicWrite(Path.Combine(directory, ProfileFileName), profileBytes, false);
                Storage.AtomicWrite(Path.Combine(directory, SkeletonFileName), skeletonBytes, false);
                if (source != null) Storage.AtomicWrite(Path.Combine(directory, SourceFileName), SecondaryMotionCodec.Write(source), false);
                string path = Path.Combine(directory, ManifestName);
                Storage.AtomicWrite(path, manifestBytes, false);
                return path;
            }
        }

        public static PhysBonesTargetPackage Read(string manifestPath)
        {
            manifestPath = Path.GetFullPath(manifestPath);
            // Packages emitted before source embedding have no sourceFile field.
            // Normalize that legacy shape before the strict manifest validator runs.
            var manifestJson = Storage.ReadObject(manifestPath);
            if (manifestJson["sourceFile"] == null) manifestJson["sourceFile"] = "";
            var manifest = Storage.Decode<PhysBonesTargetManifest>(manifestJson);
            Checks.Require(manifest.SchemaVersion == 1 && manifest.Profile == Profile, "UNSUPPORTED_FORMAT", "Unsupported PhysBones target package.");
            Checks.Require(manifest.ProfileFile == ProfileFileName && manifest.SkeletonFile == SkeletonFileName, "INVALID_MANIFEST", "PhysBones package file names are invalid.");
            Checks.Require(manifest.SourceFile == "" || manifest.SourceFile == SourceFileName, "INVALID_MANIFEST", "PhysBones package source file name is invalid.");
            SecondaryMotionCapabilities.Text(manifest.TargetId, "PhysBones package target identity");
            SecondaryMotionCapabilities.Text(manifest.SdkVersion, "PhysBones package SDK version");
            SecondaryMotionCapabilities.OptionalText(manifest.PackageVersion, "PhysBones package version");
            Checks.HashText(manifest.ProfileHash); Checks.HashText(manifest.SkeletonHash);
            string directory = Path.GetDirectoryName(manifestPath);
            byte[] profileBytes = Storage.ReadBounded(Path.Combine(directory, ProfileFileName), AuthoringLimits.MaxBlobBytes);
            byte[] skeletonBytes = Storage.ReadBounded(Path.Combine(directory, SkeletonFileName), AuthoringLimits.MaxBlobBytes);
            Checks.Require(Checks.Hash(profileBytes) == manifest.ProfileHash && Checks.Hash(skeletonBytes) == manifest.SkeletonHash, "HASH_MISMATCH", "PhysBones package payload hash differs from its manifest.");
            var target = PhysBonesTargetCodec.Read(profileBytes);
            var skeleton = RigCodec.ReadSkeleton(skeletonBytes);
            SecondaryMotionAsset source = null;
            if (manifest.SourceFile != "")
            {
                byte[] sourceBytes = Storage.ReadBounded(Path.Combine(directory, manifest.SourceFile), AuthoringLimits.MaxBlobBytes);
                source = SecondaryMotionCodec.Read(sourceBytes);
                Checks.Require(source.ContentHash == target.SourceSecondaryMotionHash, "SIMULATION_ASSET_CHANGED", "PhysBones package source asset does not match its target profile.");
            }
            else Checks.Require(target.SourceSecondaryMotionHash == "", "SIMULATION_ASSET_CHANGED", "PhysBones package is missing its pinned source asset.");
            Checks.Require(target.TargetId == manifest.TargetId && target.SdkVersion == manifest.SdkVersion && target.PackageVersion == manifest.PackageVersion, "INVALID_MANIFEST", "PhysBones package metadata differs from its target profile.");
            Checks.Require(target.SkeletonHash == skeleton.ContentHash, "SIMULATION_SKELETON_CHANGED", "PhysBones package target does not match its skeleton.");
            return new PhysBonesTargetPackage(target, skeleton, Checks.Hash(Storage.ReadBounded(manifestPath, AuthoringLimits.MaxManifestBytes)), NormalizeComponentTypeName(manifest.ComponentTypeName), source);
        }

        static string NormalizeComponentTypeName(string value)
        {
            string result = string.IsNullOrWhiteSpace(value) ? DefaultComponentTypeName : value.Trim();
            Checks.Require(result.Length <= 512 && result.IndexOf('\0') < 0, "INVALID_MANIFEST", "PhysBones component type name is invalid.");
            return result;
        }
    }
}
