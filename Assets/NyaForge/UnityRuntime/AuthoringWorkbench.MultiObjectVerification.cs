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

                // A reference avatar can remain selectable and visible while all
                // geometry/graph edits are rejected. Persist the protection marker
                // through native Save/Open, then unlock it for the next fixture.
                Execute(AuthoringOperation.SelectObject(objectIds[0]));
                SelectEditStage(1); Select(new[] { 0 });
                var protectedBefore = GraphEvaluator.Evaluate(workspace.Document.ActiveObject.Graph).Output.Mesh.ContentHash;
                referenceProtectionToggle.value = true;
                Check(referenceProtectionToggle.value && referenceProtectedObjectIds.Contains(objectIds[0]), "Reference protection toggle did not register the active object");
                Execute(AuthoringOperation.Undo());
                Check(!referenceProtectedObjectIds.Contains(objectIds[0]) && !referenceProtectionToggle.value, "Reference protection was not restored after Undo");
                Execute(AuthoringOperation.Redo());
                Check(referenceProtectedObjectIds.Contains(objectIds[0]) && referenceProtectionToggle.value, "Reference protection was not restored after Redo");
                // Generic GLB delivery enumerates every graph object. A
                // protected reference avatar must stop that path before a
                // destination folder is created; the clothing-only package is
                // the explicit delivery path instead.
                ExportGlbStatic();
                Check(status.text.Contains("参照object") && !Directory.Exists(Path.Combine(directory, "exports")),
                    "Generic GLB export included a protected reference object or created a destination");
                moveX.SetValueWithoutNotify(1); moveY.SetValueWithoutNotify(0); moveZ.SetValueWithoutNotify(0); MoveSelection();
                var protectedAfter = GraphEvaluator.Evaluate(workspace.Document.ActiveObject.Graph).Output.Mesh.ContentHash;
                Check(protectedAfter == protectedBefore && status.text.Contains("参照"), "Reference-protected object accepted a geometry edit (before=" + protectedBefore + ", after=" + protectedAfter + ", active=" + workspace.Document.ActiveObjectId + ", protected=" + referenceProtectedObjectIds.Contains(workspace.Document.ActiveObjectId) + ", status=" + status.text + ")");
                SaveProject(); OpenProject();
                Check(referenceProtectedObjectIds.Contains(objectIds[0]) && referenceProtectionToggle.value, "Reference protection was not restored by Save/Open");
                referenceProtectionToggle.value = false;
                Check(!referenceProtectedObjectIds.Contains(objectIds[0]), "Reference protection did not unlock the active object");
                checks.Add("multi-object GUI backdrop: two graph targets, Save/Open, isolated edits, visibility/framing and persisted reference protection");
            }
            finally
            {
                showAllObjects.SetValueWithoutNotify(previousVisibility);
                ReplaceWorkspace(previous, previousPath);
            }
        }
    }
}
