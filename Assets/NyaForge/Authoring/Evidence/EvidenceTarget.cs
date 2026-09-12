namespace NyaForge.Authoring.Evidence
{
    public enum EvidenceTargetKind { Final, NodeInput, NodeOutput }
    public sealed class EvidenceTarget
    {
        public EvidenceTargetKind Kind { get; }
        public string ObjectId { get; }
        public string GraphId { get; }
        public string NodeId { get; }
        public string PortId { get; }
        private EvidenceTarget(EvidenceTargetKind kind,string objectId,string graphId,string nodeId,string port)
        { Kind=kind;ObjectId=objectId;GraphId=graphId;NodeId=nodeId;PortId=port; }
        public static EvidenceTarget Final { get; }=new EvidenceTarget(EvidenceTargetKind.Final,null,null,null,null);
        public static EvidenceTarget NodeMesh(string objectId,string graphId,string nodeId,bool input=false)
        {
            Checks.Id(objectId);Checks.Id(graphId);Checks.Id(nodeId);
            return new EvidenceTarget(input ? EvidenceTargetKind.NodeInput : EvidenceTargetKind.NodeOutput,objectId,graphId,nodeId,"mesh");
        }
    }
}
