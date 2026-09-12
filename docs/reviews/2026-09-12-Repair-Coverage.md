# Rig / VRM修正後の検証範囲

元レビュー: `bb1d89c`。R01〜R09の修正後、正式テストとPlayer検証の内容を照合した。数値や検証結果はcurrent_taskの各実装記録を参照。実アバター受入と機能全体の完成判定はこの表の範囲外。

| 項目 | 正式回帰で検査する経路 | 残る境界 |
|---|---|---|
| R01 authors | VrmMetadataTests / VrmSessionCompatibilityTests: 複数作者、旧version、native snapshot往復 | 実VRM全体のスキーマ、作者表示GUI |
| R02 collider node列 | VrmMetadataTests: VRM0/1の取込・codec往復、legacy native往復。Player SaveFailureVerification: 重複/混在node列のWorkbench保存・Open | VrmImportVerificationで同一VRM0/1ファイルの取込→保存→空workspace→OpenもPASS。実picker操作は別 |
| R03 保存保護 | ProjectSnapshotTestsとPlayer SaveFailureVerification: 新規/上書き失敗、旧manifest、dirty/version、再試行、GUI/MCP handler、終了判定 | 外部MCP transportのmetadata専用試験・終了ボタンの実クリック |
| R04 Pose/State | SpringPoseTests: 連続step、変化するbase pose、有限値・tail一致 | 実アバターpreviewは未接続 |
| R05 階層 | SpringPoseTests: 2〜3joint、逆順、head offset、未登録子孫、移動/回転/scale | 一般glTF変換の取込は別契約 |
| R06 参照分離 | SpringColliderScopeTests: 独立chain追加/削除、専用collider変更、明示共有 | 実髪束への設定は未接続 |
| R07 制約 | SpringConstraintTests: 衝突なし/あり、同軸、複数球、hitRadius、中心一致、解なし・反復上限 | sphere tail近似。capsule・連続衝突検出ではない |
| R08 時間 | SpringTimeTests: 停止/再開、停止中編集、可変時間の解析値、減衰、時間分割比較 | VRM設定の時間的意味との対応、実揺れの受入 |
| R09 既定値 | VrmMetadataTests: 省略値・明示0、session往復 | VRM0の既定値を一括変更していない |
| R10 検証強化 | 各修正に再現を追加。null group拒否、実参照ありの衝突、前stateを引き継ぐ反復を検査 | R02一気通貫は追加検証済み。上記手動/外部transport受入は独立して未完了 |

追加の`AuthoringWorkbench.SpringVerification`はWindows Player上で12stepのPose/State一致、親子追従、sphere距離、交互時間と停止履歴を検査する。これはUnityの実行環境でCoreを呼ぶ自動検証であり、Spring表示GUIを操作した証拠ではない。

R10は上記の自動検証範囲で完了。追加したVrmImportVerificationが同じVRM0/1ファイルの取込→Workbench Save→空workspace→Openを通し、graph/source/settingsを保持することを確認した。結果は `Artifacts/Authoring-20260912-140928-5c88b52461154cfe828d5a6bb9cc23cf/report.json`。手動受入は独立タスクとして維持する。C0〜C5の製品目標を、この修正レビューの終了へ縮小しない。
