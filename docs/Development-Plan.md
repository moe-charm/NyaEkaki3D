# NyaForge v2 開発計画

更新: 2026-09-12。C0-R／C1-A、C1-Bの形状・UV編集、C1-CのPaint graphに加え、小物形状のGUI一周、layer/mask/PNG取り込み、3D paintと初期Surface往復まで検証記録あり。3D描画の準備をGUIから非同期化し、準備後の描画とviewport更新をPlayerで検証。最新の実装・証拠・再開手順と残件はcurrent_taskへ集約する。以下の終了条件は段階ごとの開発契約であり、C1全体の完了を意味しない。
製品目標と技術契約は [設計v2](NyaForge-Authoring-Design2.md) が正本。この文書はローカル実装への適用順を定める。初期対応OSはWindows。macOS対応は今回の工程へ加えない。

## C1-A開始時の比較表（進捗はcurrent_taskへ集約）

HEADは `6e1e4fc6d4c88b1b5f3368f3706bcec21a0a9e36` だが、制作Core・GUI・Bridge等は未コミットの作業ツリーに存在する。設計v2の第2節は同じコミットを調査した記録であり、次表はC1-A開始時点の比較で、現在はgraph文書・保存・command・canvas・Bakeまで進んでいる。既存変更を上書き・破棄しない。

| 領域 | コードで確認した現状 | v2へ向けて必要な変更 |
|---|---|---|
| 文書／command | `Domain/`・`Commands/`に分離。空projectと最大1object、offset、追加Undo、revision・再送判定・transaction | graph/asset参照、複数object、共通commandの拡張 |
| mesh | `Geometry.cs` は三角形と頂点配列、UV/normal/tangent/submesh保持。offsetはbaselineの配列indexに対応 | 制作vertex/edge/face/corner ID、domain、描画分割との対応表 |
| 保存 | native schema 2でobjects配列、hash付きblob、writer lock、保存version。旧schema 1は読込＋別フォルダ保存で移行。GraphBlobStoreは任意graph・未知payload・未解決状態を別アセットとして保存可能 | project manifestへgraph参照を統合し、旧schema 1/2の移行を保護 |
| 新規作成 | objectゼロから開始。サンプル追加と新規作成を分離 | C1でprimitive生成をgraphへ統合 |
| GUI | 独立した制作画面とvertex選択・mm移動、Viewerのファイル選択／折り畳みパネル | 同じ制作文書に接続する小さなruntime node canvas、編集段表示 |
| 出力 | `BakeStore.cs` とUnity Bridgeでstatic meshを別Unityへ渡す | graph評価結果の出力、以降UV/paint/rig/morph/target対応・再出力identity |
| 入力 | 生成済みpackの表示とnative制作保存の読込 | GLB/VRM等のadapter、pack編集可能性検査。空projectの完成を妨げない |
| グラフ | 型付きgraph、循環・domain検査、Plane／EditMesh評価、固定static objectへの接続、アセット保存 | 任意graphの文書commit、共通command、incomplete時のpreview管理、runtime node UI |
| 未実装 | 面編集、UV editor、paint、rig editor、MCP transport | C1以降で順次実装。現在の能力として表示しない |

文書はobjectゼロを表現できる。mesh自体の3頂点以上等の検査は維持し、空meshで偽装しない。現在は自作plateを明示的に追加するところまでで、自由な形状生成・造形はC1以降。

## C0〜C5の扱い（計画開始時の評価）

| 段階 | 現在の評価 | 次の到達点 |
|---|---|---|
| C0 基盤 | 部分成立。既知の頂点移動・保存・Undo・Bridge往復の過去検証あり | 空projectとsource非依存の入口、基盤の予算・未対応条件を明示 |
| C1 ノードと小物 | 基盤実装中。graph評価を既存編集へ接続済み | 任意graphの文書化とruntime canvas、直接編集・UV・色・画像・MCPを含む小物制作を一周 |
| C2 全身キャラ | 未着手 | 低ポリ全身、目・口、manual rig/weight、blink/jaw、髪束の揺れを受け取り先で確認 |
| C3 衣装・髪・顔 | 未着手 | 新規衣装と複数poseでの修正、顔・髪の操作を実用化 |
| C4 品質 | 未着手 | sculpt、retopology、high→low bake、変形品質 |
| C5 互換性 | 未着手 | 最適化、GLB/VRM等のprofile、最終環境の確認 |

