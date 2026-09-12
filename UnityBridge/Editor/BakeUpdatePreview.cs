using System.Collections.Generic;
using NyaForge.Authoring;

namespace NyaForge.UnityBridge.Editor
{
    /// <summary>Read-only target and conflict comparison. Applying updates is a separate transaction.</summary>
    public sealed class BakeUpdatePreview
    {
        public BakeOutputIdentity Source { get; }
        public ImportOwnership.Inspection Existing { get; }
        public IReadOnlyList<string> Conflicts { get; }
        public bool SameContent { get; }
        public MaterialUpdatePlan Plan { get; private set; }
        BakeUpdatePreview(BakeOutputIdentity source,ImportOwnership.Inspection existing)
        {
            Source=source;Existing=existing;var conflicts=new List<string>(existing.Conflicts);
            if(source==null || string.IsNullOrEmpty(existing.OutputId)) conflicts.Add("Explicit output identity is missing; legacy imports cannot be matched automatically.");
            else
            {
                if(source.DocumentId!=existing.DocumentId || source.ObjectId!=existing.ObjectId || source.OutputId!=existing.OutputId || source.OutputKind!=existing.OutputKind)
                    conflicts.Add("The Bake belongs to a different document or output.");
                if(source.Revision<existing.Revision) conflicts.Add("The Bake revision is older than the imported revision.");
            }
            SameContent=source!=null && source.ManifestHash==existing.ManifestHash;
            Conflicts=conflicts.AsReadOnly();
        }
        public static BakeUpdatePreview Materials(string manifestPath,string importFolder)
        {
            var source=MultiMaterialBakeStore.Read(manifestPath);
            var identity=BakeOutputIdentity.Read(manifestPath,source.Geometry);
            var preview=new BakeUpdatePreview(identity,ImportOwnership.Inspect(importFolder));
            if(preview.Conflicts.Count==0) preview.Plan=MaterialUpdatePlan.Build(source,importFolder);
            return preview;
        }
    }
}
