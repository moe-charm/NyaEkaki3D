using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void CreateMirrorGraph()
        {
            string source=Guid.NewGuid().ToString("D"), edit=Guid.NewGuid().ToString("D"),mirror=Guid.NewGuid().ToString("D"),output=Guid.NewGuid().ToString("D");
            var polygon=PolygonPrimitives.Plane(Guid.NewGuid().ToString("D"),.1f,.1f);
            polygon=polygon.MoveVertices(polygon.Vertices.Keys,new Vec3(.08f,0,0));
            var graph=new AuthoringGraph(Guid.NewGuid().ToString("D"),new[]{GraphNode.Polygon(source,polygon,new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Mirror(mirror),GraphNode.Output(output)},
                new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",mirror,"mesh"),new GraphEdge(mirror,"mesh",output,"mesh")},output);
            Execute(AuthoringOperation.AddGraph(graph));
            if(IsGraph && workspace.Document.Objects[0].Graph.GraphId==graph.GraphId) { SelectEditStage(0);Frame(); }
        }
    }
}
