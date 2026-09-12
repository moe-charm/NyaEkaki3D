# Spring計算の明示先端

`SpringBoneJointSettings.RestTailOffset` は任意のbone-local rest座標の先端offset。省略時は従来どおり `BoneDefinition.Tail - Head` を使う。指定時は `SpringJointTarget` がこのoffsetを現在poseへ変換し、初期state、長さ制約、回転方向、衝突に同じ先端を使う。骨格や表示用tailを書き換えない。

VRM1のhead/tail pairや、中間nodeを飛ばして先のjointをtailにするchainを受け取るためのCore契約。VRM adapterは元node位置とbone-local rest座標の関係を解決してoffsetを渡す必要がある。設定したtail nodeの追従と、一般の独立した子pose編集は別の問題であり、本APIがtail nodeを毎フレーム参照するわけではない。

明示offsetは有限で長さ二乗が1e-12より大きいことを要求する。chainの計算hashへ指定有無とXYZを含め、hash入力のversionを2へ更新した。先端変更後の旧stateは `SPRING_CHAIN_CHANGED` とする。これは一時的な計算状態の識別変更であり、native projectやVRM sessionの保存形式変更ではない。

検証: 表示用tailとは異なる先端、2倍scale、途中の非simulated骨と末尾joint、球衝突を含めて12回連続Step。長さ、Pose/State一致、末尾jointの追従、貫通しないこと、骨格hash不変、先端変更時の旧state拒否を確認。ゼロ/非有限入力拒否を含めCore345件合格。

VRM1末尾jointを回転対象へ追加しないchain展開、VRM0のroot/末端処理、center参照、重力と時間設定のadapter、再生GUIは未完了。`current_task.md` のI03-B/I03-Cで継続する。

## 元node原点を回転中心にする拡張（2026-09-12）

`RestHeadOffset` はbone-local rest座標の回転中心。未指定はゼロ。`RestTailOffset` と同じ座標系であり、tailはheadからの差分ではない。両点の距離で長さを決め、出力basisを回転した後にtranslationを補正して、変換後のhead位置を保つ。これによりinverse-bind由来bone Headと元node原点が違う場合も中心を取り違えない。

明示/暗黙tailとheadが一致する入力は設定構築またはskeletonを含む入力検査で拒否する。計算用chain hash versionは3となり、headの指定有無と座標も含む。native/sessionの保存schemaは変更しない。前節のversion2はtail追加時の履歴。

2倍scaleの異なる原点を使い、12回連続Stepでhead固定、tailと非simulated子jointの一致、長さを確認。head変更時の旧state拒否、暗黙tailとの一致拒否を追加しCore347件合格。元VRMからhead/tail offsetを生成するadapterと時間/重力契約の接続は次の工程。
