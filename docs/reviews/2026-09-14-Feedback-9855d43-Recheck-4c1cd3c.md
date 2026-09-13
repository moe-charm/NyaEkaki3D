# 9855d43フィードバックの現行HEAD再照合（2026-09-14）

提示された`9855d43`基準の衣装受け取り・材質レビューを、改名後の現行`main` **`4c1cd3c5ed35f57b450a92d278606ad1b71d74aa`**へ再照合した。P1 3件とP2 5件は後続実装と回帰で閉じており、本番コードの重複修正は行っていない。

| 指摘 | 現行の扱い | 根拠 |
|---|---|---|
| avatar移動後の衣装配置 | 衣装をavatar-root localで保持し、receiver rootへ一度だけ親子付け。`worldToLocalMatrix`の二重適用をしない | `UnityBridge/Editor/SkinnedClothingReceiver.cs`、移動・回転・scale付きBridge回帰 |
| 更新時に旧衣装の管理参照を失う | assignment identity（ObjectId）と生成object参照を分離し、StateHash更新でも参照を保持 | `UnityBridge/Editor/SkinnedClothingPackageWindow.cs`、ownership更新回帰 |
| UV1を受け付けて頂点データを落とす | Windows v1はUV0のみ。semantic textureのUV1は取込・GUI・GLB出力で`UNSUPPORTED_UV_SET`として明示停止 | `Assets/NyaForge/Authoring/Import/GlbMaterialSource.cs`、`GlbExportService.cs`、Core／Player回帰 |
| sparse material slotの入替 | 使用slotと入力portを保持して派生・出力 | `AccessorySkinBindingAdapter`、material slot回帰 |
| MR画像がscalar係数を無視 | Player／Bridgeで画像値へmetallic／roughness係数を適用してsmoothnessへ変換 | `AuthoringPbr.cginc`、semantic map GPU／Bridge回帰 |
| Cuffの面向き | primitive windingを修正し、片面材質の法線方向を回帰 | `PolygonPrimitives.Cuff`、Core topology回帰 |
| 共有画像＋異なるsamplerの統合 | image bytesの共有とsampler hashを分離し、texture variantを保持 | `GlbExportService.MaterialRegistry`、sampler回帰 |
| 適用前・削除後の割当読込 | assignment identityと生成物の存在判定を分離 | `NyaForgeSkinnedClothingBinding`、Bridge ownership回帰 |

## 自動・実Unity確認

- Core: **505 passed / 0 failed**。`dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj`、artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-c2301ac2fe9d4cd99a9bf34dd6ab5f2f`。
- private Unity probe: **passed**。Unity `2022.3.22f1`、`private/PhysBonesSdkProbe-20260914`、report `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealAvatar-e341c4481e0c4480bde9a31c8706573e/report.json`。
- 実RadDollV3の`Assets/Avatar/RaddollV3.fbx`で20 renderer／279 transformsを取込。実`VRCPhysBone`を構成し、Body 8,467頂点・10,770三角形の表面fit／weight移行、衣装136頂点のpackage適用、再適用、Scene Save/Open後の所有参照を確認した。

この確認は、実アセットを使った取込・fit候補・受け取り・Unityシーン永続化の証拠である。実EditorWindowのマウス／IME／Explorer操作、衣装全周の貫通ゼロと見た目、VRChat Build & Test／実機表示は未受入として残す。Workbenchのfit／weightは選択面・選択頂点・最大距離を渡すbounded overloadへ接続済みで、実マウス操作での最終確認を別カードにする。
