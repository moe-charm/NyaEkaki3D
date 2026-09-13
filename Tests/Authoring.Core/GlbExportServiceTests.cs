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
            Equal("passed", (string)report["validation"]!["glbResourceReaders"]!);
            Equal(workspace.Document.ActiveObject.Graph.GraphId, (string)report["objects"]![0]!["graphId"]!);
            var imported = GlbImporter.Read(File.ReadAllBytes(result.Path));
            Equal(workspace.Evaluate().VertexCount, imported.Mesh.VertexCount);
            Equal(workspace.Evaluate().TriangleCount, imported.Mesh.TriangleCount);
            Equal(workspace.Evaluate().Positions[0].X, imported.Mesh.Positions[0].X);
        });

        Test("skinned clothing package exports one object with stable rig sidecars", () =>
        {
            var mesh = PrimitiveGeometry.Plane(.2f, .1f);
            string rootId = GraphId(), sourceId = GraphId(), skeletonId = GraphId(), bindId = GraphId(), poseId = GraphId(), deformId = GraphId(), paintId = GraphId(), materialId = GraphId(), assignId = GraphId(), outputId = GraphId();
            var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(rootId, "Root", "", new Vec3(), new Vec3(0, .1f, 0)) });
            var binding = SkinBinding.Create(mesh, skeleton, Enumerable.Range(0, mesh.VertexCount).Select(i => new SkinBinding.VertexWeightInput(i, rootId, 1f)));
            var pose = PoseSet.Create(skeleton, new[] { new BonePose(rootId, PoseTransform.FromTranslation(new Vec3())) });
            var graph = new AuthoringGraph(GraphId(),
                new[] { GraphNode.Source(sourceId, mesh, new RestTransform(1, new Vec3())), GraphNode.SkeletonNode(skeletonId, skeleton), GraphNode.SkinBindNode(bindId, binding), GraphNode.PoseNode(poseId, pose), GraphNode.SkinDeformNode(deformId), GraphNode.Paint(paintId, 2, 1, new PaintImage(2, 1, new Rgba32(255, 0, 128, 255))), GraphNode.StandardMaterial(materialId), GraphNode.AssignMaterial(assignId), GraphNode.Output(outputId) },
                new[] { new GraphEdge(sourceId, "mesh", bindId, "mesh"), new GraphEdge(skeletonId, "skeleton", bindId, "skeleton"), new GraphEdge(skeletonId, "skeleton", poseId, "skeleton"), new GraphEdge(sourceId, "mesh", deformId, "mesh"), new GraphEdge(skeletonId, "skeleton", deformId, "skeleton"), new GraphEdge(bindId, "binding", deformId, "binding"), new GraphEdge(poseId, "pose", deformId, "pose"), new GraphEdge(deformId, "mesh", paintId, "mesh"), new GraphEdge(paintId, "image", materialId, "baseColor"), new GraphEdge(deformId, "mesh", assignId, "mesh"), new GraphEdge(materialId, "material", assignId, "material"), new GraphEdge(assignId, "mesh", outputId, "mesh") }, outputId);
            var workspace = AuthoringWorkspace.CreateEmpty(); Ok(Execute(workspace, AuthoringOperation.AddGraph(graph)));
            string glbDirectory = Path.Combine(Root, "clothing-glb-" + Guid.NewGuid().ToString("N"));
            var glb = GlbExportService.ExportSkinnedObject(workspace, workspace.InstanceId, workspace.Document.DocumentId,
                workspace.Document.DocumentRevision, workspace.Document.ActiveObjectId, glbDirectory);
            var evaluation = workspace.Document.ActiveObject.EvaluateGraph();
            string packageDirectory = Path.Combine(Root, "clothing-package-" + Guid.NewGuid().ToString("N"));
            string manifest = SkinnedClothingPackage.Export(packageDirectory, File.ReadAllBytes(glb.Path), evaluation.Output.Mesh,
                skeleton, binding, workspace.Document.DocumentId, workspace.Document.ActiveObjectId, graph.GraphId,
                workspace.Document.StateHash, graph.ContentHash);
            var package = SkinnedClothingPackage.Read(manifest);
            Equal(workspace.Document.ActiveObjectId, package.ObjectId);
            Equal(graph.GraphId, package.GraphId); Equal(graph.ContentHash, package.GraphHash);
            Equal(mesh.ContentHash, package.Mesh.ContentHash); Equal(skeleton.ContentHash, package.Skeleton.ContentHash);
            Equal(binding.ContentHash, package.Binding.ContentHash); Equal(mesh.VertexCount, package.Mesh.VertexCount);
            True(package.Glb.Length > 0 && package.Binding.Weights.Count == mesh.VertexCount);
            Equal(1, package.Materials.Count); True(package.Materials[0].HasEmbeddedBaseColorImage);
        });

        Test("GLB export report carries source import diagnostics", () =>
        {
            var workspace = AuthoringWorkspace.CreateFixture();
            string graphId = workspace.Document.ActiveObject.Graph.GraphId;
            var diagnostics = new ImportedGlbDiagnostics(graphId, Checks.Hash(new byte[] { 4, 2, 1 }), 3, null,
                new[] { new GlbImportDiagnostic("ANIMATIONS_NOT_RETAINED", "animations", false, "animation is reported only") }, 7);
            workspace.SetAttachments(new ProjectAttachments(new Dictionary<string, byte[]>
            {
                [ProjectAttachments.ImportDiagnostics] = ImportedGlbDiagnosticsCodec.Write(new[] { diagnostics })
            }));
            string directory = Path.Combine(Root, "glb-diagnostics-report-" + Guid.NewGuid().ToString("N"));
            var result = GlbExportService.ExportStatic(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory);
            var report = JObject.Parse(File.ReadAllText(result.ReportPath));
            var record = ((JArray)report["sourceDiagnostics"]!).Single() as JObject;
            Equal(graphId, (string)record!["graphId"]!);
            Equal("ANIMATIONS_NOT_RETAINED", (string)record["diagnostics"]![0]!["code"]!);
            Equal("animations", (string)record["diagnostics"]![0]!["path"]!);
        });

        Test("static GLB export accepts a display-corrected mesh override", () =>
        {
            var workspace = AuthoringWorkspace.CreateFixture();
            var evaluated = GraphEvaluator.Evaluate(workspace.Document.ActiveObject.Graph).Output;
            var positions = evaluated.Mesh.Positions.ToArray();
            positions[0] = new Vec3(0.375f, positions[0].Y, positions[0].Z);
            var corrected = evaluated.WithMesh(evaluated.Mesh.WithPositions(positions));
            string directory = Path.Combine(Root, "glb-static-override-" + Guid.NewGuid().ToString("N"));
            var result = GlbExportService.ExportStaticWithOverrides(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory,
                new Dictionary<string, GraphMeshValue>(StringComparer.Ordinal) { [workspace.Document.ActiveObject.ObjectId] = corrected });
            var imported = GlbImporter.Read(File.ReadAllBytes(result.Path));
            Equal(corrected.Mesh.VertexCount, imported.Mesh.VertexCount);
            Equal(corrected.Mesh.TriangleCount, imported.Mesh.TriangleCount);
            Equal(0.375f, imported.Mesh.Positions[0].X);
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
            var exportedObjectId = workspace.Document.ActiveObject.ObjectId;
            var exportedNodes = (JArray)JObject.Parse(ReadJsonChunk(File.ReadAllBytes(result.Path)))["nodes"]!;
            Equal(result.NodeMap.MeshNodes[exportedObjectId], exportedNodes.OfType<JObject>().Select((node, index) => new { node, index }).Single(item => item.node["mesh"] != null).index);
            Equal(result.NodeMap.BoneNodes[exportedObjectId].Count, 1);
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

        Test("skinned GLB export retains non-translation joint local transforms", () =>
        {
            // Keep the child before its parent in the authored list to exercise
            // the stable BoneId/order boundary used by the writer.
            string rootId = GraphId(), childId = GraphId();
            var skeleton = new SkeletonDefinition(new[] {
                new BoneDefinition(childId, "Child", rootId, new Vec3(0, .1f, 0), new Vec3(0, .2f, 0)),
                new BoneDefinition(rootId, "Root", "", new Vec3(), new Vec3(0, .1f, 0))
            });
            var mesh = PrimitiveGeometry.Plane(.2f, .1f);
            var binding = SkinBinding.Create(mesh, skeleton,
                Enumerable.Range(0, mesh.VertexCount).Select(i => new SkinBinding.VertexWeightInput(i, childId, 1f)));
            var pose = PoseSet.Create(skeleton, skeleton.Bones.Select(bone => new BonePose(bone.BoneId, PoseTransform.FromTranslation(bone.Head))));
            var rootLocal = SourceAffine.FromTrs(new Vec3(.25f, -.5f, .75f), new Vec4(0, 0, 0, 1), new Vec3(1, 1, 1));
            var childLocal = SourceAffine.FromTrs(new Vec3(.1f, .2f, .3f), new Vec4(0, 0, (float)Math.Sqrt(.5), (float)Math.Sqrt(.5)), new Vec3(2, 3, 4));
            var skinned = new GlbExportService.SkinnedObject {
                Mesh = new GlbExportService.MeshObject { Mesh = mesh, Name = "joint-transform" },
                Skeleton = skeleton, Binding = binding, Pose = pose,
                InverseBindMatrices = new[] { SourceAffine.Identity, SourceAffine.Identity },
                JointLocalTransforms = new[] { childLocal, rootLocal }
            };
            var bytes = GlbWriter.Build(new[] { skinned.Mesh }, skinned, GlbExportProfile.SkinnedGeometryExtended);
            var json = JObject.Parse(ReadJsonChunk(bytes));
            var nodes = ((JArray)json["nodes"]!).OfType<JObject>().ToArray();
            var childNode = nodes.Single(node => (string)node["name"] == "Child");
            var actual = ((JArray)childNode["matrix"]!).Values<double>().ToArray();
            var expected = childLocal.ToColumnMajor().ToArray();
            Equal(16, actual.Length);
            for (int i = 0; i < actual.Length; i++) Near((float)expected[i], (float)actual[i]);
            var imported = GlbSourceSkinImporter.Read(bytes, 0, 0);
            var childIndex = imported.Skin.Joints.Single(index => (string)nodes[index]["name"] == "Child");
            var world = imported.Skin.Nodes.World[childIndex].ToColumnMajor().ToArray();
            var expectedWorld = rootLocal.Compose(childLocal).ToColumnMajor().ToArray();
            for (int i = 0; i < world.Length; i++) Near((float)expectedWorld[i], (float)world[i]);
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

        Test("GLB export preserves semantic normal and metallic-roughness images", () =>
        {
            var mesh = AuthoringFixtures.Panel(1);
            var normalBytes = PaintPng.Encode(new PaintImage(2, 1, new Rgba32(128, 128, 255, 255)));
            var metallicRoughnessBytes = PaintPng.Encode(new PaintImage(2, 1, new Rgba32(16, 192, 0, 255)));
            var sampler = new MaterialTextureSampler(33071, 33648, 9984, 9728);
            var unsupportedSet = new MaterialTextureSet(
                new MaterialTextureSlot(MaterialTextureSemantic.Normal, normalBytes, "image/png", 1, .75f, sampler),
                new MaterialTextureSlot(MaterialTextureSemantic.MetallicRoughness, metallicRoughnessBytes, "image/png", 0, 1f, sampler));
            var unsupportedParameters = new MaterialParameters(new Vec4(1, 1, 1, 1), .1f, .9f, new Vec3(), MaterialAlphaMode.Opaque, .5f, unsupportedSet);
            Expect("UNSUPPORTED_UV_SET", () => GlbWriter.Build(new[] { new GlbExportService.MeshObject { Mesh = mesh, Material = new GraphMaterialValue(unsupportedParameters, null), Name = "semantic-pbr-uv1" } }, null, GlbExportProfile.StaticGeometry));
            var textureSet = new MaterialTextureSet(
                new MaterialTextureSlot(MaterialTextureSemantic.Normal, normalBytes, "image/png", 0, .75f, sampler),
                new MaterialTextureSlot(MaterialTextureSemantic.MetallicRoughness, metallicRoughnessBytes, "image/png", 0, 1f, sampler));
            var parameters = new MaterialParameters(new Vec4(1, 1, 1, 1), .1f, .9f, new Vec3(), MaterialAlphaMode.Opaque, .5f, textureSet);
            var material = new GraphMaterialValue(parameters, null);
            var bytes = GlbWriter.Build(new[] { new GlbExportService.MeshObject { Mesh = mesh, Material = material, Name = "semantic-pbr" } }, null, GlbExportProfile.StaticGeometry);
            var json = JObject.Parse(ReadJsonChunk(bytes));
            var materialJson = (JObject)((JArray)json["materials"]!)[0]!;
            True(materialJson["normalTexture"] is JObject); True(materialJson["pbrMetallicRoughness"]!["metallicRoughnessTexture"] is JObject);
            Equal(1, ((JArray)json["samplers"]!).Count); Equal(2, ((JArray)json["images"]!).Count);
            var imported = GlbImporter.Read(bytes).Materials.Single();
            True(imported.NormalTexture != null && imported.NormalTexture.HasImageBytes); True(imported.MetallicRoughnessTexture != null && imported.MetallicRoughnessTexture.HasImageBytes);
            Equal(0, imported.NormalTexture.TexCoord); Near(.75f, imported.NormalTexture.NormalScale); Equal(33071, imported.NormalTexture.Sampler.WrapS); Equal(9984, imported.NormalTexture.Sampler.MinFilter);
            True(normalBytes.SequenceEqual(imported.NormalTexture.CopyImageBytes())); True(metallicRoughnessBytes.SequenceEqual(imported.MetallicRoughnessTexture.CopyImageBytes()));
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
            Equal("other", (string)root["extensions"]!["VRMC_vrm"]!["meta"]!["licenseUrl"]!);
            Equal("https://example.com/license", (string)root["extensions"]!["VRMC_vrm"]!["meta"]!["otherLicenseUrl"]!);
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
            var sourceDiagnostic = new ImportedGlbDiagnostics(graph.GraphId, Checks.Hash(new byte[] { 9, 4 }), 1, 0,
                new[] { new GlbImportDiagnostic("EXTENSIONS_PARTIAL", "extensionsUsed", false, "extension is not emitted by this profile") });
            workspace.SetAttachments(new ProjectAttachments(new Dictionary<string, byte[]>
            {
                [ProjectAttachments.ImportDiagnostics] = ImportedGlbDiagnosticsCodec.Write(new[] { sourceDiagnostic })
            }));
            var mapping = VrmExportMetadata.RequiredHumanBones.ToDictionary(name => name, _ => 1, StringComparer.Ordinal);
            var metadata = new VrmExportMetadata("Exported avatar", new[] { "NyaForge" }, "https://example.com/license", mapping);
            string directory = Path.Combine(Root, "vrm-export-" + Guid.NewGuid().ToString("N"));
            var result = VrmExportService.ExportVrm1(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory, metadata);
            True(File.Exists(result.Path)); True(File.Exists(result.ReportPath)); Equal(1, result.ObjectCount);
            var report = JObject.Parse(File.ReadAllText(result.ReportPath)); Equal("Vrm1Humanoid", (string)report["profile"]!); Equal(Checks.Hash(File.ReadAllBytes(result.Path)), (string)report["vrmHash"]!);
            Equal(1, ((JArray)report["sourceDiagnostics"]!).Count);
            Equal("EXTENSIONS_PARTIAL", (string)report["sourceDiagnostics"]![0]!["diagnostics"]![0]!["code"]!);
            Equal("vrm1", VrmMetadataReader.Read(File.ReadAllBytes(result.Path)).Format);
        });

        Test("VRM 1 export includes skin-bound clothing objects and maps metadata to the avatar mesh", () =>
        {
            string rootId = GraphId();
            var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(rootId, "Root", "", new Vec3(), new Vec3(0, .1f, 0)) });
            AuthoringGraph BuildGraph(MeshData mesh, SkeletonDefinition selectedSkeleton = null)
            {
                var rig = selectedSkeleton ?? skeleton;
                string selectedRootId = rig.Bones[0].BoneId;
                string sourceId = GraphId(), skeletonId = GraphId(), bindId = GraphId(), poseId = GraphId(), deformId = GraphId(), outputId = GraphId();
                var binding = SkinBinding.Create(mesh, rig, Enumerable.Range(0, mesh.VertexCount).Select(i => new SkinBinding.VertexWeightInput(i, selectedRootId, 1f)));
                var pose = PoseSet.Create(rig, new[] { new BonePose(selectedRootId, PoseTransform.FromTranslation(new Vec3())) });
                return new AuthoringGraph(GraphId(),
                    new[] { GraphNode.Source(sourceId, mesh, new RestTransform(1, new Vec3())), GraphNode.SkeletonNode(skeletonId, rig), GraphNode.SkinBindNode(bindId, binding), GraphNode.PoseNode(poseId, pose), GraphNode.SkinDeformNode(deformId), GraphNode.Output(outputId) },
                    new[] { new GraphEdge(sourceId, "mesh", bindId, "mesh"), new GraphEdge(skeletonId, "skeleton", bindId, "skeleton"), new GraphEdge(skeletonId, "skeleton", poseId, "skeleton"), new GraphEdge(sourceId, "mesh", deformId, "mesh"), new GraphEdge(skeletonId, "skeleton", deformId, "skeleton"), new GraphEdge(bindId, "binding", deformId, "binding"), new GraphEdge(poseId, "pose", deformId, "pose"), new GraphEdge(deformId, "mesh", outputId, "mesh") }, outputId);
            }
            var workspace = AuthoringWorkspace.CreateEmpty();
            Ok(Execute(workspace, AuthoringOperation.AddGraph(BuildGraph(AuthoringFixtures.Panel(1)))));
            Ok(Execute(workspace, AuthoringOperation.AddGraph(BuildGraph(PrimitiveGeometry.Plane(.2f, .1f)))));
            string avatarId = workspace.Document.Objects[0].ObjectId;
            var mapping = VrmExportMetadata.RequiredHumanBones.ToDictionary(name => name, _ => 1, StringComparer.Ordinal);
            var metadata = new VrmExportMetadata("Avatar with clothing", new[] { "NyaForge" }, "https://example.com/license", mapping, usesAuthoredNodeTokens: true);
            string directory = Path.Combine(Root, "vrm-export-clothing-" + Guid.NewGuid().ToString("N"));
            var result = VrmExportService.ExportVrm1(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory, metadata, metadataObjectId: avatarId);
            Equal(2, result.ObjectCount);
            var report = JObject.Parse(File.ReadAllText(result.ReportPath));
            Equal(2, (int)report["objectCount"]!);
            var bytes = File.ReadAllBytes(result.Path);
            var root = JObject.Parse(ReadJsonChunk(bytes));
            var nodes = (JArray)root["nodes"]!;
            var meshNodes = nodes.OfType<JObject>().Where(node => node["mesh"] != null).ToArray();
            Equal(2, meshNodes.Length);
            var profile = VrmMetadataReader.Read(bytes);
            Equal(15, profile.HumanoidNodes.Count);
            int rootNode = nodes.OfType<JObject>().Select((node, index) => new { node, index })
                .Single(item => (string)item.node["name"]! == "Root").index;
            True(profile.HumanoidNodes.Values.All(index => index == rootNode));
            True(meshNodes.All(node => node["skin"] != null));
            Equal((int)meshNodes[0]["skin"]!, (int)meshNodes[1]["skin"]!);

            var mixed = AuthoringWorkspace.CreateEmpty();
            Ok(Execute(mixed, AuthoringOperation.AddGraph(BuildGraph(AuthoringFixtures.Panel(1)))));
            var foreign = new SkeletonDefinition(new[] { new BoneDefinition(GraphId(), "ForeignRoot", "", new Vec3(), new Vec3(0, .1f, 0)) });
            Ok(Execute(mixed, AuthoringOperation.AddGraph(BuildGraph(PrimitiveGeometry.Plane(.2f, .1f), foreign))));
            string rejected = Path.Combine(Root, "vrm-export-clothing-mixed-" + Guid.NewGuid().ToString("N"));
            Expect("VRM_SKELETON_MISMATCH", () => VrmExportService.ExportVrm1(mixed, mixed.InstanceId, mixed.Document.DocumentId, mixed.Document.DocumentRevision, rejected, metadata, metadataObjectId: mixed.Document.Objects[0].ObjectId));
            True(!Directory.Exists(rejected));
        });

        Test("VRM 1 export resolves authored humanoid tokens to the emitted node map", () =>
        {
            // Keep the authored order deliberately different from BoneId order
            // and use distinct bone names for every required humanoid entry.
            var names = VrmExportMetadata.RequiredHumanBones.Reverse().ToArray();
            var bones = names.Select((name, index) => new BoneDefinition(
                (index + 1).ToString("00000000-0000-0000-0000-000000000000"), name, "",
                new Vec3(index * .01f, 0, 0), new Vec3(index * .01f, .05f, 0))).ToArray();
            var skeleton = new SkeletonDefinition(bones);
            var mesh = AuthoringFixtures.Panel(1);
            var morphTarget = MorphTarget.Create(mesh, GraphId(), "Happy", new[] { new MorphDelta(0, new Vec3(.01f, 0, 0)) });
            var morphs = MorphSet.Create(mesh, new[] { morphTarget });
            string sourceId = GraphId(), skeletonId = GraphId(), bindId = GraphId(), poseId = GraphId(), deformId = GraphId(), outputId = GraphId();
            string morphId = GraphId();
            var binding = SkinBinding.Create(mesh, skeleton, Enumerable.Range(0, mesh.VertexCount)
                .Select(i => new SkinBinding.VertexWeightInput(i, bones[0].BoneId, 1f)));
            var pose = PoseSet.Create(skeleton, bones.Select(bone => new BonePose(bone.BoneId, PoseTransform.FromTranslation(bone.Head))));
            var graph = new AuthoringGraph(GraphId(),
                new[] { GraphNode.Source(sourceId, mesh, new RestTransform(1, new Vec3())), GraphNode.SkeletonNode(skeletonId, skeleton), GraphNode.SkinBindNode(bindId, binding), GraphNode.PoseNode(poseId, pose), GraphNode.SkinDeformNode(deformId), GraphNode.MorphSetNode(morphId, morphs), GraphNode.Output(outputId) },
                new[] { new GraphEdge(sourceId, "mesh", bindId, "mesh"), new GraphEdge(skeletonId, "skeleton", bindId, "skeleton"), new GraphEdge(skeletonId, "skeleton", poseId, "skeleton"), new GraphEdge(sourceId, "mesh", deformId, "mesh"), new GraphEdge(skeletonId, "skeleton", deformId, "skeleton"), new GraphEdge(bindId, "binding", deformId, "binding"), new GraphEdge(poseId, "pose", deformId, "pose"), new GraphEdge(deformId, "mesh", outputId, "mesh") }, outputId);
            var workspace = AuthoringWorkspace.CreateEmpty(); Ok(Execute(workspace, AuthoringOperation.AddGraph(graph)));
            var authoredTokens = names.Select((name, index) => new { name, index }).ToDictionary(item => item.name, item => item.index + 1, StringComparer.Ordinal);
            var spring = new VrmSpringExport(Array.Empty<VrmSpringColliderExport>(), Array.Empty<VrmSpringColliderGroupExport>(), new[]
            {
                new VrmSpringExport.SpringExportGroup("tail", new[] { new VrmSpringJointExport(1, .01f, 1.5f, .2f, new Vec3(0, -1, 0), .4f) }, Array.Empty<int>(), null)
            });
            var expressions = new[] { new VrmExpressionExport("happy", "happy", false, new[] { new VrmMorphBind(0, 0, .75f) }) };
            var metadata = new VrmExportMetadata("Node mapped avatar", new[] { "NyaForge" }, "https://example.com/license", authoredTokens, expressions: expressions, springs: spring, usesAuthoredNodeTokens: true);
            string directory = Path.Combine(Root, "vrm-node-map-" + Guid.NewGuid().ToString("N"));
            var result = VrmExportService.ExportVrm1(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory, metadata);
            var root = JObject.Parse(ReadJsonChunk(File.ReadAllBytes(result.Path)));
            var nodes = (JArray)root["nodes"]!;
            var humanBones = (JObject)root["extensions"]!["VRMC_vrm"]!["humanoid"]!["humanBones"]!;
            foreach (var name in names)
            {
                int node = (int)humanBones[name]!["node"]!;
                Equal(name, (string)((JObject)nodes[node])!["name"]!);
            }
            var profile = VrmMetadataReader.Read(File.ReadAllBytes(result.Path));
            Equal(1, profile.Expressions.Count);
            Equal("NyaForgeObject-0", (string)((JObject)nodes[profile.Expressions[0].MorphBindings[0].OwnerIndex]!)!["name"]!);
            Equal(1, profile.SpringBones.Count);
            Equal(names[0], (string)((JObject)nodes[profile.SpringBones[0].Joints[0].NodeIndex]!)!["name"]!);
        });

        Test("skinned GLB export keeps shared skin weights correct when authored bone order differs", () =>
        {
            string rootId = "00000000-0000-0000-0000-000000000001";
            string childId = "00000000-0000-0000-0000-000000000002";
            var rootBone = new BoneDefinition(rootId, "Root", "", new Vec3(), new Vec3(0, .1f, 0));
            var childBone = new BoneDefinition(childId, "Child", rootId, new Vec3(0, .1f, 0), new Vec3(0, .2f, 0));
            var firstSkeleton = new SkeletonDefinition(new[] { rootBone, childBone });
            var secondSkeleton = new SkeletonDefinition(new[] { childBone, rootBone });
            AuthoringGraph BuildGraph(MeshData mesh, SkeletonDefinition rig, string weightedBone)
            {
                string sourceId = GraphId(), skeletonId = GraphId(), bindId = GraphId(), poseId = GraphId(), deformId = GraphId(), outputId = GraphId();
                var binding = SkinBinding.Create(mesh, rig, Enumerable.Range(0, mesh.VertexCount).Select(i => new SkinBinding.VertexWeightInput(i, weightedBone, 1f)));
                var pose = PoseSet.Create(rig, rig.Bones.Select(bone => new BonePose(bone.BoneId, PoseTransform.FromTranslation(bone.Head))));
                return new AuthoringGraph(GraphId(),
                    new[] { GraphNode.Source(sourceId, mesh, new RestTransform(1, new Vec3())), GraphNode.SkeletonNode(skeletonId, rig), GraphNode.SkinBindNode(bindId, binding), GraphNode.PoseNode(poseId, pose), GraphNode.SkinDeformNode(deformId), GraphNode.Output(outputId) },
                    new[] { new GraphEdge(sourceId, "mesh", bindId, "mesh"), new GraphEdge(skeletonId, "skeleton", bindId, "skeleton"), new GraphEdge(skeletonId, "skeleton", poseId, "skeleton"), new GraphEdge(sourceId, "mesh", deformId, "mesh"), new GraphEdge(skeletonId, "skeleton", deformId, "skeleton"), new GraphEdge(bindId, "binding", deformId, "binding"), new GraphEdge(poseId, "pose", deformId, "pose"), new GraphEdge(deformId, "mesh", outputId, "mesh") }, outputId);
            }
            var workspace = AuthoringWorkspace.CreateEmpty();
            Ok(Execute(workspace, AuthoringOperation.AddGraph(BuildGraph(AuthoringFixtures.Panel(1), firstSkeleton, rootId))));
            string firstObjectId = workspace.Document.ActiveObject.ObjectId;
            Ok(Execute(workspace, AuthoringOperation.AddGraph(BuildGraph(PrimitiveGeometry.Plane(.2f, .1f), secondSkeleton, childId))));
            string secondObjectId = workspace.Document.ActiveObject.ObjectId;
            string directory = Path.Combine(Root, "glb-skinned-reordered-bones-" + Guid.NewGuid().ToString("N"));
            var result = GlbExportService.ExportSkinnedExtended(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory);
            var bytes = File.ReadAllBytes(result.Path); var json = JObject.Parse(ReadJsonChunk(bytes));
            Equal(1, ((JArray)json["skins"]!).Count);
            var nodes = (JArray)json["nodes"]!;
            int MeshIndex(string objectId) => (int)((JObject)nodes[result.NodeMap.MeshNodes[objectId]])!["mesh"]!;
            var importedFirst = GlbSkinImporter.Read(bytes, MeshIndex(firstObjectId), 0);
            var importedSecond = GlbSkinImporter.Read(bytes, MeshIndex(secondObjectId), 0);
            Equal("Root", importedFirst.Skeleton.ById[importedFirst.Binding.Weights[0].Single().BoneId].Name);
            Equal("Child", importedSecond.Skeleton.ById[importedSecond.Binding.Weights[0].Single().BoneId].Name);
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
