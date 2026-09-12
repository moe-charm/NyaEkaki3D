# Spring再生controller

`SpringPreviewController` はUnity非依存の一時再生状態を所有する。作品文書、保存、Undoを変更しない。固定skeleton/chainに対し、State、対応するcenter frame、表示Pose、未消化時間、完了step数、再生状態を保持する。

## 操作

- `Play` / `Pause`: 積分の開始・停止。停止中の経過時間は蓄積しない。停止前の端数時間は保持する。
- `Advance`: 現在のbase pose、avatar座標collider、全simulated boneのcenter変換を受け取り、1/60秒ずつ計算する。
- `Reset`: 現在base poseの初期履歴を作り、時計とstep数をゼロにして停止する。初期姿勢のcollider貫通解消は最初のStep側で行う。

center変更は最初に両履歴へ一度だけ適用し、そのframeのsubstepで共用する。substepには常に入力base poseを使い、前回の揺れた出力poseをbaseとして再投入しない。

step数ゼロでも描画用のdt=0評価を行うが、centerの移動を適用した履歴をcommitしない。正のstepがすべて成功した後でStateとCentersを一括更新する。途中で入力検査・衝突制約などが失敗した場合、時計・State・Centers・表示Pose・step数を変えず例外を返す。Resetの失敗も同様に旧状態を保持する。

## 時計の契約

`SpringFixedClock` に時計処理を分離。1回の経過時間は0〜0.25秒、最大16step。上限超過を無言で切り捨てず拒否する。float入力の累積誤差を吸収する境界許容差は1e-7秒。基準時刻はdoubleで保持する。

各substepの外部pose/center/colliderは呼出し時点のsnapshotを使う。時間内の入力補間や衝突の連続検出は実装していない。描画フレームの分割に依存しない検証は入力が同一の場合のもの。

## 検証と残件

- VRM参考式の同じ入力で0.2秒を一括/20分割し、12stepと出力pose/historyが一致。
- 停止中のcenter移動を3回描画しても履歴・時計・center frameをcommitせず、再開時に一度だけ移して基準Stepと一致。
- 包囲colliderによる失敗、不正経過時間、Resetのcenter不足で旧状態を保持し、有効入力による再試行が成功。
- 境界誤差によるstep不足を検出して修正し、Core351件合格。

VRM sessionからcenter/colliderを毎frame生成する所有者、graph評価のbase pose取得とpreview表示、GUIの再生/停止/リセット、設定変更時のreset、VRM0展開、実モデル受入は次の工程。汎用controllerの完成をI03-B/C全体の完了にはしない。
