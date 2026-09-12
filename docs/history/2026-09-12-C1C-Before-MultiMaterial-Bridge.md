# NyaForge 開発タスク

更新: 2026-09-12。複数材質BakeのCore保存/読取を追加。Core184件成功。GUI出力とUnity receiver接続が次。

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

保存はstatic writer schema2、graph writer schema3、reader 1/2/3。Paintは共有画像と部位別の独立画像に対応。材質未割当は不透明preview、標準材質は3alpha modeを選択できる。UV変更時は旧payloadを未解決として保持し、明示rebindで対応を更新する。見た目を保つ再投影ではない。旧mesh-only Bakeは画像を拒否。画像付きmeshは別のSurface Bake profile、単一標準材質はMaterial Bakeを利用。複数材質BakeはCoreのみ追加済み。

## 今回の変更と証拠

- `MultiMaterialBakeStore` を追加。`materials.nyaforge-bake.json` に元slot番号、dense submesh対応表、材質UUID、parameters hash、任意baseColor descriptorを保存。未使用の接続slotも保持。全payloadを書いた後にmanifestを置換。
- 読取ではslot重複/範囲/順序、submesh対応漏れ/個数、共有材質UUIDのpayload矛盾、画像破損を拒否。旧3profileの複数材質拒否は保持。共通Storageは整数配列だけ厳密な型検査を追加。
- Core **184 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-5aea6b3377684ba3b9678b7803dfd8b0`。疎な3/9のslot、独立画像、native再読込後の再出力、共有UUID、破損PNGと不正manifestを検証。
- GUI/receiverはまだ旧経路。新profileのUnity持込み成功を意味しない。以下は前段のPlayer検証記録。
- 新Coreを含むWindows build成功: `Builds/Windows-C1C-MultiMaterialBakeCore/NyaForge.exe`、`Logs/build-player-20260912-023902-591.log`。このbuildのPlayer実操作テストは未実施。[形式契約](docs/Multi-Material-Bake.md)参照。

- `MaterialSlotPaintEditing.AddBlank` と `AuthoringWorkbench.MaterialSlotPaint` を追加。選択slotだけ材質を複製し、新しい白紙Paintへ接続。旧画像/材質ノードを保持し、共有材質の他slotを上書きしない。ReplaceGraph 1commandで適用。
- 色塗り欄の新ボタンに対象部位番号を表示。材質欄で部位を選ぶと更新。旧単一出力用addPaintは複数材質時に無効。
- Core **182 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f5e68a0afb0240a9b708690f28d061f7`。選択slotのみ接続・旧画像保持・他slot保持・Undo/Redo/nativeを確認。
- Unity6000.4.3f1 build成功: `Builds/Windows-C1C-IndependentSlotPaint/NyaForge.exe` / `Logs/build-player-20260912-023007-107.log`。
- Windows Player PASS: `Artifacts/Authoring-20260912-023034-d6e0727f0ce14dc4bc780e9ce49f866b/report.json`。共有Paint→新slot Paintボタン→layer化→3D strokeで新画像だけ変更、旧Paintへ戻して対象外のcap面をドラッグした際に仮表示/文書変更なしを確認。既存GUI/材質GPU/準備競合も回帰。今回DensePaintなし。
- 対象外cap面の拒否枝は検証したが、直後に別の対象面が重なる遮蔽構成・maskの複数材質fixtureは未確認。Bridge未変更、複数材質出力は未接続。
- [使い方](docs/Authoring-Quickstart.md)と[部位別材質契約](docs/Material-Slots.md)へ手順/範囲を追記。[前段](docs/history/2026-09-12-C1C-Before-Independent-Paint.md)を保存。

## 次の作業

1. 追加済み `MultiMaterialBakeStore` をGUI出力とBridgeへ接続する。receiverで全payload読取後に資源生成、submesh順の材質割当、共有材質/画像の参照、Prefab永続化/描画と失敗rollbackを検証する。旧all-slots profileは保持。mask/透明輪郭/重なり遮蔽・新部位操作の残りは別途保持。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-IndependentSlotPaint -Width 1280 -Height 800 -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `Assets/NyaForge/Rendering/`: Playerとreceiverが共有する材質adapter/shader。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
