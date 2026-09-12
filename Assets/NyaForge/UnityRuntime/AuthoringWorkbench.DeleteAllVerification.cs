using System;
using System.Collections;
using System.IO;
using System.Linq;
using NyaForge.Authoring;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyDeleteAllFaces(string output,Action<string> completed)
        {
            string failure=null,before=workspace.Document.StateHash;
            faceMode.value=true;selectedFaces.UnionWith(DisplayedGraphValue().Polygon.Faces.Select(f=>f.Id));Refresh();
            yield return null;yield return null;controls.ScrollTo(deleteFacesButton);yield return null;yield return null;
            try
            {
                Check(deleteFacesButton.enabledSelf,"Last face deletion disabled");PointerProbe.Click(deleteFacesButton);
                string after=workspace.Document.StateHash;
                Check(workspace.Preview.IsComplete && projection.DisplayMesh==null && projection.Points.Length==0,"Deleting all faces retained preview geometry");
                Check(selectedFaces.Count==0 && !deleteFacesButton.enabledSelf && vertexCreateButton.enabledSelf,"Empty deletion controls incorrect");
                projectPath.SetValueWithoutNotify(Path.Combine(output,"deleted-all-project"));SaveProject();OpenProject();
                Check(workspace.Document.StateHash==after && workspace.Preview.Output.Polygon.Faces.Count==0,"Empty deletion native reopen differs");
            }
            catch(Exception e) { failure=e.ToString(); }
            // Reopening resets history: verify the deletion Undo/Redo on a fresh saved first-face document.
            if(failure==null) try
            {
                projectPath.SetValueWithoutNotify(Path.Combine(output,"first-face-project"));OpenProject();SelectEditStage(1);
                before=workspace.Document.StateHash;
                Execute(AuthoringOperation.DeletePolygonFaces(activeEditContext,DisplayedGraphValue().Polygon.Faces.Select(f=>f.Id).ToArray()));
                string after=workspace.Document.StateHash;
                Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before && projection.DisplayMesh!=null,"Last face Undo failed");
                Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==after && projection.Points.Length==0,"Last face Redo failed");
            }
            catch(Exception e) { failure=e.ToString(); }
            completed(failure);
        }
    }
}
