using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Foldout modelImportPanel;
        Label modelImportStatus;
        TextField modelImportPath;
        bool modelPickerOpen;

        void BuildModelImport(VisualElement parent)
        {
            modelImportPanel = new Foldout { text = "GLBモデルを取り込む", value = false, name = "model-import" };
            modelImportStatus = new Label { name = "model-import-status" }; modelImportStatus.style.whiteSpace = WhiteSpace.Normal; modelImportPanel.Add(modelImportStatus);
            var importHelp = new Label("GLB / VRMを取り込みます。候補を確認してnode instance（-1ならmesh / skin resource）を選べます。nodeを選ぶと、そのmeshとskinの対応を使います。対応するVRM0・VRM1では揺れをプレビューできます。選択した1メッシュを取り込み、骨のTRS・行列とinverse-bindはsource原本へ保持します。FBXの直接読込は未対応です。");
            importHelp.style.whiteSpace = WhiteSpace.Normal; modelImportPanel.Add(importHelp);
            BuildVrmSpringStatus(modelImportPanel);
            BuildPhysBonesStatus(modelImportPanel);
            BuildImportedRigStatus(modelImportPanel);
            modelImportPanel.Add(Button("GLBを選ぶ", () => { if (!modelPickerOpen) StartCoroutine(PickModel()); }, "model-import-browse"));
            modelImportPath = new TextField("ファイルパス") { name = "model-import-path" }; modelImportPath.style.flexDirection = FlexDirection.Column; modelImportPanel.Add(modelImportPath);
            BuildModelImportSelection(modelImportPanel);
            modelImportPanel.Add(Button("このGLBをgraph objectへ取り込む", () => Try(() => ImportModel(modelImportPath.value)), "model-import-apply"));
            parent.Add(modelImportPanel);
        }

        void RefreshModelImport()
        {
            if (modelImportPanel == null) return;
            RefreshImportedRigStatus();
            bool ready = workspace != null && (workspace.Document.IsEmpty || !workspace.Document.ActiveObject.IsStaticProfile);
            modelImportPanel.Q<Button>("model-import-apply").SetEnabled(ready);
            modelImportStatus.text = ready ? (workspace.Document.IsEmpty ? "空の制作projectへ取り込めます。" : "現在のgraph projectへ新しい制作対象として追加できます。") + "元ファイルはコピーせず、meshをnative graphへ取り込みます。" : "graph projectへ追加できます。static projectへは追加できません。既存作品は置き換えません。";
        }

        void ImportModel(string path)
        {
            if (workspace == null || (!workspace.Document.IsEmpty && workspace.Document.ActiveObject.IsStaticProfile)) throw new InvalidOperationException("GLB取り込みは空またはgraph projectで実行してください。");
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("GLBファイルを選択してください。");
            var bytes = File.ReadAllBytes(Path.GetFullPath(path));
            VrmMetadata vrm = VrmMetadataReader.ContainsVrm(bytes) ? VrmMetadataReader.Read(bytes) : null;
            var inventory = GlbSceneInventoryReader.Read(bytes);
            int instanceIndex = SelectedModelInstanceIndex;
            if (instanceIndex >= 0 && instanceIndex >= inventory.Instances.Count) throw new InvalidOperationException("node instance index が範囲外です。候補を確認してください。");
            int meshIndex = instanceIndex >= 0 ? inventory.Instances[instanceIndex].MeshIndex : SelectedModelMeshIndex;
            int? instanceSkinIndex = instanceIndex >= 0 ? inventory.Instances[instanceIndex].SkinIndex : (int?)null;
            int skinIndex = instanceSkinIndex ?? SelectedModelSkinIndex;
            if (meshIndex < 0 || meshIndex >= inventory.Meshes.Count) throw new InvalidOperationException("mesh index が範囲外です。候補を確認してください。");
            bool useSkin = instanceSkinIndex.HasValue;
            if (instanceIndex < 0)
            {
                var linkedSkins = inventory.Instances.Where(item => item.MeshIndex == meshIndex && item.SkinIndex.HasValue).Select(item => item.SkinIndex.Value).Distinct().ToArray();
                if (linkedSkins.Length == 1) { skinIndex = linkedSkins[0]; useSkin = true; }
                else if (linkedSkins.Length > 1) useSkin = true;
                else if (inventory.Instances.Count == 0 && inventory.Skins.Count > 0) useSkin = true;
            }
            if (useSkin)
            {
                if (skinIndex < 0 || skinIndex >= inventory.Skins.Count) throw new InvalidOperationException("skin index が範囲外です。候補を確認してください。");
                if (instanceIndex < 0)
                {
                    var linkedSkins = inventory.Instances.Where(item => item.MeshIndex == meshIndex && item.SkinIndex.HasValue).Select(item => item.SkinIndex.Value).Distinct().ToArray();
                    if (linkedSkins.Length > 1 && !linkedSkins.Contains(skinIndex)) throw new InvalidOperationException("選択meshに対応しないskin indexです。node instanceを指定してください。");
                }
                ImportSkinnedModel(bytes, vrm, meshIndex, skinIndex); return;
            }
            var imported = GlbImporter.Read(bytes, meshIndex);
            string sourceId = Guid.NewGuid().ToString("D"), outputId = Guid.NewGuid().ToString("D");
            var nodes = new List<GraphNode> { GraphNode.Source(sourceId, imported.Mesh, new RestTransform(1, new Vec3())) };
            var edges = new List<GraphEdge>(); string finalNode = sourceId;
            if (imported.Morphs != null)
            {
                string morphId = Guid.NewGuid().ToString("D"), deformId = Guid.NewGuid().ToString("D");
                var zeroWeights = imported.Morphs.Targets.ToDictionary(target => target.TargetId, _ => 0f, StringComparer.Ordinal);
                nodes.Add(GraphNode.MorphSetNode(morphId, imported.Morphs)); nodes.Add(GraphNode.MorphDeformNode(deformId, zeroWeights));
                edges.Add(new GraphEdge(sourceId, "mesh", deformId, "mesh")); edges.Add(new GraphEdge(morphId, "morphs", deformId, "morphs")); finalNode = deformId;
            }
            nodes.Add(GraphNode.Output(outputId)); edges.Add(new GraphEdge(finalNode, "mesh", outputId, "mesh"));
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), nodes, edges, outputId);
            CommitImportedGraph(graph, new ImportMetadataCandidate(null, PrepareImportedExpressions(bytes, vrm, imported.Morphs), vrm));
            Refresh(); SetStatus("GLBを取り込みました。mesh " + meshIndex + " · " + (imported.Morphs == null ? " morphなし" : " morph " + imported.Morphs.Targets.Count + "個") + (vrm == null ? "" : " · " + vrm.Format + " " + vrm.Title + " humanoid " + vrm.HumanoidNodes.Count + " expression " + vrm.Expressions.Count + " spring " + vrm.SpringBones.Count + "/" + vrm.SpringColliderGroups.Count) + " · source " + imported.SourceHash.Substring(0, 12));
        }

        void ImportSkinnedModel(byte[] bytes, VrmMetadata vrm, int meshIndex, int skinIndex)
        {
            var imported = GlbSkinImporter.Read(bytes, meshIndex, skinIndex); string sourceId = Guid.NewGuid().ToString("D"), editId = Guid.NewGuid().ToString("D"), skeletonId = Guid.NewGuid().ToString("D"), bindId = Guid.NewGuid().ToString("D"), poseId = Guid.NewGuid().ToString("D"), deformId = Guid.NewGuid().ToString("D"), outputId = Guid.NewGuid().ToString("D");
            // Keep an explicit rest-space EditMesh before binding/deformation so a newly
            // imported avatar is immediately editable from the same vertex workflow as a
            // hand-authored graph. The binding topology remains valid for offset-only edits.
            var nodes = new List<GraphNode> { GraphNode.Source(sourceId, imported.Mesh, new RestTransform(1, new Vec3())), GraphNode.Edit(editId), GraphNode.SkeletonNode(skeletonId, imported.Skeleton), GraphNode.SkinBindNode(bindId, imported.Binding), GraphNode.PoseNode(poseId, PoseSet.Create(imported.Skeleton, imported.Skeleton.Bones.Select(b => new BonePose(b.BoneId, PoseTransform.FromTranslation(b.Head))))), GraphNode.SkinDeformNode(deformId), GraphNode.Output(outputId) };
            var edges = new List<GraphEdge>(); string finalNode = sourceId;
            if (imported.Morphs != null)
            {
                string morphId = Guid.NewGuid().ToString("D"), morphDeformId = Guid.NewGuid().ToString("D"); var weights = imported.Morphs.Targets.ToDictionary(target => target.TargetId, _ => 0f, StringComparer.Ordinal);
                nodes.Insert(1, GraphNode.MorphSetNode(morphId, imported.Morphs)); nodes.Insert(2, GraphNode.MorphDeformNode(morphDeformId, weights)); edges.Add(new GraphEdge(sourceId, "mesh", morphDeformId, "mesh")); edges.Add(new GraphEdge(morphId, "morphs", morphDeformId, "morphs")); finalNode = morphDeformId;
            }
            edges.Add(new GraphEdge(finalNode, "mesh", editId, "mesh")); edges.Add(new GraphEdge(editId, "mesh", bindId, "mesh")); edges.Add(new GraphEdge(skeletonId, "skeleton", bindId, "skeleton")); edges.Add(new GraphEdge(skeletonId, "skeleton", poseId, "skeleton")); edges.Add(new GraphEdge(editId, "mesh", deformId, "mesh")); edges.Add(new GraphEdge(skeletonId, "skeleton", deformId, "skeleton")); edges.Add(new GraphEdge(bindId, "binding", deformId, "binding")); edges.Add(new GraphEdge(poseId, "pose", deformId, "pose")); edges.Add(new GraphEdge(deformId, "mesh", outputId, "mesh"));
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), nodes, edges, outputId);
            var sourceCandidate = GlbSourceSkinImporter.Read(bytes, meshIndex, skinIndex);
            var rigSession = ImportedRigSession.Create(imported, vrm, graph.GraphId, skeletonId)
                .WithSourceSkin(sourceCandidate.Skin, sourceCandidate.Binding);
            CommitImportedGraph(graph, new ImportMetadataCandidate(rigSession, PrepareImportedExpressions(bytes, vrm, imported.Morphs), vrm));
            Refresh(); SetStatus("GLB skinを取り込みました。mesh " + meshIndex + " · skin " + skinIndex + " · bone " + imported.Skeleton.Bones.Count + " · weight " + imported.Binding.Weights.Count + (imported.Morphs == null ? " · morphなし" : " · morph " + imported.Morphs.Targets.Count + "個") + (vrm == null ? "" : " · " + vrm.Format + " " + vrm.Title + " humanoid " + vrm.HumanoidNodes.Count + " expression " + vrm.Expressions.Count + " spring " + vrm.SpringBones.Count + "/" + vrm.SpringColliderGroups.Count) + " · source " + imported.SourceHash.Substring(0, 12));
        }

        IEnumerator PickModel()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var previous = workspace; string state = workspace.Document.StateHash;
            modelPickerOpen = true;
            try
            {
                var picker = Platform.WindowsFilePicker.Open(Platform.WindowsFilePicker.GetActiveWindow(), ModelImportDirectory(modelImportPath.value),
                    "モデル (*.glb;*.vrm)\0*.glb;*.vrm\0\0", "NyaForge — GLB/VRMモデルを取り込む", "glb");
                while (!picker.IsCompleted) yield return null;
                if (picker.IsFaulted) { SetStatus(picker.Exception.GetBaseException().Message); yield break; }
                if (string.IsNullOrEmpty(picker.Result)) yield break;
                if (!ReferenceEquals(previous, workspace) || state != workspace.Document.StateHash) { SetStatus("選択中に作品が変わったため、GLB取り込みを取り消しました。"); yield break; }
                modelImportPath.SetValueWithoutNotify(picker.Result);
            }
            finally { modelPickerOpen = false; }
#else
            SetStatus("GLBファイル選択はWindows版に対応しています。パスを指定して取り込めます。"); yield break;
#endif
        }

        static string ModelImportDirectory(string path)
        {
            try { return Path.GetDirectoryName(path); }
            catch (ArgumentException) { return null; }
        }
    }
}
