using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Evidence;
using NyaForge.Authoring.Graph;
using UnityEngine.UIElements;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        DropdownField evidenceTargetField;
        readonly List<EvidenceTarget> evidenceTargets=new List<EvidenceTarget>();
        EvidenceTarget selectedEvidenceTarget=EvidenceTarget.Final;
        void BuildEvidenceTarget(VisualElement parent)
        {
            evidenceTargetField=new DropdownField("撮影対象") { name="evidence-target" };
            evidenceTargetField.style.flexDirection=FlexDirection.Column;
            evidenceTargetField.RegisterValueChangedCallback(_=>
            {
                int index=evidenceTargetField.index;
                if(index>=0 && index<evidenceTargets.Count) selectedEvidenceTarget=evidenceTargets[index];
                RefreshEvidenceCapture();
            });
            parent.Add(evidenceTargetField);
        }
        void RefreshEvidenceTarget()
        {
            var labels=new List<string> { "最終結果" };evidenceTargets.Clear();evidenceTargets.Add(EvidenceTarget.Final);
            if(workspace!=null && !workspace.Document.IsEmpty)
            {
                var doc=workspace.Document;var graph=doc.Objects[0].Graph;
                foreach(var node in graph.Nodes.Values.OrderBy(n=>n.NodeId,StringComparer.Ordinal))
                {
                    var definition=BuiltinNodes.Find(node);if(definition==null) continue;
                    foreach(bool input in new[] { true,false })
                    {
                        var ports=input ? definition.Inputs : definition.Outputs;
                        if(!ports.Any(p=>p.Id=="mesh" && p.Type==PortType.Mesh)) continue;
                        evidenceTargets.Add(EvidenceTarget.NodeMesh(doc.ObjectId,graph.GraphId,node.NodeId,input));
                        labels.Add(node.TypeId+" · "+node.NodeId.Substring(0,8)+(input ? " · 入力" : " · 出力"));
                    }
                }
            }
            int selected=evidenceTargets.FindIndex(t=>t.Kind==selectedEvidenceTarget.Kind && t.ObjectId==selectedEvidenceTarget.ObjectId && t.GraphId==selectedEvidenceTarget.GraphId && t.NodeId==selectedEvidenceTarget.NodeId);
            if(selected<0) selected=0;
            selectedEvidenceTarget=evidenceTargets[selected];
            evidenceTargetField.choices=labels;evidenceTargetField.SetValueWithoutNotify(labels[selected]);evidenceTargetField.SetEnabled(!evidenceBusy);
        }
        bool CanCaptureEvidenceTarget()
        {
            if(workspace==null) return false;
            try { return EvaluatedSnapshot.Acquire(workspace,selectedEvidenceTarget).State==EvidenceState.Ready; }
            catch(Exception) { return false; }
        }
    }
}
