# VRM Spring詳細とsession version 3

更新: 2026-09-12。取込時の重力方向とcollider詳細を保持する。シミュレーションへの適用は別adapterで行う。

## 保持するデータ

- joint: node index、hitRadius、stiffness、gravityPower、dragForce、nullable GravityDirection。
- collider group: 従来のnode列と同じ順序・件数のnullable Shapesリスト。各shapeはkind（sphere/capsule）、nullable Offset、radius、nullable Tailを保持する。sphereのTailは常にnull。
- `HasCompleteDetails`は全jointの重力方向、全colliderのshape/offset、capsuleのtailが揃うかを表す。runtime対応やVRM全体の完全性の判定ではない。

`VrmSpringColliderShape`は不変の値を所有し、`VrmSpringDetailJson`はvector/shapeのJSON検査を共有する。値は正規化せず、元の座標系で保持する。VRM0のleft-handed offsetとVRM1/glTFの座標を勝手に混ぜず、Formatを見て後段adapterが変換する。

VRM1は[公式joint schema](https://raw.githubusercontent.com/vrm-c/vrm-specification/master/specification/VRMC_springBone-1.0/schema/VRMC_springBone.joint.schema.json)に従って、省略gravityDir=(0,-1,0)を適用する。[公式shape schema](https://raw.githubusercontent.com/vrm-c/vrm-specification/master/specification/VRMC_springBone-1.0/schema/VRMC_springBone.shape.schema.json)の省略offset/tail=(0,0,0)、radius=0も保持する。明示null、短いvector、文字列成分、非有限値、負radiusは拒否する。

VRM0の[Spring schema](https://raw.githubusercontent.com/vrm-c/vrm-specification/master/specification/0.0/schema/vrm.secondaryanimation.spring.schema.json)・[collider schema](https://raw.githubusercontent.com/vrm-c/vrm-specification/master/specification/0.0/schema/vrm.secondaryanimation.collidergroup.schema.json)には同じ省略値の指定がないため、gravityDir/offset自体がない場合は不明として保持する。vectorオブジェクトを指定する場合はx/y/zを揃える対応profileとし、部分指定は黙って補完・破棄せず拒否する。legacy radiusの必須検査は従来どおり。

## 保存と移行

Spring session writerはversion 3、readerはversion 1/2/3に対応する。expression sessionはversion 2のまま、project snapshotはschema 4のまま。

version 3は各jointに`gravityDirection`（3数値配列またはnull）、各collider groupに`shapes`（shape配列またはnull）を加える。shapeは`kind`、`offset`、`radius`、`tail`の4fieldを持ち、個々のvectorは3数値配列またはnull。未知field・件数不一致・sphereへのtail指定は拒否する。

旧version 1/2は重力方向とshape詳細を保存していなかった。読込時にGravityDirection/Shapesをnullにし、version 3へ再書出ししても不明のまま保持する。下向き重力や半径0を推測して復元しない。古いsnapshotはOpenだけでblobを書換えず、再取込またはsessionの再書出しで新形式になる。古いNyaForgeはversion 3を開けない。

Workbenchには詳細不足を表示する。再取込でも元ファイルのlegacy vectorが省略されている場合は不明のままなので、元設定の確認も必要である。

## 検証と残件

CoreでVRM0/1の非default重力方向、sphere offset/radius、capsule offset/tail、規定値、session往復を検証した。旧version 1/2→3で不明値を維持し、型・vector長・負radius・shape件数の不正を拒否する。

Windowsの同一VRM取込→Save→空workspace→Open検証にも詳細値のassertを追加した。旧version 2 sessionの保存失敗・再試行検証では、不明なshapeが補完されていないことを確認する。

骨対応の永続化とcapsule衝突Coreは後続実装済み。node/centerの座標変換、VRM時間パラメータとの対応、GUI previewは未完了。この変更は入力保存の精度を改善するものであり、実アバター物理の完成ではない。
