using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring.Inspection
{
    internal static class InspectionMesh
    {
        // Caller holds workspace.Gate until all response data has been copied.
        internal static GraphMeshValue Resolve(AuthoringWorkspace workspace,string instance,string documentId,long revision,string nodeId,bool input,string snapshotHash)
        {
            AuthoringStateReader.Read(workspace,instance);
            var doc=workspace.Document;
            Checks.Require(doc.DocumentId==documentId,"DOCUMENT_CHANGED","Document identity changed.");
            Checks.Require(doc.DocumentRevision==revision,"REVISION_CONFLICT","Document revision changed.");
            Checks.Require(!doc.IsEmpty && nodeId!=null && doc.Objects[0].Graph.Nodes.ContainsKey(nodeId),"NODE_NOT_FOUND","Node is not in the current graph.");
            var values=input ? workspace.Preview.Evaluation.MeshInputs : workspace.Preview.Evaluation.MeshOutputs;
            Checks.Require(values.TryGetValue(nodeId,out var value) && value!=null,"MESH_UNAVAILABLE","Current node mesh is unresolved.");
            Checks.Require(value.SnapshotHash==snapshotHash,"SNAPSHOT_CHANGED","Evaluated mesh identity changed.");
            return value;
        }
    }
}
