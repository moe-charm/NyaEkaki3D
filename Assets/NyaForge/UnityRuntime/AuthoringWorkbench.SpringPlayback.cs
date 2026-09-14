using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IVrmSpringPreview springPlayback;
        AuthoringWorkspace springWorkspace;
        string springDocumentHash, springMetadataHash, springPoseNode;
        Label springPlaybackStatus;
        Button springPlay, springPause, springReset, springRebuild, springStep;
        bool springAutomaticTick = true;

        void BuildSpringPlayback(VisualElement parent)
        {
            var panel = new Foldout { text = "揺れのプレビュー（VRM0 / VRM1）", value = false, name = "spring-playback" };
            springPlaybackStatus = new Label { name = "spring-playback-status" };
            springPlaybackStatus.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(springPlaybackStatus);
            springPlay = Button("再生 / 再開", () => Try(StartSpringPlayback), "spring-play");
            springPause = Button("一時停止", () => { springPlayback?.Pause(); RefreshSpringPlayback(); }, "spring-pause");
            springReset = Button("リセットして元の姿勢へ", () => { ClearSpringPlayback(true); }, "spring-reset");
            springRebuild = Button("現在の設定で再構築", () => Try(RebuildSpringPlayback), "spring-rebuild");
            springStep = Button("1固定step進む", () => Try(StepSpringPlayback), "spring-step");
            panel.Add(springPlay); panel.Add(springPause); panel.Add(springReset); panel.Add(springRebuild); panel.Add(springStep);
            var help = new Label("表示だけのプレビューです。保存・出力には編集中の姿勢を使います。編集・作品切替でリセットします。source nodeの一般TRSは保持され、揺れ表示へ反映されます。");
            help.style.whiteSpace = WhiteSpace.Normal; panel.Add(help); parent.Add(panel);
        }

        void RefreshSpringPlayback()
        {
            if (springPlaybackStatus == null) return;
            bool ready = importedRigSession != null && (importedVrmSpringSession?.Format == "vrm1" || (importedVrmSpringSession?.Format == "vrm0" && importedRigSession.Hierarchy != null)) && importedVrmSpringSession.SpringBones.Count > 0;
            springPlay.SetEnabled(ready); springPause.SetEnabled(springPlayback?.IsPlaying == true); springReset.SetEnabled(springPlayback != null);
            springRebuild?.SetEnabled(springPlayback != null); springStep?.SetEnabled(springPlayback != null);
            springPlaybackStatus.text = springPlayback == null ? (ready ? "再生できます。未対応設定は開始時に表示します。" : "対応するVRMのskinとSpring設定を読み込んでください。")
                : (springPlayback.IsPlaying ? "再生中" : "一時停止") + " · " + springPlayback.CompletedSteps + " step · 保存対象外";
        }

        void StartSpringPlayback()
        {
            if (springPlayback != null) { springPlayback.Play(); RefreshSpringPlayback(); return; }
            if (workspace == null || workspace.Document.IsEmpty || importedRigSession == null || importedVrmSpringSession == null)
                throw new InvalidOperationException("対応するVRMモデルを取り込んでください。");
            var graph = workspace.Document.ActiveObject.Graph;
            var poses = graph.Nodes.Values.Where(node => node.TypeId == BuiltinNodes.Pose).ToArray();
            if (poses.Length != 1 || !workspace.Preview.IsComplete || !workspace.Preview.Evaluation.PoseOutputs.TryGetValue(poses[0].NodeId, out var value))
                throw new InvalidOperationException("プレビューは評価済みのPose nodeが1個あるgraphに対応します。");
            IVrmSpringPreview candidate = importedVrmSpringSession.Format == "vrm0"
                ? new Vrm0SpringPreview(importedVrmSpringSession, importedRigSession, graph, value.Pose)
                : new Vrm1SpringPreview(importedVrmSpringSession, importedRigSession, graph, value.Pose);
            SelectEditStage(0);
            springWorkspace = workspace; springDocumentHash = workspace.Document.StateHash; springMetadataHash = workspace.Attachments.ContentHash;
            springPoseNode = poses[0].NodeId; springPlayback = candidate; springPlayback.Play(); RefreshSpringPlayback();
        }

        void RebuildSpringPlayback()
        {
            if (springPlayback == null) { StartSpringPlayback(); return; }
            bool wasPlaying = springPlayback.IsPlaying;
            ClearSpringPlayback(true);
            StartSpringPlayback();
            if (!wasPlaying) { springPlayback.Pause(); RefreshSpringPlayback(); }
        }

        void StepSpringPlayback()
        {
            if (springPlayback == null) throw new InvalidOperationException("先に揺れのプレビューを開始してください。");
            bool wasPlaying = springPlayback.IsPlaying;
            springPlayback.Play();
            TickSpringPlayback(1f / 60f);
            if (springPlayback != null && !wasPlaying) { springPlayback.Pause(); RefreshSpringPlayback(); }
        }

        void TickSpringPlayback(float elapsed)
        {
            if (springPlayback == null) return;
            if (workspace != springWorkspace || workspace.Document.StateHash != springDocumentHash || workspace.Attachments.ContentHash != springMetadataHash || projection.PreviewNodeId != "")
            { ClearSpringPlayback(true); return; }
            if (!springPlayback.IsPlaying) return;
            try
            {
                var graph = workspace.Document.ActiveObject.Graph;
                var basePose = workspace.Preview.Evaluation.PoseOutputs[springPoseNode].Pose;
                long previousStep = springPlayback.CompletedSteps;
                var pose = springPlayback.Advance(graph, basePose, elapsed);
                if (springPlayback.CompletedSteps != previousStep)
                {
                    var evaluated = GraphEvaluator.Evaluate(graph.ReplaceNode(GraphNode.PoseNode(springPoseNode, pose)));
                    if (!evaluated.IsComplete) throw new InvalidOperationException("揺れ姿勢のgraph評価に失敗しました。");
                    projection.ShowSpringPreview(SourceSkinPlaybackValue(evaluated, graph));
                }
                RefreshSpringPlayback();
            }
            catch (Exception error)
            {
                ClearSpringPlayback(true);
                springPlaybackStatus.text = "プレビュー停止: " + error.Message;
                SetStatus(springPlaybackStatus.text);
            }
        }

        void ClearSpringPlayback(bool restore)
        {
            bool hadPreview = springPlayback != null;
            springPlayback = null; springWorkspace = null;
            projection?.EndSpringPreview();
            // The authored evaluation hash is unchanged by transient playback, so
            // invalidate this projection key explicitly when the rendered mesh is
            // replaced by the normal graph result.
            hasSourceSkinProjectionKey = false;
            if (restore && hadPreview && workspace != null)
                using (var prepared = projection.PrepareGraph(workspace.Document, workspace.Preview)) prepared.Commit();
            if (restore && workspace != null) RefreshSourceSkinDisplayProjection();
            RefreshSpringPlayback();
        }
    }
}
