# NyaForge 開発タスク

更新: 2026-09-12。切断候補の可視性optionを接続。Core231件/Windows Player成功。重なりのGUI追加検証は次工程。

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

## 切断候補の可視性option

- `PolygonEdgeScreenPicker` に候補world位置のpredicateを追加。拒否された候補では最短距離を更新しないため、次の可視候補を探索できる。旧呼出はpredicateなしで互換。
- `Geometry/MeshVisibility` はray origin→候補点の両面geometry判定。候補そのものの面は許容（距離許容差max(1e-6, distance*1e-5)）。材質alphaを判定しない。
- `AuthoringWorkbench.CutVisibility` に「隠れた位置を除外」（既定ON）と共通FindCutPathHitを分離。hover/clickとも同じ判定を使う。Unity viewport rayのnear-plane originから判定し正投影にも対応。MeshData参照/RestTransformが同じ間はBVHを再利用。
- Core **231 passed / 0 failed**。UVなし2枚の重なりで、最短の奥辺を除外して手前の別辺へfallback、狭い半径では候補なし、自己面許容/奥の点拒否/距離0を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-928c681d0ffe4112a7bdf08f43e607cb`。
- 最新確認済みPlayer: `Builds/Windows-CutVisibility/NyaForge.exe`。log: `Logs/build-player-20260912-060615-356.log`。PASS: `Artifacts/Authoring-20260912-060639-3d571ed0e5de4f8fa385f41085841772/report.json`。可視性ONで既存の平面クリック/hover/確定/Undo/native/Bakeとpaint含むsuite成功。
- GUI上で重なる2枚の切替比較、toggle実クリック、BVH cache更新、hover画像は次の検証。透明材質も面として扱う旨をGUI表示。判定対象は編集中Mesh。辺ごとの最近点だけを判定し、部分遮蔽辺の別区間探索は未対応。全体計画の残件も継続。

## UVに依存しない面レイ判定

- 遮蔽判定をペイントのUV必須条件へ依存させないため、`Geometry/MeshRaycast` を抽出。既存median BVH/両面/距離制限/triangle安定順を維持し、triangle/submesh/頂点indices/barycentric U,V/距離/向きを返す。UV不要。
- `SurfacePaintMesh` はgeometryを所有し、UV補間・Owner・UV continuityの責務だけを保持。RayGeometry数値helperをGeometryへ移動（Unity metaも移動）。既存INVALID_PAINT_RAY診断コードは互換維持。
- Core **230 passed / 0 failed**。新規UVなしmeshのscale1/100/微小scale、indices/barycentric/距離制限と、既存paint/BVH/seam/全suite成功。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-3be311476b604d3c9b984a321617f61b`。
- 最新確認済みPlayer: `Builds/Windows-GeometryRaycast/NyaForge.exe`。log: `Logs/build-player-20260912-060314-627.log`。PASS: `Artifacts/Authoring-20260912-060337-6a77231137b34751aee16184e213d6d1/report.json`。ペイントを含む既存GUI suite成功。
- これは遮蔽判定の共通基盤で、辺pickerの可視性optionはまだ未接続。次は候補点へのrayを使うpredicateをpicker探索へ加え、遮蔽された最短候補を飛ばして次の見える辺を選べるようにする。表示polygon/transform単位でBVHを再利用し、透明材質の扱いを明示する。hover画像確認も残件。
- 全体計画の複数object、rig/weight/morph、各形式入出力、Evidence/MCP等も継続。

## 切断候補のhover表示

