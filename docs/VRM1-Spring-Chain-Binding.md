# VRM1 Spring chainの解決

`Vrm1SpringChainResolver` は保存したSpring/rig sessionと現在graphを検査し、`VrmSpringChainBinding` とhead/tailごとの `VrmSpringPairBinding` を返す。物理係数変換、計算状態、表示は所有しない。

joint列 `a,b,c` は `a→b`、`b→c` の2pairとなる。末尾cの設定を回転用jointに追加しない。各pairはhead側の元設定、head/tail BoneId、両方の元node原点を保持する。source原点をinverse-bind由来のbone Headと同一視せず、ここではCoreのoffsetへ変換しない。

## 検査

- SourceHash、GraphId、SkeletonHash、保存した元node原点を検査。現profileで対応していない非joint nodeは未対応として拒否する。
- headはtailの厳密な祖先。途中を飛ばすpairは許可するが、飛ばした骨もchain重複の検査対象にする。
- center指定があれば先頭head自身または祖先でなければならない。他chainのjointまたはその子孫のcenterは拒否する。
- collider group参照を範囲検査する。返却するgroup順は元設定を維持する。
- 2joint未満は再生用pairを作れない旨の診断で拒否。VRM0は専用のroot展開が必要なため、このresolverへ誤投入した場合も拒否する。

設定値は変換しない。例えばstiffness=25を1へclampしない。元の重力方向や不明値もそのまま保持する。この戻り値は実行可能なCore chainそのものではない。

## 検証と次の接続

session codec往復、3joint→2pair、途中jointの省略、元node原点とbone Headの差、stiffness>1の保持、重複、逆祖先、centerの誤り、短いchain、未対応node、source違いをCoreで確認。346件合格。

この段階はVRM1 topologyの解決まで。VRM0 root/末端展開、source原点を回転中心へ反映するCore接続、重力・時間設定、center frameの所有、再生GUI、実素材受入は未完了。I03-B全体は `current_task.md` で未完了を維持する。
