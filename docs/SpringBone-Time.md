# SpringBoneの時間と停止

更新: 2026-09-12。R08のCore契約。時間積分は`SpringTimeIntegration`、階層は`SpringPoseHierarchy`、接触位置は`SpringConstraintSolver`へ分離する。

## 正の時間幅

`Step`は0〜0.25秒の有限値を受け取る。`SpringBoneState.PreviousDeltaTime`は最後に積分した正の時間幅を保持し、初期状態は0である。これはruntime履歴であり、VRM設定sessionへ保存しない。

前後のtail差分を前回時間幅で割って速度として扱い、今回時間幅を掛けて慣性変位を予測する。初期状態は静止として扱う。重力変位は、不等間隔Verletの `a * dt * (dt + previousDt) / 2`。初回はpreviousDt=0で、静止からの半加速度項になる。

DragForceは「1/60秒あたりの速度損失率」と定義し、時間dtでの残存率を `(1-dragForce)^(60*dt)` とする。stiffnessの位置回復率は `1-exp(-stiffness*dt)`。従来の呼出し1回あたりの減衰から意味が変わるため、今後のVRM adapterは値を無検証で渡さず、previewの設定対応を確認する必要がある。

固定step accumulatorやsubstepはこのモジュールには持たない。可変時間の離散近似であり、異なる時間分割で完全に同じ軌跡となる保証はない。接触や強い力を含む大きな時間幅では精度が落ちる。入力上限は精度保証ではない。

## dt=0

既存stateを渡したdt=0の呼出しは、同じstateインスタンスを返す。current/previous tailとPreviousDeltaTimeを変更せず、慣性・重力・減衰を積分しない。停止中の再描画回数が再開時の速度へ影響しない。

表示poseは、現在のbase poseのheadと保持したtailを使い、階層・長さ・衝突を再評価する。base poseを編集すれば表示は変わるが、物理履歴は進めない。この場合、表示tailは再投影された位置、state tailは最後の物理位置なので、両者の一致を要求しない。未編集時も再投影によるfloat丸めはあり、pose hashの完全一致ではなく位置の許容差で検証する。

再開時は保持していた履歴と最後の正の時間幅から積分する。base poseを変更せず停止だけを挟んだ場合は、停止を挟まない実行と同じ結果になる。停止中の大幅なbase pose移動は再開時に制約へ反映されるため、瞬間移動で慣性を残したくない呼出し側は`CreateInitialState`で明示的にリセットする。

state=nullのStepは既存の初期化契約を維持し、その呼出しでは積分しない。エラー時は途中状態を公開しない。

## 検証

- 動いたstateへのdt=0反復で、同一state・tail履歴・前回時間幅を保持し、再開結果が停止なしの実行と一致する。
- 停止中のbase pose編集が表示headへ反映され、履歴を維持する。
- 自由な予測で、一定重力と不等時間幅の解析値、時間幅が1/4になったときの慣性変位を比較。
- 減衰の時間分割で最終速度の残存率を比較。
- 同じ0.6秒について、0.01秒固定と0.005/0.015秒交互のStepを比較。gravity=1のfixtureでtail差0.002未満を確認。これは任意の物理条件の誤差上限ではない。

GUI preview接続、VRM設定との意味の対応、実アバターの揺れ具合は別途受入が必要。