- `AuthoringWorkbench.CutPathHover` を分離。切断指定modeのpointer moveで候補辺を黄色い線、透視補正後の指定位置を10px角の黄色いUIマーカーで表示。ラベルに辺ID/割合/登録済みを表示する。UIマーカーはPickingMode.Ignoreでクリックを遮らない。
- hover時は文書や選択へ書き込まない。pointer leave/down、カメラ変更、draft refresh、mode終了で候補を消去。ドラッグ中は候補判定を行わない。色/割合表示と登録済み経路のピンク線は別owner。
- 最新確認済みPlayer: `Builds/Windows-C1B-CutHover/NyaForge.exe`。log: `Logs/build-player-20260912-055938-366.log`。PASS: `Artifacts/Authoring-20260912-060001-e6b9ac7556dd4558a4261547715203df/report.json`。
- viewportの3辺へPointerMoveを送り候補edge ID/2点line/marker表示/文書と選択不変を検査。実クリック後のhover消去と画面外move時の消去も確認。既存の経路確定/Undo/native/Bakeを含めsuite成功。Core変更なし（直近229件）。hoverの画像を残す専用captureは今回未追加。
- 次はhover画像と登録済み表示/カメラ変更時消去を確認し、面による遮蔽のある辺を選ばない可視性optionへ進む。現状は全辺候補（GUI明記）、面遮蔽なし。全体計画の残件は継続。

## 切断経路の画面クリック指定

- `AuthoringWorkbench.CutPathPicking` を分離。連続切断パネルのtoggleで有効化し、viewportの短い左クリックをPolygonEdgeScreenPicker（半径12 panel px）へ送る。割合はクリック位置から計算して経路末尾へ追加。重複と上限拒否。通常の頂点/面選択を変更しない。
- 左ドラッグ回転/右ドラッグ移動は既存経路を維持。クリック時にviewをfocusしEscapeで指定モード終了（draft保持）。パネル閉じ/文書変更/編集段変更でmode解除。3Dpaint有効化は最終段へ移るため解除、切断mode有効化時はpaintをoff。
- 最新確認済みPlayer: `Builds/Windows-C1B-CutPathPickVerified/NyaForge.exe`。log: `Logs/build-player-20260912-055711-541.log`。PASS: `Artifacts/Authoring-20260912-055733-55c978e90d4a429f90ede7fca2e4af6d/report.json`。
- `AuthoringWorkbench.CutPathPickingVerification` で3辺中点へのviewport実クリック、3地点の割合/preview、選択/文書不変、Escape送信後の通常点選択復帰、パネル閉じでmode終了/再開時draft保持を確認。その後の切断確定/Undo/Redo/native/Bakeと既存suiteも成功。Core変更なし（直近229件成功）。
- 面遮蔽なしで裏側の辺も候補になることをGUIへ明記。現在hover候補線/可視面限定なし。次は候補hoverと可視性の扱いを改善し、奥行き重なり/端点/斜めの面/ドラッグ・paint競合を追加確認する。全体計画のrig/weight/morph等は未完了。

## 画面上の辺判定Core

