# f010146フィードバックの現行HEAD再照合（2026-09-14）

提示された`f010146`基準のP1 3件／P2 5件を、現行`main` **`f3b3d29`**へ再照合した。今回のフィードバックが「疎なmaterial slot」と「Unity BridgeのMR係数」を未修正としている点は、現行HEADでは後続実装と回帰により解消済みで、同じ修正を重複適用していない。

| 指摘 | 現行HEADの状態 | 根拠 |
|---|---|---|
| MCP batchで保護objectを編集できる | 操作順にactive objectを追跡し、保護対象への変更を拒否 | `AuthoringWorkbench.ReferenceProtection.cs`、`MultiObjectVerification` |
| 単一object汎用出力が保護を通す | 納品対象解決後、全出力形式の分岐前に保護を検査 | `AuthoringWorkbench.DeliveryAllowlist.cs`、`McpExport.cs`、`ProjectActions.cs` |
| Polygon→skinの疎slotがPlayerでずれる | 派生時にauthor slotをdense submeshへ正規化し、未使用edgeを除去。Playerはderived graphの0始まりslotを使用 | `AccessorySkinBindingAdapter.cs`、`MaterialSurfaceSet.cs`、`AccessorySkinBindingTests.cs`、`MultiMaterialProjectionVerification.cs` |
| Unity BridgeのMR係数が無視される | `B×metallicFactor` と `1−G×roughnessFactor`をStandard shader用owned textureへ焼き込み、scalar二重適用を避ける | `SkinnedClothingReceiver.cs`、`BridgeBatch.VerifySemanticTexturePackage` |
| Paint原画像が別nodeへ誤差し替えになる | `PaintNodeId`を優先し、preview hashは互換fallbackに限定 | `PaintEvaluation.cs`、`GlbExportService.cs` |
| allowlist 1対象が拒否される | project全体ではなく選択後の納品対象数でsingle／multiを分岐 | `McpExport.cs`、`ProjectActions.cs` |
| 高DPIでroot boundsが固定される | `GeometryChangedEvent`ごとに現在の画面サイズ・DPIから再計算 | `AuthoringWorkbench.cs` |
| fit対象IDが別衣装へ持ち越される | object／mesh domainの所有を記録し、対象変更時にfit選択を解除 | `AuthoringWorkbench.Attachments.cs`、`AuthoringWorkbench.Objects.cs` |

## 検証

- Core: **506 passed / 0 failed**。artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-adbcb4a7267c48eda030ca0e8ccf6eb4`。疎slotのPolygon→skin→GLB往復、未使用slot、Paint node identity、材質回帰を含む。
- Unity Bridge: **PASS**。Unity `2022.3.22f1`、report `Artifacts/BridgeReceiver-20260914-103539-671-7874a6c7ed7948e0a48a1ba6fca0536b/bridge-report.json`。semantic normal／MRのStandard shader変換、BoneId衣装受け取り、更新・Undoを確認。

今回の自動検証は、実EditorWindowのマウス／IME／Explorer操作、実アバター全周fit・貫通ゼロ・見た目、VRChat Build & Test／実機表示を受入したものではない。実モデルの軽量性も同一条件のheap censusが不足しており、合格扱いにしない。
