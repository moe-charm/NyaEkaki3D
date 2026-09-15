using System.Collections.Generic;
using NyaForge.Authoring.Graph;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        // Runtime handles and authored choices for the attachment panel live
        // here so the UI builder and attachment operations share one explicit
        // state boundary. These values are transient; the graph remains the
        // source of truth for saved attachment data.
        Foldout attachmentPanel;
        Label attachmentStatus;
        Label accessoryFitSummary;
        DropdownField attachmentTarget;
        DropdownField attachmentBone;
        FloatField attachmentOffsetX, attachmentOffsetY, attachmentOffsetZ;
        FloatField accessoryFitOffsetMm, accessoryFitMaxDistanceMm, accessorySurfaceRegionRadiusMm;
        TextField accessorySurfaceTriangleIds;
        TextField accessoryClothingVertexIds;
        Toggle accessorySurfacePickMode;
        readonly HashSet<int> selectedAvatarSurfaceTriangles = new HashSet<int>();
        Button attachmentApply, attachmentRemove, accessorySkinBind, accessoryPolygonMaterialize, accessoryAutoWeight, accessorySurfaceWeight, accessorySurfaceFit, accessorySurfaceInspect, accessoryPoseCopy, accessoryUseSelectedVertices, accessoryClearSurfaceSelection, accessorySelectBoneRegion;
        readonly List<string> attachmentTargetIds = new List<string>();
        readonly List<string> attachmentBoneIds = new List<string>();
        string attachmentTargetChoice;
        string attachmentChoiceOwner;
        // Fit summary is refreshed for every selection change. Keep the last
        // immutable target evaluation so hovering/selecting vertices does not
        // re-run the avatar graph until its graph instance changes.
        AuthoringGraph accessoryFitSummaryTargetGraph;
        GraphEvaluation accessoryFitSummaryTargetEvaluation;
        string accessoryFitSummaryTargetObjectId = "";
        string surfaceFitInspectionObjectId = "";
        string surfaceFitInspectionTargetObjectId = "";
        string surfaceFitInspectionStateHash = "";
        long surfaceFitInspectionRevision = -1;
        int surfaceFitInspectionEvaluatedVertexCount;
        int surfaceFitInspectionMovedVertexCount;
        float surfaceFitInspectionMaxProjectionDistance;
        float surfaceFitInspectionAverageProjectionDistance;
        float surfaceFitInspectionMaxDisplacement;
        float surfaceFitInspectionAverageDisplacement;
        float surfaceFitInspectionOffset;
        float surfaceFitInspectionMaxDistance;
        int surfaceFitInspectionBehindSurfaceVertexCount;
        float surfaceFitInspectionMinimumSignedDistance;
        float surfaceFitInspectionMaximumSignedDistance;
        int surfaceFitInspectionProjectedBehindSurfaceVertexCount;
        float surfaceFitInspectionProjectedMinimumSignedDistance;
        float surfaceFitInspectionProjectedMaximumSignedDistance;
        int[] surfaceFitInspectionTriangleIds;
        int[] surfaceFitInspectionVertexIds;
        int[] surfaceFitInspectionBehindSurfaceVertexIds;
    }
}
