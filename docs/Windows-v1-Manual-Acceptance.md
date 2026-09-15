# Nya Ekaki 3D Windows v1 手動受入チェック

この文書は、自動fixtureの合格を実アプリ・実アバター・VRChatの受入へ読み替えないための記録用チェック表。対象candidateは `Builds/BoneSubsetV16/NyaForge.exe`（検証時点のcommitは`current_task.md` NF-V1-15P／NF-V1-15Sへ記録）。自動navigation／Save As・保存状態・再開・viewport確認は既存の反復回帰へ記録している。最新の実RadDollV3取込probeは `Artifacts/Authoring-20260914-165134-a0c1b46b609043d4a29373c9c2342aeb/report.json`、V16起動・再開反復は `Artifacts/Navigation-Repeated-BoneSubsetV16-20260914-170330.json`、衣装一周はPlayer `Artifacts/Authoring-20260914-165304-a4fdb279b679471d99fcba5733339b17/report.json`、Bridge `Artifacts/BridgeReceiver-20260914-165530-393-cb754fbf3e254af482bb0fecda21ca22/bridge-report.json`。再実行入口は `Tools\\Test-NyaForgeAuthoring.ps1 -BuildName BoneSubsetV16 -ImportOnly -ImportModel <private VRM>` または `Tools\\Test-NyaForgeRealClothing.ps1 -ModelPath <private VRM> -BuildName BoneSubsetV16`。入力モデルは公開ツリーへコピーせず、privateの作業場所から読み込む。

現行candidateの補足: 標準起動先は `Builds/Windows/NyaForge.exe`。直近の実RadDollV3一周・Bridge・標準Navigationの証跡は`current_task.md`のV43記録を正本とし、上記V16参照は過去の受入履歴として扱う。再実行時は`-BuildName Windows`を使う。

## 1. 実EditorWindow（Windows）

- [ ] Windows DPI 100%で起動し、右側controlsのラベル・ボタン・statusが読める
- [ ] DPI 150%で同じ操作を行い、折りたたみとスクロールで下段が操作できる
- [ ] 日本語IME入力、空白を含むプロジェクトパス、Explorerのファイル選択が通る
- [ ] `GLBモデルを取り込む` から対象VRM/GLBを選び、候補mesh／skin／node instanceを確認する
- [ ] 候補欄のnode（配置）／mesh（形状）／skin（骨・weight）が縦積みで読め、長い候補名はステータスの完全名で照合できる
- [ ] 取込直後に「メッシュ全体を表示」を押さなくても、全身がviewport内へ自動Frameされる
- [ ] bodyを参照保護へ切り替え、頂点編集と納品対象追加が止まることを確認する

## 2. 衣装一周（実RadDollV3）

1. avatarを取り込み、bodyのgraph objectを参照保護にする。
2. Cuffまたはチョーカーを新規作成するか、別static GLBを読み込む。
3. 頂点を実マウスで選択・移動し、材質／UV／Paintを調整する。
4. `衣装をavatar骨格へskin-bind` → `自動weight初期化（avatar表面）` → Rigでweightを手修正する。
5. avatar面領域と衣装頂点を必要な範囲だけ指定し、`fit状態を測定（変更なし）` を押す。評価頂点数、移動量、裏側候補数を記録する。
6. 裏側候補がある場合は、面領域・offset・頂点選択を見直してからfitし、front/back/left/right/斜めで目視する。
7. 肩上げ、肘曲げ、前屈、着座相当のposeで、袖・襟・裾・胸周りの交差とweight崩れを確認する。
8. native Save → アプリを閉じる → Explorerから再Openし、形状・材質・weight・参照保護・対象allowlistが一致することを確認する。
9. 選択衣装だけのskinned packageを出力し、manifestのobjectId／mesh hash／skeleton hashを記録する。
   - 新規packageでは、実際にweightが参照する骨と祖先だけがskeleton sidecarへ入る。古いpackageで171本など不要な骨が残っている場合は、最新Playerで再出力する。
   - Unity受け取りでは `候補を生成（名前・階層）` → 候補一覧を確認 → `候補を割当に反映` の順に進め、曖昧・未検出のBoneIdは手動で確定する。
10. base-colorが取込時に縮小された場合、原画像サイズ・作業画像サイズ・MIME・hashをinspectionで確認する。Paintを編集しない状態ではGLB内の画像bytesが原画像と一致し、編集後はbounded previewへ切り替わることを確認する。

