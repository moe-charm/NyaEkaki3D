using System;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        readonly struct SourceSkinDisplayCacheKey : IEquatable<SourceSkinDisplayCacheKey>
        {
            readonly string graphId;
            readonly string evaluationHash;
            readonly string poseHash;
            readonly string sourceHash;
            readonly string bindingHash;

            internal SourceSkinDisplayCacheKey(string graphId, string evaluationHash, string poseHash, string sourceHash, string bindingHash)
            {
                this.graphId = graphId ?? "";
                this.evaluationHash = evaluationHash ?? "";
                this.poseHash = poseHash ?? "";
                this.sourceHash = sourceHash ?? "";
                this.bindingHash = bindingHash ?? "";
            }

            public bool Equals(SourceSkinDisplayCacheKey other)
            {
                return string.Equals(graphId, other.graphId, StringComparison.Ordinal)
                    && string.Equals(evaluationHash, other.evaluationHash, StringComparison.Ordinal)
                    && string.Equals(poseHash, other.poseHash, StringComparison.Ordinal)
                    && string.Equals(sourceHash, other.sourceHash, StringComparison.Ordinal)
                    && string.Equals(bindingHash, other.bindingHash, StringComparison.Ordinal);
            }

            public override bool Equals(object obj) { return obj is SourceSkinDisplayCacheKey other && Equals(other); }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + graphId.GetHashCode();
                    hash = hash * 31 + evaluationHash.GetHashCode();
                    hash = hash * 31 + poseHash.GetHashCode();
                    hash = hash * 31 + sourceHash.GetHashCode();
                    return hash * 31 + bindingHash.GetHashCode();
                }
            }
        }

        readonly struct SourceSkinProjectionCacheKey : IEquatable<SourceSkinProjectionCacheKey>
        {
            readonly string previewNodeId;
            readonly string stageHash;
            readonly string finalHash;
            readonly bool showFinalResult;

            internal SourceSkinProjectionCacheKey(string previewNodeId, string stageHash, string finalHash, bool showFinalResult)
            {
                this.previewNodeId = previewNodeId ?? "";
                this.stageHash = stageHash ?? "";
                this.finalHash = finalHash ?? "";
                this.showFinalResult = showFinalResult;
            }

            public bool Equals(SourceSkinProjectionCacheKey other)
            {
                return showFinalResult == other.showFinalResult
                    && string.Equals(previewNodeId, other.previewNodeId, StringComparison.Ordinal)
                    && string.Equals(stageHash, other.stageHash, StringComparison.Ordinal)
                    && string.Equals(finalHash, other.finalHash, StringComparison.Ordinal);
            }

            public override bool Equals(object obj) { return obj is SourceSkinProjectionCacheKey other && Equals(other); }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + previewNodeId.GetHashCode();
                    hash = hash * 31 + stageHash.GetHashCode();
                    hash = hash * 31 + finalHash.GetHashCode();
                    return hash * 31 + (showFinalResult ? 1 : 0);
                }
            }
        }

        SourceSkinDisplayCacheKey sourceSkinDisplayKey;
        bool hasSourceSkinDisplayKey;
        GraphMeshValue sourceSkinDisplayValue;
        SourceSkinProjectionCacheKey sourceSkinProjectionKey;
        bool hasSourceSkinProjectionKey;

        GraphMeshValue SourceSkinDisplayValue(GraphEvaluation evaluation, AuthoringGraph graph)
        {
            if (evaluation == null || graph == null || importedRigSession?.SourceSkin == null || importedRigSession.SourceSkinBinding == null)
                return null;
            string poseHash = "";
            bool hasPoseHash = false;
            GraphNode bindingNode = null;
            foreach (var node in graph.Nodes.Values)
            {
                if (node.TypeId == BuiltinNodes.Pose && node.Pose != null)
                {
                    string candidate = node.Pose.ContentHash ?? "";
                    if (!hasPoseHash || string.CompareOrdinal(candidate, poseHash) < 0) { poseHash = candidate; hasPoseHash = true; }
                }
                if (bindingNode == null && node.TypeId == BuiltinNodes.SkinBind && node.Binding != null) bindingNode = node;
            }
            var currentBinding = bindingNode != null && evaluation.SkinBindingOutputs.TryGetValue(bindingNode.NodeId, out var bindingValue) ? bindingValue.Binding : bindingNode?.Binding;
            string bindingKey = currentBinding?.ContentHash ?? "";
            var key = new SourceSkinDisplayCacheKey(graph.GraphId, evaluation.Output?.SnapshotHash, poseHash, importedRigSession.SourceHash, bindingKey);
            if (hasSourceSkinDisplayKey && key.Equals(sourceSkinDisplayKey)) return sourceSkinDisplayValue;
            sourceSkinDisplayKey = key; hasSourceSkinDisplayKey = true; sourceSkinDisplayValue = null;
            try { sourceSkinDisplayValue = SourceSkinGraphAdapter.ApplyToEvaluation(evaluation, graph, importedRigSession, currentBinding); }
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
                hasSourceSkinDisplayKey = false; hasSourceSkinProjectionKey = false; sourceSkinDisplayValue = null; return;
            }
            var graph = workspace.Document.ActiveObject.Graph;
            var final = SourceSkinDisplayValue();
            if (final == null)
            {
                if (hasSourceSkinProjectionKey)
                {
                    hasSourceSkinProjectionKey = false;
                    using (var prepared = projection.PrepareGraph(workspace.Document, workspace.Preview)) prepared.Commit();
                }
                return;
            }
            GraphMeshValue stage = null;
            if (projection.PreviewNodeId != "") workspace.Preview.Evaluation.MeshOutputs.TryGetValue(projection.PreviewNodeId, out stage);
            var key = new SourceSkinProjectionCacheKey(projection.PreviewNodeId, stage?.SnapshotHash, final.SnapshotHash, projection.ShowFinalResult);
            if (hasSourceSkinProjectionKey && key.Equals(sourceSkinProjectionKey)) return;
            sourceSkinProjectionKey = key;
            hasSourceSkinProjectionKey = true;
            using (var prepared = projection.PrepareGraphValue(workspace.Document, stage ?? final, stage == null ? null : projection.ShowFinalResult ? final : null)) prepared.Commit();
        }

        GraphMeshValue SourceSkinPlaybackValue(GraphEvaluation evaluation, AuthoringGraph graph)
        {
            return SourceSkinDisplayValue(evaluation, graph) ?? evaluation?.Output;
        }
    }
}
