# Workbenchの揺れプレビュー

モデル取込パネル内の「揺れのプレビュー（VRM1）」から、再生/再開、一時停止、リセットを操作する。取込済みrig/Spring sessionがあり、評価可能なPose nodeが1個あるgraphが対象。VRM0や一般node変換の未対応条件は引き続き診断する。

## 所有と表示

- `AuthoringWorkbench.SpringPlayback` が操作・ライフサイクルを担当し、Coreの `Vrm1SpringPreview` が計算を所有する。
- 開始時に最終出力表示へ切り替える。毎フレームは保存graphからのbase poseを使い、揺れた出力poseで別のgraph値を一時評価する。
- `OwnedMeshProjection.Spring` がその最終meshと材質を表示する。workspaceのgraph、評価結果、attachments、Undoには書かない。
- 保存、通常出力、確認画像用のsnapshotは編集中の姿勢を使う。UIにも「保存対象外」を表示する。
- リセットは一時ownerを破棄して編集中の姿勢へ戻す。編集、作品切替、metadata変更、編集stageへの切替でも破棄する。再生時の入力・計算・表示評価エラーは診断して通常表示へ戻す。

現段階は一時graph評価とprojection再構築を使う。大きなmeshのフレーム時間・allocationは未評価で、実用サイズでの最適化/受入が必要。0.25秒を超えるフレーム時間もCoreの診断対象であり、無言で未消化時間を捨てない。

## 取込表示の修正

新規Player検証で、直接取込後のgraphは存在してもprojectionが空のままになる経路を検出した。`CommitImportedGraph` のAddGraph commandへ既存projectionを渡し、表示候補もcommandの成否と合わせて公開するように修正した。これは静的GLB/skin双方の共通経路。

## 検証範囲

Windows専用の `SpringPlaybackVerification` は自作VRM1を読み、再生により表示頂点が動くこと、graph/metadata不変、停止/再開、Reset、Save/Openで一時姿勢を保存しないこと、編集で停止することを確認する。自動時間更新を止めて固定時間を渡すhandler検証であり、OSの実マウス操作・実モデル・文字サイズや隠れの目視受入は別途。

VRM0 root展開、複数Pose node、一般node変換、実アバター、負荷・GUI目視の受入は未完了。I03-C全体の完了判断は `current_task.md` の残件を含めて行う。
