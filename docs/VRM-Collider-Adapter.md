# VRM collider座標adapter

`VrmSpringColliderAdapter` はSpring sessionと保存されたrig対応、現在のgraph/poseを受け取り、Core solverが使うavatar座標のsphere/capsule一覧を作る。再生GUIやchain設定の変換とは独立したモジュール。呼出しごとの不変snapshotであり、元sessionやposeは変更しない。

## 座標契約

現在のGLB importerは元のglTF座標を保持し、source nodeは一般affine（TRS/matrix）を保持する。VRM1のoffset/tailはそのnode-local座標として使う。VRM0の拡張offsetは `(x,y,-z)` に変換してから使う。

根拠: UniVRMの [VRMExporter](https://github.com/vrm-c/UniVRM/blob/master/Packages/VRM/Runtime/IO/VRMExporter.cs) は標準VRM0出力の反転軸をZと定義し、[VRMSpringUtility](https://github.com/vrm-c/UniVRM/blob/master/Packages/VRM/Runtime/SpringBone/VRMSpringUtility.cs) は拡張offsetを変換せず保存する。2026-09-12確認。この2点から、元glTF座標を保持する本importerではoffsetのZ反転が必要と判断した。移行用の非標準ReverseX出力を自動判別する仕組みはない。

source-localから現在poseへの変換は `ImportedNodeSpace.TransformPoint` を共用する。元node原点とinverse-bind由来のbone Headの差を保持し、Unity viewport向けの軸反転はここで重ねない。

## 半径と検査

- `PoseUniformScale` が3基底の長さの一致と直交性を検査。二乗長の最大値に対する許容差は1e-5。鏡映を含む一様scaleは正の半径倍率にする。
- 非一様scale/shearは球・カプセルを同じ形状で正確に表せないため `IMPORT_COLLIDER_SCALE_UNSUPPORTED`。最大軸による近似や無言のclampはしない。
- group順、各collider順、同じnodeの重複を保持。Coreのgroupあたり64件・半径0〜10の予算を適用し、範囲外は拒否する。
- source hash、graph/skeleton対応、元node原点、shape/offset/tailの既知性を検査する。非joint nodeは `IMPORT_BONE_UNMAPPED`。不明値をゼロやsphereに置換しない。

## 検証と残件

CoreのVRM0/1ケースでsession codec往復後、元node原点とbone Headが異なる形状に移動・90度回転・2倍scaleを適用し、解析値と比較。capsule tail、重複node、鏡映、非一様scale/shear、不明shape、未対応node、source不一致を確認した。新規2ケースを含む340件合格。

これはcollider snapshot生成までの検証。center慣性空間、重力と時間の設定変換、VRM0 rootからのchain展開、再生/停止GUI、一般source node変換、実アバターの衝突見た目は未完了。証拠と次のタスクは `current_task.md` を参照。
