using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Rig;
using NyaForge.Authoring.Simulation;

internal static partial class Program
{
    static void RunPhysBonesTargetPackageTests()
    {
        Test("PhysBones target package roundtrips profile and skeleton independently of Unity", () =>
        {
            var skeleton = BuildPhysBonesSkeleton(out var rootId, out var childId, out _);
            var chain = new PhysBonesChain("tail", rootId, new[] { rootId, childId }, PhysBonesEndpointMode.Auto, "", null,
                PhysBonesMultiChildType.Ignore, null, null, null, PhysBonesParameters.Default, PhysBonesInteraction.Default, null);
            var profile = new PhysBonesTargetProfile("vrchat.physbones", "sdk-3", "package-1", skeleton.ContentHash, "", new[] { chain });
            string directory = Dir("physbones-target-package");
            string manifest = PhysBonesTargetPackage.Export(directory, profile, skeleton);
            var reopened = PhysBonesTargetPackage.Read(manifest);
            Equal(profile.ContentHash, reopened.Target.ContentHash);
            Equal(skeleton.ContentHash, reopened.Skeleton.ContentHash);
            Equal(PhysBonesTargetPackage.DefaultComponentTypeName, reopened.ComponentTypeName);
            True(File.Exists(Path.Combine(directory, PhysBonesTargetPackage.ProfileFileName)));
            True(File.Exists(Path.Combine(directory, PhysBonesTargetPackage.SkeletonFileName)));
            True(File.Exists(manifest));

            string customDirectory = Dir("physbones-target-package-custom-type");
            const string customType = "Example.PhysBones.Component, Example.PhysBones";
            string customManifest = PhysBonesTargetPackage.Export(customDirectory, profile, skeleton, customType);
            Equal(customType, PhysBonesTargetPackage.Read(customManifest).ComponentTypeName);

            string legacyDirectory = Dir("physbones-target-package-legacy-manifest");
            string legacyManifest = PhysBonesTargetPackage.Export(legacyDirectory, profile, skeleton);
            var legacyJson = JObject.Parse(File.ReadAllText(legacyManifest));
            legacyJson.Remove("ComponentTypeName");
            File.WriteAllText(legacyManifest, legacyJson.ToString());
            Equal(PhysBonesTargetPackage.DefaultComponentTypeName, PhysBonesTargetPackage.Read(legacyManifest).ComponentTypeName);
        });

        Test("PhysBones target package refuses payload tampering before decode", () =>
        {
            var skeleton = BuildPhysBonesSkeleton(out var rootId, out _, out _);
            var chain = new PhysBonesChain("tail", rootId, new[] { rootId }, PhysBonesEndpointMode.Auto, "", null,
                PhysBonesMultiChildType.Ignore, null, null, null, PhysBonesParameters.Default, PhysBonesInteraction.Default, null);
            var profile = new PhysBonesTargetProfile("vrchat.physbones", "sdk", "", skeleton.ContentHash, "", new[] { chain });
            string directory = Dir("physbones-target-tamper");
            string manifest = PhysBonesTargetPackage.Export(directory, profile, skeleton);
            string path = Path.Combine(directory, PhysBonesTargetPackage.ProfileFileName);
            var bytes = File.ReadAllBytes(path); bytes[bytes.Length - 1] ^= 1; File.WriteAllBytes(path, bytes);
            Expect("HASH_MISMATCH", () => PhysBonesTargetPackage.Read(manifest));
        });
    }
}
