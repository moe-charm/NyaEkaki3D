# NyaForge 文書の入口

更新: 2026-09-11。まずWindowsで開発する。macOSは将来対応。

| 文書 | 役割 |
|---|---|
| [current_task.md](../current_task.md) | 現在の作業、直近の完了事項、次に着手する範囲 |
| [Rig / VRM修正後の検証範囲](reviews/2026-09-12-Repair-Coverage.md) | 正式回帰とPlayer検査の対応、未確認経路と手動受入の区別 |
| [取込骨対応](Imported-Bone-Mapping.md) | source nodeとstable BoneId、VRM humanoid対応、永続化の残件 |
| [2026-09-12 Rig / VRMレビュー](reviews/2026-09-12-Rig-Vrm-Review.md) | bb1d89cの再現不具合、修正タスクR01〜R10の完了条件と検証の限界 |
| [VRM Spring詳細](VRM-Spring-Details.md) | 重力方向と形状、Spring session v3、旧設定の不明値移行 |
| [SpringBone時間契約](SpringBone-Time.md) | 停止・再開、可変時間の積分、物理履歴と表示の違い |
| [SpringBone制約](SpringBone-Constraints.md) | 長さとsphere衝突の同時制約、計算上限、解なしと未収束の診断 |
| [SpringBone姿勢契約](SpringBone-Pose-Contract.md) | base poseとState、親子の相対変換継承、連続stepの検証と残件 |
| [作品とVRM設定の一括保存](Project-Metadata-Snapshots.md) | schema 4のmetadata参照、旧sidecar移行、単一manifest公開と保存失敗保護 |
| [設計v2](NyaForge-Authoring-Design2.md) | 製品目標・全体設計の正本。Blenderなしの制作完結、ノードと直接編集の統合 |
| [開発計画](Development-Plan.md) | ローカル実装との差分、C0〜C5への対応、直近の作業単位と終了条件 |
| [使い方](Authoring-Quickstart.md) | 現在動く機能だけの起動・操作・再検証手順 |
| [Core README](../Assets/NyaForge/Authoring/README.md) | 現在の制作CoreのAPI・保存形式・制限 |
| [Rig Core](../Assets/NyaForge/Authoring/Rig/README.md) | rest skeletonと正規化skin bindingのC2基盤・未接続範囲 |
| [Unity Bridge](../UnityBridge/README.md) | 現在の受け取り用Editor packageの導入と制約 |
| [Unity再取り込み](Unity-Import-Updates.md) | 生成資源の所有記録、変更検査、参照を保持する更新transactionの設計 |
| [preview更新と所有権](Projection-Updates.md) | 形状の全体交換と色だけの交換、Prepare/Commit/Rollbackの契約 |
| [3D Paint負荷計測](Surface-Paint-Performance.md) | Core/Playerの測定条件、比較値と未解決の待ち時間 |
| [command計測とidentity](Command-Performance.md) | commandの時間内訳、graph/node identity cacheと保存処理の境界 |
| [3D描画の準備ジョブ](Surface-Preparation.md) | workerの所有、世代による失効、取消とGUI接続の残件 |
| [材質graph](Material-Graph.md) | standard材質の型・画像binding・保存と、表示/GUI/出力の実装順 |
| [部位別材質](Material-Slots.md) | 制作slotと材質identity、描画対応、継承と複数割当の実装順 |
| [材質Bake](Material-Bake.md) | 材質と任意画像を保持する出力形式、検証・所有・Bridge接続の残件 |
| [複数材質Bake](Multi-Material-Bake.md) | 元slot・材質UUID・画像を保持するCore形式とGUI/receiver接続の残件 |
| [設計v1](NyaForge-Authoring-Design.md) | 旧設計。背景の参照用。製品範囲・実装順はv2を優先 |
| [2026-09-11 実装履歴](history/2026-09-11-Initial-Authoring-and-Navigation.md) | 最初の制作往復とGUI整理の記録・検証証拠。現在の指示書ではない |
| [C0-R実装履歴](history/2026-09-11-C0R-Empty-Projects.md) | 空project、モジュール分割、schema 1→2移行の実装と検証 |
| [C1-Aグラフ基盤](history/2026-09-11-C1A-Graph-Foundation.md) | 型付きグラフ評価の実装記録。アセット保存の続報は次の文書 |
| [C1-Aアセット保存](history/2026-09-11-C1A-Graph-Storage.md) | グラフblobと未知payloadの保存。project本体とcanvasは後続 |
| [C1-Aプロジェクト保存](history/2026-09-11-C1A-Graph-Projects.md) | 任意graphの文書所有とnative schema 3。commandとcanvasは後続 |
| [C1-A共通command](history/2026-09-11-C1A-Graph-Commands.md) | graph差替え・共通Undo・revision付きpreview。Unity GUIは後続 |
| [C1-Aノード操作](history/2026-09-11-C1A-Node-Commands.md) | 空から生成・接続・編集する共通command。Unity GUIは後続 |
| [C1-Aグラフ出力](history/2026-09-11-C1A-Graph-Bake.md) | graphのstatic BakeとUnity受け取り検証。現在の残件も記載 |
| [C1-A UI往復](history/2026-09-11-C1A-Pointer-Roundtrip.md) | panelの当たり判定とpointerイベントで制作・保存・出力を検証 |
| [C1-B整理前記録](history/2026-09-11-C1B-Before-Preparation.md) | 文書整理前の詳細進捗。古い作業一覧を含むため、再開はcurrent_taskから |
| [C1-B押出し・厚み・表示](history/2026-09-11-C1B-Extrude-Solidify.md) | 押出し・厚み・選択面描画の実装と検証記録。Mirror導入前のスナップショット |
| [C1-B UV・Mirror](history/2026-09-11-C1B-UV-and-Mirror.md) | UV島編集とMirror、最終結果同時表示までの検証履歴 |
| [Paint基盤](../Assets/NyaForge/Authoring/Paint/README.md) | 画像tile・brush・色空間・UV binding、typed graphとnative保存。GUIコードあり、操作検証はcurrent_task参照 |
| [C1-C Paint整理前記録](history/2026-09-11-C1C-Paint-Before-Preparation.md) | Paint基盤とgraph統合の証拠。GUI追加前の記述を含む過去記録 |

