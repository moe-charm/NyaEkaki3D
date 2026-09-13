# PhysBones SDK受け取り互換

NyaForgeの`vrchat.physbones`受け取りは、VRChat SDKをコンパイル時参照せず、Unity Editor内で実行時型を解決するreflection adapterを使う。SDKが未導入の通常Player／Coreは従来どおりビルドできる。

## SDK 3.7.6で確認した写像

2026-09-13にVCCキャッシュの`com.vrchat.base`／`com.vrchat.avatars` 3.7.6（Unity 2022.3）を一時Unity projectへ導入し、実際の`VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone`を生成して確認した。検証コードはUnityBridge package内の明示実行`PhysBonesSdkIntegrationVerification.Run`として保持し、SDKなしの通常起動では実行しない。

- `maxAngle`は`maxAngleX`と`maxAngleZ`へ同じ値を書き込む。
- `squish`はSDKの`maxSquish`へ写像する。
- `allowPosing`／`allowCollision`／`allowGrabbing`は`AdvancedBool { False, True, Other }`へ明示変換し、`Other`は選ばない。
- SDKに存在しない`damping`、`elasticity`、`inert`、`friction`、重力方向などへ非ゼロ値を指定した場合は、コンポーネント生成前のpreflightで`SDK_MEMBER_MISSING`として停止する。値を黙って捨てない。
- `snapToHand`、`resetWhenDisabled`、`isAnimated`、`parameter`、root／endpoint／除外骨／branch／collider／curveは型を確認して設定する。

## 再実行方法

SDKを導入した受け取り側Unity projectで、次の読み取り・生成検証を実行できる。`-RunUnityProbe`は一時フォルダへレポートとUnityログを書き、対象projectのAssetsやnative作品を変更しない。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgePhysBonesSdk.ps1 `
  -UnityProjectPath <受け取り側Unity project> `
  -UnityPath "C:\Program Files\Unity\Hub\Editor\2022.3.22f1\Editor\Unity.exe" `
  -RunUnityProbe -RequireSdk -OutputPath <probe-report.json>
```

受け取り側projectは`com.nyaforge.authoring`、`com.nyaforge.unity-bridge`、`com.nyaforge.rendering`を参照する。SDKの版はprojectのmanifest／lockfileを正本とし、probeは検出した完全修飾型とcapabilityをレポートへ保存する。

## 証跡

- 実SDK probe: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-PhysBonesSdkProbe-script-20260913.json`（`status: verified`、probe reportの`status: passed`、実コンポーネント生成・設定まで確認）
- Core: 476 passed / 0 failed（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-cebf69dc5bb5449d9e10ffe45b43746b`）
- Windows Player build: `Builds/PhysBonesSdkCompatV1/NyaForge.exe`
- Unity Bridge: `Artifacts/BridgeReceiver-20260913-172802-343-45a35a51b0ff4f50885efa1dde54ecfd/bridge-report.json`（Unity 2022.3.22f1、PASS）

この証跡はSDK型の解決と受け取り設定を確認するもの。実アバターの揺れ、VRChat Build & Test、Quest制約、実マウス／DPI差は別の手動受入である。SDKのDLLやprivate素材はリポジトリへ追加しない。
