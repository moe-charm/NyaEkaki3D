# NyaForge 開発タスク

更新: 2026-09-12。複数材質のGUI出力→Unity receiver→Prefab保存/描画を確認。Core184件、Windows Player、Unity2022.3.22f1 Bridge成功。

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

- `MultiMaterialBakeStore` の専用profileをGUI Exportへ接続。Unity Import画面に「部位別PBR材質付き」を追加。`MultiMaterialImporter` にslot処理を分離し、既存の所有フォルダ/Prefab/rollback手順を共用。
- submesh順に材質を割当。材質UUIDを共有するslotは同一Material、PNG hashを共有する画像は同一Textureへ接続。`MaterialSlots.json` に元slot・UUID・材質pathの対応を保存。未使用slotはBakeに保持、描画資源は使用slotのみ。
- Core **184 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-ce00da690d2f4e70b37759b9a738b3d2`。
- Windows build: `Builds/Windows-C1C-MultiMaterialDistinct/NyaForge.exe`、`Logs/build-player-20260912-024548-581.log`。
- Player PASS: `Artifacts/Authoring-20260912-024618-aa61cdc0ae374f5d888e5ecf5af3641c/report.json`。GUI部位別Paintの後、実際のexport buttonを押し、slotごとの材質UUID/parameters/画像pixels、meshを比較。二部位のparametersとPNG hashが異なるfixtureへ強化。DensePaintは今回未実行。
- Bridge PASS: `Artifacts/BridgeReceiver-20260912-024701-006-de01b803b13c4383b4c585693a484c0e/bridge-report.json`。Unity2022.3.22f1 Built-In/Linearで新規Prefab・mesh・材質・PNG・slot mapを保存/読取照合、途中失敗でowned folderのみ削除、既存成功import保持を確認。旧mesh/Surface/単一Material（normal/alpha）も回帰。
- `multi-material.png` は同receiver直下。青い前面・灰色の周囲と描画strokeを目視確認。画像検査はvisible objectであり、各部位の定量GPU一致や全alpha組合せを証明しない。
- 最初のreceiver `Artifacts/BridgeReceiver-20260912-024447-858-45ba692b1f0a4b89a8a4640a7920c707` は別材質UUID・同じPNGのfixtureでTexture共有を検証。複数slotが同一Material UUIDを使うreceiver fixtureは追加の余地あり。
- [形式契約](docs/Multi-Material-Bake.md)、[使い方](docs/Authoring-Quickstart.md)、Bridge README更新。前段は [履歴](docs/history/2026-09-12-C1C-Before-MultiMaterial-Bridge.md)。
## 次の作業

1. 次の製品作業は出力identityを保持する更新契約とreceiver更新処理。既存Prefab/Material参照とユーザー変更をどう保護するかを設計に照合し、初回importと分離した更新transactionを実装する。複数材質の同UUID共有receiver fixture、部位別GPU定量/alpha、mask/重なり遮蔽は検証残件として保持。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-MultiMaterialDistinct -Width 1280 -Height 800 -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `Assets/NyaForge/Rendering/`: Playerとreceiverが共有する材質adapter/shader。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
