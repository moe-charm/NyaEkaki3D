# NyaForge 開発タスク

更新: 2026-09-12。共有Paintの対象slot列挙と複数仮画像の一括更新を実装。Core181件・Player回帰合格。3D stroke GUIへの接続は次。

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

保存はstatic writer schema2、graph writer schema3、reader 1/2/3。Paintは現在mesh全体に1合成画像。材質未割当は不透明preview、標準材質は3alpha modeを選択できる。UV変更時は旧payloadを未解決として保持し、明示rebindで対応を更新する。見た目を保つ再投影ではない。旧mesh-only Bakeは画像を拒否。画像付きmeshは別のSurface Bake profileで出力する。

## 今回の変更と証拠

- `OutputSurfaceConnections.ImageSlots` を追加。Paintノードidentityから、最終出力の使用slotだけを列挙する。同じ画素hashの別Paintを共有扱いしない。
- `MaterialSurfaceSet.ShowPreviews` / projectionの複数slot仮表示APIを追加。全slotを検査・全textureを準備してから切替。準備例外は候補だけを破棄し、一部slotだけが変わることを避ける。
- Player検証へ、3/9の同時仮表示、不正slot 99を混ぜた要求で両texture保持、commit/rollbackで両仮texture復帰、全解除を追加。
- Core **181 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-8b0cc807bb4a497f8c077fcef6550e31`。共有Paintの両slot列挙と同画素の別Paint分離を確認。
- Unity6000.4.3f1 build成功: `Builds/Windows-C1C-SharedSlotPreview2/NyaForge.exe` / `Logs/build-player-20260912-022318-936.log`。
- Windows Player PASS: `Artifacts/Authoring-20260912-022355-f798c40ab91c45c68e8649418ea26f93/report.json`。上の複数slot仮表示と旧GUI/部位GUI/GPU回帰。今回DensePaintなし。texture allocation例外を実際に注入した検証は未実施で、invalid-slot事前拒否とtransaction rollbackを検査した範囲。
- 3D stroke GUIはまだ複数slotへ未接続。既存SurfacePreparationはOutput.BaseColorを前提とするため、選択Paint画像の経路へ変更が必要。ray hitのSubmeshIndexで対象外部位を拒否し、手前の対象外面を透過しないことも接続時に検査する。
- [部位別材質契約](docs/Material-Slots.md)へ共有Paint基盤と次の接続条件を記録。[前段](docs/history/2026-09-12-C1C-Before-Shared-Slot-Paint.md)を保存。

## 次の作業

1. 複数材質のPaint対象をGUIへ接続する。選択Paintの画像/UVでSurfacePreparationを作り、stroke開始時にImageSlotsを固定して複数仮表示へ渡す。ray hitのSubmeshIndexで対象部位を検査する。layer/mask・共有画像・別画像・対象外遮蔽の3D stroke/Undo/nativeを確認。次に専用Bake/Bridgeへ進む。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-SharedSlotPreview2 -Width 1280 -Height 800 -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `Assets/NyaForge/Rendering/`: Playerとreceiverが共有する材質adapter/shader。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
