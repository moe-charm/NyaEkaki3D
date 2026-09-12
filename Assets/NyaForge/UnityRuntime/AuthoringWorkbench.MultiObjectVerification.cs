using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void VerifyMultiObjectDisplay(string output, List<string> checks)
        {
            var previous = workspace;
            string previousPath = projectPath.value;
            bool previousVisibility = showAllObjects.value;
            try
            {
                ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
                CreatePlaneGraph();
                CreatePlaneGraph();
                Check(workspace.Document.Objects.Count == 2 && objectProjection.EntryCount == 1, "Multiple graph objects were not published to the display");
                Check(objectProjection.FramingPoints.Any(), "Inactive graph object has no framing points");
                var activeId = workspace.Document.ActiveObjectId;
                var activeButton = root.Q<Button>("object-select-" + activeId);
                Check(activeButton != null && activeButton.text.Length < activeId.Length && activeButton.tooltip.EndsWith(activeId, StringComparison.Ordinal), "Object selector text is not readable without losing the full identity");
                var directory = Path.Combine(output, "multi-object-display");
                projectPath.SetValueWithoutNotify(directory); SaveProject(); OpenProject();
                Check(workspace.Document.Objects.Count == 2 && objectProjection.EntryCount == 1, "Multiple graph objects were not restored on Open");
                showAllObjects.value = false;
                Check(objectProjection.EntryCount == 0 && !objectProjection.FramingPoints.Any(), "Inactive object visibility toggle did not hide the backdrop");
                showAllObjects.value = true;
                Check(objectProjection.EntryCount == 1 && objectProjection.FramingPoints.Any(), "Inactive object visibility toggle did not restore the backdrop");
                var objectIds = workspace.Document.Objects.Select(item => item.ObjectId).ToArray();
                Check(objectIds.Length == 2, "Multi-object verification lost object identities after reopen");
                var before = workspace.Document.Objects.ToDictionary(item => item.ObjectId,
                    item => GraphEvaluator.Evaluate(item.Graph).Output.Mesh.ContentHash, StringComparer.Ordinal);
                Execute(AuthoringOperation.SelectObject(objectIds[0]));
                SelectEditStage(1); Select(new[] { 0 });
                moveX.SetValueWithoutNotify(1); moveY.SetValueWithoutNotify(0); moveZ.SetValueWithoutNotify(0); MoveSelection();
                var afterFirst = workspace.Document.Objects.ToDictionary(item => item.ObjectId,
                    item => GraphEvaluator.Evaluate(item.Graph).Output.Mesh.ContentHash, StringComparer.Ordinal);
                Check(afterFirst[objectIds[0]] != before[objectIds[0]] && afterFirst[objectIds[1]] == before[objectIds[1]], "Active object edit changed the inactive graph");
                Execute(AuthoringOperation.SelectObject(objectIds[1]));
                SelectEditStage(1); Select(new[] { 0 }); MoveSelection();
                var afterSecond = workspace.Document.Objects.ToDictionary(item => item.ObjectId,
                    item => GraphEvaluator.Evaluate(item.Graph).Output.Mesh.ContentHash, StringComparer.Ordinal);
                Check(afterSecond[objectIds[1]] != before[objectIds[1]] && afterSecond[objectIds[0]] == afterFirst[objectIds[0]], "Second active object edit did not stay isolated");
                checks.Add("multi-object GUI backdrop: two graph targets, Save/Open, active editing projection, isolated edits, visibility toggle and framing");
            }
            finally
            {
                showAllObjects.SetValueWithoutNotify(previousVisibility);
                ReplaceWorkspace(previous, previousPath);
            }
        }
    }
}
