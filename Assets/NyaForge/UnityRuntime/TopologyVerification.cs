using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Topology;
using NyaForge.Authoring.Graph;
using System.IO;

namespace NyaForge.UnityRuntime
{
    internal static class TopologyVerification
    {
        internal static void Verify(string output, List<string> checks)
        {
            var positions = new[] { new Vec3(0, 0, 0), new Vec3(.2f, 0, 0), new Vec3(.2f, .2f, 0), new Vec3(.1f, .1f, 0), new Vec3(0, .2f, 0) };
            var cage = new PolygonMesh(Guid.NewGuid().ToString("D"), positions.Select((p, i) => new CageVertex((ulong)i + 1, p)),
                new[] { new CageFace(1, 0, positions.Select((_, i) => new CageCorner((ulong)i + 1, (ulong)i + 1))) });
            var rendered = PolygonRenderAdapter.Build(cage); var mesh = OwnedMeshProjection.CreateMesh(rendered.Mesh);
            try
            {
                if (mesh.vertexCount != 5 || mesh.triangles.Length != 9 || rendered.RenderTriangleMap.Any(id => id != 1))
                    throw new InvalidOperationException("Polygon render mapping differs in Player");
            }
            finally { UnityEngine.Object.Destroy(mesh); }
            checks.Add("polygon Core in Player: concave face triangulation, stable face mapping and owned Unity mesh creation");
            string source = Guid.NewGuid().ToString("D"), sink = Guid.NewGuid().ToString("D");
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), new[] { GraphNode.Polygon(source, cage, new RestTransform(1, new Vec3())), GraphNode.Output(sink) },
                new[] { new GraphEdge(source, "mesh", sink, "mesh") }, sink);
            var workspace = AuthoringWorkspace.CreateEmpty();
            var result = new AuthoringCommandService(workspace).Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph)));
            if (!result.Success) throw new InvalidOperationException(result.Code);
            string directory = Path.Combine(output, "polygon-project"); ProjectStore.Save(directory, workspace, 0);
            var loaded = ProjectStore.Open(directory);
            if (loaded.Preview.Output.Polygon.Faces[0].Corners.Count != 5 || loaded.Evaluate().ContentHash != rendered.Mesh.ContentHash)
                throw new InvalidOperationException("Polygon graph save lost cage data");
            BakeStore.Export(Path.Combine(directory, "export"), loaded);
            checks.Add("polygon graph in Player: native save/reopen keeps five-corner face and Bake emits evaluated mesh");
        }
    }
}
