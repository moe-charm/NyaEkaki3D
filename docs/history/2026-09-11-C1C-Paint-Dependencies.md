# NyaForge 開発タスク

更新: 2026-09-11。UV編集前に下流Paintの影響候補を表示する機能を追加。Core 111件・Player検証が合格。案内表示の画像も確認。

## 開発の入口

作業先: `Z:/TextureVoice_local/git/NyaForge`。Windows先行、macOSは将来。
読む順: このファイル → [開発計画](docs/Development-Plan.md) → [設計v2](docs/NyaForge-Authoring-Design2.md)の対象節 → コード。[文書一覧](docs/README.md)参照。
製品目標は小物の制作・出力を一周し、低ポリ全身キャラ、品質向上へ進むこと。設計v2は製品方針、v1は背景資料。設計中の外部依存・機能は採用済みや実装済みを意味しない。

## 現在地

| 範囲 | 現物と確認状況 |
|---|---|
| C0-R／C1-A | 空project、最大1object、typed graph、共通commandとUndo、native保存、node canvas、static Bake／Bridgeの既存検証記録あり |
| C1-B | polygon/corner ID、面選択・押出し・厚み、Mirror、編集ケージと最終結果、UV投影と島の数値編集まで実装 |
| C1-C Paint基盤 | immutable画像、Image port、Paint node、UV binding、1stroke Undo、画像blob付きnative保存、3D baseColorの既存Player合格レポートあり |
| Paint GUI | 色・半径・不透明度と2D brush、確定時の3D更新を実装。pointerドラッグ、1command／Undo、キャンセル・取得喪失・Escape、native再読込のPlayer検証合格。OS手動受入は未確認 |
| 今後 | texture付き外部出力、layer/mask、UV変更前の依存表示・再投影、一般Material graph、MCP、rig/weight/morphと全身制作 |

保存はstatic writer schema2、graph writer schema3、reader 1/2/3。設計versionと保存schemaは別物。Paintは現在mesh全体に1画像、不透明preview。UV変更時は旧payloadを未解決として保持する。画像付きmeshの既存Bakeは非対応を明示して拒否する。

## 今回の変更と証拠

- `Graph/PaintDependencies.cs`: graphの下流を走査し、保存画像があるPaint段を安定したID順で返す。最終出力外の枝も対象、無関係な枝と未描画Paintは除外。予測評価ではなく保守的な依存候補。文書・画素は変更しない。
- `UnityRuntime/AuthoringWorkbench.UvDependencies.cs`: UV欄の先頭へ候補数・短縮ID・未解決時のUndo／明示割当の案内を表示。共通Core問い合わせを使い、GUIに別の依存計算を持たない。
- Core **111 passed / 0 failed**。`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-e1b5554d64a94abd939b01f6dc40dd6f`。分岐、Mirrorを経由する依存、未描画と無関係な段の除外、文書バイト不変を検証。
- 最新ビルド `Builds/Windows-C1C-PaintDependencies/NyaForge.exe`、`Logs/build-player-20260911-210720-527.log`。GUI案内の表示と文書不変を検査し画像へ出す検証を追加。

- 最新Player pass: `Artifacts/Authoring-20260911-210747-fe9ccc6cc5524b8fb8f004c68bc6dd87/report.json`。`VerifyPaintUi`内で保存済みPaintのID表示と文書不変を検査。`uv-paint-dependencies.png`を目視確認し、1280×800で案内全文とUV投影ボタンが読める。OS手動受入は別途。

以下は明示再割当の記録:

