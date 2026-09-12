# NyaForge 開発タスク

更新: 2026-09-12。境界を1面で閉じる操作を追加。Core190件、Windows Playerの2境界cap・Undo・保存・Bakeが成功。

## 開発の入口

作業先: `Z:/TextureVoice_local/git/NyaForge`。Windows先行、macOSは将来。
読む順: このファイル → [開発計画](docs/Development-Plan.md) → [設計v2](docs/NyaForge-Authoring-Design2.md)の対象節 → コード。[文書一覧](docs/README.md)参照。
製品目標は小物の制作・出力を一周し、低ポリ全身キャラ、品質向上へ進むこと。設計v2は製品方針、v1は背景資料。設計中の外部依存・機能は採用済みや実装済みを意味しない。

## 現在地

| 範囲 | 現物と確認状況 |
|---|---|
| C0-R／C1-A | 空project、最大1object、typed graph、共通commandとUndo、native保存、node canvas、static Bake／Bridge |
| C1-B | polygon/corner ID、面選択・押出し・削除・境界cap・厚み、Mirror、編集ケージと最終結果、UV投影と島の数値編集 |
| C1-C Paint | 2D brush、Image port、UV binding、1stroke Undo、native画像保存、3D baseColor、PNG／Surface Bake |
| Layer/mask GUI | 移行・追加・選択・並替・削除・表示・不透明度・名前、mask追加/削除と描画、取消、Undo、保存を検証 |
| Image import | PNG検査・展開・縦横比保持サイズ調整・新layer追加・Undo・保存を検証。Windows pickerの実操作は手動未確認 |
| 3D paint | BVH ray、論理edge/UV連続判定、screen補間、切れた区間の描画、GUI色/mask・仮表示・取消・Undo・保存/出力を実装 |
| 今後 | 3D複数面/細かなseamの精度と操作、一般Material graph、UV再投影、出力identity更新、Evidence/MCP、rig/weight/morphと全身制作 |

保存はstatic writer schema2、graph writer schema3、reader 1/2/3。Paintは共有画像と部位別の独立画像に対応。材質未割当は不透明preview、標準材質は3alpha modeを選択できる。UV変更時は旧payloadを未解決として保持し、明示rebindで対応を更新する。見た目を保つ再投影ではない。旧mesh-only Bakeは画像を拒否。画像付きmeshは別のSurface Bake profile、単一標準材質はMaterial Bakeを利用。複数材質BakeはGUI/Bridgeまで接続済み。

## 今回の変更と証拠

- `PolygonBoundaries` は隣接面と逆向きの境界loopを検出し、分岐/nonmanifoldを拒否。`PolygonCap` は完全な3〜256頂点loopを新しい1面で閉じる。既存面と同じ頂点集合の重複面を拒否し、隣接材質を継承。新しい面だけUVを投影し、既存属性/位置を保持する。UV重複の自動解消、global自己交差検査、自由面作成やbridgeとは別。
- `PolygonCapOperation` → `PolygonEditing` → 共通command/Undo/native/Bakeへ接続。GUIは「境界を閉じる」foldoutの選択欄とボタン。境界名/頂点数を短く表示し、IDはtooltip。閉じたmeshではボタンを無効化。
- Core **190 passed / 0 failed**。閉じた形のedge入射数と向き、残存面参照/属性、削除穴の再cap、無効選択/重複拒否、Undo/Redo/native/Bakeを検証。最新Core結果: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f7cfce049b4f4a28b1aa34b464b79cb3`。
- 最新Player: `Builds/Windows-C1B-BoundaryCapReadable/NyaForge.exe`。build log: `Logs/build-player-20260912-035756-639.log`。PASS: `Artifacts/Authoring-20260912-035827-3d09b260cf574dd681c8b624624c4b83/report.json`。GUIで2つの境界を順に閉じて4→5→6面/12三角形、Undo/Redo/native/Bakeを確認。既存suiteも成功。`capped.png`を目視し閉じた形と省略されない境界欄を確認。OS/DPIの手動受入は別。
- 初回追加でUV試験のscrollがレイアウト確定前となりclip判定失敗。境界GUIをfoldoutへ整理し、UV試験はレイアウト後にScrollToするよう修正。失敗記録: `Artifacts/Authoring-20260912-035528-f8286d98f6d740cab0403563b50569ad/report.json`。成功版は上記Readable。
- 以前の検証/変更詳細は [直前のcurrent_task](docs/history/2026-09-12-C1B-Before-Boundary-Cap.md) に保存。前段のUVドラッグ・選択・Paint・材質・出力identityの実装は維持。最新Bridge variant成功は `Artifacts/BridgeReceiver-20260912-033631-177-a3094b2c12fb40238ae1a8d14c59ca15/bridge-report.json`。クラッシュ/再起動復旧成功は `Artifacts/BridgeReceiver-20260912-032808-663-746b1450f7ec4f03884991ef96823dac/`。今回の新Playerでreceiverを再実行した結果ではない。
- 未確認: 複雑な凹/非平面boundary、Paint付きcap、複数島の実drag、OS focus/DPI、scene override、GUID記録前/ステージ登録前/commit直後の中断、disk障害。全体の完成ではない。
## 次の作業

1. 削除→再生成時の要素ID割当を点検し、削除済みIDを同じdomainで再使用しない設計を実装する。その後、境界の3Dハイライト/自由面作成とmerge/bridge/cutへ進む。複数島/拡縮後のドラッグ、Paint付き作品の動作とUV回転/拡縮ハンドルも維持する。merge/bridge/cut、空polygonと削除後のID割当は残件。scene override、GUI実操作、staging登録前中断/未確認新規GUIDなども保持。復旧だけの類似テストを増やし続けない。複数材質の同UUID共有receiver fixture、部位別GPU定量/alpha、mask/重なり遮蔽は検証残件として保持。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1B-BoundaryCapReadable -Width 1280 -Height 800 -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `Assets/NyaForge/Rendering/`: Playerとreceiverが共有する材質adapter/shader。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/create/curves、全削除・空polygon、UV回転/拡縮handle・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
