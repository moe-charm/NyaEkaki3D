# C1-A: 任意グラフの制作文書とnative保存

2026-09-11。Core内部の任意graph文書と保存を実装。GUIと共通commandは未接続。

AuthoringObjectは固定static profileと任意graphを区別する。任意graphを旧offset APIで平坦化しない。文書のstate hashはcanonical graph contentを参照し、revisionだけの変更では変わらない。未接続graphも文書として保持する。

GraphProjectCodecを独立させ、native schema 3 / graph-project-v1でobject IDとgraph hashを保存する。ProjectStoreはwriter lock中に依存blobを公開し、manifestを最後にatomic更新する。保存versionと文書IDの競合検査を維持する。未知バイナリpayloadを保持し、再読込時の評価成功を必須にしない。

既存static projectのwriterはschema 2のまま。schema 1/2のreaderを保持する。staticからgraphへの移行保存は新しいディレクトリへ行い、元manifestを上書きしない。これは段階的なprofile併存であり、全プロジェクトを自動移行する実装ではない。

検証: Core **59 passed / 0 failed**。GraphDocumentTests 2項目、GraphProjectTests 3項目を追加。任意graphの候補構築・revisionとhash・未接続保持、保存往復・保存競合、未知payload・不正hash拒否、旧profileの保護を確認した。既存回帰も成功。PlayerビルドとGUIでの任意graph再読込は未確認。

次は任意graph commandとincomplete文書のtransaction／last-good preview管理を統合し、その後runtime canvasを接続する。既存GUIはstatic profileを前提としており、schema 3のGUI操作対応済みとはしない。
