# NyaForge 開発タスク

更新: 2026-09-12。面slot変更・材質接続・画像bindingを一括更新するgraph編集を実装。Core180件合格。GUIへの接続と多層/複数画像検証が次。製品全体の開発は継続中。

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

- `MaterialFaceEditing.Assign` をCore graphの独立モジュールに追加。面slot変更、slotの材質接続、保存済みPaintの対応更新をimmutable候補graphにまとめ、ReplaceGraphの1commandで適用できる。
- Paint入力の前後domain/transform、頂点IDと座標、face/corner IDと順序、UV/normal/tangentを比較し、slotだけの変更と証明できた場合のみ既存PaintRebindingで対応を移す。元々未解決のgraphを自動修復しない。画像正本と旧paint-uv-v1契約は保持する。
- 最終graphの完全評価を確認してから候補を返す。下流編集の失効を勝手にrebaseせず、失敗時は元graph/画像を保持。未使用slot接続も保持する。
- Core **180 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0413182003744a36b63466a0840f5073`。slot 7追加、画像参照/画素保持、binding変更、材質UUID、1Undo/Redo/native往復、元々不一致のPaint拒否を確認。
- Unity6000.4.3f1 build: `Builds/Windows-C1C-FaceMaterialBinding/NyaForge.exe` / `Logs/build-player-20260912-021708-087.log`。
- GUI変更なし。直近Player PASSは `Artifacts/Authoring-20260912-021011-f7b4f3fa10004d4788f2420940a5d17b/report.json`。今回Player/receiver再実行なし。layer/mask・複数Paint・下流Mirror等の新経路検査はまだ追加が必要。
- [部位別材質契約](docs/Material-Slots.md)へ一括更新と制約を追記。[前段](docs/history/2026-09-12-C1C-Before-Face-Binding.md)を保存。

## 次の作業

1. [部位別材質契約](docs/Material-Slots.md)のMaterialFaceEditingをGUIの選択面→部位割当へ接続する。新部位作成と既存slotへの移動/材質接続変更を区別。layer/mask・複数Paintの同一画像保持、下流変更の拒否、複数部位のクリックとUndoを検証する。Paint対象/共有画像の同時仮表示、専用Bake/Bridgeへ進める。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-MaterialSlotGUI3 -Width 1280 -Height 800 -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `Assets/NyaForge/Rendering/`: Playerとreceiverが共有する材質adapter/shader。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
