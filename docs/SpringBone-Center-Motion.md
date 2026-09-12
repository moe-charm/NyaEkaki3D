# Spring centerの履歴追従

`SpringCenterFrame` はsimulated BoneIdごとのcenter→avatar変換を不変snapshotとして保持する。`SpringCenterMotion.Transfer` は前回centerと現在centerの差で、Verletの現在tailと前回tailの両方を移す。計算状態はavatar座標のまま、center相対の履歴を維持する。

式: `newCenter.TransformPoint(oldCenter.InverseTransformPoint(historyPoint))`。全履歴点を変換してから新しいstateを返し、入力stateは変更しない。SkeletonHash、ChainHash、PreviousDeltaTimeを保持する。centerが変わらない場合は同じstateを返し、浮動小数点の往復誤差を加えない。

## 責務と呼出し順

1. runtime所有者がchain→center対応を解決し、全simulated boneにcenter変換を割り当てる。center指定なしはavatar基準ならIdentity。別chainのcenterを一つにまとめない。
2. 最後にcommitしたstateとcenter frameから現在centerへTransferする。
3. 現在のbase pose、avatar座標collider、変換したstateをSpringBoneSimulator.Stepへ渡す。
4. 正のdtのStep成功時だけ、stateと対応するcenter frameを一緒に保持する。失敗時は両方を保持しない。停止中に描き直す場合も元の履歴/frameから一時的に計算し、停止を物理履歴の更新にしない。

重力方向はこのモジュールで回さない。[VRMC_springBone公式仕様](https://github.com/vrm-c/vrm-specification/blob/master/specification/VRMC_springBone-1.0/README.md#considering-center-space)のcenter相対慣性とworld基準重力の区別を維持する。avatarとworldの関係を含むVRM adapterは別途必要。

frameはstateのskeleton/chain hashを保持し、全simulated boneの完全な対応、有限・可逆な基底を要求する。不足・余分・別state構成・defaultの特異基底は拒否する。同じhashでもcenter nodeの割当自体を変更する場合は、呼出し側がresetまたは明示移行を選ぶ。

## 検証と未完了事項

- 二つのchainで別centerを使用し、一方だけの移動・90度回転・2倍scaleを履歴2点へ適用。解析値、逆変換、経過時間保持、入力snapshotの独立性を検証。
- centerとbase poseを同じ量だけ移動した後、dt=0と再開を実Stepへ通し、移動しない基準ケースとの相対一致、Pose/Stateのtail一致を検証。
- 不完全・未知bone・特異基底・別chain設定の拒否を検証。Core343件合格。

これはcenter履歴計算まで。runtime所有者、VRM centerの祖先関係検査、VRM1末尾jointをtailのみとして扱うchain展開、VRM0末端処理、重力/時間の設定変換、GUI再生・停止・リセットは未接続。I03-B全体の完了にはしない。
