using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;

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
                var directory = Path.Combine(output, "multi-object-display");
                projectPath.SetValueWithoutNotify(directory); SaveProject(); OpenProject();
                Check(workspace.Document.Objects.Count == 2 && objectProjection.EntryCount == 1, "Multiple graph objects were not restored on Open");
                showAllObjects.value = false;
                Check(objectProjection.EntryCount == 0 && !objectProjection.FramingPoints.Any(), "Inactive object visibility toggle did not hide the backdrop");
                showAllObjects.value = true;
                Check(objectProjection.EntryCount == 1 && objectProjection.FramingPoints.Any(), "Inactive object visibility toggle did not restore the backdrop");
                checks.Add("multi-object GUI backdrop: two graph targets, Save/Open, active editing projection, visibility toggle and framing");
            }
            finally
            {
                showAllObjects.SetValueWithoutNotify(previousVisibility);
                ReplaceWorkspace(previous, previousPath);
            }
        }
    }
}
