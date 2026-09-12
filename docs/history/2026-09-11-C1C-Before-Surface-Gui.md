# NyaForge 開発タスク

更新: 2026-09-11。切れたUV線分を1gestureとして描くCore/commandを追加。Core140件・Player API/既存GUI回帰合格。3D入力からの区間生成とブラシGUIは未接続。製品全体の開発は継続中。

## 開発の入口

作業先: `Z:/TextureVoice_local/git/NyaForge`。Windows先行、macOSは将来。
読む順: このファイル → [開発計画](docs/Development-Plan.md) → [設計v2](docs/NyaForge-Authoring-Design2.md)の対象節 → コード。[文書一覧](docs/README.md)参照。
製品目標は小物の制作・出力を一周し、低ポリ全身キャラ、品質向上へ進むこと。設計v2は製品方針、v1は背景資料。設計中の外部依存・機能は採用済みや実装済みを意味しない。

## 現在地

| 範囲 | 現物と確認状況 |
|---|---|
| C0-R／C1-A | 空project、最大1object、typed graph、共通commandとUndo、native保存、node canvas、static Bake／Bridge |
| C1-B | polygon/corner ID、面選択・押出し・厚み、Mirror、編集ケージと最終結果、UV投影と島の数値編集 |
| C1-C Paint | immutable画像、Image port、UV binding、2D brush、1stroke Undo、native画像保存、3D baseColor、PNG／Surface Bake |
| Layer GUI | 旧Paint移行、追加・選択・並替・削除・表示・不透明度・選択層stroke、空stack操作制御。専用Player検証合格 |
| Mask GUI | 名前変更、白mask追加/削除、画像/mask切替、隠す/表示する筆、強さ、仮表示、取消、Undo、保存・出力を検証 |
| Image import | PNG検査・展開・縦横比保持サイズ調整・新layer追加・Undo・保存を検証。Windows picker実装、ネイティブダイアログ手動操作は未確認 |
| 今後 | 3D paint、一般Material graph、UV再投影、出力identity更新、Evidence/MCP、rig/weight/morphと全身制作 |

保存はstatic writer schema2、graph writer schema3、reader 1/2/3。設計versionと保存schemaは別物。Paintは現在mesh全体に1合成画像、不透明preview。UV変更時は旧payloadを未解決として保持し、明示rebindで対応を更新する。見た目を保つ再投影ではない。旧mesh-only Bakeは画像を拒否。画像付きmeshは別のSurface Bake profileで出力する。

## 今回の変更と証拠

- `PaintStrokePath` / `ApplyPaths`: 区間境界を保持し、色/maskとも全区間のcoverageを合成してから1度だけ適用。1024点/16M pixel visitsを全区間で共有。layer共通commandの1gesture Undo・再送・保存へ接続。[履歴](docs/history/2026-09-11-C1C-Stroke-Paths.md)。
- Core **140 passed / 0 failed**。`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-67c3bacd8d034729b0d8c430017792bb`。区間間の非描画、重複opacity、入力コピー、共通budget、Undo/Redo/native保存、境界/strengthを変えた再送拒否を確認。
- 最新ビルド `Builds/Windows-C1C-StrokePaths/NyaForge.exe`、成功ログ `Logs/build-player-20260911-224015-129.log`。
- 最新Player合格 `Artifacts/Authoring-20260911-224116-2c90bccb17b7496bb84a9f26f8572caa/report.json`。分割UV gesture APIの色/mask確認と既存GUI回帰。3Dブラシ入力は未接続。

## 前段: ray/UV探索の検証

- `SurfacePaintMesh` / `SurfaceRayGeometry`: immutable mesh/transformのBVH、倍精度ray交差、三角形ごとのUV補間、avatar距離、表裏、UV範囲外でも手前の面を返す遮蔽を実装。契約と次の接続は[3Dペイント](docs/Surface-Paint-Development.md)。
- Core **136 passed / 0 failed**。`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-1f14959591f7479689602b2c1e1d7fa9`。scale 1/100/1e-6・極小三角形・距離・UV seam・前後の面・片面cull・不正ray/UV欠落を確認。
- ray基盤ビルド `Builds/Windows-C1C-SurfaceRay/NyaForge.exe`。成功ログ `Logs/build-player-20260911-223334-173.log`。
- ray基盤Player合格 `Artifacts/Authoring-20260911-223414-cf318fd4b46f416caa848d8539ed8a89/report.json`。`SurfacePaintVerification` のscale 1/100 ray API確認と既存GUI検証。**3Dで塗れるGUIの合格ではない**。

