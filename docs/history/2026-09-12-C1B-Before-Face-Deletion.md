# NyaForge 開発タスク

更新: 2026-09-12。共有画像の分岐/再共有、Core出力による形状・submesh数1→3→2のUnity更新を検証。次は造形・UV編集の残件へ進む。

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

- `SharedImageUpdateVerification` を独立追加。2材質が1画像を共有する初期importから、異なる画像への分岐（PNG1個だけ追加）、再共有（未使用PNG保持）を検証。各段階のPNG bytes、Prefabの参照、材質/GUID、所有receiptを照合。manifest variantを使ったreceiver検証であり、GUI操作検証ではない。
- `GeometryUpdateVerification` を独立追加。実際のCore graph commandとMultiMaterialBakeStore.Exportで、頂点位置・頂点/三角形数・submesh数を1→3→2へ変更。更新後の位置/indices/UV/生成normal/Prefab参照、mesh/prefab GUID、使用終了した材質の保持を確認。画像なしfixtureであり、形状変更時のPaint再投影を検証したものではない。
- 最新receiver PASS: `Artifacts/BridgeReceiver-20260912-033631-177-a3094b2c12fb40238ae1a8d14c59ca15/bridge-report.json`。Unity2022.3.22f1、Surface/Material/MultiMaterialの既存suiteも成功。今回は検証module追加のみでCore/Player本体変更なし。履歴: [変更前](docs/history/2026-09-12-C1C-Before-Update-Variants.md)。

- `MaterialUpdatePlan` を追加。使用submeshごとの材質/画像pathと、追加/更新/保持の資源一覧をimmutableに計画。既存GUIDを使う画像、共有画像、新規画像の行先を決定。未管理fileと衝突する材質pathは拒否。
- `BakeUpdatePreview` が計画を生成し、`MultiMaterialUpdate` がその同じ割当を使用。commit直前とGUI反映時にComparisonKeyも再照合。GUIに140pxスクロール領域で資源一覧を表示（Editor実操作は未検証）。
- receiver fixtureで一方の材質UUIDを置換し、新規材質/PNGを追加、旧材質をGUID付きで保持。previewと実反映のmaterial/texture path一致を確認。追加後に故意に失敗させて新規資源の削除と全旧bytes/meta復元を確認。
- `CrashDuringRecovery` を追加。復元処理の途中で実processをKillし、さらに別processから復旧を完了する。未復元fileが残る途中checkpointであることも確認。
- 最終suite PASS: `Artifacts/BridgeReceiver-20260912-032808-663-746b1450f7ec4f03884991ef96823dac/`。Unity2022.3.22f1。bridge-report.jsonに資源計画/追加/rollbackと既存検証の成功。materials/prefab/receipt-recovery.jsonが全てpassed。
- materials試験は更新process PID11500→復旧中断PID81356→最終復旧PID41020。`materials-crash.json.recovery-stop.json` は中断地点の証拠で成功reportではない。最終materials-recovery.jsonで全file/metaとAssetsフォルダ集合、所有照合の復元を確認。
- Core/Player変更なし（直近Core186件）。入力Player: `Artifacts/Authoring-20260912-025612-a1dd9f3040614b619a455d00274fa3ac/report.json`。前段: [履歴](docs/history/2026-09-12-C1C-Before-Resource-Plan.md)。
- 未確認: scene override、GUI実操作、GUID記録前中断、staging journal登録前/commit直後Kill、disk障害。画像と形状を同時変更する再投影など、全variantの完了ではない。
## 次の作業

1. C1の造形/UV機能へ進む。既存のpolygon編集context・GUIを調べ、面削除など基本編集を共通command/Undo/native保存まで一周させる。scene override、GUI実操作、staging登録前中断/未確認新規GUIDなどは保持。復旧だけの類似テストを増やし続けない。複数材質の同UUID共有receiver fixture、部位別GPU定量/alpha、mask/重なり遮蔽は検証残件として保持。
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
