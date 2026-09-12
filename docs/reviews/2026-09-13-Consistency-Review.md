# 2026-09-13 embedded base-color resize recheck

Embedded base-color PNG/JPEG images above the native 1024px Paint limit are now decoded up to a bounded 8192px source dimension and deterministically downsampled while preserving aspect ratio. The resulting pixels are owned by the native project, so Save/Open does not depend on the original VRM/GLB. Corrupt, external, over-8192px or over-16MiB images remain explicit diagnostics. RadDollV3 private smoke retained its six embedded base-color images as native Paint nodes; Authoring **80 checks PASS** (`Artifacts/Authoring-20260913-081327-dc634884b6f0475e9ebbcabff60eff72/report.json`) and Unity **2022.3.22f1** Bridge **PASS** (`Artifacts/BridgeReceiver-20260913-081803-691-55c8fee7b2794de8a139d297b4ca5482/bridge-report.json`). This improves visual authoring coverage but does not claim complete non-base-color texture, animation, VRM-extension, or real VRChat retention.

# 2026-09-13 skinned node affine policy recheck

skinned display and standard Workbench/MCP GLB output now use joint world frames plus retained inverse-bind matrices as the single transform basis. The selected skinned mesh node affine remains audit metadata and is not post-applied after skinning. Core **461 passed / 0 failed** (`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-a7b92fd3fb184402a46aef4f4dfa676c`). Windows Player `Builds/SkinnedAffinePolicyV2/NyaForge.exe` passed the private RadDollV3 command-line smoke with **79 checks** (`Artifacts/Authoring-20260913-080806-87aacf4f39c74048b4c00a8a11c52212/report.json`); Unity **2022.3.22f1** Bridge passed (`Artifacts/BridgeReceiver-20260913-080912-368-fd387a22cef74d5faf30b6e7ab6e88f0/bridge-report.json`). The smoke covered candidate selection, EditMesh vertex edit, native Save/Open, standard skinned GLB output and re-import cardinality. RadDollV3 embedded material images were omitted by the existing size/format budget and reported explicitly; this is not complete material retention or real VRChat acceptance.

# 2026-09-13 attachment selection and GLB material follow-up

装着先Dropdownの選択保持を追加し、Refresh後もユーザーが選んだavatarを維持する。GUI/MCPのskinned GLB出力はgraph objectごとのinverse-bind保持経路を共有する。`baseColorFactor`は線形値として扱い、`metallicFactor`省略値は1へ合わせた。Core **460 passed / 0 failed**、Windows Player Authoring suite、Unity Bridgeで確認済み。

# 2026-09-13 consistency review receipt

## 最新HEAD（`5826247`）再照合

提示レビュー（基準 `1c76e4a`）のP1/P2を現行HEADへ再照合した。16bit `JOINTS_n` の2バイト復号、source skin後の下流編集保持、PhysBones source asset同梱、選択mesh単位のskin判定はいずれも実装済みで、Core **459 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f3e6edffd8e847178fe7b97c4dd740e7`）を確認した。P2のreset cache無効化、stable marker再利用、curve削除初期化、再bind Undo、capture camera metadata、負weight事前拒否も既存回帰を維持している。

同一skeleton hashの複数skinned meshとobjectごとのinstance affineを一つのGLBへ出力する追加実装も含む。実RadDollV3のprivate smokeはWindows PlayerとUnity BridgeでPASSしたが、実VRChat SDK/実アバター/VRChat内動作の受入とは分ける。残件は異なるskeleton結合、共有mesh/skin/morph参照、完全な材質・animation保持、実SDK受け取りである。

## source skin downstream evaluation (`SourceSkinOverrideV1`)

source skin projectionを`SkinDeform`ノード位置のmesh overrideとして再評価する実装へ更新した。最終出力へsource skinを後掛けしないため、非rest poseでの二重変形を避けながら、SkinDeform後のEditMesh/Morph/材質/Outputを保持する。Core **459 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-bae7e375d86048a78ee5450d8cdeb43a`）、Windows Playerの実RadDollV3 smokeとUnity BridgeもPASS。通常評価のstale snapshot拒否は維持し、source projectionでのみ同一domainのEditMesh snapshotを差し替え出力へ再基準化する。

## 最新実装追記（同一skeletonの複数mesh GLB出力）

