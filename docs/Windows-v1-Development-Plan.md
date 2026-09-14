# Nya Ekaki 3D Windows v1 実行計画

更新: 2026-09-14。検証対象コード: `main`（コード候補 `a77840b`、PerformanceV39／BoneSubsetV3まで確認済み）。持込提案の基準 `8c1bd7a` から、衣装package・材質・ownership marker・複数package管理・semantic texture preview・納品対象allowlist・base-color原画像sourceのnative保持・検査公開・未編集時の原画像出力まで実装が進んでいる。`f010146`フィードバックの現行HEAD再照合（[現行再照合](reviews/2026-09-14-Feedback-f010146-Recheck-5f90cb9.md)）とPerformanceV37〜V39の実RadDollV3再計測、V39の50回反復起動回帰は[current_task](../current_task.md)へ記録した。source-skin取込では既存display meshの再利用とJSON scene clone削減を実装し、V39で回帰した。直近Core artifactは `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-c0eae62745d14c7faab0ce1fadd318b4`、実衣装Player reportは `Artifacts/Authoring-20260914-150145-eb5a4a651a244b0b9eee01b6106f4918/report.json`、Unity Bridge reportは `Artifacts/BridgeReceiver-20260914-151117-020-6bdd730b5d17449e91066b33a45a9816/bridge-report.json`。

本書は持込「NyaForge Windows v1 開発計画」をコード照合して修正した実行計画。受入済み報告ではない。製品全体の目標・C0〜C5の要件は[設計v2](NyaForge-Authoring-Design2.md)を維持し、本書はWindows衣装制作v1へ至る着手順を定める。v1だけの合格を製品全体や進行中goalの完了へ読み替えない。直近の状態は[current_task](../current_task.md)。現行main（`a77840b`）ではCore 508 passed / 0 failed、`Builds/BoneSubsetV3/NyaForge.exe`のAuthoring／実RadDollV3衣装package／Unity 2022.3.22f1 Bridge受け取り、private RadDollV3全mesh import／Save/Open／GLB・VRM1 smoke、実SDKのRadDollV3 Skirt chain PhysBone設定probe、実FBXへのskinned-clothing package初回・再適用・native roundtrip smoke、実body meshを使ったsurface fit／weight transfer接続probe、semantic textureを含む合成Unity Bridgeの衣装package回帰、明示納品対象allowlistのGUI／MCP／GLB subsetとSave/Open回帰まで確認済みで、通常GLBのTEXCOORD_1は`UNSUPPORTED_UV_SET`で明示停止する。semantic textureのGUIもUV1新規適用を拒否し、UV0へ戻してから保存・出力する契約を持つ。base-colorを1024pxへ縮小した場合は原画像サイズと作業画像サイズを表示し、PNG/JPEGの原画像bytesを`image.original-source`としてnative graphへ別保持する。未編集時は原画像bytesをGLBへ再利用し、Paint編集後やsource不在時はbounded previewへ戻す。原画像sourceを持たない既存projectから復元を装わない。範囲限定fitの平均値は選択頂点数を分母にし、`EvaluatedVertexCount`として検査記録へ出す。実マウス・実EditorWindow・全周fit／貫通・見た目・Build & Test・実VRChatは未受入である。

実RadDollV3 VRMの取込・編集・保存・GLB/VRM出力と、制御fixture衣装のpackage生成・Unity Bridge受け取りをまとめて確認する再実行入口は `Tools/Test-NyaForgeRealClothing.ps1`。最新V29の実モデル実行はPlayer report `Artifacts/Authoring-20260914-082145-4f42a375b1224914af0782043c15f279/report.json`（93 checks PASS）とBridge report `Artifacts/BridgeReceiver-20260914-082703-724-7cf712d873094c299d5e03ef390fb8b5/bridge-report.json`（16 checks PASS）へ記録した。全mesh取込で一時約4.6GBのPrivate Memoryを観測したため、NF-V1-15では全mesh取込と通常編集を分けて計測する。package出力自体の証拠はcurrent_taskへ分けて記録し、実RadDollV3へ新規衣装を全周fitして見た目と貫通を受け入れる作業は別工程として残す。V24の狭い実ウィンドウ操作折り返し証跡は[current_task](../current_task.md)のNF-V1-09Hへ記録した。手動工程は [Windows v1 手動受入チェック](Windows-v1-Manual-Acceptance.md) に固定する。