- `PolygonEdgeScreenPicker` を独立追加。Polygonの論理辺を画面距離で検索し、EdgeCutLocation（元辺の割合）/距離/深度を返す。頂点はRestTransformで変換、near/farで線分をclipしてから投影する。
- `SurfaceCameraSnapshot.ProjectionWeight` を追加し、投影の同次座標wを使って画面上の割合を元の3D辺の割合へ補正。正投影は一定w、透視投影は異なるwに対応。距離同点は手前の深度、さらに同点なら小さい辺ID順。点に潰れた投影辺は手前の端点を選ぶ。
- Core **229 passed / 0 failed**。奥行きの違う辺の画面中点→3D割合1/3、scale100、正投影、半径外/負半径、near/farをまたぐ辺と元割合の再投影、カメラ後方の除外を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-65c2f9c62d314ff594e4ce1f6df739d2`。
- 今回Playerビルドなし。画面クリックGUIは未接続。次は深度同点/重なり/端点/平行投影の追加確認と、切断用クリックモードを通常の点/面選択やカメラ操作から分離して接続する。
- 現在のpickerは面による遮蔽判定なし（X-ray相当）、画面距離優先で探索は全辺O(E)。可視面だけの選択/裏面/大規模性能/viewport clippingは今後。全体計画の残件も継続。

## 連続切断の操作検証と入力復旧

- `AuthoringWorkbench.CutPathControlsVerification` を分離。選択した3辺を順番に追加する実クリック、順序/preview確認、重複辺と非edge選択の拒否、不正行で確定/線を無効化、末尾取消/クリアの実クリック、編集段変更でdraft消去を確認。
- 「最後の辺を取り消す」は経路全体の解析に依存していたため、不正な末尾行を取り消せなかった。生の非空行から末尾を取り除く処理へ変更。修復後のpreview復帰、2点→1点時の確定無効化を検証。
- 最新確認済みPlayer: `Builds/Windows-C1B-CutPathControls/NyaForge.exe`。log: `Logs/build-player-20260912-055147-222.log`。PASS: `Artifacts/Authoring-20260912-055212-d61349e1fe7e4df7b8cb85bc3192ac5b/report.json`。上記の操作後、連続切断確定/Undo/Redo/native/Bakeと既存suiteも成功。Core変更なし（直近227件成功）。
- 次は画面上の辺を直接指定して経路へ追加できるようにする。現状は両端頂点の選択→登録が必要。辺hit testを表示から分離し、画面距離・割合・変換と重なりを検証してからGUIモードを接続する。キーボード取消と通常の点/面選択・カメラ操作との競合を確認する。
- 再訪/閉loop/曲線、複雑なseam/凹面/端点混在のGUIは引き続き残件。全体計画の複数object、rig/weight/morph、入出力、Evidence/MCP等も未完了。

## 連続切断GUI

- `AuthoringWorkbench.CutPath` を分離。選択した辺を割合指定で末尾追加、複数行の順序付き入力、末尾取消、クリア、経路全体確定を提供。文字欄は1行に「頂点ID 頂点ID 位置%」。辺の位置は小ID→大ID、端点0/100を許容。
- 候補全体のPolygonCutPath検証に成功した場合のみピンクの折れ線と確定ボタンを有効にする。入力不正では文書を変更しない。foldout閉じで線消去、文書hash/編集段変更でdraft消去。確定は単一共通command、成功時のみ選択をクリア。
- 最新確認済みPlayer: `Builds/Windows-C1B-CutPath/NyaForge.exe`。log: `Logs/build-player-20260912-054921-238.log`。PASS: `Artifacts/Authoring-20260912-054946-3df604f130c9497ca1c0343b6ad78ae7/report.json`。`AuthoringWorkbench.CutPathVerification` で3地点preview、開閉、後半無効経路、確定の実クリック、2面→4面/6点→9点、1回Undo/Redo、native/Bakeを確認。既存suiteも成功。
- `cut-path.png` を目視し9点の配置と折返し説明、入力欄を確認。切断後の内側の辺は描画しないため、4面化の証拠はgeometry assertion。今回Core変更なし（直近227件成功）。
- 次は選択からの末尾追加、重複拒否、末尾取消/クリアの実クリック、編集段変更時のdraft消去を追加検証する。現状テストは経路文字欄を直接設定して確定する。複雑なseam/凹面/端点混在のGUI、経路の画面クリック直接指定、同面再訪/閉loop/曲線などは残件。

## 連続切断の共通command

- `PolygonCutPathOperation` を独立モジュールとして追加。2〜256個の不変EdgeCutLocationを読み取り専用配列へ複製し、順序付き経路を共通commandからPolygonEditingへ渡す。
- fingerprintには経路長・各辺の両端ID・正規化した割合を順番どおり格納する。既存operationのfingerprintは変更しない。同一command IDの同内容再送は再適用せず、位置や順序を変えた再送はCOMMAND_ID_REUSEDで拒否。
- Core **227 passed / 0 failed**。呼出元配列変更からの隔離、null/上限拒否、2面を横断する切断、後半で既存辺と重なる失敗時の文書hash/revision/頂点数保持、1回Undo/Redo、native polygon binary一致、Bake一致を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4b2dd4142810484d9717a0f3d8af1c38`。
- 初回は検証コードがDocumentRevisionをRevisionと誤記してコンパイル失敗。検証コードを修正し、全件成功を確認。
- 今回Playerビルドなし。最新確認済みPlayerは下記EdgeCutControlsで、連続切断はGUI未接続。次は順序付き辺位置の登録・削除・preview・確定/取消をGUIモジュールとして接続し、実操作と1回Undo/保存を確認する。
- 現在の経路は各元面を1回横断する範囲。閉loop/同面再訪/曲線、一般的なナイフ操作、複雑な非平面形状などは残件。全体計画のrig/weight/morph、複数object、各形式入出力、Evidence/MCP等も継続。

