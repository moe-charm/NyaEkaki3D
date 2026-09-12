using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;

internal static partial class Program
{
    static void RunImportedNodeSpaceTests()
    {
        Test("Source hierarchy validates cycles, parents, budget and deep translation chains", () =>
        {
            Expect("BONE_CYCLE", () => new ImportedSourceHierarchy(new[] { 1, 0 }, new Vec3[2]));
            Expect("INVALID_IMPORT", () => new ImportedSourceHierarchy(new[] { 0 }, new Vec3[1]));
            Expect("INVALID_IMPORT", () => new ImportedSourceHierarchy(new[] { 2 }, new Vec3[1]));
            Expect("BUDGET_EXCEEDED", () => new ImportedSourceHierarchy(new int[4097], new Vec3[4097]));
            var parents = Enumerable.Range(0, 4096).Select(i => i - 1).ToArray();
            var local = Enumerable.Repeat(new Vec3(1, 0, 0), 4096).ToArray();
            var hierarchy = ImportedSourceHierarchy.FromTranslations(parents, local);
            parents[1] = -1; local[1] = new Vec3();
            Equal(0, hierarchy.Parents[1]); SpringPointNear(new Vec3(4096, 0, 0), hierarchy.Origins[4095]);
        });
        Test("Source node offsets survive persistence independently of inverse bind heads", () =>
        {
            var bytes = BuildMappedVrm(false); var json = JObject.Parse(ReadJsonChunk(bytes));
            json["nodes"][2]["children"] = new JArray(4, 3);
            ((JArray)json["nodes"]).Add(new JObject { ["translation"] = new JArray(0, .2, 0) });
            ((JArray)json["nodes"]).Add(new JObject { ["translation"] = new JArray(.1, 0, 0) });
            json["nodes"][1]["translation"] = new JArray(1, 0, 0); json["nodes"][2]["translation"] = new JArray(0, .3, 0);
            bytes = ReplaceJsonChunk(bytes, json.ToString()); var source = GlbSkinImporter.Read(bytes);
            string skeletonId = Guid.NewGuid().ToString("D"), meshId = Guid.NewGuid().ToString("D"), output = Guid.NewGuid().ToString("D");
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] { GraphNode.SkeletonNode(skeletonId, source.Skeleton), GraphNode.Source(meshId, source.Mesh, new RestTransform(1, new Vec3())), GraphNode.Output(output) }, new[] { new GraphEdge(meshId, "mesh", output, "mesh") }, output);
            var session = ImportedRigSession.Create(source, VrmMetadataReader.Read(bytes), graph.GraphId, skeletonId);
            var w = AuthoringWorkspace.CreateEmpty(); Ok(Execute(w, AuthoringOperation.AddGraph(graph)));
            w.SetAttachments(new ProjectAttachments(new Dictionary<string, byte[]> { [ProjectAttachments.Rig] = ImportedRigSessionCodec.Write(session) }));
            string directory = Dir("source-node-space"); ProjectStore.Save(directory, w, 0); var opened = ProjectStore.Open(directory);
            var loaded = ImportedRigSessionCodec.Read(opened.Attachments.Read(ProjectAttachments.Rig)); var restoredGraph = opened.Document.Objects[0].Graph;
            Equal(4, loaded.Hierarchy.Children[2][0]); Equal(3, loaded.Hierarchy.Children[2][1]);
            Equal(5, loaded.Hierarchy.Parents.Count); Equal(2, loaded.Hierarchy.Parents[3]); Equal(2, loaded.Hierarchy.Parents[4]);
            SpringPointNear(new Vec3(1, .5f, 0), loaded.Hierarchy.Origins[3]);
            var v2 = JObject.Parse(Encoding.UTF8.GetString(ImportedRigSessionCodec.Write(loaded))); v2["version"] = 2; v2.Remove("hierarchy");
            var oldV2 = ImportedRigSessionCodec.Read(Encoding.UTF8.GetBytes(v2.ToString()));
            True(oldV2.Hierarchy == null && oldV2.SourceNodeOrigins != null);
            True(ImportedRigSessionCodec.Read(ImportedRigSessionCodec.Write(oldV2)).Hierarchy == null);
            var mismatch = JObject.Parse(Encoding.UTF8.GetString(ImportedRigSessionCodec.Write(loaded)));
            mismatch["hierarchy"][2]["origin"][0] = 99;
            Expect("INVALID_IMPORT", () => ImportedRigSessionCodec.Read(Encoding.UTF8.GetBytes(mismatch.ToString())));
            foreach (string fault in new[] { "missing", "duplicate", "field", "cycle" })
            {
                var broken = JObject.Parse(Encoding.UTF8.GetString(ImportedRigSessionCodec.Write(loaded)));
                if (fault == "missing") ((JArray)broken["hierarchy"][2]["children"]).RemoveAt(0);
                if (fault == "duplicate") ((JArray)broken["hierarchy"][2]["children"]).Add(3);
                if (fault == "field") ((JObject)broken["hierarchy"][0]).Remove("origin");
                if (fault == "cycle") { broken["hierarchy"][1]["parent"] = 2; broken["hierarchy"][2]["parent"] = 1; }
                Expect(fault == "cycle" ? "BONE_CYCLE" : "INVALID_IMPORT", () => ImportedRigSessionCodec.Read(Encoding.UTF8.GetBytes(broken.ToString())));
            }
            var child = source.BoneMap.Resolve(2);
            SpringPointNear(new Vec3(1, .3f, 0), loaded.SourceNodeOrigins[2]);
            SpringPointNear(new Vec3(0, .1f, 0), source.Skeleton.ById[child].Head);
            var pose = PoseSet.Create(source.Skeleton, source.Skeleton.Bones.Select(b => new BonePose(b.BoneId, b.BoneId == child ? PoseTransform.RotationZ(90, new Vec3(10, 20, 30)) : PoseTransform.FromTranslation(b.Head))));
            var space = new ImportedNodeSpace(loaded, restoredGraph, pose);
            SpringPointNear(new Vec3(9.6f, 21, 30.4f), space.TransformPoint(2, new Vec3(0, .2f, .4f)));
            Expect("IMPORT_BONE_UNMAPPED", () => space.TransformPoint(0, new Vec3()));
            var legacy = JObject.Parse(Encoding.UTF8.GetString(ImportedRigSessionCodec.Write(loaded))); legacy["version"] = 1; legacy.Remove("origins"); legacy.Remove("hierarchy");
            var old = ImportedRigSessionCodec.Read(Encoding.UTF8.GetBytes(legacy.ToString())); True(old.SourceNodeOrigins == null);
            var migrated = ImportedRigSessionCodec.Read(ImportedRigSessionCodec.Write(old)); True(migrated.SourceNodeOrigins == null && migrated.Hierarchy == null);
            Expect("IMPORT_NODE_SPACE_MISSING", () => new ImportedNodeSpace(migrated, restoredGraph, pose));
            var invalid = JObject.Parse(Encoding.UTF8.GetString(ImportedRigSessionCodec.Write(loaded))); ((JArray)invalid["origins"]).RemoveAt(0);
            Expect("INVALID_IMPORT", () => ImportedRigSessionCodec.Read(Encoding.UTF8.GetBytes(invalid.ToString())));
        });
    }
}
