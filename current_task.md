# 2026-09-15 FIT-REGION-BONE-02: 実RadDollV3で自動面領域の一周を確認

新しいBoneId基準面領域ボタンを検証用Playerにも接続し、実RadDollV3のチョーカー工程でNeckのstable BoneIdを選択した状態から半径60mmのavatar面を抽出できることを確認した。抽出面IDは既存のfit／表面weight共通入力へ渡り、衣装の明示Neck skin-bind、native Save/Open、GLB／VRM1、衣装package、Unity Bridgeの後続を壊さなかった。

- Player `Builds/BoneRegionV2/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260915-161851-358.log`）
- Core **517 passed / 0 failed**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-27f877339ce54ea79701ba6f73ba2bb9`）
- 実モデル Player **PASS 98 checks**（`Artifacts/Authoring-20260915-161920-14bc733c80f74858a4172454a1680771/report.json`）
- Unity Bridge **PASS 16 checks**（`Artifacts/BridgeReceiver-20260915-162235-598-5851bc45ea8f48da9a27a73858cd1be6/bridge-report.json`）
- 未完了: 自動抽出した範囲でfitを適用した外観、50mm以内・裏側候補0、正面／背面／左右・pose、実EditorWindow／VRChat受入。自動抽出は候補領域の作成であり、交差ゼロの証明ではない。

# 2026-09-15 FIT-REGION-BONE-01: BoneId基準のavatar面領域自動選択

チョーカーのfit範囲を実画面で手選択する際、首と衣装の重なりでavatar面クリックが安定しなかったため、選択中のstable BoneIdと半径からavatar rest meshの三角形領域を抽出する共通機能を追加した。骨名（Neckなど）をハードコードせず、Head・Tailのbone segmentから頂点または面中心が半径内に入る面をsubmesh順のflattened triangle IDで選ぶ。選択結果は既存のfit／表面weightへ同じ面IDで渡し、オレンジoverlay・要約・再計測要求も更新する。半径はmm入力（既定60mm）で、空領域や不正値は文書を変更せずエラー表示する。

- 変更: `Assets/NyaForge/Authoring/Geometry/MeshSurfaceRegion.cs`、`Assets/NyaForge/UnityRuntime/AuthoringWorkbench.AttachmentUi.cs`、`Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Attachments.cs`、`Assets/NyaForge/UnityRuntime/AuthoringWorkbench.AttachmentState.cs`
- 回帰: `Tests/Authoring.Core/MeshSurfaceRegionTests.cs`（affine transform、flattened submesh ID、半径検証）を追加。Core **517 passed / 0 failed**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-27f877339ce54ea79701ba6f73ba2bb9`）
- Player: `Builds/BoneRegionV1/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260915-160621-128.log`）ビルド成功。
- 実モデル一周: private RadDollV3で取込→頂点編集→Save/Open→GLB／VRM1→衣装package→Unity Bridgeを再実行。Player **PASS**（97 checks、`Artifacts/Authoring-20260915-160706-32f3d479cb07459da38df458e783ba91/report.json`）、Bridge **PASS**（16 checks、`Artifacts/BridgeReceiver-20260915-161250-743-c9ef8dc4cf47482d8b833e60ce6533da/bridge-report.json`）。
- 未完了: 実画面でNeck半径を調整し50mm以内・裏側候補0を達成すること、正面／背面／左右・poseでの貫通確認、DPI／IME／長いパス、実Unity Editor／VRChat受入。自動領域抽出は面候補を作る補助であり、交差ゼロを保証しない。

# 2026-09-15 ドキュメント同期: VRM意味情報診断とWindows v1残件

# 2026-09-15 現行Windows候補のnative保存状態再開

`Builds/Windows/NyaForge.exe --authoring true`をWindows native `@oai/sky`で起動し、制作画面の`保存済み制作を開く…`からprivateの`manual-real-model-choker-20260915/project.nyaforge.json`をExplorerで指定した。開く操作後、上部へ制作対象の`基本形状・制作物`と`保存済み`が表示され、ステータスに`制作状態を開きました。ここから新しい履歴を始めます。`が出た。保存済みプロジェクトを最新candidateで読み込めることを確認した。

これはnative projectのExplorer指定・再開表示の手動スモークである。再開後の全周カメラ・衣装fit・貫通・材質見た目、Unity受け取り、VRChat Build & Testは未受入のまま残る。private検証状態は公開ツリーへ追加していない。

# 2026-09-15 現行Windows候補の実ウィンドウ取込スモーク

`Builds/Windows/NyaForge.exe`をWindows native `@oai/sky`で起動し、通常窓（約1069×698）でViewerから`制作へ`を開いた。上部の`モデルを追加`で取込Panelへ移動し、`GLB / VRMを選ぶ`からExplorerを開いてprivateの`RadDollV3_VRM.vrm`を選択した。候補欄に`mesh 0 · Bag.baked`、`skin 0 · 171 bone`、10 instancesが表示され、候補確認後に`全meshをまとめて取り込む`を実行した。取込後は制作対象へスキンモデルが表示され、statusに`10 objects`、source hash、`VRM意味情報: firstPerson（未解決・詳細は警告）`、右Panelに`SpringBone設定: 5 chain · 53 joint · 4 collider group`が表示された。

これは現行ソースの通常窓でのViewer→制作→Explorer→候補確認→全mesh取込の手動スモークである。保存／再読込、長い日本語名・IME・DPI 150/200%、全周fit・貫通・材質見た目、Unity EditorWindowの適用、VRChat Build & Test／実機表示は別受入として残る。未保存の検証状態は終了時に破棄し、private素材を公開ツリーへ追加していない。

# 2026-09-15 Windows標準candidate再検証: 実RadDollV3衣装一周

現行main `69bed3d`から`Builds/Windows/NyaForge.exe`を再ビルドし、Core **515 passed / 0 failed**を再実行した。privateの`RadDollV3_VRM.vrm`を入力したPlayer Authoringは、取込・チョーカー生成／頂点編集・native Save/Open・標準skinned GLB／VRM 1.0出力・衣装package生成を含む **PASS**（`Artifacts/Authoring-20260915-111441-965632e9d6274b0badecf62de399d187/report.json`）。生成packageをUnity **2022.3.22f1** Bridgeへ渡した検証も **PASS**（`Artifacts/BridgeReceiver-20260915-111719-445-aa3b076eb92d4dda9afd832b6da6a880/bridge-report.json`）。

これは最新ソースの自動Player／Bridge経路を標準candidateで再確認した証拠であり、実EditorWindowのDPI・IME・長いパス、衣装の全周fit・貫通・材質見た目、VRChat Build & Test／実機表示、未保持VRM意味payloadの完全変換を完了扱いしない。

# 2026-09-15 Windows実画面スモーク: RadDollV3取込とチョーカー追加

`Builds/ManualCurrentV1/NyaForge.exe`をWindowsデスクトップで起動し、実モデルの取込導線を確認した。`private/viewer-data/packs/avatar-raddollv3-local/RadDollV3_VRM.vrm`をファイル選択ダイアログから指定し、「全meshをまとめて取り込む」を実行すると、モデルがビューポートへ表示され、`10 objects`、`171 bone`、`humanoid 29`、`SpringBone 5 chain / 53 joint / 4 collider group`の取込状態が表示された。続けて「基本形状を追加」から種類「リング（チョーカー）」を選び、寸法入力を表示し、「この寸法で形状を追加」を実行できた。追加後は制作対象が未保存の基本形状へ切り替わり、チョーカー調整の案内がステータスへ出た。

これはファイル選択・モデル表示・基本形状追加の手動導線を確認するスモークであり、保存／再読込、長いパス・高DPI・IME、全周フィット・貫通・見た目、Unity EditorWindow／VRChat実機受入を完了した証拠ではない。確認に使った画面は旧ManualCurrentV1で、最新SemanticInventoryビルドの機能差は別途確認する。

`docs/Development-Plan.md`と`docs/Windows-v1-Development-Plan.md`の現行main記述を更新した。Polygon→skin、衣装receiver、範囲限定fit／weight、semantic textureの実装・自動検証が進んでいること、初期profileで保持しないVRM LookAt／FirstPerson／表情material bindは`VRM_SEMANTICS_NOT_RETAINED`としてnative Save/Open・inspection・出力reportへ引き継ぐことを明記した。完全意味情報のpayload保管・変換、実EditorWindow／実SDK／実VRChat、全周fit・貫通・見た目は未受入／後続として残す。

- 文書commit: `2b642415b067da23dfcc861e99804cb5b025b56f`（`main`へpush済み、remote SHA一致）
- 検証: 文書差分の`git diff --check` PASS、private追跡ファイルなし
- 次: 意味payloadの依存込み保管を将来profileとして設計するか判断し、まずはWindows v1の実EditorWindow／外部受入カードを進める

# 2026-09-15 I04-E: VRM意味情報の在庫を明示

`VrmMetadata`へ`VrmSemanticInventory`を追加し、VRM 1.0の`lookAt`／`firstPerson`／表情`materialColorBinds`と、VRM 0.xの`firstPerson`／`materialValues`を「検出したが初期profileでは未解決」のパスとして保持するようにした。`HasLookAt`、`HasFirstPerson`、`ExpressionMaterialBindCount`、`UnresolvedPaths`、`IsComplete`を取込後の確認・将来の厳格出力判定へ渡せる。既存のgeometry、session、GLB/VRM出力は変更せず、検出時は既存`Warnings`にも境界を表示する。元payloadを保存したり、look-at／first-person／material bindを編集・変換したりする実装ではない。

- 変更: `Assets/NyaForge/Authoring/Import/VrmMetadata.cs`
- 回帰: `Tests/Authoring.Core/VrmMetadataTests.cs`へmodern／legacy semantic inventory確認を追加
- Core: **515 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-92f21edb619b4f5fa116c019941aeea4`）
- Windows Player: `Builds/SemanticInventoryV1/NyaForge.exe` をUnity 6000.4.3f1でビルド成功（`Logs/build-player-20260915-104932-512.log`）
- Authoring起動probe: **PASS**（`Artifacts/AuthoringStartup-20260915-105005-992cbcd877654492b422114fa673761f/report.json`、起動画面PNGあり）
- 文書: `docs/Model-Interchange-Spec.md`へ在庫と完全VRM出力の境界を追記

# 2026-09-15 I04-E: VRM意味情報を保存診断へ接続

`GlbImportDiagnostics.ForVrmSemantics`を追加し、取込時に検出した`lookAt`／`firstPerson`／material bind（VRM 0.xでは`materialValues`）を、既存の`ImportedGlbDiagnostics`へblocking診断として同梱するようにした。これにより、取込直後の表示だけでなくnative Save/Open後のinspection・GLB/VRM export report・strict complete-semantics判定でも、未保持の意味情報を同じsource pathで追跡できる。partial出力は従来どおり可能だが、strict出力では公開前に停止する。元payloadの保管・変換そのものはまだ実装していない。

- 変更: `Assets/NyaForge/Authoring/Import/GlbImportDiagnostics.cs`, `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Import.cs`
- 回帰: `Tests/Authoring.Core/VrmMetadataTests.cs`でmodern 3件／legacy 2件のblocking診断とpathを確認し、`ImportedGlbDiagnosticsTests.cs`でcodec往復も確認
- Core: **515 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-3b839abf5c3e430ca85590d353716f7d`）
- Windows Player: `Builds/SemanticInventoryV2/NyaForge.exe` をUnity 6000.4.3f1でビルド成功（`Logs/build-player-20260915-105316-539.log`）
- 実モデルimport-only: **PASS**（private一時RadDollV3 VRM、`Artifacts/Authoring-20260915-105403-126d9fa806c2419aa3c5119111e81dfc/report.json`、1600x1000 PNGあり）。これは自動Player経路の確認で、実マウスの全周fit・貫通・見た目、Unity／VRChat内表示は未受入。
- 実モデルVRM-output-only: **PASS**（`Artifacts/Authoring-20260915-105911-bb5ba9ac2d634987807f0aaea55bbe0e/report.json`）。出力`export-report.json`で`sourceSemanticStatus=partial`、`sourceBlockingDiagnosticCount=2`、`VRM_SEMANTICS_NOT_RETAINED`（`VRM.firstPerson`）を確認し、partial出力と未保持情報の追跡が両立している。これは自動Player／ファイル再読込の確認で、実受取側の見た目・挙動は未受入。
- 残り: 未知payloadの依存込み保管、完全VRM各受取側検証、実RadDollV3全周fit・貫通・見た目、Unity／VRChat実機受入。
# 2026-09-14 MOD-05: 保存状態Panelの通知境界

Sessionの保存状態変更時に、Workbench全体を再構築せず保存状態表示だけを更新する`AuthoringWorkbench.PersistenceRefresh.cs`を追加した。`SaveIncomplete`／読込先変更の通知はLifecycleからこの境界を通り、保存先入力欄の編集中の値は上書きしない。通常のcommand確定時は従来どおり全体refreshを使い、geometryや他Panelの再生成を増やしていない。

- 追加ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.PersistenceRefresh.cs`（`.meta`を含む）
- 接続変更: `AuthoringWorkbench.Lifecycle.cs`, `AuthoringWorkbench.RefreshState.cs`
- Player build: `Builds/GuiModularV37/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-215613-873.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-215641-dfcbc67dd5d94bfd904699113523466f/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-215710-d042fe1f34184811a48289577770a549/report.json`）
- Core: V36からCoreソース変更なし（直近 **512 passed / 0 failed**）
- 残り: metadata再同期とPanel購読の細分化、検証harness整理、実マウスのExplorer／DPI／IME／長い名称、実RadDollV3全周fit・貫通・見た目、Unity／VRChat実機受入。

V37のPlayerでprivate一時RadDollV3 VRM（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`）を使った自動実モデル一周も再確認した。全mesh取込→EditMesh→native Save/Open→GLB／VRM出力→衣装package生成は**PASS**（`Artifacts/Authoring-20260914-215848-6037034a834842b6aad6a640158fc6dc/report.json`）。生成packageのskeletonは2 bonesで、Unity **2022.3.22f1** Bridge受け取りも**PASS**（`Artifacts/BridgeReceiver-20260914-220108-201-3263e34f76694e75b4636b19e59a20b5/bridge-report.json`）。これは自動Player／Bridge経路の証拠で、実EditorWindowのマウス操作、実アバターの全周fit・貫通・見た目、VRChat内表示は未受入のまま残す。private素材は公開ツリーへコピーしていない。

# 2026-09-14 MOD-05: Workbench共有状態の分離

Workbenchの共有UIハンドル、live session、選択context、viewport状態を`AuthoringWorkbench.State.cs`へ移した。`AuthoringWorkbench.cs`はpartial-classのホストだけを持ち、機能Panel・共通command実行・全体refresh・ライフサイクル・共有状態の責務をファイル単位で追えるようにした。フィールドの参照先と初期値以外は変更していない。

- 追加ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.State.cs`（`.meta`を含む）
- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.cs`
- Player build: `Builds/GuiModularV36/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-215253-958.log`）
- Core回帰: **512 passed / 0 failed**（V35実行結果から変更なし）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-215329-bdd6ce225a9a4e89800aaaea4f720e6d/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-215356-f8f570cc9a224b2fb089e3f57beb5689/report.json`）
- 残り: Panel単位の購読境界とdirty／metadata再同期、検証harness整理、実マウスのExplorer／DPI／IME／長い名称、実RadDollV3全周fit・貫通・見た目、Unity／VRChat実機受入。

# 2026-09-14 MOD-05: 共通command実行境界の分離

Workbench本体に残っていた`Execute`／例外捕捉／ステータス表示を`AuthoringWorkbench.Execution.cs`へ移した。編集Panelは操作を`Execute`へ渡し、参照保護・揺れ再生の無効化・計測・Undo/Redo後のmetadata同期・選択の範囲検査・全体refresh・エラー表示を一つの共通境界で処理する。`AuthoringWorkbench.cs`は共有状態とライフサイクルへの入口に絞り、既存の操作順・保存形式・MCP wireは変更していない。

- 追加ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Execution.cs`（`.meta`を含む）
- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.cs`
- Player build: `Builds/GuiModularV35/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-214815-948.log`）
- Core回帰: **512 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-fbcc16771f144635b7a082dcc8594066`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-214843-65d7af1840ac4d79964b6e850bd8b09d/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-214910-681a725bc73d4d13aedc1aac7919bbab/report.json`）
- 残り: Panel単位の購読境界とdirty／metadata再同期の完全移管、共通fieldの段階的縮小、実マウスのExplorer／DPI／IME／長い名称、実RadDollV3全周fit・貫通・見た目、Unity／VRChat実機受入。

# 2026-09-14 MOD-05: 全体refresh境界の分離

共有Workbench本体に残っていた全体`Refresh()`を`AuthoringWorkbench.RefreshState.cs`へ移した。新しいファイルはlive workspaceを各projection・graph canvas・共通状態表示・Panel群へ反映する順序を所有し、頂点選択だけの変更は既存の`SelectionRefresh`を使う。`AuthoringWorkbench.cs`は共有field・共通`Execute`・エラー表示に絞り、保存形式・Undo/Redo・MCP wire・Panel内部処理は変更していない。

- 追加ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.RefreshState.cs`（`.meta`を含む）
- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.cs`
- Player build: `Builds/GuiModularV34/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-214150-748.log`）
- Core回帰: **512 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-eb4631448dd740a98367cfd9323b76c1`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-214216-4d54127c116d44c9b7cc1dd056784409/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-214250-4a8f3e6cabb143e0aaeea353fd2737fd/report.json`）
- 残り: Panel単位の購読境界とdirty／metadata再同期の完全移管、実マウスのExplorer／DPI／IME／長い名称、実RadDollV3全周fit・貫通・見た目、Unity／VRChat実機受入。

# 2026-09-14 MOD-05: 対象一覧の差分更新

頂点を1点編集するたびに対象一覧のボタンを全再生成していたため、`AuthoringWorkbench.Objects.cs`に対象一覧の差分更新境界を追加した。対象ID・表示名・active objectの組合せをキーとして保持し、キーが変わらない通常の頂点編集では既存ボタンを再利用する。空workspace、対象切替、表示名変更では一覧を再構築する。選択の正本、保存形式、tooltipの完全ID、Undo/Redo、MCP契約は変更していない。

- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Objects.cs`
- Player build: `Builds/GuiModularV32/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-213153-478.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-213221-4bc5390ed6974b3fabd9b318663b39b3/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-213256-87ca1c7371f34b4096566fd62e6f6311/report.json`）
- 残り: Panel単位の購読境界とdirty／metadata再同期の完全移管、対象一覧の実マウス確認（長い名前・DPI・IME）、実RadDollV3全周fit・貫通・見た目、Unity／VRChat実機受入。

# 2026-09-14 MOD-05: Workbenchライフサイクル責務の分離

起動・制作画面の開閉・終了確認・preview sceneの生成／破棄・Unityイベント購読を`AuthoringWorkbench.Lifecycle.cs`へ移した。`AuthoringWorkbench.cs`には共有field、共通`Execute`、全体`Refresh`を残し、画面配置は`Layout`、viewport入力は`ViewportInput`／`ViewportInteraction`へ委譲する。責務移動のみで、保存形式・Undo/Redo・MCP wire・終了ガードの挙動は変更していない。

- 追加ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Lifecycle.cs`（`.meta`を含む）
- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.cs`
- Player build: `Builds/GuiModularV33/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-213607-897.log`）
- Core回帰: **512 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-eb4631448dd740a98367cfd9323b76c1`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-213634-e20aa0d2fee04e42967917079bd0ac7e/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-213710-db14cf24a2304ea5acfe4bbf0b0b6b18/report.json`）
- 残り: Panel単位の購読境界とdirty／metadata再同期の完全移管、実マウスのExplorer／DPI／IME／長い名称、実RadDollV3全周fit・貫通・見た目、Unity／VRChat実機受入。

# 2026-09-14 MOD-05: 選択変更の部分refresh境界

頂点選択だけの変更で全Panelを再構築しないよう、`AuthoringWorkbench.SelectionRefresh.cs`を追加した。面編集（split／edge insertion／weld）の有効状態、選択表示、Rigの選択数、装着の選択頂点操作、viewportヒントだけを更新し、材質・出力・シミュレーションPanelの完全refreshは形状commandや保存時に限定する。`Select`／`PickVertex`からこの経路を呼ぶ。面モードでは面ハイライトと面操作の依存関係を維持する。V30で判明した回帰（選択後のface splitボタンが古いまま）を、依存グループを先に更新することで修正し、V31で再確認した。

- 追加ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.SelectionRefresh.cs`（`.meta`を含む）
- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ViewportInput.cs`
- Player build: `Builds/GuiModularV31/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-212647-299.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-212710-b51ad8e78eb144a48755fffe1eb597dd/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-212737-75308dac175b4d0191755a1d8825c447/report.json`）
- 残り: dirty／metadata再同期の完全移管、Panel単位の購読境界、実マウスのExplorer／DPI／IME／長い名称、実RadDollV3全周fit・貫通・見た目、Unity／VRChat実機受入。

# 2026-09-14 MOD-05: BuildUiのレイアウト責務分離

`AuthoringWorkbench.cs`に残っていた`BuildUi`のルート／ヘッダー／viewport／controls配置を`AuthoringWorkbench.Layout.cs`へ移した。Layoutは共有コンテナの生成とfeature `Build*`呼出し順だけを所有し、既存partialの編集処理・保存形式・MCP契約は変更していない。V29でUnity Playerを再ビルドし、既存のAuthoring／Navigation回帰を通した。

- 追加ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Layout.cs`（`.meta`を含む）
- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.cs`
- Player build: `Builds/GuiModularV29/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-211910-264.log`）
- Core回帰: **512 passed / 0 failed**（直近コード変更なし）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-211929-763b342295254da98ec2ae23b14a3332/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-211956-cac6859e221d41ea971297b8775a450f/report.json`）
- 残り: Panel単位の購読境界、dirty／metadata再同期の完全移管、実マウスのExplorer／DPI／IME／長い名称、実RadDollV3全周fit・貫通・見た目、Unity／VRChat実機受入。

# 2026-09-14 MOD-05: viewportイベント配線の責務分離

`BuildUi`に残っていたviewportのGeometryChanged／Pointer／Wheelイベント配線を`AuthoringWorkbench.ViewportInteraction.cs`へ移した。`Viewport.cs`はカメラとRenderTexture、`ViewportInput.cs`は選択・hit test・座標変換、`ViewportInteraction.cs`はUI Toolkitイベントから既存操作を呼ぶ接続だけを担当する。クリック、ドラッグ、ウェイトペイント、切断path、surface triangle選択、Undo履歴の挙動は変更していない。

- 追加ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ViewportInteraction.cs`（`.meta`を含む）
- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.cs`
- Player build: `Builds/GuiModularV28/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-211201-584.log`）
- Core回帰: **512 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-96e20ef4219d46ff966744e521b18276`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-211231-cd02dc41f7224fb496166eb56e93b28d/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-211259-d69c08f4c167468ea4fa0c87688b0917/report.json`）
- 残り: BuildUiの完全分離、Panel単位の購読境界、dirty／metadata再同期の完全移管、実マウスのExplorer／DPI／IME／長い名称、実RadDollV3全周fit・貫通・見た目、Unity／VRChat実機受入。

# 2026-09-14 MOD-05: 頂点操作とviewport hit testの責務分離

Workbench本体に残っていた選択頂点の移動、対象選択、viewport上の頂点hit test、画面座標変換を`AuthoringWorkbench.ViewportInput.cs`へ移した。`AuthoringWorkbench.cs`はUI配置・共有状態・共通`Execute`・全体refreshを所有し、入力処理は`Viewport`／`ViewportInput`へまとまる。world-spaceの`WorldPoints`を使う装着後hit testと、既存のgraph／static／polygon編集command、SelectionContext、Undo履歴は変更していない。

- 追加ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ViewportInput.cs`（`.meta`を含む）
- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.cs`
- Player build: `Builds/GuiModularV27/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-210331-861.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-210357-4adcddd9da274cf4be087c78e3e8a3a8/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-210431-75ccfd8de0ed433799bd5cb818374a99/report.json`）
- 残り: Shell／BuildUiの完全分離、Panel単位の購読境界、dirty／metadata再同期の完全移管、実マウスのExplorer／DPI／IME／長い名称、実RadDollV3全周fit・貫通・見た目、Unity／VRChat実機受入。

# 2026-09-14 GUI-03 継続: MCP表示名更新を共通metadata経路へ接続

GUIで保存した表示名を外部AIからも変更できるよう、専用MCP method `object_label`（tool `forge_set_object_label`）を追加した。`ObjectLabelRequest`はinstance／document／revision／attachments hash／stable ObjectIdをすべて要求し、`ObjectLabelService`がGUIと同じ`object-labels.nyaforge.bin`へ更新を行う。metadata変更はメッシュのdocument revisionを変えず、workspaceのUndo/Redo履歴へ一つの段として積む。空文字は自動役割名へ戻す。古いmetadataや競合hashは黙って上書きせず、`ATTACHMENTS_CHANGED`等で停止する。

`AuthoringIpcRequest`／`AuthoringWorkbench.McpCommands`／`AuthoringReadService.capabilities`へmethodを接続し、MCP SDK側には`ObjectLabelCommand`と`forge_set_object_label`の説明を追加した。Player回帰ではGUI保存後にMCP経路で改名し、Undo/Redo・Save/Open・対象一覧表示まで確認している。さらに外部sidecar→named pipe→Playerの実transportで、改名・stale metadata拒否・Undo/Redoを確認した。

- 追加ソース: `Assets/NyaForge/Authoring/Persistence/ObjectLabelService.cs`（`.meta`を含む）、`Assets/NyaForge/UnityRuntime/AuthoringWorkbench.McpObjectLabels.cs`（`.meta`を含む）、`Tools/NyaForge.Mcp/ObjectLabelCommand.cs`
- 接続変更: `AuthoringIpcRequest.cs`, `AuthoringWorkbench.McpCommands.cs`, `AuthoringReadService.cs`, `AuthoringWorkbench.MultiObjectVerification.cs`
- Core: **512 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-af1784a6b49946cab0eef074f51c7b95`）
- MCP SDK: `dotnet build Tools/NyaForge.Mcp/NyaForge.Mcp.csproj --no-restore --nologo` **0 warnings / 0 errors**
- Player build: `Builds/GuiLabelsMcpV26/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-205319-499.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-205340-3d288d957f914596a284676cb0614a9b/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-205420-558384436e6349589de69a22bdec3d13/report.json`）
- private RadDollV3再確認: **PASS**（Player `Artifacts/Authoring-20260914-205921-f9a91b9d3edd4194bfae5603655cfe00/report.json`、Unity 2022.3.22f1 Bridge `Artifacts/BridgeReceiver-20260914-210152-501-99bd0996ee7a42eaa8df03776a93fe46/bridge-report.json`、衣装packageのskeleton 2 bones）。private素材は公開ツリーへコピーしていない。
- 残り: group semantics、同名表示の手動確認、実マウスのExplorer／DPI／IME／長い名称、実RadDollV3全周fit・貫通・見た目、Unity／VRChat実機受入。Computer Use接続ではnative `apps`が空のため実マウス操作は未実施。

# 2026-09-14 GUI-03 / MOD-05: 制作対象の表示名をnative metadataへ分離

制作対象一覧の内部IDだけでは身体・衣装・小物の識別が難しいため、表示名をWorkbenchの一時UI状態から分離した。`ObjectLabelsCodec`がstable `ObjectId`をキーに任意の表示名を`object-labels.nyaforge.bin`へ保存し、`ProjectAttachments`（最大10件）、`ProjectStore`、`ProjectSnapshotCodec`のlegacy sidecar検出へ接続している。空欄保存はラベルを削除し、旧文書やラベル未設定の対象は従来の役割名＋短縮IDへフォールバックする。

`AuthoringWorkbench.ObjectLabels.cs`は読込hashをキャッシュし、表示名入力・保存・Undo/Redoを共通`Execute`／attachment履歴へ通す。`Objects.cs`は対象一覧に表示名入力と専用一覧を追加し、`AuthoringStateReader`／`AuthoringGraphReader`も同じmetadataから`displayName`を返す。長いScrollViewのポインタ座標に依存しないよう、自動回帰の保存確認は同じGUI commandを直接呼び、一覧の表示・選択・tooltip hit testは既存Navigation経路で確認する。

- 追加ソース: `Assets/NyaForge/Authoring/Persistence/ObjectLabelsCodec.cs`（`.meta`を含む）、`Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ObjectLabels.cs`
- 接続変更: `ProjectAttachments.cs`, `ProjectStore.cs`, `ProjectSnapshotCodec.cs`, `AuthoringStateReader.cs`, `AuthoringGraphReader.cs`, `AuthoringWorkbench.Objects.cs`
- 回帰追加: `Tests/Authoring.Core/ProjectSnapshotTests.cs`, `Tests/Authoring.Core/AuthoringStateTests.cs`, `AuthoringWorkbench.MultiObjectVerification.cs`
- Core: **510 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-dbb336a8381c4b4b8427fccf68dafb7a`）
- Player build: `Builds/GuiLabelsV24/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-204135-013.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-204157-9308ee6356504cd8ad3a877127de63e0/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-204224-23f71bf491a245408d4dd8ee85ec1be4/report.json`、`player.log`に`NYAFORGE_NAVIGATION_CHECK PASS`）
- 残り: MCPからの表示名変更・グループ意味論、同名表示の手動確認、実マウスのExplorer／DPI／IME／長い名称、実RadDollV3全周fit・貫通・見た目、Unity／VRChat実機受入。Computer Use接続ではnative `apps`が空のため実マウス操作は未実施。

# 2026-09-14 MOD-05 第三段: ビューポート責務の分離: Viewport

`AuthoringWorkbench.cs`に残っていたカメラのFrame処理とRenderTexture更新を`AuthoringWorkbench.Viewport.cs`へ移した。`Frame()`は各projectionが提供するFramingPointsをカメラ状態へ変換し、`UpdateCamera()`はUI viewportの実寸に合わせたRenderTexture再生成、orbit pose適用、surface preparationの呼出しだけを担当する。Workbench本体は共有field、BuildUi、入力イベント、終了確認の所有者として残し、既存のクリック・ホイール・自動Frameの呼び出し、保存形式、Core、MCP wire契約は変更していない。

- 追加ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Viewport.cs`（`.meta`を含む）
- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.cs`
- 設計更新: `docs/Authoring-Code-Map.md`, `docs/Windows-v1-GUI-Navigation-Plan.md`, `docs/Windows-v1-Development-Plan.md`
- 検証: `Tools/Build-NyaForge.ps1 -Target Player -BuildName GuiModularV19`でUnity **6000.4.3f1** Player build成功（`Logs/build-player-20260914-202637-096.log`、`Builds/GuiModularV19/NyaForge.exe`）。Authoring suite **PASS**（`Artifacts/Authoring-20260914-202705-4e6bb8af942e4de4a0a305a06bbed25d/report.json`、1280x800）、Navigation suite **PASS**（`Artifacts/Navigation-20260914-202731-cf4af1c2addc407ba6e5dabbf53f64de/report.json`）。
- 未受入: Panel単位の購読境界、dirty／metadata再同期の完全移管、実マウスのExplorer・DPI・IME・長い名称、実RadDollV3全周fit／貫通／見た目、Unity／VRChat内表示。Computer Use接続ではネイティブ`apps`が空で、実マウス操作は未実施。

# 2026-09-14 MOD-05 更新群の責務分離: RefreshPipeline

# 2026-09-14 MOD-05 共通UI部品の責務分離: UiElements

V18でprivate RadDollV3を再実行し、Player **95 checks PASS**（`Artifacts/Authoring-20260914-201954-7af6c0ce8bd4414fbcd0dd380b966605/report.json`）、Unity **2022.3.22f1** Bridge **16 checks PASS**（`Artifacts/BridgeReceiver-20260914-202221-884-53ba0f0f3ed74bf08bd50635e637cf6c/bridge-report.json`）を確認した。共通UI部品の移動後も、実モデルの取込・保存／再開・衣装package出力・受け取り経路は維持されている。これは自動受入であり、実EditorWindowのマウス操作・全周fit／貫通・VRChat実機表示は未受入である。

V18 Playerを通常ウィンドウで起動してComputer Useの`getState`を再確認したが、ネイティブ`apps`は空のままだった。起動したPIDは確認後に停止した。したがって、この接続では実マウス・DPI・IME・Explorer操作を自動代行できないという証拠を更新し、手動受入を未完了のまま保持する。

`AuthoringWorkbench.cs`に残っていた全Panel共通の`Row`／`Button`／`Number`生成ヘルパーを`AuthoringWorkbench.UiElements.cs`へ移した。行の折返し、ボタンの命名、数値入力の最小高さを一箇所で管理し、各Panelへ異なるDPI／狭幅ルールが混在しないようにした。UIイベント、保存形式、command経路は変更していない。

- 追加ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.UiElements.cs`（`.meta`を含む）
- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.cs`
- 検証: `Builds/GuiModularV18/NyaForge.exe`（Unity 6000.4.3f1）をビルド。Authoring suite **PASS**（`Artifacts/Authoring-20260914-201755-c26fa452a98f4919a66e1ac5f99cb15b/report.json`、1280x800）、Navigation suite **PASS**（`Artifacts/Navigation-20260914-201755-3ba7949e89b4409fab68c439f979e309/report.json`）。
- 未受入: Panel単位の購読境界、dirty／metadata再同期の完全移管、実マウスのExplorer・DPI・IME・長い名称、実RadDollV3の全周fit／貫通、Unity／VRChat内表示。

# 2026-09-14 実RadDollV3受け渡し再確認: V17

公開リポジトリ外の一時ファイル`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-RealModelSmoke\RadDollV3_VRM.vrm`を直接入力し、`Tools/Test-NyaForgeRealClothing.ps1 -BuildName GuiModularV17`を実行した。Windows Playerの取込→全mesh候補→EditMesh→native Save/Open→標準GLB／VRM出力→選択衣装package生成は**95 checks PASS**（`Artifacts/Authoring-20260914-201249-b4d210d9e71b41e48fc7a7e4f8a1a4ac/report.json`）。生成した`skeleton.nyaforge.bin`は2 bonesで、Unity **2022.3.22f1** Bridgeの受け取りも**16 checks PASS**（`Artifacts/BridgeReceiver-20260914-201516-211-e092e40f09984cf39a0fc3c1aea9010d/bridge-report.json`）。private素材は公開ツリーへコピーしていない。

これは実モデルの取込・保存・出力・受け渡し回帰であり、実EditorWindowの全周fit／貫通ゼロ／見た目、実マウス・DPI・IME・Explorer、VRChat Build & Test／実機表示の合格とは分けて扱う。Computer Useの`getState`ではこの接続にネイティブ`apps`が列挙されず、今回の実マウス操作は未実施として残す。

`AuthoringWorkbench.RefreshPipeline.cs`を追加し、既存の`Refresh()`が直接列挙していたPanel更新を、編集系（形状・UV・Paint・材質・Rig・Morph）と確認・出力系（Evidence・形状作成・取込・PhysBones・secondary motion・spring・Validation・CommandBar）へまとめた。Workbench本体は共有状態と更新順の所有者として残し、各Panelの実装やcommand経路は変更していない。これにより、Panel単位の購読・必要な更新だけへ移す次段の境界をコード上で確認しやすくした。

- 追加ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.RefreshPipeline.cs`（`.meta`を含む）
- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.cs`
- 設計更新: `docs/Authoring-Code-Map.md`, `docs/Windows-v1-GUI-Navigation-Plan.md`
- 影響範囲: UI更新の呼び出し整理のみ。Core、保存形式、MCP wire、Undo履歴は変更していない。
- 検証: 次のWindows Player buildで既存Authoring／Navigation suiteを再実行する。
- 未受入: Panel単位の購読境界、dirty／metadata再同期の完全移管、実マウスのExplorer・DPI・IME・長い名称、実RadDollV3装着／貫通、Unity／VRChat内表示。

検証結果: `Builds/GuiModularV17/NyaForge.exe`をUnity **6000.4.3f1**で再ビルドし、Authoring suite **PASS**（`Artifacts/Authoring-20260914-200958-53dc6237465b4082b3dc6618fdbb57e6/report.json`、1280x800）とNavigation suite **PASS**（`Artifacts/Navigation-20260914-201030-ce61d8afb7d14e6bb39fcdbad8ef8c7b/report.json`）を確認した。ビルドログは`Logs/build-player-20260914-200935-032.log`。最初のV16実行では既存の作業モードhit testが画面外ボタンを直接検査して失敗したため、検証側を`ScrollTo`経由へ修正し、V17で再確認した。これは回帰harnessの安定化であり、実マウスのDPI・IME・Explorer受入とは別である。

# 2026-09-14 GUI-05 / MOD-03 第二段: 作業モード導線のポインタ回帰を追加

作業モードの4ボタンが、見た目だけでなく実際のポインタ hit test から既存設定へ移動できることを確認するため、`AuthoringWorkbench.WorkModeVerification.cs`へ専用の回帰を追加した。形状編集は形状作成＋グラフ詳細、UV・色はUV／ペイント／材質、装着・骨は装着／Rig、確認・出力はEvidence／Validation／ProjectOutputを開くことを検査する。既存のパネルと編集commandは変更せず、ナビゲーションの責務だけを検証している。

- 追加ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.WorkModeVerification.cs`
- Player build: `Builds/GuiModularV11/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-193622-049.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-193807-5867461c41ee430dbbeb572bd4e15a0a/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-193846-821b8fb3b3ba4ff780ba0135e0c356eb/report.json`、1280x800、作業モードの実ポインタ検査を含む）
- Core回帰: 直近記録の**509 passed / 0 failed**からCore変更なし。今回の追加はUI検証のみ。
- 目視: Authoring artifactの`authoring.png`で、常設コマンドバー、対象、4作業モード、3D viewportの同時表示を確認。
- 未受入: 実マウスでの長い名称・Explorer選択・DPI100/150/200%、実RadDollV3装着と貫通、Unity／VRChat内表示、表示名のSave/Open永続化。自動回帰はPlayer／fixture経路の確認であり、これらを完了扱いにしない。

# 2026-09-14 MOD-05 第一段: Workbench Session / SelectionContext

Workbenchの共有状態を整理するため、`AuthoringWorkbenchSession`を追加し、live workspaceと対応する`AuthoringCommandService`を同じ所有者から生成するようにした。Workbench内の既存partialは互換プロパティ経由で同じ文書・同じUndo履歴を参照するため、Core、保存形式、MCP wire契約は変更していない。`SelectionContext`は頂点選択・面選択・active object ID・edit node IDを保持し、既存のパネルと回帰コードが別の選択コピーを作らないようにした。object／edit stageの切替時にはcontextも更新・解除する。

- 追加ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbenchSession.cs`, `Assets/NyaForge/UnityRuntime/SelectionContext.cs`
- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.cs`, `AuthoringWorkbench.ProjectActions.cs`, `AuthoringWorkbench.Objects.cs`, `AuthoringWorkbench.GraphEditing.cs`, `AuthoringWorkbench.FaceEditing.cs`
- Player build: `Builds/GuiModularV12/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-194454-848.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-194520-b01addce76eb4e4596555ac7112e0b28/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-194552-a447e25ae069435b9d7cd2b7062147d2/report.json`、1280x800）
- Core回帰: Core計算・保存形式の変更なし。直近記録の**509 passed / 0 failed**を維持するUI/runtime接続変更。
- 未受入: SelectionContextのイベント通知化、Sessionへのdirty／metadata再同期の完全移管、実マウスでのExplorer・DPI・IME・長い名称、実RadDollV3装着／貫通、Unity／VRChat内表示、表示名のSave/Open永続化。

## 次の着手タスク（モジュール化後）

- [x] **MOD-05 / WorkbenchSession 第一段**: workspace交換時にlive workspaceとcommand serviceを同じsession ownerから生成するようにした。Core／保存形式／MCP wire契約は変更していない。
- [x] **MOD-05 / SelectionContext 第一段**: object・編集段・点／面選択を一つのcontextへ集約した。各Panelの別選択コピーを廃止した。
- [x] **MOD-05 / Viewport境界**: Viewport／ViewportInput／ViewportInteraction／Layoutへ、カメラ・入力・UIイベント・画面構築の責務を分離した。V27〜V29のPlayer回帰を通過。
- [x] **MOD-05 / Lifecycle境界**: 起動・制作画面の開閉・終了確認・preview sceneの寿命・Unityイベント購読を`AuthoringWorkbench.Lifecycle.cs`へ分離し、V33のPlayer回帰を通過。
- [x] **MOD-05 / 対象一覧差分更新**: object ID・表示名・active objectが変わった時だけ一覧ボタンを再生成し、頂点編集時のUI再利用をV32で確認。
- [x] **MOD-05 / 全体refresh境界**: 全体`Refresh()`の投影・状態反映順を`AuthoringWorkbench.RefreshState.cs`へ分離し、V34のCore／Player回帰を通過。
- [x] **MOD-05 / 共通command実行境界**: `Execute`・例外捕捉・ステータス表示を`AuthoringWorkbench.Execution.cs`へ分離し、V35のCore／Player回帰を通過。
- [x] **MOD-05 / Workbench共有状態境界**: 共有UIハンドル・live session・selection context・viewport状態を`AuthoringWorkbench.State.cs`へ分離し、V36のPlayer回帰を通過。
- [x] **MOD-05 / 保存状態Panel境界**: Session通知から保存状態表示だけを`AuthoringWorkbench.PersistenceRefresh.cs`へ更新し、保存先入力欄の編集中値を保持するV37回帰を通過。
- [ ] **MOD-05 継続**: dirty／Undo後metadata再同期の完全移管、選択変更通知、検証harnessの整理を進める。
- [x] **MOD-05 通知第一段**: Sessionの状態通知をコマンドバー更新へ、SelectionContextの通知を投影選択同期へ接続し、Player回帰で一回発火を確認。
- [ ] **MOD-05 通知継続**: Panel単位の購読境界と、metadata再同期後に必要なPanelだけを更新する順序を整理する。
- [x] **GUI-03 第一段**: 表示名をstable ObjectId keyed native attachment（`object-labels.nyaforge.bin`）としてGUI／Save/Open／Undo/Redo／inspectionへ接続。旧文書は役割名へフォールバックし、内部IDは一覧の短縮表示とtooltipへ残す。
- [ ] **GUI-03 継続**: group semantics、同名表示の実マウス受入を追加する。外部MCPからの表示名変更はV26のsidecar transport回帰で完了。
- [ ] **GUI-08 / 手動一周**: 実マウスで「読込→基本形状→対象選択→編集→保存再開→出力」を行い、DPI・IME・長い名称・Explorer選択を記録する。
- [ ] **NF-V1-07/08/15**: 実RadDollV3で全周fit・貫通・見た目を確認し、Unity／VRChat内表示と区別して記録する。

# 2026-09-14 MOD-05 第二段: Sessionへの保存状態移行

`AuthoringWorkbenchSession`がlive workspace／command serviceに加えて、読み込んだ制作フォルダ（`LoadedDirectory`）と保存未完了フラグを所有するようにした。既存のSave、MCP Save、終了確認、検証コードは互換プロパティ経由で同じ値を参照するため、保存形式やMCP wire契約を変更していない。重複フィールドを除去した後、Playerで保存・再開・Undo・出力の既存一周を再確認した。

- 修正ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.cs`, `AuthoringWorkbench.ProjectActions.cs`, `AuthoringWorkbench.Saving.cs`, `AuthoringWorkbenchSession.cs`
- Player build: `Builds/GuiModularV14/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-195139-670.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-195202-3f7e55c3be2d43318a06444e087e6278/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-195236-0da0e6080b6f4ed6a31964569a58252d/report.json`、1280x800）
- 追加確認: `session owns workspace/commands and SelectionContext owns object/edit-stage/vertex/face selection`
- 未受入: dirty／metadata再同期の完全なSession移管、SelectionContextの変更通知化、検証harness整理、実マウスでのExplorer・DPI・IME・長い名称、実RadDollV3装着／貫通、Unity／VRChat内表示、表示名のSave/Open永続化。

# 2026-09-14 MOD-05 通知第一段: Session／SelectionContextイベント接続

`AuthoringWorkbenchSession.StateChanged`と`SelectionContext.Changed`を追加し、Session通知はコマンドバー表示、SelectionContext通知は投影の選択状態へ接続した。頂点・面・UV・Rig・MCPの主要な選択変更と、文書command／保存状態変更から通知を発火する。通知は既存の`Refresh`順やUndo履歴を置き換えず、Panel単位の更新へ移るための境界として利用する。

- 修正ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbenchSession.cs`, `Assets/NyaForge/UnityRuntime/SelectionContext.cs`, `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.cs`, `AuthoringWorkbench.FaceEditing.cs`, `AuthoringWorkbench.GraphEditing.cs`, `AuthoringWorkbench.Solidify.cs`, `AuthoringWorkbench.UvEditing.cs`, `AuthoringWorkbench.UvDrag.cs`, `AuthoringWorkbench.Rig.cs`, `AuthoringWorkbench.McpCommands.cs`
- 回帰ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.SessionVerification.cs`
- Player build: `Builds/GuiModularV15/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-195829-806.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-195853-7b2f7666b56742cfbb8020380f8a851e/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-195929-155bb8cebb2147fb9680230825b7e06a/report.json`、1280x800）
- 追加確認: `session owns workspace/commands and SelectionContext owns object/edit-stage/vertex/face selection with change notifications`
- 未受入: Panel単位の購読境界、dirty／metadata再同期の完全移管、実マウスでのExplorer・DPI・IME・長い名称、実RadDollV3装着／貫通、Unity／VRChat内表示、表示名のSave/Open永続化。

# 2026-09-14 MOD-05 第一段補強: Session / SelectionContext所有権回帰

共有状態の分割が実際に一つの所有者へ接続されていることを確認するため、`AuthoringWorkbench.SessionVerification.cs`を追加した。live workspaceとcommand serviceが`AuthoringWorkbenchSession`の同じインスタンスから参照され、頂点／面選択が`SelectionContext`の同じcollectionを見ていること、object切替・edit stage切替後のIDがcontextへ同期することをPlayerで検査する。これはPanelごとの状態複製を防ぐ回帰で、dirty／metadata再同期の完全移管や選択変更通知そのものは次段に残す。

- 追加ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.SessionVerification.cs`
- Player build: `Builds/GuiModularV13/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-194829-154.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-194857-5dd55828fa5a4e5abebe5ce204e7b42f/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-194929-3d65d852aaa04ea692070dd6ff4ec2a9/report.json`、1280x800）
- 追加確認: `session owns workspace/commands and SelectionContext owns object/edit-stage/vertex/face selection`
- 未受入: Sessionへのdirty／metadata再同期の完全移管、SelectionContextの変更通知化、実マウスでのExplorer・DPI・IME・長い名称、実RadDollV3装着／貫通、Unity／VRChat内表示、表示名のSave/Open永続化。

# 2026-09-14 GUI-02/03・MOD-02/03 第一段: 取込案内と対象ラベルの責務分割

制作対象の一覧で内部用語だけが表示され、取込画面も最初の手順が伝わりにくかったため、表示責務を追加分割した。`AuthoringWorkbench.ObjectLabels.cs`へ対象の役割名（基本形状／アバター（表情あり）／スキンモデル／衣装・スキン小物／装着アクセサリー／メッシュ小物）とホバー用詳細を集約し、一覧は短い役割名＋短縮ID、tooltip末尾は完全object IDとした。`AuthoringWorkbench.ModelImportGuidance.cs`へ「①ファイルを選ぶ→②候補を確認→③1件または全件を取り込む」の利用者向け説明を切り出し、内部用語中心の説明とは分離した。名前の永続化、Viewerからのモデル引継ぎ、取込Panel全体の独立クラス化は後続タスクとして残す。

- 変更ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ObjectLabels.cs`, `AuthoringWorkbench.ModelImportGuidance.cs`, `AuthoringWorkbench.Objects.cs`, `AuthoringWorkbench.Import.cs`
- 設計更新: `docs/Windows-v1-GUI-Navigation-Plan.md`（GUI-02/03、MOD-02/03の第一段を反映）
- 操作説明更新: `docs/Authoring-Quickstart.md`（モデル追加の3段階と対象役割名を反映）
- 開発計画更新: `docs/Windows-v1-Development-Plan.md`
- Player build: `Builds/GuiModularV6/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-191838-858.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-191905-25546024dd8a4338a702c7045443fdfb/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-191940-d9ef29f5edf14ac2bf64d9c6b352b513/report.json`、1280x800）
- Core回帰: **509 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-912036e642ee45a091c872f2b07eee42`）
- 未受入: 実マウスでの長い名称・候補操作、表示名のSave/Open維持、実RadDollV3装着、Unity／VRChat内表示。自動回帰はコード／fixture経路の確認であり、これらを完了扱いにしない。

# 2026-09-14 GUI-01 第一段補強: 開発者fixtureを通常導線から分離

尺度確認用の「プレート追加 ×1／×100」が制作入口に見えていたため、`developer-fixtures` foldoutへ移し、説明を「通常の制作には使わない確認用プレート」と明示した。空のviewport案内とプロジェクト説明は、上部の「モデルを追加」「基本形状を追加」へ統一した。既存の`fixture-1`／`fixture-100` stable nameは自動検証のため保持している。

- 修正ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.cs`
- GUI設計更新: `docs/Windows-v1-GUI-Navigation-Plan.md`（GUI-01第一段の完了条件を反映）
- Player build: `Builds/GuiModularV7/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-192328-817.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-192354-56f31d2a919045f294c0aa466d2acba4/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-192429-efcbb169b96043e2a8ed91e5b1192941/report.json`、1280x800）
- 未受入: 実マウスでfoldout開閉とExplorer選択、DPI別表示、実RadDollV3装着、Unity／VRChat内表示。

# 2026-09-14 GUI-06 / MOD-04 第一段: 保存・出力Panelの責務分割

保存・再開とGLB／Unity／衣装package／VRM出力が長い右側パネルに散らばっていたため、`AuthoringWorkbench.ProjectOutput.cs`へ「3 保存とUnityへの受け渡し」を切り出した。native制作状態の保存・開く・Explorer選択と、受け渡し用の各出力を同じ見出しにまとめ、用途の違いを説明する。保存サービス・出力サービス・参照保護／allowlistの判定は既存経路を再利用し、UIだけを移動した。確認／エラー状態は`BuildProjectStatus`へ分離した。

- 修正ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ProjectOutput.cs`, `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.cs`
- 設計更新: `docs/Windows-v1-GUI-Navigation-Plan.md`（GUI-06／MOD-04第一段）
- Player build: `Builds/GuiModularV8/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-192613-926.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-192641-2f4a2f44eeef4ba499c10cfcfc83ad84/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-192715-74b962da7ace4edf8f6f89e1c11fc6af/report.json`、1280x800）
- 未受入: 実マウスでの保存先Explorer、出力対象表示と実manifestの手動照合、DPI別表示、実RadDollV3装着、Unity／VRChat内表示。

# 2026-09-14 GUI-07 / MOD-04 第一段: MCP接続Panelの責務分割

AI接続のUIとWorkbenchの更新ループが同じpartialへ混在していたため、接続欄・開始／停止・説明を`AuthoringWorkbench.McpPanel.cs`へ移した。`AuthoringWorkbench.Mcp.cs`は既存の更新ループとpipe pumpだけを担当する。instance ID、既存の`DispatchMcp`、文書交換時の停止、Undo経路は変更していない。IDコピー、実sidecar接続、再接続の手動受入は後続に残す。

- 修正ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.McpPanel.cs`, `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Mcp.cs`
- 設計更新: `docs/Windows-v1-GUI-Navigation-Plan.md`（GUI-07／MOD-04第一段）
- Player build: `Builds/GuiModularV9/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-192938-420.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-193007-6e347956a9d04c67b735c1ec8ef4b4a8/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-193040-906f6e9b9ca045808aa5ef30b49b7276/report.json`、1280x800）
- 未受入: 実sidecarとの接続、instance IDのコピー操作、DPI別表示、実RadDollV3装着、Unity／VRChat内表示。

# 2026-09-14 GUI-05 / MOD-03 第一段: 作業モードナビゲーション

右側設定の長いスクロールを探す負担を減らすため、`AuthoringWorkbench.WorkModes.cs`に「形状編集」「UV・色」「装着・骨」「確認・出力」のナビゲーションを追加した。クリックすると該当する既存Foldoutを開き、最初の設定へスクロールする。編集段・対象・commandの正本は変更せず、単なる表示／移動操作として実装した。`GraphEditing`の詳細段と`ProjectOutput`にも参照を持たせ、パネルの責務を明示した。

- 修正ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.WorkModes.cs`, `AuthoringWorkbench.GraphEditing.cs`, `AuthoringWorkbench.ProjectOutput.cs`, `AuthoringWorkbench.cs`, `Assets/Resources/Viewer.uss`
- 設計更新: `docs/Windows-v1-GUI-Navigation-Plan.md`（GUI-05／MOD-03第一段の導線を追記）
- Player build: `Builds/GuiModularV10/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-193255-367.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-193325-eb2907742f4c483b820aebd5661fcd97/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-193358-30d7972636b441b0a7534a2b9a810f49/report.json`、1280x800）
- 目視: 同Authoring artifactの`authoring.png`で、対象表示の下に4つの作業モードボタンが表示され、右側設定が継続して読めることを確認。
- 未受入: 実マウスで各モードをクリックしたときの折畳み・スクロール、DPI別表示、実RadDollV3装着、Unity／VRChat内表示。

# 2026-09-14 GUI-01 / MOD-01 第一段のCore回帰

コマンドバーと形状追加のUI変更後にCore回帰を再実行し、**509 passed / 0 failed**を確認した。artifactは`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-e8a55fad2c1e4868a6a4e77aa612af10`。既存のskin・morph・材質・UV0制限・衣装package・保存再開・Undo/Redo・出力契約へ変化はない。これはCoreの自動回帰であり、GUIの実マウス操作・実RadDollV3装着・VRChat内表示の受入とは分けて扱う。

# 2026-09-14 GUI-01 / MOD-01 第一段: 常設コマンドバーと形状追加導線

制作画面のスクロールを探さなくて済むよう、`AuthoringWorkbench.CommandBar.cs`へ責務を切り出し、上部へ「モデルを追加」「基本形状を追加」「保存」「元に戻す」「やり直す」を常設した。現在の制作対象（種別、短いID、保存状態）も同じバーに表示する。各ボタンは既存の`PickModel`、`ScrollTo`、`SaveProject`、共通`Execute`を呼び、GUI専用の別編集経路を作っていない。空project・static model・graph object・Undo/Redoで有効状態を更新する。

- 修正ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.CommandBar.cs`, `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.cs`, `Assets/Resources/Viewer.uss`
- Player build: `Builds/GuiModularV3/NyaForge.exe`（Unity 6000.4.3f1）
- ビルド: **成功**（`Logs/build-player-20260914-190722-659.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-190744-58ad6214435e4e62978c40709f694269/report.json`）
- 目視確認: 同artifactの`authoring.png`で、コマンドバー・制作対象表示・3D viewport・右のスクロール設定を確認した。
- 注意: 実マウスでの新コマンドバーからのファイル選択・形状寸法入力、実RadDollV3、Unity／VRChatは未受入。保存・出力の下部UIとImportPanelの完全抽出も残る。

# 2026-09-14 GUI-04 / MOD-02 第一段の回帰修正

ノード・編集段を初期折り畳みにした第一版では、既存のpointer-based Authoring検証が`recover-faceless-edit`と`graph-create-polygon`を画面外として失敗した。通常編集の入口まで隠さないよう、親の「ノード・編集段（詳細）」は初期展開に戻し、開発者向け「検証用テンプレート」だけを折り畳む構成へ修正した。

- Player build: `Builds/GuiModularV2/NyaForge.exe`（Unity 6000.4.3f1）
- ビルド: **成功**（`Logs/build-player-20260914-190323-809.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-190346-377d67ae6fa942dea4ce8e46721250f9/report.json`）
- 既存のチョーカー／カフ／Mirror／Polygon／面編集等の自動検証を含むAuthoring checksが完走した。実マウスの新しい形状寸法入力と、実RadDollV3への装着・VRChatは別受入として未完了。

# 2026-09-14 GUI-04 / MOD-02 第一段: 基本形状パネルとノード詳細の分離

GUI導線整理の最初の実装として、`AuthoringWorkbench.ShapeCreation.cs`へ基本形状追加パネルを切り出した。リング（チョーカー）／バンド（手首カフ）を種類で選び、mm単位の半径・管の太さ／幅・厚み・分割数を入力して1つの編集可能なgraph objectとして追加できる。生成後は既存の編集段・頂点編集へ進む。`PolygonPrimitives.Choker/Cuff`を再利用し、固定寸法の生成処理をUIへ複製していない。主画面の専用チョーカー／カフボタンは、通常利用では開かない「検証用テンプレート」へ移し、自動検証用のstable nameは保持した。

`AuthoringWorkbench.GraphEditing.cs`では、Plane／空形状／四角面／左右対称と表示・編集段を「ノード・編集段（詳細）」へ折り畳んだ。通常の入口は「基本形状を追加」とし、開発者向けfixtureと製品操作を分離した。既存のgraph-create-* nameを残したため、既存のChoker／Cuff／Mirror／EmptyPolygon検証経路は維持する。

- 修正ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ShapeCreation.cs`, `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.GraphEditing.cs`, `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.cs`
- GUI設計: `docs/Windows-v1-GUI-Navigation-Plan.md`のGUI-04／MOD-02第一段を実装済みへ更新
- Player build: `Builds/GuiModularV1/NyaForge.exe`（Unity 6000.4.3f1）
- ビルド: **成功**（`Logs/build-player-20260914-190039-410.log`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-190107-23cd479f52bf41d685b68bb838e44a6d/report.json`、1280x800、22 checks）
- 未受入: 実マウスでの寸法違い追加・全周表示、実RadDollV3への装着、Unity／VRChat確認。ImportPanelと共通Workbench状態の完全抽出も未完了。

# 2026-09-14 最優先: GUI導線整理とソースの責務分割

ユーザーの制作画面フィードバックを現行ソースと照合し、[GUI導線・モジュール化計画](docs/Windows-v1-GUI-Navigation-Plan.md)を作成した。**調査・設計・タスク化は完了、以下のGUI再構成・モジュール抽出は未着手**。先の「残りは受入中心」を修正し、実操作一周の前提としてGUIそのものの改善を最優先にする。過去のUI修正・自動回帰・実モデル確認の記録は保持する。

確認した原因: Viewer→制作では初回に別の空workspaceを開く。右の長いパネルに開始操作・編集・検証fixture・保存が混在し、対象一覧は名前よりIDを表示する。`プレート追加 ×100`は100個ではなく尺度検証用fixture1個。チョーカー／カフは既存generatorを固定寸法で呼んでいる。`AuthoringWorkbench*.cs`は検証partial込み141ファイルあるが、共有fieldと横断Refreshへ依存している。

## 次に進める実装タスク

- [ ] **GUI-01 / MOD-01**: 上部の常設操作・左の対象一覧・中央3D・右の設定へ配置し、Shellと共通状態／操作の境界を整理する。fixture操作は開発者メニューへ。
- [ ] **GUI-02 / MOD-02**: 制作初回の空状態、Viewerとの状態差、モデル追加の取込パネルを整理・抽出する。元GLB/VRMの選択から名前付き候補を確定し、既存文書と取消時の内容を保持する。
- [ ] **GUI-03 / MOD-03**: 身体・髪・衣装を名前で選べる対象一覧と選択context。名前／表示／ロックの保存・Undo・旧文書を整合させる。
- [ ] **GUI-04 / MOD-02**: 「基本形状を追加」へ平面・リング・バンド等を集約し、寸法入力とチョーカー／カフのプリセットを用意。既存generatorを再利用する。
- [ ] **GUI-05 / MOD-03**: 形状／UV・ペイント／装着・骨／確認の作業モードへ既存パネルを分離。対象と編集段を常時明示する。
- [ ] **GUI-06 / MOD-04**: native保存・再開と用途別出力の導線を分離・抽出。出力対象名と参照除外を確認できるようにする。
- [ ] **GUI-07 / MOD-04**: AI接続パネルを抽出。IDコピー、状態表示、GUI/MCP共通command・Undo・再接続を各変更と同時に接続する。
- [ ] **GUI-08 / MOD-05**: 旧UIの重複撤去、必要な回帰と構成README／quickstart更新。実マウス・DPI・IME・長い名前・読込→編集→保存再開→出力を確認する。

順序は01→02/03→04/05/06。07とMODの抽出は該当GUI改修と同時、08で通し確認する。詳細な変更対象・依存・完了条件はリンク先を正とする。新しいCore、巨大なUI framework、全141ファイルの一括書換えを前提にしない。既存command・import/export・保存形式を活用し、各段階で起動可能な候補を残す。

## 後続の受入

GUI改善後に実RadDollV3で衣装編集→保存再開→Unity適用・更新・削除/Undo→VRChat確認を進める。材質実画面、MCP実接続、長時間・別Windowsの受入も維持する。今回の文書更新でこれらを完了扱いにしない。ソース変更・ビルド・実画面検証は今回行っていない。

# 2026-09-14 AI接続（MCP）パネルの高DPI崩れ修正

AI接続を展開したとき、長い日本語ボタン・instance ID欄・説明文が右側のScrollViewからはみ出して切れる問題を修正した。MCPパネルとTextFieldを親幅へ拘束し、接続操作ボタンを縦幅可変・折返しにし、説明文の折返しを明示した。制作controlsの横スクロールバーは非表示にして縦スクロールだけへ整理した。

- 修正ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Mcp.cs`, `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.cs`, `Assets/Resources/Viewer.uss`
- Player build: `Builds/McpUiV2/NyaForge.exe`（Unity 6000.4.3f1）
- 実ウィンドウ確認: Windows native `@oai/sky`で制作画面→AI接続（MCP）を展開。接続ボタンは2行で幅内、説明文は全行折返し、水平スクロールバーなしを確認した。
- 既存起動画面修正を含むPlayerビルドが成功（`Logs/build-player-20260914-182300-143.log`）。

これはMCPパネルのレイアウト確認であり、異なるWindows DPIでの実マウス・IME操作、外部sidecarとの接続実運用、実RadDollV3の全周fit・貫通、VRChat Build & Test／実機表示の受入とは分けて扱う。
# 2026-09-14 起動画面の空状態レイアウト修正

パック未読込の起動時に、空のパーツ欄・シェイプキー欄・再生欄と床だけが表示されて「崩れて見える」状態を修正した。`ViewerApp.UI.cs`へ案内カード（VRM / GLBの説明、パックを開く、最近のパック）を追加し、パックが準備できるまで通常ビューア本体と再生欄を非表示にする。読込後は従来のビューアへ戻る。`Viewer.uss`へカードの高DPI対応スタイルを追加した。

- 修正ソース: `Assets/Viewer/Runtime/ViewerApp.UI.cs`, `Assets/Viewer/Runtime/ViewerApp.cs`, `Assets/Resources/Viewer.uss`
- Player build: `Builds/StartupUiV1/NyaForge.exe`（Unity 6000.4.3f1）
- 実ウィンドウ確認: Windows native `@oai/sky` で起動時カードを目視確認。空欄のビューア controls は隠れ、案内と2つの操作ボタンが中央に表示された。
- Navigation回帰: `Tools/Test-NyaForgeNavigation.ps1 -BuildName StartupUiV1 -Width 1280 -Height 800` **PASS**（`Artifacts/Navigation-20260914-181852-ae45dff5d4464fa8ba5d53e84a53f54e/report.json`）。パック読込後の既存UI、確認セット・設定パネル、viewport領域を確認した。

これは起動画面と自動navigationの確認であり、異なるWindows DPIでの実マウス操作、実RadDollV3の全周fit・貫通・見た目、VRChat Build & Test／実機表示の受入とは分けて扱う。
# Nya Ekaki 3D — 現在のタスク（2026-09-14 再計画）

## 2026-09-14 NF-V1-15V: WindowsネイティブCUAで実モデル取込

`mcp__node_repl__js`から`@oai/sky`を初期化し、`Builds/BoneSubsetV16/NyaForge.exe`の制作画面を実操作した。`モデルを開く…`でprivate RadDollV3 VRMをWindowsファイル選択から指定し、`候補を確認`→`このGLB / VRMをgraph objectへ取り込む`まで完了。画面statusは`mesh 0 / skin 0 / bone 171 / weight 2990 / morphなし`で、診断として`MATERIALS_NOT_RETAINED`、`EXTENSIONS_PARTIAL`、材質画像の注意1件を表示した。これはネイティブファイル選択と取込経路の実操作証拠であり、見た目の品質、全周fit・貫通、VRChat内表示の手動受入とは分けて扱う。private入力自体は公開ツリーへ追加していない。

## 2026-09-14 NF-V1-15U: Computer Use運用メモの固定

リポジトリ直下に`AGENTS.md`を追加し、Windowsネイティブ操作ではブラウザ用`cua`ではなく`@oai/sky`を初期化して`list_apps`／`list_windows`から対象windowを選ぶ手順を固定した。操作後のstate再取得、ターミナル操作との分離、V16起動入口、public/private境界、Core回帰コマンドも記録した。以後、`apps: []`だけでネイティブ操作不可と判断せず、Windows用経路を先に確認する。

## 2026-09-14 NF-V1-15R: 現行HEAD Core回帰

現行HEAD `f0994a9`で`dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore`を再実行し、**509 passed / 0 failed**を確認した。artifactは`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-ef359b1c35f74f23b4e08fd5cea3bf74`。これはCore経路の回帰証拠であり、実EditorWindowのマウス・IME・DPI・Explorer、実アバター全周fit／貫通／見た目、VRChat Build & Test／実機表示の受入とは分けて扱う。

## 2026-09-14 NF-V1-15S: V16起動・制作画面反復回帰

`Tools/Test-NyaForgeNavigationRepeated.ps1 -BuildName BoneSubsetV16 -Count 2`を実行し、Playerの起動、制作画面遷移、保存状態・再開、viewport確認、終了を**2/2 PASS**した。集約結果は`Artifacts/Navigation-Repeated-BoneSubsetV16-20260914-170330.json`。これは自動fixtureの反復回帰であり、実EditorWindowのマウス・IME・DPI・Explorer操作やVRChat内表示の受入とは分けて扱う。

## 2026-09-14 NF-V1-15T: V16受入証跡の整合性監査

V16の実衣装Player report（93 checks、`passed=true`）、Unity Bridge report（16 checks、`status=passed`）、起動・保存再開反復（`passed=true`）の3証跡が現行作業ツリーに存在することを確認した。対象はそれぞれ`Artifacts/Authoring-20260914-165304-a4fdb279b679471d99fcba5733339b17/report.json`、`Artifacts/BridgeReceiver-20260914-165530-393-cb754fbf3e254af482bb0fecda21ca22/bridge-report.json`、`Artifacts/Navigation-Repeated-BoneSubsetV16-20260914-170330.json`。証跡の存在とPASSは自動検査の結果であり、実EditorWindowのマウス・IME・DPI・Explorer、実アバター全周fit／貫通／見た目、VRChat Build & Test／実機表示の手動受入とは分けて扱う。

## 2026-09-14 NF-V1-15Q: Authoring起動スクリプト

`Tools/Start-NyaForgeAuthoring.ps1`を追加し、既定の`Builds/BoneSubsetV16/NyaForge.exe`を`--authoring true`で起動できるようにした。`-BuildName`で別候補を選べ、`-PrintOnly`でパスだけを検査できる。V16に対する`-PrintOnly`は**PASS**し、最新手動受入表・クイックスタートから同じ入口へ誘導する。入力モデルやprivateデータは扱わず、公開ツリーへコピーしない。

## 2026-09-14 NF-V1-15P: V16 status修正後の実衣装一周

status footerの折返し修正を含む`Builds/BoneSubsetV16/NyaForge.exe`で、private RadDollV3 VRMの全mesh取込、native Save/Open、GLB／VRM1出力、衣装skin package生成、Unity **2022.3.22f1** Bridge受け取りを再実行した。Player **PASS**（`Artifacts/Authoring-20260914-165304-a4fdb279b679471d99fcba5733339b17/report.json`）、Bridge **PASS**（`Artifacts/BridgeReceiver-20260914-165530-393-cb754fbf3e254af482bb0fecda21ca22/bridge-report.json`）。これは自動Player／Bridge回帰であり、実EditorWindowのマウス・IME・DPI・Explorer、実アバター全周fit／貫通／見た目、VRChat Build & Test／実機表示は未受入として残す。

## 2026-09-14 NF-V1-15O: status footerの狭幅折返し

診断statusをprobeだけ一行・hiddenにしていた分岐を廃止し、実EditorWindowと自動probeの双方で折返し・表示するようにした。これにより800×600の長い取込診断も画面内で読める。`Builds/BoneSubsetV16/NyaForge.exe`のprivate RadDollV3 `-ImportOnly` probeは**PASS**（`Artifacts/Authoring-20260914-165134-a0c1b46b609043d4a29373c9c2342aeb/report.json`）。画像`import-only.png`で、statusが2行に折り返され、右側controlsのクイック導線とスクロールを維持することを確認した。Coreは直前の**509 passed / 0 failed**を正とする。実EditorWindowのマウス・IME・DPI・Explorer受入、実アバター全周fit／貫通／見た目、VRChat Build & Test／実機表示は未受入として残す。

## 2026-09-14 NF-V1-15N: V15狭幅取込表示確認

`Builds/BoneSubsetV15/NyaForge.exe`を800×600で起動する`-ImportOnly` probeを実行し、private RadDollV3 VRMの取込、自動Frame、`モデルを開く…`、右側controlsのスクロールを**PASS**した（`Artifacts/Authoring-20260914-164924-2ad2784091a74cbcb89a01d97a13e3e9/report.json`、画像`import-only.png`）。狭幅ではviewportとcontrolsが縦に収まり、下段はスクロールで操作できる。自動probeのstatus footerは診断長のため一行表示だが、実EditorWindowでは折返し・tooltipを使う。実マウス・IME・DPI・Explorerの手動受入は未完了として残す。

## 2026-09-14 NF-V1-15M: クイックスタートのV15対応

`docs/Authoring-Quickstart.md`へ現行候補`Builds/BoneSubsetV15/NyaForge.exe`、制作画面の`モデルを開く…`、取込後の自動Frameを追記し、詳細欄の既存`GLBモデルを取り込む`との関係を明記した。実行ファイルと手動受入表への導線を同じ候補へ揃えた。これは文書更新であり、本番コードの挙動変更はない。実EditorWindowのマウス・IME・DPI・Explorer、実アバター全周fit／貫通／見た目、VRChat Build & Test／実機表示は未受入として残す。

## 2026-09-14 NF-V1-15L: V15起動・終了反復回帰

`Tools/Test-NyaForgeNavigationRepeated.ps1 -BuildName BoneSubsetV15 -Count 2`を実行し、Playerの起動、制作画面遷移、保存状態・再開・viewport確認、終了を2/2 PASSした。集約結果は`Artifacts/Navigation-Repeated-BoneSubsetV15-20260914-164524.json`。これは自動fixtureの反復回帰であり、実EditorWindowのマウス・IME・DPI・Explorer操作やVRChat内表示を完了扱いしない。

## 2026-09-14 NF-V1-15K: V15実衣装・Bridge一周回帰

`Builds/BoneSubsetV15/NyaForge.exe`でprivate RadDollV3 VRMを全mesh取込し、native Save/Open、GLB／VRM1出力、衣装skin package生成、Unity **2022.3.22f1** Bridge受け取りまで再実行した。Player **PASS**（`Artifacts/Authoring-20260914-163956-a954ad2ebd584429a1312af59fa90445/report.json`）、Bridge **PASS**（`Artifacts/BridgeReceiver-20260914-164225-353-2bea67b245064ca0aae2b871ec4bb30c/bridge-report.json`）。衣装packageは同Player artifact内の`imported-accessory-skin-project/exports/clothing-20260914-074202-53164a/skinned-clothing.nyaforge.json`に生成され、skeleton sidecarは2 bonesだった。これは自動Player／Bridge回帰であり、実EditorWindowのマウス・IME・DPI・Explorer、実アバター全周fit／貫通／見た目、VRChat Build & Test／実機表示は未受入として残す。

## 2026-09-14 NF-V1-15J: 現行クイック導線Playerの再ビルド確認
## 2026-09-14 NF-V1-15J: 現行クイック導線Playerの再ビルド確認

`Tools/Build-NyaForge.ps1 -Target Player -BuildName BoneSubsetV15`で、モデル取込クイックボタンを含むWindows Playerを現行ソースから再ビルドした。Unity **6000.4.3f1**のビルドは**成功**（`Logs/build-player-20260914-163746-093.log`、`Builds/BoneSubsetV15/NyaForge.exe`）。private RadDollV3 VRMを使う`-ImportOnly` probeも**PASS**（`Artifacts/Authoring-20260914-163807-cca6bdd0131b4d989a6c671661eb2fbb/report.json`）。生成画像で全身表示、自動Frame、`モデルを開く…`、右側controls、status footerを再確認した。Coreは**509 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-5604fed038db491b99f5afabd283f163`）。実EditorWindowの手動マウス・IME・DPI・Explorer受入、実アバターの全周fit／貫通／見た目、VRChat Build & Test／実機表示は未受入として残す。

## 2026-09-14 NF-V1-15I: モデル取込のクイック導線

右側controlsの「1 制作プロジェクト」直下へ`モデルを開く…`を追加し、押下時にGLB／VRM取込foldoutを展開してWindows Explorer pickerを起動するようにした。既存の詳細候補選択・ファイルパス入力・全mesh取込は維持する。`Builds/BoneSubsetV14/NyaForge.exe`のRadDollV3 `-ImportOnly` probeは**PASS**（`Artifacts/Authoring-20260914-163414-6780f993e81841d4a7c78e0513591ba0/report.json`）。capture画像でクイックボタン、全身Frame、選択点、status footer、スクロールcontrolsの同時表示を確認した。これは自動UI captureであり、実EditorWindowのマウス・IME・DPI受入は未完了として残す。

## 2026-09-14 NF-V1-15H: モデル取込後の自動Frame

GLB／VRM取込後に更新済みprojectionのboundsで`Frame()`を実行するよう、単一mesh・skin・全mesh instanceの3経路を揃えた。従来は空projectのカメラ原点／距離が残り、実モデルのスクリーンショットで上半身が見切れる場合があった。`Builds/BoneSubsetV13/NyaForge.exe`のRadDollV3 `-ImportOnly` probeは**PASS**（`Artifacts/Authoring-20260914-163203-98d80848e51940ca9d9170d1c62e2715/report.json`）。生成画像 `import-only.png`で全身mesh・選択点・右側controls・status footerが同時に確認できる。これは自動captureによる表示証跡で、実EditorWindowのマウス・IME・DPI受入やVRChat内の見た目確認を完了扱いしない。

## 2026-09-14 NF-V1-15G: V12実衣装一周回帰

`Builds/BoneSubsetV12/NyaForge.exe`でprivate RadDollV3 VRMを全mesh取込し、native Save/Open、GLB／VRM1出力、衣装skin package生成、Unity **2022.3.22f1** Bridge受け取りまで再実行した。Player **PASS**（`Artifacts/Authoring-20260914-162657-a70cc1bb101f4a5fa8e96daab0da2a74/report.json`）、Bridge **PASS**（`Artifacts/BridgeReceiver-20260914-162928-637-f529cc4f3f884898bcdf51c5d6fca329/bridge-report.json`）。packageは`Artifacts/Authoring-20260914-162657-a70cc1bb101f4a5fa8e96daab0da2a74/imported-accessory-skin-project/exports/clothing-20260914-072905-0e6443/skinned-clothing.nyaforge.json`に生成された。これは自動Player／Bridge回帰であり、実EditorWindowのマウス・IME・DPI、実アバター全周fit・貫通・見た目、VRChat Build & Test／実機表示は未受入として残す。

## 2026-09-14 NF-V1-15F: VRMコンテナ再構築の一時コピー削減

`VrmExportService`のGLB/VRMコンテナ組み立てを、`MemoryStream`→`ToArray()`ではなく最終サイズのbyte配列へ直接書く方式へ変更した。ヘッダー、JSON padding、BIN bytesは従来と同じで、VRM metadata・readback契約は変更しない。Coreは**509 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-fda9b85f4189406d89dbf25ead091d9e`）。`Builds/BoneSubsetV12/NyaForge.exe`の実RadDollV3 `-VrmExportOnly` probeは**PASS**（`Artifacts/Authoring-20260914-162319-65cb57e8d0c44013bdb354af1b3ccb54/report.json`）。出力専用の外部2秒サンプリングは約40.5秒、working set peak **2,492.0MB**、private bytes peak **3,205.3MB**（`C:/Users/tomoaki/AppData/Local/Temp/nyaforge-vrm-output-memory-d268cab5b5dd4c98b0e79510f8198560.log`）。前回3,145.5MBとの差はOS／GPU状態を含む単一サンプルでは判定できないため、メモリ改善は未確定として扱う。機能回帰は通ったので、次はVRM1経路をさらに推測で変えず、実EditorWindowの手動保存・再開とVRChat Build & Testを受入する。

## 2026-09-14 NF-V1-15E: VRM1出力だけの測定入口

VRM1出力のピークを通常GLB／全mesh経路と分けるため、`Tools/Test-NyaForgeAuthoring.ps1 -VrmExportOnly -ImportModel <path>`／`--authoring-vrm-export-only`を追加した。probeは取込後にnative projectを保存・再開し、VRM1 packageを一度だけ出力してから、検証済みの空projectへ戻す。初回実行でclean continuation pathの未作成を検出して修正し、`Builds/BoneSubsetV11/NyaForge.exe`で実RadDollV3を使ったprobeは**PASS**（`Artifacts/Authoring-20260914-161914-1e5a9d01f19c409e8027a262d1a740a4/report.json`）。出力`model.vrm`は**85,585,656 bytes**、外部2秒サンプリングは約40.5秒、working set peak **2,428.9MB**、private bytes peak **3,145.5MB**（`C:/Users/tomoaki/AppData/Local/Temp/nyaforge-vrm-output-memory-393ceeb7e5b5474393daad5f05bdf1d7.log`）。これはVRM1のpackage・readbackと取込・保存再開を含む単一環境の一時ピークで、通常編集のアイドル予算ではない。VRM1は通常操作から分離した任意出力として扱い、次の最適化は`File.ReadAllBytes`とpackage再構築の一時配列を測定点ごとに削減する。

## 2026-09-14 NF-V1-15D: GLB readbackの共有解析

GLB出力後のreadbackで、mesh／skinごとにGLB JSONとBINを再読込していた経路を、`GlbDocumentReader`で1回だけ解析した不変documentを`GlbSceneInventoryReader`・`GlbImporter`・`GlbSkinImporter`で共有するようにした。出力検証の対象・reader・reportは維持している。Coreは**509 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-8338cd643acf4bd9bc8264af6e1b26a6`）。`Builds/BoneSubsetV9/NyaForge.exe`の実RadDollV3 GLB-output-only probeも**PASS**（`Artifacts/Authoring-20260914-161455-568d9bd86fec4c8f8d6efd6a7d89aa9d/report.json`）。外部2秒サンプリングは約22.3秒、working set peak **1,669.5MB**、private bytes peak **2,156.4MB**（`C:/Users/tomoaki/AppData/Local/Temp/nyaforge-glb-output-memory-56d44c799a684d3aaa5b329fdd4a0229.log`）。直前のBIN長取得修正後の同条件2,238.2MBからprivate bytesは約82MB低下したが、working setはOS状態の影響を受けるため、単一サンプルを一般保証や軽量性合格とは扱わない。次はVRM1のpackage経路と、実EditorWindowの手動保存・再開を別測定する。

## 2026-09-14 NF-V1-15C: GLB出力段階のピーク切り分け

取込だけのピークと、GLBを書き出す段階のピークを分けて確認できるよう、`Tools/Test-NyaForgeAuthoring.ps1 -GlbExportOnly -ImportModel <path>`／`--authoring-glb-output-only`を追加した。`Builds/BoneSubsetV8/NyaForge.exe`でRadDollV3の実VRMを使った出力専用probeは**PASS**（`Artifacts/Authoring-20260914-160515-aab20b7118514c97bbcb254908f724fb/report.json`、画像 `glb-output-only.png`）。外部2秒サンプリングは約22.3秒、working set peak **1,668.1MB**、private bytes peak **2,286.6MB**、ログは`C:/Users/tomoaki/AppData/Local/Temp/nyaforge-glb-output-memory-dd2b510f8f1445fea90edb7cc40ec703.log`。同じ実モデルのImportOnly（約1,081.3／1,527.8MB）との差から、GLB出力経路だけで一時的に約0.6〜0.8GBが増えることを確認した。これは単一環境・短時間サンプリングで、軽量性の合格判定やEditorWindowのアイドル予算ではない。

追加で、`GlbExportService`のBIN長取得時に`binary.ToArray()`を呼ばず、パディング後の長さだけを返すようにした。GLB内容・`buffers.byteLength`は維持し、最大BINの不要な一時コピーを1回減らす狙い。修正後にV8を再buildし、同じ出力専用probeは**PASS**（`Artifacts/Authoring-20260914-160934-9dcdc9c9c8b3411c9890e02dc00ba2f5/report.json`）。外部2秒サンプリングは約22.3秒、working set peak **1,653.7MB**、private bytes peak **2,238.2MB**（`C:/Users/tomoaki/AppData/Local/Temp/nyaforge-glb-output-memory-3f1fddfa27624fd88a1a82396e806f9a.log`）で、修正前の同条件観測1,668.1／2,286.6MBから約14／48MB低下した。ただしOS状態とサンプリング間隔を含む短時間観測なので、一般的な改善保証とは扱わない。VRM1の`File.ReadAllBytes`／package再構築は別の測定点として扱う。実EditorWindowの長時間操作、別Windows環境、VRChat Build & Test／実機表示は未受入。

## 2026-09-14 NF-V1-15B: 大規模morph取込の一時オブジェクト削減

GLBのmorph取込で、各頂点を一度`MorphDelta`オブジェクトへ展開してから辞書へ変換していた経路を、疎な`Dictionary<int, Vec3>`へ直接構築する内部経路へ変更した。公開API、保存形式、表情の頂点値・順序・hash契約は維持する。Coreは**509 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-a2c480428d624c4e8812c217bfdabe0d`）。`Builds/BoneSubsetV7/NyaForge.exe`でRadDollV3実衣装一周とUnity **2022.3.22f1** Bridge受け取りもPASS（Player `Artifacts/Authoring-20260914-155811-a5784b1b066d4d14958dba0b50d843ba/report.json`、Bridge `Artifacts/BridgeReceiver-20260914-160042-912-f7f11707dd1f4b529a62bc3500e36b83/bridge-report.json`）。

同じ`ImportOnly`条件の外部2秒サンプリングは約18.3秒、working set peak **1,081.3MB**、private bytes peak **1,527.8MB**（`C:/Users/tomoaki/AppData/Local/Temp/nyaforge-import-only-memory-9d8c56f67f244d7d84581d5f95061421.log`）で、前回の約1,140.9MB／1,587.4MBから約60MB減少した。一方、V7の通常一周（VRM1確認なし）は約83秒、working set peak **2,220.9MB**、private bytes peak **2,747.2MB**（`C:/Users/tomoaki/AppData/Local/Temp/nyaforge-no-vrm-memory-edb73637abbd4b90ad834d67fc5e1507.log`）で、OS状態と検証順序の揺れもあり、通常一周全体の改善量はまだ確定していない。OS・GPU・サンプリング間隔を含む観測であり、最終的な軽量性合格とは扱わない。通常編集の手動受入、出力段階の一時データ、全mesh一括、別Windows環境、VRChat Build & Test／実機表示は継続課題とする。

## 2026-09-14 NF-V1-15A: 通常候補と全meshの軽量性を分離

`Builds/BoneSubsetV5/NyaForge.exe`でprivateのRadDollV3 VRM候補を通常の単一候補経路へ渡し、candidate確認・EditMesh・native Save/Open・skinned GLB／static GLB／VRM1出力までを含むAuthoring一周を再実行した。Player reportは**PASS**（`Artifacts/Authoring-20260914-153223-c5892e948e6a4b9b9dcebe0c7eea25ce/report.json`）。外部2秒サンプリングは約107.3秒、working set peak **2,730.7MB**、private bytes peak **3,399.6MB**（サンプル51回、`C:/Users/tomoaki/AppData/Local/Temp/nyaforge-single-memory-4fdf5fa76d1a4411a1c59533788ee7df.log`）だった。

同じ候補の全mesh取込計測（NF-V1-15の既存記録）はworking set **3,667MB**、private bytes **4,492.3MB**。今回、`Tools/Test-NyaForgeAuthoring.ps1 -ImportOnly`／`--authoring-import-only`を追加し、候補取込直後だけを分離したところ、Player reportは**PASS**（`Artifacts/Authoring-20260914-154339-3f4b7f23b58b46d19b0634048e95c9f2/report.json`）、外部2秒サンプリングは約16.2秒、working set peak **1,140.9MB**、private bytes peak **1,587.4MB**（`C:/Users/tomoaki/AppData/Local/Temp/nyaforge-import-only-memory-5f351f4bcfb74787b9f17f3483d469b8.log`）だった。VRM1出力を除いた通常一周は約85秒、working set peak **2,160.7MB**、private bytes peak **2,689.4MB**（`C:/Users/tomoaki/AppData/Local/Temp/nyaforge-no-vrm-memory-22b4c12e55624ee1960fe0f44f4f90c7.log`）で、VRM1出力を含む通常一周は**2,730.7MB／3,399.6MB**、全mesh一括は**3,667MB／4,492.3MB**だった。現時点では、取込直後よりも出力・再読込、特にVRM1確認がピークを押し上げると切り分けられた。V6の実RadDollV3衣装一周とUnity 2022.3.22f1 Bridge受け取りもPASS（`Artifacts/Authoring-20260914-154437-6e785faf1e0747eab042c3beecb7772f/report.json`、`Artifacts/BridgeReceiver-20260914-154710-995-7a00b323235b4cf49bf6fbff232ac809/bridge-report.json`）。現段階の出荷方針は、**通常の1メッシュ編集を主経路として軽量化・手動受入を続け、全mesh一括とVRM1検証は明示操作の任意機能として隔離する**こと。次はVRM1出力の一時データ解放と、実EditorWindowの長時間操作を確認する。実EditorWindowの長時間操作、別Windows環境、VRChat Build & Test／実機表示は未受入のまま残す。

## 2026-09-14 NF-V1-10S: 現行PerformanceV39の実衣装一周再確認

privateのRadDollV3 VRM候補を現行`PerformanceV39`へ渡し、全mesh取込、EditMesh、native Save/Open、GLB／VRM1、選択衣装skin package生成、Unity **2022.3.22f1** Bridge受け取りを再実行した。Player **93 checks PASS**（`Artifacts/Authoring-20260914-113055-0cd70cb457dd42d0be9f9af85e84639e/report.json`）、Bridge **16 checks PASS**（`Artifacts/BridgeReceiver-20260914-113333-758-cf279d4ec1eb4efdb11c5d624dabd6ed/bridge-report.json`）。衣装packageは`Artifacts/Authoring-20260914-113055-0cd70cb457dd42d0be9f9af85e84639e/imported-accessory-skin-project/exports/clothing-20260914-023310-da7073/skinned-clothing.nyaforge.json`に生成され、reportの`passed=true`／`status=passed`とpackage存在を再確認した。画面証跡も目視し、1600×1000のviewport・右panel・status footer・スクロール可能なcontrolsを確認した。

これは実ファイルの自動受け渡しと画面証跡であり、実EditorWindowのマウス／IME／DPI操作、実アバター全周fit・貫通・見た目、VRChat Build & Test／実機表示を完了扱いしない。

## 2026-09-14 NF-V1-10R: f010146フィードバックの現行HEAD再照合

提示された`f010146`基準のP1 3件／P2 5件を現行`main` **`5f90cb9`**へ再照合した。MCP batchの参照保護、単一object汎用出力、Polygon→skinの疎material slot、Unity BridgeのMR係数、Paint原画像のnode identity、allowlist 1対象、高DPI bounds、fit対象のobject境界はいずれも後続実装と回帰で解消済みで、同じ本番コードの重複修正は行っていない。詳細は[フィードバック再照合](docs/reviews/2026-09-14-Feedback-f010146-Recheck-5f90cb9.md)へ固定した。

現行HEADのCoreは**506 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f886ca565d5b4d198da6efd94a0f4e76`）。実RadDollV3のPlayer取込→編集→Save/Open→GLB／VRM1→衣装package、Unity 2022.3.22f1 Bridgeの衣装受取とsemantic normal／MRも既存のprivate成果物で確認済み。実EditorWindowのマウス／IME／DPI、実アバター全周fit・貫通・見た目、VRChat内表示は未受入として残す。

## 2026-09-14 NF-V1-10Q: V39実RadDollV3衣装package一周

privateのRadDollV3 VRM候補を`PerformanceV39`へ渡し、全mesh取込、EditMesh、native Save/Open、GLB／VRM出力、衣装package生成、Unity **2022.3.22f1** Bridge適用まで実行した。Authoring **93 checks PASS**（`Artifacts/Authoring-20260914-111811-e062516475844dda86104aef8211faf1/report.json`）、Bridge **16 checks PASS**（`Artifacts/BridgeReceiver-20260914-112041-541-fa47513ae1124c099583c27acb0c6530/bridge-report.json`）。packageは同Authoring artifact内の`imported-accessory-skin-project/exports/clothing-20260914-022018-bbcb57/skinned-clothing.nyaforge.json`へ生成された。

これは実ファイルの自動受け渡し・保存再開・receiver smokeであり、実EditorWindowの手動fit、全周の貫通ゼロ・見た目、VRChat Build & Test／実機表示、長時間編集を完了扱いしない。

## 2026-09-14 NF-V1-10P: 現行HEADテストスイープ

現行`main`のCore実行を再確認し、**506 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0b8c393ba49c4ce0bb3704fc653b7d63`）だった。MCP transport suiteもnamed-pipe／stdio／capture metadataの**PASS**、WindowsPicker suiteもnative cancelと日本語・空白・アポストロフィ pathの**2/2 PASS**を確認した。これは.NET／Windows APIの自動検証であり、実EditorWindowの長時間・実VRChat受入を満たすものではない。

## 2026-09-14 NF-V1-10O: Unity Bridgeクラッシュ復旧回帰

現行`PerformanceV39` Authoring出力をUnity **2022.3.22f1** Bridgeへ渡し、更新中断後の再起動復旧をmaterials／prefab／receiptの3フェーズで実行した。**3/3 PASS**。証跡は`Artifacts/BridgeReceiver-20260914-111256-925-9234a6641f3d4f29bbd894aea206d112`内の`materials-recovery.json`、`prefab-recovery.json`、`receipt-recovery.json`。これは受け取り側のcheckpoint復旧回帰であり、実VRChat Build & Testや実EditorWindowの手動受入を満たすものではない。

## 2026-09-14 NF-V1-10N: Windows native file picker回帰

`Tests/WindowsPicker/WindowsPicker.Tests.csproj`へ、Viewer wrapperが参照するNyaForge側のnative picker実装を明示的に含めた。現行Windowsでnative dialogのキャンセルと、日本語・空白・アポストロフィを含むファイル選択を実行し、**2/2 PASS**した。これはWindows API／パス受け渡しの自動回帰であり、実EditorWindow内の実マウス・IME・高DPI操作を完了扱いしない。

## 2026-09-14 NF-V1-10M: 50回反復起動・終了回帰

`PerformanceV39`のWindows Playerを新規プロセスで50回起動し、標準fixtureのnavigation／Save As・保存状態・再開・viewport確認を各回で実行した。**50/50 PASS**。集約結果は`Artifacts/Navigation-Repeated-PerformanceV39-official-50.json`。再現用の`Tools/Test-NyaForgeNavigationRepeated.ps1`を追加し、`-Count 2`の自己検証もPASSした（`Artifacts/Navigation-Repeated-PerformanceV39-smoke.json`）。

これは自動fixtureの反復起動・終了回帰であり、2時間編集、IME／高DPIの実マウス操作、別Windows環境、全周fit／貫通、VRChat実機受入を満たすものではない。

## 2026-09-14 NF-V1-10L: V39反復起動・終了回帰

`PerformanceV39`のWindows Playerを新規プロセスで10回起動し、標準fixtureのnavigation／Save As・保存状態・再開・viewport確認を各回で実行した。**10/10 PASS**。集約結果は`Artifacts/Navigation-Repeated-PerformanceV39.json`、個別reportは同フォルダ内の`Navigation-20260914-105951-38aec7edbaa54724ab74f40754ddaaae`以降へ保存した。

これは反復起動での早期回帰検出であり、最終計画の50回Open/Close、2時間編集、IME／高DPIの実マウス操作、別Windows環境、VRChat実機受入を満たすものではない。

## 2026-09-14 NF-V1-10K: skinned GLBのJSON clone削減

`GlbSkinImporter`とstandalone source-skin importerが、skin属性を除くためscene全体のJSONをcloneしていた。`GlbImporter.ReadDocumentWithoutSkin`を追加し、skin属性を読み飛ばして同じ`GlbDocument`を直接geometry adapterへ渡すようにした。source-skinの公開standalone API、skin属性の拒否境界、source hash／topology検証は維持する。変更は`b089516`。

Coreは**506 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-c90656d376a84674ae330e925abfac94`）。Unity `6000.4.3f1`のV39 Player build（`Logs/build-player-20260914-105352-527.log`）、private RadDollV3候補のAuthoring suite（`Artifacts/Authoring-20260914-105632-c29fb14ef55248f59fb7031eacb10b89/report.json`）、Unity `2022.3.22f1` Bridge（`Artifacts/BridgeReceiver-20260914-105609-762-5a5c6609661c42bbb0c8e34e2f8d3b00/bridge-report.json`）はいずれも**PASS**。同条件の外部10秒サンプリングは約110.3秒、working set peak **2,470.5MB**、private bytes peak **3,075.5MB**（`Artifacts/Authoring-Memory-20260914-105632/memory-measurement.json`）で、V37〜V39間の削減量は確定できない。実EditorWindow操作、全周fit／貫通・見た目、VRChat Build & Test／実機表示、別Windows環境は未受入として残す。

## 2026-09-14 NF-V1-10J: mesh再利用後の実候補メモリ再計測

`PerformanceV38`で同じprivate RadDollV3候補1体のAuthoring suiteを外部10秒サンプリングした。Player reportは**PASS**（`Artifacts/Authoring-20260914-105010-5af979e784ee4540bd0821c031b992e1/report.json`）。実行約110.3秒、working set peak **2,444.9MB**、private bytes peak **3,080.3MB**（`Artifacts/Authoring-Memory-20260914-105010/memory-measurement.json`）だった。

V37の同形式計測（working set 2,377.9MB／private 3,063.6MB）と差が小さく、今回のmesh再利用だけで削減量を確定できない。OS状態とsuite内の一時保持が影響するため、実アバター軽量性の合格にはせず、同一条件のheap censusと全mesh／候補取込の分離を残課題とする。

## 2026-09-14 NF-V1-10I: source-skin取込のmesh再利用

skinned取込後にsource-skinを復元する際、同じGLB meshを静的rootへ再構築していた。`GlbSourceSkinImporter.ReadDataFromDocument`を追加し、Workbenchは既にデコード済みのdisplay meshへsource-spaceのnode frame・inverse-bind・weightだけを結び付けるようにした。公開`Read` APIのstandalone mesh結果とsource hash／topology検証は維持する。変更は`14547f0`。

Coreは**506 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-ac41363e3e0c41728afe928dd505f6b1`）。Unity `6000.4.3f1`のV38 Player build（`Logs/build-player-20260914-104618-659.log`）とprivate RadDollV3候補のAuthoring suiteも**PASS**（`Artifacts/Authoring-20260914-104641-a5d1513fa5e2484fa4d6143cd7f8e167/report.json`）。Unity `2022.3.22f1` Bridge受け取りも**PASS**（`Artifacts/BridgeReceiver-20260914-104842-320-b4c07288801d4f50ae982b048a01b452/bridge-report.json`）。実アバターの同一条件heap比較、実EditorWindow操作、全周fit／貫通・見た目、VRChat実機は未受入として残す。

## 2026-09-14 NF-V1-10H: PerformanceV37実RadDollV3候補の再計測

最新の`Builds/PerformanceV37/NyaForge.exe`へprivateのRadDollV3 VRMを候補1体として渡し、通常のAuthoring suite（candidate選択、EditMesh、native Save/Open、skinned GLB／VRM1出力）を再実行した。Player reportは`passed: true`（`Artifacts/Authoring-20260914-104031-e91e8d0d50d3492a9c88bc42d5331880/report.json`）。外部10秒サンプリングでは実行約110.3秒、working set peak **2,377.9MB**、private bytes peak **3,063.6MB**（`Artifacts/Authoring-Memory-20260914-104031/memory-measurement.json`）だった。

同一候補の過去観測（working set約2,795MB、約260.8秒）より小さい値だが、OS状態・suite内容・サンプリング時点の差を含むため、最適化量の確定比較とは扱わない。全mesh取込、実EditorWindowのマウス／IME／Explorer、実アバター全周fit・貫通ゼロ・見た目、VRChat Build & Test／実機表示は別受入として残す。

## 2026-09-14 NF-V1-10G: f010146フィードバックの現行HEAD再照合

提示された`f010146`基準のP1 3件／P2 5件を現行`main` **`f3b3d29`**へ再照合した。MCP参照保護、単一object汎用出力、Polygon→skinの疎material slot、Unity BridgeのMR係数、Paint原画像のnode identity、allowlist 1対象、高DPI bounds、fit対象のobject境界はいずれも後続実装と回帰で解消済みで、重複修正は行っていない。詳細は[フィードバック再照合](docs/reviews/2026-09-14-Feedback-f010146-Recheck-f3b3d29.md)へ固定した。

Coreは**506 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-adbcb4a7267c48eda030ca0e8ccf6eb4`）。Unity `2022.3.22f1` Bridgeも**PASS**（`Artifacts/BridgeReceiver-20260914-103539-671-7874a6c7ed7948e0a48a1ba6fca0536b/bridge-report.json`）。残る受入境界は実EditorWindowのマウス／IME／Explorer、実アバター全周fit・貫通ゼロ・見た目、VRChat Build & Test／実機表示、同一条件の実アバターheap censusである。

## 2026-09-14 NF-V1-10F: imported material encoded bytes共有

同一GLB imageを複数materialが参照する場合に、base-colorの原画像、normal、metallic-roughnessのencoded bytesをmaterialごとに複製していた。公開の `Copy...Bytes()` は防御コピーを維持し、取込内部だけ不変bytesを共有する生成経路を追加した。PaintNodeId、sampler、semantic、原画像の個別node identityは変えない。変更は `c0127d6` に固定した。

Coreは**506 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-d237686a8eb44c19b37839cdd36cc834`）。Unity `6000.4.3f1`のV37 Player build、private RadDollV3候補のSave/Open・VRM1出力もPASS（`Artifacts/Authoring-20260914-102926-0a3520f915df4f3f9de89f75211db3fe/report.json`）。bytes共有による実メモリ削減量は同一条件のheap censusでは未計測で、軽量性の合格とは扱わない。

## 2026-09-14 NF-V1-10E: 取込base-colorの作業画像デコード共有

同一GLB imageを複数materialが参照する場合、Workbenchが同じbase-colorをmaterialごとにUnity `Texture2D`へデコードし、1024px PaintImageとpreview hashも重複生成していた。1回の取込操作に限った `BaseColorImageIndex` cacheを追加し、解像度・MIME・preview hashを含む不変作業画像を全objectで共有する。PaintNodeId、materialごとのOriginalImage node、原画像bytesの防御コピーは従来どおり個別に保持する。変更は `10b597d` に固定した。

Coreは**506 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-c1156bd0e3ae447a97c9b7a7ab0fed3d`）。Unity `6000.4.3f1`のV36 Player build、private RadDollV3候補のSave/Open・VRM1出力もPASS（`Artifacts/Authoring-20260914-102351-8397aec61c644137ae1ae3f8652f7510/report.json`）。作業画像の共有による実メモリ削減量は同一条件のheap censusでは未計測であり、実候補取込の軽量性は継続課題として扱う。

## 2026-09-14 NF-V1-10D: GLB parsed documentのimport間共有

候補選択・全mesh instance取込では、同じGLBをinventory／mesh／skin／source-skinの各入口で再パースしていた。`GlbDocument`を一度生成し、Workbenchの1回の取込操作内でinventoryと各Importerへ共有する内部経路を追加した。JSONの再解析とモデルサイズBINの重複保持を避けつつ、各派生rootは従来どおり個別にcloneする。変更は `3d55e23` に固定した。

Coreは**506 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f4119b0bf02947908493b7970ecff22a`）。Unity `6000.4.3f1`のV35 Player build、合成Authoring suite、private RadDollV3候補のSave/Open・VRM1出力もPASS（`Artifacts/Authoring-20260914-101619-5fc716ddb52549999a816203c6cbf569/report.json`）。メモリ削減の効果量は同一条件のheap censusでは未計測であり、実候補取込の軽量性は引き続き未達課題として扱う。

## 2026-09-14 NF-V1-10C: GLB解析中BINの重複コピー削減

GLB読込時、skin importerがskin属性を除いた静的rootを評価する際に、モデルサイズのBINを複製していた。`GlbDocument`へ解析中BINを共有できる生成経路を追加し、`GlbDocumentReader`とskin／source-skinの静的評価で同一不変BINを再利用する。既存の通常生成子は防御コピーを維持する。変更は `66a45b3` に固定した。

Coreは**506 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-d8a1d2c201de4794b3dee22da8185dda`）。Unity `6000.4.3f1`のV34 Player build、合成Authoring suite、private RadDollV3候補のSave/Open・VRM1出力もPASS（実候補report `Artifacts/Authoring-20260914-100805-568c43d580de4d4abf731793a7dd7843/report.json`）。これは一時BIN保持を減らす実装と経路回帰の証拠であり、候補取込の軽量性を合格とするものではない。実アバター全周fit／見た目、実EditorWindow操作、VRChat実機は引き続き未受入である。

## 2026-09-14 NF-V1-10B: f010146フィードバックの現行HEAD再照合

提示された `f010146` 基準のP1 3件／P2 5件を、現行 `main` **`403078b`**へ再照合した。MCP batchの参照保護、単一object汎用出力の保護検査、疎なmaterial slot、Unity Bridge MR係数、Paint原画像のnode identity、納品対象allowlist、高DPI bounds、fit対象のobject境界はいずれも後続実装と回帰で解消済みで、本番コードの重複修正は行っていない。詳細は[フィードバック再照合](docs/reviews/2026-09-14-Feedback-f010146-Recheck-403078b.md)へ固定した。

Coreは**506 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-1b2721932b4e4843bac3ce68ab912291`）。V33 PlayerのAuthoring／実RadDollV3候補Save/Open・VRM1出力、Unity 2022.3.22f1 Bridgeのsemantic texture／clothing package受け取りもPASSを維持する。残る受入境界は実EditorWindowのマウス／IME／Explorer、実アバター全周fit・貫通・見た目、VRChat Build & Test／実機表示であり、実候補取込の約2.8GB観測working setと約260.8秒も軽量化課題として残す。

## 2026-09-14 NF-V1-10A: GLB画像payload共有と再計測

同一glTF imageを複数submeshが参照する場合、取込時に材質ごとのencoded bytesを複製しないよう、`GlbMaterialSourceReader`へimage index単位のpayload cacheを追加した。`CopyBaseColorImageBytes`／`CopyImageBytes`は従来どおり防御コピーを返すため、公開境界と保存内容は変わらない。Coreは**506 passed / 0 failed**、V33 Player buildとAuthoring、Unity 2022.3.22f1 Bridgeを再確認した。

private RadDollV3 VRMを候補1体だけ取り込む実行は**PASS**（`Artifacts/Authoring-20260914-095159-7b4c491446fb4a24816b410612d16466/report.json`）。外部10秒サンプリングの観測値は`sampledWorkingSetPeakMB=2795`、終了時2393MB、約260.8秒（`Artifacts/Authoring-20260914-095159-7b4c491446fb4a24816b410612d16466/memory-measurement.json`）。これは同一画像共有の効果を断定するheap計測ではなく、全mesh約3.6GB・通常fixture foreground約577MBとも条件が異なる。実アバター取込の軽量性は引き続き改善対象とする。

## 2026-09-14 NF-V1-09Z: 実VRM候補1体の軽量性計測

privateのRadDollV3 VRMを全mesh展開なしで`Builds/PerformanceV32/NyaForge.exe`へ渡し、通常の候補選択・編集・Save/Open・VRM1出力を含むAuthoring suiteを**PASS**（`Artifacts/Authoring-20260914-094416-ef09767da6c846e4977c622e1a4bd85b/report.json`）。実行時間は約261秒、取込中のプロセスworking setピークは約2.6GBだった。全mesh一括の約3.6GBより低いが、通常fixtureのforeground約577MBとは条件が違うため、実アバター取込の軽量性は未達として扱う。今後は画像デコードの解放、取込中の一時メッシュ保持、実アバター候補選択だけの計測を分離して改善する。

## 2026-09-14 NF-V1-09Y: V32実RadDollV3衣装受け取り一周

privateの実RadDollV3 VRMを`Builds/PerformanceV32/NyaForge.exe`へ渡し、候補選択・全mesh instance取込・EditMesh頂点編集・native Save/Open・標準skinned GLB／VRM1出力・衣装package生成を完了した。Player reportは**PASS**（`Artifacts/Authoring-20260914-093718-1b9d2bfe1e5342c6a06903bac1a19a5e/report.json`）。生成した衣装packageをUnity **2022.3.22f1** Bridgeへ渡し、BoneId map、avatar rootのtranslation／rotation／scale、StateHash更新、削除Undo、複数package所有、semantic normal／MRを含む受け取りを**PASS**（`Artifacts/BridgeReceiver-20260914-094245-655-c52b91eeb00f41db81d0c4b31a63f72b/bridge-report.json`）。

これは実ファイルを使った機械的な一周とBridge受け取りの証拠で、実EditorWindowのマウス／IME／Explorer操作、実アバター全周fitの貫通ゼロ・見た目、VRChat SDKのBuild & Test／実機表示を完了したことにはしない。全mesh取込時の一時メモリは約3.6GBだったため、軽量性の評価は通常1候補編集と分けて扱う。

## 2026-09-14 NF-V1-09X: 9855d43衣装・材質フィードバックの実装反映

MCP batchの参照保護、generic single-object出力の保護漏れ、sparse material slot、Unity Bridge MR係数、Paint originalのnode identity、allowlist 1対象、高DPI bounds、fit対象のobject境界を実装した。詳細と未受入境界は[実装反映確認](docs/reviews/2026-09-14-Feedback-9855d43-Recheck-current.md)へ固定した。

Coreは**506 passed / 0 failed**。Unity PlayerはV32までビルド成功し、BridgeはUnity 2022.3.22f1でsemantic textureとclothing packageをPASS。V32 Authoring suiteも**PASS**（`Artifacts/Authoring-20260914-093140-fec1bbea16b44bd0808b154884ce3989/report.json`）。multi-object検証内のMCP batch回帰で、衣装から保護アバターへ切り替える編集を拒否し、geometry hash不変を確認した。

## 2026-09-14 NF-V1-09W: 9855d43衣装・材質フィードバックの現行HEAD再照合

提示された`9855d43`基準のP1 3件／P2 5件を、現行HEAD `734ecb4`へ再照合した。avatar移動後の座標、衣装更新時のownership、UV1の明示拒否、sparse material slot、MR係数、Cuff winding、sampler variant、適用前・削除後の割当読込はいずれも後続実装と回帰で解消済みで、本番コードの重複修正は行っていない。詳細は[現行HEAD再照合](docs/reviews/2026-09-14-Feedback-9855d43-Recheck-734ecb4.md)へ固定した。

Coreは**506 passed / 0 failed**。private Unity 2022.3.22f1のRadDollV3 probe、`Builds/PerformanceV30/NyaForge.exe`のAuthoring／navigation／foreground性能回帰、実RadDollV3単一候補のSave/Open・GLB／VRM1 smokeもPASSを維持している。残る受入境界は実EditorWindowのマウス／IME／Explorer、実アバター衣装の全周fit・貫通・見た目、VRChat Build & Test／実機表示である。

## 2026-09-14 NF-V1-09V: Cuff法線向きのCore回帰追加

報告されていたCuffの面向き逆転を再発させないため、各面の幾何面法線と指定corner normalの内積が正になる回帰を追加した。**Core 506 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-d95cc0756d7f4197b67fd9d3ac7f0e7e`）。既存の閉シェル・頂点属性・片面材質向けwinding契約を維持している。

これは幾何データの向き確認であり、Unity／VRChatの実照明、裏面表示、実EditorWindowでの見た目を自動的に受入したものではない。

## 2026-09-14 NF-V1-09U: 実RadDollV3単一候補の通常Authoring回帰

全mesh展開オプションと通常経路を分けるため、private一時 `RadDollV3_VRM.vrm` を単一候補として `Builds/PerformanceV30/NyaForge.exe`へ渡し、1280×800でAuthoring suite＋VRM1出力を再実行した。**PASS**（`Artifacts/Authoring-20260914-085304-69897268cb7e4f4e95514ad6eaf8c644/report.json`）。実モデルのcandidate選択、EditMesh、頂点編集、native Save/Open、skinned GLB再読込、VRM1 metadata／geometry再読込、semantic texture・衣装関連の既存回帰を確認した。

実行中のプロセス観測はprivate bytes約3.6GB、working set約2.9GBまで上がった。これは一時的な実VRM取込＋同一suiteの全回帰を含む単一環境の値で、軽量性の合否ではない。通常の1候補取込と全mesh一括取込をNF-V1-15の計測で分離し、実EditorWindowの長時間編集・別PCは未受入として残す。

## 2026-09-14 NF-V1-09T: PerformanceV30標準作業解像度Authoring回帰

性能修正後の候補 `Builds/PerformanceV30/NyaForge.exe` を1600×1000で単独起動し、制作UIの一周を再確認した。**PASS**（`Artifacts/Authoring-20260914-085112-29a398ddae9e48ae9eae2f21cb46aa34/report.json`、画面`authoring.png`）。制作対象の参照保護・納品対象・小物装着・Polygon編集入口、頂点表示、保存・出力一致の既存回帰が、標準作業解像度でも完了した。画面を目視し、右側の長いpanelはスクロール可能で、status footerとviewportを確認できた。

自動suiteと生成画面の確認であり、実マウスによる頂点ドラッグ、IME／Explorer、実RadDollV3衣装の全周fit・貫通・見た目、VRChat Build & Test／実機表示は未受入として残る。

## 2026-09-14 NF-V1-09S: Unity Bridge更新クラッシュ後の復旧確認

実衣装packageを含む直近Authoring成果物からUnity 2022.3.22f1のBridge receiverを作成し、materials／prefab／receipt更新の途中でプロセスを停止した後、別Unityプロセスで復旧するスイートを再実行した。**3経路すべてPASS**（receiver `Artifacts/BridgeReceiver-20260914-084825-088-5f7612902ab44ba68df4bdb8bc3edb8f`、`materials-recovery.json`／`prefab-recovery.json`／`receipt-recovery.json`）。途中checkpointのPID・phase一致、復旧後のbytes／meta復元、再試行成功を確認した。

これはBridgeのowned asset更新を対象とする自動クラッシュ復旧であり、Nya Ekaki 3D本体の実EditorWindow強制終了、実VRChat Build & Test、別Windows環境での復旧を証明しない。private SDK・アバター素材はpublicへ追加していない。

## 2026-09-14 NF-V1-09R: 現行候補Playerの通常起動・ナビゲーション再確認

現行候補 `Builds/PerformanceV30/NyaForge.exe` を標準fixture付きで通常起動し、1280×800のWindows Playerナビゲーション回帰を再実行した。**PASS**（`Artifacts/Navigation-20260914-084753-296013c54cbd4e36a6eed014577dbea7/report.json`）。pack選択、キャンセル保持、名前付きsession保存、dirty切替、壊れたpath保持、fresh packの既定表示、saved session復元、utility panelの折りたたみ、authoring入口、viewport領域を確認した。画像証跡は同ディレクトリの`main.png`／`sets.png`／`settings.png`。

これは自動ナビゲーション回帰であり、実マウス・IME・Explorerの手動操作、実RadDollV3衣装の全周fit／貫通・見た目、VRChat Build & Test／実機表示は未受入として残る。

## 2026-09-14 NF-V1-09Q: 9855d43衣装・材質フィードバック再照合（現行HEAD）

提示された`9855d43`基準のP1 3件／P2 5件を、現行HEAD `f010146`へ再照合した。avatar移動後の座標、衣装更新時のownership、UV1の明示拒否、sparse material slot、MR係数、Cuff winding、sampler variant、適用前・削除後の割当読込はいずれも後続実装と回帰で解消済みで、本番コードの重複修正は行っていない。詳細は[現行HEAD再照合](docs/reviews/2026-09-14-Feedback-9855d43-Recheck-f010146.md)へ固定した。

再確認としてCore **505 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-bb8fe25bca8746b6b5b2529ec239e427`）、private Unity 2022.3.22f1のRadDollV3 probe **passed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealAvatar-e341c4481e0c4480bde9a31c8706573e/report.json`）、V30 Windows Player Authoring suite **PASS**（`Artifacts/Authoring-20260914-083803-8d33406704a64fa58853d2c5220bdfb6/report.json`）を確認した。実EditorWindowのマウス・IME・Explorer、全周fit・貫通ゼロ・見た目、VRChat Build & Test／実機表示は未受入のまま別工程とする。

## 2026-09-14 NF-V1-09P: V30性能修正後のAuthoring回帰

`pose-arms-up`を持たないfixtureでも性能計測できるようにしたV30 Player `Builds/PerformanceV30/NyaForge.exe`で、Authoring suiteを1080×700で再実行した。**PASS**（`Artifacts/Authoring-20260914-083803-8d33406704a64fa58853d2c5220bdfb6/report.json`、`authoring.png`）。性能計測の修正が制作UI・保存・出力回帰へ影響していないことを確認した。

## 2026-09-14 NF-V1-09O: V30通常Viewerのforeground性能基準

`Builds/PerformanceV30/NyaForge.exe`へ標準fixture `GeneratedPacks/NyaForgeFixture/current.StandaloneWindows64.json`を明示し、通常ウィンドウを前面化して性能計測を実行した。`pose-arms-up`がないfixtureでは、今回追加した選択規則により`pose-rest`を計測対象とし、レポートへ`performanceClipId`を保存する。

- レポート: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-PerformanceV30-Foreground-e8eaeb64df894b969e80fee4a092b347/performance.json`
- 計測完了: **true**。foreground／stopped-idleとも有効
- 60秒再生: **60.0016 FPS、P95 16.8724 ms、最大 17.7144 ms**
- Private bytes: 再生開始約577MB、終了約563MB。working set peak約335MB
- Unity 6000.4.3f1、Direct3D11、RTX 4090、Windows 11、1280×800、DPI 150%、AC電源

これは標準fixture・単一PCでの一回の基準値で、10回起動・20回更新、実RadDollV3通常編集、別GPU、VRChatの性能保証ではない。全mesh取込時の一時約4.6GB観測はNF-V1-09Nへ分けて記録する。

## 2026-09-14 NF-V1-09N: V29実RadDollV3一周とBridge受け取り

private一時VRM `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`を公開ツリーへコピーせず、`Builds/ReleaseCandidateV29/NyaForge.exe`へ直接渡した。全mesh instance取込、EditMesh、native Save/Open、skinned GLB／VRM1出力、衣装package生成を含む **93 checks PASS**（`Artifacts/Authoring-20260914-082145-4f42a375b1224914af0782043c15f279/report.json`）。生成packageをUnity **2022.3.22f1**のBridgeへ渡し、**16 checks PASS**（`Artifacts/BridgeReceiver-20260914-082703-724-7cf712d873094c299d5e03ef390fb8b5/bridge-report.json`）。

全mesh取込中はプロセスが応答状態を維持し、一時的に約4.6GBのPrivate Memoryを観測した。今回の値はRadDollV3の画像・全meshを一括展開した単一RTX 4090環境の観測で、通常編集時や別GPUの性能保証ではない。NF-V1-15の軽量性計測では、全mesh取込と通常編集を分けた計測条件にする。実EditorWindowのマウス・IME・Explorer、衣装全周の貫通ゼロ・見た目、VRChat Build & Test／実機表示は未受入として残る。

## 2026-09-14 NF-V1-09M: Authoring検証の多重起動ガード

同じReleaseCandidate Playerを2画面で同時にAuthoring検証すると、共有runtime資源の競合で両方がタイムアウトすることが分かった。`Tools/Test-NyaForgeAuthoring.ps1`へ同じ`BuildName`の`NyaForge.exe`検出を追加し、検証開始前に「順番に実行」エラーを返すようにした。製品Playerのコードや保存形式は変更していない。

PowerShell parserで **PASS** を確認。V29の単独実行（1080×700／1600×1000）はNF-V1-09Lへ記録済みで、今後は同じPlayerを並列起動しない。

## 2026-09-14 NF-V1-09L: 現行HEADのReleaseCandidateV29再確認

前回のレビュー再照合後、現行HEAD `dd76473` からWindows Player `Builds/ReleaseCandidateV29/NyaForge.exe`を別出力した。既存Playerを上書きせず、Authoring suiteを1画面ずつ実行した。

- 1080×700: **PASS**（`Artifacts/Authoring-20260914-081243-3ee4984084fb4b05828bbea7316d6edb/report.json`、`authoring.png`）
- 1600×1000: **PASS**（`Artifacts/Authoring-20260914-081548-c6b6d08ca3f84bef9b44a8614b4fcc8c/report.json`、`authoring.png`）
- 同じ1080×700成果物の衣装packageをUnity **2022.3.22f1** Bridgeへ渡し、**16 checks PASS**（`Artifacts/BridgeReceiver-20260914-081913-413-2c85882368184553872e90c8fbfdaa5e/bridge-report.json`）。

2画面同時実行ではPlayer検証が競合して120秒でタイムアウトしたが、手順どおり単独実行で完了した。これは検証ランナーの実行条件差であり、製品の失敗とは扱わない。実EditorWindowのマウス・IME・Explorer、衣装全周の貫通ゼロ・見た目、VRChat Build & Test／実機表示は未受入として残る。

## 2026-09-14 NF-V1-09K: 9855d43衣装レビュー再照合と実RadDollV3受け入れ

提示された`9855d43`基準のP1（avatar移動後の配置、衣装更新時のownership、UV1欠落）とP2（sparse material slot、MR係数、Cuff winding、sampler variant、割当読込）を現行HEAD `4c1cd3c5ed35f57b450a92d278606ad1b71d74aa`へ再照合した。後続実装と回帰で解消済みのため、本番コードの重複修正は行っていない。詳細は[レビュー再照合](docs/reviews/2026-09-14-Feedback-9855d43-Recheck-4c1cd3c.md)へ固定した。

Coreは **505 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-c2301ac2fe9d4cd99a9bf34dd6ab5f2f`）。private Unity `2022.3.22f1`の実RadDollV3プローブも **passed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealAvatar-e341c4481e0c4480bde9a31c8706573e/report.json`）。20 SkinnedMeshRenderer／279 Transformを取込し、実`VRCPhysBone`構成、Body表面fit／weight候補、衣装package適用→再適用→Scene Save/Open後のownership参照を一周した。

これは実アセットを使った機械的経路とUnityシーン永続化の受入であり、実EditorWindowのマウス・IME・Explorer、衣装全周の貫通ゼロ・見た目、VRChat Build & Test／実機表示の完了を意味しない。Workbenchのfit／weightは選択面・選択頂点・最大距離を受け取るbounded overloadへ接続済みで、実マウスによる最終確認を次の手動受入に残す。

## 2026-09-14 NF-V1-09E: Windows高DPIで制作パネルを表示

実ウィンドウを`1080x700`・Windows DPI 150%（`Screen.dpi=144`）で直接起動したところ、内部の`Screen=1080x700`レイアウトだけを使うと右側の制作controlsが物理描画領域の外へ切れる問題を確認した。`PanelSettings`を実DPI係数（`dpi/96`、1〜2倍）へ合わせ、制作workbenchのroot・左viewport・右controlsへ明示的なflex幅を設定した。自動の注入式UIプローブは従来の1:1 panel-space座標を維持する。

確認結果:

- コード: `c0647c3`（`fix: fit authoring controls on high DPI windows`）
- Player: `Builds/ReleaseCandidateV19/NyaForge.exe`
- 自動Authoring suite: **PASS**（`Artifacts/Authoring-20260914-071233-63f28ebed52d4f008219649225b02fe1/report.json`、`1080x700`、既存チェック一式）
- 実ウィンドウ確認: **右側制作パネルの表示を確認**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-manual-v19.png`）。これはWin32の`PrintWindow`による画面証跡で、実マウス・IME・Explorer・実RadDollV3全周fit・VRChat表示の受入ではない。

この変更で高DPI時の右パネル切れというコード上の問題は閉じた。100/150/200%の実マウス操作、空白path・日本語IME、実アバターの全周fit・貫通・見た目、VRChat Build & Test／実機は手動受入として残す。

## 2026-09-14 NF-V1-09H: 狭い実ウィンドウで上部操作を折り返し

1080論理px・DPI150%の実ウィンドウでは、制作画面上部の「ビューワーに戻る」「ノード表示 / 非表示」が横幅不足で縮み、ラベルが切れる状態を確認した。上部rowのラベルとボタンを縮めず、既存の折り返しを使って2段へ送るようにした。

- コード: `c29ad83`（`fix: wrap authoring toolbar controls on narrow windows`）
- Player: `Builds/ReleaseCandidateV24/NyaForge.exe`
- 自動Authoring suite: **PASS**（`Artifacts/Authoring-20260914-073925-80038ce496114460bf12388f54f870d3/report.json`、`1080x700`）
- 実ウィンドウ: **2つの上部操作ボタンが折り返され、文字が読めることを確認**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-manual-v24-1080.png`）

狭い幅ではviewportの説明文が複数行になるため、実際の制作は1600論理px程度の広さを推奨する。実マウス・IME・Explorer、実RadDollV3全周fit、VRChatは未受入のまま残す。

## 2026-09-14 NF-V1-09I: 通常ビューワーの入口回帰

V24で通常ビューワーを`1280x800`で起動し、パック選択、キャンセル時の状態保持、確認セット、設定画面、制作画面への遷移と画面描画を再確認した。Authoring専用のDPIレイアウト修正が通常ナビゲーションへ影響していないことを確認した。

- Player: `Builds/ReleaseCandidateV24/NyaForge.exe`
- Navigation report: `Artifacts/Navigation-20260914-074515-633035562ad14daeac53226ed8626bff/report.json`（**PASS**）

これはスクリプト化された入口回帰であり、実マウス、Explorer、IME、実RadDollV3の全周fit、VRChat内表示の手動受入ではない。

## 2026-09-14 NF-V1-09J: 狭いviewportとstatusの重なりを解消

1080論理px・DPI150%で、viewportの操作説明、空project案内、下部statusが同時に折り返されると重なりやすい状態を確認した。viewportが狭い場合は空project案内を隠し、statusは実ウィンドウで折り返して全文を表示する。注入式Authoring検証では固定高さを維持し、リサイズ回帰の座標契約を壊さない。

- コード: `2965a93`（`fix: keep narrow authoring hints readable`）
- Player: `Builds/ReleaseCandidateV28/NyaForge.exe`
- 自動Authoring suite: **PASS**（`Artifacts/Authoring-20260914-075949-b4b0a79fe074472ab61ab08d1fe0df31/report.json`、`1080x700`）
- 実ウィンドウ: **上部操作・右controls・status全文を確認**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-manual-v28-1080.png`）

この確認は表示の重なりを対象にしており、実マウス・IME・Explorer、実RadDollV3全周fit、VRChat Build & Test／実機表示の受入ではない。

## 2026-09-14 NF-V1-09F: V19実RadDollV3一周スモーク

最新V19 Playerへprivate一時RadDollV3 VRMを渡し、全mesh取込、EditMesh、native Save/Open、標準skinned GLB／VRM1出力、制御fixtureのskinned clothing package生成を再実行した。生成packageをUnity **2022.3.22f1** Bridgeへ渡し、stable BoneId、bind pose、avatar-local translation／rotation／scale、材質・semantic map、ownership更新・削除Undoを含む**16 checks PASS**で確認した。private入力は公開ツリーへコピーしていない。

- Player: `Builds/ReleaseCandidateV19/NyaForge.exe`
- Player report: `Artifacts/Authoring-20260914-071904-d5dacd0d18284842a6f1ed2868f3c07d/report.json`
- Clothing package: `Artifacts/Authoring-20260914-071904-d5dacd0d18284842a6f1ed2868f3c07d/imported-accessory-skin-project/exports/clothing-20260913-222128-e28c83/skinned-clothing.nyaforge.json`
- Bridge report: `Artifacts/BridgeReceiver-20260914-072421-931-2f5e73fbea2d4c288580e5fc6d654b80/bridge-report.json`

これは実モデルの取込から受け渡しまでの回帰であり、実RadDollV3へ新規衣装を全周fitして貫通・見た目を人間が受入した記録ではない。VRChat Build & Test／実機表示、100/150/200%の実マウス・IME・Explorer操作も手動受入として残す。

## 2026-09-14 NF-V1-09G: 1600px実ウィンドウのDPI二重倍率を解消

V19の`1600x1000`起動で、DPI補正後のworkbenchが描画倍率を二重に受け、右controlsが再び画面外へ出る条件を確認した。制作workbenchの実幅・実高を`dpiScale²`で予約するよう修正し、PanelSettingsの描画倍率と物理クライアント領域を一致させた。注入式UIプローブは従来の1:1座標のまま維持する。

- コード: `3dfbccf`（`fix: reserve authoring layout for scaled player surface`）
- Player: `Builds/ReleaseCandidateV23/NyaForge.exe`
- 自動Authoring suite: **PASS**（`Artifacts/Authoring-20260914-073108-75b0e150f75e42d380823ee9c6141808/report.json`、`1600x1000`）
- 実ウィンドウ: **右controls全体とスクロールバーを表示**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-manual-v23-1600.png`）

これは実ウィンドウの表示領域に対する確認であり、100/150/200%の実マウス操作、日本語IME、Explorer選択、実RadDollV3全周fit・貫通・見た目、VRChat Build & Test／実機表示の受入ではない。

## 2026-09-14: 手動受入表をReleaseCandidateV10へ同期

手動チェック表の対象を古い`ReleaseCandidateV2`から、原画像sourceの未編集時再出力まで含む現行`ReleaseCandidateV10`（コード`43cb551`）へ更新した。衣装一周の項目へ、base-colorの原画像サイズ・作業画像サイズ・MIME・hashのinspection確認と、Paint編集後にpreviewへフォールバックするGLB確認を追加した。環境マニフェストもCore 505件とV10 Player／Bridge証跡へ同期した。これは手動操作を実施した記録ではなく、次の実操作で使う候補・確認条件の同期である。

## 2026-09-14 NF-V1-09D: 未編集base-colorの原画像出力

原画像sourceと現在のbounded Paint previewのhashを比較し、hashが一致する未編集base-colorだけはGLBへ元のPNG/JPEG bytes・MIME・原寸で出力するようにした。Paintを編集した場合、または旧project／source hashのないgraphでは従来のpreview PNGへ戻る。複数材質で同じpreview hashに異なる原画像が紐づく曖昧なケースは安全側にpreviewを使う。Coreは **505 passed / 0 failed**（最新artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-2bd33d69fb4843c9960f648d30d381b4`。JPEGのMIME・bytes保持回帰を含む）、Player V10 Authoring suiteとprivate RadDollV3実モデル＋Unity Bridge smokeも **PASS**（Player `Artifacts/Authoring-20260914-063522-17459e1e1a1e466fb3ef816fa6103750/report.json`、Bridge `Artifacts/BridgeReceiver-20260914-063802-587-d17638bb920544bcb4192f9ef4b84e43/bridge-report.json`）。

## 2026-09-14 NF-V1-09C: 原画像sourceのinspection公開

`forge_get_state`／graph inspectionから、原画像bytesを返さずに関連Paint node ID、原寸、MIME、encoded byte数、content hashを確認できるようにした。AIや再開時の検査が「previewへ縮小されたか」「原画像sourceが残っているか」を判定でき、raw bytesはnative project内にのみ保持する。Coreは **504 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-60347f235e944c8fbb390baa0893b5d9`）、Player V8 Authoring suiteも **PASS**（`Artifacts/Authoring-20260914-062402-ced59853197f4ea3bce7de5e211722a0/report.json`）。

## 2026-09-14 NF-V1-09B: base-color原画像sourceのnative保持

GLB／VRM取込時に1024pxへ縮小する作業用Paintとは別に、元画像のPNG/JPEG bytes・MIME・原寸・hashを`image.original-source` nodeとしてnative graphへ保存するようにした。Save/Openとgraph wireの往復でbytesを再取得でき、Polygon→skin派生でもsource nodeを保持する。入力の上限は8192px・16MiBで、未対応形式は取込時に警告して省略する。

現段階ではGLB/VRM再出力は従来どおりbounded Paint previewを使う。未編集時に原画像bytesをそのまま出力する切替は、作業画像編集後の出力規則・sampler・メモリ予算を定めた次段タスクへ分離した。したがって既存projectや原画像sourceを持たないgraphから原画復元を装わない。

Coreは **503 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-45c53f2d644040f0895c7ae862ee6403`）。`Builds/ReleaseCandidateV7/NyaForge.exe`のAuthoring suiteも **PASS**（`Artifacts/Authoring-20260914-061617-c0c4367b5d8e4355bcb07bd74788102b/report.json`）。private RadDollV3実モデルの全mesh取込・Save/Open・GLB／VRM1・衣装packageとUnity 2022.3.22f1 Bridge受け取りも **PASS**（Player `Artifacts/Authoring-20260914-061650-d48dc5ba73ad4ac7b96378c8e8b4fa64/report.json`、Bridge `Artifacts/BridgeReceiver-20260914-061934-292-5ea96cd5aebf48eeb9a7fd3a8540f1d0/bridge-report.json`）。

## 2026-09-14 NF-V1-09A: base-color縮小の明示

GLB／VRM取込でbase-colorをnative Paintの1024px予算へ縮小した場合、取込ステータスへ原画像サイズと作業画像サイズを出す。現在は`image.original-source` nodeが原画像bytesを別保持するため、Save/Open後も品質情報を失わない。形式・サイズエラーで省略した場合と、正常に縮小した場合の注意表示も分けた。未編集時の原画像bytesを出力へ使う接続はNF-V1-09B後段として残る。

`Builds/ReleaseCandidateV5/NyaForge.exe`（Unity 6000.4.3f1）のAuthoring suiteは **PASS**（`Artifacts/Authoring-20260914-055656-491efb9544c44ac1a1bb194d27f77e02/report.json`）。private RadDollV3の実取込・Save/Open・GLB／VRM1・衣装package生成とUnity 2022.3.22f1 Bridge受け取りも **PASS**（Player `Artifacts/Authoring-20260914-055729-e2c198f284584c4c9450f7f3a60b68d1/report.json`、Bridge `Artifacts/BridgeReceiver-20260914-060010-016-6f0b11002a194bcbb7c44473efc05b8d/bridge-report.json`）。これは縮小後の作業画像と出力の回帰であり、原画像bytes保存、実EditorWindowのマウス／DPI、実VRChat表示を完了扱いにはしない。

## 2026-09-14 NF-V1-09: semantic textureのUV1適用停止

Windows v1のGLB出力契約はUV0のみなのに、Workbenchのsemantic texture GUIだけがUV1を受け付けて後段出力で停止する経路を閉じた。UV1を選んだ新規normal／metallic-roughness画像の適用は`UNSUPPORTED_UV_SET`で無変更のまま拒否し、UV0へ戻した場合だけ適用できる。ヘルプ、Quickstart、開発計画、Player回帰の説明も同じ契約へ同期した。

`Builds/ReleaseCandidateV3/NyaForge.exe`（Unity 6000.4.3f1）のAuthoring suiteは **PASS**（`Artifacts/Authoring-20260914-054918-f6707a4e6e0a4207acbf908a996b233b/report.json`）。回帰にはUV1拒否、UV0適用、normal/MRのnative Save/Open、scalar変更時のmap保持を含む。private RadDollV3の実取込・Save/Open・GLB／VRM1・衣装package生成とUnity 2022.3.22f1 Bridge受け取りも **PASS**（Player `Artifacts/Authoring-20260914-054956-1bd71cc231014a6cb0356b9491660687/report.json`、Bridge `Artifacts/BridgeReceiver-20260914-055236-691-1e00049fdd234b74ab2bace7f7211042/bridge-report.json`）。GPUでの実材質画素比較、実EditorWindowのマウス／DPI、実VRChat表示は未受入として残る。

## 2026-09-14 feedback再照合: 9855d43 → 515183a

提示された衣装受け取り・材質処理レビューを現行`main`へ再照合した。P1（移動avatarへの配置、更新時ownership参照、UV1欠落）とP2（sparse material slot、MR係数、Cuff winding、sampler共有、適用前／削除後の割当読込）は、後続実装と回帰で解消済みであるため、本番コードの重複修正は行っていない。詳細は[現行HEAD再照合](docs/reviews/2026-09-14-Feedback-9855d43-Recheck-515183a.md)へ固定した。

現行Coreは **501 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0773c4378c4745aba2e7740d364f327a`）。ReleaseCandidateV2／private RadDollV3／Unity Bridgeの既存PASS証拠も有効だが、実EditorWindowのマウス／DPI、実アバター全周fit・貫通・見た目、VRChat Build & Test／実機表示、実運用の更新・削除は未受入として残す。

## 2026-09-14 NF-V1-06: surface clearance候補の読み取り検査

fit後の確認を進めるため、`MeshSurfaceClearance`を追加した。指定された衣装頂点とavatar面領域について、最近面のwindingに対するsigned距離を計算し、裏側へ入った候補数、最小／最大距離、最大64件の頂点IDを返す。GUIの「fit状態を測定（変更なし）」とMCP `forge_surface_fit_inspect`／`forge_get_state.surfaceFitInspection`へ接続した。これは三角形交差・閉じた体積の内外判定・貫通ゼロの証明ではなく、候補値として明示する。

Coreは **501 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-29f35c8373914c3d831018a76a0f4bd3`）。MCP transportも **3項目PASS**（tool discovery／state／capture metadata）。`Builds/ReleaseCandidateV2/NyaForge.exe`（Unity 6000.4.3f1）のAuthoring suite、private RadDollV3全mesh取込・Save/Open・GLB／VRM1 smoke、生成衣装packageのUnity **2022.3.22f1** Bridge受け取りも **PASS**（Player `Artifacts/Authoring-20260914-053420-df1a6262b2bf4aa089a521373593050a/report.json`、Bridge `Artifacts/BridgeReceiver-20260914-053713-618-f28c8e4e97c640ac99c22b85a92aeab4/bridge-report.json`）。手動の実EditorWindow、全周fit・交差・見た目、VRChat Build & Test／実機表示は未受入である。

手動受入の実施手順と記録欄を [Windows-v1-Manual-Acceptance.md](docs/Windows-v1-Manual-Acceptance.md) に固定した。自動fixture／実RadDollV3 smoke／Unity Bridgeを、実マウス・実アバター全周fit・VRChat実機の結果と混同しないためのチェック表である。

## 2026-09-14 NF-V1-16: ReleaseCandidateV1の再実行

現行 `main` の出荷候補Playerを `Builds/ReleaseCandidateV1/NyaForge.exe`（Unity 6000.4.3f1、NVIDIA GeForce RTX 4090）へ別出力し、Authoring suiteを **PASS**（`Artifacts/Authoring-20260914-052458-36a6f92a0af24da28d27f780f349f008/report.json`）で確認した。private一時入力 `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm` を公開ツリーへコピーせず、実RadDollV3の全mesh取込、EditMesh、native Save/Open、標準skinned GLB／VRM1出力と再読込を一周した。制御fixtureの衣装packageも同じPlayerから生成した。

生成したpackageをUnity **2022.3.22f1** Bridgeへ渡し、**16 checks PASS**（`Artifacts/BridgeReceiver-20260914-052738-428-fe139e32ebbc4f989de3c0343a7bd732/bridge-report.json`）。avatar-local配置（translation／rotation／scale）、stable BoneId、ownership更新・削除Undo、semantic normal／MRを含む受け取り経路を確認した。これは実アバターへ新規衣装を全周fitした貫通・見た目、実EditorWindowのマウス／DPI、VRChat Build & Test／実機表示の受入ではない。今回のコード変更はなく、証拠を現行HEADへ結び付ける再実行である。

## 2026-09-14 feedback再照合: 9855d43 → 8f6377a

提示された衣装受け取り・材質処理レビューを現行 `main` `8f6377a` へ再照合した。P1（移動avatarへの配置、更新時ownership参照、UV1欠落）とP2（sparse material slot、MR係数、Cuff winding、sampler共有、適用前／削除後の割当読込）は後続実装と回帰で解消済みで、本番コードの重複修正は行っていない。詳細は [レビュー再照合](docs/reviews/2026-09-14-Feedback-9855d43-Recheck-8f6377a.md) に固定した。

Coreは **500 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-15eb0ee4b498457c9ea5ae2114c3a829`）。これは自動経路の再確認で、実EditorWindowのマウス／DPI、実RadDollV3への新規衣装全周fit・貫通・見た目、VRChat Build & Test／実機表示は未受入として残す。

## 2026-09-14 NF-V1-06: avatar表面fitのMCP読み取り検査

GUIにあった「fit状態を測定（変更なし）」と同じ bounded `MeshSurfaceFit`を、sidecarの`forge_surface_fit_inspect`から呼べるようにした。現在選択中の衣装graph／avatar target／avatar面領域／衣装頂点領域／offset／最大距離を使い、評価頂点数、移動候補数、投影距離、移動量、対象object ID、revision、state hash、選択IDを返す。測定はread-onlyで、編集後はdocument identityが変わるため`available=false`になる。`forge_get_state`にも`surfaceFitInspection`を含め、AIが直前の測定の有効性を確認できるようにした。

Coreのcapabilities／IPC回帰は **500 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-77e02612365640a6aee6fe85279025f1`）。MCP transportは **3項目PASS**、Windows Player `Builds/SurfaceFitMcpV2/NyaForge.exe`のAuthoring suiteは **PASS**（`Artifacts/Authoring-20260914-051130-1df80f3be5004be09adaf8676f5c47ab/report.json`）。外部MCP→Playerの実通信は同Playerで **PASS**（`Artifacts/Authoring-20260914-051234-b73a2b0a64cd4ae38e4e504789fc11d5/report.json`）。さらにprivate RadDollV3 VRMを指定した実モデル取込・保存・GLB/VRM出力とUnity Bridge受け取りも **PASS**（Player `Artifacts/Authoring-20260914-051445-63e3b9aef9e94755b6206d3ae1804685/report.json`、Bridge `Artifacts/BridgeReceiver-20260914-051725-815-0909406889fd4b9bbc9248e5aad43ce5/bridge-report.json`）。通常fixtureでavatar targetなしの呼出しが構造化エラーとなること、既存の制作経路が継続すること、実モデル経路へ影響がないことを確認した。

これは候補形状の数値検査で、貫通なし・見た目・実RadDollV3全周fit・VRChat受入は示さない。実モデルでの成功レスポンスと、実EditorWindowのマウス／DPI受入を次の外部確認として残す。

## 2026-09-14 feedback再照合: 9855d43 → bd30224

今回のレビューは、基準 `9855d43` のP1（受け取りavatar移動後の配置、`SaveBindings`後のownership参照、UV1欠落）とP2（sparse material slot、MR係数、Cuff winding、sampler共有、適用前／削除後の割当読込）を指摘している。現行 `main` の `bd30224` へ再照合したところ、P1/P2はすでに後続修正と回帰で閉じており、同じ本番コードを重複修正しない。

- `SkinnedClothingReceiver`は衣装をavatar-root localで保持し、rootへ一度だけ親子付けする。移動・90度回転・scale付きavatarのBridge回帰を維持する。
- `NyaForgeSkinnedClothingBinding`はassignment identity（ObjectId）と生成object参照を分離し、同一ObjectIdのStateHash更新・削除後でも割当を読み込める。
- Windows v1のUV契約はUV0。`TEXCOORD_1`は取込・semantic slot・GLB出力で`UNSUPPORTED_UV_SET`として明示停止し、データを黙って落とさない。
- 材質slot、MR係数、Cuffの面向き、sampler variantはCore／Player／Bridge回帰で保持される。

現行Coreを再実行し、**500 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-50e9228e6a0741478e09cb624f919963`）を確認した。レビューの機械的指摘に対する追加実装は不要で、残る受入境界は実EditorWindowのマウス／DPI、実RadDollV3への新規衣装全周fit・貫通・見た目、VRChat Build & Test／実機表示である。根拠と手動受入項目は[feedback再照合](docs/reviews/2026-09-14-Feedback-9855d43-Recheck-bd30224.md)へ固定する。

## 2026-09-14 NF-V1-04: 剛体装着済みPolygonのskin派生

Polygonで作ったチョーカー／カフを先にstable BoneIdへ剛体装着し、位置を確認してからskin衣装へ派生できるようにした。`AccessorySkinMaterializer`へ基準姿勢のattachment frame焼き込みを追加し、`BoneDefinition.Head + bone-local offset`をavatar-local座標へ変換して位置・normal・tangentを派生MeshSourceへ保持する。派生側からattachment nodeは除去し、元のPolygon graphとDerivedSource provenanceは残す。Workbenchの **Polygon造形をskin衣装へ派生** は、attachmentなしの従来経路と、同じavatarを対象にしたattachment付き経路の両方を扱う。現在poseの見た目を焼き込まず、基準姿勢で確定する契約である。

Coreへ「polygon materialization bakes rigid attachment placement into the skin derivative」を追加し、**500 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0e7b280081cf4990b228925d8bdce824`）。Unity Player `Builds/ChokerMaterializeV1/NyaForge.exe`もビルド成功し、既存Authoring suite **PASS**（`Artifacts/Authoring-20260914-045048-0b2598a47f3f4dfdb7ba4f2094d6f631/report.json`）。実RadDollV3全周fit・貫通・見た目、実EditorWindowの手動操作、VRChat内受入は引き続き別カードである。

同Playerへprivate RadDollV3 VRMを指定した`Tools/Test-NyaForgeRealClothing.ps1`も、実モデル取込・Save/Open・GLB/VRM出力と制御fixture衣装packageを含む **93 checks PASS**（`Artifacts/Authoring-20260914-045332-fa48a54fe9f044459110786b215c38ed/report.json`）。生成packageをUnity **2022.3.22f1** Bridgeへ渡した受入も **16 checks PASS**（`Artifacts/BridgeReceiver-20260914-045612-318-b3e42ed672214df7bffd44908c7e6f1a/bridge-report.json`）。この実モデルrunは衣装全周fitの代替ではなく、取込・保存・出力・受け取りの回帰である。

## 2026-09-14 feedback triage: 9855d43レビューの再照合

外部レビューで挙がったP1 3件（avatar-local配置、`SaveBindings`の管理参照、UV1欠落）とP2 5件（sparse material slot、MR係数、Cuff winding、sampler共有、適用前／削除後の割当読込）を現行`main`（`0a52e45`）へ再照合した。いずれも既存修正と回帰で解消済みで、本番コードの重複修正は行わない。対応の詳細は[レビュー再照合](docs/reviews/2026-09-14-Feedback-9855d43-Triage.md)へ固定した。

再実行結果はCore **499 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b47755e58fa5483999b956e2f5f39314`）、private RadDollV3 VRMを使う`Tools/Test-NyaForgeRealClothing.ps1`のWindows Player **93 checks PASS**（`Artifacts/Authoring-20260914-044139-265f4f8035bf42cb8556c1ff978e7a99/report.json`）、同成果物の衣装packageをUnity **2022.3.22f1**へ渡すBridge **16 checks PASS**（`Artifacts/BridgeReceiver-20260914-044430-218-36d67848e7d049dda79b3de3b62daefb/bridge-report.json`）。Bridgeの16件目は衣装packageのhash／sidecar／ApplyPackage回帰で、従来の15件から増えたものではなく現行suiteの全件数である。

今回のPASSはCore／Windows Player／合成Bridge／private実RadDollV3の機械的経路を分けた証拠であり、実EditorWindowのマウス／DPI、実RadDollV3への新規衣装全周fit・貫通・見た目、VRChat Build & Test／実機表示は未受入として残す。

## 2026-09-14 NF-V1-04: 実RadDollV3モデルでの取込・保存・出力スモーク

private一時ファイル `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm` を公開リポジトリへコピーせず、Windows Player `Builds/ClothingPackageV2/NyaForge.exe` へ直接渡した。再実行用 `Tools/Test-NyaForgeRealClothing.ps1` から、実RadDollV3の全mesh instance取込・生成EditMesh・頂点編集・native Save/Open・標準skinned GLB再取込・VRM 1.0出力と再読込に加え、制御されたaccessory fixtureの選択衣装だけのself-contained skin package出力を含む **93 checks PASS**（`Artifacts/Authoring-20260914-043303-8ecec8b61b36402385df0f65f9faaaae/report.json`）。10 mesh instanceを編集可能化し、feature-preserving native export roundtrip、VRM metadata、衣装packageのobject/document/mesh hashを確認した。衣装package用一時GLB stagingはWindowsの深い作業パスでMAX_PATHを超えない短い場所へ分離した。実RadDollV3から新規衣装を作る全周fit・package化そのものは、この検査とは別の手動受入である。RadDollV3はVRM 0.xのため、元のSpringBoneをVRM 1.0へ自動変換できることはこの検査の合格条件に含めず、明示的に省略した。

同じ検査成果物から生成した衣装packageをUnity **2022.3.22f1** Bridge receiverへ渡し、**15 checks PASS**（`Artifacts/BridgeReceiver-20260914-043543-954-7167574cce3e4e39b387cae8fbf2edc4/bridge-report.json`）。package skeletonが親子順でない場合も受入fixture側で安定して構築できるようにし、skinned clothingのBoneId map、移動・回転・scale付きavatar root、ownership更新・削除復元、semantic normal／MR変換を再確認した。これは実ファイルの機械的経路を通した証拠であり、実EditorWindowのマウス操作、衣装の全周fit・貫通・見た目、VRChat Build & Test／実機表示は別受入として残る。

## 2026-09-14 NF-V1-03B: 納品対象allowlistをGUI／MCP／GLBへ接続

`DeliveryAllowlistCodec`（`NAWL` v1）と `delivery-allowlist.nyaforge.bin` attachmentを追加し、明示したobject ID集合をnative snapshotへ保存する。制作対象パネルの **選択中を納品対象に含める** トグル、MCP `get_state` の `deliveryAllowlistObjectIds`／`deliveryAllowlistExplicit`、GUI／MCPの静的・skinned・extended GLBおよびmulti-object exportへ同じ選択集合を渡す。allowlistが空なら全object候補、明示時は指定objectだけを出力し、manifest／GLB reportのobject件数・IDへ反映する。参照保護objectはallowlistへ追加できず、保護bodyを含む出力は出力先作成前に `REFERENCE_EXPORT_BLOCKED` で停止する。VRM 1.0のavatar＋衣装同梱とnative project backupは意図的な全体出力として対象外である。

Coreは **499 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-baa1cf057c514b848d2215c38ef9dc84`）。allowlist codecのSave/Open、multi-object subset、skinned GLB subsetとreport IDを回帰した。Windows Player `Builds/DeliveryAllowlistV1b/NyaForge.exe` のAuthoring suiteは **83 checks PASS**（`Artifacts/Authoring-20260914-041130-a387f05c5a0e438aab81a14911a67c52/report.json`）。参照body＋衣装の2 objectで、body保護→汎用GLB停止→衣装だけをallowlist指定→GLB `objectCount=1`→Save/Open後復元を確認した。同Player成果物のUnity **2022.3.22f1** Bridge receiverも **15 checks PASS**（`Artifacts/BridgeReceiver-20260914-041200-656-5c980b0258074607a08e769232248cf3/bridge-report.json`）。

実EditorWindowでのクリック選択、実RadDollV3への衣装package適用、受け取り側でのmanifest／report対象IDの手動照合は残る。実マウス・DPI差、全周fit・貫通・見た目、VRChat Build & Test／実機表示も別受入とする。

## 2026-09-14 NF-V1-03B: 納品対象allowlistの永続化

参照bodyだけを保護しても、汎用GLB／multi-object出力はworkspace全体を列挙できるため、納品対象を明示できるようにした。`DeliveryAllowlistCodec`（`NAWL` v1）を追加し、object ID集合をsorted・重複なしで検査して `delivery-allowlist.nyaforge.bin` へ保存する。Project snapshotはattachment上限を9件へ更新し、旧schemaのsidecar移行とnative Save/Openへ接続した。

制作対象パネルの **選択中を納品対象に含める** トグル、MCP `get_state` の `deliveryAllowlistObjectIds`／`deliveryAllowlistExplicit`、GUI／MCPの静的・skinned・extended GLBおよびmulti-object exportへ同じobject集合を接続した。allowlistが空なら従来どおり全objectが候補で、1つでも明示すると指定objectだけをGLB／multi-object manifestへ出力する。参照保護objectをallowlistへ追加できず、既存の汎用出力でも保護objectが選ばれていれば `REFERENCE_EXPORT_BLOCKED` で出力先作成前に停止する。VRM 1.0のavatar＋衣装同梱とnative project backupは意図的な全体出力のため対象外である。

Coreは **498 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-5de02eeb46d14bb78b556ea62bdcd117`）。allowlist codecのnative Save/Open往復を含む。Windows Player `Builds/DeliveryAllowlistV1/NyaForge.exe` はビルド成功し、Authoring suite **83 checks PASS**（`Artifacts/Authoring-20260914-040059-38d6557651d0434d895a8666067a1d95/report.json`）。2 objectのうち参照bodyを保護して汎用GLBが停止し、衣装側だけをallowlistへ追加してGLB `objectCount=1`で出力、Save/Open後にallowlistが復元される回帰を確認した。

残るNF-V1-03Bの外部受入は、実EditorWindowでのクリック選択、実RadDollV3への衣装package適用、manifest／reportの対象IDを受け取り側で照合する手動確認である。実マウス・DPI差、全周fit・貫通・見た目、VRChat Build & Test／実機表示は引き続き別受入とする。

## 2026-09-14 feedback triage: 9855d43レビューの再照合と汎用出力ガード

外部レビューの対象は `9855d43` で、現行 `main`（`ed27950`）より前の基準だった。P1の3件は現行mainで解消済みである。受け取り側のavatar-local配置（移動・回転・scaleを含む）は `e250372` と Bridge 回帰、同じObjectIdの新StateHash保存で生成object参照を保持する更新経路は `23a2b8a`、UV1を黙って落とさず `UNSUPPORTED_UV_SET`で停止する経路は `7ea1cc5` で確認した。P2の材質slot、MR係数、Cuff winding、sampler共有、ownership markerもそれぞれ既存Core／Bridge回帰へ接続済みである。

残っていた設計上の穴として、参照bodyを保護しても汎用multi-object／GLB出力がworkspace全体を列挙できた。`EnsureGenericDeliveryExportAllowed()`を追加し、保護objectが存在する場合は出力先を作る前に `REFERENCE_EXPORT_BLOCKED` で停止する。GUIのUnity用multi-object出力、標準／拡張GLB、MCPの同経路へ適用し、参照bodyを含めない明示出口として選択衣装skin packageを案内する。native projectの保存と、avatar＋衣装を意図して同梱するVRM 1.0出力はこのガードの対象外とした。

Windows Player `Builds/RefExportGuard/NyaForge.exe` をビルドし、Authoring suite **83 checks PASS**（`Artifacts/Authoring-20260914-035018-fd13f91381904083aad2e1cd01a66071/report.json`）。2 objectの参照保護後に静的GLB出力がメッセージを返し、`exports`を作成しない回帰を含む。Coreは **497 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-63ab3b928d7f42a9acf0dbbf029f8085`）。

永続的な納品allowlistは後続の **NF-V1-03B** で実装し、GUI／MCP／manifest出力、Save/Open、選択対象のGLB回帰まで確認済み。実EditorWindowの手動操作、実RadDollV3全周fit・貫通・見た目、VRChat Build & Test／実機表示は引き続き外部受入である。

## 2026-09-14 MCPへ参照保護状態を公開

`get_state`へ`referenceProtectedObjectIds`と`activeObjectReferenceProtected`を追加した。AI側が参照bodyを編集対象から外せるよう、保護状態をエラー発生後ではなく編集前に取得できる。空projectのnamed pipe state回帰で配列とactive=falseを確認し、Windows Player `Builds/McpRefProtectionState/NyaForge.exe`のAuthoring suite **83 checks PASS**（`Artifacts/Authoring-20260914-034328-5750299d23c84ce8950a4f8308328a64/report.json`）。実MCP clientによる保護中objectの選択・編集拒否は、別途実アバター手動受入の境界として残る。

## 2026-09-14 参照保護のUndo/Redo同期

参照body保護のattachmentをUndo/Redoした際、native attachmentの正本と制作対象パネルのトグルがずれる穴を修正した。GUIとMCPのhistory undo/redo後に`reference-protection.nyaforge.bin`を再読込し、保護中の頂点編集停止状態を維持する。Windows Player `Builds/RefProtectionUndo/NyaForge.exe`のAuthoring suiteは **83 checks PASS**（`Artifacts/Authoring-20260914-034002-538c4019d23045dd9010231eca7ee326/report.json`）。ON→Undo→Redo→頂点編集停止→Save/Open→解除まで自動回帰に含む。Coreも **497 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-ada9ea537fa9447595c259624ed75b3b`）。実EditorWindowの手動操作、実RadDollV3全周fit・貫通・見た目、VRChat内表示は未受入である。

追加のBridge回帰を実施した。`NyaForgeSkinnedClothingBinding`へ同じObjectIdの新しいStateHashを保存しても、既存の生成object参照とstable BoneId割当を保持し、その後の更新で新objectへ一度だけ付け替えられることを確認した。Unity **2022.3.22f1**のBridge suite **15 checks PASS**（`Artifacts/BridgeReceiver-20260914-031910-021-7a155f84e2354bb3be94d816c8401c1a/bridge-report.json`）。これは更新時のownership参照回帰であり、実EditorWindowの手動操作・実RadDollV3全周fit・貫通・見た目・VRChat内受入とは分けて扱う。

## 2026-09-14 FeedbackFixV5 レビュー照合

通常GLBのUV1欠落を黙って許さない契約を追加した。`GlbImporter.ReadPrimitive`は`TEXCOORD_1`入力を`UNSUPPORTED_UV_SET`で拒否し、skinned importerも同じ静的形状経路を通るため同じ結果になる。Core回帰を追加し、UV0のみをWindows v1出荷範囲とすることをREADMEへ明記した。Coreは **496 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-da3659c1953a4c0991cd05296c52ef23`）。

この変更を含むWindows Playerを`Builds/WindowsV1Uv1/NyaForge.exe`へ別出力し、Authoring suiteを **83 checks PASS**した。Unity **6000.4.3f1**、GPU NVIDIA GeForce RTX 4090。証拠は`Artifacts/Authoring-20260914-022032-05bcf10ef0bb4ea08953cc45436bf119/report.json` と `authoring.png`。Player受入は通常GLBのUV1停止契約を含む自動経路の確認であり、実マウス・実RadDollV3全周fit・VRChat内表示とは別である。

Workbenchの衣装表面処理へ`avatar面ID（カンマ区切り・空欄=全て）`を追加した。指定したrest meshの三角形領域を`MeshSurfaceFit.Project`と`SkinWeightTransfer.BySurfaceProjection`へ同じ入力で渡し、範囲外ID・不正文字列・距離超過は文書を変更せず診断する。自動Authoring suiteで面ID `0`を指定したfit／surface weight、ステータスの領域表示、失敗時無変更を確認した。`Builds/WindowsSurfaceRegion/NyaForge.exe`（Unity **6000.4.3f1**）と **83 checks PASS**の証拠は`Artifacts/Authoring-20260914-022441-128a2e31fec14decbb120c3d3eb2de76/report.json` と `authoring.png`。面領域指定は受入経路へ接続したが、実マウスでの面選択、実RadDollV3全周fit・貫通・見た目、VRChat内表示は未受入である。

さらに`衣装頂点ID（カンマ区切り・空欄=全て）`を追加し、指定頂点だけをfit／surface weight更新する経路へ接続した。未選択頂点の位置と既存weightを保持し、選択頂点・avatar面領域・最大距離の検証を一つの操作で行う。「現在の衣装頂点選択を適用対象にする」ボタンでビューポート選択を同じ入力へ取り込める。Coreは **496 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-13b58921c12044f9af20c2af39255a97`）。最新`Builds/WindowsSurfaceRegion3/NyaForge.exe`（Unity **6000.4.3f1**）のAuthoring suiteも **83 checks PASS**（`Artifacts/Authoring-20260914-023409-a1b227311bbd4c12ae710e341c55a860/report.json` と `authoring.png`）。これは選択範囲の自動経路確認で、実マウスによる面／頂点選択、実RadDollV3全周fit・貫通・見た目、VRChat内表示は未受入である。

さらに`クリックでavatar面を選択（Shiftで追加）`モードを追加した。ビューポート上の非アクティブavatarをraycastし、選んだ三角形の通し番号をfit／weight共通のavatar面IDへ反映する。Authoring suiteでavatar三角形0のクリック選択、衣装頂点選択の取込、領域限定fit／weightを確認した。`Builds/WindowsSurfacePick/NyaForge.exe`（Unity **6000.4.3f1**）と **83 checks PASS**の証拠は`Artifacts/Authoring-20260914-023827-3c92db189ce045c9b79442c0ce4cf207/report.json` と `authoring.png`。raycastは自動fixtureでの確認であり、実マウスのDPI／視点操作、実RadDollV3全周fit・貫通・見た目、VRChat内表示は未受入である。

選択面を見失わないよう、avatar面領域のオレンジ色overlayを追加した。対象avatarのrest meshを表示と同じ座標変換で描画し、選択解除・対象変更・不正IDでoverlayを無効化する。`Builds/WindowsSurfaceHighlight/NyaForge.exe`（Unity **6000.4.3f1**）のAuthoring suite **83 checks PASS**（`Artifacts/Authoring-20260914-024129-11599e7c9b1c4209ac635afec67b6415/report.json` と `authoring.png`）で、既存の衣装選択・fit／weight経路への影響がないことを確認した。overlayの見た目は自動fixture画像で確認し、実RadDollV3の全周外観・DPI・VRChat内表示は未受入である。

面領域の解除操作をボタン化し、「avatar面領域を解除（全三角形）」で入力欄・選択集合・overlayを一度にクリアできるようにした。最新`Builds/WindowsSurfaceClear/NyaForge.exe`（Unity **6000.4.3f1**）のAuthoring suite **83 checks PASS**（`Artifacts/Authoring-20260914-024400-8ba2a334092c4dd6b93351665a7b9e5f/report.json` と `authoring.png`）。

最新のavatar面選択overlay版PlayerをBridgeへ渡し、Unity **2022.3.22f1**のreceiver verification **15 checks PASS**を確認した。skinned clothingの変形avatar local/world配置、ownership cleanup、複数package管理、semantic normal／MR変換を再回帰した。証拠は`Artifacts/BridgeReceiver-20260914-024250-790-c36011a940cf4582bc68b98879a5e807/bridge-report.json`。Bridge合格は合成fixture受入であり、実RadDollV3全周fit・貫通・見た目・VRChat内表示を含まない。

範囲限定fitの検査値を修正した。`MeshSurfaceFitResult`が`EvaluatedVertexCount`を持ち、衣装頂点を一部選択した場合も平均投影距離・平均移動量を評価対象だけで割る。Coreは **496 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-71e6fe78f2374cec9bdf5dcab47aa0e3`）。これでfit結果の表示値が未選択頂点数に薄められず、範囲限定操作の確認記録へ使える。実アバター全周の貫通判定・見た目受入は別途必要である。

このCore変更を含むUnity **6000.4.3f1** Playerを`Builds/WindowsFitMetrics/NyaForge.exe`へ別出力し、Authoring suite **83 checks PASS**を確認した。証拠は`Artifacts/Authoring-20260914-025023-dc782617ee3a4acc8ae12e856b1f1a39/report.json` と `authoring.png`。これは自動fixtureでのコンパイル・UI回帰であり、実マウス・実RadDollV3全周fit・貫通・VRChat内表示とは別である。

同じPlayer検証成果物をUnity **2022.3.22f1** Bridgeへ渡し、receiver verification **15 checks PASS**を再確認した。証拠は`Artifacts/BridgeReceiver-20260914-025117-261-ef07c59af2624fb9beb70a7c5a1409ad/bridge-report.json`。受け取り側の配置・ownership・semantic map回帰を含むが、実RadDollV3の全周fit・貫通・VRChat内表示は未受入である。

同じ`WindowsFitMetrics` Playerへprivate一時RadDollV3 VRM（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`）を指定し、実モデルの単体／全mesh instance取込、EditMesh頂点編集、native Save/Open、標準skin GLB再取込、VRM 1.0出力まで含むsuiteを **PASS**した。10 mesh instanceの編集可能化とfeature-preserving native exportを確認した証拠は`Artifacts/Authoring-20260914-025700-6f121300a2f5412f85f3339e36623660/report.json` と `authoring.png`。同成果物をUnity **2022.3.22f1** Bridgeへ渡したreceiver verificationも **PASS**（`Artifacts/BridgeReceiver-20260914-025952-761-5048146e313a4bd58ea2fa791936d1fd/bridge-report.json`）。これは実ファイル取込・保存・出力の自動smokeであり、実EditorWindowの手動操作、衣装の全周fit・貫通・見た目、VRChat内Build & Test／表示を完了扱いしない。

fit確認操作を追加した。小物パネルの「fit状態を測定（変更なし）」は、現在のavatar面領域・衣装頂点・offset・最大距離で`MeshSurfaceFit`を候補計算し、評価頂点数・移動候補数・投影距離・移動量を表示する。`DocumentRevision`と`StateHash`は変更しない。`Builds/WindowsFitInspect/NyaForge.exe`のAuthoring suiteは **83 checks PASS**（`Artifacts/Authoring-20260914-030227-a55f54419e60452798ee222e164bdbdb/report.json`）、private RadDollV3 VRMを用いた同suiteは **PASS**（`Artifacts/Authoring-20260914-030309-66c524a30d684bfe8cb85a08a0f8b469/report.json`）、Unity Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260914-030556-920-7fcdd4cfcd2741d1a2c4cc6f699f82e2/bridge-report.json`）。これは候補形状の数値確認であり、貫通判定・見た目・実マウス・VRChat内表示は未受入である。

fit本体のステータス表示も、範囲限定時の分母を全頂点数から評価頂点数へ揃え、「移動／評価頂点」と表示するよう修正した。`Builds/WindowsFitDisplay/NyaForge.exe`のAuthoring suiteは **83 checks PASS**（`Artifacts/Authoring-20260914-030940-df78d37dd7794790a84d4722f4b0deeb/report.json`）。

レビューで挙がった `9855d43` 系のP1/P2を現行mainへ再照合した。対象は衣装受け取りの座標、割当保存、UV1、疎なmaterial slot、MR係数、カフ面向き、sampler共有、削除済み衣装の割当読込である。

次の項目は現行実装と回帰で確認済みで、同じ修正を重ねて行わない。

| 指摘 | 現行の扱い | 根拠 |
|---|---|---|
| avatar移動後の衣装配置 | avatar-local座標を保持し、receiver rootへ一度だけ親子付け | 変形avatarのUnity Bridge fixture |
| 衣装更新時の割当参照 | Bone割当の再利用と生成object所有をObjectIdで分離 | `MatchesAssignment`、更新・削除・再適用回帰 |
| UV1の欠落 | Windows v1はUV0契約。通常GLB／UV1 semantic slotとも`UNSUPPORTED_UV_SET`で停止し、黙って破棄しない | Core import/export回帰 |
| sparse material slot | 使用submeshだけをslot mapへ残し、slot番号と入力portを保持 | Core sparse slot GLB roundtrip |
| MR係数・sampler・カフ winding | shader係数、sampler hash、quad windingを修正 | Core 496件、Player/Bridge fixture |
| 適用前・削除後の割当読込 | 生成objectの存在とassignment identityを分離 | Unity Bridge binding回帰 |

残る受入境界は、実RadDollV3 sceneでの衣装表面fit／surface weight転送、実EditorWindowのマウス操作とDPI、fit後の貫通・見た目、VRChat Build & Test／実機表示である。これらはCoreや合成Bridgeの合格へ読み替えず、`NF-V1-03A`、`NF-V1-04〜08`、`NF-V1-10`の手動受入カードとして扱う。次は実アバターのbody rendererを基準にしたfit／weight測定をprivate probeへ追加し、公開リポジトリにはSDK・素材・private sceneを入れず証拠だけを記録する。

private probeへ実RadDollV3の`Body` rendererを基準にした最小surface測定を追加した。Unity **2022.3.22f1**でBody 8,467頂点／10,770三角形をavatar-localへ変換し、実bodyの非退化三角形から1 mm離した3頂点カフを作成して、`MeshSurfaceFit.Project`（offset 0.5 mm、上限10 mm）と`SkinWeightTransfer.BySurfaceProjection`（4 influence、上限10 mm）を実行した。3頂点すべてがfitされ、最大投影距離 **0.99995 mm**、最大移動量 **0.49994 mm**、surface weight 3頂点・最大1 influence・正規化済みを確認した。証拠はpublicへ素材を含めないprivate `private/PhysBonesSdkProbe-20260914/avatar-fit-weight-report.json`。これは実bodyメッシュとの座標・距離制限・weight正規化の接続確認であり、カフ全周の貫通・衣装の見た目・手動EditorWindow操作・VRChat内受入ではない。

この照合後のCore再実行も **495 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-d89dc7e009834b1daaee015258361fb9`）。

公開コードの現行mainからWindows Playerを再生成し、`Builds/WindowsV1Final/NyaForge.exe`（Unity **6000.4.3f1**）でAuthoring suiteを **83 checks PASS**した。実行GPUはNVIDIA GeForce RTX 4090、画面は1600×1000で、証拠は`Artifacts/Authoring-20260914-021258-85e612e192e448e7b3298a6fd3ae9b97/report.json` と `authoring.png`。これは自動GUI／GPU fixtureの受入であり、実マウス操作や実RadDollV3の全周fit・VRChat内表示とは別である。

同じ`WindowsV1Final`へprivate RadDollV3 VRMを指定し、VRM出力まで含むAuthoring suiteも **90 checks PASS**した。証拠は`Artifacts/Authoring-20260914-021400-f77677eea9194ebc8d1a00f310696d17/report.json` と `authoring.png`。これは実VRMの取込・編集経路と自動GLB/VRM回帰を確認したもので、実RadDollV3 sceneへの衣装fit・貫通・手動EditorWindow操作・VRChat内Build & Testとは分けて扱う。

ChatGPT Proの持込Windows v1案を現行mainへ照合し、[採用修正版](docs/Windows-v1-Development-Plan.md)へタスク化した。方針は採用するが、実SDK／実VRChat未受入を完了扱いにせず、実装済みの衣装受け渡しを重複開発しない。製品全体のC0〜C5と進行中goalは維持する。以降の着手順はこの欄と採用修正版を優先し、下に残る日付付き記録の「次」は当時の履歴として読む。

## 2026-09-14 Downloads版Windows v1案の再確認

`C:\Users\tomoaki\Downloads\NyaForge-Windows-v1-Development-Plan.md`を検証対象コード`main`（`ca346ee`）と再照合した。**方針は妥当で、遠回りにはなっていない。** 既存の編集基盤を作り直さず、既存アバターへ衣装だけを渡す出口、造形→skinの一周、semantic texture、再適用、実受入を分ける順序は採用する。

原案からの実務上の修正は採用修正版へ反映済みである。NF-V1-02を02A/02Bへ分割し、衣装package/receiver（03A）をG1へ前倒しし、実SDK・実VRChat・実マウスをCore/Player/合成Bridgeと混同しない。12週間・週20〜25時間は見積りの仮定として採用せず、最小受け渡しと一着の実測後に見直す。

semantic normal/MRは現行実装で一周したため、ここから先はAO/emissive・全shader・FBX/BLEND・完全VRM・Quest/macOSを増やさず、NF-V1-01/03の受け取り環境とNF-V1-04〜08のカフ一着手動完走を優先する。実SDK probe済みの範囲と、実アバター／実VRChatでまだ受入していない範囲を分け、Core 496件PASSや合成Bridge PASSを実VRChat合格へ読み替えない。

## 2026-09-14 実VRChat SDK probe

VCCキャッシュの`com.vrchat.base`／`com.vrchat.avatars` **3.7.6**を公開対象外の`private/PhysBonesSdkProbe-20260914`へ展開し、Unity **2022.3.22f1**で`Tools/Test-NyaForgePhysBonesSdk.ps1 -RunUnityProbe -RequireSdk`を実行した。`status: verified`、Unity probe `passed`。実型`VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone`を解決し、capability（root/endpoint/exclusions/branches/colliders/limits/interaction/parameter）、非対応non-zero値の生成前拒否、実コンポーネント生成、stable root/BoneId設定を確認した。証拠はprivate `private/PhysBonesSdkProbe-20260914/sdk-probe-report.json` と一時Unity report/logに保存している。

この結果でNF-V1-01のSDK版固定とNF-V1-02Aの「型解決・受け取り設定」部分は進んだが、実RadDollV3 Unity sceneへの全BoneId割当、Build & Test、実VRChat内の揺れ・外観・他者視点、実マウス/DPIはまだ未受入である。SDK DLL・private素材・検証projectはpublic repositoryへ追加しない。

同じprivate projectへ`RadDollV3.fbx`を置き、Unity AssetDatabaseで読み込んだところ、20個の`SkinnedMeshRenderer`、279 transform、renderer bone参照3420件、共通root `Hips` を検出した。証拠は`private/PhysBonesSdkProbe-20260914/avatar-import-report.json`。これは骨格の存在確認であり、衣装packageの適用やVRChat Build & Testの成功とは扱わない。

## 2026-09-14 実RadDollV3 PhysBone設定probe

同じprivate projectで、実際にimportされたRadDollV3の`Skirt_B_1_1.L`→`Skirt_B_1_2.L`直下chainへ、reflection adapter経由でSDK 3.7.6の`VRCPhysBone`を生成・設定した。Stable UUIDのBoneId map、root、endpoint Auto、既定parametersを事前検証後に適用し、`physBoneConfigured: true`を確認した。証拠は`private/PhysBonesSdkProbe-20260914/avatar-physbone-report.json`。これは実SDK型と実アバター階層の接続確認であり、衣装packageの適用、揺れの見た目、Build & Test、実VRChat内受入を完了扱いしない。

## 2026-09-14 実RadDollV3 skinned-clothing package適用probe

同じprivate projectで低ポリカフ（136頂点）を生成し、skinned GLB出力→`skinned-clothing-v1` package化→実RadDollV3 FBX prefab instanceへの`SkinnedClothingReceiver.ApplyPackage`まで通した。`lower_arm.L`を明示BoneId mapへ割り当て、生成Rendererの親、rootBone、頂点数、ownership markerを確認した。同じnative graphを`AuthoringProjectExportService`で保存し`ProjectStore.Open`してgraph identity／node数を照合後、packageを再読込して再適用した。管理対象Renderer数は21（元の20＋衣装1）のまま維持し、bindingのObjectId／生成object参照も更新できた。さらに受け取りsceneを保存して開き直し、衣装Renderer 21件とbinding／ownership markerが残ることを確認した。証拠は`private/PhysBonesSdkProbe-20260914/avatar-clothing-reapply-report.json` と `private/PhysBonesSdkProbe-20260914/avatar-scene-roundtrip-report.json`（いずれもstatus passed）。これは実FBX階層への初回・再適用とnative/scene roundtripのsmokeであり、EditorWindowの手動操作、fit・貫通、見た目、Build & Test、実VRChat内受入は未完了である。

## 次に実装するカード

| 順 | ID | 状態 | 次の具体作業・完了条件 |
|---|---|---|---|
| 1 | NF-V1-01 / 03 | manifest・実SDK／実RadDollV3 chain probe済み / 外部受入BLOCKED | private受け取りprojectでSDK 3.7.6の実`VRCPhysBone`解決・生成・stable root/BoneId設定、RadDollV3のSkirt直下chain設定まで確認済み。次は実アバターsceneの全対象chain明示割当とBuild & Testを行い、未実施の実VRChatはBLOCKEDとして残す |
| 2 | NF-V1-03A | 実装・合成Bridge受入済み / 実FBX初回・再適用・native/scene roundtrip smoke済み | private RadDollV3 FBXへカフpackageを`lower_arm.L`へ明示適用し、136頂点・rootBone・ownership marker、同一package再適用時の重複なし、graph/package/sceneの保存再読込を確認。次は実EditorWindow経路での割当保存、実sceneへのfit・weight、見た目確認 |
| 3 | NF-V1-04 / 05 / 06 / 07 | Core/Player実装済み・面領域入力接続済み・手動未受入 | Polygon派生→UV/paint→面領域を指定した範囲限定fit/weight→pose確認を実マウスで通し、参照body保護・Undo・Save/Openを確認 |
| 4 | NF-V1-08 | カフ試作経路実装・Player受入済み / 実アバター未受入 | 低ポリ手首カフを頂点編集し、実RadDollV3へfit・weight・Unity適用・VRChat確認。自動テンプレート通過を販売品質と扱わない |
| 5 | NF-V1-09 / 10 | Core実装済み・Unity/VRChat未受入 | semantic normal／metallic-roughnessのpackage適用を実sceneで確認し、Standard shaderの外観・tangent・samplerを記録。occlusion/emissiveと専用paintは後続範囲 |
| 後続 | NF-V1-11〜16 / 02A / 02B | 未完了 | 複数衣装・Unity再適用→長時間/手動/別環境→VRChat/private upload/RC。詳細依存は採用修正版参照 |

GUI/MCP共通command（13）と保存・復旧（14）は各実装と同時に検証する。SDKや他者視点待ちでも、独立した08/09等のCore・GUI作業は継続できる。外部検査の未実施は未実施のまま残す。全身キャラ制作、FBX／BLEND parser、完全VRM互換、全shader、Quest対応はv1受入まで着手しない。

## 2026-09-14 clothing receiver feedback fixes

レビュー `9855d43` の受け取り・材質指摘を現行mainへ反映した。`SkinnedClothingReceiver` はpackageのavatar-rest座標を受け取り側avatar rootのlocal座標として保持し、生成objectをrootへ親子付けするだけにした。これによりavatar rootの移動・90度回転・scaleを`worldToLocalMatrix`で二重に打ち消さない。合成Bridge fixtureへ変形avatar（translation／rotation／scale）のlocal/world一致確認を追加した。

衣装更新時の割当保存は、state／skeleton／binding hashが新revisionへ変わっても同じObjectIdの管理生成物と親rootを保持する。保存済み生成物が削除済みでも`MatchesAssignment`でstable BoneId割当を読み込め、GUIの適用ボタンは同一ObjectIdのassignmentを再利用できる。異なるObjectIdのbindingは従来どおり置換対象にしない。

semantic textureはUV1のメッシュ保持が未実装のため、Windows v1の出荷契約をUV0へ固定した。GLB semantic texture取込・出力でUV1を`UNSUPPORTED_UV_SET`として画像や作品を書き込む前に停止し、従来の無言欠落をなくした。出力側の画像共有keyへsampler hashを含め、同一画像をRepeat／Clampで使う材質を統合しないようにした。Polygon→skin materializationでは評価済み`PolygonRenderMesh.MaterialSlotMap`を派生sourceへ保持する。

カフprimitiveは外側・上下・内側・底面のquad windingを反転し、指定normalと片面描画の向きを一致させた。PBR preview shaderはdecoded MR mapへmetallic factor／roughness factorを適用し、Unity package receiverもmap適用時にmetallic factorを1へ固定しない。Coreは **493 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-6a8f4aae83fc4c98bb3d8664b51980ae`）。BridgeのUnity実行と実RadDollV3 scene再確認は次の受入カードで行い、Core合格を実VRChat表示合格へ読み替えない。

Workbenchのavatar表面weight初期化も、画面の`surface fit最大距離`を共有するbounded overloadへ接続した。衣装頂点が選択avatar表面から設定距離を超える場合は`WEIGHT_TRANSFER_DISTANCE`で文書を変更せず停止する。Unity Bridgeの変形avatar fixture（translation／90度rotation／scale）とMR scalar factor確認を含む検証を再実行し、Unity **2022.3.22f1** Bridge **passed**（`Artifacts/BridgeReceiver-20260914-013009-259-2321928ff3e44410beecb4d8d3abf222/bridge-report.json`）。変更は `cd9f139` としてorigin/mainへpush済み。

sampler共有の回帰も追加し、同一画像bytesをRepeat／Clampで使うnormal／MR slotがGLB出力で別texture・別samplerとして保持されることを確認した。Coreは **494 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4c6a1a78819344e5b62b82cf8f4390ac`）。

Polygon→skinの疎な材質slot経路を追加確認した。Polygonのslot 3／9を派生skin graphへ変換し、native command経由で保持した後、skinned GLBへ出力・再取込して2 submeshと赤／青の2材質を確認できるようにした。あわせて、現在のPolygonが使っていないslotを`MaterialSlotEvaluation`が出力へ持ち越さないよう修正した。未使用bindingを残すとsubmesh数とmaterial数が一致せず、GLB exportが停止するためである。Coreは **495 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-e51efeb1c57a407980ebcfbc79ad1d66`）。

## 2026-09-14 FeedbackFixV3 Player / Bridge再確認

`Builds/FeedbackFixV3/NyaForge.exe`を再ビルドし、公開fixtureのAuthoring suiteを **87 checks PASS**（`Artifacts/Authoring-20260914-013238-f89ac9f44a4a477c95e0b2164e790098/report.json`）で確認した。同じPlayerへprivate一時RadDollV3 VRM（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`）を渡した取込→EditMesh→Save/Open→GLB／VRM出力の一周も **87 checks PASS**（`Artifacts/Authoring-20260914-013547-1dcae8e2d8fa488aadb65df48e29ab19/report.json`）。この成果物をUnity **2022.3.22f1** Bridgeへ渡し、変形avatar root・衣装package・semantic mapを含む receiver回帰も **passed**（`Artifacts/BridgeReceiver-20260914-014007-246-cb45920700374422aecbbd5e2c582ebb/bridge-report.json`）。

これは自動Playerと合成／private smokeの証拠であり、実マウス・DPI差、実RadDollV3 sceneでの衣装fit／貫通、Build & Test、実VRChat内の見た目・負荷を完了扱いしない。private素材・SDKはpublic repositoryへ追加していない。

## 2026-09-14 FeedbackFixV4 Player / Bridge再確認

`772462d`の疎な材質slot修正を含む`Builds/FeedbackFixV4/NyaForge.exe`を再ビルドした。公開fixtureのAuthoring suiteは **83 checks PASS**（`Artifacts/Authoring-20260914-015221-c185ea72d2f94cdaa84944ebf3096b50/report.json`）、private一時RadDollV3 VRM（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`）を指定したsuiteは **87 checks PASS**（`Artifacts/Authoring-20260914-015257-dc23649f52fe4f3e9d996431eab2a620/report.json`）。同private Player出力をUnity **2022.3.22f1** Bridgeへ渡し、receiver回帰も **passed**（`Artifacts/BridgeReceiver-20260914-015436-098-954aa70c383740c1a6b5ffe87b00b00b/bridge-report.json`）。

この再確認はPlayer自動経路と合成Bridgeの証拠であり、疎なslotを含む実RadDollV3 sceneのfit・貫通・手動EditorWindow操作、Build & Test、実VRChat内の見た目・負荷を完了扱いしない。private素材・SDKはpublic repositoryへ追加していない。

同じFeedbackFixV4 Playerで`-VrmExport`を有効にし、private RadDollV3 VRM取込からVRM出力を含むsuiteも **90 checks PASS**（`Artifacts/Authoring-20260914-015524-81f8c35d45504482a12e0bc3f1ba781e/report.json`）だった。これはVRM packageの自動node map／表情・Spring参照回帰とGLB出力の確認であり、UniVRM/VRChat SDK上の実Build & Test、実VRChatアップロード・表示、実アバター衣装の見た目受入とは分けて扱う。

公開候補として`Builds/WindowsV1Candidate/NyaForge.exe`を`Tools/Build-NyaForge.ps1 -Target All`で生成した（Unity **6000.4.3f1**、`Logs/build-all-20260914-020031-025.log`）。同PlayerのAuthoring suiteは **83 checks PASS**（`Artifacts/Authoring-20260914-020044-96ef375d298447a7a1530b960c1a182b/report.json`）、その出力を渡したUnity **2022.3.22f1** Bridgeも **passed**（`Artifacts/BridgeReceiver-20260914-020118-207-eed1de1948f8432e8637a35c29baa87d/bridge-report.json`）。これは公開fixtureでの再現可能な候補ビルド証拠であり、実RadDollV3 sceneのfit・貫通・VRChat内見た目・負荷受入は別カードとして残す。

## 2026-09-13 NF-V1-09/10 semantic texture contract

`MaterialTextureSlot`／`MaterialTextureSet`を追加し、normal／metallic-roughness画像について、semantic、PNG/JPEG bytes、色空間（linear）、channel契約、UV set、normal scale、glTF samplerをtyped payloadとして保持するようにした。GLB取込は埋め込み画像と安全なローカル相対URIを解決し、native graph binaryは画像をblobとして所有してSave/Openする。GLB出力はnormalTextureとmetallicRoughnessTexture、samplerを再生成し、Coreで画像bytes・channel前提・sampler・native roundtripを確認した。通常のMaterial Bakeはこの情報を落とさないよう事前拒否する。

Unity BridgeはStandard shaderへnormal mapとmetallic-roughness mapを割り当て、glTFの`B=metallic/G=roughness`をUnityのmetallic-gloss `R=metallic/A=smoothness`へ変換し、生成textureをownership cleanup対象へ含めるところまで実装した。Coreは **491 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-9d9d5d3cfac64004ae975127d5e0c89e`）、Windows Player `Builds/SemanticTextureV2/NyaForge.exe` build成功（`Logs/build-player-20260913-233518-121.log`）、Authoring suite **82 checks PASS**（`Artifacts/Authoring-20260913-233528-55f3f2bb6fa2463596b1c4af10ecc0b0/report.json`）、Unity **2022.3.22f1** Bridge **14 checks PASS**（`Artifacts/BridgeReceiver-20260913-233832-795-33b73c74efd24d73bdf00a523751f803/bridge-report.json`）。Bridge内でsemantic map package適用、normal scale、MR channel conversion、ownership cleanup、Undoによるnormal／MR texture復元まで確認したが、実RadDollV3 scene表示・実VRChatは未受入として残す。occlusion/emissive画像、専用paint/bake、全shader一致も対象外。

## 2026-09-13 NF-V1-08 カフ試作テンプレート

`PolygonPrimitives.Cuff`とWorkbenchの「手首カフ形状を追加」を追加した。内外面・上下キャップを持つ閉じた低ポリシェル（既定32分割、128編集頂点、128 quad）で、既存のPolygonEdit、頂点移動、native Save/Open、単体Bakeへ接続している。Coreは **493 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-1a4ae1d1c3f74cbea9a742fe8707b919`）、Windows Player `Builds/CuffTemplateV2/NyaForge.exe` build成功（`Logs/build-player-20260913-234616-199.log`）、Authoring suite **83 checks PASS**（`Artifacts/Authoring-20260913-234636-1f3341077d154d6ba757a0ed6a853e78/report.json`）。同じPlayerへprivate一時RadDollV3 VRM（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`）を指定したsuiteも **87 checks PASS**（`Artifacts/Authoring-20260913-235344-c86ee71d2d9f474ca4351e8177b9878f/report.json`）、その出力を渡したUnity **2022.3.22f1** Bridgeも **14 checks PASS**（`Artifacts/BridgeReceiver-20260913-235804-254-9102b0a2b95d496cb31f350808aab159/bridge-report.json`）。これは制作開始点と実モデル自動smokeの証拠であり、実RadDollV3 sceneへのfit・weight・貫通確認、実EditorWindow操作、実VRChat内の見た目・負荷、販売品質を証明しない。

## 2026-09-14 semantic texture GUI保持

材質パネルのscalar変更が既存のnormal／metallic-roughness slotを新しい`MaterialParameters`へ引き継ぐよう修正し、保持中のMIME、encoded byte数、UV set、normal scale、`B=metallic/G=roughness`契約をパネルへ表示するようにした。既存のmaterial GUI回帰を含むWindows Player `Builds/SemanticTextureGuiV1/NyaForge.exe`は **83 checks PASS**（`Logs/build-player-20260913-235951-041.log`、`Artifacts/Authoring-20260914-000015-f3a9f6cc77494025a7486cf07d36c707/report.json`）。これはsemantic mapの編集UIや実shader外観を完成扱いするものではなく、取込済みmapの保持と確認表示を追加した段階である。

## 2026-09-14 semantic texture authoring preview

材質のsemantic normal／metallic-roughness slotをWorkbenchのPBR previewへ接続した。normalはlinear画像として`_BumpMap`へ設定し、normal scaleとUV0/UV1を反映する。metallic-roughnessはglTFの`B=metallic / G=roughness`をUnity Standard shader用の`R=metallic / A=smoothness`へ変換して`_MetallicGlossMap`へ設定する。glTF samplerのwrap/filterもUnity previewへ反映する（Unityの単一wrapMode制約によりWrapTは保持値をそのまま描画できない）。

所有textureは`BaseColorSurface`のmaterial lifecycleと同じ寿命で破棄し、semantic slotを保持したscalar材質編集後も再生成する。UV1、normal、MRのshader keywordを含むWindows Player `Builds/SemanticPreviewV3/NyaForge.exe`をビルドし、1pxのnormal／MR fixtureによるGPU preview、MR channel変換、Undo/Redoを含むAuthoring suite **PASS**（83 checks、`Artifacts/Authoring-20260914-002103-0fbd93211f7f4541a6ed90fe1ce6c7b3/report.json`、画面 `authoring.png`）を確認した。

これはsemantic mapの実RadDollV3表示、tangent品質、occlusion/emissive、専用paint／bake、実VRChatの見た目を受入した記録ではない。NF-V1-09/10の残作業として、実sceneで明暗環境・UV・tangent・出力一致を目視確認する。

## 2026-09-14 semantic texture GUI import

材質パネルへWindows Explorer選択とパス適用を追加した。normal／metallic-roughnessそれぞれをPNG/JPEG（16 MiB以内、8192px以内）として検査し、UV0/UV1とnormal scaleを指定してtyped slotへ取り込む。既存のもう一方のmap、scalar値、base color Paintを保持し、native Save/Openで画像bytesを再読込できる。semantic mapがある状態では単体Material Bakeを拒否する既存契約を維持し、GLB／graph exportへ案内できる状態を保つ。

1pxのGUI fixtureをExplorer相当のパス適用経路で取り込む回帰を追加した。材質切替時も保存済みUVとnormal scaleをパネルへ同期する。Windows Player `Builds/SemanticTextureGuiV7/NyaForge.exe`、Authoring suite **PASS**（83 checks、`Artifacts/Authoring-20260914-003943-0b9d326e06764675816a9e1ec792f9ce/report.json`）で、normalのUV1/scale、MR slot、scalar変更後の保持、native Save/Openを確認した。private RadDollV3を指定した全mesh import smokeも**89 checks PASS**（`Artifacts/Authoring-20260914-003430-c52a6bd922294ac48cdaefed89760177/report.json`）、同出力のUnity **2022.3.22f1** Bridgeも**14 checks PASS**（`Artifacts/BridgeReceiver-20260914-003656-365-b7fe74b7b31d48438c7fbfa4ad067559/bridge-report.json`）。選択中の作品が変わった場合は画像指定を無効化する。

## 2026-09-14 private RadDollV3 import/Bridge recheck

private一時素材 `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm` を `Builds/SemanticPreviewV2/NyaForge.exe`へ指定し、単体取込と全mesh instance取込、EditMesh、native Save/Openを再確認した。全mesh経路は10 objects、bone 171、morph 35を保持し、Authoring suite **89 checks PASS**（`Artifacts/Authoring-20260914-001523-4437ec156ea94502aa336a06b3083dcd/report.json`）。続けて同reportの出力をUnity **2022.3.22f1** Bridgeへ渡し、**14 checks PASS**（`Artifacts/BridgeReceiver-20260914-001837-446-4ccca3e524ba4ababe56a253461f64c4/bridge-report.json`）。

このVRMは取込時に`MATERIALS_NOT_RETAINED`と`EXTENSIONS_PARTIAL`を診断しており、semantic textureの実モデル表示を証明しない。private素材はpublic repositoryへ追加していない。実EditorWindowのマウス操作、RadDollV3 sceneへの衣装fit／weight／貫通、実VRChatは引き続き未受入とする。

## 2026-09-13 持込Windows v1案の照合結果

### 採用する点

- 既存アバターへ衣装だけを渡す経路を先に固める。
- Polygonの造形結果をskin用派生graphへ確定し、fit・weight・pose・保存再開を一周させる。
- 材質はsemantic slotを先に決め、normal／metallic-roughnessをbase colorと別に検証する。
- 実マウス、Unity受け取り、実VRChatをCore／Player／合成Bridgeと別の受入gateで記録する。

### 遠回りとして止める点

- 実SDKが未導入の間に、VRChat対応をPASS扱いしない。`Tools/Test-NyaForgePhysBonesSdk.ps1`のunavailable記録を維持する。
- 全身キャラ造形、FBX／BLEND parser、完全VRM round trip、全shader、Quest／macOS対応をv1の途中へ持ち込まない。
- 既に実装・合成受入済みの`NF-V1-03A`（package、BoneId割当、ownership marker、base-color復元）を作り直さない。
- テスト件数や同じfixtureのbuild反復を進捗の代用にしない。外部受入と手動操作を優先する。

### 次の実作業カード

1. **環境・受け取り契約（NF-V1-01/03）** — [Windows-v1-Environment.md](docs/Windows-v1-Environment.md)へ対応Unity／UniVRM／SDK／shader／OSを固定し、実SDK未導入はBLOCKEDと記録する。実アバターをpublicへ置かない。
2. **実アバター初回適用（NF-V1-03A）** — private RadDollV3 sceneで全BoneIdを明示割当し、衣装packageの初回適用・再起動後の確認を行う。失敗時はsceneを変更しない。
3. **一着の手動完走（NF-V1-04〜08）** — Polygon派生、UV／paint、確定、範囲限定fit／weight、pose、Undo、Save/Openを実マウスでカフ1点に適用する。
4. **材質の意味契約（NF-V1-09/09A）** — base color／alphaは現行実装を証拠化し、normal／MRの色空間・channel・UV・sampler・縮小・所有を文書で固定してから実装する。
5. **受け渡し更新（NF-V1-11〜13）** — 同一targetの衣装2点＋小物1点、更新／削除／取消／競合を合成fixtureで回帰し、実Unity受入へ持ち込む。

この順序なら、環境待ちの外部検査を正直にBLOCKEDとして保持しつつ、現在の編集基盤を使って制作一周へ進める。v1の出荷判定はNF-V1-15/16まで完了するまで行わない。

## 2026-09-13 NF-V1-01 環境マニフェスト

[Windows-v1-Environment.md](docs/Windows-v1-Environment.md)を追加し、現行Player Unity **6000.4.3f1**、Bridge Unity **2022.3.22f1**、Core runtime **10.0.202**、OS/GPU、package版、fixture hashと証拠場所を固定した。`com.vrchat.*`未導入でPhysBones probeが`unavailable`のため、実SDK／実VRChatはBLOCKEDのまま記録する。これは環境固定の完了であり、外部受入の完了ではない。

## 2026-09-13 ownership資産のUndo回帰

ownership markerが生成したmesh・material・base-color textureを、更新／明示削除時に同じUndoグループで破棄・復元するよう補強した。失敗時は即時片付け、外部Materialは所有対象に含めない。Unity **2022.3.22f1**の合成Bridgeで、3資産を一括削除後に1回のUndoで生成objectと全資産が復元されることを確認した（**PASS**、`Artifacts/BridgeReceiver-20260913-230234-754-918978ce45d44e5385c2a4c9aa0f4046/bridge-report.json`）。実EditorWindowをマウス操作した更新／削除受入は未実施として残す。

## 2026-09-13 同一avatarへの複数package管理

`NyaForgeSkinnedClothingBinding`を「1 packageにつき1 component」とし、同じavatar rootへ衣装A・衣装B・小物を順番に適用してもidentity／生成object参照を上書きしないようにした。GUIは選択中packageのObjectIdに一致するbindingだけを読み書きし、更新・削除対象を限定する。Unity **2022.3.22f1**の合成Bridgeで、同一avatar上の2 bindingが独立して一致し、異なるObjectIdを互いに更新対象としないことを確認した（`Artifacts/BridgeReceiver-20260913-230652-034-f7eb2e45e6a44017973e7e1349ba502a/bridge-report.json`）。実EditorWindowで複数packageをマウス操作する受入は未実施。

## 2026-09-13 現行Playerのprivate RadDollV3回帰

`Builds/ClothingOwnershipV1/NyaForge.exe`へprivate一時RadDollV3 VRMを指定し、全mesh取込→EditMesh頂点編集→native Save/Open→標準skinned GLB／VRM出力まで再実行した。**91 checks PASS**（`Artifacts/Authoring-20260913-225717-54b8d7c9f0bc4ed8a7fa5c93574b5656/report.json`、画面 `authoring.png`）。同じPlayer reportと衣装packageをUnity **2022.3.22f1** Bridgeへ渡し、package／receiver／ownership／削除参照回帰も **PASS**（`Artifacts/BridgeReceiver-20260913-230018-288-e08b8d8aba4a41f99f34f6a0cd2f34b1/bridge-report.json`）。

これは最新コードのファイル経路と合成Bridgeの証拠で、EditorWindowの実マウス操作、Undoで削除を復元する実scene、実VRChat SDK／Build & Test／他者視点の受入を完了扱いしない。private素材はpublic repositoryへ追加していない。

## 2026-09-13 NF-V1-03A 最小Unity skin衣装receiver

`UnityBridge/Editor/SkinnedClothingReceiver.cs`を追加した。Coreの`MeshData`、`RestTransform`、`SkeletonDefinition`、`SkinBinding`を受け取り、指定した`avatarRoot`の子へ`SkinnedMeshRenderer`を一原子操作で生成する。頂点はrest transformを一度だけ適用してavatar root localへ変換し、bindposeは`bone.worldToLocalMatrix * avatarRoot.localToWorldMatrix`で作る。BoneId→Transformの完全な明示mapを要求し、階層外の骨、欠落map、binding不整合、material slot不一致を生成前に拒否する。Unity v1の`BoneWeight`へ黙って切り詰めず、5以上のinfluenceは`SKIN_INFLUENCES_UNSUPPORTED`で停止する。失敗時は生成したGameObject、Mesh、temporary Materialを片付け、成功時はUndoへ登録する。単一mesh/skin GLB向けの`ApplyGlb` convenienceも追加した。

設計と呼出例は[`docs/UnityBridge-Skinned-Receiver.md`](docs/UnityBridge-Skinned-Receiver.md)へ固定した。Unity **6000.4.3f1** Windows Player `Builds/SkinnedReceiverV3/NyaForge.exe`の最終compile/buildは成功（`Logs/build-player-20260913-220242-336.log`、`NYAFORGE_PLAYER_OK`）。Authoring suiteは前版で **PASS**（`Artifacts/Authoring-20260913-220106-064b5f6684a94bb79259170c49bf156d/report.json`）。さらにUnity **2022.3.22f1**の使い捨てBridgeで合成2-bone avatarへ実適用し、親子配置、BoneId map、bindpose、4-slot BoneWeightを確認（`Artifacts/BridgeReceiver-20260913-220207-563-5bcbcc9c717347848520be7fa7248fc5/bridge-report.json`）。Coreは **488 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-3d76f0258d544e368aa13b8021adf2a9`）。この証拠は合成Unity receiverまでであり、実アバターの骨map、VRChat SDK、実VRChat内の見た目・挙動は未確認のまま残す。

## 2026-09-13 NF-V1-03A 衣装専用package

GLB単体ではstable BoneIdを受取側へ安全に渡せないため、`SkinnedClothingPackage`（profile `skinned-clothing-v1`）を追加した。packageは衣装だけの`clothing.glb`、Coreの`SkeletonDefinition`、`SkinBinding`、document/object/graph/state hashをmanifestへ記録する。生成前にGLBのmesh content/topology/頂点・三角形数をsidecarと照合し、stagingを読み直してから原子公開する。受取時もmanifest・各payload hash・GLB再読込・skeleton/binding topologyを検査し、不一致ではsceneを変更しない。`SkinnedClothingReceiver.ApplyPackage`はこの検査済みpackageを明示BoneId mapで`SkinnedMeshRenderer`へ適用する。

Workbenchへ「選択衣装をskin packageで出力」を追加し、参照avatarを同梱しない単一衣装GLB＋sidecarの出力導線を用意した。Core回帰へpackage roundtripを追加し **489 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4aa72dc8e4034d658d340a611ead335e`）。Unity **6000.4.3f1** Player `Builds/ClothingPackageV1/NyaForge.exe` build成功、Authoring suite **PASS**（`Artifacts/Authoring-20260913-221015-2432c87b173840109d91c173ced1204a/report.json`）。受け取り側には`Tools/NyaForge/Import Skinned Clothing Package...`を追加し、manifestのファイル選択、全BoneIdの明示割当、事前診断、`NyaForgeSkinnedClothingBinding`への保存、同じObjectIdだけの管理対象更新をGUIから行えるようにした。package内のGLB材質情報から、base-color画像、PBR係数、alpha、emissionをStandard材質へ復元する経路も追加した。`Builds/ClothingTextureV1/NyaForge.exe`のWindows Player buildとAuthoring suite **PASS**（`Artifacts/Authoring-20260913-223900-d52a84998d2d4eb6901a910e1d8b62bc/report.json`）。Bridge検証へpackage manifestの任意入力、ownership marker、embedded base-color texture回帰を追加し、Unity **2022.3.22f1**でpackageのmanifest・GLB・skeleton・binding hash検証後に`ApplyPackage`が合成avatarへSkinnedMeshRendererと材質を生成するところまで **PASS**（`Artifacts/BridgeReceiver-20260913-223822-634-4e7652a462e042598749d02cc52ce364/bridge-report.json`）。これは合成fixtureでのpackage/receiver証拠で、実RadDollV3を対象にしたBoneId map・ownership更新・VRChat内表示、normal/MR等の追加画像slotは未受入として残す。

## 2026-09-13 skinned clothing受け取りGUIの回帰

`SkinnedClothingPackageWindow`と`NyaForgeSkinnedClothingBinding`を含む最新mainを、private一時RadDollV3 VRMで全mesh取込→頂点編集→native Save/Open→skinned GLB／VRM出力まで再確認した。Windows Player `Builds/SkinnedClothingUiV1/NyaForge.exe`のAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-222748-9e3c43e5f65f4973adcc7c55265e9c8a/report.json`）。Coreは **489 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0e7fef07df9f44388c5cc53b4c7559bd`）。同じcheck directoryとpackage manifestをUnity **2022.3.22f1** Bridgeへ渡し、receiver/package/ownership回帰も **PASS**（`Artifacts/BridgeReceiver-20260913-223057-746-4081b1618e9241d890e6e365274348c5/bridge-report.json`）。private素材はpublic repositoryへ追加していない。

この回帰はPlayer／Core／合成Bridgeの自動証拠であり、EditorWindowを実マウスで操作した受入、実RadDollV3 sceneへのBoneId割当、ownership更新のUndo、VRChat SDK／Build & Test／実機表示は外部受入として残す。

## 2026-09-13 package生成物のownership markerと材質回帰

packageから生成したSkinnedMeshRendererへ`NyaForgeSkinnedClothingManaged`を付け、NyaForgeが生成したmesh・材質・デコード済みbase-color textureの所有範囲を記録できるようにした。callerが渡した外部Materialは所有対象へ含めず、受け取り失敗時に生成資産を片付ける。Bridgeのpackage回帰は **PASS**（`Artifacts/BridgeReceiver-20260913-224614-874-19b414876eb242c6bb3b2f92469a6fd6/bridge-report.json`）。同marker込みのUnity **6000.4.3f1** Player `Builds/ClothingOwnershipV1/NyaForge.exe`でprivate RadDollV3全mesh取込→頂点編集→native Save/Open→skinned GLB／VRM出力も **PASS**（`Artifacts/Authoring-20260913-224324-078ee4d8dc904e53909e5a232da12a62/report.json`）。

## 2026-09-13 管理対象衣装の削除経路

`SkinnedClothingPackageWindow`へ「管理対象の衣装を削除（Undo可）」を追加した。選択中packageと同じObjectIdを持ち、avatar root直下にある生成objectだけを対象にし、bindingの生成object参照をUndoへ記録してから削除する。別衣装・avatar外のobject・未管理objectは停止する。`NyaForgeSkinnedClothingBinding.ClearGeneratedObject`と合成Bridge回帰で、削除後に`MatchesObject`が偽になることを確認した（Unity **2022.3.22f1**、`Artifacts/BridgeReceiver-20260913-225449-604-7f145589ddb54e9d9d0ee2e53e8359af/bridge-report.json`）。EditorWindowの実マウス操作とUndoで復元する実scene確認は未受入として残す。

## 2026-09-13 NF-V1-06 / 05 のCore接続

fitと表面weight転送へ、avatar側の対象三角形を明示的に限定する入力と、転送時の最大距離検査を追加した。三角形番号は`MeshData.Submeshes`を平坦化した順で、選択範囲を別の面へ暗黙に広げない。範囲外・空選択は事前に拒否し、距離超過は`SURFACE_FIT_DISTANCE`／`WEIGHT_TRANSFER_DISTANCE`で停止する。従来の全表面APIは互換overloadとして維持した。

回帰テストを追加し、離れた2面のfixtureで「選択面へ投影・選択面からのweight転送」と「誤った面を選んだ場合の無変更失敗」を確認した。Coreは **487 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f8e876f07220401a87ec2dd99e446230`）。これはCoreの範囲限定土台であり、Workbenchの面選択UI接続、実RadDollV3での服一周、Unity/VRChat受入は未完了のまま次カードへ残す。

## 2026-09-13 NF-V1-02A / 04 Polygon→skin派生graph

`AccessorySkinMaterializer`を追加し、`PolygonSource→PolygonEdit`の造形結果を、元のPolygon graphを変更せず新しいgraph IDの`MeshSource→EditMesh`へ一操作で派生できるようにした。評価済みの頂点・UV・サブメッシュを保ち、既存のappearance node（Paint、StandardMaterial、AssignMaterial(s)、Output）を引き継ぐ。Paintは同じ画像をpolygon domainなしのimmutable imageとして再接続し、Polygon編集後のUV配置を保つ。派生後に既存の`AccessorySkinBindingAdapter`でavatar skeleton・Pose・SkinDeformを接続し、元graph ID/hashを結果へ返す。

形状を後段で暗黙に焼かないため、複数PolygonSource、後段の幾何modifier、LayeredPaint、既存rig/attachmentは明示的に拒否する。`EditMesh`からPaintとOutputへ複数のmesh downstreamがあるappearance graphにもskin deformationを挿入する。Coreで元graphのhash不変、派生graphのskin/pose、paint画像・出力geometry一致、2 objectのnative Save/Openを確認した。Coreは **488 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-7b21bff3e1a24040bc32e17b9835b820`）。Workbenchの派生操作ボタン、Undoを含む一操作command、実Unity receiverへの適用は未接続で、NF-V1-03Aの後続に残す。

Unity **6000.4.3f1** のWindows Player `Builds/PolygonMaterializeV1/NyaForge.exe`をビルドし、既存Authoring suiteを **PASS**（`Artifacts/Authoring-20260913-214847-d03fbeaf4bde4378abdac6d5078db5b6/report.json`、画面`authoring.png`）で確認した。これは新しいPolygon派生ボタンを実マウスで押した受入ではなく、既存Player導線の回帰確認として扱う。WorkbenchのPolygon派生操作は、次の手動UI受入で元object保持・新object選択・Undo・Save/Openを確認する。

派生元を再開後も追跡できるよう、`mesh.derived-source` typed metadata nodeへ元graph IDとmaterialization時のgraph hashを保存する。Graph binary、command reader、inspection、skinned GLB許可リストへ接続し、派生結果のnative Save/Openで同じ出自情報を確認した。Coreは **488 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-36a1d1fd2c2041148c237ffd355ca49f`）。Unity **6000.4.3f1** Player `Builds/DerivedProvenanceV1/NyaForge.exe`もビルド成功（`Logs/build-player-20260913-215401-478.log`）。

## 今回のレビュー結論

方向性は採用。原案の02→後続10/12→02という受入依存の逆転を02A/02Bへ分離し、未実装のskin衣装receiverを03Aとして前倒しした。1024pxへ縮小される画像の原本/作業/出力契約を09Aへ追加。12週間・週20〜25時間は未合意の仮定として採用しない。全身制作、FBX、完全VRM、全shader、共有資源の完全統合は既存backlogへ残し、今の衣装一周に必要な接続を先行する。

検証: 持込全文、現行コード、既存SDK互換記録、公式Unity/VRChat/glTF資料を照合。今回は文書のみ更新し、Core486件・Player・実モデルPASSは既存記録として参照した。新規テスト/build/VRChat実行や外部アップロードは行っていない。

## これまでの実装・検証記録

# 2026-09-13 製品名・公開リポジトリ名の更新

公開リポジトリが `moe-charm/NyaForge` から `moe-charm/NyaEkaki3D` へ変更されたため、ローカル `origin` を `https://github.com/moe-charm/NyaEkaki3D.git` へ追従させた。README、文書入口、開発計画、設計v1/v2の表示名と対象URLを **Nya Ekaki 3D** に更新した。既存の `NyaForge` は package ID、namespace、スクリプト、実行ファイル、LocalLow設定パスに残し、互換性を維持する。履歴文書と内部コード名は過去の実装証跡として変更していない。
# 2026-09-13 GLB export report graph identity

GLB `export-report.json` の `objects[]` に、native graph objectへ追跡できる `graphId` を追加した。static/skinnedの両profileで出力対象のgraph IDを保持し、sourceDiagnosticsのgraphIdと同じreport内で直接照合できる。CoreのGLB export回帰へreportのgraphId確認を追加し、`dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore` は **486 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0247bad405b7437fb76fa5e0f163a586`）。この変更は出力追跡性を改善するもので、実Unity SDK・実VRChat内の外観/挙動受入とは別境界である。
# 2026-09-13 PhysBones SDK availability probe

`Tools/Test-NyaForgePhysBonesSdk.ps1 -UnityProjectPath .`を読み取り専用で実行した。現行NyaForge Unity projectには`com.vrchat.*` dependencyとVRChat PhysBones componentファイルがなく、statusは`unavailable`。レポートは`Artifacts/PhysBonesSdkProbe-20260913.json`へ保存した。NyaForge自身のfixture／BridgeをSDK検出と誤認せず、実SDK受け取りはSDK導入後に再実行する境界を確認した。合成Bridgeとpublic Playerの動作は既存証拠を維持する。
# 2026-09-13 GLB resource reader Player acceptance

Windows Player `Builds/ResourceReadbackV1/NyaForge.exe`へ、出力後resource readbackを接続した版をビルド。通常Authoring suiteは **PASS / 82 checks**（`Artifacts/Authoring-20260913-205314-2f2e356e7262431b9caa43b1092df3f1/report.json`）で、標準GLB／skin出力の公開前再読込を含む既存導線を確認した。`-GlbExportMcp` suiteも **PASS / 82 checks**（`Artifacts/Authoring-20260913-205221-82004a3fb3534061ac7cce0657f3c04c/report.json`）で、MCP responseの`validation.glbResourceReaders=passed`を確認した。Unity **2022.3.22f1** Bridgeは **PASS**（`Artifacts/BridgeReceiver-20260913-205352-266-729bed7bb7184a7197c940242d0824fc/bridge-report.json`）。

最初の180秒試行はPlayer終了待ちがタイムアウトしたが、reportはPASSを書いていた。長めの再試行では正常終了したため、最終証拠は後者を採用する。実VRChat内の外観・挙動、実マウス／DPI個体差、完全VRM意味情報は引き続き別受入境界。
# 2026-09-13 GLB resource readback validation

GLB公開前の検証をscene inventoryだけから、同じ再開経路で使う`GlbImporter`／`GlbSkinImporter`まで拡張した。出力内のmesh resourceとmesh/skin組合せを重複なく再読込し、instanceのないmesh resourceも検査する。読み手が開けないaccessorやskinを構造上有効なGLBとして公開しない。reportの`validation.glbResourceReaders=passed`へ記録し、既存のstaging原子公開とVRM metadata readbackは維持した。

Coreは **486 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-586f32e241be43a7a9de93e1c4721e5b`）。Unity／VRChat実機の描画・挙動は別受入境界で、reader再読込はNyaForgeのresource互換性を示す検査に限定する。
# 2026-09-13 c4a2748 feedback regression hardening

提示レビューのP1（VRMの実node参照、複数skinの骨順差、旧形式VRMへ通常GLBを追加した移行）を現行mainへ再確認し、既存の実装に加えて、VRM 1 exportのnode-map回帰を表情morph bindとSpringBone jointまで拡張した。humanoid・表情・揺れが同じ実出力nodeへ解決され、骨名やmesh nodeを取り違えないことを検証する。`ProjectActions`のstable authored tokenコメントも、実writer順序に依存しない説明へ修正した。

Coreは **486 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-d2e169120fe6481bad86022b3870ec83`）。Unity／VRChat実機の外観・挙動は未確認で、完全VRM意味情報の保持も引き続き初期profileの範囲外。
# 2026-09-13 MCP GLB export snapshot response

`forge_export_glb`の成功レスポンスを、出力report由来のdocument ID・revision・state hashへ変更し、`sourceDiagnosticCount`と`validation.glbSceneInventory`も返すようにした。出力後のworkspaceを読み直して別snapshotを返す経路を避け、AI側がその場でreport検証結果を確認できる。Unity **6000.4.3f1** Player `Builds/McpSnapshotV1/NyaForge.exe`の`-GlbExportMcp` Authoring suiteは **PASS / 82 checks**（`Artifacts/Authoring-20260913-203615-26ca37caffd847b1bae6286ef590f186/report.json`）。
# 2026-09-13 VRM report snapshot pin

VRM1 exportではGLB生成・package・metadata再読込の後、report作成直前にdocument ID／revisionを再確認するようにした。workspaceが変わっていた場合は`REVISION_CONFLICT`でstagingを破棄し、本体とreportのsnapshot不一致を防ぐ。Core **486 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-229a3ea3029d4c10b4d52b408b349140`）。Unity **6000.4.3f1** Player `Builds/SnapshotPinV1/NyaForge.exe`のAuthoring suiteは **PASS / 82 checks**（`Artifacts/Authoring-20260913-203351-90824516204b4e3ebb61385f086716a9/report.json`）。ビルドログは`Logs/build-player-20260913-203329-835.log`。
# 2026-09-13 output readback validation

GLB出力は公開前に生成済みbytesを`GlbSceneInventoryReader`で再読込し、VRM 1.0出力はさらに`VrmMetadataReader`で`vrm1`を確認するようにした。失敗時はstagingを公開せず既存作品を変更しない。reportへ`validation.glbSceneInventory=passed`、VRMでは`validation.vrmMetadataReader=passed`も記録する。Coreは **486 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-911e5ba9bbab4a3592278f8879567019`）。`Builds/OutputValidationV1/NyaForge.exe`のprivate RadDollV3 VRM suiteは **89 checks PASS**（`Artifacts/Authoring-20260913-203001-70c5a66f9f7f40aaa8bba6e85a42498d/report.json`）で、標準GLB・衣装付きGLB・VRMの全reportにreadback validationを確認した。ビルドログは`Logs/build-player-20260913-202929-488.log`。
# 2026-09-13 VRM export source diagnostics report

`VrmExportService.ExportVrm1`にも、現行graphに対応するnative import diagnosticsを`sourceDiagnostics`として同梱した。GLB内部の一時reportを削除しても、VRM成果物だけでsource hash／mesh・skin・node locator／保持不可理由を追跡できる。Coreは **486 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f2418aa50b504420a11a771ea515f1ba`）。`Builds/DiagnosticsReportV2/NyaForge.exe`でprivate RadDollV3 VRMの取込→編集→Save/Open→GLB／VRM出力・再読込を **89 checks PASS**（`Artifacts/Authoring-20260913-202425-f2aef0e5518240d2b5f8d3cb81455ff3/report.json`）し、標準GLB／衣装付きGLB／VRMの各reportに対応するsource diagnosticsが残ることを確認した。private素材はpublic repositoryへ追加していない。
# 2026-09-13 GLB export source diagnostics report

GLB export reportへnative `import-diagnostics.nyaforge.json` のsource diagnostic recordsを同梱した。source hash・mesh/skin/node locator・保持不可理由を`sourceDiagnostics`へコピーし、出力本体と同じstagingで確認できる。DiagnosticsReportV1のWindows Playerをビルドし、Authoring suiteは **PASS / 82 checks**（`Artifacts/Authoring-20260913-201906-0b40216d38fe4ca3aa8f495abd4c8a6e/report.json`）。private一時RadDollV3 VRMでは **89 checks PASS**（`Artifacts/Authoring-20260913-201938-1c022050469e4a80a0b25347cb17a4f5/report.json`）し、標準GLB reportの`sourceDiagnostics` 1件、衣装付きGLB reportの2件を再読込確認した。VRM1 reportはprofile制限を既存の`limitations`へ分離している。Player build logは`Logs/build-player-20260913-201842-396.log`。
# 2026-09-13 surface fit metrics real RadDollV3 recheck

`Builds/FitMetricsV1/NyaForge.exe`へprivate一時RadDollV3 VRMを指定し、取込→EditMesh頂点編集→native Save/Open→標準skinned GLB出力・再取込→初期VRM 1.0 package出力・metadata再読込を再確認した。Authoring suiteは **89 checks PASS**（`Artifacts/Authoring-20260913-201357-6d1e26f92eb5478d8b0123685f9b7103/report.json`、画面 `authoring.png`）。同成果物のUnity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-201553-781-4e63a53bcd2148eba8a8db1fbcd9d30e/bridge-report.json`）。private素材はpublic repositoryへ追加していない。
# 2026-09-13 surface fit quality metrics Player acceptance

Unity **6000.4.3f1** Windows Player `Builds/FitMetricsV1/NyaForge.exe` を再ビルドし、Authoring suiteを **PASS / 82 checks**（`Artifacts/Authoring-20260913-201218-cd4cc58d2e6c43928975c3fdbe5fb11/report.json`、画面 `authoring.png`）で確認した。avatar＋static GLB衣装のsurface fit後ステータスが移動頂点数・最大投影距離・最大移動量を含むこと、距離超過時の文書不変、既存のUndo／Save/Open／GLB出力導線が継続して通ることを回帰した。ビルドログは`Logs/build-player-20260913-201154-323.log`。
# 2026-09-13 surface fit quality metrics

`MeshSurfaceFit.Project`を追加し、既存の`ProjectPositions` APIを維持したまま、fit結果に移動頂点数、最近表面までの最大／平均距離、最大／平均ワールド移動量を付与した。Workbenchのfit完了ステータスにも移動頂点数・最大距離・最大移動量を表示し、近接投影の結果を人が確認してからRig／poseへ進めるようにした。bounded距離・offset・原子commit・従来出力の互換性は維持している。

Coreは **485 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4e23717a9c774fecb8a7dae26997315e`）。`docs/Model-Interchange-Spec.md` と `docs/Authoring-Quickstart.md` に計測値の意味と確認手順を追記した。交差判定・体形morph・裏面誤吸着の自動解決ではなく、fit品質を可視化する受入補助である。
# 2026-09-13 feedback recheck: c4a2748 against current main c74c1f9

提示されたレビュー（基準 `c4a2748`）を現行mainへ再照合した。P1の3件は後続コミットと既存回帰で解消済み。VRM出力は`GlbExportNodeMap`の実node対応表をhumanBones／expression／Springへ渡し、複数skinはstable `BoneId`とskin単位の順序でJOINTSを出力し、旧形式sidecarへ通常GLBを追加する場合もgraph単位のsession tableへ移行する。

P2も現行実装で確認した。Undo/RedoはDocumentとProjectAttachmentsを同じ履歴で復元し、VRM metaは`licenseUrl=other`と`otherLicenseUrl`へ分離、source-skin表示cacheは現在のbinding hashをキーに含め、装着後の描画・面選択は`WorldPoints`／`RenderWorldPoints`の共通配置を使う。PhysBones SDK probeのUnity起動引数は空白を含むパスを要素ごとに引用する。

Coreを再実行し **485 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-2800c0038d4144b6b597618edc994b12`）。このフィードバックに対する追加コード変更は不要だった。実マウス／DPI、実VRChat内の見た目・挙動、完全なVRM意味情報保持は引き続き別受入境界とする。
# 2026-09-13 private RadDollV3 VRM1 export recheck

private一時RadDollV3 VRMをWindows Player `Builds/LegacyMigrationV1/NyaForge.exe`へ指定し、取込→EditMesh頂点編集→native Save/Open→標準skinned GLB出力・再取込→初期VRM 1.0 package出力・metadata再読込まで確認した。suiteは **89 checks PASS**（`Artifacts/Authoring-20260913-195925-52de0f979d3742aca92e4d1f02146594/report.json`、画面 `authoring.png`）。VRM出力ではsource GLBのtopology／vertex・triangle数とskeleton cardinalityを保持し、humanoid／expression／Springのnode参照を再読込できた。Unity Bridge（2022.3.22f1）も **PASS**（`Artifacts/BridgeReceiver-20260913-200125-711-53b841c4e2874885b86fb21d28f625cb/bridge-report.json`）。

これは現行 `Vrm1Humanoid` 初期profileの実モデル証拠であり、material bind、texture transform、LookAt、FirstPerson、MToon、animation、任意VRM拡張、実VRChat内の外観・挙動はprofileの制限として残る。private素材はリポジトリへ追加していない。

# 2026-09-13 private RadDollV3 real-model recheck

private一時フォルダに置いたRadDollV3のVRMを、Windows Player `Builds/LegacyMigrationV1/NyaForge.exe`へコマンドライン指定して、実モデルの取込→編集用メッシュ生成→頂点編集→native Save/Open→標準skinned GLB出力・再取込まで確認した。suiteは **86 checks PASS**（`Artifacts/Authoring-20260913-195054-b6babce0d79e4d9bba30bf86eac229cb/report.json`、画面 `authoring.png`）。privateモデル自体はリポジトリへ追加していない。

この結果は実モデルのファイル経路が通ることの確認であり、実マウス／DPI差、実VRChat内の見た目・PhysBones、全アバターの自動フィット品質は別受入境界として残す。

# 2026-09-13 review feedback recheck: 4fcd0fd

# 2026-09-13 legacy sidecar migration acceptance

旧形式移行の実行時経路をWindows Playerで追加確認した。旧形式VRM Aをlegacyの`Rig`／`Expressions`／`Springs` sidecarへ戻して保存・再読込し、metadataなしの通常skinned GLB Bを追加して再度保存・再読込した。最終状態はexpression／Springが1 graph、rigが2 graphのtableになり、AのmetadataがBへ混線しないことを確認した。

Player `Builds/LegacyMigrationV1/NyaForge.exe` のAuthoring suiteは **82 checks PASS**（`Artifacts/Authoring-20260913-194633-8ce9a748a18e4a6d8c0d2cc81c6e4c7c/report.json`、画面 `authoring.png`）。Unity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-194944-534-6c031a7108e84385b302a177704509d4/bridge-report.json`）。旧形式実ファイルの任意モデル網羅、実マウス／DPI差、実VRChat内の見た目は別受入境界とする。

# 2026-09-13 pasted review recheck: c4a2748 -> 267a734

今回のレビュー（基準 `c4a2748`）を現行HEAD `267a734`へ再照合した。指摘されたP1の3件は、先行実装と追加回帰で閉じた。

- VRMのhumanoid／expression／Spring node参照は、GLB生成時の`GlbExportNodeMap`を`VrmExportService`が解決して実nodeへ変換する。骨名が異なる15本のhumanoidを、BoneId順と著者順をずらしたfixtureで出力し、各humanBones nodeが同名の実nodeを指す回帰を追加した。
- 複数skinはstable `BoneId`とBoneId単位のinverse-bind／joint local対応を使い、skinごとに順序が`[Root, Child]`／`[Child, Root]`でもJOINTSが正しい骨名へ対応する回帰を追加した。同一rest定義は一つのskin resourceを共有する。
- 旧形式のexpression／Spring sidecarは、単一graphまたはgraphId付きlegacy rigでだけ割当て、複数graphで曖昧な場合は未割当のまま通知する。旧VRM Aへ通常GLB Bを追加して保存する経路はgraph単位tableへ移行する既存実装を正とし、混線を成功扱いにしない。

Coreは **485 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-80d67a19fece437b83c5a7c4907c28bf`）。追加回帰は実node参照と骨順入替えを含む。Windows Player／実VRChat内での最終見た目、実マウス／DPI、旧形式実ファイルの手動操作は別受入境界として残す。

提示されたレビュー（基準commit `4fcd0fd`）を現行HEAD `c4a2748`へ再照合した。P1の4件は現行mainで修正済みで、重複修正は行わない。

- 複数モデル保存／再読込のmetadata混線: `ProjectActions.OpenProject`がgraph IDごとのrig／expression／Springを解決・検証してからworkspaceを置換する。旧single-session形式で割当先が曖昧な場合は未割当として停止し、混線を成功扱いにしない。
- 16bit `JOINTS_n`誤読: `GlbSourceSkinImporter`は2 bytes単位でlittle-endian joint indexを復号し、8bit／16bit同値回帰を持つ。
- 出力時のinverse-bind／骨姿勢ずれ: `GlbExportService.AddSkeleton`は安定BoneId順でinverse-bindとjoint local matrixを同じ順序へ揃え、元の回転・拡縮をmatrixとして出力する。非平行移動joint local transform回帰を含む。
- ウェイト編集の表示不一致: `SourceSkinGraphAdapter`は評価済みの現在SkinBindをsource slotへ写像し、SkinDeform後に通常のgraphを再評価する。`SourceSkinDisplay`のcache keyも現在bindingを含む。

現行Core回帰は **477 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b7a3c6657ed24dbca9eb448dd8a1f022`）。回帰名には16bit JOINTS、source skin graph downstream、standard／extended skinned GLB、non-translation joint local transforms、rest-pose vertex edit、native Save/Openが含まれる。

レビューにあるP2のうち、静的GLB表示補正、装着後face picking、Frame二重変換、PhysBones curve clear、MCP camera metadata保持、負weight拒否は現行回帰で確認済み。共有mesh／morphの完全dedup、異なるsource skeletonの結合、実マウス／DPI、実VRChat／実SDK見た目、完全VRM意味情報は継続課題として残す。
# 2026-09-13 multi-skin GLB regression lock

同一sourceの共通安定 `BoneId` を持ちながらrest定義が異なる2つのskinを合成し、拡張skinned GLBがskin resourceを2つに分離してmesh nodeのskin参照を保持するCore回帰を追加した。各skinをmesh index／skin indexで再読込できること、異なるsourceの骨は従来どおり拒否することを同じ出力契約へ固定した。

Coreは **470 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-554fa4d20d504cc6bcd46a4d81ed0fb5`）。この回帰は標準GLB出力が同一hashのshared skinだけに依存せず、同一source内の複数skin資源を安全に保持することを検証する。実RadDollV3の10 mesh／9 skin出力は前項の `AllMeshRealV3` artifactで確認済み。

# 2026-09-13 latest Windows viewer navigation recheck

`Builds/FeedbackRecheckV1/NyaForge.exe`を1280x800で起動し、パック選択、キャンセル時の文書／履歴保持、named session保存・再開、未保存変更の破棄確認、不正パス保持、最近／確認セット／設定／制作画面への遷移、utility panelの折りたたみ、viewportの利用可能領域を再確認した。Navigation suiteは **PASS**（`Artifacts/Navigation-20260913-125208-90befd32b95d416690cf89634a21c1f1/report.json`）。`main.png`／`sets.png`／`settings.png`は1280x800で文字欠けなし、制作入口の表示領域も確保されている。

これは自動Playerナビゲーションと画面画像の証拠で、実マウス・DPI個体差・Explorerの実クリックは別手動受入境界とする。
# 2026-09-13 multi-skin GLB output recheck

前回の実RadDollV3全mesh取込では、native projectの10 objectとgraph単位rig sessionを保存・再読込できた。複数のsource skin resourceを一つのglTF skinへ無理に統合すると、同じsource由来でもrest定義やinverse-bindが異なるため出力時に停止する課題が残っていた。

`GlbExportService`を、同一 `Skeleton.ContentHash` なら従来どおり一つのskinを共有し、hashが異なる場合は全object間の共通安定 `BoneId` を検証してから、skeleton hashとinverse-bind行列の組合せごとに別skin resourceを生成する方式へ変更した。共通BoneIdがない異なるsourceの結合は従来どおり `GLB_SKIN_SHARED_SKELETON` で拒否する。skin resourceを分けてもmesh node・node affine・4/32 influence・source inverse-bindを保持し、native制作データは変更しない。

Coreは **469 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-62af12a2a9a6453dafdeb24bfdf9e2be`）。`Builds/AllMeshRealV3/NyaForge.exe`でprivate一時RadDollV3 VRMを `-ImportAllModel` 実行し、10 mesh instanceの編集可能化、native Save/Open、feature-preserving native export、複数skinを含む拡張skinned GLB出力まで **PASS**（`Artifacts/Authoring-20260913-132813-15c2514e54da41ef8f7a104643df84ab/report.json`、GLB `all-model-skinned-glb/model.glb`）。Unity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-133031-021-d7d97a099abe4b0fbb7fdd6d4b5eaf7c/bridge-report.json`）。private素材はpublic repositoryへ追加していない。

実VRChatでの見た目・挙動、異なるsourceのskeleton結合、共有mesh／morph参照、完全なtexture／animation／VRM拡張保持、標準VRM出力は別の受入境界として残る。

# 2026-09-13 pasted feedback recheck on current HEAD d440900

添付されたレビュー（基準 `0d1e957`）を現行HEAD `d440900`へ再照合した。レビューのP1（複数graph metadataの混線、source skin二重変形、skinned node affine、inverse-bind欠落、装着後クリックずれ）は、現行のgraph単位session、SkinDeform入力差し替え後の再評価、skinned affineの監査保持、元行列の標準／拡張GLB出力、`WorldPoints`統一で対応済み。P2（linear material／metallic既定値、局所material slot、装着先保持、PhysBones source hash、揺れUndo、GLB共通root・morph bounds、normal/tangent morph）も回帰へ含まれている。

現行HEADで `dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore` を再実行し **469 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-1d49dba164734af09d72b54d1fd85b6f`）。`Builds/FeedbackRecheckV1/NyaForge.exe` のprivate一時RadDollV3 VRM取込→EditMesh→native Save/Open→標準skinned GLB出力→再取込を含むWindows Authoring suiteは **82 checks PASS**（`Artifacts/Authoring-20260913-124716-f48f3153e47d44cb8b3d10c6ef2ff315/report.json`、画面 `authoring.png`）。同成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-124847-559-1100e92cfca44765b56a0a95d61954be/bridge-report.json`）。private素材はpublic repositoryへ追加していない。

レビュー本文にある未修正という判定は古いcommit基準のため、同じ修正を重ねていない。実マウス／DPI差／Explorer実クリック、実VRChat内の見た目・PhysBones、実SDK受入、自動fit・貫通修正、異なるskeletonの結合、完全な外部texture・animation・VRM拡張保持、標準VRM出力は引き続き別境界とする。
# 2026-09-13 real-model external base-color import recheck

private一時RadDollV3 VRMを元データのままリポジトリへ追加せず、base-color画像1枚を`textures/rad-doll-base.png`へ分離したGLB/VRM入力（相対URI、画像2,713,743 bytes、`bufferView`なし）を生成して、外部base-color経路を実モデルで確認した。Windows Player `Builds/ExternalImageRealV1/NyaForge.exe` の800x600 Authoring suiteは **PASS、全チェック完了**（`Artifacts/Authoring-20260913-122518-c59cfd830965477faae7b40c245413e1/report.json`、画面 `authoring.png`）。この経路では候補選択、外部画像付きVRM取込、既存の編集、native Save/Open、標準GLB出力まで通過した。同成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-122636-976-32e1c3b7b48b4226bca2ab84bbe41c9b/bridge-report.json`）。元VRM・分離画像・生成入力は`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-RealModelExternal-bdae4b667e6f41c792b056f8a23a9ab2`に置き、public repositoryへ追加していない。実VRChat内の見た目・挙動、標準VRM出力、完全な追加texture map/animation/VRM拡張保持、自動fit・貫通修正、実マウス/DPI差は引き続き未検証境界とする。

# 2026-09-13 current acceptance recheck

現行HEADのCore回帰を再実行し、**469 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-72e7c3c8bf884aa8bf08783a10569021`）。`Builds/ExternalImageRealV1/NyaForge.exe`の1280x800 Navigation smokeも **PASS**（`Artifacts/Navigation-20260913-123128-8566e55139864513997702cc86995cb1/report.json`）で、パック選択、キャンセル時の状態保持、確認セット、設定画面、制作画面への遷移を再確認した。`main.png`／`sets.png`／`settings.png`で文字欠けのない表示を確認した。Computer Useのネイティブアプリ列挙はこの環境で`apps: []`かつPlayer起動API未提供だったため、実マウス／DPI差の受入は自動画像・UI smokeと分離して未検証のままとする。レビュー対応と実モデル外部画像経路の証拠は前項へ記録し、次は実SDK受け取りまたは共有参照の仕様化へ進む。

# 2026-09-13 encoded external image URI hardening

外部base-color画像URIのパーセントエンコードを復号してから相対パス検査するようにした。`%2E`などの通常のファイル名表記を受け入れ、復号後の絶対パス・remote URI・NUL・`..`によるモデルフォルダ外への脱出は従来どおり拒否する。Coreは **469 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b8622a14fdb142c39010cbe5384e2ddc`）。Windows Player `Builds/UriV1/NyaForge.exe`の800x600 Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-123450-fc13e67f9c3e4363bd1c0caf022fdc99/report.json`）、Unity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-123523-876-0e11abb8f3a742218f2efb42f0cbc413/bridge-report.json`）。

# 2026-09-13 bounded external image read and real-model recheck

外部画像の読み込みを16MiB上限付きFileStreamへ変更し、読み込み中のファイル差し替えや権限／I/O失敗を未処理例外にせず、`IMAGE_BUDGET_EXCEEDED`／`EXTERNAL_RESOURCE_MISSING`／`EXTERNAL_RESOURCE_UNAVAILABLE`として返すようにした。Core回帰は **469 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-850b459bf65e440e9ef0d7ca4d4f93ab`）。`Builds/UriV2/NyaForge.exe`でprivate一時RadDollV3の外部base-color画像付きVRMを再取込し、EditMesh、native Save/Open、標準GLB出力まで **PASS**（`Artifacts/Authoring-20260913-123800-52cd3d9e8cf54109836ec1e79f6d2d89/report.json`）。同成果物のUnity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-123916-047-006f669c0bf4403491a4577693bd5ac2/bridge-report.json`）。

# 2026-09-13 bounded GLB/VRM model read

GLB/VRM本体の取込もFileInfo確認後の無制限`ReadAllBytes`をやめ、128MiB上限付きFileStreamと読み込み失敗の明示診断へ揃えた。`Builds/BoundedImportV1/NyaForge.exe`の800x600 Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-124309-7268891a990d48588306e34cb69f8f6c/report.json`）、Unity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-124340-491-826e8d7c991340bf9fba45dcf2be42bf/bridge-report.json`）。外部画像経路のCore 469件回帰は前項の結果を正とする。

# 2026-09-13 external base-color image import

GLB/VRM取込へ、モデルファイルと同じフォルダ配下の安全な相対URIによるbase-color画像を追加した。外部画像はnative Paintへ即時コピーしてSave/Open後も元ファイルへ依存しない。モデルフォルダ外への`..`、data URI、remote URI、欠落ファイル、16MiB超は明示エラーにする。埋め込み画像と既存のサイズ縮小経路は維持し、警告文と交換仕様書を実際の保持範囲へ更新した。

Core回帰は **469 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-70d826e4f3034f6783a84eb881a41b34`）。新規テストでローカルPNGの読込・MIME・バイト一致、path traversal、非対応WebP拒否を確認した。Windows Player `Builds/ExternalImageV3/NyaForge.exe` の800x600 Authoring suiteは **PASS、79 checks**（`Artifacts/Authoring-20260913-121649-4baab4f47e744278af3c3fc1cab1922f/report.json`）、Unity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-121721-131-45d72974b08d46ffaf28e75b0c800dd6/bridge-report.json`）。Navigationは同じ実行内容を先行PlayerでPASS済み（`Artifacts/Navigation-20260913-121452-c9e3477d673549bd92a4f9e6b5b2b8a8/report.json`）。実モデルの外部URIは未観測のため、次回はprivate一時入力でこの経路を目視確認する。標準VRM出力、完全な追加texture map/animation/VRM拡張保持、実VRChat内受入は引き続き未完了境界とする。

# 2026-09-13 Windows navigation smoke recheck

最新の`Builds/ExportReportV4/NyaForge.exe`で`Tools/Test-NyaForgeNavigation.ps1 -BuildName ExportReportV4 -Width 1280 -Height 800`を実行し、**PASS**（`Artifacts/Navigation-20260913-120737-e54944adc0dc4bd89cc03ea2a5cf7060/report.json`）。パック選択、キャンセル時の現状態保持、不正パス保持、named sessionの保存/再読込、utility panelの折り畳み、制作画面への遷移とusable viewportを確認した。これはスクリプト化されたWindows Playerナビゲーション証拠で、実マウスの個体差・DPI設定差は別の手動受入境界とする。公開READMEにもGLBレポートの`glbHash`照合方法を追記した。

# 2026-09-13 GLB export report hash binding recheck

標準GLBの出力レポートへ `glbHash`（`model.glb`本体のSHA-256）を追加し、レポートとバイナリが同じ成果物か機械的に照合できるようにした。Core回帰の静的GLBレポート、Workbenchのskin-bound衣装検証、外部MCP→Player検証で本体ハッシュを確認する契約を揃えた。クイックスタートにも照合方法を記載した。

最新Core回帰は **468 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f75abb38c9684f498a2b7d9dfa5295b9`）。Windows Player `Builds/ExportReportV4/NyaForge.exe` の800x600 Authoring suiteは **PASS、82 checks**（`Artifacts/Authoring-20260913-115430-d76ec906a1144a26849c256fe25a28f5/report.json`）。外部MCPのrevision-pinned GLB出力、`reportPath`、同一出力先の再送拒否、レポート内state hash／GLB hash照合、文書状態不変を含む。private一時RadDollV3 VRM（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`）でも **PASS、82 checks**（`Artifacts/Authoring-20260913-115830-b9f27f6f5b8c41cdae29dcae272fbe0f/report.json`）。両成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（最新実モデル経路: `Artifacts/BridgeReceiver-20260913-120250-409-d0658a1587154f43b0eb62325454c9c7/bridge-report.json`）。実VRChat内の見た目・挙動、標準VRM出力、実マウス／DPI差、衣装自動fit・貫通修正は引き続き未検証境界とする。

# 2026-09-13 final export-report real-model recheck

最新のWindows Player `Builds/ExportReportV3/NyaForge.exe` で、private一時RadDollV3 VRM（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`、public repositoryへ追加しない）を読み取り専用で再検証した。800x600 Authoring suiteは **PASS、82 checks**（`Artifacts/Authoring-20260913-114549-e48c45f2dcf24379a057688838b8510b/report.json`）。実モデルの候補選択、EditMesh頂点編集、native Save/Open、標準skinned GLB出力、出力GLB再取込、材質画像の既存予算処理に加え、skin-bound衣装導線も通過した。GLB出力フォルダには`export-report.json`が生成され、document ID・revision・state hash・profile・対象件数・保持しないVRM/graph metadataを確認した。同成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-115021-716-c4dcd03ad3f14941a1c67e8671f82bd1/bridge-report.json`）。これは実モデルの自動smokeと出力記録の証拠で、実マウス/DPI差、実VRChat内の見た目・挙動、標準VRM出力、自動fit・貫通修正は引き続き未検証境界とする。

# 2026-09-13 GLB export snapshot report (final verification)

標準GLB出力の同じフォルダへ `export-report.json` を原子的に同梱するようにした。レポートはversion、profile、単位・座標、document ID、document revision、state hash、対象ごとの頂点数・三角形数・submesh数・材質slot数、標準GLBで保持しないgraph／VRM metadataを記録する。GUIのステータスと外部MCPの成功レスポンスにもreportPathを返すため、GLB単体を別スナップショットの成果物と取り違えにくい。GLB本体とレポートは同一stagingから移動し、出力失敗時に中途半端な公開物を残さない。

Coreは **468 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-9e9f78478bc945b6af161ff9d0a47c9c`）。静的GLB出力のレポートがdocument ID・state hash・profile・objectCountを保持すること、skin-bound衣装の標準GLBでもレポートを生成することを回帰した。Windows Player `Builds/ExportReportV3/NyaForge.exe` の800x600 Authoring suiteは **PASS、80 checks**（`Artifacts/Authoring-20260913-113941-7f5c0d76a5f846649c55f74f0d44bbbc/report.json`）で、外部MCP client→stdio sidecar→named pipe→PlayerのGLB export検証、success responseのreportPath、同一exportId再送拒否、document state不変を含む。Unity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-114329-943-c2610adafca54487be6c1717f853a994/bridge-report.json`）。reportのlimitationsにはrest pose、influence上限、graph/native metadata非埋込み、VRM extensions非出力を明記する。実VRChat受け入れ、標準VRM出力、実マウス/DPI差は引き続き別境界とする。

# 2026-09-13 persist avatar pose-copy source across reopen

skin-bind衣装へavatarの現在poseをコピーした後、複数avatar候補がある状態でnative projectを再読込しても同じ対象を選べるよう、graph metadata node `rig.pose-source` を追加した。PoseSourceはavatarのstable object IDだけを保持し、WorkbenchのRefresh時にattachment targetのfallbackとして使う。skin-bind時とposeコピー時に更新し、GraphBinaryCodec・Inspection・標準skinned GLB whitelistへ対応した。標準GLBはこの制作メタデータを持たないため、native project側でのみ復元する。

Coreは **454 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-2e1aa8cd7b834460b53cb64a49bef8ee`）。回帰ではPoseSourceのavatar object IDがnative Save/Openで保持され、pose-copy済み衣装の標準skinned GLB exportが成功することを確認した。Windows Player `Builds/AccessoryPoseSourceV1/NyaForge.exe` の800x600 Authoring suiteは **PASS、79 checks**（`Artifacts/Authoring-20260913-111322-4fde8bc8f8ef4e97a3209998b17b3282/report.json`、画面 `authoring.png`）。このsuiteでは別avatar＋static accessoryのskin-bind、Root pose変更→明示poseコピー、native Save/Open後のpose hash照合、rest復帰とGLB出力まで通過した。同成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-111704-717-0a3f174945384229aa5369868688eee4/bridge-report.json`）。実マウス/DPI差、実VRChat SDK・実アバター内の見た目、衣装の自動fit・貫通修正、完全なVRM出力は引き続き別境界とする。

# 2026-09-13 explicit avatar pose copy for skin-bound clothing

skin-bindした衣装をavatarと同じ姿勢で確認できるよう、Workbenchの小物パネルへ「avatarの現在poseを衣装へコピー」を追加した。選択したavatarの評価済みPoseを、同じstable skeletonを持つ衣装側Pose nodeへ明示的に再bindして保存する。avatarとのライブ共有ではなく、姿勢を変更した場合は再度コピーする運用とし、異なるskeletonの自動結合や自動fit・貫通修正は対象外とする。Rig panelのweight編集と組み合わせ、Root初期化後の袖・裾などの確認をしやすくする。

Coreは **468 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b7ea42deef8749aba7e5b2d4e9cbca98`）。新規回帰ではavatar poseを衣装skeletonへ明示rebindした変形がnative Save/Open後も一致することを確認した。Windows Player `Builds/AccessorySkinV7/NyaForge.exe` の800x600 Authoring suiteは **PASS、79 checks**（`Artifacts/Authoring-20260913-110144-8f97721ee38d4c8ab91f23fcc2b90c77/report.json`、画面 `authoring.png`）。このsuiteではavatar poseを変更→衣装へコピー→pose hashをnative Save/Openで照合→restへ戻してGLB出力まで確認した。同成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-110519-475-d0a0b7d6b45b42bd92e6c657e521d56d/bridge-report.json`）。これはskin-bind後の姿勢確認導線を示す自動証拠であり、ボタン操作そのものの実マウス受入、DPI差、実VRChat SDK・実アバター内の見た目受入は別境界として記録する。

# 2026-09-13 accessory skin-binding workflow

衣装制作の次段として、別graph objectで取り込んだstatic GLBを、選択したVRM/GLB avatarの骨格へ変換する導線を追加した。`AccessorySkinBindingAdapter`は既存のSource／Morph／EditMesh／材質経路を保ったまま、avatarのskeleton・rest pose・skin-bind・skin-deformを追加し、全頂点をRoot boneへ100%で初期化する。Workbenchの「衣装をavatar骨格へskin-bind（Root初期化）」から実行でき、以降はRig panelのweight混合／weight paintで袖・裾などを割り当てられる。既存の剛体attachmentとの同時使用は拒否して二重変形を防ぐ。

Coreは **468 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-37c2a84eee484c7e97ce92ae1f4652b4`）。新規回帰ではRoot初期化、pose変形、native Save/Open、剛体attachment競合拒否を確認した。Windows Player `Builds/AccessorySkinV2/NyaForge.exe` のAuthoring suiteは **PASS、79 checks**（`Artifacts/Authoring-20260913-103030-f7976291411648c4b15338855d36ad29/report.json`）で、VRM avatar＋static accessoryのEditMesh→剛体装着→Save/Open→skin-bind→native exportを通過した。同成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-103409-141-90be8f007e9443bea647d660eb64bf0d/bridge-report.json`）。これはRoot初期化とweight編集準備までの証拠で、avatar poseの自動共有、自動fit・貫通修正、実VRChat内受入は別境界として残す。

# 2026-09-13 pasted feedback recheck on current HEAD

今回の貼り付けレビュー（基準 `0d1e957`）を現行HEAD `c4fd4f4`へ再照合した。レビューにある5件のP1（複数graph metadataの所属、source skinの二重変形、skinned node affine、inverse-bind出力、装着後の選択判定）は、現行コードでそれぞれgraphId単位のsession table、SkinDeform入力差し替え後の再評価、skinned affineの監査metadata化、保持したinverse-bind行列の標準／拡張GLB出力、`WorldPoints`による描画・Frame・選択の統一として実装済み。P2の材質linear値・metallic既定値、primitive単位の材質、省略texture、装着先保持、PhysBones source hash、揺れUndo／再bind、GLB共通root・morph bounds、normal/tangent morph変換も現行回帰へ含まれている。静的小物の頂点編集導線も追加済みで、別VRM avatar＋static GLB accessoryのEditMesh→stable BoneId装着→native Save/Open→feature-preserving exportを確認している。

再実行した `dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore` は **466 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f7d63fcb0ae944dca2e73c04bc182ba8`）。この結果はCore回帰の証拠で、実マウス／DPI差／Explorer実クリック、実Unity SDK・実VRChat内の見た目とPhysBones、衣装の自動fit・貫通修正、異なるskeletonの結合、完全なVRM出力は別境界として残す。次の開発カードは、共有mesh／skin／morph参照の仕様化か、固定した実SDK受け取り検証のどちらか一つに絞る。

# 2026-09-13 pasted feedback recheck on current main

## Separate avatar and accessory authoring workflow

静的GLBをgraph objectへ取り込む経路にEditMesh段が無く、保存はできても頂点編集できない穴を修正した。static／skinの両方でSource（必要ならMorph）→EditMesh→Material/Outputを構成し、別ファイルのVRM avatarとstatic GLB小物を同じ作品へ追加できるようにした。小物をrest-spaceで頂点編集した後、GUIからavatar objectとstable BoneIdを選んで`object.attachment`を作成し、native Save/Openではtarget・BoneId・編集結果を保持する。attachment付きのUnity出力は情報を落とさないnative project packageへルーティングする。

Coreは再実行して **466 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-8ab2aaa111a84a5c972dd2855fb0df67`）。`Builds/ImportedAccessoryV2/NyaForge.exe` の800x600 Authoring suiteは **PASS、82 checks**（private一時RadDollV3 VRMを含む取込→EditMesh頂点編集→stable BoneId装着→native Save/Open→feature-preserving export、report `Artifacts/Authoring-20260913-101222-7c49de2909e0473990c6eaa4364c2e0d/report.json`）。同成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-101342-514-202f68aad9c94bd08fdb86c8c8b10c9c/bridge-report.json`）。画面キャプチャ `Artifacts/Authoring-20260913-101222-7c49de2909e0473990c6eaa4364c2e0d/authoring.png` はUIと制作cameraの非空描画を確認した。これは合成static accessoryとprivate avatarの自動経路であり、実マウス/DPI差、実VRChat内の見た目、衣装の自動fit・貫通修正は別境界として残る。

## Graph-keyed secondary-motion attachment

複数graphを一つのnative projectへ追加した際、旧来の単一 `secondary-motion.nyaforge.bin` が別graphの揺れ設定として表示される境界を閉じた。`SecondaryMotionSessionsCodec`（`NVSX` v1）で共通揺れassetをGraphIdごとに保存し、active object切替時はそのgraphのassetだけを表示・再生・再bind対象にする。旧単一assetは読込時にactive graphへ互換移行し、次回保存時にtableへ変換する。既存のVRM expression／Spring／rig session tableと同じ所有単位へ揃えた。

Coreは **466 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-09cbb83127594ddf9dc48dc24f570994`）。`Builds/SecondaryMotionSessionsV2/NyaForge.exe` の800x600 Authoring suiteは **PASS、81 checks**（private一時RadDollV3 VRMの取込→候補選択→EditMesh→Save/Open→標準skinned GLB再取込、VRM0/1 playback、MCP lifecycleを含む、report `Artifacts/Authoring-20260913-100432-be47d5cabfa24764a85d18e290226470/report.json`）。同成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-100559-883-c623b841ae61434eb75b6732de6c7a12/bridge-report.json`）。private素材はpublic repositoryへ追加していない。

## README capability alignment

公開READMEとAuthoring Core READMEに残っていた古い能力表記を現行実装へ更新した。root READMEはnative schema 4（旧schema読込互換）、standard static/skinned GLB profile、MCP inspection/exportを明記し、Authoring READMEはstandard skinned GLB／外部MCPを実装済みとして、完全VRM・実VRChat SDK・自動fit等の境界を残した。ドキュメント変更後も `dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj` は **465 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-ee87e9a0c12e40c8838a497d8f3b154a`）。

貼り付けられたレビュー（基準 `0d1e957`）を、現行 `main` の `f5b908a` に再照合した。レビューの5件のP1は、複数graphのrig／expression／spring所属保存、source skinを`SkinDeform`入力へ組み込む評価順、skinned node affineの監査metadata化、元のinverse-bind行列の標準／拡張GLB出力への継承、装着後`WorldPoints`による描画・Frame・選択判定の統一で対応済み。P2の材質linear値・metallic既定値、primitive単位の材質、省略textureの扱い、装着先保持、PhysBones source hash、揺れUndo／再bind、複数rootとmorph bounds、normal/tangent morph変換も回帰へ含まれている。

現行Core回帰は **465 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-50ab507c88d04f23b0f72eeda0c001a6`）。Windows Playerの800x600 Authoring suiteとUnity **2022.3.22f1** synthetic Bridge、private一時RadDollV3 VRMの取込→EditMesh→native Save/Open→標準skinned GLB出力→再取込は直前カードのPASSを正とする。レビュー文面の古いhashや「未修正」という判定を現行状態へ持ち込まない。

残る境界は、実マウス／DPI差／Explorer実クリック、実VRChat SDK・実アバター内の見た目とPhysBones動作、自動fit・貫通修正、異なるskeletonの結合、共有mesh／skin／morph参照、完全な外部texture・animation・VRM拡張保持、標準VRM出力。次の開発カードは、共有参照の明示仕様化か、実SDK版を固定した受け取り検証のどちらか一つに絞る。

# 2026-09-13 goal audit Player acceptance

現行HEADから別フォルダへ作成した `Builds/GoalAuditV1/NyaForge.exe` を、公開fixtureとprivate一時RadDollV3 VRM（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`）で再検証した。1280x800のAuthoring suiteは **81 checks PASS**（real GLB/VRM command-line import、候補選択、生成EditMesh、頂点編集、native Save/Open、標準skinned GLB出力と再取込を含む、report `Artifacts/Authoring-20260913-093714-3087afd197c3424fb519575d5641e6a6/report.json`）。同成果物をUnity **2022.3.22f1** synthetic Bridgeへ渡した検証も **PASS**（`Artifacts/BridgeReceiver-20260913-093847-492-b8cfb82dc0064581bdc0a78e38514dbc/bridge-report.json`）。最終画面 `Artifacts/Authoring-20260913-093714-3087afd197c3424fb519575d5641e6a6/authoring.png` はUI Toolkitと制作cameraが同時に描画され、右パネルはスクロール可能だった。これは自動検証と画面画像の確認であり、実マウス/DPI差、実VRChat内の見た目、実SDK受入を代替しない。

# 2026-09-13 goal audit dense-paint performance

同じ `Builds/GoalAuditV1/NyaForge.exe` の高密度Paint合成fixture（131,072 triangles、30 frame samples）を観測した。`dense-paint-profile.json`（`Artifacts/Authoring-20260913-093945-3fdc0fe97a464f3892f01c7d5808d139/dense-paint-profile.json`）では平均 **66.6668 ms**、P95 **66.6672 ms**、最大 **66.6673 ms**、観測予算250 msを超過しなかった。設定はtargetFrameRate 15、vSync 0、RTX 4090、Unity 6000.4.3f1。GC generation 0は305→335、managed heapは152,080,384→207,351,808 bytesだった。同成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-094052-865-3fb874064601480299724519f74346cf/bridge-report.json`）。これは合成fixtureと現行GPUでの観測であり、実アバター・別GPU・通常の60fps設定の性能保証ではない。

# 2026-09-13 goal audit external MCP acceptance

同じ `Builds/GoalAuditV1/NyaForge.exe` と `Tests/Mcp.Transport/bin/Debug/net10.0/Mcp.Transport.Tests.dll` を使い、外部MCP client→sidecar→live Playerの経路を再検証した。Authoring suiteは **PASS**（report `Artifacts/Authoring-20260913-094343-a58800080c474e95b92512b941da0228/report.json`）。revision固定の標準GLB出力、既存出力先の再実行拒否、文書状態不変を確認した。別実行ではsecondary-motionのstate/play/pause/rebuild/fixed-step/resetも **PASS**（report `Artifacts/Authoring-20260913-094233-ca3a879a1b2a461bb4e129a21e816145/report.json`、`mcp-external.log`）。GLB export成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-094512-926-b7d3cd6946e84fceb09827135c45f99c/bridge-report.json`）。

# 2026-09-13 attachment target retention fix

装着先Dropdownの変更イベントが表示ラベル（`graph · <id>`）をstable object IDとして保持していたため、Refresh後に選択が先頭へ戻る経路を修正した。選択肢のindexから内部IDを保存し、表示ラベルと契約IDを分離した。回帰では異なるskeleton sessionを持つ2つのavatar graph objectを用意し、2番目の対象を選択→Refreshしても選択値が保持されることを確認した。`Builds/AttachmentTargetRetentionV1/NyaForge.exe` の800x600 Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-095046-9bd3831ed6124ff7bfd59ea819ab6f37/report.json`）。同成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-095216-432-f3e7c59ba0794f8ab91d312b43f99e28/bridge-report.json`）。

# 2026-09-13 attachment target retention real-model recheck

Dropdown保持修正後の `Builds/AttachmentTargetRetentionV1/NyaForge.exe` でprivate一時RadDollV3 VRM（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`）を再検証した。800x600 Authoring suiteは **81 checks PASS**（取込候補選択、EditMesh頂点編集、native Save/Open、標準skinned GLB出力と再取込を含む、`Artifacts/Authoring-20260913-095259-96c5ddad45374685ac91e3fc8f4bc3d3/report.json`）。同成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-095436-336-6aa7dd710b6442c382c2746cbdcf75a6/bridge-report.json`）。private素材はpublic repositoryへ追加していない。

# 2026-09-13 native UI acceptance bridge check

提示されたレビュー（基準 `0d1e957`）のP1/P2は、実装・Core回帰・Windows Player/Unity Bridge検証で閉じている。追加でWindowsの実マウス/DPI受入を確認するため、既存の `Builds/ValidationSkinV3/NyaForge.exe` を起動してComputer UseのネイティブUI列挙を試したが、このセッションのブリッジは `apps: []`（ブラウザのみ）を返し、Playerのアクセシビリティ状態やクリック結果を取得できなかった。したがって実マウス、DPI差、Explorer実クリックの受入証拠は作成していない。自動Authoring suiteのPASSを実操作受入へ読み替えず、次回はネイティブUIブリッジが有効な環境で、起動画面→制作画面→スクロール→候補選択→保存導線を一操作ずつ確認する。

# 2026-09-13 model import budget preflight acceptance

モデル読込前のファイルサイズ検査を含む `Builds/ImportBudgetV1/NyaForge.exe` で、公開GLB fixtureの候補確認→取込→EditMesh→native Save/Open→標準skinned GLB出力→再取込を実行した。800x600 Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-092556-4cd5f819b60e457aaad0727bf158020c/report.json`）。同Playerでprivate一時RadDollV3 VRM（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`）も同じ一周が **PASS**（`Artifacts/Authoring-20260913-092645-27e7a7c2541043f08144e248e38d4538/report.json`）。Unity **2022.3.22f1** synthetic Bridgeは両成果物で **PASS**（`Artifacts/BridgeReceiver-20260913-092629-232-47ea92d3e9ce4c9db054a917bd651be1/bridge-report.json`、`Artifacts/BridgeReceiver-20260913-092758-792-5ae5d0b2442047648736278ec2e77f7e/bridge-report.json`）。

# 2026-09-13 model import size preflight

GLB/VRM取込の候補確認と本取込で、`File.ReadAllBytes` より前にファイル存在と128 MiB import budgetを検査する共通 `ReadModelFile` を追加した。上限超過ファイルを不要にメモリへ載せず、パス不存在も取込処理の診断へ統一する。新Player `Builds/ImportBudgetV1/NyaForge.exe` の800x600 Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-092556-4cd5f819b60e457aaad0727bf158020c/report.json`）。private一時RadDollV3 VRMの候補選択→EditMesh→native Save/Open→標準skinned GLB出力→再取込も **PASS**（`Artifacts/Authoring-20260913-092645-27e7a7c2541043f08144e248e38d4538/report.json`）。Unity **2022.3.22f1** synthetic Bridgeも両成果物で **PASS**（`Artifacts/BridgeReceiver-20260913-092629-232-47ea92d3e9ce4c9db054a917bd651be1/bridge-report.json`、`Artifacts/BridgeReceiver-20260913-092758-792-5ae5d0b2442047648736278ec2e77f7e/bridge-report.json`）。

# 2026-09-13 GLB / VRM import labels

取込機能はGLBとVRMの両方に対応しているため、制作画面の折りたたみ見出し、ファイル選択ボタン、graph object取込ボタンの表示を「GLB / VRM」へ統一した。内部の候補選択・保存・出力契約は変更していない。`Builds/ImportLabelV1/NyaForge.exe` の800x600 Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-092256-409e806db2ca462c875c481c54b16f22/report.json`、画面 `authoring.png`）。同じ成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-092326-908-354689949db74e6a869bb488cab0d506/bridge-report.json`）。

# 2026-09-13 real RadDollV3 recheck after validation fix

private一時素材 `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm` を、画像リソース重複計上修正後の `Builds/ValidationTextureDedupV1/NyaForge.exe` で再取込した。候補選択→rest-space EditMesh頂点編集→native Save/Open→標準skinned GLB出力→再取込までのAuthoring suiteは **PASS**（800x600、`Artifacts/Authoring-20260913-091957-e8571df5858c48958b862af3a3ebcfc5/report.json`、画面 `authoring.png`）。同じ成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-092110-708-47b95c99f5ea44bd9645d430261a0f04/bridge-report.json`）。private素材はpublic repositoryへ追加していない。実マウス/DPI差、実VRChat SDK/VRChat内の見た目受入とは分けて扱う。

# 2026-09-13 validation texture resource deduplication

材質接続後の評価値は同じbase-color画像を出力のlegacy `BaseColor` と `Material.BaseColor` の両方へ保持するため、出力チェックのtexture数がグラフ参照数を数えて実リソース数より多くなる場合があった。`AuthoringValidationReader` は画像hashを一度だけ数えるようにし、同一画像を材質から複数参照しても容量判定が過大にならないよう修正した。Coreへ重複参照の回帰を追加し、**465 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-2ce6c7d8eaf547fbad1d9c8d09714515`）。Windows Player `Builds/ValidationTextureDedupV1/NyaForge.exe` の800x600 Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-091728-f26ad2de57b24a038163c89a57e89b8e/report.json`、画面 `authoring.png`）。同じ成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-091801-690-5b07e4e16c6e4f1e91c24785c6675491/bridge-report.json`）。

# 2026-09-13 latest automated acceptance recheck

現行HEAD `e999ec2` でCoreを再実行し、**465 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-2ce6c7d8eaf547fbad1d9c8d09714515`）。画像リソース重複計上修正を含む `Builds/ValidationTextureDedupV1/NyaForge.exe` の800x600 Authoring suiteも **PASS**（`Artifacts/Authoring-20260913-091728-f26ad2de57b24a038163c89a57e89b8e/report.json`、画面 `authoring.png`）。同じPlayer検証成果物をUnity **2022.3.22f1** synthetic Bridgeへ渡した結果も **PASS**（`Artifacts/BridgeReceiver-20260913-091801-690-5b07e4e16c6e4f1e91c24785c6675491/bridge-report.json`）。これは自動回帰・合成receiverの証拠であり、実マウス/DPI差、実VRChat SDK/実アバター内の見た目受入とは分けて扱う。

# 2026-09-13 material output guidance

材質パネルに残っていた「材質付きUnity出力は開発中」という古い案内を、対応するUnity用profileへ書き出せる説明へ更新した。実装範囲とGUI案内の不一致を解消し、Windows Player `Builds/MaterialMessagingV1/NyaForge.exe` の800x600 Authoring suite **78 checks PASS**（`Artifacts/Authoring-20260913-090623-889afe772a2a44dba831a8fab5c80adf/report.json`）で既存操作の回帰がないことを確認した。

# 2026-09-13 authoring validation skin capacity

「出力チェック」でスキン付きgraphの骨数と頂点あたり最大influenceが常にunknownになる境界を修正した。評価済みの出力到達graphにある`SkinBindingOutputs`／`SkeletonOutputs`から実値を集計し、NyaForgeのauthoring容量（512骨／32 influence）へ明示判定する。未接続の作業用rigは集計から除外する。GUIには`actual/limit`も表示する。skin bindingが無い静的graphは従来どおりunknown、`fit`は実アバター受入を含むためunknownのままとする。Core **464 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-bce4bb3011f945c48f007146f1832ddf`）。Windows Player `Builds/ValidationSkinV3/NyaForge.exe` の800x600 Authoring suiteは **78 checks PASS**（`Artifacts/Authoring-20260913-090034-98ebf69a86954db2a008d2d70b6d785e/report.json`）。同成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-090105-766-345e4b00828e4a0e8926294900142ae7/bridge-report.json`）。さらに同じValidationSkinV3でprivate一時素材のRadDollV3 VRMを候補選択→EditMesh頂点編集→native Save/Open→標準skinned GLB出力→再取込まで再確認し、Authoring report `Artifacts/Authoring-20260913-090318-0e84c8ce478a4d03b9f76660af3b2802/report.json` とUnity Bridge `Artifacts/BridgeReceiver-20260913-090435-743-a95c669a3bad4760a2d6f6bd5f5ea377/bridge-report.json` はともに **PASS**。private素材はpublic repositoryへ追加していない。

# 2026-09-13 GLB skeleton/morph conformance

GLB出力の仕様穴を追加で閉じた。複数の親なしboneを持つskinned出力では `NyaForgeSkeletonRoot` を生成し、skinの `skeleton` から全joint rootへ到達できる共通祖先を出力する。morphの `POSITION` accessorにはglTF必須の `min/max` を付け、slot primitive側も同じ規則に揃えた。新しいCore回帰を含め **463 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f891479b2fe344a1a8935eb6974232c6`）。

Windows Player `Builds/MorphBoundsV1/NyaForge.exe` の800x600 Authoring suiteも **PASS**（`Artifacts/Authoring-20260913-084200-71702ef26aa14b9389f34352f292694a/report.json`、画面 `authoring.png`）。同成果物をUnity **2022.3.22f1** Bridgeへ渡した検証も **PASS**（`Artifacts/BridgeReceiver-20260913-084245-214-4bdf3a3c031a/bridge-report.json`）。Bridgeは合成receiverであり、実VRChat SDK/実アバター受入とは分ける。

# 2026-09-13 feedback recheck (`0d1e957`)

提示されたレビュー（基準 `0d1e957`）を現行HEAD `51eb4af`へ再照合し、Coreを再実行した。結果は **461 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4aec0876bd0c47cbb63c11a58443b069`）。5件のP1は重複実装せず、次の対応を現行の正として記録する。

| 指摘 | 現行対応 | 回帰・残る境界 |
| --- | --- | --- |
| 複数モデル後のSave/Openで情報が混ざる | rig、expression、springをgraphId単位のsession tableへ保存し、旧single attachmentはgraphIdへ移行する | 異なるskeletonの自動結合、共有mesh/skin/morph参照は未対応 |
| source skinの二重変形／下流編集消失 | `SkinDeform`入力位置へsource paletteを差し替えてgraphを再評価し、EditMesh/Morph/材質/Outputを同じ順序で保持 | 実アバターの自動fit・貫通とVRChat内見た目は未受入 |
| skinned node affineの二重適用 | skinned表示と標準GLB出力はjoint world frame＋保持したinverse-bindを正本にし、mesh node affineは監査metadataだけにする | 任意GLBの全node意味保存は未完了 |
| 出力時のinverse-bind欠落 | sourceの一般inverse-bind行列をnative rig sessionから標準／拡張skinned GLB writerへ渡す。scale・回転を含む行列回帰を維持 | 実Unity/VRChat receiverでの拡張profile受入は未確認 |
| 装着後のクリック判定ずれ | 描画・Frame・選択判定をroot変換後の`WorldPoints`で統一し、ローカル点とは分離する | 実マウス、DPI差の受入は未実施 |

列挙されたP2（linear `baseColorFactor`／metallic既定値、局所material slot、装着先選択保持、PhysBones source hash、揺れUndo revision、GLB共通root・morph境界、normal/tangent morph変換）も現行回帰で閉じている。埋め込みbase-color画像はnative Paintへ縮小保持し、Save/Openと標準skinned GLB再取込まで確認済み。完全な外部texture・animation・VRM拡張保持、実VRChat SDK/PhysBones受入は引き続き別タスクとする。

# 2026-09-13 embedded base-color GLB output recheck

実RadDollV3で、native Paintへ縮小保持した埋め込みbase-color画像が標準skinned GLB出力で失われないことを追加確認した。出力GLBをskin importerで再読込し、画像付きmaterialが存在することを検証している。Coreは直前の **461 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-a7b92fd3fb184402a46aef4f4dfa676c`）を正とする。Windows Player `Builds/MaterialResizeV3/NyaForge.exe` のAuthoring **80 checks PASS**（`Artifacts/Authoring-20260913-082925-87045e69ee724ec791289be206d54dbe/report.json`）、Unity **2022.3.22f1** Bridge **PASS**（`Artifacts/BridgeReceiver-20260913-083342-809-9f1c2a45fffe418e81ce36a04541093f/bridge-report.json`）。

# 2026-09-13 embedded base-color Save/Open hash recheck

実RadDollV3の埋め込みbase-color画像をnative Paintへ縮小保持する経路について、取込直後だけでなくnative Save/Open後のPaint画像hash一致も検証条件へ追加した。private一時素材はリポジトリへ入れていない。Coreは直前の **461 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-a7b92fd3fb184402a46aef4f4dfa676c`）を正とする。Windows Player `Builds/MaterialResizeV2/NyaForge.exe` のAuthoring **80 checks PASS**（`Artifacts/Authoring-20260913-082108-0f81ed3a0c5d485f8cfb874f8b467fb4/report.json`）、Unity **2022.3.22f1** Bridge **PASS**（`Artifacts/BridgeReceiver-20260913-082532-936-f38ed0cdd41e46b4ba92be111f08e16c/bridge-report.json`）。

# 2026-09-13 embedded base-color image budget retention

GLB/VRMの埋め込みbase-color画像が1024pxを超える場合、取込を白一色へ退避せず、最大8192pxまでデコードしてアスペクト比を保った最近傍縮小を行い、native Paintの1024px上限へ所有データとして保存するようにした。8192px超・壊れた画像・16MiB超は従来どおり明示診断で省略し、外部URIやbase-color以外のtexture mapは推測取得しない。実RadDollV3（private一時素材、埋め込み画像6件）で縮小後のPaintノードとSave/Open依存なしを確認した。Core **461 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-a7b92fd3fb184402a46aef4f4dfa676c`）。Windows Player `Builds/MaterialResizeV1/NyaForge.exe` Authoring **80 checks PASS**（`Artifacts/Authoring-20260913-081327-dc634884b6f0475e9ebbcabff60eff72/report.json`）、Unity **2022.3.22f1** Bridge **PASS**（`Artifacts/BridgeReceiver-20260913-081803-691-55c8fee7b2794de8a139d297b4ca5482/bridge-report.json`）。

# 2026-09-13 glTF material color/default alignment

glTF `baseColorFactor` は線形値として読み書きするよう修正し、取込時の不要なsRGB変換と出力時の逆変換を廃止した。`metallicFactor` の省略値もglTF仕様の1へ合わせ、linear RGBと省略既定値の回帰を追加した。Core **460 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-66295ed6bf6a4d98815455f15972ac53`）。Windows Player `Builds/MaterialLinearV1/NyaForge.exe` Authoring suite（`Artifacts/Authoring-20260913-074940-af62bbf5049a4276bccce18c480d93e2/report.json`）とUnity Bridge（`Artifacts/BridgeReceiver-20260913-075048-365-c4c2956b407148a6afe5110e50f45e0f/bridge-report.json`）はPASS。

# 2026-09-13 skinned node affine policy

skinned表示とWorkbench/MCPの標準skinned GLB出力で、mesh node affineをスキン結果へ二重適用しない方針へ揃えた。joint world frameとinverse-bindを正本にし、選択nodeのaffineはsource sessionの監査metadataとして保持する。Core **461 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-a7b92fd3fb184402a46aef4f4dfa676c`）。Windows Player `Builds/SkinnedAffinePolicyV2/NyaForge.exe` で、private一時素材 `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm` を使った Authoring **79 checks PASS**（`Artifacts/Authoring-20260913-080806-87aacf4f39c74048b4c00a8a11c52212/report.json`）、Unity **2022.3.22f1** Bridge **PASS**（`Artifacts/BridgeReceiver-20260913-080912-368-fd387a22cef74d5faf30b6e7ab6e88f0/bridge-report.json`）。実モデルは取込・頂点編集・native Save/Open・標準skinned GLB出力と再取込の一致を確認した。材質画像はサイズ/形式制限でnative Paintへ保持せず、明示診断を残す。

# 2026-09-13 attachment selection and MCP bind parity

装着先Dropdownの選択値をRefresh間で保持し、別avatarを選んだ直後に先頭候補へ戻る経路を修正した。WorkbenchのGUIとMCPのskinned GLB出力へ、graph objectごとのimported inverse-bind行列を同じ経路で渡すよう統一した。Windows Player `Builds/AttachmentSelectionV1/NyaForge.exe` のAuthoring suite（`Artifacts/Authoring-20260913-075348-262a37a7fa964c90b373e7ce80254da5/report.json`）とUnity Bridge（`Artifacts/BridgeReceiver-20260913-075417-960-ac4b7ab2f333483ba5963fc238e1586b/bridge-report.json`）はPASS。

## 2026-09-13 consistency feedback final recheck (`5826247`)

提示されたレビュー（基準 `1c76e4a`）を現行 `main` の `5826247` へ再照合した。4件のP1と列挙されたP2は後続実装で閉じており、重複修正は行わず受入証拠を更新した。16bit `JOINTS_n` は2バイト幅で復号し、8bit/16bit同値・負weight拒否を回帰。source skin表示はgraph評価後の編集結果を保持し、PhysBones target packageはprofileが参照するsource assetを同梱してhash検証する。静的GLBのskin判定は選択meshに限定し、同居する小物を取り込める。

Core **459 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f3e6edffd8e847178fe7b97c4dd740e7`）。同一skeleton hashの複数skinned mesh、objectごとのinstance affineを含むGLB出力も回帰済み。実RadDollV3はprivate一時素材で取込→編集→native Save/Open→GLB出力を再確認し、public repositoryへ追加していない（Authoring report `Artifacts/Authoring-20260913-071034-98196e26e7af47cfa3df95cd3eab7bbd/report.json`、Bridge report `Artifacts/BridgeReceiver-20260913-071148-485-e6720b8b0f8d49858a5198eafa261b7/bridge-report.json`）。

残る受入境界は、異なるskeletonの結合、共有mesh/skin/morph参照、完全な材質・animation保持、実VRChat SDK/実アバター/VRChat内のPhysBones動作、実マウス/DPI差。次のタスクはこの境界を混ぜず、共有参照の仕様化または実SDK版固定の受け取り検証へ進める。

## 2026-09-13 source skin downstream evaluation (`SourceSkinOverrideV1`)

source skin表示を、評価済みの最終出力へ後掛けする経路から、`SkinDeform`ノード出力をsource paletteで差し替えて通常のgraph評価を再実行する経路へ変更した。これによりSkinDeform後のEditMesh、Morph、材質、Outputが同じ順序で評価され、非rest poseで編集差分が二重変形されない。通常評価のstale EditMesh保護は維持し、source projection時だけ同一domainの編集snapshotを現在の差し替えmeshへ再基準化する。

Core **459 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-bae7e375d86048a78ee5450d8cdeb43a`）。非rest pose＋SkinDeform後EditMeshのsource projection回帰を追加。Windows Player `Builds/SourceSkinOverrideV1/NyaForge.exe` のAuthoring suiteは実RadDollV3 private temp素材でPASS（`Artifacts/Authoring-20260913-072229-179ff5fd12ab4d18926fe2029b293dd9/report.json`）。同Player成果物のUnity 2022.3.22f1 BridgeもPASS（`Artifacts/BridgeReceiver-20260913-072338-010-fc41053f0aa542e6a35dbaf0f0a0176b/bridge-report.json`）。

## 2026-09-13 GLB候補選択Dropdown

GLB/VRM取込パネルの数値index入力を通常画面では折りたたみ、候補確認後にmesh resource・skin resource・node instanceを名前付きDropdownから選べるようにした。既存のIntegerFieldは自動検証と互換操作のため内部保持し、Dropdown選択を同じindex契約へ同期する。node instanceを選んだ場合は、従来どおりそのmesh／skin／配置を優先する。候補確認前の未確定状態は明示し、推測選択はしない。

Windows Player `Builds/ImportChoiceV2/NyaForge.exe` の800x600 Authoring suite **PASS**（report `Artifacts/Authoring-20260913-065009-b9aa3210c8cc4c30a05bb0c4c142e5b7/report.json`）。合成multi-mesh fixtureでmesh／skin／node候補名の表示、skinned graph取込、頂点編集、複数rig session切替を回帰した。Unity Bridge **PASS**（Unity 2022.3.22f1、`Artifacts/BridgeReceiver-20260913-065043-006-c42930cffe984517a674d26c96f817c2/bridge-report.json`）。Coreは前カードの **458 passed / 0 failed** を正とする。実マウス/DPI差、任意実モデルの全候補目視、実VRChat受入は別境界として残る。

## 2026-09-13 NativeLocatorV1 実RadDollV3再回帰

native manifest locatorを含む最新Windows Playerで、private一時素材 `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`（45,341,584 bytes）を再取込した。候補確認、rest-space頂点編集、native Save/Open、標準SkinnedGeometry GLB出力までのAuthoring suiteが **PASS**（800x600、report `Artifacts/Authoring-20260913-064533-45e34c13d02f42b3b7691b7dbbb0288f/report.json`）。同じPlayer出力をUnity **2022.3.22f1** Bridgeへ渡した検証も **PASS**（`Artifacts/BridgeReceiver-20260913-064639-406-78812740b9e34d00bccdc1869d314fe8/bridge-report.json`）。private素材はpublic repositoryへ追加していない。

これは実素材の取込・保存・出力smokeであり、実マウス操作、実VRChat内の見た目、PhysBones実SDK受入、自動fit・貫通修正の証拠ではない。

## 2026-09-13 native manifest locator contract

native projectの再開パス検証を`NativeProjectLocator.RequireManifestDirectory`へAuthoring層として切り出した。`project.nyaforge.json`以外、存在しないmanifest、空パス、未対応パスを同じerror codeで拒否し、Explorer GUIは既存の未保存確認と`ProjectStore.Open`へ渡す親フォルダだけを受け取る。manifest内容の検証やworkspace置換はlocatorへ混ぜず、既存責務を維持した。

Core **458 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-6a575829f424468d91cb8827b93aef37`）。Windows Player `Builds/NativeLocatorV1/NyaForge.exe` の800x600 Authoring suite **PASS**（report `Artifacts/Authoring-20260913-064253-149db09bb4a24715bc62d8a94ac93981/report.json`）。Unity Bridge **PASS**（report `Artifacts/BridgeReceiver-20260913-064327-846-0b11677610834177860b0cea90edfd4e/bridge-report.json`）。実Explorerクリック、DPI差、実VRChat SDK受入は別境界として残る。

## 2026-09-13 consistency feedback recheck (`13bc959`)

提示されたconsistency review（基準 `1c76e4a`）を、最新main `13bc959`へ再照合した。P1の4件は現行実装とCore回帰で閉じている。16bit `JOINTS_n` は2バイト幅で復号し、8bit/16bit同値を確認済み。source skin表示は`SourceSkinGraphAdapter.ApplyToEvaluation`で最終graph outputへ適用するため、SkinDeform後のEditMeshを保持する。PhysBones target packageはprofileが参照するsource assetを同梱し、receiver側でhashを検証する。静的GLB取込のskin判定は選択meshのnode instanceに限定し、同じファイル内の静的小物を拒否しない。

P2も、揺れreset時のsource-skin projection cache無効化、PhysBones managed markerのstable root/name再利用、削除curveの初期化、secondary-motion再bindのUndo、MCP captureのcamera metadata保持、負weightの事前拒否を実装・回帰済み。今回の再実行は **Core 454 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-40f038c9de1a437994666a376c0af406`）。Explorer project pickerを含むWindows PlayerとUnity Bridgeの直近PASSは前カードの記録を正とする。

残る受入境界は、実VRChat SDKでのPhysBones component生成・更新、実マウス/DPI差、実アバターの自動fit・貫通修正、VRChat内の見た目、異なるskeletonの結合・共有参照・完全な材質/animation保持。同一skeleton hashの複数skinned mesh出力は最新カードで回帰済み。次の作業はこの境界を混ぜず、実SDKの版・完全修飾型を固定した受け取り検証、または共有参照の明示仕様化から選ぶ。

## 2026-09-13 native project Explorer picker

制作フォルダを手入力せず再開できるよう、Windows Authoring画面へ「Explorerで選ぶ…」を追加した。Explorerで`project.nyaforge.json`を選ぶと親フォルダをnative projectとして検証し、別JSON・manifest欠落・存在しない選択は開かない。ダイアログ中にworkspaceのdocumentが変わった場合も置換せず、既存の未保存確認（保存／破棄／キャンセル）を通してから開く。保存形式、nativeの正本、GLB/VRM取込経路は変更していない。

Windows Player `Builds/ProjectPickerV1/NyaForge.exe` のAuthoring suite **PASS**（report `Artifacts/Authoring-20260913-063116-c9ba956e8e8d436cb14271bb46729ed5/report.json`、800x600）。GUI control存在を回帰し、既存の取込・頂点編集・Undo/Redo・native Save/Open・GLB出力・MCP・PhysBones表示を含む全チェックを通過した。Unity Bridge **PASS**（Unity 2022.3.22f1、report `Artifacts/BridgeReceiver-20260913-063147-237-c179a2dc469943feb5969a33dbcad0da/bridge-report.json`）。Core **454 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0611989018b34eb2863ba4e713f3a35c`）。自動検証はExplorerの実クリックやDPI差、実VRChat内受入を代替しない。

## 2026-09-13 attachment GUI verification

装着ノードの制作導線を、GUIの実コントロールまで検証できるようにした。Authoring suiteは対象`object-attachment` foldout、対象graph objectのDropdown、stable BoneId Dropdownの存在を確認し、解決済みtarget/BoneIdが選択肢へ表示されることを回帰する。既存のroot pose追従、native Save/Open、未解決target拒否、標準GLBのmetadata loss guardも同じPlayerで継続確認した。

Core **454 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-fbf4a508696c4b35b50231014cc07f6a`）。Windows Player `Builds/AttachmentV9/NyaForge.exe` のAuthoring suite **PASS**（report `Artifacts/Authoring-20260913-062610-55a5b5cacad344b98de2791138163e89/report.json`、800x600）。GUIがstable IDを表示することを確認したが、実マウス/DPI差・実アバターの自動fit/貫通・実VRChat SDK受け入れは別境界として残る。

## 2026-09-13 GLB attachment loss guard

標準GLBにはNyaForgeのobject attachmentを表す共通フィールドがないため、装着ノードを含む作品のGLB出力を続行するとBoneId・target object・offsetが失われる。`GlbExportService`は装着ノードを検出した時点で`GLB_ATTACHMENT_METADATA_UNSUPPORTED`を返し、出力先を作らずnative project exportを案内する。single/multi-objectのnative routingと未解決target拒否は前カードの契約を維持する。

Core **454 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-2e1aa8cd7b834460b53cb64a49bef8ee`）。Windows Player `Builds/AttachmentV8/NyaForge.exe` のAuthoring suiteもPASS（report `Artifacts/Authoring-20260913-062245-9cb8fb0762e84c24bb7b9e6eb9780e30/report.json`）。装着ノードを黙ってGLBへ落とさないこと、native packageへ保存することを回帰した。
## 2026-09-13 attachment export routing

監査で、装着ノードだけを含むgraphが通常のMesh/Surface Bakeへ誤ルーティングされると、GLB等の標準交換形式では表現できない装着メタデータが失われる経路を確認した。`ProjectExportService`が`object.attachment`を含むgraphをfeature-preserving native projectへ送るよう修正し、再読込後のtarget object・stable BoneId・offsetを回帰した。

Core **453 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-65771e9def9948ddb08ca63778a6ebd9`）。Windows Player `Builds/AttachmentV7/NyaForge.exe` のAuthoring suiteもPASS（report `Artifacts/Authoring-20260913-061851-56d32d40d3d84ddba7fb269cfa44f67d/report.json`）。avatarのPose変更後も小物rootが追従し、複数object attachmentのnative exportと未解決target拒否を検証した。標準GLBはattachment metadataを表現しないため、小物の位置情報を保持したい場合はnative project exportを使用する。この境界をGUI/MCPの出力案内へ明示した。
## 2026-09-13 object.attachment follow-up

前回のボーンフィードバックを受け、チョーカーなどの小物を明示したavatarのstable `BoneId`へ装着する経路を実装した。graphへ`object.attachment`ノードを追加し、`targetObjectId`、`boneId`、`skeletonHash`、bone-local rest offset（メートル）を型付き・ハッシュ付きで保存する。GUIの「小物をボーンへ装着」は対象graphとBoneIdを選択して設定でき、名前推測・自動fit・貫通修正は行わない。MCPの`graph.node.add/update`でも同じpayloadを使える。

Workbenchは対象avatarのimported rig sessionをstable hashとmappingで検証し、pose評価から小物rootの位置・回転を再投影する。active/inactive graph objectの表示投影、最終結果投影、Undo対象の追加/更新/解除、native schema 4 Save/Openのrig session復元まで接続した。装着poseをmesh projectionのcache判定へ含め、pose変更時に古いrootを再利用しないようにした。

検証結果: Core **451 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b89a243dbb924b979d94173ee1f2bb11`）、Windows Player `Builds/AttachmentV3/NyaForge.exe` のAuthoring suite **PASS**（report `Artifacts/Authoring-20260913-060504-29017a8a29be484ab5e6c6540278b84a/report.json`）、MCP transport **3/3 PASS**。Playerではstable BoneId/root pose、Save/Open、rig-session restoreを含む全チェックを確認した。

この検証は合成skeletonとNyaForge内の表示・保存経路が対象で、実VRChat SDK、実アバターでの自動fit/貫通、VRChat内の見た目受入は未完了。次は実SDK受け取りと実アバターでの装着確認を別証拠として進める。
# NyaForge 開発タスク

## 2026-09-13 consistency review follow-up

提示されたレビュー指摘を現行 `main`（`dc31fa3`）へ再照合した。P1の4件（16bit `JOINTS_n` の幅、source skin後の下流編集、PhysBones packageのsource asset、選択mesh単位のskin判定）は、既存の回帰テストと実装で閉じた状態を維持している。追加の再検証では Core **448 passed / 0 failed**（artifact: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4bbb7b6347fd456ead93289028b5571a`）。`extensionsRequired` の未対応拡張を取込前に `UNSUPPORTED_EXTENSION` で停止するガード、材質slotの局所リマップ、実RadDollV3のGLB往復も現行mainに含まれる。

残る受入境界は、実VRChat SDKを導入したPhysBones component生成・更新、実アバター／VRChat内の動作、任意GLBの複数mesh・共有参照・完全材質／animation保持である。合成UnityBridgeやPlayer自動検証の合格を、これらの実環境受入へ読み替えない。次の実装単位は対象SDKの版・完全修飾型を固定したSIM-02B実SDK受け取りで、SDKが無いpublic buildの起動・保存・診断は引き続き維持する。

MCP transportも再ビルドして確認した。新しい`forge_export_glb`を含む19 toolのregistry、named pipeのinstance検証、captureのcamera metadata保持とPNG bytes非重複が **PASS**。古い18 tool期待値をテスト側で更新した。

更新: 2026-09-13。標準GLB出力のStaticGeometry／SkinnedGeometry profileとGUIボタンを追加し、normal/tangent morph、容量拡張、全weight保持の拡張GLBを含むCore448件合格。レビュー再照合後の最新HEADは`dc31fa3`で、P1/P2の回帰を再実行済み。次の実装カードは、実SDK受け取りの版固定、複数mesh/共有参照の明示設計、チョーカー小物のbone固定メタデータとプレビュー追従である。提示された2026-09-13 consistency review（基準 `1c76e4a`）を現行mainへ照合し、4件のP1（16bit JOINTS、source skin後の下流編集、PhysBones source asset、選択mesh単位のskin判定）は実装・回帰済みとして [review receipt](docs/reviews/2026-09-13-Consistency-Review.md) に固定した。最新Core artifactは `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4bbb7b6347fd456ead93289028b5571a`。MCPのinline captureはPNG base64だけをImageContentBlockへ渡し、structured/text側には画像ごとのcamera・撮影条件・hashを残すよう修正した。PhysBones再適用時の削除curve初期化と、chain配列順に依存しないmanaged marker再利用（stable target/name/root優先）も追加した。I04-Aの完全source保存/GUI接続、mesh属性/POSITION/NORMAL/TANGENT morph変換、source slot weight/一般bind deformer、GLB全JOINTS_n/WEIGHTS_n候補読取、native weight packageとrig session v5・GUI接続、評価済みgraph meshへのsource skin adapter、authored poseからのsource palette生成、Workbench取込後/揺れ再生中のsource skin表示接続まで追加。複数graph objectのsource rig sessionをgraph IDで束ねる`imported-rig-sessions.nyaforge.bin` attachmentとactive object連動表示も追加。I04-Bの入口として複数mesh/instance/skin参照を元indexで保持するGLB scene inventory、指定mesh/skinを選ぶ静的・skinned候補読取、Workbenchの候補確認・選択GUIを追加した。今回、native documentへ複数objectとactive objectを追加し、`object.select`、graph objectの追加、Save/Open、Workbench対象切替、既存1object互換を実装した。GLB取込は既存graph projectへ新しいgraph objectとして追加し、取り込んだobjectをactiveにする。さらに非active objectを読み取り専用の背面として同じviewportへ表示し、表示切替と全対象Frameを追加した。複数objectを個別に読み戻せる`multi-object.nyaforge-bake.json`パッケージ出力をGUI/MCPへ接続した。SIM-01の共通secondary-motion契約（安定ID、固定頂点、collider、出力種別、adapter能力、unknown version保持付きNYSM v1 codec）とVRM1 resolved spring migration、SIM-02のPhysBones target DTO／NYPP v1 codec／loss report境界／schema 4 attachment保存／Workbench状態表示、PhysBones target package、UnityBridgeの管理対象限定writer／reflection backend／合成receiver検証、receiverの明示stable binding保存を追加した。SIM-03Aとして揺れプレビューのGUI/MCPライフサイクル（play/pause/reset/rebuild/fixed-step/state）を共通ownerと外部MCP transportへ接続し、SIM-03Bとして固定step連続PNG・hash・実行条件のrun記録と外部MCP経路を追加した。今回はGLB primitiveの材質slotと基本PBR係数（linear baseColor、metallic/roughness、emissive、alpha）をnative `StandardMaterial`／`AssignMaterials`へ接続し、埋め込みbase colorのPNG/JPEGをnative Paintノードとして保存・表示できるようにした。外部texture/image/sampler等の未保持診断は残した。複数objectのmesh結合、共有mesh/skin/morph参照は未完了。同一skeleton／poseを共有する複数SkinDeformの同時評価は回帰済み。P1レビュー対応として16bit JOINTS読取、選択mesh単位のskin判定、source skin後の全graph編集保持、PhysBones source asset同梱を実装した。さらにrig/morphを含むgraphの`project.nyaforge.json` feature-preserving exportを追加し、静的Bakeとの出力境界を明示した。直近Core448件合格。Windows Playerの埋め込み材質画像接続を含むbuildとAuthoring suiteも合格した。実RadDollV3 VRMでWindows Playerの取込→EditMesh頂点編集→native Save/Open→標準SkinnedGeometry GLB出力を一周し、Core426件とUnity Bridgeも合格した。高密度PaintのWindows Playerで30 frameの実間隔、min/average/p95/max、GC回数、managed heap、Unity allocated/reserved bytes、targetFrameRate/vSync/render間隔/GPU名を `dense-paint-profile.json` へ保存した。合成fixtureでは平均・p95・最大約66.67ms、250ms観測境界超過なし。実RadDollV3 VRMでは埋め込みbase-color画像の上限超過を診断付きで省略し、sRGB端点をクランプ、複数material slotの局所頂点リマップを通して取込→EditMesh→Save/Open→標準skinned GLB→再取込を合格した（Player `Builds/RealModelMaterialClampV4/NyaForge.exe`、report `Artifacts/Authoring-20260913-045250-aab62f6896e0479b93da4a562d8573eb/report.json`、Core446）。任意モデル全般、実操作、性能、実VRChat受入は未完了。

### 直近の実装カード（2026-09-13）

- **T07 / 小物テンプレートと複数対象出力**: `PolygonPrimitives.Choker` に低ポリ輪形状（24×8、192頂点・192 quad、UV/法線/接線付き）を追加し、制作画面の **チョーカー形状を追加** から空projectまたは既存graph projectへgraph objectとして追加できるようにした。頂点編集、Undo対象の文書revision、Save/Open、複数対象Bakeを同じcommand経路で確認。Player `Builds/ChokerTemplate3/NyaForge.exe` の800x600 suite（`Artifacts/Authoring-20260913-054143-72427e21d3784698b20b2456ac133dba/report.json`）とUnity 2022.3.22f1 Bridge（`Artifacts/BridgeReceiver-20260913-054215-808-287af6dc0f3f438b9192877d94fe8d7b/bridge-report.json`）がPASS、Core **448 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-5aa6b693725845e099538e7e6d4f7ced`）。複数対象Bakeのobjectフォルダ名を連番だけへ短縮し、Windowsの長いUUID階層でatomic blob writeが失敗する経路も修正した。実アバターへの自動fit・実マウス受入・VRChat内の見た目は別境界として残る。

- **T06 / 公開GLB取込スモーク**: privateアバターに依存せずWindows取込経路を再現できる `Tools/New-NyaForgeGlbFixture.ps1` を追加した。静的mesh 0と2骨skinned mesh 1、PBR赤材質を含む小さなGLBを生成し、`Tools/Test-NyaForgeAuthoring.ps1 -ImportModel` で候補確認→mesh 1取込→EditMesh頂点編集→native Save/Open→標準skinned GLB出力→再取込を実行。Player `Builds/UiNarrowStatusV2/NyaForge.exe` の800x600 report `Artifacts/Authoring-20260913-052931-1a491563b9374f8dbbf19fd14d4298b5/report.json` がPASS、Unity 2022.3.22f1 Bridgeも `Artifacts/BridgeReceiver-20260913-053019-067-4cc5e17e55284e64b2364188415e7b09/bridge-report.json` でPASS。fixture生成後にGLB宣言長・JSON chunk・mesh/skin属性を検証済み。実マウス操作、任意実素材、実VRChat受入は別境界として残る。

- **T05 / Windows UI可読性**: 制作対象の長いGUIDは先頭8文字をボタンへ表示し、完全なobject identityはツールチップへ残して横方向の文字欠けを避けた。ステータス欄を狭い画面でも1行に固定し、表示更新でviewportが縮む問題を防いだ。ステータス全文もtooltipへ保持する。Player `Builds/UiNarrowStatusV2/NyaForge.exe` の800x600 Authoring suite（report `Artifacts/Authoring-20260913-052112-6c8f4cfef1e1407c9e807eaa5ba711eb/report.json`）と1600x1000 suite（report `Artifacts/Authoring-20260913-052214-0691af25c0ad410183470d1c5c8fb86e/report.json`）が合格。Unity 2022.3.22f1 BridgeもV1で合格（`Artifacts/BridgeReceiver-20260913-051636-908-559437b2ca5d48779a1f89f38eadcc7d/bridge-report.json`）。実マウス・DPI差・実VRChat受入は別境界として残る。

- **I04-E / 必須拡張ガード**: 完全なadapterがない `extensionsRequired` はGLB/VRM取込前に `UNSUPPORTED_EXTENSION` で拒否し、`extensionsUsed` は従来どおり partial 診断として保持する。Core 446件、Windows Authoring suite（Player `Builds/RequiredExtensionGuardV1/NyaForge.exe`、report `Artifacts/Authoring-20260913-045811-6f323347842b4d548e95a60aebd4700b/report.json`）合格。

- **I04-E / 実モデル材質診断と複数slot GLB往復**: 埋め込みbase-color画像は1024x1024以内だけnative Paintへ保持し、超過/不正形式は明示warningで省略する。sRGB係数は0〜1へクランプし、skinned GLBの複数material slotはスロットごとの局所頂点アクセサへ分割して再取込時の重複計上を防ぐ。Core 446件、RadDollV3 Windows Player smoke（取込→編集→保存/Open→標準skinned GLB→再取込）合格。

- **I04-C / GLB予算分離**: GLB/VRMの取込・標準GLB出力は128 MiB、1 mesh 200,000頂点までを専用予算で検査する。native blobの16 MiB予算は維持し、実素材を通すための拡張を他形式へ波及させない。
- **I04-C / 編集済みskin出力**: `SkinnedGeometry` profileは単一graphのrest pose・identity transform・4 influenceを対象に、`SkinnedGeometryExtended` profileは最大32 influenceを全JOINTS_n/WEIGHTS_n setで保持し、どちらもトポロジー不変の`EditMesh`頂点編集をGLBへ出力する。任意pose、非ゼロmorph変形、未対応nodeは二重適用を避けて拒否する。
- **I04-E / 取込診断**: `ImportedMeshSource.Diagnostics` / `ImportedSkinnedMeshSource.Diagnostics` に材質、アニメーション、extensionsRequired、extensionsUsedをコード付きで保持し、既存Warningsにも同じコードを表示する。`import-diagnostics.nyaforge.json` attachmentへgraphId単位で保存し、MCP graph inspection・取込後status・GUI詳細パネルへ公開する経路まで実装済み。Core 441件とWindows Playerでsnapshot往復、active graph表示、blocking/partialの区別を回帰した。依存資源を含む完全な材質・animation・未知拡張の保存は未完了。
- **I04-E / GLB材質出力往復**: 標準GLBのStatic/Skinned/Extended writerへ評価済みStandardMaterialと埋め込みbase-color画像を接続し、material slotがある場合はprimitiveを分離してmaterial indexを保持する。画像は決定的PNGとしてBIN bufferViewへ埋め込み、linear PBR係数をglTFのsRGB factorへ戻す。Core 446件でwriter→readerのPBR/embedded image往復を確認。外部画像・追加texture map・sampler・animation・VRM拡張は未対応。
  MCPの `forge_export_glb`（static / skinned / skinned_extended）を追加し、saveTarget配下への出力、revision固定、既存出力先の再実行拒否、文書state不変を外部sidecar経由で確認。Windows Player `Builds/McpGlbExportV3/NyaForge.exe`、Authoring suite PASS、MCP transport build PASS。
- **I04-E / 基本PBR材質ルーティング**: GLB primitiveのmaterial indexを選択mesh単位で検証し、baseColorFactor（sRGB→linear）、metallic/roughness、emissive、alpha mode/cutoffを`GlbMaterialSource`として保持する。Workbenchの静的／skinned取込で全submesh slotが揃う場合、`StandardMaterial`／`AssignMaterials`へ接続する。埋め込みbase colorのPNG/JPEGはUnityでRGBAへ変換し、未バインドのnative Paintノードとして保存・表示する。未バインド画像は誤編集を避けてGUI表示専用とし、texture/image/samplerの未解決参照・追加拡張は診断を残す。Core 446件、Windows Player `Builds/EmbeddedMaterialImageV2/NyaForge.exe` build、Authoring suite 74 checksで回帰済み。依存資源込みの完全材質往復は未完了。
- **I04-C / 拡張GLB回帰**: `GlbExportService.ExportSkinnedExtended`とWorkbenchの「拡張GLB（全weight保持）」ボタンを追加。8 influence fixtureをGLB writer→`GlbSkinImporter`で往復し、全weight set・骨数・正規化合計を確認した。Windows Player `Builds/ExtendedGlbV1/NyaForge.exe` と Authoring suiteもPASS。
- **保存往復回帰**: serialized skin bindingを再正規化せずfloat32値を保持するよう修正し、4 influenceのbyte identity回帰を追加。実RadDollV3 `RadDollV3_VRM.vrm`（private ZIP内、45,341,584 bytes、body 129,348 vertices、171 bones）を読み取り専用に選択取込し、ProjectStore Save/Open後のdocument state hash一致を確認した。private素材はpublic repoへ同梱しない。
- **active object編集経路**: Workbenchのグラフ、表示投影、材質、paint、rig、morph、UV、MCPを`Document.ActiveObject`基準へ統一し、複数対象で切替後の頂点編集が非active graphを変更しないPlayer回帰を追加した。非active objectは従来どおり読み取り専用backdropとして扱う。
- **GLB skin編集導線**: 選択したGLB/VRM skin graphへrest-space `EditMesh`を自動挿入し、取込直後に既存の頂点選択・移動・Undo/保存経路へ入れるようにした。FBX/BLEND直接取込、humanoid自動配置、任意poseを編集段へ焼き込む処理は対象外。
- **T05 / 実モデル一周スモーク**: Tools/Test-NyaForgeAuthoring.ps1 -ImportModel の任意GLB/VRM検証入口を追加。private tempのRadDollV3 VRM（mesh 1 / skin 1、129,348 vertices、171 bones、35 morphs）で候補確認、EditMesh頂点編集、native Save/Open、標準skinned GLB出力を確認した。実マウス・実VRChat受入とは分けて記録する。

## 開発の入口

今回のSIM-02B実装では、receiver GUIの保存・読込・適用前に`PhysBonesBindingValidator`を通す。必要stable BoneId／collider groupの欠落、avatar root外、重複Transform、空groupを共通診断し、vendor SDKの型判断はbackendへ委譲する。
target package manifestには受け取り側の完全修飾`ComponentTypeName`を保存し、receiverはその型を解決する。旧manifestだけ既定のVRChat PhysBone型へフォールバックする。
reflection型解決は`VrcPhysBonesReflectionResolver`へ分離し、assembly-qualified nameの厳密解決、未導入、非Component、曖昧な複数assemblyを非破壊診断する。

- **標準skin出力の安全境界**: 4 influenceを超える入力をGLBへ黙って切り捨てないよう、`GLB_SKIN_INFLUENCES`で明示拒否する。native出力は高影響数を保持する経路として残す。
作業先: `Z:/TextureVoice_local/git/NyaForge`。Windows先行、macOSは将来。
読む順: このファイル → [モデル交換仕様](docs/Model-Interchange-Spec.md) → [実素材調査](docs/Real-Asset-Import-Plan.md) → 対象コード。製品全体の範囲は [開発計画](docs/Development-Plan.md) と [設計v2](docs/NyaForge-Authoring-Design2.md) を参照。[文書一覧](docs/README.md)参照。
製品目標は小物の制作・出力を一周し、低ポリ全身キャラ、品質向上へ進むこと。設計v2は製品方針、v1は背景資料。設計中の外部依存・機能は採用済みや実装済みを意味しない。

## 次に実行するタスク

採用した方針: **nativeを制作の正本、GLB/VRMを交換形式、FBX/BLEND/Unityを原本として区別する。** 未対応データを黙って削らず、情報ごとの能力と保持結果を報告する。Blender調査は任意の開発ツールで、標準制作の必須依存へ変更しない。詳細・完了条件は [モデル交換仕様](docs/Model-Interchange-Spec.md)。ボーンのフィードバックは下記のSIM-02B→SIM-07Aを主経路にし、完了済みSIM-03A/03BのGUI/MCPライフサイクルと固定step証拠を共通土台として使う。SIM-04〜06は主経路を遅らせない任意評価へ分離する。

| 状態 / ID | 実行する作業 | 完了条件・依存 |
|---|---|---|
| [ ] I04-A / P1 **継続** | source local/world、一般TRS/matrix、inverse-bind、法線/接線変換の基盤 | 数値/reader/codec/GUI生成/native原本なしOpen、mesh/POSITION/NORMAL/TANGENT morph変換、SourceSkinBinding/SourceSkinDeformer、GLB全JOINTS_n/WEIGHTS_n候補、NYSPとrig v5/native/GUI接続、評価済みGraphMeshValueへのSourceSkinGraphAdapter、authored poseからのSourceSkinPosePalette、Workbench取込後/揺れ再生中の自動表示を追加済み。同一skeleton hashの複数graph objectをshared skinでSkinnedGeometry／Extended GLBへ出力する回帰を追加。次は異なるskeletonの明示的な出力境界、再利用mesh/objectと失敗時原子性を検証。sparse weightは未対応。float weightとnormalized UBYTE/USHORT weightを読取可能 |
| [ ] I04-B / P1 **継続** | 複数mesh/instance/skin、source→制作ID対応 | `GlbSceneInventoryReader`でmesh/primitive数、node instance、skin joint参照、node world transformを元indexのまま候補化し、候補確認・node instance選択GUIを追加した。`SharedResources`でmesh aliasと複数skin参照を明示し、GUIへ「取込後は個別編集」「node選択が必要」を表示する。native documentは最大64 objectのactive object方式へ拡張し、`object.select`、graph object追加、Save/Open、Workbench対象切替を検証済み。非active objectは読み取り専用の背面表示とFrame対象にでき、複数object package出力も追加した。GLB取込はgraph projectへ新objectとして追加する。複数graph objectのrig sessionをgraph IDで保存・active objectへ再選択する経路を追加済み。同一skeleton hashの複数skinned meshはshared skin出力へ対応。残りは異なるskeletonの結合、同名morph/共有mesh・skin参照の完全dedup、実素材受入。方針は`docs/Shared-Resource-Policy.md`に固定した。 |
| [ ] I04-C / P1 **継続** | rig/weight/morph容量とcodec/hash/表示/出力 | nativeは512骨・32 influence・512 morphへ拡張し、257骨・18weight・単一mesh262morphの削減なし往復、GLB全JOINTS_n/WEIGHTS_n取込、`SkinnedGeometryExtended`出力を回帰済み。標準SkinnedGeometryは互換上4 influenceを明示拒否する。実GLB受取先・VRChat側確認が残る |
| [ ] I04-D / P1 | 標準FBX Bridge入力と任意の変換adapter | Blender必須化なし。依存検出・変換前後比較・原本保護・失敗/取消を確認。実取込はA〜Cに依存 |
| [ ] I04-E / P1 **継続** | 機能report、材質/animation/VRM意味情報/未知拡張の保持とGUI/MCP | GLB importerのコード付きdiagnosticsをnative attachmentへ保存し、MCP graph inspection・取込後status・GUI詳細パネルで表示。未実装の `extensionsRequired` は取込前拒否済み。依存資源込みopaque保持、既知VRM内の未保持field、完全材質/animation保持が残件 |
| [ ] T04 / P2 **継続** | 時間超過・性能受入 | 高密度Paint合成fixtureでframe時間（30 samples、p95/max）、GC/managed heap/Unity allocatorを `dense-paint-profile.json` へ保存し、250ms観測境界を記録した。次は実アバター比較、長時間working-set、停止ポリシーを別試験で定める |
| [ ] T05 / P1 **継続** | Windows実素材・実操作と受取側 | RadDollV3 VRMで取込→EditMesh→Save/Open→標準skinned GLB→再取込を合格（`RealModelMaterialClampV4`）。bounded avatar-surface fitは合成fixtureとWorkbench回帰済み。残りは実マウス・DPI/文字欠け・実アバターでのpose/揺れ・実VRChatの出力受取確認、追加texture mapと大画像の完全保持 |
| [ ] T06 / P2 | 外部MCP metadata保存受入 | 内部handlerと区別し、transport経由で保存/Open・失敗保護・再試行を確認 |

## 完了した前提と残る境界

### ボーンの追加フィードバック: 実行タスク（2026-09-12）

[揺れ・布adapter計画](docs/Secondary-Motion-Plan.md)へ採用方針・依存・完了条件を整理した。**PhysBones優先、MagicaCloth2は任意adapter**。SIM-01のUnity非依存契約とcodec、SIM-02AのPhysBones target DTO／loss report境界、schema 4 attachment保存、Workbench状態表示、target package、UnityBridgeの管理対象限定writerとreflection backend、受け取り側の明示stable binding保存を実装した。実VRChat SDK／アバター動作は未確認で、直近はSIM-02Bの実SDK受け取り側を先に進める。

今回の判断を次の一本の流れで固定する。**native制作データ → 版付きtarget profile → 交換可能なsimulation adapter → GUI/MCPの共通実行所有者 → 固定stepの証拠 → 出力先ごとの受入**。PhysBonesはVRChat向けのP1経路として扱い、MagicaCloth2はUnityアプリ用の任意評価へ隔離する。MeshClothはBlendShapeとの重複と負荷を確認してからC3へ進める。vendor componentや独自scriptをVRChat出力へ持ち込まず、未対応項目はloss reportで停止・表示する。

### フィードバックから分解した実装カード

- **SIM-02B / 実SDK受け取り**: SDKの版と完全修飾型を先に固定する。target packageのmanifest/profile/skeletonを読み、stable BoneIdとcollider groupを手動割当できる状態から、managed componentだけを生成・更新する。書込み前にunsupported/warningを検査し、失敗時は原子 rollback。SDKが無いpublic buildは起動・保存・診断を維持する。
- **SIM-03B / 連続撮影と証拠**: 1 run に `input/config hash`、adapter・package版、target、Unity/build、fixed step・warmup、pose/root/collider条件、各frameの画像hash、失敗ログを束ねる。撮影はtransient previewだけを使い、native revision・保存ファイル・制作姿勢を変更しない。実VRChat受入の証拠と混ぜない。
- **SIM-07A / PhysBones受入**: 同じ髪束fixtureでroot移動、停止、旋回、pose、colliderを確認し、NyaForge preview、受取Unity、VRChat内を別々の結果として記録する。preview画像だけでVRChatの再現を判定しない。
- **SIM-04〜06 / 任意評価**: MagicaCloth2のBoneCloth→MeshCloth→BoneSpringを独立adapterとして順に評価する。vendor packageはpublic repoへ同梱せず、runtime構築待ち・固定根・衝突・破棄・性能・morph境界を小さな髪束/布で確認する。PhysBonesのP1経路を置換しない。

| 状態 / ID | 作業 | 完了条件・依存 |
|---|---|---|
| [x] SIM-01A / P1 | 共通secondary-motion契約 | `SecondaryMotionAsset`、stable chain、fixed vertex、collider group、bone/mesh output、adapter capability、`NYSM` v1 codec、VRM1 resolved spring migrationを実装し、unknown versionとstale skeleton/topologyを安全に扱う |
| [x] SIM-01B / P1 | native attachmentとVRM0移行 | `secondary-motion.nyaforge.bin` として `NYSM` をschema 4へ接続し、VRM0 source-node subtree→stable BoneId移行、未知wire版の保持・GUI表示、保存後のskeleton/topology stale診断、明示的なbone/vertex rebind API、同一BoneIdのGUI再bindまで実装。残りは実素材確認。SIM-03Aの前提 |
| [x] SIM-01B-R / P1 | stale時の明示rebind操作 | `SecondaryMotionRebind.Apply`でbone/vertex対応表を必須化し、Workbenchは同一BoneIdの安全な場合だけボタンを有効化。リグのPose/Binding再bindと二次運動attachment再bindを分離し、推測による自動対応をしない |
| [x] SIM-02A / P1 | PhysBones target packageと合成Bridge | `PhysBonesTargetProfile`／`PhysBonesChain`、`NYPP` v1、schema 4 attachment、loss report、target package、reflection writer、managed-only、branch preflight、receiver Windowを追加し合成fixtureで検証済み |
| [x] SIM-02B-1 / P1 | receiver binding事前検証 | `PhysBonesBindingValidator`をGUIの保存／読込／適用前へ接続し、必須stable BoneId／collider group、avatar root配下、重複Transform、空groupを共通診断。vendor SDKの型判断はbackendへ委譲 |
| [x] SIM-02B-2 / P1 | receiver component型の固定 | target package manifestへ完全修飾`ComponentTypeName`を保存し、receiverのreflection resolverへ渡す。旧manifestは既定型へフォールバックし、custom型と旧形式をCore／Bridgeで往復確認 |
| [x] SIM-02B-3 / P1 | receiver型解決診断 | `VrcPhysBonesReflectionResolver`を分離し、assembly-qualified型、未導入、非Componentの診断をscene変更なしで確認。GUIの適用失敗へ理由を返す |
| [x] SIM-02B-4 / P1 | receiver事前診断 | `PhysBonesBridge.Inspect`／`InspectPackage`を追加し、本適用と同じtarget／capability／scene／managed preflightをcomponent生成なしで実行。GUIの「事前診断（書き込みなし）」へ接続 |
| [x] SIM-02B / P1 | 実SDK受け取り側 | `NyaForgePhysBonesBinding`へpackage identity付きのstable BoneId／collider group手動割当を保存・読込できるようにし、reflection member catalogは継承元private field/propertyも対象にした。VRChat SDK 3.7.6（Unity 2022.3.22f1）でtarget packageをRead→InspectPackage（非破壊）→ApplyPackage（実VRCPhysBone生成・stable root設定）まで確認。未対応値は書込み前にloss reportで停止し、未管理componentを変更しない。SDK未導入時のpublic buildは維持 |
| [x] SIM-03A / P1 | GUI/MCPの設定と再生所有者 | GUI・内部MCP handler・外部sidecar toolにplay/pause/reset/rebuild/fixed-step/stateを接続し、同じtransient owner／generationで保存対象外、編集・作品切替時の破棄を確認済み。非同期vendor構築の実SDK接続は後続タスク |
| [x] SIM-03B / P1 | 連続撮影とbackend証拠 | `SecondaryMotionCaptureRecord`／codec、固定1/60秒・warmup・最大8frame・pixel budget、input/config hash、adapter／package版、target、Unity/build、pose/root/collider条件、各PNG hashと失敗statusを1 runへ束ね、`forge_secondary_motion_capture`の実MCPで3frameを確認済み。native revision／保存／制作姿勢は不変。実VRChat受入の証拠とは分ける |
| [ ] SIM-04 / P2・任意 | MagicaCloth2 BoneCloth最小評価 | vendor packageをpublic repoへ入れず任意assemblyへ隔離。自作髪束1本でruntime生成・構築完了待ち・固定根・sphere衝突・rebuild/reset/破棄・写真列を確認し、未導入buildも成功させる。SIM-02B/03Aを置換しない |
| [ ] SIM-05 / P2 | MeshClothとmorph境界 | 小さい布で固定／可動領域を指定し、BlendShape変形頂点との重複を拒否または分離案内する。法線更新と時間／GC／メモリをSIM-04と比較 |
| [ ] SIM-06 / P2 | BoneSpringとUnityアプリ向け出力 | BoneSpring fixtureの保存・再構築と、Magica用profileの依存不足診断・版照合・再出力を確認 |
| [ ] SIM-07A / P1 | PhysBones target受入 | SIM-02B＋SIM-03A/B後。同じ髪束でroot移動・停止・旋回・pose・colliderを受入し、対応SDK／受取Unity／VRChat内の結果を別々に記録 |

SIM-02Bのbinding validationは実装済み。次は対象SDKの版・完全修飾型を固定し、実componentの生成／更新を同じ明示mappingで確認する。

実行順は **SIM-01B残り → SIM-02B → SIM-07A**（SIM-03A/03Bは完了）。SIM-01BはI04-Aと並行し、SIM-04〜06は主経路の受入を置換しない。C2の必須は選んだ出力先で髪束1本が動くこと。Magica導入時の購入・vendorソース取得／配布はこのタスク化では実行せず、public repoには自作adapter・fixture・設定schemaだけを置く。

### 既存工程

- [x] **T01**: 全source node階層と元children順をrig session v3へ保存。v1/v2は階層不明を維持。
- [x] **T02**: VRM0 subtree/一時骨格/実行所有者/共通再生GUIを対応profileで接続。両形式のPlayer handler往復合格。
- [x] **T03**: ローカルFBX3件を読取調査。アバター20mesh、全3件257骨、アバター最大18deform bone影響/頂点・単一mesh262morphを確認。生データはprivate。変換・実importの成功はまだ確認していない。
- [x] **R01〜R12 / I03-A**: 記録した自動検証範囲で修正・adapter接続済み。[Rig/VRMレビュー](docs/reviews/2026-09-12-Rig-Vrm-Review.md)、[R11/R12](docs/reviews/2026-09-12-Current-Checkpoint.md)、[再生checkpoint](docs/reviews/2026-09-12-Playback-Checkpoint.md)。
- [ ] **I03-B/C / A01**: 可動world root、揺れるnodeのcollider更新順と参照runtime比較、実素材/実操作/性能の受入は残る。I03-CはVRM0/1両方のGUI/handler接続済みだが、受入全体は未完了。
- [ ] **I04 / C0〜C5**: 任意モデル取込と制作/出力の製品全体は未完了。skin/morph出力・受取側確認などを [開発計画](docs/Development-Plan.md) から省かない。

## 直近の証拠


- GLB normalized weight Player regression: Windows Player **PASS**（build `Logs/build-all-20260913-012119-799.log`、`Builds/Windows-NormalizedWeights/NyaForge.exe`）、Authoring **PASS**（`Artifacts/Authoring-20260913-012142-6ed7e6ab7d84430196e7863c51d2b4ca/report.json`）、Unity Bridge **PASS**（`Artifacts/BridgeReceiver-20260913-012215-147-4bb7328c3ab4483db3e449a60a747220/bridge-report.json`）。既存のVRM0/1・GUI・保存/出力回帰を維持した。
- GLB weight validation Player regression: Windows Player **PASS**（build `Logs/build-all-20260913-012647-017.log`、`Builds/Windows-WeightValidation/NyaForge.exe`）、Authoring **PASS**（`Artifacts/Authoring-20260913-012705-60078ac2fcfe4a04b05ba8764911e9ac/report.json`）、Unity Bridge **PASS**（`Artifacts/BridgeReceiver-20260913-012737-339-72f1beb1a02c46acb2ec5d241329e959/bridge-report.json`）。既存のGLB/VRM import failure isolationを維持した。
- GLB normalized weight regression: Core **425 passed / 0 failed**（`Logs/core-weight-validation-20260913.txt`、artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0431fca3ee01454eba28285e93dbfcf7`）。source skin importerとskin importerでnormalized UBYTE weightをfloatへ復元し、既存のfloat形式と同じnormalized bindingになることを確認。
- GLB/VRM実素材・標準GLB回帰: Core **423 passed / 0 failed**（`dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore`、artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-98124952dc914a0c9ce5aeb112e131ac`）。16bit JOINTS、選択mesh単位のskin判定、編集済みrest-pose skin出力、4 influence serialized bindingのbyte identityを確認。privateのRadDollV3 VRMを読み取り専用に監査し、全10 mesh/skin pair（body 129,348 vertices、171 bones）を取込、ProjectStore Save/Open後のstate hash一致を確認した。private素材はpublic repoへ同梱していない。
- 実RadDollV3 Playerスモーク: Windows Player **PASS**（build `Logs/build-all-20260913-011120-036.log`、`Builds/Windows-RealModelSmoke7/NyaForge.exe`）、Authoring **PASS**（`Artifacts/Authoring-20260913-011139-6d930c47c0c34967b92a95aa5df83ff3/report.json`）。private tempの`RadDollV3_VRM.vrm`を読み取り専用に使用し、mesh 1 / skin 1（129,348 vertices、171 bones、35 morphs）で候補確認、rest-space EditMeshの頂点編集、native Save/Open後のhash一致、標準skinned GLB出力を確認した。Unity Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-011244-686-8672912d9a884e5bbb10e72bcfa51eda/bridge-report.json`）。実マウス・実VRChat内動作・private素材の公開を意味しない。
- Active-object編集回帰: Windows Player **PASS**（`Logs/build-all-20260913-004333-596.log`、`Builds/Windows-ActiveObject1/NyaForge.exe`）、Authoring **PASS**（`Artifacts/Authoring-20260913-004356-d55db89466a9437db514dadad265b285/report.json`）。2つのgraph objectをSave/Openし、active objectを切り替えて頂点編集した結果、対象以外のgraph hashが不変であること、backdrop表示切替・Frameが維持されることを確認。Unity Bridge **PASS**（`Artifacts/BridgeReceiver-20260913-004426-425-63f6bf9efce640a09bb50dc67064bc3d/bridge-report.json`）。
- GLB skin取込→頂点編集回帰: Windows Player build **PASS**（`Logs/build-all-20260913-004702-147.log`、`Builds/Windows-ImportedEdit1/NyaForge.exe`）、Authoring **PASS**（`Artifacts/Authoring-20260913-004723-8b656f46809f4b31993f0cbd6aa5700f/report.json`）、Unity Bridge **PASS**（`Artifacts/BridgeReceiver-20260913-004759-056-1a634ad110c04d088a00d8ecb69a978a/bridge-report.json`）。選択mesh/skin取込時のEditMesh自動生成、頂点移動によるgraph output hash変化、既存候補・複数rig session回帰を確認。実RadDollV3のGUI目視受入ではない。
- Windows Player **PASS**: `Logs/build-all-20260913-003858-381.log`、`Builds/Windows-GlbProfiles6/NyaForge.exe`（`NYAFORGE_PLAYER_OK`）。Authoring **PASS**: `Artifacts/Authoring-20260913-003917-d47bd96805f14dc38ae049172c9aea12/report.json`。Unity Bridge **PASS**: `Artifacts/BridgeReceiver-20260913-003948-609-589ce9c598e347dd99e6061fbd3f6e2e/bridge-report.json`。

- Multi-object authoring foundation: Core **413 passed / 0 failed** (`Logs/core-multi-object-foundation-v6.txt`、temporary output `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-1b7178a1a74946b6b3fb680843792dcd`)。graph/staticの2 object追加・active選択・`object.select`・state hash・Save/Open・active object評価と、複数static objectでの編集保持を確認。最終Windows Player build **PASS** (`Logs/build-player-20260912-230432-122.log`、`Builds/Windows-MultiObjectBackdrop/NyaForge.exe`)。Player Authoring **PASS** (`Artifacts/Authoring-20260912-230451-673c5685904b451e91b19f0f469888e3/report.json`)、Bridge **PASS** (`Artifacts/BridgeReceiver-20260912-230523-234-756d4f789c794e14bae97eb3c343130f/bridge-report.json`)。非active objectの読み取り専用backdrop、表示切替、Frame、Save/Openを検証した。これはactive objectを一度に編集する基盤の証拠で、結合出力、共有参照、実素材、実操作・画像目視、実VRChat受入を含まない。
- Multi-object package export: Core **414 passed / 0 failed** (`Logs/core-multi-object-export-v2.txt`、temporary output `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0a53bf40af3142579164c8697d022158`)。複数graph objectを個別Bakeへ出力し、`multi-object.nyaforge-bake.json`から全manifestを検証、文書revision/state hash不変と既存出力先の拒否を確認。mesh結合やskin/morphの統合出力ではない。
- Feature-preserving graph export: Core **418 passed / 0 failed** (`Logs/core-native-feature-export.txt`、temporary output `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-20bbfabc91d94dba9293bec879dd533a`)。rig/morph nodeを含むgraphは`project.nyaforge.json` native packageへルーティングし、source workspaceのdocument/revision/dirty状態を変更せず、再読込後もMorphSetを保持することを確認。標準GLBのStaticGeometry／SkinnedGeometry profileは別途追加済み。Windows Player **PASS** (`Logs/build-player-20260912-233249-128.log`、`Builds/Windows-NativeFeatureExport/NyaForge.exe`)、Authoring **PASS / 73 checks** (`Artifacts/Authoring-20260912-233319-d7adb0e572cd4e2c8ef097160e9dfba0/report.json`)、Bridge **PASS** (`Artifacts/BridgeReceiver-20260912-233420-732-a4c6ac0538ca4a80ac774259d3db1ed6/bridge-report.json`)。静的Bake形式は維持し、標準GLBは今回追加した明示profileで出力する。標準VRM、任意pose/morphのskin変換、材質・animation、mesh結合は未完了。
- Multi-rig session table: Core **418 passed / 0 failed** (`Logs/core-multi-rig-sessions.txt`、temporary output `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-a074d326f26a420cb928d780ba0a854a`)。graph IDごとのImportedRigSessionを`imported-rig-sessions.nyaforge.bin`へ版付き保存し、複数sessionの順序・source skin/binding・identity往復を確認。Windows Player **PASS** (`Logs/build-player-20260912-235330-393.log`、`Builds/Windows-MultiRigSessions2/NyaForge.exe`)、Authoring **PASS** (`Artifacts/Authoring-20260912-235349-f97c7235001d421eb588b19ff602a71e/report.json`)、Bridge **PASS** (`Artifacts/BridgeReceiver-20260912-235419-288-43be89d36c63426e979704635857576e/bridge-report.json`)。旧single rig attachmentはfallbackで読める。複数meshの結合・共有参照・複数SkinDeform同時評価は未完了。
- P1 consistency fixes: Core **417 passed / 0 failed** (Logs/core-p1-final.txt、temporary output C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-3c1404b7cc604ef0bf33e4b52ddb4563)。8/16bit JOINTSの同値読取、skin付きmeshと同居する静的小物の選択取込、source skin適用後も下流graph編集を保持する評価、PhysBones target packageのsource asset同梱とreceiver側自動検証を追加。Windows Player **PASS** (Logs/build-player-20260912-p1-final2.log、Builds/Windows-P1ConsistencyFinal2/NyaForge.exe)、Authoring **PASS** (Artifacts/Authoring-20260912-232526-29c4d7d74e9d43f8b2aae80c26032d6c/report.json, 73 checks)、Bridge **PASS** (Artifacts/BridgeReceiver-20260912-232610-682-ea55fcb213e149e4aca246b668ed4e1c/bridge-report.json)。実SDK・実アバター・実VRChatの受入、mesh結合・共有参照・複数SkinDeformは未完了。

- SourceMeshTransform: Core **378 passed / 0 failed** (`Logs/core-source-mesh-transform.txt`)。非一様/shear/鏡映の位置・方向・面順/UV保持、morph ID維持とtopology再pin、morph適用との可換性、不正方向/stale入力を検証。一般skinのweight混合・GUI取込はまだ未接続。
- Windows-SourceMeshTransform build **PASS** (`Logs/build-player-20260912-170453-386.log`)。独立Coreモジュール追加のためPlayer GUI suiteは今回再実行していない。
- SourceSkinBinding/Deformer: Core **381 passed / 0 failed** (`Logs/core-source-skin-deformer.txt`)。slot順、32影響/vertex、rest inverse-bind相殺、pose paletteの位置/法線/接線/UV/topology保持、stale domain/未weight/不正paletteを確認。GLB weight decoder・native binding・GUI保存接続まで完了。既存graphの一般deform表示接続は未完了。
- GLB source skin importer: Core **384 passed / 0 failed** (`Logs/core-glb-source-skin-importer.txt`)。一meshの全primitiveでdense JOINTS/WEIGHTS set、slot順・vertex offset・skin参照をSourceSkinBindingへ接続し、unpaired/noncontiguous/type/sparse/range/宣言buffer不整合を拒否。normalized integer weightをfloatへ変換して保持し、sparseは未対応。
- Source skin package / rig v5: Core **385 passed / 0 failed** (`Logs/core-source-weight-native.txt`)。NYSPの行列＋全元weight往復、byte同一、切断/末尾/version/section破損、v5 Save/Openと旧v1〜v4情報境界を確認。GUI skinned取込とPlayer Save/Openは同じ変更で検証済み。
- Windows-SourceWeightNative2 build **PASS** (`Logs/build-player-20260912-172508-037.log`)、Player **PASS** (`Artifacts/Authoring-20260912-172528-cca84b685fc84c1eb596e2dec4dc785a/report.json`)。VRM0/1でGLB由来source package保持、原本なしOpen、揺れ再生、編集時停止、import失敗保護を確認。実マウス操作・任意実素材・画像目視の受入ではない。
- SourceSkinGraphAdapter: Core **387 passed / 0 failed** (`Logs/core-source-graph-adapter.txt`)。評価済みGraphMeshValueへsource paletteを明示適用し、meshだけを差し替え、domain/RestTransform/topologyと入力不変を保持。polygon編集値とstale topologyは拒否。
- Windows-SourceGraphAdapter build **PASS** (`Logs/build-player-20260912-173556-982.log`)、Player **PASS** (`Artifacts/Authoring-20260912-173622-7883a8c24efd4ba4a58c31245e28708c/report.json`)。既存VRM0/1 playback、source package Save/Open、import失敗保護を回帰確認。adapterの実マウス操作・任意実素材・画像目視の受入ではない。
- SourceSkinPosePalette: Core **389 passed / 0 failed** (`Logs/core-source-pose-palette.txt`)。sessionのsource joint slotとauthored BoneIdを検査し、`PoseTransform × T(-head) × sourceWorld`でrest cancellationとpose変形を確認。legacy sessionは完全source skinなしとして拒否。
- Windows-SourcePosePalette build **PASS** (`Logs/build-player-source-pose-palette.txt`)、Player **PASS** (`Artifacts/Authoring-20260912-174333-f634629d7b9b444189b96906ec2b0fb8/report.json`)。既存VRM0/1 playback、source package Save/Open、import失敗保護、rig graph回帰を確認。paletteの実マウス操作・任意実素材・画像目視の受入ではない。
- Workbench source skin display: Core **390 passed / 0 failed** (`Logs/core-source-graph-display.txt`)。評価済みgraphのSkinDeform前入力へsource paletteを適用し、取込直後のrest表示、authored pose変更、stale/legacy拒否を確認。
- Windows-SourceGraphDisplay build **PASS** (`Logs/build-player-source-graph-display.txt`、Unity `Logs/build-player-20260912-175730-836.log`)、Player **PASS** (`Artifacts/Authoring-20260912-175754-c5f5eff9c46646e5b075718e7323c699/report.json`)。取込直後と揺れ中の頂点をsource palette結果と比較し、VRM0/1の保存/Open、再利用mesh/object、失敗保護を回帰確認。実マウス操作・任意実素材・画像目視の受入ではない。
- GLB scene inventory: Core **393 passed / 0 failed** (`Logs/core-scene-inventory.txt`)。複数mesh resource、mesh instanceのnode/mesh/skin元index、nodeのsource-world transform、skin joint順を保持し、mesh/skin/nodeの明示型不正、範囲外参照、重複joint、skin-only nodeを拒否。
- Windows-SceneInventory build **PASS** (`Logs/build-player-scene-inventory.txt`、Unity `Logs/build-player-20260912-181457-233.log`)、Player **PASS** (`Artifacts/Authoring-20260912-181525-a0ffa6291c3a4931b99fc50b5d49c50e/report.json`)。既存VRM0/1取込・source palette表示・Spring再生・Save/Open・失敗保護を69 checksで回帰確認。inventoryは候補読取の自動検証で、複数対象のWorkbench公開、実マウス操作、任意実素材・画像目視の受入ではない。
- mesh/skin selection: Core **396 passed / 0 failed** (`Logs/core-final-396.txt`)。multi-mesh GLBを暗黙にmesh 0へ畳み込まず、選択したmeshのgeometry/morphとskinのsource indexを保持し、mesh index付きmorph identity、範囲外index、既存単一mesh APIの拒否を確認。
- Windows-MeshSelection build **PASS** (`Logs/build-player-mesh-selection.txt`、Unity `Logs/build-player-20260912-182005-738.log`)、Player **PASS** (`Artifacts/Authoring-20260912-182032-0b8589ed3d8a464f93b59c07f52942ff/report.json`)。既存WorkbenchのVRM0/1取込・source palette表示・Spring再生・Save/Open・失敗保護を回帰確認。選択候補の複数graph公開、実マウス操作、任意実素材・画像目視の受入ではない。
- Workbench mesh/skin selection GUI: Windows-ImportSelectionGui build **PASS** (`Logs/build-player-import-selection-gui.txt`、Unity `Logs/build-player-20260912-182741-785.log`)、Player **PASS** (`Artifacts/Authoring-20260912-182805-b75017ed69104ec5bcb4895c830547f7/report.json`)。自作multi-mesh fixtureで候補確認、mesh 1 / skin 0指定、取込後のsource payload/topologyを確認し、既存VRM0/1回帰を70 checksで実施。複数対象の同時公開、実マウス操作、任意実素材・画像目視の受入ではない。
- SIM-01 secondary-motion contract: Core **400 passed / 0 failed** (`Logs/core-secondary-motion.txt`)。`NYSM` v1のprofile／stable chain／fixed vertex／collider group往復、bone-poseとmesh-deformation出力の分離、unknown versionのopaque保持、skeleton/topology stale拒否、resolved springの共通topology移行、VRM1 sessionからのsource node→authored BoneId移行を確認。Windows-SecondaryMotionCore2 build **PASS** (`Logs/build-player-20260912-184902-690.log`)、Player **PASS** (`Artifacts/Authoring-20260912-184923-60fa1a94c49d4c67833fcb32277bdfdf/report.json`, 70 checks) で既存Workbench回帰も確認。native attachmentとVRM0 source-node移行はSIM-01Bで接続済み。PhysBones/MagicaCloth2実adapterは未接続。
- SIM-02 PhysBones target boundary: Core **404 passed / 0 failed** (`Logs/core-physbones-attachment.txt`)。`NYPP` v1のroot/endpoint/exclusion/branch/collider/limits/curve/interaction DTO往復、stable bone・stale skeleton拒否、unknown versionのopaque保持、SDK capabilityとloss reportのsupported/unsupported/warning分離、schema 4 PhysBones attachmentのbyte/hash一致Openを確認。Unity SDK component writer、実VRChat SDK/アバター動作、実操作・画像目視の受入は未確認。Windows-SecondaryMotionPhysBonesAttachment build **PASS** (`Logs/build-player-20260912-190938-424.log`)、Player **PASS** (`Artifacts/Authoring-20260912-190959-488cdb25d1124cb0b86f86853b0891c2/report.json`, 70 checks)。
- SIM-02 PhysBones Workbench status: Windows-PhysBonesStatusGui2 build **PASS** (`Logs/build-player-20260912-191727-520.log`)、Player **PASS** (`Artifacts/Authoring-20260912-191748-5e9371fb9e454c29b618a74947a443ce/report.json`, 71 checks)。保存済みtargetを新PlayerでOpenし、対応版のchain/SDK表示、unknown wire version 99の保持のみ表示を確認。Unity SDK component writer、実VRChat SDK/アバター動作、実マウス・画像目視の受入は未確認。
- SIM-02 PhysBones target package: Core **406 passed / 0 failed** (`Logs/core-physbones-bridge-package.txt`)。`physbones.nyaforge-target.json`、`physbones-target.nyaforge.bin`、`skeleton.nyaforge.bin`をhash検査付きで往復し、改ざんprofileを拒否した。
- SIM-02 UnityBridge writer: Windows-PhysBonesBridgeGui4 build **PASS** (`Logs/build-all-20260912-195001-224.log`)、Player **PASS / 71 checks** (`Artifacts/Authoring-20260912-195021-2abdd80c693f4d5ba51942a327f3e29b/report.json`)。receiver **PASS / 10 checks** (`Artifacts/BridgeReceiver-20260912-202001-392-7a27c7a4d1f647868118e83a3ca6bdc0/bridge-report.json`)、Unity 2022.3.22f1。Workbenchのtarget package書き出し、`ApplyPackage`経由のmanifest/profile/skeleton読込、stable bone mapping、managed-only更新、未管理component保護、unsupported preflight停止、SDK型形状のreflection mapping、branch表現可能性の事前検査、遅いconfigure失敗のrollback、package identity付きstable bindingの保存／stale manifest拒否を合成fixtureで確認した。受け取り側EditorWindow（stable BoneIdとcollider groupの手動割当、avatar rootへの保存／読込）もコンパイル確認済み。実VRChat SDK／実アバター／VRChat内動作の受入ではない。
- SIM-03A secondary-motion lifecycle: Windows-SIM03B build **PASS** (`Logs/build-player-20260912-204039-364.log`)、Player **PASS / 72 checks** (`Artifacts/Authoring-20260912-204100-9a3d2feb718345d880ae3b9f998f27ca/report.json`)。VRM0/1の既存Spring previewへGUI・内部MCP handler・外部sidecar toolの`secondary_motion_play`／`pause`／`reset`／`rebuild`／`step`／`state`を接続し、実MCP client→sidecar→named pipe→Player main threadの経路で再生、pause/resume、1固定step、再構築、reset、Save/Openでシミュレーションを保存しないこと、編集時停止を確認した。外部MCP fixtureはVRM1で固定し、Playerの自動tickを停止してstep数を決定的に照合した。実SDK／実アバター／VRChat内動作、実マウス操作・画像目視の受入ではない。
- SIM-03B secondary-motion capture: Core **408 passed / 0 failed**。`SecondaryMotionCaptureRecord`／codecとstrict requestを追加し、固定1/60秒、warmup、連続frameのpose/mesh/PNG hash、input/config hash、adapter/package/Unity/build、target/root/collider条件、complete/failed statusを記録する。`EvidenceModelCapture`のPNG描画をtransient `GraphMeshValue`でも共有し、`forge_secondary_motion_capture`をsidecarへ公開した。Windows-SIM03B-Capture3 build **PASS**（`Logs/build-player-20260912-210411-934.log`）、Player **PASS / 72 checks**（`Artifacts/Authoring-20260912-210441-f5a69f71558041289ab661a829c9958f/report.json`）。実MCPで128px・warmup2・3frameを取得し、PNG hash／寸法、completedSteps 3/4/5、撮影後のpreview reset、documentId/revision/stateHash不変を確認した。これはtransient previewの証拠であり、実SDK／実アバター／VRChat内動作、実マウス操作・画像目視・VRChat受入とは別である。
- SIM-01B native attachment / VRM0 migration: Core **411 passed / 0 failed**（`Logs/core-secondary-motion-rebind.txt`）。`secondary-motion.nyaforge.bin` をschema 4 attachmentへ追加し、NYSM bytes/hashのSave/Open、VRM0 source-node subtreeのchildren順展開→stable BoneId移行、VRM0/1移行結果、skeleton/topology stale時の再bind診断、明示bone/vertex mappingによるrebindを確認。未知wire版は既存codecのraw bytes保持をWorkbench表示へ接続した。Windows-SIM01B-Rebind build **PASS**（`Logs/build-player-20260912-212944-116.log`）。実素材のrebind、実SDK／実アバター／VRChat受入は未完了。
- SIM-01B-R GUI rebind: Windows-SIM01B-RebindGui5 build **PASS**（`Logs/build-player-20260912-215039-912.log`）、Player **PASS / 73 checks**（`Artifacts/Authoring-20260912-215102-0d9808eabc394e84a899bf0b1a614cd7/report.json`）。stale骨格で「同じBoneIdで再bind」ボタンが有効になり、明示stable IDでskeleton hashを再固定、保存要求を出すことを確認。別のリグPose/Binding再bindと二次運動attachment再bindを分離し、骨格stale中もtopology witnessで操作可能にした。実素材のrebind、実SDK／実アバター／VRChat受入は未完了。
- SIM-02B receiver regression: 最新Player出力を `Tools/Test-NyaForgeUnityBridge.ps1` へ接続し、Unity **2022.3.22f1** receiver **PASS**（`Artifacts/BridgeReceiver-20260912-213134-704-b2d9a3661628485d88bca9c1b40e0c19/bridge-report.json`）。PhysBonesのmanaged-only、未管理component保護、capability不足の事前停止、reflection field mapping、branch preflight、binding identity確認を回帰した。これは合成fixture受入であり、実VRChat SDK／実アバター／VRChat内動作ではない。
- SIM-02B receiver regression (RebindGui5): 最新Player成果物を同じBridge検証へ接続し、Unity **2022.3.22f1** receiver **PASS**（`Artifacts/BridgeReceiver-20260912-215208-952-9086fd84d8f541499ee8873c5e073a99/bridge-report.json`）。GUI再bind追加後もmanaged-only、未管理component保護、capability preflight、reflection mapping、branch preflight、binding identityを維持した。これは合成fixture受入であり、実VRChat SDK／実アバター／VRChat内動作ではない。
- SIM-02B reflection catalog regression: Unity **2022.3.22f1** receiver **PASS**（`Artifacts/BridgeReceiver-20260912-220031-153-b6d83d0cd2ef48ba94dc95e896ab488e/bridge-report.json`）。別component型のmarker干渉を分離したうえで、継承元private fieldを含むreflection discovery、package apply、managed-only、rollbackを確認した。これはSDK形状fixtureの受入であり、実VRChat SDK／実アバター／VRChat内動作ではない。
- SIM-02B binding validation: Unity **2022.3.22f1** receiverで`PhysBonesBindingValidator`の有効なdescendant mapping、root外bone、不足collider groupを確認した。保存／読込／適用前の共通検証としてGUIへ接続済み。これは合成fixtureの受入であり、実VRChat SDK／実アバター／VRChat内動作ではない。
- SIM-02B binding validation checkpoint: Windows Player build **PASS**（`Logs/build-player-20260912-220850-504.log`）、Player **PASS**（`Artifacts/Authoring-20260912-220901-ad5f61277a8d434a822f0ea09c3c2f43/report.json`）。同成果物をUnity **2022.3.22f1** receiverへ渡し、**10 checks PASS**（`Artifacts/BridgeReceiver-20260912-220932-900-7db78aa674cd4198849405029fd7accb/bridge-report.json`）。validatorの有効mapping、root外bone、不足collider groupを回帰した。これは合成fixtureの受入であり、実VRChat SDK／実アバター／VRChat内動作ではない。
- SIM-02B component identity checkpoint: Core **411 passed / 0 failed**（`Logs/core-physbones-component-type.txt`）。custom完全修飾型のmanifest往復と、型名を省略した旧manifestの既定型フォールバックを確認。Windows Player build **PASS**（`Logs/build-player-20260912-221503-050.log`）、Player **PASS**（`Artifacts/Authoring-20260912-221524-3e04602763934b2d82a483965a3d197b/report.json`）、Unity **2022.3.22f1** receiver **10 checks PASS**（`Artifacts/BridgeReceiver-20260912-221557-826-3ecd6b78c46743cd8321f2f3981d147b/bridge-report.json`）。これはcomponent型fixtureの受入であり、実VRChat SDK／実アバター／VRChat内動作ではない。
- SIM-02B resolver checkpoint: `VrcPhysBonesReflectionResolver`のassembly-qualified型の厳密解決、未導入型、非Component型の非破壊診断をBridge合成fixtureで確認。初回のnamespace import漏れを修正後、Unity **2022.3.22f1** receiver **10 checks PASS**（`Artifacts/BridgeReceiver-20260912-222102-611-57859d8c86c24b6ca4c52e07b29ac032/bridge-report.json`）。実VRChat SDK／実アバター／VRChat内動作ではない。
- SIM-02B inspection checkpoint: Windows Player build **PASS**（`Logs/build-player-20260912-222445-126.log`）、Player **PASS**（`Artifacts/Authoring-20260912-222457-93f8c1036d7a4da5bdab9aac1f4fc994/report.json`）。Unity **2022.3.22f1** receiver **10 checks PASS**（`Artifacts/BridgeReceiver-20260912-222531-558-e72dc25468864eef945e062f6d08dae9/bridge-report.json`）。`Inspect`／`InspectPackage`の書き込みなし、作成／更新件数、既存preflight共有を確認した。実VRChat SDK／実アバター／VRChat内動作ではない。
- 追試: Core **411 passed / 0 failed**（`Logs/core-physbones-inspection.txt`、output `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-1dadfa8c2faa40cf9ee5dcb423fb65e0`）。事前診断追加後もCore回帰なし。Unity／実SDK／実アバター／VRChat内動作の受入範囲は上記のまま。
- 受け取り側の再追試: 現在のUnityBridgeソースで `Test-NyaForgeUnityBridge.ps1 -PlayerCheckDirectory Artifacts/Authoring-20260912-222457-93f8c1036d7a4da5bdab9aac1f4fc994` を実行し、Unity **2022.3.22f1** の **PASS** を確認（`Artifacts/BridgeReceiver-20260912-222916-384-59578864671a402f8ef5e1c383eef92d/bridge-report.json`）。
- I04-B instance-aware import checkpoint: Windows Player build **PASS**（`Logs/build-player-20260912-223251-070.log`）、Authoring Player **PASS**（`Artifacts/Authoring-20260912-223313-162dccb9745c4947abe511a0bb83d3ec/report.json`）、Unity **2022.3.22f1** receiver **PASS**（`Artifacts/BridgeReceiver-20260912-223348-801-46bf29fe3a604a11b9adff8f7aa712a1/bridge-report.json`）。WorkbenchのGLB選択へnode instance indexを追加し、選択nodeのmesh／skinを採用、未skinned instanceを強制skinned importしない分岐を接続した。合成Player fixtureの回帰であり、任意実GLB／複数制作対象／実アバター受入ではない。

- GUI完全source接続: Windows-SourceSkinImport build **PASS** (`Logs/build-player-20260912-170004-544.log`)、Player **PASS** (`Artifacts/Authoring-20260912-170036-ad7444ce32a14d94b4b57c23c2f86ff7/report.json`)。VRM0/1で元GLBとNYFS一致、再生中Save、生成fixtureの原本パスを移動後にOpen、完全payload維持と制作姿勢復元を確認。import失敗保護も合格。実マウス/任意実素材の受入ではない。今回はCore変更なしでCore suiteを再実行していない。

- rig session v4: Core **375 passed / 0 failed** (`Logs/core-rig-v4.txt`)。実GLB由来sourceをWithSourceSkinで接続しnative Save/Open後のNYFS byte一致、旧v1〜v3の不明値維持、異source hash/骨集合/不正base64/nullの拒否を確認。GUI取込での生成はまだv3経路。
- Windows-RigV4 build **PASS** (`Logs/build-player-20260912-165809-522.log`)。GUI生成接続前のため今回Player suiteは再実行していない。

- SourceSkinCodec (NYFS v1): Core **374 passed / 0 failed** (`Logs/core-source-skin-codec.txt`)。GLB由来の完全matrix・bind・slot・children順の往復、write/read/write byte同一、省略/明示identity、切断/末尾/巨大count/不正basis/versionを検証。project attachment/GUI保存への接続は未完了。次はrig session/native版更新と移行。
- Windows-SourceSkinCodec build **PASS** (`Logs/build-player-20260912-165437-619.log`)。独立codec追加のためPlayer GUI suiteは今回再実行していない。

- GLB source skin reader: Core **371 passed / 0 failed** (`Logs/core-glb-source-skin.txt`)。実GLB containerのoffset付き一般bind、余剰entry、複数skin指定、省略identity、不正範囲/型/参照・sparse/外部buffer拒否を確認。native/geometry adapterは未接続。
- Windows-GlbSourceSkin build **PASS** (`Logs/build-player-20260912-164909-392.log`)。Core reader追加のためPlayer GUI suiteは再実行していない。

- SourceSkin候補型: Core **369 passed / 0 failed** (`Logs/core-source-skin.txt`)。slot順・入力不変・余剰bind保持・回転/非一様scale/鏡映のbind相殺・省略identity・257骨・不正root/slotを確認。GLB decoder/native接続は未完了。
- Windows-SourceSkin build **PASS** (`Logs/build-player-20260912-164501-118.log`)。今回の変更は独立したCore候補型のためPlayer GUI suiteは再実行していない。

- I04-A node読取: Core **366 passed / 0 failed** (`Logs/core-node-transforms.txt`)、Windows-NodeTransforms build **PASS** (`Logs/build-player-20260912-163727-416.log`)。reader単体の確認で、一般mesh/skin取込や保存の接続は未完了。
- I04-A数値基盤: Core **363 passed / 0 failed** (`Logs/core-source-affine.txt`)、Windows-SourceAffine build **PASS** (`Logs/build-player-20260912-163153-722.log`)。一般GLB取込/Player実素材の受入は未実施。
- 実装基準 `4abd9d9`。Core **360 passed / 0 failed**: `Logs/core-vrm0-preview.txt`（前段の実行結果）。Windows-Vrm0Playback Player **PASS**: `Artifacts/Authoring-20260912-160736-8e0e5bae1c24440082b9c84dd1b27fc4/report.json`。今回の仕様整理ではCore/Playerを再実行していない。
- T03: `Tools/Inspect-BlenderImport.py`をBlender 4.4.0で実行。詳細 `private/import-inspection/20260912-inventory.json`、3入力の存在/SHA256一致を再確認。素材・派生モデルの保存/出力はしていない。
- 取込パネルに残っていた「VRM0揺れ未対応」の古い説明を訂正し、説明を折り返すよう修正済み。Windows-ImportHelp build **PASS**: `Logs/build-player-20260912-161814-007.log`。説明修正のbuild結果であり、新しいimport機能や文字の目視受入を意味しない。

- 仕様整理: 設計v2/現行コードとの独立レビューを実施し、Blender任意依存と既知VRM内の未対応フィールドの保持条件を反映。関連6文書のローカルリンク112件が有効、調査スクリプトの構文検査が成功。private報告のignoreを確認。

## 実装と検証の履歴

以下は記録当時の状況。「次」「未完了」は当時の記述を含む。最新の状態・着手順は冒頭の表を使用する。

### I04-A: node transform読取と階層合成（2026-09-12）

- `GlbNodeTransformReader`にTRS/matrix decodeとchildren検査、`SourceNodeTransforms`に全local/worldの不変保持と反復合成を分離。SourceHashと元children順を保持し、通常nodeを省略しない。既存の階層検査を共用する。[契約](docs/Source-Affine.md)。
- Core **366 passed / 0 failed**: `Logs/core-node-transforms.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-d806dc6f63dc4921b688dd528efbaac0`。実GLBのmatrix/TRS・親配列順・4096段・旧profileとの原点一致、不正/曖昧入力の拒否を確認。
- Windows-NodeTransforms build **PASS**: `Logs/build-player-20260912-163727-416.log`。今回Player GUI suiteは再実行していない。一般mesh/skin importやnative保存は未接続。
- 次は一般inverse-bindを含むsource skin候補、完全な変換情報の保存と旧形式移行。GLB取込の既存translation-only制限は外さず、I04-A全体と実素材受入は未完了。

### I04-A前段: source affine数値モジュール（2026-09-12）

- `SourceAffine`へcolumn-major/TRS、parent×local、逆行列、Point/Vector/Normal/Tangentの変換を分離。double計算、入力不変、finite float出力、鏡映handednessと不正基底の診断を定めた。[計算と保存接続の契約](docs/Source-Affine.md)。
- Core **363 passed / 0 failed**: `Logs/core-source-affine.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-a8780732274b434e923d42a3b5ceede3`。非一様TRS、inverse-bind相殺、shear/鏡映、法線/接線、不正入力、小さいscaleを確認。
- Windows-SourceAffine build **PASS**: `Logs/build-player-20260912-163153-722.log`。数値モジュール追加のためPlayer GUI suiteは今回再実行していない。
- 次はnode JSON decode、完全local/world変換、一般inverse-bindの候補と保存移行。旧sessionの原点を完全な一般transformと捏造しない。既存translation-only制限を維持し、I04-A全体は未完了。

### T02: VRM0実行所有者と共通再生GUI（2026-09-12）

- `Vrm0SpringRuntime`へ設定・通常nodeのcenter/collider変換を分離し、`Vrm0SpringPreview`が固定stepとskin投影を所有する。Advanceはcontroller候補で計算し、skin投影成功後に公開。旧状態を失わない。colliderの形状検査は既存adapterを共用。Workbenchは`IVrmSpringPreview`で両形式を切替。[契約](docs/VRM0-Spring-Playback.md)。
- Core **360 passed / 0 failed**: `Logs/core-vrm0-preview.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-df8da434d3c544b0a89b385f7ba153ad`。合成VRM0 reader/session→owner、通常nodeのcenter/collider、重力Z、12step、停止/再開、外部姿勢更新、時間不正/scale変更時の状態保持とResetを確認。
- Windows-Vrm0Playback build **PASS**: `Logs/build-player-20260912-160702-715.log`。Player suite **PASS**: `Artifacts/Authoring-20260912-160736-8e0e5bae1c24440082b9c84dd1b27fc4/report.json`。VRM0/1両方で表示変化・Mesh/Object再利用・編集点復元・停止/再開・Save/Openで一時姿勢非保存・編集時破棄のhandler検証を通した。実クリック/実素材/画像目視は未受入。
- T02は対応profileの接続完了。次はT03→必要なI04→T05。256実行骨の予算、translation-only取込、base pose時点のcollider snapshot、固定avatar座標、0.25秒超frameでの停止を制限として明示する。揺れるnodeに付いたcolliderの逐次更新や可動world rootはI03-Bの比較/拡張残件。C0〜C5全体は未完了。

### T02: 通常nodeを含む一時骨格・skin姿勢の往復（2026-09-12）

- `ImportedPreviewRig`へ必要なsource node/skin joint/全祖先から一時骨格を作る責務と、制作poseとの相互変換を分離。skinのBoneIdを維持し、通常nodeは決定的IDを持つ。source原点とinverse-bind Headの差を両方向へ補正。元graph/skin/skeletonは変更しない。[契約](docs/Imported-Preview-Rig.md)。
- Core **359 passed / 0 failed**: `Logs/core-preview-rig.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-799f80a8863c4dc38d01556bbda60956`。回転姿勢往復・必要祖先・無関係枝除外・ID再現・stale拒否を確認。非joint helperを12stepシミュレーションして子skin骨へ回転を投影し、元姿勢を保持した。
- Windows-PreviewRig build **PASS**: `Logs/build-player-20260912-160115-422.log`。Coreの姿勢変換追加で、今回Player GUI suiteは再実行していない。実素材/実操作の受入は未実施。
- 次はVRM0展開targetから実行chainを作り、この一時骨格のcenter/collider変換、固定step所有者、Workbenchへ接続する。呼出側は展開targetとcenter/collider nodeを必要集合へ含める。必要node+祖先が実行Coreの512骨予算を超える場合は明示拒否。T02全体は未完了。

### T02前段: VRM0 source subtree展開（2026-09-12）

- `Vrm0SpringExpansion`を独立adapterとして追加。root/children順で全子孫（非jointを含む）を展開し、最初の子または親からの方向へ7cm延ばした仮想末端を保持。元設定・center/collider参照を残し、重複subtreeや方向不明の末端は明示拒否する。[契約と公式根拠](docs/VRM0-Spring-Expansion.md)。
- Core **357 passed / 0 failed**: `Logs/core-vrm0-expansion.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-9975b1e36afb460abcd75c44d75d6d54`。分岐順・全子孫・仮想末端数値・設定継承・拒否条件、合成VRM0 reader→rig codec→Resolveで非joint末端まで確認。
- Windows-Vrm0Expansion build **PASS**: `Logs/build-player-20260912-155712-181.log`。今回はCore adapter追加で、Player GUI suiteは再実行していない。実素材受入は未実施。
- T02は継続中。次は通常nodeを含む実行骨格/poseとsource mappingの接続。揺れの実行、center/collider、skinへの投影、Workbench所有者とGUIは未接続。VRM0を再生可能とは表示しない。

### T01: 全source階層の保存（2026-09-12）

- ImportedSourceHierarchyに全nodeの親・元children順・source rest原点を保持し、検査とtranslation合成を反復処理へ分離。4096node上限、循環/不正親/子の欠落・重複・原点不一致を拒否する。JSON処理はImportedSourceHierarchyJsonへ分離し、rig session v3へ保存。v1/v2では階層を推定しない。[契約](docs/Imported-Source-Hierarchy.md)。
- Core **354 passed / 0 failed**: `Logs/core-source-hierarchy.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-10d4a7cce4cf490e9daddf391e126fa7`。非joint末端/分岐とchildren順のnative往復、旧形式移行、壊れたpayload、4096段の計算を確認。
- Windows-SourceHierarchy build **PASS**: `Logs/build-player-20260912-154816-246.log`。Player suite **PASS**: `Artifacts/Authoring-20260912-154853-fa716e2a8d8a4c9faf552796ca1e7869/report.json`。VRM0/1取込Save/Openの全node保持をhandler検証へ追加。実素材・実操作・画像の目視受入は未実施。
- 次はT02のVRM0 root/末端展開と通常nodeの実行対応。データ保持だけでVRM0再生や一般transform取込を完了扱いにしない。I03-B全体、I04、A01、C0〜C5の残件は維持する。

### I03-C: 再生projectionの再利用（2026-09-12）

- `SpringMeshBuffers`へ再利用するposition/normal/tangent/Points配列とbounds検査を分離。同じtopology・transform・属性数・UV・材質ならUnity Mesh/GameObjectを保持し、頂点データだけ更新する。構成変更時は通常のprojection再構築へ戻す。
- 再生中は編集点バッチを作らず、終了/Resetで編集中の表示と編集点を復元する。表示が初期姿勢と一致していても復元を省略しない。graphは従来どおり一時評価し、制作文書へは書かない。
- Windows-SpringMeshReuse build **PASS**: `Logs/build-player-20260912-153043-091.log`。初回suiteは120秒で時間切れ (`Artifacts/Authoring-20260912-153122-17d1650d16ca40148eca6e10794714d8/player.log`)。検証process終了後にTimeoutSeconds=300で再実行し、Player suite **PASS**: `Artifacts/Authoring-20260912-153336-c66834f83e614fecb6278247149ea256/report.json`。Core変更なし、直近353件合格を参照。
- 専用Player検証へ12stepのMesh/Object同一性、表示頂点変化、再生中の編集点非表示とReset復元を追加。[再利用契約](docs/Workbench-Spring-Playback.md)参照。大規模モデルの速度/メモリ測定は未実施。

### I03-C: Workbench再生GUIと一時表示（2026-09-12）

- `AuthoringWorkbench.SpringPlayback`に折りたたみの再生/一時停止/リセットを追加。`OwnedMeshProjection.Spring`で一時graph評価の表示を分離する。保存graph/attachments/Undoへ揺れたposeを書かず、編集・作品・metadata・stage変更時はownerを破棄する。
- 新規Player検証で、取込後のprojectionが更新されない問題を検出（`Artifacts/Authoring-20260912-152432-0b03c51c51434b669e0a26b8f82e97e8/report.json`）。取込AddGraph commandへprojectionを渡し、成功した取込を即表示する共通経路へ修正。
- Windows-SpringPlaybackUi build **PASS**: `Logs/build-player-20260912-152520-212.log`。Player suite **PASS**: `Artifacts/Authoring-20260912-152603-441f4a62fa4d48e4bd284d07396ee2f1/report.json`。自作VRM1の取込→12stepで表示頂点変化、graph/metadata不変、停止/再開/リセット、Save/Openで一時姿勢非保存、編集時破棄を専用handler検証で確認した。
- [Workbench再生契約](docs/Workbench-Spring-Playback.md)。今回Coreは未変更で直近353件合格を参照。実クリック、文字の隠れ、実アバター、負荷は未受入。大きなmeshでの一時graph評価/projection再構築コストを今後確認する。
- 次はVRM0 root/末端展開と、実素材に必要な一般node変換を進め、対応profileでWindows実操作受入を行う。I03-B/C全体、I04、A01とC0〜C5の製品目標は未完了部分を維持する。

### I03-B/C: VRM1 preview所有者への統合（2026-09-12）

- `Vrm1SpringPreview`がsourceを固定して実行chainとcontrollerを所有する。`VrmSpringCenterAdapter`へ元node原点からのcenter変換を分離。Advanceごとに現在poseのcollider/centerを解決し、scale変更は明示Resetを要求する。候補の生成に失敗したResetでは旧previewを保持する。
- Core **353 passed / 0 failed**: `Logs/core-vrm-preview.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-ea39c6e0c6d84f37800dd1d92f5dba23`。自作VRMファイル→reader→session codec→owner→再生、停止中center移動と再開、collider pose更新、scale変更拒否/Reset、stale骨格での旧状態保護を確認。
- Windows-VrmPreviewOwner build **PASS**: `Logs/build-player-20260912-151924-482.log`。Core所有者の追加のためPlayer GUI suiteは今回再実行していない。実モデル受入は未実施。
- [VRM1 preview所有者](docs/VRM1-Preview-Owner.md)。次はWorkbenchでbase poseを取得し出力Poseを一時表示する接続と再生/停止/リセットGUI。作品・session変更時の破棄、VRM0展開、一般node transform、実素材受入も未完了。I03-B/C全体の完了にはしない。

### I03-B/C前段: 固定step再生controller（2026-09-12）

- `SpringPreviewController`へState/center/Pose/端数時間/再生状態の所有を集約し、`SpringFixedClock`へ1/60秒の時計を分離。停止中は壁時計を蓄積せず、描き直しは履歴をcommitしない。全substep成功後だけ新状態を公開し、失敗時は再試行可能な旧状態を保つ。
- floatの境界誤差で1step不足する新規ケースを検出（`Logs/core-spring-preview.txt`、349件合格/2件失敗）。1e-7秒の境界許容差を設け、修正後Core **351 passed / 0 failed**: `Logs/core-spring-preview-final.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-a6ba63c364464e1c816aafad54b2a7be`。
- 0.2秒の一括/分割一致、停止中のcenter移動と再開、Reset、衝突失敗・不正時間・不正centerでの全状態保持と再試行を検証。[controller契約](docs/Spring-Preview-Controller.md)参照。
- Windows-SpringPreviewCore build **PASS**: `Logs/build-player-20260912-151436-987.log`。Core controller追加のためPlayer GUI suiteは今回再実行していない。実モデル受入は未実施。
- 次はVRM sessionのcenter/colliderを毎frame解決し、graph base poseとpreview表示へ接続する所有者/GUI。VRM0展開、設定変更reset、一般node transform、実素材受入も未完了。I03-B/C全体は引き続き未完了。

### I03-B: VRM1実行chainと参考時間式（2026-09-12）

- `Vrm1SpringRuntimeAdapter`がtopology解決を共用し、元head/tail原点をCoreの明示offsetへ変換する。radiusへ一様scaleを適用し、stiffness>1も保持。重力情報不足・未対応scale・予算超過は拒否する。
- `VrmSpringIntegration`へVRM参考式の時間計算を分離し、既存Authoring modeは維持。modeはchain hash version4へ含む。VRM modeのdragはstep単位で、再生controllerでは固定stepを管理する。native/session schemaは変更しない。
- Core **348 passed / 0 failed**: `Logs/core-vrm-runtime-chain.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0706310605cb42df84fd619b3aed785f`。参考式の解析値、元重力ベクトル・stiffness25保持、session→実行chain→12stepの中心固定/Pose/State一致を確認。
- Windows-VrmRuntimeChain build **PASS**: `Logs/build-player-20260912-151110-601.log`。Core adapter/計算変更のためPlayer GUI suiteは今回再実行していない。実モデル受入は未実施。
- [実行chain・時間契約](docs/VRM-Spring-Dynamics.md)。次はcenter/固定step/失敗時状態を所有するcontrollerとVRM0展開。I03-B全体、I03-CのGUI、一般node transform、実素材受入は未完了。参考アルゴリズムと全runtimeの挙動同一性を保証しない。

### I03-B前段: 元node原点を回転中心に保持（2026-09-12）

- `RestHeadOffset`でCoreの回転中心を明示可能にし、`SpringJointTarget`でhead/tailと長さの検査を共用する。回転後のtranslationを補正してhead位置を維持し、bone Headとsource原点を同一視しない。未指定は従来どおりゼロ。
- Core **347 passed / 0 failed**: `Logs/core-spring-pivot.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-d2c8fd00c7c44524a0a84a8677d4e38a`。2倍scale、ずれた原点、12stepのhead固定・tailと子joint一致・長さ、head変更時のstale state、明示/暗黙先端との一致拒否を検証。
- Windows-SpringPivot build **PASS**: `Logs/build-player-20260912-150727-272.log`。Core計算変更のためPlayer GUI suiteは今回再実行していない。実モデル受入は未実施。
- [明示先端契約の回転中心拡張](docs/SpringBone-Endpoints.md)参照。計算hash version3へheadを含めたが保存schemaは変更しない。次はVRMの元設定からhead/tailと力を持つ実行用chainを構成する接続。I03-B/C、VRM0展開、runtime所有、再生GUI、実素材受入は未完了。

### I03-B: VRM1 chainのtopology解決（2026-09-12）

- `Vrm1SpringChainResolver`へVRM1 joint列の祖先/重複/center/group検査を分離し、`VrmSpringChainBinding` / `VrmSpringPairBinding`へ不変の結果を保持する。末尾jointはtail専用で、headの設定を各pairへ保持。中間jointを飛ばす場合も重複範囲に含める。
- Core **346 passed / 0 failed**: `Logs/core-vrm-chain.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-12e506fb725147b9bd716b181049a993`。codec往復、pair数、元node原点とbone Headの差、stiffness>1保持、祖先/重複/center/短いchain/未対応node/source不一致を検査。
- Windows-VrmChain build **PASS**: `Logs/build-player-20260912-150445-722.log`。Core resolver追加のためPlayer GUI suiteは今回再実行していない。実VRM受入は未実施。
- [VRM1 chain契約](docs/VRM1-Spring-Chain-Binding.md)。戻り値は元設定を保持するtopologyであり、実行可能なCore chainではない。I03-B全体は未完了。次はVRM0 root/末端の展開、元node原点を回転中心へ反映する接続、重力/時間の係数契約を実装する。runtime/GUIと実素材受入も未完了。

### I03-B前段: head/tail pairを受け取るCore先端（2026-09-12）

- `SpringBoneJointSettings.RestTailOffset`で表示用bone Tailと計算用先端を分離し、`SpringJointTarget`を初期stateとStepで共用。明示offsetから長さ・回転・衝突を計算する。未指定は既存のbone Tailを維持する。
- Core **345 passed / 0 failed**: `Logs/core-spring-endpoint.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-74112fb0d4174dfba6c403f12717c1a6`。2倍scale、表示Tailと異なる先端、非simulated中間骨と末尾joint、球衝突を12step検証。設定変更時のstale stateとゼロ/非有限offsetを拒否。
- Windows-SpringEndpoint build **PASS**: `Logs/build-player-20260912-150050-221.log`。Core変更のためPlayer GUI suiteは今回再実行していない。実アバター受入は未実施。
- 計算用chain hashはversion2でoffset指定有無/値を含む。native/sessionの保存schemaは変更しない。[明示先端契約](docs/SpringBone-Endpoints.md)。VRM chainの解決自体は次の工程で、I03-B全体は未完了。VRM1末尾jointを回転対象にしない展開、VRM0末端、center祖先検査、重力/時間を引き続き接続する。

### I03-B前段: center履歴追従（2026-09-12）

- `SpringCenterFrame`がsimulated boneごとのcenter変換を不変保持し、`SpringCenterMotion`が前回/current tailを両方現在centerへ移す。設定・skeletonの一致と可逆基底を検査し、state/timeは入力を変更せず保持する。異なるchainのcenterを混ぜない。
- Core **343 passed / 0 failed**: `Logs/core-center-motion.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-92f48ebeaacf43d9ba2872458833b83d`。複数center、移動/回転/scale、履歴両点、逆変換、停止/再開後の実Step、入力独立性・不正frame拒否を確認。
- Windows-SpringCenter build **PASS**: `Logs/build-player-20260912-145755-883.log`。Core計算追加のためPlayer GUI suiteは今回再実行していない。実マウス・実アバター受入は未実施。
- [center契約](docs/SpringBone-Center-Motion.md)にruntime所有者のcommit順と重力との分担を記録。I03-Bは未完了。次はVRM1のhead/tail pair（末尾jointは回転対象にしない）とVRM0 root/末端展開、center参照の祖先検査、設定値/時間契約を統合する。runtime/GUIと実素材受入も未接続。

### I03-A: collider座標adapter（2026-09-12）

- `VrmSpringColliderAdapter`へsource座標から現在poseへのsphere/capsule変換を分離し、`ImportedNodeSpace`を共用する。VRM0の拡張offsetは標準出力のZ反転を吸収し、VRM1は元glTF node-local値を使う。source/skeleton、形状詳細、未対応nodeを検査し、groupとcolliderの順序・重複を保持する。
- `PoseUniformScale`へ球/カプセルの半径scale検査を分離。直交した一様scale（鏡映含む）だけを受け入れ、非一様scale/shearは診断する。Coreの半径/件数予算は無言で切り詰めない。
- Core **340 passed / 0 failed**: `Logs/core-collider-adapter.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-3dec6b4cbbd7497fa948f799bf9f91ea`。session codec往復、原点とbone Headの差、90度回転・2倍scale・鏡映、sphere/capsule、未対応入力を検証した。
- Windows-ColliderAdapter build **PASS**: `Logs/build-player-20260912-145343-529.log`。Core adapter追加のためPlayer GUI suiteは今回再実行していない。実VRMの見た目受入は未実施。
- 契約と公式実装の根拠は [collider adapter](docs/VRM-Collider-Adapter.md)。これはsnapshot生成までで、再生previewは未接続。次はI03-Bのchain/center/重力/時間契約、I03-Cのruntime所有とGUI。一般node変換と実素材受入も未完了。

### R12: 取込候補と状態公開の分離（2026-09-12）

- `AuthoringWorkbench.ImportPublication.cs`へ候補の所有と公開を分離。rig/expression/Spring sessionとattachment bytesをgraph追加前に検査・準備する。追加command成功後に共有フィールドと表示を反映する。expressionの既存「未対応mappingを警告して未設定とする」動作は維持する。
- `ImportFailureVerification`は不正skinを空projectおよび取込Undo後に読み込み、文書/attachments/session参照・表示・dirty・Undo/Redo可否が不変であることを確認する。duplicate graphを実commandへ渡す拒否経路と、有効ファイルによる再試行も対象。
- Windows-ImportIsolation build **PASS**: `Logs/build-player-20260912-144923-727.log`。変更はUnityRuntimeのみで、Coreは前段R11の338件合格を参照し今回再実行していない。最終Player suite **PASS**: `Artifacts/Authoring-20260912-144950-6cef15f6584c4e30b33ce5deb691d6be/report.json`。専用Import failure isolation検査を含む。実素材・実マウス受入は別途。

### R11実装の証拠（2026-09-12）

- `ImportedJointHierarchy`へ元node木からskin joint木への変換を分離。入力木の循環/複数親検査とtranslation合成は既存importerで先に完了させる。子tail候補はsource node index順で選び、skin slot登録順に依存させない。
- 修正前は既存334件合格・追加4件失敗 (`Logs/core-hierarchy-before.txt`)。修正後 **338 passed / 0 failed** (`Logs/core-hierarchy-after.txt`)。成果物: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4250aa56bf3a4229a762cce1797b61d9`。
- Windows-JointHierarchy build **PASS**: `Logs/build-player-20260912-144506-592.log`。今回はCore import修正のためPlayer GUI suiteは再実行していない。実マウス・実アバター受入も未実施。
- nodeを骨へ追加する変更ではなく、元joint間の祖先関係を保つ変更。非joint自体へのcollider追従、一般node回転/scale、再生previewは引き続きI03/I04で扱う。

## 過去レビューR01〜R10の修正・検証記録（2026-09-12）

ユーザー依頼「ここまでチェック」「チェック後current_task更新してタスク化」に対応したレビューを起点に修正を進める。詳細な再現条件、対象行、完了条件、証拠は [Rig / VRMレビュー](docs/reviews/2026-09-12-Rig-Vrm-Review.md) を参照する。R01/R02/R03/R09はCore/Windows自動検証まで完了し、次はVRM入力契約・mapping永続化。実マウス・実VRMの受入は各自動検証と区別する。

- [x] **R03 / P1 — 保存全体の成功判定と失敗時の保護**。schema 4の単一manifestで本体とVRM設定のblob参照を一括公開し、失敗時の旧作品・設定・dirty・versionを保持。Core共通保存service、Windows GUI/MCP handlerの再試行・終了防止・Save As/Openを検証した。外部MCP transport経由のmetadata専用試験と実マウスの受入は未実施。
- [x] **R01 / P1 — VRM1 authors配列**。文字列として扱うreaderと誤ったfixtureを修正する。複数作者をimportからsession保存・再読込まで保持し、既存形式の移行も定める。
- [x] **R02 / P1 — 同一nodeの複数コライダーの往復**。`nodes:[0,0]`を重複禁止readerで拒否する問題を直す。順序・件数を保持し、VRM0/1のimport→Save→Openを確認する。
- [x] **R09 / P2 — VRM1 SpringBoneの既定値**。省略stiffness/dragForceを1.0/0.5にし、明示0と区別する。session再読込でも一致させる。
- [x] **R04 / P1 — 連続stepのPose/State整合**。step2でtailが約0.079809 mずれる回転合成を直す。同じbase pose／変化するbase poseで、出力PoseとStateが連続して一致することを確認する。
- [x] **R05 / P1 — chainの親子変換伝播**。親tailと子headが約0.079807 m離れる問題を直す。親から子へ相対offsetを保って評価し、2〜3jointと子孫追従を確認する。
- [x] **R06 / P2 — chainごとの衝突参照**。全chainのgroup参照を混合せず、所属chainの参照だけをjointへ渡す。参照なしBが別chain Aの追加で約0.290426 m動く再現を回帰化する。
- [x] **R07 / P2 — 長さと衝突の同時制約**。押し出し後の長さ制約でsphere内へ戻る問題を直す。複数sphere、hitRadius、同軸例、解なし／反復上限の診断を確認する。
- [x] **R08 / P2 — 停止・再開と時間刻み**。dt=0でもtailが約0.079304 m動く問題を直す。物理履歴を停止中に進めず、固定step / 可変stepの契約と再開を検証する。
- [x] **R10 / P2 — 入力検査と実際の経路を通る回帰**。null collider groupをdomain errorで検出する。空groupの衝突テスト、初期Stateからの反復だけのテストを改め、R01〜R09の回帰を各修正と同時に追加する。保存失敗・終了防止はWindows Playerの専用検証も必要。

検証済み: このレビューでCore **297 passed / 0 failed**を再実行。別fixtureで作者情報拒否、session往復失敗、保存失敗後dirty=False、step2姿勢ずれ、親子gap、chain間衝突混入、貫通、dt=0の進行、null参照例外、省略値の相違を確認した。Player/Bridgeは今回再実行していない。実VRM全体・手動見た目受入も未確認。

最新の再開順は冒頭のタスク表を参照する。以下はR01〜R10とI01〜I03の実装履歴で、各節の「次」「未完了」は記録時点の状況を含む。R03の保存保護は自動検証範囲で完了。設定・状態／計算／衝突／import adapter／保存coordinatorを役割ごとのモジュールへ分ける。

### 実行単位と完了判定

- [x] **R01 + R02 + R09（取込・保存、自動検証完了）**。作者名の配列保持と旧session移行、コライダーnode列の重複保持、省略値と明示0の区別を同じ段階で直す。正規の合成VRM0/1を使うCore往復テストとWindows Workbench Save/Openを完了条件とする。
- [x] **R04 + R05（姿勢・階層、Core検証完了）**。連続stepのPose/State一致と親子変換を修正する。2〜3joint、変化するbase pose、登録順、非simulated子孫と初期offsetを回帰対象にする。
- [x] **R06 + R07 + R08（衝突・時間、自動検証完了）**。chain単位の参照分離、長さと衝突の同時制約、停止・再開の契約を修正する。解なしの診断、有限値、可変dtを含めて検証する。
- [x] **各実装に同梱: R10（検証強化、自動検証完了）**。修正前に失敗する再現を正式テストへ移す。保存・OpenはPlayer経路も通し、数値計算は前stepのStateを引き継ぎ、実際の衝突参照を指定する。null groupのdomain errorも確認する。
- [ ] **基礎修正後の受入**。実VRMの読込・保存・再読込、GUIの保存して終了、文字サイズ・隠れ、姿勢と見た目をWindowsで確認する。外部MCP transportのmetadata保存も別途確認する。各記録に対象buildと確認方法を残す。

状況照合時点では未修正だったR01/R02/R09を、下記の実装と自動検証で更新した。チェック済みは自動検証範囲であり、実素材と実マウスによる受入は別タスクのまま維持する。

修正後は、未保持のgravityDir・collider shape値と保存移行を含むVRM入力契約を整え、node→stable BoneId、preview接続へ進む。一般node transform、skin/morph出力、実アバター受入、C1〜C5の全体目標は維持する。

### I03座標基盤: 元node原点の保存と変換（2026-09-12）

- GLB skin取込で親translationを合成したSourceNodeOriginsを保持する。inverse-bind由来のbone Headと同一視しない。ImportedRigSession v2へ保存し、v1読込はorigin不明として保持する。旧mapping自体は引き続き利用可能。
- `ImportedNodeSpace`へsource-local→source-rest→bone-local→posed avatarの変換を分離。source/skeleton/graphの検査と、origin不足・未対応nodeの拒否を含む。旧作品のGUIには元node座標不足を表示する。
- Core **334 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-a40c1a16d57e487083686e63ee83d0bb`。元node原点とinverse-bind Headが異なるfixtureをnative保存/Openし、移動/90度回転後の解析値と比較。v1移行で不明を維持、欠落node原点・未対応nodeを拒否した。
- 契約は [元node空間](docs/Imported-Node-Space.md)。次はVRM0/1の座標表現をsource-localへ変換するcollider adapter、半径scale、center空間と重力/時間設定を接続する。非joint nodeや一般node回転/scale、再生GUIは未完了。

- Windows-NodeSpace build **PASS**: `Logs/build-player-20260912-143434-782.log`。Authoring suite **PASS**: `Artifacts/Authoring-20260912-143546-e00b3bf1383140b0a3d863c045a4e98d/report.json`。VRM0/1のnode原点を保持してSave/Openし、local offsetからの変換結果を確認。実素材preview・実マウス受入は未実施。

### I03前段: capsule衝突コア（2026-09-12）

- `SpringBoneCollider`に任意のTailを追加し、sphere/capsuleを同じ不変型で保持する。`SpringColliderGeometry`へ最短軸点・距離を分離し、solverの各passと全形状の最終検査で使う。長さ0はsphereと等価。
- 拡張前は新規中央接触ケースが失敗（`Logs/core-capsule-before.txt`、332 passed / 1 failed）。拡張後はCore **333 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f9ae8ff572dc4be69db33617c2529b4c`。中央/端/ゼロ長、sphere混在、hitRadius、長さ・Pose/State一致、有限値・決定性を確認した。
- bounded探索と未収束診断はsphereと共通。契約は [制約solverのCapsule拡張](docs/SpringBone-Constraints.md)。実VRMのoffset/tailからavatar座標へ変換するadapterは未接続。
- 次は元nodeのrest座標と骨格rest座標の関係を保持し、source-local offsetを正しく変換する。inverse bindから得たbone headを元node座標と無条件に同一視しない。その後、center追従・時間パラメータ変換・previewの再生/停止/リセットを接続する。I03全体は未完了。

- Windows-CapsuleCore build **PASS**: `Logs/build-player-20260912-143007-334.log`。今回の変更はCore計算のみで、Player GUI suiteは再実行していない。実VRMのcapsule見た目受入も未実施。

### I02: 取込骨対応のsnapshot保存（2026-09-12）

- `ImportedRigSession` / Codec v1へsource/skeleton hash、GraphId/SkeletonNodeId、node/BoneIdとhumanoid対応を保存する責務を分離。許可attachmentにrigを追加して最大3件とし、既存の単一manifest公開・失敗保護を共用する。
- GUI skin取込でsessionを所有し、Open前にsource整合性を確認する。骨格変更時は取込パネルへstaleを表示し、対応解決を拒否する。Undoで元骨格へ戻れば対応も再び有効になる。静的mesh取込は古いrig情報を継承しない。
- Core **330 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-427a087e42fc48f997e750c19f00a55f`。session/native往復、source/graph/skeleton不一致拒否、未知field/重複拒否、rig blob失敗時の旧snapshot維持と再試行を確認。
- 契約は [取込骨対応の保存](docs/Imported-Rig-Sessions.md)。旧NyaForgeはrig attachment付き作品を開けない。元ファイルのない旧作品から対応を推測しない。I03の一般node/center/collider座標・時間変換とpreview接続は未完了。

- Windows-RigSession build **PASS**: `Logs/build-player-20260912-142438-076.log`。Authoring suite **PASS**: `Artifacts/Authoring-20260912-142507-e3f1bb9d5e044c76978b7b06987b1176/report.json`。同一VRM0/1のmapping復元、骨格編集時stale表示、Undo復帰、rigを含む3種類のblob失敗保護とGUI/MCP handler再試行を確認。実マウス・実アバター受入は未実施。

### I01: Spring詳細を欠落なく保存（2026-09-12）

- `VrmSpringColliderShape`と`VrmSpringDetailJson`へtyped shapeとJSON vector検査を分離。重力方向、sphere/capsuleのoffset/radius/tailを元の座標系で保持し、VRM1の省略値とVRM0の不明値を区別する。
- Spring sessionはversion 3 writer、version 1/2/3 reader。旧sessionで失われた方向・形状はnullで保持し、再書出し時にも推測値で埋めない。expression v2・project schema4は維持。Workbenchは詳細不足を表示する。
- Core **328 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-14f6fee4f70e4a5eb99411298cdc29f9`。VRM0/1の非default詳細・codec往復、旧v1/v2移行、不明の保持、default capsule、vector不正・件数不一致・sphere tail拒否を確認。契約と出典は [Spring詳細](docs/VRM-Spring-Details.md)。
- 元VRM0の省略vectorは不明として残る。HasCompleteDetailsは設定値の充足であり、runtime対応の保証ではない。I02のmapping永続化、I03の座標/時間変換・capsule物理・previewは未完了。

- Windows-SpringDetails build **PASS**: `Logs/build-player-20260912-141632-673.log`。Authoring suite **PASS**: `Artifacts/Authoring-20260912-141755-08f376dbcbac4e2f8f8c670351f75395/report.json`。同じVRM0/1の詳細値取込→Save/Openと、旧sessionの不明値維持を検証した。詳細不足表示の実マウス/見た目受入は未実施。

### R10完了: 同じVRMの取込→Workbench保存→Open（2026-09-12）

- `VrmVerificationFixture`は第三者素材を含まない合成GLB/VRM bytesを生成する。skin、morph、表情、VRM0同一node複数sphere、VRM1同一/異なるnodeとcapsule inventoryを含む。完全な製品アバターのVRM仕様適合fixtureではなく、取込対応profileを通す検証データ。
- `AuthoringWorkbench.VrmImportVerification`は実ファイルへ書出し、GUIの`ImportModel`→`TrySaveProject`→空workspace→`OpenProject`をVRM0/1それぞれで実行。graph hash、attachment hash、source identity、作者列、表情weight、collider node列、VRM1省略値/明示0を確認した。
- Windows-VrmImportRoundtrip build **PASS**: `Logs/build-player-20260912-140900-452.log`。Authoring suite **PASS**: `Artifacts/Authoring-20260912-140928-5c88b52461154cfe828d5a6bb9cc23cf/report.json`。専用の`VRM0/1 file import to Workbench graph...` checkが成功。追加検証はUnityRuntimeのみでCore本体は変更せず、Coreは前段の326件合格記録を維持。
- [検証範囲の照合](docs/reviews/2026-09-12-Repair-Coverage.md)で残っていたR02/R10の分断された経路を埋めた。実ファイルpickerのマウス操作、実VRMの見た目、外部MCP transportのmetadata専用受入は別タスクのまま。R01〜R10の終了はC0〜C5全体の完成ではない。

### 次の実装単位: VRM入力契約とpreview接続

- [x] **I01 — Spring入力の詳細保持（対応profileの自動検証完了）**。gravityDirとsphere/capsuleのoffset・radius・tailをtyped metadataへ保持する。旧sessionは不明値を捏造せず、移行方針と再取込の必要性を定める。Core solverのsphere対応とVRM capsule対応は区別する。
- [x] **I02 — 骨対応の保存（Core/Windows自動検証完了）**。ImportedBoneMap/humanoid bindingをnative保存へつなぎ、同一snapshotで保存・Openできるようにする。source/skeleton hashとnode参照を検査し、骨格編集時のstaleを明示する。
- [ ] **I03 — runtime preview**。VRM node→BoneId、centerとcollider座標系、VRM設定の時間的意味をadapterで変換する。未対応node/shapeを黙って除外しない。Workbenchの再生・停止・リセットは保存/Undoとは分離して接続する。

### VRM骨対応adapterと修正範囲の照合（2026-09-12）

- `ImportedBoneMap`はGLB skin取込時のnode→BoneId割当をsource/skeleton hashとともに不変保持する。`VrmHumanoidBinding`はVRM0/1のsemantic→nodeをstable BoneIdへ変換し、違うsource・変更された骨格・skin外nodeを拒否する。骨名やskin slotから推測しない。
- Core **326 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-bc76ad1859954e019babddfabf03ca08`。同名2骨、node番号とskin slotのずれ、VRM0/1、weight参照、再取込、骨格codec往復、誤った対応の拒否を確認した。
- BoneMapのnative保存/Open復元とGUIへの接続は未実装。保持中のmapを骨格codec往復後に再利用できる検証と、map自身の永続化を混同しない。詳細は [取込骨対応](docs/Imported-Bone-Mapping.md)。
- [修正後の検証範囲](docs/reviews/2026-09-12-Repair-Coverage.md)へR01〜R10の正式回帰と未確認境界を照合した。R10の残件として、VRM0/1それぞれの同一fixtureをファイル取込からWorkbench保存・Openまで通す専用検証を追加する。既存のCore import検査とPlayer session往復は別fixtureである。
- `AuthoringWorkbench.SpringVerification`を追加し、Player環境で連続12stepのPose/State、親子追従、sphere距離、交互dtと停止state保持を数値検査する。Springの表示GUIや実素材受入は未実装/未確認のまま。

- Windows-BoneMap build **PASS**: `Logs/build-player-20260912-140337-843.log`。Authoring suite **PASS**: `Artifacts/Authoring-20260912-140446-45def4a5183349bbbc3e4158e4f8498d/report.json`。専用Spring Core in Player checkも成功。実アバター見た目受入は未実施。

### R08: 停止と可変時間積分（2026-09-12）

- `SpringTimeIntegration`へ慣性・stiffness・重力・減衰の予測を分離。stateが前回の正の時間幅を保持し、今回との比率で慣性変位を補正する。初回は静止、不等間隔Verletの重力項を使う。
- dt=0は同じstateを返し、物理履歴を進めない。表示だけは編集されたbase poseへ再投影する。表示と物理状態の停止中の違い、dragForceの1/60秒基準、離散近似の限界は [時間契約](docs/SpringBone-Time.md) を参照する。
- 修正前は停止の追加2テストが失敗（`Logs/core-spring-time-before.txt`、既存318件合格）。修正後はCore **323 passed / 0 failed**、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-a588d8e40e564566b844541e4d15d8c6`。停止反復・再開、停止中編集、可変時間の慣性/重力解析値、減衰残存率、固定/交互時間の比較を確認。
- R10の各回帰はR01〜R09へ同梱したが、最終的な検証範囲の照合、実素材/GUIの受入は未完了。新しいpreview接続では時間とVRM設定値の対応も確認する。

- Windows-SpringTime build **PASS**: `Logs/build-player-20260912-135843-295.log`。GUI/保存経路は未変更のためPlayer suiteは今回再実行していない。実アバターの揺れ具合は未受入。

### R07とR10の衝突回帰: 長さ・衝突の同時制約（2026-09-12）

- `SpringConstraintSolver`へ計算を分離。骨の長さ球面上でcollider境界円へ投影し、各pass後に全colliderと長さを再検査する。hitRadiusを含み、許容差は0.00001。候補方向＋6軸方向、各32passのbounded探索とする。
- 解なしの包囲球と、反復上限による未収束を`SPRING_CONSTRAINT_UNRESOLVED`で診断。Stepは入力pose/stateを変更せず、失敗した途中状態を公開しない。任意の球集合で解が必ず見つかる保証や、連続衝突検出ではない。契約は [SpringBone制約](docs/SpringBone-Constraints.md)。
- 修正前は追加3ケースが失敗（`Logs/core-spring-constraint-before.txt`、既存313件合格）。複数球での循環も検出し、固定した開始方向の再探索を追加。修正後はCore **318 passed / 0 failed**、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-cf49b74bf4f4418a88357d66e89d414b`。
- 新規6ケースを追加し、実際にはgroupを参照しなかった旧衝突テスト1件を削除。実Stepで衝突なし/ありを比較し、同軸、hitRadius、長さ、有限性、Pose/State一致、複数球、中心一致、解なし・反復上限を確認した。R10の残件はR08の停止・時間刻み回帰と受入範囲の最終照合。

- Windows-SpringConstraints build **PASS**: `Logs/build-player-20260912-135403-871.log`。GUI経路は未変更のためPlayer suiteは今回再実行していない。Spring表示や実VRM見た目受入は未実施。

### R06とR10の入力検査: chain別コライダー（2026-09-12）

- `SpringSimulationInputs`へ入力検査・chain hash・参照解決を分離。各chainのgroup参照を昇順・重複なしで解決し、同じchain内のjointだけで不変リストを共有する。全chainの参照unionは廃止した。
- collider groupのnullは参照有無に関係なく`INVALID_SPRING`として初期化・Step入口で拒否。group数は最大256とし、group内最大64件の既存予算と組み合わせる。参照範囲外は従来の`SPRING_COLLIDER_MISSING`を維持する。
- 修正前: 既存311件合格、新規2件失敗（`Logs/core-spring-scope-before.txt`）。独立bone BがAのgroup参照で動く距離は **0.29042628 m**。修正後: Core **313 passed / 0 failed**、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-ac2eda790ccd47f88c273cd998e3476b`。
- A追加/削除・A専用コライダー変更で参照なしBが不変、共有groupを明示した場合は両骨へ作用、null groupを初期状態公開前に拒否することを確認。R07の最終貫通解消・R08の時間刻みは未修正。R10全体はそれらの回帰追加まで未完了とする。

- Windows-SpringColliderScope build **PASS**: `Logs/build-player-20260912-134944-700.log`。今回の変更はCore参照解決で、GUI/保存経路は変更していないためPlayer suiteは再実行していない。実VRMの衝突見た目受入も未実施。

### R04/R05: SpringBone姿勢と階層（2026-09-12）

- `SpringPoseHierarchy`へ親先行の走査と相対transform継承を分離。simulated jointだけでなく全骨を評価し、未登録子孫も追従させる。入力のhead offsetとbasisを維持し、chain/skeleton登録順に依存しない。
- 次tailへの回転は前stateではなく、親の変更を反映した入力poseのtail方向を起点とする。長さはその入力poseのhead-tail距離を使い、scaleを保持する。小角度の`acos`精度問題も3joint回帰で検出し、`atan2`へ変更した。
- 契約は [SpringBone pose](docs/SpringBone-Pose-Contract.md)。呼出し側は各フレームのbase poseと前Stateを別々に渡す。R06/R07/R08、null group等のR10残件と実VRM preview接続は未完了。
- 修正前: 新規4回帰が失敗、既存306件は合格（`Logs/core-spring-pose-before.txt`）。修正後: Core **311 passed / 0 failed**、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f6973616302f4d3f95da0b258af00d19`。固定/変化するbase poseの連続step、入力不変、2〜3joint、逆登録順、offsetあり、未登録子孫、移動・回転・非一様scale、有限値とPose/State tail一致を確認した。

- Windows-SpringPoseRepair build **PASS**: `Logs/build-player-20260912-134557-817.log`。Authoring suite **PASS**: `Artifacts/Authoring-20260912-134641-fa8073058be543af86d2913b37c6ecf6/report.json`。これは既存GUI/保存経路の回帰であり、SpringBone runtime表示や実VRMの手動受入は未検証。

### R01/R02/R09: VRM取込とsession互換性（2026-09-12）

- `VrmAuthorNames`へ作者リストの検査・不変コピー・旧単独名の変換を分離。VRM1は必須の非空配列を読み、各作者名と順序を保持する。上限は256名・各256文字。`Author`は表示用連結文字列、`Authors`は保存する原情報であり、カンマで再分割しない。
- expression / Spring session writerはversion 2の`authors`配列へ変更し、readerはversion 1の`author`も受け入れる。旧空名は空リスト、旧非空名は丸ごと1名として扱う。snapshot schema 4は変更しない。旧payloadは開くだけでは書き換えず、codecで再書出しするとv2になる。
- コライダーごとのnode列は重複と順序を保持し、rootやgroup参照の一意検査は維持。合成VRM0の同一node複数sphereとVRM1の同一/異なるnode・sphere/capsule inventoryを往復検証した。shape詳細値の保持は別の未完了事項。
- VRM1の省略stiffness / dragForceを1 / 0.5へ修正し、明示0と区別。VRM0の既定値は変更していない。
- Core **306 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-1f29d4a482f7474d839565a64cc827b5`。複数作者import→session往復、v1移行・v2保存、型違い・欠落・空作者拒否、複数node列、設定既定値、native snapshot Save/Openを検証。
- Windows-VrmMetadataRepair build **PASS**: `Logs/build-player-20260912-134009-576.log`。Authoring suite **PASS**: `Artifacts/Authoring-20260912-134033-211dbe64c427447b93017ac79d8a3861/report.json`。複数作者・`nodes:[0,0,2]`・設定値を持つsessionで、保存失敗保護・GUI/MCP handler再試行・Workbench Openを確認した。実VRMファイルpicker・実クリック・見た目受入は未実施。

### R03: 作品本体とmetadataの一括公開（2026-09-12）

- `ProjectAttachments`は所有するsession bytesとhashを不変データとして保持し、workspaceのdirty判定へ加える。`ProjectSnapshotCodec`はschema 2/3本体をschema 4 envelopeで包み、設定blob参照を同じmanifestへ保存する。`ProjectStore`の既存writer lock / expectedVersion境界でblobを準備・検証し、manifestを最後に一回だけ置き換え、その後にsaved状態を更新する。
- 旧schema 1/2/3のsidecarはOpen時に取り込む。schema 4は古いsidecarを参照せず、設定を除去しても復活しない。schema 1は従来どおり別保存先への移行が必要。古いバージョンのNyaForgeはschema 4を開けない。形式と責務の詳細は [metadata snapshots](docs/Project-Metadata-Snapshots.md) を参照。
- GUI import時点でmetadataをworkspaceへ渡し、GUI/MCPともに共通ProjectStoreで一括保存する。MCP handlerの保存成功でGUIの失敗表示も解除する。この時点ではR01/R02/R09は未修正だった。後続修正は下記の記録を参照し、保存transactionの修正をVRM互換性全体の合格とはしない。
- Core **303 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-3d890be653834863b2859c79e76939a6`。expression/Spring書込み失敗で旧manifest・本体・metadata・dirty・versionを維持、Save As公開失敗と再試行、legacy移行/削除、corrupt blob、未知field、graphの共通save service往復とstale writer拒否を検証。
- Windows-AtomicMetadata build **PASS**: `Logs/build-player-20260912-132607-475.log`。Authoring suite **PASS**: `Artifacts/Authoring-20260912-132736-efd679b908084601afb033b0e7681dce/report.json`。新規/既存の両方でblob排他ロックによる失敗を起こし、旧設定保持、終了/無確認切替防止、GUI/MCP handler再試行、設定を含むOpenを確認した。終了ボタンが使う判定関数の自動検証であり、実クリック・外部MCP transport・実VRMの手動受入は別途。

### R03前段: Windows GUIの保存失敗保護（履歴・2026-09-12）

- 保存の呼出しと成否判定を`AuthoringWorkbench.Saving.cs`へ分離した。保存全工程の成功をboolで返し、失敗後はWorkbenchの`saveIncomplete`を保持して未保存表示と終了／作品切替の確認を維持する。「保存して終了」は保存成功の明示結果を必須にした。
- 本体が保存できた時点でGUIの保存先も更新し、付属設定が失敗したSave Asの再試行に正しい保存versionを使う。再試行成功か、ユーザーが確認して作品を切り替えるまで失敗状態を解除しない。
- `AuthoringWorkbench.SaveFailureVerification.cs`で、専用fixtureのexpression / Spring sidecarを排他ロックして削除失敗を起こした。main保存後にも終了・無確認切替を拒否し、ロック解除後にversion 2へ保存でき、Openも成功することをPlayerで検証した。実マウスクリックではなく終了ボタンが使う判定関数の検証。
- Windows-SaveFailureGuard build **PASS**: `Logs/build-player-20260912-131616-600.log`。Authoring suite **PASS**: `Artifacts/Authoring-20260912-131645-43dcd4028e8c4f72988dd960d8e5a921/report.json`。専用の`Save failure guard` checkが成功している。本変更はUnityRuntimeのみのためCore/Bridgeの新規実行は行っていない。
- **この前段の時点の残件**: GUI終了・再試行だけの保護で、main/sidecarの一括公開は未実装だった。現在は上段のR03 snapshot実装と検証を参照する。

## 出力予算検証 `forge_validate`（2026-09-12）

- 読み取り専用の `forge_validate` を追加。`documentId` と `expectedRevision` を固定してから、`pc` / `mobile` プロファイルの三角形数・材質数・最大テクスチャ寸法を確認する。
- 結果は全体 `pass` / `fail` / `unknown` と個別checkを返す。骨変形とアバターへのfitは静的制作プロファイルの範囲外として `unknown` にする。合格をVRChatランクや目視品質の保証には読み替えない。
- Core **265 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-c212e2d98f15447cbb1cabff4275c9a6`。空出力のunknown、2材質slotのmobile fail、stale revision拒否を確認。
- Windows-Validation build/Player suite **PASS**: `Logs/build-player-20260912-090956-750.log`、`Artifacts/Authoring-20260912-091026-c10b50cb99a34e7e82dedbf7d8e45a78/report.json`。外部MCPからlive Playerへの検証呼出しも成功。
- 残件はpose別fit検査、実アバターimport、skin/morph出力、複数object。C2の骨・weight・pose基盤は着手済みで、実アバター接続を次の段階とする。

## 出力チェックGUI（2026-09-12）

- 材質パネルの下に折りたたみ式の「出力チェック」を追加。PC / モバイルの目安を選び、現在の文書revisionを自動で固定して検査できる。
- `AuthoringValidationRequest.Create` と `AuthoringValidationReader` を共用し、GUI・MCP・Core検証で判定ロジックを分けない。結果は個別checkの `pass` / `fail` / `unknown` を短く表示する。
- `AuthoringWorkspace.IsExecuting` を公開し、検査ボタンをcommand transaction中だけ無効化。検査自体は文書・履歴を変更しない。
- Windows-ValidationUi build/Player suite **PASS**: `Logs/build-player-20260912-091321-826.log`、`Artifacts/Authoring-20260912-091345-cce0d209c4574c12943ccc16df92f04f/report.json`。
- 目視での文字サイズ・折りたたみ操作は未受入。C2 Rigの実マウス見た目受入と、実アバター接続を次に進める。

## C2 Rigコア基盤（2026-09-12）

### MorphコアとGraph/UI接続（2026-09-12）

- `MorphTarget` はmesh topology hashに紐づく疎なrest-space頂点差分をstable target ID・名前とともに保持し、同一頂点の重複・範囲外・非有限値を拒否する。`MorphSet` は最大256ターゲット、ID/名前重複、別topologyを拒否する。
- `MorphDeformer` は0..1のtarget weightを検査し、複数targetをrest meshへ加算適用する。pose済みmeshへ焼き込む設計ではなく、属性（normal/tangent/UV/submesh）を元meshから保持する。
- `MorphCodec` は疎payloadを厳密な`NYRM` v1へ保存・復元する。mesh topology hash、件数、UTF-8、末尾bytesを検査し、壊れたassetや別meshを公開しない。
- Core **282 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f817e5ee174e410ca5c2f78063889c91`。Morph変形の半量適用、属性保持、決定的codec往復、未知target／範囲外weight／stale topology／末尾bytes拒否を確認。
- Windows-MorphCore build / Authoring Player suite **PASS**: `Logs/build-all-20260912-110016-285.log`、`Artifacts/Authoring-20260912-110055-aa6d942115e2424a930f9d3e39c352a3/report.json`。既存Rig GUI lifecycleも再回帰した。

### Morph Graph/UI接続（2026-09-12）

- `PortType.MorphSet`、`rig.morph-set`、`rig.morph-deform`を追加。MorphSetはmesh topology hashを保持し、MorphDeformはtarget IDごとの0..1 weightを入力payloadとして正規化・保存する。
- Graph evaluatorはMorphSetのtyped outputと、rest-space `MorphDeformer`によるmesh outputを提供する。変形後もmaterial/baseColor/slot参照を保持し、別topologyでは診断を出して公開しない。
- native graph schema 3へ`NYRM` blob参照とcanonical sorted weight payloadを接続。`graph_inspect`はmorph hash、target数、delta数、targetごとのcontent hashを返す。
- Graph canvasに「Morphサンプル」、Workbenchに折りたたみ式target選択／weight編集を追加。既存のcommand・Undo・保存経路でnode更新を行う。
- Core **284 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-52240aca170840e9b350e6859ddd0241`。Morph graphの評価、inspection、native保存→再読込、半量変形、topology変更拒否を確認。
- Windows-MorphUi Player build / Authoring suite **PASS**: `Logs/build-player-20260912-111005-833.log`、`Artifacts/Authoring-20260912-111029-eb76635897054006a74c1d0117fd5872/report.json`。標準fixtureの画像を目視し、追加UIは折りたたみ領域のため実クリック受入は未実施。
- 次は実アバターの編集可能mesh import境界を確定し、normal再計算とMorph/skin exportを別段階で接続する。

### GLB import境界（2026-09-12）

- `Authoring.Import.GlbImport` を追加。GLB v2のheader／chunk長／UTF-8 JSON／単一BINを検査し、16MiB予算内でPOSITION、任意のNORMAL/TANGENT/TEXCOORD_0、triangle indexをCPU可読`MeshData`へ変換する。
- 1 meshあたり最大32 primitiveまでをsubmeshとして結合し、primitiveごとの頂点順序とindex範囲を保持する。skin、属性有無の混在、morph target数の混在、sparse accessor、非対応component/type、未知chunkは`UNSUPPORTED_FORMAT`等で拒否する。元のscene階層・material・quadを復元したとは主張しない。
- primitiveのPOSITION morph targetを`MorphSet`へ変換し、`extras.targetNames`を名前へ反映、source hashから決定的target IDを生成する。normal/tangent morph差分、humanoid、VRM metadataは未対応としてwarningsに残す。
- WorkbenchへWindows Explorer経由の「GLBモデルを取り込む」を追加。空の制作projectだけに新規graph（Source→MorphDeform任意→Output）を作り、既存作品を置き換えない。元GLBをprivateへコピーせず、取り込み済みmesh/blobのみnative graphへ保存する。
- Core **286 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-8a99a1b52c014bda9edfe8a06a9ab595`。合成GLBの複数primitive結合・submesh分離・morph差分結合、skin拒否、破損header拒否を確認。
- Windows-GlbMulti Player build / Authoring suite **PASS**: `Logs/build-player-20260912-113225-389.log`、`Artifacts/Authoring-20260912-113250-94a7a54f014948bf9904b11fcb901beb/report.json`。標準fixtureの起動・描画・既存GUI回帰を確認し、画像も目視した。Unity Bridge receiverも **PASS**: `Artifacts/BridgeReceiver-20260912-113320-467-aecbf12069194bd49a2a6fe6a16ed16d/bridge-report.json`。GLB pickerの実マウス選択とRadDollV3/VRM実データ受け入れは未確認。

### GLB skin importとGUI接続（2026-09-12）

- `GlbDocumentReader` を共通化し、header／chunk／JSON／BINのbounded検査をmesh adapterとskin adapterで共有する。静的mesh側のsource hashと既存Morph保持を変えない。
- `Authoring.Import.GlbSkinImport` を追加。1 mesh／1 skin、最大512 joints、連続するJOINTS_n＋WEIGHTS_nの最大32 influence、translation-onlyのnode／inverse-bindを`SkeletonDefinition`／`SkinBinding`へ変換する。複数skin、回転・非unit scale、JOINTS_1、未weight、sparse／非対応accessorは理由付きで拒否する。
- Windows WorkbenchのGLB取り込みは、skin配列の有無をboundedに判定し、skin付きならSource→MorphDeform任意→Skeleton／SkinBind／Pose→SkinDeform→Output graphを生成する。初期poseはimportしたbone headをtranslationへ置き、既存のRig UI／Undo／native保存経路を利用する。
- Core **288 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-03d1dfa8995d4643b115d34d1e8910db`。合成skinのjoint階層・normalized weight・source hash保持、回転拒否を確認。
- Windows-GlbSkin Player build / Authoring suite **PASS**: `Logs/build-player-20260912-114347-992.log`、`Artifacts/Authoring-20260912-114413-637c0868011d4e329b83a5a2ddb77d74/report.json`。標準fixtureの起動・描画・GUI回帰を目視した。Unity Bridge receiverも **PASS**: `Artifacts/BridgeReceiver-20260912-114608-484-b8f097c1eee54d34a015286334605c27/bridge-report.json`。skin付きGLBの実ファイルpicker操作とRadDollV3／VRM実データは未確認。

### VRM metadata境界（2026-09-12）

- `VrmMetadataReader` を追加。VRM 1.0の`extensions.VRMC_vrm`とVRM 0.xの`extensions.VRM`から、spec version、title/name、author、humanoid semantic→glTF node indexをsource hash付きで読む。UniVRMやUnity APIへ依存しない。
- malformed mapping、node範囲外、重複human bone、未対応spec versionは`INVALID_VRM`／`UNSUPPORTED_FORMAT`で拒否する。expression、look-at、spring bone、MToon、一般transform、VRM exportは別adapterのまま保持する。
- GLB/VRM pickerのfilterを`.glb`／`.vrm`へ拡張し、取り込み時にmetadataを検査する。skin付きなら既存skin graphへ接続し、statusへVRM format/title/humanoid数を表示する。
- Core **291 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-eb7fc75824464221943c9207fe446012`。VRM 1.0／0.x mapping、source identity、未対応versionと欠落extension拒否を確認。
- Windows-VrmMetadata Player build / Authoring suite **PASS**: `Logs/build-player-20260912-115103-670.log`、`Artifacts/Authoring-20260912-115128-ad0dc9697de74b4798fb64c9633adfe4/report.json`。標準fixtureの起動・描画・GUI回帰を目視した。Unity Bridge receiverも **PASS**: `Artifacts/BridgeReceiver-20260912-115200-239-6157d093ce40429f85b7a9745da4db30/bridge-report.json`。実VRMファイル、humanoid姿勢、expression/springの受け取り先は未確認。

### VRM expression inventory境界（2026-09-12）

- `VrmExpression` と `VrmMetadata.Expressions` を追加。VRM 1.0 `expressions.preset/custom` と VRM 0.x `blendShapeMaster.blendShapeGroups` から、名前、preset/custom区分、morph/material bind件数だけを最大256件まで読む。source内のbindをNyaForgeのmorph/materialへ自動適用する処理はまだ持たない。
- GUIのモデル取り込みstatusにVRM expression件数を表示し、対応範囲ラベルをidentity・humanoid・expression inventoryまで更新した。
- Core **291 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-26953d296bb74bd1a44480faeaa939b3`。VRM 1.0／0.xテストでexpression名称、preset/custom区分、morph/material bind件数を確認した。
- 追加のCore **291 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-08598e8437b14a6aa9741ffc44bd1cef`。source morph bindのindex/weight保持と、単一ownerから`MorphSet` stable IDへの解決を確認した。
- Windows-VrmExpressions Player build / Authoring suite **PASS**: `Logs/build-player-20260912-115915-345.log`、`Artifacts/Authoring-20260912-115941-fe34fb0e48c749fe9b2d7d2701d315d8/report.json`。標準fixtureの起動・描画・既存GUI回帰を目視した。Unity Bridge receiverも **PASS**: `Artifacts/BridgeReceiver-20260912-120017-323-9b0849f34ded4f3db2c762fe93bcd6da/bridge-report.json`。実VRMファイルの表情適用、MToon、spring、look-at、一般transformは未確認・未対応のまま。

### VRM expression → Morph対応境界（2026-09-12）

- `VrmMorphBinding` がVRM 1.0のnode/index/weightとVRM 0.xのmesh/index/0〜100 weightを正規化して保持する。`VrmExpressionMapper.ResolveForSingleOwner` は1つのownerと既存`MorphSet`のtarget indexを照合し、stable target IDと0..1 weightの辞書へ変換する。owner違い、範囲外index、同一targetの重複bindは推測せず拒否する。
- Core VRMテストでVRM 1.0／0.xのsource bind値と、GLB morph targetへのweight解決（0.5）を確認した。これはexpression payloadをnative graphへ保存・適用するUIではなく、次段の表情編集へ渡すための純粋なadapterである。
- 実VRMの複数mesh owner、material bind適用、表情スライダー、spring、look-at、一般transform、skin exportは未確認・未対応のまま。
- Windows-VrmExpressionMap Player build / Authoring suite **PASS**: `Logs/build-player-20260912-120714-065.log`、`Artifacts/Authoring-20260912-120743-92d014c8543840c89f8a81945b1f1459/report.json`。新Import module追加後も標準fixtureの起動・描画・既存GUI回帰を目視した。Unity Bridge receiverも **PASS**: `Artifacts/BridgeReceiver-20260912-120817-875-6fde4712d8d3474b83139201c12a4326/bridge-report.json`。

### VRM expression UI接続（2026-09-12）

- `MorphSet`のstable ID生成を`GlbImporter.MorphTargetId`へ共通化し、`VrmExpressionMapper`がMorphSetの並び順ではなく元のsource morph indexからtarget IDを解決するよう修正した。複数Morphを含むfixtureでindex 0の対応を確認した。
- WorkbenchのMorphパネルへ「VRM表情」選択と「選択したVRM表情を適用」を追加。GLB/VRM取り込み時に単一mesh ownerを解決できた表情だけを一覧にし、選択すると全Morph weightを更新して既存の`UpdateNode`とUndoへ渡す。新規／開き直したprojectへVRM metadataを永続化する処理はまだ持たない。
- Core **291 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-c9e2b12978f543c583125e3dadb12fdb`。Windows-VrmExpressionUi2 Player build / Authoring suite **PASS**: `Logs/build-player-20260912-121732-971.log`、`Artifacts/Authoring-20260912-121804-8ece484896984c1d970e44ba2b804a66/report.json`。Unity Bridge receiverも **PASS**: `Artifacts/BridgeReceiver-20260912-121838-049-71972bc3cce24c78b7e8f09e398683f7/bridge-report.json`。実VRMをpickerから選び表情適用する手動受入は未確認。

### VRM expression session sidecar（2026-09-12）

- `VrmExpressionSession`／`VrmExpressionSessionCodec`を追加し、mapped expressionの名前・preset/custom・stable Morph ID weightを`vrm-expression-session.nyaforge.json`へbounded deterministic JSONとして保存する。`AuthoringWorkbench`のSave/Openへ接続したため、同じprojectを開き直しても表情一覧と適用対象を復元できる。元VRMファイルやprivate assetはコピーしない。
- Codec往復・決定性と複数Morph index解決をCoreで確認した。現在のgraphとsidecarのtarget IDが一致しない場合は適用時に再取り込みを要求する。
- 追加Core **291 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-42e7fc51a1354f728929a4d4c2d054c7`。Windows-VrmSession Player build / Authoring suite **PASS**: `Logs/build-player-20260912-122449-685.log`、`Artifacts/Authoring-20260912-122518-a0c65904ada24891b5237649164be16e/report.json`。Unity Bridge receiverも **PASS**: `Artifacts/BridgeReceiver-20260912-122554-137-24a5c4fa1a6941a0998ada473b086944/bridge-report.json`。実VRMによるSave/Open手動受入は未確認。

- `Authoring.Rig` を独立モジュールとして追加。`SkeletonDefinition` はcanonical UUIDのbone、親子階層、head/tailのrest座標を不変データとして保持し、循環・欠落親・重複IDを公開前に拒否する。
- `SkinBinding` はmeshのtopology hashとskeleton hashを固定し、全頂点に1〜4本の明示boneを要求して、重みを降順・決定的順序で正規化する。同一boneの重複、未知bone、未weight、上限超過を拒否する。
- `PoseTransform` と `SkinDeformer` を追加。bone headを基準にしたrest-relative affine poseを適用し、最大4 influenceの位置を線形ブレンドする。mesh topology hash / skeleton hash / 全bone poseを毎回照合し、normal・tangent・UVは元mesh所有のまま保持する。
- `RigCodec` を追加。`NYRG` v1の専用バイナリでskeletonとbindingを保存し、復元時に元mesh topology hash・skeleton hash・厳密な件数/UTF-8/末尾bytesを検査する。
- `rig.skeleton` typed graph nodeを接続。`PortType.Skeleton` と `GraphSkeletonValue` を追加し、native graph schema 3のblobへ `NYRS` v1 skeletonを保存する。`graph_inspect` はnodeごとにskeleton hash、bone数、親子・head/tailを返す。保存→再読込のCore往復を固定した。
- `rig.skin-bind` typed graph nodeを追加。mesh＋skeleton入力の型を検査し、topology hash・skeleton hash・1〜4 influence／全頂点weightを照合して `GraphSkinBindingValue` を出力する。`NYRB` v1 binding blobをnative graphへ保存し、`graph_inspect` は頂点数・influence数・最大influence数を返す。
- この段階ではhumanoid自動配置、MCPからのskin bind生成、skin exportは未接続。rest定義・binding・姿勢評価の正本と、bone移動時のstale／明示再bindを共通commandへ接続した。normal再計算と補正morphは別工程。
- Graph canvasの「Rigサンプル」をPlane＋Root/Child骨＋左右weight＋25度pose＋skin-deformまで拡張。Rigパネルにbone選択、選択頂点への指定weight混合、Root 100%割当、brush weight paint、選択boneのXYZ pose回転、rest bone移動、stale依存の明示再bindを接続した。weight paintは画面上のブラシ半径で頂点を集め、1ドラッグを1つのUndoにまとめる。実アバターのimportはまだ別工程。
- `rig.pose` と `rig.skin-deform` を追加。NYRP v1 pose blob、skeleton/binding/pose hash照合、rest-relative変形meshをnative graphへ保存できる。`graph_inspect` はpose hash／bone数を返す。
- Core **280 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-ffb7f155a0cb4a5cbc77cb59b81c060d`（既存target boneの再weight置換、XYZ Euler pose、Euler pose codec往復の回帰確認を追加）。Windows-MultiAxisPose Player build / suite（MCPなし）**PASS**: `Logs/build-all-20260912-104615-663.log`、`Artifacts/Authoring-20260912-104645-9058853eb3c74d968935d967a1768e63/report.json`。Player内のRig graph評価・inspection・schema3再読込、weight paintのブラシ半径・1操作1Undo、XYZ poseのUndo/Redo lifecycleを確認した。MCP付き全体suiteは外部save transportの一時失敗で未合格だが、`dotnet build Tests/Mcp.Transport/Mcp.Transport.Tests.csproj --nologo` は0警告・0エラー。実マウスでの手動見た目受入、実アバターimportは未確認。
- Unity Bridge受け取り側も `Artifacts/BridgeReceiver-20260912-104935-490-6ec4f5282eb74f96b1615e8520802e95/bridge-report.json` でscale 1/100のBake往復 **PASS**。Rig poseのEditor/VRChat実表示を確認したものではなく、既存Bake受け渡しの回帰確認。

## 面材質割り当ての実通信確認（2026-09-12）

- `mesh.assign-materials` の wire を追加し、整数の疎なslot番号を保持したまま複数材質を登録できるようにした。
- `graph_inspect` の各nodeへ `materialSlots` と `assignedMaterials` を追加。slotごとの材質値とcontent hashを確認でき、画像本体は返さずidentity/hash/寸法だけを返す。
- `polygon.faces.material` の wire を `MaterialFaceEditing` へ接続。`context`・`elementIds`・`materialSlot`・`materialNodeId` を受け、面slotと材質nodeの接続を同一Undo単位で更新する。slot型違い・材質node ID欠落は拒否する。
- Core **262 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-371bfc07471e411884eb35dc28d0ed9b`。疎slot 3/9のinspection、assign-materials wire、面材質割り当てのUndo/Redoと再送を確認。
- Windows-FaceMaterial build/Player suite **PASS**: `Logs/build-player-20260912-090000-851.log`、`Artifacts/Authoring-20260912-090123-58119a05cc614d779e4274d3e32c066a/report.json`。既存のMCP生成・inspect・paint・layer・import・capture・export・save一周も継続成功。
- 残件は材質値そのものの編集UI、面選択からの実操作導線、schema registry、複数object/rig/weight/morph。目視受入と販売品質判定は別途必要。

## 現在地

| 範囲 | 現物と確認状況 |
|---|---|
| C0-R／C1-A | 空project、最大1object、typed graph、共通commandとUndo、native保存、node canvas、static Bake／Bridge |
| C1-B | polygon/corner ID、面選択・押出し・削除・境界cap・厚み、Mirror、編集ケージと最終結果、UV投影と島の数値編集 |
| C1-C Paint | 2D brush、Image port、UV binding、1stroke Undo、native画像保存、3D baseColor、PNG／Surface Bake |
| Layer/mask GUI | 移行・追加・選択・並替・削除・表示・不透明度・名前、mask追加/削除と描画、取消、Undo、保存を検証 |
| Image import | PNG検査・展開・縦横比保持サイズ調整・新layer追加・Undo・保存を検証。Windows pickerの実操作は手動未確認 |
| 3D paint | BVH ray、論理edge/UV連続判定、screen補間、切れた区間の描画、GUI色/mask・仮表示・取消・Undo・保存/出力を実装 |
| 今後 | 3D複数面/細かなseamの精度と操作、一般Material graph、UV再投影、出力identity更新、Evidence/MCP、rig/weight/morphと全身制作 |

保存は設定なしstatic writer schema2、graph writer schema3、metadata付きsnapshot writer schema4、reader 1/2/3/4。Paintは共有画像と部位別の独立画像に対応。材質未割当は不透明preview、標準材質は3alpha modeを選択できる。UV変更時は旧payloadを未解決として保持し、明示rebindで対応を更新する。見た目を保つ再投影ではない。旧mesh-only Bakeは画像を拒否。画像付きmeshは別のSurface Bake profile、単一標準材質はMaterial Bakeを利用。複数材質BakeはGUI/Bridgeまで接続済み。

## PNG importのWindows実通信

- PlayerImportVerificationを分離追加。専用8x4青PNGをアプリ側fixtureで生成し、MCPからSHA256/絶対path/fitで64x64 layerへ取り込み。layer ID/合成hash変化、同command再送、元PNG改変拒否、Undo/Redoを確認。
- 改変は専用fixtureのみでfinallyに元bytesへ戻す。import後の試験layerは削除し、後続のlayer/paint/output/save検証も成功。
- Windows-McpImportLive build/Player suite PASS: Logs/build-player-20260912-084305-490.log、Artifacts/Authoring-20260912-084327-30a6d0c530d64406b2adf0a9513d5613/report.json。今回Core変更なし（直近260）。
- imported pixelの個別実通信readbackは未実施（Core/GUI共通pipelineでは検証済み）。schema/通信回復/取り込み再送の元file依存解消、複数object/rig等の全体残件を継続。
## 複数材質スロットinspection

- mesh.assign-materialsのwire生成（parameters.slots整数配列）をCommandWireGraphReaderへ追加。重複/型違いは拒否し、GraphNodeの疎なslot番号を維持する。
- graph_inspectにnodeのmaterialSlotsとassignedMaterialsを追加。スロットごとに材質値（linear RGBA/metallic/roughness/emission/alpha/hash）を返す。画像はhash/寸法のみで、面IDやスロット番号の欠落を避ける。
- polygon.faces.materialを既存MaterialFaceEditingへ接続するwireを追加し、観測済みcontext/face IDsとmaterialSlotを明示する。
- Core261 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-5aaf49ad31f04d95a5f5dbe693651c46。疎slot 3/9のinspection、assign-materials node wireの並べ替え/文字列拒否を確認。
- Windows-MultiMaterialInspect build/Player suite PASS: Logs/build-player-20260912-085516-488.log、Artifacts/Authoring-20260912-085555-0a7329c440c94cc3a6ff75ca944383cd/report.json。MCP生成/材質inspection/既存制作一周も成功。全体目標継続。
## VRM SpringBone inventory境界（2026-09-12）

- `VrmSpringBoneGroup`／`VrmSpringJoint`／`VrmSpringColliderGroup`を追加し、VRM 1.0の`extensions.VRMC_springBone`（colliders、colliderGroups、springs）とVRM 0.xの`secondaryAnimation`（boneGroups、colliderGroups）から、node参照、chain/root数、collider数、boundedな基本パラメータを読む。VRM 1.0ではshapeのsphere/capsuleとradius、0.xではsphere radiusの形式を検証する。
- SpringBoneはinventory専用で、揺れの物理計算、姿勢適用、collider形状のruntime化はまだ行わない。node範囲外、重複joint、重複index、未対応spec version、予算超過は推測せず拒否する。取り込みstatusへ`spring <chain>/<colliderGroup>`を表示する。
- 公式仕様の構造に合わせた境界である（[VRMC_springBone 1.0](https://github.com/vrm-c/vrm-specification/blob/master/specification/VRMC_springBone-1.0/README.md)、[VRM 0.x secondaryAnimation](https://github.com/vrm-c/vrm-specification/blob/master/specification/0.0/README.ja.md)）。
- Core **292 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-d30603607f7d46c696603787788cf80a`。VRM 1.0／0.xのchain、joint、collider inventoryを追加検証した。Player/Authoring/BridgeのSpringBone表示を含むWindows実ビルド確認は次に行う。実VRMの実データ、SpringBone runtime挙動、手動見た目受入は未確認。
### VRM SpringBone session sidecar（2026-09-12）

- `VrmSpringSession`／`VrmSpringSessionCodec`を追加し、SpringBoneのchain、joint、root、center、collider group、基本パラメータを`vrm-spring-session.nyaforge.json`へbounded deterministic JSONとして保存する。元VRM bytesとruntimeの物理状態は保存しない。
- Workbenchのモデル取り込み時にsessionを作り、Save/Open／新規project切替で表情sessionと同じライフサイクルを通す。モデル取り込みパネルに復元済みのchain／joint／collider group数と「物理未実装」を表示する。
- Core **292 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-7f9bd35157d24405a8fba0bf168dbd5a`。session codecの往復・決定性とmodern collider node保持を追加確認した。Windows-VrmSpringSession2 Player build / Authoring suite **PASS**: `Logs/build-player-20260912-124641-743.log`、`Artifacts/Authoring-20260912-124711-2f4bf3dc9cae41ae8f7daa33ea743bb5/report.json`。Unity Bridge receiverも **PASS**: `Artifacts/BridgeReceiver-20260912-124750-959-ec88c07451b74aed908178b9bd7409df/bridge-report.json`。実VRMのSave/Open手動受入とSpringBone runtime挙動は未確認。
### SpringBone preview core（2026-09-12）

- `SpringBoneJointSettings`、`SpringBoneChain`、`SpringBoneColliderGroup`、`SpringBoneState`、`SpringBoneSimulator`を`Authoring/Rig`へ追加した。stable `BoneId`のchainを入力に、detachedなVerlet 1ステップ、stiffness/gravity/drag、rest length制約、sphere collider解決を計算し、完全な`PoseSet`と次状態を返す。Unity、VRM node index、シーン状態、元VRM bytesには依存しない。
- 入力はchain 256、joint 1,024、collider group参照、delta time 0〜0.25秒などの予算と範囲を検証し、重複joint、未知bone、別skeleton/chainのstateを拒否する。`CreateInitialState`は状態をコピーして保持し、同じ入力の反復結果を決定的にする。
- この段階はVRM node→stable `BoneId` adapter、Workbench/Unity runtimeへの姿勢接続、VRM形式への物理設定保存、実VRMの見た目受入を含まない。次段で受け取り先を決めて接続する。
- Core **297 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-676025c66f614051bc750de1b7a6cf70`。初期状態、gravityによる姿勢回転と長さ制約、sphere collider、入力拒否、状態コピー・決定性を確認した。Windows-SpringPreviewCore Player build **PASS**: `Logs/build-player-20260912-125700-714.log`、`Builds/Windows-SpringPreviewCore/NyaForge.exe`。Authoring suite **PASS**: `Artifacts/Authoring-20260912-125726-ea62fbeeb288427ab8aefc0ab045afbb/report.json`、screenshot `authoring.png`を目視した。Unity Bridge receiverも **PASS**: `Artifacts/BridgeReceiver-20260912-125809-882-6a66d11236df481794f08776fbaad3c2/bridge-report.json`。実VRMのSave/Open、VRM node mapping、SpringBoneの実ランタイム挙動、手動見た目受入は未確認。
## 材質inspection

- AuthoringMaterialReaderを分離追加。graph_inspectの各nodeにmaterialOutput（linear RGBA、metallic、roughness、emission、alphaMode/cutoff、contentHash、画像identity）とassignedMaterialを返す。画像本体は返さずhash/寸法のみ。
- Windows-MaterialInspect build/Player suite PASS: Logs/build-player-20260912-085038-291.log、Artifacts/Authoring-20260912-085111-d01856be26f947f8a47dbc40c08092c2/report.json。MCP生成時の標準材質値と割当先contentHash一致を実通信で確認。
- Core260 passed/0 failed（直近再確認: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f14a0b7718334e52afef49de9f70dcc5）。MCP transport build成功。
- 次は材質値更新操作、複数材質slotのinspection/編集、schema registry/通信lifecycle、複数object/rig等を継続。全体目標は未完了。
## MCP通信回復検証

- AuthoringPipeRecoveryVerificationを分離追加。空frame、UTF-8不正、壊れたJSON、method型不正、改行前切断を送った後にnamed pipe listenerが次接続を受けられることをWindows Playerで確認。
- AuthoringIpcRequestはmethod型をキャスト前に検査し、field集合比較をordinalへ統一。import envelopeのinstance/kind型も明示的に検査する。
- Windows-McpRecovery build成功: Logs/build-player-20260912-084547-299.log。Player suite PASS: Artifacts/Authoring-20260912-084653-e9a13f9cd5d44a8d9e79bc2ce6a5dace/report.json。既存MCP生成/頂点/面/paint/layer/import/capture/export/saveも継続PASS。
- malformed frameは相手へ構造化errorを返さず切断し、次の接続を受ける契約。正常なget_state継続を実通信で確認。送信側のdeadlineと1要求1接続は維持する。ACL readback、複数同時client、バイト単位read性能、job/cancelは残件。
## MCP画像import endpoint

- forge_import_image / import_imageを追加。1個のlayers.importと明示command envelopeのみ受付。path/sourceHash/fit/サイズ/index/layer ID/contextを指定し、Unity main threadでPaintPngImporterと共有PaintLayerImportを通して既存layer commandへ変換。
- CommandWireImageImportを分離しhost decoderを注入。通常forge_applyではlayers.importは非公開。任意pathは読み取りのみ、入力PNGは16MiB/1024制限とprofile検査。sourceHashは元bytesのSHA256。
- 再送は元fileが存在しhash一致する場合に同じdecoded commandとして再送可能。file消失/変更はdecode前に失敗するため、配送不明時はstate/layersを再確認。元PNGのpath/hashではなくdecoded操作のfingerprintで既存command cacheが比較する点を明示。
- Core260 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-383e0e47df64419a8052ca58985466b5。import envelope/loader注入/fit/追加ID/再送/元bytes変更拒否を確認。MCP transport suite（10 tools）成功。
- Windows-McpImport build成功: Logs/build-player-20260912-084122-167.log。今回importの実PNG実通信は未検証、次に追加する。全体開発継続。
## 画像import共通基盤

- PaintLayerImport.Prepareを分離し、fit/寸法/レイヤー作成をGUIと今後のMCPで共有する構成へ移行。GUI ImportPaintImageは同helperを使用。
- PaintPngImporterをファイル読取と検査済みPaintPngInputのDecodeへ分割。任意のexpectedHashを渡した場合は読んだ元bytesに対しCore RequireSourceHashで検査してからdecodeする。GUI従来呼出はhash指定なし。
- Core260 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0edf8e03fb25446480784ba08c8a29b6。透明余白/色/寸法不一致/元bytes変更拒否を確認。
- Player suite PASS: Artifacts/Authoring-20260912-083720-3f53786d0cff438d9d7e74035234be52/report.json。共通化後のGUI PNG取込/fit/Undo/保存/Surface出力および既存MCP suite成功。
- Windows-SharedImport build成功: Logs/build-player-20260912-083654-162.log。MCP import endpoint自体は次工程。file identityとcommand再送仕様を接続し、画像import実通信を確認する。全体目標継続。
## レイヤー/マスク描画のWindows実通信

- PlayerLayerVerificationを拡張。overlayへの緑描画→ゼロmaskで元画像へ復帰→mask strokeで一部表示→mask除去で描画画像へ復帰→Undo/Redoを実MCPで検証。
- 合成image hashとhasMaskを照合。後続のレイヤー管理、5方向撮影、Surface Bake/readback、native保存も成功。
- Windows-McpMasks build/Player suite PASS: Logs/build-player-20260912-083344-733.log、Artifacts/Authoring-20260912-083412-88944e4245394448907993fce5cc05ac/report.json。今回は検証拡張でCore変更なし（直近259）。
- 継続残件: 画像import、複数経路stroke/再bind、MCP schema/通信lifecycle、複数object/rigなど。全体目標は継続、目視受入は未完了。
## MCPレイヤー/マスク描画

- CommandWireLayerDrawingを分離。layers.stroke、layers.mask.fill（uniform target byte/明示サイズ）、layers.mask.stroke（target byte/strength）、layers.mask.clearを追加。fillは置換、clearはmask除去。既存PaintLayerChange/LayerEditing/Undoへ接続。
- UV点列とRGBA byte検査をCommandWirePaintReaderの共通helperへ抽出。sidecar target/strengthを追加しcapabilitiesと説明を同期。
- Core259 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-67be22bb59bb45a99055f74b0753a7a7。既存layer wire試験を拡張し緑pixel描画→mask追加/描画で背景白→clearで緑復帰→Undo/Redoを検証。sidecar/probe build成功。
- 今回Windows Player再build/実通信は未実施。次にmask描画実通信、画像import、schema整理/通信lifecycle等を継続。最新実通信版はWindows-McpLayers。全体目標継続。
## レイヤー管理のWindows実通信

- PlayerLayerVerificationを独立追加し実MCPで移行/透明layer追加/背景非表示/再表示/名前/順序/削除/Undo/Redoを実行。layerStackのID/名前/順序とimage hashで結果を確認。
- 管理操作後も描画画像hashを保持。layered Paintを最終Outputへ接続して五方向撮影/Surface Bake/native保存が成功。Bakeの中心赤/背景白のpixel readbackも既存fixtureで成功。
- Windows-McpLayers build/Player suite PASS: Logs/build-player-20260912-082938-642.log、Artifacts/Authoring-20260912-083005-1e70c849486b4b768eb9298ee45566c6/report.json。今回はCore変更なし（直近259）。
- layer/maskの描画操作、画像import、schema統合と通信lifecycle、複数object/rig等の全体開発を継続。人間の目視受入とは区別する。
## MCPレイヤー管理の接続

- AuthoringLayerReaderを分離しgraph_inspectにbottom-to-topのlayerStack（ID/name/opacity/visible/hasMask/サイズ/context）を追加。未解決UVではcontextを返さず既存stack情報を保持。
- CommandWireLayerReaderとLayerContextCommandを分離。layers.migrate/add/appearance/remove/move/renameを公開。空の追加layerは透明、サイズとindexを明示。既存LayerEditingでstack/UV/domain競合を検査。
- Core259 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-53bfe88c204f4de09abc87367d8872c4。移行再送/画素保持、透明追加/古いcontext拒否、可視性/並替/名前/削除/Undo/native往復を確認。sidecar/probe build成功。
- 今回Windows Player再build/実通信は未実施。次はレイヤー管理の実通信とlayer/mask描画接続を検証。最新実通信版はWindows-McpPaintOutput。全体目標継続。
## MCP塗装モデルの撮影・出力・保存一周

- PlayerPaintOutputVerificationを分離。実MCPでpolygon edit meshとPaint imageを新Outputに接続→最終出力を変更→5方向撮影→Surface Bake→native保存→元の出力へ復帰を検証。
- Unity側でSurface Bakeを読み戻し64x64、中心pixel赤/背景pixel白を確認。画像付き出力がmesh-onlyに落ちないことを実ファイルで確認。
- Windows-McpPaintOutput build/Player suite PASS: Logs/build-player-20260912-082437-913.log、Artifacts/Authoring-20260912-082459-09687042abb24a7cbd2f5dbf80cec518/report.json。mcp-create.logに塗装モデル一周と既存生成/編集/Undo/保存の成功を記録。
- 今回は既存本体機能の実通信を接続して検証を強化。Core変更なし、直近258passed。native保存要求成功とversionを確認、塗装状態のnativeファイルは後続fixture保存で更新される。塗装Surface Bakeは独立出力として残る。
- Layer/mask/importのMCP対応、撮影の人間目視受入/Unity Bridge実受入、schema/通信回復/複数object/rigなど全体残件を継続。
## MCPペイント接続

- CommandWirePaintReaderとPaintContextCommandを分離。image.paintノード作成（整数width/height）、paint.stroke（paintContext/UV points/pixel radius/sRGB RGBA bytes）を公開。
- PaintEditContext.FromIdentityは明示image/UV/domain hashを検査。graph_inspectにpaintContextとimageOutput概要を追加し、現在の描画対象を観測可能にした。既存PaintEditingによる古いimage/UV拒否とcommand replayを共用。
- Core258 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-e3e2a58f7bd7418ea0b6c25cf85cd960。描画pixel、二重再送、古いcontext拒否、Undo/Redo、native画像hash保存往復、byte範囲とnodeサイズ検査。
- Windows実MCPでPaint node作成/接続→描画image hash変化→再送一回性→古いcontext拒否→Undo/Redo一致が成功。Player suite PASS: Artifacts/Authoring-20260912-082240-ec626bde941143bf89cbd30fff030c0b/report.json。
- Windows-McpPaint build成功: Logs/build-player-20260912-082215-168.log。単層Paintのみ。Layer/mask/画像importや最終出力への画像接続の実通信一周、schema等は引続き残件。全体開発継続。
## MCP頂点/面追加・厚み・UV

- polygon.vertices.add（position）、polygon.faces.create（順序付きelementIds/materialSlot）、polygon.solidify（thickness）、polygon.uv.project（既存投影）を公開。すべて明示contextと既存command/Undo実装を共用。生成IDはedit outputのinspectionで取得する。
- Core257 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f8a3ca08f7a945d9be2315654e23fc73。空polygon→3頂点→取得IDで面作成→厚み→全corner UV→native保存/reopen一致を確認。文字列thickness拒否。
- Windows実MCPで押出し後のUV投影→face inspectionの全corner UV読取→Undo/Redoが成功。Player suite PASS: Artifacts/Authoring-20260912-081844-68b03af346284c3594f5240a75ce3d90/report.json。頂点/面追加と厚みの個別実通信はCore確認と区別して残る。
- Windows-McpPolygonUv build成功: Logs/build-player-20260912-081822-267.log。全体目標継続、Paint接続/追加geometry操作/schema等は残件。
## MCPポリゴン編集

- CommandWirePolygonOperationsを独立追加。polygon.vertices.translate / polygon.faces.extrude / polygon.faces.deleteを既存AuthoringOperationへ接続。contextとcanonical string elementIds、必要時rest-space deltaを指定。重複IDを拒否。
- graph_inspectでpolygon-editの入力contextを公開。face/vertex対象IDはedit nodeの現在outputから取得し、input snapshotのcontextと混同しない。faceless polygonのcontextも取得可能。
- Core256 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-74e2cfde1d1a4aed9ba42f1d5115823d。押出し/移動/削除、再送一回性、Undo/Redo、重複と数値ID拒否を確認。
- 実MCPでpolygon生成→面取得→取得IDで押出し→面数増加確認→Undo/Redo成功。Player suite PASS: Artifacts/Authoring-20260912-081540-80846a1d0dc34223a74e1394d1c0b7c2/report.json。移動/削除の個別実通信はCore試験とは別に残る。
- Windows-McpPolygonEdit build成功: Logs/build-player-20260912-081515-472.log。追加polygon操作、UV/Paint、schema統合、job/通信回復、複数object/rig等の全体残件を継続。
## MCPポリゴン生成wire

- CommandWirePolygonReaderを分離追加。mesh.polygon-sourceのdomainId、明示頂点/面/corner ID、座標、materialSlot、scale/translationを解析。mesh.polygon-editの空payload生成も公開。
- IDはゼロなしcanonical decimal string。既存PolygonMeshの参照/重複/予算検査を共用。wireは4096頂点/1024面、実IPCは従来65536bytes上限。現profileはcorner属性なし。UV/normal/tangent付sourceは未公開。
- sidecar PolygonCommand DTOを分離。capabilitiesとforge_apply説明を更新。
- Core255 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-3b591cdbac2b4ec7be7726b2f2f26bc5。完全IPC envelopeから生成/同ID再送/面取得、2^53超ID保持、数値ID/非canonical ID/欠損参照拒否を確認。
- Windows実MCPでpolygon node生成→face取得→node削除が成功。大きなface ID/corner順保持を確認。Player suite PASS: Artifacts/Authoring-20260912-081231-e895d4a043604767b2ec546253d9594e/report.json。
- Windows-McpPolygon build成功: Logs/build-player-20260912-081207-569.log。polygon編集操作wire、context取得、UV/Paintなどは継続残件。
## MCP面inspection公開

- forge_faces_inspect / faces_inspectを公開。既存頂点要求をMeshPageRequest/MeshPageQueryへ改名して共用、Unity meta GUIDを維持。面は64件上限、document/revision/snapshotで取得対象を固定する。
- Core254 passed/0 failed（C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-084747b4431f40e983cb86f03700cd2a）。face envelope解析と65件拒否を追加。MCP transport suiteと9 tool registry成功。
- Windows実MCP Player suite PASS: Artifacts/Authoring-20260912-080853-c8a678be47544305a30b33d498122370/report.json。triangle-onlyへの面要求拒否、その後のstate取得と編集/撮影/出力/保存継続を確認。
- Windows-McpFaces build成功: Logs/build-player-20260912-080823-241.log。
- polygon面の正常読取/疎ID/UVはCore検証。実MCPのpolygon正常読取と面編集は次のpolygon生成wire接続で確認する。全体開発は継続。
## 面inspectionと共通mesh解決

- AuthoringFaceReaderを独立追加。polygon面を安定ID順に最大64面/ページ、各面のmaterialSlotと順序を保持したcorner ID/vertex ID/UVで返す。64bit IDは文字列。最大256corner/面の既存制約と合わせページ量を制限。
- InspectionMeshへdocument/revision/node/current snapshot検証を共通化し、頂点/面readerが同じ現在評価を参照。triangle-only meshには架空のpolygon IDを与えずPOLYGON_REQUIRED。faceless polygonは空ページ。
- Core254 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b055072b7d13434fab76e84e1c3ea403。2^53超の疎ID、corner順/UV、位置transform、結果の分離、範囲/空面を確認。
- 面APIはCoreのみ。MCP公開とpolygon graph生成/編集のwire接続を次に進める。Windows再buildは未実施、最新実通信成功版はWindows-McpVertices。全体目標は継続。
## MCP頂点inspection接続

- VertexPageRequest（Core wire検査）とVertexPageQuery（sidecar DTO）を分離しforge_vertices_inspectを公開。GUI main threadからAuthoringVertexReaderへ接続。document/revision、node input/output、snapshotHash、offset/countを明示する。
- Windows-McpVertices build成功。実MCPで2ページから4頂点のID/位置/identity/終端を確認し、取得IDで頂点編集→撮影→出力→native保存まで成功。
- Player suite PASS: Artifacts/Authoring-20260912-080338-d923003d07994fa1b3f40cc3422f4643/report.json。build log: Logs/build-player-20260912-080314-800.log。
- Core253 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-04a45b911866462e8cbe609511110862。wireの文字列数値、巨大整数、範囲、port拒否を追加。MCP transport suite成功。
- polygon疎ID/transformの追加検証、face情報/selection、polygon/PaintのMCP編集、schema統合、通信回復・job管理、複数objectとrig等は継続残件。全体完成ではない。
## 頂点inspectionのCore基盤

- AuthoringVertexReaderを独立追加。node input/outputの現在評価から、rest/avatar位置を最大1024頂点ずつ返す。document/revision/snapshotを毎ページ検証し、未解決入力には古いpreviewを代用しない。
- mesh頂点indexとpolygon安定IDを区別。polygon IDはulong精度を失わない10進文字列。offsetはIDではなくID順の位置。空の末尾ページを許可しnextOffsetで終了を示す。
- Core252 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-cf95f3667fdf47dfaaa4bdd39162b3ad。ページ境界、位置、snapshot不一致、revision変更、未解決入力拒否を確認。
- この段階はCore APIのみ。MCP要求/sidecar公開、polygon疎IDとtransformの追加確認、Windows実通信は次の作業。Player再buildは未実施。全体開発は継続。
## MCP標準材質と材質付き出力

- CommandWireMaterialReaderを分離しmaterial.standard/mesh.assign-materialを公開。linear RGBA/metallic/roughness/emission/alphaMode/cutoffを必須型で指定し、既存MaterialParametersの範囲検査を共用。sidecar NodeParametersとcapabilitiesを拡張。
- 初回Windows-McpMaterialの外部生成はIPC切断で失敗。材質配列を含むgraph envelopeが旧depth8を越えるためdepth12へ修正し、完全なIPC envelopeのCore解析回帰を追加。
- Core251 passed/0 failed（C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-a9fa054209bc4065a7c59c7dd7180a94）。材質値/alpha保持、不正値・文字列enum拒否、nested envelope解析を確認。
- Windows-McpMaterialDepth build/McpProbe付きPlayer suite PASS: `Logs/build-player-20260912-075303-189.log`、`Artifacts/Authoring-20260912-075327-688685f7948844a0858175607e11c207/report.json`。外部MCPで材質付きgraph生成→頂点編集→5方向画像→Material Bake→保存。PlayerからBakeを読み直しRGBA主要RGB/metallic/roughness一致を確認。既存suite成功。
- 今回は画像textureなしの単一材質。画像/Paintや複数材質のMCP生成・個別往復、材質表示の目視確認、polygon編集等は残件。次は選択に必要な頂点情報/inspectionとpolygon/paint接続を進める。全体goal継続。
## MCP export公開

- ProjectExportRequest/ExportProjectCommandとWorkbench.McpExportを分離しforge_export公開。GUIで選択済みproject directoryとdocument/revision/明示GUID exportIdを指定。出力先はproject/exports/exportId。既存directory/fileはEXPORT_DESTINATION_EXISTSとして拒否し、同ID再送で上書きしない。
- ProjectExportServiceで現在revisionとcomplete状態を検査し属性に応じ既存Bake形式へ出力。resultはsuccess/code/manifestPath/kind/document/revision/hash。native保存や文書変更とは独立。
- Core250 passed/0 failed（C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4884ba4e2e314a9aa434095eac00ca5e）、MCP transport suite成功。
- Windows-McpExport build/McpProbe付きPlayer suite PASS: `Logs/build-player-20260912-074737-361.log`、`Artifacts/Authoring-20260912-074804-264ec9eb97c641b08519cbff1f20cc16/report.json`。外部MCPでstatic出力、再送拒否/bytes保持/state不変、Player側Bake再読込mesh hash一致を確認。既存生成/編集/撮影/保存/GUI suiteも成功。
- MCPでのSurface/Material/MultiMaterialの個別往復、外部Unity Bridge実受入、directory競合の別process同時操作/reparse point境界は未検証。export失敗時の部分blob清掃やidempotent成功再取得も未実装（既存先拒否）。次はpolygon/paint等の制作範囲とschema/inspection/通信回復の残件を進める。全体goal継続。
## 明示revisionの共通export service

- Persistence/ProjectExportServiceを追加。instance/document/revisionを確認し、現行complete評価の属性からMesh/Surface/Material/MultiMaterialの既存BakeStoreへ振分け。書込実装は重複せず、材質/画像を落としてmesh-onlyへfallbackしない。結果はmanifest path/kind/document/revision/hash。
- GUI Exportを同serviceへ移行。Core250 passed/0 failed（C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-1bd278dfc7e2485faae234b1056ad7a6）。mesh/material出力と読込、状態/dirty不変、古いrevisionではdirectory未作成を確認。Surface/MultiMaterialの既存試験もsuiteに含む。
- Windows-ExportService build/McpProbe付きPlayer suite PASS: `Logs/build-player-20260912-074404-388.log`、`Artifacts/Authoring-20260912-074429-fcc28d78d95747aca8411e69b2040d70/report.json`。既存GUI書き出し、MCP編集/撮影/保存/生成など成功。
- MCP export toolはまだ未公開。次は同serviceへtyped export要求をつなぎ、GUI project配下の新規出力directoryとmanifest応答を検証する。出力先アプリの実受入は別gate。polygon/paint/schema/通信回復等の全体残件を継続。
## MCP native保存の実Player接続

- ProjectSaveWireReaderとSaveProjectCommand、Workbench.McpSaveを分離追加。forge_save_projectはget_stateのsaveTarget（GUIで選んだdirectory/期待saveVersion）とdocument/revisionを指定。別directoryはSAVE_TARGET_CHANGED。ProjectSaveService経由で保存しGUIのsavedDirectory/path/dirty表示を同期。
- 保存は既存projectの破棄/openや任意path選択をしない。保存直前の文書identity/revisionとdisk saveVersionを検査。通信成功とは別にsuccess/codeを返し、同要求再送はSAVE_CONFLICT。
- Core249 passed/0 failed（C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-7effe577ce2c49a29408f8a812f3b278）、MCP transport suite成功。
- Windows-McpSave build/McpProbe付きPlayer suite PASS: `Logs/build-player-20260912-074103-215.log`、`Artifacts/Authoring-20260912-074128-8c20b0343904493a854a92107a0e0e44/report.json`。static/graph両方で実MCP保存、再送競合、dirty解除/revision不変を確認。Playerから保存物を再openしてstateHash一致とGUI savedDirectory一致を確認。出力は各fixtureのmcp-static-native/mcp-created-native。
- 保存先はGUI選択済みpathに限定。ファイルpicker/保存先変更のMCP機能、layout保存のMCP同期、入力破損・停止等の通信回復試験は残件。次はexport接続とpolygon/paint/schema等の制作一周に必要な機能を進める。全体goal継続。
## 明示revisionの保存service

- Persistence/ProjectSaveServiceを追加。不変ProjectSaveRequestでinstance/document/revision/directory/expectedSaveVersionを指定し、workspace.Gate内で一致確認してから既存ProjectStore.Saveへ渡す。保存結果はdocument/revision/hash/version/正規化directoryの不変record。書込仕様とblob処理を重複しない。
- Core249 passed/0 failed（C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-eb3098bf58a647d08e9af01eb5a829c7）。編集後の古いrevision拒否とmanifest未作成、保存/再読込hash一致、同expectedSaveVersionの再要求拒否と既存bytes不変、別instance/document拒否を確認。
- 保存再送は現状SAVE_CONFLICTとなる。編集commandのidempotencyと混同しない。次は保存要求のwire/sidecar/GUI同期を接続し、保存先境界と明示version、保存結果の再取得を定義する。MCP保存はまだ未公開、GUIは従来ProjectStore呼出のまま。Playerは今回未build。全体計画継続。
## MCPへ五方向の画像コンテンツ返却

- Workbench.McpCaptureを分離。最終Ready snapshotを一回取得し、既存EvidenceModelCaptureで正面/背面/左/右/斜め256pxを撮影。metadataとcapture record、PNGをinline返却。ディスクへ保存せず、記録内filenameはinline画像のhash識別用。
- sidecar CaptureResultが5つのImageContentBlockとmetadata/cameraのText/StructuredContentへ変換しforge_capture公開。base64をtextへ重複掲載しない。IPC応答上限は4MiB。
- Windows-McpCapture buildとMcpProbe付きPlayer suite PASS: `Logs/build-player-20260912-073529-842.log`、`Artifacts/Authoring-20260912-073554-6d0a0c0e9e9f4d2ea6de0d3b32b2e29f/report.json`、mcp-external.log。外部MCPから編集後モデルを撮影し5画像block/PNG signature/256px/sha256/取得stateHash/snapshot一致を検証。既存編集/生成/GUI suiteも成功。Core suiteは今回は未再実行（直近248）。
- 最終結果/固定256pxのみ。node対象・カメラ指定/比較・job管理・途中取消はMCP未接続。同期main-threadで5枚撮るため重いモデルではtimeout検証が必要。metadataのcaptureStatus=not_requestedは取得記録で、別capture recordのcompleteが撮影結果。実AI clientで画像が表示された人間の目視受入とは区別する。
- 次はnative保存/exportのMCP接続と頂点情報等のinspectionを進め、空から小物制作を一周させる。schema/通信回復/ACLや全体制作計画の残件も継続。
## MCP graph頂点編集と上流identity検査

- GraphEditContext.FromIdentityとCommandWireContextReaderを追加。明示graph/node/inputSnapshot/domainを検査し、既存TranslateGraphVerticesへ渡す。現在のcontextで勝手に補完しない。graph_inspectは対応EditMeshのeditContextと入力vertexCountを返す。
- sidecarへEditContextCommand、graph.vertices.translateを接続。deltaはrest空間、vertexIdsは入力render index。polygonのstable IDは別の未公開経路。
- Core248 passed /0 failed（C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f96e96a839c545cdb8c8f0aa11377289）。直接GraphEditingとのmesh hash一致、上流変更後のEDIT_CONTEXT_STALE拒否と文書不変を確認。
- Windows-McpGraphVerticesのbuild/McpProbe付きPlayer suite PASS: `Logs/build-player-20260912-073204-590.log`、`Artifacts/Authoring-20260912-073229-61f9ed1a9276475f93d36feb73ed8d07/report.json`。実外部MCPでplane→edit→output生成、inspectからcontext取得、頂点移動、上流サイズ変更、旧context拒否を確認。既存suiteも成功。
- 頂点位置のchunk取得、専用forge_edit_context/selection ID、polygon/paint/Mirror等のMCP拡張、Evidence画像返却と保存/export、schema統合、通信回復/ACL検証などは残件。全体計画継続。
## 外部MCPから空Playerへgraph生成

- sidecarのGraphCommand/NodeCommand/NodeParameters/EdgeCommandを別ファイルへ追加し、ApplyOperationへgraph/node/newObjectIdを接続。capabilitiesに生成/node追加更新削除/output指定と対応node3種を掲載。node更新はpayload置換であることをtool説明に明示。
- PlayerCreateVerificationを独立追加。実外部MCPで空→明示IDのplane/output graph生成→同command再送→Undoで空→Redo同hash→plane幅更新を確認。元workspaceはPlayer verifierのfinallyで復元。
- Windows-McpCreateGraph buildとMcpProbe付きPlayer suite PASS: `Logs/build-player-20260912-072835-290.log`、`Artifacts/Authoring-20260912-072901-934ff339c4734ba6b5896f233bd17879/report.json`、mcp-create.log。既存static編集/replay/conflict/UndoとGUI suiteも成功。Coreは前回247件、今回は再実行なし。
- 対応nodeはplane/edit/outputに限定。他のnode、graph頂点context、polygon/paint、native保存/export/captureのMCP公開は次。空projectはユーザーが開いたものを利用し、既存制作物を破棄するproject.create/openはまだ公開していない。全体計画継続。
## Graph生成wireのCore拡張

- AddGraph(graph,objectId) overloadを追加。既存GUI用AddGraph(graph)はGUID生成を維持し、transportは明示object IDを使用して再読込/再送時のfingerprintを安定化。
- CommandWireGraphReaderを別partialファイルへ分離。object.add_graph、graph.node.add/update/remove、graph.outputを解釈。graphId/outputNodeId/nodes/edgesとnodeId/typeId/version/parametersを厳密に検査。初期node wire対応はprimitive.plane、mesh.edit、mesh.output。parameter payloadの編集履歴や他node種は未対応。
- Core247 passed/0 failed（C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-2af4495646d544ba815592de22bc63b7）。空workspaceへ明示IDでplane→outputを生成、再送でrevision不変、Undoで空に復帰、Redoで同object ID/mesh hash、version型拒否を確認。
- Sidecar ApplyOperation DTOはまだgraph/node payload未対応。capabilitiesのremoteOperationsも前回5種のままで、新生成経路は未公開扱い。次はtyped DTO/schemaとcapabilitiesを接続し外部MCPから空→graph生成→編集をPlayerで検証する。Player再buildは今回は未実行。全体計画は継続。
## MCP applyの実Player接続

- AuthoringReadRequestをAuthoringIpcRequestへ改名。apply時のみcommand envelopeを受け、outer/inner instance一致を検査。上限64KiB。typed ApplyCommand/ApplyOperationをsidecarへ追加しforge_apply公開。任意コード実行や現在revision補完はしない。
- Workbench.McpCommandsがmain-threadでcommands.Execute(envelope,projection)を呼び、選択/GUIをrefresh。個別CommandResultのsuccess/code/revision/hash/evaluationを返す。通信成功とcommand成功は別。再送はcommand IDを維持し、sidecar自動再試行なし。
- capabilitiesへapplyと5種のremoteOperationsを掲載。static vertices.translate、graph.connect/disconnect、history.undo/redoのみ。グラフ頂点編集・node生成・polygon/paint/project作成等はまだ未公開。
- Core246 passed/0 failed（C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b7e2322ec4314686abe0307de3cbc581）。MCP transport suite PASS。
- Windows-McpApply buildと外部McpProbe付きPlayer suite PASS: `Logs/build-player-20260912-072334-663.log`、`Artifacts/Authoring-20260912-072359-59a2e8b6e5ed4c768fa38454fba4c92a/report.json` と mcp-external.log。実Playerを外部MCPから頂点移動→同ID再送→旧revision拒否→Undoし、stateHash復元を確認。既存GUI suiteも成功。編集中画面の目視は別途。
- 次はgraph edit context/typed node生成等を接続し、空から小物を作るMCP経路を実証する。schema正本の統合、タイムアウト/停止/不正入力回復の追加検証、Evidence画像返却、全体制作計画は継続。
## 型付きcommand wire reader

- Commands/CommandWireReaderを独立追加。expectedInstanceId/documentId/revision/commandId/objectId/baselineHashを必須指定し、現在値による補完やcommand ID再生成をしない。operationごとのfield/type検査を経て既存AuthoringOperation factoryへ変換。
- 初期対応はhistory.undo/redo、static vertices.translate、graph.connect/disconnect。1..64 operation、頂点ID最大4096。未知operation、余剰field、文字列revision等を拒否。graph編集context・node生成・polygon/paint等は次の拡張対象で、MCPへはまだ公開していない。
- Core246 passed / 0 failed。wire経由と通常編集のmesh hash一致、同command再送がrevisionを増やさない、古いrevision拒否、Undo、未知payload/type拒否を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-260192484c734482abbe1685a73e6475`。
- 次はwire envelopeをMCP/IPCへ接続し、projection付き既存command serviceをmain-threadで呼びGUIを同期する。成功/失敗はcommand結果を返し、timeout後の再送は同command IDを維持。remoteEditingは接続完了までfalseのまま。Playerビルドは今回は未実行。全体計画継続。
## MCP graph inspection

- AuthoringGraphReaderを独立追加。workspace.Gate内でdocument/revision/hashとgraph node/edge/port/diagnosticsを取得。instance依存portはBuiltinNodes.Findで解決。現在のevaluationのmesh input/output hash/domain/renderableだけを返し、stale previewを代入しない。空文書はgraph=null。
- forge_graph_inspectをsidecarへ接続。現在の単一graph全体（最大128node/512edge）の概要を返す。形状payload/parameter値/parameter schema/対象filter/cursorはまだ未提供。
- Core244 passed/0 failed（C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-5eae18a3a01c497a80b8c73558045c36）。切断後のdiagnostic/current nullと古い取得値の不変性、空/別instance拒否を確認。MCP transport suite PASS。
- 外部probeは一時的な非空fixtureを読み、元workspaceをfinallyで復元。Windows-McpGraphInspect buildと外部McpProbe付きPlayer suite PASS。`Logs/build-player-20260912-071649-194.log`、`Artifacts/Authoring-20260912-071714-7fa60a36b0ab4b8a8ed2f536a68fd011/report.json`。実graphのnodes/edges/evaluationとdocument/hash、capabilities/stateも確認。
- 次はtyped編集要求と既存command envelopeを接続し、revision/commandIdの契約を維持してGUIと同じ結果を実証する。対象filter/schema等のinspection残件、Evidence captureのMCP公開、通信回復/ACLの追加検証、全体制作計画も継続。
## MCP capabilitiesとread protocol分離

- Core Inspection/AuthoringReadRequestとAuthoringReadServiceを追加。requestはUTF8/4KiB/depth8/重複field/余剰field/必須型/GUID/versionを検査し、文字列version等の暗黙変換を拒否。listenerはrequest解析と業務処理を委譲し、methodをmain-threadへ渡す。
- forge_capabilitiesを公開。アプリのBuiltinNodes.Definitionsからtype/version/portを生成し、instance依存portを明示。remoteMethods、remoteEditing=false、単位/空間、graph上限を返す。ローカルnode対応とMCP編集対応を混同しない。
- Core 243 passed/0 failed（C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-3a17ffb5303b4336abf6ee4db7b25964）。MCP transport/protocol suite PASS。Windows-McpCapabilities buildと外部McpProbe付きPlayer suiteもPASS: `Logs/build-player-20260912-071336-472.log`、`Artifacts/Authoring-20260912-071402-0d046bedc5fd4f338c7a86ff6bc66428/report.json`。実capabilities＋3回state取得を確認。
- capabilitiesのparameter schema/出力profile属性契約はまだ未掲載。未知/破損request後の実Player回復、停止/切替/ACL検証も残件。次はgraph inspectionを同read serviceへ接続し、共通command/Evidence接続へ進む。全体goal継続。
## 実外部MCPからPlayerへの状態取得成功

- Tests/Mcp.Transport/PlayerStateVerificationとWorkbench.McpExternalVerificationを分離追加。明示McpProbe時のみ、Playerが外部.NET probeを起動→公式SDK client→stdio sidecar→named pipe→実Player main-thread readerへ接続する。3回連続でinstance/document/revision/stateHashを期待値と照合。
- 初回Windows-McpEndToEndはstate call失敗。応答送信直後のDisconnectNamedPipeで未読outputが失われ得るため、応答後はclient closeまでread待機し接続deadlineで上限を設定。外部probeへserver stderrも記録。
- 修正後Windows-McpResponseLifetimeのPlayer suite PASS。`Logs/build-player-20260912-070956-459.log`、`Artifacts/Authoring-20260912-071017-c76730a77cc5402cae7f3dd31e8f4759/report.json` と `mcp-external.log`。3連続get_state成功、既存GUI suiteも成功。
- 再現: `dotnet build Tests/Mcp.Transport/Mcp.Transport.Tests.csproj` 後、`Tools/Test-NyaForgeAuthoring.ps1 -BuildName Windows-McpResponseLifetime -McpProbe Tests/Mcp.Transport/bin/Debug/net10.0/Mcp.Transport.Tests.dll -TimeoutSeconds 600`。
- 接続の実証範囲は状態取得のみ。次はprotocol入力検査/切断・停止・切替の回復、capabilities/graph inspectionと共通command/Evidence接続を進める。MCP全体と制作全体は未完了。
## Unity named pipe互換性修正・Player成功

- 診断buildのstackでSystem.Security.Principal.WindowsIdentity.get_OwnerのNotImplementedを特定。`Artifacts/Authoring-20260912-070417-a2479b023c2f4da5999ffb0d78241392/player.log`。CurrentUserOnlyの内部処理で発生していた。
- Platform/WindowsAuthoringPipeへnative作成を分離。process tokenのTokenUser SIDを取得し、そのSIDだけへのprotected DACLを指定。CreateNamedPipeWはduplex/overlapped/first-instance/remote拒否。SafePipeHandleをNamedPipeServerStreamへ渡して以後の通信を共用。SID/descriptor/tokenのnative資源をfinallyで解放。
- listenerは1個のserverを保持し、各要求後Disconnectして次の接続を待つ。作り直しと旧client handleの競合を避ける。
- Windows Player build/suite PASS: `Builds/Windows-McpNativePersistent/NyaForge.exe`、`Logs/build-player-20260912-070600-589.log`、`Artifacts/Authoring-20260912-070621-e5fea6101463414db4c2671103b51389/report.json`。Player内の別thread client→named pipe→main-thread state readerのinstance/document一致を確認。既存suiteも成功。
- 実外部MCP client→sidecar→このPlayerの一気通貫は次。複数要求/停止再開/文書切替/不正入力回復/ACL読戻しと別user拒否はまだ追加検証が必要。MCP編集/画像など全体計画は未完了。
## Unity受信処理の初回実装・互換性失敗

- Platform/AuthoringPipeServerとAuthoringWorkbench.Mcpを分離追加。単一client/要求上限4096byte/8秒timeout/main-thread Pump/停止時dispose、明示開始/停止とinstance欄を実装。現在はget_stateのみ。
- Windows-McpListenerVerifiedはビルド成功。しかしPlayer実通信は失敗。`Artifacts/Authoring-20260912-070222-204d7604dce743ae93f6c8145cd38132/report.json` は passed=false、IPC test canceled。player.logのAI接続停止理由は「The method or operation is not implemented.」。Unity Monoで使用したNamedPipeServerStream/CurrentUserOnly周辺の互換性問題が疑われるが、正確な呼出箇所のstackはまだ未取得。失敗を接続成功と扱わない。
- 次はFailureにstackを残して箇所を確定し、Windows native named pipeと明示ACLのadapter等で互換性を解決する。CurrentUserOnlyを単に外す回避はしない。新GUIからのMCP接続はまだ利用不可。最後の機能確認済みPlayerはWindows-EvidenceCamera。
- Core/MCP側既存試験の成功とは別のUnity runtime失敗。全体goalは継続、外部入力待ちのblockerではない。
## MCP protocol往復と状態取得Core

- Tests/Mcp.Transport/McpProtocolVerificationは公式SDK clientで実sidecar子processを起動。stdio接続、forge_get_stateの一覧、tool call→named pipe→結果JSON、IPCエラーがMCP IsErrorへ伝わることをPASS。受信側はテストpipeでありUnityではない。旧通信試験もPASS。
- Authoring/Inspection/AuthoringStateReaderを独立追加。workspace.Gate内でinstance/document/revision/hash/dirty/saveVersion/Undo/Redo/評価状態/object概要を同時取得し、コピーJSONを返す。sourceEpochは未対応のnull。別instanceとcommand実行中の再入を拒否する。
- Core 242 passed / 0 failed。空/編集前後/コピー隔離/別instance拒否を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-be5cac2a60b8461ea4481b170e432a97`。Playerビルドは今回未実行。
- 次はUnity側pipe listener、main-thread dispatch、instance表示/寿命管理へ接続し、実Player→MCPでstateを確認する。現時点では実アプリ接続/編集は未完成。全体計画は継続。
## MCP sidecarの初期実装

- Tools/NyaForge.Mcpを独立.NET10プロジェクトとして追加。公式SDK 2.2.0、lockfile、stdio host、forge_get_state入口、instance GUID必須のnamed pipe client。Unity listenerは未実装なので実アプリにはまだ接続できない。
- build 0 warning/error。Tests/Mcp.Transportは実named pipeで正常応答/別instance拒否/接続不可時取消をPASS。実MCP clientとのprotocol往復は未検証。
- [MCP実装メモ](docs/Mcp-Integration.md)へ依存の版/license、IPC上限/相関ID、残件を記録。次はMCP protocol往復とUnity側main-thread dispatch/listenerを実装する。既存全体計画は継続。
## 保存済みカメラでの比較撮影

- AuthoringWorkbench.EvidenceCameraを独立追加。比較toggle、比較元capture manifestのpath欄、最後の成功セットを比較元にするボタンを提供。再起動後も保存済みpackageのpathから同条件の撮影が可能。
- 比較時はEvidenceCaptureReaderでpackage全体の整合を確認し、記録済みviewの位置/注視点/up/倍率/clip/解像度/順序を再使用する。自動fitしない。元packageが欠落/破損していれば失敗とし、新規自動fitに切り替えない。撮影開始時にviewを取得し入力欄をlockする。
- Windows Player build/suite PASS: `Builds/Windows-EvidenceCamera/NyaForge.exe`、`Logs/build-player-20260912-065316-388.log`、`Artifacts/Authoring-20260912-065341-0956acac415343fc9f7b301e955862e9/report.json`。GUI開始実クリック、編集後の全camera値一致、PNGの変化、入力lockと欠落source拒否を検証。既存suite成功、Core変更なし。
- 比較元はpath指定（ファイルpicker未接続）。元の画像/metadataを含むpackage一式が必要。新画像にcamera条件は保存されるが比較元capture IDの専用関連付け、並列画像比較UI、実際の再起動操作は未検証/未実装。形状変更ではみ出す場合は自動補正しない旨をGUIに表示。
- 次はEvidence/MCP接続計画に従いMCPの依存/通信方式を確認してadapterへ進む。全身/rig/weight/morph等を含む全体計画は継続。
## 撮影対象のノード入力・出力選択

- AuthoringWorkbench.EvidenceTargetを独立追加。撮影対象dropdownに最終結果と各対応mesh portの入力/出力を表示。node IDで選択を維持し、対象削除/文書切替で最終結果へ戻す。未解決/面なし対象は撮影不可、撮影中は選択不可。
- 撮影開始時に選択対象のEvaluatedSnapshotを取得し、以後の編集から独立して保存する。最終結果が未完了でも解決済みnodeを撮影できるCore契約を使用。
- Windows buildとPlayer suite PASS: `Builds/Windows-EvidenceTarget/NyaForge.exe`、`Logs/build-player-20260912-065044-155.log`、`Artifacts/Authoring-20260912-065106-5351402f39444057b8df251c5c949b4e/report.json`。新GUI検証ではnode出力を選択→開始実クリック→選択ロック→保存metadataのnode ID一致と文書不変を確認。既存suiteも成功。
- Core変更なし。入力port選択/未解決node/削除時fallbackのGUI実操作は未検証。次は固定cameraの比較撮影とMCPへ進む。全体計画の未実装範囲は継続。
## 厚み・テクスチャ付き五方向撮影の検証

- EvidenceTexturedVerificationを独立追加。自作planeへ厚みを付けた6面モデル、赤/青の8x8 texture、標準材質を使い、正面・背面・左・右・斜めの撮影を検証。
- 全方向に着色pixelがあり、正面/背面に赤と青が残ること、材質数1・画像hash数1・polygon面数6、capture packageの保存読込とPNG hash一致を確認。斜め画像を目視し厚みと色分けを確認。
- 最新確認済みPlayer: `Builds/Windows-EvidenceTextured/NyaForge.exe`。build log: `Logs/build-player-20260912-064446-034.log`。PASS: `Artifacts/Authoring-20260912-064510-2fd5d2b4c880499baff906746e8b2fb7/report.json`。既存Player suiteも成功。Core変更なし（直近241件、今回は再実行なし）。
- 簡単な自作fixtureの検証であり、アバター全体や複雑な材質の品質保証ではない。次はnode対象指定・固定camera比較のGUI接続、MCP。全身制作・rig/weight/morphを含む全体計画は未完了。
## 保存結果へのGUI導線

- AuthoringWorkbench.EvidenceResultを独立追加。最後に成功したcapture manifestのreadonly欄、保存先フォルダを開く、パスコピーを提供。現在の試行のlastEvidencePathと成功済みsuccessfulEvidencePathを分離し、中止/保存失敗で成功結果を消さない。
- ファイル存在時だけ結果ボタンを有効化。フォルダは成功結果の親directoryをUseShellExecuteで開き、パスを引数文字列へ連結しない。コピーはユーザーのボタンクリック時にのみ行う。
- 最新確認済みPlayer: `Builds/Windows-EvidenceResult/NyaForge.exe`。log: `Logs/build-player-20260912-064221-184.log`。PASS: `Artifacts/Authoring-20260912-064246-b344be026d424d3ab6391368697fce3f/report.json`。保存結果path/ボタン有効性/親directory、中止・失敗後の成功結果保持と既存suite成功。Core変更なし（直近241件）。
- OS Explorerの起動とclipboardは自動fixtureで実行していない。結果欄の長いパス表示、削除済みファイル時の操作、再起動後の履歴保持は追加対象。成功結果は現在の起動中だけ保持する。
- 次は厚み付き・材質/texture付きモデルの五方向captureを検証し、node対象指定/固定camera比較をGUIへ接続する。MCP等の全体計画も継続。

## Evidence中止/失敗/文書切替の検証

- AuthoringWorkbench.EvidenceLifecycleVerificationを分離。開始/中止の実クリック→directory未作成/manifest未公開、既存fileを保存directory指定→失敗/既存保持/controls復帰、正しいdirectoryへ再試行を確認。
- 撮影開始直後に表裏頂点を移動し、保存metadataが開始時stateHashを持ち、現在の編集が巻き戻されないことを確認。さらに撮影中に空workspaceへ切替え、保存は旧document ID、GUIは空のままを検証。
- 別文書に切り替わった場合は完了欄へ「撮影開始時の文書の結果です。現在の文書とは異なります。」を表示。改行依存の初回置換が適用されなかったことをrgで検出し、finallyの実コードへ適用して再build。
- 最新確認済みPlayer: `Builds/Windows-EvidenceLifecycleStatus/NyaForge.exe`。log: `Logs/build-player-20260912-064006-489.log`。PASS: `Artifacts/Authoring-20260912-064029-ea5e588ebf884eef82254a581ae4faee/report.json`。上記と既存suite成功。Core変更なし（直近241件）。
- 次は材質/textureを持つ厚みのあるモデルの5方向capture検証、保存結果へアクセスするGUI、現view固定比較/対象node指定へ進む。現在中止は最初のframe待ちで試験、描画後/保存中断やOS終了時の中断復旧は未検証。全体計画/MCP/rig等も継続。

## Evidence五方向プリセットとGUI

- EvidenceViewPresets.FiveViewsはbounds中心と共通倍率で正面(+Z)/背面/左(-X)/右/斜めを生成。固定球半径に余白を付け、各viewで個別にfitしない。
- AuthoringWorkbench.EvidenceCaptureを分離。「モデルの確認画像を保存」foldout、保存directory、開始/中止、進捗/保存先を提供。最終Ready meshのsnapshotを1回取得し、各frameに1枚撮影。二重起動禁止。途中で制作が変わっても取得済みsnapshotを使う。全画像が揃うまでset保存しない。
- 最新確認済みPlayer: `Builds/Windows-EvidenceGuiVerified/NyaForge.exe`。log: `Logs/build-player-20260912-063701-611.log`。PASS: `Artifacts/Authoring-20260912-063727-6cc4eddfe67c4436a876e6866f4aba93/report.json`。開始ボタン実クリック、busy排他、5枚/異なるcamera位置/共通倍率、set読込と文書hash不変、既存suite成功。evidence-gui.pngを目視し説明/保存欄/開始ボタンを確認。
- Core単独suiteは今回未再実行（直近241件、Player compileとsuite成功）。平面fixtureなので側面が線/背景になる場合は正常。厚みのある物体/材質付き5方向の画像品質は次。
- 次は中止の実クリック、撮影中の編集/新規切替、保存失敗後の再試行、保存完了欄の表示/フォルダを開く導線を確認。Unity unload完了待ち/複数job queue、空/面なし撮影、GUIのnode対象指定、MCPは残件。全体計画も継続。

## Capture readerと実画像保存往復

- EvidenceCaptureReaderは専用schema/項目型、1..8枚、index順、profile/背景/照明識別、camera軸/clip/解像度を検証。metadataの固定filename/ID/hash/Ready状態、PNGのhash名/内容hash/構造/寸法を照合。結果はコピー隔離したrecordでありgeometryを再構築しない。
- Core **241 passed / 0 failed**（既存テスト拡張）。保存読込のbytes/ID一致とコピー隔離、../file拒否、解像度不一致、PNG改変拒否を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-5cef58a4b56e4c3c86dca91a6f7d68c2`。
- 最新確認済みPlayer: `Builds/Windows-EvidencePackage/NyaForge.exe`。log: `Logs/build-player-20260912-063348-006.log`。PASS: `Artifacts/Authoring-20260912-063411-07ec79e130f746279ef605cfd1ee72fe/report.json`。実capture画像2枚（同じview再撮影）をevidence-packageへ保存/読込しsnapshot ID/枚数/PNG bytes一致。既存suiteも成功。
- 実画像の異なる多方向view、材質/画像付きモデル、GUI入口/queue/取消、環境識別（graphics/color space）と各profile拡張は次工程。Readerはhash整合を検査するが署名認証ではない。同じview2枚のテストを多方向検証と扱わない。全体計画の残件も継続。

## Evidence画像セットの保存Core

- EvidenceCaptureSetは1..8枚、同じ取得snapshot参照のみを許容。入力配列はclone。EvidenceCaptureCodecは専用kind=nyaforge.evidence.captureとしてcamera/解像度/profile/背景/照明識別/PNG hashと元metadata file/hashを記録。単独metadataは撮影なしの取得記録として不変、撮影完了は別capture recordで示す。
- EvidenceCaptureStoreはmetadata→hash名PNG→capture manifestの順で公開。既存ファイルは同内容のみ再利用し異内容を拒否。失敗時は公開済みblobが残り得るが、新capture manifestを先に公開しない。既存captureを置換しない。
- Core **241 passed / 0 failed**。metadata hash/ID、PNG bytes/camera参照、同set再保存、配列隔離、異snapshot混在/9枚/PNG寸法不一致拒否、既存PNG不一致時の新manifest未公開と既存保持を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-79ab586bc70446eb85afe093c85bb3b0`。
- 今回は合成PNGによる保存契約テストで、実撮影画像のstore接続は次。Playerビルドなし。capture reader、camera/画像hashの再読込検証、実撮影→保存のUnity fixture、GUI複数view/queue/MCPは未実装。撮影内容がsnapshotと一致する保証はcapture実装/fixtureによるもので、公開constructorは画像の出所を認証しない。
- 次はcapture readerで相対file名/metadata IDとhash/PNG hashと寸法を照合し、実画像capture fixtureの保存往復へ接続する。全体計画の残件も継続。

## モデル専用Evidence capture

- EvidenceViewに固定正投影camera/解像度/clip、EvidenceImageにsnapshot/view/PNGコピー/hash/render profileを保持。PNGはPaintPngInputで構造/寸法を検査。
- EvidenceModelCaptureを独立追加。専用scene/camera/mesh/material/RenderTextureをsnapshotから作り、camera.sceneで分離。GUI/編集markerを生成しない。既存MaterialSurfaceSetを再利用。Unity main threadで同期描画、finallyで表示停止/資源破棄/scene unloadを要求。
- 最新確認済みPlayer: `Builds/Windows-EvidenceCaptureSeam/NyaForge.exe`。log: `Logs/build-player-20260912-062838-457.log`。PASS: `Artifacts/Authoring-20260912-062902-b7c4a61ebc814c38a08cec91f80e322f/report.json`。専用512px画像のdecode、live編集後の旧snapshot画像一致、別scene同layer巨大cube非混入、固定cameraの新snapshot画像差、PNGcopy隔離と既存suite成功。
- 初回は移動量.2で退化面を作り編集拒否。次は片面だけ移動し表裏seamの裏面が輪郭を埋め画像差なし。fixtureの対応頂点0/4を.03動かして修正。失敗記録はArtifacts/Authoring-20260912-062620-eafb8aa7732140aabe9345c8cda16cb4と062728-e98d5d49bcac49e1b1d38bdc7118e231。後者のevidence-model.pngを目視しUIなしのパネルを確認。
- Core testは今回未再実行（Player全体compile/GUI suiteで検証、直近240件）。Ready meshだけ対応。空/faceless capture、複数view、材質/画像付きcapture検証、queue/取消、専用manifestとartifact公開、GUI入口、MCPは次工程。PNG構造検査はdecodeの代替ではない。

## Evidence metadata reader

- `EvidenceManifestReader` と外部変更から隔離した `EvidenceRecord` を追加。UTF8厳密/byte budget/depth12/重複property/末尾JSON、項目集合、型、ID/hash、非負数、bounds順序、target/state/final整合を検査。CopyMetadataはdeep cloneで返す。
- metadata schema1だけを許容し、captureStatusやvalidationを撮影済み/passへ変えた入力を拒否。artifactsは空のみ。値なし状態にmesh/hash/metricsを付けた不整合を拒否する。これは形状payloadのhash照合や実際のfit検証ではなく、証跡JSONの構造/整合検証。
- Core **240 passed / 0 failed**。Ready/Empty/Incomplete読込、copy隔離、不正capture/fit/schema型/未知項目/負数/bounds/mesh欠落/状態/final状態、末尾JSON/重複key/不正UTF8拒否を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-9847ebbe9dc848e3b443de50d884a321`。
- 初回編集スクリプトはPowerShellの引用符でparse失敗し、変更は未適用。修正後に実行しCore成功。今回Playerビルドなし。
- 残る読込検証：Faceless/途中Ready/複数材質/最大予算/巨大整数の組合せ。次はモデル専用captureのcamera/profile/artifact契約を実装する。既存metadataを撮影成功扱いに変更せず、別capture resultから画像hashを結ぶ。全体計画/MCP/rig等も継続。

## Evidence metadataの保存

- `EvidenceManifestCodec` はschemaVersion1/kind=nyaforge.evidence.metadataの明示JSONを出力。snapshot/document/target/final状態/計測/診断/hashを記録し、Mesh等の全payloadを暗黙serializeしない。captureStatus=not_requested、validation各項目=not_run、artifacts=[]。未実装sourceEpoch/workspaceRevision/poseはnull。
- `EvidenceStore.SaveMetadata` は指定directory内へsnapshotId.evidence.jsonを新規公開する。既存Storage.Lock/AtomicWriteを利用し、同内容の再保存はidempotent、異なる既存内容はEVIDENCE_CONFLICTで保持。既存証跡を置換しない。
- Core **238 passed / 0 failed**。同snapshotの決定的bytes、ID/hash、未取得/未検証/null表現、atomic保存と再保存、編集後の別ID保存と旧証跡保持、異内容拒否/一時file残留なし、Empty/Incompleteのgeometry非捏造と診断保持を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-cefcdb3c2e0c44118c58c7f9cc842e88`。
- 今回Playerビルドなし。metadata writerのみで、reader/schema厳密検証/画像artifact付きpublicationは未実装。撮影済みと偽装する入力は受け付けないAPIにしている。電源断/SMB中断の実機注入は未検証。
- 次はmanifest readerの型/ID/hash/状態整合と予算検査を追加し、モデル専用captureのcamera/profile/artifactを別契約で接続する。画像失敗とcommand commitは分離したまま。全体計画の残件も継続。

## Evidenceの処理段指定

- `EvidenceTarget` を独立追加。Final既定、NodeInput/NodeOutputはobject/graph/node/mesh portを明示する。対象object/graphの一致、node存在、対応mesh portを検査。値が未解決ならIncompleteでValueなし。
- `EvaluatedSnapshot.Target` と `FinalEvaluationComplete` を追加。最終評価未完了でも取得できる途中のmeshはReady/Facelessとして返すが、最終評価未完了・graph診断・stale preview revisionを別に保持。OutputNodeIdはgraphの最終node、Target.NodeIdは選択段。混同しない。
- Core **236 passed / 0 failed**。編集nodeのinput/output hash差、最終接続切断後の途中output保持、最終Incomplete、対象不一致/非対応port拒否、入力未解決、材質編集後の取得済みmaterial/画像bytes保持を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-291c856b43a447298a5c168c55a0b83a`。
- 今回Playerビルドなし。対応portは現行のmeshのみ。Image/Material単独、複数object、pose、generator version/source/workspaceの追加identity、manifest/画像capture/MCPは残件。
- 次はEvidence manifestを独立codecとして作り、snapshot/対象/metrics/diagnosticsと未検証項目を保存できるようにする。その後同じsnapshot由来のモデル画像を紐づける。既存UI検証captureとは分ける。全体計画の残件も継続。

## Evidence snapshotとmetricsのCore

- `Evidence/EvaluatedSnapshot` と `EvidenceMetrics` を分離。workspace.Gate内でcommitted final outputを取得し、instance/document/revision/stateHash/object/graph/output nodeと一意snapshot IDを保持。内容比較はOutputContentHashで別管理。command実行中の再入取得は拒否。
- Empty/Ready/Faceless/Incompleteを区別。IncompleteはValue/metricsを返さず、診断と古いpreview revisionだけを保持。古いmeshを現revisionとして扱わない。空projectにも架空のgeometryを作らない。
- metricsはrender頂点/triangle/submeshと論理頂点/faceを区別。world boundsはrender positions、faceless時だけ全loose点から取得。論理topologyなしはnull、0とは区別。assigned material数と重複なし画像hashも収集。
- Core **234 passed / 0 failed**。取得後の編集でsnapshot/metrics不変、再取得のID差/内容hash一致、Undoで内容同一かつ新revision、未完了時のstale preview隔離、空/0点/1点faceless、scale100+translationのboundsを確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b841a2ba05b542699eee0a56b0653232`。
- 今回Playerビルドなし。finalのみを取得するCoreで、node input/output指定・manifest保存・画像/専用camera・pose・capture queue・MCPはまだ実装していない。SourceEpoch/workspaceRevision等は現行native Coreに正本がないため捏造していない。fit/pose validationも未取得。
- 次は材質/画像付きsnapshotの保持と選択node outputのidentity/診断契約を検証し、Evidence manifestへ進む。Unity captureはこの不変snapshotを入力とする。全体計画の残件は維持。

## 操作案内統一とC1次工程

- `AuthoringWorkbench.ViewportHint` に案内の責務を集約。3Dpaint→切断指定→面/点選択の順で判定し、各mode更新とpanel閉じに追従。切断中の通常点選択案内を修正。
- 最新確認済みPlayer: `Builds/Windows-ViewportHint/NyaForge.exe`。log: `Logs/build-player-20260912-061104-942.log`。PASS: `Artifacts/Authoring-20260912-061132-e773f2822af64a20921e365cb17601d4/report.json`。既存suite成功。cut-visibility-hover.pngで上部が切断位置追加/Escape終了へ変わり、文字が収まることを目視確認。Core変更なし（直近231件）。
- Development-Planと設計v2 §8〜9を読み直し、C1の残るEvidence/MCPへ進む。手動受入やC1全体の完了を宣言しない。[Evidence接続計画](docs/Evidence-Integration-Plan.md) に現物のGate/Preview/WorkbenchCaptureの境界と検証順を記録。
- 次のコード変更はCoreの不変EvaluatedSnapshot取得とmetrics。empty/faceless/incomplete/staleを区別し、取得後の編集で結果が変わらないことを確認。その後専用モデルcapture/画像対応、MCPへ進む。形状/UV/Paint残件と全身制作等は継続。

## 重なった面の可視性GUI検証

- `AuthoringWorkbench.CutVisibilityVerification` を分離。正投影で2枚の面を重ね、ON時の可視辺fallback、toggle実クリックでOFF→最短の隠れた辺、各viewportクリックとhoverの一致、登録済み説明を確認。
- BVH同形状再利用、手前面の削除による再構築/奥辺選択、Undo後の遮蔽復帰を確認。候補・設定・draftでは文書hashを変更しない。
- `UpdateCamera` にClearCutPathHoverが実際には未接続だったため追加。前段の記録の意図と現物の不一致を修正し、カメラ更新時の線/候補消去を検証。
- 最新確認済みPlayer: `Builds/Windows-CutVisibilityVerified/NyaForge.exe`。log: `Logs/build-player-20260912-060856-515.log`。PASS: `Artifacts/Authoring-20260912-060922-6b79c76f3c2b496fb4a5c8013f496e7e/report.json`。既存suiteも成功。Core変更なし（直近231件）。
- `cut-visibility-hover.png` を目視。手前底辺の黄色線、中央マーカー、候補5–6/50.0%/登録済み、可視性説明の折返しを確認。上部のselection-hintは通常の点選択案内が残り、切断指定modeへの追従は次に整える。
- 残件：透視で斜め/部分遮蔽/近接面/透明材質、変換変更によるcache再構築、drag/paint競合、部分遮蔽辺の別区間探索、大規模性能。全体計画の複数object、rig/weight/morph、各形式入出力、Evidence/MCP等も継続。

## 切断候補の可視性option

- `PolygonEdgeScreenPicker` に候補world位置のpredicateを追加。拒否された候補では最短距離を更新しないため、次の可視候補を探索できる。旧呼出はpredicateなしで互換。
- `Geometry/MeshVisibility` はray origin→候補点の両面geometry判定。候補そのものの面は許容（距離許容差max(1e-6, distance*1e-5)）。材質alphaを判定しない。
- `AuthoringWorkbench.CutVisibility` に「隠れた位置を除外」（既定ON）と共通FindCutPathHitを分離。hover/clickとも同じ判定を使う。Unity viewport rayのnear-plane originから判定し正投影にも対応。MeshData参照/RestTransformが同じ間はBVHを再利用。
- Core **231 passed / 0 failed**。UVなし2枚の重なりで、最短の奥辺を除外して手前の別辺へfallback、狭い半径では候補なし、自己面許容/奥の点拒否/距離0を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-928c681d0ffe4112a7bdf08f43e607cb`。
- 最新確認済みPlayer: `Builds/Windows-CutVisibility/NyaForge.exe`。log: `Logs/build-player-20260912-060615-356.log`。PASS: `Artifacts/Authoring-20260912-060639-3d571ed0e5de4f8fa385f41085841772/report.json`。可視性ONで既存の平面クリック/hover/確定/Undo/native/Bakeとpaint含むsuite成功。
- GUI上で重なる2枚の切替比較、toggle実クリック、BVH cache更新、hover画像は次の検証。透明材質も面として扱う旨をGUI表示。判定対象は編集中Mesh。辺ごとの最近点だけを判定し、部分遮蔽辺の別区間探索は未対応。全体計画の残件も継続。

## UVに依存しない面レイ判定

- 遮蔽判定をペイントのUV必須条件へ依存させないため、`Geometry/MeshRaycast` を抽出。既存median BVH/両面/距離制限/triangle安定順を維持し、triangle/submesh/頂点indices/barycentric U,V/距離/向きを返す。UV不要。
- `SurfacePaintMesh` はgeometryを所有し、UV補間・Owner・UV continuityの責務だけを保持。RayGeometry数値helperをGeometryへ移動（Unity metaも移動）。既存INVALID_PAINT_RAY診断コードは互換維持。
- Core **230 passed / 0 failed**。新規UVなしmeshのscale1/100/微小scale、indices/barycentric/距離制限と、既存paint/BVH/seam/全suite成功。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-3be311476b604d3c9b984a321617f61b`。
- 最新確認済みPlayer: `Builds/Windows-GeometryRaycast/NyaForge.exe`。log: `Logs/build-player-20260912-060314-627.log`。PASS: `Artifacts/Authoring-20260912-060337-6a77231137b34751aee16184e213d6d1/report.json`。ペイントを含む既存GUI suite成功。
- これは遮蔽判定の共通基盤で、辺pickerの可視性optionはまだ未接続。次は候補点へのrayを使うpredicateをpicker探索へ加え、遮蔽された最短候補を飛ばして次の見える辺を選べるようにする。表示polygon/transform単位でBVHを再利用し、透明材質の扱いを明示する。hover画像確認も残件。
- 全体計画の複数object、rig/weight/morph、各形式入出力、Evidence/MCP等も継続。

## 切断候補のhover表示

- `AuthoringWorkbench.CutPathHover` を分離。切断指定modeのpointer moveで候補辺を黄色い線、透視補正後の指定位置を10px角の黄色いUIマーカーで表示。ラベルに辺ID/割合/登録済みを表示する。UIマーカーはPickingMode.Ignoreでクリックを遮らない。
- hover時は文書や選択へ書き込まない。pointer leave/down、カメラ変更、draft refresh、mode終了で候補を消去。ドラッグ中は候補判定を行わない。色/割合表示と登録済み経路のピンク線は別owner。
- 最新確認済みPlayer: `Builds/Windows-C1B-CutHover/NyaForge.exe`。log: `Logs/build-player-20260912-055938-366.log`。PASS: `Artifacts/Authoring-20260912-060001-e6b9ac7556dd4558a4261547715203df/report.json`。
- viewportの3辺へPointerMoveを送り候補edge ID/2点line/marker表示/文書と選択不変を検査。実クリック後のhover消去と画面外move時の消去も確認。既存の経路確定/Undo/native/Bakeを含めsuite成功。Core変更なし（直近229件）。hoverの画像を残す専用captureは今回未追加。
- 次はhover画像と登録済み表示/カメラ変更時消去を確認し、面による遮蔽のある辺を選ばない可視性optionへ進む。現状は全辺候補（GUI明記）、面遮蔽なし。全体計画の残件は継続。

## 切断経路の画面クリック指定

- `AuthoringWorkbench.CutPathPicking` を分離。連続切断パネルのtoggleで有効化し、viewportの短い左クリックをPolygonEdgeScreenPicker（半径12 panel px）へ送る。割合はクリック位置から計算して経路末尾へ追加。重複と上限拒否。通常の頂点/面選択を変更しない。
- 左ドラッグ回転/右ドラッグ移動は既存経路を維持。クリック時にviewをfocusしEscapeで指定モード終了（draft保持）。パネル閉じ/文書変更/編集段変更でmode解除。3Dpaint有効化は最終段へ移るため解除、切断mode有効化時はpaintをoff。
- 最新確認済みPlayer: `Builds/Windows-C1B-CutPathPickVerified/NyaForge.exe`。log: `Logs/build-player-20260912-055711-541.log`。PASS: `Artifacts/Authoring-20260912-055733-55c978e90d4a429f90ede7fca2e4af6d/report.json`。
- `AuthoringWorkbench.CutPathPickingVerification` で3辺中点へのviewport実クリック、3地点の割合/preview、選択/文書不変、Escape送信後の通常点選択復帰、パネル閉じでmode終了/再開時draft保持を確認。その後の切断確定/Undo/Redo/native/Bakeと既存suiteも成功。Core変更なし（直近229件成功）。
- 面遮蔽なしで裏側の辺も候補になることをGUIへ明記。現在hover候補線/可視面限定なし。次は候補hoverと可視性の扱いを改善し、奥行き重なり/端点/斜めの面/ドラッグ・paint競合を追加確認する。全体計画のrig/weight/morph等は未完了。

## 画面上の辺判定Core

- `PolygonEdgeScreenPicker` を独立追加。Polygonの論理辺を画面距離で検索し、EdgeCutLocation（元辺の割合）/距離/深度を返す。頂点はRestTransformで変換、near/farで線分をclipしてから投影する。
- `SurfaceCameraSnapshot.ProjectionWeight` を追加し、投影の同次座標wを使って画面上の割合を元の3D辺の割合へ補正。正投影は一定w、透視投影は異なるwに対応。距離同点は手前の深度、さらに同点なら小さい辺ID順。点に潰れた投影辺は手前の端点を選ぶ。
- Core **229 passed / 0 failed**。奥行きの違う辺の画面中点→3D割合1/3、scale100、正投影、半径外/負半径、near/farをまたぐ辺と元割合の再投影、カメラ後方の除外を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-65c2f9c62d314ff594e4ce1f6df739d2`。
- 今回Playerビルドなし。画面クリックGUIは未接続。次は深度同点/重なり/端点/平行投影の追加確認と、切断用クリックモードを通常の点/面選択やカメラ操作から分離して接続する。
- 現在のpickerは面による遮蔽判定なし（X-ray相当）、画面距離優先で探索は全辺O(E)。可視面だけの選択/裏面/大規模性能/viewport clippingは今後。全体計画の残件も継続。

## 連続切断の操作検証と入力復旧

- `AuthoringWorkbench.CutPathControlsVerification` を分離。選択した3辺を順番に追加する実クリック、順序/preview確認、重複辺と非edge選択の拒否、不正行で確定/線を無効化、末尾取消/クリアの実クリック、編集段変更でdraft消去を確認。
- 「最後の辺を取り消す」は経路全体の解析に依存していたため、不正な末尾行を取り消せなかった。生の非空行から末尾を取り除く処理へ変更。修復後のpreview復帰、2点→1点時の確定無効化を検証。
- 最新確認済みPlayer: `Builds/Windows-C1B-CutPathControls/NyaForge.exe`。log: `Logs/build-player-20260912-055147-222.log`。PASS: `Artifacts/Authoring-20260912-055212-d61349e1fe7e4df7b8cb85bc3192ac5b/report.json`。上記の操作後、連続切断確定/Undo/Redo/native/Bakeと既存suiteも成功。Core変更なし（直近227件成功）。
- 次は画面上の辺を直接指定して経路へ追加できるようにする。現状は両端頂点の選択→登録が必要。辺hit testを表示から分離し、画面距離・割合・変換と重なりを検証してからGUIモードを接続する。キーボード取消と通常の点/面選択・カメラ操作との競合を確認する。
- 再訪/閉loop/曲線、複雑なseam/凹面/端点混在のGUIは引き続き残件。全体計画の複数object、rig/weight/morph、入出力、Evidence/MCP等も未完了。

## 連続切断GUI

- `AuthoringWorkbench.CutPath` を分離。選択した辺を割合指定で末尾追加、複数行の順序付き入力、末尾取消、クリア、経路全体確定を提供。文字欄は1行に「頂点ID 頂点ID 位置%」。辺の位置は小ID→大ID、端点0/100を許容。
- 候補全体のPolygonCutPath検証に成功した場合のみピンクの折れ線と確定ボタンを有効にする。入力不正では文書を変更しない。foldout閉じで線消去、文書hash/編集段変更でdraft消去。確定は単一共通command、成功時のみ選択をクリア。
- 最新確認済みPlayer: `Builds/Windows-C1B-CutPath/NyaForge.exe`。log: `Logs/build-player-20260912-054921-238.log`。PASS: `Artifacts/Authoring-20260912-054946-3df604f130c9497ca1c0343b6ad78ae7/report.json`。`AuthoringWorkbench.CutPathVerification` で3地点preview、開閉、後半無効経路、確定の実クリック、2面→4面/6点→9点、1回Undo/Redo、native/Bakeを確認。既存suiteも成功。
- `cut-path.png` を目視し9点の配置と折返し説明、入力欄を確認。切断後の内側の辺は描画しないため、4面化の証拠はgeometry assertion。今回Core変更なし（直近227件成功）。
- 次は選択からの末尾追加、重複拒否、末尾取消/クリアの実クリック、編集段変更時のdraft消去を追加検証する。現状テストは経路文字欄を直接設定して確定する。複雑なseam/凹面/端点混在のGUI、経路の画面クリック直接指定、同面再訪/閉loop/曲線などは残件。

## 連続切断の共通command

- `PolygonCutPathOperation` を独立モジュールとして追加。2〜256個の不変EdgeCutLocationを読み取り専用配列へ複製し、順序付き経路を共通commandからPolygonEditingへ渡す。
- fingerprintには経路長・各辺の両端ID・正規化した割合を順番どおり格納する。既存operationのfingerprintは変更しない。同一command IDの同内容再送は再適用せず、位置や順序を変えた再送はCOMMAND_ID_REUSEDで拒否。
- Core **227 passed / 0 failed**。呼出元配列変更からの隔離、null/上限拒否、2面を横断する切断、後半で既存辺と重なる失敗時の文書hash/revision/頂点数保持、1回Undo/Redo、native polygon binary一致、Bake一致を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4b2dd4142810484d9717a0f3d8af1c38`。
- 初回は検証コードがDocumentRevisionをRevisionと誤記してコンパイル失敗。検証コードを修正し、全件成功を確認。
- 今回Playerビルドなし。最新確認済みPlayerは下記EdgeCutControlsで、連続切断はGUI未接続。次は順序付き辺位置の登録・削除・preview・確定/取消をGUIモジュールとして接続し、実操作と1回Undo/保存を確認する。
- 現在の経路は各元面を1回横断する範囲。閉loop/同面再訪/曲線、一般的なナイフ操作、複雑な非平面形状などは残件。全体計画のrig/weight/morph、複数object、各形式入出力、Evidence/MCP等も継続。

## 切断GUI追加検証と連続経路Core

- `AuthoringWorkbench.EdgeCutControlsVerification` を分離。選択した両端から辺A/Bへ実登録、0/100端点のpreview/確定（新頂点なし）、Undo、クリアボタン、foldout開閉、編集段変更時draft消去を確認。
- 最新確認済みPlayer: `Builds/Windows-C1B-EdgeCutControls/NyaForge.exe`。log: `Logs/build-player-20260912-053932-668.log`。PASS: `Artifacts/Authoring-20260912-053951-f9e3b76c329648c3be532b76e6f5bdc7/report.json`。既存suite成功。このbuildは下記CutPath Core追加前。
- `EdgeCutLocation` と `PolygonCutPath` を追加。2〜256箇所の順序付き辺交差位置を受け取り、各元面を1回だけ横断する。全点を候補へ挿入して隣接地点をFaceSplitで順に結ぶ。辺再訪/共通面なし/同面再訪を拒否。共有辺は1回だけ挿入し、既存の面ごとのcorner補間を再利用。
- Core **226 passed / 0 failed**。2quad横断→4quad/追加3頂点、共有点1個に両側UV seam保持、切断辺の共有、binary往復、辺再訪/飛び越し拒否、元mesh不変を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4951aecb64064a529cd302572b298301`。
- CutPathはまだ共通command/GUIへ未接続。次は経路payloadの複製/command fingerprint/replay/Undo/nativeを整える。複数回同面横断、閉じたloop、辺上再訪、非平面face、複雑な経路は未対応/未検証。単一面切断GUIは維持。
## 辺上切断GUI（前段記録）

- `AuthoringWorkbench.EdgeCut` を分離。2辺のID入力/選択2点から登録、百分率、候補検証、ピンク線、確定/クリアを提供。文書hash/編集段変更でdraftを破棄、foldout閉じで線を非表示。成功時だけ選択をクリアする。
- `BoundaryHighlightProjection` は既定の閉loopを維持し、開いた2点線も扱えるように拡張。線は候補の頂点から算出し、文書へは確定時の共通commandで反映する。
- 最新Player: `Builds/Windows-C1B-EdgeCut/NyaForge.exe`。log: `Logs/build-player-20260912-053717-140.log`。PASS: `Artifacts/Authoring-20260912-053736-0a15846425154bcf820bdb18132c55b4/report.json`。25%/75%線preview、2quad/6頂点、確定時線/選択消去、無効指定で文書/選択不変、Undo/Redo/native/Bakeと既存suite成功。`edge-cut.png`を目視し追加点と入力欄の折返しを確認。面内の線は保存後には表示しないため、切断の証拠は面数/頂点数のassertionによる。
- Core変更なし（直近225件）。選択からの辺登録ボタン/クリアの実クリックと、端点0/100、凹面/共有UV seamのGUIは追加確認対象。複数面横断と連続切断は未対応。
## 辺上の点を結ぶ切断Core（前段記録）

- `PolygonEdgeCut` / `PolygonEdgeCutOperation` を追加。異なる2辺が共有する1面を、各辺の指定位置で切る。割合は小vertex ID→大IDの0〜1、0/1なら既存端点を使い、内部ならPolygonEdgeInsertionで点/cornerを追加してPolygonFaceSplitへ接続する。
- 点追加と分割を不変の候補上で行い、1つの共通commandで確定する。途中の失敗は文書/ID履歴を変えない。既存のUV seam補間・planar/対角線検証を再利用。単一平面の切断であり、連続ナイフ/複数面横断はまだ実装していない。
- Core **225 passed / 0 failed**。quadの対辺25%/75%→2quad/追加2点、外周corner参照保持、切断辺共有、端点使用、同一辺/範囲外拒否、点追加後の無効分割で文書不変、再送/1回Undo/Redo/native/Bakeを確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-1ac5696bf6c0441c977a6b6678adad1e`。
- 今回Playerビルドなし。次はGUIで辺2つと位置を指定し、preview/確定/取消を扱う。shared seamや非平面/凹面の組合せ、大きいID/予算境界、複数面の連続切断は残件。
## 複数面dissolve GUI

- 既存FaceMerge GUIを「選択した面を1面にまとめる」に拡張し、DissolvePolygonFacesへ接続。2面以上で有効。既存2面の操作検証も継続。旧Merge core/commandは互換用に保持。
- `AuthoringWorkbench.FaceDissolveVerification` を分離。非連結2面の拒否時に文書/選択が保持されること、4面fan→1quad/中央点除去/残存面選択、Undo/Redo/native/Bakeを検証。
- 最新Player: `Builds/Windows-C1B-Dissolve/NyaForge.exe`。log: `Logs/build-player-20260912-053156-238.log`。PASS: `Artifacts/Authoring-20260912-053215-a665b305c67b4bc1b4cb882e86e4516a/report.json`。既存suiteも成功。`dissolved-faces.png`を目視し四角面/点4個と操作ボタンを確認。
- Core変更なし（直近223件）。Paint付き非線形UV、大きな選択、凹外周のGUIは残件。
## 複数面dissolveのCore（前段記録）

- `PolygonFaceDissolve` / `PolygonDissolveOperation` を追加。連続する同一平面/向き/材質の2面以上から内部辺を除去する。内部辺のUV/normal/tangent一致とmanifold/向きを検査。選択領域が接続し、外周が3〜256角の単純な1loopになる場合のみ確定。穴/分岐/離れた領域を拒否する。
- 最小face IDと残存cornerを保持し、内部で不要になった頂点を除去。別の面や元から独立している点は保持。ID履歴を維持し、候補の三角分割後に共通commandへ確定。既存2面Mergeは互換のため維持。
- Core **223 passed / 0 failed**。4triangle fan→1quad/中央点除去/外周corner参照保持/入力順不変/UV seam拒否/非連結拒否/穴付きring拒否、command無効時状態保持/Undo/Redo/native/Bakeを確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-fbce87e4252544ec9813ff68b724ac21`。
- GUI未接続、今回Playerビルドなし。非線形UVの内部補間は再三角分割で変わり得る。複雑な凹外周/大規模選択/全属性形式/Paint付き形状は未検証。
## 面なし下流の診断と復旧案内

- `AuthoringWorkbench.FacelessRecovery` に案内と「面がない編集段へ戻る」ボタンを分離。最終評価が未完了で、評価済みの面なしPolygonEditがある場合に表示。編集段リストの最初の該当段を選ぶ。文書は変更しない。面の作成またはUndoを案内する。面がない出力に材質を追加するボタンも無効化。
- Core **220 passed / 0 failed**。Mirror/Paint/材質の3経路で全面削除後のNO_RENDERABLE_FACES、編集値保持、後続node参照保持、native保存再読込、未完了Bake拒否、Undoによる復旧/Redoを確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4ae9064b4f164e979491ceb15d932dfb`。
- 最新Player: `Builds/Windows-C1B-FacelessRecoveryEdit/NyaForge.exe`。log: `Logs/build-player-20260912-052657-930.log`。PASS: `Artifacts/Authoring-20260912-052717-0b20fbda67bb4c1e9bc4cc6cb374cd15/report.json`。Mirrorの削除後に復旧ボタン実クリック/文書不変/編集可能/Undoで完了/Redo/native案内復帰と既存suiteを確認。
- 初回テストはMirror作成後の最終出力表示のままDeleteを呼び、編集contextなしで失敗。検証側でSelectEditStage(1)を明示して修正。初回記録: `Artifacts/Authoring-20260912-052618-a14af27d1abf4a01bd5da94eac013737/report.json`。
- 未確認: 保存済みPaint画像付きの新しい面へのrebind、複数の面なし編集段がある場合の選択導線、全UIの診断文言。今回Paint fixtureは既定画像生成nodeで、独自stroke画像の網羅試験ではない。
## 全ての面の削除（前段記録）

- PolygonDeletionは最後の面も削除可能。削除で不要になった頂点を除去し、元から独立していた点とID割当履歴を保持する。面なし時はrender生成を呼ばない。GUIの全選択時無効化を解除。
- 属性はcornerに属するため全削除時に消える。再作成の最初の面は既定UV/normal/tangentを生成する仕様。以前の属性はUndoで復元する。空状態に旧属性形式を持ち越す機能は実装していない。Weldによる全collapseは引き続き拒否。
- Core **217 passed / 0 failed**。独立点保持/ID履歴、再追加から面作成、最終面削除のgraph/native、面なしBake拒否、Undo/Redoを検証。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-5152d96c71ef445caa70f3445d259479`。
- 最新Player: `Builds/Windows-C1B-DeleteAll/NyaForge.exe`。log: `Logs/build-player-20260912-052210-125.log`。PASS: `Artifacts/Authoring-20260912-052228-1b324910f8b845539f5b045b213cb375/report.json`。最後の面の削除ボタン/空表示/操作有効性/native再読込/Undo/Redo、既存suite成功。検証処理は `AuthoringWorkbench.DeleteAllVerification` に分離。
- 未確認: Paint/材質付き作品の全削除後の下流診断、全属性形式での再作成、scale100/translation。次は面なし時の下流nodeとGUI操作の対応を確認し、ユーザーに回復方法を示せるようにする。
## 空PolygonのGUI統合（前段記録）

- 「空の形状から始める」を追加。面なしpreviewでもOwnedMeshProjectionは編集点を生成する。最終結果の重ね表示はMeshがある場合のみ。0点のFrameはdefaultへ戻す。GUIの未完了表示と面なし案内を分け、移動判定はPolygonの有無を使う。UV投影/厚み/Paint追加は面なし時に無効化。
- 最新Player: `Builds/Windows-C1B-EmptyPolygon/NyaForge.exe`。log: `Logs/build-player-20260912-051859-316.log`。PASS: `Artifacts/Authoring-20260912-051918-36d0f04cd13e4640b52b537f5a9cc4a2/report.json`。空開始ボタン、1/2/3点追加と各native再読込、最初の面、Undoで点のみ/Redo、面の再読込/Bakeを確認。既存suiteも成功。`first-face.png`を目視し三角面と編集欄を確認。
- Core変更なし（直近215件）。まだ全削除を許容していない。面なし状態で材質など全機能のボタン有効性/診断を網羅していない。scale100/translationや最初の面の別属性、面なしプロジェクトの旧版互換性説明は追加検証対象。
## 面なしgraph/command/native統合（前段記録）

- GraphMeshValueは面なしpolygonに限りMesh=nullを許容し、snapshotへ面なし識別とpolygon hashを含める。既存meshありのhashは維持。PolygonSource/PolygonEditで面なしを評価成功として流し、Outputでは画像接続なしの場合に通す。その他のnodeはNO_RENDERABLE_FACESで拒否する。
- PolygonEditingは点追加/移動/最初の面作成に限り面なし入力を許可。面あり候補のrender検証は維持。preview.IsCompleteは評価完了を示し、Mesh非nullとは独立。workspace.Evaluate()はこの状態でnullを返す。
- Bakeは面なし最終出力をNO_RENDERABLE_FACESで拒否。最初の面作成後は出力可能。空sourceの出力由来ハッシュはpolygon正本hashを使う（既存meshありsourceは従来通り）。初回試験でこのbaselineのnull参照を検出して修正。
- Core **215 passed / 0 failed**。空graph/点の追加ごとのnative保存再読込/移動/UV拒否時状態不変/最初の面/Undoで面なし/Redo/再読込/Bakeを確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-219820f0bc8f48c7b3c45d7b510f5942`。
- GUI/Playerはまだ面なし対応していない。今回は新規Playerビルドなし。次はOwnedMeshProjectionのmesh=null時の点生成とWorkbenchのrendering依存条件を修正し、空から始めるGUIを接続する。Mirror/材質/Paint等での診断、全削除、空初期属性の維持も残件。
## 空PolygonのCore対応（前段記録）

- PolygonMeshで0頂点/0面、1点/2点を許容し、空ID集合の上限を0にする。既存の面/参照/ID検査は保持。
- PolygonBinaryCodecは面なしだけv3（ID上限付き80byte header、属性flags0）として保存。v1/v2の非空条件と通常のwriterは保持。PolygonNewFaceは最初の面にUV/normal/tangentを生成。PolygonEditPointsは面なしにも対応。renderはNO_RENDERABLE_FACESで明示拒否。
- Core **214 passed / 0 failed**。空/履歴/1点/2点保存の往復、旧versionへの偽装拒否、truncated/flags拒否、最初の面とv1への移行を検証。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-5abea58ea32a4425ba14e022ca4db146`。
- 今回Playerビルドなし。graph/command/GUIは面なしのまま編集を継続できる段階ではない。GraphMeshValueがmesh非nullを前提とするため、次の統合を [空Polygon統合設計](docs/Empty-Polygon-Integration.md) に記録。native project schema3とpolygon blob v3を混同しない。全削除/属性形式の維持は残件。
## 点描画の集約と256点制限撤廃

- `EditPointProjection` に点表示の所有・描画・選択・破棄を分離。1点1GameObject/sphereから、2,048点を1meshにまとめた八面体マーカーへ変更。各pointは6頂点/8三角形、未選択/選択の2submesh。materialはownerから借り、mesh/rootは自身で破棄。面モードではrendererを非表示。選択不変時はindices再生成を省く。
- `OwnedMeshProjection` がこのモジュールを所有しtransactionの候補破棄/置換に追従。`PickVertex` は全編集点を探索し、最初の256点で打ち切らない。クリック探索はO(n)、選択変更は全バッチのindicesを更新するため、大規模性能の最終解ではない。
- 最新Player: `Builds/Windows-C1B-PointBatches/NyaForge.exe`。log: `Logs/build-player-20260912-050918-492.log`。PASS: `Artifacts/Authoring-20260912-050938-b223ab6f9c074ab3adfb4fbce6ee15a8/report.json`。2,053編集点/2バッチ、末尾2052番の実クリック→stable ID2053、全選択/解除、最終出力4点/1バッチ→編集2,053点/2バッチを確認。既存suiteも成功。`many-points.png`を目視し選択点と密集点描画を確認。
- Core変更なし（直近212件）。今回は通常scaleの2,053点。10万点等のCPU/GPU/メモリ計測、密集点の見やすさ、画面サイズ一定の点、近接/重なり選択、空polygonは残件。
## 追加点から面へのGUI一周

- `AuthoringWorkbench.LooseVertexWorkflowVerification` を分離。移動ボタンでX+10/Y-20mm、未接続点marker位置更新/三角形hash不変、Undo/Redoを確認。面作成GUIで4→1→5の三角面を追加し、編集対応表から未接続点が除かれることを確認。Undoで未接続点へ戻す、Redo/native再読込/Bakeまで検証。
- 初回suiteは次のUV検証で `UV wheel did not zoom`。前のfoldout変更後にlayout未確定のままScrollToしていたため、`AuthoringWorkbench.UvEditingVerification`で2frame後にScrollTo、さらに2frame待ってから操作するよう変更。製品の入力処理は変更なし。
- 最新Player: `Builds/Windows-C1B-LooseToFaceLayout/NyaForge.exe`。log: `Logs/build-player-20260912-050614-640.log`。PASS: `Artifacts/Authoring-20260912-050631-51fb3b907b66419980a9ec93b56e5c67/report.json`。`loose-to-face.png`を目視し追加点から伸びた三角面と操作欄を確認。初回失敗: `Artifacts/Authoring-20260912-050529-f3ca2f8a83024baea79d74bf975489d8/report.json`。
- Core変更なし（直近212件）。今回はscale1の小型fixture。scale100/translation、複雑なseam、256点超の描画/クリックは残件。
## 未接続頂点GUIと編集点対応

- `PolygonEditPoints` はrender頂点列をprefixとして保持し、未接続頂点をstable ID順で追加。正本/三角形mesh/UV/Bakeは変更しない。`OwnedMeshProjection` は編集段でこの点列を表示・framingし、`SelectedPolygonVertices`と移動commandは同じ対応表でstable IDへ変換。
- `AuthoringWorkbench.VertexCreation` に座標GUIを分離。モデル原点からのmm座標をscaleでmesh-localへ換算して共通commandへ渡し、成功時に追加点を選択する。
- 最初のPlayer検証でmesh hashが不変な際のColorUpdate再利用が新頂点表示を省く問題を検出。編集polygon参照も再利用条件に含め、未接続点追加/位置変更と最終出力への切替時には表示を再構築するよう修正。
- Core **212 passed / 0 failed**。既存試験へ編集点prefix不変/末尾stable ID検査を追加。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-c5f8c6a33ddc4810be616bb011b900d3`。
- 最新Player: `Builds/Windows-C1B-LooseVertexCache/NyaForge.exe`。log: `Logs/build-player-20260912-050245-504.log`。PASS: `Artifacts/Authoring-20260912-050302-191afb1243194c0d8bfbe5ff9b90cc40/report.json`。追加ボタン/編集5点/描画4頂点/クリックstable ID選択/Undo/Redo/native再読込/Bake、既存suite成功。`loose-vertex.png`を目視し独立点と座標欄を確認。初回失敗: `Artifacts/Authoring-20260912-050145-db3f6efe59ff4419bbf8c8dbd32fffbe/report.json`。
- 次の検証: 未接続点の移動→面登録→面生成のGUI一周、scale100/translation、seamを持つmeshでの対応表、final切替とcache再構築。既存の256点marker/クリック上限は残っており、大規模meshでは後方の追加点をクリックできない。点描画の集約と選択範囲拡張が必要。空polygon対応も残件。
## 面作成操作検証と新規頂点Core（前段記録）

- `AuthoringWorkbench.FaceDraftVerification` を分離。1点ずつ4→1→5の順で登録、重複追加拒否、反転2回、末尾削除、クリアを実クリックで確認。文書hash不変、foldout開閉の輪郭消去/再表示、編集段を離れた際のdraft破棄も確認。
- Player PASS: `Artifacts/Authoring-20260912-045703-f54c1311a1684d9f95da02c6a86fb97e/report.json`。build: `Builds/Windows-C1B-FaceDraftControls/NyaForge.exe`、log: `Logs/build-player-20260912-045639-794.log`。既存suiteも成功。このビルドは次項の新頂点Core追加より前。
- `PolygonVertexCreation.Add` / `PolygonAddVertexOperation` を分離。mesh-local位置に未接続頂点を追加し、割当履歴より大きいIDを使用。面と属性は保持。有限値/頂点予算/ID枯渇を検査し、共通commandのUndo/nativeへ接続。
- Core **212 passed / 0 failed**。面保持/render hash不変、追加頂点での面作成、面削除後のID再使用防止、非有限値拒否、command replay/Undo/Redo/nativeの未接続頂点保持、Bakeを確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-2c8be12fb01141c9be5ab61cf71de358`。
- 現状 `PolygonRenderAdapter` は面corner由来の頂点のみ出力するため、未接続頂点はBake/preview meshに出ない。正本には保存される。次はこれを表示/選択する編集ケージ側の機能と追加GUIを実装する。製品で使える頂点追加GUIが完成したとは扱わない。空polygonの初期面作成は引き続き残件。
## 順序付き面作成: Core/command/GUI実装済み

- `PolygonFaceCreation.Create` / `PolygonCreateFaceOperation` を追加。既存頂点3〜256個の指定順を周囲と面の向きとして保持。共有辺の向き/過剰共有、重複面、未知/重複頂点を検査する。新face/cornerは割当履歴より上、既存面/頂点は保持。材質slot指定、`PolygonNewFace`による新面UV/normal/tangent生成、候補三角分割を経て共通commandから確定。
- Core **210 passed / 0 failed**。既存辺への三角面追加、指定順/材質/ID/既存面保持、凹五角形、逆向き、自己交差拒否、command拒否時の文書保持/replay/Undo/Redo/native/Bakeを検証。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4708b8e90e2540d4b974589e104439fa`。
- GUI: `AuthoringWorkbench.FaceCreation`。1頂点ずつ末尾登録、カンマ区切り順序入力、反転/末尾削除/クリア、材質slot、妥当性メッセージ、緑の輪郭。文書確定は共通command。文書hash/graph/node変更時に登録クリア。輪郭資源はownerが破棄。輪郭は閉じた線のみで、塗りつぶし/向き矢印なし。
- Player: `Builds/Windows-C1B-FaceCreation/NyaForge.exe`。log: `Logs/build-player-20260912-045426-748.log`。PASS: `Artifacts/Authoring-20260912-045451-9fe3ba5208d94587912fb26dfdc4a96d/report.json`。入力順に対応した輪郭3点/文書不変、向き不一致の無効化、面追加/登録クリア、Undo/Redo/native/Bakeと既存suiteを確認。`created-face.png`を目視しUIの文字/折返しを確認。正面画像は追加面が側面のため面数の証拠はassertionによる。Core変更なし（直近210件）。
- 次の確認: 1頂点ずつの追加/重複拒否/反転/末尾削除/クリアを実クリックで確認、編集中の文書/段変更によるdraft破棄、複数行/長い入力のGUI、複雑な面でのpreview負荷。新規頂点の配置と空polygonへの最初の面は未実装。
- 制約: 頂点接触だけの非manifoldや全体交差を網羅検査しない。新面UVの重なり解消/Paint再投影なし。面積が相殺される自己交差輪郭は共有法線計算で `INVALID_EXTRUSION` となる場合があり、拒否はできるがエラー説明の改善が必要。初回テストはこのコード差で1件失敗し、非ゼロ面積の交差fixtureで三角分割の交差検出を別途確認した。
- 以下のWeld/BoundaryBridgeの確認済み機能を保持。
## 頂点weld: CoreとGUI実装済み

- `PolygonWeld.AtCenter` と `PolygonWeldOperation` を追加。選択頂点の平均位置へまとめ、最小vertex IDを残す。計算順序はID順に固定。連続して潰れるcornerは最小corner IDの属性を採用し、面ごとのUV/normal/tangentを保持。3角未満になった面は除去する。ID割当履歴と無関係な未使用頂点は保持。
- 非連続の同一頂点再訪、重複面、辺の過剰共有、分岐境界、切れたface fan、接続方向不整合を拒否。候補全体の三角分割後に共通commandから確定する。全削除は現行polygon制約により拒否。
- Core **207 passed / 0 failed**。quad→triangle/中心位置/入力順序/ID、潰れたtriangle除去、閉じたboxとcorner属性、未使用頂点保持、不正入力時の文書保持、command replay、Undo/Redo/native/Bakeを確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b5c8786a24e0475fa3428e7aab3eb725`。
- GUI: `AuthoringWorkbench.Weld` に操作ボタンと残存IDのrender alias再選択を分離。点モード2頂点以上で有効。拒否時は文書/選択を保持し、成功時は残存頂点を選択（除去された場合は空選択）。Core変更なし、直近207件を引き継ぐ。
- 最新Player: `Builds/Windows-C1B-Weld/NyaForge.exe`。build log: `Logs/build-player-20260912-044825-652.log`。PASS: `Artifacts/Authoring-20260912-044849-91040a2fd3224764909e3d7a4d37cb55/report.json`。quad→triangle、残存頂点選択、不正weldの文書/選択保持、Undo/Redo/native/Bakeと既存suiteを確認。`weld.png`を目視し三角形とボタン文字を確認。保存再読込後の画像なので、選択保持の証拠は直後のGUI assertionによる。
- 未実装/未検証: 距離による自動weld、最後に選択した位置への統合、全体自己交差、Paint再投影、複雑なseamのGUI選択/大規模選択/OS/DPI手動受入。以下は前段BoundaryBridgeの検証記録。
## 今回の変更と証拠

- `PolygonBridge`（形状）/`PolygonBridgeOperation`（command）/`AuthoringWorkbench.Bridge`（GUI）を分離。等頂点数3〜256の独立した2境界を四角面で接続する。自動接続位置は距離最小の循環対応。境界方向、ID割当履歴、重複面、辺入射数、三角分割を検査する。
- `PolygonNewFace` にcap/bridge共通の新面属性生成を分離。既存面を保持し、新面は最初の境界の隣接材質を継承。UVは新面ごとの投影で、島の重複を解消しない。
- GUIは最初の境界を黄色、相手を水色で表示。接続ずれを指定可能。同じ境界の選択や閉じた形ではボタンを無効化。表示資源は所有者が破棄する。
- Core **203 passed / 0 failed**。閉じた形の向き、既存属性/ID、無効指定、command replay、Undo/Redo/native/Bakeを確認。結果: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-76672dba180a4549b71287523a0f04d3`。
- 最新Player: `Builds/Windows-C1B-BoundaryBridge/NyaForge.exe`。build log: `Logs/build-player-20260912-044039-672.log`。PASS: `Artifacts/Authoring-20260912-044102-fea66ef0ba674544ab758a7b102616d3/report.json`。GUI2境界表示/同境界拒否/6面12三角形/境界消去/Undo/Redo/native/Bakeと既存suite成功。
- `bridged-boundaries.png` を目視して操作欄の文字と閉じた結果の表示を確認。正面画像なので奥行きはこの画像単独の証拠ではない。閉じた形は境界数0/面数/三角形数/Core方向検査で確認。ビルド後に検証コードのエラー文と試験用出力フォルダ名のみを修正（製品コード変更なし）。
- 未対応/未検証: 不等頂点数bridge、全体自己交差、UV packing、複雑な非平面境界、Paint付き作品、OS/DPI実操作。全体の完成ではない。
- 以前の詳細は [前段履歴](docs/history/2026-09-12-C1B-Before-Boundary-Bridge.md)。頂点追加・面分割/結合・境界cap・UV・Paint・材質・出力復旧の成果と各残件を保持。Unity receiverの最新証拠は同履歴参照（今回再実行なし）。
## 次の作業

1. 次はPolygonCutPathを共通commandへ接続し、経路payload/fingerprint/失敗時原子性/Undo/replay/nativeを検証する。その後GUIの経路登録へ進む。連続した複数面の切断も引き続き目標として保持する。保存済みPaint画像の全削除後rebindと面なし複数編集段の導線も残件として保持する。点描画の大規模性能/密集時の選択も残件。render indexとstable IDの境界、既存Paint/UV/出力の意味を保護する。選択順/向き/既存辺の共有/UVとIDの扱いを明確にしてCoreから共通command/GUIへ接続する。既存stable ID/属性/共通command/Undo/native/GUIの境界を維持する。不等頂点数bridgeも残件。自由面作成・vertex weld/多面merge・連続knifeも維持。複数島/拡縮後のドラッグ、Paint付き作品の動作とUV回転/拡縮ハンドルも維持する。merge/bridge/cut、空polygonと削除後のID割当は残件。scene override、GUI実操作、staging登録前中断/未確認新規GUIDなども保持。復旧だけの類似テストを増やし続けない。複数材質の同UUID共有receiver fixture、部位別GPU定量/alpha、mask/重なり遮蔽は検証残件として保持。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1B-EdgeCutControls -Width 1280 -Height 800 -TimeoutSeconds 600
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `Assets/NyaForge/Rendering/`: Playerとreceiverが共有する材質adapter/shader。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/create/curves、全削除・空polygon、UV回転/拡縮handle・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。

## レビュー追従の回帰証拠 2026-09-13

- MCP sidecarはPNGの`data`だけをstructured/textから除去し、画像ごとのcamera・解像度・hash・撮影条件を保持する。`Tests/Mcp.Transport/CaptureResultVerification.cs`でbytes非重複とcamera保持を検証し、transport suite 3項目PASS。
- PhysBonesは削除curve初期化とstable target/root/name marker再利用を実装済み。Unity Bridge 2022.3.22f1の最新検証はPASS。
- 実VRChat SDK内の受入と揺れリセットキャッシュは未完了。secondary-motion再bindのUndoはCore回帰で確認済み。

## secondary-motion再bind Undo 実素材検証 2026-09-13

- Core **426 passed / 0 failed**。metadata rebindをUndo/Redoすると、Document geometryを変えずにattachment bytesと再読込可能なmetadataを元へ戻し、Redoでrebound bytesへ戻す。
- Windows Player **PASS / 74 checks**（`Builds/RebindUndo/NyaForge.exe`, `Logs/build-all-20260913-014245-743.log`, `Artifacts/Authoring-20260913-014317-08b047e74a554ba88b3f760ba11a05e1/report.json`）。実RadDollV3 VRMでstale skeleton→same-BoneId rebind→Undo/Redoのattachment復元を確認した。
- Unity Bridge **PASS**（Unity 2022.3.22f1, `Artifacts/BridgeReceiver-20260913-014429-987-190b810cf80a499cba2ed3c8ca51a256/bridge-report.json`）。実VRChat SDK内の動作受入と揺れリセットcacheは未完了。

## 揺れリセット表示cache検証 2026-09-13

- `ClearSpringPlayback`でsource-skin投影cache keyを無効化し、transient spring meshから通常graph表示へ戻すとき同じevaluation hashでも再投影するよう修正した。
- Windows Player **PASS / 74 checks**（`Builds/ResetCache/NyaForge.exe`, `Logs/build-all-20260913-014541-107.log`, `Artifacts/Authoring-20260913-014601-ad53de01f4a34333926580298d3f1c36/report.json`）。VRM1/VRM0で再生→reset後のsource-skin表示がbaselineへ戻る回帰を確認した。
- Unity Bridge **PASS**（Unity 2022.3.22f1, `Artifacts/BridgeReceiver-20260913-014712-999-fcdbade261d64afe917d7e3c338f925a/bridge-report.json`）。

## 一般TRS / inverse-bind取込の拡張 2026-09-13

- `GlbSkinImporter`のskinned GLB入口をtranslation-onlyからsource affine対応へ更新した。`GlbNodeTransformReader`のnode TRS/matrixをそのまま階層へ接続し、inverse-bindは`GlbMatrixAccessorReader`で一般4x4 affineとして読み、逆行列の原点からportable skeletonのHeadを求める。source skin側のNYFS/NYSPは従来どおりlocal/world、bind、normal/tangent変換に使う。
- IBM accessorの有効な`byteStride`とstorage `target`を拒否せず、MAT4 element幅64 bytes、4-byte alignment、範囲、strideを検証する。stride 64の標準出力と不正stride 60を回帰した。
- GUIの説明、Import README、Model Interchange、Source Affine、node hierarchyの記述を現行能力へ更新した。FBX直接取込、複数mesh結合、未知拡張・材質・animationの完全保持、標準VRM出力、実VRChat受入は未完了。
- Core **426 passed / 0 failed** (`dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore`、artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0a39624ef96840098816b23f58061815`)。追加回帰は一般node TRSを保持したskinned取込、source matrix accessorのstride互換性と不正stride拒否。実RadDollV3の再取込は未実施で、下記のPlayer/Bridge回帰のみ実施した。
- Windows Player / Bridge再回帰: Player build `Logs/build-player-20260913-015653-885.log` (`Builds/GeneralTrs/NyaForge.exe`)、Authoring **PASS / 74 checks** (`Artifacts/Authoring-20260913-015713-d25dd1013a9548e5adfe804ffba7a74b/report.json`)、Bridge **PASS** (`Artifacts/BridgeReceiver-20260913-015746-828-dbeb9bd2b7fb4c7dad00c00267f5792d/bridge-report.json`)。一般TRS対応後も既存GUI・保存・Bake・Bridge回帰は通過した。invalid fixtureは有効なnon-unit scaleを拒否テストに使わないよう、特異scale 0へ更新した。
- 実RadDollV3再回帰: private temp `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`（45,341,584 bytes）を`-ImportModel`で再取込し、Player **PASS / 74 checks** (`Artifacts/Authoring-20260913-015937-4c3dc3c8c3114d2585dc4d5d3e11cc8c/report.json`)。mesh 1 / skin 1、129,348 vertices、171 bones、35 morphsの候補確認、rest-space頂点編集、native Save/Open、SkinnedGeometry GLB出力と再読込を確認した。Unity Bridge **PASS** (`Artifacts/BridgeReceiver-20260913-020039-628-8b0e715c5a4a4fa48331586332bb0460/bridge-report.json`)。素材はpublic repoへ同梱していない。

## 静的node instanceのaffine適用 2026-09-13

- `GlbImporter.Read(bytes, meshIndex, instanceWorld)`を追加し、選択したGLB node instanceのworld affineを静的meshのpositions、normal、tangent、POSITION morphへ`SourceMeshTransform`で適用する。Workbenchのnode instance選択から静的小物を取り込むと、元sceneの配置・回転・拡縮を失わず編集対象へ入る。source resource選択（instanceなし）は従来どおりidentityで読む。
- Core **427 passed / 0 failed** (`dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore`、artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-c77095bc73ec4ed39dc77977e3ba7ba6`)。回転＋非一様scale＋translationがgeometryとmorphへ同じaffineで適用されること、source meshのidentityと警告を確認した。
- skinned node instanceのmesh-node affine、mesh結合、shared mesh/skin参照、material/animation/未知拡張保持は引き続きI04-B/Eの残件。標準VRM出力と実VRChat受入も未完了。
- 静的node affine変更後のPlayer確認: `Builds/InstanceAffine/NyaForge.exe`、log `Logs/build-player-20260913-020315-915.log`。Authoring **PASS / 74 checks** (`Artifacts/Authoring-20260913-020337-727ddfb457c540d18788a1b6c47267c8/report.json`)、Bridge **PASS** (`Artifacts/BridgeReceiver-20260913-020409-148-0cad4b3e3ab54e23839d8e70ed3d432d/bridge-report.json`)。さらにRadDollV3 VRMを同buildで再取込し、Player **PASS / 74 checks** (`Artifacts/Authoring-20260913-020423-8731649659a7441d942ac79d343de2f3/report.json`)、Bridge **PASS** (`Artifacts/BridgeReceiver-20260913-020528-915-0be4ce394f9f435dbc5fc5c95c1be047/bridge-report.json`)。

## スキン付きnode instance affineの保存・表示・出力（2026-09-13）

- `GlbSkinImporter.Read(bytes, meshIndex, skinIndex, instanceWorldTransform)` を追加し、選択したskinned nodeのworld affineをsource skinとは分離して `ImportedSkinnedMeshSource.InstanceWorldTransform` へ保持する。Workbenchのnode instance選択から渡し、頂点編集はsource-local空間のまま扱う。
- `ImportedRigSession` と codecをv6へ拡張し、`meshInstanceTransform` を16要素のcolumn-major affineとしてnative Save/Openで往復する。旧v1〜v5は従来どおり読める。
- `SourceSkinGraphAdapter` はsource paletteで変形した最終graph outputへinstance affineを最後に適用する。これにより下流EditMeshを維持した表示と、node transformを含む表示座標が一致する。
- 標準SkinnedGeometry GLB出力はmesh nodeの `matrix` としてaffineを保持する。Core **428 passed / 0 failed**（`dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore`、artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f32e8173dfcc4dada13ab9bacfd28e7d`）。v6 codec、表示投影、matrix出力を合成fixtureで確認した。
- 残る境界は実RadDollV3でのnode instance選択を含むPlayer GUI受入、複数SkinDeform/共有参照、VRChat実機でのmatrix解釈確認。通常のVRChat受入完了とは扱わない。

## skinned instance v6 Player / Bridge検証（2026-09-13）

- Windows Player build **PASS**: `Logs/build-player-20260913-021725-624.log`, `Builds/SkinnedInstanceAffineV6/NyaForge.exe`。
- 合成Authoring suite **PASS / 74 checks**: `Artifacts/Authoring-20260913-021746-1f7f7e88b7964f6ebbae701adf735ec0/report.json`。
- private RadDollV3 VRMを使ったAuthoring suite **PASS / 74 checks**: `Artifacts/Authoring-20260913-021825-ea371b80cb854b9ba8d31247e4e30471/report.json`。実素材の候補取込、EditMesh、native Save/Open、標準skinned GLB出力・再取込を確認した。素材はpublic repositoryへ入れていない。
- Unity Bridge **PASS**（Unity 2022.3.22f1）: `Artifacts/BridgeReceiver-20260913-021930-911-a5659f6dc6c24f98bbd8fe74410feac1/bridge-report.json`。
- これはPlayer自動検証とprivate実素材smokeの証拠であり、実マウスで特定node instanceを選び、matrixを含む出力をVRChat内で受入した証拠ではない。SIM-02B/SIM-07Aと実操作受入は継続する。

## skinned instance status表示（2026-09-13）

Workbenchの取込骨対応statusに「node affine保存済み」を表示し、選択nodeの配置情報がsessionへ保持されていることをGUIで確認できるようにした。Windows Player build **PASS**: `Logs/build-player-20260913-022259-574.log`, `Builds/SkinnedInstanceAffineV6Ui/NyaForge.exe`。

## normal/tangent morphの実素材境界（2026-09-13）

private RadDollV3 VRMにNORMAL/TANGENT morphが含まれることを確認した。現行MorphSetはPOSITION deltaだけを表現するため、取込は継続しつつ未対応属性をwarningへ明記する。実モデルを拒否する変更は採用しない。専用attribute morph schemaでは、normal/tangent deltaの変換・保存・表示・出力を同じsource hashで往復できることを完了条件にする。Core **429 passed / 0 failed**（`dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore`、artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f32e8173dfcc4dada13ab9bacfd28e7d`）。

## normal/tangent morph v2 接続（2026-09-13）

`MorphTarget`／`MorphSet`へ疎なNORMAL/TANGENT差分を追加し、`NYRM v2`で保存した。旧`NYRM v1`は属性差分なしとして読み続ける。`MorphDeformer`はweight適用時に法線を再正規化し、接線のXYZ差分とhandednessを保持する。`SourceMeshTransform`はnormal差分を逆転置の非正規化ベクトル、tangent差分を線形ベクトルとして変換する。

GLBのprimitive target `NORMAL`／`TANGENT`を読み、対応するbase属性がある場合はnative morphへ保持する。base属性が無い既存素材はPOSITIONを保持したまま明示warningを出す。標準GLB出力も保持済み差分をtarget accessorへ書く。回帰として属性差分のcodec/deformer、属性欠落warning、既存POSITION-only GLB、選択node affine、実RadDollV3 VRM取込を確認した。

- Core: **430 passed / 0 failed** (`Logs/core-morph-v2.txt`, `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-6dacbb61170542f089fc127ae2a66790`)
- Windows Player: **PASS** (`Logs/build-player-20260913-023614-206.log`, `Builds/MorphV2/NyaForge.exe`)
- 実RadDollV3 VRM Authoring smoke: **PASS** (`Artifacts/Authoring-20260913-023708-6afa00ae146341d59023d66281e62b43/report.json`, Unity 6000.4.3f1)。これは実マウス操作・実VRChat内受入・private素材公開を意味しない。
- 残り: normal/tangent差分を含む実GLBの再出力→再取込で数値一致を追加確認し、corner domain／複数mesh共有参照と標準VRM出力へ広げる。

## I04-C source rig容量拡張（2026-09-13）

実素材調査で確認した単一mesh 262 morph、257骨、18 influenceを削減せず扱えるよう、native rigの予算を拡張した。`SkeletonDefinition.MaxBones`は512、`SkinBinding.MaxInfluencesPerVertex`は32、`MorphSet.MaxTargets`は512。`GlbSkinImporter`は連続する全JOINTS_n/WEIGHTS_n setを読み、32 influenceまで`SkinBinding`へ渡す。標準`SkinnedGeometry` GLB出力は受取先互換のため4 influence制限を維持し、超過時は明示的に拒否する。

Coreの容量回帰で257骨・18 influence・262 morphのnative codec往復を確認した。実RadDollV3で使った既存Windows smokeとは別に、次は257骨／複数weight setを含む実GLB受入を追加する。PhysBones／secondary-motionの1024骨予算とは別の、通常rig authoring予算である。

- Core: **432 passed / 0 failed** (`Logs/core-capacity.txt`, `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-2000423f2af54ce4ad3f2b3ab2c2ed68`)
- 残り: 複数mesh共有参照、corner domain morph、4 influenceを超える標準GLB出力の別profile、標準VRM出力、実VRChat受入。

- 追加回帰: `GlbSkinImporter`の合成fixtureでJOINTS_0/WEIGHTS_0とJOINTS_1/WEIGHTS_1を同一vertexへ統合し、重複なしの2 influenceとしてnative bindingへ公開できることを確認した。
- Windows Player容量拡張build **PASS** (`Logs/build-player-20260913-024616-295.log`, `Builds/RigCapacityV1/NyaForge.exe`)、RadDollV3 VRM Authoring smoke **PASS** (`Artifacts/Authoring-20260913-024636-5173918167f34b6cb6ffab85dd973313/report.json`, Unity 6000.4.3f1)。実素材での通常取込回帰は確認したが、257骨／18 influenceを含む別実GLBの実ファイル受入とVRChat内確認は未完了。
- `SkinnedGeometryExtended`の8 influence writer→GLB再取込回帰を追加。Core **434 passed / 0 failed** (`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-34e4f81e4e8f4c6597dd61acdf96bb65`)。Windows Player build **PASS** (`Logs/build-player-20260913-025706-941.log`, `Builds/ExtendedGlbV1/NyaForge.exe`)、Authoring suite **PASS** (`Artifacts/Authoring-20260913-025733-6d8dccdbe10949f8baa277d967cdec8e/report.json`)。実Unity/VRChat受取先での拡張profile受入は未確認。
- Source skin display now accepts multiple `SkinDeform` branches when they share skeleton and pose. Core回帰・Windows Player build **PASS** (`Logs/build-player-20260913-030252-754.log`, `Builds/MultiSkinV1/NyaForge.exe`)、Authoring suite **PASS** (`Artifacts/Authoring-20260913-030314-fddca601322a40ffb4bf2644ed0087ad/report.json`)。異なるmesh／pose／shared instanceの一般化は未完了。

## GLB取込診断（2026-09-13）

- `GlbImportDiagnostic` と `Diagnostics` を静的／skinned import結果へ追加。選択meshに材質・画像リソースがある場合は `MATERIALS_NOT_RETAINED`、animationは `ANIMATIONS_NOT_RETAINED`、`extensionsRequired` は `REQUIRED_EXTENSIONS_NOT_RETAINED`（blocking）、`extensionsUsed` は `EXTENSIONS_PARTIAL`（partial）として、path・理由とともに保持する。既存Warningsにも同じコードを出す。
- Core **436 passed / 0 failed**。自作GLBへ4種類を同居させ、Diagnosticsのblocking/partial分類と警告表示を確認した。Windows Player **PASS** (`Logs/build-player-20260913-031410-506.log`, `Builds/GlbDiagnosticsV2/NyaForge.exe`)、Authoring suite **PASS** (`Artifacts/Authoring-20260913-031430-b65d18fdb5a54ace9f04e7d21a903284/report.json`)。
- これは「完全取込成功」と表示しないための診断契約であり、材質・animation・未知拡張を依存資源ごとnativeへ保存する実装、GUI/MCPの詳細report、required拡張の網羅的拒否は未完了。

## 複数object inspection（2026-09-13）

- `AuthoringGraphReader`のgraph inspectionへ`activeObjectId`と`objects`一覧を追加。active objectは従来どおり詳細graphを返し、全objectについてgraphId、nodeCount、評価完了／stale、output要約、diagnosticsを返す。非active graphも純粋評価で確認するため、MCPから対象を選ぶ前に全体状態を読める。
- Core **437 passed / 0 failed** (`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-6bff3098e94c4eb2a0560bd6617b678e`)、Windows Player **PASS** (`Logs/build-player-20260913-031801-663.log`, `Builds/GraphInspectV1/NyaForge.exe`)、Authoring suite **PASS** (`Artifacts/Authoring-20260913-031821-7095e42ce7554e4587deb67f44e0ba35/report.json`)。
- objectごとの頂点ページ、編集context、材質／paint操作は引き続きactive objectを明示選択してから行う。

## GLB診断のnative保存（2026-09-13）

- `import-diagnostics.nyaforge.json` attachment（schema 4）を追加し、graphIdごとにsourceHash、mesh／skin locator、blocking／partialの`GlbImportDiagnostic`を保存する。既存attachmentと同じblob hash・writer lock・失敗時旧snapshot保持を使い、元GLBを再読込せず診断を復元する。
- `forge_graph_inspect`のactive graph詳細と全object summaryへ`importDiagnostics`を追加。Core **441 passed / 0 failed** (`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-95fd651be1094560b029f04b72aa1d7c`)、snapshot／inspection往復と旧GLB diagnostics sidecarの保存時schema 4移行を回帰した。
- Windows Player **PASS** (`Logs/build-player-20260913-032941-070.log`, `Builds/ImportDiagnosticsV2/NyaForge.exe`)、Authoring suite **PASS** (`Artifacts/Authoring-20260913-032959-ca8fa373b3ef4e56b32c0f6660fe4dcf/report.json`)。取込後status表示、Graph inspection一覧、診断attachment復元を含むPlayerで確認した。

## GLB診断のGUI詳細表示（2026-09-13）

- `取込診断（保存済み）` foldoutをGLB取込パネルへ追加。保存済みrecord数、保持不可／一部保持の件数、active graphを表示し、各graphのsource hash・mesh／skin locator・診断のseverity／code／path／messageを確認できる。attachmentが壊れていても作品を置き換えず、読込エラーを同じ領域へ表示する。
- Windows Player **PASS** (`Logs/build-player-20260913-033512-860.log`, `Builds/ImportDiagnosticsUiV2/NyaForge.exe`)、Authoring suite **PASS** (`Artifacts/Authoring-20260913-033534-e6ed3ced2eb04b8896208cde5f0f05ab/report.json`)。合成した保存済みdiagnosticsをGUIへ注入し、summary・active locator・詳細labelの表示を自動検証した。Core **441 passed / 0 failed** (`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-95fd651be1094560b029f04b72aa1d7c`)。旧GLB diagnostics sidecarの保存時schema 4移行も回帰した。
- GUIは保持結果の確認導線であり、材質・animation・未知拡張を依存資源ごとnativeへ完全保存する機能や、実モデルを使った目視受入を含まない。
## 2026-09-13 same-skeleton multi-mesh skinned GLB output

標準 `SkinnedGeometry`／`SkinnedGeometryExtended` GLB出力を、同じskeleton hashを共有する複数graph objectへ拡張した。各objectの評価済みmesh、頂点編集、binding、材質／slotを個別primitiveとして保持し、writerは一つの共有skinとmesh/nodeごとのskin参照を出力する。異なるskeletonを混ぜる場合は `GLB_SKIN_SHARED_SKELETON`、複数objectへinstance affineを同時指定する場合は `GLB_SKIN_MULTI_INSTANCE_TRANSFORM` で明示拒否し、単一objectの既存契約は維持する。

Core **459 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f8a072778bec466e98e7f0f98a3790d2`）。合成2mesh fixtureでGLBのmesh 2個・shared skin 1個、mesh node双方の同一skin index、各meshの再読込を回帰し、異なるskeletonの混在は `GLB_SKIN_SHARED_SKELETON` で出力先を作らず拒否することも確認した。Windows Player `Builds/MultiSkinnedV1/NyaForge.exe` の800x600 Authoring suite **PASS**（report `Artifacts/Authoring-20260913-065647-9698b768b4444eaaa9fda875f9e1934a/report.json`）。Unity Bridge **PASS**（Unity 2022.3.22f1、`Artifacts/BridgeReceiver-20260913-065719-599-977c683e22af42a3bdf635526c1c7557/bridge-report.json`）。異なるskeletonの自動統合、共有mesh／morph参照の完全保持、実VRChat内の見た目と実マウス／DPI受入は別境界として残る。
## 2026-09-13 per-object node affine in multi-mesh GLB output

複数graph objectをshared-skin GLBへ出力する際、`ImportedRigSession.MeshInstanceTransform`をgraph object IDごとに収集し、mesh nodeごとのcolumn-major `matrix`として保持する経路を追加した。GUIとMCPは`ExportSkinnedWithTransforms`／`ExportSkinnedExtendedWithTransforms`を使い、単一objectの既存API互換、未知object IDの拒否、異なるskeletonの拒否を維持する。これにより、同一skeletonを共有する複数meshを元node配置込みで一括出力できる。

Core **459 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-19be84b2f5824a7d96c40b8019560659`）。合成2mesh fixtureでobjectごとに異なるtranslationを与え、両mesh nodeのmatrix保持と再読込を回帰した。Windows Player `Builds/PerObjectAffineV1/NyaForge.exe` の800x600 Authoring suite **PASS**（report `Artifacts/Authoring-20260913-070744-4761096855254f5780f9773c388a29fd/report.json`）。Unity Bridge **PASS**（Unity 2022.3.22f1、`Artifacts/BridgeReceiver-20260913-070816-667-d581cf14d9734e499dae852fe97d49f7/bridge-report.json`）。実VRChat内の配置・見た目、異なるskeleton結合、共有mesh／morph参照、実マウス／DPI受入は別境界として残る。
## 2026-09-13 PerObjectAffineV1 実RadDollV3再回帰

`Builds/PerObjectAffineV1/NyaForge.exe`でprivate一時素材 `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm` を読み取り専用に使い、GLB/VRM候補確認、skin取込、rest-space頂点編集、native Save/Open、標準skin GLB出力を再回帰した。最新Player Authoring suite **PASS**（800x600、`Artifacts/Authoring-20260913-071034-98196e26e7af47cfa3df95cd3eab7bbd/report.json`）。同じ出力のUnity **2022.3.22f1 BridgeもPASS**（`Artifacts/BridgeReceiver-20260913-071148-485-e6720b8b0f8d49858a5198eafa261b7b/bridge-report.json`）。private素材はpublic repositoryへ追加していない。

これは実素材の取込・編集・保存・GLB出力smokeであり、実マウス/DPI差、実VRChat内の見た目・挙動、標準VRM出力、異なるskeleton結合と完全材質保持の証拠ではない。
# 2026-09-13 graph-keyed VRM sessions / display-hit consistency follow-up

レビュー指摘のうち、複数graphをまたぐ保存再開で表情・Springが別素材へ混ざる経路を閉じた。`Expressions` と `Springs` はGraphIdをキーにした bounded table (`NVXE`/`NVXS`) として保存し、旧単一blobはrigまたは単独graphへ移行して読める。Open時は全rig/sessionのsource hashを照合し、active object切替では対応graphのsessionだけを表示する。

source skin表示はSkinDeform位置のoverride後にgraphを再評価し、後段EditMeshを保持する。標準GLB出力にはWorkBenchのimported source skinからinverse-bind行列をgraph object単位で渡せる経路を追加した（従来APIのidentity fallbackと明示instance affine互換は維持）。装着小物の頂点hit testはpreview rootのworld座標を使い、描画位置とクリック判定を一致させた。

Core **460 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-57272bc5a1d74d37aed5a0f21cfab31f`）。Windows Player `Builds/ConsistencyFollowupV1/NyaForge.exe` のAuthoring suite（private一時RadDollV3 import指定、report `Artifacts/Authoring-20260913-074524-bcf817fd606e477eae95415f5f1d7032/report.json`）とUnity 2022.3.22f1 Bridge（`Artifacts/BridgeReceiver-20260913-074633-351-fcfd3de0c8c146328d42e82de7541f1b/bridge-report.json`）はPASS。実VRChat SDK、実マウス/DPI差、任意モデルの完全なnode transform互換は別受入境界として残す。

## 2026-09-13 all-mesh-instance import workflow

実アバターをbody・hair・衣装などの複数meshへ分けて編集できるよう、WorkbenchのGLB/VRM取込パネルへ「このファイルの全mesh instanceを取り込む」を追加した。各node instanceのmesh／skin対応とworld affineを保ったまま、static／skinned graphを候補ごとに検査し、一つのcommand batchで既存graph projectへ原子的に追加する。途中失敗、object上限64件超過、未対応入力では文書・metadata・Undoを変更しない。従来の候補を一つずつ取り込む導線は維持した。

Core **469 passed / 0 failed**（`dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore`、artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f9109c8d3e9d491598ae0a1716b16940`）。Windows Player `Builds/AllMeshImportV1/NyaForge.exe` のAuthoring suiteは **PASS / 82 checks**（private一時RadDollV3 VRM smoke、合成multi-mesh取込のnative Save/Openとgraph-keyed rig session保持を含む、`Artifacts/Authoring-20260913-125940-84b3ec59c2f44150914ea23da3e7f4ec/report.json`）。同成果物のUnity **2022.3.22f1 Bridge**も **PASS**（`Artifacts/BridgeReceiver-20260913-130102-541-67238901e17f4faa92fe130cdae8b146/bridge-report.json`）。GUIナビゲーションも **PASS**（`Artifacts/Navigation-20260913-130116-c93b7acb2d5e41e7abd85b9d50814441/report.json`）。

この回帰は合成fixtureとprivate実モデルの取込・保存・出力smokeであり、実マウス／DPI差、実VRChat内の見た目・PhysBones、異なるskeletonの自動結合、共有mesh／skin／morph参照、完全VRM出力は別受入境界として残す。次のカードは共有参照の明示仕様化か、版固定した実SDK受け取り検証のどちらか一つに絞る。

## 2026-09-13 all-mesh-instance real-model recheck

実モデルでも一括取込の入口を検証できるよう、`Tools/Test-NyaForgeAuthoring.ps1 -ImportModel <path> -ImportAllModel` を追加した。private一時RadDollV3 VRMを使い、ファイル内の全mesh instanceをstatic／skinned graphへ展開し、各graphのEditMesh、graph-keyed rig session、native Save/Open、feature-preserving native exportを確認する。public fixtureの通常Authoring suiteは従来どおり維持する。

Windows Player `Builds/AllMeshRealV1/NyaForge.exe` の実RadDollV3 Authoring suiteは **PASS / 84 checks**（`Artifacts/Authoring-20260913-130636-74e77cd8f5b143b5a62324d73c5e3669/report.json`）。全mesh instanceの取込・保存・再開・native export roundtripを含む。同成果物のUnity **2022.3.22f1 Bridge**も **PASS**（`Artifacts/BridgeReceiver-20260913-131142-286-11d7292fd3e74bb492946824f1af290d/bridge-report.json`）。

これはprivate実モデルの自動smokeであり、実マウス／DPI差、実VRChat内の見た目・PhysBones、異なるskeletonの自動結合、共有mesh／skin／morph参照、完全VRM出力は別受入境界として残す。
# 2026-09-13 multi-object output validation

出力前のValidationがactive objectだけを見ていたため、body＋衣装の組合せでtriangle・vertex・材質・画像・骨格数を過小評価する経路を修正した。現在は全objectを同じdocument revisionで評価し、どれか一つでも未完了／stale／面なしなら `unknown` とする。負荷値は全objectの合計、同一skeleton hashは一度だけ数え、別skeletonは重複出力分を加算する。既存のfit判定がunknownである境界は維持した。

Coreは **471 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-cffa6324e716435794fde74d1fefdfef`）。2つのstatic objectを作り、triangle 8・render vertex 16として集計される回帰を追加した。Windows Player `Builds/MultiObjectValidationV1/NyaForge.exe` のAuthoring suiteは **PASS / 80 checks**（`Artifacts/Authoring-20260913-134148-d06d092bc9ce4b538a559653169febaf/report.json`）。Unity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-134221-106-41ca7637e04d4e719c2ef6b97fdab020/bridge-report.json`）。
# 2026-09-13 validation panel readability

「出力チェック」の結果表示を、判定だけの横並び文字列から、実測metrics・各check・警告を一行ずつ表示する形式へ変更した。複数objectの集計値（triangles、renderVertices、materials、textures、bones等）が保存前に読み取りやすくなり、unknown時の理由も同じ欄へ表示する。ドキュメントや制作データは変更しない。

Unity 6000.4.3f1のWindows Player `Builds/ValidationUiV1/NyaForge.exe` をビルドし、1280x800 Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-134900-bff15d3d95da44119750676e20bff74a/report.json`、画面 `authoring.png`）。同じ成果物のUnity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-134937-440-25ae2cadbe9042eb8525367e45f91011/bridge-report.json`）。Coreは変更なしで直近 **471 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-ffdc185b64364eaca06099e90ddc39fb`）。画像は自動Playerの初期制作画面で、実マウス・DPI個体差の受入とは分けて扱う。
# 2026-09-13 validation object count

出力チェックの`metrics`へ制作対象object数を追加し、GUIの縦型結果表示でも先頭に表示するようにした。複数objectを合算した判定であることを画面上でも確認できる。Coreの検証は **471 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-ba80481a8387490f8bec0b941183a9dd`）。

Unity 6000.4.3f1のWindows Player `Builds/ValidationUiV2/NyaForge.exe` をビルドし、1280x800 Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-135121-c60399d2161043dc8320f267b52fc99e/report.json`、画面 `authoring.png`）。同じ成果物のUnity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-135154-187-26dfd082c99b4bd3a373c6250f6d83d9/bridge-report.json`）。
# 2026-09-13 validation UI real-model recheck

最新 `Builds/ValidationUiV2/NyaForge.exe` で、private一時RadDollV3 VRM（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`）を使った候補選択→EditMesh頂点編集→native Save/Open→標準skinned GLB出力→再取込を再確認した。Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-135407-87d2b19633744c218be890edbf327601/report.json`、画面 `authoring.png`）。同成果物のUnity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-135526-451-d3d9354f898c4191ac9cd27b56408543/bridge-report.json`）。private素材はpublic repositoryへ追加していない。これは自動Player経路の証拠で、実マウス・DPI差・実VRChat内受入とは分けて扱う。
# 2026-09-13 validation UI all-mesh recheck

最新 `Builds/ValidationUiV2/NyaForge.exe` でprivate一時RadDollV3 VRMの **全mesh instance取込** を実行し、body・hair等を複数graph objectとして公開した。全objectの頂点編集、native Save/Open、feature-preserving native export、拡張skinned GLB出力と再読込までのAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-135624-c8605946d44b464c94e50b95aa4a59d7/report.json`、画面 `authoring.png`）。同成果物のUnity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-135831-583-5cc12364c6b6485c8902bb5320ae5c3e/bridge-report.json`）。private素材はpublic repositoryへ追加していない。
# 2026-09-13 per-object validation details

出力チェック結果に、制作対象ごとの状態と規模を追加した。集計の`metrics.objects`に加えて、結果JSONの`objects`へ`objectId`、`status`、`triangles`、`renderVertices`、`materials`、`textures`を保存する。未完成またはメッシュなしの対象は`status: unknown`として原因候補を残し、完成対象は評価済みメッシュの実測値を記録する。判定ロジックと合計値は変更していない。

Unityの出力チェック画面にも「対象別」欄を追加し、対象ID先頭8文字、状態、頂点数、三角形数を縦に表示する。これにより、複数オブジェクトの合計だけでは分からなかった「どの衣装／小物が未完成か」を画面上で確認できる。Core回帰では2オブジェクトの個別結果と合計値を固定した。

Coreは **471 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-3d71370b6873449e9888a1fe109e47ec`）。Windows Player `Builds/ValidationObjectsV1/NyaForge.exe`のAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-140207-6001fe322ecb477cb9333b5a243c6b94/report.json`、画面`authoring.png`）。Unity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-140240-211-18db2ee56ab14264af3fc677cb032408/bridge-report.json`）。実マウス、DPI個体差、実VRChat内の見た目・挙動は別の手動受入境界とする。
# 2026-09-13 initial VRM 1.0 humanoid export profile

標準VRM出力の最初の実装単位として、既存のrest-pose skinned GLBを基礎に`VRMC_vrm` extensionを付ける`VrmExportService`を追加した。WorkbenchのVRM1ボタンは、取込時のgraph単位`ImportedRigSession`をstable `BoneId`で出力GLTF nodeへ解決し、作品名・作者・作品license URLを明示して`model.vrm`を作る。1つのskinned avatar graph object、VRM 1.0 humanoid必須15骨、絶対HTTP(S) license URLを満たさない場合は出力先を作らず拒否する。

このprofileはmeta・humanoidと、MorphSetへ解決できるmorphTargetBindsを出力する。material bind、texture transform、LookAt、FirstPerson、SpringBone、MToon、animation、任意VRM拡張は未出力で、`export-report.json`へ制限を記録する。未解決の表情targetは黙って落とさず拒否する。既存のskinned GLBのBIN／geometryを再利用し、CoreでVRM extension再読込とmesh頂点・三角形数の一致を固定した。仕様書とAuthoring READMEにもこの境界を追記した。

Coreは **473 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-574e8b4ed9954863b12b364c0311e4eb`）。新規回帰はVRM1 packageの`VRMC_vrm`・meta・humanoid・morphTargetBinds再読込とGLB geometry不変、および`VrmExportService`の`model.vrm`／レポート生成を検証する。Windows Player `Builds/Vrm1HumanoidV2/NyaForge.exe`のAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-141819-d7cba444ef044b1c8daf70bd9cc13dfe/report.json`、画面`authoring.png`）。Unity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-141855-421-2d8df4c7896b48e8bd7635873f296452/bridge-report.json`）。実RadDollV3でのVRM1出力、UniVRM／VRChatでの受取・外観・挙動は未検証境界とする。
# 2026-09-13 P1: 保存・skinned出力・ウェイト表示の整合性修正

古いcommit基準のレビューで再現された4件を現行mainへ再照合し、次を修正した。

- 複数objectのOpen時に、active graphの`ImportedRigSession`・Expressions・Springを同じGraphIdから解決する。legacy単一sessionを別objectのmetadataへ照合しない。
- source skinのinverse-bindをsource joint slot順のまま渡さず、出力skeletonのstable `BoneId`順へ並べ替える。対応できない骨は出力前に停止する。
- source nodeのworld matrixから、出力skeleton親子に合わせたjoint local matrixを構成してGLBへ出力し、元の回転・拡縮・中間helperを保持する。matrixがない制作graphは従来のrest-derived translationを使う。
- source skin表示を取込時の古いbindingで固定せず、現在の`SkinBind`評価結果をsource slotへ変換してから下流graphを再評価する。ウェイト編集後の表示と出力対象が一致する。
- VRM 1.0出力へ、詳細形状と`gravityDir`が揃ったVRM 1.0由来sessionに限り`VRMC_springBone`（sphere/capsule、collider group、spring joint）を追加した。VRM 0.x、詳細不足、空group、未解決BoneIdは黙って変換せず拒否する。

Coreは **474 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-ac362427c5f54bdb8a20ef3a6200159b`）。新規回帰はVRM SpringBone packageの再読込とshape保持を確認した。Unity 6000.4.3f1のWindows Player `Builds/P1FixV4/NyaForge.exe` はコンパイル・ビルド成功。Authoring全件自動suiteは600秒設定で **PASS**（`Artifacts/Authoring-20260913-144051-aa589a686bcd48f4a8893525316f52ea/report.json`、`authoring.png`）。同成果物のUnity 2022.3.22f1 Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-144125-134-189a9721c1684d6cbc8707751a808f89/bridge-report.json`）。

未完了境界は、実モデルでの今回の保存後matrix／ウェイト編集目視、実VRChat／UniVRM受入、VRM material bind・LookAt・FirstPerson・MToon・animation・任意拡張、異なるskeleton結合、自動fit・貫通修正。次回は短い専用fixtureでP1の保存→再読込→GLB再取込をPlayer検証し、長時間suiteと分けて証拠化する。

# 2026-09-13 P1修正後のprivate RadDollV3再確認

public repositoryへ素材を追加せず、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`を入力にして、今回のP1修正後Playerで候補選択・編集・native Save/Open・標準skinned GLB出力・再取込を実行した。Windows Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-144320-e0bdb1af8a344640884475842253746f/report.json`、`authoring.png`）。同成果物のUnity 2022.3.22f1 Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-144450-130-658ed8b59fbb4075ace026dd047b0161/bridge-report.json`）。これはprivate実モデルでの自動確認で、骨回転・拡縮と編集ウェイトの個別目視、実VRChat／UniVRM受入は引き続き別境界とする。
# 2026-09-13 VRM1実モデル書き出し確認とWindows一時パス修正

実RadDollV3 VRM（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`）を使い、候補選択→EditMesh頂点編集→native Save/Open→標準skinned GLB出力→再取込に続けて、保存済みプロジェクトを再オープンしたVRM 1.0出力を確認した。出力は `model.vrm` と `export-report.json` を持つ一つのrevision-pinned directoryとなり、`VrmMetadataReader`で`vrm1`・humanoid 29 nodeを再読込できた。RadDollV3はVRM 0.xのため、製品側の方針どおりVRM 0.x SpringBoneの自動変換はせず、検証ではそのsessionを明示的に除外した（出力reportの`springBone:false`）。

この確認中、長いfixture／projectパスにVRM用のsource GLBとstaging suffixを二重に付けると、GLB report書込みがWindowsのパス長制約で失敗することを再現した。`VrmExportService`の一時source／stagingを要求先の隣接ディレクトリに短い名前で作り、最終出力だけを指定先へ移動するよう修正した。失敗時はstagingを削除し、既存の出力先を作らない契約を維持する。

Core **474 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-854a809c2f964cc992be0c79f5e296f9`）。Unity 6000.4.3f1の最終Player `Builds/Vrm1RealFinal/NyaForge.exe` はビルド成功（`Logs/build-player-20260913-150428-392.log`）。実モデルのVRM1付きAuthoring suiteは `Builds/Vrm1RealV6/NyaForge.exe` で **PASS**（`Artifacts/Authoring-20260913-150125-27e7a01df65a458bbfee2ed5eec5fd18/report.json`、VRM出力を含む85 checks）。

これはVRM1パッケージの実モデル自動確認であり、UniVRM／VRChat SDKへの受け取り、実VRChat内の見た目・SpringBone挙動、material bind・LookAt・FirstPerson・MToon・animation・任意拡張の完全出力、実マウス／DPI差の受入ではない。
# 2026-09-13 静的GLB表示形状の整合性修正

レビューで指摘された「表示形状の静的GLBが表示と違う」経路を修正した。Workbenchの静的GLB出力とMCP静的GLB出力へ、source skin表示補正済みのgraph meshをobject単位で渡す`ExportStaticWithOverrides`を追加し、現在のSkinBind weightを使ってsource palette補正を再計算する。これにより、取込後の頂点編集・weight編集を含む最終表示形状を静的GLBへ反映する。材質slot出力はprimitiveごとに参照頂点を局所リマップするため、再読込時の共有頂点数が変わる場合を許容し、実モデル検証ではsubmeshごとの三角形座標を比較する。

Coreは **475 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-61489347c8a14bda811fa7cce7e2e1e6`）。`static GLB export accepts a display-corrected mesh override` 回帰を追加した。Windows Player `Builds/StaticDisplayV3/NyaForge.exe` はビルド成功（`Logs/build-all-20260913-151801-205.log`）。private一時RadDollV3 VRMで、候補取込→EditMesh頂点編集→native Save/Open→標準skinned GLB再取込に加え、静的GLBの表示補正済み三角形座標照合まで含むAuthoring suiteが **PASS**（`Artifacts/Authoring-20260913-151822-e69b71eec5214ac9a17d65808cdb916b/report.json`、画面`authoring.png`）。

今回の自動確認はCoreとWindows PlayerのCPU／GLB往復であり、実マウス・DPI差、UniVRM／VRChat SDK受け取り、実VRChat内の外観・挙動は別の手動受入境界として残す。private素材はpublic repositoryへ追加していない。

追補: 同じPlayer check directoryを入力にUnity 2022.3.22f1 Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-152230-116-37b90f8d982042ad908a9036375297c3/bridge-report.json`）。

# 2026-09-13 装着後の面選択座標修正

チョーカーなどのrigid attachment後に面クリック判定がずれるP2を修正した。`PickFace`のレイ判定へ、描画と同じ`OwnedMeshProjection.WorldPoints`を渡し、装着rootの位置・回転を含むワールド座標で三角形を検査する。編集用のpolygon face ID対応は従来どおり維持する。

Unity 6000.4.3f1のWindows Player `Builds/StaticDisplayFinal/NyaForge.exe` はビルド成功（`Logs/build-all-20260913-152337-934.log`）。private一時RadDollV3 VRMを使ったAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-152359-5a2f78a3cbc740d19ef14517744e2a64/report.json`、画面`authoring.png`）。静的GLB表示形状照合を含む前回のBridge検証もPASS済み（`Artifacts/BridgeReceiver-20260913-152230-116-37b90f8d982042ad908a9036375297c3/bridge-report.json`）。

# 2026-09-13 最終結果projectionの二重Transform修正

レビューで指摘されたFrame中心／ズームずれを再確認し、`FinalResultProjection`の二重Transformを修正した。以前は頂点へ`RestTransform`を適用した`Points`を、同じ変換を持つrootで再度変換していた。現在はmesh頂点をローカル座標で保持し、root Transformを一度だけ適用する。attachment時は親のrigid poseをそのまま継承するため、位置・回転・拡縮が二重適用されない。Frameのboundsにも同じWorldPointsを使う。

Unity 6000.4.3f1のWindows Player `Builds/FrameFixV1/NyaForge.exe` はビルド成功（`Logs/build-all-20260913-152738-593.log`）。private一時RadDollV3 VRMでのAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-152800-7a096f00b6a4406aadd30f5653c68d14/report.json`）。1000x700のNavigation suiteも **PASS**（`Artifacts/Navigation-20260913-152926-06083e8c9bda4195b974481f8094d716/report.json`、`settings.png`で文字欠けなしを確認）。

# 2026-09-13 FrameFixV1 複数mesh実モデル再確認

Frame修正後の最新Playerで、private一時RadDollV3 VRMの全mesh instance取込を再実行した。body・hair等を複数graph objectへ展開し、各EditMesh、native Save/Open、graph-keyed metadata、feature-preserving native exportまで含むAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-153007-1f6a00200ada48a3949c7ecc8d206868/report.json`）。同じcheck directoryを使ったUnity **2022.3.22f1 Bridge**も **PASS**（`Artifacts/BridgeReceiver-20260913-153215-866-247a7f0038f04e1985530cd1123e3031/bridge-report.json`）。

# 2026-09-13 ドキュメントのVRM出力範囲同期

`docs/Authoring-Quickstart.md`の古い「標準VRM export未実装」という記述を、現行実装へ更新した。現在はVRM 1.0の初期profile（humanoid/meta、解決可能なmorph bind、詳細付きVRM1 SpringBone）を出力できる。一方、material bind・LookAt・FirstPerson・MToon・animation・任意拡張と、VRM 0.xの自動変換は引き続き対象外で、完全VRM出力とは扱わない。

# 2026-09-13 VRM1出力手順のQuickstart追記

現行WorkbenchにあるVRM 1.0 metadata入力と`VRM 1.0（humanoid）`出力ボタンの操作手順を`docs/Authoring-Quickstart.md`へ追加した。1つのskinned avatar graph、15必須humanoid骨、rest pose、license URL、出力先`exports/vrm1-日時-ID/`、SpringBone／未対応機能の境界を明記した。実VRChat／UniVRM受取確認は引き続き別受入項目である。

# 2026-09-13 Development-Plan現行到達点の同期

開始時点の比較表と現行mainの能力が混同されないよう、`docs/Development-Plan.md`へ現行到達点を追加した。実装・自動検証済みの範囲（graph／直接編集／Rig・weight／GLB・VRM取込／atomic全mesh／保存再開／GLB・初期VRM1出力／MCP／Bridge）と、手動受入・実VRChat受取・完全VRM・FBX/BLEND・自動fitを未完了境界として明記した。

# 2026-09-13 面選択のrender頂点／UV seam対応

面クリック判定を編集点配列から分離し、`PolygonRendering.Mesh`のrender頂点を使うよう補強した。UV seamで同じ制作頂点が複数render頂点へ分割される場合も配列範囲を誤らず、装着rootのワールドTransformを描画と同じく一度だけ適用する。Quickstartへ挙動を追記した。

Unity 6000.4.3f1のWindows Player `Builds/FacePickFixV2/NyaForge.exe` はビルド成功（`Logs/build-all-20260913-153738-772.log`）。private一時RadDollV3 VRMを使うAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-153802-08b46c08414f48148c6070fba145c34b/report.json`）。1000x700 Navigationも **PASS**（`Artifacts/Navigation-20260913-153928-dc07eec7a79c4c0ea09f7c2534f038aa/report.json`）。

# 2026-09-13 面選択ワールド頂点キャッシュ

高密度モデルでの軽量性を保つため、面クリックごとのrender頂点ワールド座標配列生成をやめ、`OwnedMeshProjection`構築時に`RenderWorldPoints`をキャッシュするようにした。UV seam分割を含むrender domainと、頂点編集用の`WorldPoints`を別々に保持し、通常クリックは追加割り当てなしで判定する。projection再構築（mesh／Transform／attachment変更）時だけキャッシュを更新する。

Coreは **475 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-6d3e3e05bc934e43900a2ccad27b6101`）。Unity 6000.4.3f1のWindows Player `Builds/FacePickPerfV1/NyaForge.exe` はビルド成功（`Logs/build-all-20260913-154110-759.log`）。RadDollV3全mesh取込を含むAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-154132-e96fc21de38f4fa59f67b3b123813c5a/report.json`）。1000x700 Navigationも **PASS**（`Artifacts/Navigation-20260913-154343-66577ae7461f400d9b3d0249ef459056/report.json`）。

# 2026-09-13 Save/Recovery crash検証

保存・再開の安全性を強めるため、最新のFacePickPerfV1全mesh実モデルcheckを入力に`Test-NyaForgeCrashRecovery.ps1`を実行した。Unity 2022.3.22f1 Bridgeの通常検証に加え、materials／prefab／receiptの更新途中停止後にプロセスを再起動し、recovery状態を再読込できることを確認した。Bridge reportは **PASS**（`Artifacts/BridgeReceiver-20260913-154451-682-e8552774ca7b4d10ad51b1d0fc684fe3/bridge-report.json`）。recovery証跡3件（`materials-recovery.json`、`prefab-recovery.json`、`receipt-recovery.json`）もすべて **PASS**。元のチェック成果物・制作データは変更していない。

# 2026-09-13 揺れ再生中のワールド座標キャッシュ修正

揺れ再生中にメッシュ頂点を再利用して更新する経路で、描画メッシュだけが新しい位置になり、`WorldPoints`／`RenderWorldPoints`が前フレームの座標に残る問題を修正した。これにより、再生中の面選択・頂点選択・Frameの対象座標が、実際に表示しているspring出力と一致する。`SpringMeshBuffers`がavatar-local pointsからrootのワールド座標を更新し、互換spring出力の再利用経路で両キャッシュへ反映する。トポロジー・材質・Transformが変わる場合は従来どおりprojectionを再構築する。

Unity 6000.4.3f1のWindows Player `Builds/SpringWorldCacheV1/NyaForge.exe` はビルド成功（`Logs/build-all-20260913-154739-666.log`）。private一時RadDollV3 VRMを使ったAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-154803-9c4b28c5417b4ed4b41cd923de05ecc8/report.json`、画面`authoring.png`）。suiteにはVRM0/1 playback handlers、再生中の再利用メッシュ・編集点復元・GUI/MCP pause/resume/rebuild/reset/step、Save/Openでsimulationを除外する確認を含む。

今回は実モデル自動suiteでの一連の再生・保存・編集経路を確認した。実マウス操作で再生中に面をクリックする手動受入、Unity Bridge／UniVRM／VRChat実機での描画・揺れ挙動は別途確認する。

# 2026-09-13 複数graphでのlegacy VRM sidecar誤結合ガード

GraphId付きsession tableがある現行projectでは既にobject単位でmetadataを解決しているが、旧形式の単一VRM sidecarが残っている場合に、複数graphの選択中objectへ推測で割り当てないようOpen経路を強化した。legacy expression／Springは、graphが1つの場合、または同梱legacy rigがactive graphを明示する場合だけ互換fallbackを許可し、それ以外はgraph-keyed tableだけを正本として扱う。これによりVRM avatar Aと通常GLB Bを同一projectへ保存した後、B選択時にAのexpression／Springを混ぜる経路を閉じた。既存の単一graph legacy projectとschema 4 tableは維持する。

Unity 6000.4.3f1のWindows Player `Builds/LegacySessionGuardV1/NyaForge.exe` はビルド成功（`Logs/build-all-20260913-155431-565.log`）。private一時RadDollV3 VRMを使った単体＋全mesh Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-155503-e7bd9bac89c34afa9da61b58b3ceef4f/report.json`）。候補選択、複数mesh取込、graph-keyed rig/session、native Save/Open、skin／extended GLB出力、VRM0/1 playback lifecycleを含む既存回帰を通過した。

Core回帰も **475 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-fd7df009a97b4f20b7b165ec691d24c4`）。この変更はUnity Open互換ガードのため、Coreの件数は前回と同じ。実VRChat／UniVRM受取、legacy sidecarを意図的に複数graphへ混在させる破損fixtureの手動確認は別境界とする。

# 2026-09-13 Quickstartの保存・取込範囲同期

`docs/Authoring-Quickstart.md`の古いschema 2移行表記を現行schema 4へ更新した。GLB／VRMの説明も、単一mesh/skin選択取込に加えて同一ファイルの全mesh instance原子的取込を明記し、実装・検証済みの範囲と一致させた。
# 2026-09-13 legacy sidecar診断の可視化

複数graph projectで旧形式の単一VRM expression／Spring sidecarを開いたとき、割当先を推測せず保持するだけでは画面上で理由が分かりにくかった。`OpenProject`で曖昧な旧形式payloadを検出し、読み込み後に「未割当・sidecarは保存済み・元モデル再取込またはGraphId付きprojectへ移行」という状態をStatusへ表示するようにした。GraphId付きsession table、単一graphの旧project、legacy rigがactive graphを明示するケースは従来どおり復元する。

Windows Player `Builds/LegacySessionGuardV2/NyaForge.exe` はビルド成功（`Logs/build-all-20260913-160144-681.log`）。private一時RadDollV3 VRMを使った単体＋全mesh Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-160206-0372f66742b643aab57caf219ea6fe84/report.json`、画面`authoring.png`）。候補選択、複数mesh取込、graph-keyed metadata、native Save/Open、skin／extended GLB出力、VRM0/1 playback lifecycleを含む既存回帰を通過した。Coreも **475 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-fa8ff7533c224346bbdf9b8e398def7d`）。

この検証は自動Player／Coreの証跡であり、legacy sidecarを意図的に複数graphへ混在させた手動fixture、実マウス・DPI差、UniVRM／VRChat受取、実VRChat内の見た目・PhysBones挙動は別受入境界とする。
# 2026-09-13 取込metadataの原子commit

取込候補の検証後にgraphを先に追加し、その後metadata codecが失敗すると対象だけ残る可能性を閉じた。単体／全mesh取込とも、rig・expression・Spring・secondary-motion・GLB diagnosticsを次状態のattachmentへ事前にシリアライズし、`ProjectAttachments`を構築してからAddGraph commandを実行する。codec・予算・hash検査で失敗した場合は文書、attachment、session辞書、Undo履歴を変更しない。成功後だけruntime session辞書とworkspace attachmentを公開する。

Coreは **475 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-164ca8b20e1b46919318228bbab20ed2`）。Unity 6000.4.3f1のWindows Player `Builds/ImportAtomicV1/NyaForge.exe` はビルド成功（`Logs/build-all-20260913-160853-398.log`）。private一時RadDollV3 VRMの単体＋全mesh Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-160919-4fff40e348e744e19ff68e3529e966f0/report.json`、画面`authoring.png`）。同成果物のUnity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-161135-894-38ef39ad8ded4282b5a0f5945d6795e2/bridge-report.json`）。

codec失敗を意図的に注入する破損fixture、実マウス・DPI差、UniVRM／VRChat受取、実VRChat内の外観・PhysBones挙動は別受入境界とする。
# 2026-09-13 Quickstartの点選択上限表記修正

実装済みの点マーカー／クリック選択が全編集点を対象とし、2,048点単位で描画をまとめる仕様に対して、Quickstartに残っていた「先頭256頂点まで」という古い説明を修正した。ID指定も全頂点を対象とする記述を維持した。

これはドキュメント整合性の修正で、製品コード・保存形式は変更していない。直近のWindows Player Authoring／Unity Bridge／Core検証結果は前項の `ImportAtomicV1` 記録を正本とする。
# 2026-09-13 Authoring中の描画応答性と観測値

通常viewerは非操作時15fps／非フォーカス5fpsで省電力にする一方、制作画面を開いている間は60fpsと通常描画へ切り替えるようにした。頂点・面・UV・3D paintのpointer編集で、viewerの待機用frame capが残って入力表示を遅らせないための変更である。制作画面を閉じると従来のviewer省電力制御へ戻る。

Unity 6000.4.3f1の`Builds/AuthoringResponsiveV1/NyaForge.exe`でDense Paint計測を実施し、合成131,072三角形・256px画像・30 frameの観測値は target 60fps、平均16.76ms、P95 17.04ms、最大19.17ms、GC0は30回増加だった（`Artifacts/Authoring-20260913-161603-8d5717f2193641c68741d344132aa4c7/dense-paint-profile.json`）。これはRTX 4090・Unity Player一台の観測値で、他GPUや大規模実アバターの性能保証ではない。

同Playerでprivate一時RadDollV3 VRMの全mesh instance取込・頂点編集・native Save/Open・GLB出力を含むAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-161644-c1daec285f28438b870d2e5df732733c/report.json`）。Unity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-161901-669-68d3a0ef75924fdd8f100564689704de/bridge-report.json`）。1000×700 Navigationも **PASS**（`Artifacts/Navigation-20260913-161921-42b24c9738b1421f8e49fb1256e1f1c2/report.json`）。

性能の正式な合否は機種別の測定が必要であり、実マウス・DPI差、UniVRM／VRChat受取、実VRChat内の外観・PhysBones挙動は別受入境界とする。
# 2026-09-13 Authoring応答性の回帰固定

Authoring画面を開いている間の60fps／OnDemand描画解除を、Dense Paint Player検証の明示条件として固定した。`Application.targetFrameRate >= 60` かつ `OnDemandRendering.renderFrameInterval == 1` でなければ、制作画面の回帰としてsuiteを失敗させる。通常viewerの待機省電力制御とは分離している。

Unity 6000.4.3f1の`Builds/AuthoringResponsiveV2/NyaForge.exe`はビルド成功（`Logs/build-all-20260913-162013-417.log`）。Dense Paint回帰は **PASS**（`Artifacts/Authoring-20260913-162035-7c6436829ccb48ef8967a24b9c4824d6/report.json`）。同Playerでprivate一時RadDollV3 VRMの全mesh取込・頂点編集・native Save/Open・GLB出力を含むAuthoring suiteも **PASS**（`Artifacts/Authoring-20260913-162111-22190844683a40ba8e49ee9a4ae0b804/report.json`）。Unity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-162322-577-2235196b85fd4ed9bcb5c06550bfa253/bridge-report.json`）。

これはPlayer一台の応答性回帰であり、機種別性能保証、実マウス・DPI差、UniVRM／VRChat受取、実VRChat内の外観・PhysBones挙動は別受入境界とする。

# 2026-09-13 出力補正失敗の黙示フォールバック防止

静的GLBの表示形状をsource skin補正から作る経路で、補正が失敗した場合に未補正のgraph出力へ黙って戻る処理を削除した。現在はstale binding・不足したsource skin・不完全な評価を`AuthoringException`としてGUI／MCPへ返し、表示と異なる静的GLBを成功扱いで公開しない。これは「表示した形と出力した形を一致させる」ための安全境界で、原因を直してから再出力する。

実モデル検証側も、raw source-slotのinverse-bind配列を直接渡す経路を廃止し、本番GUI／MCPと同じstable BoneId順のinverse-bind再整列、親相対joint-local行列、identity node transformの組合せを使うようにした。これまでのall-mesh smokeがこの出力規則を実際に通るようになった。

Coreは **475 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-72abf8521b22410480b1013456745ce0`）。Unity 6000.4.3f1のWindows Player `Builds/OutputConsistencyV1/NyaForge.exe` はビルド成功（`Logs/build-all-20260913-163056-409.log`）。private一時RadDollV3 VRMの全mesh取込→編集→native Save/Open→拡張skinned GLB出力を含むAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-163128-9961c8b0bc08419bbf7d066126f789ef/report.json`、`authoring.png`）。同成果物のUnity **2022.3.22f1 Bridge**も **PASS**（`Artifacts/BridgeReceiver-20260913-163342-168-0dd45298a43349f6859ac54fd0b4e83b/bridge-report.json`）。

この回帰はCore・Windows Player・Bridgeの自動証跡であり、実マウス／DPI差、UniVRM／VRChat実機での受け取りと見た目、完全VRM semanticsは別の手動受入境界として残す。

# 2026-09-13 MCP Undo/Redoの揺れ設定キャッシュ同期

MCPのapply経路がGUIのExecuteを経由しないため、history.undo／history.redoでProjectAttachmentsだけ復元され、共通揺れ設定の表示・再生キャッシュが古いまま残る可能性を修正した。MCPも専用のcommand helperを通し、履歴操作が成功した直後にactive graphのsecondary-motion attachmentを再読込してから選択・表示を更新する。これでGUIとMCPの保存済み揺れ設定の復元境界が一致する。

回帰検証へ、GUIのrebind Undo/Redoに加えてMCP Undo/Redoを追加し、復元前後のattachment raw hashがactive cacheへ反映されることを確認した。Coreは **475 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4e68ec5a34524831a4a9cc0a84b173dd`）。Unity 6000.4.3f1のWindows Player `Builds/McpHistorySyncV2/NyaForge.exe` はビルド成功（`Logs/build-all-20260913-163945-132.log`）。private一時RadDollV3 VRMを使ったAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-164008-0387e88c1cb14a4a91110187259842c2/report.json`、`authoring.png`）。同成果物のUnity **2022.3.22f1 Bridge**も **PASS**（`Artifacts/BridgeReceiver-20260913-164225-649-8406d9aa891c42fdbc2403176809679d/bridge-report.json`）。

これは自動Player／Core／Bridgeでの履歴同期証跡であり、外部sidecarを使った実MCPプロセス接続、実マウス／DPI差、実VRChat内の受取・外観・挙動は別受入境界として残す。

# 2026-09-13 レビュー指摘のGLB姿勢・編集ウェイト回帰固定

4fcd0fd時点のレビューで挙がった保存／出力／ウェイト編集のP1を現行mainへ再照合し、再発防止のCore回帰を追加した。GLB出力はstable BoneId順のinverse-bindと親相対joint-local行列を使い、子が親より先に並ぶ骨格でも回転・非一様拡縮・平行移動を保持することを確認した。source skin表示は評価中の現在SkinBind入力をSourceSkinGraphAdapterへ渡し、ポーズ中のウェイト再割当が表示メッシュへ反映されることを確認した。GraphId付きrig／expression／Spring、表示と静的出力の不一致停止、静的小物取込の既存修正も維持する。

Coreは **476 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-17e01774b91a44ddb4878de8d86aff1b`）。これはコード経路と合成fixtureの回帰証跡であり、実マウス／DPI差、UniVRM／VRChat SDK受取、実VRChat内の外観・挙動は引き続き別受入境界とする。

# 2026-09-13 MCP状態へattachment identityを追加

MCP／状態読取で形状の`stateHash`とは別に、VRM expression・Spring・rig・PhysBones・secondary-motion等の保存済みattachment全体を識別できる`attachmentsHash`を返すようにした。AI側が保存前後やUndo/Redo後のメタデータ状態を、推測ではなくハッシュで確認できる。`AuthoringStateReader`とCore状態回帰へ接続し、attachment変更後のhash更新も確認した。

Coreは **476 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-2d4b6aa55f8b4a2288d8ab472f029123`）。

Windows Player `Builds/StateIdentityV1/NyaForge.exe` のRadDollV3全mesh Authoring回帰も **PASS**（`Artifacts/Authoring-20260913-165352-4f8f0b1535304817bacbf22f607d5e4a/report.json`）。同じ成果物をUnity **2022.3.22f1** Bridgeへ渡した受け取り検証も **PASS**（`Artifacts/BridgeReceiver-20260913-165607-326-e4802bf2ad78487193be352cfc688dab/bridge-report.json`）。

生成されたAuthoring画面（`Artifacts/Authoring-20260913-165352-4f8f0b1535304817bacbf22f607d5e4a/authoring.png`）を目視し、右側パネルの縦スクロール、制作対象表示、頂点操作案内、下部statusが同一画面内で欠けずに描画されることを確認した。これは自動capture画像の確認であり、実マウス・DPI別の手動受入ではない。

# 2026-09-13 PhysBones SDK受け取りpreflight

実VRChat SDKをまだ導入していないWindows環境でも、受け取り先Unity projectの依存とPhysBone候補を先に確認できる読み取り専用`Tools/Test-NyaForgePhysBonesSdk.ps1`を追加した。target manifestの`ComponentTypeName`、`com.vrchat.*`依存、Unity-owned source/package locations内のPhysBone候補ファイルをJSONへ記録する。候補検出は型形状の完全一致や実component生成を保証せず、Unity Bridgeの事前診断と実SDK／実アバター受入へ明示的につなぐ。`-RequireSdk`で候補不足をCI上の失敗として扱える。

候補ファイルの検索条件は`VRCPhysBone`／`VRCSDK`／`VRChat`へ限定し、NyaForge自身の`PhysBones` fixtureやbridge markerをSDK実体と誤認しないようにした。

合成Unity project（`com.vrchat.base`＋`Assets/VRCPhysBone.cs`）では`candidate_found`、実Bridge receiverでは`unavailable`を返すことを確認した。実行中にPowerShell組み込みの`$Matches`と衝突する変数名も修正済みで、候補リスト生成が正常に完了する。

# 2026-09-13 graph_inspect／validateのattachment identity整合

`get_state`だけでなく、MCPが編集結果を詳細確認する`graph_inspect`と`validate`にも`attachmentsHash`を追加した。これで形状の`stateHash`と、rig・expression・Spring・PhysBones・secondary-motion等の保存済みattachmentを、状態確認経路ごとに同じ識別子で照合できる。空プロジェクトと不完全出力のCore回帰で値を固定し、従来の診断・metrics・statusは変更していない。

Coreは **476 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-c45f173496074d3fb072ef7995e946b0`）。この変更は状態読取の契約整合を対象とし、Windows Player／Unity Bridge／実SDK・実VRChatの受け取りは既存の受入境界を引き継ぐ。

# 2026-09-13 VRChat SDK 3.7.6 PhysBones受け取り写像

レビューで残っていた実SDK受け取り境界を、VCCキャッシュのVRChat SDK 3.7.6（Unity 2022.3）で確認した。reflection backendが実際の`VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone`を解決し、root／endpoint／除外骨／branch／collider／limit／curve／interaction／parameterを設定できるようにした。SDK 3.7の`maxAngleX`／`maxAngleZ`、`maxSquish`、`AdvancedBool { False, True, Other }`を抽象設定へ明示写像する。SDKにないdamping等や重力方向へ非ゼロ値を渡す場合は、コンポーネント生成前のpreflightで`SDK_MEMBER_MISSING`として停止し、値を黙って捨てない。詳細は`docs/PhysBones-SDK-Compatibility.md`に記録した。

実SDK probe **PASS**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-PhysBonesSdkProbe-20260913-5e7f66bbf32a464286e9a84465f5a79c/physbones-sdk-report-16.json`）。Core **476 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-cebf69dc5bb5449d9e10ffe45b43746b`）。Unity 6000.4.3f1 Windows Player build **PASS**（`Builds/PhysBonesSdkCompatV1/NyaForge.exe`、`Logs/build-all-20260913-172701-801.log`）。同PlayerのAuthoring suite **PASS**（`Artifacts/Authoring-20260913-172731-2a6d0733c50c48f4a71c37ccaee5a9a3/report.json`）、Unity 2022.3.22f1 Bridge **PASS**（`Artifacts/BridgeReceiver-20260913-172802-343-45a35a51b0ff4f50885efa1dde54ecfd/bridge-report.json`）。

残る境界は、実アバターを使った揺れの見た目、VRChat Build & Test／実機、Quest制約、実マウス／DPI差。SDK DLLとprivate素材はpublic repositoryへ追加しない。

# 2026-09-13 GLB共有リソースの明示化

I04-Bの残件を、暗黙のmesh aliasを作らずに参照関係を確認できる形へ進めた。`GlbSceneInventory.SharedResources`がmesh resourceごとのnode index／skin indexを公開し、同じmeshを複数nodeが参照する場合や一つのmeshへ複数skinが対応する場合を候補確認で区別する。Workbenchのmesh候補・候補statusへ共有数を表示し、全mesh instance取込後はnodeごとに独立編集できることを明記した。source hash＋mesh indexを含むmorph IDと、graph keyedのskinned session保持は既存契約を引き継ぐ。

Coreへ「同一mesh resourceを2 nodeが参照するinventoryが、aliasを報告しつつ2 instanceを保持する」回帰を追加した。共有参照の方針は`docs/Shared-Resource-Policy.md`へ記録し、import READMEも同期した。safe resource dedup、異なるsource skeletonの結合、外部アプリの共有解釈、実素材での共有参照受入はI04-Bの残件として継続する。

Coreは **477 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-07c2979fe4314ddfab27df9c7c858bd5`）。Unity 6000.4.3f1のWindows Player `Builds/SharedResourceUiV1/NyaForge.exe` はビルド成功（`Logs/build-all-20260913-173935-325.log`）、Authoring suite **PASS**（`Artifacts/Authoring-20260913-174003-379716347af5449d96e02f3d3f4caaa8/report.json`、画面`authoring.png`）、Unity 2022.3.22f1 Bridge **PASS**（`Artifacts/BridgeReceiver-20260913-174039-231-85b8e1f85217483f8cbc2367b9c34a8b/bridge-report.json`）。

# 2026-09-13 PhysBones実SDK probeの再利用化

一時検証スクリプトだけに依存しないよう、`UnityBridge/Editor/PhysBonesSdkIntegrationVerification.cs`を追加した。SDK導入済みの受け取り側Unity projectで、`PhysBonesSdkIntegrationVerification.Run`を明示実行すると、実行時型解決、capability列挙、非破壊preflight、実`VRCPhysBone`生成・stable root設定を一度に確認できる。`Tools/Test-NyaForgePhysBonesSdk.ps1 -RunUnityProbe -RequireSdk`から呼び出し、レポートとUnityログは一時フォルダへ出す。SDKなしの通常Player／Core起動ではこのprobeを自動実行しない。

再利用probeを実際のSDK 3.7.6 projectで再実行し、`status=verified`（probe `status=passed`）を確認した。非対応のdampingへ非ゼロ値を渡すケースも、コンポーネント生成前に`SDK_MEMBER_MISSING`で拒否されることを回帰した。レポートは `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-PhysBonesSdkProbe-script-20260913-final2.json`。SDK DLL、対象project、private素材はリポジトリへ追加しない。

Core／Windows Player／Unity Bridgeの直前PASS証跡は前項のPhysBones SDK受け取り写像を正とする。実アバターの揺れ、VRChat Build & Test／実機、Quest制約、実マウス／DPI差は未完了境界として継続する。
# 2026-09-13 clean GLBのsource locator保持

取込時にlossy診断が0件のGLBでも、`ImportedGlbDiagnostics`レコードを必ず生成して保存するようにした。これによりsource hash・選択mesh index・skin indexが診断の有無に依存せずnative projectへ残り、共有meshの出所をSave/Open後も追跡できる。Playerの取込診断画面には、空診断レコードのlocatorと「診断項目なし」を確認する回帰を追加し、Coreでは空診断locatorのsnapshot Save/Openを検証した。

Coreは **477 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-212d7df90b524f1383c60eda06d182e2`）。Unity 6000.4.3f1のWindows Player `Builds/ImportLocatorV2/NyaForge.exe`はビルド成功（`Logs/build-all-20260913-174750-928.log`）。Authoring suiteは **PASS・81 checks**（`Artifacts/Authoring-20260913-174813-4363023cd488480fa353aece07713e87/report.json`、画面`authoring.png`）で、clean GLB locator回帰を含む。同成果物をUnity **2022.3.22f1** Bridgeへ渡した受け取り検証も **PASS**（`Artifacts/BridgeReceiver-20260913-174848-448-a38f7b33920a452997438a283373cc3e/bridge-report.json`）。

safe resource dedup、異なるsource skeletonの結合、実素材・実マウス／DPI差、UniVRM／VRChat Build & Test・実機での見た目とPhysBones挙動は引き続き別受入境界とする。
# 2026-09-13 実モデルVRM 1.0 geometry再読込検証

VRM 1.0初期profileの実モデル受入を強化し、`VerifyCommandLineVrmExport`で出力`model.vrm`を`VrmMetadataReader`だけでなく`GlbSkinImporter`でも再読込するようにした。VRM包装前に生成した標準skinned GLBと、VRM内GLBのmesh topology hash・頂点数・三角形数・骨数が一致することを確認する。これによりmeta/humanoidが読めるだけでなく、包装時にgeometry／skeleton cardinalityが変わっていないことを実モデル回帰へ固定した。RadDollV3はVRM 0.x由来のため、SpringBone自動変換は仕様どおり検証対象から明示除外している。

private一時RadDollV3 VRMを使ったWindows Player `Builds/VrmRealGeometryV1/NyaForge.exe` のAuthoring suiteは **PASS・88 checks**（`Artifacts/Authoring-20260913-175341-bff0e1119a8443c18770a3eea7f2b6df/report.json`）。VRM 1.0 metadata再読込、source skinned GLBとのgeometry／骨数一致、native Save/Open、GLB出力を確認した。同成果物のUnity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-175531-361-e30d839270284c66b41684879f7add00/bridge-report.json`）。private素材・生成VRMはpublic repositoryへ追加していない。

VRM 1.0のmaterial bind・LookAt・FirstPerson・MToon・animation・任意拡張、VRM 0.x SpringBone変換、実VRChat内の見た目・挙動、実マウス／DPI差は引き続き別受入境界とする。
# 2026-09-13 all-mesh source locator Save/Open回帰

全mesh instance取込でも、graphごとのGLB source locator（source hash・mesh index・skin index）がnative Save/Open後に失われたり別resourceへずれたりしないことを実モデル回帰へ追加した。`VerifyCommandLineAllModelImport`はdiagnostics attachmentを再読込し、元inventoryのinstance数・source hash・mesh/skin locator集合と比較する。同一mesh resourceを複数nodeが参照する入力でも、graph objectを潰さず出所を追跡できる境界を保つ。

private一時RadDollV3 VRMを使ったWindows Player `Builds/AllModelLocatorV1/NyaForge.exe` のAuthoring suiteは **PASS・87 checks**（`Artifacts/Authoring-20260913-175755-355fae7f46f34d2884f677bf6e3a832c/report.json`）。全mesh instance取込、各graph EditMesh、native Save/Open、locator一致、native export、拡張skinned GLB出力を確認した。同成果物のUnity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-180013-231-0bb75aeca3664743a70c922a437e19c5/bridge-report.json`）。private素材・生成物はpublic repositoryへ追加していない。

共有mesh／morphの完全共有保持、異なるsource skeletonの結合、実マウス／DPI差、実VRChat内の見た目・PhysBones挙動、完全VRM意味情報は引き続き別受入境界とする。
# 2026-09-13 GLB diagnostics v2: node instance locator

同一mesh／skin resourceを複数nodeが参照するGLBで、mesh indexとskin indexだけでは別instanceを区別できないため、`ImportedGlbDiagnostics`をv2へ拡張して任意のsource `nodeIndex`を保存するようにした。v1 sidecarはnodeなしとして後方互換読込し、新規writerはnode indexを含める。単体／全mesh取込では明示したnode instanceのindexをrecordへ渡し、取込診断GUIにも表示する。全mesh Save/Open回帰は元inventoryのnode・mesh・skin locator集合とsource hashを比較し、同一resourceの別nodeを潰していないことを検証する。

Coreは **477 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f29beb3b99ac413785703172d22f4d19`）。旧v1 payloadの読込、v2 node locatorのroundtrip、空診断のsnapshot Save/Openを含む。Unity 6000.4.3f1 Windows Player `Builds/NodeLocatorV1/NyaForge.exe` のprivate一時RadDollV3全mesh Authoring suiteは **PASS・87 checks**（`Artifacts/Authoring-20260913-180434-a0ccdab3ae5c41d8bc58ac7f2327f4be/report.json`、画面`authoring.png`）。同成果物のUnity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-180656-635-b2f92b767fbf44f6ab3409efe8790e96/bridge-report.json`）。private素材・生成物はpublic repositoryへ追加していない。

完全なmesh／morph shared-resource dedup、異なるsource skeletonの結合、実マウス／DPI差、実VRChat内の見た目・PhysBones挙動、完全VRM意味情報は継続課題とする。
# 2026-09-13 GLB diagnostics v2のinspection接続

GLB import diagnostics v2の`nodeIndex`を`AuthoringGraphReader`の`importDiagnostics`へ公開した。保存済みのsource hash・mesh/skin・node locatorをMCPの`graph_inspect`でも読めるため、共有meshを別nodeへ誤結合せずAI側から確認できる。v1 sidecarの互換読込、v2 codec、snapshot Save/Open、Workbench UI表示、全mesh実モデルlocator照合を同じ契約へ揃えた。

Coreは **477 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-8309a09287bc4dc49422ce67a5ca60ac`）。Unity 6000.4.3f1 Windows Player `Builds/NodeLocatorV2/NyaForge.exe` はビルド成功（`Logs/build-all-20260913-180920-947.log`）。公開fixtureのAuthoring suiteは **PASS・81 checks**（`Artifacts/Authoring-20260913-180942-182d788931524eeeb8914736c746607e/report.json`）。private一時RadDollV3 VRMの全mesh instance取込→EditMesh→native Save/Open→node／mesh／skin locator照合→native exportは **PASS・87 checks**（`Artifacts/Authoring-20260913-181029-3c9195c61881409aac8b50855128769a/report.json`）。同成果物のUnity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-181247-864-57000c26354b41449c18c00975c59778/bridge-report.json`）。private素材・生成物はpublic repositoryへ追加していない。

完全なmesh／morph shared-resource dedup、異なるsource skeletonの結合、実マウス／DPI差、実VRChat内の見た目・PhysBones挙動、完全VRM意味情報は継続課題とする。

# 2026-09-13 feedback implementation: VRM node mapping, canonical multi-skin order, legacy migration

最新レビュー（基準 `c4a2748`）のP1 3件を現行コードへ反映した。`GlbExportService`が出力した実node indexを `GlbExportResult.NodeMap` として返し、`VrmExportService`はWorkbenchの authored node token（0=mesh、1+=skeleton order）を実nodeへ解決してからVRMをパッケージする。GLB writerはBoneId順を共通のcanonical orderとしてJOINTS、joint local matrix、inverse-bind、shared skin identityを同じ順序で扱うため、skinごとの配列順が異なっても出力先の骨参照が一致する。旧single-sessionの expression／Springを持つ作品へGLBを追加した場合も、graph-keyed tableへ移行して保存する。

P2のうち、`SkinBinding.ContentHash`を追加してsource-skin表示cacheの大規模文字列化を廃止し、Undo/Redo後はworkspace attachmentからrig／expression／Spring cacheをgraph存在範囲へ再同期するようにした。装着小物の描画・面選択は、attachment時の子translationを共通の配置規則へ揃えた。PhysBones SDK probeのUnity／report／log／project path引数は空白を含むWindows pathでも1引数として渡す。

Core回帰は **479 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-ed7711fa737a47f08e9273344f79148e`）。GLB node mapの実indexとVRM `licenseUrl=other`／`otherLicenseUrl`を追加検証した。Unity **6000.4.3f1**で `Builds/FeedbackFix2/NyaForge.exe`を再ビルドし、800x600 Authoring suite **PASS**（`Artifacts/Authoring-20260913-184138-af3460e8fe7d4089a66e76a55e9d4981/report.json`、画面 `authoring.png`）。

実Unity SDK／実VRChat内の見た目・挙動、実マウス／DPI差、複数source skeletonの自動結合、完全VRM意味情報（texture transforms・animation等）は引き続き別受入境界とする。private素材はpublic repositoryへ追加していない。
# 2026-09-13 real-model VRM1 output recheck after node-map fix

Using private temporary input `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-RealModelSmoke\RadDollV3_VRM.vrm` with `Builds/FeedbackFix2/NyaForge.exe`, the 800x600 Authoring suite passed. The checks included command-line real GLB/VRM import, generated EditMesh and vertex edit, native Save/Open, standard skinned GLB export and reimport, then VRM 1.0 package export with metadata re-read. The output package preserved source skinned GLB topology, vertex/triangle counts, and skeleton cardinality. Evidence: `Artifacts/Authoring-20260913-184347-065d9267276c4e609d16320a0443080c/report.json` and `authoring.png`.

This confirms the node-map and authored-token resolution path on the private model. VRM0 SpringBone was intentionally omitted from this VRM1 export check. Real UniVRM/VRChat runtime appearance and behavior, complete VRM extensions, automatic clothing fit/penetration repair, and manual mouse/DPI acceptance remain separate gates.
# 2026-09-13 clothing weight transfer initialisation

衣装skin-bind後のRoot 100%初期化だけでは、毎回すべての頂点を手作業で割り当てる必要があった。`SkinWeightTransfer.ByBoneProximity`をAuthoring/Rigへ追加し、rest骨segmentまでの距離から最大4本を選び、決定的に正規化した初期weightを生成する。GUIの小物パネルに「自動weight初期化（骨近傍）」を追加した。これはfit・貫通判定・最終weight品質を保証する機能ではなく、Rig panelでの確認・手修正とpose確認を必須とする初期化支援である。既存のRoot初期化とstable BoneId装着は変更していない。

Core回帰は **480 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0d70cc4826e642758fddfe38da6debad`）。自動weightの決定性、全頂点の正規化、最大4 influence、不正falloff拒否を追加確認した。Unity **6000.4.3f1**で `Builds/WeightTransferV1/NyaForge.exe` をビルドし、800x600 Authoring suite **PASS**（`Artifacts/Authoring-20260913-185032-5f9e24cf1ea146f8a6a4c2005557dbb5/report.json`、画面 `authoring.png`）。

自動fit・貫通修正、nearest-surface transfer、衣装の実アバター内見た目、手動mouse/DPI受入は別タスクとして残す。

# 2026-09-13 自動weight初期化のWorkbench回帰

小物ワークフローへ、Root 100%初期化の後に「自動weight初期化（骨近傍）」を実行する検証を追加した。自動初期化がSkinBindの内容hashを更新し、全頂点を1〜4本の正のinfluenceへ正規化した状態で、後続のpose copy・native Save/Open・skinned GLB出力へ進めることを確認する。自動結果は骨segment距離による初期値であり、fit・貫通判定・販売品質を保証しないため、Rig panelでの確認・手修正を引き続き必須とする。

Coreは **480 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-60e729da5dfc4547899e81182d56c397`）。Unity **6000.4.3f1** Windows Player `Builds/AutoWeightV2/NyaForge.exe` のAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-185647-a0e9524d71b94715aab9d5249cbf41ad/report.json`、画面 `authoring.png`）。

# 2026-09-13 同一avatar骨格の衣装をVRMへ同梱

従来のVRM1出力はgraph object 1個に限定され、skin-bindした衣装をavatarと一緒に出力できなかった。`VrmExportService`へmetadata対象object IDを追加し、同じskeleton hashを共有する複数のskinned graph objectを一つのGLB／VRMへ含めるようにした。Workbenchはactive humanoid avatarをmetadataの正本とし、static object、剛体attachment、異なるskeletonが残る場合は出力前に拒否する。humanoid・expression・Springのnode tokenはGLB writerの実node mapからavatar mesh／skeletonへ解決し、reportのobjectCountも実際の同梱件数を記録する。

Coreは **481 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-fed81b349a32470c8386e9ce1584fb96`、multi-object VRM回帰を含む）。Unity **6000.4.3f1** Windows Player `Builds/VrmClothingV2/NyaForge.exe` のAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-190247-bcfa83cf90c34dbb904099e2a5bc7d45/report.json`）。private fixtureで、avatar＋skin-bound clothingの自動weight、pose、native Save/Open、GLB／VRM出力、両meshの再読込を確認した。fixture・生成物・SDKはpublic repositoryへ追加していない。

これはVRM1初期profileの同一skeleton同梱であり、material bind、LookAt、FirstPerson、MToon、animation、任意拡張、実UniVRM／VRChat内の見た目・挙動は引き続き別受入境界とする。

# 2026-09-13 VRM同梱のAPI側skeleton検査

Workbenchの事前検査だけに依存しないよう、`VrmExportService.ExportVrm1`自身でもmetadata対象avatarのskeleton hashを基準に全graph objectを検査するようにした。異なるskeletonを直接API／MCPから渡した場合も`VRM_SKELETON_MISMATCH`で出力先を作らず停止する。Coreへ混在拒否の回帰を追加した。

Coreは **481 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f4b22134d2eb49308a09ca54c0780028`）。Unity **6000.4.3f1** Windows Player `Builds/VrmClothingV3/NyaForge.exe` のAuthoring suiteも **PASS**（`Artifacts/Authoring-20260913-190629-7f5413cf3b194f06a17f4fcb25a9015e/report.json`）。

# 2026-09-13 avatar表面からの衣装weight初期化

骨segment距離だけでなく、avatarのrest mesh表面から衣装weightを初期化できる経路を追加した。`MeshSurfaceProjection`がavatar meshを不変スナップショットとしてBVH化し、衣装各頂点のavatar空間位置から最近三角形を決定する。最近三角形3頂点の既存SkinBindingをバリセントリック係数で補間し、最大4本へ決定的に並べて正規化する。変形前のavatar mesh／現在のavatar bindingを使うため、pose済み表示メッシュを誤って参照しない。位置合わせ・自動fit・貫通修正を行う機能ではないため、Rig確認とpose確認を必須とする。

衣装パネルへ **自動weight初期化（avatar表面）** を追加し、avatarのrest meshとSkinBindが解決できるときだけ有効化した。従来の **自動weight初期化（骨近傍）** はフォールバックとして残した。Coreへ最近三角形補間の決定性、正規化、最大influence、stale binding拒否を追加し、Workbenchのavatar＋static GLB衣装回帰でもRoot初期化→骨近傍→avatar表面→pose copy→Save/Open→GLB/VRM出力を確認した。

Coreは **482 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b1f45c67d67b48e8a8a943b09ee8aeec`）。Unity **6000.4.3f1** Windows Player `Builds/SurfaceWeightV1/NyaForge.exe` のビルドは成功（`Logs/build-player-20260913-191751-692.log`）。同PlayerのAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-192030-2dbe35b99eae47609522395e4f39c436/report.json`、画面 `authoring.png`）。最初の120秒実行はsuite全体が収まらずタイムアウトしたため、同じ検証をTimeoutSeconds 300で再実行して完了を確認した。private素材・生成物・SDKはpublic repositoryへ追加していない。

実アバター衣装の表面対応品質、体形差への自動fit、貫通修正、実マウス／DPI差、実VRChat内の見た目・PhysBones挙動は引き続き別受入境界とする。

# 2026-09-13 bounded avatar-surface fit

衣装の位置合わせ用に、`MeshSurfaceFit.ProjectPositions` とWorkbenchの **衣装をavatar表面へfit** を追加した。衣装の現在のEditMesh結果をavatar rest meshの最近三角形へ投影し、指定したsurface offsetを法線方向へ加え、EditMeshのrest-space deltaとして一つのUndo commandへ置き換える。最大距離とoffsetをメートルで検査し、1頂点でも範囲を超えた場合は候補計算の段階で停止して文書を変更しない。トポロジー、UV、材質、weightは変更せず、fit後に既存のRig／pose確認へ進める。裏面への吸着、体形差、衣装同士の交差、販売品質を自動解決する機能ではない。

Coreは **483 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-7cff4eefa45448e69311e4a2d6200a01`）。bounded距離、offset上限、頂点数保持、最近面投影を回帰した。Unity **6000.4.3f1** Windows Player `Builds/SurfaceFitV3/NyaForge.exe` のビルドは成功（`Logs/build-player-20260913-193531-032.log`）。同PlayerのAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-193552-8e2244be8e9041678fd90f84ad6746ec/report.json`、画面 `authoring.png`）で、avatar＋static GLB衣装のskin-bind、weight初期化、surface fit、最大距離超過時の文書不変、pose copy、Save/Open、GLB／VRM出力を同じ検証へ通した。

表面法線の向きが入力meshのwindingに依存するため、offsetの符号と複数poseの交差はRig確認が必要。実アバターでの体形差、自動fit品質、貫通修正、実マウス／DPI差、実VRChat内の見た目・PhysBones挙動は別受入境界とする。

# 2026-09-13 PhysBones実SDK package-flow再検証

実SDK検証を直接profile生成だけで終わらせず、`PhysBonesTargetPackage.Export`→`Read`→`InspectPackage`→`ApplyPackage`の受け取り経路へ更新した。対象はVRChat SDK 3.7.6（`com.vrchat.base`／`com.vrchat.avatars`）、Unity 2022.3.22f1。完全修飾 `VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone` をmanifestへ保持し、preflightがシーンを変更しないこと、SDKで表現できない非ゼロ値をcomponent生成前に `SDK_MEMBER_MISSING` で停止すること、packageから実componentを生成してstable root／bone mappingを設定することを確認した。

検証レポート: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-PhysBonesSdkProbe-package-flow-20260913.json`（status `verified`、Unity probe `passed`）。UnityBridgeの合成受け取り回帰も **PASS**（`Artifacts/BridgeReceiver-20260913-193241-889-d5072cb90b02488cbd8d9ef8e6d8a161/bridge-report.json`）。SDK projectと一時packageはpublic repositoryへ追加していない。

実アバターへのstable BoneId／collider手動割当、複数pose・root移動・停止／再開の挙動、VRChat Build & Test／実機の見た目とPhysBones挙動はSIM-07Aの別受入境界として残す。
## 2026-09-14 参照body保護の保存・編集停止回帰

制作対象パネルへ **選択中を参照として保護（編集不可）** を追加した。avatarなどの基準objectを保護すると、表示・選択・保存・出力は維持したまま、頂点・材質・リグ・graphの編集commandとMCP操作を`REFERENCE_PROTECTED`で停止する。保護対象IDはnative schema 4の許可attachment `reference-protection.nyaforge.bin`へ、sortedなGUID列として保存する。Save/OpenでIDとGUIトグルを復元し、解除後は通常編集へ戻れる。

Coreは **497 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-a7d03400063146bc8b010f7bf4e874bb`）。参照ID codecの重複・順序・破損入力とnative attachment roundtripを確認した。Unity **6000.4.3f1** Player `Builds/RefProtection/NyaForge.exe`は、2 objectの選択・表示・Save/Open・編集分離に加え、保護中の頂点編集停止、保護設定の保存・再読込、解除まで **PASS**（`Artifacts/Authoring-20260914-033432-f1362aff12ff4b2b8e1db3edfd231ac4/report.json`）。同成果物のUnity **2022.3.22f1** Bridge receiver suiteも **15 checks PASS**（`Artifacts/BridgeReceiver-20260914-033504-121-546e15ff46e24f19b28033b7b97acac7/bridge-report.json`）。これは自動fixtureでの保存・command境界回帰であり、実RadDollV3 EditorWindowの手動操作、実body全周fit・貫通・見た目、VRChat内表示は未受入である。

# 2026-09-14 Windows native mouse acceptance (partial)

Windows用Computer Useで、`Builds/PerformanceV39/NyaForge.exe`を実際に起動し、次の操作をマウスで確認した。

- 「制作へ」→「ビューアーに戻る」の画面遷移
- 「パックを開く…」からWindowsファイル選択ダイアログを開く
- `GeneratedPacks/NyaForgeFixture/current.StandaloneWindows64.json`を選択して読み込む
- 「身体だけ表示」でCollarを非表示にし、「パック既定の表示に戻す」で復帰

読み込み後は`NyaForge synthetic fixture`が表示され、パーツ表示のチェック状態とステータス「調整しました」を画面で確認できた。これは実マウス経路が動くことの受入であり、実RadDollV3の読み込み、頂点編集、保存・再開、実DPI差、VRChat内表示は別の手動受入として継続する。Windows native Computer Useは、ブラウザ用Cuaとは別の操作経路を使用する。

# 2026-09-14 実RadDollV3のGUI保存・再開チェック

一時作業フォルダ `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-ManualAcceptance-20260914` を使い、Windows native Computer Useで実VRMを制作画面へ取り込んだ。候補確認には `mesh 0 (Bag.baked, 1 primitive) · skins 10 instances 10` が表示され、全mesh instance取込後は **10 objects**、取込骨対応 **171 bone / humanoid 29** を確認した。private素材と一時保存物はpublic repositoryへ追加していない。

同フォルダへGUIの「保存」を実行し、続けて同じ制作画面の「開く」を実行した。画面ステータスは「制作状態を開きました」となり、`project.nyaforge.json` と `blobs` が生成された。保存後のCore再評価では、10個すべてのgraph objectが `complete=True / out=True / diag=0` で、保存データのgraph破損は確認されなかった。

ただし、開き直し直後のviewportには「グラフの評価が未完了です」が残った。metricsには2307頂点 / 2609△が表示されるため、現時点では保存失敗と断定せず、**投影表示または警告ラベル更新の不一致**を次の調査対象とする。実マウスでの頂点選択・移動・Undo/Redo、GLB/VRM出力後の再読込、実Unity/VRChat内の見た目、DPI差は未受入である。

# 2026-09-14 評価警告ラベルの表示修正

`GeometryChangedEvent`が、viewport幅が通常のときに評価結果を無視してempty hintを常時表示していた。これを、狭いviewportでは非表示、通常幅では現在の`DisplayedGraphValue().Mesh`が未解決のときだけ表示する条件へ修正した。これにより、実GUIでEditMeshへ切り替えた後の頂点移動・Undo/Redoでも、正常なgraphを「評価未完了」と誤表示しない。

Unity **6000.4.3f1**で `Builds/UiHintFixV1/NyaForge.exe` を再ビルドし、800x600 Authoring suite **PASS**（`Artifacts/Authoring-20260914-122339-7eac5517b3cd47bfb423b6a1cbf287a7/report.json`）。実RadDollV3の保存・再開後のCore評価は前項のとおり全10 graphがcompleteであり、修正後Playerの実RadDollV3再取込→EditMesh→頂点編集→Undo/Redoの手動再確認を次の受入に残す。

# 2026-09-14 UiHintFixV1 実RadDollV3再開後の頂点編集

修正版 `Builds/UiHintFixV1/NyaForge.exe` をWindows native Computer Useで起動し、一時制作フォルダ `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-ManualAcceptance-20260914` を開いた。実RadDollV3の10 objectsを再表示し、graph選択を `EditMesh · 8c443c13` へ切り替えた状態で、メトリクス **2307頂点 / 2609△** と編集対象を確認した。このとき正常な評価結果へ「グラフの評価が未完了です」が誤表示されないことを確認した。

頂点ID 0をID指定で選択し、X座標欄の編集を確定して「選択頂点を移動」を実行した。ステータスは「編集を反映しました。元に戻す・やり直すで確認できます。」となり、未保存変更表示が出た。続けて「元に戻す」「やり直す」を各1回実行し、操作後も同じEditMesh表示と編集履歴UIを維持した。最後に「保存」を実行し、Player.logの `保存しました: C:\Users\tomoaki\AppData\Local\Temp\NyaForge-ManualAcceptance-20260914` と `project.nyaforge.json` の更新を確認した。

これは実モデルを対象にしたWindowsマウス経路の再開後編集・Undo/Redo・保存確認であり、UiHintFixV1のAuthoring suite **PASS**（`Artifacts/Authoring-20260914-122339-7eac5517b3cd47bfb423b6a1cbf287a7/report.json`）を補完する。頂点座標の数値差分を外部比較したものではなく、実アバターの全周fit、衣装の貫通、VRChat内の表示・PhysBones挙動、異なるDPIでの操作は引き続き別受入境界とする。

# 2026-09-14 実RadDollV3上の手首カフ装着・派生・native出力

UiHintFixV1で実RadDollV3を含む一時制作フォルダ `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-ManualAcceptance-Cuff3-20260914` を開き、GUIの「手首カフ形状を追加」を実行した。追加直後のPolygonEditは264頂点 / 256△で、native保存・再読込後もカフのprimitive objectを保持した。

再読込後、装着パネルでアバター対象と `handrole.R` のstable BoneIdを指定し、「この小物を装着」を実行した。ステータスは「小物をstable BoneIdへ装着しました」となり、続けて「Polygon造形をskin衣装へ派生」「自動weight初期化（骨近傍）」を実行した。派生後は新しい衣装objectが選択され、metricsは264頂点 / 256△。手首カフを全身avatar表面へ投影するfit測定は、頂点が設定距離を超えるため停止した。これは手首ボーン装着の用途で全身fitを無理に適用しないための確認で、fitによる形状変更は行っていない。

派生・装着・weight初期化後に同フォルダへGUI保存し、`project.nyaforge.json` を外部確認した。native schema 4、project objects 12、documentRevision 10、activeObjectIdは派生衣装、attachments 3を確認した。さらに「Unity用に書き出す」を実行し、`exports\bake-20260914-035504-b621c8\project.nyaforge.json` を生成した。出力manifestもschema 4 / 12 objects / revision 10で、native受け渡し経路が成立している。

これは実モデルでのカフ作成、stable BoneId装着、skin衣装派生、骨近傍weight初期化、native Save、Unity向けnative exportのWindowsマウス受入である。カフの実手首位置への頂点調整、複数poseでの追従、貫通、実Unity/VRChat内の見た目・PhysBones、別DPIでの操作、GLB/VRM最終商品出力は引き続き別受入境界とする。

# 2026-09-14 選択衣装skin packageのattachment干渉修正

同じnative作品に、元の小物Polygon（BoneId装着メタデータを保持）と、そこから派生したskin衣装を共存させた場合、従来のGLB検査が作品全体を走査していたため、派生衣装だけを選択した「選択衣装をskin packageで出力」まで停止していた。`GlbExportService.ValidateRequest`へ明示的な出力対象IDを渡し、attachment検査を選択対象へ限定した。全体出力、またはattachmentを含む選択対象は、従来どおり`GLB_ATTACHMENT_METADATA_UNSUPPORTED`で停止する。選択衣装のallowlistは重複・空・古いIDも検査するため、別objectの装着情報を黙って落とさない。

Coreへ、rigid attachmentを持つobjectと通常のskinned clothing objectを同一workspaceへ置き、後者だけを`ExportSkinnedObject`へ渡す回帰を追加した。GLB生成、objectCount=1、元attachment objectの保持を確認。

Coreは **507 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0806745367db4fcc9c5b7e854322ece2`）。Unity **6000.4.3f1** Windows Player `Builds/ClothingPackageV3/NyaForge.exe` のビルドも成功（`Logs/build-player-20260914-130401-609.log`、`NYAFORGE_PLAYER_OK`）。

実RadDollV3＋手首カフでは、旧Playerで元Polygonの装着、skin派生、骨近傍weight初期化、native Save/Open、Unity用native exportまで確認済み。旧Playerでのskin package出力は今回の修正前検査により停止したため、新Playerで派生衣装だけを選択したskin package出力と、`clothing.glb`／skeleton・binding sidecarの存在確認を次の手動受入に残す。全身surface fitは手首カフ用途では距離超過で停止する仕様で、ボーン装着の位置合わせを優先する。

追試として同じCore suiteを再実行し、**507 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-ad2cc5d370a54feca797fdafbea6fdef`）を確認した。

# 2026-09-14 新Playerで実衣装skin package出力を確認

修正版 `Builds/ClothingPackageV3/NyaForge.exe`をWindows native Computer Useで起動し、実RadDollV3＋手首カフのnative作品 `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-ManualAcceptance-Cuff3-20260914\project.nyaforge.json`をExplorerから開いた。派生衣装graph `73e5a236`を選択した状態で「選択衣装をskin packageで出力」を実行し、ステータスに成功表示が出た。

出力先 `exports\clothing-20260914-042448-0333be`には `skinned-clothing.nyaforge.json`、`clothing.glb`、`skeleton.nyaforge.bin`、`binding.nyaforge.bin` が生成された。元のBoneId装着Polygonを同じ作品に残したまま、選択したskin衣装だけをpackage化できることを実モデルで確認した。次の受入はこのpackageをUnity Bridgeへ適用し、移動済みavatar・更新・材質・UVを一周することにする。

# 2026-09-14 実カフpackageのUnity Bridge受け渡し

既存の合格済みPlayer reportを基準に、実RadDollV3＋手首カフから生成した `skinned-clothing.nyaforge.json` を `Tools/Test-NyaForgeUnityBridge.ps1` の隔離receiverへ渡した。Unity **2022.3.22f1** の `BridgeBatch.VerifyRoundTrip` は終了コード0、`NYAFORGE_BRIDGE_ROUNDTRIP_PASSED`、status `passed` を返した。証拠は `Artifacts/BridgeReceiver-20260914-132906-406-7039fd4e03d344759dd7ade55e5032f0/bridge-report.json` と `bridge.log`。

この経路ではpackageのmanifest／GLB／skeleton／binding hash検査後、stable BoneId mapからSkinnedMeshRendererを生成し、所有markerとgeometry/materialの受け渡しを確認した。Bridgeは合成avatar fixture上の受け取り確認であり、実Unity EditorWindowでの手動BoneId割当、移動・回転・scale済み実avatar、衣装更新・削除Undo、実VRChat表示はまだ別受入である。次は実receiver sceneへpackageを適用し、手動受入チェック表の3章を埋める。

# 2026-09-14 現行HEADのCore再確認と次の受入ゲート

現行 `main` (`b7237bb`) で `dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore` を再実行し、**507 passed / 0 failed** を確認した。artifact は `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-bc81d7dd83934127808e42f22b3e246d`。GLBの骨・skin・材質・UV0制限・衣装package・VRM node map・Spring・保存再開・Undo/Redoを含むCore回帰は通過している。

Windows v1の次の完了ゲートは、機能追加ではなく実環境の一貫性確認に固定する。順序は、(1) 実Unity Editorで移動・回転・scaleしたavatarへの衣装適用、衣装A→B更新、削除・Undo、(2) material slot／semantic normal・MR／UV0の見た目確認、(3) Unity受け取り後の保存・再開とVRChat Build & Test、(4) 受入結果を記録してpush、である。Mac、Quest、複数avatar、完全なBlender代替編集はWindows v1完了後へ送る。実RadDollV3と手首カフのpackage生成・Bridge受け渡しは確認済みだが、実EditorWindowでの全周fit・貫通ゼロ・VRChat内表示は未受入のまま残す。

# 2026-09-14 実カフpackageのUnity Bridge再確認

現行 main から、実RadDollV3＋手首カフで生成した skinned-clothing.nyaforge.json を Tools/Test-NyaForgeUnityBridge.ps1 へ再投入した。Unity **2022.3.22f1** の隔離receiverは終了コード0、Unity Bridge verification passed、status passed。証跡は $p/bridge-report.json と $p/bridge.log。manifest／GLB／skeleton／bindingのhash、stable BoneId、SkinnedMeshRenderer生成、所有marker、geometry/material受け渡し、変換済みavatar fixtureを再確認した。実EditorWindowでの全周fit・貫通、VRChat Build & Test／実機表示は未受入である。

# 2026-09-14 実Unity Editorのavatar変換観察（部分受入）

privateの `PhysBonesSdkProbe-20260914` をUnity **2022.3.22f1**で開き、実RadDollV3の子階層にある `RadDollV3 Cuff Probe` を確認した。Inspectorには `Skinned Mesh Renderer`、root bone `lower_arm.L (Transform)`、`Nya Forge Skinned Clothing Managed`、stable ObjectId／State Hash／GLB・skeleton・binding hashが表示された。avatar rootのPosition Xを一時的に2、Rotation Yを45、Scaleを1.5へ変更した後も、カフはavatar階層の管理objectとして残り、カフ自身のlocal Position/Rotationは0、local Scaleは1のままだった。最後にavatar rootをPosition 0、Rotation 0、Scale 1へ戻した。これは親子関係とlocal placementの部分観察であり、実アバター全周の貫通・見た目、衣装A→B更新、削除Undo、VRChat内表示を合格とする証拠ではない。

# 2026-09-14 実EditorWindow package受入の次タスク

Unity **2022.3.22f1**の実EditorWindowで、実カフpackage `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-ManualAcceptance-Cuff3-20260914\exports\clothing-20260914-042448-0333be\skinned-clothing.nyaforge.json`をWindowsのファイル選択から読み込み、package検証後に `RaddollV3 (Transform)` をAvatar rootへ指定できることを確認した。画面には **264 vertices / 256 triangles、171 bones** と表示され、stable BoneIdの割当欄が生成された。保存済み割当がない場合は警告が表示され、現状のGUIでは171本を明示的に1本ずつ割り当てない限り、診断・適用ボタンは有効にならない。これは安全な明示対応としては正しいが、実アバターでの初回作業量が大きいというUX上の課題を確認したもの。シーンは保存していない。

Windows v1の実装順を次のように固定する。

1. **受入を止めない最小作業**：まず既存の手動割当経路を、少数骨のカフfixtureで `保存→事前診断→適用→更新→削除→Undo` まで完了させる。実アバター全171本の完全割当は、候補UIなしでは手作業の受入対象にしない。
2. **次のUX実装**：package skeletonのstable BoneIdに対し、avatar配下のTransformから「名前・階層が一意に一致する候補」を表示する。候補はプレビューとして提示し、ユーザーの **一括承認** 後にだけ割当へ反映する。曖昧・欠落・avatar root外は未割当のまま残し、名前だけで無確認に適用しない。
3. **その後の実機受入**：候補承認後に実カフで適用し、avatar rootの移動・回転・scale、衣装A→B更新、削除・Undo、normal／MR／alpha／UV0の見た目を一周する。最後にVRChat Build & Testで表示・貫通・PhysBonesを確認する。

今回のEditorWindow操作はpackage読込とroot指定までの部分受入であり、171本の割当、実衣装の全周見た目、VRChat内表示を合格とは扱わない。自動fixtureの507 Core passとBridge passも、この手動受入の代替にはしない。

# 2026-09-14 stable BoneId候補表示の実装

受け取りGUIのstable bone欄へ、`候補を生成（名前・階層）` と `候補を割当に反映` を追加した。packageのBoneIdとavatar配下Transformについて、まず親子名から作った階層pathの一意一致を探し、見つからない場合だけTransform名の一意一致を候補にする。候補は一意なものだけを別辞書へ保持し、ユーザーが反映ボタンを押すまで現在の割当を変更しない。重複・欠落は件数を表示し、全割当が揃わない限り従来どおり保存・適用を有効化しない。これにより、名前だけの無確認自動装着を避けつつ、171本の初回割当を確認付きで短縮できる。

`NyaForge.UnityBridge.Editor` のソース構文はUnity Editor projectへ反映する対象として確認し、既存Core回帰は **507 passed / 0 failed**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-91513ffd9c2644168de5332754722304`）。候補表示の実Unity画面反映と171本一括割当後の適用は、Editorの再import後に手動受入する。現時点で実packageの読込・Avatar root指定までは確認済みだが、実アバター全周の見た目・貫通・VRChat内表示は未受入である。

# 2026-09-14 実EditorWindowでstable BoneId候補を反映

Unity **2022.3.22f1**のprivateプローブへ最新のEditor/Runtimeソースを反映し、全アセット再import後にC#コンパイルエラーが消えることを確認した（VRChat SDK由来の警告2件のみ）。実カフpackageをファイル選択から読み込み、`RaddollV3 (Transform)`をAvatar rootへ指定した。

`候補を生成（名前・階層）`を実行すると、packageの **171 bones** に対して **169/171本を一意候補として検出**し、残り2本は未検出のまま停止した。続けて`候補を割当に反映`を押すと、一覧のTransform欄へ一意候補だけが入り、既存の手動割当を上書きしないことを画面で確認した。候補生成・反映後も、保存・診断・適用の前に全割当確認を要求する表示が残る。

これは実Unity EditorWindowでの候補生成・明示反映の受入であり、169本の自動確定や残り2本の推測割当は行っていない。実衣装の適用、移動・回転・scale済みavatarでの全周見た目・貫通、衣装更新・削除Undo、VRChat Build & Testは引き続き未受入である。

# 2026-09-14 衣装packageの骨サブセット化

レビューで実カフpackageのskeletonが171本（うち `RightEye_HighLight` と `Goggles_2` は受け取りavatarに存在しない）となり、衣装が使わない骨まで初回割当を要求していた。ウェイトに実際に参照されるBoneIdとその祖先だけを残す `SkeletonBindingSubset.ForBinding` を追加し、選択衣装packageのGLB・skeleton sidecar・binding sidecarを同じ縮約骨格から生成するようにした。元の編集用graphと全身avatar骨格は変更しない。

`SkinBinding.RebindToSkeleton` とGLB出力のsubset overloadは、BoneIdでinverse-bind／joint-local行列を再対応させ、元の配列順をsubset順として誤読しない。Coreへ祖先保持・未使用骨除外・binding再構成の回帰を追加し、`dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore` は **508 passed / 0 failed**。

残りの受入は、修正版Playerで実カフpackageを再生成して骨数がウェイト参照＋祖先の必要本数になること、Unity EditorWindowで候補生成・適用・保存・衣装表示を一周すること。移動／回転／scale済みavatar、衣装A→B更新、UV0・normal・MRの実画素、VRChat Build & Testは別のWindows実機ゲートとして記録する。private probeと既存の旧packageは変更せず、public repositoryにはソースとテストだけを入れる。

上記のPlayerを `Builds/BoneSubsetV1/NyaForge.exe` としてビルドし、`Tools/Test-NyaForgeRealClothing.ps1` を実RadDollV3 VRMで再実行した。Player **93 checks PASS**、Unity Bridge **17 checks PASS**。生成packageは `Artifacts/Authoring-20260914-143044-d7954b6a327f477cad213eae519635fb/imported-accessory-skin-project/exports/clothing-20260914-053257-0a7daa/skinned-clothing.nyaforge.json`、skeleton sidecarは **2 bones**（Child／Root）で、GLB・sidecarの受け取りまで成功した。Unity EditorWindowの手動プローブは旧ライブラリと現行ソースの混在が残り、再起動後もコンパイルエラー表示が残ったため、このターンでは新packageの画面適用を合格扱いしない。実際の移動／回転／scale、衣装更新・削除Undo、UV0・normal・MR画素、VRChat Build & Testは引き続き未受入。

# 2026-09-14 骨サブセット診断の安全化と実モデル再確認

`SkeletonBindingSubset.ForBinding`が不正なBoneIdを受けた場合、辞書の内部例外ではなく`BONE_NOT_FOUND`として停止するようにし、親骨をキューで辿る実装へ整理した。欠損BoneIdの回帰を`Tests/Authoring.Core/RigTests.cs`へ追加した。

Coreは **508 passed / 0 failed**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-e57c356acde5482787b244e40832be3c`）。`Builds/BoneSubsetV2/NyaForge.exe`（commit `6ffac33`）のWindows Player buildも成功（`Logs/build-player-20260914-144859-507.log`）。

実RadDollV3 VRMで`Tools/Test-NyaForgeRealClothing.ps1`を再実行し、Player **93 checks PASS**、Unity **2022.3.22f1 Bridge PASS**を確認した。生成packageは`Artifacts/Authoring-20260914-144919-e0c7f7e6a61743a9a458df71ece8c34b/imported-accessory-skin-project/exports/clothing-20260914-055133-bd4246/skinned-clothing.nyaforge.json`、skeleton sidecarは **2 bones / 201 bytes**だった。package GLB・skeleton・bindingのhash検証とBridge受け渡しまで成功している。

`docs/Windows-v1-Manual-Acceptance.md`をBoneSubsetV2へ更新し、古い171本packageを使わず、候補生成→一覧確認→候補反映→未検出骨の手動確定という受け取り手順を明記した。実Unity EditorWindowの全周見た目・貫通、衣装A→B更新・削除Undo、normal／MR／UV0の実画素、VRChat Build & Testはまだ別の手動受入ゲートである。private probeの旧package／SDK混在はpublic repositoryへ取り込まない。


# 2026-09-14 実衣装受入スクリプトの骨sidecar検査

`Tools/Test-NyaForgeRealClothing.ps1`へ、Bridgeへ渡す前の`skeleton.nyaforge.bin`ヘッダー（NYRS、version 1、1〜256本）検査と骨数表示を追加した。古いpackageを誤って受入へ進めず、実モデル一周のログだけで必要骨数を確認できる。既存のmanifest／GLB／binding hash検査とUnity Bridge検証はそのまま維持する。

追加の実行結果: 骨sidecar検査を含む`Test-NyaForgeRealClothing.ps1`を再実行し、`Clothing skeleton: 2 bones`、Player PASS、Unity Bridge PASSを確認した。最新artifactは`Artifacts/Authoring-20260914-145454-d4fa2141c07c4d34b81bb68cd2bf2cca`、receiverは`Artifacts/BridgeReceiver-20260914-145739-335-6c3bd4bb63f54291946d18b73ac6d7bd`。

# 2026-09-14 Player衣装package subset受入を強化

Authoring suiteの実衣装package検査へ、現在の衣装graphから`SkeletonBindingSubset.ForBinding`を再計算し、package側skeleton hash・骨数・binding skeleton hashが一致することを追加した。これで不要骨の削減が実Player経路でも回帰する。

`Builds/BoneSubsetV3/NyaForge.exe`（build log `Logs/build-player-20260914-150122-506.log`）で実RadDollV3を再実行し、Player **93 checks PASS**。subset検査を含むcheck文字列を`Artifacts/Authoring-20260914-150145-eb5a4a651a244b0b9eee01b6106f4918/report.json`へ記録した。`Clothing skeleton: 2 bones`の事前検査後、Unity **2022.3.22f1 Bridge**もPASS（`Artifacts/BridgeReceiver-20260914-150422-168-20bf09b7f9aa48b2aa7d96cb52c872a0/bridge-report.json`）。

# 2026-09-14 現行BoneSubsetV3の指し先同期

`Builds/BoneSubsetV3/NyaForge.exe`（commit `17ae697`）をWindows v1の現行candidateとして固定した。Coreは **508 passed / 0 failed**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-e57c356acde5482787b244e40832be3c`）。privateの実RadDollV3 VRMでPlayer **93 checks PASS**、衣装packageのskeleton sidecarは **2 bones**（ウェイト参照＋祖先）となり、package側skeleton／binding hashの一致検査もPlayer内でPASSした。証跡は `Artifacts/Authoring-20260914-150145-eb5a4a651a244b0b9eee01b6106f4918/report.json`、Unity **2022.3.22f1** Bridgeは `Artifacts/BridgeReceiver-20260914-150422-168-20bf09b7f9aa48b2aa7d96cb52c872a0/bridge-report.json`。

`docs/Windows-v1-Development-Plan.md` と `docs/Windows-v1-Manual-Acceptance.md` のcandidate・証跡をこのV3へ同期した。次に実際に行うことは、Unity EditorWindowでV3 packageを読込み、候補生成→一覧確認→候補反映→残りがあれば手動確定→保存→診断→適用を一周すること。その後、avatarの移動／回転／scale、衣装A→B更新、削除Undo、normal／MR／UV0の見た目、VRChat Build & Testを確認する。自動Player／Bridge PASSはこの手動受入の代替にはしない。

# 2026-09-14 骨候補探索の共通化とBridge回帰

受け取りGUIのstable BoneId候補探索を`SkinnedClothingPackageWindow.FindUniqueBindingSuggestions`へ共通化し、avatar階層を1回収集して名前・階層候補を解決するようにした。候補生成の結果は従来どおり一意なものだけを保持し、曖昧・欠落は自動反映しない。Bridge batchにも同じresolverの回帰を追加し、実RadDollV3から生成した2骨packageを、階層候補が一意に全件解決できることを確認した。

Unity **2022.3.22f1** Bridgeを再実行し、適用・hash／sidecar・ownership・更新／削除Undo・semantic textureに加えて、`unique hierarchy candidates`を含む **PASS**。証跡は`Artifacts/BridgeReceiver-20260914-151117-020-6bdd730b5d17449e91066b33a45a9816/bridge-report.json`。これは候補resolverの自動回帰であり、実EditorWindowのマウス操作、実アバター全周の貫通・見た目、VRChat Build & Testの合格とは分けて扱う。

# 2026-09-14 候補resolver変更後のCore再確認

`SkinnedClothingPackageWindow`の候補resolver共通化後にCoreを再実行し、**508 passed / 0 failed**を確認した。artifactは`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-c0eae62745d14c7faab0ce1fadd318b4`。骨・skin・材質・UV0制限・衣装package・保存再開・Undo/Redoを含む既存回帰に変化はない。Unity Bridgeの候補resolver回帰は`Artifacts/BridgeReceiver-20260914-151117-020-6bdd730b5d17449e91066b33a45a9816/bridge-report.json`でPASS済み。実EditorWindowの手動適用、実アバター全周の貫通・見た目、VRChat Build & Testは未受入である。

# 2026-09-14 multi-instance画像payload共有の実モデル再確認

全mesh instance取込時に同じglTF imageがmeshごとに複製されないよう、1回の取込操作で使う`GlbImportImageCache`を追加した。公開APIの画像bytesは従来どおり防御コピーのまま、内部の不変encoded payloadだけをmesh間で共有する。Coreへ2つのmesh resourceが同一画像bytesを参照する回帰を追加し、**509 passed / 0 failed**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-0c1f59d41e1c41e4935cdf02239d9274`）。

`Builds/BoneSubsetV4/NyaForge.exe`でprivate実RadDollV3 VRMを再実行し、Player **93 checks PASS**、衣装skeleton **2 bones**、Unity **2022.3.22f1** Bridge PASSを確認した。証跡はPlayer `Artifacts/Authoring-20260914-151706-7104cbc828634b7b908141762f6a7604/report.json`、Bridge `Artifacts/BridgeReceiver-20260914-151939-562-a960367cb1c14d8a82878419d6aa0dda/bridge-report.json`。今回の共有は取込時の一時重複を減らす設計で、実モデルのheap削減量は同一条件の再計測が必要なため、軽量性の最終合格とは扱わない。

# 2026-09-14 画像payload共有後の実メモリ再計測

`Builds/BoneSubsetV4/NyaForge.exe`でprivate RadDollV3 VRMの全mesh取込＋Save/Open／GLB／VRM1／衣装package suiteを外部サンプリングした。Playerは`Artifacts/Authoring-20260914-152215-d7a276d57e324107b92aed4695ce22e8/report.json`でPASSしたが、working set peak **3,667MB**、private bytes peak **4,492.3MB**（30 samples）だった。前回の観測値と条件・OS状態が完全一致しないため共有キャッシュの削減量は断定せず、全mesh一括取込の軽量性は未達として扱う。通常の1候補編集を全mesh経路から分離すること、取込中のgraph／Unity評価キャッシュを段階化することを次の性能課題にする。public repositoryへprivate素材は追加していない。

# 2026-09-14 全mesh取込の二重読込除去

全mesh command-line検証で、inventory確認後に同じVRMを`ImportAllModelInstances`が再読込・再parseしていた経路を修正した。既に読み込んだbytesと`GlbDocument`を再利用する内部overloadを追加し、通常GUIの単一読込契約も維持した。`Builds/BoneSubsetV5/NyaForge.exe`のビルドは成功し、private実RadDollV3 VRMのPlayer **93 checks PASS**、衣装skeleton **2 bones**、Unity **2022.3.22f1** Bridge **PASS**を確認した。証跡はPlayer `Artifacts/Authoring-20260914-152725-f668eee74c7f41a08712e014c37c2619/report.json`、Bridge `Artifacts/BridgeReceiver-20260914-153001-201-d4c23163fb89454a8c9ad0bcfaf411a9/bridge-report.json`。二重読込を除いた同一条件のheap比較は未実施のため、軽量性の数値合格とは扱わない。

# 2026-09-14 開発手順の常設記録とWindows DPI表示確認

忘れやすい開発手順を`AGENTS.md`へ常設した。対象リポジトリ・branch・remoteを作業開始時に確認し、実装と未完了の受入条件を`current_task.md`へ記録する。Core／Player／Bridgeの自動結果、実マウスの画面確認、実アバター・VRChat確認を別証跡として扱い、公開commit前には`private/`・`Builds/`・`Artifacts/`などの境界を確認する。区切りごとに`git diff --check`、対象テスト、commit後の`git ls-remote origin refs/heads/main`を確認する。

`Assets/NyaForge/UnityRuntime/AuthoringWorkbench.cs`のWindows DPI bounds計算を、PanelSettingsのscaleに対して一度だけ論理幅・高さへ変換するよう修正した。`Builds/DpiFixV1/NyaForge.exe`をUnity **6000.4.3f1**でビルドし、Windowsネイティブ`@oai/sky`の最大化ウィンドウ（2560x1440）で制作画面を確認した。右側の操作パネルと広いviewportがウィンドウ全体に配置され、ボタンとラベルが読める状態になった。今回のDpiFixV1画面は空プロジェクトでのレイアウト確認であり、RadDollV3のモデル表示・実マウスでの衣装操作・実アバター全周・VRChat内表示の合格とは扱わない。既存V16のRadDollV3取込証跡は別記録として維持する。

# 2026-09-14 BoneSubsetV5実RadDollV3一周再確認

`Builds/BoneSubsetV5/NyaForge.exe`でprivateの`RadDollV3_VRM.vrm`を再実行した。Playerは **93 checks PASS**、衣装skeleton sidecarは **2 bones**。Unity **2022.3.22f1** Bridgeは **16 checks / status passed**で、manifest・GLB・skeleton・bindingのhash、stable BoneId、hierarchy候補、ApplyPackageによるscene生成、更新・削除Undo、semantic normal／MR channel変換を確認した。Player証跡は`Artifacts/Authoring-20260914-174539-796a31b3f43e4ef7b93aabdab939eaff/report.json`、Bridge証跡は`Artifacts/BridgeReceiver-20260914-174815-172-2d6ee338b1084d9899b9b08abc8fc44f/bridge-report.json`、package manifestは`Artifacts/Authoring-20260914-174539-796a31b3f43e4ef7b93aabdab939eaff/imported-accessory-skin-project/exports/clothing-20260914-084752-eaeadb/skinned-clothing.nyaforge.json`にある。これは自動Player／Bridgeの再確認であり、実EditorWindowのマウスによる全周見た目・貫通、移動／回転／scale済み実avatar、VRChat Build & Testの合格とは扱わない。

# 2026-09-14 Unity 2022.3受け取りprobeのAPI互換修正

受け取り用のUnity **2022.3.22f1**で`ViewerApp.RevisionAcceptance`だけがUnity 6の`FindObjectsByType<T>(FindObjectsInactive)` overloadを直接参照していたため、private probeがSafe Modeへ入っていた。両バージョンにある`FindObjectsOfType<T>(true)`へ置き換え、Unity 6000.4.3f1の`Builds/UnityCompatV1/NyaForge.exe`を再ビルドした。private `PhysBonesSdkProbe-20260914`へ同ソースを反映して再起動し、コンパイルエラーなしで`RadDollV3ClothingProbe` sceneが通常起動することを確認した（Unity Editor画面でRadDollV3階層とモデルを表示）。これは受け取り環境のcompile／scene起動互換性の確認であり、衣装の実EditorWindow適用、全周の見た目・貫通、VRChat Build & Testの合格とは扱わない。

# 2026-09-14 Computer Useの操作経路をAGENTSへ固定

ネイティブWindows画面をブラウザ用`cua`で確認して`apps: []`と誤判定しないよう、`AGENTS.md`に操作経路の選択基準を明文化した。NyaForge・Unity・ファイルダイアログは`mcp__node_repl__js`の`@oai/sky`、Webページは`mcp__cua_repl`を使う。`@oai/sky`操作は対象windowを再取得してから実行し、クリック・入力・ドラッグの直後に新しいwindow stateを取得する。この記録は手動操作の再現手順であり、NyaForgeの自動テストやVRChat実機受入の結果ではない。

# 2026-09-14 Windows v1残タスクの優先順位整理

現時点でCore／Player／Unity Bridgeの主要な自動経路は実装・回帰が進んでいるため、次の開発は機能を無制限に増やすのではなく、実RadDollV3を使った出荷前の一周を完成させる。自動テストのPASSは、実EditorWindowのマウス操作、実アバターの全周見た目・貫通、VRChat内表示の受入とは分けて扱う。

## Windows v1で先に完了させるもの

1. **実モデルの衣装一周**: RadDollV3へカフ／チョーカーを読み込み、頂点・UV・材質・ウェイトを編集し、保存→再読込→GLB／衣装package出力まで確認する。
2. **Unity Bridge実運用**: 受け取り先アバターを移動・回転・拡縮した状態で適用する。A→B更新、削除、Undo、再適用を行い、重複生成やユーザー所有部品の破壊がないことを確認する。
3. **VRChat確認**: PC向けBuild & Testで、衣装の貫通、全周の見た目、PhysBones、シェーダー、性能ランクを確認する。
4. **材質の実画面確認**: Base Color・透明・Normal・Metallic/Roughness・sampler・材質slot・Unity Bridge側係数を実GPU画面で確認する。データhashだけでは合格にしない。
5. **MCP実接続**: GUIから接続開始し、`get_state`、編集、撮影、出力、Undo、再接続までをsidecar経由で一周する。
6. **Windows安定性・出荷記録**: DPI 100/150/200%、日本語IME、空白入りパス、長時間編集、複数回更新、別Windows環境を確認し、最終候補のビルド番号・制限事項・証跡を整える。

## v1後へ回すもの

FBX／BLEND直接取込、UV1や追加テクスチャ、完全自動fit・貫通修正、全VRM仕様、ポーズ／モーフretarget、macOS／Quest、Blender完全代替の編集機能は、Windows v1の受入を完了してから扱う。現状の自動fixtureはこれらを完了扱いにしない。

次の着手単位は、**実RadDollV3で「編集→保存→Unity適用→更新→削除／Undo→VRChat Build & Test」**を通し、そこで見つかった不具合だけを修正する。この一周が終わるまで、追加の大規模機能は保留する。

# 2026-09-14 シェイプキー対象メッシュの常時表示

シェイプキー欄の対象`rendererId`がtooltipだけではWindows／UI Toolkitの環境によって確認できなかったため、各シェイプキー名の下へ`対象メッシュ: displayName · ID: rendererId`を常時表示するようにした。displayNameがない場合はpath、pathもない場合はrendererIdを表示し、tooltipにはrendererIdとpathを残す。「自動」は従来どおりAI推定ではなく、そのキーの初期値へ戻す操作である。

- 修正ソース: `Assets/Viewer/Runtime/ViewerApp.UI.cs`, `Assets/Resources/Viewer.uss`
- Player build: `Builds/MorphTargetV1/NyaForge.exe`（Unity 6000.4.3f1）
- ビルド: **成功**（`Logs/build-player-20260914-184311-059.log`）
- 既存の未コミット変更は保持し、Core／Bridgeの仕様や保存形式は変更していない。
- 実モデルでの各キーと身体・衣装メッシュの意味確認は、表示名を見ながら実RadDollV3で行う手動受入として残す。
# 2026-09-14 MOD-05: 装着Panel状態の分離

小物装着のUIハンドル、候補ID、fit検査の一時結果を`AuthoringWorkbench.AttachmentState.cs`へ移した。`AuthoringWorkbench.Attachments.cs`は装着Panelの構築・候補解決・fit／weight操作を担当し、状態の所有場所を見つけやすくした。保存対象の装着設定は引き続きgraphを正本とし、partial間で永続データを複製していない。

- 追加ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.AttachmentState.cs`（`.meta`を含む）
- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Attachments.cs`
- Player build: `Builds/GuiModularV38/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-220945-854.log`）
- Core回帰: **512 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-e00f12baa3cb4e5fa27777ee016302d3`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-221017-4c39dc2beba94ed3a7fc93c6f050d48d/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-221059-ceba5ca227324dd9ad4abaf377804560/report.json`）
- 残り: metadata再同期とPanel購読の細分化、検証harness整理、実マウスのExplorer／DPI／IME／長い名称、実RadDollV3全周fit・貫通・見た目、Unity／VRChat実機受入。

# 2026-09-14 MOD-05: fit検査DTOの分離

装着fit検査のUI／MCP間で使う一時結果を`AttachmentSurfaceFitMeasurement.cs`へ移した。計測DTOはgraph・Workbench・sceneを参照せず、装着処理から表示・inspectionへ値だけを渡す。保存形式やfit計算は変更していない。

- 追加ソース: `Assets/NyaForge/UnityRuntime/AttachmentSurfaceFitMeasurement.cs`（`.meta`を含む）
- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Attachments.cs`
- Player build: `Builds/GuiModularV39/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-221408-311.log`）
- Core回帰: **512 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-7607191d233d433eb723deb7d37dfb1a`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-221437-cac2435e9aa140e6b5a06220fd1f3dda/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-221437-e1c839cec331458996e5a7fae1f22032/report.json`）

# 2026-09-14 MOD-05: 出力用source-skin変換の分離

GLB／VRM出力時のinstance transform、inverse-bindのBoneId並べ替え、joint local transform生成を`AuthoringWorkbench.ExportTransforms.cs`へ移した。`ProjectActions.cs`は保存・出力操作の入口に集中し、出力用の座標変換とstable BoneId対応を一つのpartialで追えるようにした。出力service、保存形式、MCP wireの挙動は変更していない。

- 追加ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ExportTransforms.cs`（`.meta`を含む）
- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ProjectActions.cs`
- Player build: `Builds/GuiModularV40/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-221750-307.log`）
- Core回帰: **512 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-7607191d233d433eb723deb7d37dfb1a`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-221819-406c1f61cfca4288854324011fd1b175/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-221819-e68fc69bfcd246e0b952d2e85e3f3142/report.json`）
- 実RadDollV3自動一周: **PASS**（取込→EditMesh→native Save/Open→GLB／VRM→衣装package、`Artifacts/Authoring-20260914-221858-6a41535b33b741438ff6b62ee1aecbd7/report.json`）
- Unity Bridge 2022.3.22f1: **PASS**（`Artifacts/BridgeReceiver-20260914-222120-240-d237c9620a6c492693254eb094cc30ba/bridge-report.json`）

# 2026-09-14 MOD-05: 装着Panel構築の分離

小物装着PanelのUI構築を`AuthoringWorkbench.AttachmentUi.cs`へ移した。`AuthoringWorkbench.Attachments.cs`は装着候補の解決、適用、fit、weight移行に集中し、入力欄・tooltip・ボタンの配置は専用ファイルで追えるようにした。イベントは既存の操作メソッドへ接続し、保存形式・MCP wire・実行順は変更していない。

- 追加ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.AttachmentUi.cs`（`.meta`を含む）
- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Attachments.cs`
- V41 Player／Authoring／Navigation: **PASS**（`Builds/GuiModularV41/NyaForge.exe`、`Logs/build-player-20260914-222355-111.log`、`Artifacts/Authoring-20260914-222547-11d4698ecb0842a19ab5e8f5da291fd4/report.json`、`Artifacts/Navigation-20260914-222547-d5a5ddc1e28d456792b305948649bdb4/report.json`）

# 2026-09-14 FORMAT: Viewer pack pointerとAuthoring正本の境界

`GeneratedPacks/NyaForgeFixture/current.StandaloneWindows64.json`の実体を確認した。これは`schemaVersion`、`packId`、`buildTarget`、revision manifest path、manifest hashだけを持つWindows確認パック用pointerで、制作データの正本ではない。制作正本はAuthoring保存先の`project.nyaforge.json`＋`blobs/`＋許可されたattachmentであることをQuickstartへ明記した。

- 確認ファイル: `GeneratedPacks/NyaForgeFixture/current.StandaloneWindows64.json`
- V41 Player build: `Builds/GuiModularV41/NyaForge.exe`（`Logs/build-player-20260914-222355-111.log`）

# 2026-09-14 GUI-08 / MOD-05: 空状態Panelの可視性境界

空の制作projectで装着・編集・出力Panelまで常時表示されるため導線が長くなる問題へ、`AuthoringWorkbench.ContextVisibility.cs`を追加した。空状態では制作対象・装着・graph詳細・確認・出力を隠し、基本形状と上部のモデル追加／保存入口を残す。モデル取込commandからは取込Panelを明示表示し、候補確認中の状態を隠さない。Panelは破棄せずdisplayだけを切り替えるため、Foldout値・callback・保存形式・MCP wireは保持する。

- 追加ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ContextVisibility.cs`（`.meta`を含む）
- 接続変更: `AuthoringWorkbench.RefreshState.cs`, `AuthoringWorkbench.CommandBar.cs`, `AuthoringWorkbench.Import.cs`
- V42では既存の低レベルUI検証が空projectの非表示Panel内ボタンを直接probeして失敗したため、通常UIの空状態整理は維持しつつ、`--authoring-check-output`／`--navigation-check-output`起動時だけ検証harness向けにPanelを表示する互換境界を追加した。
- V43 Player build: `Builds/GuiModularV43/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-223334-009.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-223359-dee1a65290924bc6a8151ef7be7996d5/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260914-223431-e29ec051bc954cfa98201a35af2cf5cb/report.json`）
- Unity Bridge 2022.3.22f1: **PASS**（`Artifacts/BridgeReceiver-20260914-223704-283-6bd642790ec74f59a076e819958ada64/bridge-report.json`）
- 未実施: 実マウスのDPI／IME／長い名称、実RadDollV3全周fit・貫通・見た目、Unity／VRChat実機受入。
V43で確認セットの反復起動・終了を50回実行し、全サイクル**PASS**だった。集約レポートは`Artifacts/Navigation-Repeated-GuiModularV43-20260914-223809.json`。これは同一Windows環境の自動反復証拠であり、2時間編集・別PC・実マウスの手動受入を置き換えない。

V43でprivate一時RadDollV3 VRM（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`）を使った実モデル衣装一周もPASSした。取込→全mesh→EditMesh→native Save/Open→GLB／VRM出力→衣装package生成を`Artifacts/Authoring-20260914-224132-8cc3399ab32540f3ab4a0bd97a52b81a/report.json`で確認し、生成package（skeleton 2 bones）のUnity 2022.3.22f1 Bridge受け取りも`Artifacts/BridgeReceiver-20260914-224630-112-225ee94c5ae144e9880c57e7149381b5/bridge-report.json`でPASSだった。private素材は公開ツリーへコピーしていない。これは自動Player／Bridge経路の証拠で、実EditorWindowのマウス操作、全周fit・貫通・見た目、VRChat内表示、販売品質は未受入のまま残る。

最新ソースを標準起動先へ反映するため、`Builds/Windows/NyaForge.exe`をV43相当のソースから再ビルドし、`Tools/Start-NyaForgeAuthoring.ps1`の既定BuildNameを`Windows`へ揃えた。個別の検証版は`-BuildName GuiModularV43`のように分離して保持する。

標準起動先`Builds/Windows/NyaForge.exe`でもNavigation回帰を再実行し、**PASS**を確認した。証拠は`Artifacts/Navigation-20260914-224954-eeccbc168d4a4534ab30a3a351d92f9b/report.json`。これは既定バイナリの起動・パック読込・確認セット導線の自動確認で、実マウスのDPI／IME／長い名称は手動受入へ残す。

標準起動先`Builds/Windows/NyaForge.exe`でAuthoring全回帰も再実行し、**PASS**を確認した。証拠は`Artifacts/Authoring-20260914-225318-2809f7ddf6ae40c1a94ecd7ba2e8b021/report.json`。検証版V43と同じソース経路で、保存・再開・頂点編集・材質・衣装package・MCP・出力の自動チェックを通過している。
# 2026-09-14 FORMAT: ポインターと実体の運用方針

確認パックは小さな`current.<buildTarget>.json`ポインターと、revision配下のmanifest／blob実体を併用する。ポインターは現在採用revision・相対manifest path・hashを示す入口として共有・切替に使い、制作正本やバックアップの代わりにはしない。実体manifestはポインターなしでも直接開ける。Authoring作品は`project.nyaforge.json`、`blobs/`、許可されたattachmentを一組で保存する。READMEとQuickstartへこの境界を記載した。

- 対象: `GeneratedPacks/NyaForgeFixture/current.StandaloneWindows64.json`
- 検証: ポインターが`manifestPath`と`manifestSha256`を持ち、同じrevision実体へ解決することを確認
- 残り: ポインター欠損・実体欠損・hash不一致をGUIで診断する手動受入

ポインターの実体対応を確認する`Tools/Test-NyaForgePackPointer.ps1`を追加した。相対manifest pathのroot脱出、必須欄／Windows target、実体欠損、SHA-256、packId／revision一致を検証し、検証済みの絶対パスとrevisionをJSONで返す。GUIのエラー表示と、ポインター欠損・実体欠損・hash不一致の手動受入は残る。

# 2026-09-14 NF-V1-02A: Unity実VRChat PhysBones SDK probe再確認

private `PhysBonesSdkProbe-20260914` を Unity **2022.3.22f1** で再実行し、`private/PhysBonesSdkProbe-20260914/sdk-probe-report-latest.json` の `status: verified`、Unity probe `passed` を確認した。実SDKの `VRCPhysBone` runtime type解決、root／endpoint／exclusions／branches／colliders／limits／interaction／parameter capability、target packageの非変更preflight、stable BoneId mappingによる実コンポーネント生成・設定が全て通っている。Unity側の一時artifactは `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-PhysBonesSdkProbe-69ac3623abad428aa554d5fca4beec03/report.json` と同probeの `unity.log` に出力された。

これはSDK型解決・コンポーネント生成・安定骨対応の証拠であり、実RadDollV3全sceneの衣装適用、EditorWindowの実マウス操作、全周の見た目・貫通、Build & Test、VRChatクライアント内表示の合格とは扱わない。private project／SDK DLL／avatar素材／ログは公開ツリーへ追加していない。`docs/Windows-v1-Environment.md`にも同じ境界を追記した。

# 2026-09-14 FORMAT: ポインターSHA-256表記の互換性

Viewerのポインター検証で、SHA-256の16進表記を大文字・小文字のどちらでも受け付けるようにした。`PackStore.Verify`の実体hash比較と`VerifyUpdatePointer`の形式検査を同じcase-insensitive契約へ揃え、別ツールが大文字で書いた`manifestSha256`でも、パス・実体・pack／revision一致が通れば開けるようにした。ポインターのroot脱出や実体欠損を緩めた変更ではない。

Coreは **512 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b632a3e57fec4c28968a1d4c69d9e1b3`）。`Builds/Windows/NyaForge.exe`もUnity **6000.4.3f1**で再ビルド成功（`Logs/build-player-20260914-230023-236.log`）。`Tools/Test-NyaForgePackPointer.ps1`は公開fixtureの通常表記と、一時コピーで`manifestSha256`を大文字化したポインターの両方で`status: passed`を確認した。大文字化fixtureは`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-PointerCase-bb79689a0ce04236bf3185c7954411a3`に作成した一時検証用で、公開ツリーへ追加していない。

# 2026-09-14 MOD-06: 基本形状preset catalogの分離

チョーカー／手首カフの表示名、既定寸法、説明、生成callbackを`AuthoringWorkbench.ShapePresetCatalog.cs`へ分離した。`ShapeCreation.cs`はpreset一覧からDropdownと入力欄を組み立て、選択presetへ生成を委譲する。index値の条件分岐をUI本体から除き、形状追加時にパネルへ個別のハードコードを増やさない構造にした。既存の検証用template IDと`PolygonPrimitives`の生成処理は維持している。

- 追加ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ShapePresetCatalog.cs`（`.meta`を含む）
- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ShapeCreation.cs`
- コードマップ: `docs/Authoring-Code-Map.md`の基本形状責務を更新
- Player build: `Builds/ShapeCatalogV1/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260914-230415-390.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260914-230444-491be0a412804e78b3c87e15cc904153/report.json`）。チョーカー／カフの既存生成、頂点編集、Save/Open、Bake、作業モード導線を確認した。
- 実EditorWindowのマウスによる形状選択・実RadDollV3への適用・全周見た目／貫通・VRChat内表示は未受入のまま残る。

# 2026-09-14 GUI-09: 詳細Panelの初期展開を整理

通常の制作画面では、`ノード・編集段（詳細）` と `保存とUnityへの受け渡し` を初期折りたたみに変更した。保存は上部command barから直ちに使え、詳細ノード・出力項目は作業モード `形状編集`／`確認・出力` から必要なときだけ開く。`--authoring-check-output`／`--navigation-check-output`では従来どおりPanelを展開し、既存のpointer検証IDを維持する。形状カタログと同様に、ユーザー導線と検証compatibilityを分離した。

- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.GraphEditing.cs`, `AuthoringWorkbench.ProjectOutput.cs`
- Player回帰で通常起動の初期画面と自動検証Panelの両方を確認する。

`Builds/GuiNavigationV1/NyaForge.exe`（Unity **6000.4.3f1**、`Logs/build-player-20260914-230734-279.log`）をビルドし、Authoring **PASS**（`Artifacts/Authoring-20260914-230756-7f2f146bcf804aecb7f17c81e522da0e/report.json`）とNavigation **PASS**（`Artifacts/Navigation-20260914-230823-1546d7fc3b0c494da2e18505ad0c3453/report.json`）を確認した。自動検証はcompatibility分岐で詳細Panelを展開するため、通常起動時の折りたたみ状態はコード設定による確認として、実マウスのDPI／IME／長い名称と合わせて手動受入へ残す。

# 2026-09-14 GUI-09補正: NavigationのDPI probe引数を統一

Navigation検証の実引数は`--navigation-check-output`なのに、Viewer／Authoring双方のPanelSettings DPI判定が旧`--navigation-check`だけを見ていた。判定を実際の出力引数へ揃え、Navigationのpointer座標もAuthoringと同じ1:1 probe契約で扱うようにした。通常Windows起動時のDPI補正は変更していない。

`Builds/DpiProbeV2/NyaForge.exe`（Unity **6000.4.3f1**、`Logs/build-player-20260914-230955-386.log`）を再ビルドし、Navigation **PASS**（`Artifacts/Navigation-20260914-231017-60c00d9bd01b47fcb7be83615487e2d3/report.json`）、Authoring **PASS**（`Artifacts/Authoring-20260914-231021-53d734ecdcd24241a6eacdbcdebf2d9b/report.json`）を確認した。

# 2026-09-14 GUI-10: ポインターから実体manifestへの表示

Viewerの設定パネルへ`pack-source-info`を追加し、現在の入口がポインター・確認セット・実体manifestのどれか、実際に読み込んだmanifestのファイル名・revision・完全pathを表示するようにした。保存済みセットを開いた場合も、入口と実体を分けて表示する。ポインターの検証・hash・読み込み処理は既存経路を再利用し、保存形式は変更していない。Navigation回帰へ入口→実体表示の確認を追加した。

`Builds/SourceInfoV1/NyaForge.exe`（Unity **6000.4.3f1**、`Logs/build-player-20260914-231206-990.log`）でNavigation **PASS**（`Artifacts/Navigation-20260914-231227-80feff88b9cb400dbb3adb1998e38b3f/report.json`）を確認し、`settings shows pointer-to-manifest source mapping`が通過した。Authoringも **PASS**（`Artifacts/Authoring-20260914-231231-e269782106c04638b8933d118c3425be/report.json`）。実マウスでの表示確認とDPI／IME／長いpathの手動受入は残る。

# 2026-09-14 GUI-09検証: 通常Authoring起動probeを追加

自動検証時だけPanelを展開するcompatibility分岐と、通常起動時の折りたたみ状態を混同しないよう、`AuthoringWorkbench.RunStartupProbe`と`Tools/Test-NyaForgeAuthoringStartup.ps1`を追加した。通常の空project起動で、graph詳細・保存出力Panelが閉じ、基本形状・上部の保存／形状ボタンが見えることをPlayer自身が確認し、screenshot／reportを保存する。これは実マウス、DPI／IME、実モデル、VRChatの受入とは分ける。

`Builds/StartupProbeV2/NyaForge.exe`（Unity **6000.4.3f1**、`Logs/build-player-20260914-232327-160.log`）をビルドし、通常起動probeを**PASS**で確認した。証拠は`Artifacts/AuthoringStartup-20260914-232348-2b18395db4e244049ecb51ce5e8c660c/report.json`と`authoring-startup.png`。`graphDetailsExpanded=false`、`projectOutputExpanded=false`、`shapeCreationExpanded=true`、保存／形状ボタン表示、画面サイズ1600×1000、スクリーンショット非黒を確認した。probeは非表示起動では黒画像になり得るため、確認時はPlayerを通常ウィンドウで起動し、画像の可視ピクセルも検査する。

# 2026-09-14 NF-V1-実モデル一周: 最新候補で再確認

`Builds/FinalCandidateV1/NyaForge.exe`（Unity **6000.4.3f1**、`Logs/build-player-20260914-232731-680.log`）をビルドし、Core回帰を**512 passed / 0 failed**で確認した。privateの`RadDollV3_VRM.vrm`を`Tools/Test-NyaForgeRealClothing.ps1`へ渡し、Playerの取込→全mesh→EditMesh→native Save/Open→GLB／VRM出力→衣装package生成をPASS、skeleton sidecar **2 bones**を確認した。証拠は`Artifacts/Authoring-20260914-232748-6f8ef341ef1e48198c27134dd1ba2f97/report.json`。Unity **2022.3.22f1** Bridgeの受け取りもPASSで、`Artifacts/BridgeReceiver-20260914-233013-035-58f88a9d96a741249bbd9c382429df9/bridge-report.json`に記録した。privateモデルbytesは公開ツリーへ追加していない。

この結果は自動Player／Bridge経路の再確認であり、実EditorWindowのマウス操作、移動・回転・scale済み実avatar、全周の見た目・貫通、VRChat Build & Test／クライアント内表示、販売品質の受入とは分ける。次の手動受入は`docs/Windows-v1-Manual-Acceptance.md`の1〜4節に従う。

# 2026-09-15 MANUAL-01: Windows実ウィンドウの基本操作probe

Computer UseのWindows用`@oai/sky`で`Builds/FinalCandidateV1/NyaForge.exe`を一意に選択し、実ウィンドウを操作した。Viewerから「制作へ」へ移動し、空の制作projectで「基本形状を追加」→リング形状作成→viewport上の頂点クリック選択→ホイール拡大→ドラッグ回転→Undoを一周した。選択頂点のハイライト、形状表示、回転後の見え方、Undo後の空状態、右側Panelのスクロールを目視確認した。終了時の未保存確認は勝手に破棄せずキャンセルし、検証用Playerはプロセスを停止した。

このprobeは実マウス相当の基本導線を確認したものだが、ファイルExplorer選択、日本語IME、DPI 150/200%、実RadDollV3のfit・貫通・材質・全周、移動／回転／scale済みavatar、VRChat Build & Testは未受入のまま残る。自動Player／BridgeのPASSや空projectの目視を、実モデル・販売品質の合格へ読み替えない。

# 2026-09-15 GUI-11: GLB／VRM候補選択欄の可読性

実ウィンドウのモデル取込で、候補欄が右パネル内の横並びになり、node・mesh・skinの値が短く切れていた。候補欄を`node（配置）`／`mesh（形状）`／`skin（骨・weight）`として縦積みにし、パネル幅いっぱいへ配置した。popupの候補文字列はnode index・mesh index・skin indexと短い名前に絞り、候補確認ステータスには完全名、primitive数、共有状況、source hashを残す。長い名前でUnityが横スクロールを出さないよう、一定長を超える表示名は候補欄だけ省略する。取込処理、保存形式、MCP wire、mesh／skinの選択値は変更していない。

- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ImportSelection.cs`, `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Import.cs`, `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.VrmImportVerification.cs`, `Assets/Resources/Viewer.uss`
- Quickstart／手動受入: `docs/Authoring-Quickstart.md`, `docs/Windows-v1-Manual-Acceptance.md`
- Player build: `Builds/ImportSelectionReadableV4/NyaForge.exe`（Unity **6000.4.3f1**、`Logs/build-player-20260914-235920-065.log`）
- Core回帰: **512 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-be90eb5c2cfc4f598be7c2b9dde24d07`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260915-000402-5128c527f5ba429ab8e58d25081dd5f0/report.json`）。候補inventory、mesh／skin／node選択、複数rig sessionに加え、3欄の可読性class／縦積みをPlayer内で検査した。初回180秒実行はタイムアウトしたため、600秒上限で再実行してPASSを確認した。
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260915-000338-fe0d939135f84eb696b695aff57ff506/report.json`）。標準Viewer起動、pack読込、確認セット導線、pointer→manifest表示を確認した。
- 実ウィンドウ目視: `@oai/sky`でV2を起動し、候補確認後の3欄を確認。ラベルは全幅で読め、選択値は2行へ折り返せた。V3でのpopup最終目視はファイルpicker座標が安定せず完走できなかったため、V4では短いindex／名前／m-s表記と横スクロール抑制を自動回帰で確認した。実モデルはprivateの合成smoke VRMを使ったため、全身の見た目・fit・貫通受入とは扱わない。
- 残り: DPI 150/200%、日本語IME、長い実パス、実RadDollV3全周fit・貫通・材質、Unity／VRChat実機は手動受入表へ残る。

# 2026-09-15 NF-V1-実モデル一周: GUI-11後の再確認

GUI-11の最新V4 Playerでprivateの`RadDollV3_VRM.vrm`を再実行し、実モデルの取込→全mesh候補→編集→native Save/Open→GLB／VRM出力→衣装package生成を**PASS**で確認した。Player reportは`Artifacts/Authoring-20260915-000854-8a1e3e5b8aa041c382b52d32839d1303/report.json`、生成した衣装skeleton sidecarは2 bones。生成packageをUnity **2022.3.22f1**の隔離Bridgeへ渡し、manifest／GLB／skeleton／binding hash、stable BoneId、SkinnedMeshRenderer生成、semantic材質変換を含む受け取りも**PASS**（`Artifacts/BridgeReceiver-20260915-001401-193-671cf5d1fd4c4e099cbe42a9b6aa6b9d/bridge-report.json`）。

これはprivate実モデルの自動Player／Bridge経路の証拠であり、実EditorWindowのマウスによる全周fit・貫通・材質見た目、移動／回転／scale済みavatar、VRChat Build & Test／クライアント表示の合格へは読み替えない。private素材とSDKは公開ツリーへ追加していない。

# 2026-09-15 GUI-12: 取込アクション文言の短縮

実ウィンドウで長い取込ボタンの末尾が切れるため、`このGLB / VRMをgraph objectへ取り込む`を`選択候補を取り込む`へ、`このファイルの全mesh instanceを取り込む`を`全meshをまとめて取り込む`へ短縮した。取込処理・保存形式・MCP wireは変更していない。案内文とPlayer検証も同じ呼称へ同期した。

- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Import.cs`, `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ModelImportGuidance.cs`, `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.VrmImportVerification.cs`, `docs/Authoring-Quickstart.md`
- Player build: `Builds/ImportActionsReadableV1/NyaForge.exe`（Unity **6000.4.3f1**、`Logs/build-player-20260915-002138-057.log`）
- Core回帰: **512 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-cf7d142174a94026b93c75ab0564e17b`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260915-002200-90321977e7c84c258db67811cb4b82e3/report.json`）。3つの候補欄の可読性と短い取込アクション文言をPlayer内で検査した。
- 実ウィンドウ: V4でWindows ExplorerのGLB／VRM picker起動、private RadDollV3 path入力、候補inventory表示、node／mesh／skin縦積みと短いpopup候補を確認済み。新しい短縮ボタンの自動確認は上記Player回帰で行った。

# 2026-09-15 NF-V1-実モデル一周: GUI-12現行ビルド

GUI-12で取込アクション文言を短縮した現行`ImportActionsReadableV1` Playerへ、privateの`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-RealModelSmoke\RadDollV3_VRM.vrm`を再投入した。実モデルの取込→全mesh候補→EditMesh→native Save/Open→GLB／VRM出力→衣装package生成を**PASS**で確認した。Player reportは`Artifacts/Authoring-20260915-002910-f27cf09eb46e40198167d52ef290c0f5/report.json`、生成packageは`Artifacts/Authoring-20260915-002910-f27cf09eb46e40198167d52ef290c0f5/imported-accessory-skin-project/exports/clothing-20260914-153112-404e4b/skinned-clothing.nyaforge.json`、clothing skeleton sidecarは2 bonesだった。生成packageをUnity **2022.3.22f1**の隔離Bridgeへ渡し、manifest／GLB／skeleton／binding hash、stable BoneId、SkinnedMeshRenderer生成、semantic材質変換を含む受け取りも**PASS**（`Artifacts/BridgeReceiver-20260915-003406-911-ef6d444e19814f24b464905a15fe228d/bridge-report.json`）。

これはGUI-12後のprivate実モデル自動Player／Bridge経路の再確認であり、実EditorWindowの全周fit・貫通・材質見た目、移動／回転／scale済みavatar、VRChat Build & Test／クライアント表示、販売品質の合格へは読み替えない。private素材とSDKは公開ツリーへ追加していない。次は手動受入表の実Unity EditorWindow経路とVRChat Build & Testを進める。

# 2026-09-15 MANUAL-02: 現行Playerの実ウィンドウ部分受入

`Builds/ImportActionsReadableV1/NyaForge.exe`をWindows native Computer Use（`@oai/sky`）で起動し、ExplorerのGLB／VRM pickerからprivateの`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-RealModelSmoke\RadDollV3_VRM.vrm`を指定した。候補確認後、縦積みの`node（配置）`／`mesh（形状）`／`skin（骨・weight）`欄、完全path表示、`全meshをまとめて取り込む`を実操作で確認した。全mesh取込後のstatusは`GLB / VRMの全mesh instanceを取り込みました。10 objects`。画面を最大化して10個のスキンモデル一覧、制作対象切替、`メッシュ全体を表示`導線を確認した。

body候補（object ID先頭`12707472`）を選択し、`選択中を参照として保護（編集不可）`を有効化した。status `選択中のobjectを参照として保護しました。編集操作は停止します。`を確認し、viewportクリック後も編集停止状態が維持された。これは実ウィンドウのExplorer／候補／全mesh／対象切替／参照保護の部分受入であり、保存／再開、実衣装の頂点編集・fit・貫通、移動／回転／scale済みavatar、Unity更新／削除Undo、normal／MR／UV0画素、VRChat Build & Testは未受入のまま残る。private素材とSDKは公開ツリーへ追加していない。詳細は`docs/Windows-v1-Manual-Acceptance.md`の2026-09-15節へ同期した。

# 2026-09-15 MANUAL-03: native制作状態の保存・終了・再開

`Builds/ImportActionsReadableV1/NyaForge.exe`の実ウィンドウで、空の制作プロジェクトへリング形状を追加し、private保存先`Z:\TextureVoice_local\git\RadDollV3-clothing\private\viewer-data\packs\manual-authoring-reopen-20260915`へ保存した。`project.nyaforge.json`と`blobs/`の生成、status `保存しました: ...manual-authoring-reopen-20260915`を確認した。その後アプリを終了・再起動し、`制作へ`→`確認・出力`→`3 保存とUnityへの受け渡し`から同じフォルダを指定して`開く`を実行。未保存確認で`変更を破棄して進む`を選択し、status `制作状態を開きました。ここから新しい履歴を始めます。`、保存前と同じobject ID先頭`3f98f4a4`、リング形状の再表示、上部`保存済み`を確認した。

これはnative制作状態の保存→終了→再起動→再開の手動受入PASS。実RadDollV3の衣装編集・fit・貫通、Unity更新／削除Undo、normal／MR／UV0画素、VRChat Build & Testは未受入。検証用projectはprivate配下で公開ツリーへ追加していない。
# 2026-09-15 GUI-13: 空projectから保存済み制作を再開する導線

空の制作projectでは従来、保存Panelが対象object追加後まで非表示だったため、保存済み作品を開くにはダミー形状を作って「確認・出力」へ移動する必要があった。右側の制作project直下へ「保存済み制作を再開」欄と「保存済み制作を開く…」ボタンを追加し、既存のWindows Explorer picker／未保存確認／native Open経路へ直接つないだ。自動検証時は既存のcompatibility表示を維持し、通常起動だけ空project欄を表示する。保存形式・MCP wire・GLB/VRM出力は変更していない。

- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Layout.cs`, `AuthoringWorkbench.ContextVisibility.cs`, `AuthoringWorkbench.State.cs`, `AuthoringWorkbench.StartupProbe.cs`
- Quickstart: `docs/Authoring-Quickstart.md`
- Player build: `Builds/EmptyProjectOpenV1/NyaForge.exe`（Unity **6000.4.3f1**、`Logs/build-player-20260915-011934-290.log`）
- 通常起動probe: **PASS**（`Artifacts/AuthoringStartup-20260915-012001-b73438a6d78c4626a94d7bdd4914fce5/report.json`）。`emptyProjectReopenVisible=true`、再開ボタンの表示高さ、既存の折りたたみ状態をPlayer内で確認した。Playerはreport出力後に終了しない環境挙動があり、probeのPASS marker／report／screenshotを証拠とした。
- 境界: 実ウィンドウでのExplorer選択と、保存→終了→再起動→再開のmanual acceptanceは`MANUAL-03`で既に確認済み。今回の新ボタン自体の実マウス操作、DPI 150/200%、IME、長いpath、実RadDollV3のfit・材質、Unity／VRChatは未受入のまま。

起動probeの終了時に`Application.Quit`が通常の未保存確認へ入り、reportはPASSでも外部スクリプトがタイムアウトしていた。probe側で`allowQuit`を有効にしてから終了するよう修正し、検証結果とプロセス終了の契約を揃える。

`Builds/EmptyProjectOpenV2/NyaForge.exe`（Unity **6000.4.3f1**、`Logs/build-player-20260915-012701-329.log`）を再ビルドし、`Tools/Test-NyaForgeAuthoringStartup.ps1 -BuildName EmptyProjectOpenV2`を実行。**PASS**（`Artifacts/AuthoringStartup-20260915-012723-a2d03fb9cbb142b78eb619960355b67a/report.json`）で、report生成後のプロセス終了まで確認できた。`emptyProjectReopenVisible=true`、再開ボタン表示、画面サイズ1600×1000、可視ピクセルありを記録した。

同じV2 Playerで通常のAuthoring回帰も実行し、**PASS**（`Artifacts/Authoring-20260915-012802-c6f787147b2d4442b4fd20cb9860d762/report.json`）。既存の保存／再開・頂点編集・材質・衣装package・MCP・出力経路を含むsuiteに影響がないことを確認した。Coreは今回UI／probeのみの変更のため、直近の **512 passed / 0 failed** を継続利用した。

# 2026-09-15 STATUS: v1受入前の現在地点

直近のmainは `ffb457e` で、`dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore` を再実行し **514 passed / 0 failed** を確認した。証跡は `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-72262532d83148babfa65c297e3be973`。Authoring起動probe、空projectの保存・終了・再開、private RadDollV3 VRMの取込→編集→native Save/Open→GLB／VRM出力→衣装package、Unity Bridge受け取りは既存の証拠でPASSを維持している。これは自動回帰・private素材smoke・合成Bridgeの範囲であり、実EditorWindowでの実衣装fit／貫通／見た目、移動・回転・scale済みavatar、VRChat Build & Test／クライアント表示の受入とは分ける。

追加レビューで、既知VRM拡張を認識しただけで完全保持と表示しない契約を再確認した。metaの利用条件、lookAt、firstPerson、expressionの材質・texture binding・制御flagなど、未保持・未解決の意味情報を含む入力は診断を残し、完全VRM出力を成功扱いにしない。`docs/Model-Interchange-Spec.md`にはこの判定と往復検証表が既に反映されている。

次は、(1) 実RadDollV3 sceneでの衣装一着のEditorWindow操作（fit・surface weight・手修正・全周確認）、(2) Unity側の移動／回転／scale・更新／削除Undo、(3) VRChat Build & Test／クライアント表示、の順に手動受入カードを進める。FBX／BLEND parser、全shader、Quest／macOS、完全VRM互換はこの受入が終わるまでv1の途中へ追加しない。

# 2026-09-15 GUI-14: モデル選択後の候補自動確認

Windows ExplorerでGLB／VRMを選んだ直後に、モデル取込欄の候補確認を自動実行するようにした。ファイル選択後、`node（配置）`／`mesh（形状）`／`skin（骨・weight）`の候補、完全パス、source情報を表示し、候補欄へスクロールする。確認に失敗した場合は取込を進めず、ファイル形式またはパスを確認するstatusを表示する。既存の候補選択、全mesh取込、保存形式、MCP wire、GLB／VRM出力は変更していない。

- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Import.cs`（`PickModel()`）
- Player build: `Builds/GuiModelAutoInspectV1/NyaForge.exe`（Unity **6000.4.3f1**、`Logs/build-player-20260915-015320-803.log`）
- Core回帰: **512 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-2807675c101d433283df2dc3e07f73f6`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260915-015342-cdd6678f04e5417a9e714a2a70207d3a/report.json`）
- 実モデル自動一周: private `RadDollV3_VRM.vrm` の取込→全mesh→EditMesh→native Save/Open→GLB／VRM出力→衣装packageを **PASS**（`Artifacts/Authoring-20260915-015427-cc42204ac895443fb34bfc28c4fc601a/report.json`）
- Unity Bridge: Unity **2022.3.22f1** の隔離受け取りを **PASS**（`Artifacts/BridgeReceiver-20260915-015651-585-d7b33c111c874af7ad14d2131cf6242b/bridge-report.json`、clothing skeleton 2 bones）
- 実ウィンドウ確認: 新Playerの制作画面とモデル取込欄（path／node／mesh／skin／取込操作）は表示確認済み。Explorer選択から自動候補確認までの一連は、複数ウィンドウ環境でpickerの対象が安定しなかったため、手動受入PASSとは扱わず残す。

この変更で空の制作projectでも、ファイルを選んだ後に候補確認を探す必要がなくなる。自動回帰はコード・Player経路の証拠であり、DPI 150/200%、日本語IME、実EditorWindowでの実衣装fit・貫通・全周見た目、移動／回転／scale済みavatar、Unity更新／削除Undo、VRChat Build & Test／クライアント表示の受入とは分ける。private素材とSDKは公開ツリーへ追加していない。

# 2026-09-15 UNITY-UI-01: 衣装package受け取り操作の到達性

Unity 2022.3.22f1の実EditorWindowで、衣装package読込・avatar root指定・stable BoneId欄の表示までは確認できた一方、最小サイズのウィンドウでは骨割当一覧の固定スクロール領域が下部の保存／診断／適用／削除ボタンを押し出していた。`UnityBridge/Editor/SkinnedClothingPackageWindow.cs`へウィンドウ全体の縦スクロールを追加し、コンパクトなレイアウトでも下部操作へ到達できるようにした。privateの隔離Unity probeへ同じソースを反映し、再コンパイル後にスクロールバーと下部ボタン（現在の割当を保存、保存済み割当を読み込む、事前診断、衣装を作成／更新、管理対象の衣装を削除）を実ウィンドウで表示確認した。

- Core回帰: **512 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-ab60a155d14a463095bdefdd7bbc23a9`）
- Unity Bridge回帰: **PASS**（Unity 2022.3.22f1、`Artifacts/BridgeReceiver-20260915-022438-250-13608a0b35c948fba8dc01a7dda74729/bridge-report.json`）
- 実EditorWindow: package読込、avatar root指定、2本のstable BoneId欄、全体スクロール、下部操作の表示を確認。今回のsynthetic packageでは再コンパイル後に割当がリセットされたため、実衣装の適用結果・見た目・貫通・更新／削除Undoは未受入。
- 公開境界: privateのRadDollV3素材、Unity SDK、probe sceneは公開ツリーへ追加していない。privateコピーは`private/`のignore対象。

残りの手動受入は、実RadDollV3衣装を使った全周fit・weight・材質、移動／回転／scale済みavatarへの適用、更新／削除Undo、VRChat Build & Testである。今回の修正はUI操作到達性を解決するもので、これらの実機受入を代替しない。

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
# 2026-09-15 GUI-16: 装着パネルの手順表示と詳細説明の折りたたみ

装着・骨パネルは長い技術説明が操作欄の後ろに続き、初めて使うと「どの順番で何を押すか」が分かりにくかった。`AuthoringWorkbench.AttachmentUi.cs`を整理し、上部に「対象avatar／BoneId／剛体装着またはskin-bind／fit・weight／保存」の短い手順を常設した。剛体小物、衣装skin-bind、fit・weightの区切りラベルも追加し、詳細説明は既定で閉じた`操作説明（詳細）`Foldoutへ移した。装着対象・BoneId・offset・各操作の保存形式と処理経路は変更していない。

- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.AttachmentUi.cs`
- 回帰補強: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.AttachmentVerification.cs`（短い手順と詳細Foldoutの存在・既定折りたたみを確認）
- Player build: `Builds/AttachmentGuideV1/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260915-025358-136.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260915-025420-2e4f70571a01471089c95ba5991aff88/report.json`、装着・skin-bind・fit・weight・保存/再読込を含む）
- Core: **512 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0efac60471e445408f5f3411e2e2f6dd`）
- 未受入: 実マウスでのDPI／IME／長い名称、実RadDollV3全周fit・貫通・見た目、VRChat内表示。自動Player回帰をこれらの手動受入へ読み替えない。

# 2026-09-15 UNITY-UI-03: GUI-16後の衣装package Bridge回帰

GUI-16で整理した装着手順PanelのPlayer生成物と、同じAuthoring artifactから生成した衣装packageをUnity **2022.3.22f1**の隔離Bridgeへ渡し、package検証・manifest／GLB／skeleton／bindingのhash・stable BoneId・SkinnedMeshRenderer生成・semantic材質変換を含む受け取り回帰を再実行した。**PASS**。

- Player入力: `Artifacts/Authoring-20260915-025420-2e4f70571a01471089c95ba5991aff88/report.json`
- 衣装package: 同artifactの`imported-accessory-skin-project/exports/clothing-20260914-175424-989b71/skinned-clothing.nyaforge.json`
- Bridge証跡: `Artifacts/BridgeReceiver-20260915-030038-621-9970614e7d33432db720866c76f398bd/bridge-report.json`
- 実EditorWindowではsynthetic packageの操作到達性まで確認済み。実RadDollV3衣装の適用見た目・fit・貫通、移動／回転／scale済みavatar、更新／削除Undo、VRChat Build & Testは未受入。

現行HEADは`9f89414`（GUI-16）で、今回の変更は証跡ドキュメントのみ。次は実Unity EditorWindowで実アバターへ衣装を適用し、座標変換・fit・weight・全周見た目を確認する。VRM意味情報の完全保持、Quest／macOS、FBX／BLEND parserはv1手動受入後の範囲に残す。

# 2026-09-15 GUI-17: AI接続Panelの狭幅レイアウトと導線整理

AI接続（MCP）Panelの長い説明が常時表示されると、狭いWindowsウィンドウやDPI拡大時に右端が切れ、接続開始の手順も埋もれていた。`AuthoringWorkbench.McpPanel.cs`へ短い手順案内（接続開始→instance IDをsidecarへ指定）を追加し、長い説明を既定で閉じた「接続の使い方（詳細）」Foldoutへ移した。開始操作は`この制作へ接続`へ短縮し、instance ID欄もsidecar用途を明示した。`AuthoringWorkbench.Layout.cs`ではcontrols側を最小幅260pxまで縮小可能にし、viewport側の最小幅を120pxへ調整した。`Viewer.uss`にはMCP Foldout内容の幅制約を追加し、横スクロールへ逃げないようにした。

- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.McpPanel.cs`, `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Layout.cs`, `Assets/Resources/Viewer.uss`
- 回帰補強: `AuthoringWorkbench.Verification.cs`で短い案内・詳細Foldoutの存在と既定折りたたみを確認
- Player build: `Builds/McpPanelV1/NyaForge.exe`（Unity **6000.4.3f1**、`Logs/build-player-20260915-030618-300.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260915-030639-db3f07ec15ab48f995d996e2900b454d/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260915-030737-4b8a8bd01a074b71a18c2cdd9fcfc6fa/report.json`）
- Core: **512 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b4af5ea13eca4b0aa72bed2666bf9d67`）
- 手動境界: 通常サイズでのコード／Player回帰は確認したが、実ウィンドウの幅441px相当、DPI 150/200%、日本語IME、実sidecar接続の目視は未受入。実RadDollV3のfit・貫通・材質、Unity更新／削除Undo、VRChat内表示も未受入。

# 2026-09-15 RECHECK-01: 現行HEADのCore／Unity Bridge再検証

GUI-17後の現行HEAD `bed3e6228767a41ad0558c81d5675c22786c9dfc` で、保存・出力・保護・材質slot・semantic texture・衣装fitの回帰を再実行した。Coreは `512 passed / 0 failed`。証跡は `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-fbf71f3edab345e8ad73edaaa4bd0fb2`。

GUI-17で生成した実package（`Artifacts/Authoring-20260915-030639-db3f07ec15ab48f995d996e2900b454d`）をUnity **2022.3.22f1**の隔離receiverへ再投入し、manifest／GLB／skeleton／bindingのhash、stable BoneId、SkinnedMeshRenderer生成、semantic材質変換を **PASS**。証跡は `Artifacts/BridgeReceiver-20260915-031943-436-367517da82af43f2b2f064bf95204751/bridge-report.json`。

これはCore／Bridgeの自動再検証であり、実RadDollV3 EditorWindowでの衣装fit・貫通・全周見た目、移動／回転／scale後の手動適用、Unity更新／削除Undo、VRChat Build & Test／クライアント表示の受入へは読み替えない。Unity `PhysBonesSdkProbe-20260914` は別のUnity instanceが同じprojectを開いているため、今回のprobe起動は再実行していない。既存の `private/PhysBonesSdkProbe-20260914/sdk-probe-report-latest.json` に記録済みのverified結果を正本として扱う。

次の実作業は、既存Unity EditorWindowへ実RadDollV3用packageを読み込み、候補生成・明示割当・適用・更新／削除Undoを一周すること。手動確認が終わるまでQuest／macOS、FBX／BLEND parser、完全VRM互換はv1へ追加しない。

# 2026-09-15 UNITY-MANUAL-01: 実Unityでのpackage読込とBoneId手動割当

開いているUnity **2022.3.22f1** の `PhysBonesSdkProbe-20260914` で、現行の衣装manifest `Artifacts/Authoring-20260915-015427-cc42204ac895443fb34bfc28c4fc601a/imported-accessory-skin-project/exports/clothing-20260914-165627-2cfadd/skinned-clothing.nyaforge.json` を `NyaForge Clothing` windowへ読み込んだ。manifest／3 vertices・1 triangle／2 bones／`RaddollV3 (Transform)` の表示を確認した。

`候補を生成（名前・階層）`ではpackage側の `Child`／`Root` と実avatarのstable identityが一致せず、`0/2本を一意候補として検出`となった。推測割当は行わず、Unity object pickerから `Child → Neck (Transform)`、`Root → Hips (Transform)`を手動指定できることを確認した。これは候補生成と明示割当のUI・identity境界の受入である。

packageはsynthetic 3頂点のため、割当保存・事前診断・衣装適用後の実RadDollV3全周見た目・貫通・pose変形の合格材料にはしない。実衣装packageでの候補／手動割当→保存→診断→適用→更新／削除UndoとVRChat Build & Testは未受入のまま残す。作業中のUnity projectは別instanceによるロックを避けるため閉じていない。

# 2026-09-15 FIX-01: Windows長いパスのハッシュ資産出力

深い開発フォルダでMaterial／Paint／Evidenceの画像やmesh blobを書き出すと、SHA-256全文をファイル名へ使う経路がWindowsの従来パス長境界を越え、`DirectoryNotFoundException`になる問題を切り分けた。`Storage.HashFilePath`を追加し、通常の短いパスでは従来の全文hash名を維持し、240文字を超える場合だけ先頭8文字と末尾8文字をつないだ決定的な短縮名へ切り替えるようにした。manifestの完全hashと読み込み時の内容hash検証は維持し、既存の全文hashファイルも優先して読める。Material bake、Paint PNG、Evidence captureの書き込み・読み込みを同じヘルパーへ統一した。

- 変更: `Assets/NyaForge/Authoring/Persistence/Storage.cs`
- 変更: `Assets/NyaForge/Authoring/Persistence/BakeImagePayload.cs`
- 変更: `Assets/NyaForge/Authoring/Persistence/PaintPngExport.cs`
- 変更: `Assets/NyaForge/Authoring/Evidence/EvidenceCaptureStore.cs`
- 変更: `Assets/NyaForge/Authoring/Evidence/EvidenceCaptureReader.cs`
- 回帰: 長い保存先でcompact blob／PNG名を確認するMaterial bakeテストを追加
- Core: **513 passed / 0 failed**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-98b931281c0046ea8816e17a4c1ed7e2`）
- Windows Player: 一時コピーを`C:\Users\tomoaki\AppData\Local\Temp\NyaEkaki3D-build-20260915-033127`へ作成してビルド・再実行。通常のMaterial／Paint PNG／Surface出力は通過し、長いパス由来の失敗は解消した。`report.json`は既存の検証ハーネスであるsave-failure故障注入（期待された`DirectoryNotFoundException`）と`Viewport restoration differs`で`passed=false`のため、Player全体PASSや実UI／Unity／VRChat受入へは読み替えない。
- 公開境界: 一時Player、private素材、生成Artifactsは公開ツリーへ追加していない。`private/`追跡除外を維持する。

次は、長いパスを含む実RadDollV3の衣装packageを作成し、Unity受け取り・更新／削除Undo・移動／回転／scale後の表示を手動確認する。実衣装のskin-bind／fit／貫通とVRChat Build & Testは引き続き未受入。

# 2026-09-15 FIX-02: 深い出力先のatomic stagingとPlayer回帰の再成立

FIX-01でblob／PNGのhash名を短縮しても、GLB・複数object・衣装packageのstaging directoryへ出力先名とGUIDを連結する経路が、深いWindowsパスでatomic temporary fileの上限を越えていた。`Storage.StagingDirectory`を追加し、destinationの兄弟へ短い`.nf-<tag>-<8hex>` staging directoryを作ってから同一volume内で公開するようにした。GLB、MultiObject、SkinnedClothingPackageの各writerへ接続した。故障注入側も`Storage.HashFilePath`を使い、短縮blob名でもsave failure guardが同じ実体をロックするようにした。

Surface viewport検証は、Ready通知前の古いboundsを復元後の値と比較していたため、UI Toolkitの再flowで誤検知していた。Ready後のlayoutを待ち、復元後は現在のviewportが利用可能でcamera targetが現行textureを指すことを確認する検証へ整理した。

- 変更: `Assets/NyaForge/Authoring/Persistence/Storage.cs`
- 変更: `Assets/NyaForge/Authoring/Persistence/GlbExportService.cs`
- 変更: `Assets/NyaForge/Authoring/Persistence/MultiObjectExportService.cs`
- 変更: `Assets/NyaForge/Authoring/Persistence/SkinnedClothingPackage.cs`
- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.SaveFailureVerification.cs`
- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.SurfaceViewportVerification.cs`
- 回帰追加: 深い保存先でMultiObject packageを公開し、各object manifestを再読込。衣装packageでも深い保存先を使用。
- Core: **514 passed / 0 failed**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-ca68a186c8e04458a4d1574d70b38637`）
- Windows Player Authoring: **PASS**（一時build `C:\Users\tomoaki\AppData\Local\Temp\NyaEkaki3D-build-20260915-033127\Builds\Windows-DeepStage6\NyaForge.exe`、report `Artifacts/Authoring-20260915-040344-c9aab4d3e43842ea80aeffb100a0c8d5/report.json`）
- Real Clothing script: **PASS**。実RadDollV3 VRMを入力にした取込と、衣装workflow／package生成をPlayerで実行し、`Clothing skeleton: 2 bones`を確認。Unity **2022.3.22f1** Bridgeのpackage受け取りもPASS（`Artifacts/BridgeReceiver-20260915-040652-218-600e0951e948479f9f4c5fb708952365/bridge-report.json`）。
- 境界: このReal Clothing scriptは実avatar入力＋合成accessory fixtureによる自動回帰であり、実EditorWindowでの実衣装全周fit／貫通／見た目、avatar移動・回転・scale、更新／削除Undo、VRChat Build & Testを代替しない。
- 公開境界: private素材、Unity project、temporary Player、Artifactsはpublic treeへ追加していない。`git ls-files private`は空。

次は、このpackageを開いているUnity EditorWindowへ読み込み、実avatar rootへ候補／明示BoneId割当→適用→更新／削除Undoを実操作で一周する。その後、通常poseとVRChat Build & Testを確認する。

# 2026-09-15 UNITY-MANUAL-02: 現行衣装packageの明示BoneId割当

Unity **2022.3.22f1** の隔離 `PhysBonesSdkProbe-20260914` に、FIX-02で生成した衣装manifestを `NyaForge Clothing` windowから読み込んだ。manifest／`3 vertices · 1 triangles`／`2 bones`／`RaddollV3 (Transform)` の表示を確認した。

package側のstable binding名（`Child`／`Root`）は実avatarのstable identityと一致せず、自動候補に頼れない状態だった。Unity object pickerで `Child → Neck (Transform)`、`Root → Hips (Transform)` を明示指定できることを確認した。割当保存と事前診断のUIボタンまで到達したが、適用結果の見た目はこのsynthetic packageでは評価対象にしない。

- package: `C:/Users/tomoaki/AppData/Local/Temp/NyaEkaki3D-build-20260915-033127/Artifacts/Authoring-20260915-040417-ddf3fcbad8094156a88a93760db5dbd5/imported-accessory-skin-project/exports/clothing-20260914-190628-429e91/skinned-clothing.nyaforge.json`
- avatar root: `RaddollV3 (Transform)`
- 手動割当: `Child → Neck`、`Root → Hips`
- 境界: 合成3頂点packageのUI到達性・明示割当のみ。実RadDollV3衣装の全周fit／貫通／pose変形、更新／削除Undo、VRChat Build & Testは未受入。

# 2026-09-15 UNITY-MANUAL-03: 衣装packageの適用・更新・削除導線

Unity **2022.3.22f1** の隔離 `PhysBonesSdkProbe-20260914` で、`UNITY-MANUAL-02` と同じ現行衣装manifestを `NyaForge Clothing` windowへ読み込み、明示割当（`Child → Neck`、`Root → Hips`）を保持した状態で操作を一周した。

- `事前診断（書き込みなし）` → `事前診断OK (sceneへの書き込みなし)`
- `衣装を作成／更新` → `衣装を適用しました。`
- 同じpackageで再度 `衣装を作成／更新`（更新経路）
- `管理対象の衣装を削除（Undo可）` → `管理対象衣装を削除しました。Undoで元の関連付けへ戻せます。`

EditorWindowのボタン導線・scene書き込み前診断・適用／更新／削除の状態遷移は実操作で確認できた。入力は合成3頂点・2骨packageのため、実RadDollV3衣装の全周fit、貫通、pose変形、材質の見た目、avatar移動／回転／scale後の配置合格には使わない。Undoで復元する最終確認と、実衣装packageでの同じ一周は次の手動受入に残す。

# 2026-09-15 STATUS-01: 実RadDollV3 packageのUnity読込状態

現行HEADで再生成した実RadDollV3入力由来の衣装manifestを、Unity **2022.3.22f1** の隔離 `PhysBonesSdkProbe-20260914` に読み込んだ。Player側の実RadDollV3取込・衣装workflow・package生成と、Unity Bridge受け取りは **PASS**。Unity EditorWindowではmanifest、`3 vertices · 1 triangles`、`2 bones`、`RaddollV3 (Transform)` を確認した。

package側のstable bindingは `Child`／`Root` で、実avatarのstable identityとは一致しないため、候補生成は **0/2本**。自動推測は行わず、次は object picker で実avatarの対応Transformを明示して、割当保存→事前診断→適用→更新→削除Undoを実packageで一周する。

- 自動回帰: Core **514 passed / 0 failed**
- Unity Bridge: **PASS**（`Artifacts/BridgeReceiver-20260915-043056-967-2de2edb815fe495a8e7e10d13c6820f1/bridge-report.json`）
- Player実衣装workflow: **PASS**（`Artifacts/Authoring-20260915-042821-8826ff0bfd574ad7b05e92c148f76fdf/report.json`）
- 境界: packageは合成3頂点・2骨fixtureを含むため、実RadDollV3衣装の全周fit／貫通／材質見た目、avatar移動・回転・scale、VRChat Build & Testの合格には読み替えない。

# 次回受入候補: VRM意味情報の完全出力境界

VRMのmeta利用条件、lookAt、firstPerson、expressionの材質・texture bindingなど、現行readerが完全保持できない意味情報は、完全互換出力と誤認しないゲートを設ける。未保持フィールドがある場合は警告または出力停止を選べるよう、`docs/Model-Interchange-Spec.md` の検証表へ往復比較項目を追加する。

# 2026-09-15 UNITY-UI-03: 衣装骨割当の階層表示

衣装受け取り画面のstable BoneId欄だけでは、実avatarのどのTransformを指定すべきか確認しづらかったため、`UnityBridge/Editor/SkinnedClothingPackageWindow.cs`の各割当欄へ次を表示するようにした。

- package skeletonから再構成した`期待階層`
- ObjectFieldへ指定したTransformのシーン完全パス
- 未指定時の`未設定`表示
- 候補生成前の割当済み本数（`n/全骨数`）

既存の明示ObjectField、曖昧候補を自動反映しない規則、保存／診断／適用の検証条件は変更していない。長い階層はウィンドウ全体の既存スクロールで確認する。現行packageを使ったUnity **2022.3.22f1** Bridge回帰は **PASS**（`Artifacts/BridgeReceiver-20260915-045302-404-74d6951b626b44ada74ca4c278b1e99a/bridge-report.json`）。Coreは **514 passed / 0 failed**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-fcb0a78d84214929ab9a38dbe6bdbd9b`）。

これは受け取り画面の対応確認を補助する実装・コンパイル回帰であり、実RadDollV3衣装の全周fit／貫通／材質見た目、avatar移動・回転・scale、VRChat Build & Testの手動受入を完了したことを意味しない。

# 2026-09-15 VERIFICATION-01: UI改善後の実モデル一周再確認

衣装受け取りUIの階層表示変更後、既存のWindows Player一周を現行 `main` 近傍で再実行した。privateの実RadDollV3 VRMを入力に、取込→全mesh→編集→native Save/Open→標準GLB／VRM1出力→衣装package生成まで **93 checks PASS**。レポートは `Artifacts/Authoring-20260915-045536-cc0ec63a35fc41c7aeb4088525c6518a/report.json`、衣装packageは `Artifacts/Authoring-20260915-045536-cc0ec63a35fc41c7aeb4088525c6518a/imported-accessory-skin-project/exports/clothing-20260914-195747-8572d7/skinned-clothing.nyaforge.json`。

同packageを現行Unity BridgeソースでUnity **2022.3.22f1**へ受け取り、Bridge回帰も **PASS**（`Artifacts/BridgeReceiver-20260915-045824-396-97fd29bf619645df85ab5a26eb99c825/bridge-report.json`）。この再確認は実avatar入力と合成accessory fixtureによる自動経路であり、実EditorWindowの新しい階層表示、実衣装の全周fit／貫通／材質見た目、avatar移動・回転・scale、VRChat Build & Testの手動受入には読み替えない。
# 2026-09-15 REAL-CLOTHING-01: 実RadDollV3からのチョーカー作成probe

実際のprivate RadDollV3 VRMを入力に、既存の制作導線でチョーカーを作り、Neckへ明示的にskin-bindして、native Save/Open後に衣装packageを出力するPlayer probeを追加した。`Tools/Test-NyaForgeRealClothing.ps1`から`-RealClothing`を通じて実行でき、通常のsynthetic accessory probeとは分けている。チョーカー原形は24×8の編集トポロジーだが、評価・出力メッシュはシーム展開後の225頂点／384三角形となるため、検証は固定値ではなく生成結果の一致で確認する。

- 追加ソース: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.RealClothingVerification.cs`（`.meta`を含む）
- 接続変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Verification.cs`, `Tools/Test-NyaForgeAuthoring.ps1`, `Tools/Test-NyaForgeRealClothing.ps1`
- Core回帰: **514 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-37fca489c97d436bb7deac435e9160cc`）
- Unity 6000.4.3f1 Player `RealClothingV3`: build成功（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealClothingBuild-20260915/Builds/RealClothingV3/NyaForge.exe`）
- 実RadDollV3 Player: **94 checks PASS**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealClothingBuild-20260915/Artifacts/Authoring-20260915-051810-399f508dbc4b452a95f5118f7a7fedad/report.json`）
- 最終ソース反映後の再確認（Player `RealClothingV4`）も **PASS**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealClothingBuild-20260915/Artifacts/Authoring-20260915-052708-da52b52ea1b94ae0ac34864a972177e2/report.json`）。
- 頂点編集をreport項目へ明示した最終再確認（Player `RealClothingV6`）は **95 checks PASS**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealClothingBuild-20260915/Artifacts/Authoring-20260915-054119-6c161bef86be403aa2994cd95518836e/report.json`）。同packageのUnity Bridgeも **PASS**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealClothingBuild-20260915/Artifacts/BridgeReceiver-20260915-054457-364-c96fd9f7449845e39e69cc5a3f65c5b4/bridge-report.json`）。
- 生成衣装package: `.../real-clothing-project/exports/clothing-20260914-202122-651891/skinned-clothing.nyaforge.json`（Neck参照、225 vertices／384 triangles）
- Unity 2022.3.22f1 Bridge: **PASS**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealClothingBuild-20260915/Artifacts/BridgeReceiver-20260915-053257-308-caa61b915f614de39c09c87f68cac86b/bridge-report.json`）。Unity起動時に掃除されるproject `Temp`をPhysBones fixtureの出力先に使わないよう検証 harness を修正し、実衣装packageの受け取りまで完了した。
- 未受入: 実EditorWindowのマウスでの衣装作成・全周fit・貫通・材質見た目、avatar移動／回転／scale、Unity実SDKでの適用、VRChat Build & Test／実機表示。

# 2026-09-15 VRM-SEMANTIC-01: VRM出力の完全性境界

VRM1出力は入力側の保持できない意味情報を`sourceDiagnostics`へ記録していたが、呼び出し側が通常の成功と完全保持を混同しやすかった。`VrmExportResult.SourceSemanticsComplete`とreportの`sourceSemanticStatus`／`sourceBlockingDiagnosticCount`を追加し、blocking診断がある出力を`partial`として明示する。APIへ`requireCompleteSourceSemantics`を追加し、厳格指定時は`VRM_SEMANTICS_INCOMPLETE`でstaging作成前に停止する。GUIにも「入力の未保持情報があれば出力を停止」チェックを追加し、既定のpartial出力は制限付き副経路として残した。衣装package／標準GLBの主経路は変更していない。

- 変更: `Assets/NyaForge/Authoring/Persistence/VrmExportService.cs`
- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.State.cs`
- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ProjectOutput.cs`
- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ProjectActions.cs`
- 仕様同期: `docs/Model-Interchange-Spec.md`
- Core回帰: **515 passed / 0 failed**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-2c38996cdcc44f35b587cc140d116ddf`）。non-blocking診断は`complete`、blocking診断は`partial`、厳格指定は出力先未作成を確認。
- 未確認: Unity Player再ビルド、実EditorWindowでのチェック操作、実VRM／VRChat外観。これらは自動Core回帰の合格へ読み替えない。

追試: Unity 6000.4.3f1 Player `VrmSemanticGateV1`を隔離コピーで再ビルドし、通常Authoringを **PASS**（`Artifacts/Authoring-20260915-060411-15f3d71d7eac4de0a7ceb03c1b4508de/report.json`）。private RadDollV3 VRMを入力した実衣装probeも **90 checks PASS**（`Artifacts/Authoring-20260915-060447-b0ce865e46e14666a4c7ec6d3c7472b6/report.json`）、生成packageは同report配下の`real-clothing-project/exports/clothing-20260914-210659-e6f717/skinned-clothing.nyaforge.json`。今回の実衣装probeは合成accessory fixtureを含むため、実EditorWindow操作、全周fit／貫通／材質見た目、Unity実SDK適用、VRChat Build & Test／実機表示は未確認のまま。

# 2026-09-15 VIEWER-OPEN-01: 制作正本を誤選択した場合の導線

Viewerの「パックを開く…」でnative制作正本 project.nyaforge.json を選ぶと、従来はroot projectのunknown-fieldをそのまま表示していた。ViewerApp.OpenPathでファイル名を判定し、NATIVE_PROJECT_REQUIRES_AUTHORING と「制作へ」への案内を表示するようにした。確認パックの current.StandaloneWindows64.json は引き続き通常どおり読み込める。

- Unity 6000.4.3f1 Player NativeProjectHintV1を隔離ビルド。
- current.StandaloneWindows64.jsonをWindowsファイル選択から開き、RadDollV3衣装確認を表示できることを確認。
- 同じダイアログから project.nyaforge.json を選び、現在のモデルを保持したままコードと案内文が表示されることを確認。
- 確認範囲はViewer導線とエラー表示。制作画面でのnative project再開、実EditorWindowの衣装作成、VRChat受入は別カード。

# 2026-09-15 UNITY-MANUAL-03: 実RadDollV3衣装packageの受け取り導線

Unity **2022.3.22f1** の隔離 `PhysBonesSdkProbe-20260914` で、現行の実RadDollV3入力由来packageを `NyaForge Clothing` windowへ読み込み、stable bone bindingを実avatarへ明示指定して操作を一周した。

- package: `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-RealClothingBuild-20260915\Artifacts\Authoring-20260915-060447-b0ce865e46e14666a4c7ec6d3c7472b6\real-clothing-project\exports\clothing-20260914-210659-e6f717\skinned-clothing.nyaforge.json`
- 読込表示: `225 vertices · 384 triangles`、`4 bones`、avatar root `RaddollV3 (Transform)`
- 明示割当: `Chest → Chest`、`Spine → Spine`、`Neck → Neck`、`Hips → Hips`
- `現在の割当を保存` → stable bone割当をavatar rootへ保存
- `事前診断（書き込みなし）` → `事前診断OK (sceneへの書き込みなし)`
- `衣装を作成／更新` → `衣装を適用しました。`
- 同じpackageで `衣装を作成／更新` を再実行し、更新経路を通過
- `管理対象の衣装を削除（Undo可）` → `管理対象衣装を削除しました。Undoで元の関連付けへ戻せます。`

実EditorWindowでのpackage読込、Object pickerによる4本の明示割当、保存、書き込み前診断、適用、更新、削除の導線は確認できた。`Ctrl+Z`は送信したが、画面状態から生成衣装の復元を確認できなかったため、Undo復元は未受入として残す。今回の画面は受け取り導線の確認であり、実衣装の全周fit、貫通、pose変形、材質の見た目、avatar移動・回転・scale後の配置、Unity実SDK適用、VRChat Build & Test／実機表示の合格には読み替えない。

# 2026-09-15 GUI-04: 基本形状寸法の入力保持

基本形状パネルの表示更新が、入力済みの寸法を既定値へ戻してしまう不具合を修正した。`Refresh` はコマンド実行・対象切替・保存／再開後にも走るため、入力欄は現在のプリセットIDが変わったときだけ既定値を設定し、それ以外の更新では利用者の値を保持する。プリセットを明示的に切り替えた場合は、選択した種類の既定寸法へ戻る。

- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ShapeCreation.cs`
- 回帰: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ShapeCreationVerification.cs`（寸法保持、プリセット既定値、再Refreshを同じUI状態で確認）
- Unity 6000.4.3f1 隔離Windows Player `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-ShapeInputsV2-src\Builds\ShapeInputsV2\NyaForge.exe`：build成功
- Authoring自動検証: **87 checks PASS**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-ShapeInputsV2-src\Artifacts\Authoring-20260915-063948-4b3e0d858ae1425dbf999c7859b1c3d5\report.json`）。追加項目 `shape creation inputs: custom dimensions survive refresh and preset changes restore only the selected preset defaults` を確認。
- Core: **515 passed / 0 failed**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-effb6d4787db4461b3aaff3339cc6704`）
- 境界: Player自動UI回帰であり、実マウスのDPI別表示、実RadDollV3への全周fit／貫通／材質見た目、Unity実SDK、VRChat実機受入は完了扱いにしない。

# 2026-09-15 GUI-06: 納品対象スコープの表示

保存と納品出力の違いをさらに確認しやすくするため、`ProjectOutput`へ現在の汎用GLB／Unity出力対象の概要を表示した。allowlist未指定時は全object、指定時は対象数、参照保護objectの衝突、allowlist不整合を短い状態表示へまとめ、詳細な対象名とstable ObjectIdはツールチップへ残す。衣装skin packageは従来どおり選択中の衣装objectだけを出力する説明も明示した。出力処理・allowlist判定そのものは変更していない。

- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.State.cs`
- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ProjectOutput.cs`
- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.PersistenceRefresh.cs`
- 回帰: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Verification.cs`（空projectで納品対象なしの表示を確認）
- 設計同期: `docs/Windows-v1-GUI-Navigation-Plan.md`, `docs/Windows-v1-Development-Plan.md`
- Unity 6000.4.3f1 隔離Windows Player `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-OutputScopeV1-src\Builds\OutputScopeV1\NyaForge.exe`：build成功
- Authoring自動検証: **87 checks PASS**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-OutputScopeV1-src\Artifacts\Authoring-20260915-070416-cd479b7b1e18458095f3debc9e23a699\report.json`）
- 境界: 出力対象の表示と自動UI回帰の確認であり、実マウスのDPI／IME／長い名称、実RadDollV3全周fit・貫通・材質、Unity実SDK、VRChat実機受入は未完了のまま。

追試: 複数objectのPlayer回帰で、参照bodyを保護した状態の「参照保護 1件」と、衣装objectだけをallowlist指定した状態の「指定 1 object」への表示切替を確認した。`OutputScopeV2` は **87 checks PASS**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-OutputScopeV1-src\Artifacts\Authoring-20260915-070747-d2c1b5aba6cc4875a9fb861c79ba3d45\report.json`）。

# 2026-09-15 GUI-05: 制作正本ファイルの表示

保存・再開用のnative制作状態が、入力した制作フォルダのどこにあるか画面だけでは分かりにくかったため、保存欄の直下へ `project.nyaforge.json` のフルパスを表示するラベルを追加した。フォルダ欄の保存方式とExplorer選択は変更していない。長いパスは1行表示でレイアウトを押し広げず、ラベルのツールチップに実体パス全体を入れる。

- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.State.cs`
- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ProjectOutput.cs`
- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.PersistenceRefresh.cs`
- 回帰: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Verification.cs`（保存直後に正本ファイル名が表示されることを確認）
- Unity 6000.4.3f1 隔離Windows Player `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-ManifestPathV1-src\Builds\ManifestPathV2\NyaForge.exe`：build成功
- Authoring自動検証: **87 checks PASS**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-ManifestPathV1-src\Artifacts\Authoring-20260915-065717-5a9a6f3a006148a1850fa9e28dab5d45\report.json`）。正本パス表示追加後もruntime pointer、保存／再開／出力導線を含む一周が合格。
- 境界: フルパス表示と自動UI回帰の確認であり、実マウスのDPI別文字サイズ、実RadDollV3の外観、Unity実SDK、VRChat実機受入は完了扱いにしない。

# 2026-09-15 GUI-09: fit／weight対象サマリー

装着Panelのfit／weight欄だけでは、現在どの衣装頂点・avatar面へ処理をかけるのか、測定結果がまだ有効かを判断しにくかった。`object-surface-fit-summary`を追加し、衣装の指定範囲と全頂点数、avatarの指定面領域と全三角形数、skin-bind状態、fit測定済み／再測定要否を常時表示する。ID入力やviewport面選択で範囲を変えた場合、対象・範囲・offset・最大距離が前回測定と一致しない限り測定を有効扱いしない。長い内容は1行表示とtooltipで確認できる。

- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.AttachmentState.cs`
- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.AttachmentUi.cs`
- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Attachments.cs`
- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.SelectionRefresh.cs`
- 回帰: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.AttachmentVerification.cs`, `AuthoringWorkbench.ImportedAccessoryVerification.cs`（summaryの構築、衣装2頂点／avatar1面の指定、計測済み表示）
- Core: **515 passed / 0 failed**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-602af81e20cc478b91572077039e8017`）
- Unity 6000.4.3f1 隔離Windows Player `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-FitSummaryV4-src\Builds\FitSummaryV4\NyaForge.exe`: build成功
- Authoring自動検証: **PASS**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-FitSummaryV4-src\Artifacts\Authoring-20260915-071855-1bcf655624a94fb59aec4b59d5717d06\report.json`）
- 境界: 対象範囲の可視化と測定のstale判定を確認したもので、実マウスのDPI／IME、実RadDollV3の全周fit・貫通・材質見た目、avatar移動・回転・scale、Unity実SDK、VRChat Build & Test／実機表示は未受入のまま。

# 2026-09-15 GUI-10: 作業モード切替時のPanel整理

作業モードのボタンを押しても、前のモードの長いFoldoutが開いたまま残り、右側の操作欄が混雑していた。`AuthoringWorkbench.WorkModes.FocusWorkMode`で形状／UV・色／装着・骨／確認・出力を同じ編集領域として扱い、選択した領域だけを開き、他を閉じるようにした。保存形式・Document・Undo・編集commandは変更していない。自動検証は低レベルボタンを後続で直接使うため、work-mode検証後に従来の展開状態を復元する。

- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.WorkModes.cs`
- 回帰: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.WorkModeVerification.cs`（各モードの対象Panelが開き、前モードが閉じることを確認）
- Unity 6000.4.3f1 隔離Windows Player `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-WorkModeV3-src\Builds\WorkModeV3\NyaForge.exe`: build成功
- Authoring自動検証: **PASS**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-WorkModeV3-src\Artifacts\Authoring-20260915-072706-64d4ec1fd75f40d7860ebdc4f21d844a\report.json`）
- 境界: 自動UI回帰の確認であり、実ウィンドウの狭幅／DPI／IME／実マウス、実RadDollV3の全周fit・貫通・材質見た目、Unity実SDK、VRChat Build & Test／実機表示は未受入のまま。

# 2026-09-15 GUI-11: Morph target完全IDと作業モード導線

シェイプキー調整の一覧は名前と短縮IDだけだったため、同名targetや複数アバターを扱うと、どの差分を編集しているか画面上で照合しづらかった。Morph targetのDropdownへ選択中targetの完全IDをtooltip表示し、Morph nodeがない状態では古いIDを残さず案内へ戻すようにした。確認・出力の作業モードからMorph／表情差分Panelを開けるようにし、モード切替時の自動折りたたみ対象にも含めた。編集値、保存形式、VRM mapping、Undo経路は変更していない。

- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Morph.cs`
- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.WorkModes.cs`
- 回帰拡張: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.WorkModeVerification.cs`（確認・出力モードでMorph Panelも開き、装着・骨Panelが閉じることを確認）
- 検証: 隔離Windows Player `NyaForge-MorphTargetContextV1`をbuildし、Morph tooltip回帰を含む通常Authoring **88 checks PASS**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Artifacts\Authoring-20260915-075505-dd749e30efe04cbdbcd7fd21e1702619\report.json`）。
- 境界: tooltipの実マウス表示、長いtarget名・高DPI、実RadDollV3の表情見た目、Unity／VRChat実機は手動受入として残る。Panel状態欄には現在の制作対象名も表示する。
# 2026-09-15 PERF-03: source-skin表示キャッシュの一時割当削減

source-skin表示と投影のキャッシュ判定は、従来の複数hashを毎回連結した文字列から、値比較だけを行う`SourceSkinDisplayCacheKey`／`SourceSkinProjectionCacheKey`へ変更した。表示更新のたびに大きなweight列を文字列化する経路は現行コードに存在せず、キー連結由来の一時文字列を避け、pose／skin-bind候補の決定も割当なしの明示ループへ置き換えた。graph／評価結果／pose／source／skin-bindのいずれかが変わった場合だけ再投影し、空workspaceへ戻ったときはキャッシュを無効化する。ドキュメントや保存形式、Undo、出力データは変更していない。

- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.SourceSkinDisplay.cs`
- Core: **515 passed / 0 failed**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-9575e1a826b84cb3a6edb8e0fc7397b9`）
- Unity 6000.4.3f1 隔離Player `NyaForge-MorphTargetContextV1`へ反映してbuild成功。通常Authoring **88 checks PASS**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Artifacts\Authoring-20260915-080040-a6c06217d1ff44c3b8258a2862315543\report.json`）。明示ループ版も同Playerでbuild成功し、通常Authoring **88 checks PASS**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Artifacts\Authoring-20260915-080440-8b10674a8b944889a5d84d2f3c9fefc7\report.json`）。空hashを含むpose候補の比較も従来の順序意味を維持するよう補正し、`SourceSkinCacheV3`でbuild・通常Authoring **88 checks PASS**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Artifacts\Authoring-20260915-081056-991fc05457e14be3946227681bfbf3c7\report.json`）。投影キーも値比較へ移行し、`SourceSkinCacheV4`でbuild・通常Authoring **88 checks PASS**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Artifacts\Authoring-20260915-081337-cf85d44f76ce4ff8889c92d038b4400e\report.json`）。
- commit/push: `b9b30b7 perf: avoid source skin cache key allocations` と `8b89fe6 perf: reduce source skin cache scan allocations` と `f395ffe fix: preserve empty pose hash cache identity` と `ae16883 perf: avoid source skin projection key allocations` を`origin/main`へpush済み。
- 境界: Unity実EditorWindowのGC／native memory計測、実RadDollV3の全周fit・貫通・材質見た目、VRChat実機受入は未完了。今回の変更はキー生成の割当削減であり、軽量性合格を単独で宣言しない。
# 2026-09-15 PERF-04: 軽量化後の起動・終了反復

軽量化後の隔離Windows Player `NyaForge-MorphTargetContextV1`へ公開fixtureのmanifestとrevision sidecar一式を配置し、通常Navigation（1280x800、windowed）を20回連続実行した。各回でfixtureのOpen、viewport領域、source mapping表示を確認し、**20/20 PASS**となった。初回試行は隔離コピーにfixture sidecarがなく検証前提を満たさなかったため、fixture一式を揃えて再実行した。実アバターの長時間編集・GC/native memory・別Windows環境の性能合格へは読み替えない。

- Player: `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Builds\SourceSkinCacheV2\NyaForge.exe`
- 記録: `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Artifacts\Navigation-Repeated-SourceSkinCacheV2-20260915-080734.json`
- 境界: 20回の起動・終了と基本navigationの安定性のみ。2時間編集、20回の衣装更新、実EditorWindowのDPI／IME、実RadDollV3の全周fit・貫通・材質見た目、Unity／VRChat実機は未受入。

# 2026-09-15 PERF-05: fitサマリーのavatar評価キャッシュ

装着Panelのfit／weight対象サマリーは頂点選択変更のたびに更新されるため、対象avatarのGraphEvaluationを毎回再実行すると大きなavatarほどUI操作の負荷が増える。Graphが不変であることを利用し、対象ObjectIdとGraphインスタンスが同じ間は前回の評価結果を再利用するキャッシュを追加した。Graph操作・対象切替・再読込でGraphインスタンスが変わった場合だけ再評価する。fit計測、保存データ、Undo、出力結果の意味は変更していない。

- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.AttachmentState.cs`
- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Attachments.cs`
- Unity 6000.4.3f1 隔離Windows Player `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Builds\SourceSkinCacheV5\NyaForge.exe`：build成功
- Authoring自動検証: **88 checks PASS**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Artifacts\Authoring-20260915-081829-b889542fdc0e4c0d99823a773e652fb1\report.json`）
- Core: **515 passed / 0 failed**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-39b4c52f3ec64ba6a32db0667263a20b`）
- 境界: 自動回帰と不変Graph評価の再利用確認であり、実EditorWindowのGC/native memory計測、実マウスのDPI／IME、実RadDollV3全周fit・貫通・材質見た目、Unity実SDK、VRChat実機受入は未完了。

# 2026-09-15 PERF-06: fitサマリー評価の解放境界

制作対象を空projectへ戻したときや装着先avatarが外れたときに、fitサマリーが保持していた大きなGraphEvaluationを明示的に解放する境界を追加した。対象切替時の再評価キャッシュと合わせ、古いavatarのmesh評価をUI状態が参照し続けないようにした。表示内容、fit計測、保存形式、Undo、出力は変更していない。

- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Attachments.cs`
- Unity 6000.4.3f1 隔離Windows Player `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Builds\SourceSkinCacheV6\NyaForge.exe`：build成功
- Authoring自動検証: **88 checks PASS**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Artifacts\Authoring-20260915-082218-2508a45a9961408283b3b1dc6c9328ff\report.json`）
- Core: **515 passed / 0 failed**（直近Coreソース変更なし）
- 境界: 自動回帰によるキャッシュ無効化経路の確認であり、実EditorWindowのGC/native memory計測、実マウスのDPI／IME、実RadDollV3全周fit・貫通・材質見た目、Unity実SDK、VRChat実機受入は未完了。

# 2026-09-15 REAL-CLOTHING-08: 軽量化後の実RadDollV3一周

最新のfitサマリー評価キャッシュ／解放境界を反映した隔離Windows Player `RealClothingV7`で、private `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-RealModelSmoke\RadDollV3_VRM.vrm`を使った実モデル自動一周を再実行した。取込→全mesh→制作形状追加→頂点編集→skin／装着→native Save/Open→GLB／VRM出力→衣装package生成まで **97 checks PASS**。生成packageのskeleton sidecarは4 bonesで、Unity **2022.3.22f1** Bridge受け取りも **16 checks PASS**（status `passed`）となった。

- Player: `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Builds\RealClothingV7\NyaForge.exe`
- Player report: `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Artifacts\Authoring-20260915-082445-5015fc2033b64326ace23df85ea75d2b\report.json`
- Clothing package: `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Artifacts\Authoring-20260915-082445-5015fc2033b64326ace23df85ea75d2b\real-clothing-project\exports\clothing-20260914-232807-0f560a\skinned-clothing.nyaforge.json`
- Bridge report: `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Artifacts\BridgeReceiver-20260915-082831-438-16d1b54182ec46b59de121237461fc7d\bridge-report.json`
- 境界: これはprivate実モデルの自動Player／Bridge経路の確認であり、実EditorWindowのマウス操作、全周fit・貫通・材質見た目、avatar移動／回転／scale、VRChat Build & Test／実機表示、販売品質の合格には読み替えない。private素材とUnity SDKは公開ツリーへ追加していない。

# 2026-09-15 PERF-07: attachment target evaluationの共有

装着Panelのfitサマリーだけでなく、avatar表面の表示・面選択・fit測定・fit適用・surface weight・poseコピーでも同じavatar GraphEvaluationを使うよう、`EvaluateAttachmentTarget`へ集約した。対象ObjectIdと不変Graph参照が同じ間は再評価せず、対象変更・Graph置換時だけ更新する。fit・weight・poseの計算結果や保存形式は変更していない。

- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Attachments.cs`
- 隔離Player `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Builds\SourceSkinCacheV8\NyaForge.exe`：build成功
- 通常Authoring: **88 checks PASS**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Artifacts\Authoring-20260915-083149-b11f5765a1b0432cb338f1b9e4211259\report.json`）
- 実RadDollV3自動一周: **97 checks PASS**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Artifacts\Authoring-20260915-083227-6ec20b3fb8804fed8c035e54bd99d33e\report.json`）
- Unity Bridge: **16 checks PASS**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Artifacts\BridgeReceiver-20260915-083546-672-e5646219b9824c50b34f358c08d8f1cb\bridge-report.json`）
- Core: **515 passed / 0 failed**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-9ed55fff469b483b86c15bd8f5a6192d`）
- 境界: 自動経路の評価共有確認であり、実EditorWindowのGC/native memory計測、実マウスのDPI／IME、実RadDollV3全周fit・貫通・材質見た目、avatar移動／回転／scale、VRChat Build & Test／実機表示は未完了。

# 2026-09-15 GUI-12: 狭幅・高DPIでの操作ボタン折返し

日本語の長いボタン名がUI Toolkitの固有最小幅に引っ張られ、狭いcontrols欄やDPI拡大時に右端で切れる経路を共通UI部品で修正した。`Row`へ幅制限を追加し、共通`Button`へ最小幅0・縮小・通常折返し・自動高さを設定した。MCP接続Panelと説明欄も親幅へ追従するようにした。MCPのinstance ID自体は長いため、入力欄の省略表示とtooltipで全文を確認する既存契約を維持している。

- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.UiElements.cs`, `AuthoringWorkbench.McpPanel.cs`
- commit: `11449cf` (`fix: keep authoring controls readable on narrow windows`)
- 隔離Player: `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Builds\ResponsiveUiV1\NyaForge.exe`（Unity 6000.4.3f1）build成功
- Authoring: **PASS / 88 checks**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Artifacts\Authoring-20260915-084045-5d2b408cbf0b405999242aa3f498af38\report.json`、capture `authoring.png`）
- Navigation: **PASS**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Artifacts\Navigation-20260915-084124-f24633668f974a79bf6cedd06fe9edfc\report.json`）
- 境界: 自動captureでcontrolsの折返しと既存導線を確認した。実EditorWindowのDPI 100/150/200%、IME、Explorer、実マウスでの最終目視は未受入のまま。実RadDollV3全周fit・貫通・材質見た目、Unity／VRChat実機も未受入。

# 2026-09-15 GUI-13: 装着先の表示名・役割・完全ID照合

装着Panelのavatar候補が短い内部IDだけで表示され、複数の制作対象を見分けにくかった。候補を保存済み表示名・`avatar graph`／`static`役割・短縮IDの組合せへ変更し、選択中avatarは完全ObjectIdをtooltipへ表示するようにした。Bone候補も選択中の名前とstable BoneIdをtooltipで照合でき、装着状態欄は解決できるBone名を優先して表示する。graph、保存形式、Undo、packageの内容は変更していない。

- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Attachments.cs`
- 回帰: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.AttachmentVerification.cs`へ表示名・完全ID tooltipの確認を追加
- source commit: `9f7b71e` (`ux: identify attachment targets by display name`)
- 隔離Player: `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Builds\AttachmentLabelsV2\NyaForge.exe`（Unity 6000.4.3f1）build成功
- Authoring: **PASS**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Artifacts\Authoring-20260915-085136-93d1c9e9c9fe418cbfc37caa42ce698f\report.json`）
- 実RadDollV3一周: **97 checks PASS**、Unity 2022.3.22f1 Bridge **16 checks PASS**（同じ表示名修正を含むAttachmentLabelsV1、`Artifacts/Authoring-20260915-084652-61a929c05aa04d7aae373f9ff15d7d19/report.json`、`Artifacts/BridgeReceiver-20260915-085015-177-f0109053579640008a77878d40088da6/bridge-report.json`）
- 境界: 自動回帰とprivate実モデルのpackage受け取りまで。実EditorWindowでの候補選択・tooltip目視、DPI／IME／Explorer、全周fit・貫通・材質見た目、VRChat実機は未受入。

# 2026-09-15 MANUAL-01: Windowsネイティブ制作画面の初期表示

AGENTS.mdの手順どおり`@oai/sky`で、隔離ビルド `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-MorphTargetContextV1-src\Builds\AttachmentLabelsV2\NyaForge.exe` の実ウィンドウを一意に取得し、Viewerから`制作へ`をクリックした。1069×698のWindowsウィンドウで、空プロジェクト案内、MCP見出し、`新しい空プロジェクト`／`モデルを開く…`／`保存済み制作を開く…`の長い日本語ボタンが右controls欄内に収まり、横方向へ切れずに表示されることを目視確認した。ステータスも画面下部に表示された。

- 手動確認: **部分PASS**（初期空状態の表示・導線・折返し）
- 未確認: 実Explorer選択、実モデル取込、装着先候補の表示名／tooltip、DPI 150/200%、IME、長時間編集、実RadDollV3の全周fit・貫通・材質見た目、Unity／VRChat実機
- Computer Use: `@oai/sky`で実施。ブラウザ用CUAの`apps: []`判定は使用していない。

# 2026-09-15 GUI-16: 空projectのモデル取込欄を表示・自動追従

空projectで右側の`モデルを開く…`を押したとき、Foldoutの値だけが開き、`RefreshContextVisibility`の表示ゲートによりモデル取込欄が`display:none`のままになる経路があった。表示処理を`ShowModelImportPanel`へ統一し、ファイル選択後は候補欄のレイアウト確定を待って縦スクロールするようにした。候補確認のstatusと、node／mesh／skin選択・完全path欄が同じ操作欄内で見える状態を確認できる。

- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Layout.cs`
- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ContextVisibility.cs`
- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Import.cs`
- Unity 6000.4.3f1 Player `Builds/ImportScrollV4/NyaForge.exe`: build成功
- Authoring自動検証: **PASS**（`Artifacts/Authoring-20260915-091419-80188523b5714a6cb6df1692bffc4749/report.json`）
- Core: **515 passed / 0 failed**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-3d69b6db6d854d59a3057a31f2942917`）
- Windows native `@oai/sky`: 空projectの`モデルを開く…`からモデル取込Foldoutを表示し、右controlsがファイルパス・node／mesh／skin・`候補を確認`・取込ボタン付近へ自動追従することを目視確認した。実ファイルの選択と取込完了はこの確認では行っていない。
- 境界: 自動スクロールの画面到達性と空project表示ゲートの確認であり、DPI 150/200%、IME、長いpath、実RadDollV3の取込・fit・貫通・材質、Unity／VRChat実機は未受入。生成Playerは公開ツリーへ追加していない。

# 2026-09-15 REAL-CLOTHING-09: 最新実モデルpackageのUnity Bridge受け取り

`Builds/ImportScrollV4`を反映した実RadDollV3自動一周で、private VRMから生成したskinned clothing packageをUnity Bridgeへ渡した。Player側は **95 checks PASS**（GLB/VRM取込、全mesh取込、形状追加、頂点編集、明示skin-bind、native Save/Open、GLB/VRM出力、package生成）。package manifestは225 vertices／384 trianglesで、skeleton sidecarは4 bonesだった。Unity **2022.3.22f1** Bridge側も **16 checks PASS**。移動・回転・scaleしたavatar rootでのavatar-local配置、BoneId map、StateHash更新、所有物削除＋Undo、複数package所有権、normal／MR変換、package hash検証を確認した。

- Player report: `Artifacts/Authoring-20260915-091912-0de78bcafc1f48599416beae139509bd/report.json`
- Clothing package: `Artifacts/Authoring-20260915-091912-0de78bcafc1f48599416beae139509bd/real-clothing-project/exports/clothing-20260915-002121-35f3b0/skinned-clothing.nyaforge.json`
- Bridge report: `Artifacts/BridgeReceiver-20260915-092331-827-721df21cdd2d42febe1976ab703625a0/bridge-report.json`
- 境界: 自動Player／Bridge経路の確認であり、実EditorWindowでの全周fit・貫通・材質見た目、実マウスのDPI／IME、VRChat Build & Test／実機表示、販売品質の合格には読み替えない。private素材・生成物・Unity SDKは公開ツリーへ追加していない。

# 2026-09-15 GUI-17: controls欄の狭幅レスポンシブ化

固定356pxだった制作controlsを、実Windows Playerでは画面幅32%（最小260px・最大420px）でレイアウトするよう変更した。狭いウィンドウでviewportを確保しながら、通常／最大化画面では日本語ラベル・寸法入力・作業モードを読みやすく保つ。ポインタ座標を使う注入UI検証では従来幅を維持し、既存回帰の座標契約を変えていない。最大化した実Windowsウィンドウで、空projectの開始案内、作業モード、基本形状の種類・寸法・追加ボタンが右欄内に収まることを目視確認した。

- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Layout.cs`
- Player: `Builds/CompactControlsV2/NyaForge.exe`（Unity 6000.4.3f1）build成功
- Authoring: **88 checks PASS**（`Artifacts/Authoring-20260915-093040-d253b457a8514999bf1897a12830309c/report.json`）
- Navigation: **PASS**（`Artifacts/Navigation-20260915-093316-fa72cfdb2c8e458d9b3408a3b1478854/report.json`）
- Core: **515 passed / 0 failed**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-39aae153ab1f4a7683f5796b2008b4a7`）
- 実RadDollV3自動一周: **95 checks PASS**（`Artifacts/Authoring-20260915-093345-debbe91188f949d7b59c6cdf22089fcd/report.json`）。生成packageのUnity 2022.3.22f1 Bridge受け取りも **16 checks PASS**（`Artifacts/BridgeReceiver-20260915-093632-021-5f3425a2c8c04b1db26ab057e1520374/bridge-report.json`）。
- 境界: 最大化画面の実表示確認であり、DPI 100/150/200%を個別に切り替えた最終受入、狭い非最大化窓での全コントロール目視、IME、実EditorWindow全周fit・貫通・材質、VRChat実機は未受入。生成物・private素材は公開ツリーへ追加していない。

# 2026-09-15 MANUAL-10: 実Explorer取込と基本形状追加

`Builds/CompactControlsV2/NyaForge.exe`を最大化したWindows native `@oai/sky`で操作した。上部の`モデルを追加`からWindowsファイルダイアログを開き、表示されたRadDollV3 VRMを選択して開いた。取込Panelで候補を確認し、mesh 0／skin 0を選択した状態から`選択候補を取り込む`を実行した。statusに`GLB skinを取り込みました`、mesh／bone／weight件数、取込診断が表示され、対象meshがviewportへ表示された。その後、上部の`基本形状を追加`でリング（チョーカー）の寸法欄を開き、既定値のまま`この寸法で形状を追加`を実行した。statusにチョーカー形状追加が表示され、viewportに追加形状が現れた。

- [x] 実ExplorerでVRMを選択
- [x] 候補確認と選択mesh取込
- [x] 基本形状パネルを開いてチョーカー追加
- [ ] 全mesh一括、頂点編集、skin-bind、保存／再開、fit・貫通、Unity／VRChat実機（既存MANUAL-05／自動REAL-CLOTHINGで一部確認済み）

これは実Explorerと実マウスによる取込・形状追加の手動受入であり、今回は保存を実行していない。private素材・制作データは公開ツリーへ追加していない。

# 2026-09-15 MCP-03: Undo/Redo後のVRM metadata再同期

MCPの`history.undo`／`history.redo`後に、workspaceのattachment bytesだけが履歴復元され、Workbenchが保持するgraph別のVRM rig・expression・Spring session辞書が旧状態のまま残る経路を修正した。`ExecuteMcpCommand`でもGUIの共通履歴経路と同じ`RefreshImportedVrmSessionsFromWorkspace`を先に呼び、続く表示更新・VRM出力が現在のdocumentとmetadataを参照するようにした。保存形式、MCP wire、通常の編集操作は変更していない。

- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.McpCommands.cs`
- Core: **515 passed / 0 failed**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-799de624031b4c53b703b89b8e4438a8`）
- Windows Player build: `Builds/McpMetadataRefreshV1/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260915-095556-250.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260915-095625-9d47e2d3351543b0a7c11dbce13cdf07/report.json`）
- 境界: MCP実sidecarからのVRM表情／Spring Undo/Redo目視、実EditorWindowのDPI／IME、実VRChat SDK／実機表示は別受入として未完了。

# 2026-09-15 MOD-06: attachment metadata同期境界の共通化

GUIとMCPの履歴処理で個別に並んでいたrig／VRM expression／Spring／参照保護／納品対象の再読込を、`AuthoringWorkbench.MetadataRefresh.cs`の`RefreshMetadataFromWorkspace`へ集約した。Undo/Redoでdocumentとattachmentが同時に戻るとき、両経路が同じ順序でインメモリキャッシュを再構築してからPanel更新・VRM出力へ進む。保存形式、MCP wire、attachment codec、編集commandの意味は変更していない。コードマップにも責務を追記した。

- 追加: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.MetadataRefresh.cs`（`.meta`を含む）
- 接続: `AuthoringWorkbench.Execution.cs`, `AuthoringWorkbench.McpCommands.cs`
- 設計記録: `docs/Authoring-Code-Map.md`
- Player build: `Builds/MetadataBoundaryV1/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260915-100143-693.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260915-100209-925f3381c011428da8ec7ebbddb9edc6/report.json`）
- Core: 直近 **515 passed / 0 failed**（MetadataRefreshはUnityRuntimeのみ）
- 境界: 実VRM expression／Springを外部MCPでUndo/Redoする目視、実EditorWindowのDPI／IME、実Unity SDK／VRChat実機は別受入として未完了。

# 2026-09-15 REAL-CLOTHING-10: metadata境界共通化後の実RadDollV3一周

`Builds/MetadataBoundaryV1`へ`RefreshMetadataFromWorkspace`共通化を反映したPlayerで、privateの`RadDollV3_VRM.vrm`を使う実モデル自動一周を再実行した。前回の120秒検証は実モデル到達前にタイムアウトしていたため、今回は`-TimeoutSeconds 300`で切り分けた。GLB／VRM取込、候補確認、全mesh経路、チョーカー形状追加、頂点編集、明示Neck skin-bind、native Save/Open、選択衣装package出力まで完了し、**Authoring check PASS**。出力packageはGLBとBoneId付きsidecarを含む。

- Player: `Builds/MetadataBoundaryV1/NyaForge.exe`（Unity 6000.4.3f1）
- report: `Artifacts/Authoring-20260915-100811-909ac0e8d1c1407b9e393541fdbb9372/report.json`
- capture: `Artifacts/Authoring-20260915-100811-909ac0e8d1c1407b9e393541fdbb9372/authoring.png`
- clothing package: `Artifacts/Authoring-20260915-100811-909ac0e8d1c1407b9e393541fdbb9372/real-clothing-project/exports/clothing-20260915-010955-4556a5/skinned-clothing.nyaforge.json`
- 実行ログには、衣装派生・保存・`skinned-clothing.nyaforge.json`出力・`NYAFORGE_AUTHORING_CHECK PASS`を確認した。
- 境界: 自動Playerでの実モデル経路確認であり、実EditorWindowのDPI／IME／Explorer全周fit・貫通・材質見た目、Unity／VRChat実機、販売品質の合格には読み替えない。private素材・生成物は公開ツリーへ追加していない。

# 2026-09-15 MANUAL-12: 現行Playerでの実Explorer取込・形状追加・保存

`Builds/ManualCurrentV1/NyaForge.exe`を`@oai/sky`で起動し、制作画面を最大化して実操作した。上部の`モデルを追加`からWindowsのファイルダイアログを開き、privateの`RadDollV3_VRM.vrm`を選択してEnterで確定した。候補欄でmesh 0／skin 0を確認し、`選択候補を取り込む`をクリックするとviewportにモデルが表示され、statusにmesh 0・bone 171・weight 2990と診断が出た。続けて`基本形状を追加`から既定のリング（チョーカー）を追加し、viewportに編集可能なリングと対象モデルを表示した。上部の`保存`を実行し、statusにAppData配下の制作フォルダへの保存完了が表示された。`ビューアーに戻る`→`制作へ`の画面遷移でも保存済み状態と対象表示が維持され、終了時に未保存確認は出なかった。最後にPlayerを閉じ、残留ウィンドウがないことを確認した。

- Player: `Builds/ManualCurrentV1/NyaForge.exe`（Unity 6000.4.3f1）
- 手動確認: **PASS**（実Explorer選択、候補確定、skin取込、基本形状追加、保存、Viewer／Authoring遷移、終了）
- 確認できた表示: 最大化時の取込欄・寸法入力・頂点編集・Rig／Morph／出力の作業欄を右controlsで確認。長い説明文はパネル内で折返し、横方向の切れは見られなかった。
- 未確認: DPI 150/200%、IME入力、全周fit・貫通・材質見た目、Unity／VRChat実機、実package受け取り。自動Player／Bridgeの合格や今回の手動保存を販売品質へ読み替えない。
- private素材・手動制作データ・生成Playerは公開ツリーへ追加していない。

# 2026-09-15 UX-IMPORT-FIRST: 取込Panelの最初の操作を先頭へ配置

モデル取込Panelの短い3ステップ案内の直後に、`① GLB / VRMを選ぶ`と`選択したファイル`を配置した。VRMの技術説明・SpringBone・Rig・診断情報は長くなり得るため、初回利用者が長いスクロールをしなくても最初の操作へ到達できる導線にした。コマンドバーの`モデルを追加`と同じExplorer／PickModel経路を使い、選択ファイルの完全パスは従来どおりtooltipへ表示する。取込処理・保存形式・出力形式は変更していない。

- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Import.cs`
- Windows Player: `Builds/ImportFirstActionV1/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260915-112654-684.log`）
- Authoring自動回帰: **PASS**（`Artifacts/Authoring-20260915-112716-68877039adb24fa486e3d6eda695dc45/report.json`）
- 手動確認: Windows native `@oai/sky`で`--authoring true`を起動し、制作画面の`モデルを追加`から取込Panelを開くところまで確認。新ビルドの実スクリーンショット確認は継続中。
- 境界: 既存のDPI・IME・長い日本語名、全周fit・貫通・材質見た目、Unity／VRChat実機受入は別カードのまま。

# 2026-09-15 検証: UX-IMPORT-FIRST後のCore回帰

`main`の`a6c74a6`（取込Panel先頭導線）へ対して、`dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore`を再実行し **515 passed / 0 failed** を確認した。artifactは`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-eace1477c19145728e36c48da68f8366`。この回帰はCoreの保存・再開・頂点編集・材質・GLB/VRM出力契約を確認するもので、実EditorWindowのDPI・IME・長いパス、実アバターの全周fit・貫通・材質見た目、Unity／VRChat実機受入とは別境界である。

# 2026-09-15 MANUAL-13: ImportFirstActionV1の取込導線を実画面確認

`Builds/ImportFirstActionV1/NyaForge.exe --authoring true`をWindows native `@oai/sky`で起動し、空の制作画面の上部`モデルを追加`からGLB／VRM取込Panelを開いた。右Panelを先頭へ戻すと、短い案内の直後に`① GLB / VRMを選ぶ`ボタンと`選択したファイル`欄が表示され、技術診断より前に最初の操作へ到達できることを確認した。ボタンからExplorerが開き、キャンセル後も制作画面へ戻れることを確認した。

- 手動確認: **PASS**（空状態→モデル追加→Explorer表示→キャンセル→取込Panel先頭導線）
- 確認画像: `C:/Users/tomoaki/AppData/Local/Temp/nya-import-panel-home3.png`
- 境界: 実モデルの取込・保存・再開、DPI／IME、長いパス、全周fit・貫通・材質見た目、Unity／VRChat実機、販売品質は別受入。Explorerで表示した場所はprivate素材領域で、追跡対象へ追加していない。

# 2026-09-15 WINDOWS-CANDIDATE-01: 標準Windows候補を現行mainへ更新

標準起動先の`Builds/Windows/NyaForge.exe`が古いビルドだったため、現行`main`で再ビルドした。Player buildは成功し、同じ候補で空状態のAuthoring回帰、Navigation回帰、private RadDollV3の実衣装一周を順に実行した。

- Player: `Builds/Windows/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260915-114157-388.log`）
- Core: **515 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-5ee5363390014bfb82084abe9f9eb170`）
- Authoring startup: **PASS**（`Artifacts/Authoring-20260915-114236-35f29d8f874540f784136b094cec120a/report.json`）
- Navigation: **PASS**（`Artifacts/Navigation-20260915-114308-a2ccc377839e4bbfabe4bdd5cb79808a/report.json`）
- 実RadDollV3一周: **97 checks PASS**（`Artifacts/Authoring-20260915-114332-958668cbb20444feab3e93741125c654/report.json`）。取込、全mesh、頂点編集、チョーカー、skin-bind、native Save/Open、GLB／VRM1、衣装packageまで確認。
- Unity Bridge: **16 checks PASS**（`Artifacts/BridgeReceiver-20260915-114642-479-fa21e65183d946c98306b42cb5cabb02/bridge-report.json`）。移動・回転・scale avatar root、BoneId、更新／削除Undo、normal／MRを確認。
- 境界: 自動Player／Bridgeの合格は、実EditorWindowのDPI／IME、全周fit・貫通・材質見た目、VRChat Build & Test／実機表示、販売品質の合格には読み替えない。private素材・生成物・Unity SDKは公開ツリーへ追加していない。

# 2026-09-15 WINDOWS-CANDIDATE-02: 現行標準候補の反復起動回帰

現行`Builds/Windows/NyaForge.exe`でNavigationの起動・終了を20回反復し、**20/20 PASS**を確認した。画面サイズは1280×800、各回の個別reportと集約結果を保存している。

- 集約: `Artifacts/Navigation-Repeated-Windows-20260915-115310.json`
- 境界: 起動／終了とNavigationの回帰であり、2時間編集、DPI 150/200%、IME、実アバターの全周fit・貫通・材質見た目、VRChat実機は未受入。生成物は公開ツリーへ追加していない。

# 2026-09-15 MANUAL-14: 現行Windows候補の実Explorer取込・チョーカー・保存

現行`Builds/Windows/NyaForge.exe`をWindows native `@oai/sky`で起動し、空の制作画面から実操作した。`モデルを追加`でExplorerを開き、privateの`RadDollV3_VRM.vrm`をフォルダ移動と行選択で指定した。候補欄に`mesh 0 (Bag.baked)`、skin 0・171 bone、10 instancesを確認し、`選択候補を取り込む`でviewportへモデルを表示した。`基本形状を追加`を開くと、既定の`リング（チョーカー）`寸法と`① GLB / VRMを選ぶ`から始まる取込導線が同一Panelに表示され、`この寸法で形状を追加`で編集可能なリングを追加できた。上部`保存`を実行し、制作対象が`保存済み`になり、native正本がAppData配下へ保存されたことを確認した。

- 手動確認: **PASS**（現行標準候補、実Explorer、VRM候補確認、skin取込、チョーカー追加、保存）
- 確認画像: `C:/Users/tomoaki/AppData/Local/Temp/nya-current-import-candidates.png`, `C:/Users/tomoaki/AppData/Local/Temp/nya-current-manual-imported.png`, `C:/Users/tomoaki/AppData/Local/Temp/nya-current-manual-choker-added.png`, `C:/Users/tomoaki/AppData/Local/Temp/nya-current-manual-saved.png`
- 保存先表示: `C:/Users/tomoaki/AppData/LocalLow/NyaForge/NyaForge/Authoring/Project-14df807e`
- 境界: 現行候補での実Explorer／取込／形状追加／保存の確認であり、再起動後の手動再Open、DPI 150/200%、IME、全周fit・貫通・材質見た目、Unity／VRChat実機は未受入。private素材・生成物は公開ツリーへ追加していない。

# 2026-09-15 UX-RECENT-PROJECT: 最後の制作を安全に再開

Windows向けの制作画面に、native保存した制作フォルダを再利用する小さなpointerを追加した。pointerは`Application.persistentDataPath/Authoring/last-project.pointer`へUTF-8で保存し、保存 payloadそのものは複製しない。読み込み時はパス長・文字・存在を検査したうえで`project.nyaforge.json`を`NativeProjectLocator`で検証するため、壊れた／古いpointerは無効としてExplorer選択へ戻せる。正常保存後だけatomic replaceで更新し、pointer書込み失敗は本体保存を失敗扱いにしない。

空の制作状態と制作・出力Panelから`最後の制作を再開`を押せるようにし、未保存確認を通してから既存の`OpenProject`へ接続した。Windows native `@oai/sky`で新Playerを起動し、空状態から右Panelをスクロールしてボタンを押し、未保存変更の確認で`変更を破棄して進む`を選択したところ、保存済みpointerのfixture projectが開き、制作対象・頂点表示・保存済み状態が表示された。Computer Useの再接続後の実操作で再開経路を確認できた。

- 追加: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.RecentProject.cs`（`.meta`を含む）
- 接続: `AuthoringWorkbench.Layout.cs`, `ProjectOutput.cs`, `PersistenceRefresh.cs`, `Saving.cs`, `State.cs`
- pointer実体: `C:/Users/tomoaki/AppData/LocalLow/NyaForge/NyaForge/Authoring/last-project.pointer`（自動Authoringでも生成とmanifest存在を確認）
- Core: **515 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-582ff3af37d44e3dbd8b0ae4e90c29f0`）
- Windows Player: `Builds/RecentProjectV1/NyaForge.exe`（`Logs/build-player-20260915-121904-471.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260915-122007-81db73a7e35a4a32b4463bd5a0921eaa/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260915-122404-46e459705ff44a6f94351ace5e5150d6/report.json`）
- 境界: pointerは再開を便利にする補助情報で、native manifestが正本。実RadDollV3の全周fit・貫通・材質見た目、DPI／IME、Unity／VRChat実機、販売品質は別受入のまま。private素材・生成物は公開ツリーへ追加していない。

# 2026-09-15 MANUAL-15: 実RadDollV3の保存後再起動・pointer再開

`Builds/RecentProjectV1/NyaForge.exe --authoring true`をComputer Useで最大化して起動し、private `RadDollV3_VRM.vrm`をExplorerから選択した。候補確認後に全mesh（10 objects、171 bone、SpringBone 5 chain / 53 joint / 4 collider group）を取り込み、既定リング（チョーカー）を追加して保存した。保存先は`C:/Users/tomoaki/AppData/LocalLow/NyaForge/NyaForge/Authoring/Project-3688f569`で、保存後pointerもこのnative projectを指した。

Playerを終了して同じbuildを再起動し、`最後の制作を再開`を押した。未保存確認で`変更を破棄して進む`を選ぶと、実RadDollV3とチョーカーを含む制作状態が再読込され、上部に`保存済み`、statusに`制作状態を開きました`が表示された。pointerによる実モデルの再開を、Computer Useの再接続後に手動確認できた。

- 手動確認: **PASS**（private実VRM選択、全mesh取込、チョーカー追加、保存、Player再起動、pointer再開）
- 実体: `C:/Users/tomoaki/AppData/LocalLow/NyaForge/NyaForge/Authoring/Project-3688f569/project.nyaforge.json`
- 境界: 再開後の全周fit・貫通・材質見た目、頂点編集の実マウス一周、DPI／IME、Unity／VRChat実機、販売品質は別受入。生成物とprivate素材は公開ツリーへ追加していない。

# 2026-09-15 UX-FRAME-01: native再開後の表示フレーム順序を修正

workspaceを開き直した直後にprojection更新前の点群で`Frame()`していたため、再開した実アバターが極端に拡大される場合があった。`ReplaceWorkspace`の順序を`Select → Refresh → Frame`へ変更し、新しいworkspaceの最終projectionを基準にカメラを合わせるようにした。保存形式・カメラの保存契約・編集操作は変更していない。

- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ProjectActions.cs`
- Core: **515 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-59afa0299f724a5eb7aaeb3daf3472b1`）
- Player build: `Builds/RecentProjectFrameV1/NyaForge.exe`（`Logs/build-player-20260915-124019-665.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260915-124107-ab195aafab664431967bd76f2af987f6/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260915-124143-a877801d1e8346d7839eeabf16ab2cdd/report.json`）
- 手動確認: Computer Useでpointerから実RadDollV3＋チョーカーを再開し、再開直後に全身がviewport内へ収まることを確認。旧実装で発生した極端な拡大状態を再現しない。
- 境界: 実EditorWindowのDPI／IME、全周fit・貫通・材質見た目、Unity／VRChat実機、販売品質は別受入。生成Player・private素材は公開ツリーへ追加していない。

# 2026-09-15 UX-OUTPUT-MODE-01: 確認・出力の導線を出力Panel優先へ整理

作業モードの`確認・出力`がMorph／表情差分を先頭へ指定していたため、保存・GLB・VRM・衣装packageを探す利用者が別の編集欄へ移動していた。`FocusWorkMode`の対象順を`projectOutputPanel → validationPanel → evidencePanel → morphPanel`へ変更し、出力モードの最初の対象を保存／受け渡しPanelにした。各Panelの実装、command、保存形式、MCP wireは変更していない。

- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.WorkModes.cs`
- Player build: `Builds/OutputModeV1/NyaForge.exe`（`Logs/build-player-20260915-124550-123.log`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260915-124614-ad0875c474404ae48b9a6c3aa6074ec4/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260915-124654-a257fd37f44147669be04e6e703a468e/report.json`）
- 手動確認: Windows native Computer Useで`確認・出力`を押し、statusに`確認・出力の設定を表示しました`が表示されることを確認。出力Panel優先の順序を実画面へ反映した。
- 境界: 実EditorWindowのDPI／IME、実RadDollV3の全周fit・貫通・材質見た目、Unity／VRChat実機、販売品質は別受入。生成Player・private素材は公開ツリーへ追加していない。

# 2026-09-15 UX-SEMANTIC-TEXTURE-01: UV1 semantic texture の適用導線を明示

Normal／metallic-roughness画像のWindows v1出力はUV0のみ対応するため、UV dropdownでUV1を選んだ状態では両方の「画像を適用」ボタンを無効化し、semantic summaryに「UV1はWindows v1非対応。UV0へ戻してから適用してください。」と表示するようにした。材質未選択・再読込後も同じ可否判定を更新し、UV0へ戻すと両ボタンが再び有効になる。既存のファイル選択、画像bytesの保存形式、UV1を将来保持する契約は変更していない。

- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Materials.cs`, `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.MaterialUiVerification.cs`
- Player build: `Builds/SemanticTextureUiV3/NyaForge.exe`（`Logs/build-player-20260915-130233-678.log`）
- Core: **515 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-2d20e27182544e148854b992e3e5606b`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260915-130255-c57a3a62ed7f4d9bbdb81d3b3f6f3f7f/report.json`）。UV1で適用不可・UV0で再有効化・Normal/MR保存を確認。
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260915-130605-23d76250ce004bdbb6c71f102499a104/report.json`）
- Computer Use: 再接続を試行したが、この時点のセッションは `Trusted RPC service is not configured: sky` で画面操作サービス未接続。自動Player検証のみ完了。
- 境界: 実EditorWindowでのDPI／IME、実アバター材質のGPU画素見た目、Unity／VRChat実機は別受入。private素材・生成Playerは公開ツリーへ追加していない。

# 2026-09-15 UX-FIT-SUMMARY-01: 装着Panelのfit要約を狭い画面向けに整理

装着Panelのfit要約は、avatarの内部ID・頂点数・三角形数・bind状態を一行へ詰め込んでいたため、右側の狭いcontrolsで末尾が見えにくかった。Panelの高さを増やして折り返すと既存のポインタ検証で下部操作が画面外へ移るため、表示欄の高さは維持し、画面には「fit対象: 衣装範囲・avatar面範囲・計測状態」の短い要約だけを表示するようにした。完全な対象ID、件数、bind状態、rest mesh確認はツールチップへ残し、選択範囲と計測状態の変更時も同じ情報源から更新する。保存形式、fit／weight計算、MCP wireは変更していない。

- 変更: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Attachments.cs`, `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.AttachmentUi.cs`
- Player build: `Builds/AttachmentSummaryCompactV1/NyaForge.exe`（Unity 6000.4.3f1、`Logs/build-player-20260915-131933-535.log`）
- Core: **515 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-19edabfe884446da8ecb06641927d191`）
- Authoring回帰: **PASS**（`Artifacts/Authoring-20260915-131955-0a72f29e4e984820989fc9bbe4814a6c/report.json`）
- Navigation回帰: **PASS**（`Artifacts/Navigation-20260915-132305-751a18cb65034f7db32e6f706b5a3fd7/report.json`）
- Unity Bridge: **PASS**（`Artifacts/BridgeReceiver-20260915-132325-203-1d27d21d1d4d487aa67cbc02c17cafb3/bridge-report.json`）
- Computer Use: 再接続を試行したが、このセッションではnative `apps: []`、`cua.getApp`／`cua.listApps`未提供でWindowsアプリ操作は未接続。ブラウザ操作のみ利用可能。
- 境界: 実EditorWindowのDPI／IME、実アバター全周fit・貫通・材質見た目、VRChat実機、販売品質は別受入。private素材・生成Player・Artifactsは公開ツリーへ追加していない。

# 2026-09-15 COMPUTER-USE-RETRY-01: native Computer Use の再接続確認

ブラウザ側の `cua` ではなく、AGENTS.mdで指定されたWindowsネイティブ `@oai/sky` 経路を再初期化した。`Z:\TextureVoice_local\git\NyaForge\Builds\AttachmentSummaryCompactV1\NyaForge.exe` のウィンドウを1件へ絞り、window id `11927658` を取得。起動時Viewer画面から座標クリックで「制作へ」を実行し、制作プレビュー画面へ遷移したことを直後のスクリーンショット（856×565）で確認した。

- 結果: **native Computer Use 接続・クリック・画面更新を確認**。
- 境界: AXツリーはUnity描画UIの子要素を公開しないため、今回の確認は座標クリックによる制作画面遷移まで。モデル選択・ファイルダイアログ・実アバター編集は次の手動受入で行う。
- 公開境界: private素材、生成Player、Artifactsは公開ツリーへ追加していない。

# 2026-09-15 COMPUTER-USE-RETRY-02: native ExplorerでRadDollV3候補を確認

同じWindows native `@oai/sky`接続でNyaForgeを再取得し、Viewerの「制作へ」からAuthoringへ遷移した。上部の「モデルを追加」を押してExplorerを開き、private `Z:\TextureVoice_local\git\RadDollV3-clothing\private\viewer-data\packs\avatar-raddollv3-local\RadDollV3_VRM.vrm`を選択・確定した。NyaForgeの「候補を確認」で `mesh 0 (Bag.baked, 1 primitive)・skins 10・instances 10・source 6e5e0a0a82c2` を表示でき、ファイル選択と候補確認までを実画面で確認した。

- 結果: **native Computer Use接続、Viewer→Authoring、Explorer選択、候補確認をPASS**。
- 未完了: このビルドの候補欄は「取込対応: なし」と表示され、実モデルの取り込み完了・編集・保存までは確認していない。AXツリーはUnity描画UIの子要素を公開しないため、座標操作後のスクリーンショットで判定した。
- 公開境界: private素材、生成Player、Artifactsは公開ツリーへ追加していない。

# 2026-09-15 COMPUTER-USE-RETRY-03: 現行Windowsビルドで取込・形状追加・保存を確認

前回の作業重複を避けるため、AGENTS.mdで指定されたnative `@oai/sky` 接続を再利用し、現行ビルド `Builds/Windows/NyaForge.exe` を1ウィンドウへ絞って確認した。2560×1440へ最大化した状態で、private `Z:\TextureVoice_local\git\RadDollV3-clothing\private\viewer-data\packs\avatar-raddollv3-local\RadDollV3_VRM.vrm` をExplorerから選択し、候補確認後に「全meshをまとめて取り込む」を実行した。

- 結果: **native Computer Use接続、Explorer選択、候補確認、全mesh取込、モデル表示、チョーカー形状追加、保存をPASS**。
- 取込後status: `10 objects · source 6e5e0a0a82c2 · VRM意味情報: firstPerson（未解決・詳細は警告）`。取込対応欄に `171 bone · humanoid 29 · node affine保存済み`、SpringBone欄に `5 chain · 53 joint · 4 collider group` を確認。
- 形状追加後status: `チョーカー形状を追加しました。頂点編集・厚み・UV・材質を調整してください。`。保存先表示: `C:\Users\tomoaki\AppData\LocalLow\NyaForge\NyaForge\Authoring\Project-1d638f0d`。
- 再起動後は別の保存済み制作（制作対象ID `f2d8c732-21b...`）を表示できたが、保存直後の `Project-1d638f0d` を指定した再読込とは分けて扱う。手動の頂点変更、GLB/VRM出力、Unity／VRChat実機表示は未受入。
- 公開境界: private素材、生成Player、Artifactsは公開ツリーへ追加していない。

# 2026-09-15 CURRENT-GOAL-V1: 現行ソースの実RadDollV3衣装一周を再検証

`Builds/CurrentGoalV1/NyaForge.exe`を現行mainから再ビルドし、private `Z:\TextureVoice_local\git\RadDollV3-clothing\private\viewer-data\packs\avatar-raddollv3-local\RadDollV3_VRM.vrm`を入力として`Tools/Test-NyaForgeRealClothing.ps1`を実行した。取込候補選択、全mesh取込、EditMesh頂点編集、native Save/Open、標準skinned GLB出力／再取込、VRM 1.0 package出力、衣装package生成、Unity Bridge受け取りまでを一周確認した。

- Core: **515 passed / 0 failed**（`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-f4a64781b4de4dd388b5975138a04631`）。
- Windows Player: **PASS**（97 checks、`Artifacts/Authoring-20260915-135812-e0a722b8cfd341eabce1812020911a92/report.json`、画像 `authoring.png`）。
- Unity Bridge: **PASS**（Unity 2022.3.22f1、16 checks、`Artifacts/BridgeReceiver-20260915-140128-404-cde924dab1974541b23d2dbb635d4579/bridge-report.json`）。衣装skeletonは4 bones。
- 主な確認: 実VRMの全mesh編集、choker vertex edit、source診断、Save/Open、標準GLBの形状・材質・skin、VRM1のloss report、衣装packageのstable skeleton/BoneId binding、移動・回転・scale avatar rootへの配置、更新／削除ownership、normal/MR channel。
- 境界: Player／Core／Bridgeの自動・合成受入であり、実EditorWindowの全周fit・貫通・材質見た目、DPI 150/200%・IME・長いパス、VRChat Build & Test／実機表示、完全VRM意味payloadの変換は未受入。private素材・生成Player・Artifactsは公開ツリーへ追加していない。

# 2026-09-15 MANUAL-16: 現行候補のnative再開と出力条件ガード

`Builds/CurrentGoalV1/NyaForge.exe --authoring true`をnative `@oai/sky`で起動し、未保存確認で「変更を破棄して進む」を選択した。空状態から保存済み制作をExplorerで指定し、`Project-3688f569/project.nyaforge.json`を開くと、制作対象の基本形状・制作物、保存済み状態、viewport表示が復元された。右Panelをスクロールして作業モードの「確認・出力」を実画面で開き、標準GLB（skin/morph保持）を選択した。

- 手動確認: **PASS**（Player再起動、Explorerからnative project再開、作業モード「確認・出力」への遷移、出力条件の明示ガード）。
- 出力ガード: 基本形状がskin-bind前の状態では、statusに`Skinned GLB export requires one source, skeleton, skin binding and optional pose.`を表示し、出力を作成しなかった。Panelには標準GLB（表示形状）、標準GLB（skin/morph保持）、拡張GLB（全weight保持）、衣装skin packageの用途説明が表示された。
- 境界: 今回はskin未バインド形状での拒否確認であり、手動の頂点ドラッグ、実skin-bind後のGLB/VRM出力、DPI 150/200%・IME・長いパス、全周fit・貫通・材質見た目、Unity／VRChat実機は未受入。private素材・生成Player・Artifactsは公開ツリーへ追加していない。

# 2026-09-15 MANUAL-17: native Computer Use で Polygon 衣装派生・保存を再開

作業が重なったため、`Builds/CurrentGoalV1/NyaForge.exe --authoring true` の既存ウィンドウをnative `@oai/sky`で再取得し、保存済み `Project-3688f569/project.nyaforge.json` をExplorerから開いた。現在の制作対象（基本形状・制作物）を装着・骨パネルで確認し、保存済みavatar骨格を派生先として `Polygon造形をskin衣装へ派生` を実行した。画面上で制作対象が `衣装 / スキン小物` に切り替わり、statusに元graph保持の派生完了を表示した後、保存を再実行して `C:\Users\tomoaki\AppData\LocalLow\NyaForge\NyaForge\Authoring\Project-3688f569` へ保存済み表示になることを確認した。

- 結果: **native Computer Use接続、保存済みproject再開、Polygon→skin衣装派生、保存をPASS**。
- 境界: 今回は派生と保存まで。衣装の全周fit・貫通、weight paint、実skin-bind後のGLB/VRM出力、DPI 150/200%・IME、Unity／VRChat実機表示は別受入。private素材、生成Player、Artifactsは公開ツリーへ追加していない。

# 2026-09-15 MANUAL-18: 派生衣装のweight初期化とskin package出力

`Project-3688f569`をnative Computer Useで再開し、派生した`衣装 / スキン小物`を装着・骨パネルから確認した。avatar表面weight初期化と表面fitは、設定距離を超える頂点があるためstatusで安全に拒否された。骨segment近傍の自動weight初期化は成功し、保存後に確認・出力へ移動した。

- 手動確認: **PASS**（衣装graph再開、骨近傍weight初期化、保存、出力Panel遷移）。
- 標準skinned GLB: `Skinned GLB export requires one source, skeleton, skin binding and optional pose.` と表示して出力を作成しなかった。衣装graph単体にはavatar sourceがないため、avatar全体出力と衣装package出力を分ける既存ガードを確認した。
- skin package: **PASS**。`C:\Users\tomoaki\AppData\LocalLow\NyaForge\NyaForge\Authoring\Project-3688f569\exports\clothing-20260915-053550-46dcdf` に `clothing.glb`、`binding.nyaforge.bin`、`skeleton.nyaforge.bin`、`skinned-clothing.nyaforge.json` を生成。画面statusでもGLBとBoneId sidecarの同梱を確認した。
- 境界: 表面fitの全周見た目・貫通、weight paintの手修正、VRM実機、Unity／VRChat Build & Test、DPI・IME・長いパスは別受入。生成物はprivate LocalLow出力で、公開ツリーへ追加していない。

# 2026-09-15 MANUAL-19: native装着済みチョーカーの衣装化と再出力

`Builds/CurrentGoalV1/NyaForge.exe`をnative `@oai/sky`で操作し、保存済み`Project-3688f569`の新しいチョーカー形状を制作対象にした。装着先avatarを選択し、RadDollV3 skeletonの`Head`系stable BoneIdを指定して「この小物を装着」を実行。statusに`小物をstable BoneIdへ装着しました。pose変更時にプレビューが追従します。`と表示され、viewportでもリングが首位置へ移動した。

続けて「Polygon造形をskin衣装へ派生」を実行し、`衣装 / スキン小物`へ切り替わること、装着位置をavatar rest座標へ焼き込んだ旨のstatus、元graph保持を確認した。衣装の「自動weight初期化（骨近傍）」を実行して成功し、保存後に確認・出力へ移動した。

- 手動確認: **PASS**（stable BoneId装着、首位置表示、Polygon→skin衣装派生、骨近傍weight初期化、native保存）。
- 出力確認: **PASS**。`C:\Users\tomoaki\AppData\LocalLow\NyaForge\NyaForge\Authoring\Project-3688f569\exports\clothing-20260915-055830-d894fa` に `clothing.glb`（26,400 bytes）、`binding.nyaforge.bin`（41,548 bytes）、`skeleton.nyaforge.bin`（3,153 bytes）、`skinned-clothing.nyaforge.json`（1,064 bytes）を生成。package metadataはvertex 225、triangle 384、stable skeleton/binding hashを保持し、statusでもGLBとBoneId sidecar同梱を確認した。
- 境界: 今回はHead系BoneIdでの首装着とpackage経路の確認。avatar表面fitの全周見た目・貫通、Rig panelでの手修正、VRM実機、Unity／VRChat Build & Test、DPI・IME・長いパス、販売品質は別受入。生成packageはLocalLowのprivate出力で、公開ツリーへ追加していない。

# 2026-09-15 MANUAL-20: native fit計測と安全な保留

保存済み`Project-3688f569`をnative Computer Useで再開し、「装着・骨」モードから`fit状態を測定（変更なし）`を実行した。全三角形・全頂点（衣装225頂点）を対象に、最大投影距離90.772 mm、平均投影距離38.086 mm、最大移動量90.253 mm、裏側候補16頂点と表示された。既定の50 mmを超えるため、まずfit距離を500 mmへ変更して測定を完了できることも確認した。

続けて`衣装をavatar表面へfit`を一度実行し、225/225頂点が移動候補になることを画面で確認したが、全周の見た目・貫通を実画面で判断できないため、直後にUndoした。トップ表示は再び保存済みとなり、既存の衣装package／保存状態を変更していない。

- 手動確認: **PASS**（fit検査の変更なし計測、距離超過の安全な拒否、許容距離を広げた計測、fit適用後のUndoによる保存状態復元）。
- 次の手修正: `Head`系BoneIdで作ったチョーカーの装着offset／avatar面領域を首へ限定し、50 mm以内・裏側候補0を目標に再計測する。自動fitの結果を採用する前に正面・背面・左右とposeで外観を確認する。
- 境界: 数値は最近面候補の検査であり、交差ゼロや全周の販売品質を証明しない。Rig手修正、VRM実機、Unity／VRChat Build & Test、DPI・IME・長いパスは別受入。private素材・生成Player・Artifactsは公開ツリーへ追加していない。

# 2026-09-15 MANUAL-21: Neck再装着と50mm fitガード

保存済み`Project-3688f569`をnative Computer Useで再開し、保存データ上で`Tail_2`になっていたチョーカーのBoneIdを、RadDollV3 skeletonのstable BoneId `Neck · b7411bb3`へ変更した。「この小物を装着」を実行すると首位置へ追従し、statusにstable BoneId装着完了が表示された。fit最大距離を既定の50mmへ戻して`fit状態を測定（変更なし）`を実行したところ、`A clothing vertex is farther from the avatar surface than the configured limit.`で安全に停止した。fit適用は行わず、Neck装着状態だけを同じProjectへ保存し、headerが`保存済み`へ戻ることを確認した。

- 手動確認: **PASS**（stable BoneIdの再選択、首位置への再装着、50mm距離ガード、未適用のまま保存）。
- 未完了: 衣装225頂点を首まわりのavatar面・頂点へ限定する操作が必要。目標は50mm以内、裏側候補0。表面領域のクリック選択が現在の縮尺・重なりで安定しないため、次は首を拡大した状態でavatar面と衣装頂点を分けて選択し、再計測する。
- 境界: 今回は首BoneIdと距離ガードの確認であり、fit適用後の外観・貫通、全周・pose、VRM実機、Unity／VRChat Build & Test、DPI・IME・長いパス、販売品質は別受入。private素材・生成Player・Artifactsは公開ツリーへ追加していない。
