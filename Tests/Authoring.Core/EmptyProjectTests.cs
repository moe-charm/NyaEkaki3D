using System;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    static void RunEmptyProjectTests()
    {
        Test("empty project saves and reopens without a fabricated mesh", () =>
        {
            var w = AuthoringWorkspace.CreateEmpty();
            True(w.Document.IsEmpty); Equal(0, w.Document.Objects.Count); True(w.Evaluate() == null);
            string dir = Dir("empty"); ProjectStore.Save(dir, w, 0);
            False(w.IsDirty); False(Directory.Exists(Path.Combine(dir, "blobs")));
            var reopened = ProjectStore.Open(dir);
            True(reopened.Document.IsEmpty); Equal(w.Document.StateHash, reopened.Document.StateHash);
            False(reopened.IsDirty); False(reopened.CanUndo); False(w.InstanceId == reopened.InstanceId);
            string export = Path.Combine(dir, "no-export");
            Expect("NO_EXPORTABLE_OBJECT", () => BakeStore.Export(export, reopened));
            False(Directory.Exists(export));
            Code("NO_EDITABLE_OBJECT", Execute(reopened, Move()));
        });
        Test("mesh creation is transactional and undo returns to empty", () =>
        {
            var w = AuthoringWorkspace.CreateEmpty(); string empty = w.Document.StateHash;
            var command = w.NewCommand(AuthoringOperation.AddMesh(AuthoringFixtures.Panel(100), new RestTransform(100, new Vec3())));
            var service = new AuthoringCommandService(w);
            Ok(service.Execute(command)); string id = w.Document.ObjectId;
            Ok(service.Execute(command)); Equal(1L, w.Document.DocumentRevision);
            Ok(Edit(w, 0, new Vec3(.01f, 0, 0)));
            Near(-.09f, w.Document.Transform.ToAvatarPoint(w.Evaluate().Positions[0]).X);
            Ok(Execute(w, AuthoringOperation.Undo()));
            Ok(Execute(w, AuthoringOperation.Undo()));
            True(w.Document.IsEmpty); Equal(empty, w.Document.StateHash);
            Ok(Execute(w, AuthoringOperation.Redo())); Equal(id, w.Document.ObjectId);
            Ok(Execute(w, AuthoringOperation.Redo()));
            string dir = Dir("created"); ProjectStore.Save(dir, w, 0);
            Equal(w.Evaluate().ContentHash, ProjectStore.Open(dir).Evaluate().ContentHash);
        });
        Test("failed creation batch and projection leave empty project intact", () =>
        {
            var w = AuthoringWorkspace.CreateEmpty();
            var add = AuthoringOperation.AddMesh(AuthoringFixtures.Panel(1), new RestTransform(1, new Vec3()));
            Code("INVALID_VERTEX", new AuthoringCommandService(w).Execute(w.NewCommand(add, AuthoringOperation.TranslateVertices(new[] { 999 }, new Vec3(1, 0, 0)))));
            True(w.Document.IsEmpty); False(w.CanUndo);
            var p = new ProbeProjection { ThrowCommit = true };
            Code("COMMAND_FAILED", new AuthoringCommandService(w).Execute(w.NewCommand(add), p));
            True(w.Document.IsEmpty); True(p.Prepared.RolledBack); False(w.CanUndo);
        });
        foreach (string scale in new[] { "scale1", "scale100" }) Test("frozen v1 migration preserves source and mesh " + scale, () =>
        {
            string source = Path.Combine(AppContext.BaseDirectory, "Fixtures", "LegacyV1", scale);
            string manifest = Path.Combine(source, ProjectStore.ManifestName);
            string original = File.ReadAllText(manifest);
            Equal(1, JObject.Parse(original).Value<int>("schemaVersion"));
            var legacy = ProjectStore.Open(source);
            var expected = BakeStore.Read(Path.Combine(source, "exports", "edited", BakeStore.ManifestName));
            Equal(expected.MeshContentHash, legacy.Evaluate().ContentHash);
            Near(-.09f, legacy.Document.Transform.ToAvatarPoint(legacy.Evaluate().Positions[0]).X);
            string copy = Dir("legacy-" + scale);
            foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
            {
                var target = Path.Combine(copy, Path.GetRelativePath(source, file));
                Directory.CreateDirectory(Path.GetDirectoryName(target)); File.Copy(file, target);
            }
            var copyWorkspace = ProjectStore.Open(copy);
            Expect("MIGRATION_REQUIRED", () => ProjectStore.Save(copy, copyWorkspace, copyWorkspace.SaveVersion));
            Equal(original, File.ReadAllText(Path.Combine(copy, ProjectStore.ManifestName)));
            string migrated = Dir("migrated-" + scale); ProjectStore.Save(migrated, legacy, 0);
            Equal(2, JObject.Parse(File.ReadAllText(Path.Combine(migrated, ProjectStore.ManifestName))).Value<int>("schemaVersion"));
            var reopened = ProjectStore.Open(migrated);
            Equal(legacy.Document.StateHash, reopened.Document.StateHash);
            Equal(expected.MeshContentHash, reopened.Evaluate().ContentHash);
            Equal(original, File.ReadAllText(manifest));
        });
    }
}
