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
        void VerifyCuffTemplate(string output, List<string> checks)
        {
            var previous = workspace; string previousPath = savedDirectory;
            try
            {
                ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
                var button = root.Q<Button>("graph-create-cuff");
                Check(button != null && button.enabledSelf, "Cuff template button is unavailable in an empty project");
                CreateCuffGraph();
                Check(workspace.Document.Objects.Count == 1 && IsGraph, "Cuff template did not create a graph object");
                var first = DisplayedGraphValue()?.Polygon;
                Check(first != null && first.Vertices.Count == 32 * 4 && first.Faces.Count == 32 * 4, "Cuff template topology differs");
                var firstHash = workspace.Document.StateHash;
                SelectEditStage(1);
                Select(new[] { 0 }); moveX.SetValueWithoutNotify(1); moveY.SetValueWithoutNotify(0); moveZ.SetValueWithoutNotify(0); MoveSelection();
                Check(workspace.Document.StateHash != firstHash, "Cuff template edit did not advance the document");
                string project = Path.Combine(output, "cuff-template-project"); projectPath.SetValueWithoutNotify(project); SaveProject();
                string savedHash = workspace.Document.StateHash; OpenProject();
                Check(workspace.Document.Objects.Count == 1 && workspace.Document.StateHash == savedHash && !workspace.IsDirty, "Cuff template Save/Open differs");
                var manifest = ProjectExportService.Export(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision,
                    Path.Combine(output, "cuff-template-bake")).ManifestPath;
                Check(File.Exists(manifest), "Cuff template Bake manifest is missing");
                checks.Add("cuff template: closed low-poly shell creation, vertex edit, Save/Open and Bake");
            }
            finally { ReplaceWorkspace(previous, previousPath); }
        }
    }
}
