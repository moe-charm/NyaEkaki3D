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
        IEnumerator VerifyManyPoints(string outputDirectory,Action<string> completed)
        {
            var previous=workspace;string previousPath=savedDirectory,failure=null;
            try
            {
                try
                {
                    ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null);
                    var plane=PolygonPrimitives.Plane(Guid.NewGuid().ToString("D"));
                    var vertices=plane.Vertices.Values.Concat(Enumerable.Range(0,2049).Select(i=>new CageVertex((ulong)i+5,i==2048 ? new Vec3(.4f,.2f,0) : new Vec3((i%64)*.002f,(i/64)*.002f,0))));
                    var mesh=new PolygonMesh(plane.DomainId,vertices,plane.Faces);
                    string source=Guid.NewGuid().ToString("D"),edit=Guid.NewGuid().ToString("D"),output=Guid.NewGuid().ToString("D");
                    var graph=new AuthoringGraph(Guid.NewGuid().ToString("D"),new[]{GraphNode.Polygon(source,mesh,new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(output)},
                        new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",output,"mesh")},output);
                    Execute(AuthoringOperation.AddGraph(graph));SelectEditStage(editStageIds.IndexOf(edit));faceMode.value=false;Frame();
                }
                catch(Exception e) { failure=e.ToString(); }
                yield return null;yield return null;
                if(failure==null) try
                {
                    string before=workspace.Document.StateHash;
                    Check(projection.Points.Length==2053 && projection.PointBatchCount==2,"Point batch count or coverage differs");
                    PointerProbe.ClickAt(view,VertexPanelPoint(projection.Points[2052]));
                    Check(SelectedPolygonVertices().SequenceEqual(new ulong[]{2053}) && projection.SelectedPointCount==1,"Point beyond first batch not selected");
                    Select(Enumerable.Range(0,2053));Check(projection.SelectedPointCount==2053,"All-point highlight truncated");
                    Select(Array.Empty<int>());Check(projection.SelectedPointCount==0 && workspace.Document.StateHash==before,"Point highlight modified document");
                    SelectEditStage(0);Check(projection.Points.Length==4 && projection.PointBatchCount==1,"Final view retained loose points");
                    SelectEditStage(1);Check(projection.Points.Length==2053 && projection.PointBatchCount==2,"Editing points not restored");
                    Select(new[]{2052});Frame();SetStatus("2053点を2バッチで表示。末尾クリック・全選択・段切替を確認。");
                }
                catch(Exception e) { failure=e.ToString(); }
                if(failure==null) yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(outputDirectory,"many-points.png"),error=>failure=error);
                completed(failure);
            }
            finally { ReplaceWorkspace(previous,previousPath); }
        }
    }
}
