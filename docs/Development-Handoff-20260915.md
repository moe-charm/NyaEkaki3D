# Nya Ekaki 3D 開発引継ぎメモ（2026-09-15）

環境移行のための停止点を記録する。ここに書いた自動検証結果は、実マウス・Unity Editor・VRChat実機の受入結果へ読み替えない。

## 現在地

- リポジトリ: `Z:\TextureVoice_local\git\NyaForge`
- branch: `main`
- remote: `https://github.com/moe-charm/NyaEkaki3D.git`
- GUI停止点: `cd211e7 refactor: share wrapped choice layout`
- 最新実装commit: `5189309 feat: support sparse inverse-bind accessors`（sparse本体`eff6039`、Unity `.meta`を含む）
- 最新記録commit: `5c3a141 refactor: centralize selection presentation refresh`
- 引継ぎ文書更新commit: `5c3a141 refactor: centralize selection presentation refresh`
- `main`はpush済み、作業ツリーはclean
- Windows Player: `Builds/HandoffV2/NyaForge.exe`

移行後の再構築確認として、現行`main`から`HandoffV2`を新規ビルドし、Core／通常Authoring／実RadDollV3衣装工程／Unity Bridgeを再実行した。全て **PASS**。実モデルartifactは`Artifacts/Authoring-20260915-182352-e35ea993d1224ca99d2574cead259264/report.json`、packageは`real-clothing-project/exports/clothing-20260915-092543-2d0bbf/skinned-clothing.nyaforge.json`、Bridgeは`Artifacts/BridgeReceiver-20260915-182632-565-6ebd5c0eb8f94ac0b261aff74fc9febc/bridge-report.json`。

環境移行直前の実モデル停止確認として、同PlayerへprivateのRadDollV3 VRMを渡した一周も **PASS** した。artifactは`Artifacts/Authoring-20260915-180348-8a01c5330e2f43b9a4f91ae158950f8c/report.json`、衣装packageは`real-clothing-project/exports/clothing-20260915-090538-6e3484/skinned-clothing.nyaforge.json`。171 bones／35 morphsの取込、チョーカー頂点編集、native Save/Open、標準skinned GLB／衣装package出力までを含む。これはWindows Player自動検証であり、実マウス・Unity Editor・VRChat実機の受入へは読み替えない。

今回の停止点では、取込・基本形状・装着PanelのDropdownを共通`UiElements`部品へ移し、長い表示名を折り返すレイアウトとtooltipの責務を統一した。さらにGLB sparse accessorを共通readerへ接続し、静的POSITION／indices、skinned JOINTS_n／WEIGHTS_n、逆bind MAT4の差分形式をゼロ埋め＋検証済み上書きで読めるようにした。対象avatarは表示名・役割・短縮ID、BoneIdは骨名・短縮IDを画面に出し、完全なIDはtooltipで確認する。保存形式、stable ID、MCP wire、出力契約は変更していない。

## 検証済み証跡

- Core: **521 passed / 0 failed**（sparse POSITION／weight／inverse-bindと、疎skinned GLBのwriter→再取込往復を含む）
  - `C:\Users\tomoaki\AppData\Local\Temp\NyaForge-Core-Tests-aea1f92d753a4f87871c01f40fdf2574`
- HandoffV2 Player build: Unity **6000.4.3f1**（`Builds/HandoffV2/NyaForge.exe`、`Logs/build-player-20260915-182215-662.log`）
- HandoffV2 Authoring: **PASS**（`Artifacts/Authoring-20260915-182308-272a6d1dfd7847a4b17868012c3665c3/report.json`）
- HandoffV2 実RadDollV3 Authoring: **PASS**（`Artifacts/Authoring-20260915-182352-e35ea993d1224ca99d2574cead259264/report.json`）
- HandoffV2 実衣装package Unity Bridge: **PASS**（Unity 2022.3.22f1、`Artifacts/BridgeReceiver-20260915-182632-565-6ebd5c0eb8f94ac0b261aff74fc9febc/bridge-report.json`）
- HandoffV2 Navigation 1069×700: **PASS**（`Artifacts/Navigation-20260915-182919-cdbe56bb2b74443b8ce75f85c5fcab14/report.json`）。pack／recent／set／settings→Authoring、キャンセル／不正path保持、named session再開、utility panelの収まりを含む。
- SelectionEventV1 Player: Unity **6000.4.3f1**（`Builds/SelectionEventV1/NyaForge.exe`、`Logs/build-player-20260915-183146-810.log`）とAuthoring **PASS 88 checks**（`Artifacts/Authoring-20260915-183218-9e0160f220a1467f94fd74809db5a8f5/report.json`）。SelectionContext通知を選択依存Panelの更新境界へ接続した変更を含む。
- Player build: Unity **6000.4.3f1**
  - `Builds/SparseMatrixV1/NyaForge.exe`
  - `Logs/build-player-20260915-175918-288.log`
- Authoring自動検証: **PASS**（1069×698、`Artifacts/Authoring-20260915-175937-87f6de8879c64e8ea3948ed1d24a95af/report.json`）
- 疎accessor対応後の実RadDollV3 Player: **PASS**（1069×698、`Artifacts/Authoring-20260915-180348-8a01c5330e2f43b9a4f91ae158950f8c/report.json`）
  - 衣装package: `real-clothing-project/exports/clothing-20260915-090538-6e3484/skinned-clothing.nyaforge.json`
- 同packageのUnity Bridge: **PASS**（Unity 2022.3.22f1、`Artifacts/BridgeReceiver-20260915-181855-184-cfebc7793a0c4b1b83b45e8ef17724c7/bridge-report.json`）
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