## 3. Unity受け取り

- [ ] Unity 2022.3.22f1の受け取り側でmanifestを読み込み、stable BoneIdを手動割当する
- [ ] 各BoneId欄の`期待階層`と割り当てたTransformの完全パスを確認してから保存する
- [ ] avatar rootを移動・回転・scale変更した状態で適用し、衣装がavatar-local位置を保つ
- [ ] 衣装Aを適用 → Bへ更新 → Unity再起動 → 割当を読込し、Aの重複やownership喪失がない
- [ ] 削除後に割当だけを読み込め、Undoで管理objectと資産が戻る
- [ ] normal／MRの見た目、Repeat／Clamp、alphaを受け取り側で確認する
- [ ] 未編集base-colorの原画像再出力と、編集後previewへのフォールバックをmanifest／GLBの画像MIME・寸法・hashで確認する

## 4. VRChat

- [ ] 対応するVRChat SDK／Unityプロジェクトを明記し、Build & Testが成功する
- [ ] PC向け実機または試験アバターで、rest pose・肩上げ・肘曲げ・前屈・着座を確認する
- [ ] PhysBonesは許可コンポーネントだけで構成され、未対応componentを持ち込まない
- [ ] VRChat内の表示、貫通、負荷、アップロード後の再現を記録する

## 記録欄

- candidate build / commit:
- Windows / DPI / GPU:
- avatar source hash（private記録）:
- fit inspection JSON / screenshot:
- native project path:
- clothing package manifest / hash:
- Unity SDK / receiver project:
- VRChat Build & Test結果:
- 未受入・再現条件:

「fit状態を測定」の裏側候補値は最近面のwindingによる保守的なサンプルであり、三角形交差・閉じた体積の内外判定・貫通ゼロを証明しない。画像の可愛さや販売品質も、数値検査とは別に人間またはAIの確認結果として記録する。

## 2026-09-15 実ウィンドウ部分受入（現行Player）

`Builds/ImportActionsReadableV1/NyaForge.exe`をWindows native Computer Use（`@oai/sky`）で起動し、次を実マウス操作で確認した。

- 「制作へ」→「モデルを追加」からExplorerのGLB／VRM pickerを開き、privateの`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-RealModelSmoke\RadDollV3_VRM.vrm`を指定。
- 候補確認後、縦積みの`node（配置）`／`mesh（形状）`／`skin（骨・weight）`欄と完全path表示を確認。
- `全meshをまとめて取り込む`を実行し、status `GLB / VRMの全mesh instanceを取り込みました。10 objects`を確認。
- 画面を最大化し、10個の`スキンモデル`対象一覧、制作対象の切替、`メッシュ全体を表示`導線を確認。
- body候補（object ID先頭`12707472`）を選択して`選択中を参照として保護（編集不可）`を有効化。status `選択中のobjectを参照として保護しました。編集操作は停止します。`を確認し、viewportクリック後も編集停止状態が維持された。

これはExplorer選択、候補確認、全mesh取込、対象切替、参照保護の実ウィンドウ部分受入である。DPI 150/200%、日本語IME、保存／再開、実衣装の頂点編集・fit・貫通、Unity受け取りの更新／削除Undo、normal／MR／UV0画素、VRChat Build & Testは未受入のまま残る。private素材とSDKは公開ツリーへ追加していない。

## 2026-09-15 MANUAL-03: native制作状態の保存・終了・再開

`Builds/ImportActionsReadableV1/NyaForge.exe`で空の制作プロジェクトへリング形状を追加し、制作画面の`3 保存とUnityへの受け渡し`で次の一周を実ウィンドウ操作した。

- 保存先を`Z:\TextureVoice_local\git\RadDollV3-clothing\private\viewer-data\packs\manual-authoring-reopen-20260915`へ指定し、`保存`を実行。status `保存しました: ...manual-authoring-reopen-20260915`、`project.nyaforge.json`と`blobs/`の生成を確認。
- アプリを閉じて同じPlayerを再起動し、`制作へ`→`確認・出力`→`3 保存とUnityへの受け渡し`を開いた。
- 保存先を再指定して`開く`を実行。未保存確認で`変更を破棄して進む`を選択し、status `制作状態を開きました。ここから新しい履歴を始めます。`、保存前と同じobject ID先頭`3f98f4a4`、リング形状の再表示、上部`保存済み`を確認。

