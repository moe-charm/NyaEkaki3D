# NyaForge 開発タスク

更新: 2026-09-11。小物形状のGUI制作→保存→画像付きUnity出力→別UnityのPrefab描画まで自動検証・画像確認。全体は開発中。

## 開発の入口

作業先: `Z:/TextureVoice_local/git/NyaForge`。Windows先行、macOSは将来。
読む順: このファイル → [開発計画](docs/Development-Plan.md) → [設計v2](docs/NyaForge-Authoring-Design2.md)の対象節 → コード。[文書一覧](docs/README.md)参照。
製品目標は小物の制作・出力を一周し、低ポリ全身キャラ、品質向上へ進むこと。設計v2は製品方針、v1は背景資料。設計中の外部依存・機能は採用済みや実装済みを意味しない。

## 現在地

| 範囲 | 現物と確認状況 |
|---|---|
| C0-R／C1-A | 空project、最大1object、typed graph、共通commandとUndo、native保存、node canvas、static Bake／Bridgeの既存検証記録あり |
| C1-B | polygon/corner ID、面選択・押出し・厚み、Mirror、編集ケージと最終結果、UV投影と島の数値編集まで実装 |
| C1-C Paint基盤 | immutable画像、Image port、Paint node、UV binding、1stroke Undo、画像blob付きnative保存、3D baseColorの既存Player合格レポートあり |
| Paint GUI | 色・半径・不透明度と2D brush、確定時の3D更新を実装。pointerドラッグ、1command／Undo、キャンセル・取得喪失・Escape、native再読込のPlayer検証合格。OS手動受入は未確認 |
| 今後 | layer/mask、UV再投影、一般Material graph、出力identityの更新、MCP、rig/weight/morphと全身制作 |

保存はstatic writer schema2、graph writer schema3、reader 1/2/3。設計versionと保存schemaは別物。Paintは現在mesh全体に1画像、不透明preview。UV変更時は旧payloadを未解決として保持する。旧mesh-only Bakeは画像を拒否。画像付きmeshは別のSurface Bake profileで出力する。

## 今回の変更と証拠

- `UnityRuntime/AuthoringWorkbench.ItemVerification.cs`: 空projectから四角面を作り、安定頂点IDから表示indexを引いて移動対象を選択。移動・厚み・UV投影・Paint追加・ブラシ・保存／開く・Surface出力をGUIイベントで通す。OS入力の手動受入ではない。
- 検証例は100×140×4mm、12三角形のひし形プレート。黄色地にピンク帯。形状寸法、保存後のstate hash、出力meshと全画素の一致を確認。
- `UnityBridge/Editor/SurfaceRenderVerification.cs`: 保存したPrefabを別UnityでrenderしPNGへ出す。camera/light/instance/render targetの所有と破棄、変更したambient設定の復元をまとめた独立モジュール。
- `Tools/Test-NyaForgeUnityBridge.ps1`: `-ItemFixture`でGUIが出力した小物を検査。`-RenderSurface`でgraphicsを有効にして描画検証。通常のheadless検証を維持。
- 最新ビルド `Builds/Windows-C1C-ItemWorkflow/NyaForge.exe`、成功ログ `Logs/build-player-20260911-212803-748.log`。
- 最新Player pass `Artifacts/Authoring-20260911-212911-6b60bc1eed714e7e8326d2b4afc58584/report.json`。`item-project`にnative作品とGUI出力が残る。`item-workflow.png`を目視確認。
- 別Unity **2022.3.22f1 pass**: `Artifacts/BridgeReceiver-20260911-213020-793-b23f9370842a4bf7a6c6df9457993d5c/bridge-report.json`。既存scale1/100と、今回GUIが出力した小物のmesh/UV/PNG/material/Prefabを検査。
- 同receiverの `surface.png` を目視確認。ひし形とピンク帯を確認。Standard材質と照明による明るさはViewerの確認shaderと異なる。
- Core **116 passed / 0 failed** は前回。今回Core変更なし、再実行なし。形式/API/試験の詳細は[Surface Bake履歴](docs/history/2026-09-11-C1C-Surface-Bake.md)と[形式仕様](docs/Surface-Bake-Format.md)。
- 最初のfixture失敗は表示indexと制作頂点IDの混同。既存RenderVertexMapを使うよう検証を修正し、期待寸法を変更せず合格した。
- この例は機能往復用の公開自作fixture。販売品質のアクセサリーや装着済みモデルではなく、ユーザーの操作受入／VRChat確認は未実施。

## 次の作業: 小物制作の一周を確認し、C1残件を進める

1. 小物形状の初期GUI一周は確認済み。次はPaintのlayer/opacity/maskを設計v2 §20.2へ合わせて実装し、文書・画像blob・Undoと出力合成結果を接続する。単一画像のまま完成としない。
2. Surface profileの材質/PNG改変・未知profile・画像blob破損・失敗時の受け取り側asset非作成を拡充。URPは実装経路ありだが専用受け取り試験は未実施。
3. 複数Paint段・複数材質、layer/opacity/mask、画像取込、3D paintを設計v2 §20.2に沿って拡張する。現在の1画像固定を完成要件にしない。
4. geometryの模様を保つUV再投影/rebake、再export identityの更新・利用者設定の保護を実装。明示UV再割当とは別機能。
5. C1のEvidence/MCP共通commandと残る造形操作を進め、C2の低ポリ全身・rig/weight/morph制作へつなぐ。全体目標は未完了。

再検証:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-ItemWorkflow -Width 1280 -Height 800
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeUnityBridge.ps1 -PlayerCheckDirectory <今回のPlayer検証出力> -ItemFixture -RenderSurface
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚みの品質と自己交差、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。初期予算のUV表示2048面・画像1024角を最終製品要件と読み替えない。

詳細な過去記録は[文書一覧](docs/README.md)から参照。history内の「次」「未実装」は当時の状態であり、再開指示はこのファイルを優先する。







