# PhysBones SDK受け取り互換

NyaForgeの`vrchat.physbones`受け取りは、VRChat SDKをコンパイル時参照せず、Unity Editor内で実行時型を解決するreflection adapterを使う。SDKが未導入の通常Player／Coreは従来どおりビルドできる。

## SDK 3.7.6で確認した写像

2026-09-13にVCCキャッシュの`com.vrchat.base`／`com.vrchat.avatars` 3.7.6（Unity 2022.3）を一時Unity projectへ導入し、実際の`VRC.SDK3.Dynamics.PhysBone.Components.VRCPhysBone`を生成して確認した。

- `maxAngle`は`maxAngleX`と`maxAngleZ`へ同じ値を書き込む。
- `squish`はSDKの`maxSquish`へ写像する。
- `allowPosing`／`allowCollision`／`allowGrabbing`は`AdvancedBool { False, True, Other }`へ明示変換し、`Other`は選ばない。
- SDKに存在しない`damping`、`elasticity`、`inert`、`friction`、重力方向などへ非ゼロ値を指定した場合は、コンポーネント生成前のpreflightで`SDK_MEMBER_MISSING`として停止する。値を黙って捨てない。
- `snapToHand`、`resetWhenDisabled`、`isAnimated`、`parameter`、root／endpoint／除外骨／branch／collider／curveは型を確認して設定する。

## 証跡

- 実SDK probe: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-PhysBonesSdkProbe-20260913-5e7f66bbf32a464286e9a84465f5a79c/physbones-sdk-report-16.json`（`status: passed`、実コンポーネント生成・設定まで確認）
- Core: 476 passed / 0 failed（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-cebf69dc5bb5449d9e10ffe45b43746b`）
- Windows Player build: `Builds/PhysBonesSdkCompatV1/NyaForge.exe`
- Unity Bridge: `Artifacts/BridgeReceiver-20260913-172802-343-45a35a51b0ff4f50885efa1dde54ecfd/bridge-report.json`（Unity 2022.3.22f1、PASS）

この証跡はSDK型の解決と受け取り設定を確認するもの。実アバターの揺れ、VRChat Build & Test、Quest制約、実マウス／DPI差は別の手動受入である。SDKのDLLやprivate素材はリポジトリへ追加しない。
