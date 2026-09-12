# NyaForge 開発タスク

更新: 2026-09-12。PNGの30形式を実Playerで全画素比較し合格。palette/tRNSの事前検査を強化。Core164件とGUI回帰が合格。製品全体の開発は継続中。

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

- `Tools/Generate-PngVariants.py` が30形式の合成PNGと期待RGBAを生成。gray 1/2/4/8/16、RGB 8/16、palette 1/2/4/8、gray-alpha/RGBA 8/16を通常scanlineとAdam7でそれぞれ検査。通常scanlineは全5filter、9×11はpacked row端数と全7passを通す。
- `PngVariantVerification` は製品importerで展開し、sourceから導いた期待値と全byteを照合。alpha0のRGB、上下方向、部分palette tRNS、16bit sample 0/1が8bit同色になっても透明判定を混同しないことを確認。任意16bit値の丸め仕様全体を検証済みとはしない。
- `PaintPngInput` のpalette/tRNS構造検査を強化。indexed palette必須・bit depth上限・grayへのpalette禁止・tRNSの順序/重複/長さ/色形式をnative decoderへ渡す前に拒否する。
- Core **164 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-2282a1ba382549448b285467b8c1e485`。
- Unityコンパイル成功: `Builds/Windows-C1C-PngVariantsValidated/NyaForge.exe` / `Logs/build-player-20260912-004846-813.log`。
- Player PASS: `Artifacts/Authoring-20260912-004922-68b5a9bf5f8e418f9734e151302c6207/report.json`。`png-variants.txt`に全30形式のbyte一致。既存のGUI追加/Undo/保存/Surfaceと準備競合も回帰。今回はDensePaintなし。最新dense PASSは前段 `Artifacts/Authoring-20260912-004242-9a3f2ec63c6e4335b0e688d46f4b82dc/report.json`。
- [画像取り込み契約](docs/Paint-Image-Import.md)、[前段の記録](docs/history/2026-09-12-C1C-Before-Png-Variants.md)参照。history内の「次」は当時の状態。

## 次の作業

1. 設計v2/C1-Cに従い一般Material graphの最初の実用経路を設計・実装する。現行Output.baseColor画像との移行・保存互換性を確認し、材質値の所有、評価、GUI、出力を同じ契約で接続する。複数材質/toon/透明描画も製品範囲として維持する。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-PngVariantsValidated -Width 1280 -Height 800 -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
