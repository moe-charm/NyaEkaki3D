# 取込元nodeと骨IDの対応

更新: 2026-09-12。GLB skin取込とVRM humanoidをつなぐCore adapter。

## 責務

`GlbSkinImporter`が骨格を作る際に使ったnode→BoneId辞書を、取込結果の`BoneMap`へ渡す。`ImportedBoneMap`はsource hash、skeleton hash、不変の対応表を保持する。source hashから骨IDを再計算するコードを別adapterへ複製しない。

glTF node番号とskinのjoint slotは異なる。たとえば`skin.joints=[1,2]`のslot 0はnode 1を指す。骨名も一意とは限らない。この対応表は名前・配列順による推測を使わず、取込時の実際の割当を保持する。

対応表は作成時の全骨を一対一で含む。skinにないnodeは含まない。`Resolve`は未対応nodeを`IMPORT_BONE_UNMAPPED`として拒否する。`ValidateFor`は異なる元ファイルを`IMPORT_SOURCE_CHANGED`、骨格revisionの相違を`IMPORT_SKELETON_CHANGED`で拒否する。

`VrmHumanoidBinding.Create`は、VRM0/1のhumanoid semantic→node inventoryを、同じ取込のsemantic→BoneIdへ変換する。sourceとskeletonを検査してから全参照を解決し、部分的に解決した結果を公開しない。完全なVRM humanoid必須骨セットの検査やretargetingを提供するものではない。

## 保存と未対応範囲

後続の [ImportedRigSession](Imported-Rig-Sessions.md) がnode/humanoid対応をgraphと同じsnapshotへ保存する。Open後にsource/skeleton hashを検査してBoneMapを復元する。骨格編集でstaleになった対応表は自動更新せず、GUIへ表示する。

既存のNYRS骨格形式と骨ID生成方法は変更していない。一般node回転・scale、複数skin、skin外のSpring node、sphere/capsule詳細値・gravityDirの保持、runtime preview接続も別段階である。対応表だけを追加して実VRM全体を読めるようになったとは扱わない。

## 検証

合成VRM0/1にmesh nodeを先頭追加し、skin slotとnode番号をずらした。2本の骨を同名にしてもhips/spineを正しく対応づけ、skin weightがそのIDを参照することを確認した。同じbytesの再取込・骨格codec往復でIDを保持し、別source・編集後骨格・skin外nodeは拒否する。
