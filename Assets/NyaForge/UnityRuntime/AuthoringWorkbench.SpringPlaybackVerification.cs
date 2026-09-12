using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void VerifySpringPlayback(string output, List<string> checks)
        {
            foreach (bool legacy in new[] { false, true })
            {
                var directory = Path.Combine(output, legacy ? "vrm0-playback" : "vrm1-playback");
                Directory.CreateDirectory(directory); VerifySpringPlaybackFormat(directory, checks, legacy);
            }
        }

        void VerifySpringPlaybackFormat(string output, List<string> checks, bool legacy)
        {
            springAutomaticTick = false;
            try
            {
                var path = Path.Combine(output, "playback.vrm");
                File.WriteAllBytes(path, VrmVerificationFixture.Create(legacy, playback: true));
                ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null); ImportModel(path);
                Check(importedRigSession?.SourceSkin != null, "GUI import did not retain complete source skin");
                var sourcePackage = new SourceSkinPackage(importedRigSession.SourceSkin, importedRigSession.SourceSkinBinding);
                var sourcePayload = SourceSkinPackageCodec.Write(sourcePackage);
                var fixturePackage = GlbSourceSkinImporter.Read(File.ReadAllBytes(path));
                Check(sourcePayload.SequenceEqual(SourceSkinPackageCodec.Write(new SourceSkinPackage(fixturePackage.Skin, fixturePackage.Binding))), "GUI import changed source frames, binds or weights");
                var importedGraph = workspace.Document.Objects[0].Graph;
                var expectedRestDisplay = SourceSkinGraphAdapter.ApplyToEvaluation(workspace.Preview.Evaluation, importedGraph, importedRigSession);
                Check(expectedRestDisplay != null && projection.DisplayMesh != null && projection.DisplayMesh.vertexCount == expectedRestDisplay.Mesh.VertexCount, "Workbench did not publish a source-skin display value");
                for (int i = 0; i < expectedRestDisplay.Mesh.VertexCount; i++)
                    Check((projection.DisplayMesh.vertices[i] - OwnedMeshProjection.ToUnity(expectedRestDisplay.Mesh.Positions[i])).sqrMagnitude < 1e-10f, "Workbench display differs from source rest deformation");
                string authored = workspace.Document.StateHash, metadata = workspace.Attachments.ContentHash;
                var baseline = projection.DisplayMesh.vertices;
                Check(springPlay.enabledSelf, "Spring play button is disabled for VRM");
                var mcpPlay = DispatchSecondaryMotionMcp("secondary_motion_play");
                Check((bool)mcpPlay["success"] && (bool)mcpPlay["playing"], "MCP secondary-motion play did not start the shared playback owner");
                var mcpRebuild = DispatchSecondaryMotionMcp("secondary_motion_rebuild");
                Check((bool)mcpRebuild["success"] && (bool)mcpRebuild["playing"] && (long)mcpRebuild["completedSteps"] == 0,
                    "MCP secondary-motion rebuild did not replace the transient playback owner");
                TickSpringPlayback(1f / 60);
                var reusedMesh = projection.DisplayMesh; var reusedObject = projection.DisplayObject;
                var dynamicGraph = workspace.Document.Objects[0].Graph;
                var dynamicEvaluation = GraphEvaluator.Evaluate(dynamicGraph.ReplaceNode(GraphNode.PoseNode(springPoseNode, springPlayback.Pose)));
                var expectedDynamicDisplay = SourceSkinGraphAdapter.ApplyToEvaluation(dynamicEvaluation, dynamicGraph, importedRigSession);
                Check(expectedDynamicDisplay != null && projection.DisplayMesh.vertexCount == expectedDynamicDisplay.Mesh.VertexCount, "Spring did not publish a source-skin display value");
                for (int i = 0; i < expectedDynamicDisplay.Mesh.VertexCount; i++)
                    Check((projection.DisplayMesh.vertices[i] - OwnedMeshProjection.ToUnity(expectedDynamicDisplay.Mesh.Positions[i])).sqrMagnitude < 1e-10f, "Spring display differs from source palette deformation");
                for (int i = 1; i < 12; i++)
                {
                    TickSpringPlayback(1f / 60);
                    Check(projection.DisplayMesh == reusedMesh && projection.DisplayObject == reusedObject, "Spring playback rebuilt unchanged mesh layout");
                }
                Check(projection.PointBatchCount == 0, "Playback retained edit point batches");
                Check(springPlayback != null && springPlayback.CompletedSteps == 12, "GUI Spring preview did not run");
                Check(projection.DisplayMesh.vertices.Where((v, i) => (v - baseline[i]).sqrMagnitude > 1e-10f).Any(), "Spring pose did not reach displayed mesh");
                Check(workspace.Document.StateHash == authored && workspace.Attachments.ContentHash == metadata, "Playback changed authored state");
                var mcpPause = DispatchSecondaryMotionMcp("secondary_motion_pause");
                Check((bool)mcpPause["success"] && !(bool)mcpPause["playing"], "MCP secondary-motion pause did not pause the shared owner");
                var frozen = springPlayback.State;
                TickSpringPlayback(.2f);
                Check(ReferenceEquals(frozen, springPlayback.State) && !springPause.enabledSelf, "Pause did not freeze preview");
                var mcpResume = DispatchSecondaryMotionMcp("secondary_motion_play");
                Check((bool)mcpResume["success"] && (bool)mcpResume["playing"], "MCP secondary-motion resume did not restart playback");
                TickSpringPlayback(1f / 60);
                Check(springPlayback.CompletedSteps == 13, "Resume reset or skipped history");
                projectPath.SetValueWithoutNotify(Path.Combine(output, "playback-project"));
                Check(TrySaveProject(), "Save during Spring playback failed");
                // Only this generated fixture is moved; native reopening must not depend on the source path.
                File.Move(path, path + ".source-unavailable");
                OpenProject();
                Check(importedRigSession?.SourceSkin != null && importedRigSession.SourceSkinBinding != null && sourcePayload.SequenceEqual(SourceSkinPackageCodec.Write(new SourceSkinPackage(importedRigSession.SourceSkin, importedRigSession.SourceSkinBinding))), "Save/Open lost complete source frames, binds or weights");
                Check(springPlayback == null && workspace.Document.StateHash == authored && !HasUnsaved, "Open persisted transient simulation");
                var restored = projection.DisplayMesh.vertices;
                Check(restored.Where((v, i) => (v - baseline[i]).sqrMagnitude > 1e-10f).Any() == false, "Open did not restore authored mesh");
                StartSpringPlayback(); TickSpringPlayback(1f / 60); var mcpReset = DispatchSecondaryMotionMcp("secondary_motion_reset");
                Check((bool)mcpReset["success"] && !(bool)mcpReset["playing"], "MCP secondary-motion reset did not clear playback");
                Check(springPlayback == null && !springReset.enabledSelf && projection.PointBatchCount > 0, "Reset did not restore editing projection");
                StartSpringPlayback();
                springPlayback.Pause();
                long beforeStep = springPlayback.CompletedSteps;
                var mcpStep = DispatchSecondaryMotionMcp("secondary_motion_step");
                Check((bool)mcpStep["success"] && !(bool)mcpStep["playing"] && (long)mcpStep["completedSteps"] == beforeStep + 1,
                    "MCP secondary-motion step did not advance one fixed step while remaining paused");
                var graph = workspace.Document.Objects[0].Graph;
                var node = graph.Nodes[springPoseNode];
                Execute(AuthoringOperation.UpdateNode(GraphNode.PoseNode(node.NodeId, node.Pose)));
                Check(springPlayback == null, "Editing did not stop Spring playback");
                checks.Add((legacy ? "VRM0" : "VRM1") + " playback handlers: complete source skin matches GLB and survives native Save/Open; displayed mesh changes with reused mesh/object and restored edit points, authored graph/metadata unchanged, GUI and MCP pause/resume/rebuild/reset/step, Save/Open excludes simulation, editing stops playback");
            }
            finally { ClearSpringPlayback(true); springAutomaticTick = true; }
        }
    }
}