旧NF-0/NF-1をC0/C1の完了と機械的に読み替えない。旧開発順の「pack編集→MCP→チョーカー」をそのまま継続せず、空project→graph→直接編集と出力を進める。

## 次の変更単位

以下のC0-R、C1-A等はこの計画内の小分け名であり、設計v2の段階を追加・置換するものではない。

### C0-R: 空projectと既存データの保護（実装・検証済み）

対象: `Assets/NyaForge/Authoring`、`Assets/NyaForge/UnityRuntime`、`Tests/Authoring.Core`。Viewerへの接続変更は新規/openの入口に限定する。

1. 現行schema 1の保存サンプルと評価hashを自作fixtureで確保し、移行の比較元にする。
2. baseline必須の文書から、objectゼロを許すprojectモデルへ拡張する。mesh自体の妥当性検査は維持する。
3. `project.create/open` とobject追加の責務を定義する。未保存変更は現行の確認を経て扱い、instance/revision/commandIdの契約を維持する。
4. 新規projectは空から始める。plateは明示したサンプル追加として残し、パック・アバター・animationなしで保存と再読込を通す。
5. 空の表示に「形を追加して始める」案内を出す。カメラ・選択は制作Undoへ混ぜない。

終了条件:

- パックなし、objectゼロで新規→保存→再起動→読込が成立し、勝手にplateが増えない。
- 空projectの撮影は背景・案内を正常に扱う。出力は対象なしを明示し、架空のmeshや成功を返さない。
- 新規作成のキャンセルで元文書と未保存編集が残る。
- 旧schema 1のprojectを開け、基準mesh・offset・表示寸法を失わない。新形式への保存は元ファイルを上書きしない移行経路から始める。
- scale 1/100の既存編集・Undo/Redo・保存・static Bakeの回帰を維持する。

### C1-A: 最小の型付きグラフとnode canvas（自動往復確認済み・手動受入待ち）

現状と検証の正本は [current_task](../current_task.md)。任意graph文書・schema3保存・共通command・runtime canvas・編集段・graph Bakeに加え、配置永続化とruntime pointerイベントの制作往復の検証記録がある。OS入力・キーボード・IME・ユーザーの手動受入とは区別する。終了条件は以下を維持する。

- documentにgraphを置き、旧offset layerはEditMeshのpayloadとして表現する。別のlayer評価順を第二の正本にしない。
- runtime UI Toolkitで追加・接続・切断・選択・parameter変更と処理段previewを実装する。Editor専用GraphViewをPlayerへ入れない。
- node配置はWorkspaceState、接続とparameterは制作commandへ送る。ノード操作と直接編集に別のUndoを作らない。
- 未接続graphは明示したincomplete状態として保存可能にし、古いpreviewのrevisionを表示する。最新結果の出力成功とは扱わない。
- 型違い、循環、古いrevision、古い編集contextを拒否し、失敗時に文書・履歴・表示を保つ。未知nodeのpayloadは保持する。

終了条件: Windows PlayerでPlane生成→EditMesh→Outputを接続し、parameterと頂点を変更、Undo、保存・再読込、static Bridgeへ出力できる。これはC1全体の完了ではない。

### C1-B以降: 制作meshと小物の一周

- polygon/cornerと安定ID、RenderVertexMap/RenderTriangleMapを入れ、編集ケージの選択が分割後の表示へ正しく対応することを先に検証する。
- 面の押出し、mirror/厚み、単純UV投影、base color paint・保存を完成例に必要な順で追加する。小物例はplaneから直接編集する色付きペンダントを候補とする。
- EvidenceとMCPを共通commandへ接続し、同じ操作のgeometry hash・画像・数値を照合する。MCP実装はUI操作の自動クリックを正本にしない。
- C1の完成例を通したらC2の低ポリ全身制作へ進む。小物のバリエーションだけを増やし続けない。

### C1-C: 色付き小物の保存・出力

Paint GUIの既存コードを検証し、未確定previewと文書commitを分離したまま操作を完成させる。続いてUV変更時の依存表示・明示rebind/rebake、texture付き出力へ進む。一般Material graph、layer/mask、3D paintは段階追加する。

終了条件は、色付き小物をGUIで作成し、Undo・native保存・再読込で再現し、画像と材質を含む対応profileで外部へ渡せること。現状のmesh-only Bake拒否を、画像の黙った欠落に置き換えない。GUI実装・Player試験・画像確認・手動受入・受け取り先確認を別々に記録する。C1全体のEvidence／MCP残件は維持する。

