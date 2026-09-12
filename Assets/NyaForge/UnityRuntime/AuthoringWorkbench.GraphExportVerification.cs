using System;
using System.Collections.Generic;
using System.IO;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void VerifyGraphExports(string output, List<string> checks, List<string> exports)
        {
            var previous = workspace; string previousPath = savedDirectory;
            try
            {
                foreach (float scale in new[] { 1f, 100f })
                {
                    ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
                    string source = Guid.NewGuid().ToString("D"), edit = Guid.NewGuid().ToString("D"), sink = Guid.NewGuid().ToString("D");
                    var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] {
                        GraphNode.Source(source, AuthoringFixtures.Panel(scale), new RestTransform(scale, new Vec3())), GraphNode.Edit(edit), GraphNode.Output(sink) },
                        new[] { new GraphEdge(source, "mesh", edit, "mesh"), new GraphEdge(edit, "mesh", sink, "mesh") }, sink);
                    Execute(AuthoringOperation.AddGraph(graph)); SelectEditStage(editStageIds.IndexOf(edit)); Select(new[] { 0 });
                    moveX.SetValueWithoutNotify(10); moveY.SetValueWithoutNotify(0); moveZ.SetValueWithoutNotify(0); MoveSelection();
                    Check(Math.Abs(projection.Points[0].x + .09f) < .00001f, "Graph scale GUI edit is not 1cm");
                    string directory = Path.Combine(output, "graph-scale" + scale);
                    ProjectStore.Save(directory, workspace, 0); ReplaceWorkspace(ProjectStore.Open(directory), directory);
                    string manifest = BakeStore.Export(Path.Combine(directory, "export"), workspace); exports.Add(manifest);
                    var bake = BakeStore.Read(manifest);
                    Check(bake.MeshContentHash == workspace.Evaluate().ContentHash && bake.Transform.Scale == scale, "Graph Bake lost final geometry or scale");
                    Execute(AuthoringOperation.Disconnect(sink, "mesh"));
                    string rejected = Path.Combine(directory, "incomplete-export"); bool refused = false;
                    try { BakeStore.Export(rejected, workspace); }
                    catch (AuthoringException e) { refused = e.Code == "GRAPH_INCOMPLETE"; }
                    Check(refused && !Directory.Exists(rejected), "Incomplete graph export created output");
                    checks.Add("graph scale" + scale + ": GUI 1cm edit, schema3 reopen, final Bake, incomplete export rejection");
                }
            }
            finally { ReplaceWorkspace(previous, previousPath); }
        }
    }
}
