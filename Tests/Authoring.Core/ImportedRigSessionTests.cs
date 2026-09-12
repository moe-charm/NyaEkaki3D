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
            var session = ImportedRigSession.Create(source, metadata, graph.GraphId, skeletonId);
            var payload = ImportedRigSessionCodec.Write(session);
            var w = AuthoringWorkspace.CreateEmpty(); Ok(Execute(w, AuthoringOperation.AddGraph(graph)));
            w.SetAttachments(new ProjectAttachments(new Dictionary<string, byte[]> { [ProjectAttachments.Rig] = payload }));
            string directory = Dir("imported-rig-session"); ProjectStore.Save(directory, w, 0);
            var loaded = ProjectStore.Open(directory); var reopened = ImportedRigSessionCodec.Read(loaded.Attachments.Read(ProjectAttachments.Rig));
            var loadedGraph = loaded.Document.Objects[0].Graph;
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
    }
}
