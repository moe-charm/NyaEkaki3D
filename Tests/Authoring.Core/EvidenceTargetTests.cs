using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Evidence;
using NyaForge.Authoring.Graph;
internal static partial class Program
{
    static void RunEvidenceTargetTests()
    {
        Test("evidence node input output stay distinct while final is incomplete",()=>
        {
            string plane,edit;var graph=PlaneGraph(out plane,out edit);var w=AuthoringWorkspace.CreateEmpty();Ok(Execute(w,AuthoringOperation.AddGraph(graph)));
            var input=EvidenceTarget.NodeMesh(w.Document.ObjectId,graph.GraphId,edit,true);var output=EvidenceTarget.NodeMesh(w.Document.ObjectId,graph.GraphId,edit);
            Ok(Execute(w,AuthoringOperation.TranslateGraphVertices(GraphEditing.Context(graph,edit),new[]{0},new Vec3(.02f,0,0))));
            var a=EvaluatedSnapshot.Acquire(w,input);var b=EvaluatedSnapshot.Acquire(w,output);True(a.OutputContentHash!=b.OutputContentHash);Equal(EvidenceTargetKind.NodeInput,a.Target.Kind);
            Ok(Execute(w,AuthoringOperation.Disconnect(graph.OutputNodeId,"mesh")));var partial=EvaluatedSnapshot.Acquire(w,output);
            Equal(EvidenceState.Ready,partial.State);True(!partial.FinalEvaluationComplete);True(partial.Diagnostics.Count>0);Equal(b.OutputContentHash,partial.OutputContentHash);
            Equal(EvidenceState.Incomplete,EvaluatedSnapshot.Acquire(w).State);
            Expect("EVIDENCE_TARGET_STALE",()=>EvaluatedSnapshot.Acquire(w,EvidenceTarget.NodeMesh(GraphId(),graph.GraphId,edit)));
            Expect("EVIDENCE_TARGET_UNSUPPORTED",()=>EvaluatedSnapshot.Acquire(w,EvidenceTarget.NodeMesh(w.Document.ObjectId,graph.GraphId,plane,true)));
            Ok(Execute(w,AuthoringOperation.Disconnect(edit,"mesh")));Equal(EvidenceState.Incomplete,EvaluatedSnapshot.Acquire(w,input).State);
        });
        Test("evidence material and image content survive later material edits",()=>
        {
            var f=MaterialFixture();var w=AuthoringWorkspace.CreateEmpty();Ok(Execute(w,AuthoringOperation.AddGraph(f.graph)));var original=EvaluatedSnapshot.Acquire(w);
            Equal(1,original.Metrics.AssignedMaterialCount);True(original.Metrics.ImageHashes.Contains(original.Value.BaseColor.ImageHash));
            var bytes=original.Value.BaseColor.Image.CopyRgba();string material=original.Value.Material.Parameters.ContentHash;
            Ok(Execute(w,AuthoringOperation.ReplaceGraph(f.graph.ReplaceNode(GraphNode.StandardMaterial(f.material,new MaterialParameters(new Vec4(.2f,.4f,.6f,1),.7f,.3f,new Vec3(),MaterialAlphaMode.Opaque,.5f))))));
            True(EvaluatedSnapshot.Acquire(w).OutputContentHash!=original.OutputContentHash);Equal(material,original.Value.Material.Parameters.ContentHash);True(bytes.SequenceEqual(original.Value.BaseColor.Image.CopyRgba()));
        });
    }
}