## 切断GUI追加検証と連続経路Core

- `AuthoringWorkbench.EdgeCutControlsVerification` を分離。選択した両端から辺A/Bへ実登録、0/100端点のpreview/確定（新頂点なし）、Undo、クリアボタン、foldout開閉、編集段変更時draft消去を確認。
- 最新確認済みPlayer: `Builds/Windows-C1B-EdgeCutControls/NyaForge.exe`。log: `Logs/build-player-20260912-053932-668.log`。PASS: `Artifacts/Authoring-20260912-053951-f9e3b76c329648c3be532b76e6f5bdc7/report.json`。既存suite成功。このbuildは下記CutPath Core追加前。
- `EdgeCutLocation` と `PolygonCutPath` を追加。2〜256箇所の順序付き辺交差位置を受け取り、各元面を1回だけ横断する。全点を候補へ挿入して隣接地点をFaceSplitで順に結ぶ。辺再訪/共通面なし/同面再訪を拒否。共有辺は1回だけ挿入し、既存の面ごとのcorner補間を再利用。
- Core **226 passed / 0 failed**。2quad横断→4quad/追加3頂点、共有点1個に両側UV seam保持、切断辺の共有、binary往復、辺再訪/飛び越し拒否、元mesh不変を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4951aecb64064a529cd302572b298301`。
- CutPathはまだ共通command/GUIへ未接続。次は経路payloadの複製/command fingerprint/replay/Undo/nativeを整える。複数回同面横断、閉じたloop、辺上再訪、非平面face、複雑な経路は未対応/未検証。単一面切断GUIは維持。
## 辺上切断GUI（前段記録）

- `AuthoringWorkbench.EdgeCut` を分離。2辺のID入力/選択2点から登録、百分率、候補検証、ピンク線、確定/クリアを提供。文書hash/編集段変更でdraftを破棄、foldout閉じで線を非表示。成功時だけ選択をクリアする。
- `BoundaryHighlightProjection` は既定の閉loopを維持し、開いた2点線も扱えるように拡張。線は候補の頂点から算出し、文書へは確定時の共通commandで反映する。
- 最新Player: `Builds/Windows-C1B-EdgeCut/NyaForge.exe`。log: `Logs/build-player-20260912-053717-140.log`。PASS: `Artifacts/Authoring-20260912-053736-0a15846425154bcf820bdb18132c55b4/report.json`。25%/75%線preview、2quad/6頂点、確定時線/選択消去、無効指定で文書/選択不変、Undo/Redo/native/Bakeと既存suite成功。`edge-cut.png`を目視し追加点と入力欄の折返しを確認。面内の線は保存後には表示しないため、切断の証拠は面数/頂点数のassertionによる。
- Core変更なし（直近225件）。選択からの辺登録ボタン/クリアの実クリックと、端点0/100、凹面/共有UV seamのGUIは追加確認対象。複数面横断と連続切断は未対応。
## 辺上の点を結ぶ切断Core（前段記録）

- `PolygonEdgeCut` / `PolygonEdgeCutOperation` を追加。異なる2辺が共有する1面を、各辺の指定位置で切る。割合は小vertex ID→大IDの0〜1、0/1なら既存端点を使い、内部ならPolygonEdgeInsertionで点/cornerを追加してPolygonFaceSplitへ接続する。
- 点追加と分割を不変の候補上で行い、1つの共通commandで確定する。途中の失敗は文書/ID履歴を変えない。既存のUV seam補間・planar/対角線検証を再利用。単一平面の切断であり、連続ナイフ/複数面横断はまだ実装していない。
- Core **225 passed / 0 failed**。quadの対辺25%/75%→2quad/追加2点、外周corner参照保持、切断辺共有、端点使用、同一辺/範囲外拒否、点追加後の無効分割で文書不変、再送/1回Undo/Redo/native/Bakeを確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-1ac5696bf6c0441c977a6b6678adad1e`。
- 今回Playerビルドなし。次はGUIで辺2つと位置を指定し、preview/確定/取消を扱う。shared seamや非平面/凹面の組合せ、大きいID/予算境界、複数面の連続切断は残件。
## 複数面dissolve GUI

