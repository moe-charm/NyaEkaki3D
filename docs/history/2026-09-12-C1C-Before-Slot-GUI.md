# NyaForge 開発タスク

更新: 2026-09-12。複数材質を表示へ接続し、GPU色分け・Undo・仮texture rollbackと既存GUI回帰に合格。複数材質GUI/出力は未接続。製品全体の開発は継続中。

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

- `MaterialSurfaceSet` に表示submeshの材質配列とslotごとの画像所有を集約。MaterialSlotMapから対応を解決し、単一材質/画像なしも同じ所有集合へ統合した。複数材質projectionの一時拒否を除去。
- `ColorUpdate` が集合を一括prepare/commit/rollbackする。材質だけの変更でmesh/rootを再生成しない。rollbackは旧集合の仮textureを保持し、失敗候補だけをDisposeする。
- `ShowPaintPreview(image, authoredSlot)` を追加。複数材質の非null仮画像にはslot指定が必要。nullは全解除。GUIの複数slot Paint操作は未接続。
- 新GPU fixture: sparse 3/9の材質を表赤・裏緑として描画。slot 3だけの青変更、mesh/root再利用、Undoで赤へ復帰、指定slotだけの仮textureとrollback保持を検査。見え方の検査用に前後面を1cm離して同一深度の重なりを避ける。
- 最初の検査は周囲の照明のdielectric反射が混ざり、緑/青成分の閾値を超えて失敗（`Artifacts/Authoring-20260912-020300-9e07c541628b45c7aaa9c65866d2b5ca`）。検証材質をblack-albedo metallicにして発光色の割当だけを比較し、再build/検証で合格。製品shaderは変更していない。
- Unity6000.4.3f1 build成功: `Builds/Windows-C1C-MultiMaterialView2/NyaForge.exe` / `Logs/build-player-20260912-020429-418.log`。
- Windows Player PASS: `Artifacts/Authoring-20260912-020456-29390a4f53c643318c2a2080ee19f326/report.json`。上の新GPU fixtureと旧材質/画像/GUI回帰。`multi-material-front/back/edited/undo.png`を出力。今回はDensePaintなし。
- Core/Bridgeの変更なし。直近Core177件は `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-7bedd32649d341ddad853fca9b3db6d1`。既存Bridge PASSは `Artifacts/BridgeReceiver-20260912-015914-965-c62c50eb5a5f4260b6195551a6c3dd28/bridge-report.json`。今回は再実行していない。
- [部位別材質契約](docs/Material-Slots.md)へ表示/所有の実装範囲を追記。[前段](docs/history/2026-09-12-C1C-Before-MultiMaterial-View.md)を保存。複数材質GUI/Bake/Bridgeはまだ未接続。

## 次の作業

1. [部位別材質契約](docs/Material-Slots.md)のGUIへ進む。slot対応のOutputSurfaceConnections、面割当/材質選択、Paint対象を接続する。画像別binding/失効/保存と、1つのPaintが複数slotに使われるときの同時仮表示を検証する。続いて専用Bake/Bridgeまで接続して部位別材質の一周とする。次に他texture slotとUV再投影へ進む。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-MultiMaterialView2 -Width 1280 -Height 800 -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `Assets/NyaForge/Rendering/`: Playerとreceiverが共有する材質adapter/shader。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
