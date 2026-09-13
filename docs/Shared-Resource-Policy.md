# GLB共有リソースの扱い

GLBでは一つの`mesh` resourceを複数nodeが参照でき、一つの`skin` resourceを複数nodeが共有できる。NyaForgeはこの参照関係を取込候補では明示するが、編集時に別graph objectを暗黙共有しない。頂点編集や材質編集が別nodeへ波及しないことを優先する。

## 取込

- `GlbSceneInventory.SharedResources`が、各mesh resourceを参照するnode indexとskin indexを保持する。
- 同じmesh resourceを複数nodeが参照している場合、全mesh instance取込はnodeごとの独立graph objectを作る。GUIには共有数と「取込後は個別編集」を表示する。
- 一つのmesh resourceに複数skin resourceが対応する場合、node instanceを選ばない取込は停止する。skin slotをindexだけで推測しない。
- morph target IDはsource hashとmesh indexを含むため、同名targetが別mesh resourceで衝突しない。
- skinned graphのnative sessionはsource hash、skin index、source skin packageを保持する。static graphのimport diagnosticsはsource hashとmesh indexを保持する。

## 出力

- 同じstable skeleton hashと互換bindを共有するskinned objectは、一つのglTF skinへまとめられる。
- rest定義、inverse-bind、またはsource identityが異なる場合はskin resourceを分ける。異なるsource skeletonをbone名だけで統合しない。
- mesh resourceの共有は編集用graphのaliasを意味しない。出力では評価済みmeshをprimitive/node単位に書き出し、共有による編集波及を発生させない。
- native projectはgraph、編集履歴、stable ID、元のsource locatorを正本とし、標準GLBは交換形式として扱う。標準GLBへnativeの共有参照やattachment metadataを埋め込むことは約束しない。

この方針は「共有を検出して表示する」と「共有を安全に統合する」を分ける。完全なmesh／skin／morph参照の保持、編集後のresource dedup、外部アプリでの共有解釈は、対応profileごとに追加検証が必要である。
