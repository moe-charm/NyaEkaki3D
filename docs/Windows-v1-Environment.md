# Windows v1 検証環境マニフェスト

記録日: 2026-09-14。検証対象コード: `main` `2965a93`。直近Core証拠の実行対象commitは同commit。これは出荷環境の保証ではなく、同じ検証を再実行するための基準である。

## 現在固定できているもの

| 項目 | 値 | 状態 |
|---|---|---|
| repository / branch | `moe-charm/NyaEkaki3D` / `main` | 固定 |
| Authoring Player | Unity `6000.4.3f1` (`6000.4.3.3789224`) | PASS |
| Unity Bridge receiver | Unity `2022.3.22f1` (`2022.3.22.8944612`) | PASS |
| .NET Core test runtime | `10.0.202` | PASS |
| Authoring package | `com.nyaforge.authoring` `0.1.0` | 固定 |
| Unity Bridge package | `com.nyaforge.unity-bridge` `0.1.0` | 固定 |
| Rendering package | `com.nyaforge.rendering` `0.1.0` | 固定 |
| package contract | `skinned-clothing-v1`、meters、`Storage.Coordinates`、stable BoneId明示割当 | 固定 |
| private receiver SDK probe | VRChat `com.vrchat.base` / `com.vrchat.avatars` `3.7.6`、Unity `2022.3.22f1` | `VRCPhysBone`解決・生成・preflight PASS |
| OS | Windows 11 Pro `10.0.26200` | 基準機 |
| GPU | AMD Radeon(TM) Graphics / NVIDIA GeForce RTX 4090 | 基準機 |

## fixtureと証拠

- Core: `dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore`。検証対象mainで **505 passed / 0 failed**（最新artifact: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-2bd33d69fb4843c9960f648d30d381b4`、JPEG原画像MIME・bytes保持回帰を含む）。最新package fixtureはpublic repositoryへ置かず、一時artifactへ出力する。
- Navigation: V24 `1280x800`の通常ビューワー入口回帰は`Artifacts/Navigation-20260914-074515-633035562ad14daeac53226ed8626bff/report.json`でPASS。
- Windows Player: `Builds/ReleaseCandidateV10/NyaForge.exe`で確認したprivate一時RadDollV3 VRMの全mesh取込→EditMesh→native Save/Open→標準skinned GLB／VRM出力、semantic texture、原画像source inspection、未編集base-color原画像再出力の証拠は従来artifactを参照する。高DPI UI回帰は`Builds/ReleaseCandidateV28/NyaForge.exe`、実RadDollV3一周スモークは`Builds/ReleaseCandidateV23/NyaForge.exe`で追加確認した。Player reportは`Artifacts/Authoring-20260914-071904-d5dacd0d18284842a6f1ed2868f3c07d/report.json`、Unity Bridge 16 checksは`Artifacts/BridgeReceiver-20260914-072421-931-2f5e73fbea2d4c288580e5fc6d654b80/bridge-report.json`、高DPI画面証跡は`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-manual-v28-1080.png`である。
- Unity Bridge: 上記Player reportと衣装packageを使った合成receiver回帰。`Artifacts/BridgeReceiver-20260914-063802-587-d17638bb920544bcb4192f9ef4b84e43/bridge-report.json`（PASS）。packageのBoneId、bindpose、材質、ownership、semantic map適用を含む。
- private素材は公開リポジトリへ追加しない。入力pathやsceneはローカル証拠でのみ管理し、共有時はhashと匿名化したreportを使う。

## 未固定・受入待ち

- Authoring本体には`com.vrchat.*` dependencyを入れず、private receiver probeへSDK 3.7.6を導入して型解決・生成を確認済み。実RadDollV3 scene、実クライアント、Build & Test、実VRChatは **未受入/BLOCKED** とする。
- UniVRMの受入版、実RadDollV3 Unity sceneの全BoneId割当、対象shader版は外部受入時に追加固定する。
- EditorWindowの実マウス操作、DPI 100/150/200%、日本語・空白path、別Windows環境は未受入である。

- SDK probe証拠: `private/PhysBonesSdkProbe-20260914/sdk-probe-report.json`（`status: verified`）。private project・SDK DLL・private avatar素材は公開しない。
- RadDollV3 Unity import probe: `private/PhysBonesSdkProbe-20260914/avatar-import-report2.json`（20 `SkinnedMeshRenderer`、279 transforms、renderer bone参照3420件、共通root `Hips`）。
- RadDollV3実chain PhysBone probe: `private/PhysBonesSdkProbe-20260914/avatar-physbone-report.json`（`Skirt_B_1_1.L`→`Skirt_B_1_2.L`へ実`VRCPhysBone`設定済み）。衣装package適用、実scene全chain、Build & Test、実VRChatは未受入。
- RadDollV3実衣装package probe: `private/PhysBonesSdkProbe-20260914/avatar-clothing-reapply-report.json`（低ポリカフ136頂点を`skinned-clothing-v1`として出力・読込し、`lower_arm.L`へ初回適用・再適用、Renderer数21、rootBone／ownership marker／binding更新、native graph/package roundtrip確認）。実EditorWindow操作、fit・貫通、見た目、Build & Test、実VRChatは未受入。

## 使い方

このmanifestのPASSはCore／Player／合成Bridge／private SDK probeの範囲だけを示す。実Unity avatar、実VRChat、他者視点、出荷候補の判定へ自動的に読み替えない。private probeで型解決は済んだため、次は同じfixtureとcommit hashを使った実RadDollV3 sceneのNF-V1-02A/02B受入へ進む。