using System;
using NyaForge.Authoring;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    /// <summary>
    /// Shared command boundary for GUI edits.
    ///
    /// Feature panels only construct operations; this module owns protection,
    /// playback invalidation, measurement, history side effects, and the final
    /// projection refresh so every edit follows the same path.
    /// </summary>
    public sealed partial class AuthoringWorkbench
    {
        void Execute(params AuthoringOperation[] operations)
        {
            Try(() =>
            {
                if (ReferenceProtectionBlocks(operations)) throw new InvalidOperationException(ReferenceProtectionMessage);
                ClearSpringPlayback(true);
                var result = ExecuteMeasured(operations);
                if (!result.Success) { Refresh(); throw new InvalidOperationException(result.Code + ": " + result.Message); }
                if (operations.Length == 1 && (operations[0].Kind == "history.undo" || operations[0].Kind == "history.redo"))
                {
                    RefreshSecondaryMotionAttachmentFromWorkspace();
                    RefreshImportedVrmSessionsFromWorkspace();
                    RefreshReferenceProtectionFromWorkspace();
                    RefreshDeliveryAllowlistFromWorkspace();
                }
                var guiWatch = measureCommands ? System.Diagnostics.Stopwatch.StartNew() : null;
                selection.RemoveWhere(i => i < 0 || i >= projection.Points.Length);
                selectionContext.NotifyChanged();
                projection.Select(selection);
                Refresh();
                SetStatus(result.EvaluationComplete ? "編集を反映しました。元に戻す・やり直すで確認できます。" : "編集を保存可能な状態で保持しました。接続またはノードの診断を確認してください。");
                if (guiWatch != null) commandMeasurement.guiMs = guiWatch.Elapsed.TotalMilliseconds;
            });
        }

        void Try(Action action)
        {
            try { action(); }
            catch (Exception error) { SetStatus(error.Message); Debug.LogWarning("[NyaForge authoring] " + error); }
        }

        void SetStatus(string text)
        {
            status.text = text ?? "";
            // The footer stays one line on compact windows; keep the complete
            // diagnostic available through the native tooltip for inspection.
            status.tooltip = status.text;
            Debug.Log("[NyaForge authoring] " + status.text);
        }
    }
}