これはnative制作状態の保存→アプリ終了→再起動→再開の手動受入PASSである。実RadDollV3の衣装編集・fit・貫通、Unity更新／削除Undo、normal／MR／UV0画素、VRChat Build & Testは未受入のまま残る。検証用リングのprojectはprivate配下で、公開ツリーへ追加していない。

## 2026-09-15 UNITY-UI-01: Unity衣装受け取り画面の下部操作到達性

Unity 2022.3.22f1の最小サイズ相当の衣装受け取りウィンドウで、骨割当一覧の固定スクロール領域が下部操作を押し出していた。ウィンドウ全体を縦スクロールできるようにした後、privateの隔離probeでスクロールバーを表示し、次の操作が画面下部へ到達できることを確認した。

- [x] package読込、avatar root、stable BoneId欄の表示
- [x] ウィンドウ全体の縦スクロール
- [x] `現在の割当を保存`、`保存済み割当を読み込む`、`事前診断（書き込みなし）`、`衣装を作成／更新`、`管理対象の衣装を削除（Undo可）`の表示
- [ ] 実RadDollV3衣装の適用結果・全周fit・貫通・材質
- [ ] avatar移動／回転／scale、更新／削除Undo、VRChat Build & Test

今回の実ウィンドウ確認はsynthetic packageのUI到達性に限定する。実衣装の見た目やVRChat受入へは読み替えない。

# 2026-09-15 UNITY-UI-02: PhysBones受け取り操作の到達性

PhysBones target package受け取り画面にも、stable bone一覧とcollider group欄が下部の保存／診断／適用操作を押し出す可能性があった。`UnityBridge/Editor/PhysBonesTargetPackageWindow.cs`へウィンドウ全体の縦スクロールを追加し、最小サイズでもpackage検証、割当保存／読込、事前診断、component作成／更新へ到達できる構造に揃えた。骨一覧内部の既存スクロールは維持している。

- [x] EditorWindowコードへ全体スクロールを追加
- [ ] Unity実EditorWindowで実packageを読み込み、collider groupを含む下部操作を目視確認
- [ ] VRChat SDK実環境でPhysBones適用結果を確認

Core／Bridgeの自動回帰はコード変更後に再実行する。実SDK・実avatarの見た目とVRChat受入は別カードとして残す。

# 2026-09-15 MANUAL-04: 実RadDollV3 VRMの候補確認と単一skin取込

`Builds/GuiModelAutoInspectV1/NyaForge.exe`をWindows native `@oai/sky`で操作し、Explorerからprivateの`Z:\TextureVoice_local\git\RadDollV3-clothing\private\viewer-data\packs\avatar-raddollv3-local\RadDollV3_VRM.vrm`を選択した。自動候補確認後、`選択候補を取り込む`を実行し、status `GLB skinを取り込みました。mesh 0・skin 0・mesh 0・bone 171・weight 2990・morphなし`を確認した。取込診断には`MATERIALS_NOT_RETAINED`、`EXTENSIONS_PARTIAL`、材質画像の注意1件が表示された。

これは実Explorer選択・候補確認・単一skin取込の手動受入PASSである。viewportで対象メッシュが表示されたことも確認した。材質保持・全mesh取込・実衣装編集・fit／貫通・保存再開・Unity適用・VRChat表示は別受入として未完了のまま残す。private素材と生成物は公開ツリーへ追加していない。

# 2026-09-15 GUI-15: コマンドバーへ現在対象の表示名と完全IDを表示

上部の制作対象表示が短い内部IDだけだったため、複数モデル編集時に何を操作しているか判別しにくかった。`AuthoringWorkbench.CommandBar`で、現在対象の表示名（カスタム名または役割名）・種別・短いID・保存状態を常時表示し、tooltipへ完全object IDとgraph情報を出すようにした。空projectでは従来の開始案内tooltipを表示する。対象一覧のstable ID、保存形式、MCP wireは変更していない。

- [x] 表示名／役割名をコマンドバーへ反映
- [x] tooltipへ完全object IDを反映
- [x] Multi-object Player検証へ表示名・ID確認を追加
- [ ] 実マウスで長い日本語名・DPI 150/200%の折返しを確認

# 2026-09-15 GUI-15: コマンドバーの現在対象表示

上部のコマンドバーが短いobject IDだけを示していたため、複数モデル編集時に現在の対象を判別しにくかった。`AuthoringWorkbench.CommandBar`を、表示名（カスタム名または役割名）・種別・短いID・保存状態の表示へ更新し、tooltipには完全object IDとgraph情報を残した。空projectではモデル／基本形状の開始案内をtooltipへ表示する。Multi-object検証へ表示名と完全IDの確認を追加した。

