# NyaForge 開発タスク

更新: 2026-09-12。準備queueのworker制限・世代・取消・回復をCoreへ追加。Core163件とUnityコンパイル成功。GUI非同期準備への接続は次の工程。製品全体の開発は継続中。

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

- `SurfacePreparationInput` にimmutable mesh/配置/cameraとlogical IDのコピーを集約。完成結果はray BVHとcoverageをセットで所有する。
- `SurfacePreparationQueue` はworker1件と置換可能な待機1件に制限。新しい世代だけがReady/Failedを公開し、遅れて完了した古い結果は破棄する。Clear/Disposeは待機・完成結果・cacheを捨て、実行中へ取消を通知する。呼出元を終了待ちで止めない。
- cameraだけ変更した場合は成功済みrayを再利用。logical IDの変更は再構築。例外はFailedとして観測し、次のRequestで再試行できる。取消チェックは構築段階間で、実行中の構築を即時停止するものではない。[準備ジョブの契約](docs/Surface-Preparation.md)参照。
- Core **163 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-952db231b6f74b59a961b272a36ada7b`。worker実行、ray再利用、入力配列の所有、待機集約、遅い結果の破棄、Clear/Dispose、失敗からの回復を追加検証。
- Unityコンパイル成功: `Builds/Windows-C1C-PreparationQueue/NyaForge.exe` / `Logs/build-player-20260912-002644-992.log`。今回のqueueはまだGUIで使用していないため、GUI非同期化の完了や初回待ち解消とは扱わない。
- GUI側の最新Player合格は前段の `Artifacts/Authoring-20260912-001844-ed1243ea0d2245d797310e5b25f632a6/report.json`。今回は新たなGUI回帰を実行していない。次は準備表示・世代確認・失効/取消をworkbenchへ接続する。
- 前段は [整理前の記録](docs/history/2026-09-12-C1C-Before-Preparation-Queue.md)、[3D入力契約](docs/Surface-Paint-Development.md)、[負荷計測](docs/Surface-Paint-Performance.md)、[画像取り込み](docs/Paint-Image-Import.md)、[小物一周](docs/history/2026-09-11-C1C-Item-Workflow.md)に保持。history内の「次」は当時の状態。

## 次の作業

1. 実装したSurfacePreparationQueueをworkbenchへ接続し、ray/coverageをpointer downより前に準備する。同じ入力を毎frame再送しない。Preparing表示、Ready世代確認、camera/mesh変更で失効、workspace交換/終了のClear/DisposeとPlayer競合試験を実装する。[負荷計測](docs/Surface-Paint-Performance.md)の初回構築約1秒が対象。曲がった長いstroke、多数の区間、ray4096回上限、camera変更後の構築費、退化したscreen交差とOS/DPI手動操作は残る。
2. Paintの代表PNG（palette/gray/16bit/interlace）、ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. 一般Material graph、複数材質、UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-PreparationQueue -Width 1280 -Height 800 -DensePaint -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