## 1. 採用判断と遠回りの修正

**衣装を既存Unityアバターへ渡す出口を先に実証し、既存の編集基盤から自作衣装1点を完成させる方針を採用する。** 全身造形、完全VRM、FBX parser、全shader、共有資源の完全統合を同時に始める必要はない。

ただし原案をそのまま直列実行しない。次を修正する。

1. 原案NF-V1-02はE01〜E08すべてを要求するが、E06のnormal/MRは後続10、E07の衣装再適用は後続12に依存する。**初回受入02Aと機能追加後の受入02Bに分割**し、循環を除く。
2. 主経路の「衣装だけをBridgeで渡す」は未完成。既存Bridgeの静的MeshRenderer生成・任意親への配置と、PhysBonesの型写像は、skinned衣装を既存avatar骨へ接続する実装とは別。**03Aの最小受け渡し実装をG1に前倒し**する。G4では初めて接続するのでなく更新を完成させる。
3. 実クライアントや他者視点が未実施でも、独立したPolygon確定command・転送領域制限の開発は継続できる。外部受入はVRChat対応を公表する必須条件であり、全ローカル実装の開始条件にはしない。骨・単位・保存を壊す外部不具合が判明した場合はその修正を優先する。
4. 材質対応の前に、**現在の1024px縮小の扱い**を決める。元画像の解像度・hashと作業画像を区別し、縮小後の画像しか保持しない入力を原画保持済みと扱わない。全paint上限の単純引上げや全材質IRの作り直しを既定にしない。
5. 「12週・週20〜25時間」は持込側の仮定。ユーザーの稼働条件として採用しない。最小受け渡しと自作衣装一周の実測後に見積り直す。
6. 確認用人体fixtureは現行のものを再利用し、欠けた受入ケースだけ補う。件数を増やすためのテスト、同じ条件のbuild/readback反復、小物バリエーションの追加を進捗の代用にしない。

## 2. コード照合結果

