# NyaForge 開発タスク

更新: 2026-09-11。C1-B: UV島の選択・移動・回転・拡縮をWindows Playerで検証。製品全体は開発中。

## 開発の入口

作業先: `Z:/TextureVoice_local/git/NyaForge`。Windows先行、macOSは将来。
読む順: このファイル → [開発計画](docs/Development-Plan.md) → [設計v2](docs/NyaForge-Authoring-Design2.md)の対象節 → コード。[文書一覧](docs/README.md)参照。
製品目標はVRキャラ・衣装・小物の造形から出力まで。小物の一周、低ポリ全身キャラの一周、品質向上の順。設計の記載だけを実装済み・依存採用済み・公開承認済みとしない。

## 現在の実装

- C0-R: 空project、最大1object、static頂点編集、共通Undo/Redo、保存、Bake／Bridge。
- C1-A: 型付きgraph、共通command、native schema3、runtime canvas、編集段、配置保存、graph Bake。自動制作往復あり。手動受入は未確認。
- C1-B: polygon/cornerと安定ID、三角形化、RenderVertexMap／RenderTriangleMap、PolygonSource／PolygonEdit、stable-ID頂点移動、面選択と領域押出し、厚み付け。
- 表示: 幾何法線の陰影、選択面overlay、モード別の入力と案内。描画専用データを文書・出力に混ぜない。
- Mirror v1: PolygonEdit→Mirror→Outputで上流の編集を対称側へ再評価。軸X/Y/Z、reference空間の対称面位置m、有効切替。新しいnode codecをschema3の既存graph envelopeに追加。旧schemaを変更しない。
- GUI: 「左右対称で始める」、canvasのMirror追加とparameter操作。編集段の元形状を青緑、最終結果を灰色で同時表示できる。選択と編集は元形状のみ。

保存はstatic writer schema2、graph writer schema3、readerは1/2/3。profile変更は別フォルダ保存。旧triangle入力からquadやseamを推測復元しない。

## 今回の変更と証拠

最新はUV島編集。`Topology/UvIslands.cs`で共有edgeの両端UVが完全一致する面を連結、`UvIslandTransform.cs`で選択範囲のUV bounds中心を基準に移動・回転・正の一様拡縮。`PolygonTangents.cs`で回転後の接線を形状とUVから再構成する。`Commands/UvOperations.cs`はimmutable設定とbounded選択を共通commandへ接続。UV図は島選択を通知し、Workbenchから操作する。

- Core: **99 passed / 0 failed**。`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-d1b2f64712fe4c20abb8e9e517a6b1cc`。共有UVの連結、seam分離、移動、拡縮／回転と異方的なUVの接線、再送・Undo・保存・Bakeを確認。
- 最新ビルド: `Builds/Windows-C1B-UVEdit/NyaForge.exe`、`Logs/build-player-20260911-202613-296.log`。
- 最新Player pass: `Artifacts/Authoring-20260911-202638-aeed9afc363446d7b697a4fe5f382463/report.json`。UV図のpointer選択、変形ボタン、Undo/Redo、保存・再読込、Bakeを確認。既存回帰も成功。
- `uv-edited.png`を確認。3D形状は矩形のまま、選択UVが30度回転・0.7倍・U+0.05になり、数値欄と適用ボタンが読める。
- 最初のPlayer検証で、UVクリック内の面モードChangeEventが遅れて実行され新しい選択を消す問題を検出。UVからのモード切替をSetValueWithoutNotify＋一括選択更新に変更して解消。失敗ログは202300／202428／202538のartifact、最終成功は上記。
- UV図の描画・pickは同じ0〜1内／先頭2048面制限。範囲外のUVはデータに保持するが図から選択できない。ドラッグ・pan/zoom・自動再pack・負scaleは未実装。3D面選択を種に操作すると接続UV島全体へ展開する。

以下は前回までの記録（最新値は上記）:

最新は`Topology/PolygonUvProjection.cs`、共通`graph.polygon.uv-project` command、`UnityRuntime/UvPreview.cs`、`AuthoringWorkbench.Uv.cs`と専用Player検証。面ごとの主軸投影を0〜1の格子へ配置する。各セル内で縦横比を維持し5%ずつ余白を確保。頂点位置・vertex/face/corner ID・normal・materialを保持し、既存tangentを新UV方向に更新する。UVは全cornerへ追加／置換し、元sourceは変更しない。

- Core: **96 passed / 0 failed**。`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f456d87d04c3456186a91a8c787127cd`。厚み形状の6島非重複、ID／位置、接線直交性、再投影決定性、Undo、native保存、BakeのUV保持を確認。
- 最新ビルド: `Builds/Windows-C1B-UV/NyaForge.exe`、`Logs/build-player-20260911-201516-286.log`。
- 最新Player pass: `Artifacts/Authoring-20260911-201541-134973923f704bf48385c8cd7cd2d4cc/report.json`。UV投影ボタンのpointer操作、6面のUV範囲、表示、Undo/Redo、保存・再読込・Bake。既存回帰も成功。
- `uv.png`を1280×800で確認。3Dの選択面とUV輪郭をオレンジで対応表示し、6面の配置が見える。最初の撮影ではUV図がスクロール下に隠れていたため、図まで移動して再撮影済み。
- 自動unwrap、seam指定、島の結合・直接編集、均一texel密度、paintは未実装。UV画面は確認用で、先頭2048面かつ全cornerが0〜1内の面のみ描画。描画上限を保存データへ適用しない。OS入力の手動受入・今回のUV形状の別Unity受け取りは未実施。

