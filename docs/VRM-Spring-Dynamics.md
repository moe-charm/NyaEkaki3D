# VRM1の実行用chainと時間計算

`Vrm1SpringRuntimeAdapter.CreateChains` は保存済みSpring/rig session、graph、poseからCoreの `SpringBoneChain` を作る。topologyは `Vrm1SpringChainResolver` に任せ、元nodeのhead/tail原点とbone Headとの差を明示offsetとして保持する。元VRM1の設定を専用の `VrmReference` 計算modeへ渡す。

## 二つの計算mode

- `Authoring`: 既存の可変dt対応Verlet、経過時間に応じた減衰・指数復元、加速度項。既存の係数範囲・重力方向の正規化を維持する。
- `VrmReference`: [公式仕様の参考アルゴリズム](https://github.com/vrm-c/vrm-specification/blob/master/specification/VRMC_springBone-1.0/README.md#inertia-calculation)に合わせ、慣性 `(current - previous) * (1 - drag)`、復元 `unit(restDirection) * stiffness * dt`、重力 `gravityDirection * gravityPower * dt` を現在tailへ加える。この節は仕様上non-normativeであり、UniVRMと全条件で同一の動作を保証するものではない。

VRM modeでは有限・非負のstiffnessをそのまま保持し、1へclampしない。重力方向も元ベクトルを保持する。半径・gravityPower・dragなどの既存Core予算は維持し、超過は明示拒否。計算結果が非有限ならstateを公開する前に拒否する。衝突は既存の同時制約solverで、参考実装の逐次押し出しと同一ではない。

VRMのdragは1step単位なのでフレーム分割へ不変ではない。再生所有者は固定step（初期想定1/60秒）と未消化時間を管理する必要がある。dt=0の停止契約は引き続き共通Stepが守る。centerの移動で重力を回す処理は加えていない。現adapterはavatar座標を固定worldとして扱い、移動するavatar root/worldの統合は今後のruntimeで明示する。

半径scaleと復元方向を正しく扱うため、chain作成時のposeは一様scale・直交基底に限る。動作中のscale変更ではchainと半径の再構成・state resetを所有者が判断する。現関数はcenter frameやcolliderを自動で更新しない。

計算hash version4はmodeを含む。異なるmodeの旧stateを誤って使わない。native/sessionの保存schema変更はない。

## 検証

Core348件合格。VRM参考式を各力の解析値と比較し、stiffness25・非単位重力ベクトルの保持を確認。保存session→head/tail pair→実行chain→12回Stepで、元node原点の回転中心固定とPose/State先端一致を確認した。

VRM0 root展開、center/固定step/失敗時の状態を所有する再生controller、GUI再生・停止・リセット、実モデル受入は未完了。I03-B全体の完了にはしない。
