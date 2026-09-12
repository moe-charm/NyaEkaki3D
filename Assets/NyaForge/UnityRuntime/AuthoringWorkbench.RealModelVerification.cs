using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        /// <summary>
        /// Optional local acceptance path for a real GLB/VRM supplied on the Player command line.
        /// It is intentionally opt-in so the public fixture suite never depends on private assets.
        /// </summary>
        void VerifyCommandLineModelImport(string path, string output, List<string> checks)
        {
            path = Path.GetFullPath(path);
            Check(File.Exists(path), "Command-line model fixture was not found: " + path);
            string extension = Path.GetExtension(path);
            Check(string.Equals(extension, ".glb", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".vrm", StringComparison.OrdinalIgnoreCase), "Command-line model fixture must be .glb or .vrm.");
            ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
            modelImportPath.SetValueWithoutNotify(path);
            // RadDollV3 and similar VRM files commonly put the body at mesh 1. The
            // candidate inspector remains the source of truth; callers may pass a
            // different selected instance through the normal GUI when needed.
            modelImportMeshIndex.SetValueWithoutNotify(1);
            modelImportSkinIndex.SetValueWithoutNotify(0);
            modelImportInstanceIndex.SetValueWithoutNotify(-1);
            InspectModelSelection(path);
            ImportModel(path);
            Check(!workspace.Document.IsEmpty && workspace.Document.ActiveObject.Graph.Nodes.Values.Any(node => node.TypeId == BuiltinNodes.EditMesh), "Real model import did not publish an editable graph stage.");
            var importedPaints = workspace.Document.ActiveObject.Graph.Nodes.Values
                .Where(node => node.TypeId == BuiltinNodes.Paint && node.PaintImage != null).ToArray();
            Check(importedPaints.All(node => node.PaintImage.Width <= 1024 && node.PaintImage.Height <= 1024), "Imported base-color Paint exceeded the native 1024px budget.");
            if (importedPaints.Length > 0)
                checks.Add("embedded base-color images are owned by native Paint and fit the 1024px budget");
            SelectEditStage(1); Select(new[] { 0 });
            moveX.SetValueWithoutNotify(1); moveY.SetValueWithoutNotify(0); moveZ.SetValueWithoutNotify(0);
            string before = GraphEvaluator.Evaluate(workspace.Document.ActiveObject.Graph).Output.Mesh.ContentHash;
            MoveSelection();
            string edited = GraphEvaluator.Evaluate(workspace.Document.ActiveObject.Graph).Output.Mesh.ContentHash;
            Check(before != edited, "Real model vertex edit did not change graph output.");
            string project = Path.Combine(output, "real-model-project");
            projectPath.SetValueWithoutNotify(project);
            Check(TrySaveProject(), "Real model project save failed.");
            OpenProject();
            Check(GraphEvaluator.Evaluate(workspace.Document.ActiveObject.Graph).Output.Mesh.ContentHash == edited, "Real model Save/Open changed edited geometry.");
            string glb = Path.Combine(project, "exports", "real-model-skinned");
            var export = GlbExportService.ExportSkinned(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, glb);
            Check(File.Exists(export.Path), "Real model standard skinned GLB was not created.");
            var exportedBytes = File.ReadAllBytes(export.Path);
            var exportedInventory = GlbSceneInventoryReader.Read(exportedBytes);
            Check(exportedInventory.Meshes.Count == 1 && exportedInventory.Skins.Count == 1, "Real model skinned GLB output did not retain one mesh and skin.");
            var exportedSkin = GlbSkinImporter.Read(exportedBytes, 0, 0);
            Check(exportedSkin.Mesh.Positions.Count > 0 && exportedSkin.Skeleton.Bones.Count == importedRigSession.SourceSkin.Joints.Count, "Real model skinned GLB output changed mesh or skeleton cardinality.");
            checks.Add("real GLB/VRM command-line import: candidate selection, generated EditMesh, vertex edit, native Save/Open, standard skinned GLB output and reimport cardinality");
            // Return the verifier to a freshly persisted empty project so the
            // following fixture suite starts with a clean command history and
            // no projection left over from the large imported mesh.
            string empty = Path.Combine(output, "real-model-empty");
            ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
            projectPath.SetValueWithoutNotify(empty);
            Check(TrySaveProject(), "Real model smoke could not persist its clean continuation state.");
            OpenProject();
        }
    }
}
