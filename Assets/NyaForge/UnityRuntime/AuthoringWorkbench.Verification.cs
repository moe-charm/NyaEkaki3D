using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        [Serializable]
        sealed class VerificationReport
        {
            public bool passed;
            public string failure;
            public string unityVersion;
            public string graphicsDevice;
            public string[] checks;
            public string[] bakeManifests;
            public string[] graphBakeManifests;
            public string screenshot;
            public string screenshotKind = "UI Toolkit target texture with the live authoring camera texture";
            public int width, height;
            public int triangles;
        }

        public void RunVerification(string output)
        {
            // The verifier launches a hidden Player; focus changes must not suspend its UI event probes.
            Application.runInBackground=true;
            StartCoroutine(Verify(output));
        }

        IEnumerator Verify(string output)
        {
            output = Path.GetFullPath(output);
            Directory.CreateDirectory(output);
            graphCanvas.SetLayoutDirectory(Path.Combine(output, "workspace-layouts"));
            var checks = new List<string>();
            var exports = new List<string>();
            var graphExports = new List<string>();
            string failure = null;
            var arguments = Environment.GetCommandLineArgs();
            int modelImportIndex = Array.IndexOf(arguments, "--authoring-import-model");
            int reopenIndex = Array.IndexOf(arguments, "--authoring-reopen-project");
            if (reopenIndex >= 0 && reopenIndex + 1 < arguments.Length)
            {
                var reopened = ProjectStore.Open(arguments[reopenIndex + 1]);
                ReplaceWorkspace(reopened, arguments[reopenIndex + 1]);
                checks.Add("reopened persisted project in a new Player process: " + reopened.Document.DocumentId);
            }
            // Let UI Toolkit establish layout and the graphics device render first.
            for (int i = 0; i < 12; i++) yield return null;
            yield return VerifyMcpListener(error=>failure=error);
            int probeIndex=Array.IndexOf(arguments,"--authoring-mcp-probe");
            if(failure==null && probeIndex>=0 && probeIndex+1<arguments.Length)
            {
                bool secondary=Array.IndexOf(arguments,"--authoring-secondary-mcp")>=0;
                bool glbExport=Array.IndexOf(arguments,"--authoring-glb-export-mcp")>=0;
                if(secondary)
                {
                    yield return VerifyExternalMcp(arguments[probeIndex+1],output,error=>failure=error,false,true);
                    if(failure==null) checks.Add("External MCP client -> sidecar -> live Player: secondary-motion state/play/pause/rebuild/fixed-step/reset and authored-state preservation");
                }
                else if(glbExport)
                {
                    yield return VerifyExternalMcp(arguments[probeIndex+1],output,error=>failure=error,false,false,true);
                    if(failure==null) checks.Add("External MCP client -> sidecar -> live Player: revision-pinned standard GLB export and replay destination protection");
                }
                else
                {
                    yield return VerifyExternalMcp(arguments[probeIndex+1],output,error=>failure=error);
                    if(failure==null) yield return VerifyExternalMcp(arguments[probeIndex+1],output,error=>failure=error,true);
                    if(failure==null) checks.Add("External MCP client -> sidecar -> live Player: three matching instance/document/revision/hash reads");
                }
            }
            try
            {
                Check(failure==null,"Player MCP listener: "+failure);
                checks.Add("Player named pipe state request: background transport, main-thread snapshot, document/instance correlation");
                Check(workspace.Document.IsEmpty && projection.DisplayMesh == null && projection.Points.Length == 0, "Startup should be an empty project");
                string emptyPath = Path.Combine(output, "empty");
                projectPath.SetValueWithoutNotify(emptyPath); SaveProject();
                string emptyId = workspace.Document.DocumentId;
                OpenProject();
                Check(workspace.Document.IsEmpty && workspace.Document.DocumentId == emptyId && !HasUnsaved, "Empty save/reopen failed");
                if (modelImportIndex >= 0 && modelImportIndex + 1 < arguments.Length)
                    VerifyCommandLineModelImport(arguments[modelImportIndex + 1], output, checks);
                AddSample(1);
                Check(!workspace.Document.IsEmpty && workspace.CanUndo && projection.Points.Length > 0, "Sample did not use the command path");
                VerifyImportDiagnosticsPanel(checks);
                bool replaced = false;
                ConfirmReplace(() => replaced = true);
                Check(confirmRow.resolvedStyle.display != UnityEngine.UIElements.DisplayStyle.None, "Unsaved guard missing");
                CancelReplace();
                Check(confirmRow.style.display.value == DisplayStyle.None && !replaced && !workspace.Document.IsEmpty && HasUnsaved, "Cancel changed the project or left confirmation open");
                Execute(AuthoringOperation.Undo());
                Check(workspace.Document.IsEmpty && !HasUnsaved && projection.DisplayMesh == null, "Undo did not restore empty saved project");
                Check(projection.Points.Length == 0, "Stale vertex markers remained");
                controls.scrollOffset = Vector2.zero;
                checks.Add("empty startup/save/reopen; sample command; unsaved cancel; Undo clears projection");
            }
            catch (Exception e) { failure = e.ToString(); Debug.LogException(e); }
            yield return WorkbenchCapture.Write(GetComponent<UnityEngine.UIElements.UIDocument>(), camera, Path.Combine(output, "empty.png"),
                error => { if (error != null) failure = (failure ?? "") + "\n" + error; });
            try
            {
                foreach (float scale in new[] { 1f, 100f })
                {
                    ReplaceWorkspace(AuthoringWorkspace.CreateFixture(scale), null);
                    string baseline = workspace.Document.BaselineMesh.ContentHash;
                    var original = projection.Points[0];
                    Select(new[] { 0 });
                    moveX.SetValueWithoutNotify(10); moveY.SetValueWithoutNotify(0); moveZ.SetValueWithoutNotify(0);
                    MoveSelection();
                    Check(Vector3.Distance(projection.Points[0], original + Vector3.right * .01f) < 1e-6f, "1cm edit does not match world coordinates");
                    Check(workspace.Document.BaselineMesh.ContentHash == baseline, "Baseline mutated");
                    Check(projection.DisplayMesh.isReadable, "Owned mesh is not readable");
                    string edited = workspace.Evaluate().ContentHash;
                    string graphDirectory = Path.Combine(output, "graph-assets-" + scale);
                    string graphHash = GraphBlobStore.Write(graphDirectory, workspace.Document.Objects[0].Graph);
                    var savedGraph = GraphBlobStore.Read(graphDirectory, graphHash);
                    var graphResult = NyaForge.Authoring.Graph.GraphEvaluator.Evaluate(savedGraph);
                    Check(graphResult.IsComplete && graphResult.Output.Mesh.ContentHash == edited, "Graph blob roundtrip changed evaluated geometry");
                    checks.Add("scale" + scale + ": graph assets saved/read by Windows Player, evaluated hash preserved");
                    Execute(AuthoringOperation.Undo());
                    Check(workspace.Evaluate().ContentHash == baseline, "Undo did not restore baseline");
                    Execute(AuthoringOperation.Redo());
                    Check(workspace.Evaluate().ContentHash == edited, "Redo changed result");
                    Execute(AuthoringOperation.SetLayerEnabled(false));
                    Check(workspace.Evaluate().ContentHash == baseline, "Disabled layer still changes geometry");
                    Execute(AuthoringOperation.SetLayerEnabled(true));
                    Check(workspace.Evaluate().ContentHash == edited, "Enabled layer differs");
                    string project = Path.Combine(output, "scale" + scale.ToString("0", System.Globalization.CultureInfo.InvariantCulture));
                    projectPath.SetValueWithoutNotify(project);
                    SaveProject();
                    Check(!workspace.IsDirty && File.Exists(Path.Combine(project, "project.nyaforge.json")), "Save failed");
                    string oldInstance = workspace.InstanceId;
                    OpenProject();
                    Check(workspace.InstanceId != oldInstance && !workspace.CanUndo, "Reopen retained instance or transient undo history");
                    Check(workspace.Evaluate().ContentHash == edited, "Reopen lost edit");
                    Check(Vector3.Distance(projection.Points[0], original + Vector3.right * .01f) < 1e-6f, "Reopen projection differs");
                    string manifest = BakeStore.Export(Path.Combine(project, "exports", "edited"), workspace);
                    var bake = BakeStore.Read(manifest);
                    Check(bake.Mesh.ContentHash == edited, "Bake differs from committed document");
                    exports.Add(manifest);
                    checks.Add("scale" + scale + ": owned projection, +10mm, baseline preserved, Undo/Redo, layer, save/reopen, bake");
                }

                Frame();
                var point = VertexPanelPoint(projection.Points[0]);
                Select(Array.Empty<int>());
                PickVertex(point, false);
                Check(selection.SetEquals(new[] { 0 }), "Pointer-to-vertex selection failed");
                Check(view.worldBound.width >= 150 && view.worldBound.height >= 100, "Authoring viewport clipped");
                Check(moveButton.worldBound.height >= 30 && moveX.worldBound.height >= 30, "Edit controls clipped");
                checks.Add("camera projection to GUI vertex selection; scrollable controls; readable control heights");
                // A new path is a create operation, not a stale save of the old path.
                string saveAs = Path.Combine(output, "save-as");
                projectPath.SetValueWithoutNotify(saveAs); SaveProject();
                Check(File.Exists(Path.Combine(saveAs, ProjectStore.ManifestName)), "Save As failed after a previous save");
                Execute(AuthoringOperation.TranslateVertices(new[] { 4 }, new Vec3(.001f, 0, 0)));
                Check(!WantsToQuit(), "Unsaved authoring work did not prevent exit");
                Execute(AuthoringOperation.Undo());
                Check(!workspace.IsDirty && WantsToQuit(), "Saved state should allow exit after Undo");
                confirmRow.style.display = UnityEngine.UIElements.DisplayStyle.None;
                controls.scrollOffset = Vector2.zero;
                checks.Add("Save As creates a new project; unsaved exit guard; Undo returns to saved state");
                VerifyGraphProjection(output, checks);
                VerifySaveFailureGuard(output, checks);
                VerifySpringCore(checks);
                VerifyMultiObjectDisplay(output, checks);
                VerifyChokerTemplate(output, checks);
                VerifyAttachment(output, checks);
                VerifyVrmImportRoundtrip(output, checks);
                VerifySecondaryMotionRebind(checks);
                VerifyPhysBonesTargetStatus(output, checks);
                VerifyGraphExports(output, checks, graphExports);
                TopologyVerification.Verify(output, checks);
                RigGraphVerification.Verify(output, checks);
                PaintVerification.Verify(output, checks);
                PaintLayerAssetsVerification.Verify(output,checks);
                SurfacePaintVerification.Verify(checks);
                SurfaceCameraVerification.Verify(checks);EvidenceCaptureVerification.Verify(output,checks);EvidenceTexturedVerification.Verify(output,checks);
                ColorProjectionVerification.Verify(checks);
                MaterialProjectionVerification.Verify(output,checks);
                MultiMaterialProjectionVerification.Verify(output,checks);
                SetStatus("自動検証: scale 1 / 100 の 1cm編集・保存・出力が一致しました。");
                Check(status.tooltip == status.text, "Status tooltip did not retain the complete diagnostic text");
                checks.Add("narrow status footer stays one line while its complete text remains available as a tooltip");
            }
            catch (Exception e) { failure = e.ToString(); Debug.LogException(e); }

            string screenshot = Path.Combine(output, "authoring.png");
            yield return WorkbenchCapture.Write(GetComponent<UnityEngine.UIElements.UIDocument>(), camera, screenshot,
                error => { if (error != null) failure = (failure ?? "") + "\n" + error; });
            checks.Add("GPU UI/mesh capture with nonempty pixel check");
            yield return VerifyCanvasPointer(output, error => { if (error != null) failure = (failure ?? "") + "\n" + error; else checks.Add("runtime pointer events with panel hit testing: node creation, ports, parameter Apply, drag/layout, vertex pick/move, save/reopen/export"); });
            yield return VerifyRigWeightPaint(output, error => { if (error != null) failure = (failure ?? "") + "\n" + error; else checks.Add("Rig edit lifecycle: brush radius, one drag/one command, selected bone weight update, XYZ pose and Undo/Redo"); });
            yield return VerifyFaceEditing(output, error => { if (error != null) failure = (failure ?? "") + "\n" + error; else checks.Add("face pointer pick/extrude/delete/cap: five to four to six faces, boundary highlight follows loop positions without document changes and clears on foldout/stage/closure, two boundaries capped, Undo/Redo, native reopen, Bake"); });
            yield return VerifySolidify(output, error => { if (error != null) failure = (failure ?? "") + "\n" + error; else checks.Add("solidify GUI: closed six-face shell, Undo/Redo, native reopen, twelve-triangle Bake"); });
            yield return VerifyMirror(output, error => { if (error != null) failure = (failure ?? "") + "\n" + error; else checks.Add("Mirror starter pointer, upstream edit propagation, Undo/Redo, native reopen, Bake"); });
            yield return VerifyUv(output, error => { if (error != null) failure = (failure ?? "") + "\n" + error; else checks.Add("UV projection pointer: six packed faces, preview, Undo/Redo, native reopen and Bake"); });
            yield return VerifyFaceMerge(output,error=> { if(error!=null) failure=(failure??"")+"\n"+error;else checks.Add("Face merge button: two triangles become one quad, retained selection only, Undo/Redo, native reopen and Bake"); });
            yield return VerifyFaceDissolve(output,error=> { if(error!=null) failure=(failure??"")+"\n"+error;else checks.Add("Multi-face dissolve GUI: disconnected selection retained, four triangles to quad, internal vertex removed, Undo/Redo/native/Bake"); });
            yield return VerifyFaceSplit(output,error=> { if(error!=null) failure=(failure??"")+"\n"+error;else checks.Add("Face split: pointer plus Shift selects diagonal, button creates two triangles, invalid adjacent selection preserves state/selection, Undo/Redo native and Bake"); });
            yield return VerifyEdgeInsertion(output,error=> { if(error!=null) failure=(failure??"")+"\n"+error;else checks.Add("Edge insertion button: 25-percent position, new vertex selected, invalid endpoint preserves state/selection, Undo/Redo, native reopen and Bake"); });
            yield return VerifyEvidenceGui(output,error=> { if(error!=null) failure=(failure??"")+"\n"+error;else checks.Add("Evidence GUI: real button, five fixed-scale views, package reopen, document unchanged"); });
            yield return VerifyCutVisibility(output,error=> { if(error!=null) failure=(failure??"")+"\n"+error;else checks.Add("Cut visibility: overlapping faces, pointer toggle/click, hover capture, camera cleanup and BVH invalidation"); });
            yield return VerifyCutPath(output,error=> { if(error!=null) failure=(failure??"")+"\n"+error;else checks.Add("Cut path GUI: multi-face preview, invalid late split, one Undo/Redo, native reopen and Bake"); });
            yield return VerifyEdgeCut(output,error=> { if(error!=null) failure=(failure??"")+"\n"+error;else checks.Add("Edge cut GUI: preview, two quads, invalid state/selection retained, Undo/Redo/native/Bake"); });
            yield return VerifyBoundaryBridge(output,error=> { if(error!=null) failure=(failure??"")+"\n"+error;else checks.Add("Boundary bridge GUI: two highlights, same-loop rejection, closed box, Undo/Redo, native reopen and Bake"); });
            yield return VerifyWeld(output,error=> { if(error!=null) failure=(failure??"")+"\n"+error;else checks.Add("Weld GUI: centroid collapse, retained vertex selected, invalid weld preserves selection/document, Undo/Redo, native reopen and Bake"); });
            yield return VerifyFaceCreation(output,error=> { if(error!=null) failure=(failure??"")+"\n"+error;else checks.Add("Ordered face GUI: outline without document changes, wrong winding rejected, creation clears draft, Undo/Redo, native reopen and Bake"); });
            yield return VerifyVertexCreation(output,error=> { if(error!=null) failure=(failure??"")+"\n"+error;else checks.Add("Loose vertex GUI: add, point click, render mesh unchanged, Undo/Redo and native reopen/Bake"); });
            yield return VerifyManyPoints(output,error=> { if(error!=null) failure=(failure??"")+"\n"+error;else checks.Add("2053 editing points in 2 batches, last-point click, select all/clear, final/edit stage switch"); });
            yield return VerifyEmptyPolygon(output,error=> { if(error!=null) failure=(failure??"")+"\n"+error;else checks.Add("Empty polygon GUI: 0-3 point stages, native reopen, first face, Undo/Redo and Bake"); });
            yield return VerifyFacelessRecovery(output,error=> { if(error!=null) failure=(failure??"")+"\n"+error;else checks.Add("Faceless downstream GUI: return-to-edit button, document unchanged, Undo recovery, Redo, native diagnostic reopen"); });
            yield return VerifyUvEditing(output, error => { if (error != null) failure = (failure ?? "") + "\n" + error; else checks.Add("UV wheel cursor anchor, right-drag pan, navigated island picking/reset without document changes; island transform, Undo/Redo, native reopen and Bake"); });
            yield return VerifyUvDrag(output,error=> { if(error!=null) failure=(failure??"")+"\n"+error;else checks.Add("UV island drag: preview-only motion, one revision/Undo, outside-tile pan/pick, Esc/capture-loss cancellation, stale revision rejection, native reopen and Bake"); });
            yield return VerifyPaintGraph(output, error => { if (error != null) failure = (failure ?? "") + "\n" + error; else checks.Add("Paint Image graph ports, stroke Undo/Redo, native image references, textured projection, explicit unsupported static Bake"); });
            yield return VerifyPaintUi(output, error => { if (error != null) failure = (failure ?? "") + "\n" + error; else checks.Add("paint GUI pointer stroke: preview isolation, one command/Undo, cancel/capture loss, native reopen"); });
            yield return CaptureGraphEditing(output, error => { if (error != null) failure = (failure ?? "") + "\n" + error; });
            yield return VerifyItemWorkflow(output,error=> { if (error != null) failure = (failure ?? "") + "\n" + error; else checks.Add("Diamond item GUI: vertex moves, thickness, UV, paint, save/reopen and surface export button"); });
            yield return VerifyLayerGraph(output,error=> { if (error != null) failure=(failure ?? "")+"\n"+error; else checks.Add("Layered Paint graph: native layer/mask/hidden payload ownership, composited preview and Surface export"); });
            yield return VerifyLayerUi(output,error=> { if(error!=null) failure=(failure ?? "")+"\n"+error; else checks.Add("Layer GUI: migration, add/select/stroke, appearance, order/remove/empty controls, Undo, native reopen, composite PNG/Surface"); });
            yield return VerifyMaskUi(output,error=> { if(error!=null) failure=(failure ?? "")+"\n"+error; else checks.Add("Mask GUI: rename, add/select, linear preview and one stroke Undo, Escape/target cancellation, reveal/remove, native reopen, composite PNG/Surface"); });
            yield return VerifyImageImport(output,error=> { if(error!=null) failure=(failure ?? "")+"\n"+error; else checks.Add("PNG import: exact RGBA/orientation, invalid input isolation, aspect fit as new layer, Undo, owned native image after source loss, Surface"); });
            yield return VerifySurfacePaintUi(output,error=> { if(error!=null) failure=(failure ?? "")+"\n"+error; else checks.Add("3D paint GUI: sampled drag, temporary preview isolation, one command Undo, Escape/capture cancellation, mask paint, native reopen and Surface"); });
            yield return VerifyMaterialUi(output,error=> { if(error!=null) failure=(failure ?? "")+"\n"+error; else checks.Add("Material GUI: create preserving Paint, atomic migration Undo/Redo, validated values, unchanged precision, 3D paint through material, native save/reopen and adding Paint to an untextured material"); });
            yield return VerifySurfaceBoundaries(output,error=> { if(error!=null) failure=(failure ?? "")+"\n"+error; else checks.Add("3D boundary GUI: shared-edge UV seam, background gap, unpaintable foreground, disconnected strokes, Undo, native and Surface"); });
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--authoring-dense-paint")>=0)
                yield return VerifyDensePaint(output,error=> { if(error!=null) failure=(failure ?? "")+"\n"+error; else checks.Add("Dense 131072-triangle paint GUI: cold/warm strokes, preview isolation, simplified continuous raster, one command and Undo/Redo; timings recorded"); });
            var report = new VerificationReport
            {
                passed = failure == null, failure = failure, unityVersion = Application.unityVersion,
                graphicsDevice = SystemInfo.graphicsDeviceName, checks = checks.ToArray(), bakeManifests = exports.ToArray(),
                graphBakeManifests = graphExports.ToArray(),
                screenshot = screenshot, width = Screen.width, height = Screen.height,
                triangles = workspace?.Evaluate()?.TriangleCount ?? 0
            };
            File.WriteAllText(Path.Combine(output, "report.json"), JsonUtility.ToJson(report, true));
            Debug.Log("NYAFORGE_AUTHORING_CHECK " + (report.passed ? "PASS" : "FAIL"));
            allowQuit = true;
            Application.Quit(report.passed ? 0 : 1);
        }

        static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}