| 論点 | 現行証拠 | 判定・作業 |
|---|---|---|
| 造形→skin | [AccessorySkinBindingAdapter](../Assets/NyaForge/Authoring/Graph/AccessorySkinBindingAdapter.cs)はEditMesh一つと一つの下流接続を要求。[GLB writer](../Assets/NyaForge/Authoring/Persistence/GlbExportService.cs)はMeshSource一つを要求しPolygon系nodeを許可しない | Polygon評価結果から派生MeshSource/EditMeshを作る接続が必要。許可node追加だけで済ませない |
| Unity衣装受取 | [SkinnedClothingReceiver](../UnityBridge/Editor/SkinnedClothingReceiver.cs)が明示BoneId mapからSkinnedMeshRendererを生成し、[skinned-clothing-v1 package](../Assets/NyaForge/Authoring/Persistence/SkinnedClothingPackage.cs)がGLB・skeleton・bindingをhash付きで束ねる | 合成Unity receiverまで実証済み。実アバターの骨map、ownership更新、実SDK/VRChatは03A/02Aの外部受入として残す |
| nativeとUnity出力 | [ProjectExportService](../Assets/NyaForge/Authoring/Persistence/ProjectExportService.cs)はskin/morph/attachmentをnative packageへ振り分ける | native保存成功をUnity装着成功としない。参照bodyを除く出力対象指定も03Aの範囲 |
| fit/weight制限 | [MeshSurfaceFit](../Assets/NyaForge/Authoring/Geometry/MeshSurfaceFit.cs)は全頂点・全avatar面を最近面へ投影し距離制限あり。[SkinWeightTransfer](../Assets/NyaForge/Authoring/Rig/SkinWeightTransfer.cs)の表面転送は全頂点・全avatar面を使い距離引数なし | 選択頂点・元body面領域・距離制限を双方へ接続する06を採用 |
| 材質・解像度 | [ImportMaterials](../Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ImportMaterials.cs)はbase colorを縮小してPaintへ所有保存。[PaintImage](../Assets/NyaForge/Authoring/Paint/PaintImage.cs)は最大1024px | base colorの縮小契約とsemantic mapの画像所有・出力品質を09Aで受入。未対応の画像slotは明示する |
| semantic texture | `MaterialTextureSlot`がnormal／metallic-roughnessのchannel・色空間・UV set・sampler・bytesを保持し、GLB writer/readerへ接続。材質GUIは既存mapをscalar変更時に保持し、Explorer/パスからの画像取込、契約表示、Windows PBR previewへnormal／MRを接続 | Coreのnative/GLB回帰、Player GUI取込・Save/Open・scalar保持、shader preview接続まで実装済み。Windows v1のsemantic mapはUV0を出荷対象とし、UV1は取込／出力時に`UNSUPPORTED_UV_SET`で明示停止する。semantic map付き単体Material Bakeは拒否しGLB/graphへ分ける。実RadDollV3 sceneのtangent/外観、出力一致、occlusion/emissive、実VRChatは外部受入・後続範囲 |
| SDK検証 | [互換記録](PhysBones-SDK-Compatibility.md)とprivate probeにSDK 3.7.6の実component生成・写像・非対応値拒否の証拠あり。Authoring本体へSDKを同梱しない設計は維持 | 「実SDKを一度も試していない」は解消。実アバターscene、Build & Test、実VRChat受入は別途必要 |
| 既存回帰 | 現行mainのCore501件、Playerのprivate RadDollV3往復、semantic textureを含む合成Unity Bridgeのpackage受入記録あり | 既存証拠は実マウス・実VRChatの成功を意味しない。変更箇所と外部受入を別に記録する |
| 共有・更新基盤 | [共有方針](Shared-Resource-Policy.md)、BridgeのImportOwnership・UpdateJournal・PrefabManagedBindings | 同一target複数nodeと管理asset更新基盤を再利用。全mesh結合や全journalの新設は不要 |

## 3. v1の成果物と境界

1体の既存VRM/GLBを参照し、チョーカー等の小物とカフ等の簡単な衣装を新規造形・調整する。nativeを正本として保存・再開し、衣装だけを既存Unity avatarへ適用する。元avatarのDescriptor、Animator/FX、元shader、利用者の非管理設定は維持する。

参照objectと納品objectを明示し、参照の誤編集と誤出力を防ぐ。参照のロック、出力allowlistと対象avatar選択はGUI/MCP・保存・再開で同じ契約に従う。名前の一致だけで骨を結合せず、stable BoneIdと受け取り側Transformの明示対応、親子/rest/単位・bind frameを検査する。不一致を強引に通すretargetは別工程。

GLB/初期VRMは対応範囲を明示した交換用の副経路とする。UniVRMでVRMを読めることはVRChat avatarとして完成した証拠ではない。Unity/SDKを使った適用・Build & Testを主経路で確認する。

新規全身キャラ、複雑な髪・sculpt・high→low bake、全自動fit、異rest自動retarget、FBX/BLEND直接取込、全VRM意味情報、macOS/Questはv1後の既存backlogに残す。新規衣装制作を実現できない場合は本v1未達として記録し、「取込衣装調整版」への無断の目標変更を行わない。

## 4. 依存を修正したタスク

NF-V1のIDは持込提案との対応用に維持。状態は実装済み・合成受入済み・外部未受入を分けて記録する。実装済みの03Aを重複して作らない。

