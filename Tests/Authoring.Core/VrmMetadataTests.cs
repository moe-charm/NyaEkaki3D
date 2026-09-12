using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring.Import;

internal static partial class Program
{
    static void RunVrmMetadataTests()
    {
        Test("VRM 1.0 metadata reader keeps source identity and humanoid node mapping", () =>
        {
            var root = JObject.Parse(ReadJsonChunk(BuildGlb())); root["nodes"] = new JArray(new JObject { ["name"] = "Hips", ["mesh"] = 0 }); var primitive = (JObject)((JArray)root["meshes"]![0]!["primitives"]!)[0]!; ((JArray)primitive["targets"]!).Add(new JObject { ["POSITION"] = 2 }); root["extensions"] = new JObject { ["VRMC_vrm"] = new JObject { ["specVersion"] = "1.0", ["meta"] = new JObject { ["name"] = "Sample", ["authors"] = "Nya" }, ["humanoid"] = new JObject { ["humanBones"] = new JObject { ["hips"] = new JObject { ["node"] = 0 } } }, ["expressions"] = new JObject { ["preset"] = new JObject { ["happy"] = new JObject { ["morphTargetBinds"] = new JArray(new JObject { ["node"] = 0, ["index"] = 0, ["weight"] = 0.5 }) } }, ["custom"] = new JObject { ["smile"] = new JObject { ["morphTargetBinds"] = new JArray(new JObject { ["node"] = 0, ["index"] = 0, ["weight"] = 0.25 }), ["materialColorBinds"] = new JArray(new JObject { ["material"] = 0 }) } } } } };
            var bytes = ReplaceJsonChunk(BuildGlb(), root.ToString(Newtonsoft.Json.Formatting.None)); var profile = VrmMetadataReader.Read(bytes);
            Equal("vrm1", profile.Format); Equal("1.0", profile.SpecVersion); Equal("Sample", profile.Title); Equal(0, profile.HumanoidNodes["hips"]); Equal(2, profile.Expressions.Count); Equal("happy", profile.Expressions[0].Name); Equal(1, profile.Expressions[0].MorphTargetBindCount); Equal(0, profile.Expressions[0].MaterialBindCount); Equal(0, profile.Expressions[0].MorphBindings[0].OwnerIndex); Equal(0, profile.Expressions[0].MorphBindings[0].MorphIndex); Near(.5f, profile.Expressions[0].MorphBindings[0].Weight); Equal("smile", profile.Expressions[1].Name); True(profile.Expressions[1].IsCustom); Equal(1, profile.Expressions[1].MorphTargetBindCount); Equal(1, profile.Expressions[1].MaterialBindCount); True(VrmMetadataReader.ContainsVrm(bytes));
            var imported = GlbImporter.Read(bytes); var mapped = VrmExpressionMapper.ResolveForImportedMesh(bytes, profile, imported.Morphs); Equal(2, imported.Morphs.Targets.Count); Equal(1, mapped[0].Weights.Count); True(mapped[0].Weights.ContainsKey(GlbImporter.MorphTargetId(profile.SourceHash, 0))); Near(.5f, mapped[0].Weights.Values.Single());
            var session = VrmExpressionSession.Create(profile, mapped); var sessionBytes = VrmExpressionSessionCodec.Write(session); var reopened = VrmExpressionSessionCodec.Read(sessionBytes); Equal(profile.SourceHash, reopened.SourceHash); Equal(2, reopened.Expressions.Count); Near(.5f, reopened.Expressions[0].Weights.Values.Single()); True(sessionBytes.SequenceEqual(VrmExpressionSessionCodec.Write(reopened)));
        });

        Test("VRM 0.x metadata reader accepts legacy humanBones array", () =>
        {
            var root = JObject.Parse(ReadJsonChunk(BuildGlb())); root["nodes"] = new JArray(new JObject { ["name"] = "Hips" }); root["extensions"] = new JObject { ["VRM"] = new JObject { ["specVersion"] = "0.0", ["meta"] = new JObject { ["title"] = "Legacy", ["author"] = "Nya" }, ["humanoid"] = new JObject { ["humanBones"] = new JArray(new JObject { ["bone"] = "hips", ["node"] = 0 }) }, ["blendShapeMaster"] = new JObject { ["blendShapeGroups"] = new JArray(new JObject { ["name"] = "Joy", ["presetName"] = "happy", ["binds"] = new JArray(new JObject { ["mesh"] = 0, ["index"] = 0, ["weight"] = 50.0 }), ["materialValues"] = new JArray(new JObject { ["materialName"] = "body" }) }) } } };
            var profile = VrmMetadataReader.Read(ReplaceJsonChunk(BuildGlb(), root.ToString(Newtonsoft.Json.Formatting.None))); Equal("vrm0", profile.Format); Equal("Legacy", profile.Title); Equal(0, profile.HumanoidNodes["hips"]); Equal(1, profile.Expressions.Count); Equal("Joy", profile.Expressions[0].Name); Equal("happy", profile.Expressions[0].Preset); Equal(1, profile.Expressions[0].MorphTargetBindCount); Equal(1, profile.Expressions[0].MaterialBindCount); Near(.5f, profile.Expressions[0].MorphBindings[0].Weight);
        });

        Test("VRM metadata reader refuses absent and unsupported extensions", () =>
        {
            Expect("UNSUPPORTED_FORMAT", () => VrmMetadataReader.Read(BuildGlb())); var root = JObject.Parse(ReadJsonChunk(BuildGlb())); root["extensions"] = new JObject { ["VRMC_vrm"] = new JObject { ["specVersion"] = "2.0", ["meta"] = new JObject(), ["humanoid"] = new JObject() } }; Expect("UNSUPPORTED_FORMAT", () => VrmMetadataReader.Read(ReplaceJsonChunk(BuildGlb(), root.ToString(Newtonsoft.Json.Formatting.None))));
        });

        Test("VRM SpringBone inventory reads bounded VRM 1.0 and 0.x chains", () =>
        {
            var modernRoot = JObject.Parse(ReadJsonChunk(BuildGlb())); modernRoot["nodes"] = new JArray(new JObject { ["name"] = "Root" }, new JObject { ["name"] = "Tail" }, new JObject { ["name"] = "TailTip" });
            var modernVrm = new JObject { ["specVersion"] = "1.0", ["meta"] = new JObject(), ["humanoid"] = new JObject { ["humanBones"] = new JObject { ["hips"] = new JObject { ["node"] = 0 } } } };
            var modernSpring = new JObject { ["specVersion"] = "1.0", ["colliders"] = new JArray(new JObject { ["node"] = 0, ["shape"] = new JObject { ["sphere"] = new JObject { ["radius"] = 0.2 } } }), ["colliderGroups"] = new JArray(new JObject { ["name"] = "body", ["colliders"] = new JArray(0) }), ["springs"] = new JArray(new JObject { ["name"] = "tail", ["joints"] = new JArray(new JObject { ["node"] = 1, ["hitRadius"] = 0.05, ["dragForce"] = 0.2 }, new JObject { ["node"] = 2 }), ["colliderGroups"] = new JArray(0), ["center"] = 0 }) };
            modernRoot["extensions"] = new JObject { ["VRMC_vrm"] = modernVrm, ["VRMC_springBone"] = modernSpring };
            var modern = VrmMetadataReader.Read(ReplaceJsonChunk(BuildGlb(), modernRoot.ToString(Newtonsoft.Json.Formatting.None))); Equal(1, modern.SpringBones.Count); Equal(2, modern.SpringBones[0].Joints.Count); Equal(1, modern.SpringColliderGroups.Count); Equal(1, modern.SpringColliderGroups[0].ColliderCount); Equal(0, modern.SpringBones[0].CenterNodeIndex);
            var session = VrmSpringSession.Create(modern); var sessionBytes = VrmSpringSessionCodec.Write(session); var reopenedSession = VrmSpringSessionCodec.Read(sessionBytes); Equal(modern.SourceHash, reopenedSession.SourceHash); Equal(1, reopenedSession.SpringBones.Count); Equal(2, reopenedSession.SpringBones[0].Joints.Count); Equal(1, reopenedSession.ColliderGroups[0].ColliderNodeIndices.Count); Equal(0, reopenedSession.ColliderGroups[0].ColliderNodeIndices[0]); True(sessionBytes.SequenceEqual(VrmSpringSessionCodec.Write(reopenedSession)));

            var legacyRoot = JObject.Parse(ReadJsonChunk(BuildGlb())); legacyRoot["nodes"] = new JArray(new JObject { ["name"] = "Root" }, new JObject { ["name"] = "Tail" });
            var legacyVrm = new JObject { ["specVersion"] = "0.0", ["meta"] = new JObject(), ["humanoid"] = new JObject { ["humanBones"] = new JArray(new JObject { ["bone"] = "hips", ["node"] = 0 }) } };
            var legacySecondary = new JObject { ["colliderGroups"] = new JArray(new JObject { ["node"] = 0, ["colliders"] = new JArray(new JObject { ["offset"] = new JObject { ["x"] = 0, ["y"] = 0, ["z"] = 0 }, ["radius"] = 0.1 }) }), ["boneGroups"] = new JArray(new JObject { ["comment"] = "tail", ["bones"] = new JArray(1), ["colliderGroups"] = new JArray(0), ["stiffiness"] = 1.0, ["dragForce"] = 0.3, ["hitRadius"] = 0.05 }) };
            legacyVrm["secondaryAnimation"] = legacySecondary; legacyRoot["extensions"] = new JObject { ["VRM"] = legacyVrm };
            var legacy = VrmMetadataReader.Read(ReplaceJsonChunk(BuildGlb(), legacyRoot.ToString(Newtonsoft.Json.Formatting.None))); Equal(1, legacy.SpringBones.Count); Equal(1, legacy.SpringBones[0].RootBoneNodes.Count); Equal(1, legacy.SpringColliderGroups[0].ColliderCount); Equal("tail", legacy.SpringBones[0].Name);
        });
    }
}
