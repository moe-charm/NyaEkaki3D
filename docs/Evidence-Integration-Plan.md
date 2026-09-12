# Evidence実装への接続計画

2026-09-12。設計v2 §8〜9とDevelopment-PlanのC1契約に基づく。実装済みとは扱わない。

## 現物と境界

AuthoringWorkspace.Gateがcommandの排他点。Document/Previewはcommitごとに置換され、Previewは未完了時に以前のOutputを保持する。したがってOutputだけを読んで現revisionの成功を宣言してはいけない。WorkbenchCaptureはUI回帰画像専用で、8frame待つ間の編集排他や制作snapshotとの整合を保証しない。モデル専用Evidenceへそのまま転用しない。

## 最初の実装

1. CoreのEvidence/EvaluatedSnapshotを独立追加する。workspace.Gate内でinstance/document/revision/stateHash/対象object/graph/node-output/評価完了状態と不変GraphMeshValueを取得。current output、empty、faceless、incomplete/staleを区別する。古いpreviewを現行のgeometryとして返さない。
2. 計測は同じsnapshotのMesh/Polygon/Transformから導出する。描画頂点数と論理頂点数、triangle/face数、world bounds、材質数、画像hashを区別。未取得のfit/pose検査をpassにしない。静的native作品にsourceEpochやpose/timeを捏造せず、対象外/未実装を明示する。
3. snapshot IDは取得時の一意ID、内容hashは再現比較用として別に持つ。同内容でも別instance/revisionの識別を保持。Coreテストで取得後の編集/Undoによる不変性、空、面なし、未完了を確認。
4. Unity側EvidenceCaptureは専用cameraとsnapshot由来の独立projectionを所有する。UI/編集点/hover線を撮影しない。複数viewのcamera/照明/解像度を固定して記録する。Main threadで生成・描画・破棄し、capture queueの所有と取消を定義する。
5. メタデータと画像はjob単位の新規出力へ保存し、画像hashとsnapshot IDをmanifestへ結ぶ。撮影失敗をcommand失敗に読み替えず、commandを再実行しない。画像保存失敗/取消/入力revision変化を明示する。
6. GUIの撮影入口とfixtureでgeometry/metrics/imageの一致を確認。その後MCP adapterへ同じserviceを公開する。MCPはnamed instanceへのIPC、typed schema、command service共用。実装前に公式SDKの版・依存・配布条件を確認する。

## 検証範囲

最初のCoreはsnapshot取得とmetricsのみ。画像検証/モデル専用camera/MCP接続/多方向/固定比較/poseは別の未完了項目として維持する。C1全体はこれらを満たすまで完了にしない。既存形状/UV/Paintの残件とC2以降の全身/rig/weight/morph等も消さない。
