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
            var importedPaintHashes = importedPaints.OrderBy(node => node.NodeId, StringComparer.Ordinal)
                .Select(node => Checks.Hash(PaintImageCodec.Write(node.PaintImage))).ToArray();
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
            var reopenedPaintHashes = workspace.Document.ActiveObject.Graph.Nodes.Values
                .Where(node => node.TypeId == BuiltinNodes.Paint && node.PaintImage != null)
                .OrderBy(node => node.NodeId, StringComparer.Ordinal)
                .Select(node => Checks.Hash(PaintImageCodec.Write(node.PaintImage))).ToArray();
            Check(importedPaintHashes.SequenceEqual(reopenedPaintHashes), "Real model Save/Open changed owned base-color Paint images.");
            string glb = Path.Combine(project, "exports", "real-model-skinned");
            var export = GlbExportService.ExportSkinned(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, glb);
            Check(File.Exists(export.Path), "Real model standard skinned GLB was not created.");
            var exportedBytes = File.ReadAllBytes(export.Path);
            var exportedInventory = GlbSceneInventoryReader.Read(exportedBytes);
            Check(exportedInventory.Meshes.Count == 1 && exportedInventory.Skins.Count == 1, "Real model skinned GLB output did not retain one mesh and skin.");
            var exportedSkin = GlbSkinImporter.Read(exportedBytes, 0, 0);
            Check(exportedSkin.Mesh.Positions.Count > 0 && exportedSkin.Skeleton.Bones.Count == importedRigSession.SourceSkin.Joints.Count, "Real model skinned GLB output changed mesh or skeleton cardinality.");
            if (importedPaints.Length > 0)
            {
                int exportedImages = exportedSkin.Materials.Count(material => material.HasEmbeddedBaseColorImage);
                Check(exportedImages > 0, "Real model skinned GLB output lost embedded base-color images.");
                checks.Add("standard skinned GLB output retains embedded base-color images");
            }
            var expectedStatic = SourceSkinDisplayValue(workspace.Preview.Evaluation, workspace.Document.ActiveObject.Graph) ?? workspace.Preview.Evaluation.Output;
            string staticGlb = Path.Combine(project, "exports", "real-model-static");
            var staticExport = GlbExportService.ExportStaticWithOverrides(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, staticGlb, StaticDisplayMeshesForExport());
            var staticImported = GlbImporter.Read(File.ReadAllBytes(staticExport.Path));
            Check(expectedStatic != null && expectedStatic.Mesh != null && staticImported.Mesh.TriangleCount == expectedStatic.Mesh.TriangleCount,
                "Real model static GLB output changed the display mesh topology.");
            // Material-slot export may compact each primitive to the vertices it
            // actually references. Compare the emitted triangle positions in
            // primitive order instead of requiring the original shared vertex
            // domain to survive byte-for-byte.
            float maxPositionDelta = 0f;
            Check(staticImported.Mesh.Submeshes.Count == expectedStatic.Mesh.Submeshes.Count,
                "Real model static GLB output changed the display mesh submesh layout.");
            for (int submesh = 0; submesh < expectedStatic.Mesh.Submeshes.Count; submesh++)
            {
                var expectedIndices = expectedStatic.Mesh.Submeshes[submesh];
                var actualIndices = staticImported.Mesh.Submeshes[submesh];
                Check(actualIndices.Length == expectedIndices.Length,
                    "Real model static GLB output changed a display mesh primitive.");
                for (int index = 0; index < expectedIndices.Length; index++)
                {
                    var a = expectedStatic.Mesh.Positions[expectedIndices[index]];
                    var b = staticImported.Mesh.Positions[actualIndices[index]];
                    maxPositionDelta = Math.Max(maxPositionDelta, Math.Max(Math.Abs(a.X - b.X), Math.Max(Math.Abs(a.Y - b.Y), Math.Abs(a.Z - b.Z))));
                }
            }
            Check(maxPositionDelta <= 1e-5f, "Real model static GLB output did not match the display-corrected vertex positions (max delta " + maxPositionDelta.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ").");
            checks.Add("standard static GLB output matches the source-skin display mesh");
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

        void VerifyCommandLineAllModelImport(string path, string output, List<string> checks)
        {
            path = Path.GetFullPath(path);
            Check(File.Exists(path), "All-mesh command-line model fixture was not found: " + path);
            string extension = Path.GetExtension(path);
            Check(string.Equals(extension, ".glb", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".vrm", StringComparison.OrdinalIgnoreCase), "All-mesh command-line model fixture must be .glb or .vrm.");
            var bytes = ReadModelFile(path);
            var inventory = GlbSceneInventoryReader.Read(bytes);
            int expected = inventory.Instances.Count > 0 ? inventory.Instances.Count : inventory.Meshes.Count;
            Check(expected > 1 && expected <= 64, "All-mesh command-line fixture must contain two to 64 mesh instances.");

            ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
            modelImportPath.SetValueWithoutNotify(path);
            ImportAllModelInstances(path);
            Check(workspace.Document.Objects.Count == expected, "All-mesh command-line import did not publish every mesh instance.");
            int expectedRigs = inventory.Instances.Count(item => item.SkinIndex.HasValue);
            Check(importedRigSessions.Count == expectedRigs, "All-mesh command-line import did not retain one rig session per skinned graph.");
            Check(workspace.Document.Objects.All(item => item.Graph.Nodes.Values.Any(node => node.TypeId == BuiltinNodes.EditMesh)), "All-mesh command-line import did not make every graph editable.");
            string state = workspace.Document.StateHash;
            string metadata = workspace.Attachments.ContentHash;
            string project = Path.Combine(output, "all-model-project");
            projectPath.SetValueWithoutNotify(project);
            Check(TrySaveProject(), "All-mesh command-line project save failed.");

            ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
            projectPath.SetValueWithoutNotify(project);
            OpenProject();
            Check(workspace.Document.Objects.Count == expected && workspace.Document.StateHash == state && workspace.Attachments.ContentHash == metadata, "All-mesh command-line Save/Open changed project state.");
            var rigBytes = workspace.Attachments.Read(ProjectAttachments.RigSessions);
            var reopenedRigs = rigBytes == null ? new Dictionary<string, ImportedRigSession>(StringComparer.Ordinal) : ImportedRigSessionsCodec.Read(rigBytes);
            Check(reopenedRigs.Count == expectedRigs && workspace.Document.Objects.Where(item => !item.IsStaticProfile).All(item => reopenedRigs.ContainsKey(item.Graph.GraphId)), "All-mesh command-line Save/Open lost graph-keyed rig sessions.");
            var diagnosticsBytes = workspace.Attachments.Read(ProjectAttachments.ImportDiagnostics);
            Check(diagnosticsBytes != null, "All-mesh command-line Save/Open lost GLB source locators.");
            var diagnostics = ImportedGlbDiagnosticsCodec.Read(diagnosticsBytes);
            Check(diagnostics.Count == expected && diagnostics.Values.All(item => item.SourceHash == inventory.SourceHash), "All-mesh command-line source locator count or hash changed after Save/Open.");
            var expectedLocatorValues = inventory.Instances.Count > 0
                ? inventory.Instances.Select(item => item.MeshIndex + ":" + (item.SkinIndex.HasValue ? item.SkinIndex.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "-1"))
                : inventory.Meshes.Select((item, index) => index + ":-1");
            var expectedLocators = expectedLocatorValues
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var actualLocators = diagnostics.Values.Select(item => item.MeshIndex + ":" + (item.SkinIndex.HasValue ? item.SkinIndex.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "-1"))
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            Check(expectedLocators.SequenceEqual(actualLocators), "All-mesh command-line source locators changed mesh/skin selection after Save/Open.");

            string package = Path.Combine(output, "all-model-package");
            var exported = ProjectExportService.Export(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, package);
            var reopenedPackage = ProjectStore.Open(Path.GetDirectoryName(exported.ManifestPath));
            Check(exported.Kind == ProjectExportKind.AuthoringProject && reopenedPackage.Document.Objects.Count == expected && reopenedPackage.Document.StateHash == workspace.Document.StateHash && reopenedPackage.Attachments.ContentHash == workspace.Attachments.ContentHash, "All-mesh command-line native export did not roundtrip.");
            checks.Add("real GLB/VRM all-mesh command-line import: every mesh instance editable, native Save/Open and feature-preserving native export roundtrip");

            if (expectedRigs == expected)
            {
                // Exercise the same export adapters as the production GUI/MCP
                // path. In particular, inverse-bind entries are reordered by
                // stable BoneId and authored source joint frames are emitted as
                // parent-relative local matrices; passing raw source-slot arrays
                // here would leave the real-model smoke blind to those rules.
                var transforms = SkinnedNodeTransformsForExport();
                var inverseBinds = SkinnedInverseBindMatrices();
                var jointLocals = SkinnedJointLocalTransforms();
                string glbDirectory = Path.Combine(output, "all-model-skinned-glb");
                var glb = GlbExportService.ExportSkinnedExtendedWithTransforms(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, glbDirectory, transforms, inverseBinds, jointLocals);
                var glbInventory = GlbSceneInventoryReader.Read(File.ReadAllBytes(glb.Path));
                Check(glb.ObjectCount == expected && glbInventory.Meshes.Count == expected && glbInventory.Skins.Count > 0, "All-mesh command-line extended skinned GLB did not retain every mesh instance.");
                checks.Add("real GLB/VRM all-mesh extended skinned GLB output retains every mesh instance with one or more validated skin resources");
            }

            string empty = Path.Combine(output, "all-model-empty");
            ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
            projectPath.SetValueWithoutNotify(empty);
            Check(TrySaveProject(), "All-mesh command-line smoke could not persist its clean continuation state.");
            OpenProject();
        }

        void VerifyCommandLineVrmExport(string output, List<string> checks)
        {
            string project = Path.Combine(output, "real-model-project");
            Check(File.Exists(Path.Combine(project, ProjectStore.ManifestName)), "VRM export check requires the persisted real-model project.");
            // The preceding model smoke deliberately returns the Player to a
            // clean empty project. Reopen the persisted avatar before invoking
            // the VRM exporter so its graph-keyed rig/session data is active.
            projectPath.SetValueWithoutNotify(project);
            OpenProject();
            // RadDollV3 is a VRM 0.x source. Keep the export check focused on
            // the VRM 1.0 humanoid package; VRM 0.x SpringBone is intentionally
            // not auto-converted by the production exporter.
            if (importedVrmSpringSession?.Format == "vrm0")
            {
                importedVrmSpringSessions.Remove(workspace.Document.ActiveObject.Graph.GraphId);
                importedVrmSpringSession = null;
                checks.Add("real model VRM 0.x SpringBone was explicitly omitted from the VRM 1.0 export check");
            }
            vrmName.SetValueWithoutNotify("NyaForge real-model verification");
            vrmAuthors.SetValueWithoutNotify("NyaForge");
            vrmLicenseUrl.SetValueWithoutNotify("https://example.com/nyaforge-verification-license");
            ExportVrm1();
            string exports = Path.Combine(project, "exports");
            var directories = Directory.Exists(exports) ? Directory.GetDirectories(exports, "vrm1-*") : Array.Empty<string>();
            Check(directories.Length == 1, "Real model VRM 1.0 export did not create exactly one output directory.");
            string model = Path.Combine(directories[0], VrmExportService.FileName);
            string report = Path.Combine(directories[0], VrmExportService.ReportFileName);
            Check(File.Exists(model) && File.Exists(report), "Real model VRM 1.0 export is missing model.vrm or export-report.json.");
            var metadata = VrmMetadataReader.Read(File.ReadAllBytes(model));
            Check(metadata.Format == "vrm1" && metadata.HumanoidNodes.Count >= 15, "Real model VRM 1.0 export did not retain humanoid metadata.");
            string sourceGlb = Path.Combine(project, "exports", "real-model-skinned", "model.glb");
            Check(File.Exists(sourceGlb), "Real model VRM geometry comparison source GLB is missing.");
            var sourceSkin = GlbSkinImporter.Read(File.ReadAllBytes(sourceGlb), 0, 0);
            var vrmSkin = GlbSkinImporter.Read(File.ReadAllBytes(model), 0, 0);
            Check(sourceSkin.Mesh.TopologyHash == vrmSkin.Mesh.TopologyHash && sourceSkin.Mesh.VertexCount == vrmSkin.Mesh.VertexCount && sourceSkin.Mesh.TriangleCount == vrmSkin.Mesh.TriangleCount && sourceSkin.Skeleton.Bones.Count == vrmSkin.Skeleton.Bones.Count,
                "Real model VRM 1.0 package changed the GLB geometry or skeleton cardinality.");
            checks.Add("VRM 1.0 package preserves source skinned GLB topology, vertex/triangle counts and skeleton cardinality");
            checks.Add("real GLB/VRM command-line import: VRM 1.0 package export and metadata re-read");
            // Leave the common fixture suite in its expected empty startup
            // state after this opt-in export check.
            string empty = Path.Combine(output, "real-model-empty");
            projectPath.SetValueWithoutNotify(empty);
            OpenProject();
        }
    }
}