読む順序は `current_task.md` → 開発計画 → 設計v2の該当節 → 対象コード。全体設計に機能が載っていても、実装済みとは限らない。持込設計の固定コミット調査と、ローカルの未コミット実装を区別する。

[C1-C Paint GUI・再割当・依存表示の履歴](history/2026-09-11-C1C-Paint-Dependencies.md)には、PNG出力前の実装と検証を保存している。

[Surface Bake形式](Surface-Bake-Format.md)は画像付きstatic出力の契約。[PNG単体出力の履歴](history/2026-09-11-C1C-Png-Export.md)はその前段の記録。

## 更新の約束

[3Dペイント開発契約](Surface-Paint-Development.md)はray/UV探索の実装範囲と、seam・gesture・GUI接続の残件を分けて定義する。

[画像取り込み契約](Paint-Image-Import.md)はPNGの色・透過・サイズ調整・所有・ファイル選択と確認範囲を定義する。

[マスク描画Coreの履歴](history/2026-09-11-C1C-Mask-Core.md)は共有ブラシ範囲計算、線形mask描画、共通commandと130件Core検証の記録。mask GUIの現在地はcurrent_taskを参照する。

- 製品方針は設計v2、具体的な開発順は開発計画、今回の進捗はcurrent_taskへ書く。同じ作業一覧を全ファイルへ複製しない。
- 実装後は「実装・自動検証・手動確認・未確認」を分けて記録し、使い方も現物に合わせる。
- 完了履歴はhistoryへ移し、current_taskは次の開発者が着手できる長さに保つ。履歴内の段階名や「次」は当時の記録。
- 設計v2の外部ライブラリ・SDK情報は採用候補の調査記録。実装時に採用版・利用条件・Player対応を再確認し、未採用機能をcapabilitiesへ載せない。
- コード変更なしの文書整理ではビルド合格を新たに主張しない。保存schemaの変更には独立した移行方針と検証を用意する。


