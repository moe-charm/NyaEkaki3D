using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Geometry;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;
using NyaForge.Authoring.Topology;
using NyaForge.Authoring.Paint;

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

        Test("clothing surface fit is bounded and preserves vertex count", () =>
        {
            var avatar = PrimitiveGeometry.Plane(.2f, .2f);
            var clothing = PrimitiveGeometry.Plane(.1f, .1f);
            var measured = MeshSurfaceFit.Project(clothing, new RestTransform(1, new Vec3(0, 0, .02f)),
                avatar, new RestTransform(1, new Vec3()), .005f, .1f);
            var fitted = measured.Positions;
            Equal(clothing.VertexCount, fitted.Length);
            True(fitted.All(position => Math.Abs(position.Z + .025f) < 1e-5f));
            Equal(clothing.VertexCount, measured.MovedVertexCount);
            Near(.02f, measured.MaxProjectionDistance);
            Near(.025f, measured.MaxDisplacement);
            Near(measured.MaxProjectionDistance, measured.AverageProjectionDistance);
            Near(measured.MaxDisplacement, measured.AverageDisplacement);
            Equal(clothing.VertexCount, measured.EvaluatedVertexCount);
            var legacy = MeshSurfaceFit.ProjectPositions(clothing, new RestTransform(1, new Vec3(0, 0, .02f)),
                avatar, new RestTransform(1, new Vec3()), .005f, .1f);
            True(fitted.SequenceEqual(legacy));
            Expect("SURFACE_FIT_DISTANCE", () => MeshSurfaceFit.ProjectPositions(clothing,
                new RestTransform(1, new Vec3(0, 0, 1f)), avatar, new RestTransform(1, new Vec3()), 0f, .1f));
            Expect("INVALID_SURFACE_FIT", () => MeshSurfaceFit.ProjectPositions(clothing, new RestTransform(1, new Vec3()),
                avatar, new RestTransform(1, new Vec3()), .2f, .1f));
        });

        Test("surface clearance reports back-side candidates without claiming collision proof", () =>
        {
            var avatar = new MeshData(
                new[] { new Vec3(-1, -1, 0), new Vec3(1, -1, 0), new Vec3(0, 1, 0) },
                new[] { new Vec3(0, 0, 1), new Vec3(0, 0, 1), new Vec3(0, 0, 1) },
                new[] { new Vec4(1, 0, 0, 1), new Vec4(1, 0, 0, 1), new Vec4(1, 0, 0, 1) },
                new[] { new Vec2(0, 0), new Vec2(1, 0), new Vec2(.5f, 1) },
                new[] { new[] { 0, 1, 2 } });
            var clothing = new MeshData(
                new[] { new Vec3(-.1f, -.1f, -.01f), new Vec3(.1f, -.1f, -.01f), new Vec3(0, .1f, -.01f) },
                new[] { new Vec3(0, 0, -1), new Vec3(0, 0, -1), new Vec3(0, 0, -1) },
                new[] { new Vec4(1, 0, 0, -1), new Vec4(1, 0, 0, -1), new Vec4(1, 0, 0, -1) },
                new[] { new Vec2(0, 0), new Vec2(1, 0), new Vec2(.5f, 1) },
                new[] { new[] { 0, 1, 2 } });
            var behind = MeshSurfaceClearance.Inspect(clothing, new RestTransform(1, new Vec3()), avatar,
                new RestTransform(1, new Vec3()), toleranceMetres: .0001f);
            Equal(3, behind.EvaluatedVertexCount);
            Equal(3, behind.BehindSurfaceVertexCount);
            Equal(3, behind.BehindSurfaceVertexIndices.Count);
            Near(-.01f, behind.MinimumSignedDistance);
            Near(-.01f, behind.MaximumSignedDistance);

            var clear = MeshSurfaceClearance.Inspect(clothing, new RestTransform(1, new Vec3(0, 0, .02f)), avatar,
                new RestTransform(1, new Vec3()), new[] { 0, 2 });
            Equal(2, clear.EvaluatedVertexCount);
            Equal(0, clear.BehindSurfaceVertexCount);
            True(clear.MinimumSignedDistance > .009f && clear.MaximumSignedDistance > .009f);
            Expect("SELECTION_EMPTY", () => MeshSurfaceClearance.Inspect(clothing, new RestTransform(1, new Vec3()), avatar,
                new RestTransform(1, new Vec3()), Array.Empty<int>()));
        });

        Test("clothing fit and surface weights respect an explicit avatar triangle region", () =>
        {
            var avatar = new MeshData(
                new[] {
                    new Vec3(0, 0, 0), new Vec3(1, 0, 0), new Vec3(0, 1, 0),
                    new Vec3(2, 0, 0), new Vec3(3, 0, 0), new Vec3(2, 1, 0)
                },
                Enumerable.Repeat(new Vec3(0, 0, 1), 6).ToArray(),
                Enumerable.Repeat(new Vec4(1, 0, 0, 1), 6).ToArray(),
                Enumerable.Repeat(new Vec2(0, 0), 6).ToArray(),
                new[] { new[] { 0, 1, 2, 3, 4, 5 } });
            var clothing = new MeshData(
                new[] { new Vec3(2.2f, .2f, .1f), new Vec3(2.4f, .2f, .1f), new Vec3(2.2f, .4f, .1f) },
                Enumerable.Repeat(new Vec3(0, 0, 1), 3).ToArray(),
                Enumerable.Repeat(new Vec4(1, 0, 0, 1), 3).ToArray(),
                new[] { new Vec2(0, 0), new Vec2(1, 0), new Vec2(0, 1) },
                new[] { new[] { 0, 1, 2 } });
            var fitted = MeshSurfaceFit.Project(clothing, new RestTransform(1, new Vec3()),
                avatar, new RestTransform(1, new Vec3()), .02f, .5f, new[] { 1 });
            True(fitted.Positions.All(position => Math.Abs(position.Z - .02f) < 1e-5f));
            Expect("SURFACE_FIT_DISTANCE", () => MeshSurfaceFit.Project(clothing,
                new RestTransform(1, new Vec3()), avatar, new RestTransform(1, new Vec3()),
                .02f, .5f, new[] { 0 }));

            string root = GraphId(), child = GraphId();
            var skeleton = new SkeletonDefinition(new[] {
                new BoneDefinition(root, "Root", "", new Vec3(), new Vec3(0, .1f, 0)),
                new BoneDefinition(child, "Child", root, new Vec3(2, 0, 0), new Vec3(2, .1f, 0))
            });
            var raw = new[] {
                new SkinBinding.VertexWeightInput(0, root, 1),
                new SkinBinding.VertexWeightInput(1, root, 1),
                new SkinBinding.VertexWeightInput(2, root, 1),
                new SkinBinding.VertexWeightInput(3, child, 1),
                new SkinBinding.VertexWeightInput(4, child, 1),
                new SkinBinding.VertexWeightInput(5, child, 1)
            };
            var avatarBinding = SkinBinding.Create(avatar, skeleton, raw);
            var transferred = SkinWeightTransfer.BySurfaceProjection(clothing,
                new RestTransform(1, new Vec3()), avatar, new RestTransform(1, new Vec3()),
                avatarBinding, skeleton, 2, .5f, new[] { 1 });
            True(transferred.Weights.Values.All(values => values.All(value => value.BoneId == child)));
            Expect("WEIGHT_TRANSFER_DISTANCE", () => SkinWeightTransfer.BySurfaceProjection(clothing,
                new RestTransform(1, new Vec3()), avatar, new RestTransform(1, new Vec3()),
                avatarBinding, skeleton, 2, .5f, new[] { 0 }));

            var existingClothing = SkinBinding.Create(clothing, skeleton, new[] {
                new SkinBinding.VertexWeightInput(0, root, 1),
                new SkinBinding.VertexWeightInput(1, root, 1),
                new SkinBinding.VertexWeightInput(2, root, 1)
            });
            var partialFit = MeshSurfaceFit.Project(clothing, new RestTransform(1, new Vec3()),
                avatar, new RestTransform(1, new Vec3()), .02f, .5f, new[] { 0, 1 }, new[] { 1 });
            if (!(!partialFit.Positions[0].Equals(clothing.Positions[0]) && !partialFit.Positions[1].Equals(clothing.Positions[1]) &&
                partialFit.Positions[2].Equals(clothing.Positions[2]))) throw new Exception("Surface fit did not preserve unselected clothing vertices");
            var partialWeights = SkinWeightTransfer.BySurfaceProjection(clothing,
                new RestTransform(1, new Vec3()), avatar, new RestTransform(1, new Vec3()),
                avatarBinding, skeleton, 2, .5f, new[] { 1 }, new[] { 0, 1 }, existingClothing);
            if (!(partialWeights.Weights[0].All(value => value.BoneId == child) &&
                partialWeights.Weights[1].All(value => value.BoneId == child) &&
                partialWeights.Weights[2].Count == 1 && partialWeights.Weights[2][0].BoneId == root &&
                Math.Abs(partialWeights.Weights[2][0].Weight - 1f) < 1e-6f))
                throw new Exception("Surface weight transfer did not preserve an unselected clothing vertex");

            var variedClothing = new MeshData(
                new[] { new Vec3(.02f, .02f, .02f), new Vec3(.04f, .02f, .04f), new Vec3(.02f, .04f, .08f) },
                Enumerable.Repeat(new Vec3(0, 0, 1), 3).ToArray(),
                Enumerable.Repeat(new Vec4(1, 0, 0, 1), 3).ToArray(),
                new[] { new Vec2(0, 0), new Vec2(1, 0), new Vec2(0, 1) },
                new[] { new[] { 0, 1, 2 } });
            var selectedFit = MeshSurfaceFit.Project(variedClothing, new RestTransform(1, new Vec3()),
                avatar, new RestTransform(1, new Vec3()), 0f, .5f, new[] { 0, 2 }, null);
            Equal(2, selectedFit.EvaluatedVertexCount);
            Near(.05f, selectedFit.AverageProjectionDistance);
            Near(.05f, selectedFit.AverageDisplacement);
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

        Test("polygon materialization preserves source and appearance before skin binding", () =>
        {
            string source = GraphId(), edit = GraphId(), paint = GraphId(), material = GraphId(), assignment = GraphId(), output = GraphId();
            var polygon = PolygonPrimitives.Plane(GraphId(), .2f, .2f);
            var graph = new AuthoringGraph(GraphId(),
                new[] {
                    GraphNode.Polygon(source, polygon, new RestTransform(1, new Vec3())),
                    GraphNode.PolygonEdit(edit),
                    GraphNode.Paint(paint, 4, 4),
                    GraphNode.StandardMaterial(material),
                    GraphNode.AssignMaterial(assignment),
                    GraphNode.Output(output)
                },
                new[] {
                    new GraphEdge(source, "mesh", edit, "mesh"),
                    new GraphEdge(edit, "mesh", paint, "mesh"),
                    new GraphEdge(paint, "image", material, "baseColor"),
                    new GraphEdge(edit, "mesh", assignment, "mesh"),
                    new GraphEdge(material, "material", assignment, "material"),
                    new GraphEdge(assignment, "mesh", output, "mesh")
                }, output);
            var before = GraphEvaluator.Evaluate(graph);
            True(before.IsComplete && before.Output.Mesh != null && before.Output.BaseColor != null);
            string sourceHash = GraphContentIdentity.Hash(graph);
            string root = GraphId();
            var skeleton = new SkeletonDefinition(new[] {
                new BoneDefinition(root, "Root", "", new Vec3(), new Vec3(0, .1f, 0))
            });
            string derivedId = GraphId();
            var result = AccessorySkinMaterializer.Materialize(graph, skeleton, root, GraphId(), derivedId);
            Equal(graph.GraphId, result.SourceGraphId);
            Equal(sourceHash, result.SourceGraphHash);
            Equal(sourceHash, GraphContentIdentity.Hash(graph));
            Equal(derivedId, result.Graph.GraphId);
            True(result.Graph.Nodes.Values.Any(node => node.TypeId == BuiltinNodes.MeshSource));
            True(result.Graph.Nodes.Values.Any(node => node.TypeId == BuiltinNodes.EditMesh));
            True(result.Graph.Nodes.Values.Any(node => node.TypeId == BuiltinNodes.SkinBind));
            True(result.Graph.Nodes.Values.Any(node => node.TypeId == BuiltinNodes.PoseSource));
            var marker = result.Graph.Nodes.Values.Single(node => node.TypeId == BuiltinNodes.DerivedSource);
            Equal(graph.GraphId, marker.DerivedFromGraphId);
            Equal(sourceHash, marker.DerivedFromGraphHash);
            var after = GraphEvaluator.Evaluate(result.Graph);
            True(after.IsComplete && after.Output.Mesh != null && after.Output.BaseColor != null);
            Equal(before.Output.Mesh.ContentHash, after.Output.Mesh.ContentHash);
            Equal(before.Output.BaseColor.ImageHash, after.Output.BaseColor.ImageHash);
            var deform = result.Graph.Nodes.Values.Single(node => node.TypeId == BuiltinNodes.SkinDeform);
            True(result.Graph.Edges.Count(edge => edge.FromNode == deform.NodeId && edge.FromPort == "mesh") >= 1);
            var workspace = AuthoringWorkspace.CreateEmpty();
            Ok(Execute(workspace, AuthoringOperation.AddGraph(graph, GraphId())));
            Ok(Execute(workspace, AuthoringOperation.AddGraph(result.Graph, GraphId())));
            string directory = Dir("polygon-materialized-native-" + Guid.NewGuid().ToString("N"));
            ProjectStore.Save(directory, workspace, 0);
            var reopened = ProjectStore.Open(directory);
            Equal(2, reopened.Document.Objects.Count);
            True(reopened.Document.Objects.Any(item => item.Graph.GraphId == graph.GraphId));
            True(reopened.Document.Objects.Any(item => item.Graph.GraphId == result.Graph.GraphId &&
                item.Graph.Nodes.Values.Any(node => node.TypeId == BuiltinNodes.SkinBind) &&
                item.Graph.Nodes.Values.Any(node => node.TypeId == BuiltinNodes.DerivedSource &&
                    node.DerivedFromGraphId == graph.GraphId && node.DerivedFromGraphHash == sourceHash)));
        });

        Test("polygon materialization keeps sparse material slots through skinned GLB export", () =>
        {
            string source = GraphId(), edit = GraphId(), red = GraphId(), blue = GraphId(), assignment = GraphId(), output = GraphId();
            var basePolygon = PolygonPrimitives.Plane(GraphId(), .2f, .2f);
            var baseFace = basePolygon.Faces[0];
            var duplicateVertices = basePolygon.Vertices.Values.Select(vertex =>
                new CageVertex(vertex.Id + 10, new Vec3(vertex.Position.X + .3f, vertex.Position.Y, vertex.Position.Z)));
            var duplicateCorners = baseFace.Corners.Select(corner =>
                new CageCorner(corner.Id + 10, corner.VertexId + 10, corner.Uv0, corner.Normal, corner.Tangent));
            var polygon = new PolygonMesh(basePolygon.DomainId,
                basePolygon.Vertices.Values.Concat(duplicateVertices),
                new[] { new CageFace(baseFace.Id, 3, baseFace.Corners), new CageFace(baseFace.Id + 10, 9, duplicateCorners) });
            var graph = new AuthoringGraph(GraphId(),
                new[] {
                    GraphNode.Polygon(source, polygon, new RestTransform(1, new Vec3())),
                    GraphNode.PolygonEdit(edit),
                    GraphNode.StandardMaterial(red, new MaterialParameters(new Vec4(1, 0, 0, 1), 0, 1, new Vec3())),
                    GraphNode.StandardMaterial(blue, new MaterialParameters(new Vec4(0, 0, 1, 1), 0, 1, new Vec3())),
                    GraphNode.AssignMaterials(assignment, new[] { 3, 9 }),
                    GraphNode.Output(output)
                },
                new[] {
                    new GraphEdge(source, "mesh", edit, "mesh"),
                    new GraphEdge(edit, "mesh", assignment, "mesh"),
                    new GraphEdge(red, "material", assignment, GraphNode.MaterialSlotPort(3)),
                    new GraphEdge(blue, "material", assignment, GraphNode.MaterialSlotPort(9)),
                    new GraphEdge(assignment, "mesh", output, "mesh")
                }, output);
            var before = GraphEvaluator.Evaluate(graph);
            True(before.IsComplete && before.Output.SlotMaterials.Count == 2);
            var skeleton = new SkeletonDefinition(new[] {
                new BoneDefinition(GraphId(), "Root", "", new Vec3(), new Vec3(0, .1f, 0))
            });
            var materialized = AccessorySkinMaterializer.Materialize(graph, skeleton, skeleton.Bones[0].BoneId, derivedGraphId: GraphId());
            var after = GraphEvaluator.Evaluate(materialized.Graph);
            True(after.IsComplete && after.Output.SlotMaterials.Count == 2);
            Equal(2, after.Output.SlotMaterials.Count);
            True(after.Output.SlotMaterials.ContainsKey(3) && after.Output.SlotMaterials.ContainsKey(9));
            var workspace = AuthoringWorkspace.CreateEmpty();
            Ok(Execute(workspace, AuthoringOperation.AddGraph(materialized.Graph)));
            Equal(2, workspace.Preview.Output.SlotMaterials.Count);
            Equal(2, workspace.Preview.Output.Mesh.Submeshes.Count);
            string directory = System.IO.Path.Combine(Root, "polygon-materialized-sparse-glb-" + Guid.NewGuid().ToString("N"));
            var exported = GlbExportService.ExportSkinned(workspace, workspace.InstanceId, workspace.Document.DocumentId,
                workspace.Document.DocumentRevision, directory);
            var imported = GlbSkinImporter.Read(System.IO.File.ReadAllBytes(exported.Path));
            Equal(2, imported.Materials.Count);
            Equal(2, imported.Mesh.Submeshes.Count);
            True(imported.Materials.Any(material => material.Parameters.BaseColor.X > .9f && material.Parameters.BaseColor.Z < .1f));
            True(imported.Materials.Any(material => material.Parameters.BaseColor.Z > .9f && material.Parameters.BaseColor.X < .1f));
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

        Test("polygon materialization bakes rigid attachment placement into the skin derivative", () =>
        {
            string source = GraphId(), edit = GraphId(), output = GraphId(), target = GraphId();
            var polygon = PolygonPrimitives.Plane(GraphId(), .2f, .2f);
            var graph = new AuthoringGraph(GraphId(), new[] {
                GraphNode.Polygon(source, polygon, new RestTransform(1, new Vec3())),
                GraphNode.PolygonEdit(edit), GraphNode.Output(output),
                GraphNode.AttachmentNode(GraphId(), target, GraphId(), Checks.Hash(new byte[] { 2 }), new Vec3(.01f, .02f, .03f))
            }, new[] { new GraphEdge(source, "mesh", edit, "mesh"), new GraphEdge(edit, "mesh", output, "mesh") }, output);
            var before = GraphEvaluator.Evaluate(graph);
            var root = GraphId();
            var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(root, "Root", "", new Vec3(.2f, .3f, .4f), new Vec3(.2f, .4f, .4f)) });
            var attachmentFrame = PoseTransform.FromTranslation(new Vec3(.21f, .32f, .43f));
            var materialized = AccessorySkinMaterializer.Materialize(graph, skeleton, root, poseSourceObjectId: target,
                derivedGraphId: GraphId(), bakeAttachmentTransform: attachmentFrame);
            var after = GraphEvaluator.Evaluate(materialized.Graph);
            True(before.IsComplete && after.IsComplete && after.Output.Mesh != null);
            True(!materialized.Graph.Nodes.Values.Any(node => node.TypeId == BuiltinNodes.Attachment));
            Equal(before.Output.Mesh.TopologyHash, after.Output.Mesh.TopologyHash);
            Near(before.Output.Mesh.Positions[0].X + .21f, after.Output.Mesh.Positions[0].X);
            Near(before.Output.Mesh.Positions[0].Y + .32f, after.Output.Mesh.Positions[0].Y);
            Near(before.Output.Mesh.Positions[0].Z + .43f, after.Output.Mesh.Positions[0].Z);
            True(GraphEvaluator.Evaluate(graph).Output.Mesh.ContentHash == before.Output.Mesh.ContentHash);
        });
    }
}