- 既存FaceMerge GUIを「選択した面を1面にまとめる」に拡張し、DissolvePolygonFacesへ接続。2面以上で有効。既存2面の操作検証も継続。旧Merge core/commandは互換用に保持。
- `AuthoringWorkbench.FaceDissolveVerification` を分離。非連結2面の拒否時に文書/選択が保持されること、4面fan→1quad/中央点除去/残存面選択、Undo/Redo/native/Bakeを検証。
- 最新Player: `Builds/Windows-C1B-Dissolve/NyaForge.exe`。log: `Logs/build-player-20260912-053156-238.log`。PASS: `Artifacts/Authoring-20260912-053215-a665b305c67b4bc1b4cb882e86e4516a/report.json`。既存suiteも成功。`dissolved-faces.png`を目視し四角面/点4個と操作ボタンを確認。
- Core変更なし（直近223件）。Paint付き非線形UV、大きな選択、凹外周のGUIは残件。
## 複数面dissolveのCore（前段記録）

- `PolygonFaceDissolve` / `PolygonDissolveOperation` を追加。連続する同一平面/向き/材質の2面以上から内部辺を除去する。内部辺のUV/normal/tangent一致とmanifold/向きを検査。選択領域が接続し、外周が3〜256角の単純な1loopになる場合のみ確定。穴/分岐/離れた領域を拒否する。
- 最小face IDと残存cornerを保持し、内部で不要になった頂点を除去。別の面や元から独立している点は保持。ID履歴を維持し、候補の三角分割後に共通commandへ確定。既存2面Mergeは互換のため維持。
- Core **223 passed / 0 failed**。4triangle fan→1quad/中央点除去/外周corner参照保持/入力順不変/UV seam拒否/非連結拒否/穴付きring拒否、command無効時状態保持/Undo/Redo/native/Bakeを確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-fbce87e4252544ec9813ff68b724ac21`。
- GUI未接続、今回Playerビルドなし。非線形UVの内部補間は再三角分割で変わり得る。複雑な凹外周/大規模選択/全属性形式/Paint付き形状は未検証。
## 面なし下流の診断と復旧案内

- `AuthoringWorkbench.FacelessRecovery` に案内と「面がない編集段へ戻る」ボタンを分離。最終評価が未完了で、評価済みの面なしPolygonEditがある場合に表示。編集段リストの最初の該当段を選ぶ。文書は変更しない。面の作成またはUndoを案内する。面がない出力に材質を追加するボタンも無効化。
- Core **220 passed / 0 failed**。Mirror/Paint/材質の3経路で全面削除後のNO_RENDERABLE_FACES、編集値保持、後続node参照保持、native保存再読込、未完了Bake拒否、Undoによる復旧/Redoを確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4ae9064b4f164e979491ceb15d932dfb`。
- 最新Player: `Builds/Windows-C1B-FacelessRecoveryEdit/NyaForge.exe`。log: `Logs/build-player-20260912-052657-930.log`。PASS: `Artifacts/Authoring-20260912-052717-0b20fbda67bb4c1e9bc4cc6cb374cd15/report.json`。Mirrorの削除後に復旧ボタン実クリック/文書不変/編集可能/Undoで完了/Redo/native案内復帰と既存suiteを確認。
- 初回テストはMirror作成後の最終出力表示のままDeleteを呼び、編集contextなしで失敗。検証側でSelectEditStage(1)を明示して修正。初回記録: `Artifacts/Authoring-20260912-052618-a14af27d1abf4a01bd5da94eac013737/report.json`。
- 未確認: 保存済みPaint画像付きの新しい面へのrebind、複数の面なし編集段がある場合の選択導線、全UIの診断文言。今回Paint fixtureは既定画像生成nodeで、独自stroke画像の網羅試験ではない。
## 全ての面の削除（前段記録）

