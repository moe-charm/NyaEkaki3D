# NyaForge 開発タスク

更新: 2026-09-12。面を2頂点の対角線で分割する操作を追加。Core198件、Windows Playerのクリック/Undo/native/Bakeが成功。検証モードの背面実行設定を追加し、全体再検証も成功。

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

- `PolygonFaceSplit`/`PolygonSplitOperation`/`AuthoringWorkbench.FaceSplit`を分離。1つの共通平面の非隣接2頂点を対角線で2面へ分割。位置/UV/normal/tangentを保持し、新faceと共有端点corner2個を割当履歴より上へ生成。候補の三角分割方向/面積で外側を通る対角線を拒否。既存対角線/曖昧な共通面/非平面も拒否する。
- GUIは点モードでクリック＋Shiftクリックで2頂点選択→「2頂点を結んで面を分割」。成功時だけrender-index選択を解除し、拒否時は選択を保持。自由ナイフ・辺上への新頂点追加は別課題。
- Core **198 passed / 0 failed**。quad→triangles、属性/新ID、凹面の内側/外側対角線、無効選択と状態保持、Undo/Redo/native/Bakeを検証。結果: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-9a873f49311d4658af715e3e91781244`。
- 分割機能のPlayer PASS: `Artifacts/Authoring-20260912-041759-54365074b11a43b1b28220cb82274971/report.json`、`Builds/Windows-C1B-FaceSplit/NyaForge.exe`。途中で時間がかかったが最終成功。クリック/Shift、ボタン、無効選択保持、Undo/Redo/native/Bakeを含む既存suiteを確認。
- 隠しPlayerのfocus喪失による停止を避けるため、`RunVerification`だけApplication.runInBackground=trueにした。通常起動のProjectSettingsは未変更。新build `Windows-C1B-FaceSplitBackground`、log `Logs/build-player-20260912-041955-639.log`。最終PASS: `Artifacts/Authoring-20260912-042036-7124a9fa152d400597014b3d91d3d964/report.json`。プロセス正常終了済み。`split-face.png`を目視し対角線の2面とGUIを確認。最終利用入口は `Builds/Windows-C1B-FaceSplitBackground/NyaForge.exe`。
- 前段記録: [履歴](docs/history/2026-09-12-C1B-Before-Face-Split.md)。以下は以前の面結合の記録。

- `PolygonFaceMerge`（形状処理）/`PolygonMergeOperation`（共通command）/`AuthoringWorkbench.FaceMerge`（GUI）を分離。共有manifold辺が1本の2面に限定し、同一平面・方向・材質、共有端点のUV/normal/tangent一致を確認。外周を1faceへまとめ、小さいface IDと残存corner属性、ID上限を保持する。
- 新ボタン「隣接する2面を結合」は2面選択時に有効。結合後は消えたface IDを選択から取り除く。UV外周値は保持するが内部の三角分割と補間が変わる場合がある。三角形数削減を保証する操作ではない。多面dissolve・曲面merge・vertex weldは未対応。
- Core **195 passed / 0 failed**。triangles→quad、外周/属性参照/割当履歴保持、材質/UV seam/非平面/重複選択拒否、command Undo/Redo/native/Bakeを検証。結果: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-62ea1055bafd4961b9f21b50e17bda67`。
- 最新Player: `Builds/Windows-C1B-FaceMerge/NyaForge.exe`。build log: `Logs/build-player-20260912-041216-751.log`。PASS: `Artifacts/Authoring-20260912-041249-45cbc2e3f62c414482a0c40a79b90d3f/report.json`。ボタンクリック→1quad/2triangles・残存面選択・Undo/Redo/native/Bakeを確認。既存suiteも成功。`merged-faces.png` を目視し形状と操作欄を確認。複雑な凹面/非線形UV補間/Paint付き作品は未検証。
- 前段記録: [履歴](docs/history/2026-09-12-C1B-Before-Face-Merge.md)。以下は以前の境界表示記録。

- `BoundaryHighlightProjection` に表示専用のLineRenderer/Material所有と破棄を分離。選択loopの頂点をRestTransformで表示空間へ変換し、閉じた黄色線を描画する。Colliderや文書変更なし。線幅は境界寸法から算出し上限/下限あり。奥の線を常に透視するX-ray表示ではない。
- 境界dropdown/foldoutとRefreshへ接続。非編集段・境界なし・foldout閉じで消去。GUIの境界検出失敗時も理由をtooltipに保持する。`OnDestroy`で資源破棄。
- 最新Player: `Builds/Windows-C1B-BoundaryHighlight/NyaForge.exe`。build log: `Logs/build-player-20260912-040728-222.log`。PASS: `Artifacts/Authoring-20260912-040757-3ec044926567471895b5ec490a9fd60e/report.json`。2境界の点列/表示座標を照合し、切替/閉じ開き/最終出力/編集へ戻る/全cap後の消去とdocument hash不変を検証。既存suiteも成功。`boundary-highlight.png` を目視し、選択した穴の黄色線と境界名を確認。
- Core変更なし（直近193件）。今回の試験は通常scaleの四角い境界。極端な縮尺、複雑な非平面loop、線の遮蔽、OS/DPI/手動のdropdown選択は残件。前段記録: [履歴](docs/history/2026-09-12-C1B-Before-Boundary-Highlight.md)。以下は以前のID履歴記録。