以下は前回までの記録（最新値は上記）:

最新の変更は`UnityRuntime/FinalResultProjection.cs`と`AuthoringWorkbench.FinalPreview.cs`。編集用projectionとは別の読取専用描画meshを所有し、同じprepared projectionのcommit／rollback／破棄に従う。最終形状は選択マーカーやpick配列へ追加しない。フレーミングには両方の形状を含める。未完了graphでは古い最終結果を重ねない。

- 最新ビルド: `Builds/Windows-C1B-FinalPreview/NyaForge.exe`、`Logs/build-player-20260911-200915-099.log`。
- 最新Player pass: `Artifacts/Authoring-20260911-200942-453e770fb2cb4adcb95281cfd37f59bc/report.json`。
- 反転側クリックでは選択されず、元頂点は選択・編集できることをpointerイベントで確認。表示toggleで文書hash／revisionが変わらず、接続切断で灰色表示が消え、Undoで復帰することも検証。既存保存・Bake回帰も成功。
- `mirror.png`を確認。青緑の編集ケージにだけ点が表示され、反転側が灰色で並ぶ。表示切替と説明は読める。Coreは変更していないため今回再実行せず、94項目は以下の前回結果。

前回のMirror基盤の記録:

`Topology/PolygonMirror.cs`が反転複製、`Graph/MirrorEvaluation.cs`が参照座標変換とdomain、`GraphNode.Mirror.cs`がtyped parameterを担当。canvas parameter、Workbench開始操作、Player検証も別ファイルに分離。
反転コピーの面winding・法線・接線方向とhandednessを修正。元形状のIDを保持し、コピー側へ新IDを割当。出力polygonはMirror node IDの別domain。Graph domainには入力domainも含め、内容変化はsnapshotで検査する。

- Core: **94 passed / 0 failed**。`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-d1814b14716b4adb87fc8d735ca0fa4e`。
- X/Y/Z反転、面の向きと属性、元形状保持、scale1/100＋translation下の対称面、上流編集追従、Undo/Redo、node有効切替、native再読込、Bakeを追加確認。
- 最新ビルド: `Builds/Windows-C1B-Mirror/NyaForge.exe`、`Logs/build-player-20260911-200513-663.log`。
- Player pass: `Artifacts/Authoring-20260911-200539-668869ae347c4544b656ac9165bf5022/report.json`。開始ボタンをpointerクリック、上流頂点変更、対称側の座標、Undo/Redo、保存・再読込、Bakeを確認。既存回帰も成功。
- 同ディレクトリの`mirror.png`を1280×800で確認。編集した片側とその反転側を表示できる。node cardの軸・位置入力を実際のpointerで一周する検証は未追加。
- 今回の検証はUI Toolkit pointerイベント。OS入力・手動受入・mirror形状の別Unity受け取り・VRChat検証は未実施。

## 制限と次の作業

1. Mirrorは元形状＋反転コピー。中心面での切断・頂点結合・重複面除去は未実装。中心をまたぐ元形状は重なり得る。任意triangle入力は自動変換せず、polygon入力を要求する。
2. 編集ケージと最終結果の同時表示は初期実装済み。最終結果は不透明な灰色のため、今後のsubdivision等でケージを覆う場合の透過／wire表示と選択視認性は別途改善する。現時点で全modifierの編集操作を保証しない。
3. 単純UV投影と島選択・数値変形は初期実装済み。次はbase color paint・画像保存を進める。UV変更がpaintの対応を壊した場合の保持／再配置契約を先に確認する。UV図のpan/zoom、ドラッグ操作も残件。EvidenceとMCPを共通commandへ接続し、C1の一周からC2全身制作へ進む。
4. 押出しの非manifold・属性・再押出し・複数材質の追加回帰を保持。厚みは平均頂点法線方式で、鋭角の均一肉厚・自己交差修正・調整可能modifier化が残る。
5. Mirror後のPolygonEditで編集した後、上流／軸変更時の未解決payload保持、中心結合の対応表、node parameterのGUI回帰を拡張する。

押出しは底の開いた5面、厚み付けは四角面から裏を含む6面を作る。いずれもskin/morph転送、UV atlas、自動自己交差修正は未実装。詳細は[Topology契約](Assets/NyaForge/Authoring/Topology/README.md)。

## 作業境界

`Assets/NyaForge/Authoring/`はUnity非依存のDomain・Graph・Topology・Commands・Projection・Persistence。`Assets/NyaForge/UnityRuntime/`は表示・GUI・Player検証。`UnityBridge/`は受け取り先Editor処理。
多数の既存未コミット変更を保持。privateモデル・画像・packをコピーしない。commit/push未実施。既存Playerは終了せず、別BuildNameを使う。

[押出し・厚み・表示の過去記録](docs/history/2026-09-11-C1B-Extrude-Solidify.md)と[整理前記録](docs/history/2026-09-11-C1B-Before-Preparation.md)の「次」「未実装」は当時の記述。現在の進捗の正本はこのファイル。
