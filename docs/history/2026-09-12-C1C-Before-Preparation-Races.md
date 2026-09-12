# NyaForge 開発タスク

更新: 2026-09-12。準備queueをGUIへ接続し、最新Readyだけで描画を開始する。Core163件とGUI・高密度Player回帰に合格。最終整理後のPlayer再確認も合格。製品全体の開発は継続中。

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

- `AuthoringWorkbench.SurfacePreparation` が準備queue・入力・世代・状態表示・再試行を所有。主threadでcameraを値に写し、workerでray/coverageを構築。50ms pollとcamera/GUI変更時の入力検査を行い、同じ入力は再要求しない。
- pointer downは最新世代のReadyだけを採用。準備中のpointerは後から自動再生しない。camera/mesh更新で旧strokeを取消し、mode解除・close・detach・workspace交換でClear、破棄でDispose。旧同期coverage cacheを削除した。
- `SurfacePreparationInput.WithCamera` は所有済みimmutable logical IDを共有。Core既存163件に共有・ray再利用の検査を追加し **163 passed / 0 failed**。`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b9ec99e45e2a4a7da0d1a91e0dc53b77`。
- GUI試験はworker完了をframeを進めながら待つ。色/mask、取消、Undo/Redo、保存/出力、camera操作、viewport resizeとcoverage再利用、seam/gap/occluder、高密度描画がPASS: `Artifacts/Authoring-20260912-003610-b66a300d75d74980a3440973ea3cf1d4/report.json`。
- 同profileの131,072triで、準備待ち約1,350ms、Ready後の初回down約13ms、up約36ms。前段の初回down約984msから構築を切り離した。準備コスト自体が消えた意味ではない。待機値は最終camera設定後の残り時間で、全CPU時間/GPU完了時間ではない。
- 最終整理版 `Builds/Windows-C1C-PreparationGuiFinal/NyaForge.exe` / `Logs/build-player-20260912-003716-433.log` はコンパイル成功。最終PlayerもPASS: `Artifacts/Authoring-20260912-003814-87c823d218ba4c8e87b030b470839e03/report.json`。準備待ち約1,350ms、初回down約13ms、up約38ms。surface-paint.pngで準備完了表示を目視確認。
- [準備ジョブの契約](docs/Surface-Preparation.md)、[前段の記録](docs/history/2026-09-12-C1C-Before-Preparation-Gui.md)、[負荷計測](docs/Surface-Paint-Performance.md)参照。history内の「次」は当時の状態。
## 次の作業

1. GUI準備中の競合を意図的なworker停止で検査する。camera連続変更、workspace交換、mode切替、失敗表示と再試行を対象にする。Coreのbarrier検査と通常Player回帰は済みだが、GUIでの競合を強制する検査は残る。曲がった長いstroke、多数区間、ray4096回上限、退化したscreen交差とOS/DPI手動操作も残す。
2. Paintの代表PNG（palette/gray/16bit/interlace）、ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. 一般Material graph、複数材質、UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-PreparationGuiFinal -Width 1280 -Height 800 -DensePaint -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
