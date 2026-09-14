# 9855d43フィードバックの実装反映確認（2026-09-14）

提示された`9855d43`基準のP1 3件／P2 5件を、現行作業ツリーへ反映した。今回はレビュー記録だけでなく、再現条件に対応する実装と回帰を追加している。

| 指摘 | 対応 | 根拠 |
|---|---|---|
| MCPの1バッチで衣装から保護アバターへ選択を切り替えて編集できる | 操作順にactive objectを追跡し、保護対象への変更操作を拒否。Undo/Redoとobject.selectは許可 | `AuthoringWorkbench.ReferenceProtection.cs`、`MultiObjectVerification`のMCP batch回帰 |
| generic single-object出力が保護対象を通す | GLB／Bakeの全経路で納品対象を先に解決し、参照保護を検査。複数作品で1対象だけ許可された場合は選択objectだけを書き出す | `AuthoringWorkbench.McpExport.cs`、`AuthoringWorkbench.ProjectActions.cs`、`ProjectExportService.ExportObject` |
| sparse material slotがskin派生・Playerでずれる | Polygonのauthor slotを派生MeshSourceのdense submeshへ正規化し、未使用slotのedgeを除去。元の編集グラフは保持 | `AccessorySkinBindingAdapter.cs`、2 slot／未使用slot 9のCore GLB回帰 |
| Unity BridgeのMR係数が無視される | glTFのB×metallicFactor、1−G×roughnessFactorをStandard shader用テクスチャへ焼き込み、scalarは二重適用しない | `SkinnedClothingReceiver.cs`、Unity 2022.3.22f1 Bridge semantic texture回帰 |
| 同じpreview画像を持つPaintのoriginalが入れ替わる | `PaintNodeId`をGraphImageValueへ渡し、original sourceをnode identityで優先解決。preview hashは互換fallback | `PaintEvaluation.cs`、`GlbExportService.cs`の異なるoriginal回帰 |
| multi-objectでallowlist 1対象が拒否される | project object数ではなく納品対象数で分岐 | `McpExport.cs`／`ProjectActions.cs` |
| 高DPI root boundsが初回値で固定される | 通常UIでGeometryChangedEventごとに現在ScreenサイズとDPIからroot boundsを再計算 | `AuthoringWorkbench.cs` |
| fit target IDが別衣装へ持ち越される | object selection変更時にfit inspection／選択面／選択頂点をクリアし、所有objectを記録 | `AuthoringWorkbench.Objects.cs` |

## 検証

- Core: **506 passed / 0 failed**。sparse slotの2 slot／未使用slot、Paint node identity、既存GLB・skin・材質回帰を含む。artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-37236934d9df42168eda9df79fc396e2`。
- Windows Player build: **PASS**。Unity `6000.4.3f1`、`Builds/PerformanceV31/NyaForge.exe`および追加MCP回帰を含む`Builds/PerformanceV32/NyaForge.exe`。
- Authoring report: V31の自動suiteは`report.json`で`passed: true`（`Artifacts/Authoring-20260914-092150-9326992d985d426caff41384cb0d926b/report.json`）。検証終了後のPlayer終了待ちがタイムアウトしたため、ランナーの終了コードはPASS扱いにしていない。V32の再実行は終了待ち時間内にreport生成まで到達せず、追加回帰の実行完了は未確認。
- Unity Bridge: **PASS**。Unity `2022.3.22f1`でsemantic normal/MR、材質・複数材質・clothing packageを確認。`Artifacts/BridgeReceiver-20260914-092607-762-920be59b4a4e4816aaf92bbb8b5a0ce7/bridge-report.json`、`Artifacts/BridgeReceiver-20260914-092641-820-eeded8a4b26943ad8593f58cb6d595f1/bridge-report.json`。

実EditorWindowのマウス／IME／Explorer操作、実アバター全周fit・貫通ゼロ・見た目、VRChat Build & Test／実機表示は未受入。`private/`のアバター・SDK素材は公開ツリーへ追加していない。