### C2開始: Rigコア（実装着手）

`Assets/NyaForge/Authoring/Rig` にUnity非依存のrest skeletonとskin bindingを置く。boneはstable IDと親子関係、head/tailを持ち、bindingはmesh topology hash・skeleton hashを固定して1〜4本の正規化weightを保持する。`PoseTransform` と `SkinDeformer` はrest-relative affine poseを適用し、位置だけを線形ブレンドする。循環、未知bone、未weight、重複、上限超過、stale topology/skeleton/poseは候補生成・評価時に拒否する。

`rig.skeleton`（NYRS v1）、`rig.skin-bind`（NYRB v1）、`rig.pose`（NYRP v1）、`rig.skin-deform`（rest-relative変形）の評価・保存・inspectionまで接続済み。Graph canvasのRigサンプルとRigパネルのbone選択、weight混合、Root 100%割当、ブラシweight paint、pose XYZ回転、rest bone移動、stale依存の明示再bindを実装し、Windows Player内の専用検証で評価・inspection・schema3再読込、1操作1Undo、pose Undo/Redo lifecycleを確認した。Morphについても、mesh topology hash付きの疎なrest-space頂点差分（`MorphTarget`/`MorphSet`）、0..1適用（`MorphDeformer`）、`NYRM` v1 codecに加えて、`rig.morph-set`／`rig.morph-deform` typed graph、native保存、inspection、Graph canvasのMorphサンプル、Workbenchのtarget/weight編集を接続した。GLB v2はboundedな複数triangle primitiveをsubmeshへ結合し、共通属性レイアウトとPOSITION morphを保持する最初の実データ入口まで接続し、別adapterでtranslation-onlyのskinとJOINTS_0/WEIGHTS_0を既存Rig graphへ接続した。CoreとWindows Playerで変形・属性保持・往復・stale拒否を確認した。これはC2全体の完了ではない。次の変更単位でVRM/humanoid adapter、一般node transform、normal/morph連携、skin exportを順に接続し、各段階でrest→poseの数値検証と受け取り先確認を追加する。実マウスでの手動見た目受入は別に確認する。

## 移行時に固定する判断

| 論点 | 方針／実装着手時の確認 |
|---|---|
| 設計versionと保存version | 別物。native schemaを変える変更でcodec・migration・旧形式のfixtureをまとめて追加。Bake schemaは必要が生じるまで変更しない |
| 旧offsetからEditMesh | baseline hashと配列対応を保存する。旧データからquadやseamの同一頂点を推測復元しない |
| 三角形形式の再利用 | 既存MeshDataは旧形式adapterと表示／出力に活かす。polygon/corner正本を後から無理に頂点配列へ押し込まない |
| 上流変更 | 対応が壊れたpayloadは未解決として保持。indexが同じという理由だけで適用しない |
| 外部依存 | 現在のmanifestにgeometry/graph/MCP候補は未導入。必要な変更単位で版・ライセンス・runtime対応・属性保持を実証して採用 |
| 実アバター | private packは確認用入力のまま。編集可能性の検証前に制作対象へ昇格させない |
| 出力 | C0/C1はstatic Bridgeの保証範囲を明示。skin/morph/VRChat対応は専用fixtureと受け取り先確認が必要 |

## 検証と作業記録

初期の結果は [実装履歴](history/2026-09-11-Initial-Authoring-and-Navigation.md)、C0-Rの新しい証拠は [C0-R記録](history/2026-09-11-C0R-Empty-Projects.md) に保存した。次のコード変更では変更範囲に応じ、順番に実施する。

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Build-NyaForge.ps1 -Target Player
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -Width 1280 -Height 800
```

空projectやgraphの回帰は上の既存試験に追加する。Viewerを触った場合は `Tools/Test-NyaForgeNavigation.ps1`、出力を触った場合は `Tools/Test-NyaForgeUnityBridge.ps1 -PlayerCheckDirectory <今回のAuthoring検証出力>` も実施。新たな依存の導入はPlayerビルドで確認する。

数値試験、描画画像の確認、ユーザーの手動操作、受け取り先の動作を区別する。合格した範囲・未確認・次の作業をcurrent_taskへ記録し、長くなった完了記録はhistoryへ移す。





