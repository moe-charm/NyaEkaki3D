# NyaForge 開発タスク

更新: 2026-09-11。レイヤーGUI専用Player検証合格。マスク描画Coreと共通commandを追加、Core130件合格。mask編集GUIは次の作業。

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
| Layer GUI | 旧Paint移行、追加・選択・並替・削除・表示・不透明度・選択層strokeを実装、専用Player検証合格。mask描画・名前変更GUIは未実装 |
| 今後 | mask GUI・image import・3D paint、UV再投影、一般Material graph、出力identityの更新、MCP、rig/weight/morphと全身制作 |

保存はstatic writer schema2、graph writer schema3、reader 1/2/3。設計versionと保存schemaは別物。Paintは現在mesh全体に1画像、不透明preview。UV変更時は旧payloadを未解決として保持する。旧mesh-only Bakeは画像を拒否。画像付きmeshは別のSurface Bake profileで出力する。

## 最新の変更と証拠

- マスク描画Core: `BrushCoverage`に色とmaskの範囲計算を分離し、`PaintMaskStroke`でlinear coverage8を補間。`PaintLayerChange.MaskStroke`を共通commandへ接続。色画像保持、1stroke/Undo、再送、stale context、native保存を検証。**130 passed / 0 failed**、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-5b74f981a4764b149bd7eddb87574217`。詳細は[Mask Core](docs/history/2026-09-11-C1C-Mask-Core.md)。mask GUIは未実装。
- 最新ビルドは `Builds/Windows-C1C-MaskCore/NyaForge.exe`、成功ログ `Logs/build-player-20260911-220647-244.log`。共有ブラシ変更後のPlayer回帰も合格: `Artifacts/Authoring-20260911-220754-4bab83a8cfbb4e66b40ce3fc6b9d3e71/report.json`。色・レイヤーGUIを含む既存操作を検証。mask GUI試験の合格ではない。

- `PaintLayerChange` / `LayerEditing` / `LayerOperations` に編集意図、stack/UV/domain context、共通commandを分離。追加・削除・移動・表示・不透明度・名前・mask置換・stroke、旧Paintの明示移行を実装。
- `AuthoringWorkbench.PaintLayers.cs` にGUIを分離。2Dは選択層、3D・PNG・Surfaceは合成結果。名前変更・mask編集のGUIは未実装。
- Core **127 passed / 0 failed**。`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0b4133a6933b402fa4b5dc60312b71fa`。移行時の透明画素RGB保持、Undo/Redo、再送、stale contextと不正操作を検証。
- レイヤーGUI導入時ビルド `Builds/Windows-C1C-LayerGui/NyaForge.exe`。成功ログ `Logs/build-player-20260911-220127-259.log`。
- レイヤーGUI導入時Player検証合格: `Artifacts/Authoring-20260911-220151-18e8e66c99764cdc845f00e0d7f129ed/report.json`。`AuthoringWorkbench.LayerUiVerification.cs` が移行・追加・strokeの1command/背景保持・表示/不透明度・並替・削除・空stack操作制御・Undo・選択・保存再読込・合成PNG/Surfaceを検証。数値やdropdown選択はコード設定、ボタンとbrushはUI Toolkit pointerイベント。OS手動受入とは区別する。
- 同ディレクトリ `layer-ui.png` のレイヤー操作欄を目視確認。`layer-ui-project` は2層の編集可能な作品。名前変更・mask描画、表示の快適さの手動評価は残件。

## 前段のGraph統合記録（以下の未実装表記は当時の状態）

- `GraphNode.PaintLayers.cs`とtyped registry: `image.paint-layers` v1を追加。Mesh入力/Image出力。元stackを所有し、immutable nodeの合成画像を表示・出力へ渡す。旧image.paint v1のcodecは維持。
- GraphBinaryCodecはlayer metadataと全画像/mask依存を既存blob callbackで保存。native schema3は変更なし。合成画像だけで作品を保存しない。
- 非表示layerの変更も文書hashへ反映。共通UpdateNode/Undoで元stackへ戻せる。専用layer commandとstroke/migrationは次の作業。
- UV未解決でもstackを保持してnative保存。rebindはstackを保ち、旧image hashだけでなくstack hashも検査する。legacy PaintEditingはlayered nodeを拒否して誤ったflattenを防ぐ。
- Core **124 passed / 0 failed**。`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b0a34c9b043c46818b2e04751db10e4d`。native依存、非表示層とmask、文書/合成hashの区別、Undo、stale rebind、UV未解決保存、空stack、合成Surface出力を検証。
- 最新ビルド `Builds/Windows-C1C-LayerGraph/NyaForge.exe`、成功ログ `Logs/build-player-20260911-214657-512.log`。
- 最新Player pass `Artifacts/Authoring-20260911-214747-0f3c0caeec284e8d9cd58593a2521d4e/report.json`。3layer/opacity/mask/非表示層を `layered-project` へnative保存・再読込し、合成画像と `layer-surface` 出力を比較。`layer-graph.png`を目視確認。
- layer nodeの作成・更新は現在API経由。**GUIはまだ旧1画像のPaint段を選ぶ**。layer選択・追加・mask stroke・旧Paint移行は未実装。実装済みと取り違えない。

asset codec履歴は[Layer Assets](docs/history/2026-09-11-C1C-Layer-Assets.md)。小物のGUIから別Unity描画までの証拠は[小物一周](docs/history/2026-09-11-C1C-Item-Workflow.md)。

## 次の作業: mask編集・名前変更と制作機能の拡張

空stackの操作制御とレイヤーGUI専用検証は完了。mask stroke Coreも実装・検証済み。以下1〜4は実装・専用Player検証済みの段階記録として保持。次はmaskの追加/削除と描画、レイヤー名変更を共通command経由でGUIに接続し、画像とmaskの編集対象・preview・取消・Undoを区別して検証する。その後5と下記造形/C2の残件を進める。

対象: 設計v2 §5.1／5.3／7／20.2。

1. stack hash・layer ID・UV/domain付き編集contextを定義し、layer追加/削除/並替/表示/opacity/mask/strokeを共通commandへ接続。非表示layer変更でも古いcontextを拒否し、再送とUndo/native往復を検証。
2. 旧image.paint v1を1layerへ移す明示migration commandを追加。画像/UV対応を維持し、Undoで元node typeへ戻せるようにする。旧ファイルを無断で書き換えない。
3. GUIのPaint段一覧でlayered nodeを扱い、layer一覧と編集対象を追加。layer更新は同じstackを使う。未確定strokeは一時previewに限定し、1strokeの確定/取消/保存を検証。
4. PNG/Surface/3D表示を同じ合成結果へ接続し、非表示層・maskを保存後も残す。今回の合成出力APIは検証済み。layer GUIからの一周は未実施。
5. image import・3D paint・複数材質・UV再投影、Surfaceの異常入力と受け取り側rollback、出力identity更新を進める。C1のEvidence/MCP・造形残件とC2 rig/weight/morphを維持する。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-MaskCore -Width 1280 -Height 800
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚みの品質と自己交差、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。初期予算のUV表示2048面・画像1024角を最終製品要件と読み替えない。

詳細な過去記録は[文書一覧](docs/README.md)から参照。history内の「次」「未実装」は当時の状態であり、再開指示はこのファイルを優先する。












