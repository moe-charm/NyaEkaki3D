# 3D描画データの準備ジョブ

2026-09-12。CoreのqueueをGUIへ接続済み。pointer downでは構築せず、最新入力に対応するReady結果がある場合だけ描画を始める。

## 入力と結果

`SurfacePreparationInput` はimmutable MeshData、配置、SurfaceCameraSnapshotとlogical vertex IDのコピーを所有する。呼出元のID配列を書き換えても入力は変わらない。完成結果 `PreparedSurface` はray BVHとscreen coverageの両方を持つ。片方だけ完成した状態をReadyとして公開しない。

`SurfacePreparationQueue.Request(input)` は世代番号を返す。`Read()` はimmutableな `SurfacePreparationStatus` を返し、phaseはIdle/Preparing/Ready/Failed/Disposed。Readyだけが完成surfaceを持つ。過去に読んだstatusは書き換えられないため、GUIは世代番号と現在の入力を確認してから採用する。

## 実行・取消

- workerは同時に1つ。実行中の次に待機できる入力も1つで、新しいRequestが来たら待機入力を置き換える。
- 新しいRequestは実行中のCancellationTokenを取消し、以前の世代の結果/失敗を公開しない。builderが取消を無視して遅れて返っても、この世代検査で破棄する。
- `Clear()` は世代を進め、待機入力・完成結果・再利用rayを捨ててIdleへ戻す。workspace交換に使う。
- `Dispose()` は待機入力と完成結果を捨て、実行中へ取消を通知する。UI threadで終了待ちは行わず、その後のRequestは拒否する。
- 例外はFailedのErrorとして観測可能にし、Taskの未観測例外として放置しない。後続Requestで再試行できる。
- 現在の取消チェックはray構築の前後とcoverage構築後。構築ループそのものを即時中断するものではない。取消後も進行中の1段階の計算が短時間続く場合がある。

同じmesh hash・配置・logical IDでcameraだけを変えた場合は、直前に成功したray BVHを再利用する。cameraに依存するcoverageは作り直す。取消された半完成rayをcacheへ公開しない。Clear/Disposeでcache参照を解放する。

## 確認したこと

Core163件の中で、実workerの準備、camera変更時のray再利用、logical IDのコピーと変更時の再構築、待機入力の集約、遅い完了の世代検査、Clear/Dispose後の非公開、失敗の観測と再試行を検査した。race試験にはbarrierを使い、取消を意図的に無視して返るbuilderも試す。Unity向けコンパイルも確認するが、GUIの準備状態表示を試験したという意味ではない。

## GUIの実装

`AuthoringWorkbench.SurfacePreparation` がGUI側の所有者。50msのscheduleとcamera更新・Paint refresh・pointer downで現在の入力を確認する。mesh hash・配置・UV hash・render vertex map参照・camera snapshotが同じならRequestを再送しない。cameraのみの更新では `WithCamera` で既にコピーしたimmutable logical IDを共有し、大量のID配列を毎回コピーしない。

Preparing/Ready/Failedを日本語で表示し、Failedには再試行ボタンを出す。準備中に押したpointerは保留・再生せず、準備完了後に新しく押して開始する。Readyの世代と現在入力を確認し、strokeはその完成結果をclosureへ固定する。主threadでTask.Wait/GetResultは使わない。

mode解除・workbenchを閉じる操作・panel detach・workspace交換でClear、OnDestroyでschedule停止とDisposeを行う。camera変更は進行中strokeを取消し、新しいcoverageを要求する。旧同期 `SurfaceProjectionCache` は削除した。

Player回帰は実workerのReadyをframeを進めながら待ち、色/mask、取消、Undo/Redo、保存/出力、camera操作、viewport変更によるcoverage交換と同一入力での再利用、境界fixture、高密度描画を検査する。従来の「2frame後なら構築済み」という仮定は使わない。高密度profileでは準備待ちを `preparationWaitMs` に別記する。これは最終camera設定後の待機部分で、全準備のCPU時間やGPU完了時間ではない。

## GUI競合の再現試験

`ControlledSurfacePreparation` は実queueのbuilderを差し替え、初回workerをbarrierで停止する検証fixture。Coreはhost assemblyにinternalアクセスを許可し、公開builder APIやユーザー設定を追加せずにPlayer内で競合を再現する。以後の完成処理には実ray/coverageを使う。取消を無視して返すため、古い結果を排除するのは実queueの世代検査である。fixtureは検証時だけ生成し、通常起動のqueueには介入しない。

`AuthoringWorkbench.SurfacePreparationRaceVerification` は準備中の連続camera変更、mode解除、空workspaceへの交換、失敗表示とpointerによる再試行を検証する。同一入力で再要求しないこと、Preparing表示、準備中のpointerで作品も仮描画も変化しないこと、遅いUpでも再生されないことを各ケースで確認する。mode解除/交換後はworker完了までframeを進め、Idleに古い結果が上書きされないことを確認する。検証が失敗/中断してもbarrierは解除し、worker終了をUI threadで待たずに資源を破棄する。

`Windows-C1C-PreparationRaces` / `Artifacts/Authoring-20260912-004242-9a3f2ec63c6e4335b0e688d46f4b82dc/report.json` で通常GUI・競合・高密度描画がPASS。Core163件も合格。OS/DPI実操作、アプリ終了とOS側の強制終了はこの検査には含まない。

全体の入力契約は [3Dペイント開発契約](Surface-Paint-Development.md)、計測条件は [負荷計測](Surface-Paint-Performance.md)を参照する。