- PolygonDeletionは最後の面も削除可能。削除で不要になった頂点を除去し、元から独立していた点とID割当履歴を保持する。面なし時はrender生成を呼ばない。GUIの全選択時無効化を解除。
- 属性はcornerに属するため全削除時に消える。再作成の最初の面は既定UV/normal/tangentを生成する仕様。以前の属性はUndoで復元する。空状態に旧属性形式を持ち越す機能は実装していない。Weldによる全collapseは引き続き拒否。
- Core **217 passed / 0 failed**。独立点保持/ID履歴、再追加から面作成、最終面削除のgraph/native、面なしBake拒否、Undo/Redoを検証。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-5152d96c71ef445caa70f3445d259479`。
- 最新Player: `Builds/Windows-C1B-DeleteAll/NyaForge.exe`。log: `Logs/build-player-20260912-052210-125.log`。PASS: `Artifacts/Authoring-20260912-052228-1b324910f8b845539f5b045b213cb375/report.json`。最後の面の削除ボタン/空表示/操作有効性/native再読込/Undo/Redo、既存suite成功。検証処理は `AuthoringWorkbench.DeleteAllVerification` に分離。
- 未確認: Paint/材質付き作品の全削除後の下流診断、全属性形式での再作成、scale100/translation。次は面なし時の下流nodeとGUI操作の対応を確認し、ユーザーに回復方法を示せるようにする。
## 空PolygonのGUI統合（前段記録）

- 「空の形状から始める」を追加。面なしpreviewでもOwnedMeshProjectionは編集点を生成する。最終結果の重ね表示はMeshがある場合のみ。0点のFrameはdefaultへ戻す。GUIの未完了表示と面なし案内を分け、移動判定はPolygonの有無を使う。UV投影/厚み/Paint追加は面なし時に無効化。
- 最新Player: `Builds/Windows-C1B-EmptyPolygon/NyaForge.exe`。log: `Logs/build-player-20260912-051859-316.log`。PASS: `Artifacts/Authoring-20260912-051918-36d0f04cd13e4640b52b537f5a9cc4a2/report.json`。空開始ボタン、1/2/3点追加と各native再読込、最初の面、Undoで点のみ/Redo、面の再読込/Bakeを確認。既存suiteも成功。`first-face.png`を目視し三角面と編集欄を確認。
- Core変更なし（直近215件）。まだ全削除を許容していない。面なし状態で材質など全機能のボタン有効性/診断を網羅していない。scale100/translationや最初の面の別属性、面なしプロジェクトの旧版互換性説明は追加検証対象。
## 面なしgraph/command/native統合（前段記録）

- GraphMeshValueは面なしpolygonに限りMesh=nullを許容し、snapshotへ面なし識別とpolygon hashを含める。既存meshありのhashは維持。PolygonSource/PolygonEditで面なしを評価成功として流し、Outputでは画像接続なしの場合に通す。その他のnodeはNO_RENDERABLE_FACESで拒否する。
- PolygonEditingは点追加/移動/最初の面作成に限り面なし入力を許可。面あり候補のrender検証は維持。preview.IsCompleteは評価完了を示し、Mesh非nullとは独立。workspace.Evaluate()はこの状態でnullを返す。
- Bakeは面なし最終出力をNO_RENDERABLE_FACESで拒否。最初の面作成後は出力可能。空sourceの出力由来ハッシュはpolygon正本hashを使う（既存meshありsourceは従来通り）。初回試験でこのbaselineのnull参照を検出して修正。
- Core **215 passed / 0 failed**。空graph/点の追加ごとのnative保存再読込/移動/UV拒否時状態不変/最初の面/Undoで面なし/Redo/再読込/Bakeを確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-219820f0bc8f48c7b3c45d7b510f5942`。
- GUI/Playerはまだ面なし対応していない。今回は新規Playerビルドなし。次はOwnedMeshProjectionのmesh=null時の点生成とWorkbenchのrendering依存条件を修正し、空から始めるGUIを接続する。Mirror/材質/Paint等での診断、全削除、空初期属性の維持も残件。
## 空PolygonのCore対応（前段記録）

