using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        System.Collections.IEnumerator CaptureGraphEditing(string output, Action<string> error)
        {
            var previous = workspace; string previousPath = savedDirectory;
            try
            {
                ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null); CreatePlaneGraph();
                graphCanvas.style.display = UnityEngine.UIElements.DisplayStyle.Flex;
                Select(new[] { 0 }); MoveSelection();
                controls.ScrollTo(editStage);
            }
            catch (Exception e) { error(e.ToString()); ReplaceWorkspace(previous, previousPath); yield break; }
            try
            {
                yield return null;
                yield return WorkbenchCapture.Write(GetComponent<UnityEngine.UIElements.UIDocument>(), camera,
                    Path.Combine(output, "graph-editing.png"), error);
            }
            finally { graphCanvas.style.display = UnityEngine.UIElements.DisplayStyle.None; ReplaceWorkspace(previous, previousPath); }
        }

        void VerifyGraphProjection(string output, List<string> checks)
        {
            var original = workspace; string originalPath = savedDirectory;
            try
            {
                ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
                string plane = Guid.NewGuid().ToString("D"), sink = Guid.NewGuid().ToString("D");
                var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] { GraphNode.Plane(plane), GraphNode.Output(sink) },
                    new[] { new GraphEdge(plane, "mesh", sink, "mesh") }, sink);
                Execute(AuthoringOperation.AddGraph(graph));
                Check(workspace.Preview.IsComplete && projection.DisplayMesh != null && projection.Points.Length == 4, "Graph projection missing");
                Execute(AuthoringOperation.Disconnect(sink, "mesh"));
                Check(workspace.Preview.IsStale && projection.Points.Length == 4 && metrics.text.Contains("表示 rev"), "Stale graph preview not identified");
                string directory = Path.Combine(output, "graph-project");
                ProjectStore.Save(directory, workspace, 0);
                ReplaceWorkspace(ProjectStore.Open(directory), directory);
                Check(!workspace.Preview.IsComplete && projection.DisplayMesh == null && projection.Points.Length == 0, "Incomplete reopen fabricated preview");
                Execute(AuthoringOperation.Connect(new GraphEdge(plane, "mesh", sink, "mesh")));
                Check(workspace.Preview.IsComplete && projection.Points.Length == 4, "Graph reconnect projection missing");
                Execute(AuthoringOperation.Undo());
                Check(workspace.Preview.IsStale && projection.Points.Length == 4, "Graph Undo lost last good projection");
                checks.Add("graph Player projection: create, disconnect stale revision, schema3 reopen without fabricated output, reconnect and Undo");
                ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
                CreatePlaneGraph();
                Check(activeEditContext != null && projection.Points.Length == 4, "GUI Plane did not select editable stage");
                Select(new[] { 0 }); moveX.SetValueWithoutNotify(10); moveY.SetValueWithoutNotify(0); moveZ.SetValueWithoutNotify(0);
                MoveSelection();
                Check(Math.Abs(projection.Points[0].x + .09f) < .00001f, "GUI graph move differs from 10mm");
                Check(selection.Contains(0), "Same input edit unexpectedly lost selection");
                var editGraph = workspace.Document.Objects[0].Graph;
                string editNode = activeEditContext.NodeId;
                string sourceNode = editGraph.Edges.First(e => e.ToNode == editNode).FromNode;
                Execute(AuthoringOperation.UpdateNode(GraphNode.Plane(sourceNode, .4f, .2f)));
                Check(activeEditContext == null && selection.Count == 0 && projection.Points.Length == 0, "Stale edit input remained editable");
                Execute(AuthoringOperation.Undo());
                Check(activeEditContext != null && projection.Points.Length == 4, "Undo did not restore editable stage");
                SelectEditStage(0);
                Check(activeEditContext == null && !moveButton.enabledSelf, "Final output incorrectly editable without a stage");
                checks.Add("GUI graph editing: Plane starter selects EditMesh, 10mm move, upstream invalidation clears selection, Undo restores stage, final output read-only");
                ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
                graphCanvas.VerifyEmptyGraphConstruction();
                graphCanvas.VerifyLayoutRoundtrip();
                checks.Add("canvas layout: fresh view restores positions, authoring state unchanged, corrupt layout falls back without losing graph");
                Check(activeEditContext != null, "Canvas edit action did not select stage");
                checks.Add("canvas button/port handlers: empty node creation, connect Plane/EditMesh/Output, invalid connection preserves document, select edit stage");
                ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null); CreatePolygonGraph();
                Check(activeEditContext != null && workspace.Preview.Output.Polygon.Faces[0].Corners.Count == 4, "Polygon starter lost quad or edit context");
                ulong stableVertex = DisplayedGraphValue().PolygonRendering.RenderVertexMap[0].VertexId;
                float initialX = workspace.Preview.Output.Polygon.Vertices[stableVertex].Position.X;
                Select(new[] { 0 }); moveX.SetValueWithoutNotify(10); MoveSelection();
                Check(Math.Abs(workspace.Preview.Output.Polygon.Vertices[stableVertex].Position.X - initialX - .01f) < .00001f, "GUI stable-ID move differs");
                string polygonDirectory = Path.Combine(output, "polygon-edit-project"); ProjectStore.Save(polygonDirectory, workspace, 0);
                ReplaceWorkspace(ProjectStore.Open(polygonDirectory), polygonDirectory);
                Check(workspace.Preview.Output.Polygon.Faces[0].Corners.Count == 4, "Polygon edit reopen lost quad");
                BakeStore.Export(Path.Combine(polygonDirectory, "export"), workspace);
                checks.Add("GUI polygon starter: quad retained, render pick mapped to stable vertex, 10mm move, native reopen and Bake");
            }
            finally { ReplaceWorkspace(original, originalPath); }
        }
    }
}
