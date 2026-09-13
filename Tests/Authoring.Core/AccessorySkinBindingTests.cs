using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;

internal static partial class Program
{
    static void RunAccessorySkinBindingTests()
    {
        Test("clothing weight transfer seeds deterministic nearest bone influences", () =>
        {
            var mesh = PrimitiveGeometry.Plane(.2f, .2f);
            string root = GraphId(), upper = GraphId();
            var skeleton = new SkeletonDefinition(new[] {
                new BoneDefinition(root, "Root", "", new Vec3(0, 0, 0), new Vec3(0, .1f, 0)),
                new BoneDefinition(upper, "Upper", root, new Vec3(0, .1f, 0), new Vec3(0, .3f, 0))
            });
            var first = SkinWeightTransfer.ByBoneProximity(mesh, skeleton, .02f, 2);
            var second = SkinWeightTransfer.ByBoneProximity(mesh, skeleton, .02f, 2);
            Equal(first.ContentHash, second.ContentHash);
            True(first.Weights.Values.All(values => values.Count == 2 && Math.Abs(values.Sum(value => value.Weight) - 1f) < 1e-6f));
            True(first.Weights.Values.SelectMany(values => values).All(value => value.BoneId == root || value.BoneId == upper));
            Expect("INVALID_WEIGHT", () => SkinWeightTransfer.ByBoneProximity(mesh, skeleton, 0f));
        });

        Test("clothing surface transfer interpolates avatar triangle weights", () =>
        {
            var avatar = PrimitiveGeometry.Plane(.2f, .2f);
            var clothing = new MeshData(
                new[] { new Vec3(0, 0, 0), new Vec3(.04f, 0, 0), new Vec3(0, .04f, 0) },
                new[] { new Vec3(0, 0, -1), new Vec3(0, 0, -1), new Vec3(0, 0, -1) },
                new[] { new Vec4(1, 0, 0, -1), new Vec4(1, 0, 0, -1), new Vec4(1, 0, 0, -1) },
                new[] { new Vec2(0, 0), new Vec2(1, 0), new Vec2(0, 1) },
                new[] { new[] { 0, 1, 2 } });
            string root = GraphId(), upper = GraphId();
            var skeleton = new SkeletonDefinition(new[] {
                new BoneDefinition(root, "Root", "", new Vec3(0, 0, 0), new Vec3(0, .1f, 0)),
                new BoneDefinition(upper, "Upper", root, new Vec3(0, .1f, 0), new Vec3(0, .2f, 0))
            });
            var raw = new[] {
                new SkinBinding.VertexWeightInput(0, root, 1), new SkinBinding.VertexWeightInput(1, upper, 1),
                new SkinBinding.VertexWeightInput(2, upper, 1), new SkinBinding.VertexWeightInput(3, root, 1)
            };
            var avatarBinding = SkinBinding.Create(avatar, skeleton, raw);
            var first = SkinWeightTransfer.BySurfaceProjection(clothing, new RestTransform(1, new Vec3(0, 0, .02f)),
                avatar, new RestTransform(1, new Vec3()), avatarBinding, skeleton, 2);
            var second = SkinWeightTransfer.BySurfaceProjection(clothing, new RestTransform(1, new Vec3(0, 0, .02f)),
                avatar, new RestTransform(1, new Vec3()), avatarBinding, skeleton, 2);
            Equal(first.ContentHash, second.ContentHash);
            True(first.Weights.Values.All(values => values.Count >= 1 && values.Count <= 2 && Math.Abs(values.Sum(value => value.Weight) - 1f) < 1e-6f));
            True(first.Weights.Values.Any(values => values.Count == 2));
            Expect("INFLUENCE_LIMIT", () => SkinWeightTransfer.BySurfaceProjection(clothing, new RestTransform(1, new Vec3()), avatar,
                new RestTransform(1, new Vec3()), avatarBinding, skeleton, 0));
            Expect("SKIN_TOPOLOGY_CHANGED", () => SkinWeightTransfer.BySurfaceProjection(clothing, new RestTransform(1, new Vec3()),
                AuthoringFixtures.Panel(1), new RestTransform(1, new Vec3()), avatarBinding, skeleton, 2));
        });

        Test("static accessory can become a root-initialized avatar skin graph", () =>
        {
            var mesh = AuthoringFixtures.Panel(1);
            string source = GraphId(), edit = GraphId(), output = GraphId();
            var graph = new AuthoringGraph(GraphId(),
                new[] { GraphNode.Source(source, mesh, new RestTransform(1, new Vec3())), GraphNode.Edit(edit), GraphNode.Output(output) },
                new[] { new GraphEdge(source, "mesh", edit, "mesh"), new GraphEdge(edit, "mesh", output, "mesh") }, output);
            var skeleton = new SkeletonDefinition(new[] {
                new BoneDefinition(GraphId(), "Root", "", new Vec3(), new Vec3(0, .1f, 0)),
                new BoneDefinition(GraphId(), "Chest", "", new Vec3(0, .1f, 0), new Vec3(0, .2f, 0))
            });
            string root = skeleton.Bones[0].BoneId;
            var before = GraphEvaluator.Evaluate(graph);
            string avatarObjectId = GraphId();
            var changed = AccessorySkinBindingAdapter.BindToSkeleton(graph, before.MeshOutputs[edit].Mesh, skeleton, root, avatarObjectId);
            var after = GraphEvaluator.Evaluate(changed);
            True(after.IsComplete && after.Output != null && after.Output.Mesh != null);
            True(changed.Nodes.Values.Any(node => node.TypeId == BuiltinNodes.PoseSource && node.PoseSourceObjectId == avatarObjectId));
            var bind = changed.Nodes.Values.Single(node => node.TypeId == BuiltinNodes.SkinBind);
            Equal(mesh.VertexCount, bind.Binding.Weights.Count);
            True(bind.Binding.Weights.Values.All(values => values.Count == 1 && values[0].BoneId == root && Math.Abs(values[0].Weight - 1f) < 1e-6f));
            Equal(before.Output.Mesh.ContentHash, after.Output.Mesh.ContentHash);

            var pose = changed.Nodes.Values.Single(node => node.TypeId == BuiltinNodes.Pose);
            var movedPose = PoseEditing.SetRotationZ(pose.Pose, skeleton, root, 90);
            var posed = GraphEvaluator.Evaluate(changed.ReplaceNode(GraphNode.PoseNode(pose.NodeId, movedPose)));
            True(posed.IsComplete && posed.Output.Mesh.ContentHash != after.Output.Mesh.ContentHash);

            // Copying an avatar pose is a rebind onto the clothing's copied
            // skeleton. Keep the operation explicit and prove that the
            // non-rest pose survives native persistence with its deformation.
            var copiedPose = PoseEditing.Rebind(movedPose, skeleton);
            Equal(movedPose.ContentHash, copiedPose.ContentHash);
            var posedGraph = changed.ReplaceNode(GraphNode.PoseNode(pose.NodeId, copiedPose));
            var posedWorkspace = AuthoringWorkspace.CreateEmpty();
            Ok(Execute(posedWorkspace, AuthoringOperation.AddGraph(posedGraph)));
            string posedDirectory = Dir("accessory-pose-copy-native-" + Guid.NewGuid().ToString("N"));
            ProjectStore.Save(posedDirectory, posedWorkspace, 0);
            var posedReopened = ProjectStore.Open(posedDirectory);
            Equal(posedWorkspace.Preview.Output.Mesh.ContentHash, posedReopened.Preview.Output.Mesh.ContentHash);

            var workspace = AuthoringWorkspace.CreateEmpty();
            Ok(Execute(workspace, AuthoringOperation.AddGraph(changed)));
            string directory = Dir("accessory-skin-native-" + Guid.NewGuid().ToString("N"));
            ProjectStore.Save(directory, workspace, 0);
            var reopened = ProjectStore.Open(directory);
            var restored = reopened.Document.Objects[0].Graph;
            True(restored.Nodes.Values.Any(node => node.TypeId == BuiltinNodes.SkinBind));
            Equal(avatarObjectId, restored.Nodes.Values.Single(node => node.TypeId == BuiltinNodes.PoseSource).PoseSourceObjectId);
            var glb = GlbExportService.ExportSkinned(workspace, workspace.InstanceId, workspace.Document.DocumentId,
                workspace.Document.DocumentRevision, System.IO.Path.Combine(Root, "accessory-pose-source-glb-" + Guid.NewGuid().ToString("N")));
            True(System.IO.File.Exists(glb.Path));
            Equal(workspace.Preview.Output.Mesh.ContentHash, reopened.Preview.Output.Mesh.ContentHash);
        });

        Test("accessory skin binding refuses a rigid attachment conflict", () =>
        {
            var mesh = AuthoringFixtures.Panel(1); string source = GraphId(), edit = GraphId(), output = GraphId();
            var graph = new AuthoringGraph(GraphId(),
                new[] { GraphNode.Source(source, mesh, new RestTransform(1, new Vec3())), GraphNode.Edit(edit), GraphNode.Output(output),
                    GraphNode.AttachmentNode(GraphId(), GraphId(), GraphId(), Checks.Hash(new byte[] { 1 }), new Vec3()) },
                new[] { new GraphEdge(source, "mesh", edit, "mesh"), new GraphEdge(edit, "mesh", output, "mesh") }, output);
            var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(GraphId(), "Root", "", new Vec3(), new Vec3(0, .1f, 0)) });
            Expect("ACCESSORY_ATTACHMENT_CONFLICT", () => AccessorySkinBindingAdapter.BindToSkeleton(graph, mesh, skeleton, skeleton.Bones[0].BoneId));
        });
    }
}
