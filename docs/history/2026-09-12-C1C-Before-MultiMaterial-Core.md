# NyaForge 開発タスク

更新: 2026-09-12。部位別材質の土台として制作slotと描画submeshの対応を分離。Core175件・Player回帰に合格。複数材質割当graph/GUIは次。製品全体の開発は継続中。

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

- 部位別材質に向け、制作slotキーと描画submesh番号を分離。`PolygonRenderMesh.MaterialSlotMap` は描画submesh→制作slotキーのimmutable対応表。
- 従来はslotの最大値まで空submeshを生成していたため、slot 3/9などに空きがあるとMeshData検査で失敗した。使用中キーを昇順に圧縮して描画し、元のCageFace.Materialは変更しない。slot 3の面が消えても残りは制作キー9のままで、描画slot0へ対応する。
- Core **175 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-893de8db662e458d9b7b6ab8408f60ea`。sparse対応とtriangle/face map、順反転の同一hash、面除去後のキー保持、native往復、slot 7の押出し・Mirror継承を追加。既存材質/画像/geometryも回帰。
- 最初の追加テストはpredicate内でtriangle counterを進める誤りで失敗。face IDを先に取り出す形へ修正して全件合格。製品の対応表をテストに合わせて弱めていない。
- Unity6000.4.3f1 build成功: `Builds/Windows-C1C-MaterialSlots/NyaForge.exe`、`Logs/build-player-20260912-015106-902.log`。
- Windows Player PASS: `Artifacts/Authoring-20260912-015210-52209bffc8a24899a646f628826d5ea1/report.json`。材質・Paint・既存GUI/geometry回帰。sparse slotの新fixtureはCoreで検査し、まだGUIにslot編集機能は追加していない。今回DensePaintなし。
- 独立Unity2022.3.22f1 receiver: `Artifacts/BridgeReceiver-20260912-015236-546-b5c717e6e4de426995d05b50e8b8fca3/bridge-report.json`。今回Player出力を入力とする旧profile/材質/法線/alpha回帰。
- [部位別材質契約](docs/Material-Slots.md)を追加。材質node UUIDをidentity、制作slotキーを面の対応として使い、graph/native→表示→GUI→専用Bake/Bridgeの残りを明示した。
- [前段の記録](docs/history/2026-09-12-C1C-Before-Material-Slots.md)参照。複数材質の見た目やGUIはまだ未実装であり、この対応表だけで完成とは扱わない。

## 次の作業

1. [部位別材質契約](docs/Material-Slots.md)の複数割当ノードを実装する。制作slotキー別のtyped Material port接続と評価binding/nativeを追加し、既存v1を保持する。表示はMaterialSlotMapで材質配列へ変換。GUIの面割当/材質選択、専用Bake/Bridgeまで接続して初めて部位別材質の一周とする。次に他texture slotとUV再投影へ進む。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-MaterialSlots -Width 1280 -Height 800 -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `Assets/NyaForge/Rendering/`: Playerとreceiverが共有する材質adapter/shader。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
