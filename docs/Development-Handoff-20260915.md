# Nya Ekaki 3D 開発引継ぎメモ（2026-09-15）

環境移行のための停止点を記録する。ここに書いた自動検証結果は、実マウス・Unity Editor・VRChat実機の受入結果へ読み替えない。

## 現在地

- リポジトリ: `Z:\TextureVoice_local\git\NyaForge`
- branch: `main`
- remote: `https://github.com/moe-charm/NyaEkaki3D.git`
- GUI停止点: `cd211e7 refactor: share wrapped choice layout`
- 最新実装commit: `73ec1eb chore: track sparse accessor asset metadata`（sparse対応本体は`eff6039`）
- 最新記録commit: `2a7d17d docs: record sparse accessor checkpoint`
- 引継ぎ文書更新commit: `367642b docs: add development handoff checkpoint`
- `main`はpush済み、作業ツリーはclean
- Windows Player: `Builds/WrappedChoiceV1/NyaForge.exe`

今回の停止点では、取込・基本形状・装着PanelのDropdownを共通`UiElements`部品へ移し、長い表示名を折り返すレイアウトとtooltipの責務を統一した。さらにGLB sparse accessorを共通readerへ接続し、静的POSITION／indicesとskinned JOINTS_n／WEIGHTS_nの差分形式をゼロ埋め＋検証済み上書きで読めるようにした。対象avatarは表示名・役割・短縮ID、BoneIdは骨名・短縮IDを画面に出し、完全なIDはtooltipで確認する。保存形式、stable ID、MCP wire、出力契約は変更していない。

## 検証済み証跡

- Core: **519 passed / 0 failed**（sparse accessor回帰を含む）
  - `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-1c284d2fe28944be8785d7118decea3a`
- Player build: Unity **6000.4.3f1**
  - `Builds/SparseAccessorV1/NyaForge.exe`
  - `Logs/build-player-20260915-175429-951.log`
- Authoring自動検証: **PASS**（1069×698、`Artifacts/Authoring-20260915-175457-b55456bf4152414c904f601bf41f3d60/report.json`）
- Player build: Unity **6000.4.3f1**
  - `Logs/build-player-20260915-173613-822.log`
  - `Builds/WrappedChoiceV1/NyaForge.exe`
- Authoring: 1069×698 **PASS**
  - `Artifacts/Authoring-20260915-173638-ee084f4b08df45efb7163094701bf0b7/report.json`
- 実RadDollV3 Player: **PASS**
  - `Artifacts/Authoring-20260915-173806-1ce32564863d4c6c931ca5229c9a94d7/report.json`
  - 同artifact内に実衣装package `real-clothing-project/exports/*/skinned-clothing.nyaforge.json`
- Unity Bridge: Unity **2022.3.22f1** **PASS**
  - `Artifacts/BridgeReceiver-20260915-174102-609-23abb3f848b44f4fb12818755f7f8734/bridge-report.json`

## 再開手順

PowerShellでリポジトリを開き、対象branchとremoteを確認する。

```powershell
Set-Location 'Z:\TextureVoice_local\git\NyaForge'
git switch main
git pull --ff-only origin main
git status --short
```

Core回帰とPlayerを別のbuild名で実行する。起動中のPlayerを上書きしない。

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore
powershell -ExecutionPolicy Bypass -File Tools/Build-NyaForge.ps1 -Target Player -BuildName <new-build-name>
```

実モデル一周は、privateに保存したRadDollV3 VRMを`-ImportModel`へ渡す。private素材は公開ツリーへコピーしない。

```powershell
powershell -ExecutionPolicy Bypass -File Tools/Test-NyaForgeAuthoring.ps1 `
  -BuildName <new-build-name> -RealClothing -ImportModel '<private VRM path>' `
  -Width 1069 -Height 698 -TimeoutSeconds 240
```

生成されたreport内の`real-clothing-project/exports/*/skinned-clothing.nyaforge.json`をUnity Bridgeへ渡す。

```powershell
powershell -ExecutionPolicy Bypass -File Tools/Test-NyaForgeUnityBridge.ps1 `
  -PlayerCheckDirectory '<Authoring artifact>' `
  -ClothingPackageManifest '<skinned-clothing.nyaforge.json>' `
  -TimeoutSeconds 1200
```

## 次に行う受入

1. 実ウィンドウで取込→対象avatar／BoneId選択→基本形状→頂点編集→fit／weight→Save/Open→出力を一周する。
2. 1069×698の狭い窓とDPI 100/150/200%、日本語IME、長い表示名・空白pathを確認する。
3. 実RadDollV3で正面・背面・左右・斜め、肩上げ・肘曲げ・前屈・着座相当poseのfit・貫通・材質見た目を確認する。
4. Unity Editorでstable BoneId割当、avatar root移動／回転／scale、衣装A→B更新、削除・Undo、normal／MR／alphaを確認する。
5. 対応SDKを固定したUnityでBuild & Testし、VRChat内表示・負荷・アップロード後の再現を別証跡として記録する。

## 未完了の境界

VRM LookAt／FirstPerson／表情material bindなど初期profileで保持しない意味情報は、`VRM_SEMANTICS_NOT_RETAINED`として診断・保存・出力reportへ残る。完全意味情報の保管・変換は別profileの課題。MagicaCloth2はVRChat出力の根拠にせず、VRChatでは許可されたPhysBones経路を確認する。

進捗を更新したら、実装・Core／Player／Bridge自動結果・実マウス・Unity／VRChat受入を混ぜずに`current_task.md`へ時系列で追記する。公開commit前には`git ls-files private`が空で、`Builds/`と`Artifacts/`が未追跡であることを確認する。
