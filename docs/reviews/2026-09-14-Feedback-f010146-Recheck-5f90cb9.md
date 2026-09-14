# f010146 フィードバック現行HEAD再照合

提示されたレビューは `f010146` 時点のコードを対象としている。現行 `main` `5f90cb9` へ再照合したところ、指摘されたP1 3件とP2 5件は、後続の `b9c51e6` までに実装・回帰済みだったため、本番コードへ同じ修正を重ねていない。

| 指摘 | 現行の対応 | 確認 |
| --- | --- | --- |
| MCP batchで保護objectを編集できる | batch内のobject.select／addを順に追跡し、各変更操作の対象IDを参照保護検査 | `AuthoringWorkbench.ReferenceProtection.cs`、multi-object回帰 |
| 単一object汎用出力が保護を通す | 納品対象を解決してから、単一・複数・GLBの分岐前に保護検査 | `McpExport.cs`、`ProjectActions.cs`、delivery allowlist回帰 |
| Polygon→skinの疎なmaterial slot | 派生時にauthor slotをdense submeshへ正規化し、未使用slotのedgeを除去。元のPolygon graphは疎slotを保持 | `AccessorySkinBindingAdapter.cs`、未使用slotを含むGLB往復 |
| BridgeのMR係数 | glTFのB×metallicFactor、1−G×roughnessFactorをStandard shader用owned textureへ焼き込み、scalarの二重適用を停止 | `SkinnedClothingReceiver.cs`、Bridge semantic MR回帰 |
| Paint原画像の誤差し替え | preview hashだけでなくPaintNodeIdを優先して原画像を解決 | `GlbExportService.cs`、同一preview・別原画像回帰 |
| 複数objectから1個だけ納品 | 明示allowlistの選択集合でobject単位出力を選択 | `ProjectExportService.ExportObject`、allowlist回帰 |
| 高DPIでウィンドウ変更に追従しない | 実ウィンドウでは毎回のGeometryChangedEventでDPI boundsを再計算 | `AuthoringWorkbench.cs` |
| fit対象IDの衣装間持ち越し | active object／target object／mesh domain／revision／state hashを検査し、切替時に測定を無効化 | `AuthoringWorkbench.Attachments.cs`、fit inspection回帰 |

## 現行HEADの再実行

- Core: **506 passed / 0 failed**。対象コマンドは `dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore`。artifactは `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f886ca565d5b4d198da6efd94a0f4e76`。
- Windows Player: PerformanceV39の実RadDollV3候補で、取込→頂点編集→Save/Open→skinned GLB／VRM1→衣装packageを自動一周済み。Player reportと入力モデルはprivate成果物として管理する。
- Unity Bridge: Unity 2022.3.22f1で衣装package受取、BoneId、ownership更新・削除Undo、semantic normal／MRを確認済み。実EditorWindowのマウス／IME／DPI操作と、VRChat内のBuild & Test／見た目は別受入。

このフィードバックを理由に残る作業は、本番コードの再修正ではなく、実EditorWindowでの一衣装操作、実アバター全周fit・貫通・見た目、実VRChat表示の手動受入である。
