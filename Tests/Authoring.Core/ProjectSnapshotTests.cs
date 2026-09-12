using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Simulation;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    static ProjectAttachments SnapshotMetadata(string suffix) => new ProjectAttachments(new Dictionary<string, byte[]>
    {
        [ProjectAttachments.Expressions] = Encoding.UTF8.GetBytes("expression-" + suffix),
        [ProjectAttachments.Springs] = Encoding.UTF8.GetBytes("spring-" + suffix),
        [ProjectAttachments.Rig] = Encoding.UTF8.GetBytes("rig-" + suffix)
    });

    static void RunProjectSnapshotTests()
    {
        Test("metadata rebind participates in Undo and Redo", () =>
        {
            var w = Fresh(); var original = SnapshotMetadata("before"); var rebound = SnapshotMetadata("after");
            w.SetAttachments(original); string geometry = w.Document.StateHash;
            w.SetAttachmentsWithHistory(rebound);
            Equal(rebound.ContentHash, w.Attachments.ContentHash); Equal(geometry, w.Document.StateHash);
            Ok(Execute(w, AuthoringOperation.Undo())); Equal(original.ContentHash, w.Attachments.ContentHash); Equal(geometry, w.Document.StateHash);
            Ok(Execute(w, AuthoringOperation.Redo())); Equal(rebound.ContentHash, w.Attachments.ContentHash); Equal(geometry, w.Document.StateHash);
        });

        Test("observed save service preserves graph snapshot metadata and stale writer protection", () =>
        {
            var w = AuthoringWorkspace.CreateEmpty();
            string plane = Guid.NewGuid().ToString("D"), output = Guid.NewGuid().ToString("D");
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] { GraphNode.Plane(plane), GraphNode.Output(output) }, new[] { new GraphEdge(plane, "mesh", output, "mesh") }, output);
            Ok(Execute(w, AuthoringOperation.AddGraph(graph))); w.SetAttachments(SnapshotMetadata("graph"));
            string directory = Dir("snapshot-observed-graph");
            ProjectSaveService.Save(w, new ProjectSaveRequest(w.InstanceId, w.Document.DocumentId, w.Document.DocumentRevision, directory, 0));
            var loaded = ProjectStore.Open(directory); var stale = ProjectStore.Open(directory);
            Equal(w.Document.StateHash, loaded.Document.StateHash); Equal(w.Attachments.ContentHash, loaded.Attachments.ContentHash);
            ProjectSaveService.Save(loaded, new ProjectSaveRequest(loaded.InstanceId, loaded.Document.DocumentId, loaded.Document.DocumentRevision, directory, 1));
            Expect("SAVE_CONFLICT", () => ProjectStore.Save(directory, stale, 1));
            Equal(w.Attachments.ContentHash, ProjectStore.Open(directory).Attachments.ContentHash);
        });

        Test("PhysBones target attachment survives schema 4 snapshot Open", () =>
        {
            var skeleton = BuildPhysBonesSkeleton(out var rootId, out _, out _);
            var chain = new PhysBonesChain("tail", rootId, new[] { rootId }, PhysBonesEndpointMode.Auto, "", null, PhysBonesMultiChildType.Ignore, null, null, null, PhysBonesParameters.Default, PhysBonesInteraction.Default, null);
            var profile = new PhysBonesTargetProfile("vrchat.physbones", "sdk", "package", skeleton.ContentHash, "", new[] { chain });
            var bytes = PhysBonesTargetCodec.Write(profile); var w = Fresh(); w.SetAttachments(new ProjectAttachments(new Dictionary<string, byte[]> { [ProjectAttachments.PhysBones] = bytes })); string directory = Dir("snapshot-physbones"); ProjectStore.Save(directory, w, 0);
            var opened = ProjectStore.Open(directory); var restored = opened.Attachments.Read(ProjectAttachments.PhysBones); True(bytes.SequenceEqual(restored)); Equal(profile.ContentHash, PhysBonesTargetCodec.Read(restored).ContentHash); False(opened.IsDirty); Equal(6, ProjectAttachments.MaxCount);
        });

        Test("secondary motion attachment survives schema 4 snapshot Open", () =>
        {
            var skeleton = BuildSecondarySkeleton(out _, out var childId); var mesh = AuthoringFixtures.Panel(1);
            var profile = new SecondaryMotionProfile("test.adapter", "test.simulator", 1, "1", "", SecondaryMotionOutputKind.BonePose, Array.Empty<byte>());
            var asset = new SecondaryMotionAsset(profile, skeleton.ContentHash, mesh.TopologyHash, new[] { new SecondaryMotionChain("hair", new[] { childId }, Array.Empty<int>()) }, null, null);
            var bytes = SecondaryMotionCodec.Write(asset); var w = Fresh(); w.SetAttachments(new ProjectAttachments(new Dictionary<string, byte[]> { [ProjectAttachments.SecondaryMotion] = bytes })); string directory = Dir("snapshot-secondary-motion"); ProjectStore.Save(directory, w, 0);
            var opened = ProjectStore.Open(directory); var restored = opened.Attachments.Read(ProjectAttachments.SecondaryMotion); True(bytes.SequenceEqual(restored)); Equal(asset.ContentHash, SecondaryMotionCodec.Read(restored).ContentHash); False(opened.IsDirty);
        });

        foreach (string failedName in new[] { ProjectAttachments.Expressions, ProjectAttachments.Springs, ProjectAttachments.Rig })
            Test("snapshot failed blob preserves old document and metadata: " + failedName, () =>
            {
                string directory = Dir("snapshot-" + failedName); var w = Fresh();
                var oldMetadata = SnapshotMetadata("old"); w.SetAttachments(oldMetadata); ProjectStore.Save(directory, w, 0);
                string manifest = File.ReadAllText(Path.Combine(directory, ProjectStore.ManifestName));
                string oldMesh = w.Evaluate().ContentHash;
                Ok(Edit(w, 0, new Vec3(.01f, 0, 0)));
                var nextMetadata = SnapshotMetadata("new"); w.SetAttachments(nextMetadata);
                string blocked = Path.Combine(directory, "blobs", nextMetadata.Hashes[failedName] + ".bin");
                using (var handle = new FileStream(blocked, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    var bytes = nextMetadata.Read(failedName); handle.Write(bytes, 0, bytes.Length); handle.Flush(true);
                    bool failed = false; try { ProjectStore.Save(directory, w, 1); } catch (IOException) { failed = true; }
                    True(failed); True(w.IsDirty); Equal(1L, w.SaveVersion);
                    Equal(manifest, File.ReadAllText(Path.Combine(directory, ProjectStore.ManifestName)));
                    var reopened = ProjectStore.Open(directory);
                    Equal(oldMesh, reopened.Evaluate().ContentHash); Equal(oldMetadata.ContentHash, reopened.Attachments.ContentHash);
                }
                ProjectStore.Save(directory, w, 1); False(w.IsDirty); Equal(2L, w.SaveVersion);
                var saved = ProjectStore.Open(directory); Equal(w.Evaluate().ContentHash, saved.Evaluate().ContentHash); Equal(nextMetadata.ContentHash, saved.Attachments.ContentHash);
            });

        Test("legacy metadata migrates and removed attachments never resurrect", () =>
        {
            string directory = Dir("snapshot-legacy"); var w = Fresh(); ProjectStore.Save(directory, w, 0);
            var bytes = Encoding.UTF8.GetBytes("legacy-owned-metadata");
            File.WriteAllBytes(Path.Combine(directory, ProjectAttachments.Springs), bytes);
            var loaded = ProjectStore.Open(directory); False(loaded.IsDirty); True(bytes.SequenceEqual(loaded.Attachments.Read(ProjectAttachments.Springs)));
            loaded.SetAttachments(ProjectAttachments.Empty); True(loaded.IsDirty); ProjectStore.Save(directory, loaded, 1);
            Equal(4, JObject.Parse(File.ReadAllText(Path.Combine(directory, ProjectStore.ManifestName))).Value<int>("schemaVersion"));
            File.WriteAllText(Path.Combine(directory, ProjectAttachments.Springs), "stale");
            var reopened = ProjectStore.Open(directory); Equal(0, reopened.Attachments.Hashes.Count); False(reopened.IsDirty);
        });

        Test("snapshot Save As failure can retry without advancing source version", () =>
        {
            var w = Fresh(); string original = Dir("snapshot-source"), destination = Dir("snapshot-copy");
            ProjectStore.Save(original, w, 0); w.SetAttachments(SnapshotMetadata("copy"));
            Directory.CreateDirectory(Path.Combine(destination, ProjectStore.ManifestName));
            bool failed = false; try { ProjectStore.Save(destination, w, 0); } catch (IOException) { failed = true; }
            True(failed); True(w.IsDirty); Equal(1L, w.SaveVersion); False(File.Exists(Path.Combine(destination, ProjectStore.ManifestName)));
            Equal(0, ProjectStore.Open(original).Attachments.Hashes.Count);
            Directory.Delete(Path.Combine(destination, ProjectStore.ManifestName));
            ProjectStore.Save(destination, w, 0); Equal(1L, w.SaveVersion); False(w.IsDirty);
            Equal(w.Attachments.ContentHash, ProjectStore.Open(destination).Attachments.ContentHash);
        });

        Test("snapshot rejects corrupted metadata blobs and unknown envelope fields", () =>
        {
            string directory = Dir("snapshot-corrupt"); var w = Fresh(); w.SetAttachments(SnapshotMetadata("valid")); ProjectStore.Save(directory, w, 0);
            string path = Path.Combine(directory, ProjectStore.ManifestName); string text = File.ReadAllText(path);
            var root = JObject.Parse(text); root["unexpected"] = true; File.WriteAllText(path, root.ToString());
            Expect("INVALID_MANIFEST", () => ProjectStore.Open(directory)); File.WriteAllText(path, text);
            File.WriteAllText(Path.Combine(directory, "blobs", w.Attachments.Hashes[ProjectAttachments.Springs] + ".bin"), "corrupted");
            Expect("HASH_MISMATCH", () => ProjectStore.Open(directory));
        });
    }
}
