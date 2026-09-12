using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Graph;
namespace NyaForge.Authoring.Evidence
{
    public enum EvidenceState { Empty, Ready, Faceless, Incomplete }
    /// <summary>One atomic acquisition of committed final output; never substitutes a stale preview.</summary>
    public sealed class EvaluatedSnapshot
    {
        public string SnapshotId { get; }=Guid.NewGuid().ToString("D");
        public string InstanceId { get; }
        public string DocumentId { get; }
        public long DocumentRevision { get; }
        public string StateHash { get; }
        public string ObjectId { get; }
        public string GraphId { get; }
        public string OutputNodeId { get; }
        public EvidenceState State { get; }
        public EvidenceTarget Target { get; }
        public bool FinalEvaluationComplete { get; }
        public long? StalePreviewRevision { get; }
        public GraphMeshValue Value { get; }
        public IReadOnlyList<GraphDiagnostic> Diagnostics { get; }
        public EvidenceMetrics Metrics { get; }
        public string OutputContentHash=>Value?.SnapshotHash;
        private EvaluatedSnapshot(AuthoringWorkspace workspace,EvidenceTarget target)
        {
            var doc=workspace.Document;var preview=workspace.Preview;
            Target=target;FinalEvaluationComplete=preview.IsComplete;
            if(preview.IsStale) StalePreviewRevision=preview.OutputRevision;
            InstanceId=workspace.InstanceId;DocumentId=doc.DocumentId;DocumentRevision=doc.DocumentRevision;StateHash=doc.StateHash;
            ObjectId=doc.ObjectId;GraphId=doc.IsEmpty ? null : doc.Objects[0].Graph.GraphId;OutputNodeId=doc.IsEmpty ? null : doc.Objects[0].Graph.OutputNodeId;
            Diagnostics=Array.AsReadOnly(preview.Evaluation?.Diagnostics.ToArray() ?? Array.Empty<GraphDiagnostic>());
            if(target.Kind!=EvidenceTargetKind.Final)
            {
                Checks.Require(!doc.IsEmpty && target.ObjectId==ObjectId && target.GraphId==GraphId,"EVIDENCE_TARGET_STALE","Evidence object or graph no longer matches.");
                var graph=doc.Objects[0].Graph;
                Checks.Require(graph.Nodes.TryGetValue(target.NodeId,out var node),"NODE_NOT_FOUND","Evidence node is missing.");
                var definition=BuiltinNodes.Find(node);
                var ports=target.Kind==EvidenceTargetKind.NodeInput ? definition?.Inputs : definition?.Outputs;
                Checks.Require(ports!=null && ports.Any(p=>p.Id==target.PortId && p.Type==PortType.Mesh),"EVIDENCE_TARGET_UNSUPPORTED","Select a supported mesh port.");
                var values=target.Kind==EvidenceTargetKind.NodeInput ? preview.Evaluation.MeshInputs : preview.Evaluation.MeshOutputs;
                if(values.TryGetValue(target.NodeId,out var value))
                { Value=value;State=value.Mesh==null ? EvidenceState.Faceless : EvidenceState.Ready;Metrics=new EvidenceMetrics(value); }
                else State=EvidenceState.Incomplete;
            }
            else if(doc.IsEmpty) State=EvidenceState.Empty;
            else if(!preview.IsComplete || preview.IsStale || preview.OutputRevision!=doc.DocumentRevision)
            { State=EvidenceState.Incomplete;StalePreviewRevision=preview.IsStale ? preview.OutputRevision : null; }
            else
            { Value=preview.Output;State=Value.Mesh==null ? EvidenceState.Faceless : EvidenceState.Ready;Metrics=new EvidenceMetrics(Value); }
        }
        public static EvaluatedSnapshot Acquire(AuthoringWorkspace workspace,EvidenceTarget target=null)
        {
            if(workspace==null) throw new ArgumentNullException(nameof(workspace));
            lock(workspace.Gate)
            {
                Checks.Require(!workspace.Executing,"REENTRANT_EVIDENCE","Cannot acquire evidence during a command transaction.");
                return new EvaluatedSnapshot(workspace,target ?? EvidenceTarget.Final);
            }
        }
    }
}

