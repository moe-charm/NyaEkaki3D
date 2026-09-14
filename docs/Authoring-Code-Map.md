# Nya Ekaki 3D Authoringコードマップ

この文書は、Windows v1の制作画面を変更するときに「どこへ書くか」を迷わないための入口です。`AuthoringWorkbench`は1つのMonoBehaviourですが、機能ごとのpartialへ表示・検証責務を分けています。保存形式、GLB/VRM解析、Coreの形状計算はUI partialへ複製しません。

## 変更の入口

| 変更したいもの | 主なファイル | ここで持つ責務 |
|---|---|---|
| 上部の常設操作 | `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.CommandBar.cs` | モデル追加、基本形状追加、保存、Undo/Redo、現在対象と未保存表示 |
| 作業モードの移動 | `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.WorkModes.cs` | 形状／UV・色／装着・骨／確認・出力のFoldoutを開き、他の編集領域を折りたたんで該当位置へスクロール |
| Morph／表情差分の識別 | `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Morph.cs` | target名と短縮IDを一覧表示し、選択中targetの完全IDをtooltipへ表示。Morph weight／VRM表情の編集要求は既存commandへ渡す |
| 作業モードの回帰 | `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.WorkModeVerification.cs` | ポインタhit testで4モードの遷移を検査。編集処理は持たない |
| 基本形状の追加 | `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ShapeCreation.cs`, `AuthoringWorkbench.ShapePresetCatalog.cs` | 寸法入力と追加要求。表示名・既定寸法・説明・生成callbackはpreset catalogで管理し、リング／バンドの生成は`PolygonPrimitives`へ委譲 |
| 対象一覧と役割名 | `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Objects.cs`, `AuthoringWorkbench.ObjectLabels.cs` | 選択、表示／参照保護、短縮表示、tooltipの完全IDと役割説明。stable ObjectId keyed表示名のGUI入力・保存・Undo/Redoもここで扱う |
| 小物装着の一時状態 | `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.AttachmentState.cs` | 装着PanelのUIハンドル、候補ID、fit検査結果などの一時状態。保存される装着設定はgraph側を正本とし、ここへ永続データを追加しない |
| 小物装着Panelの構築 | `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.AttachmentUi.cs` | 装着Panelの入力欄、ボタン、tooltip、help文とイベント接続。装着計算や保存処理は持たない |
| fit／weight対象サマリー | `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Attachments.cs`, `AuthoringWorkbench.AttachmentState.cs`, `AuthoringWorkbench.SelectionRefresh.cs` | 現在の衣装頂点・avatar面の対象範囲、全体数、skin-bind状態、fit測定の有効性を表示。測定結果やgraphの保存正本は持たず、入力変更時に再測定要否を再計算する |
| fit検査の受け渡し型 | `Assets/NyaForge/UnityRuntime/AttachmentSurfaceFitMeasurement.cs` | fit／clearanceの計測結果をUIとMCPへ渡す一時DTO。WorkbenchやDocumentへの参照を持たない |
| GLB/VRMの取込案内 | `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Import.cs`, `AuthoringWorkbench.ModelImportGuidance.cs` | 利用者向けの手順説明と既存取込commandの接続。解析処理はImport serviceへ委譲 |
| 保存と受け渡し | `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ProjectOutput.cs` | native保存／開く、Explorer選択、GLB・Unity・衣装package・VRMの操作配置 |
| 出力用source-skin変換 | `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ExportTransforms.cs` | inverse-bind／joint local transformをstable BoneId順へ揃え、GLB／VRM出力へ渡す。保存・出力service自体はUI partialへ複製しない |
| AI接続 | `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.McpPanel.cs`, `AuthoringWorkbench.Mcp.cs`, `AuthoringWorkbench.McpCommands.cs`, `AuthoringWorkbench.McpObjectLabels.cs` | 接続欄・開始／停止の表示とpipe更新。MCP commandの実装は`McpCommands`等へ分離し、表示名更新は専用metadata serviceへ委譲 |
| 共有文書とcommand service | `Assets/NyaForge/UnityRuntime/AuthoringWorkbenchSession.cs`, `AuthoringWorkbench.SessionVerification.cs` | live workspace、command service、読み込んだ保存先、保存未完了状態を同じ所有者から管理し、`StateChanged`をコマンドバーへ通知。Player回帰で同一参照を検査し、パネルごとの別履歴を禁止 |
| 表示名metadata | `Assets/NyaForge/Authoring/Persistence/ObjectLabelsCodec.cs`, `ObjectLabelService.cs`, `ProjectAttachments.cs`, `ProjectStore.cs`, `ProjectSnapshotCodec.cs` | stable ObjectId→表示名のbounded attachmentをnative Save/Open・legacy sidecar検出へ接続。GUI／MCPの更新はrevision・attachments hashを検査し、UI状態やObjectId自体を変更しない |
| inspectionの表示名 | `Assets/NyaForge/Authoring/Inspection/AuthoringStateReader.cs`, `AuthoringGraphReader.cs` | GUIと同じ表示名attachmentを`displayName`として返し、未設定時はnull／役割名側のfallbackを許容 |
| 選択context | `Assets/NyaForge/UnityRuntime/SelectionContext.cs`, `AuthoringWorkbench.SessionVerification.cs` | 頂点／面選択、active object ID、edit node IDを共有し、`Changed`を投影選択へ通知。object／stage切替の同期をPlayer回帰で検査し、surface領域など別ドメインの選択は各機能の責務に残す |
| 造形・UV・材質・Rig | `AuthoringWorkbench.GraphEditing.cs`, `AuthoringWorkbench.Uv*.cs`, `AuthoringWorkbench.Material*.cs`, `AuthoringWorkbench.Rig*.cs` | 各編集段の表示と既存commandへの入力 |
| 全体の配置と更新順 | `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.cs`, `AuthoringWorkbench.State.cs`, `AuthoringWorkbench.Execution.cs`, `AuthoringWorkbench.Layout.cs`, `AuthoringWorkbench.Lifecycle.cs`, `AuthoringWorkbench.PersistenceRefresh.cs`, `AuthoringWorkbench.ContextVisibility.cs`, `AuthoringWorkbench.RefreshState.cs`, `AuthoringWorkbench.RefreshPipeline.cs`, `AuthoringWorkbench.UiElements.cs`, `AuthoringWorkbench.Viewport.cs`, `AuthoringWorkbench.ViewportInteraction.cs` | partial-classのホストはWorkbench本体へ、共有UIハンドル・session・selection context・viewport状態はStateへ、共通command実行・例外捕捉・ステータス表示はExecutionへ、起動／開閉／終了確認とpreview sceneの寿命はLifecycleへ、保存状態だけの表示更新はPersistenceRefreshへ、空projectと制作状態に応じたPanel可視性はContextVisibilityへ、全体refreshの投影・状態反映順はRefreshStateへ、ルート／ヘッダー／viewport／controlsの配置と`Build*`順序はLayoutへ、編集系／確認・出力系のPanel更新群はRefreshPipelineへ、行・ボタン・数値入力はUiElementsへ、Frame／カメラRenderTextureはViewportへ、UI Toolkitのviewportイベント配線はViewportInteractionへ集約 |
| viewport入力 | `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ViewportInput.cs` | 選択頂点の移動command、対象選択、world-space頂点hit test、viewport座標変換。カメラ／RenderTextureの寿命やmesh計算は持たない |
| 選択変更の部分refresh | `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.SelectionRefresh.cs` | 頂点選択に依存する面編集・Rig・装着表示だけを更新。材質・出力・シミュレーションの完全refreshや選択の正本は持たない |
| 対象一覧の差分更新 | `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Objects.cs` | object ID・表示名・active objectをキーに一覧ボタンの再生成を抑制。頂点編集では既存UIを再利用し、選択の正本・保存形式・tooltipは変更しない |

