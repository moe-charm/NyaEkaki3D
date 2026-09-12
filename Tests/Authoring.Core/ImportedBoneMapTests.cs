using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;

internal static partial class Program
{
    static byte[] BuildMappedVrm(bool legacy, bool unmapped = false)
    {
        var bytes = BuildSkinnedGlb(); var root = JObject.Parse(ReadJsonChunk(bytes));
        // skin slots 0/1 refer to nodes 1/2. Names deliberately cannot distinguish the bones.
        root["nodes"] = new JArray(new JObject { ["name"] = "mesh", ["mesh"] = 0, ["skin"] = 0 },
            new JObject { ["name"] = "same", ["children"] = new JArray(2) },
            new JObject { ["name"] = "same", ["translation"] = new JArray(0, .1, 0) });
        root["skins"][0]["joints"] = new JArray(1, 2);
        var humanoid = legacy
            ? new JObject { ["humanBones"] = new JArray(new JObject { ["bone"] = "hips", ["node"] = 1 }, new JObject { ["bone"] = "spine", ["node"] = unmapped ? 0 : 2 }) }
            : new JObject { ["humanBones"] = new JObject { ["hips"] = new JObject { ["node"] = 1 }, ["spine"] = new JObject { ["node"] = unmapped ? 0 : 2 } } };
        root["extensions"] = new JObject { [legacy ? "VRM" : "VRMC_vrm"] = new JObject {
            ["specVersion"] = legacy ? "0.0" : "1.0", ["meta"] = legacy ? new JObject { ["author"] = "fixture" } : new JObject { ["authors"] = new JArray("fixture") }, ["humanoid"] = humanoid } };
        return ReplaceJsonChunk(bytes, root.ToString());
    }

    static void RunImportedBoneMapTests()
    {
        RunImportedRigSessionTests();
        RunImportedJointHierarchyTests();
        foreach (bool legacy in new[] { false, true })
            Test("VRM humanoid maps source nodes, not names or skin slots: " + legacy, () =>
            {
                var bytes = BuildMappedVrm(legacy); var imported = GlbSkinImporter.Read(bytes);
                var metadata = VrmMetadataReader.Read(bytes);
                var mapping = VrmHumanoidBinding.Create(metadata, imported.BoneMap, imported.Skeleton);
                Equal(2, imported.BoneMap.ByNode.Count); False(imported.BoneMap.ByNode.ContainsKey(0));
                Equal(imported.BoneMap.Resolve(1), mapping.BoneIds["hips"]); Equal(imported.BoneMap.Resolve(2), mapping.BoneIds["spine"]);
                Equal(mapping.BoneIds["hips"], imported.Skeleton.ById[mapping.BoneIds["spine"]].ParentBoneId);
                Equal(imported.SourceHash, mapping.SourceHash); Equal(imported.Skeleton.ContentHash, mapping.SkeletonHash);
                var reopened = RigCodec.ReadSkeleton(RigCodec.WriteSkeleton(imported.Skeleton));
                imported.BoneMap.ValidateFor(metadata.SourceHash, reopened);
                Equal(mapping.BoneIds["spine"], VrmHumanoidBinding.Create(metadata, imported.BoneMap, reopened).BoneIds["spine"]);
                var again = GlbSkinImporter.Read(bytes); Equal(imported.BoneMap.Resolve(1), again.BoneMap.Resolve(1));
                Near(.5f, imported.Binding.Weights[1].Single(value => value.BoneId == mapping.BoneIds["spine"]).Weight);
            });

        Test("VRM bone adapters reject another source, edited skeleton, and non-joint nodes", () =>
        {
            var bytes = BuildMappedVrm(false); var imported = GlbSkinImporter.Read(bytes); var metadata = VrmMetadataReader.Read(bytes);
            Expect("IMPORT_SOURCE_CHANGED", () => VrmHumanoidBinding.Create(VrmMetadataReader.Read(BuildMappedVrm(true)), imported.BoneMap, imported.Skeleton));
            var edited = SkeletonEditing.MoveBone(imported.Skeleton, imported.BoneMap.Resolve(2), new Vec3(.01f, 0, 0), new Vec3(.01f, 0, 0));
            Expect("IMPORT_SKELETON_CHANGED", () => VrmHumanoidBinding.Create(metadata, imported.BoneMap, edited));
            var missingBytes = BuildMappedVrm(false, true); var missing = GlbSkinImporter.Read(missingBytes);
            Expect("IMPORT_BONE_UNMAPPED", () => VrmHumanoidBinding.Create(VrmMetadataReader.Read(missingBytes), missing.BoneMap, missing.Skeleton));
        });
    }
}