- PolygonMeshで0頂点/0面、1点/2点を許容し、空ID集合の上限を0にする。既存の面/参照/ID検査は保持。
- PolygonBinaryCodecは面なしだけv3（ID上限付き80byte header、属性flags0）として保存。v1/v2の非空条件と通常のwriterは保持。PolygonNewFaceは最初の面にUV/normal/tangentを生成。PolygonEditPointsは面なしにも対応。renderはNO_RENDERABLE_FACESで明示拒否。
- Core **214 passed / 0 failed**。空/履歴/1点/2点保存の往復、旧versionへの偽装拒否、truncated/flags拒否、最初の面とv1への移行を検証。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-5abea58ea32a4425ba14e022ca4db146`。
- 今回Playerビルドなし。graph/command/GUIは面なしのまま編集を継続できる段階ではない。GraphMeshValueがmesh非nullを前提とするため、次の統合を [空Polygon統合設計](docs/Empty-Polygon-Integration.md) に記録。native project schema3とpolygon blob v3を混同しない。全削除/属性形式の維持は残件。
## 点描画の集約と256点制限撤廃

- `EditPointProjection` に点表示の所有・描画・選択・破棄を分離。1点1GameObject/sphereから、2,048点を1meshにまとめた八面体マーカーへ変更。各pointは6頂点/8三角形、未選択/選択の2submesh。materialはownerから借り、mesh/rootは自身で破棄。面モードではrendererを非表示。選択不変時はindices再生成を省く。
- `OwnedMeshProjection` がこのモジュールを所有しtransactionの候補破棄/置換に追従。`PickVertex` は全編集点を探索し、最初の256点で打ち切らない。クリック探索はO(n)、選択変更は全バッチのindicesを更新するため、大規模性能の最終解ではない。
- 最新Player: `Builds/Windows-C1B-PointBatches/NyaForge.exe`。log: `Logs/build-player-20260912-050918-492.log`。PASS: `Artifacts/Authoring-20260912-050938-b223ab6f9c074ab3adfb4fbce6ee15a8/report.json`。2,053編集点/2バッチ、末尾2052番の実クリック→stable ID2053、全選択/解除、最終出力4点/1バッチ→編集2,053点/2バッチを確認。既存suiteも成功。`many-points.png`を目視し選択点と密集点描画を確認。
- Core変更なし（直近212件）。今回は通常scaleの2,053点。10万点等のCPU/GPU/メモリ計測、密集点の見やすさ、画面サイズ一定の点、近接/重なり選択、空polygonは残件。
## 追加点から面へのGUI一周

- `AuthoringWorkbench.LooseVertexWorkflowVerification` を分離。移動ボタンでX+10/Y-20mm、未接続点marker位置更新/三角形hash不変、Undo/Redoを確認。面作成GUIで4→1→5の三角面を追加し、編集対応表から未接続点が除かれることを確認。Undoで未接続点へ戻す、Redo/native再読込/Bakeまで検証。
- 初回suiteは次のUV検証で `UV wheel did not zoom`。前のfoldout変更後にlayout未確定のままScrollToしていたため、`AuthoringWorkbench.UvEditingVerification`で2frame後にScrollTo、さらに2frame待ってから操作するよう変更。製品の入力処理は変更なし。
- 最新Player: `Builds/Windows-C1B-LooseToFaceLayout/NyaForge.exe`。log: `Logs/build-player-20260912-050614-640.log`。PASS: `Artifacts/Authoring-20260912-050631-51fb3b907b66419980a9ec93b56e5c67/report.json`。`loose-to-face.png`を目視し追加点から伸びた三角面と操作欄を確認。初回失敗: `Artifacts/Authoring-20260912-050529-f3ca2f8a83024baea79d74bf975489d8/report.json`。
- Core変更なし（直近212件）。今回はscale1の小型fixture。scale100/translation、複雑なseam、256点超の描画/クリックは残件。
## 未接続頂点GUIと編集点対応

- `PolygonEditPoints` はrender頂点列をprefixとして保持し、未接続頂点をstable ID順で追加。正本/三角形mesh/UV/Bakeは変更しない。`OwnedMeshProjection` は編集段でこの点列を表示・framingし、`SelectedPolygonVertices`と移動commandは同じ対応表でstable IDへ変換。
- `AuthoringWorkbench.VertexCreation` に座標GUIを分離。モデル原点からのmm座標をscaleでmesh-localへ換算して共通commandへ渡し、成功時に追加点を選択する。
- 最初のPlayer検証でmesh hashが不変な際のColorUpdate再利用が新頂点表示を省く問題を検出。編集polygon参照も再利用条件に含め、未接続点追加/位置変更と最終出力への切替時には表示を再構築するよう修正。
- Core **212 passed / 0 failed**。既存試験へ編集点prefix不変/末尾stable ID検査を追加。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-c5f8c6a33ddc4810be616bb011b900d3`。
- 最新Player: `Builds/Windows-C1B-LooseVertexCache/NyaForge.exe`。log: `Logs/build-player-20260912-050245-504.log`。PASS: `Artifacts/Authoring-20260912-050302-191afb1243194c0d8bfbe5ff9b90cc40/report.json`。追加ボタン/編集5点/描画4頂点/クリックstable ID選択/Undo/Redo/native再読込/Bake、既存suite成功。`loose-vertex.png`を目視し独立点と座標欄を確認。初回失敗: `Artifacts/Authoring-20260912-050145-db3f6efe59ff4419bbf8c8dbd32fffbe/report.json`。
- 次の検証: 未接続点の移動→面登録→面生成のGUI一周、scale100/translation、seamを持つmeshでの対応表、final切替とcache再構築。既存の256点marker/クリック上限は残っており、大規模meshでは後方の追加点をクリックできない。点描画の集約と選択範囲拡張が必要。空polygon対応も残件。
## 面作成操作検証と新規頂点Core（前段記録）

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

