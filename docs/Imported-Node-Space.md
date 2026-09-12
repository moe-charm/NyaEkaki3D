# 元node座標と骨格座標の変換

更新: 2026-09-12。I03の座標変換基盤。対象は現行GLB skin importerのtranslation-only profileである。

## 保存する原点

`GlbSkinImporter`は親のtranslationを合成したglTF nodeのworld原点を計算している。これをskin jointごとの`SourceNodeOrigins`として取込結果に保持する。inverse bind matrixから骨のHeadを求める処理とは分ける。両者が異なる入力もあり、骨のHeadだけから元node原点を復元できるとは限らない。

`ImportedRigSession` version 2はこの対応表を`origins:[{node,position:[x,y,z]}]`に保存する。bone mappingと同じnode集合を必須とし、重複・不足・未知node・非有限座標を拒否する。node番号順に決定的に書き出す。

version 1の読込は従来のbone mappingを保持し、SourceNodeOrigins=nullとする。version 2へ再書出ししてもnullのまま。旧作品の骨対応やhumanoid参照は利用できるが、元nodeの座標変換には再取込が必要であり、GUIにも表示する。project schema 4とattachment名は変更しない。version 1のみ対応の旧NyaForgeは新しいrig sessionを開けない。

## 変換

`ImportedNodeSpace`はrig sessionと対象graphを検査し、その骨格のposeを受け取る。source nodeのローカル点pは、次の順で変換する。

1. source-rest点 = 保存したnode原点 + p
2. bone-local点 = source-rest点 − bone.Head
3. 表示座標点 = 現在のBonePose.TransformPoint(bone-local点)

これにより、元node原点とinverse-bind Headの差を保ったまま、現在の骨の移動・回転へ追従する。pをそのままBonePoseへ渡す方法ではこの差が欠落する。

sessionが別graph/骨格を指す場合、元node原点がない場合、skinにないnodeを指定した場合は診断して拒否する。元node座標は変形後の位置ではなく、取込時の基準値であり、骨格編集で適用前のhash検査を通らなくなる。

## 限界と検証

これはsource-local点を現在のposeへ変換する汎用の基盤であり、VRM0のleft-handed offsetを直接入力する処理ではない。VRMの座標表現をsource-localへ変換するadapter、collider半径のscale、center空間と重力方向、非joint node、一般node回転/scaleは引き続き必要。

Coreはnode原点=(1,0.3,0)、inverse-bind Head=(0,0.1,0)の合成データをnative保存・Openし、移動＋90度回転後の点を解析値と比較する。旧v1→v2で不明を保持し、欠落originと未対応nodeを拒否する。

Windowsの同一VRM0/1取込→Save/Open fixtureも元node Y=0.3、Head Y=0.1へ変更し、local offset Y=0.2が表示Y=0.5になることを確認する。実アバターやpreview表示の完成を示すものではない。
