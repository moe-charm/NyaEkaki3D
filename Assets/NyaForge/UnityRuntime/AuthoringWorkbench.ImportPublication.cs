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

            internal ImportMetadataCandidate(ImportedRigSession rig, VrmExpressionSession expressions, VrmMetadata metadata)
            {
                Rig = rig; Expressions = expressions;
                Springs = metadata == null ? null : VrmSpringSession.Create(metadata);
                Attachments = PrepareImportedMetadata(Rig, Expressions, Springs);
            }
        }

        void CommitImportedGraph(AuthoringGraph graph, ImportMetadataCandidate candidate)
        {
            var attachments = candidate.Attachments;
            SecondaryMotionAsset secondary = BuildImportedSecondaryMotion(graph, candidate.Rig, candidate.Springs);
            if (secondary != null)
            {
                var owned = new Dictionary<string, byte[]>(); foreach (var name in attachments.Hashes.Keys) owned[name] = attachments.Read(name);
                owned[ProjectAttachments.SecondaryMotion] = SecondaryMotionCodec.Write(secondary);
                attachments = new ProjectAttachments(owned);
            }
            var result = new AuthoringCommandService(workspace).Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph)), projection);
            if (!result.Success) throw new InvalidOperationException(result.Code + ": " + result.Message);
            workspace.SetAttachments(attachments);
            importedRigSession = candidate.Rig;
            importedVrmSession = candidate.Expressions;
            importedVrmSpringSession = candidate.Springs;
            importedSecondaryMotionDocument = secondary == null ? null : SecondaryMotionCodec.ReadDocument(SecondaryMotionCodec.Write(secondary));
            importedSecondaryMotionAsset = secondary;
            RefreshVrmSpringStatus();
        }

        static SecondaryMotionAsset BuildImportedSecondaryMotion(AuthoringGraph graph, ImportedRigSession rig, VrmSpringSession springs)
        {
            if (rig == null || springs == null) return null;
            var poseNode = graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.Pose && node.Pose != null);
            if (poseNode == null) throw new AuthoringException("INVALID_POSE", "Spring migration requires an authored pose node.");
            return springs.Format == "vrm0"
                ? VrmSecondaryMotionMigration.FromVrm0(springs, rig, graph, poseNode.Pose)
                : VrmSecondaryMotionMigration.FromVrm1(springs, rig, graph, poseNode.Pose);
        }
    }
}
