# NyaForge 開発タスク

更新: 2026-09-12。standard材質をUnity表示へ接続。色/光沢/alphaのGPU検査、Undo/rollbackと既存GUI・高密度描画が合格。材質編集GUIと材質出力は未接続。製品全体の開発は継続中。

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

- `StandardMaterialAdapter` と共有PBR shaderでlinear tint・texture・metallic・roughness→smoothness・emission・alphaを描画へ反映。Linear Built-In限定で、未対応pipelineやshader欠損を明示エラーにする。
- Opaque/CutoutとBlendを別shaderで所有。Cutoutはtexture×tint alphaの閾値、Blendはfade/ZWrite off/RGBのみの書込でビューポートの二重透過を防ぐ。`AuthoringPreviewLights` はstage所有でPreviewLayerのみを照らす。
- `BaseColorSurface` が画像なしの材質と確定/仮textureを所有し、`ColorUpdate` のprepare/commit/rollbackで材質を交換。mesh/rootを保持し、Undoでもshaderが戻る。制作正本にnormalがないときは表示meshだけで法線を生成する。
- `MaterialProjectionVerification` が実GPUの不透明/切抜き/半透明とtarget alpha、tint・roughness・metallicによる描画変化、emission、画像previewを保持するrollback、mesh再利用、Undo/Redoを確認。7枚の小型GPU検査PNGを記録。芸術的品質の承認を意味しない。
- Unityコンパイル成功: `Builds/Windows-C1C-MaterialLighting/NyaForge.exe` / `Logs/build-player-20260912-010620-832.log`。
- Player全GUI/PNG30形式/準備競合/DensePaint PASS: `Artifacts/Authoring-20260912-010701-ec12192939944379a9a6d7c04e89c7c9/report.json`。同denseの131,072triで準備待ち約1,350ms、初回down約14ms、up約39ms。Core本体は前段から変更なし（前段167件合格）。
- 初回shaderビルドは予約語sampleで失敗し修正済み。残留UnityLockfileはUnity.exeが存在せずexclusive openできることを確認して削除。ユーザーPlayerには介入していない。
- [材質graph/描画契約](docs/Material-Graph.md)、[前段の記録](docs/history/2026-09-12-C1C-Before-Material-Preview.md)参照。history内の「次」は当時の状態。

## 次の作業

1. 材質作成・選択・値編集GUIと既存Paint→Materialへの明示移行を共通commandで接続する。3D Paintの依存判定を材質経路へ拡張する。表示adapterは実装済み。GUIから値変更/Undo/native再読込の外観を検証した後、材質Bake/Bridgeへ進む。[材質graph](docs/Material-Graph.md)を参照。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-MaterialLighting -Width 1280 -Height 800 -DensePaint -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
