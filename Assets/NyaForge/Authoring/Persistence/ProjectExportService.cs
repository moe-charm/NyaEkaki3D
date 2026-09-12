using System;
using System.Linq;
namespace NyaForge.Authoring
{
    public enum ProjectExportKind { Mesh, Surface, Material, MultiMaterial, AuthoringProject }
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
                ValidateAttachmentReferences(workspace.Document);
                if (RequiresNativeProjectExport(workspace.Document))
                {
                    string nativePath = AuthoringProjectExportService.Export(workspace, instance, document, revision, directory);
                    return new ProjectExportResult(nativePath, ProjectExportKind.AuthoringProject, workspace.Document);
                }
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

        /// <summary>Returns true when a standard Bake cannot preserve the graph's typed metadata.</summary>
        public static bool RequiresNativeProjectExport(AuthoringDocument document)
        {
            if (document == null) return false;
            return document.Objects.Any(item => !item.IsStaticProfile && item.Graph.Nodes.Values.Any(node =>
                node.TypeId == Graph.BuiltinNodes.Skeleton || node.TypeId == Graph.BuiltinNodes.SkinBind ||
                node.TypeId == Graph.BuiltinNodes.Pose || node.TypeId == Graph.BuiltinNodes.SkinDeform ||
                node.TypeId == Graph.BuiltinNodes.MorphSet || node.TypeId == Graph.BuiltinNodes.MorphDeform ||
                node.TypeId == Graph.BuiltinNodes.Attachment));
        }

        /// <summary>Rejects unresolved attachment targets before any export directory is created.</summary>
        public static void ValidateAttachmentReferences(AuthoringDocument document)
        {
            if (document == null) return;
            foreach (var item in document.Objects)
            {
                if (item.IsStaticProfile || item.Graph == null) continue;
                foreach (var node in item.Graph.Nodes.Values.Where(node => node.TypeId == Graph.BuiltinNodes.Attachment))
                {
                    var target = document.Objects.FirstOrDefault(candidate => candidate.ObjectId == node.AttachmentTargetObjectId);
                    Checks.Require(target != null && !target.IsStaticProfile, "ATTACHMENT_TARGET_MISSING", "Attachment target object is not present in this document.");
                    Checks.Require(target.ObjectId != item.ObjectId, "ATTACHMENT_TARGET_SELF", "An accessory cannot attach to itself.");
                }
            }
        }
    }
}
