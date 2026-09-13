# 9855d43レビューの再照合（2026-09-14）

外部レビューで挙がったP1 3件／P2 5件を、改名後の現行`main`（`0a52e45`）へ再照合した。レビュー対象の`9855d43`は現行mainより前の基準で、今回の確認では本番コードの追加修正は不要だった。

| 指摘 | 現行の扱い | 根拠 |
|---|---|---|
| avatar移動後の衣装配置 | avatar-localの衣装座標を保持し、receiver rootへ一度だけ親子付け | `e250372`、Bridgeの移動・回転・scale fixture |
| `SaveBindings`で旧衣装の管理参照を失う | Bone割当の再利用と生成objectの所有参照をObjectIdで分離 | `23a2b8a`、同一ObjectIdのStateHash更新回帰 |
| UV1を受け付けるのに頂点データを落とす | Windows v1ではUV0のみ。TEXCOORD_1を`UNSUPPORTED_UV_SET`で取込・出力とも停止 | `7ea1cc5`、Core import/export回帰 |
| sparse material slotの入替 | 使用slotと入力portを保持して派生・出力する | `7724628`、Core sparse slot回帰 |
| MR画像がscalar係数を無視 | shaderで画像値へmetallic／roughness係数を適用 | `7ed8d15`、Player/Bridge semantic map回帰 |
| Cuffの面向き | primitive windingを修正し、片面材質のCore回帰を保持 | `PolygonPrimitives.Cuff`、Cuff topology回帰 |
| 共有画像＋異なるsamplerの統合 | image bytesとsampler hashを分離してtextureを共有しない | `46a0738`、sampler variants回帰 |
| 適用前・削除後の割当読込 | assignment identityと生成object存在を分離 | `NyaForgeSkinnedClothingBinding`、Bridge ownership回帰 |

## 再実行した証拠

- `dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore`: **499 passed / 0 failed**。artifact: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b47755e58fa5483999b956e2f5f39314`
- `Tools/Test-NyaForgeRealClothing.ps1 -ModelPath C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm -BuildName ClothingPackageV2`: Player **93 checks PASS**（Unity 6000.4.3f1、RTX 4090）。report: `Artifacts/Authoring-20260914-044139-265f4f8035bf42cb8556c1ff978e7a99/report.json`
- 同じ成果物から出力した`skinned-clothing.nyaforge.json`をUnity Bridgeへ適用: **16 checks PASS**（Unity 2022.3.22f1）。report: `Artifacts/BridgeReceiver-20260914-044430-218-36d67848e7d049dda79b3de3b62daefb/bridge-report.json`

この証拠はCore、Windows Player、合成Bridge、private実RadDollV3の取込・保存・出力経路を分けて示す。実EditorWindowのマウス／DPI、実RadDollV3へ新規衣装を全周fitした貫通・見た目、VRChat Build & Test／実機表示は未受入で、今回のPASSへ読み替えない。

