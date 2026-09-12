# C1-A: 不変グラフアセットの保存

2026-09-11。C1-Aの途中成果。任意graphアセットをディスクへ保存できるようになったが、制作project本体のcommit・保存やnode canvasは未完了。

## 実装

`Persistence/GraphBlobStore.cs`と`GraphBinaryCodec.cs`を追加。graph/node/edgeと明示的な型別payloadをNYFG v1として符号化し、mesh/deltaはhash付きblobへ分離する。ノードと接続の列挙順に依存しないhashを生成し、依存blobを先に、graph blobを最後に公開する。既存のwriter lockとatomic blob保存を利用する。

未接続graph、上流変更で未解決になったEditMesh、未知type/versionを保存・再読込できる。未知payloadは不変のbyte列を正本にし、UTF-8として読めない将来のパラメーターも変換せず保持する。表示用の文字列はUTF-8またはbase64と明示する。

node/edge数・文字列長・payload長を割当前に制限し、単一blob 16MiB・参照合計64MiBの予算を検査する。hash不一致、切れたpayload、末尾の余剰bytes、未対応wire version、過大countを拒否する。

## 検証

- Core **54/54**。追加8項目は`GraphStorageTests.cs`。任意接続と差分、scale100、列挙順とhash、未知テキスト／バイナリ、未接続・未解決状態、破損graph／依存mesh、wire形式と予算を確認。
- Playerビルド: `Logs/build-player-20260911-183136-727.log`、`Builds/Windows-C1A-Storage/NyaForge.exe`。
- Windows Player: `Artifacts/Authoring-20260911-183251-8bbe983a3c24489ca531bf2955dbfa2a/report.json` pass。Unityの実行環境でscale1/100のgraph保存・読込と評価hashを確認し、既存の空project・編集・Undo・保存・Bakeも回帰確認。
- 今回はUIレイアウトやBake形式を変更していない。以前のBridgeの合格記録は過去の証拠として保持し、新しいgraph形式の受け取り互換性を主張しない。

## 次の接続点

GraphBlobStore.Writeはhashを返す低水準APIで、project.nyaforge.jsonは更新しない。ProjectStoreがwriter lockを取得したtransactionの中ではWriteLockedを使い、graph参照を持つ新schemaのmanifestを最後に確定する予定。

既存schema 1/2の移行fixtureは固定済み。次はAuthoringDocumentに任意graphを持たせ、graph command・incomplete commit・古いpreviewのrevisionを統合する。その後にruntime node canvasを接続する。設計全体の目標は継続中。
