using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NyaForge.Authoring;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    private static int passed, failed;
    private static readonly string Root = Path.Combine(Path.GetTempPath(),"NyaForge-Core-Tests-" + Guid.NewGuid().ToString("N"));
    private static int Main(string[] args)
    {
        Directory.CreateDirectory(Root);
        if(args.Contains("--surface-profile")) return SurfacePaintProfile.Run(Root);
        RunAuthoringStateTests();
        RunVrm0SpringExpansionTests();
        RunImportedPreviewRigTests();
        RunVrm0SpringPreviewTests();
        RunSourceAffineTests();
        RunSourceSkinTests();
        RunGlbSourceSkinTests();
        RunSourceSkinCodecTests();
        RunSourceMeshTransformTests();
        RunSourceSkinDeformerTests();
        RunSourceSkinGraphAdapterTests();
        RunSourceSkinPosePaletteTests();
        RunGlbSourceSkinImporterTests();
        RunGlbNodeTransformTests();
        RunFaceInspectionTests();
        RunPolygonWireTests();
        RunPaintWireTests();
        RunLayerWireTests();
        RunPaintLayerImportTests();
        RunCommandWireTests();
        RunCommandWireGraphTests();
        RunCommandWireContextTests();
        RunProjectSaveServiceTests();
        RunProjectSnapshotTests();
        RunProjectExportServiceTests();
        RunEmptyProjectTests();
        RunGraphTests();
        RunGraphStorageTests();
        RunGraphIdentityCacheTests();
        RunGraphDocumentTests();
        RunGraphProjectTests();
        RunGraphCommandTests();
        RunGraphNodeCommandTests();
        RunGraphBakeTests();
        RunGraphLayoutTests();
        RunPolygonMeshTests();
        RunPolygonDerivedDataTests();
        RunPolygonStorageTests();
        RunPolygonEditingTests();
        RunPolygonExtrusionTests();
        RunPolygonDeletionTests();
        RunPolygonCapTests();
        RunPolygonIdHistoryTests();
        RunPolygonFaceMergeTests();
        RunPolygonFaceSplitTests();
        RunPolygonEdgeInsertionTests();
        RunPolygonBridgeTests();
        RunPolygonWeldTests();
        RunPolygonFaceCreationTests();
        RunPolygonVertexCreationTests();
        RunEmptyPolygonTests();
        RunEmptyPolygonGraphTests();
        RunDeleteAllFacesTests();
        RunFacelessDownstreamTests();
        RunPolygonDissolveTests();
        RunPolygonEdgeCutTests();
        RunPolygonCutPathTests(); RunCutPathCommandTests(); RunEdgeScreenPickerTests(); RunMeshRaycastTests(); RunMeshVisibilityTests(); RunEvidenceSnapshotTests(); RunEvidenceTargetTests(); RunEvidenceManifestTests(); RunEvidenceReaderTests(); RunEvidenceCaptureStoreTests();
        RunPolygonSolidifyTests();
        RunPolygonMirrorTests();
        RunPolygonUvTests();
        RunUvIslandTests();
        RunPaintTests();
        RunPaintGraphTests();
        RunPaintRebindTests();
        RunPaintDependencyTests();
        RunPaintPngTests();
        RunSurfaceBakeTests();
        RunPaintLayerTests();
        RunPaintLayerStorageTests();
        RunLayerGraphTests();
        RunLayerCommandTests();
        RunMaskStrokeTests();
        RunImageImportTests();
        RunSurfacePaintMeshTests();
        RunStrokePathTests();
        RunSurfaceStrokeSamplerTests();
        RunSurfaceScreenCoverageTests();
        RunSurfaceCameraSnapshotTests();
        RunSurfacePreparationQueueTests();
        RunMaterialGraphTests();
        RunMaterialBakeTests();
        RunMultiMaterialBakeTests();
        RunBakeOutputIdentityTests();
        RunMaterialSlotTests();
        RunMaterialAssignmentTests();
        RunMaterialFaceTests();
        RunValidationTests();
        RunRigTests();
        RunSkinDeformerTests();
        RunRigCodecTests();
        RunMorphTests();
        RunMorphGraphTests();
        RunGlbImportTests();
        RunGlbSkinImportTests();
        RunVrmMetadataTests();
        RunSpringBoneTests();
        RunSkeletonGraphTests();
        RunPoseGraphTests();
        RunPaintPathSimplifierTests();
        Test("baseline and attribute ownership", () =>
        {
            var fixture = AuthoringFixtures.Panel(1); var p = fixture.Positions.ToArray(); var n = fixture.Normals.ToArray(); var t = fixture.Tangents.ToArray(); var uv = fixture.Uv0.ToArray(); var s = fixture.Submeshes.ToArray();
            var mesh = new MeshData(p,n,t,uv,s); string hash = mesh.ContentHash;
            p[0] = new Vec3(9,9,9); n[0] = new Vec3(1,0,0); s[0][0] = 7; mesh.Submeshes[0][0] = 6;
            Equal(hash,mesh.ContentHash); Near(-.1f,mesh.Positions[0].X); Equal(0,mesh.Submeshes[0][0]); Equal(8,mesh.VertexCount); Equal(4,mesh.TriangleCount);
        });
        foreach (float scale in new[] { 1f,100f }) Test("rest metre delta with source scale " + scale, () =>
        {
            var w = AuthoringWorkspace.Create(AuthoringFixtures.Panel(scale),new RestTransform(scale,new Vec3(3,4,5)),"translated fixture");
            var old = w.Document; Ok(Edit(w,0,new Vec3(.01f,0,0))); var now = w.Evaluate();
            Near(.01f,w.Document.Transform.ToAvatarPoint(now.Positions[0]).X - old.Transform.ToAvatarPoint(old.BaselineMesh.Positions[0]).X);
            Near(old.BaselineMesh.Positions[4].X,now.Positions[4].X); Equal(old.BaselineMesh.TopologyHash,now.TopologyHash); Equal(-1f,now.Normals[0].Z); Equal(1f,now.Tangents[4].W); Equal(2,now.Submeshes.Count);
        });
        Test("same command succeeds before old revision check", () =>
        {
            var w = Fresh(); var c = w.NewCommand(Move()); var service = new AuthoringCommandService(w); var first = service.Execute(c); Ok(first);
            True(object.ReferenceEquals(first,service.Execute(c))); Equal(1L,w.Document.DocumentRevision); Near(-.09f,w.Evaluate().Positions[0].X);
        });
        Test("reusing command ID with different payload is refused", () =>
        {
            var w = Fresh(); var c = w.NewCommand(Move()); var service = new AuthoringCommandService(w); Ok(service.Execute(c)); c.Operations = new[] { AuthoringOperation.TranslateVertices(new[] { 1 },new Vec3(.01f,0,0)) };
            Code("COMMAND_ID_REUSED",service.Execute(c)); Equal(1L,w.Document.DocumentRevision);
        });
        Test("old revision cannot overwrite another edit", () =>
        {
            var w = Fresh(); var stale = w.NewCommand(Move()); Ok(Edit(w,1,new Vec3(.01f,0,0))); Code("REVISION_CONFLICT",new AuthoringCommandService(w).Execute(stale)); Equal(1L,w.Document.DocumentRevision);
        });
        Test("same-count changed baseline and wrong object refused", () =>
        {
            var w = Fresh(); var c = w.NewCommand(Move()); var p = w.Document.BaselineMesh.Positions.ToArray(); p[0] = new Vec3(-.2f,-.05f,-.02f);
            var changed = new MeshData(p,w.Document.BaselineMesh.Normals.ToArray(),w.Document.BaselineMesh.Tangents.ToArray(),w.Document.BaselineMesh.Uv0.ToArray(),w.Document.BaselineMesh.Submeshes.ToArray());
            c.ExpectedBaselineHash = changed.ContentHash; Code("BASE_MESH_CHANGED",new AuthoringCommandService(w).Execute(c));
            c = w.NewCommand(Move()); c.ObjectId = Guid.NewGuid().ToString("D"); Code("SOURCE_CHANGED",new AuthoringCommandService(w).Execute(c));
        });
        Test("invalid second batch operation leaves no edit or history", () =>
        {
            var w = Fresh(); string before = w.Document.StateHash; var c = w.NewCommand(Move(),AuthoringOperation.TranslateVertices(new[] { 999 },new Vec3(1,0,0)));
            Code("INVALID_VERTEX",new AuthoringCommandService(w).Execute(c)); Equal(before,w.Document.StateHash); False(w.CanUndo); Equal(0L,w.Document.DocumentRevision);
        });
        Test("duplicate selection and nonfinite delta rejected", () =>
        {
            var w = Fresh(); Code("INVALID_VERTEX",new AuthoringCommandService(w).Execute(w.NewCommand(AuthoringOperation.TranslateVertices(new[] { 0,0 },new Vec3(1,0,0)))));
            Code("NON_FINITE",Edit(w,0,new Vec3(float.NaN,0,0))); Equal(0L,w.Document.DocumentRevision);
        });
        Test("degenerate triangle candidate refused", () => { var w = Fresh(); Code("DEGENERATE_TRIANGLE",Edit(w,0,new Vec3(.2f,0,0))); False(w.CanUndo); });
        Test("projection commit failure rolls back document and scene", () =>
        {
            var w = Fresh(); var projection = new ProbeProjection { ThrowCommit = true }; string state = w.Document.StateHash; var command = w.NewCommand(Move()); var service = new AuthoringCommandService(w);
            var failure = service.Execute(command,projection); Code("COMMAND_FAILED",failure); True(projection.Prepared.RolledBack); True(projection.Prepared.Disposed); False(projection.Prepared.Visible); Equal(state,w.Document.StateHash); False(w.CanUndo);
            projection.ThrowCommit = false; True(object.ReferenceEquals(failure,service.Execute(command,projection))); Equal(1,projection.PrepareCount);
        });
        Test("projection prepare failure leaves state intact", () => { var w = Fresh(); Code("COMMAND_FAILED",new AuthoringCommandService(w).Execute(w.NewCommand(Move()),new ProbeProjection { ThrowPrepare = true })); Equal(0L,w.Document.DocumentRevision); False(w.CanUndo); });
        Test("two nested edits cannot clear outer execution guard", () =>
        {
            var w = Fresh(); var service = new AuthoringCommandService(w); var p = new ProbeProjection();
            p.DuringPrepare = () => { Code("REENTRANT_COMMAND",service.Execute(w.NewCommand(Move()))); Code("REENTRANT_COMMAND",service.Execute(w.NewCommand(Move()))); Expect("REENTRANT_SAVE",() => ProjectStore.Save(Path.Combine(Root,"nested"),w,0)); };
            Ok(service.Execute(w.NewCommand(Move()),p)); Equal(1L,w.Document.DocumentRevision); Near(-.09f,w.Evaluate().Positions[0].X);
        });
        Test("cleanup failure does not change successful result", () =>
        {
            var w = Fresh(); var service = new AuthoringCommandService(w); var p = new ProbeProjection { ThrowDispose = true }; var c = w.NewCommand(Move()); var result = service.Execute(c,p); Ok(result); True(object.ReferenceEquals(result,service.Execute(c))); Equal(1L,w.Document.DocumentRevision);
        });
        Test("Undo Redo preserve geometry and increase revisions", () =>
        {
            var w = Fresh(); string baseline = w.Evaluate().ContentHash; Ok(Edit(w,0,new Vec3(.01f,0,0))); string edited = w.Evaluate().ContentHash;
            Ok(Execute(w,AuthoringOperation.Undo())); Equal(baseline,w.Evaluate().ContentHash); Equal(2L,w.Document.DocumentRevision); Ok(Execute(w,AuthoringOperation.Redo())); Equal(edited,w.Evaluate().ContentHash); Equal(3L,w.Document.DocumentRevision);
            Ok(Execute(w,AuthoringOperation.Undo())); Ok(Edit(w,1,new Vec3(.01f,0,0))); False(w.CanRedo); Code("HISTORY_EMPTY",Execute(w,AuthoringOperation.Redo()));
        });
        Test("layer disable and Undo preserve deltas", () =>
        {
            var w = Fresh(); Ok(Edit(w,0,new Vec3(.01f,0,0))); string edited = w.Evaluate().ContentHash; Ok(Execute(w,AuthoringOperation.SetLayerEnabled(false))); Equal(w.Document.BaselineMesh.ContentHash,w.Evaluate().ContentHash); Equal(1,w.Document.Offsets.Count); Ok(Execute(w,AuthoringOperation.Undo())); Equal(edited,w.Evaluate().ContentHash);
        });
        Test("native reopen restores state, resets history and instance", () =>
        {
            var w = Fresh(); True(w.IsDirty); var c = w.NewCommand(Move()); Ok(new AuthoringCommandService(w).Execute(c)); string dir = Dir("roundtrip");
            Equal(1L,ProjectStore.Save(dir,w,0)); False(w.IsDirty); var loaded = ProjectStore.Open(dir); Equal(w.Document.StateHash,loaded.Document.StateHash); Equal(w.Evaluate().ContentHash,loaded.Evaluate().ContentHash); False(loaded.CanUndo); False(loaded.CanRedo); False(loaded.IsDirty); Code("STALE_INSTANCE",new AuthoringCommandService(loaded).Execute(c));
            Ok(Execute(w,AuthoringOperation.Undo())); True(w.IsDirty); Ok(Execute(w,AuthoringOperation.Redo())); False(w.IsDirty);
        });
        Test("optimistic saved version prevents stale overwrite", () =>
        {
            string dir = Dir("conflict"); var w = Fresh(); ProjectStore.Save(dir,w,0); var stale = ProjectStore.Open(dir); Ok(Edit(w,0,new Vec3(.01f,0,0))); ProjectStore.Save(dir,w,1); Ok(Edit(stale,1,new Vec3(.01f,0,0))); Expect("SAVE_CONFLICT",() => ProjectStore.Save(dir,stale,1)); Equal(w.Document.StateHash,ProjectStore.Open(dir).Document.StateHash);
        });
        Test("writer lock excludes another writer", () => { string dir = Dir("lock"); using (var handle = new FileStream(Path.Combine(dir,".nyaforge.writer.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)) Expect("PROJECT_LOCKED",() => ProjectStore.Save(dir,Fresh(),0)); });
        Test("save retains old immutable blobs", () =>
        {
            string dir = Dir("retain"); var w = Fresh(); ProjectStore.Save(dir,w,0); string[] first = Directory.GetFiles(Path.Combine(dir,"blobs")); Ok(Edit(w,0,new Vec3(.01f,0,0))); ProjectStore.Save(dir,w,1); foreach (string file in first) True(File.Exists(file)); Equal(3,Directory.GetFiles(Path.Combine(dir,"blobs")).Length);
        });
        Test("corrupted blob refused without replacing manifest", () =>
        {
            string dir = Dir("corrupt"); var w = Fresh(); ProjectStore.Save(dir,w,0); string manifest = File.ReadAllText(Path.Combine(dir,ProjectStore.ManifestName)); string blob = Path.Combine(dir,"blobs",w.Document.BaselineMesh.ContentHash + ".bin"); var bytes = File.ReadAllBytes(blob); bytes[bytes.Length - 1] ^= 1; File.WriteAllBytes(blob,bytes);
            Expect("HASH_MISMATCH",() => ProjectStore.Open(dir)); Expect("HASH_MISMATCH",() => ProjectStore.Save(dir,w,1)); Equal(manifest,File.ReadAllText(Path.Combine(dir,ProjectStore.ManifestName)));
        });
        Test("manifest rejects unknown missing duplicate wrong-type fields", () =>
        {
            string dir = Dir("shape"); ProjectStore.Save(dir,Fresh(),0); string path = Path.Combine(dir,ProjectStore.ManifestName); string original = File.ReadAllText(path);
            foreach (Action<JObject> mutation in new Action<JObject>[] { j => j["surprise"] = 1, j => j.Remove("documentId"), j => j["schemaVersion"] = "1", j => j["objects"][0]["transform"]["scale"] = "1", j => j["name"] = JValue.CreateNull() }) { var json = JObject.Parse(original); mutation(json); File.WriteAllText(path,json.ToString()); Expect("INVALID_MANIFEST",() => ProjectStore.Open(dir)); }
            File.WriteAllText(path,original.Insert(original.IndexOf('{') + 1,"\"schemaVersion\":1,")); Expect("INVALID_MANIFEST",() => ProjectStore.Open(dir));
        });
        Test("manifest path traversal and unsupported version refused", () =>
        {
            string dir = Dir("traversal"); ProjectStore.Save(dir,Fresh(),0); string path = Path.Combine(dir,ProjectStore.ManifestName); var json = JObject.Parse(File.ReadAllText(path)); json["objects"][0]["baselineHash"] = "../outside"; File.WriteAllText(path,json.ToString()); Expect("INVALID_HASH",() => ProjectStore.Open(dir)); json["schemaVersion"] = 99; File.WriteAllText(path,json.ToString()); Expect("UNSUPPORTED_FORMAT",() => ProjectStore.Open(dir));
        });
        Test("over-budget manifest refused before JSON allocation", () => { string dir = Dir("oversized"); File.WriteAllBytes(Path.Combine(dir,ProjectStore.ManifestName),new byte[AuthoringLimits.MaxManifestBytes + 1]); Expect("BUDGET_EXCEEDED",() => ProjectStore.Open(dir)); });
        foreach (float scale in new[] { 1f,100f }) Test("Bake scale " + scale + " preserves attributes", () =>
        {
            var w = AuthoringWorkspace.CreateFixture(scale); Ok(Edit(w,0,new Vec3(.01f,0,0))); var loaded = BakeStore.Read(BakeStore.Export(Dir("bake" + scale),w)); Near(-.09f,loaded.Transform.ToAvatarPoint(loaded.Mesh.Positions[0]).X); Near(-.1f,loaded.Transform.ToAvatarPoint(loaded.Mesh.Positions[4]).X); Equal(w.Evaluate().ContentHash,loaded.MeshContentHash); Equal(w.Document.BaselineMesh.TopologyHash,loaded.Mesh.TopologyHash); True(loaded.NormalsPreservedAfterPositionEdit); Equal(2,loaded.Mesh.Submeshes.Count); Equal(8,loaded.Mesh.Uv0.Count); Equal(8,loaded.Mesh.Tangents.Count); Equal(w.Document.DocumentRevision,loaded.DocumentRevision);
        });
        Test("Bake refuses skin/morph rather than dropping them", () =>
        {
            string path = BakeStore.Export(Dir("unsupported"),Fresh()); string original = File.ReadAllText(path); foreach (string field in new[] { "hasSkin","hasBlendShapes" }) { var json = JObject.Parse(original); json[field] = true; File.WriteAllText(path,json.ToString()); Expect("EXPORT_UNSUPPORTED_FEATURE",() => BakeStore.Read(path)); }
        });
        Test("forged oversized mesh count rejected before allocation", () =>
        {
            string dir = Dir("oversized-count"); string path = BakeStore.Export(dir,Fresh()); var json = JObject.Parse(File.ReadAllText(path)); var bytes = File.ReadAllBytes(Path.Combine(dir,"blobs",json.Value<string>("meshContentHash") + ".bin")); Array.Copy(BitConverter.GetBytes(int.MaxValue),0,bytes,12,4); ReplaceBlob(dir,path,json,bytes); Expect("BUDGET_EXCEEDED",() => BakeStore.Read(path));
        });
        Test("trailing mesh bytes rejected even with matching hash", () =>
        {
            string dir = Dir("trailing"); string path = BakeStore.Export(dir,Fresh()); var json = JObject.Parse(File.ReadAllText(path)); byte[] original = File.ReadAllBytes(Path.Combine(dir,"blobs",json.Value<string>("meshContentHash") + ".bin")); var bytes = new byte[original.Length + 1]; Array.Copy(original,bytes,original.Length); ReplaceBlob(dir,path,json,bytes); Expect("INVALID_BLOB",() => BakeStore.Read(path));
        });
        Test("unsupported scale and nonfinite transform rejected", () => { Expect("UNSUPPORTED_TRANSFORM",() => new RestTransform(0,new Vec3())); Expect("UNSUPPORTED_TRANSFORM",() => new RestTransform(-1,new Vec3())); Expect("NON_FINITE",() => new RestTransform(1,new Vec3(float.NaN,0,0))); });
        if (args.Length == 2 && args[0] == "--export-fixtures")
        {
            string output = Path.GetFullPath(args[1]); Directory.CreateDirectory(output);
            foreach (float scale in new[] { 1f,100f }) { var w = AuthoringWorkspace.CreateFixture(scale); Ok(Edit(w,0,new Vec3(.01f,0,0))); string dir = Path.Combine(output,"scale" + scale); ProjectStore.Save(dir,w,0); Console.WriteLine("FIXTURE " + BakeStore.Export(Path.Combine(dir,"exports","edited"),w)); }
        }
        Console.WriteLine("RESULT " + passed + " passed, " + failed + " failed"); Console.WriteLine("Artifacts: " + Root); return failed == 0 ? 0 : 1;
    }
    private static AuthoringWorkspace Fresh() { return AuthoringWorkspace.CreateFixture(); }
    private static AuthoringOperation Move() { return AuthoringOperation.TranslateVertices(new[] { 0 },new Vec3(.01f,0,0)); }
    private static CommandResult Edit(AuthoringWorkspace w, int index, Vec3 delta) { return Execute(w,AuthoringOperation.TranslateVertices(new[] { index },delta)); }
    private static CommandResult Execute(AuthoringWorkspace w, AuthoringOperation op) { return new AuthoringCommandService(w).Execute(w.NewCommand(op)); }
    private static string Dir(string name) { string path = Path.Combine(Root,name); Directory.CreateDirectory(path); return path; }
    private static void ReplaceBlob(string dir, string path, JObject json, byte[] bytes) { string hash; using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(); File.WriteAllBytes(Path.Combine(dir,"blobs",hash + ".bin"),bytes); json["meshContentHash"] = hash; File.WriteAllText(path,json.ToString()); }
    private static void Test(string name, Action action) { try { action(); passed++; Console.WriteLine("PASS " + name); } catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + e); } }
    private static void True(bool value) { if (!value) throw new Exception("Expected true."); }
    private static void False(bool value) { True(!value); }
    private static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected,actual)) throw new Exception("Expected " + expected + ", got " + actual); }
    private static void Near(float expected, float actual) { if (Math.Abs(expected - actual) > .000001f) throw new Exception("Expected " + expected + ", got " + actual); }
    private static void Ok(CommandResult result) { if (!result.Success) throw new Exception(result.Code + ": " + result.Message); }
    private static void Code(string expected, CommandResult result) { False(result.Success); Equal(expected,result.Code); }
    private static void Expect(string code, Action action) { try { action(); } catch (AuthoringException e) { Equal(code,e.Code); return; } throw new Exception("Expected error " + code); }
    private sealed class ProbeProjection : IAuthoringProjection
    {
        internal bool ThrowPrepare, ThrowCommit, ThrowDispose; internal Action DuringPrepare; internal int PrepareCount; internal PreparedProjection Prepared;
        public IPreparedProjection Prepare(AuthoringDocument candidate, MeshData mesh) { PrepareCount++; if (ThrowPrepare) throw new Exception("Prepare failed"); if (DuringPrepare != null) DuringPrepare(); Prepared = new PreparedProjection { ThrowCommit = ThrowCommit, ThrowDispose = ThrowDispose }; return Prepared; }
    }
    private sealed class PreparedProjection : IPreparedProjection
    {
        internal bool ThrowCommit, ThrowDispose, Visible, RolledBack, Disposed;
        public void Commit() { Visible = true; if (ThrowCommit) throw new Exception("Commit failed after changing scene"); }
        public void Rollback() { Visible = false; RolledBack = true; }
        public void Dispose() { Disposed = true; if (ThrowDispose) throw new Exception("Cleanup failed"); }
    }
}















