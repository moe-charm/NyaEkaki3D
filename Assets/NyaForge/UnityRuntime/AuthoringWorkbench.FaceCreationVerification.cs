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
        IEnumerator VerifyFaceCreation(string outputDirectory,Action<string> completed)
        {
            var previous=workspace;string previousPath=savedDirectory,failure=null;
            try
            {
                try
                {
                    ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null);
                    var plane=PolygonPrimitives.Plane(Guid.NewGuid().ToString("D"));
                    var vertices=plane.Vertices.Values.Concat(plane.Vertices.Values.Select(v=>new CageVertex(v.Id+4,v.Position+new Vec3(0,0,.2f))));
                    var back=new CageFace(2,0,plane.Faces[0].Corners.Reverse().Select(c=>new CageCorner(c.Id+4,c.VertexId+4,c.Uv0,c.Normal.Value*-1,new Vec4(c.Tangent.Value.X,c.Tangent.Value.Y,c.Tangent.Value.Z,-c.Tangent.Value.W))));
                    var mesh=new PolygonMesh(plane.DomainId,vertices,new[]{plane.Faces[0],back});
                    string source=Guid.NewGuid().ToString("D"),edit=Guid.NewGuid().ToString("D"),output=Guid.NewGuid().ToString("D");
                    var graph=new AuthoringGraph(Guid.NewGuid().ToString("D"),new[]{GraphNode.Polygon(source,mesh,new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(output)},
                        new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",output,"mesh")},output);
                    Execute(AuthoringOperation.AddGraph(graph));SelectEditStage(editStageIds.IndexOf(edit));Frame();
                    faceMode.value=false;faceCreatePanel.value=true;facePerimeter.value="4, 1, 5";
                }
                catch(Exception e) { failure=e.ToString(); }
                if(failure==null) yield return VerifyFaceDraftControls(error=>failure=error);
                yield return null;yield return null;
                controls.ScrollTo(faceCreateButton);yield return null;yield return null;
                if(failure==null) try
                {
                    string before=workspace.Document.StateHash;
                    Check(faceCreateButton.enabledSelf && faceCreateOutline.VertexCount==3,"Face draft preview missing");
                    facePerimeter.value="1, 4, 5";Check(!faceCreateButton.enabledSelf && faceCreateOutline.VertexCount==0,"Wrong winding accepted");
                    facePerimeter.value="4, 1, 5";Check(workspace.Document.StateHash==before,"Face preview changed document");
                    PointerProbe.Click(faceCreateButton);
                    Check(DisplayedGraphValue().Polygon.Faces.Count==3 && workspace.Evaluate().TriangleCount==5,"Face creation GUI geometry differs");
                    Check(facePerimeter.value=="" && faceCreateOutline.VertexCount==0,"Committed face draft not cleared");
                    string after=workspace.Document.StateHash;
                    Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before,"GUI face creation Undo differs");
                    Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==after,"GUI face creation Redo differs");
                    string directory=Path.Combine(outputDirectory,"created-face-project");projectPath.SetValueWithoutNotify(directory);SaveProject();OpenProject();
                    Check(workspace.Document.StateHash==after && workspace.Preview.Output.Polygon.Faces.Count==3,"Created face native reopen differs");
                    Check(BakeStore.Read(BakeStore.Export(Path.Combine(directory,"exports","created"),workspace)).Mesh.ContentHash==workspace.Evaluate().ContentHash,"Created face Bake differs");
                    SelectEditStage(1);Frame();controls.ScrollTo(faceCreateButton);SetStatus("頂点の指定順で面を作成。Undo・保存・出力を確認。");
                }
                catch(Exception e) { failure=e.ToString(); }
                if(failure==null) yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(outputDirectory,"created-face.png"),error=>failure=error);
                completed(failure);
            }
            finally { faceCreatePanel.value=false;ReplaceWorkspace(previous,previousPath); }
        }
    }
}



