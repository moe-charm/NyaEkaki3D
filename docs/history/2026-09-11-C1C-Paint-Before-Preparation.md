# NyaForge 開発タスク

更新: 2026-09-11。Paintのtyped graph・ストロークUndo・画像付きnative保存・3D表示を検証。製品全体は開発中。

## 開発の入口

作業先: `Z:/TextureVoice_local/git/NyaForge`。Windows先行、macOSは将来。
読む順: このファイル → [開発計画](docs/Development-Plan.md) → [設計v2](docs/NyaForge-Authoring-Design2.md)の対象節 → コード。[文書一覧](docs/README.md)参照。
製品目標はVRキャラ・衣装・小物の造形から出力まで。小物の一周、低ポリ全身キャラの一周、品質向上の順。設計の記載だけを実装済み・依存採用済み・公開承認済みとしない。

## 現在の実装

- C0-R: 空project、最大1object、static頂点編集、共通Undo/Redo、保存、Bake／Bridge。
- C1-A: 型付きgraph、共通command、native schema3、runtime canvas、編集段、配置保存、graph Bake。自動制作往復あり。手動受入は未確認。
- C1-B: polygon/cornerの安定ID、三角形化と選択対応、PolygonSource／PolygonEdit、頂点移動、面選択・押出し・厚み、Mirrorノード、編集ケージと最終結果の同時表示。
- UV: 面ごとの投影・格子配置、UV図の島選択、数値移動／回転／拡縮、接線再構成、共通Undo／保存／Bake。
- Paint: immutable画像タイル、2Dストローク、typed Image port、Paint node、UV binding、1stroke command／Undo、画像blobを参照するnative graph保存、最終出力のbaseColor表示。ブラシGUI・画像付き外部出力・layer/maskは未実装。

保存はstatic writer schema2、graph writer schema3、readerは1/2/3。native schemaは変更せず、新node codecから画像blobを参照する。PaintImageStore単体は低レベルasset API。画像付き作品はProjectStoreのgraph保存で再現する。

## 今回の変更と検証

最新の変更:

- `GraphNode.Paint.cs`、`PaintEvaluation.cs`: image.paint v1。Mesh入力でUVを固定しImage出力。Outputのoptional baseColor Image入力で同じdomain／UV対応を検査。一般的なMaterial graphはまだ未実装。
- `GraphEvaluation`はMesh／Scalar／Imageを区別し、画像出力と最終baseColorを保持。snapshotに画像hashを含める。原画像はノードのimmutable payloadとして所有。
- `PaintEditing.cs`と`Commands/PaintOperations.cs`: 画像hash・UVhash・domainを固定したcontext、コピーされたストローク点列、fingerprint、1stroke／1Undo、再送と古いcontext拒否。
- GraphBinaryCodecは画像blobを依存として先に保存。UV変更時はPAINT_UV_CHANGEDとして旧画像payloadを保持し、未解決のままnative保存可能。位置だけの変更では対応を維持。
- `BaseColorSurface.cs`とpreview shaderは画像を所有texture/materialとして描画。現profileは不透明表示。static Bakeは画像を黙って落とさず、フォルダ作成前にEXPORT_UNSUPPORTED_FEATUREを返す。
- Core: **108 passed / 0 failed**。`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-d3183c44759843b99526561d3114a8b5`。型不一致、別domainへの誤適用、strokeのUndo／再送／古いcontext、native画像保存、UV変更の未解決保持と復帰、無効strokeでの状態保持を確認。
- 最新ビルド: `Builds/Windows-C1C-PaintGraph/NyaForge.exe`、`Logs/build-player-20260911-204008-326.log`。
- 最新Player pass: `Artifacts/Authoring-20260911-204049-462314f022f24757b86c37307893a946/report.json`。2strokeの画像付きproject保存・再読込、画像hash、所有texture、Undo、メッシュ専用Bakeの拒否。既存回帰も成功。
- `paint-graph.png`を確認。再読込した2色の線を3Dの面へ描画できる。まだユーザーがブラシGUIで塗った結果ではなく、公開の自作検証fixture。

以下は前回の画像基盤の記録:

