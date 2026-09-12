# C1-A: ノード単位command

2026-09-11。GraphOperationsとGraphOperationEvaluatorを分離し、共通command／Undoへ接続した。

空projectへのgraph object追加、node追加・削除・parameter更新、Output指定、接続・切断、明示したGraphEditContextでの頂点移動を提供する。node削除はその接続を同時に除去し、選択Outputの削除ではOutput指定も解除する。parameter更新でtype/versionを変更しない。接続済みinputの置換は切断＋接続を同じbatchで送る。

candidate graphを各操作で検証し、最後の候補だけを公開する。型違い・循環・古いcontext等で失敗したbatchは文書・preview・履歴を変更しない。追加object ID・node payload・edge・contextはcommand fingerprintへ含める。ノードの画面配置はこの制作commandに含めない。

Core **63 passed / 0 failed**。GraphNodeCommandTestsの2項目で、空projectからPlane→EditMesh→Outputを構築、頂点移動、native保存・再読込、ノード削除・Undoで空まで復元する経路を検証。誤接続batchのrollback、上流変更後の古いcontext拒否、type変更拒否、切断・再接続も確認。

未確認: Unity Playerビルドと実GUI操作。次はOwnedMeshProjectionとWorkbenchをgraph evaluation／明示edit contextへ対応させ、runtime canvasを接続する。Core APIの成立をGUI完成とはしない。
