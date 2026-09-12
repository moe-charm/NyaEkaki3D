using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Simulation;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        // Detached candidate: validate and serialize everything before changing the workspace.
        sealed class ImportMetadataCandidate
        {
            internal readonly ImportedRigSession Rig;
            internal readonly VrmExpressionSession Expressions;
            internal readonly VrmSpringSession Springs;
            internal readonly ProjectAttachments Attachments;
            internal readonly ImportedGlbDiagnostics GlbDiagnostics;

            internal ImportMetadataCandidate(ImportedRigSession rig, VrmExpressionSession expressions, VrmMetadata metadata, ImportedGlbDiagnostics glbDiagnostics = null)
            {
                Rig = rig; Expressions = expressions;
                Springs = metadata == null ? null : VrmSpringSession.Create(metadata);
                GlbDiagnostics = glbDiagnostics;
                Attachments = PrepareImportedMetadata(Rig, Expressions, Springs);
            }
        }

        void CommitImportedGraph(AuthoringGraph graph, ImportMetadataCandidate candidate)
        {
            var attachments = candidate.Attachments;
            SecondaryMotionAsset secondary = BuildImportedSecondaryMotion(graph, candidate.Rig, candidate.Springs);
            var existing = workspace.Attachments;
            var owned = new Dictionary<string, byte[]>();
            foreach (var name in existing.Hashes.Keys) owned[name] = existing.Read(name);
            foreach (var name in attachments.Hashes.Keys) owned[name] = attachments.Read(name);
            if (candidate.GlbDiagnostics != null)
            {
                var records = new Dictionary<string, ImportedGlbDiagnostics>(StringComparer.Ordinal);
                var existingDiagnostics = workspace.Attachments.Read(ProjectAttachments.ImportDiagnostics);
                if (existingDiagnostics != null)
                    foreach (var item in ImportedGlbDiagnosticsCodec.Read(existingDiagnostics)) records.Add(item.Key, item.Value);
                records[candidate.GlbDiagnostics.GraphId] = candidate.GlbDiagnostics;
                owned[ProjectAttachments.ImportDiagnostics] = ImportedGlbDiagnosticsCodec.Write(records.Values);
            }
            if (secondary != null)
            {
                owned[ProjectAttachments.SecondaryMotion] = SecondaryMotionCodec.Write(secondary);
            }
            attachments = new ProjectAttachments(owned);
            var result = new AuthoringCommandService(workspace).Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph)), projection);
            if (!result.Success) throw new InvalidOperationException(result.Code + ": " + result.Message);
            if (candidate.Rig != null)
            {
                // The graph identity is stable across object reordering and is the
                // correct key for source data belonging to each skinned object.
                importedRigSessions[graph.GraphId] = candidate.Rig;
                owned[ProjectAttachments.RigSessions] = ImportedRigSessionsCodec.Write(importedRigSessions);
            }
            if (candidate.Expressions != null)
            {
                importedVrmSessions[graph.GraphId] = candidate.Expressions;
                owned[ProjectAttachments.Expressions] = VrmExpressionSessionsCodec.Write(importedVrmSessions);
            }
            if (candidate.Springs != null)
            {
                importedVrmSpringSessions[graph.GraphId] = candidate.Springs;
                owned[ProjectAttachments.Springs] = VrmSpringSessionsCodec.Write(importedVrmSpringSessions);
            }
            attachments = new ProjectAttachments(owned);
            workspace.SetAttachments(attachments);
            if (candidate.Rig != null) importedRigSession = candidate.Rig;
            RefreshImportedVrmSessionsForActiveGraph();
            if (secondary != null)
            {
                importedSecondaryMotionDocument = SecondaryMotionCodec.ReadDocument(SecondaryMotionCodec.Write(secondary));
                importedSecondaryMotionAsset = secondary;
            }
            RefreshVrmSpringStatus();
        }

        static SecondaryMotionAsset BuildImportedSecondaryMotion(AuthoringGraph graph, ImportedRigSession rig, VrmSpringSession springs)
        {
            if (rig == null || springs == null) return null;
            // VRM0 spring roots may legally reference source nodes that are not
            // joints of the selected skin (for example, an avatar can keep hair
            // dynamics outside the body skin). The authored secondary-motion
            // attachment uses the selected graph skeleton, so defer those
            // source-only chains to the transient preview instead of attempting
            // to index an authored pose with a preview-only bone id.
            if (springs.Format == "vrm0")
            {
                var expanded = Vrm0SpringExpansion.Resolve(springs, rig, graph);
                bool sourceOnly = expanded.Any(group =>
                    group.Targets.Any(target => !rig.NodeToBone.ContainsKey(target.NodeIndex)) ||
                    (group.CenterNodeIndex >= 0 && !rig.NodeToBone.ContainsKey(group.CenterNodeIndex)) ||
                    group.ColliderGroupIndices.Any(index => springs.ColliderGroups[index].ColliderNodeIndices.Any(node => !rig.NodeToBone.ContainsKey(node))));
                if (sourceOnly) return null;
            }
            var poseNode = graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.Pose && node.Pose != null);
            if (poseNode == null) throw new AuthoringException("INVALID_POSE", "Spring migration requires an authored pose node.");
            return springs.Format == "vrm0"
                ? VrmSecondaryMotionMigration.FromVrm0(springs, rig, graph, poseNode.Pose)
                : VrmSecondaryMotionMigration.FromVrm1(springs, rig, graph, poseNode.Pose);
        }
    }
}
