using System.Collections.Generic;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void CaptureImportedMetadata()
        {
            var bytes = new Dictionary<string, byte[]>();
            if (importedRigSession != null) bytes.Add(ProjectAttachments.Rig, ImportedRigSessionCodec.Write(importedRigSession));
            if (importedVrmSession != null) bytes.Add(ProjectAttachments.Expressions, VrmExpressionSessionCodec.Write(importedVrmSession));
            if (importedVrmSpringSession != null) bytes.Add(ProjectAttachments.Springs, VrmSpringSessionCodec.Write(importedVrmSpringSession));
            workspace.SetAttachments(new ProjectAttachments(bytes));
        }
    }
}
