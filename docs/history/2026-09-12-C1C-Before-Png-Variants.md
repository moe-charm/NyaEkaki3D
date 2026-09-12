# NyaForge 開発タスク

更新: 2026-09-12。実workerを停止させたGUI競合検査に合格。準備中のcamera連続変更・mode解除・workspace交換・失敗/再試行をPlayerで検証。Core163件と高密度回帰も合格。製品全体の開発は継続中。

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

- `ControlledSurfacePreparation` は検証専用fixture。実際のCore queueへ渡すbuilderの初回実行だけをbarrierで止め、UnityはframeとGUIイベントを進める。取消を意図的に無視して完成結果を返し、queueの世代検査による古い結果の破棄を実際に通す。待機には15秒の上限があり、Disposeは解除後にworkerの完了を非同期で待ってbarrier資源を破棄する。
- `AuthoringWorkbench.SurfacePreparationRaceVerification` を通常の3D GUI回帰へ追加。同一入力の8frameでは再要求なし、Preparingの表示、準備中のDown/Move/Upで描画なし、完了後の遅いUpでも再生なしを検査。
- cameraを3回更新したときは初回と最新の2回だけを構築。mode解除・空workspaceへの交換後に古いworkerが返ってもIdleを維持し、再開/復元後は新しくReadyになることを確認。
- workerの意図的な失敗がGUIに表示され、自動で無限再試行せず、実際の再試行ボタンのpointer clickでReadyへ回復することを確認。各ケースで確定済み作品・仮描画が変化しない。
- Core **163 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0091c5907b8f464db73de1eff1319f78`。
- Unityコンパイル成功: `Builds/Windows-C1C-PreparationRaces/NyaForge.exe` / `Logs/build-player-20260912-004206-424.log`。
- 全GUI・競合・131,072tri描画のPlayer PASS: `Artifacts/Authoring-20260912-004242-9a3f2ec63c6e4335b0e688d46f4b82dc/report.json`。競合ケースはSurfacePaintUiの中で実行され、失敗時はreport.failureへ伝播する。
- [準備ジョブの契約](docs/Surface-Preparation.md)、[前段の記録](docs/history/2026-09-12-C1C-Before-Preparation-Races.md)、[負荷計測](docs/Surface-Paint-Performance.md)参照。history内の「次」は当時の状態。

## 次の作業

1. 代表PNG（palette/gray/16bit/interlace）の入力をPlayerで検証し、画像取り込みの対応契約と残件を整理する。準備queueの通常GUI接続と競合検査は済み。曲がった長いstroke、多数区間、ray4096回上限、退化したscreen交差とOS/DPI手動操作は残す。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. 一般Material graph、複数材質、UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-PreparationRaces -Width 1280 -Height 800 -DensePaint -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