- `PolygonIdWatermarks` を追加。vertex/face/cornerの割当上限をimmutable snapshotへ保持し、削除・移動・UV・材質変更で引継ぐ。Extrude/Solidify/Mirror/Capは上限から新IDを割り当てる。現在の編集系列で削除後に作り直す際の再利用を防ぐ。
- NYFP polygon blobはv1/v2 reader。履歴上限と現在のlive最大IDが等しい場合は既存v1を維持、異なる場合はv2（headerにUInt64×3追加）。下限不正とtruncationを拒否。native graph/project schemaは変更なし。旧アプリはv2 blob非対応。旧v1から既に失われた削除履歴は復元できない。
- Core **193 passed / 0 failed**。削除→UV/材質/移動→binary/native再読込→Extrude/Cap、Mirror/Solidifyの割当、Undo/Redo、v1のbyte roundtrip、v2不正入力とID枯渇を検証。結果: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-1714bc93984846fbaadca833eb84d34f`。
- 最新Player: `Builds/Windows-C1B-PolygonIdHistory/NyaForge.exe`。build log: `Logs/build-player-20260912-040324-810.log`。PASS: `Artifacts/Authoring-20260912-040404-b70b5560145c46be97078a768e05d4a2/report.json`。既存の造形/UV/Paint/native/Bake GUI suiteも成功。ID履歴自体の詳細な系列はCore試験が証拠。
- 限界: Undoは履歴を含む過去snapshotを復元するため、分岐をまたぐプロセス全体の単調allocatorではない。外部参照のdomain/snapshot/revision照合は引き続き必要。新規source構築は新しい割当履歴を開始する。前段詳細: [履歴](docs/history/2026-09-12-C1B-Before-Id-History.md)。以下は前回の境界cap記録。

- `PolygonBoundaries` は隣接面と逆向きの境界loopを検出し、分岐/nonmanifoldを拒否。`PolygonCap` は完全な3〜256頂点loopを新しい1面で閉じる。既存面と同じ頂点集合の重複面を拒否し、隣接材質を継承。新しい面だけUVを投影し、既存属性/位置を保持する。UV重複の自動解消、global自己交差検査、自由面作成やbridgeとは別。
- `PolygonCapOperation` → `PolygonEditing` → 共通command/Undo/native/Bakeへ接続。GUIは「境界を閉じる」foldoutの選択欄とボタン。境界名/頂点数を短く表示し、IDはtooltip。閉じたmeshではボタンを無効化。
- Core **190 passed / 0 failed**。閉じた形のedge入射数と向き、残存面参照/属性、削除穴の再cap、無効選択/重複拒否、Undo/Redo/native/Bakeを検証。最新Core結果: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f7cfce049b4f4a28b1aa34b464b79cb3`。
- 最新Player: `Builds/Windows-C1B-BoundaryCapReadable/NyaForge.exe`。build log: `Logs/build-player-20260912-035756-639.log`。PASS: `Artifacts/Authoring-20260912-035827-3d09b260cf574dd681c8b624624c4b83/report.json`。GUIで2つの境界を順に閉じて4→5→6面/12三角形、Undo/Redo/native/Bakeを確認。既存suiteも成功。`capped.png`を目視し閉じた形と省略されない境界欄を確認。OS/DPIの手動受入は別。
- 初回追加でUV試験のscrollがレイアウト確定前となりclip判定失敗。境界GUIをfoldoutへ整理し、UV試験はレイアウト後にScrollToするよう修正。失敗記録: `Artifacts/Authoring-20260912-035528-f8286d98f6d740cab0403563b50569ad/report.json`。成功版は上記Readable。
- 以前の検証/変更詳細は [直前のcurrent_task](docs/history/2026-09-12-C1B-Before-Boundary-Cap.md) に保存。前段のUVドラッグ・選択・Paint・材質・出力identityの実装は維持。最新Bridge variant成功は `Artifacts/BridgeReceiver-20260912-033631-177-a3094b2c12fb40238ae1a8d14c59ca15/bridge-report.json`。クラッシュ/再起動復旧成功は `Artifacts/BridgeReceiver-20260912-032808-663-746b1450f7ec4f03884991ef96823dac/`。今回の新Playerでreceiverを再実行した結果ではない。
- 未確認: 複雑な凹/非平面boundary、Paint付きcap、複数島の実drag、OS focus/DPI、scene override、GUID記録前/ステージ登録前/commit直後の中断、disk障害。全体の完成ではない。
## 次の作業

1. 辺の途中に頂点を挿入する操作を実装し、任意位置の分割に向けて進める。自由面作成・bridge・vertex weld/多面mergeも維持。複数島/拡縮後のドラッグ、Paint付き作品の動作とUV回転/拡縮ハンドルも維持する。merge/bridge/cut、空polygonと削除後のID割当は残件。scene override、GUI実操作、staging登録前中断/未確認新規GUIDなども保持。復旧だけの類似テストを増やし続けない。複数材質の同UUID共有receiver fixture、部位別GPU定量/alpha、mask/重なり遮蔽は検証残件として保持。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1B-FaceSplitBackground -Width 1280 -Height 800 -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `Assets/NyaForge/Rendering/`: Playerとreceiverが共有する材質adapter/shader。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/create/curves、全削除・空polygon、UV回転/拡縮handle・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
