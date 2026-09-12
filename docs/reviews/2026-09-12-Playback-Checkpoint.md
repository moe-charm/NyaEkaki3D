# VRM1再生接続後のチェックと次の作業

対象: `4c9ced1`。ユーザー依頼「チェック後 current_task 更新してタスク化」に対応。今回はレビュー・Core再実行・文書更新のみ。製品実装は変更していない。

## 確認結果

- Coreを今回再実行し **353 passed / 0 failed**。`Logs/core-checkpoint-taskification.txt`、成果物 `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-dcf9b8c76e474c7ba28e80a10bc9f425`。
- VRM1の再生・停止・Reset、文書/metadata変更時の破棄、一時graph評価、Mesh/Object再利用と編集点復元のコードを照合した。確認範囲で新しい再現不具合は確定していない。以下は残仕様と未検証項目である。
- 既存Windows-SpringMeshReuseの `Artifacts/Authoring-20260912-153336-c66834f83e614fecb6278247149ea256/report.json` は `passed=true`。再生handler、Mesh/Object同一性、保存時の一時姿勢除外が記録されている。今回build/Playerは再実行していない。スクリーンショットの目視、実クリック、実素材、速度は未確認。
- `GlbSkinImport.cs` は全nodeのtranslationを計算するが、返す原点はskin jointだけ。`ImportedRigSessionCodec` はv2で全source階層を保存しない。通常nodeや非joint末端を含むVRM0展開の前提が不足している。これは修正済みR11の「joint間の祖先関係保持」と別の残件。
- `SpringFixedClock` は1frameが0.25秒を超えると拒否し、Workbenchは例外時に再生を停止・元姿勢へ戻す。負荷時やウィンドウ操作時の継続性をまだ受け入れていない。
- `private/`、`Logs/`、`Builds/`、`Artifacts/` はignore対象。`git ls-files`でprivate名のpathおよび `.vrm/.fbx/.blend` の追跡はなかった。全履歴・全形式を対象にした素材監査ではない。

## 実行可能な作業単位

| ID / 優先 | 作業・責務 | 依存 | 完了条件 |
|---|---|---|---|
| T01 / P1 | 全source nodeの親子関係・原点を独立した不変型で保持し、rig sessionへ保存する | なし | 中間node、非joint末端、分岐、順序を保存/Openで保持。循環・範囲外・原点不一致・予算超過を拒否。v1/v2は階層不明のまま移行し、骨から推測しない。CoreとPlayer取込往復を確認 |
| T02 / P1 | VRM0 root展開、末端、center、設定継承をadapterへ実装しpreviewへ接続する | T01 | 公式仕様/実装に基づき分岐・末端方向・長さ・座標系を文書化。通常nodeの実行対応範囲を決め、未対応は明示拒否。数値回帰と取込→再生→保存/Openを確認。VRM1回帰を維持 |
| T03 / P1 | ローカル実素材の取込要件を読み取り確認する | なし、T01と独立 | 対象ファイル・mesh/skin数・node変換・非joint参照・morph/材質の必要範囲をprivate記録へ残す。I04の具体的実装単位と対応外表示を決める。素材をコミットしない |
| T04 / P2 | フレーム遅延時の動作と性能を測り、時間超過の扱いを決める | 対応profileの入力 | 通常再生と0.25秒超の入力、ウィンドウ操作を確認。frame時間・GC・メモリを記録。停止/再開/時間破棄等の方針を明示し、採用方針の回帰を追加。Mesh再利用だけで高速化済みと判定しない |
| T05 / P1 | Windows実操作・実素材の受入 | T03で対象確定、必要なI04対応 | 対象build/入力/解像度/DPIと結果を記録。ファイル選択→表示→再生/停止/Reset→Save/Open、Undo、保存して終了、文字サイズ・欠けを実操作で確認。未対応素材は成功表示しない |
| T06 / P2 | 外部MCP transportからmetadata保存を確認する | 保存対応build | 外部クライアントの取込/保存/Open、失敗時旧状態・dirty保持と再試行を証拠化。内部handler試験と区別する |

次の実装単位はT01。T03の結果によってI04をT02/T05に先行させる。T01/T02はI03-B、T04はI03-C、T05/T06はA01に属する。既存C0〜C5、skin/morph出力と受け取り先検証は開発計画に残す。タスク化は実装完了を意味しない。
