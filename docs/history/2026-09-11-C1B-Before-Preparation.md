# NyaForge 開発タスク

更新: 2026-09-11。C1-Bの制作mesh基盤を実装中。C1-Aの自動制作往復を確認、手動受入は未実施。Windows先行、macOSは将来。製品全体は未完成。

## 開発の入口

ユーザー指示: 予定どおり開発し、最初から責務を分けたモジュール構成にする。
読む順: このファイル → [開発計画](docs/Development-Plan.md) → [設計v2](docs/NyaForge-Authoring-Design2.md) → 対象コード。[文書一覧](docs/README.md)も参照。
目標はVRキャラ・衣装・小物をBlenderなしで造形から出力まで制作するアプリ。ノードと直接編集は同じ文書・command・Undoを使う。C1-Aの完了を全体完成としない。

## 現在の実装

- C0-R: 空project、static object追加・offset・Undo/Redo・保存・static BakeとBridge。
- Graph: MeshSource / Plane / EditMesh / Output / Scalar、型・循環・domain検査、未知payloadと未接続状態を保持。
- Domain: 任意graphを不変文書として所有。旧static profileと区別し、旧APIで平坦化しない。
- 保存: static writerはnative schema 2、graph writerはschema 3。readerは1/2/3。profile変更は別フォルダへ保存。旧fixtureは固定済み。
- Commands: graph追加、node追加・削除・更新、Output指定、接続・切断、context付き頂点移動。既存Undo・revision・再送・rollbackを共有。
- Preview: 現在評価と最後の成功出力、そのrevisionを保持。未接続commitを出力成功と区別。再読込で古いpreviewを捏造しない。
- Unity: OwnedMeshProjectionがgraphのmesh／transformを表示。Workbenchでschema 3を開き、未完了／古い表示revisionを明示。表示なしでもRefreshできる。

GUIに「Planeグラフから始める」とEditMeshの表示・編集段選択を追加。選択した処理段の結果を描画し、そのinput contextで頂点移動する。上流変更でcontextが変わった場合は選択解除し、未解決payloadは編集しない。最終出力は確認用。layer操作はstaticに限定。

runtime node canvasを追加。上部の表示切替で開き、Plane/EditMesh/Output/Scalarの追加、出力→入力クリックによる接続、切断、寸法・値の適用、EditMesh選択、有効切替、ノード削除、最終Output指定ができる。見出しドラッグ終了と制作保存時に配置をローカルworkspace状態へ保存し、制作Undoへ混ぜない。

graphの最終評価結果を既存static Bakeへ出力できる。未完了時は古いpreviewを出力せず、フォルダ作成前に拒否。配置の永続化は実装済み。runtime pointerイベントでGUI制作往復を確認。OS入力・キーボード・IMEと手動の使い勝手は未確認。

## 次の作業

C1-Bで `Authoring/Topology/` を追加。安定vertex/face/corner ID、endpoint IDによるedge、polygon/corner属性、凹多角形の三角形化、RenderVertexMap/RenderTriangleMap、旧triangle入力adapterを実装。UV seamで表示頂点を分割し、位置は共有した制作vertexとして動かす。詳細は [Topology契約](Assets/NyaForge/Authoring/Topology/README.md)。

polygon保存codecとPolygonSource／PolygonEdit graph payloadを統合済み。native schema3のgraph blobからpolygon blobを参照し、面・角・安定IDを保持する。PolygonEditは元形状を変更せず、入力snapshot/domainに結び付いた編集結果を保持。安定IDの頂点移動は共通command・Undo・保存に接続した。

GUIの「四角面から始める」で実quadを作成できる。描画頂点の選択をRenderVertexMapで制作vertex IDへ変換し、seamの重複IDは1回だけ動かす。旧render-index EditMeshでpolygonを誤編集しないよう拒否する。次はface選択・押出しと、その対応ID／属性の方針を実装する。面編集GUIは未実装。

