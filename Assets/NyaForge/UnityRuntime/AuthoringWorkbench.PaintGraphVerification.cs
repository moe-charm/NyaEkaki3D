using System;
using System.Collections;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;
using NyaForge.Authoring.Topology;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyPaintGraph(string directory, Action<string> completed)
        {
            var previous = workspace; string previousPath = savedDirectory, failure = null;
            try
            {
                try
                {
                    string source = Guid.NewGuid().ToString("D"), paint = Guid.NewGuid().ToString("D"), output = Guid.NewGuid().ToString("D");
                    var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[]
                    {
                        GraphNode.Polygon(source,PolygonPrimitives.Plane(Guid.NewGuid().ToString("D")),new RestTransform(1,new Vec3())),
                        GraphNode.Paint(paint,128,128),GraphNode.Output(output)
                    }, new[] { new GraphEdge(source,"mesh",paint,"mesh"),new GraphEdge(source,"mesh",output,"mesh"),new GraphEdge(paint,"image",output,"baseColor") },output);
                    ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null); Execute(AuthoringOperation.AddGraph(graph));
                    string before = workspace.Document.StateHash;
                    Execute(AuthoringOperation.PaintImageStroke(PaintEditing.Context(graph,paint),new[]{new Vec2(.2f,.25f),new Vec2(.8f,.75f)},8,new Rgba32(245,70,145)));
                    string after = workspace.Document.StateHash;
                    Check(after != before && workspace.Preview.Output.BaseColor != null,"Paint graph command failed");
                    Execute(AuthoringOperation.Undo()); Check(workspace.Document.StateHash == before,"Paint Undo failed");
                    Execute(AuthoringOperation.Redo()); Check(workspace.Document.StateHash == after,"Paint Redo failed");
                    Execute(AuthoringOperation.PaintImageStroke(PaintEditing.Context(workspace.Document.Objects[0].Graph,paint),new[]{new Vec2(.2f,.75f),new Vec2(.8f,.25f)},8,new Rgba32(40,160,230,180)));
                    string painted = workspace.Document.StateHash, image = workspace.Preview.Output.BaseColor.ImageHash;
                    projectPath.SetValueWithoutNotify(Path.Combine(directory,"painted-project")); SaveProject(); OpenProject();
                    Check(workspace.Document.StateHash == painted && workspace.Preview.Output.BaseColor.ImageHash == image,"Paint native project roundtrip failed");
                    var renderer = projection.DisplayObject.GetComponentInChildren<MeshRenderer>();
                    Check(renderer.sharedMaterial.mainTexture != null && renderer.sharedMaterial.mainTexture.width == 128,"Paint projection has no owned texture");
                    string rejected = Path.Combine(directory,"paint-bake-unsupported");
                    try { BakeStore.Export(rejected,workspace); throw new InvalidOperationException("Paint texture was silently discarded by static Bake"); }
                    catch (AuthoringException e) { Check(e.Code == "EXPORT_UNSUPPORTED_FEATURE" && !Directory.Exists(rejected),"Paint export refusal was not atomic"); }
                    Frame(); SetStatus("Paint graph: 2ストロークを画像付きprojectへ保存・再読込。色付き表示を確認。");
                    var surface = SurfaceBakeStore.Read(SurfaceBakeStore.Export(Path.Combine(directory,"surface-export"),workspace));
                    Check(surface.Geometry.MeshContentHash == workspace.Evaluate().ContentHash &&
                        surface.CopyPng().SequenceEqual(PaintPng.Encode(workspace.Preview.Output.BaseColor.Image)),"Surface Bake mesh/image differs");
                }
                catch (Exception e) { failure = e.ToString(); }
                if (failure == null)
                    yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(directory,"paint-graph.png"),error=>failure=error);
                string unresolved = null, retainedImage = null;
                if (failure == null) try
                {
                    var graph = workspace.Document.Objects[0].Graph;
                    var source = graph.Nodes.Values.Single(n => n.SourcePolygon != null);
                    retainedImage = workspace.Preview.Output.BaseColor.ImageHash;
                    Execute(AuthoringOperation.UpdateNode(GraphNode.Polygon(source.NodeId,PolygonUvProjection.Apply(source.SourcePolygon),source.Transform)));
                    Check(!workspace.Preview.IsComplete,"UV change did not invalidate paint");
                    unresolved = workspace.Document.StateHash;
                    paintPanel.value = true;
                }
                catch (Exception e) { failure = e.ToString(); }
                yield return null; yield return null;
                if(failure==null) try { controls.ScrollTo(paintRebindButton); } catch(Exception e) { failure=e.ToString(); }
                yield return null; yield return null;
                if (failure == null) try
                {
                    PointerProbe.Click(paintRebindButton);
                    Check(workspace.Preview.IsComplete && workspace.Preview.Output.BaseColor.ImageHash == retainedImage,"GUI rebind did not retain the image");
                    string rebound = workspace.Document.StateHash;
                    Execute(AuthoringOperation.Undo()); Check(workspace.Document.StateHash == unresolved && !workspace.Preview.IsComplete,"Rebind Undo lost unresolved state");
                    Execute(AuthoringOperation.Redo()); Check(workspace.Document.StateHash == rebound,"Rebind Redo failed");
                    projectPath.SetValueWithoutNotify(Path.Combine(directory,"rebound-project")); SaveProject(); OpenProject();
                    Check(workspace.Document.StateHash == rebound && workspace.Preview.Output.BaseColor.ImageHash == retainedImage,"Rebind native reopen differs");
                    Frame(); controls.ScrollTo(paintCanvas);
                    SetStatus("旧画像を新UVへ明示割当。画素を保持し、Undo・保存・再読込を確認。");
                }
                catch (Exception e) { failure = e.ToString(); }
                if (failure == null)
                    yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(directory,"paint-rebind.png"),error=>failure=error);
                completed(failure);
            }
            finally { paintPanel.value = false; ReplaceWorkspace(previous,previousPath); }
        }
    }
}
