using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Inspection;
using NyaForge.Authoring.Topology;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    static void RunValidationTests()
    {
        Test("validation reports incomplete output as unknown without mutation", () =>
        {
            var workspace = AuthoringWorkspace.CreateEmpty();
            var before = workspace.Document.StateHash;
            var request = AuthoringValidationRequest.Read(new JObject { ["documentId"] = workspace.Document.DocumentId, ["expectedRevision"] = 0, ["profile"] = "pc" });
            var result = AuthoringValidationReader.Read(workspace, workspace.InstanceId, request);
            Equal("unknown", (string)result["status"]); Equal(before, workspace.Document.StateHash); Equal(0L, (long)result["revision"]);
        });

        Test("mobile validation fails when two material slots are assigned", () =>
        {
            var imported = TriangleMeshAdapter.Import(GraphId(), AuthoringFixtures.Panel(1));
            var polygon = new PolygonMesh(imported.DomainId, imported.Vertices.Values,
                imported.Faces.Select((face, index) => new CageFace(face.Id, index % 2 == 0 ? 3 : 9, face.Corners)));
            string source = GraphId(), red = GraphId(), blue = GraphId(), slots = GraphId(), output = GraphId();
            var graph = new AuthoringGraph(GraphId(), new[] { GraphNode.Polygon(source, polygon, new RestTransform(1, new Vec3())), GraphNode.StandardMaterial(red), GraphNode.StandardMaterial(blue), GraphNode.AssignMaterials(slots, new[] { 3, 9 }), GraphNode.Output(output) },
                new[] { new GraphEdge(source, "mesh", slots, "mesh"), new GraphEdge(red, "material", slots, "material-3"), new GraphEdge(blue, "material", slots, "material-9"), new GraphEdge(slots, "mesh", output, "mesh") }, output);
            var workspace = AuthoringWorkspace.CreateEmpty();
            Ok(new AuthoringCommandService(workspace).Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph))));
            var request = AuthoringValidationRequest.Read(new JObject { ["documentId"] = workspace.Document.DocumentId, ["expectedRevision"] = workspace.Document.DocumentRevision, ["profile"] = "mobile" });
            var result = AuthoringValidationReader.Read(workspace, workspace.InstanceId, request);
            Equal("fail", (string)result["status"]); Equal(2, (int)result["metrics"]["materials"]); Equal("fail", (string)result["checks"].Children<JObject>().Single(c => (string)c["name"] == "materials")["status"]);
        });

        Test("validation rejects a stale revision", () =>
        {
            var workspace = AuthoringWorkspace.CreateFixture();
            var request = AuthoringValidationRequest.Read(new JObject { ["documentId"] = workspace.Document.DocumentId, ["expectedRevision"] = 1, ["profile"] = "pc" });
            Expect("REVISION_CONFLICT", () => AuthoringValidationReader.Read(workspace, workspace.InstanceId, request));
        });
    }
}
