using System.Collections.Generic;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        static ProjectAttachments PrepareImportedMetadata(ImportedRigSession rig, VrmExpressionSession expressions, VrmSpringSession springs)
        {
            var bytes = new Dictionary<string, byte[]>();
            if (rig != null) bytes.Add(ProjectAttachments.Rig, ImportedRigSessionCodec.Write(rig));
            if (expressions != null) bytes.Add(ProjectAttachments.Expressions, VrmExpressionSessionCodec.Write(expressions));
            if (springs != null) bytes.Add(ProjectAttachments.Springs, VrmSpringSessionCodec.Write(springs));
            return new ProjectAttachments(bytes);
        }
    }
}
