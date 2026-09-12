using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Inspection;
using NyaForge.Authoring.Rig;
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

        Test("validation reports evaluated skin capacity instead of unknown", () =>
        {
            var mesh = AuthoringFixtures.Panel(1);
            string root = GraphId(), child = GraphId(), skeletonNode = GraphId(), source = GraphId(), bindingNode = GraphId(), poseNode = GraphId(), deform = GraphId(), output = GraphId();
            var skeleton = new SkeletonDefinition(new[]
            {
                new BoneDefinition(root, "Root", "", new Vec3(), new Vec3(0, .1f, 0)),
                new BoneDefinition(child, "Child", root, new Vec3(0, .1f, 0), new Vec3(0, .2f, 0))
            });
            var binding = SkinBinding.Create(mesh, skeleton, Enumerable.Range(0, mesh.VertexCount).SelectMany(i => new[]
            {
                new SkinBinding.VertexWeightInput(i, root, .25f),
                new SkinBinding.VertexWeightInput(i, child, .75f)
            }));
            var pose = PoseSet.Create(skeleton, skeleton.Bones.Select(bone => new BonePose(bone.BoneId, PoseTransform.FromTranslation(bone.Head))));
            string unusedRoot = GraphId(), unusedChild = GraphId(), unusedSkeletonNode = GraphId(), unusedSource = GraphId(), unusedBindingNode = GraphId(), unusedPoseNode = GraphId(), unusedDeform = GraphId();
            var unusedSkeleton = new SkeletonDefinition(new[]
            {
                new BoneDefinition(unusedRoot, "UnusedRoot", "", new Vec3(), new Vec3(0, .1f, 0)),
                new BoneDefinition(unusedChild, "UnusedChild", unusedRoot, new Vec3(0, .1f, 0), new Vec3(0, .2f, 0)),
                new BoneDefinition(GraphId(), "UnusedLeaf", unusedChild, new Vec3(0, .2f, 0), new Vec3(0, .3f, 0))
            });
            var unusedBinding = SkinBinding.Create(mesh, unusedSkeleton, Enumerable.Range(0, mesh.VertexCount).Select(i => new SkinBinding.VertexWeightInput(i, unusedRoot, 1f)));
            var unusedPose = PoseSet.Create(unusedSkeleton, unusedSkeleton.Bones.Select(bone => new BonePose(bone.BoneId, PoseTransform.FromTranslation(bone.Head))));
            var graph = new AuthoringGraph(GraphId(),
                new[] { GraphNode.Source(source, mesh, new RestTransform(1, new Vec3())), GraphNode.SkeletonNode(skeletonNode, skeleton), GraphNode.SkinBindNode(bindingNode, binding), GraphNode.PoseNode(poseNode, pose), GraphNode.SkinDeformNode(deform), GraphNode.Output(output),
                    GraphNode.Source(unusedSource, mesh, new RestTransform(1, new Vec3())), GraphNode.SkeletonNode(unusedSkeletonNode, unusedSkeleton), GraphNode.SkinBindNode(unusedBindingNode, unusedBinding), GraphNode.PoseNode(unusedPoseNode, unusedPose), GraphNode.SkinDeformNode(unusedDeform) },
                new[]
                {
                    new GraphEdge(source, "mesh", bindingNode, "mesh"), new GraphEdge(skeletonNode, "skeleton", bindingNode, "skeleton"),
                    new GraphEdge(skeletonNode, "skeleton", poseNode, "skeleton"), new GraphEdge(source, "mesh", deform, "mesh"),
                    new GraphEdge(skeletonNode, "skeleton", deform, "skeleton"), new GraphEdge(bindingNode, "binding", deform, "binding"),
                    new GraphEdge(poseNode, "pose", deform, "pose"), new GraphEdge(deform, "mesh", output, "mesh"),
                    new GraphEdge(unusedSource, "mesh", unusedBindingNode, "mesh"), new GraphEdge(unusedSkeletonNode, "skeleton", unusedBindingNode, "skeleton"),
                    new GraphEdge(unusedSkeletonNode, "skeleton", unusedPoseNode, "skeleton"), new GraphEdge(unusedSource, "mesh", unusedDeform, "mesh"),
                    new GraphEdge(unusedSkeletonNode, "skeleton", unusedDeform, "skeleton"), new GraphEdge(unusedBindingNode, "binding", unusedDeform, "binding"),
                    new GraphEdge(unusedPoseNode, "pose", unusedDeform, "pose")
                }, output);
            var workspace = AuthoringWorkspace.CreateEmpty(); Ok(new AuthoringCommandService(workspace).Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph))));
            var request = AuthoringValidationRequest.Read(new JObject { ["documentId"] = workspace.Document.DocumentId, ["expectedRevision"] = workspace.Document.DocumentRevision, ["profile"] = "pc" });
            var result = AuthoringValidationReader.Read(workspace, workspace.InstanceId, request);
            Equal("pass", (string)result["status"]); Equal(2, (int)result["metrics"]["bones"]); Equal(2, (int)result["metrics"]["maxInfluences"]);
            Equal("pass", (string)result["checks"].Children<JObject>().Single(c => (string)c["name"] == "bones")["status"]);
            Equal("pass", (string)result["checks"].Children<JObject>().Single(c => (string)c["name"] == "maxInfluences")["status"]);
        });

        Test("validation rejects a stale revision", () =>
        {
            var workspace = AuthoringWorkspace.CreateFixture();
            var request = AuthoringValidationRequest.Read(new JObject { ["documentId"] = workspace.Document.DocumentId, ["expectedRevision"] = 1, ["profile"] = "pc" });
            Expect("REVISION_CONFLICT", () => AuthoringValidationReader.Read(workspace, workspace.InstanceId, request));
        });
    }
}
