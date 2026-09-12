using System;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    public sealed partial class GraphCanvas
    {
        string layoutDirectory = Path.Combine(Application.persistentDataPath, "Workspace", "GraphLayouts");
        string layoutBinding, layoutWarning;

        internal void SetLayoutDirectory(string directory)
        { layoutDirectory = Path.GetFullPath(directory); layoutBinding = null; binding = null; }

        void LoadLayout()
        {
            string key = workspace.InstanceId + ":" + workspace.Document.DocumentId + ":" + (Graph?.GraphId ?? "");
            if (layoutBinding == key) return;
            layoutBinding = key; positions.Clear(); layoutWarning = null;
            if (Graph == null) return;
            try
            {
                foreach (var point in GraphLayoutStore.Load(layoutDirectory, workspace.Document.DocumentId, Graph.GraphId))
                    positions[LayoutKey(point.Key)] = new Vector2(point.Value.X, point.Value.Y);
            }
            catch (Exception e)
            {
                layoutWarning = "配置を読み込めません。標準配置で表示します。";
                Debug.LogWarning("[NyaForge layout] " + e.Message);
            }
        }

        public void SaveLayout()
        {
            if (workspace == null || Graph == null) return;
            try
            {
                var points = Graph.Nodes.Keys.Where(id => positions.ContainsKey(LayoutKey(id))).ToDictionary(id => id,
                    id => new CanvasPoint(positions[LayoutKey(id)].x, positions[LayoutKey(id)].y));
                GraphLayoutStore.Save(layoutDirectory, workspace.Document.DocumentId, Graph.GraphId, points);
                layoutWarning = null;
            }
            catch (Exception e)
            {
                layoutWarning = "配置を保存できません。制作データの保存状態とは別です。";
                Debug.LogWarning("[NyaForge layout] " + e.Message);
            }
            ShowState();
        }
    }
}
