# NyaForge 開発タスク

更新: 2026-09-12。Prefab管理参照の選択的照合を追加。利用者の子object/Light/Transform/Renderer設定を保持する更新と例外rollbackを確認。次は永続journal/中断復旧。

## 開発の入口

作業先: `Z:/TextureVoice_local/git/NyaForge`。Windows先行、macOSは将来。
読む順: このファイル → [開発計画](docs/Development-Plan.md) → [設計v2](docs/NyaForge-Authoring-Design2.md)の対象節 → コード。[文書一覧](docs/README.md)参照。
製品目標は小物の制作・出力を一周し、低ポリ全身キャラ、品質向上へ進むこと。設計v2は製品方針、v1は背景資料。設計中の外部依存・機能は採用済みや実装済みを意味しない。

## 現在地

| 範囲 | 現物と確認状況 |
|---|---|
| C0-R／C1-A | 空project、最大1object、typed graph、共通commandとUndo、native保存、node canvas、static Bake／Bridge |
| C1-B | polygon/corner ID、面選択・押出し・厚み、Mirror、編集ケージと最終結果、UV投影と島の数値編集 |
| C1-C Paint | 2D brush、Image port、UV binding、1stroke Undo、native画像保存、3D baseColor、PNG／Surface Bake |
| Layer/mask GUI | 移行・追加・選択・並替・削除・表示・不透明度・名前、mask追加/削除と描画、取消、Undo、保存を検証 |
| Image import | PNG検査・展開・縦横比保持サイズ調整・新layer追加・Undo・保存を検証。Windows pickerの実操作は手動未確認 |
| 3D paint | BVH ray、論理edge/UV連続判定、screen補間、切れた区間の描画、GUI色/mask・仮表示・取消・Undo・保存/出力を実装 |
| 今後 | 3D複数面/細かなseamの精度と操作、一般Material graph、UV再投影、出力identity更新、Evidence/MCP、rig/weight/morphと全身制作 |

保存はstatic writer schema2、graph writer schema3、reader 1/2/3。Paintは共有画像と部位別の独立画像に対応。材質未割当は不透明preview、標準材質は3alpha modeを選択できる。UV変更時は旧payloadを未解決として保持し、明示rebindで対応を更新する。見た目を保つ再投影ではない。旧mesh-only Bakeは画像を拒否。画像付きmeshは別のSurface Bake profile、単一標準材質はMaterial Bakeを利用。複数材質BakeはGUI/Bridgeまで接続済み。

## 今回の変更と証拠

- `PrefabManagedBindings` にroot GameObject・MeshFilter・MeshRendererのlocal file IDと参照mesh/material GUID+local IDのfingerprintを分離。所有receipt v3が保持。v1/v2は従来の全file比較を維持して推測で緩和しない。
- v3のPrefabは管理参照とmetaだけを競合判定。保存済みの利用者component/子object/Transform/Renderer設定は更新を妨げない。未保存component/Prefab Stageのdirtyは拒否するコードを追加（Stage操作の実検証は残る）。Mesh/材質/PNGは従来の厳密比較を維持。
- 更新は既存LoadPrefabContentsで管理参照のみ変更。receipt再保存後も利用者の追加ファイルをowned集合へ含めない。
- Bridge PASS: `Artifacts/BridgeReceiver-20260912-030604-416-a05062c1a6fa4e4283ce28a764e30f6f/bridge-report.json`、Unity2022.3.22f1 Built-In/Linear。ユーザー設定後のPrefabで位置/回転/scale、Renderer.enabled、子object/Light.range/intensity、保存済みroot名を保持。管理mesh参照nullを拒否、旧receiptでの保守的拒否を確認。追加txtを保持し所有へ取り込まない。既存4段階rollbackもユーザー変更後のbytes/metaに戻ることを確認。
- 最初の検証はroot名の期待値で失敗。Unityが保存した名前（StaticMesh）と入力直後の名前を混同していた。保存済みassetから基準名を取得する検証へ修正。Transform等を外す変更はしていない。
- Core/Player変更なし。入力Player: `Artifacts/Authoring-20260912-025612-a1dd9f3040614b619a455d00274fa3ac/report.json`。直近Core186件。前段詳細は [履歴](docs/history/2026-09-12-C1C-Before-Selective-Prefab.md)。
- 未確認: scene override/Prefab variant/nested prefab、Prefab Stageの実操作、新規材質/画像共有分岐、幾何更新。例外rollbackはメモリのみで、強制終了/復元中エラー対応はまだない。
## 次の作業

1. 永続journalによる更新中断/rollback失敗の復旧を実装する。現AssetUpdateTransactionはメモリbackupのみ。準備済みbackupのflush→mutation→commit印、recoveryの対象検査と再試行性、GUI/起動時の復旧案内を設計/検証する。その後に資源追加/共有分岐/削除候補preview、幾何更新とscene overrideを進める。複数材質の同UUID共有receiver fixture、部位別GPU定量/alpha、mask/重なり遮蔽は検証残件として保持。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-BakeOutputIdentity -Width 1280 -Height 800 -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `Assets/NyaForge/Rendering/`: Playerとreceiverが共有する材質adapter/shader。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
