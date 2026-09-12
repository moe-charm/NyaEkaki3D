# NyaForge 開発タスク

更新: 2026-09-12。保存後の材質で画像×tint alphaとcutoff境界を表裏12ケースのGPU RGBで確認。製品全体の開発は継続中。

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

- `MaterialAlphaVerification` をBridge検証モジュールへ追加。Coreで生成・Bake出力した材質をImportMaterialで保存し、Material/Prefabを再importしてからGPUのlinear float targetへ描画する。
- 画像alpha128/255とtint alpha0/0.5/1、Opaqueでalpha0、Cutout閾値0.249/0.253を表裏で計12ケース検査。emissionと既知の背景色による期待RGBを独立計算し、12ケースすべて小数6桁の出力で一致。
- Unity 2022.3.22f1 receiver PASS: `Artifacts/BridgeReceiver-20260912-014719-748-3ba249caef5f482f981297b252bb34cb/bridge-report.json`。数値は `material-alpha-pixels.txt`。前段のGUI出力 `Artifacts/Authoring-20260912-014430-22062cc925e64a3d950ee117907409c5` を入力とし、旧scale1/100・Surface、材質rollback、画像なし/法線なし3modeも同時回帰。
- 初回はテストカメラの背景色をlinear値のまま渡したため不一致（`Artifacts/BridgeReceiver-20260912-014653-227-c7417645acc142dcafedf1f137102d3f`）。カメラがsRGBとして解釈するためgammaへ変換して指定し、新規receiverで再検証した。製品shaderの変更は不要だった。
- 本体/Coreの変更なし。最新Playerは `Builds/Windows-C1C-MaterialNormals/NyaForge.exe`、Player PASSは上の014430、直近Core173 passed / 0 failedは `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-3809aa9c8b91431bade870d46a5e1cb1`。今回それらの再build/再実行はしていない。
- RGBのstraight fade合成を確認した範囲であり、target alphaのcoverage意味、透明面sort、影、MSAA、透明輪郭filteringは未検証。Editor GUIの実操作、任意照明での外観受入も別。
- [材質Bake契約](docs/Material-Bake.md)と[材質graph](docs/Material-Graph.md)の古い未接続記述を更新。[前段の記録](docs/history/2026-09-12-C1C-Before-Material-Alpha.md)参照。

## 次の作業

1. 異なる複数材質の割当を実装する。現CageFace.Materialはslot整数であり、設計v2は配列順だけに依存しないface group/材質identityと新face継承を要求する。安定した材質参照とslotの対応を定め、Core graph/native→表示→GUI→専用Bake/Bridgeの順で接続する。既存v1の全slot一括材質profileを黙って変更しない。続いて他texture slotとUV再投影へ進む。
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
