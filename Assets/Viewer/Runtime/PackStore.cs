using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Viewer.Contracts;

namespace Viewer.Runtime
{
    public sealed class VerifiedPack
    {
        public string Path, Hash;
        public PackManifest Manifest;
        public string Directory => System.IO.Path.GetDirectoryName(Path);
    }
    public static class PackStore
    {
        public static VerifiedPack Verify(string path, string token, string expectedHash = null)
        {
            path = System.IO.Path.GetFullPath(path);
            if (!File.Exists(path)) throw new ContractException("PACK_INCOMPLETE", "パックが見つかりません。");
            var manifest = JsonFiles.Read<PackManifest>(path, out string hash);
            // SHA-256 hex is case-insensitive. Normalize the comparison so a
            // pointer written by another tool cannot be rejected solely for
            // using upper-case hexadecimal characters.
            if (expectedHash != null && !string.Equals(hash, expectedHash, StringComparison.OrdinalIgnoreCase))
                throw new ContractException("PACK_HASH_MISMATCH", "保存時のパックと一致しません。");
            Validation.Manifest(manifest, token);
            var directory = System.IO.Path.GetDirectoryName(path);
            foreach (var b in manifest.bundles)
            {
                var file = JsonFiles.PackChild(directory, b.path);
                if (!File.Exists(file) || new FileInfo(file).Length != b.sizeBytes) throw new ContractException("PACK_INCOMPLETE", "パックが不完全です: " + b.path);
                if (JsonFiles.Sha256(file) != b.sha256) throw new ContractException("PACK_HASH_MISMATCH", "パックの内容が一致しません: " + b.path);
            }
            return new VerifiedPack { Path = path, Hash = hash, Manifest = manifest };
        }
        public static List<BundleRecord> DependencyOrder(PackManifest m)
        {
            var result = new List<BundleRecord>(); var seen = new HashSet<string>();
            var lookup = m.bundles.ToDictionary(b => b.id);
            void Visit(BundleRecord b)
            {
                if (!seen.Add(b.id)) return;
                foreach (var id in b.dependsOn) Visit(lookup[id]);
                result.Add(b);
            }
            foreach (var b in m.bundles) Visit(b);
            return result;
        }
        public static SessionDocument Defaults(VerifiedPack p)
        {
            return new SessionDocument
            {
                schemaVersion = 1,
                pack = Reference(p), avatarId = p.Manifest.avatarId, rigProfileId = p.Manifest.rigProfileId,
                visibilityOverrides = Array.Empty<VisibilityOverride>(), morphOverrides = Array.Empty<MorphOverride>(),
                unresolvedOverrides = Array.Empty<UnresolvedOverride>(),
                motion = new MotionState { clipId = p.Manifest.defaultClipId, timeSeconds = 0, speed = 1, loop = true },
                camera = new CameraState { targetMeters = new[] { 0f, .9f, 0f }, orientationQuat = new[] { 0f, 1f, 0f, 0f }, distanceMeters = 2.8f, verticalFovDegrees = 35 },
                preview = new PreviewState { lightPresetId = "studio", mode = "original" }
            };
        }
        public static PackReference Reference(VerifiedPack p) => new PackReference { packId = p.Manifest.packId, revision = p.Manifest.revision, manifestPath = p.Path, manifestSha256 = p.Hash };
    }
    public sealed class ActivePack : IDisposable
    {
        public VerifiedPack Verified;
        public AvatarInstance Avatar;
        public readonly Dictionary<string, AssetBundle> Bundles = new Dictionary<string, AssetBundle>();
        public void DestroyInstances() { Avatar?.Dispose(); Avatar = null; }
        public void Dispose()
        {
            DestroyInstances();
            var ordered = PackStore.DependencyOrder(Verified.Manifest);
            ordered.Reverse();
            foreach (var b in ordered) if (Bundles.TryGetValue(b.id, out var bundle) && bundle) bundle.Unload(true);
            Bundles.Clear();
        }
        public void Instantiate()
        {
            var m = Verified.Manifest;
            var prefab = Bundles[m.avatarPrefab.bundleId].LoadAsset<GameObject>(m.avatarPrefab.assetName);
            if (!prefab) throw new ContractException("PACK_PREFAB_MISSING", "身体データを読み込めません。");
            var clips = new Dictionary<string, AnimationClip>();
            foreach (var c in m.clips)
            {
                var clip = Bundles[c.bundleId].LoadAsset<AnimationClip>(c.assetName);
                if (!clip) throw new ContractException("REQUIRED_BINDING_MISSING", "確認ポーズを読み込めません。");
                clips.Add(c.clipId, clip);
            }
            Avatar = new AvatarInstance(prefab, m, clips);
        }
    }
}
