using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;
using NyaForge.Authoring.Paint;
using NyaForge.Authoring.Inspection;
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
            True(File.Exists(result.ReportPath));
            var report = JObject.Parse(File.ReadAllText(result.ReportPath));
            Equal(workspace.Document.DocumentId, (string)report["documentId"]!);
            Equal(workspace.Document.StateHash, (string)report["stateHash"]!);
            Equal(Checks.Hash(File.ReadAllBytes(result.Path)), (string)report["glbHash"]!);
            Equal("StaticGeometry", (string)report["profile"]!);
            Equal(1, (int)report["objectCount"]!);
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

        Test("GLB export refuses attachment metadata instead of dropping it", () =>
        {
            string planeId = GraphId(), attachmentId = GraphId(), outputId = GraphId();
            var graph = new AuthoringGraph(GraphId(), new[] {
                GraphNode.Plane(planeId),
                GraphNode.AttachmentNode(attachmentId, GraphId(), GraphId(), Checks.Hash(new byte[] { 3, 1, 4 }), new Vec3()),
                GraphNode.Output(outputId) },
                new[] { new GraphEdge(planeId, "mesh", outputId, "mesh") }, outputId);
            var workspace = AuthoringWorkspace.CreateEmpty(); Ok(Execute(workspace, AuthoringOperation.AddGraph(graph)));
            string directory = Path.Combine(Root, "glb-attachment-refused-" + Guid.NewGuid().ToString("N"));
            Expect("GLB_ATTACHMENT_METADATA_UNSUPPORTED", () => GlbExportService.ExportStatic(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory));
            True(!Directory.Exists(directory));
        });

        Test("standard skinned GLB export roundtrips skeleton and weights", () =>
        {
            var mesh = AuthoringFixtures.Panel(1);
            string rootId = GraphId(), skeletonId = GraphId(), sourceId = GraphId(), bindId = GraphId(), poseId = GraphId(), deformId = GraphId(), materialId = GraphId(), material2Id = GraphId(), assignId = GraphId(), outputId = GraphId();
            var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(rootId, "Root", "", new Vec3(), new Vec3(0, .1f, 0)) });
            var binding = SkinBinding.Create(mesh, skeleton, Enumerable.Range(0, mesh.VertexCount).Select(i => new SkinBinding.VertexWeightInput(i, rootId, 1f)));
            var pose = PoseSet.Create(skeleton, new[] { new BonePose(rootId, PoseTransform.FromTranslation(new Vec3())) });
            var graph = new AuthoringGraph(GraphId(),
                new[] { GraphNode.Source(sourceId, mesh, new RestTransform(1, new Vec3())), GraphNode.SkeletonNode(skeletonId, skeleton), GraphNode.SkinBindNode(bindId, binding), GraphNode.PoseNode(poseId, pose), GraphNode.SkinDeformNode(deformId), GraphNode.StandardMaterial(materialId), GraphNode.StandardMaterial(material2Id), GraphNode.AssignMaterials(assignId, new[] { 0, 1 }), GraphNode.Output(outputId) },
                new[] { new GraphEdge(sourceId, "mesh", bindId, "mesh"), new GraphEdge(skeletonId, "skeleton", bindId, "skeleton"), new GraphEdge(skeletonId, "skeleton", poseId, "skeleton"), new GraphEdge(sourceId, "mesh", deformId, "mesh"), new GraphEdge(skeletonId, "skeleton", deformId, "skeleton"), new GraphEdge(bindId, "binding", deformId, "binding"), new GraphEdge(poseId, "pose", deformId, "pose"), new GraphEdge(deformId, "mesh", assignId, "mesh"), new GraphEdge(materialId, "material", assignId, GraphNode.MaterialSlotPort(0)), new GraphEdge(material2Id, "material", assignId, GraphNode.MaterialSlotPort(1)), new GraphEdge(assignId, "mesh", outputId, "mesh") }, outputId);
            var workspace = AuthoringWorkspace.CreateEmpty(); Ok(Execute(workspace, AuthoringOperation.AddGraph(graph)));
            string directory = Path.Combine(Root, "glb-skinned-" + Guid.NewGuid().ToString("N"));
            var result = GlbExportService.ExportSkinned(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory);
            var imported = GlbSkinImporter.Read(File.ReadAllBytes(result.Path));
            Equal(mesh.VertexCount, imported.Mesh.VertexCount); Equal(1, imported.Skeleton.Bones.Count); Equal(mesh.VertexCount, imported.Binding.Weights.Count); Equal(2, imported.Materials.Count);
            string transformedDirectory = Path.Combine(Root, "glb-skinned-instance-" + Guid.NewGuid().ToString("N"));
            var instance = SourceAffine.FromTrs(new Vec3(1, 2, 3), new Vec4(0, 0, (float)Math.Sqrt(.5), (float)Math.Sqrt(.5)), new Vec3(2, 3, 4));
            var transformed = GlbExportService.ExportSkinned(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, transformedDirectory, instance);
            var json = JObject.Parse(ReadJsonChunk(File.ReadAllBytes(transformed.Path))); var node = ((JArray)json["nodes"]!).OfType<JObject>().Single(value => value["mesh"] != null);
            Equal(16, ((JArray)node["matrix"]!).Count); Near(1f, (float)node["matrix"]![12]!); Near(2f, (float)node["matrix"]![13]!); Near(3f, (float)node["matrix"]![14]!);
            var inventory = GlbSceneInventoryReader.Read(File.ReadAllBytes(transformed.Path)); var restored = GlbSkinImporter.Read(File.ReadAllBytes(transformed.Path), 0, 0, inventory.Instances.Single().WorldTransform);
            True(restored.InstanceWorldTransform != null); Near(1f, restored.InstanceWorldTransform.TransformPoint(new Vec3()).X); Near(2f, restored.InstanceWorldTransform.TransformPoint(new Vec3()).Y); Near(3f, restored.InstanceWorldTransform.TransformPoint(new Vec3()).Z);
            string retainedDirectory = Path.Combine(Root, "glb-skinned-retained-bind-" + Guid.NewGuid().ToString("N"));
            var retained = SourceAffine.FromTrs(new Vec3(), new Vec4(0, 0, 0, 1), new Vec3(2, 2, 2));
            var objectId = workspace.Document.ActiveObject.ObjectId;
            var retainedResult = GlbExportService.ExportSkinnedWithTransforms(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision,
                retainedDirectory, new Dictionary<string, SourceAffine>(), new Dictionary<string, IReadOnlyList<SourceAffine>>
                { [objectId] = new[] { retained } });
            var retainedSkin = GlbSourceSkinImporter.Read(File.ReadAllBytes(retainedResult.Path)).Skin;
            Near(2f, retainedSkin.InverseBindMatrices[0].TransformVector(new Vec3(1, 0, 0)).X);
        });

        Test("multi-mesh skinned GLB export shares one skeleton", () =>
        {
            var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(GraphId(), "Root", "", new Vec3(), new Vec3(0, .1f, 0)) });
            AuthoringGraph BuildGraph(MeshData mesh, SkeletonDefinition selectedSkeleton = null)
            {
                var rig = selectedSkeleton ?? skeleton;
                string rootId = rig.Bones[0].BoneId;
                string sourceId = GraphId(), skeletonId = GraphId(), bindId = GraphId(), poseId = GraphId(), deformId = GraphId(), outputId = GraphId();
                var binding = SkinBinding.Create(mesh, rig, Enumerable.Range(0, mesh.VertexCount).Select(i => new SkinBinding.VertexWeightInput(i, rootId, 1f)));
                var pose = PoseSet.Create(rig, new[] { new BonePose(rootId, PoseTransform.FromTranslation(new Vec3())) });
                return new AuthoringGraph(GraphId(),
                    new[] { GraphNode.Source(sourceId, mesh, new RestTransform(1, new Vec3())), GraphNode.SkeletonNode(skeletonId, rig), GraphNode.SkinBindNode(bindId, binding), GraphNode.PoseNode(poseId, pose), GraphNode.SkinDeformNode(deformId), GraphNode.Output(outputId) },
                    new[] { new GraphEdge(sourceId, "mesh", bindId, "mesh"), new GraphEdge(skeletonId, "skeleton", bindId, "skeleton"), new GraphEdge(skeletonId, "skeleton", poseId, "skeleton"), new GraphEdge(sourceId, "mesh", deformId, "mesh"), new GraphEdge(skeletonId, "skeleton", deformId, "skeleton"), new GraphEdge(bindId, "binding", deformId, "binding"), new GraphEdge(poseId, "pose", deformId, "pose"), new GraphEdge(deformId, "mesh", outputId, "mesh") }, outputId);
            }
            var first = AuthoringFixtures.Panel(1);
            var second = PrimitiveGeometry.Plane(.2f, .1f);
            var workspace = AuthoringWorkspace.CreateEmpty();
            Ok(Execute(workspace, AuthoringOperation.AddGraph(BuildGraph(first))));
            Ok(Execute(workspace, AuthoringOperation.AddGraph(BuildGraph(second))));
            string directory = Path.Combine(Root, "glb-skinned-multi-" + Guid.NewGuid().ToString("N"));
            var result = GlbExportService.ExportSkinned(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory);
            Equal(2, result.ObjectCount);
            var bytes = File.ReadAllBytes(result.Path);
            var json = JObject.Parse(ReadJsonChunk(bytes));
            Equal(2, ((JArray)json["meshes"]!).Count);
            Equal(1, ((JArray)json["skins"]!).Count);
            var meshNodes = ((JArray)json["nodes"]!).OfType<JObject>().Where(node => node["mesh"] != null).ToArray();
            Equal(2, meshNodes.Length);
            Equal((int)meshNodes[0]["skin"]!, (int)meshNodes[1]["skin"]!);
            var firstImported = GlbSkinImporter.Read(bytes, 0, 0);
            var secondImported = GlbSkinImporter.Read(bytes, 1, 0);
            Equal(2, new[] { firstImported.Mesh.VertexCount, secondImported.Mesh.VertexCount }.Distinct().Count());
            True(new[] { first.VertexCount, second.VertexCount }.Contains(firstImported.Mesh.VertexCount));
            True(new[] { first.VertexCount, second.VertexCount }.Contains(secondImported.Mesh.VertexCount));
            Equal(1, firstImported.Skeleton.Bones.Count);
            Equal(1, secondImported.Skeleton.Bones.Count);

            var transforms = workspace.Document.Objects.ToDictionary(item => item.ObjectId,
                item => SourceAffine.FromTrs(new Vec3(item.ObjectId == workspace.Document.Objects[0].ObjectId ? 1f : -1f, 0, 0), new Vec4(0, 0, 0, 1), new Vec3(1, 1, 1)), StringComparer.Ordinal);
            string transformedDirectory = Path.Combine(Root, "glb-skinned-multi-affine-" + Guid.NewGuid().ToString("N"));
            var transformed = GlbExportService.ExportSkinnedWithTransforms(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, transformedDirectory, transforms);
            var transformedNodes = ((JArray)JObject.Parse(ReadJsonChunk(File.ReadAllBytes(transformed.Path)))["nodes"]!).OfType<JObject>().Where(node => node["mesh"] != null).ToArray();
            Equal(2, transformedNodes.Length);
            True(transformedNodes.All(node => node["matrix"] is JArray && ((JArray)node["matrix"]!).Count == 16));

            var foreign = new SkeletonDefinition(new[] { new BoneDefinition(GraphId(), "ForeignRoot", "", new Vec3(), new Vec3(0, .1f, 0)) });
            var mixedWorkspace = AuthoringWorkspace.CreateEmpty();
            Ok(Execute(mixedWorkspace, AuthoringOperation.AddGraph(BuildGraph(first, skeleton))));
            Ok(Execute(mixedWorkspace, AuthoringOperation.AddGraph(BuildGraph(second, foreign))));
            string rejectedDirectory = Path.Combine(Root, "glb-skinned-mixed-" + Guid.NewGuid().ToString("N"));
            Expect("GLB_SKIN_SHARED_SKELETON", () => GlbExportService.ExportSkinned(mixedWorkspace, mixedWorkspace.InstanceId, mixedWorkspace.Document.DocumentId, mixedWorkspace.Document.DocumentRevision, rejectedDirectory));
            True(!Directory.Exists(rejectedDirectory));
        });

        Test("skinned GLB export keeps same-source skins with distinct rest definitions separate", () =>
        {
            var first = AuthoringFixtures.Panel(1);
            var second = PrimitiveGeometry.Plane(.2f, .1f);
            string sharedBone = GraphId();
            var firstSkeleton = new SkeletonDefinition(new[] { new BoneDefinition(sharedBone, "Root", "", new Vec3(), new Vec3(0, .1f, 0)) });
            var secondSkeleton = new SkeletonDefinition(new[] { new BoneDefinition(sharedBone, "Root", "", new Vec3(.01f, 0, 0), new Vec3(.01f, .2f, 0)) });
            AuthoringGraph BuildGraph(MeshData mesh, SkeletonDefinition rig)
            {
                string rootId = rig.Bones[0].BoneId;
                string sourceId = GraphId(), skeletonId = GraphId(), bindId = GraphId(), poseId = GraphId(), deformId = GraphId(), outputId = GraphId();
                var binding = SkinBinding.Create(mesh, rig, Enumerable.Range(0, mesh.VertexCount).Select(i => new SkinBinding.VertexWeightInput(i, rootId, 1f)));
                var pose = PoseSet.Create(rig, new[] { new BonePose(rootId, PoseTransform.FromTranslation(rig.Bones[0].Head)) });
                return new AuthoringGraph(GraphId(),
                    new[] { GraphNode.Source(sourceId, mesh, new RestTransform(1, new Vec3())), GraphNode.SkeletonNode(skeletonId, rig), GraphNode.SkinBindNode(bindId, binding), GraphNode.PoseNode(poseId, pose), GraphNode.SkinDeformNode(deformId), GraphNode.Output(outputId) },
                    new[] { new GraphEdge(sourceId, "mesh", bindId, "mesh"), new GraphEdge(skeletonId, "skeleton", bindId, "skeleton"), new GraphEdge(skeletonId, "skeleton", poseId, "skeleton"), new GraphEdge(sourceId, "mesh", deformId, "mesh"), new GraphEdge(skeletonId, "skeleton", deformId, "skeleton"), new GraphEdge(bindId, "binding", deformId, "binding"), new GraphEdge(poseId, "pose", deformId, "pose"), new GraphEdge(deformId, "mesh", outputId, "mesh") }, outputId);
            }
            var workspace = AuthoringWorkspace.CreateEmpty();
            Ok(Execute(workspace, AuthoringOperation.AddGraph(BuildGraph(first, firstSkeleton))));
            Ok(Execute(workspace, AuthoringOperation.AddGraph(BuildGraph(second, secondSkeleton))));
            string directory = Path.Combine(Root, "glb-skinned-source-skins-" + Guid.NewGuid().ToString("N"));
            var result = GlbExportService.ExportSkinnedExtended(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory);
            var json = JObject.Parse(ReadJsonChunk(File.ReadAllBytes(result.Path)));
            Equal(2, ((JArray)json["skins"]!).Count);
            var meshNodes = ((JArray)json["nodes"]!).OfType<JObject>().Where(node => node["mesh"] != null).ToArray();
            Equal(2, meshNodes.Length);
            True((int)meshNodes[0]["skin"]! != (int)meshNodes[1]["skin"]!);
            Equal(1, GlbSkinImporter.Read(File.ReadAllBytes(result.Path), 0, 0).Skeleton.Bones.Count);
            Equal(1, GlbSkinImporter.Read(File.ReadAllBytes(result.Path), 1, 1).Skeleton.Bones.Count);
        });

        Test("skinned GLB export connects multiple root bones through a common skeleton root", () =>
        {
            var mesh = AuthoringFixtures.Panel(1);
            string rootA = GraphId(), rootB = GraphId();
            var skeleton = new SkeletonDefinition(new[]
            {
                new BoneDefinition(rootA, "RootA", "", new Vec3(0, 0, 0), new Vec3(0, .1f, 0)),
                new BoneDefinition(rootB, "RootB", "", new Vec3(.2f, 0, 0), new Vec3(.2f, .1f, 0))
            });
            var binding = SkinBinding.Create(mesh, skeleton, Enumerable.Range(0, mesh.VertexCount)
                .Select(i => new SkinBinding.VertexWeightInput(i, rootA, 1f)));
            var pose = PoseSet.Create(skeleton, skeleton.Bones.Select(bone => new BonePose(bone.BoneId, PoseTransform.FromTranslation(bone.Head))));
            var skinned = new GlbExportService.SkinnedObject
            {
                Mesh = new GlbExportService.MeshObject { Mesh = mesh, Name = "multi-root" },
                Skeleton = skeleton, Binding = binding, Pose = pose
            };
            var bytes = GlbWriter.Build(new[] { skinned.Mesh }, skinned, GlbExportProfile.SkinnedGeometry);
            var json = JObject.Parse(ReadJsonChunk(bytes));
            var skin = (JObject)((JArray)json["skins"]!)[0];
            int skeletonNode = (int)skin["skeleton"]!;
            var rootNode = (JObject)((JArray)json["nodes"]!)[skeletonNode];
            var children = (JArray)rootNode["children"]!;
            Equal(2, children.Count);
            var jointNodes = ((JArray)skin["joints"]!).Select(token => (int)token).ToHashSet();
            True(children.All(child => jointNodes.Contains((int)child!)));
            Equal("NyaForgeSkeletonRoot", (string)rootNode["name"]!);
            var imported = GlbSkinImporter.Read(bytes);
            Equal(2, imported.Skeleton.Bones.Count);
            Equal(2, GlbSourceSkinImporter.Read(bytes).Skin.InverseBindMatrices.Count);
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

        Test("GLB morph POSITION accessors include required bounds", () =>
        {
            var mesh = AuthoringFixtures.Panel(1); string id = GraphId();
            var morph = MorphTarget.Create(mesh, id, "Bounds", new[] { new MorphDelta(0, new Vec3(.25f, -.5f, .75f)) });
            var bytes = GlbWriter.Build(new[] { new GlbExportService.MeshObject { Mesh = mesh, Morphs = MorphSet.Create(mesh, new[] { morph }), Name = "morph-bounds" } }, null, GlbExportProfile.StaticGeometry);
            var json = JObject.Parse(ReadJsonChunk(bytes));
            var primitive = (JObject)((JArray)((JObject)((JArray)json["meshes"]!)[0])!["primitives"]!)[0];
            var target = (JObject)((JArray)primitive["targets"]!)[0];
            int accessorIndex = (int)target["POSITION"]!;
            var accessor = (JObject)((JArray)json["accessors"]!)[accessorIndex];
            True(accessor["min"] is JArray && accessor["max"] is JArray);
            Equal(3, ((JArray)accessor["min"]!).Count); Equal(3, ((JArray)accessor["max"]!).Count);
            Near(-.5f, (float)((JArray)accessor["min"]!)[1]!); Near(.75f, (float)((JArray)accessor["max"]!)[2]!);
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

        Test("VRM 1 package adds explicit humanoid metadata without changing GLB geometry", () =>
        {
            var mesh = AuthoringFixtures.Panel(1); var morph = MorphTarget.Create(mesh, GraphId(), "Happy", new[] { new MorphDelta(0, new Vec3(.01f, 0, 0)) });
            var glb = GlbWriter.Build(new[] { new GlbExportService.MeshObject { Mesh = mesh, Morphs = MorphSet.Create(mesh, new[] { morph }), Name = "avatar" } }, null, GlbExportProfile.StaticGeometry);
            var humanoid = VrmExportMetadata.RequiredHumanBones.ToDictionary(name => name, _ => 0, StringComparer.Ordinal);
            var metadata = new VrmExportMetadata("Nya test avatar", new[] { "Moe, Charm" }, "https://example.com/license", humanoid, expressions: new[] { new VrmExpressionExport("happy", "happy", false, new[] { new VrmMorphBind(0, 0, .75f) }) });
            var vrm = VrmExportService.Package(glb, metadata);
            var root = JObject.Parse(ReadJsonChunk(vrm));
            Equal("1.0", (string)root["extensions"]!["VRMC_vrm"]!["specVersion"]!);
            Equal("Nya test avatar", (string)root["extensions"]!["VRMC_vrm"]!["meta"]!["name"]!);
            True(((JArray)root["extensionsUsed"]!).Values<string>().Contains("VRMC_vrm"));
            var imported = GlbImporter.Read(vrm);
            Equal(mesh.VertexCount, imported.Mesh.VertexCount);
            Equal(mesh.TriangleCount, imported.Mesh.TriangleCount);
            var profile = VrmMetadataReader.Read(vrm);
            Equal("vrm1", profile.Format);
            Equal("Nya test avatar", profile.Title);
            Equal(15, profile.HumanoidNodes.Count);
            Equal(1, profile.Expressions.Count); Equal("happy", profile.Expressions[0].Name); Equal(1, profile.Expressions[0].MorphTargetBindCount); Near(.75f, profile.Expressions[0].MorphBindings[0].Weight);
        });

        Test("VRM 1 package emits VRMC_springBone 1.0 inventory", () =>
        {
            var mesh = AuthoringFixtures.Panel(1);
            var glb = GlbWriter.Build(new[] { new GlbExportService.MeshObject { Mesh = mesh, Name = "avatar" } }, null, GlbExportProfile.StaticGeometry);
            var humanoid = VrmExportMetadata.RequiredHumanBones.ToDictionary(name => name, _ => 0, StringComparer.Ordinal);
            var spring = new VrmSpringExport(
                new[] { new VrmSpringColliderExport(0, "capsule", new Vec3(0, .1f, 0), .02f, new Vec3(0, .2f, 0)) },
                new[] { new VrmSpringColliderGroupExport("body", new[] { 0 }) },
                new[] { new VrmSpringExport.SpringExportGroup("tail", new[] { new VrmSpringJointExport(0, .01f, 1.5f, .2f, new Vec3(0, -1, 0), .4f) }, new[] { 0 }, null) });
            var metadata = new VrmExportMetadata("Nya spring avatar", new[] { "NyaForge" }, "https://example.com/license", humanoid, springs: spring);
            var vrm = VrmExportService.Package(glb, metadata);
            var root = JObject.Parse(ReadJsonChunk(vrm));
            var extension = (JObject)root["extensions"]!["VRMC_springBone"]!;
            Equal("1.0", (string)extension["specVersion"]!); Equal(1, ((JArray)extension["colliders"]!).Count); Equal(1, ((JArray)extension["springs"]!).Count);
            True(((JArray)root["extensionsUsed"]!).Values<string>().Contains("VRMC_springBone"));
            var profile = VrmMetadataReader.Read(vrm);
            Equal(1, profile.SpringBones.Count); Equal(1, profile.SpringColliderGroups.Count); Equal("capsule", profile.SpringColliderGroups[0].Shapes[0].Kind);
        });

        Test("VRM 1 export writes a revision-pinned package directory", () =>
        {
            var mesh = AuthoringFixtures.Panel(1); string boneId = GraphId();
            var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(boneId, "Root", "", new Vec3(), new Vec3(0, .1f, 0)) });
            string sourceId = GraphId(), skeletonId = GraphId(), bindId = GraphId(), poseId = GraphId(), deformId = GraphId(), outputId = GraphId();
            var binding = SkinBinding.Create(mesh, skeleton, Enumerable.Range(0, mesh.VertexCount).Select(i => new SkinBinding.VertexWeightInput(i, boneId, 1f)));
            var pose = PoseSet.Create(skeleton, new[] { new BonePose(boneId, PoseTransform.FromTranslation(new Vec3())) });
            var graph = new AuthoringGraph(GraphId(),
                new[] { GraphNode.Source(sourceId, mesh, new RestTransform(1, new Vec3())), GraphNode.SkeletonNode(skeletonId, skeleton), GraphNode.SkinBindNode(bindId, binding), GraphNode.PoseNode(poseId, pose), GraphNode.SkinDeformNode(deformId), GraphNode.Output(outputId) },
                new[] { new GraphEdge(sourceId, "mesh", bindId, "mesh"), new GraphEdge(skeletonId, "skeleton", bindId, "skeleton"), new GraphEdge(skeletonId, "skeleton", poseId, "skeleton"), new GraphEdge(sourceId, "mesh", deformId, "mesh"), new GraphEdge(skeletonId, "skeleton", deformId, "skeleton"), new GraphEdge(bindId, "binding", deformId, "binding"), new GraphEdge(poseId, "pose", deformId, "pose"), new GraphEdge(deformId, "mesh", outputId, "mesh") }, outputId);
            var workspace = AuthoringWorkspace.CreateEmpty(); Ok(Execute(workspace, AuthoringOperation.AddGraph(graph)));
            var mapping = VrmExportMetadata.RequiredHumanBones.ToDictionary(name => name, _ => 1, StringComparer.Ordinal);
            var metadata = new VrmExportMetadata("Exported avatar", new[] { "NyaForge" }, "https://example.com/license", mapping);
            string directory = Path.Combine(Root, "vrm-export-" + Guid.NewGuid().ToString("N"));
            var result = VrmExportService.ExportVrm1(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory, metadata);
            True(File.Exists(result.Path)); True(File.Exists(result.ReportPath)); Equal(1, result.ObjectCount);
            var report = JObject.Parse(File.ReadAllText(result.ReportPath)); Equal("Vrm1Humanoid", (string)report["profile"]!); Equal(Checks.Hash(File.ReadAllBytes(result.Path)), (string)report["vrmHash"]!);
            Equal("vrm1", VrmMetadataReader.Read(File.ReadAllBytes(result.Path)).Format);
        });

        Test("MCP GLB export request is revision pinned and profile strict", () =>
        {
            string instance = Guid.NewGuid().ToString("D"), document = Guid.NewGuid().ToString("D"), export = Guid.NewGuid().ToString("D");
            var payload = new JObject { ["version"] = 1, ["requestId"] = Guid.NewGuid().ToString("D"), ["expectedInstanceId"] = instance, ["method"] = "export_glb",
                ["export"] = new JObject { ["documentId"] = document, ["expectedRevision"] = 7, ["directory"] = "C:/project", ["exportId"] = export, ["profile"] = "skinned_extended" } };
            var request = AuthoringIpcRequest.Parse(System.Text.Encoding.UTF8.GetBytes(payload.ToString(Newtonsoft.Json.Formatting.None)));
            Equal("export_glb", request.Method); Equal(GlbExportProfile.SkinnedGeometryExtended, request.GlbExport.Profile); Equal(7L, request.GlbExport.ExpectedRevision);
            payload["export"]!["profile"] = "unknown";
            Expect("INVALID_GLB_EXPORT_REQUEST", () => AuthoringIpcRequest.Parse(System.Text.Encoding.UTF8.GetBytes(payload.ToString(Newtonsoft.Json.Formatting.None))));
        });
    }
}
