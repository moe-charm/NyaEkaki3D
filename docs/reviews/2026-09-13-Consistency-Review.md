# 2026-09-13 consistency review receipt

提示されたレビュー（基準: `1c76e4a`）を現行 `main` と照合した。4件のP1は後続コミットで修正済みであり、同じ不具合を未対応として再実装しない。以下は現行コードと回帰の対応表である。

| 指摘 | 現行対応 | 回帰・境界 |
| --- | --- | --- |
| 16bit `JOINTS_n` の2バイト幅誤読 | `GlbSourceSkinImporter` が `row[i * 2] \| row[i * 2 + 1] << 8` で読取る | 8bit/16bit同値、全dense set、負weight拒否をCoreで確認。sparse weightは未対応 |
| source skin後の下流編集消失 | `SourceSkinGraphAdapter.ApplyToEvaluation` が最終graph outputへsource paletteを適用 | EditMeshを含むsource skin graph回帰をCoreで確認。同一skeleton／poseを共有する複数 `SkinDeform` 同時評価も回帰済み |
| PhysBones packageのsource不足 | target packageへ `secondary-motion.nyaforge.bin` を同梱し、receiverがprofile hashで検証 | package往復・改ざん・source hash不一致をCore/Bridgeで確認。実SDK受入は未実施 |
| skin付きGLBと同居する静的小物の拒否 | `GlbImporter.MeshHasSkin(root, meshIndex)` が選択meshだけを判定 | 同居fixtureで小物取込とskin mesh拒否をCoreで確認。複数mesh結合・共有参照は未対応 |

## 検証

- Core: **441 passed / 0 failed** (`dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore`)
- 最新artifact: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-95fd651be1094560b029f04b72aa1d7c`
- 実RadDollV3はprivate素材としてのみ取込 smoke に使用し、public repositoryへ同梱していない。

このレビューで残る実装対象は、I04-A/Bの複数mesh・共有mesh/skin/morph参照、I04-Eの材質・未知拡張の完全保持、SIM-02B/SIM-07Aの実SDK/実VRChat受入である。I04-Eの入口として、GLB importerは材質・animation・extensionsRequired/Usedのコード付きdiagnosticsを返し、blocking/partialを区別する回帰を追加した。今回、選択primitiveのmaterial slotと基本PBR係数（baseColorFactorのlinear化、metallic/roughness、emissive、alpha）をnative `StandardMaterial`／`AssignMaterials`へ接続した。画像・sampler・追加拡張は引き続き未保持としてdiagnosticを返す。容量拡張としてnativeは512骨／32 influence／512 morph、GLB取込と拡張GLB出力は全JOINTS_n/WEIGHTS_n setへ対応した。標準SkinnedGeometry出力は受取先互換のため4 influenceを明示拒否する。Coreや合成Bridgeの合格を、実SDK・実VRChatでの受入完了とは扱わない。

複数objectのinspection一覧（activeObjectId、graphId、評価状態、output要約、diagnostics）はCore回帰とPlayer compileで確認済み。GLB取込diagnosticsは`import-diagnostics.nyaforge.json`へschema 4で保存し、再Open後のinspectionと取込後statusへ復元する経路もCoreで確認済み。GUIの詳細report表示はPlayer自動検証まで実装済みで、材質・animation・未知拡張の完全保持は残件。

Windows Playerの再ビルドとAuthoring suiteもPASS（`Logs/build-player-20260913-032941-070.log`, `Builds/ImportDiagnosticsV2/NyaForge.exe`, `Artifacts/Authoring-20260913-032959-ca8fa373b3ef4e56b32c0f6660fe4dcf/report.json`）。
GUI詳細reportの検証もPASS（`Logs/build-player-20260913-033512-860.log`, `Builds/ImportDiagnosticsUiV2/NyaForge.exe`, `Artifacts/Authoring-20260913-033534-e6ed3ced2eb04b8896208cde5f0f05ab/report.json`）。
基本PBR材質ルーティング後の再検証もPASS（Core **442 passed / 0 failed**、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-85bc5a3302b94ac8960ee30a095b48da`、`Logs/build-player-20260913-035134-502.log`、`Builds/ImportedMaterialV2/NyaForge.exe`、`Artifacts/Authoring-20260913-035152-40bbaef3c91a4adfbe22ca513f00e432/report.json`）。埋め込みbase color画像の抽出・unbound Paint評価追加後もCore **444 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-da747d4828e944c1acb4f7874925853e`）、Windows Player `Builds/EmbeddedMaterialImageV2/NyaForge.exe`（`Logs/build-player-20260913-040537-250.log`）と Authoring suite（`Artifacts/Authoring-20260913-040557-b83542e77e2c46c2869518a629e4bff7/report.json`, 74 checks）がPASSした。
