using System.IO;
using System.Linq;
using NyaForge.Authoring;
internal static partial class Program
{
    static void RunProjectExportServiceTests()
    {
        Test("observed export routes mesh and material without changing document",()=>
        {
            var w=AuthoringWorkspace.CreateFixture();string before=w.Document.StateHash;long revision=w.Document.DocumentRevision;bool dirty=w.IsDirty;
            var result=ProjectExportService.Export(w,w.InstanceId,w.Document.DocumentId,revision,Dir("routed-mesh"));Equal(ProjectExportKind.Mesh,result.Kind);Equal(w.Evaluate().ContentHash,BakeStore.Read(result.ManifestPath).MeshContentHash);
            Equal(before,w.Document.StateHash);Equal(revision,w.Document.DocumentRevision);Equal(dirty,w.IsDirty);
            var f=MaterialFixture();var material=AuthoringWorkspace.CreateEmpty();Ok(Execute(material,AuthoringOperation.AddGraph(f.graph)));
            var exported=ProjectExportService.Export(material,material.InstanceId,material.Document.DocumentId,material.Document.DocumentRevision,Dir("routed-material"));Equal(ProjectExportKind.Material,exported.Kind);True(File.Exists(exported.ManifestPath));MaterialBakeStore.Read(exported.ManifestPath);
            string stale=Path.Combine(Root,"stale-export-no-write");
            Expect("REVISION_CONFLICT",()=>ProjectExportService.Export(w,w.InstanceId,w.Document.DocumentId,revision+1,stale));True(!Directory.Exists(stale));
        });
        Test("feature graph export preserves morph authoring as a native project package", () =>
        {
            string planeId = GraphId(), morphId = GraphId(), deformId = GraphId(), outputId = GraphId(), targetId = GraphId();
            var mesh = NyaForge.Authoring.Graph.PrimitiveGeometry.Plane(.2f, .1f);
            var morphs = NyaForge.Authoring.Rig.MorphSet.Create(mesh, new[] { NyaForge.Authoring.Rig.MorphTarget.Create(mesh, targetId, "Smile", new[] { new NyaForge.Authoring.Rig.MorphDelta(0, new Vec3(.1f, 0, 0)) }) });
            var graph = new NyaForge.Authoring.Graph.AuthoringGraph(GraphId(), new[] { NyaForge.Authoring.Graph.GraphNode.Plane(planeId), NyaForge.Authoring.Graph.GraphNode.MorphSetNode(morphId, morphs), NyaForge.Authoring.Graph.GraphNode.MorphDeformNode(deformId, new System.Collections.Generic.Dictionary<string, float> { [targetId] = .5f }), NyaForge.Authoring.Graph.GraphNode.Output(outputId) },
                new[] { new NyaForge.Authoring.Graph.GraphEdge(planeId, "mesh", deformId, "mesh"), new NyaForge.Authoring.Graph.GraphEdge(morphId, "morphs", deformId, "morphs"), new NyaForge.Authoring.Graph.GraphEdge(deformId, "mesh", outputId, "mesh") }, outputId);
            var workspace = AuthoringWorkspace.CreateEmpty(); Ok(Execute(workspace, AuthoringOperation.AddGraph(graph)));
            string before = workspace.Document.StateHash; long revision = workspace.Document.DocumentRevision; bool dirty = workspace.IsDirty;
            var result = ProjectExportService.Export(workspace, workspace.InstanceId, workspace.Document.DocumentId, revision, Path.Combine(Root, "feature-native-export-" + System.Guid.NewGuid().ToString("N")));
            Equal(ProjectExportKind.AuthoringProject, result.Kind); True(File.Exists(result.ManifestPath));
            var reopened = ProjectStore.Open(Path.GetDirectoryName(result.ManifestPath));
            True(reopened.Preview.Evaluation.MorphSetOutputs.ContainsKey(morphId));
            Equal(before, workspace.Document.StateHash); Equal(revision, workspace.Document.DocumentRevision); Equal(dirty, workspace.IsDirty);
        });
        Test("accessory attachment export rejects an unresolved target before writing", () =>
        {
            string planeId = GraphId(), attachmentId = GraphId(), outputId = GraphId();
            string targetObjectId = GraphId(), boneId = GraphId(), skeletonHash = Checks.Hash(new byte[] { 4, 2, 0 });
            var graph = new NyaForge.Authoring.Graph.AuthoringGraph(GraphId(), new[] {
                NyaForge.Authoring.Graph.GraphNode.Plane(planeId),
                NyaForge.Authoring.Graph.GraphNode.AttachmentNode(attachmentId, targetObjectId, boneId, skeletonHash, new Vec3(.01f, 0, -.02f)),
                NyaForge.Authoring.Graph.GraphNode.Output(outputId) },
                new[] { new NyaForge.Authoring.Graph.GraphEdge(planeId, "mesh", outputId, "mesh") }, outputId);
            var workspace = AuthoringWorkspace.CreateEmpty(); Ok(Execute(workspace, AuthoringOperation.AddGraph(graph)));
            string directory = Path.Combine(Root, "attachment-unresolved-export-" + System.Guid.NewGuid().ToString("N"));
            Expect("ATTACHMENT_TARGET_MISSING", () => ProjectExportService.Export(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory));
            True(!Directory.Exists(directory));
        });
        Test("multi-object attachment export keeps both graph objects in one native package", () =>
        {
            string targetPlane, targetEdit; var target = PlaneGraph(out targetPlane, out targetEdit);
            var workspace = AuthoringWorkspace.CreateEmpty(); Ok(Execute(workspace, AuthoringOperation.AddGraph(target)));
            string targetObjectId = workspace.Document.ActiveObject.ObjectId;
            string accessoryPlane, accessoryEdit; var accessory = PlaneGraph(out accessoryPlane, out accessoryEdit);
            Ok(Execute(workspace, AuthoringOperation.AddGraph(accessory)));
            string attachmentId = GraphId(), boneId = GraphId(), skeletonHash = Checks.Hash(new byte[] { 1, 9, 9 });
            Ok(Execute(workspace, AuthoringOperation.AddNode(NyaForge.Authoring.Graph.GraphNode.AttachmentNode(attachmentId, targetObjectId, boneId, skeletonHash, new Vec3(.02f, 0, 0))))) ;
            string directory = Path.Combine(Root, "multi-attachment-native-" + System.Guid.NewGuid().ToString("N"));
            var result = ProjectExportService.Export(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory);
            Equal(ProjectExportKind.AuthoringProject, result.Kind);
            var reopened = ProjectStore.Open(Path.GetDirectoryName(result.ManifestPath));
            Equal(2, reopened.Document.Objects.Count);
            var restored = reopened.Document.Objects.Single(item => item.ObjectId == workspace.Document.ActiveObject.ObjectId).Graph.Nodes[attachmentId];
            Equal(targetObjectId, restored.AttachmentTargetObjectId); Equal(boneId, restored.AttachmentBoneId);
            Expect("ATTACHMENT_NATIVE_EXPORT_REQUIRED", () => MultiObjectExportService.Export(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, Path.Combine(Root, "multi-attachment-bake-" + System.Guid.NewGuid().ToString("N"))));
        });
    }
}
