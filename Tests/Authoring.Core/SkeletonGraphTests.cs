using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Inspection;
using NyaForge.Authoring.Rig;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    static void RunSkeletonGraphTests()
    {
        Test("skeleton graph node evaluates as a typed value and survives native graph storage", () =>
        {
            string root = GraphId(), skeletonNode = GraphId(), plane = GraphId(), output = GraphId();
            var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(root, "Root", "", new Vec3(), new Vec3(0, .1f, 0)) });
            var graph = new AuthoringGraph(GraphId(), new[] { GraphNode.SkeletonNode(skeletonNode, skeleton), GraphNode.Plane(plane), GraphNode.Output(output) },
                new[] { new GraphEdge(plane, "mesh", output, "mesh") }, output);
            var workspace = AuthoringWorkspace.CreateEmpty(); Ok(new AuthoringCommandService(workspace).Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph))));
            True(workspace.Preview.Evaluation.SkeletonOutputs.ContainsKey(skeletonNode)); Equal(skeleton.ContentHash, workspace.Preview.Evaluation.SkeletonOutputs[skeletonNode].Skeleton.ContentHash);
            var inspection = AuthoringGraphReader.Read(workspace, workspace.InstanceId); var skeletonInfo = inspection["graph"]["nodes"].Children<JObject>().Single(n => (string)n["nodeId"] == skeletonNode)["skeletonOutput"];
            Equal(skeleton.ContentHash, (string)skeletonInfo["skeletonHash"]); Equal(1, (int)skeletonInfo["boneCount"]);
            string directory = Dir("skeleton-graph-native"); ProjectStore.Save(directory, workspace, 0); var reopened = ProjectStore.Open(directory);
            Equal(workspace.Document.StateHash, reopened.Document.StateHash); True(reopened.Preview.Evaluation.SkeletonOutputs.ContainsKey(skeletonNode));
            Equal(skeleton.ContentHash, reopened.Document.Objects[0].Graph.Nodes[skeletonNode].Skeleton.ContentHash);
        });

        Test("skin bind graph node validates typed mesh/skeleton inputs and survives native storage", () =>
        {
            string root = GraphId(), skeletonNode = GraphId(), plane = GraphId(), bind = GraphId(), output = GraphId();
            var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(root, "Root", "", new Vec3(), new Vec3(0, .1f, 0)) });
            var mesh = PrimitiveGeometry.Plane(.2f, .1f);
            var binding = SkinBinding.Create(mesh, skeleton, Enumerable.Range(0, mesh.VertexCount).Select(i => new SkinBinding.VertexWeightInput(i, root, 1)));
            var graph = new AuthoringGraph(GraphId(), new[] { GraphNode.Plane(plane), GraphNode.SkeletonNode(skeletonNode, skeleton), GraphNode.SkinBindNode(bind, binding), GraphNode.Output(output) },
                new[] { new GraphEdge(plane, "mesh", bind, "mesh"), new GraphEdge(skeletonNode, "skeleton", bind, "skeleton"), new GraphEdge(plane, "mesh", output, "mesh") }, output);
            var workspace = AuthoringWorkspace.CreateEmpty(); Ok(new AuthoringCommandService(workspace).Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph))));
            True(workspace.Preview.Evaluation.SkinBindingOutputs.ContainsKey(bind));
            var inspection = AuthoringGraphReader.Read(workspace, workspace.InstanceId); var bindingInfo = inspection["graph"]["nodes"].Children<JObject>().Single(n => (string)n["nodeId"] == bind)["skinBindingOutput"];
            Equal(mesh.TopologyHash, (string)bindingInfo["meshTopologyHash"]); Equal(mesh.VertexCount, (int)bindingInfo["vertexCount"]); Equal(1, (int)bindingInfo["maxInfluencesPerVertex"]);
            string directory = Dir("skin-bind-graph-native"); ProjectStore.Save(directory, workspace, 0); var reopened = ProjectStore.Open(directory);
            True(reopened.Preview.Evaluation.SkinBindingOutputs.ContainsKey(bind)); Equal(binding.SkeletonHash, reopened.Preview.Evaluation.SkinBindingOutputs[bind].Binding.SkeletonHash);
        });

        Test("skin binding editing reassigns selected vertices deterministically", () =>
        {
            string root = GraphId(), child = GraphId();
            var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(root, "Root", "", new Vec3(), new Vec3(0, .1f, 0)), new BoneDefinition(child, "Child", root, new Vec3(), new Vec3(.1f, 0, 0)) });
            var mesh = PrimitiveGeometry.Plane(.2f, .1f);
            var binding = SkinBinding.Create(mesh, skeleton, Enumerable.Range(0, mesh.VertexCount).SelectMany(i => new[] { new SkinBinding.VertexWeightInput(i, root, .5f), new SkinBinding.VertexWeightInput(i, child, .5f) }));
            var changed = SkinBindingEditing.AssignVertices(binding, mesh, skeleton, new[] { 2, 0, 2 }, child);
            Near(1, changed.Weights[0][0].Weight); Equal(child, changed.Weights[0][0].BoneId); Near(.5f, changed.Weights[1][0].Weight); Near(.5f, changed.Weights[1][1].Weight);
            var reordered = SkinBindingEditing.AssignVertices(binding, mesh, skeleton, new[] { 0, 2 }, child);
            True(RigCodec.WriteBinding(changed).SequenceEqual(RigCodec.WriteBinding(reordered)));
            Expect("SELECTION_EMPTY", () => SkinBindingEditing.AssignVertices(binding, mesh, skeleton, Array.Empty<int>(), root));
        });

        Test("skin binding editing blends a selected bone and rejects stale skeletons", () =>
        {
            string root = GraphId(), child = GraphId();
            var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(root, "Root", "", new Vec3(), new Vec3(0, .1f, 0)), new BoneDefinition(child, "Child", root, new Vec3(), new Vec3(.1f, 0, 0)) });
            var mesh = PrimitiveGeometry.Plane(.2f, .1f);
            var binding = SkinBinding.Create(mesh, skeleton, Enumerable.Range(0, mesh.VertexCount).Select(i => new SkinBinding.VertexWeightInput(i, root, 1)));
            var changed = SkinBindingEditing.SetVerticesWeight(binding, mesh, skeleton, new[] { 1 }, child, .25f);
            Near(.25f, changed.Weights[1].Single(v => v.BoneId == child).Weight); Near(.75f, changed.Weights[1].Single(v => v.BoneId == root).Weight);
            var replaced = SkinBindingEditing.SetVerticesWeight(changed, mesh, skeleton, new[] { 1 }, child, .5f);
            Near(.5f, replaced.Weights[1].Single(v => v.BoneId == child).Weight); Near(.5f, replaced.Weights[1].Single(v => v.BoneId == root).Weight);
            Expect("SKIN_SKELETON_CHANGED", () => SkinBindingEditing.SetVerticesWeight(changed, mesh, new SkeletonDefinition(new[] { new BoneDefinition(root, "Root", "", new Vec3(), new Vec3(0, .1f, 0)) }), new[] { 1 }, root, .5f));
        });

        Test("pose editing updates one bone and preserves the complete pose", () =>
        {
            string root = GraphId(), child = GraphId();
            var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(root, "Root", "", new Vec3(), new Vec3(0, .1f, 0)), new BoneDefinition(child, "Child", root, new Vec3(), new Vec3(.1f, 0, 0)) });
            var pose = PoseSet.Create(skeleton, new[] { new BonePose(root, PoseTransform.Identity), new BonePose(child, PoseTransform.Identity) });
            var changed = PoseEditing.SetRotationZ(pose, skeleton, child, 30);
            Equal(2, changed.Poses.Count); Equal(skeleton.ContentHash, changed.SkeletonHash); True(!changed.ContentHash.Equals(pose.ContentHash, StringComparison.Ordinal));
            var euler = PoseEditing.SetRotationEuler(pose, skeleton, child, 20, 10, 30);
            var eulerTransform = euler.Poses.Single(item => item.BoneId == child).Transform;
            True(Math.Abs(eulerTransform.TransformPoint(new Vec3(0, 1, 0)).Z) > .01f);
            Near(0, eulerTransform.Translation.X); Near(0, eulerTransform.Translation.Y); Near(0, eulerTransform.Translation.Z);
            True(PoseCodec.Write(euler).SequenceEqual(PoseCodec.Write(PoseCodec.Read(PoseCodec.Write(euler), skeleton))));
            Expect("POSE_BONE_UNKNOWN", () => PoseEditing.SetRotationZ(pose, skeleton, GraphId(), 10));
        });

        Test("rest bone movement changes skeleton identity and explicit rebind restores assets", () =>
        {
            string root = GraphId(), child = GraphId();
            var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(root, "Root", "", new Vec3(), new Vec3(0, .1f, 0)), new BoneDefinition(child, "Child", root, new Vec3(.1f, 0, 0), new Vec3(.1f, .1f, 0)) });
            var mesh = PrimitiveGeometry.Plane(.2f, .1f);
            var binding = SkinBinding.Create(mesh, skeleton, Enumerable.Range(0, mesh.VertexCount).Select(i => new SkinBinding.VertexWeightInput(i, i < 2 ? root : child, 1)));
            var pose = PoseSet.Create(skeleton, new[] { new BonePose(root, PoseTransform.Identity), new BonePose(child, PoseTransform.Identity) });
            var moved = SkeletonEditing.MoveBone(skeleton, child, new Vec3(0, .01f, 0), new Vec3(0, .01f, 0));
            True(moved.ContentHash != skeleton.ContentHash); Expect("SKIN_SKELETON_CHANGED", () => binding.ValidateFor(mesh, moved)); Expect("POSE_SKELETON_CHANGED", () => pose.ValidateFor(moved));
            Equal(moved.ContentHash, SkinBindingEditing.Rebind(binding, mesh, moved).SkeletonHash); Equal(moved.ContentHash, PoseEditing.Rebind(pose, moved).SkeletonHash);
            Expect("BONE_NOT_FOUND", () => SkeletonEditing.MoveBone(skeleton, GraphId(), new Vec3(), new Vec3()));
        });

        Test("explicit BoneId map rebinds skin, pose and graph without name or slot inference", () =>
        {
            string sourceRoot = GraphId(), sourceChild = GraphId();
            string targetRoot = GraphId(), targetChild = GraphId();
            var source = new SkeletonDefinition(new[] {
                new BoneDefinition(sourceRoot, "SourceRoot", "", new Vec3(), new Vec3(0, .1f, 0)),
                new BoneDefinition(sourceChild, "SourceChild", sourceRoot, new Vec3(0, .1f, 0), new Vec3(.1f, .1f, 0)) });
            var target = new SkeletonDefinition(new[] {
                new BoneDefinition(targetRoot, "AvatarRoot", "", new Vec3(), new Vec3(0, .1f, 0)),
                new BoneDefinition(targetChild, "AvatarChild", targetRoot, new Vec3(0, .1f, 0), new Vec3(.1f, .1f, 0)),
                new BoneDefinition(GraphId(), "Extra", "", new Vec3(.5f, 0, 0), new Vec3(.5f, .1f, 0)) });
            var mesh = PrimitiveGeometry.Plane(.2f, .1f);
            var binding = SkinBinding.Create(mesh, source, Enumerable.Range(0, mesh.VertexCount).Select(i =>
                new SkinBinding.VertexWeightInput(i, i < 2 ? sourceRoot : sourceChild, 1f)));
            var pose = PoseSet.Create(source, new[] { new BonePose(sourceRoot, PoseTransform.Identity), new BonePose(sourceChild, PoseTransform.RotationZ(20, new Vec3(.1f, .1f, 0))) });
            var map = new Dictionary<string, string> { [sourceRoot] = targetRoot, [sourceChild] = targetChild };
            var reboundBinding = SkeletonRebindAdapter.RebindBinding(binding, mesh, source, target, map);
            Equal(target.ContentHash, reboundBinding.SkeletonHash);
            Equal(targetRoot, reboundBinding.Weights[0][0].BoneId); Equal(targetChild, reboundBinding.Weights[2][0].BoneId);
            var reboundPose = SkeletonRebindAdapter.RebindPose(pose, source, target, map);
            Equal(target.ContentHash, reboundPose.SkeletonHash); Equal(3, reboundPose.Poses.Count);
            Equal(pose.Poses.Single(item => item.BoneId == sourceChild).Transform.XAxis.X, reboundPose.ByBoneId[targetChild].Transform.XAxis.X);

            string skeletonId = GraphId(), bindingId = GraphId(), poseId = GraphId(), sourceId = GraphId(), deformId = GraphId(), outputId = GraphId();
            var graph = new AuthoringGraph(GraphId(), new[] { GraphNode.Source(sourceId, mesh, new RestTransform(1, new Vec3())), GraphNode.SkeletonNode(skeletonId, source), GraphNode.SkinBindNode(bindingId, binding), GraphNode.PoseNode(poseId, pose), GraphNode.SkinDeformNode(deformId), GraphNode.Output(outputId) },
                new[] { new GraphEdge(sourceId, "mesh", bindingId, "mesh"), new GraphEdge(skeletonId, "skeleton", bindingId, "skeleton"), new GraphEdge(sourceId, "mesh", deformId, "mesh"), new GraphEdge(skeletonId, "skeleton", deformId, "skeleton"), new GraphEdge(bindingId, "binding", deformId, "binding"), new GraphEdge(poseId, "pose", deformId, "pose"), new GraphEdge(skeletonId, "skeleton", poseId, "skeleton"), new GraphEdge(deformId, "mesh", outputId, "mesh") }, outputId);
            var reboundGraph = GraphSkeletonRebindAdapter.Rebind(graph, mesh, skeletonId, bindingId, poseId, target, map);
            var evaluation = GraphEvaluator.Evaluate(reboundGraph);
            True(evaluation.IsComplete && evaluation.Output != null);
            Equal(target.ContentHash, reboundGraph.Nodes[skeletonId].Skeleton.ContentHash);
        });

        Test("skeleton rebind rejects incomplete, ambiguous, hierarchy or rest-frame maps", () =>
        {
            string sourceRoot = GraphId(), sourceChild = GraphId(), targetRoot = GraphId(), targetChild = GraphId();
            var source = new SkeletonDefinition(new[] { new BoneDefinition(sourceRoot, "Root", "", new Vec3(), new Vec3(0, .1f, 0)), new BoneDefinition(sourceChild, "Child", sourceRoot, new Vec3(0, .1f, 0), new Vec3(.1f, .1f, 0)) });
            var target = new SkeletonDefinition(new[] { new BoneDefinition(targetRoot, "TargetRoot", "", new Vec3(), new Vec3(0, .1f, 0)), new BoneDefinition(targetChild, "TargetChild", targetRoot, new Vec3(0, .1f, 0), new Vec3(.1f, .1f, 0)) });
            var mesh = PrimitiveGeometry.Plane(.2f, .1f); var binding = SkinBinding.Create(mesh, source, Enumerable.Range(0, mesh.VertexCount).Select(i => new SkinBinding.VertexWeightInput(i, sourceRoot, 1f))); var pose = PoseSet.Create(source, source.Bones.Select(bone => new BonePose(bone.BoneId, PoseTransform.Identity)));
            Expect("SKELETON_REBIND_INCOMPLETE", () => SkeletonRebindAdapter.RebindBinding(binding, mesh, source, target, new Dictionary<string, string> { [sourceRoot] = targetRoot }));
            Expect("SKELETON_REBIND_AMBIGUOUS", () => SkeletonRebindAdapter.RebindPose(pose, source, target, new Dictionary<string, string> { [sourceRoot] = targetRoot, [sourceChild] = targetRoot }));
            Expect("SKELETON_REBIND_HIERARCHY", () => SkeletonRebindAdapter.RebindPose(pose, source, target, new Dictionary<string, string> { [sourceRoot] = targetChild, [sourceChild] = targetRoot }));
            var moved = new SkeletonDefinition(new[] { new BoneDefinition(targetRoot, "TargetRoot", "", new Vec3(), new Vec3(0, .1f, 0)), new BoneDefinition(targetChild, "TargetChild", targetRoot, new Vec3(0, .101f, 0), new Vec3(.1f, .1f, 0)) });
            Expect("SKELETON_REBIND_REST", () => SkeletonRebindAdapter.RebindBinding(binding, mesh, source, moved, new Dictionary<string, string> { [sourceRoot] = targetRoot, [sourceChild] = targetChild }));
        });
    }
}
