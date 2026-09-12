# NyaForge 開発タスク

更新: 2026-09-12。材質付きGUI出力→独立Unity Bridge受取・保存・描画を接続して検証。共通Renderingパッケージへ分離。製品全体の開発は継続中。

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

- `Assets/NyaForge/Rendering` にshader/StandardMaterialAdapterを移動し、`com.nyaforge.rendering` としてPlayer/Bridgeで共有。CoreはUnity非依存のまま。材質値・両面描画・alpha方式を共通実装で扱う。
- Bridge `ImportMaterial` が全payload・対応pipeline/shaderを検査してから新規所有folderへMesh/Texture/Material/Prefabを保存する。途中の例外では自分のfolderだけを削除する。既存importを更新・置換しない。
- 「Unity用に書き出す」が最終出力に合わせて材質→Surface→mesh-onlyを選択する。Bridge windowに3形式の選択を追加し、ファイル選択時に既定manifest名から設定する。内容検証は各readerが行う。
- GUI試験をAPI直接出力から実際の出力ボタン操作へ変更。native reopen後の材質/mesh/画像の一致を検査し、同じ出力一式をreceiverへ渡した。
- Unity 6000.4.3f1 build成功: `Builds/Windows-C1C-MaterialBridge/NyaForge.exe`、`Logs/build-player-20260912-013829-320.log`。
- Windows Player PASS: `Artifacts/Authoring-20260912-013853-e1eb5ef79a2e432ca6fb8ce37d7d1155/report.json`。材質GUI、描画、出力、既存GUI・PNG30形式・準備競合を回帰。今回DensePaintなし。
- 独立Unity 2022.3.22f1 Built-In / Linear PASS: `Artifacts/BridgeReceiver-20260912-014001-240-471958d028da4b15930cb1841d7402ed/bridge-report.json`。旧scale1/100・Surface、新材質のshader参照/値/PNG設定/Prefab読戻し、注入失敗時のfolder rollbackを検査。`material.png`を目視し、紫の面とpaint線を確認。
- 現receiver材質fixtureは画像付きCutout。可視objectの描画成功とserialized値の一致を示すもので、全alpha mode・任意照明・全形状の外観一致ではない。Bridge windowはコンパイル済みだがEditor GUIの手操作は未確認。
- Coreの変更なし。直近Core173 passed / 0 failedは `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-3809aa9c8b91431bade870d46a5e1cb1`。今回のUnity回帰と区別する。
- [材質Bake契約](docs/Material-Bake.md)、[前段の記録](docs/history/2026-09-12-C1C-Before-Material-Bridge.md)参照。
## 次の作業

1. 材質付きmeshの法線欠落時のreceiver表示を確認・整備し、全alpha mode/画像なしの保存とreceiver描画を検証する。標準材質の一周から、異なる複数材質・他texture slotへ進む。URPは未対応として保持。[材質Bake](docs/Material-Bake.md)参照。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-MaterialBridge -Width 1280 -Height 800 -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `Assets/NyaForge/Rendering/`: Playerとreceiverが共有する材質adapter/shader。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
