using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;
using NyaForge.Authoring.Simulation;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    static void RunSecondaryMotionContractTests()
    {
        Test("secondary motion contract keeps bone and mesh outputs distinct", () =>
        {
            var skeleton = BuildSecondarySkeleton(out var rootId, out var childId); var mesh = AuthoringFixtures.Panel(1);
            var profile = new SecondaryMotionProfile("test.adapter", "test.simulator", 1, "1", "", SecondaryMotionOutputKind.BonePose, Array.Empty<byte>());
            var chain = new SecondaryMotionChain("hair", new[] { childId }, new[] { 0 });
            var colliders = new SecondaryMotionColliderGroup("body", new[] { new SecondaryMotionCollider("", new Vec3(0, 0, 0), .1f) });
            var asset = new SecondaryMotionAsset(profile, skeleton.ContentHash, mesh.TopologyHash, new[] { chain }, new[] { colliders }, Array.Empty<int>());
            asset.ValidateFor(skeleton, mesh);
            var pose = PoseSet.Create(skeleton, skeleton.Bones.Select(bone => new BonePose(bone.BoneId, PoseTransform.FromTranslation(bone.Head))));
            var request = new SecondaryMotionEvaluationRequest(asset, skeleton, mesh, pose, 0);
            True(request.Asset == asset); True(SecondaryMotionEvaluationResult.Bone(asset, pose).BonePose == pose);
            var alteredSubmeshes = mesh.Submeshes.Select(indices => indices.Reverse().ToArray()).ToArray();
            var alteredMesh = new MeshData(mesh.Positions.ToArray(), mesh.Normals.ToArray(), mesh.Tangents.ToArray(), mesh.Uv0.ToArray(), alteredSubmeshes);
            Expect("SIMULATION_TOPOLOGY_CHANGED", () => asset.ValidateFor(skeleton, alteredMesh));
            var meshProfile = new SecondaryMotionProfile("test.mesh.adapter", "test.simulator", 1, "1", "", SecondaryMotionOutputKind.MeshDeformation, Array.Empty<byte>());
            var meshAsset = new SecondaryMotionAsset(meshProfile, "", mesh.TopologyHash, null, null, new[] { 0, 1 });
            meshAsset.ValidateFor(null, mesh); var meshRequest = new SecondaryMotionEvaluationRequest(meshAsset, null, mesh, null, 0);
            True(meshRequest.BasePose == null); True(SecondaryMotionEvaluationResult.MeshDeformation(meshAsset, mesh).Mesh == mesh);
            Expect("INVALID_SIMULATION", () => new SecondaryMotionAsset(profile, skeleton.ContentHash, mesh.TopologyHash, new[] { chain }, new[] { colliders }, new[] { 0 }));
            var wrongSkeleton = BuildSecondarySkeleton(out var wrongRootId, out var wrongChildId);
            var wrongPose = PoseSet.Create(wrongSkeleton, new[] { new BonePose(wrongRootId, PoseTransform.Identity), new BonePose(wrongChildId, PoseTransform.Identity) });
            Expect("SIMULATION_SKELETON_CHANGED", () => SecondaryMotionEvaluationResult.Bone(asset, wrongPose));
        });

        Test("secondary motion codec roundtrips and retains unknown versions", () =>
        {
            var skeleton = BuildSecondarySkeleton(out _, out var childId); var mesh = AuthoringFixtures.Panel(1);
            var profile = new SecondaryMotionProfile("test.adapter", "test.simulator", 1, "1", "pkg", SecondaryMotionOutputKind.BonePose, new byte[] { 1, 2, 3 });
            var asset = new SecondaryMotionAsset(profile, skeleton.ContentHash, mesh.TopologyHash,
                new[] { new SecondaryMotionChain("hair", new[] { childId }, Array.Empty<int>()) }, null, null);
            var bytes = SecondaryMotionCodec.Write(asset); var reopened = SecondaryMotionCodec.Read(bytes);
            Equal(asset.ContentHash, reopened.ContentHash); Equal(asset.Profile.PackageVersion, reopened.Profile.PackageVersion); Equal(3, reopened.Profile.AdapterPayload.Count);
            var unknown = (byte[])bytes.Clone(); BitConverter.GetBytes(99).CopyTo(unknown, 4);
            var document = SecondaryMotionCodec.ReadDocument(unknown); False(document.IsSupported); Equal(99, document.WireVersion); True(document.RawBytes.SequenceEqual(unknown)); Equal(ChecksHashForTest(unknown), document.RawHash);
            Expect("UNSUPPORTED_FORMAT", () => SecondaryMotionCodec.Read(unknown));
            var capabilities = new SecondaryMotionCapabilities("test.adapter", "test.simulator", "1", "pkg", SecondaryMotionOutputKind.BonePose, true, true, new[] { "reset", "step" });
            True(capabilities.Accepts(reopened.Profile)); True(SecondaryMotionProfile.FromCapabilities(capabilities, 1, Array.Empty<byte>()).OutputKind == SecondaryMotionOutputKind.BonePose);
            False(capabilities.Accepts(new SecondaryMotionProfile("other", "test.simulator", 1, "1", "pkg", SecondaryMotionOutputKind.BonePose, Array.Empty<byte>())));
        });

        Test("secondary motion session table keeps assets attached to their graph", () =>
        {
            var skeleton = BuildSecondarySkeleton(out _, out var childId); var mesh = AuthoringFixtures.Panel(1);
            var profile = new SecondaryMotionProfile("test.adapter", "test.simulator", 1, "1", "", SecondaryMotionOutputKind.BonePose, Array.Empty<byte>());
            var first = new SecondaryMotionAsset(profile, skeleton.ContentHash, mesh.TopologyHash,
                new[] { new SecondaryMotionChain("first", new[] { childId }, Array.Empty<int>()) }, null, null);
            var second = new SecondaryMotionAsset(profile, skeleton.ContentHash, mesh.TopologyHash,
                new[] { new SecondaryMotionChain("second", new[] { childId }, Array.Empty<int>()) }, null, null);
            string graphA = Guid.NewGuid().ToString("D"), graphB = Guid.NewGuid().ToString("D");
            var bytes = SecondaryMotionSessionsCodec.Write(new Dictionary<string, SecondaryMotionAsset> { [graphA] = first, [graphB] = second });
            True(SecondaryMotionSessionsCodec.IsTable(bytes));
            var reopened = SecondaryMotionSessionsCodec.Read(bytes);
            Equal(2, reopened.Count); Equal(first.ContentHash, reopened[graphA].ContentHash); Equal(second.ContentHash, reopened[graphB].ContentHash);
            var tampered = (byte[])bytes.Clone(); BitConverter.GetBytes(99).CopyTo(tampered, 4);
            Expect("UNSUPPORTED_FORMAT", () => SecondaryMotionSessionsCodec.Read(tampered));
        });

        Test("resolved spring data migrates to stable secondary motion topology", () =>
        {
            var skeleton = BuildSecondarySkeleton(out var rootId, out var childId); var mesh = AuthoringFixtures.Panel(1);
            var settings = new SpringBoneJointSettings(childId, .02f, .5f, .1f, new Vec3(0, -1, 0), .2f);
            var sourceChain = new SpringBoneChain("legacy hair", new[] { settings }, new[] { 0 });
            var sourceGroup = new SpringBoneColliderGroup("legacy body", new[] { new SpringBoneCollider(new Vec3(0, 0, 0), .1f) });
            var profile = new SecondaryMotionProfile("nyaforge.vrm-spring", "vrm1", 1, "1", "", SecondaryMotionOutputKind.BonePose, Array.Empty<byte>());
            var asset = VrmSecondaryMotionMigration.FromSpringChains(profile, skeleton.ContentHash, mesh.TopologyHash, new[] { sourceChain }, new[] { sourceGroup });
            asset.ValidateFor(skeleton, mesh); Equal(childId, asset.Chains[0].BoneIds[0]); Equal(1, asset.ColliderGroups[0].Colliders.Count); Equal(0, asset.Chains[0].ColliderGroupIndices[0]);
            Equal(rootId, skeleton.Bones[0].BoneId);
        });

        Test("VRM1 spring session migration resolves source nodes to authored BoneIds", () =>
        {
            var json = JObject.Parse(ReadJsonChunk(BuildMappedVrm(false)));
            json["extensions"]!["VRMC_springBone"] = new JObject {
                ["specVersion"] = "1.0",
                ["colliders"] = new JArray(new JObject { ["node"] = 1, ["shape"] = new JObject { ["sphere"] = new JObject { ["offset"] = new JArray(0, 0, 0), ["radius"] = .1f } } }),
                ["colliderGroups"] = new JArray(new JObject { ["name"] = "body", ["colliders"] = new JArray(0) }),
                ["springs"] = new JArray(new JObject {
                    ["name"] = "hair", ["colliderGroups"] = new JArray(0),
                    ["joints"] = new JArray(
                        new JObject { ["node"] = 1, ["hitRadius"] = .02f, ["stiffness"] = .5f, ["gravityPower"] = .1f, ["dragForce"] = .2f, ["gravityDir"] = new JArray(0, -1, 0) },
                        new JObject { ["node"] = 2, ["hitRadius"] = .02f, ["stiffness"] = .5f, ["gravityPower"] = .1f, ["dragForce"] = .2f, ["gravityDir"] = new JArray(0, -1, 0) })
                })
            };
            var bytes = ReplaceJsonChunk(BuildMappedVrm(false), json.ToString()); var imported = GlbSkinImporter.Read(bytes); var metadata = VrmMetadataReader.Read(bytes);
            string skeletonId = Guid.NewGuid().ToString("D"), sourceId = Guid.NewGuid().ToString("D"), outputId = Guid.NewGuid().ToString("D");
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] { GraphNode.SkeletonNode(skeletonId, imported.Skeleton), GraphNode.Source(sourceId, imported.Mesh, new RestTransform(1, new Vec3())), GraphNode.Output(outputId) }, new[] { new GraphEdge(sourceId, "mesh", outputId, "mesh") }, outputId);
            var rig = ImportedRigSession.Create(imported, metadata, graph.GraphId, skeletonId);
            var pose = PoseSet.Create(imported.Skeleton, imported.Skeleton.Bones.Select(bone => new BonePose(bone.BoneId, PoseTransform.FromTranslation(bone.Head))));
            var asset = VrmSecondaryMotionMigration.FromVrm1(VrmSpringSession.Create(metadata), rig, graph, pose);
            asset.ValidateFor(imported.Skeleton, imported.Mesh); Equal(1, asset.Chains.Count); Equal(1, asset.Chains[0].BoneIds.Count); Equal(rig.NodeToBone[1], asset.Chains[0].BoneIds[0]); Equal(1, asset.ColliderGroups[0].Colliders.Count);
        });

        Test("VRM0 spring session migration expands source nodes into authored BoneIds", () =>
        {
            var bytes = BuildMappedVrm(true); var json = JObject.Parse(ReadJsonChunk(bytes));
            json["extensions"]["VRM"]["secondaryAnimation"] = new JObject {
                ["colliderGroups"] = new JArray(),
                ["boneGroups"] = new JArray(new JObject { ["comment"] = "legacy hair", ["bones"] = new JArray(2), ["center"] = -1, ["colliderGroups"] = new JArray(), ["stiffiness"] = .5f, ["gravityPower"] = .1f, ["gravityDir"] = new JObject { ["x"] = 0, ["y"] = -1, ["z"] = 0 } })
            };
            bytes = ReplaceJsonChunk(bytes, json.ToString()); var imported = GlbSkinImporter.Read(bytes); var metadata = VrmMetadataReader.Read(bytes);
            string skeletonId = Guid.NewGuid().ToString("D"), sourceId = Guid.NewGuid().ToString("D"), poseId = Guid.NewGuid().ToString("D"), outputId = Guid.NewGuid().ToString("D");
            var pose = PoseSet.Create(imported.Skeleton, imported.Skeleton.Bones.Select(bone => new BonePose(bone.BoneId, PoseTransform.FromTranslation(bone.Head))));
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] { GraphNode.SkeletonNode(skeletonId, imported.Skeleton), GraphNode.Source(sourceId, imported.Mesh, new RestTransform(1, new Vec3())), GraphNode.PoseNode(poseId, pose), GraphNode.Output(outputId) }, new[] { new GraphEdge(sourceId, "mesh", outputId, "mesh") }, outputId);
            var rig = ImportedRigSession.Create(imported, metadata, graph.GraphId, skeletonId);
            var asset = VrmSecondaryMotionMigration.FromVrm0(VrmSpringSession.Create(metadata), rig, graph, pose);
            asset.ValidateFor(imported.Skeleton, imported.Mesh); Equal("vrm0", asset.Profile.SimulatorId); Equal(1, asset.Chains.Count); Equal(rig.NodeToBone[2], asset.Chains[0].BoneIds[0]);
        });

        Test("secondary-motion rebind requires explicit stable maps and repins identities", () =>
        {
            var sourceSkeleton = BuildSecondarySkeleton(out var sourceRoot, out var sourceChild); var targetSkeleton = BuildSecondarySkeleton(out var targetRoot, out var targetChild); var mesh = AuthoringFixtures.Panel(1);
            var profile = new SecondaryMotionProfile("test.adapter", "test.simulator", 1, "1", "", SecondaryMotionOutputKind.BonePose, Array.Empty<byte>());
            var source = new SecondaryMotionAsset(profile, sourceSkeleton.ContentHash, mesh.TopologyHash,
                new[] { new SecondaryMotionChain("hair", new[] { sourceChild }, Array.Empty<int>()) },
                new[] { new SecondaryMotionColliderGroup("body", new[] { new SecondaryMotionCollider(sourceRoot, new Vec3(), .1f) }) }, null);
            Expect("SIMULATION_REBIND_REQUIRED", () => SecondaryMotionRebind.Apply(source, targetSkeleton, mesh, null));
            var rebound = SecondaryMotionRebind.Apply(source, targetSkeleton, mesh, new Dictionary<string, string> { [sourceRoot] = targetRoot, [sourceChild] = targetChild });
            rebound.ValidateFor(targetSkeleton, mesh); Equal(targetSkeleton.ContentHash, rebound.SkeletonHash); Equal(targetChild, rebound.Chains[0].BoneIds[0]); Equal(targetRoot, rebound.ColliderGroups[0].Colliders[0].BoneId);
            Expect("SIMULATION_REBIND_AMBIGUOUS", () => SecondaryMotionRebind.Apply(source, targetSkeleton, mesh, new Dictionary<string, string> { [sourceRoot] = targetRoot, [sourceChild] = targetRoot }));
            var meshProfile = new SecondaryMotionProfile("test.mesh.adapter", "test.simulator", 1, "1", "", SecondaryMotionOutputKind.MeshDeformation, Array.Empty<byte>());
            var meshSource = new SecondaryMotionAsset(meshProfile, "", mesh.TopologyHash, null, null, new[] { 0, 1 });
            var changedMesh = new MeshData(mesh.Positions.ToArray(), mesh.Normals.ToArray(), mesh.Tangents.ToArray(), mesh.Uv0.ToArray(), mesh.Submeshes.Select(indices => indices.Reverse().ToArray()).ToArray());
            Expect("SIMULATION_REBIND_REQUIRED", () => SecondaryMotionRebind.Apply(meshSource, null, changedMesh, null));
            var meshRebound = SecondaryMotionRebind.Apply(meshSource, null, changedMesh, new Dictionary<string, string>(), new Dictionary<int, int> { [0] = 2, [1] = 3 });
            Equal(changedMesh.TopologyHash, meshRebound.MeshTopologyHash); True(meshRebound.FixedVertexIndices.SequenceEqual(new[] { 2, 3 }));
        });

        Test("PhysBones target DTO roundtrips stable mapping and preserves endpoint data", () =>
        {
            var skeleton = BuildPhysBonesSkeleton(out var rootId, out var childId, out var excludedId);
            var curve = new PhysBonesCurve(PhysBonesCurveChannel.Radius, new[] { new PhysBonesCurveKey(0, 0), new PhysBonesCurveKey(1, .25f) });
            var chain = new PhysBonesChain("tail", rootId, new[] { rootId }, PhysBonesEndpointMode.Position, "", new Vec3(0, .4f, 0), PhysBonesMultiChildType.Ignore, new[] { excludedId }, null, null, PhysBonesParameters.Default, PhysBonesInteraction.Default, new[] { curve });
            var profile = new PhysBonesTargetProfile("vrchat.physbones", "sdk-3", "package-1", skeleton.ContentHash, "", new[] { chain });
            profile.ValidateFor(null, skeleton); var reopened = PhysBonesTargetCodec.Read(PhysBonesTargetCodec.Write(profile));
            Equal(profile.ContentHash, reopened.ContentHash); Equal(profile.SkeletonHash, reopened.SkeletonHash); Equal(PhysBonesEndpointMode.Position, reopened.Chains[0].EndpointMode); True(reopened.Chains[0].EndpointPosition.HasValue); Equal(excludedId, reopened.Chains[0].ExcludedBoneIds[0]); Equal(2, reopened.Chains[0].Curves[0].Keys.Count);
            var unknown = PhysBonesTargetCodec.Write(profile); BitConverter.GetBytes(99).CopyTo(unknown, 4); var document = PhysBonesTargetCodec.ReadDocument(unknown); False(document.IsSupported); Equal(99, document.WireVersion); True(document.RawBytes.SequenceEqual(unknown)); Expect("UNSUPPORTED_FORMAT", () => PhysBonesTargetCodec.Read(unknown));
        });

        Test("PhysBones loss report separates supported values from unsupported target features", () =>
        {
            var skeleton = BuildPhysBonesSkeleton(out var rootId, out _, out _); var chain = new PhysBonesChain("tail", rootId, new[] { rootId }, PhysBonesEndpointMode.Auto, "", null, PhysBonesMultiChildType.Ignore, null, null, null, PhysBonesParameters.Default, PhysBonesInteraction.Default, new[] { new PhysBonesCurve(PhysBonesCurveChannel.Stiffness, new[] { new PhysBonesCurveKey(0, .5f) }) });
            var profile = new PhysBonesTargetProfile("vrchat.physbones", "sdk-profile", "", skeleton.ContentHash, "", new[] { chain });
            var capabilities = new PhysBonesCapabilities("vrchat.physbones", "sdk-installed", new[] { PhysBonesFeatures.Root, PhysBonesFeatures.Limits, PhysBonesFeatures.Interaction }); var report = PhysBonesLossReport.Compare(profile, capabilities);
            False(report.IsLossless); True(report.Supported.Any(item => item.Path == PhysBonesFeatures.Root)); True(report.Unsupported.Any(item => item.Path == PhysBonesFeatures.Curves)); True(report.Warnings.Any(item => item.Code == "SDK_VERSION_MISMATCH")); True(report.ContentHash.Length == 64);
        });

        Test("PhysBones target rejects unstable or stale bone mappings", () =>
        {
            var skeleton = BuildPhysBonesSkeleton(out var rootId, out var childId, out _); Expect("INVALID_PHYSBONES", () => new PhysBonesChain("bad", rootId, new[] { rootId, rootId }, PhysBonesEndpointMode.Auto, "", null, PhysBonesMultiChildType.Ignore, null, null, null, PhysBonesParameters.Default, PhysBonesInteraction.Default, null));
            var chain = new PhysBonesChain("tail", rootId, new[] { rootId, childId }, PhysBonesEndpointMode.Auto, "", null, PhysBonesMultiChildType.Ignore, null, null, null, PhysBonesParameters.Default, PhysBonesInteraction.Default, null); var profile = new PhysBonesTargetProfile("vrchat.physbones", "sdk", "", skeleton.ContentHash, "", new[] { chain }); var changed = BuildSecondarySkeleton(out _, out _); Expect("SIMULATION_SKELETON_CHANGED", () => profile.ValidateFor(null, changed));
        });
    }

    static SkeletonDefinition BuildSecondarySkeleton(out string rootId, out string childId)
    {
        rootId = Guid.NewGuid().ToString("D"); childId = Guid.NewGuid().ToString("D");
        return new SkeletonDefinition(new[]
        {
            new BoneDefinition(rootId, "Root", "", new Vec3(0, 0, 0), new Vec3(0, 1, 0)),
            new BoneDefinition(childId, "Child", rootId, new Vec3(0, 1, 0), new Vec3(0, 2, 0))
        });
    }

    static SkeletonDefinition BuildPhysBonesSkeleton(out string rootId, out string childId, out string excludedId)
    {
        rootId = Guid.NewGuid().ToString("D"); childId = Guid.NewGuid().ToString("D"); excludedId = Guid.NewGuid().ToString("D");
        return new SkeletonDefinition(new[]
        {
            new BoneDefinition(rootId, "Root", "", new Vec3(0, 0, 0), new Vec3(0, 1, 0)),
            new BoneDefinition(childId, "Child", rootId, new Vec3(0, 1, 0), new Vec3(0, 2, 0)),
            new BoneDefinition(excludedId, "Excluded", rootId, new Vec3(.1f, 1, 0), new Vec3(.1f, 2, 0))
        });
    }
}