| ID | 作業・成果物 | 開始条件 | 完了条件 |
|---|---|---|---|
| NF-V1-01 | receiver環境manifest・既存fixture棚卸し | なし | [Windows-v1-Environment.md](Windows-v1-Environment.md)へPlayer/receiver/Core/OS/GPUとfixture・hash・保存場所を固定。SDK/UniVRM/shaderの未導入項目はBLOCKEDとして記録 |
| NF-V1-03 | 衣装出力・骨対応・所有権契約 | 01の入力/target候補 | 参照body、納品allowlist、rest/骨対応、生成領域、更新key、競合/削除方針を定義。既存GLB＋sidecar／既存Bake拡張を比較し、receiverで実証する最小経路一つを選ぶ。参照保護中の汎用multi-object／GLB納品は`REFERENCE_EXPORT_BLOCKED`で停止し、選択衣装skin packageを明示出口とするガードを実装済み。永続allowlistのGUI/MCP編集は03Bへ分離 |
| NF-V1-03B | 納品対象allowlistの永続化 | 03 | `DeliveryAllowlistCodec`、native attachment、GUI／MCP state、multi-object／GLB subset出力、Save/Open、参照body混入拒否、Player回帰まで実装済み。実EditorWindowでのクリック操作と受け取り側manifest／report照合は外部受入として残す |
| NF-V1-03A | 最小衣装packageと初回Unity適用 | 03 | `skinned-clothing-v1`で衣装GLB・stable skeleton・bindingを個別に渡す。参照bodyの非同梱、BoneId map・IBM対応、事前hash検証、失敗時無変更を確認。合成Unity receiver、明示割当GUI、ownership markerまで実証済み。実アバター適用と実更新結果は外部受入として残す |
| NF-V1-02A | G1初回の独立reader・SDK・クライアント受入 | 01、03A（衣装ケース） | E01〜E05の現行対応分、E06のbase color/alpha、E08のlocal段階を確認。独立readerで骨/形状を比較。E07更新・未実装map・他者視点は後続へ明示的に分ける |
| NF-V1-04 | Polygon造形確定command | 03の座標/出自契約 | 元graphを残し派生MeshSource/EditMesh graphを一括生成。UV seam/corner→render vertex対応、material/paint、元object/graph/revisionとhash、transformを保持。剛体attachment済みのPolygonは基準姿勢のbone-local offsetをavatar-localへ焼き込み、派生skinからattachment metadataを除去する。Undo一回、失敗無変更。新規skinへ進める |
| NF-V1-05 | topology確定・属性依存 | 04 | skin/morph前に造形確定。既存skin/morphの未対応topology変更を事前拒否。再造形は新派生へ明示転送し旧派生を保持 |
| NF-V1-06 | 範囲を指定するfit/weight | 03の座標/対象契約 | 衣装の選択頂点、元body面領域、距離を共通候補として使う。Workbenchのavatar面ID・衣装頂点ID入力、衣装頂点選択の取込、ビューポートavatar面raycastをfit／weightへ接続済み。fit測定は最近面のsigned clearanceによる裏側候補数・距離・頂点IDもGUI／MCPへ返す。範囲外は無変更、未選択頂点・weightを保持。これは三角形交差・貫通ゼロの証明ではなく、薄い表裏・袖/胴体・遠方の負例は実アバター手動受入へ残る。候補/未対応点/移動量を確認して確定 |
| NF-V1-07 | 衣装検査GUIと参照保護 | 05、06 | rest編集/pose確認、bodyと衣装の表示、参照ロック、既存weight修正、固定pose群をGUIへ接続。参照object保護のnative保存・Open復元・Undo/Redo同期と頂点編集停止はPlayer回帰済み。実EditorWindowの手動操作、実アバターの全周fit・貫通・見た目は別受入として残す。新ブラシは一周で必要性が判明したものに限定 |
| NF-V1-08 | 自作衣装1点の全工程 | 03A、07。外部判定は02A | `PolygonPrimitives.Cuff`で低ポリ手首カフのprimitive→造形→UV/paint→確定→weight→保存再開→Unity→VRChatを手順だけで再現。Player自動経路は確認済み、実アバター・実VRChat・販売品質は未受入 |
| NF-V1-09 | 材質semantic slot設計 | 03 | 既存材質を拡張し、用途・色空間・channel・UV・sampler・adapter版を定義。Windows v1はUV0を出荷対象とし、GUIもUV1の新規適用を拒否する。Unity shader固有名はadapter。全面IR置換なし |
| NF-V1-09A〜D | 画像解像度・所有・出力品質 | 09 | 入力/作業/出力解像度とhashを表示・保存・inspectionへ。1024px作業previewとは別にPNG/JPEG原画像bytesを`image.original-source`としてnative graphへ保持し、inspectionでは原寸・MIME・byte数・hashだけを返す。preview hashが未編集と一致する場合だけ原画像bytesをGLBへ戻し、編集後・旧project・曖昧な複数sourceはpreview PNGへフォールバックする。元画像sourceを持たない既存projectから原画を復元したとは扱わない |
| NF-V1-10 | normal/MR画像の一周 | 09、09A、03Aのshader決定 | 衣装の画像指定・プレビュー・native・出力・receiverで一致。normal方向/tangent、MRのG=roughness/B=metallic、linear値とsRGB色、alphaを検証。専用paint/AO/emissive/bakeは追加しない |
| NF-V1-11 | 同一targetの複数object受入 | 03A、08、10 | 衣装2点＋小物1点を独立nodeで受取。同名骨/順序違い/別source負例。物理的mesh結合やdedupを必須にしない |
| NF-V1-12 | Unity更新・再適用 | 03A。最終複数回帰は11 | ownership/journalを衣装へ拡張。同じObjectIdのStateHash更新でBoneId割当と既存管理参照を保持し、更新時の管理object付け替えをBridge回帰で確認済み。A→B→再起動の実EditorWindow操作、削除・取消・失敗・利用者変更の競合、元avatar設定保護は手動受入として残る |
| NF-V1-13 | GUI/MCPとsnapshot整合 | 新commandごとに04/06/12と同時実施 | GUI/MCP同一command、古いrevision/二重要求/取消/編集中exportで破損や二重編集なし。read-onlyの`forge_surface_fit_inspect`はGUIと同じbounded候補計算を使い、対象identity・選択ID・距離・移動量を返す。出力対象とreport/hashが一致。G4末に再接続する計画にしない |
| NF-V1-14 | 保存・復旧・旧版受入 | schema変更時から継続、RC時最終確認 | 既存transaction回帰を再利用。中断で最後の正常projectを保持し、破損・移行・復旧案内と許容損失範囲を確認 |
| NF-V1-15 | 手動・長時間・別Windows | 一周可能なcandidate、最終は14 | DPI100/150/200%、IME/日本語/空白path、2時間編集、50回Open/Close、別PCと本人以外の手順テスト。条件・実測値・未実施を記録 |
| NF-V1-02B | 追加機能と同期の外部受入 | 10、11、12 | E06追加map、E07再適用、E08private uploadと他者視点を確認。E01〜E05も変更の影響範囲を回帰。VRChat受入未完了のまま対応済みとしない |
| NF-V1-16 | RCと対応表 | 02A/02B、08、10〜15 | 出荷candidateのcommit/build/package hashへ必要証拠をひも付け、対象経路にP0/P1なし。変更に影響しない証拠の再利用理由を記す |

