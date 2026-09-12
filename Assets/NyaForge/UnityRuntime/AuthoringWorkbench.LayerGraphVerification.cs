using System;
using System.Collections;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;
using NyaForge.Authoring.Topology;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyLayerGraph(string directory,Action<string> completed)
        {
            var previous=workspace;string previousPath=savedDirectory,failure=null;
            try
            {
                try
                {
                    string source=Guid.NewGuid().ToString("D"),paint=Guid.NewGuid().ToString("D"),output=Guid.NewGuid().ToString("D");
                    var graph=new AuthoringGraph(Guid.NewGuid().ToString("D"),new[]{
                        GraphNode.Polygon(source,PolygonPrimitives.Plane(Guid.NewGuid().ToString("D")),new RestTransform(1,new Vec3())),
                        GraphNode.Paint(paint),GraphNode.Output(output)},new[]{new GraphEdge(source,"mesh",paint,"mesh"),new GraphEdge(source,"mesh",output,"mesh"),new GraphEdge(paint,"image",output,"baseColor")},output);
                    var context=PaintEditing.Context(graph,paint);
                    var bottom=new PaintLayer(Guid.NewGuid().ToString("D"),"背景",new PaintImage(64,64,new Rgba32(255,210,50)));
                    var top=new PaintLayer(Guid.NewGuid().ToString("D"),"模様",new PaintImage(64,64,new Rgba32(245,70,145)),.75f,true,
                        new PaintMask(64,64,Enumerable.Range(0,4096).Select(i=>(byte)((i%64)*4)).ToArray()));
                    var hidden=new PaintLayer(Guid.NewGuid().ToString("D"),"保存する非表示層",new PaintImage(64,64,new Rgba32(0,0,255)),1,false);
                    var stack=new PaintLayers(64,64,new[]{bottom,top,hidden});
                    graph=graph.ReplaceNode(GraphNode.LayeredPaint(paint,stack,context.UvHash,context.MeshDomain));
                    ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null);Execute(AuthoringOperation.AddGraph(graph));
                    string state=workspace.Document.StateHash,image=workspace.Preview.Output.BaseColor.ImageHash;
                    projectPath.SetValueWithoutNotify(Path.Combine(directory,"layered-project"));SaveProject();OpenProject();
                    var read=workspace.Document.Objects[0].Graph.Nodes[paint].LayerStack;
                    Check(workspace.Document.StateHash==state && workspace.Preview.Output.BaseColor.ImageHash==image &&
                        read.Layers.Count==3 && !read.Layers[2].Visible && read.Layers[2].Id==hidden.Id && read.Layers[1].Mask!=null,"Layer native project roundtrip differs");
                    var surface=SurfaceBakeStore.Read(SurfaceBakeStore.Export(Path.Combine(directory,"layer-surface"),workspace));
                    Check(surface.BaseColor.CopyRgba().SequenceEqual(stack.Composite().CopyRgba()),"Layer Surface output differs from composite");
                    Frame();SetStatus("Layer graph: 3層・opacity・mask・非表示層をnative保存。合成表示とSurface出力を確認。");
                }
                catch(Exception e) { failure=e.ToString(); }
                if(failure==null)
                    yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(directory,"layer-graph.png"),error=>failure=error);
                completed(failure);
            }
            finally { ReplaceWorkspace(previous,previousPath); }
        }
    }
}
