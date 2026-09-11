using System;

namespace Viewer.Contracts
{
    [Serializable] public sealed class Toolchain
    {
        public string unityVersion, buildTarget, architecture, graphicsApi, renderPipeline, colorSpace;
        public string lilToonVersion, packageLockSha256, shaderProfileId, compatibilityToken;
        public int importRecipeVersion, runtimeContractVersion;
    }
    [Serializable] public sealed class BundleRecord
    {
        public string id, path, sha256;
        public long sizeBytes;
        public string[] dependsOn;
    }
    [Serializable] public sealed class AssetReference { public string bundleId, assetName; }
    [Serializable] public sealed class RendererRecord
    {
        public string rendererId, displayName, path, rendererType, category;
        public bool defaultVisible, required;
    }
    [Serializable] public sealed class MorphBinding
    {
        public string rendererId, shapeName;
        public float minWeight, maxWeight, defaultWeight;
        public bool required;
    }
    [Serializable] public sealed class ClipRecord
    {
        public string clipId, displayName, bundleId, assetName;
        public double durationSeconds, sampleRate;
        public bool required;
    }
    [Serializable] public sealed class PackManifest
    {
        public int schemaVersion;
        public string packId, revision, displayName, avatarId, avatarSourceVersion, rigProfileId;
        public Toolchain toolchain;
        public BundleRecord[] bundles;
        public AssetReference avatarPrefab;
        public RendererRecord[] renderers;
        public MorphBinding[] morphBindings;
        public ClipRecord[] clips;
        public string defaultClipId;
        public string[] excludedFeatures;
    }
    [Serializable] public sealed class CurrentReference
    {
        public int schemaVersion;
        public string packId, revision, buildTarget, manifestPath, manifestSha256;
    }
    [Serializable] public sealed class PackReference
    {
        public string packId, revision, manifestPath, manifestSha256;
    }
    [Serializable] public sealed class VisibilityOverride { public string rendererId; public bool visible; }
    [Serializable] public sealed class MorphOverride { public string rendererId, shapeName; public float weight; }
    [Serializable] public sealed class UnresolvedOverride
    {
        public string kind, rendererId, shapeName, reasonCode;
        public float weight;
    }
    [Serializable] public sealed class MotionState
    {
        public string clipId;
        public double timeSeconds, speed;
        public bool loop;
    }
    [Serializable] public sealed class CameraState
    {
        public float[] targetMeters, orientationQuat;
        public float distanceMeters, verticalFovDegrees;
    }
    [Serializable] public sealed class PreviewState { public string lightPresetId, mode; }
    [Serializable] public sealed class SessionDocument
    {
        public int schemaVersion;
        public PackReference pack;
        public string avatarId, rigProfileId;
        public VisibilityOverride[] visibilityOverrides;
        public MorphOverride[] morphOverrides;
        public MotionState motion;
        public CameraState camera;
        public PreviewState preview;
        public UnresolvedOverride[] unresolvedOverrides;
    }
    public sealed class ContractException : Exception
    {
        public readonly string Code;
        public ContractException(string code, string message) : base(message) { Code = code; }
    }
}
