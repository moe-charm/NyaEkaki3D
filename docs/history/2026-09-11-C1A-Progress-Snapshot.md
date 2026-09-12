# NyaForge 開発タスク

更新: 2026-09-11
状態: **C1-A進行中。グラフCoreと不変アセットの保存を実装。任意graphのnative保存まで実装。次はcommand・runtime canvas。**

直近のコード変更: 空projectへのgraph追加、node追加・削除・更新、Output指定、接続・切断、context付き頂点移動を共通commandへ接続。Core **63 passed / 0 failed**。次はUnity projection／Workbenchのgraph対応とcanvas。詳細は [ノード操作記録](docs/history/2026-09-11-C1A-Node-Commands.md)。Playerは未ビルド。

## 開発目標と読む順序

ユーザー指示: 予定どおり開発し、最初から責務を分けたモジュール構成で実装する。

Windowsを先行し、VRキャラ・衣装・小物の造形から出力までBlenderなしで完結するアプリを目指す。macOSは将来対応。ノードと直接編集は同じ文書・command・Undoへ接続する。C0-Rの完了を設計全体の完成とは扱わない。

1. このファイル
2. [開発計画](docs/Development-Plan.md): 着手順と完了条件
3. [設計v2](docs/NyaForge-Authoring-Design2.md): 全体設計の正本
4. [文書一覧](docs/README.md): 操作・旧設計・履歴の入口

## 前段の完了: C0-R

- [x] 変更前の自作schema 1 projectとBakeを `Tests/Authoring.Core/Fixtures/LegacyV1/` に固定。
- [x] CoreをDomain・Commands・Projection・Persistenceへ分離。Unityのプロジェクト操作とGUI検証撮影も別ファイルへ分離。
- [x] objectゼロの新規project、保存・読込、メッシュ追加command、追加のUndo/Redoを実装。
- [x] 制作画面を空から開始し、サンプル追加と新規作成を分離。空の画面に案内を表示。
- [x] native schema 2（objects配列）へ変更。旧schema 1は読込可能、移行保存は別フォルダへ。Bakeはschema 1を維持。
- [x] 空のexportを `NO_EXPORTABLE_OBJECT` で拒否し、出力フォルダを作らない。
- [x] 未保存変更のキャンセル、空へのUndoと表示資源の解放、別Playerプロセスでの再読込を検証。
- [x] Core・Player・Unity Bridge・Viewerの回帰と画像確認を実施。

詳細は [C0-R実装記録](docs/history/2026-09-11-C0R-Empty-Projects.md)。現在の制約は最大1static object・1offset layer、正の一様scaleと平行移動。これは初期profileであり、複数objectやgraph対応済みとはしない。

## 今使う実行ファイル

`Builds/Windows-C1A-Storage/NyaForge.exe`（グラフアセット保存を含む既存UIの回帰確認版。ノード画面はまだない）

既存の `Builds/Windows/NyaForge.exe` がユーザー操作で起動中だったため、終了させず別ディレクトリへビルドした。最初の既定先ビルドは使用中PDBの置換で失敗しており、その出力は更新完了版として扱わない。現在の回帰確認版はWindows-C1A-Storage。Windows-C1AとWindows-C0Rは前段の検証済み版として残している。

ビルドスクリプトへ `-BuildName` と、起動中の同じ出力先をビルド前に拒否する検査を追加。ユーザーの起動中プロセス・保存状態は変更していない。commit/pushは行っていない。

## 進行中: C1-A

### 次回の着手順

1. **文書と保存**: AuthoringObject／AuthoringDocumentへ任意graphを統合し、ProjectStoreからgraph hashを参照する。native形式の変更と旧schema 1/2の移行を一緒に設計・実装する。既存fixtureは再生成しない。
2. **操作と表示**: ノード追加・接続・parameter変更をCommandsへ追加し、直接編集と同じUndoを使う。未接続文書を保持し、最後に成功したpreviewと現在revisionを区別する。
3. **Windows GUI**: Coreの契約が通ってからruntime node canvasを接続する。ノード配置はworkspace状態とし、制作履歴へ混ぜない。

最初の確認経路はPlane→EditMesh→Outputの生成・接続、parameter／頂点変更、Undo、保存・再読込、static Bridge出力。担当モジュールと完了条件は [開発計画](docs/Development-Plan.md) に従う。Player・Bridgeの下記結果は以前のビルドの検証記録として保持する。

### C1-A全体のチェックリスト

- [x] graph/node/port定義、型・循環・domain検査と評価結果をCoreの独立モジュールとして実装。
- [ ] 最小集合 `MeshSource / Primitive(Plane) / EditMesh / Output` を候補に、同じ制作文書へ組み込む。
- [ ] offsetをEditMesh payloadへ移し、layerとgraphを二重の正本にしない。旧schema 1/2の読込・移行を保護。
- [ ] Windows runtime node canvasで追加・接続・切断・parameter・処理段previewを提供。Editor専用GraphViewへ依存しない。
- [ ] node配置はworkspace状態、接続・parameter・頂点編集は制作commandへ送る。
- [ ] 未接続や未知nodeを保持し、incompleteと古いpreviewを明示。出力の成功と区別する。
- [ ] 誤接続・循環・上流変更・古いcontext・失敗時rollback、保存・再読込・Bridge出力を検証。

