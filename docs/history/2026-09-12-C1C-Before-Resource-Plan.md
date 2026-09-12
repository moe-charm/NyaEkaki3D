# NyaForge 開発タスク

更新: 2026-09-12。journal v2にstaging所有を記録。強制終了後の復元と一時フォルダ限定cleanupを3地点で確認。復旧中断試験と資源計画は残る。

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

- `StagingOwnership` を追加。専用フォルダ名・GUID・全ファイルfingerprintを記録し、更新先とは異なることを確認して削除。内容変更があれば残して拒否。通常finallyも同じ限定確認を使う。
- journal v2はhasStagingフラグと所有情報を保持。旧v1/一時フォルダなしを読む。復旧で対象資源を戻した後にstagingを除去。commit済みでもstagingが残ればPending一覧へ出し、対象資源を戻さずcleanupだけ行う。
- JsonUtilityがnullのclassを空objectとして読むため、hasStagingを明示してnull判定を修正した。
- 一時的なFile.Replace失敗に最大5回・計620msの再試行を追加。journal保存は非atomic上書きへ退避しない。資源復元の既存fallbackは維持。
- PASS: `Artifacts/BridgeReceiver-20260912-032247-646-5f7de6a9b6dd46ad8a99fb936f5f118f/` のbridge-reportとmaterials/prefab/receipt-recovery.json。Unity2022.3.22f1。3地点の実Kill→別PID復旧で、元file/metaとGUIDだけでなく、Assets直下のフォルダ集合も開始前と一致（staging除去・他フォルダ維持）。正常更新・例外rollbackも回帰。
- Core/Player変更なし。直近Core186件、入力Player `Artifacts/Authoring-20260912-025612-a1dd9f3040614b619a455d00274fa3ac/report.json`。前段: [履歴](docs/history/2026-09-12-C1C-Before-Staging-Cleanup.md)。
- 未確認: 復旧途中のKill（内部checkpoint引数のみ追加済み）、staging作成後journal作成前のKill、staging外部変更/cleanup失敗fixture、commit後Kill、新規資源GUID未確認/画像共有分岐、GUI手操作。全目標の完了ではない。
## 次の作業

1. 復旧途中Kill→別processで再試行するfixtureを追加（UpdateJournal.Recoverの内部checkpointが利用可能）。続いて資源追加/共有分岐/削除候補previewと幾何更新を実装/検証する。stagingのjournal登録前中断、未確認新規GUID、scene override、GUI手操作を保持する。複数材質の同UUID共有receiver fixture、部位別GPU定量/alpha、mask/重なり遮蔽は検証残件として保持。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-BakeOutputIdentity -Width 1280 -Height 800 -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `Assets/NyaForge/Rendering/`: Playerとreceiverが共有する材質adapter/shader。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
