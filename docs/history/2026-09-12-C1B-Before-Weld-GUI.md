# NyaForge 開発タスク

更新: 2026-09-12。頂点weldのCoreと共通commandを追加。Core207件成功。weldのGUI/Playerは未接続。最新確認済みPlayerはBoundaryBridge。

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

## 頂点weld: Core実装済み、GUIは次工程

- `PolygonWeld.AtCenter` と `PolygonWeldOperation` を追加。選択頂点の平均位置へまとめ、最小vertex IDを残す。計算順序はID順に固定。連続して潰れるcornerは最小corner IDの属性を採用し、面ごとのUV/normal/tangentを保持。3角未満になった面は除去する。ID割当履歴と無関係な未使用頂点は保持。
- 非連続の同一頂点再訪、重複面、辺の過剰共有、分岐境界、切れたface fan、接続方向不整合を拒否。候補全体の三角分割後に共通commandから確定する。全削除は現行polygon制約により拒否。
- Core **207 passed / 0 failed**。quad→triangle/中心位置/入力順序/ID、潰れたtriangle除去、閉じたboxとcorner属性、未使用頂点保持、不正入力時の文書保持、command replay、Undo/Redo/native/Bakeを確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b5c8786a24e0475fa3428e7aab3eb725`。
- 未実装: GUIの選択/成功時選択更新/Player検証、距離による自動weld、最後に選択した位置への統合。全体自己交差やPaint再投影は未対応。今回Playerの新規ビルドなし。以下は前段BoundaryBridgeの検証記録。
## 今回の変更と証拠

- `PolygonBridge`（形状）/`PolygonBridgeOperation`（command）/`AuthoringWorkbench.Bridge`（GUI）を分離。等頂点数3〜256の独立した2境界を四角面で接続する。自動接続位置は距離最小の循環対応。境界方向、ID割当履歴、重複面、辺入射数、三角分割を検査する。
- `PolygonNewFace` にcap/bridge共通の新面属性生成を分離。既存面を保持し、新面は最初の境界の隣接材質を継承。UVは新面ごとの投影で、島の重複を解消しない。
- GUIは最初の境界を黄色、相手を水色で表示。接続ずれを指定可能。同じ境界の選択や閉じた形ではボタンを無効化。表示資源は所有者が破棄する。
- Core **203 passed / 0 failed**。閉じた形の向き、既存属性/ID、無効指定、command replay、Undo/Redo/native/Bakeを確認。結果: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-76672dba180a4549b71287523a0f04d3`。
- 最新Player: `Builds/Windows-C1B-BoundaryBridge/NyaForge.exe`。build log: `Logs/build-player-20260912-044039-672.log`。PASS: `Artifacts/Authoring-20260912-044102-fea66ef0ba674544ab758a7b102616d3/report.json`。GUI2境界表示/同境界拒否/6面12三角形/境界消去/Undo/Redo/native/Bakeと既存suite成功。
- `bridged-boundaries.png` を目視して操作欄の文字と閉じた結果の表示を確認。正面画像なので奥行きはこの画像単独の証拠ではない。閉じた形は境界数0/面数/三角形数/Core方向検査で確認。ビルド後に検証コードのエラー文と試験用出力フォルダ名のみを修正（製品コード変更なし）。
- 未対応/未検証: 不等頂点数bridge、全体自己交差、UV packing、複雑な非平面境界、Paint付き作品、OS/DPI実操作。全体の完成ではない。
- 以前の詳細は [前段履歴](docs/history/2026-09-12-C1B-Before-Boundary-Bridge.md)。頂点追加・面分割/結合・境界cap・UV・Paint・材質・出力復旧の成果と各残件を保持。Unity receiverの最新証拠は同履歴参照（今回再実行なし）。
## 次の作業

1. 次は頂点weldのGUIを接続し、成功後の選択・拒否時の状態保持・Undo/native/BakeをPlayerで検証する。既存stable ID/属性/共通command/Undo/native/GUIの境界を維持する。不等頂点数bridgeも残件。自由面作成・vertex weld/多面merge・連続knifeも維持。複数島/拡縮後のドラッグ、Paint付き作品の動作とUV回転/拡縮ハンドルも維持する。merge/bridge/cut、空polygonと削除後のID割当は残件。scene override、GUI実操作、staging登録前中断/未確認新規GUIDなども保持。復旧だけの類似テストを増やし続けない。複数材質の同UUID共有receiver fixture、部位別GPU定量/alpha、mask/重なり遮蔽は検証残件として保持。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1B-BoundaryBridge -Width 1280 -Height 800 -TimeoutSeconds 600
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `Assets/NyaForge/Rendering/`: Playerとreceiverが共有する材質adapter/shader。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/create/curves、全削除・空polygon、UV回転/拡縮handle・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。