C1-A後は制作polygon/corner・UV/paint・MCP等で小物制作を一周し、C2で低ポリ全身キャラへ進む。全体の要件は開発計画と設計v2に保持している。

### 今回実装したグラフ基盤

`Assets/NyaForge/Authoring/Graph/` に不変graph/node/edge、型定義、検証、Plane生成、評価、編集contextを分離した。MeshSource、Plane、EditMesh、Output、Scalar v1を扱う。未知type/versionはメモリー上でpayloadを保持してUNKNOWN_NODEを返す。未接続はincompleteで、独立したノードのpreview結果は取得できる。

既存AuthoringObjectはSource→EditMesh→Outputを所有し、BaselineMesh/Transform/Offsets/LayerEnabledはその参照APIへ変更。既存のcommand・Undo・評価・Bakeはグラフ評価を通る。旧保存データのstate/geometry hashは維持した。

**任意グラフのCore文書・native保存は実装済み。commandによるcommitとGUIは未完了。** ProjectStoreのschema 2は固定static profileだけを保存し、グラフを再構築している。今回、別モジュールGraphBlobStoreで任意graphアセットの保存・読込を実装した。未知nodeのバイナリpayload、未接続、上流変更で未解決になった編集も保持し、hashで参照できる。プロジェクトmanifestへの参照はschema 3で実装済み。incomplete文書のtransaction、任意ノードの追加・接続command、UI canvasは次の作業。移行用schema 2 fixtureは`Tests/Authoring.Core/Fixtures/LegacyV2/`に固定済み。

詳細と検証は [グラフ基盤記録](docs/history/2026-09-11-C1A-Graph-Foundation.md) と [アセット保存記録](docs/history/2026-09-11-C1A-Graph-Storage.md)。モジュール契約は [Graph README](Assets/NyaForge/Authoring/Graph/README.md) と [保存形式](Assets/NyaForge/Authoring/Persistence/GraphStorage.md)。

## ソースコードの入口

| モジュール | 責務 |
|---|---|
| `Assets/NyaForge/Authoring/Domain/` | 不変の文書・object。空projectを表現 |
| `AuthoringWorkspace.cs` | 現在文書、instance、履歴、保存状態 |
| `Commands/` | operation、候補評価、transaction、再送・revision検査 |
| `Projection/` | 表示候補の確定・rollback・解放の契約 |
| `Persistence/` | 新形式codec、旧形式reader、厳密JSON、blob、atomic保存 |
| `ProjectStore.cs` / `BakeStore.cs` | 保存調停／static出力 |
| `UnityRuntime/AuthoringWorkbench.ProjectActions.cs` | 新規/open/save/export、未保存確認 |
| `UnityRuntime/OwnedMeshProjection.cs` | 自分で所有するMesh・マーカー、空表示への切替 |
| `UnityRuntime/WorkbenchCapture.cs` | GUI検証の撮影。将来のmodel-only Evidenceとは別 |
| `Tests/Authoring.Core/EmptyProjectTests.cs` | 空project・追加Undo・旧形式移行の回帰 |

上表の短いパスは `Assets/NyaForge/Authoring/` 基準。ただしUnityRuntimeは `Assets/NyaForge/` 基準、Testsはrepo基準。

## 検証結果

- Core: **63 passed / 0 failed**（グラフ11項目＋アセット保存8項目＋任意graph文書2項目）。最新実行の出力は `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-e951412220484fae94c6a52a43d89147`。
- 最新Playerビルド: `Logs/build-player-20260911-183136-727.log`、Windows-C1A-Storage。
- 最新Player回帰: `Artifacts/Authoring-20260911-183251-8bbe983a3c24489ca531bf2955dbfa2a/report.json` pass。Unity内でgraphアセットのscale1/100保存・読込と評価hashを追加確認。
- 前段Player回帰: `Artifacts/Authoring-20260911-182113-3121ea206c6540de90d6e9b31238b392/report.json` pass。空project、scale 1/100、Undo、保存・出力をグラフ評価経路で確認。
- 最新Bridge: `Artifacts/BridgeReceiver-20260911-182135-805-3f8f59e14f9f49fda84637f391f42079/bridge-report.json`、Unity 2022.3.22f1、6項目pass。
- 以下はC0-R時の記録（今回の新しいgraph操作やGUI canvasの確認ではない）:
- Player: `Artifacts/Authoring-20260911-180834-b5d246459f4c42d79bd8cdfc8f5b8825/report.json` pass。別プロセスで空projectを開き、scale 1/100の編集・保存・出力も確認。1280×800の空画面を実際に開いて確認。
- Unity Bridge: `Artifacts/BridgeReceiver-20260911-180525-606-4134f8a73e6c459980f27d211db1ab6d/bridge-report.json` pass。Unity 2022.3.22f1。後続変更はGUI・GUI検証のみで出力Coreは同じ。
- Viewer: `Artifacts/Navigation-20260911-180838-a0ad4682e81a4f99989ac3838c0c03c9/report.json` pass。
- 起動中Playerへのビルド拒否を確認。実際の手動GUI受入・VRChat内動作は別工程。

作業先は `Z:/TextureVoice_local/git/NyaForge`。HEADは `6e1e4fc6d4c88b1b5f3368f3706bcec21a0a9e36`、以前からの多数の未コミット差分を保持している。次の変更前にGit状態を確認する。RadDollV3-clothingのprivateモデル・画像・packを公開repoへコピーしない。



