using System;
using System.Collections;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Topology;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyLooseVertexWorkflow(string output,Action<string> completed)
        {
            string failure=null,before=workspace.Document.StateHash,render=workspace.Evaluate().ContentHash;
            var old=DisplayedGraphValue().Polygon.Vertices[5].Position;
            Select(PolygonEditPoints.VertexIds(DisplayedGraphValue().Polygon).Select((id,index)=>(id,index)).Where(p=>p.id==5).Select(p=>p.index));
            moveX.value=10;moveY.value=-20;moveZ.value=0;
            controls.ScrollTo(moveButton);yield return null;yield return null;
            if(failure==null) try
            {
                PointerProbe.Click(moveButton);
                var value=DisplayedGraphValue();var position=value.Polygon.Vertices[5].Position;
                Check(Math.Abs(position.X-old.X-.01f)<1e-6 && Math.Abs(position.Y-old.Y+.02f)<1e-6,"Loose vertex GUI move position differs");
                Check(workspace.Evaluate().ContentHash==render,"Moving loose vertex changed triangle mesh");
                var p=value.Transform.ToAvatarPoint(position);
                Check(Vector3.Distance(projection.Points[4],new Vector3(p.X,p.Y,p.Z))<1e-6,"Moved loose marker remained cached");
                string moved=workspace.Document.StateHash;
                Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before,"Loose move Undo differs");
                Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==moved,"Loose move Redo differs");
                faceCreatePanel.value=true;facePerimeter.value="4, 1, 5";
            }
            catch(Exception e) { failure=e.ToString(); }
            yield return null;yield return null;controls.ScrollTo(faceCreateButton);yield return null;yield return null;
            if(failure==null) try
            {
                Check(faceCreateButton.enabledSelf,"Loose vertex face draft invalid");
                string moved=workspace.Document.StateHash;PointerProbe.Click(faceCreateButton);
                Check(DisplayedGraphValue().Polygon.Faces.Count==2 && workspace.Evaluate().TriangleCount==3,"Loose vertex not incorporated into face");
                Check(PolygonEditPoints.VertexIds(DisplayedGraphValue().Polygon).Length==DisplayedGraphValue().PolygonRendering.RenderVertexMap.Count,"Connected vertex still appended as loose");
                string joined=workspace.Document.StateHash;
                Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==moved && projection.Points.Length==5,"Face Undo did not restore loose point");
                Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==joined,"Face Redo differs");
                string directory=Path.Combine(output,"loose-to-face-project");projectPath.SetValueWithoutNotify(directory);SaveProject();OpenProject();
                Check(workspace.Document.StateHash==joined,"Joined face native reopen differs");
                Check(BakeStore.Read(BakeStore.Export(Path.Combine(directory,"exports","joined"),workspace)).Mesh.ContentHash==workspace.Evaluate().ContentHash,"Joined face Bake differs");
                SelectEditStage(1);Frame();SetStatus("追加頂点を移動し、面に接続。Undo・保存・出力を確認。");
            }
            catch(Exception e) { failure=e.ToString(); }
            if(failure==null) yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(output,"loose-to-face.png"),error=>failure=error);
            faceCreatePanel.value=false;completed(failure);
        }
    }
}
