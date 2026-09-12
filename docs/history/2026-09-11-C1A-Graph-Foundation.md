# C1-A: グラフ評価の基盤

2026-09-11。C1-Aの途中成果。**ノード画面と任意グラフの保存は未実装**であり、C1-Aの完了報告ではない。

## 実装した範囲

`Assets/NyaForge/Authoring/Graph/`へ型定義・不変node/edge/graph・検証・Plane生成・評価・編集context・旧profile変換を分離した。Unity依存を持たない。

MeshSource、Plane、EditMesh、Output、Scalarのv1を扱う。ScalarはPlaneの幅／高さ入力に接続できる。既知portの型違い、存在しないport/node、二重入力、循環を拒否する。循環は選択出力へつながらない部分も検査する。

未接続inputはincompleteで、独立したnodeのpreviewは取得可能。未知typeや将来versionはpayloadをメモリー上で残し、UNKNOWN_NODEを返す。現在の判定は保守的で、graph全体に診断が残る場合IsCompleteとしない。

EditMeshの差分はreferenceメートルで、input snapshot hashとdomainに固定する。上流の寸法変更、同じ形の別source、古いcontextを検出し、payloadを黙って付け替えない。明示した再base機能はまだない。EditMeshを無効にするとpayloadを保持したまま迂回する。

既存AuthoringObjectの正本を固定のSource→EditMesh→Outputへ変更し、BaselineMesh/Transform/Offsets/LayerEnabledはnodeの参照にした。既存command・Undo・native保存・Bakeの評価経路もgraphを通る。旧state/geometry hashは維持した。

## 未完了の範囲

- 任意graphをAuthoringDocumentへcommitするAPIとgraph用保存schema。
- graphの追加・接続・parameter変更command、incomplete文書のcommit、古いpreviewのrevision表示。
- runtime node canvas、workspace layout、処理段のGUI選択。
- 未知nodeのディスク保存・再読込。現状はメモリー上の保持だけ。
- polygon/cornerの安定ID。現時点のdomainはsource ID＋三角形topology hash、offsetはbaseline index。

schema 2は引き続き固定static profileだけを表し、保存後に同じgraphを再構築する。任意graphを保存したと主張しない。次のschema移行用に自作LegacyV2 fixtureを固定した。

## 検証証拠

- Core: **46 passed / 0 failed**。新規11項目は `Tests/Authoring.Core/GraphTests.cs`。型接続・循環・未接続・未知node・上流変更・domain変更・古いcontext・不正頂点・退化面・旧profileのscale 1/100保存往復を検証。
- Playerビルド: `Logs/build-player-20260911-182008-384.log`。実行ファイルは `Builds/Windows-C1A/NyaForge.exe`。使用中の既存出力先を避けた。
- Player: `Artifacts/Authoring-20260911-182113-3121ea206c6540de90d6e9b31238b392/report.json` pass。空project、既存GUIのscale 1/100編集・Undo・保存・Bakeを新しいgraph経路で検証。新しいnode canvasの試験ではない。
- Unity Bridge: `Artifacts/BridgeReceiver-20260911-182135-805-3f8f59e14f9f49fda84637f391f42079/bridge-report.json`、Unity 2022.3.22f1、6項目pass。頂点・属性・メートル座標・Prefab参照とパス拒否を確認。

今回の変更は公開のCore・試験・文書に限り、privateモデルの取込やcommit/pushをしていない。開発目標は継続中。
