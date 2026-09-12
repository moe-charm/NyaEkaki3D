# NyaForge 開発タスク

更新: 2026-09-12。Bake出力ID sidecar、receipt v2、更新先照合を追加。Core186件・Player・Bridge成功。更新適用は未実装。

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

- 全4Bake exporterが `BakeOutputIdentity.Publish` でmanifestと `.identity.json` を出力。graph output UUID/static object UUID、文書/object UUID、revision、manifestHashを記録。既存Bake schemaは保持。二ファイル同時atomicではなく、欠落/不整合を更新候補で拒否する。
- 初回import全経路でsidecarを検証してから資源を生成。所有receipt v2へ出力IDを記録。旧receipt v1/sidecarなしは未識別として読めるが自動照合しない。
- `BakeUpdatePreview.Materials` とGUI照合ボタンを追加。別出力、古いrevision、legacy、利用者変更をconflictとして返す。読取専用。変更資源の一覧/commit/rollbackはまだない。
- Core **186 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f23d501e3d1040c9936b1f0284383f4c`。編集/native往復で出力ID維持、全profile、hash/revision/kind不整合、legacy読取を検証。
- build `Builds/Windows-C1C-BakeOutputIdentity/NyaForge.exe` / `Logs/build-player-20260912-025541-248.log`。
- Player PASS `Artifacts/Authoring-20260912-025612-a1dd9f3040614b619a455d00274fa3ac/report.json`。既存造形/Paint/材質/GUI出力回帰。今回DensePaintなし。
- Bridge PASS `Artifacts/BridgeReceiver-20260912-025656-092-5dd2444c4ebc447caeecb377e56635c7/bridge-report.json`、Unity2022.3.22f1 Built-In/Linear。新sidecarを持つGUI出力から初回importし、一致/別ID/古いrevision/旧receipt拒否と所有検査、旧profilesを確認。新しいEditor GUIボタン実操作は未検証。
- [更新契約](docs/Unity-Import-Updates.md)を更新。[前段の証拠](docs/history/2026-09-12-C1C-Before-Output-Identity.md)。
## 次の作業

1. `docs/Unity-Import-Updates.md` に沿い、変更/追加/削除の資源計画と既存GUIDを保持する更新transactionを実装する。出力IDと所有検査/target照合は追加済み。入力読取中/commit直前の競合、profile変更、Prefab未管理component維持を扱う。複数材質の同UUID共有receiver fixture、部位別GPU定量/alpha、mask/重なり遮蔽は検証残件として保持。
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
