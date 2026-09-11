using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Viewer.Contracts
{
    public sealed class RendererBindingMap
    {
        public int schemaVersion;
        public string rigProfileId;
        public RendererBinding[] renderers;
    }
    public sealed class RendererBinding
    {
        public string rendererId, sourcePath, rendererType, category, displayName;
        public bool required;
    }
    public static class RendererBindings
    {
        // Paths locate source objects; only the explicit rendererId is identity.
        // Never infer a rename from display name, mesh hash, order or vertex count.
        public static Dictionary<string, RendererBinding> Resolve(RendererBindingMap map, string rigProfileId, IReadOnlyDictionary<string, string> actual)
        {
            void Require(bool ok, string message)
            { if (!ok) throw new ContractException("BINDING_MAP_INVALID", message); }
            Require(map != null && map.schemaVersion == 1 && map.rigProfileId == rigProfileId && map.renderers != null, "対応表の版・骨格を確認してください。");
            var paths = new HashSet<string>(StringComparer.Ordinal);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var resolved = new Dictionary<string, RendererBinding>(StringComparer.Ordinal);
            foreach (var row in map.renderers)
            {
                Require(row != null && row.rendererId != null && Regex.IsMatch(row.rendererId, @"\A[A-Za-z0-9][A-Za-z0-9_.:-]{0,159}\z") && ids.Add(row.rendererId), "対応表のIDが不正・重複しています。");
                Require(!string.IsNullOrWhiteSpace(row.sourcePath) && paths.Add(row.sourcePath), "対応表のsourcePathが空・重複しています。");
                Require(row.rendererType == "SkinnedMeshRenderer" || row.rendererType == "MeshRenderer", "対応表のrenderer型が不正です。");
                Require(row.category == "body" || row.category == "outfit" || row.category == "accessory", "対応表のcategoryが不正です。");
                Require(!string.IsNullOrWhiteSpace(row.displayName), "対応表の表示名が空です。");
                if (!actual.TryGetValue(row.sourcePath, out var type))
                {
                    Require(!row.required, "必須パーツがありません。改名時は同じIDのsourcePathを更新してください: " + row.rendererId + " / " + row.sourcePath);
                    continue;
                }
                Require(type == row.rendererType, "パーツ型が対応表と異なります: " + row.sourcePath);
                resolved.Add(row.sourcePath, row);
            }
            foreach (var path in actual.Keys)
                Require(resolved.ContainsKey(path), "未登録パーツです。対応表へ明示的に登録してください: " + path);
            return resolved;
        }
    }
}
