# NyaForge 開発タスク

更新: 2026-09-12。standard材質と割当ノードのCore評価・保存・Undoを実装。Core167件とUnityコンパイル成功。材質の表示/編集GUI/出力は未接続。既存GUI回帰も合格。製品全体の開発は継続中。

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

保存はstatic writer schema2、graph writer schema3、reader 1/2/3。Paintは現在mesh全体に1合成画像、不透明preview。UV変更時は旧payloadを未解決として保持し、明示rebindで対応を更新する。見た目を保つ再投影ではない。旧mesh-only Bakeは画像を拒否。画像付きmeshは別のSurface Bake profileで出力する。

## 今回の変更と証拠

- `MaterialParameters` にlinear tint、metallic、roughness、linear emission、Opaque/Cutout/Blend、cutoffをimmutable値として集約。範囲/有限値を検査しcanonical identityを保持する。
- `material.standard`（Image→Material）と `mesh.assign-material`（Mesh+Material→Mesh）を追加。旧Output.baseColorは変更しない。画像bindingのUV/domain検査を共用し、材質がmesh/画像正本を複製しない。
- 材質のsnapshotを評価結果へ含め、parameters変更でmesh/画像hashを保持。共通UpdateNode/Undo/Redo、graph wire/native保存・再読込を通した。wire version1・native graph schema3の旧payloadは変更しない。
- 暫定状態: Unity表示/GUI/材質Bakeは未実装。Unity projectionと現行Bakeは材質を含む出力を明示拒否する。値だけ保存して無視する描画/出力にはしない。後段geometryと二重割当も明示診断。次はこの暫定制限を表示/出力実装へ置き換える。
- Core **167 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-fea437e6823f4e04bf3f2f4383df265a`。値検査、型接続、別domain画像、競合/後段geometry、identity、Undo、native/graph byte roundtrip、現行Bake拒否を検証。
- Unityコンパイル成功: `Builds/Windows-C1C-MaterialCore/NyaForge.exe` / `Logs/build-player-20260912-005642-887.log`。既存GUI回帰PASS: `Artifacts/Authoring-20260912-005804-e8a2225293db4ac7994fd1018b896f2b/report.json`。PNG30形式と準備競合も再確認。今回はDensePaintなし。材質GUIの動作検証とは扱わない。
- [材質graph契約と実装順](docs/Material-Graph.md)、[前段の記録](docs/history/2026-09-12-C1C-Before-Material-Graph.md)参照。PNG30形式は前段のPlayerで確認済み。history内の「次」は当時の状態。

## 次の作業

1. 材質のUnity資源所有とprepare/commit/rollbackを実装し、Coreで保持したtint/metallic/roughness/emission/alphaを実描画へ適用する。Paintの仮画像とcolor-only更新へ統合する。続いて材質作成・編集GUIとPaint→Material経路、材質Bake/Bridgeを通す。[材質graph](docs/Material-Graph.md)を参照。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-MaterialCore -Width 1280 -Height 800 -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
