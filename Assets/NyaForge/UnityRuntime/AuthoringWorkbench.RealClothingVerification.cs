using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        /// <summary>
        /// Creates a real, authored choker graph against a command-line VRM.
        /// This probe deliberately stops at a deterministic package: visual fit
        /// and penetration still belong to the manual avatar acceptance.
        /// </summary>
        void VerifyCommandLineRealClothing(string path, string output, List<string> checks)
        {
            path = Path.GetFullPath(path);
            Check(File.Exists(path), "Real clothing model fixture was not found: " + path);
            string extension = Path.GetExtension(path);
            Check(string.Equals(extension, ".glb", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".vrm", StringComparison.OrdinalIgnoreCase),
                "Real clothing model fixture must be .glb or .vrm.");

            ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
            modelImportPath.SetValueWithoutNotify(path);
            modelImportMeshIndex.SetValueWithoutNotify(1);
            modelImportSkinIndex.SetValueWithoutNotify(0);
            modelImportInstanceIndex.SetValueWithoutNotify(-1);
            InspectModelSelection(path);
            ImportModel(path);
            var avatar = workspace.Document.ActiveObject;
            Check(avatar != null && !avatar.IsStaticProfile && avatar.Graph != null,
                "Real clothing probe did not publish a skinned avatar object.");
            var session = RigFor(avatar);
            var avatarSkeleton = session == null ? null : TryResolveSkeleton(session, avatar.Graph);
            Check(avatarSkeleton != null, "Real clothing probe did not retain the avatar skeleton.");
            var neck = avatarSkeleton.Bones.FirstOrDefault(bone =>
                string.Equals(bone.Name, "Neck", StringComparison.OrdinalIgnoreCase));
            Check(neck != null, "Real clothing probe requires a Neck bone in the imported avatar.");
            string avatarObjectId = avatar.ObjectId;

            // Start from the same production primitive exposed by the GUI.
            CreateChokerGraph();
            string polygonObjectId = workspace.Document.ActiveObjectId;
            Check(polygonObjectId != avatarObjectId && IsGraph,
                "Real clothing probe did not create a separate choker graph object.");
            attachmentTargetChoice = avatarObjectId;
            RefreshAttachmentControls();
            int neckIndex = attachmentBoneIds.IndexOf(neck.BoneId);
            Check(neckIndex >= 0, "Real clothing probe did not expose the Neck BoneId in attachment controls.");
            attachmentBone.index = neckIndex;
            ApplyAttachment();
            var rigid = workspace.Document.ActiveObject.Graph.Nodes.Values.SingleOrDefault(node => node.TypeId == BuiltinNodes.Attachment);
            Check(rigid != null && rigid.AttachmentTargetObjectId == avatarObjectId && rigid.AttachmentBoneId == neck.BoneId,
                "Real clothing probe did not retain the explicit Neck attachment.");

            // Bake the rigid placement into an authored graph, then replace the
            // default root weights with an explicit Neck-only binding. The
            // result follows the actual avatar skeleton when received by Unity.
            MaterializePolygonAccessory();
            var clothing = workspace.Document.ActiveObject;
            Check(clothing != null && clothing.ObjectId != avatarObjectId && clothing.Graph != null,
                "Real clothing probe did not publish the skin clothing graph.");
            var evaluation = clothing.EvaluateGraph();
            var edit = clothing.Graph.Nodes.Values.SingleOrDefault(node => node.TypeId == BuiltinNodes.EditMesh);
            var bind = clothing.Graph.Nodes.Values.SingleOrDefault(node => node.TypeId == BuiltinNodes.SkinBind && node.Binding != null);
            GraphMeshValue editValue = null;
            bool hasEditMesh = edit != null && evaluation.MeshOutputs.TryGetValue(edit.NodeId, out editValue);
            Check(edit != null && bind != null && hasEditMesh && editValue?.Mesh != null,
                "Real clothing probe lost the authored choker mesh or SkinBind stage.");
            int authoredVertexCount = editValue.Mesh.VertexCount;
            var neckBinding = SkinBinding.Create(editValue.Mesh, avatarSkeleton,
                Enumerable.Range(0, editValue.Mesh.VertexCount)
                    .Select(vertex => new SkinBinding.VertexWeightInput(vertex, neck.BoneId, 1f)));
            Execute(AuthoringOperation.UpdateNode(GraphNode.SkinBindNode(bind.NodeId, neckBinding)));
            clothing = workspace.Document.ActiveObject;
            evaluation = clothing.EvaluateGraph();
            var outputMesh = evaluation.Output?.Mesh;
            GraphSkinBindingValue bindingValue = null;
            bool hasBinding = evaluation.SkinBindingOutputs.TryGetValue(bind.NodeId, out bindingValue);
            bool neckWeights = hasBinding && bindingValue?.Binding != null && bindingValue.Binding.Weights.Values.All(values =>
                values.Count == 1 && values[0].BoneId == neck.BoneId && Math.Abs(values[0].Weight - 1f) < 1e-6f);
            Check(outputMesh != null && outputMesh.VertexCount == authoredVertexCount && neckWeights,
                "Real clothing probe did not create a complete Neck skin binding (outputVertices=" +
                (outputMesh == null ? "null" : outputMesh.VertexCount.ToString()) + ", binding=" +
                hasBinding + ", weights=" + (bindingValue?.Binding?.Weights.Count.ToString() ?? "null") + ").");

            string project = Path.Combine(output, "real-clothing-project");
            projectPath.SetValueWithoutNotify(project);
            Check(TrySaveProject(), "Real clothing project save failed.");
            string savedHash = workspace.Document.StateHash;
            OpenProject();
            Check(workspace.Document.StateHash == savedHash && !workspace.IsDirty,
                "Real clothing project changed after native Save/Open.");
            Execute(AuthoringOperation.SelectObject(clothing.ObjectId));
            ExportSelectedClothingPackage();
            var manifests = Directory.GetFiles(Path.Combine(project, "exports"),
                SkinnedClothingPackage.ManifestFileName, SearchOption.AllDirectories);
            Check(manifests.Length == 1, "Real clothing probe did not publish exactly one choker package.");
            var package = SkinnedClothingPackage.Read(manifests[0]);
            Check(package.ObjectId == clothing.ObjectId && package.Mesh.VertexCount == authoredVertexCount &&
                package.Skeleton.Bones.Any(bone => bone.BoneId == neck.BoneId) &&
                package.Binding.Weights.Values.All(values => values.Any(value => value.BoneId == neck.BoneId)),
                "Real clothing package did not retain the Neck-bound authored choker.");
            checks.Add("real VRM input -> authored choker primitive -> explicit Neck skin-bind -> native Save/Open -> clothing package");

            string empty = Path.Combine(output, "real-clothing-empty");
            ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
            projectPath.SetValueWithoutNotify(empty);
            Check(TrySaveProject(), "Real clothing probe could not persist its clean continuation state.");
            OpenProject();
        }
    }
}
