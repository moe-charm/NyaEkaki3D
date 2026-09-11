using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Viewer.Contracts
{
    public static class Validation
    {
        public const int Schema = 1;
        static void Require(bool condition, string code, string message) { if (!condition) throw new ContractException(code, message); }
        public static bool Finite(double n) => !double.IsNaN(n) && !double.IsInfinity(n);
        static void Id(string id) => Require(id != null && Regex.IsMatch(id, @"^[A-Za-z0-9][A-Za-z0-9_.:-]{0,159}$"), "ID_INVALID", "IDが不正です: " + id);
        static void Hash(string hash) => Require(hash != null && Regex.IsMatch(hash, "^[a-f0-9]{64}$"), "HASH_INVALID", "hash形式が不正です。");
        static void Unique(IEnumerable<string> ids) { var a = ids.ToArray(); foreach (var id in a) Id(id); Require(a.Distinct(StringComparer.Ordinal).Count() == a.Length, "ID_DUPLICATE", "IDが重複しています。"); }
        public static void Manifest(PackManifest m, string expectedToken = null)
        {
            Require(m.schemaVersion == Schema, "PACK_SCHEMA_UNSUPPORTED", "パックの保存形式に対応していません。");
            Id(m.packId); Id(m.revision); Id(m.avatarId); Id(m.rigProfileId);
            Require(m.toolchain.buildTarget == "StandaloneWindows64" && m.toolchain.architecture == "x86_64" && m.toolchain.graphicsApi == "Direct3D11", "PACK_TARGET_MISMATCH", "Windows用パックを選んでください。");
            Require(m.toolchain.renderPipeline == "BuiltIn" && m.toolchain.runtimeContractVersion == 1, "PACK_TOOLCHAIN_MISMATCH", "このアプリに対応する版で更新してください。");
            if (expectedToken != null) Require(m.toolchain.compatibilityToken == expectedToken, "PACK_TOOLCHAIN_MISMATCH", "アプリとパックのバージョンが一致しません。");
            Hash(m.toolchain.packageLockSha256);
            Unique(m.bundles.Select(x => x.id)); Unique(m.renderers.Select(x => x.rendererId)); Unique(m.clips.Select(x => x.clipId));
            Require(m.bundles.Length > 0 && m.renderers.Length > 0 && m.clips.Length > 0, "PACK_EMPTY", "パックに表示データがありません。");
            var bundles = m.bundles.ToDictionary(x => x.id, StringComparer.Ordinal);
            foreach (var b in m.bundles)
            {
                Hash(b.sha256); Require(b.sizeBytes > 0, "PACK_INCOMPLETE", "パックが空です。");
                Require(b.dependsOn.Distinct().Count() == b.dependsOn.Length && b.dependsOn.All(bundles.ContainsKey), "PACK_DEPENDENCY_INVALID", "依存パックが不正です。");
            }
            var visiting = new HashSet<string>(); var complete = new HashSet<string>();
            void Visit(string id)
            {
                if (complete.Contains(id)) return;
                Require(visiting.Add(id), "PACK_DEPENDENCY_CYCLE", "パックの依存が循環しています。");
                foreach (var child in bundles[id].dependsOn) Visit(child);
                visiting.Remove(id); complete.Add(id);
            }
            foreach (var id in bundles.Keys) Visit(id);
            Require(bundles.ContainsKey(m.avatarPrefab.bundleId) && !string.IsNullOrEmpty(m.avatarPrefab.assetName), "PACK_PREFAB_MISSING", "身体データがありません。");
            var rs = m.renderers.ToDictionary(x => x.rendererId, StringComparer.Ordinal);
            Require(m.renderers.Select(x => x.path).Distinct().Count() == m.renderers.Length, "ID_DUPLICATE", "表示パスが重複しています。");
            foreach (var r in m.renderers)
                Require(!string.IsNullOrEmpty(r.path) && (r.rendererType == "SkinnedMeshRenderer" || r.rendererType == "MeshRenderer"), "RENDERER_INVALID", "表示データの型が不正です。");
            var keys = new HashSet<(string, string)>();
            foreach (var b in m.morphBindings)
            {
                Require(keys.Add((b.rendererId, b.shapeName)), "ID_DUPLICATE", "シェイプキーが重複しています。");
                Require(rs.ContainsKey(b.rendererId) && rs[b.rendererId].rendererType == "SkinnedMeshRenderer" && !string.IsNullOrEmpty(b.shapeName), "MORPH_BINDING_INVALID", "シェイプキーの対応が不正です。");
                Require(Finite(b.minWeight) && Finite(b.maxWeight) && Finite(b.defaultWeight) && b.minWeight <= b.defaultWeight && b.defaultWeight <= b.maxWeight, "MORPH_RANGE_INVALID", "シェイプキーの範囲が不正です。");
            }
            Require(m.clips.Any(c => c.clipId == m.defaultClipId), "REQUIRED_BINDING_MISSING", "確認ポーズがありません。");
            foreach (var c in m.clips) Require(bundles.ContainsKey(c.bundleId) && Finite(c.durationSeconds) && c.durationSeconds > 0 && Finite(c.sampleRate) && c.sampleRate > 0, "CLIP_INVALID", "確認ポーズの時間が不正です。");
        }
        public static void Session(SessionDocument s)
        {
            Require(s.schemaVersion == Schema, "SESSION_SCHEMA_UNSUPPORTED", "この保存形式には対応していません。");
            Id(s.pack.packId); Id(s.pack.revision); Id(s.avatarId); Id(s.rigProfileId); Hash(s.pack.manifestSha256); Id(s.motion.clipId);
            Require(!string.IsNullOrWhiteSpace(s.pack.manifestPath), "PACK_PATH_INVALID", "パックの保存場所がありません。");
            Unique(s.visibilityOverrides.Select(x => x.rendererId));
            var keys = new HashSet<(string, string)>();
            foreach (var v in s.morphOverrides) Require(keys.Add((v.rendererId, v.shapeName)) && Finite(v.weight), "MORPH_INVALID", "手動シェイプキー値が不正です。");
            var unresolved = new HashSet<(string, string)>();
            foreach (var u in s.unresolvedOverrides) Require(u.kind == "morph" && Finite(u.weight) && unresolved.Add((u.rendererId, u.shapeName)) && (u.reasonCode == "BINDING_MISSING" || u.reasonCode == "WEIGHT_OUT_OF_RANGE"), "MORPH_INVALID", "保留シェイプキーが不正です。");
            Require(Finite(s.motion.timeSeconds) && s.motion.timeSeconds >= 0 && Finite(s.motion.speed) && s.motion.speed >= .1 && s.motion.speed <= 2, "MOTION_INVALID", "再生時刻または速度が不正です。");
            var c = s.camera;
            Require(c.targetMeters.Length == 3 && c.orientationQuat.Length == 4 && c.targetMeters.All(x => Finite(x)) && c.orientationQuat.All(x => Finite(x)), "CAMERA_INVALID", "カメラの座標が不正です。");
            Require(Math.Abs(c.orientationQuat.Sum(x => x * x) - 1) < .01 && Finite(c.distanceMeters) && c.distanceMeters > 0 && Finite(c.verticalFovDegrees) && c.verticalFovDegrees >= 5 && c.verticalFovDegrees <= 120, "CAMERA_INVALID", "カメラの向きまたは距離が不正です。");
            Require(new[] { "original", "unlit", "bodyDiagnostic" }.Contains(s.preview.mode) && new[] { "studio", "outdoor", "dark" }.Contains(s.preview.lightPresetId), "PREVIEW_INVALID", "表示モードが不正です。");
        }
        public static List<string> Reconcile(SessionDocument s, PackManifest next, PackManifest previous = null)
        {
            Require(s.pack.packId == next.packId && s.avatarId == next.avatarId && s.rigProfileId == next.rigProfileId, "RIG_INCOMPATIBLE", "対象の身体・骨格が一致しません。");
            var renderers = next.renderers.Select(r => r.rendererId).ToHashSet();
            if (previous != null)
            {
                Require(previous.renderers.Where(r => r.required).All(r => renderers.Contains(r.rendererId)), "REQUIRED_BINDING_MISSING", "必須パーツが更新先にありません。");
                Require(previous.clips.Where(c => c.required).All(c => next.clips.Any(n => n.clipId == c.clipId)), "REQUIRED_BINDING_MISSING", "必須ポーズが更新先にありません。");
                Require(previous.morphBindings.Where(b => b.required).All(b => next.morphBindings.Any(n => n.rendererId == b.rendererId && n.shapeName == b.shapeName)), "REQUIRED_BINDING_MISSING", "必須シェイプキーが更新先にありません。");
            }
            Require(s.visibilityOverrides.All(x => renderers.Contains(x.rendererId)), "REQUIRED_BINDING_MISSING", "表示中のパーツが更新先にありません。");
            var clip = next.clips.FirstOrDefault(c => c.clipId == s.motion.clipId);
            Require(clip != null, "REQUIRED_BINDING_MISSING", "選択中のポーズがありません。");
            var messages = new List<string>();
            if (s.motion.timeSeconds > clip.durationSeconds) { s.motion.timeSeconds = clip.durationSeconds; messages.Add("ポーズの終端へ時刻を調整しました。"); }
            var active = new List<MorphOverride>(); var waiting = new List<UnresolvedOverride>();
            var previouslyWaiting = s.unresolvedOverrides;
            var all = s.morphOverrides.Concat(s.unresolvedOverrides.Where(u => !s.morphOverrides.Any(v => v.rendererId == u.rendererId && v.shapeName == u.shapeName)).Select(u => new MorphOverride { rendererId = u.rendererId, shapeName = u.shapeName, weight = u.weight }));
            foreach (var v in all)
            {
                var b = next.morphBindings.FirstOrDefault(x => x.rendererId == v.rendererId && x.shapeName == v.shapeName);
                if (b != null && v.weight >= b.minWeight && v.weight <= b.maxWeight) active.Add(v);
                else waiting.Add(new UnresolvedOverride { kind = "morph", rendererId = v.rendererId, shapeName = v.shapeName, weight = v.weight, reasonCode = b == null ? "BINDING_MISSING" : "WEIGHT_OUT_OF_RANGE" });
            }
            s.morphOverrides = active.ToArray(); s.unresolvedOverrides = waiting.ToArray();
            foreach (var prior in previouslyWaiting)
            {
                if (active.Any(v => v.rendererId == prior.rendererId && v.shapeName == prior.shapeName))
                    messages.Add("保留を解消しました: " + prior.rendererId + " / " + prior.shapeName);
            }
            if (waiting.Count > 0) messages.Add(waiting.Count + "件の調整を保留しました。");
            return messages;
        }
    }
}
