# Windows v1 検証環境マニフェスト

記録日: 2026-09-13。現行main照合: `15c8654`。証拠の実行対象commitは `27eb0e6`。これは出荷環境の保証ではなく、同じ検証を再実行するための基準である。

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
| OS | Windows 11 Pro `10.0.26200` | 基準機 |
| GPU | AMD Radeon(TM) Graphics / NVIDIA GeForce RTX 4090 | 基準機 |

## fixtureと証拠

- Core: `dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore`。現行mainで **489 passed / 0 failed** の記録を維持する。最新package fixtureはpublic repositoryへ置かず、一時artifactへ出力する。
- Windows Player: private一時RadDollV3 VRMを全mesh取込→EditMesh→native Save/Open→標準skinned GLB／VRM出力。入力SHA-256は `6e5e0a0a82c28b958ea18e5a0c71b8b02e85d4d500afbd95839b4b154c0e805c`。最新証拠は `Artifacts/Authoring-20260913-225717-54b8d7c9f0bc4ed8a7fa5c93574b5656/report.json`（91 checks PASS）。
- Unity Bridge: 上記Player reportと衣装packageを使った合成receiver回帰。`Artifacts/BridgeReceiver-20260913-230234-754-918978ce45d44e5385c2a4c9aa0f4046/bridge-report.json`（PASS）。packageのBoneId、bindpose、材質、ownership、Undo削除を含む。
- private素材は公開リポジトリへ追加しない。入力pathやsceneはローカル証拠でのみ管理し、共有時はhashと匿名化したreportを使う。

## 未固定・受入待ち

- 現行NyaForge projectには`com.vrchat.*` dependencyがなく、`Tools/Test-NyaForgePhysBonesSdk.ps1`は `unavailable`。実VRChat SDK／実クライアント／Build & Testは **BLOCKED** とする。
- UniVRMの受入版、実RadDollV3 Unity sceneの全BoneId割当、対象shader版は外部受入時に追加固定する。
- EditorWindowの実マウス操作、DPI 100/150/200%、日本語・空白path、別Windows環境は未受入である。

## 使い方

このmanifestのPASSはCore／Player／合成Bridgeの範囲だけを示す。実Unity avatar、実VRChat、他者視点、出荷候補の判定へ自動的に読み替えない。SDKを導入した受け取りprojectが用意できたら、同じfixtureとcommit hashを記録してNF-V1-02A/02Bを再実行する。
