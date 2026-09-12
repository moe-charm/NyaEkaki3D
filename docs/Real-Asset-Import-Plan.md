# 実素材の取込調査（T03）

この文書は観測記録。採用仕様・モジュール責務・I04の完了条件は [モデル交換仕様](Model-Interchange-Spec.md) を参照。

2026-09-12、T03として手元のアバターFBXとチョーカー/カフスFBXの3ファイルをBlender 4.4.0で再検査した。`Tools/Inspect-BlenderImport.py`はバックグラウンドで読み取り、JSON報告だけを書き、モデルを保存/書出ししない。詳細なpath・object/bone/shape名・行列・source hashは追跡除外の`private/import-inspection/20260912-inventory.json`に保持する。検査後に全sourceのSHA256一致を再確認した。

## 現行実装と実入力の差

| 項目 | 実入力で確認した要求 | 現行制作import / Core |
|---|---|---|
| ファイル | 3件ともFBX。今回調べた素材フォルダでは実素材GLB/VRMなし | GLB2/VRM、FBX直接取込なし |
| mesh | アバター20、小物各2 | GLB取込は1mesh |
| skeleton | 3件とも257骨 | SkeletonDefinition/pose/関連codecは512骨上限 |
| weights | アバター最大18 deform bone影響/頂点、4超が4,042頂点。小物は最大4/3 | SkinBindingは32、GLBは連続するJOINTS_n/WEIGHTS_n |
| morph | アバター全mesh合計320キー（Basis除く）、単一mesh最大262、名前の集合317 | MorphSet最大512、POSITION/NORMAL/TANGENT対応 |
| transform | アバター20objectで非identity基底。3件とも257骨中255骨のrest matrixが非identity基底 | nodeとinverse-bindのrotation/scaleは未対応 |

数字はBlenderのFBX読込結果。ウェイト数はArmature modifierが参照するdeform boneに一致する頂点groupを対象に数えた。最終GLBのjointセットと同一とは未確認。shapeキーもmeshごとの意味を持ち、同じ名前だから無条件に統合できない。Blender importerの結果を元VRChat環境の挙動やGLB変換後の一致とは扱わない。骨数だけ増やす、meshを結合する、上位4weightに切るだけでは対応完了にならない。

## 調査から決めた次の作業

T03は読取調査として完了し、実モデルの取込成功・見た目受入は未完了。次はI04-Aのsource affine基盤から進める。複数mesh、容量、FBX経路、未対応情報のreportを含む全体の実装順は [モデル交換仕様 第8節](Model-Interchange-Spec.md#8-現在地点と実装順) に一本化する。

初回の作業案ではBlender FBX→GLBを必須工程のように記したが、設計v2の「Blenderなしの制作完結」に合わせ、任意の開発検査/互換adapterとして扱う。標準FBX経路はUnity Bridgeを基準に開発し、runtime直接importerは別途選定する。いずれも変換成功だけで情報保持・受取側受入にしない。

検査の実行例（Blender実行ファイルはローカル環境で検出する）:

```text
blender --background --factory-startup --disable-autoexec --python-exit-code 1 --python Tools/Inspect-BlenderImport.py -- --output private/import-inspection/inventory.json <input.fbx> [input.blend ...]
```

実検査はBlender 4.4.0で3入力とも成功。最終報告のsource pathとhashは再確認済み。調査中に小物の検証用pathがなくなったためexportsの同名ファイルを照合し、同じSHA256であることを確認して報告の入力先を更新した。元ファイルへの保存・モデル書出しは実施していない。
