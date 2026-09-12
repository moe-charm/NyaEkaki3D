# NyaForge 開発タスク

更新: 2026-09-12。既存部位の材質を分けるGUI入口を接続。Core178件とPlayer GUI回帰に合格。面の部位作成・Paint対象・複数材質出力は未接続。製品全体の開発は継続中。

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

- `MaterialSlotEditing` をCoreへ追加。単一割当から全使用slotへの接続移行と、選択slotだけの材質複製を新graphとして返す。元の画像接続は維持し、ReplaceGraphの共通Undoに参加する。
- `OutputSurfaceConnections.Resolve(graph, slot)` を追加。複数材質でslot未指定の場合はnullで返し、暗黙の別部位へのPaint接続を避ける。
- `AuthoringWorkbench.MaterialSlots` に有効化ボタン・部位dropdown・材質複製を追加。部位選択または複製後は該当材質を編集対象にする。新しい面の部位作成はまだ未実装。
- Core **178 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-54f21dd99da84f5b9e8a9da1a3661ff5`。画像接続とgeometry保持、UUID更新、移行/複製のUndoを追加。
- Unity build初回はUnityのLINQで非対応のFirstOrDefault(defaultValue)により失敗。Count/indexへ置換。Unityプロセス不在とlockの排他openを確認し、残ったTemp/UnityLockfileだけを削除して再buildした。GUI試験でパネルを先に展開し、layout後にクリックするよう調整。
- 最終build成功: `Builds/Windows-C1C-MaterialSlotGUI3/NyaForge.exe` / `Logs/build-player-20260912-020938-176.log`。
- Windows Player PASS: `Artifacts/Authoring-20260912-021011-f7b4f3fa10004d4788f2420940a5d17b/report.json`。実ボタンで移行/複製、画像・材質UUID保持、Undo/Redoを確認。旧GUIと複数材質GPUも回帰。新GUI fixtureは既存1slotからの操作で、多部位の実クリック確認は残る。今回はDensePaintなし。
- Bridge未変更、今回receiverは再実行していない。複数材質Bakeは引き続き未対応。画像は材質複製後も同じPaintを共有し、Paint GUIへのslot接続は次。
- [部位別材質契約](docs/Material-Slots.md)と[使い方](docs/Authoring-Quickstart.md)を更新。[前段](docs/history/2026-09-12-C1C-Before-Slot-GUI.md)を保存。

## 次の作業

1. [部位別材質契約](docs/Material-Slots.md)の選択面→部位割当と既存材質への再接続を追加し、多部位GUIで検証する。続いてPaint対象をslot経路へ接続し、共有Paintの同時仮表示と画像別binding/失効/保存を確認。専用Bake/Bridgeまでつないで部位別材質の一周とする。次に他texture slotとUV再投影へ進む。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-MaterialSlotGUI3 -Width 1280 -Height 800 -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `Assets/NyaForge/Rendering/`: Playerとreceiverが共有する材質adapter/shader。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
