using System;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;

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
            var result = new AuthoringCommandService(workspace).Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph)), projection);
            if (!result.Success) throw new InvalidOperationException(result.Code + ": " + result.Message);
            workspace.SetAttachments(candidate.Attachments);
            importedRigSession = candidate.Rig;
            importedVrmSession = candidate.Expressions;
            importedVrmSpringSession = candidate.Springs;
            RefreshVrmSpringStatus();
        }
    }
}
