# C1-A: graph commandとrevision付きpreview

2026-09-11。任意graph差替えを共通command serviceへ接続した。ノード単位の追加・接続・parameter APIとGUIは後続。

- `OperationEvaluator`はgraphとstatic操作を振り分ける。graph.replaceは不変graphを持ち、canonical hashを再送fingerprintへ含める。
- commandのsource検査はstaticのbaseline hashまたは任意graphのcontent hashを使う。既存revision・instance・command ID検査を維持する。
- `AuthoringPreview`は現在の評価と最後の成功出力、両方のrevisionを所有する。未接続graphのcommitはCOMMITTED_INCOMPLETEを返し、現在のmesh成功hashを返さない。古いpreviewは同じobjectだけで保持する。再読込では古いpreviewを捏造しない。
- `IGraphAuthoringProjection`を追加。準備・commit・rollbackの既存契約でgraphの表示候補を扱う。旧static専用projectionでgraph編集を確定しようとした場合は拒否する。Unity側はまだ未接続。
- 文書とpreviewは表示commit成功後に同時に確定し、既存のUndo/Redo履歴を共有する。static編集の不正な縮退三角形等は以前どおり拒否する。

Core **61 passed / 0 failed**。追加2項目でgraph→incomplete→Undo/Redo、再送、保存・再読込、static履歴復元、旧projectionの拒否と文書・preview保持を検証。Playerビルド・GUIでの操作は未確認。次はノード単位commandとUnityのgraph projection、その後canvasを接続する。