### 最初の着手順

1. 01で既存receiver候補・過去SDK証拠・fixtureを確認し、03で衣装だけの受取契約を固定する。
2. 03Aで剛体/skinの最小受取を作り、02Aで独立reader・実SDK・localクライアントを確認する。
3. 04/05で造形→skinの欠けた接続、06/07で範囲限定転送を実装し、08のカフ1点を完走する。環境待ちでも独立したCore/GUI作業は進める。
4. 09/09A/10で必要材質・解像度を固め、11/12で複数出力・更新。13/14は各機能へ同時に組み込む。
5. 15/02B/16で手動・長時間・別環境・出荷候補を受け入れる。

## 5. 外部受入E01〜E08の配置

| ID | G1で確認すること | 後続で完成させること |
|---|---|---|
| E01 | Nya Ekaki 3D生成物なしの基準avatarでBuild & Test。環境版・ログ | RCクリーン導入 |
| E02 | 独立GLB reader/UniVRMで寸法・向き・bone/IBM・morphと対応VRM参照。既知座標の期待値 | 新map・新packageの外部互換 |
| E03 | 小物の明示装着、首のyaw/pitch/roll、scaleとreload | 自作小物と複数対象 |
| E04 | skin衣装、pose、同名骨/別source/骨順の負例 | 自作衣装と複数pose、4超weightは無言削減しない |
| E05 | 実PB componentのroot/endpoint/collider/設定・local挙動 | 対応すると決めたgrab/pose等と同期、reset/reload |
| E06 | 現行base color・alpha、布と金属係数の基準画像 | normal/MR・透過縁・裏面を固定shaderと明暗2環境で確認 |
| E07 | 現行static updateの証拠を確認し、新skin更新は未完了として残す | 12完了後にA→B→Editor再開、重複なし、元設定保持 |
| E08 | local Build & Test。upload可能条件を確認 | 同一candidateのprivate uploadと別クライアント/他者視点。外部送信は実行対象・公開範囲をユーザーが確認した段階で行う |

