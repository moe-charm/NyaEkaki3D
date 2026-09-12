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
    static void RunImportedRigSessionTests()
    {
        RunImportedNodeSpaceTests();
        Test("Imported rig identity survives snapshot Open and detects changed skeleton", () =>
        {
            var bytes = BuildMappedVrm(false); var source = GlbSkinImporter.Read(bytes); var metadata = VrmMetadataReader.Read(bytes);
            string sourceId = Guid.NewGuid().ToString("D"), skeletonId = Guid.NewGuid().ToString("D"), outputId = Guid.NewGuid().ToString("D");
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] { GraphNode.Source(sourceId, source.Mesh, new RestTransform(1, new Vec3())), GraphNode.SkeletonNode(skeletonId, source.Skeleton), GraphNode.Output(outputId) }, new[] { new GraphEdge(sourceId, "mesh", outputId, "mesh") }, outputId);
            var legacySession = ImportedRigSession.Create(source, metadata, graph.GraphId, skeletonId);
            True(ImportedRigSessionCodec.Read(ImportedRigSessionCodec.Write(legacySession)).SourceSkin == null);
            var session = legacySession.WithSourceSkin(GlbSourceSkinReader.Read(bytes));
            var candidate = GlbSourceSkinImporter.Read(bytes);
            var weightedSession = legacySession.WithSourceSkin(candidate.Skin, candidate.Binding);
            var weightedRoundTrip = ImportedRigSessionCodec.Read(ImportedRigSessionCodec.Write(weightedSession));
            True(weightedRoundTrip.SourceSkin != null && weightedRoundTrip.SourceSkinBinding != null);
            True(SourceSkinPackageCodec.Write(new SourceSkinPackage(candidate.Skin, candidate.Binding)).SequenceEqual(SourceSkinPackageCodec.Write(new SourceSkinPackage(weightedRoundTrip.SourceSkin, weightedRoundTrip.SourceSkinBinding))));
            var secondGraphId = GraphId();
            var secondSession = ImportedRigSession.Create(source, metadata, secondGraphId, skeletonId).WithSourceSkin(candidate.Skin, candidate.Binding);
            var sessions = new Dictionary<string, ImportedRigSession> { [weightedSession.GraphId] = weightedSession, [secondGraphId] = secondSession };
            var sessionsRoundTrip = ImportedRigSessionsCodec.Read(ImportedRigSessionsCodec.Write(sessions));
            Equal(2, sessionsRoundTrip.Count); True(sessionsRoundTrip[weightedSession.GraphId].SourceSkinBinding != null); Equal(secondGraphId, sessionsRoundTrip[secondGraphId].GraphId);
            var weightedJson = JObject.Parse(Encoding.UTF8.GetString(ImportedRigSessionCodec.Write(weightedSession))); Equal(5,(int)weightedJson["version"]); True(weightedJson["sourceSkinPackage"] != null);
            var payload = ImportedRigSessionCodec.Write(session);
            var w = AuthoringWorkspace.CreateEmpty(); Ok(Execute(w, AuthoringOperation.AddGraph(graph)));
            w.SetAttachments(new ProjectAttachments(new Dictionary<string, byte[]> { [ProjectAttachments.Rig] = payload, [ProjectAttachments.RigSessions] = ImportedRigSessionsCodec.Write(sessions) }));
            string directory = Dir("imported-rig-session"); ProjectStore.Save(directory, w, 0);
            var loaded = ProjectStore.Open(directory); var reopened = ImportedRigSessionCodec.Read(loaded.Attachments.Read(ProjectAttachments.Rig));
            var restoredSessions = ImportedRigSessionsCodec.Read(loaded.Attachments.Read(ProjectAttachments.RigSessions)); Equal(2, restoredSessions.Count); True(restoredSessions[weightedSession.GraphId].SourceSkinBinding != null);
            var loadedGraph = loaded.Document.Objects[0].Graph;
            True(reopened.SourceSkin != null);
            True(SourceSkinCodec.Write(session.SourceSkin).SequenceEqual(SourceSkinCodec.Write(reopened.SourceSkin)));
            Equal(source.BoneMap.Resolve(1), reopened.ResolveHumanoid(loadedGraph)["hips"]);
            True(payload.SequenceEqual(ImportedRigSessionCodec.Write(reopened)));
            var editedSkeleton = SkeletonEditing.MoveBone(source.Skeleton, source.BoneMap.Resolve(1), new Vec3(.01f, 0, 0), new Vec3(.01f, 0, 0));
            Expect("IMPORT_SKELETON_CHANGED", () => reopened.Resolve(loadedGraph.ReplaceNode(GraphNode.SkeletonNode(skeletonId, editedSkeleton))));
            // Restoring the original payload (as Undo does) makes the mapping valid again.
            Equal(source.BoneMap.Resolve(1), reopened.Resolve(loadedGraph).Resolve(1));
            Expect("IMPORT_SOURCE_CHANGED", () => reopened.ValidateSource(new string('0', 64)));
            var wrongGraph = new AuthoringGraph(Guid.NewGuid().ToString("D"), graph.Nodes.Values, graph.Edges, graph.OutputNodeId);
            Expect("IMPORT_GRAPH_CHANGED", () => reopened.Resolve(wrongGraph));
            var json = JObject.Parse(Encoding.UTF8.GetString(payload)); ((JArray)json["nodes"]).Add(json["nodes"][0].DeepClone());
            Expect("INVALID_IMPORT", () => ImportedRigSessionCodec.Read(Encoding.UTF8.GetBytes(json.ToString())));
            json = JObject.Parse(Encoding.UTF8.GetString(payload)); json["extra"] = 1;
            Expect("INVALID_IMPORT", () => ImportedRigSessionCodec.Read(Encoding.UTF8.GetBytes(json.ToString())));
        });
        Test("Imported rig v4 rejects mismatched source payload and preserves legacy unknown data", () =>
        {
            var bytes = BuildMappedVrm(false); var source = GlbSkinImporter.Read(bytes);
            var legacy = ImportedRigSession.Create(source, null, GraphId(), GraphId());
            var complete = GlbSourceSkinReader.Read(bytes);
            var payload = ImportedRigSessionCodec.Write(legacy.WithSourceSkin(complete));
            var json = JObject.Parse(Encoding.UTF8.GetString(payload)); Equal(4,(int)json["version"]);
            foreach(int version in new[] {1,2,3})
            {
                var old = (JObject)json.DeepClone(); old["version"] = version; old.Remove("sourceSkin");
                if(version < 3) old.Remove("hierarchy");
                if(version < 2) old.Remove("origins");
                var migrated = ImportedRigSessionCodec.Read(Encoding.UTF8.GetBytes(old.ToString()));
                True(migrated.SourceSkin == null);
                True(ImportedRigSessionCodec.Read(ImportedRigSessionCodec.Write(migrated)).SourceSkin == null);
            }
            var wrongHashNodes = new SourceNodeTransforms(new string('0',64), complete.Nodes.Hierarchy.Parents,
                complete.Nodes.Local, complete.Nodes.Hierarchy.Children);
            Expect("IMPORT_SOURCE_CHANGED",()=>legacy.WithSourceSkin(new SourceSkin(wrongHashNodes,0,complete.Joints,complete.InverseBindMatrices)));
            Expect("INVALID_IMPORT",()=>legacy.WithSourceSkin(new SourceSkin(complete.Nodes,0,new[] {complete.Joints[0]},null)));
            foreach(var value in new JToken[] {JValue.CreateNull(), new JValue("not base64!"), new JValue(3)})
            {
                var broken = (JObject)json.DeepClone(); broken["sourceSkin"] = value;
                Expect("INVALID_IMPORT",()=>ImportedRigSessionCodec.Read(Encoding.UTF8.GetBytes(broken.ToString())));
            }
            var altered = (JObject)json.DeepClone(); altered["sourceHash"] = new string('0',64);
            Expect("IMPORT_SOURCE_CHANGED",()=>ImportedRigSessionCodec.Read(Encoding.UTF8.GetBytes(altered.ToString())));
        });
    }
}