- `Graph/PaintRebinding.cs`、`Commands/PaintRebindOperations.cs`: 旧画像画素を保持してUV/domainだけを明示更新。旧binding・画像hash・入力node・新mesh snapshotを固定し、古いcontextを拒否。再投影ではない。
- `UnityRuntime/AuthoringWorkbench.PaintRebind.cs`: 未解決時だけ割当ボタンと意味を表示。上流が未解決なら操作不可。確定は共通commandで、Undo可能。
- Core **110 passed / 0 failed**。`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-9fc1dcf779bf4fbf8d552c95bd1a3b72`。画素不変、再送、stale拒否、Undo/Redo、native往復、切断入力を検証。
- 最新ビルド `Builds/Windows-C1C-PaintRebind/NyaForge.exe`、成功ログ `Logs/build-player-20260911-210132-916.log`。
- 最新Player pass `Artifacts/Authoring-20260911-210155-0259ba745c7e4e1d8a5e244b8b2d0574/report.json`。PaintGraph検証内でUV変更→GUI割当→Undo/Redo→保存再読込を確認。`paint-rebind.png`を目視確認。

以下は前回のPaint GUI記録:

- `PaintCanvas.cs`: 取得解除通知だけでは描きかけが残る失敗を再現。描画中だけ16ms間隔でpointer所有を確認し、喪失時はキャンセル。PointerCancelも共通の破棄処理へ接続。完了・破棄・Disposeで監視を停止する。
- `AuthoringWorkbench.PaintUiVerification.cs`: 取得・解除をpanelの更新をまたいで検証。Escapeイベント、1stroke／1revision、Undo/Redo、未確定文書の不変性、保存再読込を確認。
- 前回ビルド: `Builds/Windows-C1C-PaintUI/NyaForge.exe`。成功ログ `Logs/build-player-20260911-205535-295.log`。
- 前回Player pass: `Artifacts/Authoring-20260911-205601-a2961a449056473d9f570e000590b1a3/report.json`。既存形状・UV・graph・保存の回帰を含む。
- 同フォルダの `paint-ui.png` を目視確認。1280×800で色・半径・不透明度・キャンバスと3D結果を表示。下の取消ボタンはスクロールで表示する。
- 失敗記録: `Artifacts/Authoring-20260911-205417-a6a7539ee986496bbde64eeb2ef3b0a2/report.json`。取得解除後もIsDrawingが残った。成功するまで失敗を無視せず、画面側の所有監視で修正した。
- Core **108 passed / 0 failed** は前回記録。当時のGUI変更ではCore再実行なし。詳細は[Paint整理前記録](docs/history/2026-09-11-C1C-Paint-Before-Preparation.md)。
- [使い方](docs/Authoring-Quickstart.md)へ2Dブラシ操作と現制限を追記。OSマウス・DPIの手動受入、画像付き作品の外部Unity受け取り、VRChatは未確認。

## 次の作業: 画像・材質付き出力

対象設計: v2 §5.3、§7、§19、§20.2、§21。

1. `BakeStore.cs`／`Persistence/BakeSource.cs`とBridgeのschema1契約を確認。画像付きprofileは別schemaまたは明示profileとして設計し、既存schema1 readerとfixtureを保護する。
2. 明示rebindは初期実装・検証済み。複数Paint段・複数の依存経路と、上流編集を含むGUI回帰を追加する。画像の再投影・rebakeとは呼ばない。
3. 画像・材質付き出力profileとPNG出力を設計し、保存画像と受け取り側の対応を検証する。既存mesh-only Bakeの拒否を黙った画像欠落に変えない。
4. geometryへ同じ模様を保つrebakeは別途snapshotと投射契約を設計・実装する。一般Material graph、layer/mask、texture付き出力の残件を維持する。
5. 色付き小物の制作→保存→画像・材質付き外部出力を一周させ、Evidence／MCP、C2全身制作へ進む。

再検証:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-PaintDependencies -Width 1280 -Height 800
```

次の変更でCoreを触ればCore試験、出力を触れば対応するBridge試験を追加。既存Playerは終了せず別BuildNameを使う。

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚みの品質と自己交差、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。初期予算のUV表示2048面・画像1024角を最終製品要件と読み替えない。

詳細な過去記録は[文書一覧](docs/README.md)から参照。history内の「次」「未実装」は当時の状態であり、再開指示はこのファイルを優先する。




