# 面がないPolygonの統合

更新: 2026-09-12。Coreの正本/バイナリ保存は実装済み。以下のgraph/GUI統合は未実装。

## 実装済みの契約

- PolygonMeshは0頂点/0面、1点/2点も表す。面がある場合のcorner参照/属性/ID/予算検査は維持。
- 面なしblobのみversion3、80byte header（ID上限含む）、corner flags=0。v1/v2の非空条件と既存writerのbyte列は維持。最初の面はUV/normal/tangentを生成し、通常のv1/v2へ戻る。これはpolygon blobのバージョンでありnative project schema番号とは別。
- PolygonEditPointsは面なしならstable ID順の全点を返す。PolygonRenderAdapter.BuildはNO_RENDERABLE_FACESを返し、空の三角形MeshDataを偽装しない。

## 次工程の設計

1. GraphMeshValueで「polygonとして解決済み、描画Meshなし」を表現する。snapshotにはpolygon hashを含め、既存meshありのhashを変えない。null meshを受ける条件をpolygon面なしに限定する。
2. GraphEvaluatorのPolygonSource/PolygonEdit、PolygonEditing.Applyを面なしに対応させる。AddVertex/Move/最初のCreateFaceを共通commandで検証。面がないと使えないUV/Paint/材質処理は明示的に拒否し、null参照例外にしない。
3. AuthoringPreviewの評価成功と描画可能性を分け、command/保存は成功、Bakeは明確な理由で不可とする。Undo/Redoで面あり/なしを往復するnative保存試験を追加。
4. OwnedMeshProjectionは描画meshなしでもrootと編集点を生成する。Workbenchの選択/移動/追加/面順序入力はpolygonを基準とし、renderingの有無だけで操作を停止しない。Frameは0点時のdefaultを使う。
5. 空polygonで始めるGUIを追加し、0→1→2→3点の保存/再読込、最初の面、Undoで面なし、Redo、BakeまでPlayerで通す。

全削除は別に、最後の面削除時の不要頂点/属性形式の扱いを定義する必要がある。現行DeleteFaces/Weldの全削除拒否はまだ変更しない。

## 2026-09-12 graph統合の到達点

項目1〜3の基本経路を実装し、Core215件成功。Mesh=nullは面なしPolygonに限定。PolygonSource/Edit/画像なしOutputだけが面なしを通す。点追加/移動/最初の面の共通commandとnative保存、Undo/Redo、面生成後のBakeを検証。空sourceのBake baselineはpolygon hashを採用。UI/Projectionの項目4〜5は未実装で、新Playerにはまだ提供していない。評価完了と描画可能性をGUIでも区別する必要がある。

## GUI統合の到達点

2026-09-12: 空開始/面なし編集点/案内/一部ボタン有効性を実装。Windows-C1B-EmptyPolygonのPlayerで0→3点の保存再読込、最初の面、Undo/Redo/Bakeを検証済み。全削除、全機能の面なし入力診断、旧版互換性案内は残件。

2026-09-12: 全面削除を実装。削除により不要になった頂点は除去、既存独立点とID履歴は保持。corner属性は面とともに削除され、再作成は既定属性を採用する。Core217件、Windows-C1B-DeleteAllのGUI検証成功。Paint/材質付き全削除後の回復操作は残件。
