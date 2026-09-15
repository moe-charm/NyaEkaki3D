# Nya Ekaki 3D — Windows v1 GUI導線整理

更新: 2026-09-15。状態: **GUI-01〜07の第一段、GUI-03表示名metadata第一段とMCP更新経路、MOD-01〜04の第一段、MOD-05のSession／SelectionContext第一段、Lifecycle・Layout・RefreshState・Execution・PersistenceRefresh・RefreshPipeline・UiElements・Viewport・ViewportInput・ViewportInteraction・SelectionRefresh・対象一覧差分更新の共通責務分離を実装。GUI-05の作業モードポインタ回帰、GUI-04の寸法入力保持回帰、GUI-06の制作正本／納品対象表示回帰、fit対象サマリー表示、作業モード切替時のPanel自動折りたたみ、Morph target完全ID tooltipを追加。最終分割・実操作受入は未完了**。

ユーザー提供の制作パネル画像と現行作業ツリーを照合した。今回の範囲は導線設計であり、実マウス操作・新ビルドの受入結果ではない。[設計v2 §13](NyaForge-Authoring-Design2.md#13-人間用ワークスペースとノード画面)の「中央3D、左object一覧、右parameter、下部node」をWindows v1の既存機能へ具体化する。新しい編集エンジンや全面的な保存形式変更を先行させない。

## 1. 確認した問題

| 現状 | コード上の根拠 | 利用者への影響 |
|---|---|---|
| 「制作へ」で表示中の女の子が消える | `ViewerApp.Authoring.cs::OpenAuthoring`はViewerのAvatarを非表示にする。`AuthoringWorkbench.Open`は初回に空workspaceを作る。モデル引継ぎ引数はない | 同じモデルの編集へ移ったつもりでも別の空領域になり、選択解除や故障に見える |
| 全操作が右の長いScrollViewへ順番に追加される | `AuthoringWorkbench.cs::BuildUi`がMCP、project、object、装着、造形、取込、UV、Paint、Rig、保存・出力を同じsideへ追加 | 入口・編集対象・保存がスクロールで消え、操作順序が分からない |
| 作り始めるボタンが重複する | `BuildUi`と`GraphEditing.cs::BuildGraphEditing`にplate、Plane graph、空polygon、四角面、mirror、choker、cuff | 基本形状と内部データ方式の違いを利用者に選ばせている |
| 「プレート追加 ×100」は個数ではない | `ProjectActions.cs::AddSample(float scale)`→`AuthoringFixtures.Panel(scale)`と`RestTransform(scale, ...)`。元座標を1/scaleにしており、尺度違いのfixtureを1個追加する | 100個増える操作と誤解する。静的profileを選ぶとGLB追加も止まり、入口で行き止まりになる |
| チョーカー／カフは固定寸法で生成 | `GraphEditing.cs`が`PolygonPrimitives.Choker/Cuff`を既定引数で呼ぶ | キャラクター専用・装着済みの完成形のように見えるが、実際は原点の基本形状 |
| 対象が`graph · ID先頭8桁`表示 | `Objects.cs::RefreshObjectSelection`。`AuthoringObject`に表示名プロパティはない | 身体、髪、衣装のどれを編集しているか分からない |
| 取込が内部用語中心 | `Import.cs`、`ImportSelection.cs`にnode instance、mesh/skin resource、graph object | ファイルを選んだ後も取込対象・確定手順が分かりにくい |
| 保存・受渡しが多数のボタンに分散 | `BuildUi`下部の制作フォルダ、保存、開く、GLB各種、Unity、衣装package、VRM | 保存と納品出力を混同する。参照bodyの混入範囲も把握しにくい |

以前の「空画面は何も選択されていない」という説明は不十分だった。提示画像の状態は**制作プロジェクトにまだモデルがない**。Viewerの表示内容と制作文書は現在別であり、単なる選択の問題ではない。

## 2. 主導線と画面構成

主導線: **制作プロジェクトを開く／新規 → モデル・形状を追加 → 対象を選ぶ → 編集 → 保存 → 出力先を選ぶ**。

```text
Nya Ekaki 3D   [ファイル] [追加] [保存] [元に戻す] [やり直す]    [出力] [AI接続]
プロジェクト名・未保存印     編集対象: 腕輪01 / 形状編集 / 参照ロック状態
┌対象一覧─────────┬3Dビュー────────────────┬選択対象の設定──────┐
│モデル名         │[形状][UV・ペイント][装着・骨][確認]│今のモードの設定だけ│
│ ├身体  表示/鍵  │                              │を表示             │
│ └髪    表示/鍵  │  空なら開く・追加する案内      │                   │
│腕輪01  表示/鍵  │                              │                   │
│[＋追加]         │                              │                   │
└────────────────┴[ノードを表示（詳細）]────────┴───────────────────┘
状態・選択数・操作結果 ／ 詳細ログを開く
```

- 左は名前で選べる対象一覧。複数meshのモデルは表示上のグループにまとめる。v1では親子transform編集や骨階層エディタを同時実装しない。
- 右は選択対象・作業モードに対応した設定のみ。長い説明はヘルプ／詳細へ、操作できない理由は該当箇所に短く表示する。
- 上部の保存・Undo/Redo・追加・出力はスクロールで消さない。AI接続は上部ボタンから専用パネルへ開く。
- ノードとIDによる詳細編集は残す。通常の頂点編集にはnode/port選択を要求しないが、どの編集段へ作用するかは明示する。
- viewportで対象全体の選択と点／面の選択を混同しない。対象名と選択モードを常時表示する。

## 3. 入口と空状態

### 起動・Viewerから制作への移動

- 「制作へ」は「制作ワークスペースを開く」とし、既存の制作文書があればその文書へ戻す。表示中モデルで勝手に置き換えない。
- 初回の空画面は「制作プロジェクトを開く」「モデルを追加」「基本形状を追加」の3入口を出す。参照保護、装着、頂点IDなどは空状態には出さない。`ContextVisibility`で空projectのPanel表示を制御し、モデル取込中は明示的に取込Panelを表示する。
- Viewerにモデルがある場合は「ビューアーのモデルはまだ制作に追加されていません」と説明し、「元のVRM/GLBを選んで追加」へ誘導する。
- packから編集可能な元ファイルを確実に解決できる契約は未確認。最初はファイル選択で既存importを使い、AssetBundleからの自動変換や表示中モデルの自動コピーを約束しない。
- 将来の直接引継ぎは別タスク。Viewerのシェイプキー・非表示状態・アニメーション状態は、明示対応ができるまで制作へ移ったと扱わない。

### ファイル操作の区別

| 操作名 | 挙動 |
|---|---|
| 制作プロジェクトを開く | native制作文書を開く。未保存なら保存／破棄／キャンセル |
| モデルを追加 | GLB/VRMを現在文書へ追加。既存内容を置換しない |
| 基本形状を追加 | 寸法を指定して現在文書へ1 object追加 |
| 保存／名前を付けて保存 | 編集状態をnative文書として保存 |
| 出力 | 用途、対象、対応制限を確認して別成果物を作る |

## 4. 取込・対象名・プリセット

取込はファイル選択→名前付き候補一覧→確定の1パネルへ集約する。「全身（全パーツ）」と「パーツを選ぶ」を明示し、確定前に数と制限を表示する。全mesh取込はメモリ負荷が大きいため黙って実行しない。未対応・失敗・キャンセルは元文書を保持する。高度なresource/index指定は詳細へ置く。

対象一覧は元モデル名／パーツ名を表示し、重複は連番で区別する。IDは詳細欄・コピーで取得できるようにする。ObjectId/GraphIdを名前へ置換しない。表示名・モデルグループは版付きmetadataで永続化する案を第一候補とし、既存metadataとの重複を実装時に確認する。旧文書は「オブジェクト 1」等へフォールバック。名前変更・Undo/Redo・再読込・MCPでも同じ対象を保つ。

チョーカー／カフの生成関数は再利用し、主画面の専用ボタンを「追加」へまとめる。

| 追加メニュー | パラメーター／役割 |
|---|---|
| 平面 | 幅・高さ。通常はPolygonの四角面を使う |
| 空のメッシュ | 頂点から作る利用者向け。初心者の既定にはしない |
| リング（丸い断面） | 現Choker。半径・管の太さ・分割数 |
| バンド（平たい断面） | 現Cuff。内半径・幅・厚み・分割数 |
| プリセット | 「チョーカー」「手首カフ」は上記の寸法例。身体への自動装着ではない |

表示単位はmm、内部はm。追加前に設定でき、確定で1回のUndoになる。初期配置は原点と明示し、選択／Frameで見失わない。追加後は通常の頂点編集へ進む。分割数を後から変更してUV・weight・編集差分を壊す再生成はv1へ含めない。Mirrorは形状の種類ではなく編集の「左右対称」として整理する。

fixture ×1/×100、旧Plane graph、Rig/Morphサンプルは開発者向け検証メニューへ移す。既存回帰のcommandは残し、通常UIにテスト都合のボタンを復活させない。外部プリセット市場やplugin機構はこの整理の前提にしない。

## 5. 編集・出力・表示サイズ

- **形状**: オブジェクト／点／面選択、移動、押出し、厚み、左右対称。高度なID入力は詳細。
- **UV・ペイント**: 画像と3Dを並べ、対象メッシュと材質名を表示。既存のUV/Paint/Material処理を接続する。
- **装着・骨**: 衣装、対象アバター、装着先の順で指定。剛体装着とウェイトで曲がる衣装を別の選択肢にする。参照bodyをロックしても表示・fit元選択は可能とする。
- **確認**: 最終結果、ポーズ、表情、揺れ、撮影。確認用変形と保存される編集を表示上も区別する。
- **出力**: 衣装をUnityへ／汎用GLB／VRM（対応範囲限定）を用途で選ぶ。対象名一覧、参照除外、静的形状か骨付きか、制限を示して確定する。既存allowlist・参照保護のガードを維持する。
- 「自動」など効果が曖昧なラベルは、実処理を確認して「手動調整を解除」等へ変更する。アニメーション優先へ戻るケースを単なるゼロ化と説明しない。
- 狭い窓では左右パネルを個別に開閉し、3Dと主要操作を確保する。文字を小さくして押し込まない。DPI100/150/200%、長い日本語名・長いID・エラー時にも操作を隠さない。

## 6. 実装タスクと完了条件

GUI-04とMOD-02の第一段（基本形状パネル、寸法入力、生成処理のUI分離、ノード詳細の折り畳み）に加え、GUI-02/03の第一段（取込手順説明、対象役割名、完全IDホバー）は実装済み。残りの優先順位は製品の操作改善順であり、コード障害のP1/P2分類ではない。

| ID | 順序・依存 | 作業／主な対象 | 完了条件 |
|---|---|---|---|
| GUI-01 | 最優先 | `BuildUi`を上部・対象一覧・3D・設定・下部へ責務分割。通常操作と開発者メニューを分離 | **第一段実装済み**: 常設コマンドバーからモデル追加／基本形状追加／保存／Undo／Redoを選べ、制作対象と保存状態を表示する。尺度fixtureは「開発者向け確認用fixture」へ折り畳み、空状態案内も通常入口へ統一した。空状態・完全な上下分割の最終確認は残る |
| GUI-02 | 01 | Viewer→制作の状態説明とimport専用パネル。`ViewerApp.Authoring`、`Import*` | **第一段実装済み**: 取込手順を`ModelImportGuidance`へ抽出し、ファイル選択→候補確認→1件／全件取込の順序を表示する。Viewer状態の引継ぎと候補名の永続化は残る |
| GUI-03 | 01 | 名前付き対象一覧、選択状態、表示・参照ロック。`Objects.cs`、metadata | **第一段実装済み**: 形状・アバター／スキンモデル・装着アクセサリー等の役割名と、完全object IDをホバー表示する。表示名はstable ObjectId keyed `object-labels.nyaforge.bin`へ保存し、GUIと`forge_set_object_label`のMCP更新、Undo/Redo・Save/Open・inspectionへ接続した。外部sidecar専用toolの実transportもPASS。group semantics、同名表示、旧文書の手動確認は残る |
| GUI-04 | 01 | 基本形状追加パネル＋寸法プリセット。`GraphEditing.cs`、`PolygonPrimitives` | **第一段実装済み**: リング／バンドの種類、mm寸法、分割数を指定して追加できる。実マウスでの異なる寸法・頂点編集・Undo・Save/Open確認と、プリセット拡張は残る |
| GUI-05 | 01、03 | 作業モード別の設定切替、選択モード・編集段の明示 | **第一段実装済み**: 形状編集／UV・色／装着・骨／確認・出力ボタンから既存設定へ移動できる。**第二段でPlayerのポインタ hit test 回帰を追加**し、各ボタンが対応する既存Foldoutを開くことを確認した。身体を参照にして衣装を選ぶ実操作、切替で編集破棄・二重イベント・異なる対象への書込みがないことの手動確認は残る |
| GUI-06 | 01、03 | 保存と用途別出力の集約。`ProjectActions`、参照保護／allowlist | **第一段実装済み**: native保存／開く、Explorer選択、GLB・Unity・衣装package・VRM出力を`ProjectOutput`へ集約し、保存と納品の違いを説明する。制作正本のフルパスと、汎用出力の対象数・参照保護衝突・allowlist不整合を表示する。実出力との手動照合、参照bodyの誤同梱確認は残る |
| GUI-07 | 01〜06と並行して接続 | AI接続専用パネル、IDコピー、MCP対象名・状態・結果の表示 | **第一段実装済み**: MCP接続欄を`McpPanel`へ分離し、短い接続手順と折りたたみ詳細、既存のinstance ID・開始／停止・Undo経路を維持する。IDコピー、再接続、実sidecarとの手動確認は残る |
| GUI-09 | GUI-05、06 | fit／weight対象と測定状態の常時表示 | **第一段実装済み**: 装着Panelに衣装頂点・avatar面の対象範囲、全体数、skin-bind状態、fit測定済み／再測定要否を表示する。指定変更時の再測定状態を自動判定し、詳細はtooltipへ残す。実アバターの全周fit・貫通・見た目受入は残る |
| GUI-10 | GUI-05 | 作業モードごとの表示整理 | **第一段実装済み**: 形状／UV・色／装着・骨／確認・出力の切替時、選択した領域だけを開き、前の長いPanelを閉じる。確認・出力モードからMorph／表情差分も開ける。保存・編集状態とPanelの利用可能性は変更しない。狭幅・DPI・実マウスでの最終表示確認はGUI-08へ残る |
| GUI-08 | 02〜07 | 狭幅・DPI、旧文書、実操作一周、quickstart更新 | 実マウスで「読込→形状追加→対象選択→編集→保存再開→出力」。クリックと描画の一致、DPI100/150/200%、IME、長い名称を記録。未実施条件は未受入のまま残す |

着手順は01→02/03→04/05/06、07は各変更と同時に接続、最後に08。GUI-01だけを巨大なUIフレームワーク開発にしない。まず既存UI Toolkitとcommandを移動・整理する。

## 7. 既存計画との関係・検証方針

この導線整理を、[Windows v1計画](Windows-v1-Development-Plan.md)のNF-V1-08実操作一周とNF-V1-15手動受入の前提として追加する。従来の「残りは確認中心」という判断を修正し、**GUI導線そのものの実装が必要**とする。GUI-03第一段の表示名metadataは実装済みだが、Unity/VRChat・材質・長時間検査と実マウス受入は削除せず後続に維持する。

- 本計画には実装済み範囲と次段の設計を併記する。GUI-03第一段はCore 510件とPlayer Authoring／Navigationで今回検証済みで、残りの実操作・実アバター・Unity／VRChat受入を完了扱いにしない。
- 実装時は既存commandを再利用。名前の永続化やプリセット寸法等、意味が変わる部分に回帰を追加する。
- Navigation／Choker／Cuff等の検証が古いボタン名・表示位置へ依存する箇所を移行し、新しい正規のGUI導線もPlayerで通す。
- 該当Player buildで実マウスと実画面を確認する。折返し指定やbuild成功だけで読みやすさを受入済みにしない。
- packからの自動編集引継ぎ、自由なドッキング、多数の新プリミティブ、完全Blender互換、macOS対応は別途とする。

## 8. ソースコードの責務分割（追加要望）

調査時点で`UnityRuntime/AuthoringWorkbench*.cs`は141ファイルある（検証用partialも含む）。ファイルは分かれているが、同じpartial classのfield・`Execute`・`Refresh`を共有する。`BuildUi`は多数の機能の構築を担い、`Refresh`は機能横断の更新順序を持つ。第一段としてコマンドバー、形状作成、対象ラベル、取込案内、保存／出力、MCP、作業モード、作業モード回帰を個別partialへ分け、GUI-03の表示名正本を`ObjectLabelsCodec`へ切り出した。**partialを増やすだけでなく、状態の所有者と依存方向を明確にする**。

以下は実装予定の責務と候補名。ディレクトリ・クラスはまだ作成していない。

| 責務／候補配置 | 所有するもの | 所有しないもの |
|---|---|---|
| `Workbench/WorkbenchShell` | 上部・左右・中央・下部の配置、モード切替、パネル表示、DPI追従 | メッシュ変更、GLB解析、保存形式 |
| `Workbench/WorkbenchSession` | 唯一のworkspace参照、文書交換、共通command実行、Undo後のmetadata再同期、dirty状態 | 個別ButtonやTextField、表示名attachmentのcodec実装 |
| `Workbench/SelectionContext` | 選択object、編集段、点／面選択、選択変更通知 | 別コピーのmeshや独自Undo履歴 |
| `Workbench/Panels/ImportPanel` | 元ファイル・候補・取込進捗の表示と操作要求 | import algorithm、任意の他パネルfield |
| `Workbench/Panels/ObjectListPanel` | 名前・表示・ロックの一覧と操作要求。表示名はstable ObjectId keyed metadataへ書く | ID再発行、骨格の対応判定 |
| `Workbench/Panels/ShapeCreationPanel` | 寸法入力・プリセット選択・単位表示 | choker/cuff生成処理の複製 |
| `Workbench/Panels/ShapePanel`, `SurfacePanel`, `RigPanel`, `PreviewPanel` | 作業別の表示・入力・局所的な一時状態 | workspace正本や共有可変fieldの横取り |
| `Workbench/Panels/ProjectPanel`, `ExportPanel`, `AiConnectionPanel` | 保存先・出力対象・接続状態と操作要求 | writer、受渡し契約、別のMCP編集実装 |
| 既存`OwnedMeshProjection`等／Viewport controller | 描画、カメラ、hit test、RenderTextureの寿命 | ファイル入出力、個別設定画面 |

依存方向は`Panel → 明示した操作callback/機能controller → 共通command → 既存Authoring Core`。読取りはsessionから必要な状態だけを渡す。CoreはUI Toolkitへ依存させない。既存のimport/export service・codec・generatorを再利用し、UI整理に合わせて別系統のCoreを作らない。

### 現在の分割状況

第一段の実装は既存partial classを利用して段階的に進めている。`State`が共有UIハンドル・session・selection context・viewport状態、`Lifecycle`がUnityイベント購読・preview sceneの寿命・終了確認、`PersistenceRefresh`が保存状態だけの表示更新、`Layout`がルート／ヘッダー／viewport／controlsの配置とfeature `Build*`順序、`RefreshState`が全体refreshの投影・状態反映順、`Execution`が共通command実行・例外捕捉・ステータス表示を所有し、`SelectionRefresh`が頂点選択に依存するPanelだけを部分更新し、`Objects`の対象一覧はobject ID・表示名・active objectが変わった時だけボタンを再生成する。`CommandBar`、`ShapeCreation`、`ObjectLabels`、`ModelImportGuidance`、`ProjectOutput`、`McpPanel`、`McpObjectLabels`、`WorkModes`、`WorkModeVerification`がそれぞれ表示責務または検証責務を持つ。`ObjectLabels`は`ObjectLabelsCodec`／`ObjectLabelService`を通じて表示名をnative attachmentへ永続化し、Objects／inspection／MCPの両方が同じ正本を読む。`AuthoringWorkbenchSession`はlive workspace、command service、保存状態を所有して`StateChanged`を通知し、`SelectionContext`は頂点／面選択と対象・編集段の識別を所有して`Changed`を投影へ通知する。`AuthoringWorkbench.RefreshPipeline.cs`は編集系Panel群と確認・出力系Panel群のrefresh呼び出しをまとめ、`AuthoringWorkbench.ViewportInput.cs`は対象選択・頂点hit test・viewport座標変換をまとめ、`AuthoringWorkbench.cs`にはpartial-classのホストだけを残している。dirty／metadata再同期の完全移管とPanel単位の購読境界は次段で、外部sidecar専用toolのtransport受入はV26でPASS済み。既存のCore・保存形式・MCP wire契約を変更しない小さな単位で進める。

### 状態・イベントの規則

- 最初は`AuthoringWorkbench`を互換ホストとして残し、1機能ずつ抽出する。全141ファイルを一度に移動しない。Unityのscene/prefab参照、`.meta` GUID、既存partial検証を壊さない。
- パネルに巨大なWorkbench本体を渡して全fieldへ触らせず、必要なread stateと操作callbackだけを渡す。汎用サービスロケータ、全項目入りの可変共有Context、反射によるfield接続を導入しない。
- 正本は既存workspaceと永続attachment。モード・折畳み・スクロール位置はUI状態。表示名・ロック等の保存対象はcommand経由で更新し、UI状態と混ぜない。
- UIの値反映はイベントを発火させないsetterを使う。パネルを閉じる／再開する際に購読を重複させず、所有者が解除・破棄する。取込／撮影中の遅延結果は開始時文書と照合する。
- UI非表示時も保存やMCPを動かす機能を止めない。GUIとMCPは同じ操作契約・参照保護・revision検査・Undo経路を使う。
- `Refresh`の依存順序は最初に保持して抽出する。状態更新の単位が明確になった後で、選択／文書／表示変更に必要なパネルだけを更新する。モジュール化と評価キャッシュ最適化を一括変更しない。
- 大きい検証partialは機能の抽出後にharnessへ段階移行する。通常UIに隠しfixtureを残すことをテスト成立の条件にしない。

### モジュール化タスク

| ID | 依存・組み込む先 | 作業 | 完了条件 |
|---|---|---|---|
| MOD-01 | GUI-01の最初 | 共通実行・状態・寿命の依存を整理し、Shellを抽出。既存Workbenchを互換ホスト化 | **第一段実装済み**: 常設コマンドバーを`AuthoringWorkbench.CommandBar.cs`へ抽出し、既存command・sessionを利用する。Shell全体の抽出、寿命・購読境界、起動／文書交換の手動確認は残る |
| MOD-02 | MOD-01、GUI-02/04 | ImportPanelとShapeCreationPanelを独立クラスへ抽出 | **第一段実装済み**（`ShapeCreation.cs`、`ModelImportGuidance.cs`）。Import処理本体のPanel化、他機能のprivate field境界、取消・追加1回1Undo・不正寸法の実操作確認は残る |
| MOD-03 | MOD-01、GUI-03/05 | 対象一覧・選択context・作業モード別パネルを抽出 | **第一段実装済み**（`ObjectLabels.cs`、`WorkModes.cs`、`SelectionContext.cs`）。object切替／編集段切替と頂点／面選択を一つのcontextへ集約し、表示切替後の編集が前のobjectへ適用されないことを既存回帰で確認。表示名のnative永続化は`ObjectLabelsCodec`へ分離済み。group semanticsと各機能Panelの完全分離は残る |
| MOD-04 | MOD-02/03、GUI-06/07 | 保存・出力・AI接続パネルを抽出し、共通実行経路と読取りを整理 | **第一段実装済み**（`ProjectOutput.cs`、`McpPanel.cs`）。パネルを閉じても保存・MCPが正しい対象へ到達し、再接続やSave/Openが独立した別workspaceを作らないこと。IDコピーと実sidecar受入は残る |
| MOD-05 | 各抽出と同時、完了判定はGUI-08 | 古いUI構築／全体Refreshの重複撤去、検証harness移行、実装後の構成README | **第一段実装済み**（`AuthoringWorkbenchSession.cs`、`SelectionContext.cs`、`AuthoringWorkbench.Lifecycle.cs`、`AuthoringWorkbench.RefreshState.cs`、`AuthoringWorkbench.Execution.cs`、`AuthoringWorkbench.RefreshPipeline.cs`、対象一覧差分更新、[Authoringコードマップ](Authoring-Code-Map.md)）。同じ文書・Undo履歴を共有すること、更新群の責務境界をPlayer回帰で確認。dirty／metadata再同期、Panel単位の購読、検証harness移行、実操作受入は残る |

改修単位は「GUIタスク1件＋そのパネルの抽出＋必要な回帰＋記録」。モジュール化だけの長期間の先行工事を避け、毎段階で起動可能な候補を残す。新しいassembly分割や名前空間の全面改名は、依存が実証されて必要になった時に別判断する。

2026-09-15追記: GUI-08の狭幅対応として、実Windows Playerのcontrols欄を固定幅から画面幅32%（260〜420px）へ変更した。最大化した実ウィンドウで作業モード・基本形状・寸法入力の収まりを確認し、注入UI回帰は旧幅を維持して88 checks PASS、実RadDollV3一周は95 checks PASS、Unity Bridgeは16 checks PASS。DPI別、狭い非最大化窓、IME、実EditorWindow全周fit・貫通・材質、VRChat実機は引き続きGUI-08／E03〜E08の未受入項目とする。