`ExportSkinned`／`ExportSkinnedExtended`は、同一skeleton hashを共有する複数graph objectを一つのGLBへ出力できるようになった。meshごとのprimitiveとnodeを保持しつつskinは共有し、異なるskeletonや複数objectへのinstance affine指定は明示的に拒否する。Core 459件、Windows Player、Unity Bridgeで合成2mesh往復を確認した。異なるskeletonの結合、共有mesh／morph参照、実VRChat SDK受入は引き続き未完了。

## 最新HEAD再照合（`13bc959`）

前回receiptの基準HEAD以降もP1/P2の修正を維持している。Core再実行は **454 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-40f038c9de1a437994666a376c0af406`）。Explorerから`project.nyaforge.json`を選ぶnative project再開導線も追加済みで、標準GLBでは表現できないattachment metadataをnative packageへ保持する出力境界を壊していない。

実SDK・実VRChat・実マウス/DPI差・任意GLBの完全な依存資源保持は、この静的／合成回帰の証拠には含めない。

## 再照合（2026-09-13 / `dc31fa3`）

現行mainで4件のP1修正を再確認し、Core **448 passed / 0 failed**（artifact: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4bbb7b6347fd456ead93289028b5571a`）を取得した。直近のチョーカー形状追加、既存graphへの小物object追加、頂点編集、Save/Open、複数対象BakeもPlayerで確認済み。合成Bridgeの合格は実VRChat SDKの受入とは分け、次の作業をSDKの版・完全修飾型を固定した実component生成／更新へ限定する。

狭いWindows画面の追加確認では、ステータス欄を1行へ固定し全文をtooltipへ残す修正後に800x600／1600x1000のAuthoring suiteをともにPASSさせた。最新800x600 reportは `Artifacts/Authoring-20260913-052112-6c8f4cfef1e1407c9e807eaa5ba711eb/report.json`、最新1600x1000 reportは `Artifacts/Authoring-20260913-052214-0691af25c0ad410183470d1c5c8fb86e/report.json`。これはPlayer内自動操作の受入であり、DPI差・実マウス・実VRChat内の確認とは分ける。

提示されたレビュー（基準: `1c76e4a`）を現行 `main` と照合した。4件のP1は後続コミットで修正済みであり、同じ不具合を未対応として再実装しない。以下は現行コードと回帰の対応表である。

| 指摘 | 現行対応 | 回帰・境界 |
| --- | --- | --- |
| 16bit `JOINTS_n` の2バイト幅誤読 | `GlbSourceSkinImporter` が `row[i * 2] \| row[i * 2 + 1] << 8` で読取る | 8bit/16bit同値、全dense set、負weight拒否をCoreで確認。sparse weightは未対応 |
| source skin後の下流編集消失 | `SourceSkinGraphAdapter.ApplyToEvaluation` が `SkinDeform` 出力をsource paletteで差し替え、下流graphを再評価 | 非rest pose＋SkinDeform後EditMesh、同一skeleton／poseを共有する複数 `SkinDeform` 同時評価をCoreで確認 |
| PhysBones packageのsource不足 | target packageへ `secondary-motion.nyaforge.bin` を同梱し、receiverがprofile hashで検証 | package往復・改ざん・source hash不一致をCore/Bridgeで確認。実SDK受入は未実施 |
| skin付きGLBと同居する静的小物の拒否 | `GlbImporter.MeshHasSkin(root, meshIndex)` が選択meshだけを判定 | 同居fixtureで小物取込とskin mesh拒否をCoreで確認。同一skeleton hashの複数meshはshared skin出力へ対応、共有mesh／morph参照は未対応 |

## 検証

