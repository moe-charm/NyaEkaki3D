using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        string sourceSkinDisplayKey;
        GraphMeshValue sourceSkinDisplayValue;
        string sourceSkinProjectionKey;

        GraphMeshValue SourceSkinDisplayValue(GraphEvaluation evaluation, AuthoringGraph graph)
        {
            if (evaluation == null || graph == null || importedRigSession?.SourceSkin == null || importedRigSession.SourceSkinBinding == null)
                return null;
            string poseHash = graph.Nodes.Values.Where(node => node.TypeId == BuiltinNodes.Pose && node.Pose != null)
                .Select(node => node.Pose.ContentHash).OrderBy(hash => hash, StringComparer.Ordinal).FirstOrDefault() ?? "";
            string key = graph.GraphId + ":" + (evaluation.Output?.SnapshotHash ?? "") + ":" + poseHash + ":" + importedRigSession.SourceHash;
            if (key == sourceSkinDisplayKey) return sourceSkinDisplayValue;
            sourceSkinDisplayKey = key; sourceSkinDisplayValue = null;
            try { sourceSkinDisplayValue = SourceSkinGraphAdapter.ApplyToEvaluation(evaluation, graph, importedRigSession); }
            catch (AuthoringException) { }
            return sourceSkinDisplayValue;
        }

        GraphMeshValue SourceSkinDisplayValue()
        {
            if (!IsGraph || workspace?.Preview?.Evaluation == null) return null;
            return SourceSkinDisplayValue(workspace.Preview.Evaluation, workspace.Document.ActiveObject.Graph);
        }

        void RefreshSourceSkinDisplayProjection()
        {
            if (!IsGraph || workspace?.Preview?.Evaluation == null)
            {
                sourceSkinDisplayKey = sourceSkinProjectionKey = ""; sourceSkinDisplayValue = null; return;
            }
            var graph = workspace.Document.ActiveObject.Graph;
            var final = SourceSkinDisplayValue();
            if (final == null)
            {
                if (sourceSkinProjectionKey != "")
                {
                    sourceSkinProjectionKey = "";
                    using (var prepared = projection.PrepareGraph(workspace.Document, workspace.Preview)) prepared.Commit();
                }
                return;
            }
            GraphMeshValue stage = null;
            if (projection.PreviewNodeId != "") workspace.Preview.Evaluation.MeshOutputs.TryGetValue(projection.PreviewNodeId, out stage);
            string key = projection.PreviewNodeId + ":" + (stage?.SnapshotHash ?? "") + ":" + final.SnapshotHash + ":" + projection.ShowFinalResult;
            if (key == sourceSkinProjectionKey) return;
            sourceSkinProjectionKey = key;
            using (var prepared = projection.PrepareGraphValue(workspace.Document, stage ?? final, stage == null ? null : projection.ShowFinalResult ? final : null)) prepared.Commit();
        }

        GraphMeshValue SourceSkinPlaybackValue(GraphEvaluation evaluation, AuthoringGraph graph)
        {
            return SourceSkinDisplayValue(evaluation, graph) ?? evaluation?.Output;
        }
    }
}