受入状態はPASS（証拠あり）、FAIL（不一致）、NOT RUN（未実施）、BLOCKED（その検査に必要な条件なし）、N/A（範囲外・理由あり）。これは検査単位の状態であり、プロジェクト全体を停止する宣言ではない。

独立readerの成功、自作readerの成功、Windows Player、実SDK、実VRChat、手動UIを別々に記録する。一つのreportへ全部PASSを合成しない。private素材・動画・環境固有pathはローカル証拠として扱い、publicには自作fixtureと公開可能な要約を置く。

## 6. 軽量性と画像品質の受入

持込の「P95入力100ms、1080pで30fps、2時間、50回Open/Close、warm baseline比10%または100MiB以内」は暫定測定案。V30では標準fixtureのforeground再生60秒＋停止5秒を実行し、**60.0016 FPS、P95 16.8724ms、最大17.7144ms、private bytes約577MB→563MB、working set peak約335MB**を観測した（レポート `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-PerformanceV30-Foreground-e8eaeb64df894b969e80fee4a092b347/performance.json`）。これはUnity 6000.4.3f1／RTX 4090／Windows 11／1280×800／DPI150%の一回の基準値で、予算合否や実RadDollV3通常編集の保証ではない。V39では50回の自動起動・終了回帰を完了した（`Artifacts/Navigation-Repeated-PerformanceV39-official-50.json`）。ただし20回更新・2時間編集、実マウス操作、別Windows環境の最終計測は未実施。FPS平均だけで長い入力停止を見逃さず、保持Unity Object数とmanaged/nativeメモリも記録する。異なる場面のピーク同士を比較してリーク判定しない。

Paintと表示textureで同じ解像度・更新頻度を必要とするかを09Aで判断する。原本4Kを常時CPU展開して全layerへ複製する変更は、軽量性・Undo・保存予算の検証を伴わせる。画像の縮小をユーザーへ明示し、原本未保持は出力損失として扱う。

## 7. 検証資料と原案の所在

持込原案は利用者のDownloadsに保存された `NyaForge-Windows-v1-Development-Plan.md`。SHA-256: `DA2364394716B44B4DCBE916E763DD5C77132C06F119B31B78A91127CE99D718`。原案は変更しない。本書は転載原文ではなく採用修正版で、日程仮定・依存・受取未実装・画像品質を修正した。

公式資料を2026-09-13に照合した範囲:

- [VRChat対応Unity](https://creators.vrchat.com/sdk/upgrade/current-unity-version/): 2022.3.22f1。standalone PlayerのUnity版とは分離。SDK/UniVRM/shaderは01で実際のmanifest/lockを固定する。
- [VRChat avatar作成手順](https://creators.vrchat.com/avatars/creating-your-first-avatar/): localテストとアップロード後の他者表示は別。アップロード可能条件も初回に確認する。
- [glTF材質仕様](https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#materials): 色画像と数値map、MR channel、normal/tangentをadapter間で明示する。

本書の初回策定時の検証は持込文書、現行ソース、既存記録、上記公式情報の照合のみだった。実装後のCore/Player/Bridge/SDK検証結果は[current_task](../current_task.md)へ時系列で追記し、合成fixtureと実アバター・実VRChatの受入を混同しない。




