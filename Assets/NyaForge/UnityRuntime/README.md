# UnityRuntimeのpartial構成

`AuthoringWorkbench`はUnityの1つの`MonoBehaviour`として動作しますが、ソースは責務ごとのpartialへ分けています。partial間で共有するのは`AuthoringWorkbench.State.cs`の一時UI状態と、`AuthoringWorkbenchSession`／`SelectionContext`です。保存される制作データはWorkbenchのフィールドへ複製せず、`AuthoringWorkspace.Document`と各serviceを正本にします。

## 変更場所の目安

- UIの配置と`Build*`の呼出し順: `AuthoringWorkbench.Layout.cs`
- 共通ボタンのcommand実行、Undo/Redo、エラー表示: `AuthoringWorkbench.Execution.cs`
- 起動・閉じる・preview sceneの寿命: `AuthoringWorkbench.Lifecycle.cs`
- 保存状態だけの表示更新: `AuthoringWorkbench.PersistenceRefresh.cs`
- 全体refreshとPanel更新順: `AuthoringWorkbench.RefreshState.cs` / `RefreshPipeline.cs`
- 空状態と制作状態のPanel表示境界: `AuthoringWorkbench.ContextVisibility.cs`
- viewportのRenderTexture・Frame: `AuthoringWorkbench.Viewport.cs`
- viewportイベントと頂点hit test: `AuthoringWorkbench.ViewportInteraction.cs` / `ViewportInput.cs`
- 小物装着の一時UI状態: `AuthoringWorkbench.AttachmentState.cs`
- 小物装着Panelの構築: `AuthoringWorkbench.AttachmentUi.cs`
- 小物装着の表示・fit・weight操作: `AuthoringWorkbench.Attachments.cs`
- fit検査の一時値オブジェクト: `AttachmentSurfaceFitMeasurement.cs`
- GLB／VRM出力のsource-skin変換: `AuthoringWorkbench.ExportTransforms.cs`

`ContextVisibility`は通常のWindows UIでは空状態の導線を短くするためにPanelを隠します。既存の自動Authoring／Navigation検証は空状態でも低レベルボタンを直接probeするため、`--authoring-check-output`または`--navigation-check-output`起動時だけ検証用にPanelを表示します。これは実ユーザー向け表示を戻す設定ではなく、旧検証入口との互換境界です。

新しい操作は、既存のCore／serviceを呼び、状態を変更する場合は共通`Execute`を通します。新しいPanelを追加するときは、配置・一時状態・操作・refresh・検証を別のpartialへ分け、`AuthoringWorkbench.cs`へ実装を戻さないでください。自動fixtureの検証は`*Verification.cs`に置き、実マウス、Unity Editor、VRChatの受入結果とは別に記録します。
