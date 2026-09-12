using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
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
            modelImportPanel.Add(new Label("Windows先行。現在はGLB v2の1プリミティブ静的meshとPOSITION morphを読み込みます。FBX/VRM、skin、複数primitiveは対応範囲外です。"));
            modelImportPanel.Add(Button("GLBを選ぶ", () => { if (!modelPickerOpen) StartCoroutine(PickModel()); }, "model-import-browse"));
            modelImportPath = new TextField("ファイルパス") { name = "model-import-path" }; modelImportPath.style.flexDirection = FlexDirection.Column; modelImportPanel.Add(modelImportPath);
            modelImportPanel.Add(Button("このGLBを新規graphへ取り込む", () => Try(() => ImportModel(modelImportPath.value)), "model-import-apply"));
            parent.Add(modelImportPanel);
        }

        void RefreshModelImport()
        {
            if (modelImportPanel == null) return;
            bool ready = workspace != null && workspace.Document.IsEmpty;
            modelImportPanel.Q<Button>("model-import-apply").SetEnabled(ready);
            modelImportStatus.text = ready ? "空の制作projectへ取り込めます。元ファイルはコピーせず、meshをnative graphへ取り込みます。" : "取り込みは空の制作projectで実行してください。既存作品は置き換えません。";
        }

        void ImportModel(string path)
        {
            if (workspace == null || !workspace.Document.IsEmpty) throw new InvalidOperationException("GLB取り込みは空の制作projectで実行してください。");
            if (string.IsNullOrWhiteSpace(path)) throw new InvalidOperationException("GLBファイルを選択してください。");
            var imported = GlbImporter.Read(File.ReadAllBytes(Path.GetFullPath(path)));
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
            var result = new AuthoringCommandService(workspace).Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph)));
            if (!result.Success) throw new InvalidOperationException(result.Code + ": " + result.Message);
            Refresh(); SetStatus("GLBを取り込みました。" + (imported.Morphs == null ? " morphなし" : " morph " + imported.Morphs.Targets.Count + "個") + " · source " + imported.SourceHash.Substring(0, 12));
        }

        IEnumerator PickModel()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var previous = workspace; string state = workspace.Document.StateHash;
            modelPickerOpen = true;
            try
            {
                var picker = Platform.WindowsFilePicker.Open(Platform.WindowsFilePicker.GetActiveWindow(), ModelImportDirectory(modelImportPath.value),
                    "GLBモデル (*.glb)\0*.glb\0\0", "NyaForge — GLBモデルを取り込む", "glb");
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
