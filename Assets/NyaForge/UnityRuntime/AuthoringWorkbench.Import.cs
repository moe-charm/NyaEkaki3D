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

        sealed class ImportedGraphBuild
        {
            internal readonly AuthoringGraph Graph;
            internal readonly ImportMetadataCandidate Candidate;
            internal readonly int MeshIndex;
            internal readonly int? SkinIndex;
            internal readonly string DisplayName;

            internal ImportedGraphBuild(AuthoringGraph graph, ImportMetadataCandidate candidate,
                int meshIndex, int? skinIndex, string displayName)
            {
                Graph = graph; Candidate = candidate; MeshIndex = meshIndex;
                SkinIndex = skinIndex; DisplayName = displayName ?? "";
            }
        }

        void BuildModelImport(VisualElement parent)
        {
            modelImportPanel = new Foldout { text = "GLB / VRMモデルを取り込む", value = false, name = "model-import" };
            modelImportStatus = new Label { name = "model-import-status" }; modelImportStatus.style.whiteSpace = WhiteSpace.Normal; modelImportPanel.Add(modelImportStatus);
            BuildModelImportGuidance(modelImportPanel);
            var importHelp = new Label("GLB / VRMの候補を確認してから、1メッシュまたはファイル内の全mesh instanceを追加します。nodeを選ぶと、その配置とmesh／skinの対応を使います。対応するVRM0・VRM1では揺れをプレビューできます。骨のTRS・行列とinverse-bindはsource原本へ保持します。FBXの直接読込は未対応です。")
            {
                name = "model-import-technical-help"
            };
            importHelp.style.whiteSpace = WhiteSpace.Normal; modelImportPanel.Add(importHelp);
            BuildVrmSpringStatus(modelImportPanel);
            BuildPhysBonesStatus(modelImportPanel);
            BuildImportedRigStatus(modelImportPanel);
            BuildModelImportDiagnostics(modelImportPanel);
            modelImportPanel.Add(Button("GLB / VRMを選ぶ", () => { ShowModelImportPanel(); if (!modelPickerOpen) StartCoroutine(PickModel()); }, "model-import-browse"));
            modelImportPath = new TextField("ファイルパス") { name = "model-import-path" }; modelImportPath.style.flexDirection = FlexDirection.Column; modelImportPanel.Add(modelImportPath);
            BuildModelImportSelection(modelImportPanel);
            modelImportPanel.Add(Button("このGLB / VRMをgraph objectへ取り込む", () => Try(() => ImportModel(modelImportPath.value)), "model-import-apply"));
            modelImportPanel.Add(Button("このファイルの全mesh instanceを取り込む", () => Try(() => ImportAllModelInstances(modelImportPath.value)), "model-import-all"));
            parent.Add(modelImportPanel);
        }

        void RefreshModelImport()
        {
            if (modelImportPanel == null) return;
            RefreshImportedRigStatus();
            RefreshImportedGlbDiagnostics();
            bool ready = workspace != null && (workspace.Document.IsEmpty || !workspace.Document.ActiveObject.IsStaticProfile);
            modelImportPanel.Q<Button>("model-import-apply").SetEnabled(ready);
            modelImportPanel.Q<Button>("model-import-all").SetEnabled(ready);
            modelImportStatus.text = ready ? (workspace.Document.IsEmpty ? "空の制作projectへ取り込めます。" : "現在のgraph projectへ新しい制作対象として追加できます。") + "元ファイルはコピーせず、meshをnative graphへ取り込みます。" : "graph projectへ追加できます。static projectへは追加できません。既存作品は置き換えません。";
        }

        void ImportModel(string path)
        {
            if (workspace == null || (!workspace.Document.IsEmpty && workspace.Document.ActiveObject.IsStaticProfile)) throw new InvalidOperationException("GLB取り込みは空またはgraph projectで実行してください。");
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("GLBファイルを選択してください。");
            string fullPath = Path.GetFullPath(path); string sourceDirectory = Path.GetDirectoryName(fullPath);
            var bytes = ReadModelFile(fullPath);
            VrmMetadata vrm = VrmMetadataReader.ContainsVrm(bytes) ? VrmMetadataReader.Read(bytes) : null;
            var document = GlbDocumentReader.Read(bytes);
            var inventory = GlbSceneInventoryReader.Read(document);
            var imageCache = new Dictionary<int, ImportedBaseColorImage>();
            var encodedImageCache = new GlbImportImageCache();
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
                var skinnedInstanceWorld = instanceIndex >= 0 ? inventory.Instances[instanceIndex].WorldTransform : null;
                int? sourceNodeIndex = instanceIndex >= 0 ? inventory.Instances[instanceIndex].NodeIndex : (int?)null;
                ImportSkinnedModel(bytes, vrm, meshIndex, skinIndex, skinnedInstanceWorld, sourceDirectory, sourceNodeIndex, document, imageCache, encodedImageCache); return;
            }
            var instanceWorld = instanceIndex >= 0 ? inventory.Instances[instanceIndex].WorldTransform : null;
            int? staticSourceNodeIndex = instanceIndex >= 0 ? inventory.Instances[instanceIndex].NodeIndex : (int?)null;
            var build = BuildStaticImport(bytes, vrm, meshIndex, sourceDirectory, instanceWorld, inventory.Instances.FirstOrDefault(item => item.MeshIndex == meshIndex)?.Name, staticSourceNodeIndex, document, imageCache, encodedImageCache);
            CommitImportedGraph(build.Graph, build.Candidate);
            Refresh(); Frame(); SetStatus("GLBを取り込みました。mesh " + meshIndex + " · " + build.DisplayName);
        }

        void ImportSkinnedModel(byte[] bytes, VrmMetadata vrm, int meshIndex, int skinIndex, SourceAffine instanceWorldTransform = null, string sourceDirectory = null, int? sourceNodeIndex = null, GlbDocument parsedDocument = null, IDictionary<int, ImportedBaseColorImage> imageCache = null, GlbImportImageCache encodedImageCache = null)
        {
            var build = BuildSkinnedImport(bytes, vrm, meshIndex, skinIndex, instanceWorldTransform, sourceDirectory, null, sourceNodeIndex, parsedDocument, imageCache, encodedImageCache);
            CommitImportedGraph(build.Graph, build.Candidate);
            Refresh(); Frame(); SetStatus("GLB skinを取り込みました。mesh " + meshIndex + " · skin " + skinIndex + " · " + build.DisplayName);
        }

        ImportedGraphBuild BuildStaticImport(byte[] bytes, VrmMetadata vrm, int meshIndex, string sourceDirectory,
            SourceAffine instanceWorldTransform, string sourceName, int? sourceNodeIndex = null, GlbDocument parsedDocument = null, IDictionary<int, ImportedBaseColorImage> imageCache = null, GlbImportImageCache encodedImageCache = null)
        {
            var imported = parsedDocument == null
                ? GlbImporter.ReadFromDirectory(bytes, meshIndex, sourceDirectory, instanceWorldTransform)
                : GlbImporter.ReadDocument(parsedDocument, meshIndex, instanceWorldTransform, sourceDirectory, encodedImageCache);
            string sourceId = Guid.NewGuid().ToString("D"), editId = Guid.NewGuid().ToString("D"), outputId = Guid.NewGuid().ToString("D");
            var nodes = new List<GraphNode> { GraphNode.Source(sourceId, imported.Mesh, new RestTransform(1, new Vec3())) };
            var edges = new List<GraphEdge>(); string finalNode = sourceId;
            if (imported.Morphs != null)
            {
                string morphId = Guid.NewGuid().ToString("D"), deformId = Guid.NewGuid().ToString("D");
                var zeroWeights = imported.Morphs.Targets.ToDictionary(target => target.TargetId, _ => 0f, StringComparer.Ordinal);
                nodes.Add(GraphNode.MorphSetNode(morphId, imported.Morphs)); nodes.Add(GraphNode.MorphDeformNode(deformId, zeroWeights));
                edges.Add(new GraphEdge(sourceId, "mesh", deformId, "mesh")); edges.Add(new GraphEdge(morphId, "morphs", deformId, "morphs")); finalNode = deformId;
            }
            nodes.Add(GraphNode.Edit(editId)); edges.Add(new GraphEdge(finalNode, "mesh", editId, "mesh")); finalNode = editId;
            var materialWarnings = new List<string>();
            finalNode = AppendImportedMaterials(nodes, edges, finalNode, imported.Mesh.Submeshes.Count, imported.Materials, materialWarnings, imageCache);
            nodes.Add(GraphNode.Output(outputId)); edges.Add(new GraphEdge(finalNode, "mesh", outputId, "mesh"));
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), nodes, edges, outputId);
            // Persist the source locator even when the selected mesh has no
            // lossy-feature warnings. This keeps shared-resource identity
            // available after native Save/Open instead of making it depend on
            // whether a diagnostic happened to be emitted.
            var diagnostics = new ImportedGlbDiagnostics(graph.GraphId, imported.SourceHash, imported.MeshIndex, null, imported.Diagnostics, sourceNodeIndex);
            var candidate = new ImportMetadataCandidate(null, PrepareImportedExpressions(bytes, vrm, imported.Morphs), vrm, diagnostics);
            string display = (string.IsNullOrWhiteSpace(sourceName) ? "mesh " + meshIndex : sourceName) +
                (imported.Morphs == null ? " · morphなし" : " · morph " + imported.Morphs.Targets.Count + "個") +
                " · source " + imported.SourceHash.Substring(0, 12) + ImportDiagnosticSummary(imported.Diagnostics) + ImportMaterialWarningSummary(materialWarnings);
            return new ImportedGraphBuild(graph, candidate, meshIndex, null, display);
        }

        ImportedGraphBuild BuildSkinnedImport(byte[] bytes, VrmMetadata vrm, int meshIndex, int skinIndex,
            SourceAffine instanceWorldTransform, string sourceDirectory, string sourceName, int? sourceNodeIndex = null, GlbDocument parsedDocument = null, IDictionary<int, ImportedBaseColorImage> imageCache = null, GlbImportImageCache encodedImageCache = null)
        {
            var imported = parsedDocument == null
                ? GlbSkinImporter.ReadFromDirectory(bytes, meshIndex, skinIndex, sourceDirectory, instanceWorldTransform)
                : GlbSkinImporter.ReadFromDocument(parsedDocument, meshIndex, skinIndex, sourceDirectory, instanceWorldTransform, encodedImageCache);
            string sourceId = Guid.NewGuid().ToString("D"), editId = Guid.NewGuid().ToString("D"), skeletonId = Guid.NewGuid().ToString("D"), bindId = Guid.NewGuid().ToString("D"), poseId = Guid.NewGuid().ToString("D"), deformId = Guid.NewGuid().ToString("D"), outputId = Guid.NewGuid().ToString("D");
            var nodes = new List<GraphNode> { GraphNode.Source(sourceId, imported.Mesh, new RestTransform(1, new Vec3())), GraphNode.Edit(editId), GraphNode.SkeletonNode(skeletonId, imported.Skeleton), GraphNode.SkinBindNode(bindId, imported.Binding), GraphNode.PoseNode(poseId, PoseSet.Create(imported.Skeleton, imported.Skeleton.Bones.Select(b => new BonePose(b.BoneId, PoseTransform.FromTranslation(b.Head))))), GraphNode.SkinDeformNode(deformId), GraphNode.Output(outputId) };
            var edges = new List<GraphEdge>(); string finalNode = sourceId;
            if (imported.Morphs != null)
            {
                string morphId = Guid.NewGuid().ToString("D"), morphDeformId = Guid.NewGuid().ToString("D");
                var weights = imported.Morphs.Targets.ToDictionary(target => target.TargetId, _ => 0f, StringComparer.Ordinal);
                nodes.Insert(1, GraphNode.MorphSetNode(morphId, imported.Morphs)); nodes.Insert(2, GraphNode.MorphDeformNode(morphDeformId, weights));
                edges.Add(new GraphEdge(sourceId, "mesh", morphDeformId, "mesh")); edges.Add(new GraphEdge(morphId, "morphs", morphDeformId, "morphs")); finalNode = morphDeformId;
            }
            edges.Add(new GraphEdge(finalNode, "mesh", editId, "mesh")); edges.Add(new GraphEdge(editId, "mesh", bindId, "mesh")); edges.Add(new GraphEdge(skeletonId, "skeleton", bindId, "skeleton")); edges.Add(new GraphEdge(skeletonId, "skeleton", poseId, "skeleton")); edges.Add(new GraphEdge(editId, "mesh", deformId, "mesh")); edges.Add(new GraphEdge(skeletonId, "skeleton", deformId, "skeleton")); edges.Add(new GraphEdge(bindId, "binding", deformId, "binding")); edges.Add(new GraphEdge(poseId, "pose", deformId, "pose"));
            var materialWarnings = new List<string>();
            finalNode = AppendImportedMaterials(nodes, edges, deformId, imported.Mesh.Submeshes.Count, imported.Materials, materialWarnings, imageCache);
            edges.Add(new GraphEdge(finalNode, "mesh", outputId, "mesh"));
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"), nodes, edges, outputId);
            // The display importer has already decoded the selected mesh. Read
            // only source-space frames and weights here instead of asking the
            // source-skin importer to clone JSON and parse the same geometry a
            // second time. SourceSkinBinding stores topology identity and
            // weights, so the decoded mesh is safe to reuse (including an
            // explicitly selected node affine).
            var sourceData = GlbSourceSkinImporter.ReadDataFromDocument(parsedDocument ?? GlbDocumentReader.Read(bytes), meshIndex, skinIndex, imported.Mesh);
            var rigSession = ImportedRigSession.Create(imported, vrm, graph.GraphId, skeletonId).WithSourceSkin(sourceData.Skin, sourceData.Binding);
            var diagnostics = new ImportedGlbDiagnostics(graph.GraphId, imported.SourceHash, imported.MeshIndex, imported.SkinIndex, imported.Diagnostics, sourceNodeIndex);
            var candidate = new ImportMetadataCandidate(rigSession, PrepareImportedExpressions(bytes, vrm, imported.Morphs), vrm, diagnostics);
            string display = (string.IsNullOrWhiteSpace(sourceName) ? "mesh " + meshIndex : sourceName) +
                " · bone " + imported.Skeleton.Bones.Count + " · weight " + imported.Binding.Weights.Count +
                (imported.Morphs == null ? " · morphなし" : " · morph " + imported.Morphs.Targets.Count + "個") +
                " · source " + imported.SourceHash.Substring(0, 12) + ImportDiagnosticSummary(imported.Diagnostics) + ImportMaterialWarningSummary(materialWarnings);
            return new ImportedGraphBuild(graph, candidate, meshIndex, skinIndex, display);
        }

        void ImportAllModelInstances(string path)
        {
            if (workspace == null || (!workspace.Document.IsEmpty && workspace.Document.ActiveObject.IsStaticProfile)) throw new InvalidOperationException("GLB取り込みは空またはgraph projectで実行してください。");
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("GLBファイルを選択してください。");
            ImportAllModelInstances(path, null, null);
        }

        /// <summary>
        /// Imports all instances from an already decoded document when the
        /// caller has just inspected the same file. This avoids a second file
        /// read, GLB parse and model-sized buffer during command-line checks;
        /// the normal GUI path still uses the single-argument entry point.
        /// </summary>
        void ImportAllModelInstances(string path, byte[] suppliedBytes, GlbDocument suppliedDocument)
        {
            if (workspace == null || (!workspace.Document.IsEmpty && workspace.Document.ActiveObject.IsStaticProfile)) throw new InvalidOperationException("GLB取り込みは空またはgraph projectで実行してください。");
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("GLBファイルを選択してください。");
            string fullPath = Path.GetFullPath(path); string sourceDirectory = Path.GetDirectoryName(fullPath); var bytes = suppliedBytes ?? ReadModelFile(fullPath);
            VrmMetadata vrm = VrmMetadataReader.ContainsVrm(bytes) ? VrmMetadataReader.Read(bytes) : null;
            var document = suppliedDocument ?? GlbDocumentReader.Read(bytes);
            var inventory = GlbSceneInventoryReader.Read(document);
            var imageCache = new Dictionary<int, ImportedBaseColorImage>();
            var encodedImageCache = new GlbImportImageCache();
            var builds = new List<ImportedGraphBuild>();
            if (inventory.Instances.Count > 0)
            {
                foreach (var instance in inventory.Instances)
                {
                    if (instance.SkinIndex.HasValue)
                        builds.Add(BuildSkinnedImport(bytes, vrm, instance.MeshIndex, instance.SkinIndex.Value, instance.WorldTransform, sourceDirectory, instance.Name, instance.NodeIndex, document, imageCache, encodedImageCache));
                    else builds.Add(BuildStaticImport(bytes, vrm, instance.MeshIndex, sourceDirectory, instance.WorldTransform, instance.Name, instance.NodeIndex, document, imageCache, encodedImageCache));
                }
            }
            else
            {
                for (int meshIndex = 0; meshIndex < inventory.Meshes.Count; meshIndex++)
                {
                    var linked = inventory.Instances.Where(item => item.MeshIndex == meshIndex && item.SkinIndex.HasValue).Select(item => item.SkinIndex.Value).Distinct().ToArray();
                    if (linked.Length > 1) throw new InvalidOperationException("mesh resourceへ複数skinが対応するため、node instanceを選んでください。");
                    builds.Add(linked.Length == 1
                        ? BuildSkinnedImport(bytes, vrm, meshIndex, linked[0], null, sourceDirectory, inventory.Meshes[meshIndex].Name, null, document, imageCache, encodedImageCache)
                        : BuildStaticImport(bytes, vrm, meshIndex, sourceDirectory, null, inventory.Meshes[meshIndex].Name, null, document, imageCache, encodedImageCache));
                }
            }
            if (builds.Count == 0) throw new InvalidOperationException("取り込めるmesh instanceがありません。");
            if (workspace.Document.Objects.Count + builds.Count > 64) throw new InvalidOperationException("全mesh instanceを追加するとobject上限64件を超えます。候補を選んで分割して取り込んでください。");
            CommitImportedGraphs(builds);
            Refresh(); Frame(); SetStatus("GLB / VRMの全mesh instanceを取り込みました。" + builds.Count + " objects · source " + inventory.SourceHash.Substring(0, 12));
        }

        static string ImportDiagnosticSummary(IReadOnlyList<GlbImportDiagnostic> diagnostics)
        {
            if (diagnostics == null || diagnostics.Count == 0) return "";
            return " · 診断 " + string.Join(" / ", diagnostics.Select(item => (item.IsBlocking ? "保持不可" : "一部保持") + ":" + item.Code));
        }

        static string ImportMaterialWarningSummary(IReadOnlyList<string> warnings)
        {
            return warnings == null || warnings.Count == 0 ? "" : " · 材質画像の注意 " + warnings.Count + "件（詳細は取込ステータス）";
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

        /// <summary>Reject oversized model files before allocating their byte buffer.</summary>
        static byte[] ReadModelFile(string path)
        {
            string fullPath = Path.GetFullPath(path);
            var info = new FileInfo(fullPath);
            if (!info.Exists) throw new FileNotFoundException("GLB / VRM file was not found.", fullPath);
            const long maximum = NyaForge.Authoring.AuthoringLimits.MaxGlbImportBytes;
            try
            {
                using (var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Length <= 0 || stream.Length > maximum)
                        throw new AuthoringException("BUDGET_EXCEEDED", "GLB / VRM file exceeds the 128 MiB import budget.");
                    int length = checked((int)stream.Length); var result = new byte[length]; int offset = 0;
                    while (offset < result.Length)
                    {
                        int read = stream.Read(result, offset, result.Length - offset);
                        if (read <= 0) throw new IOException("The GLB / VRM file ended before its declared length.");
                        offset += read;
                    }
                    return result;
                }
            }
            catch (AuthoringException) { throw; }
            catch (FileNotFoundException) { throw new FileNotFoundException("GLB / VRM file was not found.", fullPath); }
            catch (DirectoryNotFoundException) { throw new FileNotFoundException("GLB / VRM file was not found.", fullPath); }
            catch (UnauthorizedAccessException error) { throw new AuthoringException("IMPORT_RESOURCE_UNAVAILABLE", "GLB / VRM file could not be read: " + error.Message); }
            catch (IOException error) { throw new AuthoringException("IMPORT_RESOURCE_UNAVAILABLE", "GLB / VRM file could not be read: " + error.Message); }
        }
    }
}
