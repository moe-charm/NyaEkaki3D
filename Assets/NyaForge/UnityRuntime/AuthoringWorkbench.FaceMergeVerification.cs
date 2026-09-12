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
        IEnumerator VerifyFaceMerge(string outputDirectory,Action<string> completed)
        {
            var previous=workspace;string previousPath=savedDirectory,failure=null;
            try
            {
                try
                {
                    ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null);
                    var mesh=TriangleMeshAdapter.Import(Guid.NewGuid().ToString("D"),PolygonRenderAdapter.Build(PolygonPrimitives.Plane(Guid.NewGuid().ToString("D"))).Mesh);
                    string source=Guid.NewGuid().ToString("D"),edit=Guid.NewGuid().ToString("D"),output=Guid.NewGuid().ToString("D");
                    var graph=new AuthoringGraph(Guid.NewGuid().ToString("D"),new[]{GraphNode.Polygon(source,mesh,new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(output)},
                        new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",output,"mesh")},output);
                    Execute(AuthoringOperation.AddGraph(graph));SelectEditStage(editStageIds.IndexOf(edit));Frame();
                    faceMode.value=true;selectedFaces.UnionWith(mesh.Faces.Select(f=>f.Id));Refresh();
                }
                catch(Exception e) { failure=e.ToString(); }
                yield return null;yield return null;
                controls.ScrollTo(mergeFacesButton);yield return null;yield return null;
                if(failure==null) try
                {
                    string before=workspace.Document.StateHash;
                    PointerProbe.Click(mergeFacesButton);
                    var polygon=DisplayedGraphValue().Polygon;
                    Check(polygon.Faces.Count==1 && polygon.Faces[0].Corners.Count==4 && workspace.Evaluate().TriangleCount==2,"GUI face merge did not create a quad");
                    Check(selectedFaces.SetEquals(new[]{polygon.Faces[0].Id}) && !mergeFacesButton.enabledSelf,"Merged selection contains a removed face");
                    string after=workspace.Document.StateHash;
                    Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before,"GUI face merge Undo differs");
                    Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==after,"GUI face merge Redo differs");
                    string directory=Path.Combine(outputDirectory,"merged-faces-project");projectPath.SetValueWithoutNotify(directory);SaveProject();OpenProject();
                    Check(workspace.Document.StateHash==after && workspace.Preview.Output.Polygon.Faces.Count==1,"Merged face native reopen differs");
                    Check(BakeStore.Read(BakeStore.Export(Path.Combine(directory,"exports","merged"),workspace)).Mesh.ContentHash==workspace.Evaluate().ContentHash,"Merged face Bake differs");
                    SelectEditStage(1);Frame();controls.ScrollTo(mergeFacesButton);SetStatus("面結合: 2つの三角面→1つの四角面。Undo・保存・出力を確認。");
                }
                catch(Exception e) { failure=e.ToString(); }
                if(failure==null) yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(outputDirectory,"merged-faces.png"),error=>failure=error);
                completed(failure);
            }
            finally { ReplaceWorkspace(previous,previousPath); }
        }
    }
}
