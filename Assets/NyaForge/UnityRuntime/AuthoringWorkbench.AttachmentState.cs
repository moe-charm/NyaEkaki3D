using System.Collections.Generic;
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
        FloatField accessoryFitOffsetMm, accessoryFitMaxDistanceMm;
        TextField accessorySurfaceTriangleIds;
        TextField accessoryClothingVertexIds;
        Toggle accessorySurfacePickMode;
        readonly HashSet<int> selectedAvatarSurfaceTriangles = new HashSet<int>();
        Button attachmentApply, attachmentRemove, accessorySkinBind, accessoryPolygonMaterialize, accessoryAutoWeight, accessorySurfaceWeight, accessorySurfaceFit, accessorySurfaceInspect, accessoryPoseCopy, accessoryUseSelectedVertices, accessoryClearSurfaceSelection;
        readonly List<string> attachmentTargetIds = new List<string>();
        readonly List<string> attachmentBoneIds = new List<string>();
        string attachmentTargetChoice;
        string attachmentChoiceOwner;
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
        int[] surfaceFitInspectionTriangleIds;
        int[] surfaceFitInspectionVertexIds;
        int[] surfaceFitInspectionBehindSurfaceVertexIds;
    }
}
