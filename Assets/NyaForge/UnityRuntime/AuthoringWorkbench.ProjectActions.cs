using System;
using System.IO;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void CancelReplace() { confirmRow.style.display = UnityEngine.UIElements.DisplayStyle.None; }
        void AddSample(float scale)
        {
            Execute(AuthoringOperation.AddMesh(AuthoringFixtures.Panel(scale), new RestTransform(scale, new Vec3())));
            if (!workspace.Document.IsEmpty) { Select(new[] { 0 }); Frame(); }
        }
        void ConfirmReplace(Action action)
        {
            if (!HasUnsaved) { Try(action); return; }
            confirmRow.Clear(); confirmRow.style.display = DisplayStyle.Flex;
            confirmRow.Add(new Label("未保存の変更があります。保存するか、変更を破棄して進んでください。"));
            confirmRow.Add(Button("変更を破棄して進む", () => { confirmRow.style.display = DisplayStyle.None; Try(action); }, "authoring-discard"));
            confirmRow.Add(Button("キャンセル", CancelReplace, "authoring-cancel"));
            controls.schedule.Execute(() => controls.ScrollTo(confirmRow));
        }

        void ReplaceWorkspace(AuthoringWorkspace next, string loadedPath)
        {
            paintCanvas?.CancelStroke();
            ClearSurfacePreparation();
            string oldStage = projection.PreviewNodeId;
            projection.PreviewNodeId = "";
            try { using (var prepared = projection.PrepareGraph(next.Document, next.Preview)) prepared.Commit(); }
            catch { projection.PreviewNodeId = oldStage; throw; }
            activeEditContext = null;
            selectedFaces.Clear(); faceMode.SetValueWithoutNotify(false);
            ClearImportedVrmExpressions();
            ClearImportedVrmSpring();
            workspace = next; commands = new AuthoringCommandService(workspace);
            saveIncomplete = false;
            savedDirectory = loadedPath == null ? null : Path.GetFullPath(loadedPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            projectPath.SetValueWithoutNotify(loadedPath ?? Path.Combine(Application.persistentDataPath, "Authoring", "Project-" + Guid.NewGuid().ToString("N").Substring(0, 8)));
            Select(projection.Points.Length == 0 ? Array.Empty<int>() : new[] { 0 }); Frame(); Refresh();
            SetStatus(loadedPath == null ? "新しい制作プロジェクトです。形を追加して始めてください。" : "制作状態を開きました。ここから新しい履歴を始めます。旧形式は別フォルダへ保存してください。");
        }


        void OpenProject()
        {
            var directory = Path.GetFullPath(projectPath.value);
            var next = ProjectStore.Open(directory);
            var expressionBytes = next.Attachments.Read(ProjectAttachments.Expressions);
            var springBytes = next.Attachments.Read(ProjectAttachments.Springs);
            var expressionSession = expressionBytes == null ? null : VrmExpressionSessionCodec.Read(expressionBytes);
            var springSession = springBytes == null ? null : VrmSpringSessionCodec.Read(springBytes);
            ReplaceWorkspace(next, directory);
            importedVrmSession = expressionSession; Refresh();
            importedVrmSpringSession = springSession; RefreshVrmSpringStatus();
        }

        void Export() => Try(() =>
        {
            var directory = Path.Combine(Path.GetFullPath(projectPath.value), "exports", "bake-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6));
            string manifest=ProjectExportService.Export(workspace,workspace.InstanceId,workspace.Document.DocumentId,workspace.Document.DocumentRevision,directory).ManifestPath;
            SetStatus("Unity用に書き出しました: " + manifest);
        });

    }
}
