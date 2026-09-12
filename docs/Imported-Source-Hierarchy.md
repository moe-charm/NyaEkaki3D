# 元node階層の保持

`ImportedSourceHierarchy`はskin jointに限定せず、元GLBの全nodeをsource index順に保持する。`Parents`はrootを-1で表し、`Children`は元のchildren配列順を保持する。`Origins`はtranslation-only profileで合成したsource rest座標であり、inverse-bind由来のbone Headとは別。

件数は1〜4096。範囲外・自己参照・循環・親と子の不一致・重複/欠落する子・非有限原点を拒否する。入力配列はコピーし、外部から変更できない。親検査とtranslation合成は反復処理で、深い入力木を再帰で辿らない。skinの最大256jointという別の制限は維持する。

rig session writerは完全source skinなしでv3、ありでv4。`hierarchy`はnullまたはsource index順の `{parent, children, origin}` 配列。readerはv1〜v4を読み、旧版の不明な階層はnullのまま再保存する。旧joint mappingや原点から元階層を推測しない。階層がある場合、既存joint原点との一致を検査する。v4の完全基底・bind payloadは [source affine契約](Source-Affine.md)を参照。プロジェクトmanifest形式は変更しない。

このデータ保持によって通常nodeが編集可能な骨になったり、VRM0が再生可能になるわけではない。VRM0のroot展開・末端生成・通常nodeへのpose適用はT02で別途実装する。一般回転/scaleの取込もI04の範囲に残す。

Core検証: 通常node末端と分岐の原点、children順序、native保存/Open、v1/v2移行、joint原点不一致、循環/不正親/件数上限、4096段の計算と入力変更からの独立性。WindowsのVRM0/1取込Save/Open検証にも全node階層の保持を追加。最新の実行結果はcurrent_taskを参照。
