# NyaForge 開発タスク

更新: 2026-09-12。材質BakeのCore出力/読取とWindows Player往復を実装・検証。Core173件とGUI回帰に合格。Bridge受取と製品GUI出力経路は未接続。製品全体の開発は継続中。

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

- `MaterialBakeStore` はgeometry、standard材質parameters、任意baseColor画像を別profile/manifestで出力・読取する。旧mesh-only/画像Surfaceのmanifest仕様は維持。新形式は `material.nyaforge-bake.json`。
- `MaterialParametersCodec` は44byte canonical payloadを所有し、範囲/enum/noncanonicalを拒否。`BakeImagePayload` に画像blob/PNGの保存・照合を共通化し、旧Surfaceも同じ検査を使用する。
- 新manifestのbaseColorは0/1件の必須配列。共通Storageのnull/欠落禁止ルールを緩めず、画像なしは空配列で表現する。hashとPNG/画像の一致を検査し、最後にmanifestをatomic置換する。
- Core **173 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-3809aa9c8b91431bade870d46a5e1cb1`。3alpha mode、画像あり/なし、native往復後の出力、同じ入力の再出力、PNGコピー所有、破損で旧manifest保持、schema/profile/field/payload異常、未割当/未解決時の非作成を確認。旧Surfaceのscale1/100も回帰。
- Unityコンパイル成功: `Builds/Windows-C1C-MaterialBake/NyaForge.exe` / `Logs/build-player-20260912-012528-124.log`。
- Windows Player PASS: `Artifacts/Authoring-20260912-012652-2e708dabc3a34ee2b8cf5a31230d50cd/report.json`。材質GUIでnativeを開き直した後に新APIで出力/読取し、mesh/材質hash/全画像byteの一致を検査。既存GUI、PNG30形式、準備競合も回帰。今回はDensePaintなし。
- 次のreceiver入力: `Artifacts/Authoring-20260912-012652-2e708dabc3a34ee2b8cf5a31230d50cd/material-export/material.nyaforge-bake.json`。同directory上のmaterial-export-manifest.txtにもpathを記録。まだBridgeはこの新形式を読まないため、既存receiver試験を新材質の合格証拠にしない。
- [材質Bake契約](docs/Material-Bake.md)、[前段の記録](docs/history/2026-09-12-C1C-Before-Material-Bake.md)参照。history内の「次」は当時の状態。

## 次の作業

1. 新しいMaterialBakeStoreをUnity Bridgeへ接続する。previewと同じ両面/alpha/linear色を使うshader/adapterの共有とpackage配置を決め、全payload検証後の独立asset folder作成、Material/texture/Prefab保存、失敗rollback、読戻しと描画を通す。旧profileの互換を維持し、検証後に製品GUIの出力ボタンへ接続する。[材質Bake](docs/Material-Bake.md)を参照。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-MaterialBake -Width 1280 -Height 800 -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
