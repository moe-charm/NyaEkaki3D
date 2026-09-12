using System;
using System.Collections;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyRigWeightPaint(string output, Action<string> completed)
        {
            var previous = workspace;
            var previousPath = savedDirectory;
            string failure = null;
            try
            {
                ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
            }
            catch (Exception e)
            {
                completed(e.ToString());
                yield break;
            }
            try
            {
                try
                {
                    graphCanvas.AddRigSampleForVerification();
                }
                catch (Exception e) { failure = e.ToString(); }
                yield return null;
                yield return null;
                if (failure == null) try
                {
                    Check(TryResolveRig(out _, out _, out _), "Rig sample did not resolve for weight paint");
                    rigPanel.value = true;
                    rigBoneChoice.index = Math.Min(1, rigBoneChoice.choices.Count - 1);
                    rigWeightField.SetValueWithoutNotify(.5f);
                    rigBrushRadius.SetValueWithoutNotify(24);
                    rigWeightPaint.SetValueWithoutNotify(true);
                    RefreshRig();
                    Check(rigWeightPaint.enabledSelf && RigWeightPaintActive, "Weight paint control was not enabled");
                    var before = workspace.Document.StateHash;
                    var revision = workspace.Document.DocumentRevision;
                    var first = VertexPanelPoint(projection.Points[0]);
                    var second = VertexPanelPoint(projection.Points[1]);
                    BeginRigWeightStroke(first);
                    UpdateRigWeightStroke(second);
                    Check(rigStrokeVertices.Count > 0, "Weight paint brush did not hit any projected vertices");
                    Check(rigPainting && workspace.Document.StateHash == before && workspace.Document.DocumentRevision == revision, "Weight paint stroke changed the document before release");
                    EndRigWeightStroke();
                    var after = workspace.Document.StateHash;
                    Check(!rigPainting && after != before && workspace.Document.DocumentRevision == revision + 1, "Weight paint stroke was not committed as one command");
                    GraphNode bindingNode; MeshData mesh; NyaForge.Authoring.Rig.SkeletonDefinition skeleton;
                    Check(TryResolveRig(out bindingNode, out mesh, out skeleton), "Weight paint binding disappeared after commit");
                    var child = skeleton.Bones[Math.Min(1, skeleton.Bones.Count - 1)].BoneId;
                    Check(bindingNode.Binding.Weights[0].Any(item => item.BoneId == child && item.Weight > 0), "Weight paint did not update the brushed vertex");
                    Execute(AuthoringOperation.Undo());
                    Check(workspace.Document.StateHash == before, "Weight paint Undo failed");
                    Execute(AuthoringOperation.Redo());
                    Check(workspace.Document.StateHash == after, "Weight paint Redo failed");
                    var poseBefore = workspace.Document.StateHash;
                    var poseRevision = workspace.Document.DocumentRevision;
                    rigPoseX.SetValueWithoutNotify(15);
                    rigPoseY.SetValueWithoutNotify(20);
                    rigPoseDegrees.SetValueWithoutNotify(30);
                    SetSelectedPose();
                    var poseAfter = workspace.Document.StateHash;
                    Check(poseAfter != poseBefore && workspace.Document.DocumentRevision == poseRevision + 1, "Multi-axis pose was not committed");
                    GraphNode poseNode; NyaForge.Authoring.Rig.SkeletonDefinition poseSkeleton;
                    Check(TryResolvePose(out poseNode, out poseSkeleton), "Pose node disappeared after multi-axis edit");
                    var poseTransform = poseNode.Pose.Poses.Single(item => item.BoneId == child).Transform;
                    Check(Math.Abs(poseTransform.YAxis.Z) > .01f, "Multi-axis pose did not retain X rotation");
                    Execute(AuthoringOperation.Undo());
                    Check(workspace.Document.StateHash == poseBefore, "Multi-axis pose Undo failed");
                    Execute(AuthoringOperation.Redo());
                    Check(workspace.Document.StateHash == poseAfter, "Multi-axis pose Redo failed");
                    SetStatus("Rig weight paint: 1ドラッグを1 Undoとして反映しました。");
                }
                catch (Exception e) { failure = e.ToString(); }
            }
            finally
            {
                rigPainting = false;
                rigStrokeVertices.Clear();
                ReplaceWorkspace(previous, previousPath);
            }
            completed(failure);
        }
    }
}
