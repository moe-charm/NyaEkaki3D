# 作品本体とVRM設定の一括保存

2026-09-12。R03の保存保護。関連: [current_task](../current_task.md)、[レビュー](reviews/2026-09-12-Rig-Vrm-Review.md)。

## 所有と公開

`AuthoringWorkspace.Attachments`は、作品に属するexpression / Spring sessionの不変バイト列を保持する。`ProjectAttachments`は名前、サイズ、所有するコピー、決定的hashを管理する。`SetAttachments`はworkspaceのlock内で置き換え、文書が同じでも設定だけ変更されれば`IsDirty`になる。元VRMやprivate素材のコピーは含めない。

`ProjectStore.Save`は従来と同じworkspace lockとwriter lock、documentId / expectedSaveVersionの照合を使う。形状・graphと設定のblobを書き、内容hashを確認してから、最後に`project.nyaforge.json`を原子的に置き換える。SaveVersion、SavedStateHash、SavedAttachmentsHashの更新はその後だけ行う。GUIとMCPの`ProjectSaveService`は同じ処理を利用する。

blob書込みや最後の公開に失敗した場合、旧manifestが参照するblobは変更しないため、以前の本体と設定を同じ組で開ける。新しい孤立blobが残ることは許容し、自動削除しない。旧manifestを読んだreaderも、その参照先を読み続けられる。OS・ストレージ自体の障害に対するバックアップ保証とは別の契約である。

## Schema 4

metadataを持つprojectは、次のenvelopeを使う。`ProjectSnapshotCodec`が扱い、中のprojectは従来のschema 2または3の構造を維持する。

```json
{
  "schemaVersion": 4,
  "project": { "schemaVersion": 3, "...": "従来のgraph manifest" },
  "attachments": [
    { "name": "vrm-expression-session.nyaforge.json", "hash": "SHA256" },
    { "name": "vrm-spring-session.nyaforge.json", "hash": "SHA256" }
  ]
}
```

例の`...`と`SHA256`は説明用の省略表記。実際のwriterは完全なproject manifestと64文字のhashを出す。attachment名は上記2種類のみで、最大2件。内容は既存`blobs/<hash>.bin`へ保存し、読込時にサイズ・hash・名前・参照重複・envelopeの未知fieldを検査する。

設定なしの新しいprojectは従来のschema 2/3を維持する。一度schema 4で保存した保存先は、全設定を除去してもschema 4と空attachmentsを保持し、残っている旧sidecarが復活しないようにする。metadata付きprojectを古いNyaForgeで開くと未対応schemaとして拒否される。

## 旧形式からの移行

schema 1/2/3を開いたときだけ、ルートの旧expression / Spring sidecarを読み取り、workspaceへ取り込む。次の保存でblob参照へ移行する。旧sidecarは削除も上書きもしない。schema 1の本体は従来どおり別フォルダ保存による移行が必要である。

schema 4の読込はmutableな旧sidecarを参照しない。`OpenProject`はsnapshot内の設定をcodecでdecodeしてから表示を置き換える。設定自体のVRM仕様適合は保存transactionとは別に検査する。作者とコライダー列のcodec修正は以下を参照。

## GUI / MCPと残る境界

import成功時に`AuthoringWorkbench.Metadata.cs`がsessionをworkspaceへ渡す。GUI保存は設定の別ファイル書出しを行わず、ProjectStoreで一括保存する。MCPも同じworkspaceを保存するため、GUI外の保存で設定が抜けない。MCP handlerの保存成功はGUIの保存失敗表示も解除する。

`saveIncomplete`は、もともとcleanな文書で保存を要求して失敗した場合にも未完了を表示する補助状態である。失敗時の終了防止と、ユーザーが明示的に破棄して切り替える導線を維持する。canvas配置の保存は制作内容とは別であり、そのエラー時にもGUIは未完了を表示する。

attachmentsはimport情報としてworkspace単位で保持し、現時点では個別の編集UI / command / Undo履歴を持たない。文書Undoでimport graphを戻してもmetadataを即削除せずRedo用に保持する。将来設定そのものを編集する段階では、文書revisionと共通commandの契約へ統合する必要がある。

## 検証

- Core: expression / Springそれぞれの書込み失敗で旧manifest、本体、設定、dirty、versionを確認。再試行、Save As公開失敗、legacy移行・除去、corrupt blob、未知fieldを検査。
- MCPが使うProjectSaveService: graph＋metadataを保存・再読込し、古いwriterの保存versionを拒否。
- Windows Player: 新規／上書き保存でmetadata blobを排他ロックし、終了・無確認切替を拒否。GUI保存とMCP handlerの再試行、および設定を含むOpenを確認。
- 外部MCP transportを通すmetadata保存、実マウスの「保存して終了」、実VRMでの受入は今回の専用検査には含めない。実行ログはcurrent_taskへ記録する。

## Session version 2（2026-09-12・作者配列への移行履歴）

現在はSpring writerがversion 3、expression writerはversion 2。重力方向・形状と不明値の移行は [Spring詳細](VRM-Spring-Details.md) を参照。

expression / Springの新しいwriterは`author`文字列の代わりに`authors`配列を出力する。readerはversion 1と2を受け入れ、旧単独名はカンマを含め1名として保持する。旧空名は空配列へ対応する。新形式の再書出しは決定的で、最大256名・各256文字の不変リストを保持する。`Author`は表示用の連結値で、保存元は`Authors`である。

旧snapshotのpayloadはOpenだけでは変更せず、そのまま保存してもversion 1を維持できる。sessionをcodecで再書出ししたときにversion 2となる。旧NyaForgeのsession readerはversion 2を扱えない。project envelopeはschema 4を維持する。

VRM1 importは非空の作者配列を必須とする。これは[公式meta schema](https://raw.githubusercontent.com/vrm-c/vrm-specification/master/specification/VRMC_vrm-1.0/schema/VRMC_vrm.meta.schema.json)に基づく。省略したstiffness=1、dragForce=0.5は[公式joint schema](https://raw.githubusercontent.com/vrm-c/vrm-specification/master/specification/VRMC_springBone-1.0/schema/VRMC_springBone.joint.schema.json)に合わせ、明示0は保持する。VRM全体のスキーマ検証を実装したという意味ではない。

コライダーごとのnode列は重複を保持する。参照集合に必要な一意性検査とは分け、同一nodeの複数コライダーを保存後に失わない。shapeの詳細値とgravityDirは後続のSpring version 3へ保持する。runtimeへの変換は別タスクである。