- `Authoring/Paint/PaintImage.cs`: 64×64 tile、変更tileのみcopy、最大1024×1024。sRGB RGB／straight alpha／左下原点。公開の画素取得は値またはコピー。
- `PaintStroke.cs`: UV座標の点列、円形brush、線分距離によるcoverage。1stroke内のcoverageをunionし、入力点密度による重ね塗りを避ける。線形光でalpha合成。最大1024点・半径0.5〜512px・累計16M pixel visits。
- `PaintImageCodec.cs`: NYFI v1。色profileと寸法、RGBAを保存。読込は寸法・総byte長・version/profileを検査してから画像を確保。
- `PaintUvBinding.cs`: domain、面・corner・vertex対応、material slot、UV値からhash。位置だけの変更は許容、UV／topology／domain変更はPAINT_UV_CHANGED。まだgraph評価に未接続。
- `UnityRuntime/PaintTextureAdapter.cs`: sRGB texture作成と所有権。`PaintVerification.cs`: blob保存とPNG画素往復。
- Core: **104 passed / 0 failed**。`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-76c31a65d3914efb8781654e0d7fa57a`。
- ビルド: `Builds/Windows-C1C-PaintCore/NyaForge.exe`、`Logs/build-player-20260911-203235-112.log`。
- Player pass: `Artifacts/Authoring-20260911-203306-8ffd9bfeed014d5d892495698982d4ed/report.json`。画像blob→texture→PNG→decodeで全画素・寸法・行方向を確認。既存制作GUI回帰も成功。
- 同ディレクトリの`paint-core.png`を確認。ピンクと半透明青の線が重なる技術fixture。これは色塗りGUIの完成画像ではない。

## 次の実装: Paintを作品へ接続

設計v2 5.3／19／20.2を確認し、次の順で進める。

1. Image port／Paint node／native画像参照／1stroke Undoは初期統合済み。次は2D paint GUIの開始・Paint段選択・palette／radius・stroke captureを実装し、ポインタを離したとき1回だけcommitする。
2. 2D画像と3D面のbase color表示を同期。stroke previewと確定文書を分離。UIキャンセル／capture喪失時に未確定strokeを破棄し、保存対象へ混ぜない。
3. UV未解決のpaintは旧画像を保持。明示rebind／rebakeのGUI、既存paintを含む作品のUV変更前の依存表示を実装する。失敗を単なる空画像に置き換えない。
4. 一般Material port／複数materialと画像の割当、layer／opacity／maskのモデルを拡張する。現baseColor入力はmesh全体の1画像。
5. 保存・再読込・PNG・mesh/material出力を含む色付き小物の一周を検証。既存static Bakeの画像非対応を、画像があるのに黙って落とす挙動にしない。
6. layer opacity／mask／画像取込、3D paint、Evidence／MCPへ拡張。C1からC2全身制作へ進む。

## 保持する残件

Mirror中心切断・頂点結合、下流編集と上流変更の回帰、node parameterのpointer回帰。厚みの均一肉厚・自己交差・modifier化。UVのpan/zoom・ドラッグ・seam編集・unwrap。最終結果がケージを覆う場合の透過／wire。skin/morph・rig・target出力は今後。
UV図は先頭2048面、全cornerが0〜1内の面のみ描画／pick。範囲外UVは保存可能。画像基盤も1024上限は初期予算で、製品全体の最終要件を縮めるものではない。

## 作業境界

`Assets/NyaForge/Authoring/`はUnity非依存。`Assets/NyaForge/UnityRuntime/`は表示・GUI・Player検証。`UnityBridge/`は受け取り先Editor処理。
既存未コミット変更を保持。privateモデル・画像・packをコピーしない。commit/push未実施。既存Playerを終了せず、別BuildNameを使う。OS入力の手動受入、今回の画像付き作品の外部Unity受け取り・VRChatは未実施。

[UV・Mirrorの過去記録](docs/history/2026-09-11-C1B-UV-and-Mirror.md)、[押出し・厚み・表示](docs/history/2026-09-11-C1B-Extrude-Solidify.md)の「次」「未実装」は当時の記述。進捗の正本はこのファイル。
