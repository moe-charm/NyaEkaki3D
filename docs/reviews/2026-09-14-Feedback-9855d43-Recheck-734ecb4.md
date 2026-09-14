# 9855d43フィードバックの現行HEAD再照合（2026-09-14）

提示された`9855d43`基準の衣装受け取り・材質レビューを、改名後の現行`main` **`734ecb4`**へ再照合した。P1 3件とP2 5件は、`9855d43`以降の実装と回帰で解消済みであり、今回のフィードバックを理由に本番コードへ重複修正は加えていない。

| 指摘 | 現行の扱い | 根拠 |
|---|---|---|
| avatar移動後の衣装配置 | 衣装はavatar-root localで生成し、受け取りrootへ一度だけ親子付けする。bindposeも同じ座標系で作る | `UnityBridge/Editor/SkinnedClothingReceiver.cs`、移動・回転・scale付きBridge回帰 |
| 更新時に旧衣装の管理参照を失う | assignment identity（ObjectId）と生成object参照を分離し、StateHash更新でも所有参照を保持する | `UnityBridge/Editor/SkinnedClothingPackageWindow.cs`、ownership更新回帰 |
| UV1を受け付けて頂点データを落とす | Windows v1はUV0のみ。semantic textureのUV1は取込・出力で`UNSUPPORTED_UV_SET`として明示停止する | `GlbMaterialSource.cs`、`GlbExportService.cs`、Core回帰 |
| sparse material slotの入替 | authored slotとdense submeshの対応を派生・出力まで保持する | `AccessorySkinMaterializer`、material slot回帰 |
| MR画像がscalar係数を無視 | Player shader／Unity Bridgeで画像値へmetallic／roughness係数を適用する | `AuthoringPbr.cginc`、semantic texture回帰 |
| Cuffの面向き | primitive windingと指定normalを一致させ、片面材質の法線方向を回帰する | `PolygonPrimitives.Cuff`、Core topology回帰 |
| 共有画像＋異なるsamplerの統合 | image共有とsampler variant共有を分け、Repeat／Clampを別textureとして出力する | `GlbExportService`、sampler variant回帰 |
| 適用前・削除後の割当読込 | assignmentの同一性と生成objectの存在判定を分離する | `NyaForgeSkinnedClothingBinding`、Bridge ownership回帰 |

## 検証

- Core: **506 passed / 0 failed**。Cuffの幾何winding／normal一致、GLB材質・衣装ownership・semantic texture回帰を含む。artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-d95cc0756d7f4197b67fd9d3ac7f0e7e`。
- private Unity probe: **passed**。Unity `2022.3.22f1`、RadDollV3の取込、実`VRCPhysBone`構成、Body surface fit／weight候補、衣装package適用・再適用・Scene Save/Open後のownershipを確認した。
- Windows Player: **PASS**。`Builds/PerformanceV30/NyaForge.exe`のAuthoring／navigation／foreground性能回帰と、実RadDollV3単一候補の取込・Save/Open・GLB／VRM1 smokeを確認した。

この確認は自動経路とprivate実アバターprobeまでを対象にしたもの。実EditorWindowのマウス／IME／Explorer操作、実アバター衣装の全周fit・貫通ゼロ・見た目、VRChat Build & Test／実機表示は未受入として残る。Workbenchのfit／weightは選択面・選択頂点・最大距離を渡すbounded経路へ接続済みだが、販売品質の判定は手動受入で行う。
