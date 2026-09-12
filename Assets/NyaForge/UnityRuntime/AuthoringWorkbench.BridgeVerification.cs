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
        IEnumerator VerifyBoundaryBridge(string outputDirectory,Action<string> completed)
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
                    boundaryPanel.value=true;bridgePanel.value=true;boundaryChoice.index=0;bridgeTarget.index=1;bridgeOffset.value=-1;
                }
                catch(Exception e) { failure=e.ToString(); }
                yield return null;yield return null;
                controls.ScrollTo(bridgeButton);yield return null;yield return null;
                if(failure==null) try
                {
                    string before=workspace.Document.StateHash;
                    Check(boundaryHighlight.VertexCount==4 && bridgeHighlight.VertexCount==4,"Two bridge boundaries not highlighted");
                    bridgeTarget.index=0;Check(!bridgeButton.enabledSelf && bridgeHighlight.VertexCount==0,"Same boundary accepted");
                    bridgeTarget.index=1;Check(workspace.Document.StateHash==before,"Boundary selection modified document");
                    PointerProbe.Click(bridgeButton);
                    var polygon=DisplayedGraphValue().Polygon;
                    Check(polygon.Faces.Count==6 && workspace.Evaluate().TriangleCount==12 && PolygonBoundaries.Find(polygon).Count==0,"Bridge did not close box");
                    Check(boundaryHighlight.VertexCount==0 && bridgeHighlight.VertexCount==0 && !bridgeButton.enabledSelf,"Closed boundaries still highlighted");
                    string after=workspace.Document.StateHash;
                    Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before,"GUI boundary bridge Undo differs");
                    Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==after,"GUI boundary bridge Redo differs");
                    string directory=Path.Combine(outputDirectory,"bridged-boundaries-project");projectPath.SetValueWithoutNotify(directory);SaveProject();OpenProject();
                    Check(workspace.Document.StateHash==after && workspace.Preview.Output.Polygon.Faces.Count==6,"Bridged boundary native reopen differs");
                    Check(BakeStore.Read(BakeStore.Export(Path.Combine(directory,"exports","bridged"),workspace)).Mesh.ContentHash==workspace.Evaluate().ContentHash,"Bridged boundary Bake differs");
                    SelectEditStage(1);Frame();controls.ScrollTo(bridgeButton);SetStatus("2つの境界を四角面で接続。Undo・保存・出力を確認。");
                }
                catch(Exception e) { failure=e.ToString(); }
                if(failure==null) yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(outputDirectory,"bridged-boundaries.png"),error=>failure=error);
                completed(failure);
            }
            finally { bridgePanel.value=false;boundaryPanel.value=false;ReplaceWorkspace(previous,previousPath); }
        }
    }
}


