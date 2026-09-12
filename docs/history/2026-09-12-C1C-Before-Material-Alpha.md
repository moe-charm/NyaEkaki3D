# NyaForge 開発タスク

更新: 2026-09-12。材質付きmeshの法線欠落を共有描画モジュールで補完。画像なし3alpha modeのreceiver保存・描画とPlayer回帰に合格。製品全体の開発は継続中。

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

- `DisplayMeshNormals.Ensure` を共有Renderingモジュールへ追加。Player表示と材質Bridgeの派生Unity Meshに、法線属性がない場合だけ法線を補う。既存法線、Core mesh、Bake blobは変更しない。旧mesh-only/Surfaceの属性保持も維持。
- 再計算はUnityの既存頂点分割を使う。weldやsmooth-groupの推測はしないため、UV seam等がhard edgeになる場合はある。
- receiverに画像なし・法線なし平面の3alpha mode fixtureを追加。期待法線を三角形の既知windingから-Zと照合し、保存したMesh/Prefabの読戻し、textureなし、shader/cutoff/RGBA書込み、source hash保持、照明付き描画を確認。
- 独立Unity 2022.3.22f1 Built-In / Linear PASS: `Artifacts/BridgeReceiver-20260912-014325-420-42839d70b0d546749661b66547f2c331/bridge-report.json`。旧scale1/100・Surface、GUI材質出力、注入失敗rollbackも回帰。`no-image-Blend.png`で照明付き平面の描画を目視。これは透明率やcutoff境界の数値的な画素一致を証明しない。
- 最初のreceiver試験は新fixtureのUUID書式誤りで失敗（`Artifacts/BridgeReceiver-20260912-014300-253-6c37e06ac844436cabc086088697d918`）。既存のcanonical D形式へ修正し、上の新規receiverで合格。
- Unity 6000.4.3f1 build成功: `Builds/Windows-C1C-MaterialNormals/NyaForge.exe`、`Logs/build-player-20260912-014345-875.log`。
- Windows Player PASS: `Artifacts/Authoring-20260912-014430-22062cc925e64a3d950ee117907409c5/report.json`。共有helperを含む本体、材質GPU/GUI、出力、既存GUI・PNG30形式・準備競合を回帰。今回DensePaintなし。
- Coreの変更なし。直近Core173 passed / 0 failedは `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-3809aa9c8b91431bade870d46a5e1cb1`。今回のUnity回帰と区別する。
- [材質Bake契約](docs/Material-Bake.md)、[前段の記録](docs/history/2026-09-12-C1C-Before-Material-Normals.md)参照。
## 次の作業

1. 標準材質のreceiverで透明率/cutoff境界を画素比較し、画像alphaとtint alphaの合成を確認する。その後は異なる複数材質・他texture slot、UV再投影へ進む。生成法線の曲面/seam品質とURPは残件として保持。[材質Bake](docs/Material-Bake.md)参照。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-MaterialNormals -Width 1280 -Height 800 -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `Assets/NyaForge/Rendering/`: Playerとreceiverが共有する材質adapter/shader。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