1. EditMesh選択・編集contextは実装済み。複数処理段とscale100のGUI回帰をcanvas導入時に拡張する。
2. node操作のpointer入力とparameter適用の検証を広げる。canvas本体・card・配置保存・検証を分離済み。配置保存先はApplication.persistentDataPath/Workspace/GraphLayoutsで、document IDとgraph IDを検査する。破損時は警告と標準配置で制作文書を表示する。
3. graph Bake adapterは実装・Bridge検証済み。将来のnode種類追加時は出力元と法線属性の契約を拡張する。
4. PlayerでPlane→EditMesh→Output、parameter／頂点変更、Undo、保存・再読込、Bridge出力を一周。GUI画像も確認する。

上記の自動経路は成立。次の実装はC1-B: 三角形の表示配列とは別に、安定vertex/edge/face/corner IDと編集ケージを導入し、RenderVertexMap/RenderTriangleMapの契約を実装する。旧triangle保存を推測でquad化せずadapterとして維持する。押出し等のtopology編集はこの対応が検証できてから共通commandへ追加する。

以降はC1の制作polygon/corner・UV・paint・MCPを含む小物制作、C2全身キャラ、C3衣装・髪・顔、C4品質、C5互換性。全体要件は設計v2に保持する。

## 最新の検証

- Core: **79 passed / 0 failed**。PolygonEditのscale1/100、元形状保持・Undo/Redo・native再読込・Bake・古いcontext拒否を追加。
- Player: `Builds/Windows-C1B-PolygonEdit/NyaForge.exe`。
- ビルド成功: `Logs/build-player-20260911-193644-195.log`。
- Player pass: `Artifacts/Authoring-20260911-193706-f57016cfd50442cd958d6b911ba18ece/report.json`。
- GUIの四角面開始・stable-IDへの選択変換・10mm移動・native再読込・Bakeを追加検証。まだface選択／押出しの確認ではない。
- Playerで五角の凹polygonをgraph保存・再読込し、5cornerの制作faceを維持したままBakeできることを追加検証。
- Playerで凹polygonの三角形化・face対応表・Unity Mesh生成を追加確認。polygonのGUI編集確認ではない。
- UIイベント経由でnode追加・接続・寸法Apply・drag・頂点pick/移動・保存・再読込・出力を一周。深い出力先で一時ファイル名が長くなる不具合を修正。詳細は [Pointer往復記録](docs/history/2026-09-11-C1A-Pointer-Roundtrip.md)。
- 新しいcanvas instanceで配置復元、文書hash/revision不変、破損配置での標準表示を確認。検証配置はartifact内のworkspace-layoutsへ隔離。
- graph生成・切断と古い表示revision・schema3再読込・再接続・Undoを追加検証。既存static scale1/100編集・保存・Bakeも回帰成功。
- GUIのPlane開始、EditMesh選択、10mm移動、上流変更時の選択解除・編集停止、Undo復元、最終出力の編集禁止もPlayerで確認。
- canvasのボタン／ポートと同じhandlerで、空からの追加・接続・最終Output指定・誤接続の拒否・編集段選択をPlayer検証。物理pointerでの操作受入とは区別する。
- graph-editing.pngにcanvasと3D結果を撮影。直前ビルドの同画面を1280×800で確認し、最終版では多数nodeのcanvas領域を拡張。レイアウト変更後の撮影が黒くなる問題はcamera再描画で修正済み。
- graph sourceのscale1/100でGUI編集→schema3再読込→Bake→別Unity受け取りを検証。Bridge最新: `Artifacts/BridgeReceiver-20260911-190806-977-4fcbd9da9108443ab765b13236d61f86/bridge-report.json`、6項目pass。詳細は [graph Bake記録](docs/history/2026-09-11-C1A-Graph-Bake.md)。

## 作業境界

`Assets/NyaForge/Authoring/`のDomain・Graph・Commands・Projection・PersistenceはUnity非依存。`Assets/NyaForge/UnityRuntime/`は表示・操作・Player検証。
作業先は `Z:/TextureVoice_local/git/NyaForge`。多数の既存未コミット変更を保持。commit/pushは未実施。privateモデル・画像・packをコピーしない。ユーザーの既存Playerは終了させず、別BuildNameへ出力する。
以前の詳細は `docs/history/2026-09-11-C1A-Progress-Snapshot.md` に保存。その「次」「未実装」は当時の記述。現在の指示にはこのファイルを使う。
