using System;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;
using NyaForge.Authoring.Paint;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    static void RunGlbExportServiceTests()
    {
        Test("standard static GLB export is readable and preserves evaluated geometry", () =>
        {
            var workspace = AuthoringWorkspace.CreateFixture();
            string directory = Path.Combine(Root, "glb-static-" + Guid.NewGuid().ToString("N"));
            var result = GlbExportService.ExportStatic(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory);
            True(File.Exists(result.Path)); Equal(GlbExportProfile.StaticGeometry, result.Profile); Equal(1, result.ObjectCount);
            var imported = GlbImporter.Read(File.ReadAllBytes(result.Path));
            Equal(workspace.Evaluate().VertexCount, imported.Mesh.VertexCount);
            Equal(workspace.Evaluate().TriangleCount, imported.Mesh.TriangleCount);
            Equal(workspace.Evaluate().Positions[0].X, imported.Mesh.Positions[0].X);
        });

        Test("GLB export validates the observed revision before creating output", () =>
        {
            var workspace = AuthoringWorkspace.CreateFixture();
            string directory = Path.Combine(Root, "glb-stale-" + Guid.NewGuid().ToString("N"));
            Expect("REVISION_CONFLICT", () => GlbExportService.ExportStatic(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision + 1, directory));
            True(!Directory.Exists(directory));
        });

        Test("standard skinned GLB export roundtrips skeleton and weights", () =>
        {
            var mesh = PrimitiveGeometry.Plane(.2f, .1f);
            string rootId = GraphId(), skeletonId = GraphId(), sourceId = GraphId(), bindId = GraphId(), poseId = GraphId(), deformId = GraphId(), outputId = GraphId();
            var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(rootId, "Root", "", new Vec3(), new Vec3(0, .1f, 0)) });
            var binding = SkinBinding.Create(mesh, skeleton, Enumerable.Range(0, mesh.VertexCount).Select(i => new SkinBinding.VertexWeightInput(i, rootId, 1f)));
            var pose = PoseSet.Create(skeleton, new[] { new BonePose(rootId, PoseTransform.FromTranslation(new Vec3())) });
            var graph = new AuthoringGraph(GraphId(),
                new[] { GraphNode.Source(sourceId, mesh, new RestTransform(1, new Vec3())), GraphNode.SkeletonNode(skeletonId, skeleton), GraphNode.SkinBindNode(bindId, binding), GraphNode.PoseNode(poseId, pose), GraphNode.SkinDeformNode(deformId), GraphNode.Output(outputId) },
                new[] { new GraphEdge(sourceId, "mesh", bindId, "mesh"), new GraphEdge(skeletonId, "skeleton", bindId, "skeleton"), new GraphEdge(skeletonId, "skeleton", poseId, "skeleton"), new GraphEdge(sourceId, "mesh", deformId, "mesh"), new GraphEdge(skeletonId, "skeleton", deformId, "skeleton"), new GraphEdge(bindId, "binding", deformId, "binding"), new GraphEdge(poseId, "pose", deformId, "pose"), new GraphEdge(deformId, "mesh", outputId, "mesh") }, outputId);
            var workspace = AuthoringWorkspace.CreateEmpty(); Ok(Execute(workspace, AuthoringOperation.AddGraph(graph)));
            string directory = Path.Combine(Root, "glb-skinned-" + Guid.NewGuid().ToString("N"));
            var result = GlbExportService.ExportSkinned(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory);
            var imported = GlbSkinImporter.Read(File.ReadAllBytes(result.Path));
            Equal(mesh.VertexCount, imported.Mesh.VertexCount); Equal(1, imported.Skeleton.Bones.Count); Equal(mesh.VertexCount, imported.Binding.Weights.Count);
            string transformedDirectory = Path.Combine(Root, "glb-skinned-instance-" + Guid.NewGuid().ToString("N"));
            var instance = SourceAffine.FromTrs(new Vec3(1, 2, 3), new Vec4(0, 0, (float)Math.Sqrt(.5), (float)Math.Sqrt(.5)), new Vec3(2, 3, 4));
            var transformed = GlbExportService.ExportSkinned(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, transformedDirectory, instance);
            var json = JObject.Parse(ReadJsonChunk(File.ReadAllBytes(transformed.Path))); var node = ((JArray)json["nodes"]!).OfType<JObject>().Single(value => value["mesh"] != null);
            Equal(16, ((JArray)node["matrix"]!).Count); Near(1f, (float)node["matrix"]![12]!); Near(2f, (float)node["matrix"]![13]!); Near(3f, (float)node["matrix"]![14]!);
            var inventory = GlbSceneInventoryReader.Read(File.ReadAllBytes(transformed.Path)); var restored = GlbSkinImporter.Read(File.ReadAllBytes(transformed.Path), 0, 0, inventory.Instances.Single().WorldTransform);
            True(restored.InstanceWorldTransform != null); Near(1f, restored.InstanceWorldTransform.TransformPoint(new Vec3()).X); Near(2f, restored.InstanceWorldTransform.TransformPoint(new Vec3()).Y); Near(3f, restored.InstanceWorldTransform.TransformPoint(new Vec3()).Z);
        });

        Test("extended skinned GLB export roundtrips every authored influence set", () =>
        {
            var mesh = PrimitiveGeometry.Plane(.2f, .1f);
            string sourceId = GraphId(), skeletonId = GraphId(), bindId = GraphId(), poseId = GraphId(), deformId = GraphId(), outputId = GraphId();
            var bones = Enumerable.Range(0, 8).Select(i => new BoneDefinition(GraphId(), "Bone" + i, "", new Vec3(i * .01f, 0, 0), new Vec3(i * .01f, .1f, 0))).ToArray();
            var skeleton = new SkeletonDefinition(bones);
            var binding = SkinBinding.Create(mesh, skeleton, Enumerable.Range(0, mesh.VertexCount).SelectMany(vertex => bones.Select(bone => new SkinBinding.VertexWeightInput(vertex, bone.BoneId, .125f))));
            var pose = PoseSet.Create(skeleton, bones.Select(bone => new BonePose(bone.BoneId, PoseTransform.FromTranslation(bone.Head))));
            var graph = new AuthoringGraph(GraphId(),
                new[] { GraphNode.Source(sourceId, mesh, new RestTransform(1, new Vec3())), GraphNode.SkeletonNode(skeletonId, skeleton), GraphNode.SkinBindNode(bindId, binding), GraphNode.PoseNode(poseId, pose), GraphNode.SkinDeformNode(deformId), GraphNode.Output(outputId) },
                new[] { new GraphEdge(sourceId, "mesh", bindId, "mesh"), new GraphEdge(skeletonId, "skeleton", bindId, "skeleton"), new GraphEdge(skeletonId, "skeleton", poseId, "skeleton"), new GraphEdge(sourceId, "mesh", deformId, "mesh"), new GraphEdge(skeletonId, "skeleton", deformId, "skeleton"), new GraphEdge(bindId, "binding", deformId, "binding"), new GraphEdge(poseId, "pose", deformId, "pose"), new GraphEdge(deformId, "mesh", outputId, "mesh") }, outputId);
            var workspace = AuthoringWorkspace.CreateEmpty(); Ok(Execute(workspace, AuthoringOperation.AddGraph(graph)));
            string directory = Path.Combine(Root, "glb-skinned-extended-" + Guid.NewGuid().ToString("N"));
            var result = GlbExportService.ExportSkinnedExtended(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory);
            Equal(GlbExportProfile.SkinnedGeometryExtended, result.Profile);
            var bytes = File.ReadAllBytes(result.Path); var json = JObject.Parse(ReadJsonChunk(bytes));
            var attrs = (JObject)((JObject)((JArray)((JObject)((JArray)json["meshes"]!)[0])!["primitives"]!)[0])!["attributes"]!;
            True(attrs["JOINTS_1"] != null && attrs["WEIGHTS_1"] != null);
            var imported = GlbSkinImporter.Read(bytes);
            Equal(8, imported.Skeleton.Bones.Count); Equal(8, imported.Binding.Weights[0].Count); Near(1f, imported.Binding.Weights[0].Sum(weight => weight.Weight));
        });

        Test("skinned GLB export keeps a rest-pose vertex edit", () =>
        {
            var mesh = PrimitiveGeometry.Plane(.2f, .1f);
            string rootId = GraphId(), skeletonId = GraphId(), sourceId = GraphId(), bindId = GraphId(), poseId = GraphId(), deformId = GraphId(), outputId = GraphId();
            var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(rootId, "Root", "", new Vec3(), new Vec3(0, .1f, 0)) });
            var binding = SkinBinding.Create(mesh, skeleton, Enumerable.Range(0, mesh.VertexCount).Select(i => new SkinBinding.VertexWeightInput(i, rootId, 1f)));
            var pose = PoseSet.Create(skeleton, new[] { new BonePose(rootId, PoseTransform.FromTranslation(new Vec3())) });
            var baseGraph = new AuthoringGraph(GraphId(),
                new[] { GraphNode.Source(sourceId, mesh, new RestTransform(1, new Vec3())), GraphNode.SkeletonNode(skeletonId, skeleton), GraphNode.SkinBindNode(bindId, binding), GraphNode.PoseNode(poseId, pose), GraphNode.SkinDeformNode(deformId), GraphNode.Output(outputId) },
                new[] { new GraphEdge(sourceId, "mesh", bindId, "mesh"), new GraphEdge(skeletonId, "skeleton", bindId, "skeleton"), new GraphEdge(skeletonId, "skeleton", poseId, "skeleton"), new GraphEdge(sourceId, "mesh", deformId, "mesh"), new GraphEdge(skeletonId, "skeleton", deformId, "skeleton"), new GraphEdge(bindId, "binding", deformId, "binding"), new GraphEdge(poseId, "pose", deformId, "pose"), new GraphEdge(deformId, "mesh", outputId, "mesh") }, outputId);
            var baseEvaluation = GraphEvaluator.Evaluate(baseGraph);
            string editId = GraphId(), editedOutputId = GraphId();
            var edit = GraphNode.Edit(editId, true, new System.Collections.Generic.Dictionary<int, Vec3> { [0] = new Vec3(.01f, 0, 0) }, baseEvaluation.Output.SnapshotHash, baseEvaluation.Output.DomainId);
            var editedGraph = new AuthoringGraph(baseGraph.GraphId, baseGraph.Nodes.Values.Concat(new[] { edit, GraphNode.Output(editedOutputId) }), baseGraph.Edges.Concat(new[] { new GraphEdge(deformId, "mesh", editId, "mesh"), new GraphEdge(editId, "mesh", editedOutputId, "mesh") }), editedOutputId);
            var workspace = AuthoringWorkspace.CreateEmpty(); Ok(Execute(workspace, AuthoringOperation.AddGraph(editedGraph)));
            string directory = Path.Combine(Root, "glb-skinned-edit-" + Guid.NewGuid().ToString("N"));
            var result = GlbExportService.ExportSkinned(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory);
            var imported = GlbSkinImporter.Read(File.ReadAllBytes(result.Path));
            Near(mesh.Positions[0].X + .01f, imported.Mesh.Positions[0].X);
        });

        Test("standard GLB export retains normal and tangent morph attributes", () =>
        {
            var mesh = AuthoringFixtures.Panel(1); string id = GraphId();
            var morph = MorphTarget.Create(mesh, id, "Normals", new[] { new MorphDelta(0, new Vec3(.01f, 0, 0)) },
                new[] { new MorphDelta(0, new Vec3(0, .2f, 0)) }, new[] { new MorphDelta(0, new Vec3(0, .1f, 0)) });
            var morphs = MorphSet.Create(mesh, new[] { morph });
            var bytes = GlbWriter.Build(new[] { new GlbExportService.MeshObject { Mesh = mesh, Morphs = morphs, Name = "morph" } }, null, GlbExportProfile.StaticGeometry);
            var imported = GlbImporter.Read(bytes);
            Equal(1, imported.Morphs.Targets[0].NormalDeltas.Count); Equal(1, imported.Morphs.Targets[0].TangentDeltas.Count);
            Near(.2f, imported.Morphs.Targets[0].NormalDeltas[0].Y); Near(.1f, imported.Morphs.Targets[0].TangentDeltas[0].Y);
        });

        Test("GLB export preserves standard PBR material and embedded base color", () =>
        {
            var mesh = AuthoringFixtures.Panel(1);
            var image = new PaintImage(2, 1, new Rgba32(255, 0, 128, 255));
            var parameters = new MaterialParameters(new Vec4(.25f, .5f, .75f, .8f), .3f, .7f, new Vec3(.1f, .2f, .3f), MaterialAlphaMode.Cutout, .4f);
            var material = new GraphMaterialValue(parameters, new GraphImageValue(image, "", "fixture-domain"));
            var bytes = GlbWriter.Build(new[] { new GlbExportService.MeshObject { Mesh = mesh, Material = material, Name = "pbr" } }, null, GlbExportProfile.StaticGeometry);
            var json = JObject.Parse(ReadJsonChunk(bytes));
            True(json["materials"] is JArray && ((JArray)json["materials"]).Count == 1);
            True(json["images"] is JArray && ((JArray)json["images"]).Count == 1);
            Equal("MASK", (string)json["materials"]![0]!["alphaMode"]!);
            var imported = GlbImporter.Read(bytes);
            Equal(1, imported.Materials.Count); Equal(.3f, imported.Materials[0].Parameters.Metallic); Equal(.7f, imported.Materials[0].Parameters.Roughness);
            True(imported.Materials[0].HasEmbeddedBaseColorImage); True(imported.Materials[0].CopyBaseColorImageBytes().Length > 8);
        });
    }
}