## 依存方向

```text
UI partial
  → 明示的なcallback / 既存のExecute
  → AuthoringCommandService
  → Authoring Core / 保存・出力service
```

新しいパネルは、workspaceやDocumentのコピーを保持せず、必要な状態を`Refresh`から読み取ります。編集を行う場合は必ず共通`Execute`を通し、GUIだけのUndo履歴を作りません。ファイル入出力・GLB/VRM仕様・メッシュ計算をUI partialへ追加しないでください。

## 新機能を追加する手順

1. 既存command／serviceで再利用できる処理と、画面に固有の表示状態を分ける。
2. 表示状態とイベント接続を責務名のpartialへ追加する。`AuthoringWorkbench.cs`へ直接パネルの細部を増やさない。
3. `BuildUi`からは`Build<Feature>`を1回呼び、`Refresh`からは`Refresh<Feature>`を1回呼ぶ。
4. 自動確認が必要なら、製品操作と同じUIイベントを使う`<Feature>Verification.cs`を別ファイルに置く。
5. Core／Player／実マウスのどこまで確認したかを`current_task.md`へ記録する。Player回帰成功をUnity／VRChatや実アバターの受入へ読み替えない。

## 次段の分離候補

`WorkbenchSession`と`SelectionContext`の第一段、および基本的な変更通知は導入済みです。`State`で共有UIハンドル・session・selection context・viewport状態、`Lifecycle`でUnityイベント購読・preview sceneの寿命・終了確認、`PersistenceRefresh`で保存状態だけの表示更新、`Layout`でルート・ヘッダー・viewport・controls配置とfeature `Build*`順序、`RefreshState`で全体refreshの投影・状態反映順、`Execution`で共通command実行・例外捕捉・ステータス表示を分け、`RefreshPipeline`で編集系と確認・出力系の更新群を分け、`UiElements`で共通UI生成、`Viewport`でFrame／カメラRenderTexture、`ViewportInput`で対象選択・頂点hit test・座標変換、`ViewportInteraction`でUI Toolkitイベント配線、`SelectionRefresh`で選択依存Panelの部分更新、`Objects`で対象一覧の差分更新を分けました。GUI-03では表示名を`ObjectLabelsCodec`／`ObjectLabelService`のnative attachmentへ分離し、Objects／inspection／MCPが同じ正本を読むようにしています。既存の更新順を保ったまま、UI本体の責務を小さくしています。次に大きな変更をするときは、dirty／metadata再同期とPanel単位の購読・更新順をそれぞれへ移します。導入時も既存Core、保存形式、MCP wire契約を変えず、1責務ずつ移して回帰を通します。
