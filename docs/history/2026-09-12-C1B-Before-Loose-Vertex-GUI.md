# NyaForge 開発タスク

更新: 2026-09-12。面作成の登録/反転/削除/段変更を実クリック検証。新規の未接続頂点Core/commandを追加、212件成功。新頂点のGUI表示は未接続。

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

## 面作成操作検証と新規頂点Core

- `AuthoringWorkbench.FaceDraftVerification` を分離。1点ずつ4→1→5の順で登録、重複追加拒否、反転2回、末尾削除、クリアを実クリックで確認。文書hash不変、foldout開閉の輪郭消去/再表示、編集段を離れた際のdraft破棄も確認。
- Player PASS: `Artifacts/Authoring-20260912-045703-f54c1311a1684d9f95da02c6a86fb97e/report.json`。build: `Builds/Windows-C1B-FaceDraftControls/NyaForge.exe`、log: `Logs/build-player-20260912-045639-794.log`。既存suiteも成功。このビルドは次項の新頂点Core追加より前。
- `PolygonVertexCreation.Add` / `PolygonAddVertexOperation` を分離。mesh-local位置に未接続頂点を追加し、割当履歴より大きいIDを使用。面と属性は保持。有限値/頂点予算/ID枯渇を検査し、共通commandのUndo/nativeへ接続。
- Core **212 passed / 0 failed**。面保持/render hash不変、追加頂点での面作成、面削除後のID再使用防止、非有限値拒否、command replay/Undo/Redo/nativeの未接続頂点保持、Bakeを確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-2c8be12fb01141c9be5ab61cf71de358`。
- 現状 `PolygonRenderAdapter` は面corner由来の頂点のみ出力するため、未接続頂点はBake/preview meshに出ない。正本には保存される。次はこれを表示/選択する編集ケージ側の機能と追加GUIを実装する。製品で使える頂点追加GUIが完成したとは扱わない。空polygonの初期面作成は引き続き残件。
## 順序付き面作成: Core/command/GUI実装済み

- `PolygonFaceCreation.Create` / `PolygonCreateFaceOperation` を追加。既存頂点3〜256個の指定順を周囲と面の向きとして保持。共有辺の向き/過剰共有、重複面、未知/重複頂点を検査する。新face/cornerは割当履歴より上、既存面/頂点は保持。材質slot指定、`PolygonNewFace`による新面UV/normal/tangent生成、候補三角分割を経て共通commandから確定。
- Core **210 passed / 0 failed**。既存辺への三角面追加、指定順/材質/ID/既存面保持、凹五角形、逆向き、自己交差拒否、command拒否時の文書保持/replay/Undo/Redo/native/Bakeを検証。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4708b8e90e2540d4b974589e104439fa`。
- GUI: `AuthoringWorkbench.FaceCreation`。1頂点ずつ末尾登録、カンマ区切り順序入力、反転/末尾削除/クリア、材質slot、妥当性メッセージ、緑の輪郭。文書確定は共通command。文書hash/graph/node変更時に登録クリア。輪郭資源はownerが破棄。輪郭は閉じた線のみで、塗りつぶし/向き矢印なし。
- Player: `Builds/Windows-C1B-FaceCreation/NyaForge.exe`。log: `Logs/build-player-20260912-045426-748.log`。PASS: `Artifacts/Authoring-20260912-045451-9fe3ba5208d94587912fb26dfdc4a96d/report.json`。入力順に対応した輪郭3点/文書不変、向き不一致の無効化、面追加/登録クリア、Undo/Redo/native/Bakeと既存suiteを確認。`created-face.png`を目視しUIの文字/折返しを確認。正面画像は追加面が側面のため面数の証拠はassertionによる。Core変更なし（直近210件）。
- 次の確認: 1頂点ずつの追加/重複拒否/反転/末尾削除/クリアを実クリックで確認、編集中の文書/段変更によるdraft破棄、複数行/長い入力のGUI、複雑な面でのpreview負荷。新規頂点の配置と空polygonへの最初の面は未実装。
- 制約: 頂点接触だけの非manifoldや全体交差を網羅検査しない。新面UVの重なり解消/Paint再投影なし。面積が相殺される自己交差輪郭は共有法線計算で `INVALID_EXTRUSION` となる場合があり、拒否はできるがエラー説明の改善が必要。初回テストはこのコード差で1件失敗し、非ゼロ面積の交差fixtureで三角分割の交差検出を別途確認した。
- 以下のWeld/BoundaryBridgeの確認済み機能を保持。
## 頂点weld: CoreとGUI実装済み

