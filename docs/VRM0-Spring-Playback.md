# VRM0再生の接続

`Vrm0SpringRuntime`はsource subtree展開を一時source骨格の実行chainへ変換する。全target・center・collider nodeを必要集合へ入れ、通常nodeを省略しない。`Vrm0SpringPreview`が固定step controllerを所有し、出力を制作skinのPoseSetへ戻す。Workbenchは`IVrmSpringPreview`を通してVRM0/VRM1を選択する。

## 座標と設定

各実行骨Headはsource原点なので、head offsetは0、tail offsetは展開先端との差。hitRadiusは現在poseの一様scaleを乗せ、stiffnessとdragはVRM参考時間式へ渡す。重力の未保持は拒否する。

VRM0の拡張gravityDirはUnity座標のまま出力されるため、NyaForgeのraw glTF座標へZを反転する。これは既存のVRM0 collider offset変換と同じ規約。根拠: [公式VRMSpringUtility](https://raw.githubusercontent.com/vrm-c/UniVRM/master/Packages/VRM/Runtime/SpringBone/VRMSpringUtility.cs)のExportSecondary/LoadSecondary（2026-09-12確認）と、[既存collider座標契約](VRM-Collider-Adapter.md)。非標準軸変換や可動world rootは対象外。

centerは現在の一時source poseから取得する。colliderは既存の形状・予算・座標検査を共用し、通常nodeの変換も使える。どちらも各Advanceの入力姿勢から更新する。同じstep内で揺れた結果へcolliderを逐次追従させる方式とは異なるため、完全なUniVRM実行一致は保証しない。

## 所有と失敗

Resetは設定・pose・collider・centerをすべて候補として検査してから公開する。Advanceはcontrollerをforkし、全stepとskin姿勢への投影が成功したときだけ採用する。controllerの設定とState/Pose/centerは不変値なので、候補の失敗は元controllerへ影響しない。scale変更はResetを要求する。

Workbenchの文書/attachment変更、編集、作品切替で再生を破棄し、保存は編集中のposeを使う。VRM0の旧rig sessionに階層がなければ再生対象にならず、再取込が必要。一般node回転/scaleの取込、必要集合256骨超、非一様scaleは対応外。元ファイル全体を任意に再生できるという意味ではない。

## 検証と残件

Coreは合成VRM0 reader→session往復→実行所有者を通し、通常nodeのcenter/collider、重力Z変換、12step、停止/再開、外部姿勢更新、時間不正時の旧状態、scale変更拒否とResetを検証する。Windows handler検証は両形式の表示・Mesh再利用・保存/Open・Reset・編集時破棄を対象とする。実行結果はcurrent_taskに記録する。

実素材・実クリック・DPI/文字の欠け・性能測定・可動world rootの検証は未完了。参照runtimeとの比較、とくに揺れるnodeに付いたcolliderの更新順は追加検証の対象。
