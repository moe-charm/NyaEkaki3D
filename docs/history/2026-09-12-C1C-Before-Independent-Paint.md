# NyaForge 開発タスク

更新: 2026-09-12。共有Paintの複数slot 3D描画をGUIへ接続。Playerで両仮画像・確定・Undo/nativeと旧GUI回帰に合格。別Paint/遮蔽・新Paint追加・出力は継続中。

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

保存はstatic writer schema2、graph writer schema3、reader 1/2/3。Paintは現在mesh全体に1合成画像。材質未割当は不透明preview、標準材質は3alpha modeを選択できる。UV変更時は旧payloadを未解決として保持し、明示rebindで対応を更新する。見た目を保つ再投影ではない。旧mesh-only Bakeは画像を拒否。画像付きmeshは別のSurface Bake profileで出力する。

## 今回の変更と証拠

- `RefreshSurfacePaint` が選択Paintにつながる複数slotを認識。SurfacePreparationを選択Paintの画像/UV経路へ変更し、Output.BaseColor前提を除去。
- stroke開始時に対象slotとMaterialSlotMapを固定。raycastの最前面hitが対象外slotなら区間を切り、奥へ透過しない。仮表示を複数slot APIへ送り、確定は既存layer commandで1回、取消は全仮画像解除。
- MaterialUiVerificationにlayer化した押出し小物→面分割→共有Paintの2部位描画を追加。両texture仮表示、両image hash更新、Undo/Redo、native保存/再読込を検査。
- Unity6000.4.3f1 build成功: `Builds/Windows-C1C-SlotPaintGUI/NyaForge.exe` / `Logs/build-player-20260912-022637-345.log`。
- Windows Player PASS: `Artifacts/Authoring-20260912-022710-47bda56413b14966b18a9add03a67d7d/report.json`。共有2部位の実pointer stroke、既存GUI・準備競合・材質GPUを回帰。`shared-slot-paint` にnative出力。今回DensePaintなし。
- Core未変更。直近181 passed / 0 failedは `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-8b0cc807bb4a497f8c077fcef6550e31`。今回は再実行せず。Bridgeも未変更。
- 対象外slot遮蔽は実装したが今回の新fixtureは両slot共有画像のため拒否枝の描画検証は未実施。別Paint・mask・部位別新Paint追加GUIと複数材質Bakeが残る。
- [部位別材質契約](docs/Material-Slots.md)へGUI接続を追記。[前段](docs/history/2026-09-12-C1C-Before-Slot-Paint-GUI.md)を保存。

## 次の作業

1. 部位別に新しいPaintを追加するGUIをslot経路へ接続し、別Paintの独立描画と対象外面の遮蔽を実際のstrokeで検証する。mask・共有画像の取消/保存も確認し、複数材質の専用Bake/Bridgeへ進む。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-SlotPaintGUI -Width 1280 -Height 800 -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `Assets/NyaForge/Rendering/`: Playerとreceiverが共有する材質adapter/shader。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
