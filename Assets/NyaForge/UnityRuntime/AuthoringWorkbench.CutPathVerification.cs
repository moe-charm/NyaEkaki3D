using System;
using System.Collections;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;
using UnityEngine.UIElements;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyCutPath(string output,Action<string> completed)
        {
            var previous=workspace;string previousPath=savedDirectory,failure=null,before=null,after=null;
            try
            {
                try
                {
                    var positions=new[]{new Vec3(0,0,0),new Vec3(.1f,0,0),new Vec3(.2f,0,0),new Vec3(0,.1f,0),new Vec3(.1f,.1f,0),new Vec3(.2f,.1f,0)};
                    ulong corner=0;
                    CageFace Face(ulong id,ulong[] ids)=>new CageFace(id,0,ids.Select(v=>new CageCorner(++corner,v,new Vec2(positions[v-1].X,positions[v-1].Y))));
                    var mesh=new PolygonMesh(Guid.NewGuid().ToString("D"),positions.Select((p,i)=>new CageVertex((ulong)i+1,p)),new[]{Face(1,new ulong[]{1,2,5,4}),Face(2,new ulong[]{2,3,6,5})});
                    string source=Guid.NewGuid().ToString("D"),edit=Guid.NewGuid().ToString("D"),end=Guid.NewGuid().ToString("D");
                    var graph=new AuthoringGraph(Guid.NewGuid().ToString("D"),new[]{GraphNode.Polygon(source,mesh,new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(end)},new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",end,"mesh")},end);
                    ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null);Execute(AuthoringOperation.AddGraph(graph));SelectEditStage(1);faceMode.value=false;Frame();
                    cutPathPanel.value=true;cutPathText.value="1 4 50\n2 5 50\n3 6 50";before=workspace.Document.StateHash;
                    Check(cutPathCommit.enabledSelf && cutPathLine.VertexCount==3,"Path preview missing");
                    cutPathPanel.value=false;Check(cutPathLine.VertexCount==0,"Closed path retained line");cutPathPanel.value=true;
                    Check(cutPathLine.VertexCount==3 && workspace.Document.StateHash==before,"Draft mutated document");
                    cutPathText.value="1 4 50\n2 5 0\n3 6 0";
                    Check(!cutPathCommit.enabledSelf && cutPathLine.VertexCount==0 && workspace.Document.StateHash==before,"Invalid late path split retained preview or changed document");
                    cutPathText.value="1 4 50\n2 5 50\n3 6 50";
                }
                catch(Exception e) { failure=e.ToString(); }
                if(failure==null) yield return VerifyCutPathControls(error=>failure=error);
                if(failure==null) yield return VerifyCutPathPicking(error=>failure=error);
                yield return null;yield return null;controls.ScrollTo(cutPathCommit);yield return null;yield return null;
                if(failure==null) try
                {
                    PointerProbe.Click(cutPathCommit);after=workspace.Document.StateHash;
                    Check(DisplayedGraphValue().Polygon.Faces.Count==4 && DisplayedGraphValue().Polygon.Vertices.Count==9 && workspace.Evaluate().TriangleCount==8,"Path commit geometry differs");
                    Check(cutPathText.value=="" && cutPathLine.VertexCount==0 && selection.Count==0,"Path commit left draft/selection");
                    Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before,"Path Undo differs");
                    Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==after,"Path Redo differs");
                    string directory=Path.Combine(output,"cut-path-project");projectPath.SetValueWithoutNotify(directory);SaveProject();OpenProject();
                    Check(workspace.Document.StateHash==after,"Path native reopen differs");
                    Check(BakeStore.Read(BakeStore.Export(Path.Combine(directory,"exports","cut"),workspace)).Mesh.ContentHash==workspace.Evaluate().ContentHash,"Path Bake differs");
                    SelectEditStage(1);Frame();SetStatus("連続切断：2面→4面、追加3点。Undo・保存・出力を確認。");
                }
                catch(Exception e) { failure=e.ToString(); }
                if(failure==null) yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(output,"cut-path.png"),error=>failure=error);
                completed(failure);
            }
            finally { cutPathPanel.value=false;ReplaceWorkspace(previous,previousPath); }
        }
    }
}


