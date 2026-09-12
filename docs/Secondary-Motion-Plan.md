# 揺れ・布シミュレーションの接続計画

更新: 2026-09-12。ボーンのフィードバックを受けた採用方針と未着手タスク。製品範囲は設計v2のC2〜C5、Windows先行を維持する。MagicaCloth2の導入済み・動作確認済みを意味しない。

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

保存はnative正本へ版付きで追加する。現在の3種類のmetadata attachmentへ無制限のvendor JSONを混入させず、schema・byte予算・移行を設計する。依存packageなしでも作品を開き設定を保管できること。未知版は編集/再生不可を示して保持し、値を初期化しない。固定領域はmesh IDとtopology依存を持ち、頂点の追加/削除時に明示再対応する。

計算結果は一時表示。保存・Undo・出力は制作姿勢を使う。骨と頂点の両方を動かすadapterを同じ出力型へ無理に押し込まない。base pose→simulation→表示の順とworld root・collider更新時点を各adapterの契約に含め、同じ骨への二重適用を拒否する。

撮影証拠にはinput/config hash、simulator/adapter/package版、target、Unity/build版、時間刻み・step数・warmup、入力pose/root運動、カメラ条件を記録する。固定stepや決定的な再実行ができないbackendは能力に明記し、画像の完全一致を約束しない。

## タスクと完了条件

SIM-01のCore契約を実装済み。`Authoring/Simulation`に安定ID付きchain、fixed vertex、collider group、bone/mesh出力種別、adapter capability/evaluation interface、unknown versionを保持する`NYSM` v1 codecを置いた。`Import/VrmSecondaryMotionMigration`はresolved VRM1 spring chainを共通topologyへ変換する。native attachment接続、未知版のWorkbench表示、PhysBones／MagicaCloth2実adapterは未着手。巨大な汎用物理層を先に作らず、既存VRMと髪束fixtureで境界を確定する方針は維持する。

検証: Core **400 passed / 0 failed**（`Logs/core-secondary-motion.txt`）。Windows-SecondaryMotionCore2 build／Player（既存回帰70 checks）もPASS（`Logs/build-player-20260912-184902-690.log`、`Artifacts/Authoring-20260912-184923-60fa1a94c49d4c67833fcb32277bdfdf/report.json`）。これはUnityコンパイルと合成fixtureの自動検証であり、PhysBones/MagicaCloth2の実runtime、実アバター、VRChat内、実マウス操作、画像目視の受入ではない。

| ID / 優先・段階 | 作業 | 依存 / 完了条件 |
|---|---|---|
| SIM-01 / P1・C2 **実装継続** | 共通データ・adapter能力・版付き保存契約 | `Authoring/Simulation`の`SecondaryMotionAsset`／`ISecondaryMotionAdapter`／`NYSM` v1 codecでchain/fixed/colliderとtarget profileを分離し、骨/頂点出力を区別。依存なしOpenのためunknown wire versionをopaque保持し、skeleton/topology変更とstable ID欠落を拒否。VRM1 resolved spring migrationを追加。native attachmentへの保存/Open・未知版GUI表示・旧VRM0 source-node移行は次段 |
| SIM-02 / P1・C2 | PhysBones target DTOとUnity Bridge | SIM-01とskin出力。stable bone対応、root/末端/除外/分岐、collider、制限、曲線、interaction設定を対象SDK版付きで扱う。対応/未対応一覧、再出力時の管理対象限定更新、受け取り側設定と動作の確認を記録 |
| SIM-03 / P1・C2 | 共通GUI/MCPと再生所有者 | SIM-01。既存VRM0/1を段階接続。設定Undo・保存/Open、build待ち/失敗/取消、再構築中のproject切替、reset、一定時間再生、連続撮影を検証。外部MCP transportも別途確認 |
| SIM-04 / P2・C2 | MagicaCloth2 BoneCloth最小評価 | SIM-01/03。利用可能package/ライセンスとUnity/Burst/Collections版を記録。任意assemblyに隔離し、未導入buildも成功。自作髪束1本でruntime生成・設定・構築完了待ち・固定根・sphere衝突・再構築/reset・破棄・写真列を確認して採用可否を記録 |
| SIM-05 / P2・C3 | MeshClothとmorph・固定領域の評価 | SIM-04と頂点領域編集。小さい布で固定/可動をGUI/MCP指定。morph変形頂点との重複を検出して拒否/対象分離を案内。形状を焼き込む場合は明示した派生assetへ。morphを黙って無効化しない。法線更新と時間/GC/メモリをBoneClothと比較 |
| SIM-06 / P2・C3 | BoneSpringとUnityアプリ向け出力 | SIM-04。BoneSpring用の小fixtureで設定・保存・再構築を確認。Magica用profileをUnity受取側へ出し、依存不足診断・package版照合・再出力を検証 |
| SIM-07 / P1・C2→C5 | target別の受入証拠 | SIM-02/03、任意targetは04〜06後。同じ髪束とroot移動/停止・旋回・pose・colliderの手順で記録。VRChat用は対応SDK/受取UnityとVRChat内で確認。Magica結果と混同しない。I03-B/T04/T05の更新順・性能・実操作課題も残さない |

SIM-02/03を先に進め、SIM-04はPhysBonesの受入を置換しない任意評価。MeshClothやMagica導入完了をC2の全身キャラ完成条件へ追加しない。C2の必須は選んだ出力先で髪束1本が動くこと。Magica導入時の購入・vendorソース取得/配布はこのタスク化では実行していない。public repoには自作adapter・fixture・設定schemaを置き、vendor assetを同梱しない。

## 評価の判定

採用は「起動した」だけで決めず、保存の独立性、再構築失敗からの復帰、対象morphとの互換性、同一骨への競合、性能、出力先での再現を記録する。Magica採用を見送ってもnative制作とPhysBones/VRM出力を継続できる構造を完了条件に含める。