- Player build: `Builds/CommandContextV1/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260915-023657-149.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260915-023719-20f7d87d697e4efd9cb09e132bb949b3/report.json`、86 checks）。multi-object表示名／MCP更新／Undo／Redo／Save/Openを含む。
- Core回帰: **512 passed / 0 failed**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-423186c6f063442b9990083304938634`）
- 未受入: 実マウスでの長い日本語名・DPI 150/200%の折返し、実RadDollV3衣装fit／貫通／材質、Unity／VRChat実機。

# 2026-09-15 MANUAL-05: 実RadDollV3取込からチョーカー頂点編集・保存再開

`Builds/GuiModelAutoInspectV1/NyaForge.exe`をWindows native `@oai/sky`で操作し、privateのRadDollV3 VRMをExplorerから選択、自動候補確認後に`全meshをまとめて取り込む`を実行した。statusは`GLB / VRMの全mesh instanceを取り込みました。10 objects`。上部の`基本形状を追加`からリング（チョーカー）を追加し、制作対象一覧で追加された`基本形状`を選択、`他の制作対象も表示`をオフにして単独表示した。`メッシュ全体を表示`で形状をフレームし、viewportの頂点をクリックして選択、X方向10mmの`選択頂点を移動`を実行した。status `編集を反映しました。元に戻す・やり直すで確認できます。`を確認した。

保存先`Z:\TextureVoice_local\git\RadDollV3-clothing\private\viewer-data\packs\manual-real-model-choker-20260915`へ`保存`を実行し、status `保存しました`と保存済み表示を確認した。同じ画面で`開く`を実行し、status `制作状態を開きました。ここから新しい履歴を始めます。`、リングの再表示、`保存済み`表示を確認した。

これは実RadDollV3のExplorer取込→全mesh→基本形状追加→対象切替→単独表示→頂点選択／移動→native Save/Openの手動受入PASSである。材質の見た目、衣装skin-bind／fit／貫通、Unity受け取り、VRChat Build & Testは別受入として未完了のまま残す。private素材と制作データは公開ツリーへ追加していない。

# 2026-09-15 MANUAL-06: 隔離ビルドの初期制作画面レイアウト

`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Builds\AttachmentLabelsV2\NyaForge.exe`を`@oai/sky`で起動し、Viewerの`制作へ`を実マウスクリックした。1069×698のWindowsウィンドウで、空プロジェクト案内と右側controlsの`AI接続（MCP）`、`新しい空プロジェクト`、`モデルを開く…`、`保存済み制作を開く…`を確認した。長い日本語ボタンはcontrols欄内で折り返され、右端へはみ出さなかった。下部statusも表示された。

- [x] 初期空状態の導線・長いボタンの折返し・status表示
- [ ] DPI 150/200%、IME、Explorer、実モデル取込、装着先候補の表示名／tooltip
- [ ] 実RadDollV3の全周fit・貫通・材質見た目、Unity／VRChat実機

これは初期レイアウトの部分受入であり、上の未確認項目を合格扱いしない。証跡の詳細は`current_task.md`のMANUAL-01に記録した。

## 2026-09-15 MANUAL-07: 空projectのモデル取込欄への自動追従

`Builds/ImportScrollV4/NyaForge.exe`を`@oai/sky`で起動し、空projectの`制作へ`から右controlsの`モデルを開く…`を実マウスクリックした。モデル取込Foldoutが表示され、右側が`ファイルパス`、`node（配置）`、`mesh（形状）`、`skin（骨・weight）`、`候補を確認`、`選択候補を取り込む`、`全meshをまとめて取り込む`付近へ自動追従することを目視確認した。以前のように折りたたみ値だけが変わり候補欄が非表示になる状態は再現しなかった。

- [x] 空projectの入口からモデル取込Foldoutを表示
- [x] 候補選択欄と取込ボタンまで自動追従
- [ ] 実Explorerでファイルを選び候補確認・取込完了
- [ ] DPI 150/200%、IME、長いpath、実RadDollV3のfit・貫通・材質、Unity／VRChat実機

これは取込欄の表示・到達性の手動受入であり、実ファイル取込や販売品質の合格へは読み替えない。詳細は`current_task.md` GUI-16に記録した。
