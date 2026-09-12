using System;
using System.Collections;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using UnityEngine.UIElements;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyFacelessRecovery(string output,Action<string> completed)
        {
            var previous=workspace;string path=savedDirectory,failure=null,before=null,deleted=null;
            try
            {
                try
                {
                    ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null);CreateMirrorGraph();SelectEditStage(1);before=workspace.Document.StateHash;
                    Execute(AuthoringOperation.DeletePolygonFaces(activeEditContext,DisplayedGraphValue().Polygon.Faces.Select(f=>f.Id).ToArray()));
                    deleted=workspace.Document.StateHash;SelectEditStage(0);
                    Check(facelessRecovery.style.display==DisplayStyle.Flex && facelessRecoveryButton.enabledSelf,"Recovery control missing");
                }
                catch(Exception e) { failure=e.ToString(); }
                yield return null;yield return null;controls.ScrollTo(facelessRecoveryButton);yield return null;yield return null;
                if(failure==null) try
                {
                    PointerProbe.Click(facelessRecoveryButton);
                    Check(activeEditContext!=null && DisplayedGraphValue().Polygon.Faces.Count==0 && projection.Points.Length==0,"Recovery did not select faceless edit stage");
                    Check(workspace.Document.StateHash==deleted && vertexCreateButton.enabledSelf,"Recovery changed document or disabled editing");
                    Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before && workspace.Preview.IsComplete && facelessRecovery.style.display==DisplayStyle.None,"Undo did not recover final output");
                    Execute(AuthoringOperation.Redo());Check(!workspace.Preview.IsComplete && workspace.Document.StateHash==deleted,"Redo did not restore diagnostic");
                    projectPath.SetValueWithoutNotify(Path.Combine(output,"faceless-recovery-project"));SaveProject();OpenProject();
                    Check(workspace.Document.StateHash==deleted && facelessRecovery.style.display==DisplayStyle.Flex,"Reopened diagnostic missing");
                }
                catch(Exception e) { failure=e.ToString(); }
                completed(failure);
            }
            finally { ReplaceWorkspace(previous,path); }
        }
    }
}

