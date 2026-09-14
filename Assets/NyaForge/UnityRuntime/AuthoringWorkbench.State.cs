using System;
using System.Collections.Generic;
using NyaForge.Authoring;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    /// <summary>
    /// Cross-panel state shared by the authoring partials.
    /// Feature files own their controls and operations; this file owns the
    /// stable UI handles, live session, selection context, and viewport state.
    /// </summary>
    public sealed partial class AuthoringWorkbench : MonoBehaviour
    {
        // Shared UI handles. Feature-specific controls live beside their
        // partial implementation; this file keeps only cross-panel handles.
        VisualElement root, view, confirmRow;
        Label status, metrics, selectionLabel, projectLabel, projectManifestLabel, projectOutputScopeLabel, emptyHint;
        VisualElement emptyProjectEntryPanel;
        TextField projectPath;
        TextField vrmName, vrmAuthors, vrmLicenseUrl;
        Toggle vrmRequireCompleteSemantics;
        IntegerField vertexId;
        ScrollView controls;
        GraphCanvas graphCanvas;
        Image previewImage;
        RenderTexture previewTexture;
        FloatField moveX, moveY, moveZ;
        Toggle layer;
        Button undoButton, redoButton, moveButton;
        Action close, reopen;
        Camera camera;
        GameObject stage;
        OwnedMeshProjection projection;
        MultiObjectProjection objectProjection;
        AvatarSurfaceSelectionProjection avatarSurfaceSelection;

        // Session is the single owner of the live document and command history.
        AuthoringWorkbenchSession session;
        AuthoringWorkspace workspace => session?.Workspace;
        AuthoringCommandService commands => session?.Commands;
        string savedDirectory
        {
            get => session?.LoadedDirectory;
            set { if (session != null) session.SetLoadedDirectory(value); }
        }
        bool saveIncomplete
        {
            get => session != null && session.SaveIncomplete;
            set { if (session != null) session.SaveIncomplete = value; }
        }

        readonly SelectionContext selectionContext = new SelectionContext();
        HashSet<int> selection => selectionContext.VertexIndices;
        HashSet<ulong> selectedFaces => selectionContext.FaceIds;

        bool active, orbiting, allowQuit;
        int orbitButton;
        Vector2 pointerStart, pointerLast;
        Vector3 target;
        Quaternion orbit = Quaternion.Euler(0, 180, 0);
        float distance = .24f;

        bool HasUnsaved => session != null && (session.IsDirty || saveIncomplete);
        public bool IsOpen => active;
    }
}