- Core: **448 passed / 0 failed** (`dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore`)
- 最新artifact: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4bbb7b6347fd456ead93289028b5571a`
- MCP transport: **3 passed / 0 failed**（19 tool registry、instance付きnamed pipe、captureのcamera metadata保持とPNG bytes非重複）。
- 実RadDollV3はprivate素材としてのみ取込 smoke に使用し、public repositoryへ同梱していない。

このレビューで残る実装対象は、I04-A/Bの異なるskeleton結合・共有mesh/skin/morph参照、I04-Eの材質・未知拡張の完全保持、SIM-02B/SIM-07Aの実SDK/実VRChat受入である。I04-Eの入口として、GLB importerは材質・animation・extensionsRequired/Usedのコード付きdiagnosticsを返し、blocking/partialを区別する回帰を追加した。今回、選択primitiveのmaterial slotと基本PBR係数（baseColorFactorのlinear化、metallic/roughness、emissive、alpha）をnative `StandardMaterial`／`AssignMaterials`へ接続した。画像・sampler・追加拡張は引き続き未保持としてdiagnosticを返す。容量拡張としてnativeは512骨／32 influence／512 morph、GLB取込と拡張GLB出力は全JOINTS_n/WEIGHTS_n setへ対応した。標準SkinnedGeometry出力は受取先互換のため4 influenceを明示拒否する。Coreや合成Bridgeの合格を、実SDK・実VRChatでの受入完了とは扱わない。

複数objectのinspection一覧（activeObjectId、graphId、評価状態、output要約、diagnostics）はCore回帰とPlayer compileで確認済み。GLB取込diagnosticsは`import-diagnostics.nyaforge.json`へschema 4で保存し、再Open後のinspectionと取込後statusへ復元する経路もCoreで確認済み。GUIの詳細report表示はPlayer自動検証まで実装済みで、材質・animation・未知拡張の完全保持は残件。

Windows Playerの再ビルドとAuthoring suiteもPASS（`Logs/build-player-20260913-032941-070.log`, `Builds/ImportDiagnosticsV2/NyaForge.exe`, `Artifacts/Authoring-20260913-032959-ca8fa373b3ef4e56b32c0f6660fe4dcf/report.json`）。
GUI詳細reportの検証もPASS（`Logs/build-player-20260913-033512-860.log`, `Builds/ImportDiagnosticsUiV2/NyaForge.exe`, `Artifacts/Authoring-20260913-033534-e6ed3ced2eb04b8896208cde5f0f05ab/report.json`）。
基本PBR材質ルーティング後の再検証もPASS（Core **442 passed / 0 failed**、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-85bc5a3302b94ac8960ee30a095b48da`、`Logs/build-player-20260913-035134-502.log`、`Builds/ImportedMaterialV2/NyaForge.exe`、`Artifacts/Authoring-20260913-035152-40bbaef3c91a4adfbe22ca513f00e432/report.json`）。埋め込みbase color画像の抽出・unbound Paint評価追加後もCore **444 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-da747d4828e944c1acb4f7874925853e`）、Windows Player `Builds/EmbeddedMaterialImageV2/NyaForge.exe`（`Logs/build-player-20260913-040537-250.log`）と Authoring suite（`Artifacts/Authoring-20260913-040557-b83542e77e2c46c2869518a629e4bff7/report.json`, 74 checks）がPASSした。
その後、標準GLB writerにも評価済みStandardMaterialと埋め込みbase-color PNGを接続し、slot別primitiveのmaterial indexを保持する出力往復を追加した。Core **445 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-ea856cd43d024edbb2698fca557f4c02`）。出力は外部画像、追加texture map、sampler、animation、VRM拡張を含まないため、完全な材質往復とは扱わない。
さらにMCPへ `forge_export_glb` を追加し、static/skinned/skinned_extended profile、saveTarget配下への固定、revision/state不変、同じexportIdの出力先再利用拒否を外部sidecar→Windows Playerで確認した。Player build `Builds/McpGlbExportV3/NyaForge.exe`、Authoring report `Artifacts/Authoring-20260913-042411-d4ffa53ce78d430baf6b34abed2a7f72/report.json`、MCP transport buildがPASSした。
# 2026-09-13 glTF material color/default alignment

`baseColorFactor`を線形値として取込・出力し、不要なsRGB往復変換を除去した。`metallicFactor`省略時はglTF既定値1を使う。Core 460件、Windows Player Authoring suite、Unity Bridgeで回帰した。

# 2026-09-13 follow-up: graph-keyed metadata, bind retention, and hit testing

追加レビューのP1「複数graph保存で表情・Springが混線」は、`NVXE`/`NVXS` のGraphId keyed session table、旧単一blob fallback、Open時の全rig/session source hash検証で対応した。source skinはSkinDeform位置を差し替えて下流graphを再評価するため、非rest poseの編集差分を二重適用しない。Workbenchのskinned GLB経路にはimported source skinのinverse-bind行列をobject単位で渡す新overloadを追加し、制作graphはrest-derived fallbackを使う。装着小物の頂点pickはpreview root適用後のworld座標を使用する。

検証: Core **460 passed / 0 failed**。Windows Player `VrmSessionTableV4` のAuthoring suite（private一時RadDollV3指定）とUnity 2022.3.22f1 BridgeはPASS。実VRChat SDK、任意GLBの完全なnode transform互換、実マウス/DPI差は引き続き別の受入境界とする。
