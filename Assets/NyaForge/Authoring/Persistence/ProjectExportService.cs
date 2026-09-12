using System;
namespace NyaForge.Authoring
{
    public enum ProjectExportKind { Mesh, Surface, Material, MultiMaterial }
    public sealed class ProjectExportResult
    {
        public string ManifestPath { get; }
        public ProjectExportKind Kind { get; }
        public string DocumentId { get; }
        public long Revision { get; }
        public string StateHash { get; }
        internal ProjectExportResult(string path,ProjectExportKind kind,AuthoringDocument doc)
        { ManifestPath=path;Kind=kind;DocumentId=doc.DocumentId;Revision=doc.DocumentRevision;StateHash=doc.StateHash; }
    }
    /// <summary>Version-observed routing to existing Bake profiles without dropping image/material attributes.</summary>
    public static class ProjectExportService
    {
        public static ProjectExportResult Export(AuthoringWorkspace workspace,string instance,string document,long revision,string directory)
        {
            if(workspace==null) throw new ArgumentNullException(nameof(workspace));
            lock(workspace.Gate)
            {
                Checks.Require(!workspace.Executing,"REENTRANT_EXPORT","Cannot export during a projection transaction.");
                Checks.Require(workspace.InstanceId==instance,"STALE_INSTANCE","Export targets another instance.");
                Checks.Require(workspace.Document.DocumentId==document,"DOCUMENT_CHANGED","Export targets another document.");
                Checks.Require(workspace.Document.DocumentRevision==revision,"REVISION_CONFLICT","Document changed before export.");
                Checks.Require(!workspace.Document.IsEmpty,"NO_EXPORTABLE_OBJECT","Add a mesh before exporting.");
                Checks.Require(workspace.Preview.IsComplete && !workspace.Preview.IsStale,"GRAPH_INCOMPLETE","Export requires complete current evaluation.");
                var value=workspace.Preview.Output;
                var kind=value.SlotMaterials!=null ? ProjectExportKind.MultiMaterial : value.Material!=null ? ProjectExportKind.Material : value.BaseColor!=null ? ProjectExportKind.Surface : ProjectExportKind.Mesh;
                string path;
                switch(kind)
                {
                    case ProjectExportKind.MultiMaterial:path=MultiMaterialBakeStore.Export(directory,workspace);break;
                    case ProjectExportKind.Material:path=MaterialBakeStore.Export(directory,workspace);break;
                    case ProjectExportKind.Surface:path=SurfaceBakeStore.Export(directory,workspace);break;
                    default:path=BakeStore.Export(directory,workspace);break;
                }
                return new ProjectExportResult(path,kind,workspace.Document);
            }
        }
    }
}