- `PolygonWeld.AtCenter` と `PolygonWeldOperation` を追加。選択頂点の平均位置へまとめ、最小vertex IDを残す。計算順序はID順に固定。連続して潰れるcornerは最小corner IDの属性を採用し、面ごとのUV/normal/tangentを保持。3角未満になった面は除去する。ID割当履歴と無関係な未使用頂点は保持。
- 非連続の同一頂点再訪、重複面、辺の過剰共有、分岐境界、切れたface fan、接続方向不整合を拒否。候補全体の三角分割後に共通commandから確定する。全削除は現行polygon制約により拒否。
- Core **207 passed / 0 failed**。quad→triangle/中心位置/入力順序/ID、潰れたtriangle除去、閉じたboxとcorner属性、未使用頂点保持、不正入力時の文書保持、command replay、Undo/Redo/native/Bakeを確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b5c8786a24e0475fa3428e7aab3eb725`。
- GUI: `AuthoringWorkbench.Weld` に操作ボタンと残存IDのrender alias再選択を分離。点モード2頂点以上で有効。拒否時は文書/選択を保持し、成功時は残存頂点を選択（除去された場合は空選択）。Core変更なし、直近207件を引き継ぐ。
- 最新Player: `Builds/Windows-C1B-Weld/NyaForge.exe`。build log: `Logs/build-player-20260912-044825-652.log`。PASS: `Artifacts/Authoring-20260912-044849-91040a2fd3224764909e3d7a4d37cb55/report.json`。quad→triangle、残存頂点選択、不正weldの文書/選択保持、Undo/Redo/native/Bakeと既存suiteを確認。`weld.png`を目視し三角形とボタン文字を確認。保存再読込後の画像なので、選択保持の証拠は直後のGUI assertionによる。
- 未実装/未検証: 距離による自動weld、最後に選択した位置への統合、全体自己交差、Paint再投影、複雑なseamのGUI選択/大規模選択/OS/DPI手動受入。以下は前段BoundaryBridgeの検証記録。
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

1. 次は未接続頂点の編集用表示/選択と新規頂点追加GUIを実装する。render indexとstable IDの境界、既存Paint/UV/出力の意味を保護する。選択順/向き/既存辺の共有/UVとIDの扱いを明確にしてCoreから共通command/GUIへ接続する。既存stable ID/属性/共通command/Undo/native/GUIの境界を維持する。不等頂点数bridgeも残件。自由面作成・vertex weld/多面merge・連続knifeも維持。複数島/拡縮後のドラッグ、Paint付き作品の動作とUV回転/拡縮ハンドルも維持する。merge/bridge/cut、空polygonと削除後のID割当は残件。scene override、GUI実操作、staging登録前中断/未確認新規GUIDなども保持。復旧だけの類似テストを増やし続けない。複数材質の同UUID共有receiver fixture、部位別GPU定量/alpha、mask/重なり遮蔽は検証残件として保持。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1B-FaceDraftControls -Width 1280 -Height 800 -TimeoutSeconds 600
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `Assets/NyaForge/Rendering/`: Playerとreceiverが共有する材質adapter/shader。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/create/curves、全削除・空polygon、UV回転/拡縮handle・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。






