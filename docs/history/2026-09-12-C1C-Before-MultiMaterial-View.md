# NyaForge 開発タスク

更新: 2026-09-12。複数材質割当のCore graph/nativeを実装しCore177件・旧GUI回帰に合格。複数材質表示/GUI/出力は未接続。製品全体の開発は継続中。

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

- `GraphNode.AssignMaterials` / `mesh.assign-materials` v1を追加。canonicalなslotキーごとにtyped Material入力を生成し、graph edgeの接続元UUIDで材質を参照する。旧単一割当v1は維持。
- `MaterialSlotEvaluation` / `MaterialSlotBinding` を独立モジュールにし、使用slotの割当検査、各画像のdomain/UV照合、immutable slot辞書を実装。未接続・不足・二重割当・後段geometryの材質消失を診断する。
- `GraphMeshValue.SlotMaterials` とcanonical snapshot、graph binaryのslot payloadを追加。native graph schema3/wire1の新node typeで保存する。材質の値変更はmesh hashを保持する。
- Core **177 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-7bedd32649d341ddad853fca9b3db6d1`。slot 3/9への異なる材質接続、順序、値変更/Undo/Redo/native、無効キー・未接続・割当不足・競合、旧3export profileでの拒否を確認。
- まだPlayerの複数材質表示は未接続。projectionは材質を黙って省略せず明示拒否する一時状態。旧Bake/Surface/Material all-slotsは複数材質を拒否する。これを最終製品の制約とはしない。
- Unity6000.4.3f1 build成功: `Builds/Windows-C1C-MultiMaterialCore/NyaForge.exe` / `Logs/build-player-20260912-015751-585.log`。
- Windows Player PASS: `Artifacts/Authoring-20260912-015839-5b112223ed034a5b9e1d29526ce341a3/report.json`。旧単一材質・Paint・既存GUI/geometry回帰。複数材質GUIを操作した結果ではない。今回DensePaintなし。
- receiver記録: `Artifacts/BridgeReceiver-20260912-015914-965-c62c50eb5a5f4260b6195551a6c3dd28/bridge-report.json`。上のPlayer出力による旧profile/材質/alpha/法線回帰であり、複数材質出力の検証ではない。
- [部位別材質契約](docs/Material-Slots.md)へ実装範囲を追記。[前段](docs/history/2026-09-12-C1C-Before-MultiMaterial-Core.md)を保存。

## 次の作業

1. [部位別材質契約](docs/Material-Slots.md)の複数材質表示を接続し、projectionの一時拒否を外す。MaterialSlotMapから材質配列へ変換し、複数textureの所有とprepare/commit/rollbackを実装。画像別binding/失効のCore検査も追加する。続いてslot対応のOutputSurfaceConnections、GUIの面割当/材質選択、専用Bake/Bridgeまで接続して部位別材質の一周とする。次に他texture slotとUV再投影へ進む。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-MultiMaterialCore -Width 1280 -Height 800 -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `Assets/NyaForge/Rendering/`: Playerとreceiverが共有する材質adapter/shader。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
