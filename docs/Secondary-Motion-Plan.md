# 揺れ・布シミュレーションの接続計画

更新: 2026-09-12。ボーンのフィードバックを受けた採用方針と実装状況。製品範囲は設計v2のC2〜C5、Windows先行を維持する。MagicaCloth2の導入済み・動作確認済みを意味しない。

## 採用する順序

制作データの保存と対象IDを整え、交換可能な計算adapterの契約を用意する。VRChat向けはPhysBones出力と受け取り先検証を優先。MagicaCloth2はC2の髪束1本で評価する任意adapterとし、MeshClothはその後のC3評価へ分ける。既存VRM0/1 previewを壊して置き換えず、共通の実行所有・診断へ段階的に接続する。

現在のI04-A（完全transform/bind保存）を中断しない。共通契約の設計は先行できるが、実装は安定したsource/制作IDと保存契約に接続する。自作髪束fixtureの試験に標準FBX取込完了は必須ではない。実アバター受入にはI04の必要範囲が必要。

## 公式資料から確認した前提

- BoneClothはTransformによる布、MeshClothは頂点、BoneSpringはTransformのばねを扱う。MeshClothはBoneClothより負荷が高いと説明されている。[Cloth Type](https://magicasoft.jp/en/mc2_magicacloth_basic/)
- C#による生成・設定・実行時構築が可能。構築には数frameの遅延があり、固定/可動領域はruntime用に指定する必要がある。Editorのペイント画面をPlayerに移植する前提にしない。[Runtime Construction](https://magicasoft.jp/en/mc2_runtime_build/)
- BlendShapeで変形する頂点へのMeshCloth利用は不可と明記されている。体型・衣装morphとの重複を採用前に検査する。[制限事項](https://magicasoft.jp/en/mc2_about/)
- VRChatの許可一覧にMagicaCloth2はなく、一覧外componentや独自scriptは動作しない。VRChat向けにMagicaCloth2 runtimeを出力しない。[許可component](https://creators.vrchat.com/avatars/whitelisted-avatar-components/whitelisted-avatar-components/)
- PhysBonesはVRChatの髪・尻尾等の二次動作を担当する。別simulatorのプレビューをPhysBonesの完成確認としない。[PhysBones](https://creators.vrchat.com/common-components/physbones/)

## モジュールと保存の責任

| 層 | 保持・担当する内容 |
|---|---|
| Authoring Core | mesh/skeleton/skin、chain・固定領域・collider参照。安定ID、topology/hash依存、単位/座標系を持つ。造形・UV・weightもここで制作する |
| 版付きtarget profile | simulator ID、adapter/schema/package版、対応target、固有値と元設定。PhysBones/VRM/Magicaの値を同じ欄へ上書きしない |
| 計算adapter | 機能一覧、検証、構築、再生、停止、reset、破棄。一時bone poseまたはmesh変形を返す。Coreへvendor型を持ち込まない |
| 実行所有者 | 非同期構築・取消・世代ID、workspace変更による破棄、状態公開、時間/更新順、projection所有。各adapterの機能差を明示する |
| GUI/MCP | 同一command経由の設定変更、再構築・reset・一定時間再生・連続撮影。準備中/失敗/依存不足を表示する |
| 出力adapter | VRChat PhysBones、VRM、Magicaを使うUnityアプリを別profileとして検証・出力。未対応項目をloss reportで報告する |

保存はnative正本へ版付きで追加する。既存の4種類の型付きmetadata attachmentへ無制限のvendor JSONを混入させず、schema・byte予算・移行を設計する。依存packageなしでも作品を開き設定を保管できること。未知版は編集/再生不可を示して保持し、値を初期化しない。固定領域はmesh IDとtopology依存を持ち、頂点の追加/削除時に明示再対応する。

計算結果は一時表示。保存・Undo・出力は制作姿勢を使う。骨と頂点の両方を動かすadapterを同じ出力型へ無理に押し込まない。base pose→simulation→表示の順とworld root・collider更新時点を各adapterの契約に含め、同じ骨への二重適用を拒否する。

撮影証拠にはinput/config hash、simulator/adapter/package版、target、Unity/build版、時間刻み・step数・warmup、入力pose/root運動、カメラ条件を記録する。固定stepや決定的な再実行ができないbackendは能力に明記し、画像の完全一致を約束しない。

## タスクと完了条件

### 採用判断を実装へ渡す境界

フィードバックの採用順は **PhysBones（P1）→ 固定step証拠（P1）→ 出力先別受入（P1）** とする。MagicaCloth2はUnityアプリ向けの任意adapterであり、VRChat用の代替実装として扱わない。制作の正本はnative、交換はGLB/VRM、adapter固有値は版付きtarget profileに保存する。各runは入力hashと設定hashで再現対象を固定し、previewの画像・ログ・状態をVRChat内の受入記録から分離する。

実装カードの完了条件は以下の通り。

- **SIM-02B**: SDK版・型を固定し、target packageを読み込んで managed componentだけを更新できる。unsupported/warningは書込み前に返し、失敗時のrollbackとSDK未導入buildを確認する。
- **SIM-03B**: 固定step/warmupの連続frame、各画像hash、adapter/package/Unity/build、pose/root/collider条件、失敗ログを1 runへ束ねる。native revision・保存・制作姿勢は不変とする。
- **SIM-07A**: root移動・停止・旋回・pose・colliderを、NyaForge preview／受取Unity／VRChat内の3面で別証拠として記録する。
- **SIM-04〜06**: vendor依存をpublic repoから隔離し、BoneCloth、MeshCloth、BoneSpringを小さなfixtureで個別評価する。MeshClothはBlendShape重複を拒否または分離案内し、性能を記録する。

SIM-01のCore契約を実装済み。`Authoring/Simulation`に安定ID付きchain、fixed vertex、collider group、bone/mesh出力種別、adapter capability/evaluation interface、unknown versionを保持する`NYSM` v1 codecを置いた。`Import/VrmSecondaryMotionMigration`はresolved VRM1 spring chainを共通topologyへ変換する。SIM-02としてPhysBones target DTO、`NYPP` v1 codec、SDK capabilityとsupported/unsupported/warningを分けるloss report、schema 4のPhysBones attachment保存/Open、Workbenchの対応版/stale/未知版表示、target package（manifest・profile・skeleton payload）、UnityBridgeのreflection writerと管理対象限定更新を追加した。実VRChat SDK／アバターでのcomponent動作とMagicaCloth2実adapterは未確認。巨大な汎用物理層を先に作らず、既存VRMと髪束fixtureで境界を確定する方針は維持する。

検証: SIM-01/02 Core **406 passed / 0 failed**（`Logs/core-physbones-bridge-package.txt`）。`NYPP` v1 attachmentに加え、`physbones.nyaforge-target.json`・profile・skeleton payloadのhash検査付き往復と改ざん拒否を確認した。Windows-SecondaryMotionPhysBonesAttachment build／Player（既存回帰70 checks）もPASS（`Logs/build-player-20260912-190938-424.log`、`Artifacts/Authoring-20260912-190959-488cdb25d1124cb0b86f86853b0891c2/report.json`）。続くWindows-PhysBonesStatusGui2 build／Player **71 checks**では、保存済みtargetの対応版表示とunknown wire version 99の保持のみ表示を確認した（`Logs/build-player-20260912-191727-520.log`、`Artifacts/Authoring-20260912-191748-5e9371fb9e454c29b618a74947a443ce/report.json`）。Windows-PhysBonesBridgeGui4 build／Player **71 checks**とUnityBridge receiver **10 checks**もPASS（`Logs/build-all-20260912-195001-224.log`、`Artifacts/Authoring-20260912-195021-2abdd80c693f4d5ba51942a327f3e29b/report.json`、`Artifacts/BridgeReceiver-20260912-202001-392-7a27c7a4d1f647868118e83a3ca6bdc0/bridge-report.json`）。Workbenchのtarget package書き出し、`ApplyPackage`経由のmanifest/profile/skeleton読込、managed-only更新、未管理component保護、unsupported停止、reflection mapping、branch表現可能性の事前検査、configure失敗時rollback、受け取り側EditorWindowのstable BoneId／collider group手動割当とavatar rootへの保存／読込を確認した。これはUnityコンパイルと合成fixtureの自動検証であり、実VRChat SDK／実アバター／VRChat内、実マウス操作、画像目視の受入ではない。

SIM-03Aの追加検証: Windows-SIM03B build **PASS**（`Logs/build-player-20260912-204039-364.log`）、Player **PASS / 72 checks**（`Artifacts/Authoring-20260912-204100-9a3d2feb718345d880ae3b9f998f27ca/report.json`）。VRM0/1のSpring previewへGUI・内部MCP handler・外部sidecar toolの`secondary_motion_play`／`pause`／`reset`／`rebuild`／`step`／`state`を接続し、実MCP client→sidecar→named pipe→Player main threadの経路で再生、停止、再構築、固定step、reset、Save/Open時の非保存、編集時破棄を確認した。外部MCP fixtureはVRM1で固定し、Playerの自動tickを止めてstep数を決定的に照合した。これはMCP transportとPlayer内自動検証であり、非同期vendor構築、実SDK／実アバター／VRChat内動作、実マウス操作・画像目視の受入ではない。

実装を止めずに受け入れ可能な順へ、SIM-01〜07を次の小タスクへ分ける。SIM-03AのGUI・内部handler・外部sidecar lifecycleとSIM-03Bの固定step capture証拠は完了済みで、現在の主経路は **SIM-02B → SIM-07A**。MagicaCloth2は任意評価へ隔離する。

| ID / 優先・段階 | 作業 | 依存 / 完了条件 |
|---|---|---|
| SIM-01A / P1・C2 **完了** | 共通データ・adapter能力・版付き保存契約 | `SecondaryMotionAsset`／`ISecondaryMotionAdapter`／`NYSM` v1 codec、VRM1 resolved spring migration、unknown version保持、skeleton/topology stale拒否を実装済み |
| SIM-01B / P1・C2 | native attachmentとVRM0移行 | native Save/Open、未知版GUI表示、stale再bind、旧VRM0 source-node移行をI04-Aへ接続。SIM-03Aと並行可能 |
| SIM-02A / P1・C2 **完了** | PhysBones target packageと合成Bridge | `NYPP` v1、schema 4 attachment、target package、loss report、stable bone／collider mapping、managed-only、branch preflight、rollback、receiver Windowを合成fixtureで検証済み |
| SIM-02B / P1・C2 **次** | 実SDK受け取り側 | SDK版・型を固定し、manifest/profile/skeleton読込、stable BoneId／collider group手動割当、実component生成・更新を確認。unsupportedは書込み前停止、未管理component保護、SDK未導入public build維持 |
| SIM-03A / P1・C2 **完了** | 共通GUI/MCPと再生所有者 | GUI・内部MCP handler・外部sidecar toolのplay/pause/reset/rebuild/fixed-step/stateを同じtransient owner／generationへ接続し、再生・停止・再構築・固定step・reset・Save/Open非保存・編集時破棄を合成backendと実named-pipe経路で確認済み。非同期vendor構築は後続 |
| SIM-03B / P1・C2 **完了** | 連続撮影とbackend証拠 | `SecondaryMotionCaptureRecord`／codec、固定1/60秒・warmup・最大8frame・pixel budget、input/config hash、adapter／package版、target、Unity/build、pose/root/collider条件、各PNG hashと失敗statusをrun単位で記録。外部MCPの3frame実通信とnative状態不変を検証済み |
| SIM-04 / P2・C2・任意 | MagicaCloth2 BoneCloth最小評価 | SIM-01/03。vendor依存を任意assemblyへ隔離し、未導入buildを維持。自作髪束1本のruntime生成・構築完了待ち・固定根・sphere衝突・rebuild/reset/破棄・写真列で採用可否を判断 |
| SIM-05 / P2・C3 | MeshClothとmorph・固定領域の評価 | SIM-04後。BlendShape変形頂点との重複を拒否または分離案内し、法線更新と時間／GC／メモリを比較。morphを黙って無効化しない |
| SIM-06 / P2・C3 | BoneSpringとUnityアプリ向け出力 | SIM-04後。BoneSpring fixtureの保存・再構築、Magica用profileの依存不足診断・package版照合・再出力を確認 |
| SIM-07A / P1・C2→C5 | PhysBones target別受入 | SIM-02B＋SIM-03A/B後。同じ髪束でroot移動／停止・旋回・pose・colliderを確認し、受取UnityとVRChat内の結果を別証拠として記録 |

SIM-02Bの実SDK受け取り側とSIM-07AのPhysBones受入を進め、SIM-04はPhysBonesの受入を置換しない任意評価。MeshClothやMagica導入完了をC2の全身キャラ完成条件へ追加しない。C2の必須は選んだ出力先で髪束1本が動くこと。Magica導入時の購入・vendorソース取得／配布はこのタスク化では実行していない。public repoには自作adapter・fixture・設定schemaを置き、vendor assetを同梱しない。

## 評価の判定

採用は「起動した」だけで決めず、保存の独立性、再構築失敗からの復帰、対象morphとの互換性、同一骨への競合、性能、出力先での再現を記録する。Magica採用を見送ってもnative制作とPhysBones/VRM出力を継続できる構造を完了条件に含める。
