using System.Collections.Generic;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void VerifyStateOwnership(List<string> checks)
        {
            Check(session != null && ReferenceEquals(session.Workspace, workspace),
                "Workbench session does not own the live workspace");
            Check(ReferenceEquals(session.Commands, commands),
                "Workbench panels are not using the session command service");
            Check(ReferenceEquals(selection, selectionContext.VertexIndices) &&
                  ReferenceEquals(selectedFaces, selectionContext.FaceIds),
                "Vertex/face selection has more than one owner");
            Check(selectionContext.ObjectId == (workspace.Document.ActiveObjectId ?? ""),
                "Selection context object identity is stale");
            Check(selectionContext.EditNodeId == (activeEditContext?.NodeId ?? ""),
                "Selection context edit-stage identity is stale");
            int sessionEvents = 0;
            System.Action onSessionChanged = () => sessionEvents++;
            session.StateChanged += onSessionChanged;
            session.NotifyChanged();
            session.StateChanged -= onSessionChanged;
            Check(sessionEvents == 1, "Session state notification did not fire exactly once");
            long selectionRevision = selectionContext.Revision;
            int selectionEvents = 0;
            System.Action onSelectionChanged = () => selectionEvents++;
            selectionContext.Changed += onSelectionChanged;
            selectionContext.NotifyChanged();
            selectionContext.Changed -= onSelectionChanged;
            Check(selectionEvents == 1 && selectionContext.Revision == selectionRevision + 1,
                "SelectionContext change notification did not fire exactly once");
            checks.Add("session owns workspace/commands and SelectionContext owns object/edit-stage/vertex/face selection with change notifications");
        }
    }
}
