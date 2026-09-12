using System;
using System.Collections.Generic;
using System.IO;
using NyaForge.Authoring;
using NyaForge.Authoring.Topology;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void VerifyChokerTemplate(string output, List<string> checks)
        {
            var previous = workspace; string previousPath = savedDirectory;
            try
            {
                ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
                var button = root.Q<Button>("graph-create-choker");
                Check(button != null && button.enabledSelf, "Choker template button is unavailable in an empty project");
                CreateChokerGraph();
                Check(workspace.Document.Objects.Count == 1 && IsGraph, "Choker template did not create a graph object");
                var first = DisplayedGraphValue()?.Polygon;
                Check(first != null && first.Vertices.Count == 24 * 8 && first.Faces.Count == 24 * 8, "Choker template topology differs");
                var firstHash = workspace.Document.StateHash;
                CreateChokerGraph();
                Check(workspace.Document.Objects.Count == 2 && IsGraph, "Choker template could not be added to an existing graph project");
                Check(workspace.Document.Objects[0].Graph != null && workspace.Document.Objects[1].Graph != null, "Choker template object profile differs");
                SelectEditStage(1);
                Select(new[] { 0 }); moveX.SetValueWithoutNotify(1); moveY.SetValueWithoutNotify(0); moveZ.SetValueWithoutNotify(0); MoveSelection();
                Check(workspace.Document.StateHash != firstHash, "Choker template edit did not advance the document");
                string project = Path.Combine(output, "choker-template-project"); projectPath.SetValueWithoutNotify(project); SaveProject();
                string savedHash = workspace.Document.StateHash; OpenProject();
                Check(workspace.Document.Objects.Count == 2 && workspace.Document.StateHash == savedHash && !workspace.IsDirty, "Choker template Save/Open differs");
                var manifest = MultiObjectExportService.Export(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision,
                    Path.Combine(output, "choker-template-bake")).ManifestPath;
                Check(MultiObjectExportService.Read(manifest).Objects.Count == 2, "Choker template multi-object bake differs");
                checks.Add("choker template: low-poly ring creation, add-to-avatar graph project, vertex edit, Save/Open and multi-object bake");
            }
            finally { ReplaceWorkspace(previous, previousPath); }
        }
    }
}
