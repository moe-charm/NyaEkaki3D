# NyaForge 開発タスク

更新: 2026-09-12。材質GUIとPaint経路を接続。Core168件、GUI作成/値編集/3D paint/native往復と高密度回帰に合格。数値欄の配置修正後もGUI回帰合格。材質付き出力は未実装。製品全体の開発は継続中。

## 開発の入口

作業先: `Z:/TextureVoice_local/git/NyaForge`。Windows先行、macOSは将来。
読む順: このファイル → [開発計画](docs/Development-Plan.md) → [設計v2](docs/NyaForge-Authoring-Design2.md)の対象節 → コード。[文書一覧](docs/README.md)参照。
製品目標は小物の制作・出力を一周し、低ポリ全身キャラ、品質向上へ進むこと。設計v2は製品方針、v1は背景資料。設計中の外部依存・機能は採用済みや実装済みを意味しない。

## 現在地

| 範囲 | 現物と確認状況 |
|---|---|
| C0-R／C1-A | 空project、最大1object、typed graph、共通commandとUndo、native保存、node canvas、static Bake／Bridge |
| C1-B | polygon/corner ID、面選択・押出し・厚み、Mirror、編集ケージと最終結果、UV投影と島の数値編集 |
| C1-C Paint | 2D brush、Image port、UV binding、1stroke Undo、native画像保存、3D baseColor、PNG／Surface Bake |
| Layer/mask GUI | 移行・追加・選択・並替・削除・表示・不透明度・名前、mask追加/削除と描画、取消、Undo、保存を検証 |
| Image import | PNG検査・展開・縦横比保持サイズ調整・新layer追加・Undo・保存を検証。Windows pickerの実操作は手動未確認 |
| 3D paint | BVH ray、論理edge/UV連続判定、screen補間、切れた区間の描画、GUI色/mask・仮表示・取消・Undo・保存/出力を実装 |
| 今後 | 3D複数面/細かなseamの精度と操作、一般Material graph、UV再投影、出力identity更新、Evidence/MCP、rig/weight/morphと全身制作 |

保存はstatic writer schema2、graph writer schema3、reader 1/2/3。Paintは現在mesh全体に1合成画像、不透明preview。UV変更時は旧payloadを未解決として保持し、明示rebindで対応を更新する。見た目を保つ再投影ではない。旧mesh-only Bakeは画像を拒否。画像付きmeshは別のSurface Bake profileで出力する。

## 今回の変更と証拠

- `AuthoringWorkbench.Materials` に材質欄を追加。最終出力への作成、登録済み材質選択、sRGB色コード・不透明度・金属感・粗さ・発光・透明方式・cutoffの編集と共通UpdateNodeでの適用を実装。
- 不正値と古いparametersを拒否。無変更適用はcommandなし。hex表示が丸められていても色を変更していなければ元linear RGBを保持する。発光はlinear RGBから最大成分の強さ/正規化色へ再表示するため表示上の組が変わる場合があるが、積は保持する。
- Core `OutputSurfaceConnections` を共通化。Paint画像を引き継ぐ材質作成をReplaceGraphの1commandで行い、Undo1回で旧経路へ戻す。材質経由の3D Paintと、単色材質への後付けPaintもこの経路を使う。
- `MaterialUiVerification` は実GUI操作で画像を保持した作成/Undo/Redo、不正値/無変更適用、材質経由の3D stroke、材質値とmeshの保持、native保存/再読込、材質作成後のPaint追加を検査。
- Core **168 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-354967483a034c1ba5a28a729314332d`。
- 初回GUI/全回帰/DensePaint PASS: `Artifacts/Authoring-20260912-011448-9dc22805a2a548559e28fa754cce0e15/report.json`。画面を確認し、長い数値の幅を確保するため数値欄を縦配置へ修正。未作成時の編集欄は隠す。
- 最終ビルド: `Builds/Windows-C1C-MaterialGuiFinal/NyaForge.exe` / `Logs/build-player-20260912-011622-476.log` はコンパイル成功。最終Player PASS: `Artifacts/Authoring-20260912-011746-6cebd533a92c4135b34f34027fd2d20f/report.json`。最終回はDensePaintなし。material-ui.pngで数値欄を目視確認。
- [材質graph/GUI契約](docs/Material-Graph.md)、[使い方](docs/Authoring-Quickstart.md)、[前段の記録](docs/history/2026-09-12-C1C-Before-Material-Gui.md)参照。history内の「次」は当時の状態。

## 次の作業

1. 材質を保持するBake DTO/profile、保存/読取、Unity Bridge受取とrollbackを実装する。現在のnative材質GUI/表示は接続済み。旧mesh-only/画像Surface profileの互換を保持し、材質付きnative→再読込→出力→受取を一周する。[材質graph](docs/Material-Graph.md)を参照。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-MaterialGuiFinal -Width 1280 -Height 800 -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