## 前段: PNG取り込みの検証

- `PaintPngInput` の入力検査、`PaintPngImporter` のhost decode、`PaintImageFit` のUnity非依存サイズ調整、`AuthoringWorkbench.PaintImport` のGUI/commandを分離。画像正本は既存native blobへ保存する。契約は[画像取り込み](docs/Paint-Image-Import.md)。
- Windows pickerを `UnityRuntime/Platform/WindowsFilePicker` に共有化。旧ViewerのJSON pickerは同じfilter/title/拡張子をwrapperから渡す。PNG選択中に文書が変わると追加を取り消す。
- Core **132 passed / 0 failed**。`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f6a2a29c355f42eabfaa0a7674bda4b1`。
- PNG取り込み導入時ビルド `Builds/Windows-C1C-ImageImport/NyaForge.exe`。成功ログ `Logs/build-player-20260911-222502-979.log`。
- PNG取り込み導入時Player合格 `Artifacts/Authoring-20260911-222627-a6c1e6ae61b947cea40092b09fc63413/report.json`。RGBAと上下方向、透明画素のRGB保持、寸法違い/欠損/破損の文書変更なし、サイズ調整と新層選択、1revision/Undo/Redo、元画像を壊した後のnative再読込、Surface合成一致を確認。
- `image-import.png` でチェック表示と説明の見切れ修正を目視確認。`image-import-project` は編集可能な保存作品。検証用の元PNGは独立保存の試験で意図的に壊してある。
- GUI試験はパスをコード設定し、追加ボタンへpointerイベントを送る。Explorerネイティブダイアログの手動操作や全PNG変種対応を確認したと扱わない。

## 前段: マスクGUIの検証

- `AuthoringWorkbench.PaintMasks.cs` にGUIを分離。名前・mask設定を折り畳み、描画対象と隠す/表示する筆を追加。確定処理は共通commandを通る。
- `PaintCanvas` は描画開始時にpreview処理を固定。maskの仮表示は `PaintMaskStroke`、表示変換は `PaintMaskDisplay`。表示用グレースケールをmask正本として保存しない。
- Core **130 passed / 0 failed**。`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-283d7d714ac6466b859966b8a892200d`。
- マスクGUI導入時ビルド `Builds/Windows-C1C-MaskGui/NyaForge.exe`。成功ログ `Logs/build-player-20260911-221248-890.log`。
- マスクGUI導入時Player合格 `Artifacts/Authoring-20260911-221320-55683d9b8b384ffd9e1a1be5b6daba3b/report.json`。名前変更/Undo、mask追加・選択、strength .5のpreview/commit一致、色画像保持、1stroke Undo/Redo、Escape/対象切替取消、白で復元、mask削除/Undo、native再読込、合成PNG/Surfaceを検証。
- 同ディレクトリ `mask-ui.png` は白黒のmaskと合成3Dの差を目視確認。`mask-ui-project` は2層・mask付き作品、`mask-surface` は対応出力。
- 操作試験はUI Toolkit pointerイベントを使用。数値とdropdown値はコード設定。OS手動受入や商品品質の完成を意味しない。Coreのmask値128とPlayer texture生バイト128も比較した。

前段の詳細は[今回整理前](docs/history/2026-09-11-C1C-Before-Mask-Gui.md)、[Mask Core](docs/history/2026-09-11-C1C-Mask-Core.md)、[Layer Assets](docs/history/2026-09-11-C1C-Layer-Assets.md)、[小物一周](docs/history/2026-09-11-C1C-Item-Workflow.md)。history内の「次」「未実装」は当時の状態。

## 次の作業

対象: 設計v2 §5.1／5.3／7／20.2と開発計画C1-C。

1. PNG importは実装・専用Player検証済み。palette/gray/16bit/interlaceの代表画像、ネイティブpicker手動操作、ICC変換、他形式/1024超の入力は残件として保持する。詳細は画像取り込み契約。次の実装は2。
2. 3D paint: ray/UV探索と切れたUV線分の1gesture描画は実装済み。次は可視面のhit列から区間を生成し、3D brush GUI/仮表示へ接続する。UV seam・裏面・途中の背景・gesture取消・Undo・保存を検証する。2D brush基盤を流用し、入力と文書操作を分離する。詳細は3Dペイント開発契約。
3. 一般Material graph、複数材質、UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。mask/画像の大きなstackの性能改善、手動操作の快適さも未受入。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-StrokePaths -Width 1280 -Height 800
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
