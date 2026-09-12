using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void VerifySpringPlayback(string output, List<string> checks)
        {
            springAutomaticTick = false;
            try
            {
                var path = Path.Combine(output, "playback.vrm");
                File.WriteAllBytes(path, VrmVerificationFixture.Create(false, playback: true));
                ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null); ImportModel(path);
                string authored = workspace.Document.StateHash, metadata = workspace.Attachments.ContentHash;
                var baseline = projection.DisplayMesh.vertices;
                Check(springPlay.enabledSelf, "Spring play button is disabled for VRM1");
                StartSpringPlayback();
                TickSpringPlayback(1f / 60);
                var reusedMesh = projection.DisplayMesh; var reusedObject = projection.DisplayObject;
                for (int i = 1; i < 12; i++)
                {
                    TickSpringPlayback(1f / 60);
                    Check(projection.DisplayMesh == reusedMesh && projection.DisplayObject == reusedObject, "Spring playback rebuilt unchanged mesh layout");
                }
                Check(projection.PointBatchCount == 0, "Playback retained edit point batches");
                Check(springPlayback != null && springPlayback.CompletedSteps == 12, "GUI Spring preview did not run");
                Check(projection.DisplayMesh.vertices.Where((v, i) => (v - baseline[i]).sqrMagnitude > 1e-10f).Any(), "Spring pose did not reach displayed mesh");
                Check(workspace.Document.StateHash == authored && workspace.Attachments.ContentHash == metadata, "Playback changed authored state");
                springPlayback.Pause(); RefreshSpringPlayback(); var frozen = springPlayback.State;
                TickSpringPlayback(.2f);
                Check(ReferenceEquals(frozen, springPlayback.State) && !springPause.enabledSelf, "Pause did not freeze preview");
                StartSpringPlayback(); TickSpringPlayback(1f / 60);
                Check(springPlayback.CompletedSteps == 13, "Resume reset or skipped history");
                projectPath.SetValueWithoutNotify(Path.Combine(output, "playback-project"));
                Check(TrySaveProject(), "Save during Spring playback failed");
                OpenProject();
                Check(springPlayback == null && workspace.Document.StateHash == authored && !HasUnsaved, "Open persisted transient simulation");
                var restored = projection.DisplayMesh.vertices;
                Check(restored.Where((v, i) => (v - baseline[i]).sqrMagnitude > 1e-10f).Any() == false, "Open did not restore authored mesh");
                StartSpringPlayback(); TickSpringPlayback(1f / 60); ClearSpringPlayback(true);
                Check(springPlayback == null && !springReset.enabledSelf && projection.PointBatchCount > 0, "Reset did not restore editing projection");
                StartSpringPlayback();
                var graph = workspace.Document.Objects[0].Graph;
                var node = graph.Nodes[springPoseNode];
                Execute(AuthoringOperation.UpdateNode(GraphNode.PoseNode(node.NodeId, node.Pose)));
                Check(springPlayback == null, "Editing did not stop Spring playback");
                checks.Add("VRM1 playback handlers: displayed mesh changes with reused mesh/object and restored edit points, authored graph/metadata unchanged, pause/resume/reset, Save/Open excludes simulation, editing stops playback");
            }
            finally { ClearSpringPlayback(true); springAutomaticTick = true; }
        }
    }
}