1. 次はPolygonCutPathを共通commandへ接続し、経路payload/fingerprint/失敗時原子性/Undo/replay/nativeを検証する。その後GUIの経路登録へ進む。連続した複数面の切断も引き続き目標として保持する。保存済みPaint画像の全削除後rebindと面なし複数編集段の導線も残件として保持する。点描画の大規模性能/密集時の選択も残件。render indexとstable IDの境界、既存Paint/UV/出力の意味を保護する。選択順/向き/既存辺の共有/UVとIDの扱いを明確にしてCoreから共通command/GUIへ接続する。既存stable ID/属性/共通command/Undo/native/GUIの境界を維持する。不等頂点数bridgeも残件。自由面作成・vertex weld/多面merge・連続knifeも維持。複数島/拡縮後のドラッグ、Paint付き作品の動作とUV回転/拡縮ハンドルも維持する。merge/bridge/cut、空polygonと削除後のID割当は残件。scene override、GUI実操作、staging登録前中断/未確認新規GUIDなども保持。復旧だけの類似テストを増やし続けない。複数材質の同UUID共有receiver fixture、部位別GPU定量/alpha、mask/重なり遮蔽は検証残件として保持。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1B-EdgeCutControls -Width 1280 -Height 800 -TimeoutSeconds 600
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `Assets/NyaForge/Rendering/`: Playerとreceiverが共有する材質adapter/shader。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/create/curves、全削除・空polygon、UV回転/拡縮handle・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。



























