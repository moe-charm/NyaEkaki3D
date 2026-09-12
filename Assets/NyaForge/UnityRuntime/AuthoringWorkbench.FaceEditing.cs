using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Topology;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        readonly HashSet<ulong> selectedFaces = new HashSet<ulong>();
        Toggle faceMode;
        Label faceLabel;
        FloatField extrusionDepth;
        Button extrudeButton;
        Button deleteFacesButton;
        void SelectElements(bool all)
        {
            if (faceMode.value && faceMode.enabledSelf)
            {
                selectedFaces.Clear();
                if (all) foreach (var face in DisplayedGraphValue().Polygon.Faces) selectedFaces.Add(face.Id);
                Refresh();
            }
            else Select(all ? Enumerable.Range(0, projection.Points.Length) : Enumerable.Empty<int>());
        }
        void BuildFaceEditing(VisualElement parent)
        {
            faceMode = new Toggle("面をクリックして選択") { name = "face-selection-mode" }; parent.Add(faceMode);
            faceMode.RegisterValueChangedCallback(_ => { selection.Clear(); selectedFaces.Clear(); Refresh(); });
            faceLabel = new Label { name = "selected-faces" }; parent.Add(faceLabel);
            extrusionDepth = Number(parent, "面の押出し (mm)", 20, "extrusion-depth");
            extrusionDepth.tooltip = "最初に並ぶ選択面の法線方向。負数で逆方向へ押し出します。";
            extrudeButton = Button("選択面を押し出す", ExtrudeSelectedFaces, "extrude-faces"); parent.Add(extrudeButton);
            deleteFacesButton = Button("選択面を削除", DeleteSelectedFaces, "delete-faces"); parent.Add(deleteFacesButton);
            deleteFacesButton.tooltip = "選択した面と、その削除で不要になる頂点を取り除きます。Undoで戻せます。全ての面も削除できます。元から独立した点は残します。";
            BuildBoundaryEditing(parent);
            BuildFaceMerge(parent);
            BuildFaceSplit(parent);
            BuildEdgeInsertion(parent);BuildEdgeCut(parent);BuildCutPath(parent);
            BuildWeld(parent);
            BuildFaceCreation(parent);
            BuildVertexCreation(parent);
        }
        void RefreshFaceEditing()
        {
            var polygon = DisplayedGraphValue()?.Polygon;
            bool editable = activeEditContext != null && polygon != null;
            faceMode.SetEnabled(editable); extrusionDepth.SetEnabled(editable);
            if (polygon == null) selectedFaces.Clear();
            else { var ids = new HashSet<ulong>(polygon.Faces.Select(f => f.Id)); selectedFaces.RemoveWhere(id => !ids.Contains(id)); }
            faceLabel.text = selectedFaces.Count == 0 ? "面の選択なし" : "選択面: " + string.Join(", ", selectedFaces.OrderBy(id => id));
            extrudeButton.SetEnabled(editable && selectedFaces.Count > 0);
            deleteFacesButton.SetEnabled(editable && selectedFaces.Count>0);
            RefreshBoundaryEditing(polygon,editable);
            mergeFacesButton.SetEnabled(editable && selectedFaces.Count>=2);
            bool selectingFaces = faceMode.value && editable;
            splitFaceButton.SetEnabled(editable && !selectingFaces && SelectedPolygonVertices().Length==2);
            RefreshEdgeInsertion(editable,selectingFaces);
            weldButton.SetEnabled(editable && !selectingFaces && SelectedPolygonVertices().Length>=2);
            RefreshFaceCreation();RefreshEdgeCut();RefreshCutPath();
            vertexCreateButton.SetEnabled(editable);
            RefreshViewportHint();
            vertexId.SetEnabled(!selectingFaces); root.Q<Button>("authoring-select-id").SetEnabled(!selectingFaces);
            moveX.SetEnabled(!selectingFaces); moveY.SetEnabled(!selectingFaces); moveZ.SetEnabled(!selectingFaces);
            projection.SelectFaces(selectingFaces ? DisplayedGraphValue().PolygonRendering?.RenderTriangleMap : null, selectedFaces, selectingFaces);
            selectionLabel.text = selectingFaces ? "面モード · Shiftで追加／解除" : selectionLabel.text;
            if (selectingFaces) moveButton.SetEnabled(false);
        }
        void ExtrudeSelectedFaces() => Try(() =>
        {
            if (activeEditContext == null || selectedFaces.Count == 0) throw new InvalidOperationException("PolygonEditの面を選択してください。");
            var polygon = DisplayedGraphValue().Polygon;
            var reference = polygon.Faces.First(f => f.Id == selectedFaces.Min());
            var delta = PolygonExtrusion.FaceNormal(polygon, reference) * (extrusionDepth.value / 1000);
            Execute(AuthoringOperation.ExtrudePolygonFaces(activeEditContext, selectedFaces.OrderBy(id => id).ToArray(), delta));
            selection.Clear(); Refresh();
        });
        void DeleteSelectedFaces() => Try(() =>
        {
            if(activeEditContext==null || selectedFaces.Count==0) throw new InvalidOperationException("PolygonEditの面を選択してください。");
            Execute(AuthoringOperation.DeletePolygonFaces(activeEditContext,selectedFaces.OrderBy(id=>id).ToArray()));
            selection.Clear();selectedFaces.Clear();Refresh();
        });
        void PickFace(Vector2 panelPosition, bool add)
        {
            var value = DisplayedGraphValue(); if (activeEditContext == null || value?.PolygonRendering == null) return;
            var rect = view.worldBound;
            var ray = camera.ViewportPointToRay(new Vector3((panelPosition.x - rect.x) / rect.width, 1 - (panelPosition.y - rect.y) / rect.height, 0));
            float nearest = float.PositiveInfinity; ulong? hit = null; int triangle = 0; var points = projection.Points;
            foreach (var submesh in value.Mesh.Submeshes)
                for (int i = 0; i < submesh.Length; i += 3, triangle++)
                {
                    var a = points[submesh[i]]; var b = points[submesh[i + 1]]; var c = points[submesh[i + 2]];
                    var e1 = b - a; var e2 = c - a; var p = Vector3.Cross(ray.direction, e2); float determinant = Vector3.Dot(e1, p);
                    if (Mathf.Abs(determinant) < 1e-10f) continue;
                    float inverse = 1 / determinant; var t = ray.origin - a; float u = Vector3.Dot(t, p) * inverse;
                    if (u < 0 || u > 1) continue; var q = Vector3.Cross(t, e1); float v = Vector3.Dot(ray.direction, q) * inverse;
                    if (v < 0 || u + v > 1) continue; float distance = Vector3.Dot(e2, q) * inverse;
                    if (distance >= 0 && distance < nearest) { nearest = distance; hit = value.PolygonRendering.RenderTriangleMap[triangle]; }
                }
            if (!add) selectedFaces.Clear();
            if (hit.HasValue && (!add || !selectedFaces.Remove(hit.Value))) selectedFaces.Add(hit.Value);
            Refresh();
        }
    }
}






