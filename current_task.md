# NyaForge 開発タスク

更新: 2026-09-12。I04-Aの完全source保存/GUI接続、mesh属性/POSITION morph変換、source slot weight/一般bind deformer、GLB全JOINTS_n/WEIGHTS_n候補読取、native weight packageとrig session v5・GUI接続、評価済みgraph meshへのsource skin adapter、authored poseからのsource palette生成、Workbench取込後/揺れ再生中のsource skin表示接続まで追加。I04-Bの入口として複数mesh/instance/skin参照を元indexで保持するGLB scene inventory、指定mesh/skinを選ぶ静的・skinned候補読取、Workbenchの候補確認・選択GUIを追加した。SIM-01の共通secondary-motion契約（安定ID、固定頂点、collider、出力種別、adapter能力、unknown version保持付きNYSM v1 codec）とVRM1 resolved spring migration、SIM-02のPhysBones target DTO／NYPP v1 codec／loss report境界／schema 4 attachment保存を追加した。制作graphへの複数対象公開と複数SkinDeformは未完了。直近Core404件合格。任意の実モデル取込・実操作・性能の受入は未完了。

## 開発の入口

作業先: `Z:/TextureVoice_local/git/NyaForge`。Windows先行、macOSは将来。
読む順: このファイル → [モデル交換仕様](docs/Model-Interchange-Spec.md) → [実素材調査](docs/Real-Asset-Import-Plan.md) → 対象コード。製品全体の範囲は [開発計画](docs/Development-Plan.md) と [設計v2](docs/NyaForge-Authoring-Design2.md) を参照。[文書一覧](docs/README.md)参照。
製品目標は小物の制作・出力を一周し、低ポリ全身キャラ、品質向上へ進むこと。設計v2は製品方針、v1は背景資料。設計中の外部依存・機能は採用済みや実装済みを意味しない。

## 次に実行するタスク

採用した方針: **nativeを制作の正本、GLB/VRMを交換形式、FBX/BLEND/Unityを原本として区別する。** 未対応データを黙って削らず、情報ごとの能力と保持結果を報告する。Blender調査は任意の開発ツールで、標準制作の必須依存へ変更しない。詳細・完了条件は [モデル交換仕様](docs/Model-Interchange-Spec.md)。

| 状態 / ID | 実行する作業 | 完了条件・依存 |
|---|---|---|
| [ ] I04-A / P1 **継続** | source local/world、一般TRS/matrix、inverse-bind、法線/接線変換の基盤 | 数値/reader/codec/GUI生成/native原本なしOpen、mesh/POSITION morph変換、SourceSkinBinding/SourceSkinDeformer、GLB全JOINTS_n/WEIGHTS_n候補、NYSPとrig v5/native/GUI接続、評価済みGraphMeshValueへのSourceSkinGraphAdapter、authored poseからのSourceSkinPosePalette、Workbench取込後/揺れ再生中の自動表示を追加済み。次は複数mesh/instance/skin、複数SkinDeform、再利用mesh/object、normal/tangentと失敗時原子性を検証。現在はSkinDeform一個のsource入力を対象にし、sparse/normalized weightは未対応 |
| [ ] I04-B / P1 **着手** | 複数mesh/instance/skin、source→制作ID対応 | `GlbSceneInventoryReader`でmesh/primitive数、node instance、skin joint参照、node world transformを元indexのまま候補化し、`GlbImporter.Read(bytes, meshIndex)` / `GlbSkinImporter.Read(bytes, meshIndex, skinIndex)` / `GlbSourceSkinImporter.Read(bytes, meshIndex, skinIndex)` とWorkbenchの候補確認・選択GUIを追加した。次は選択候補のgeometry/skinを複数制作対象へ公開し、Objects[0]前提、同名morph、共有参照、編集/保存/Openを解消・検証する |
| [ ] I04-C / P1 | rig/weight/morph容量とcodec/hash/表示/出力 | 257骨・18weight・単一mesh262morph以上の入力を削減なしで往復。byte/メモリ予算と超過時の拒否を同時に決める |
| [ ] I04-D / P1 | 標準FBX Bridge入力と任意の変換adapter | Blender必須化なし。依存検出・変換前後比較・原本保護・失敗/取消を確認。実取込はA〜Cに依存 |
| [ ] I04-E / P1 | 機能report、材質/animation/VRM意味情報/未知拡張の保持とGUI/MCP | 必須未知拡張の拒否、既知VRM内の未保持field、opaque依存資源と参照失効、未対応を完全成功にしない。report設計はAと並行可 |
| [ ] T04 / P2 | 時間超過・性能受入 | 0.25秒超frameで停止する現行動作の扱い、frame時間/GC/メモリを実測し方針と回帰を定める |
| [ ] T05 / P1 | Windows実素材・実操作と受取側 | 必要なI04後に取込・pose/揺れ・保存/Open・終了・DPI/文字欠け・出力を実確認。build/input hash/手順/結果を記録 |
| [ ] T06 / P2 | 外部MCP metadata保存受入 | 内部handlerと区別し、transport経由で保存/Open・失敗保護・再試行を確認 |

## 完了した前提と残る境界

### ボーンの追加フィードバック: SIMタスク（2026-09-12）

[揺れ・布adapter計画](docs/Secondary-Motion-Plan.md)へ採用方針・依存・完了条件を整理した。**PhysBones優先、MagicaCloth2は任意adapter**。SIM-01のUnity非依存契約とcodec、SIM-02のPhysBones target DTO／loss report境界とschema 4 attachment保存を実装したが、PhysBones／MagicaCloth2の導入・実行接続はまだ行っていない。直近はSIM-02の未知版表示・Unity SDK BridgeとI04-Aの保存接続を継続する。

- [ ] SIM-01 / P1 **継続**: `Authoring/Simulation`に共通データ（stable bone chain、fixed vertex、collider group、output kind）、adapter capability/evaluation contract、unknown versionを保持する`NYSM` v1 codecを追加。VRM1のresolved springを共通topologyへ移すmigrationも追加した。次はnative attachmentへの保存/Open・未知版GUI表示・stale再bindをI04-Aへ接続。
- [ ] SIM-02 / P1 **継続**: `PhysBonesTargetProfile`／`PhysBonesChain`でSDK版付きのstable root・endpoint・exclusion・branch・collider・limits・curves・interactionを保持し、`NYPP` v1 codec、schema 4の`physbones-target.nyaforge.bin` attachment、`PhysBonesLossReport`で未知版保持・対応/未対応/SDK差異を明示するCore境界を追加。次は未知版のWorkbench表示、Unity SDK component writer、管理対象限定更新、受け取り側設定/動作確認。
- [ ] SIM-03 / P1: GUI/MCPの設定・再構築・reset・一定時間再生・連続撮影とbackend証拠。
- [ ] SIM-04 / P2: C2でMagicaCloth2 BoneClothの髪束1本を任意評価。未導入buildも維持。
- [ ] SIM-05 / P2: C3でMeshClothの固定領域・morph重複拒否と性能を評価。
- [ ] SIM-06 / P2: BoneSpringとMagica対応Unityアプリ用出力。
- [ ] SIM-07 / P1: C2〜C5でtarget別受入。VRChat内確認と他simulatorのプレビューを区別。I03-B/T04/T05と接続。

### 既存工程

- [x] **T01**: 全source node階層と元children順をrig session v3へ保存。v1/v2は階層不明を維持。
- [x] **T02**: VRM0 subtree/一時骨格/実行所有者/共通再生GUIを対応profileで接続。両形式のPlayer handler往復合格。
- [x] **T03**: ローカルFBX3件を読取調査。アバター20mesh、全3件257骨、アバター最大18deform bone影響/頂点・単一mesh262morphを確認。生データはprivate。変換・実importの成功はまだ確認していない。
- [x] **R01〜R12 / I03-A**: 記録した自動検証範囲で修正・adapter接続済み。[Rig/VRMレビュー](docs/reviews/2026-09-12-Rig-Vrm-Review.md)、[R11/R12](docs/reviews/2026-09-12-Current-Checkpoint.md)、[再生checkpoint](docs/reviews/2026-09-12-Playback-Checkpoint.md)。
- [ ] **I03-B/C / A01**: 可動world root、揺れるnodeのcollider更新順と参照runtime比較、実素材/実操作/性能の受入は残る。I03-CはVRM0/1両方のGUI/handler接続済みだが、受入全体は未完了。
- [ ] **I04 / C0〜C5**: 任意モデル取込と制作/出力の製品全体は未完了。skin/morph出力・受取側確認などを [開発計画](docs/Development-Plan.md) から省かない。

## 直近の証拠

- SourceMeshTransform: Core **378 passed / 0 failed** (`Logs/core-source-mesh-transform.txt`)。非一様/shear/鏡映の位置・方向・面順/UV保持、morph ID維持とtopology再pin、morph適用との可換性、不正方向/stale入力を検証。一般skinのweight混合・GUI取込はまだ未接続。
- Windows-SourceMeshTransform build **PASS** (`Logs/build-player-20260912-170453-386.log`)。独立Coreモジュール追加のためPlayer GUI suiteは今回再実行していない。
- SourceSkinBinding/Deformer: Core **381 passed / 0 failed** (`Logs/core-source-skin-deformer.txt`)。slot順、32影響/vertex、rest inverse-bind相殺、pose paletteの位置/法線/接線/UV/topology保持、stale domain/未weight/不正paletteを確認。GLB weight decoder・native binding・GUI保存接続まで完了。既存graphの一般deform表示接続は未完了。
- GLB source skin importer: Core **384 passed / 0 failed** (`Logs/core-glb-source-skin-importer.txt`)。一meshの全primitiveでdense JOINTS/WEIGHTS set、slot順・vertex offset・skin参照をSourceSkinBindingへ接続し、unpaired/noncontiguous/type/sparse/range/宣言buffer不整合を拒否。normalized/sparseは未対応。
- Source skin package / rig v5: Core **385 passed / 0 failed** (`Logs/core-source-weight-native.txt`)。NYSPの行列＋全元weight往復、byte同一、切断/末尾/version/section破損、v5 Save/Openと旧v1〜v4情報境界を確認。GUI skinned取込とPlayer Save/Openは同じ変更で検証済み。
- Windows-SourceWeightNative2 build **PASS** (`Logs/build-player-20260912-172508-037.log`)、Player **PASS** (`Artifacts/Authoring-20260912-172528-cca84b685fc84c1eb596e2dec4dc785a/report.json`)。VRM0/1でGLB由来source package保持、原本なしOpen、揺れ再生、編集時停止、import失敗保護を確認。実マウス操作・任意実素材・画像目視の受入ではない。
- SourceSkinGraphAdapter: Core **387 passed / 0 failed** (`Logs/core-source-graph-adapter.txt`)。評価済みGraphMeshValueへsource paletteを明示適用し、meshだけを差し替え、domain/RestTransform/topologyと入力不変を保持。polygon編集値とstale topologyは拒否。
- Windows-SourceGraphAdapter build **PASS** (`Logs/build-player-20260912-173556-982.log`)、Player **PASS** (`Artifacts/Authoring-20260912-173622-7883a8c24efd4ba4a58c31245e28708c/report.json`)。既存VRM0/1 playback、source package Save/Open、import失敗保護を回帰確認。adapterの実マウス操作・任意実素材・画像目視の受入ではない。
- SourceSkinPosePalette: Core **389 passed / 0 failed** (`Logs/core-source-pose-palette.txt`)。sessionのsource joint slotとauthored BoneIdを検査し、`PoseTransform × T(-head) × sourceWorld`でrest cancellationとpose変形を確認。legacy sessionは完全source skinなしとして拒否。
- Windows-SourcePosePalette build **PASS** (`Logs/build-player-source-pose-palette.txt`)、Player **PASS** (`Artifacts/Authoring-20260912-174333-f634629d7b9b444189b96906ec2b0fb8/report.json`)。既存VRM0/1 playback、source package Save/Open、import失敗保護、rig graph回帰を確認。paletteの実マウス操作・任意実素材・画像目視の受入ではない。
- Workbench source skin display: Core **390 passed / 0 failed** (`Logs/core-source-graph-display.txt`)。評価済みgraphのSkinDeform前入力へsource paletteを適用し、取込直後のrest表示、authored pose変更、stale/legacy拒否を確認。
- Windows-SourceGraphDisplay build **PASS** (`Logs/build-player-source-graph-display.txt`、Unity `Logs/build-player-20260912-175730-836.log`)、Player **PASS** (`Artifacts/Authoring-20260912-175754-c5f5eff9c46646e5b075718e7323c699/report.json`)。取込直後と揺れ中の頂点をsource palette結果と比較し、VRM0/1の保存/Open、再利用mesh/object、失敗保護を回帰確認。実マウス操作・任意実素材・画像目視の受入ではない。
- GLB scene inventory: Core **393 passed / 0 failed** (`Logs/core-scene-inventory.txt`)。複数mesh resource、mesh instanceのnode/mesh/skin元index、nodeのsource-world transform、skin joint順を保持し、mesh/skin/nodeの明示型不正、範囲外参照、重複joint、skin-only nodeを拒否。
- Windows-SceneInventory build **PASS** (`Logs/build-player-scene-inventory.txt`、Unity `Logs/build-player-20260912-181457-233.log`)、Player **PASS** (`Artifacts/Authoring-20260912-181525-a0ffa6291c3a4931b99fc50b5d49c50e/report.json`)。既存VRM0/1取込・source palette表示・Spring再生・Save/Open・失敗保護を69 checksで回帰確認。inventoryは候補読取の自動検証で、複数対象のWorkbench公開、実マウス操作、任意実素材・画像目視の受入ではない。
- mesh/skin selection: Core **396 passed / 0 failed** (`Logs/core-final-396.txt`)。multi-mesh GLBを暗黙にmesh 0へ畳み込まず、選択したmeshのgeometry/morphとskinのsource indexを保持し、mesh index付きmorph identity、範囲外index、既存単一mesh APIの拒否を確認。
- Windows-MeshSelection build **PASS** (`Logs/build-player-mesh-selection.txt`、Unity `Logs/build-player-20260912-182005-738.log`)、Player **PASS** (`Artifacts/Authoring-20260912-182032-0b8589ed3d8a464f93b59c07f52942ff/report.json`)。既存WorkbenchのVRM0/1取込・source palette表示・Spring再生・Save/Open・失敗保護を回帰確認。選択候補の複数graph公開、実マウス操作、任意実素材・画像目視の受入ではない。
- Workbench mesh/skin selection GUI: Windows-ImportSelectionGui build **PASS** (`Logs/build-player-import-selection-gui.txt`、Unity `Logs/build-player-20260912-182741-785.log`)、Player **PASS** (`Artifacts/Authoring-20260912-182805-b75017ed69104ec5bcb4895c830547f7/report.json`)。自作multi-mesh fixtureで候補確認、mesh 1 / skin 0指定、取込後のsource payload/topologyを確認し、既存VRM0/1回帰を70 checksで実施。複数対象の同時公開、実マウス操作、任意実素材・画像目視の受入ではない。
- SIM-01 secondary-motion contract: Core **400 passed / 0 failed** (`Logs/core-secondary-motion.txt`)。`NYSM` v1のprofile／stable chain／fixed vertex／collider group往復、bone-poseとmesh-deformation出力の分離、unknown versionのopaque保持、skeleton/topology stale拒否、resolved springの共通topology移行、VRM1 sessionからのsource node→authored BoneId移行を確認。Windows-SecondaryMotionCore2 build **PASS** (`Logs/build-player-20260912-184902-690.log`)、Player **PASS** (`Artifacts/Authoring-20260912-184923-60fa1a94c49d4c67833fcb32277bdfdf/report.json`, 70 checks) で既存Workbench回帰も確認。native attachment、PhysBones/MagicaCloth2実adapter、VRM0 source-node移行は未接続。
- SIM-02 PhysBones target boundary: Core **404 passed / 0 failed** (`Logs/core-physbones-attachment.txt`)。`NYPP` v1のroot/endpoint/exclusion/branch/collider/limits/curve/interaction DTO往復、stable bone・stale skeleton拒否、unknown versionのopaque保持、SDK capabilityとloss reportのsupported/unsupported/warning分離、schema 4 PhysBones attachmentのbyte/hash一致Openを確認。Unity SDK component writer、実VRChat SDK/アバター動作、実操作・画像目視の受入は未確認。Windows-SecondaryMotionPhysBonesAttachment build **PASS** (`Logs/build-player-20260912-190938-424.log`)、Player **PASS** (`Artifacts/Authoring-20260912-190959-488cdb25d1124cb0b86f86853b0891c2/report.json`, 70 checks)。

- GUI完全source接続: Windows-SourceSkinImport build **PASS** (`Logs/build-player-20260912-170004-544.log`)、Player **PASS** (`Artifacts/Authoring-20260912-170036-ad7444ce32a14d94b4b57c23c2f86ff7/report.json`)。VRM0/1で元GLBとNYFS一致、再生中Save、生成fixtureの原本パスを移動後にOpen、完全payload維持と制作姿勢復元を確認。import失敗保護も合格。実マウス/任意実素材の受入ではない。今回はCore変更なしでCore suiteを再実行していない。

- rig session v4: Core **375 passed / 0 failed** (`Logs/core-rig-v4.txt`)。実GLB由来sourceをWithSourceSkinで接続しnative Save/Open後のNYFS byte一致、旧v1〜v3の不明値維持、異source hash/骨集合/不正base64/nullの拒否を確認。GUI取込での生成はまだv3経路。
- Windows-RigV4 build **PASS** (`Logs/build-player-20260912-165809-522.log`)。GUI生成接続前のため今回Player suiteは再実行していない。

- SourceSkinCodec (NYFS v1): Core **374 passed / 0 failed** (`Logs/core-source-skin-codec.txt`)。GLB由来の完全matrix・bind・slot・children順の往復、write/read/write byte同一、省略/明示identity、切断/末尾/巨大count/不正basis/versionを検証。project attachment/GUI保存への接続は未完了。次はrig session/native版更新と移行。
- Windows-SourceSkinCodec build **PASS** (`Logs/build-player-20260912-165437-619.log`)。独立codec追加のためPlayer GUI suiteは今回再実行していない。

- GLB source skin reader: Core **371 passed / 0 failed** (`Logs/core-glb-source-skin.txt`)。実GLB containerのoffset付き一般bind、余剰entry、複数skin指定、省略identity、不正範囲/型/参照・sparse/外部buffer拒否を確認。native/geometry adapterは未接続。
- Windows-GlbSourceSkin build **PASS** (`Logs/build-player-20260912-164909-392.log`)。Core reader追加のためPlayer GUI suiteは再実行していない。

- SourceSkin候補型: Core **369 passed / 0 failed** (`Logs/core-source-skin.txt`)。slot順・入力不変・余剰bind保持・回転/非一様scale/鏡映のbind相殺・省略identity・257骨・不正root/slotを確認。GLB decoder/native接続は未完了。
- Windows-SourceSkin build **PASS** (`Logs/build-player-20260912-164501-118.log`)。今回の変更は独立したCore候補型のためPlayer GUI suiteは再実行していない。

- I04-A node読取: Core **366 passed / 0 failed** (`Logs/core-node-transforms.txt`)、Windows-NodeTransforms build **PASS** (`Logs/build-player-20260912-163727-416.log`)。reader単体の確認で、一般mesh/skin取込や保存の接続は未完了。
- I04-A数値基盤: Core **363 passed / 0 failed** (`Logs/core-source-affine.txt`)、Windows-SourceAffine build **PASS** (`Logs/build-player-20260912-163153-722.log`)。一般GLB取込/Player実素材の受入は未実施。
- 実装基準 `4abd9d9`。Core **360 passed / 0 failed**: `Logs/core-vrm0-preview.txt`（前段の実行結果）。Windows-Vrm0Playback Player **PASS**: `Artifacts/Authoring-20260912-160736-8e0e5bae1c24440082b9c84dd1b27fc4/report.json`。今回の仕様整理ではCore/Playerを再実行していない。
- T03: `Tools/Inspect-BlenderImport.py`をBlender 4.4.0で実行。詳細 `private/import-inspection/20260912-inventory.json`、3入力の存在/SHA256一致を再確認。素材・派生モデルの保存/出力はしていない。
- 取込パネルに残っていた「VRM0揺れ未対応」の古い説明を訂正し、説明を折り返すよう修正済み。Windows-ImportHelp build **PASS**: `Logs/build-player-20260912-161814-007.log`。説明修正のbuild結果であり、新しいimport機能や文字の目視受入を意味しない。

- 仕様整理: 設計v2/現行コードとの独立レビューを実施し、Blender任意依存と既知VRM内の未対応フィールドの保持条件を反映。関連6文書のローカルリンク112件が有効、調査スクリプトの構文検査が成功。private報告のignoreを確認。

## 実装と検証の履歴

以下は記録当時の状況。「次」「未完了」は当時の記述を含む。最新の状態・着手順は冒頭の表を使用する。

### I04-A: node transform読取と階層合成（2026-09-12）

- `GlbNodeTransformReader`にTRS/matrix decodeとchildren検査、`SourceNodeTransforms`に全local/worldの不変保持と反復合成を分離。SourceHashと元children順を保持し、通常nodeを省略しない。既存の階層検査を共用する。[契約](docs/Source-Affine.md)。
- Core **366 passed / 0 failed**: `Logs/core-node-transforms.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-d806dc6f63dc4921b688dd528efbaac0`。実GLBのmatrix/TRS・親配列順・4096段・旧profileとの原点一致、不正/曖昧入力の拒否を確認。
- Windows-NodeTransforms build **PASS**: `Logs/build-player-20260912-163727-416.log`。今回Player GUI suiteは再実行していない。一般mesh/skin importやnative保存は未接続。
- 次は一般inverse-bindを含むsource skin候補、完全な変換情報の保存と旧形式移行。GLB取込の既存translation-only制限は外さず、I04-A全体と実素材受入は未完了。

### I04-A前段: source affine数値モジュール（2026-09-12）

- `SourceAffine`へcolumn-major/TRS、parent×local、逆行列、Point/Vector/Normal/Tangentの変換を分離。double計算、入力不変、finite float出力、鏡映handednessと不正基底の診断を定めた。[計算と保存接続の契約](docs/Source-Affine.md)。
- Core **363 passed / 0 failed**: `Logs/core-source-affine.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-a8780732274b434e923d42a3b5ceede3`。非一様TRS、inverse-bind相殺、shear/鏡映、法線/接線、不正入力、小さいscaleを確認。
- Windows-SourceAffine build **PASS**: `Logs/build-player-20260912-163153-722.log`。数値モジュール追加のためPlayer GUI suiteは今回再実行していない。
- 次はnode JSON decode、完全local/world変換、一般inverse-bindの候補と保存移行。旧sessionの原点を完全な一般transformと捏造しない。既存translation-only制限を維持し、I04-A全体は未完了。

### T02: VRM0実行所有者と共通再生GUI（2026-09-12）

- `Vrm0SpringRuntime`へ設定・通常nodeのcenter/collider変換を分離し、`Vrm0SpringPreview`が固定stepとskin投影を所有する。Advanceはcontroller候補で計算し、skin投影成功後に公開。旧状態を失わない。colliderの形状検査は既存adapterを共用。Workbenchは`IVrmSpringPreview`で両形式を切替。[契約](docs/VRM0-Spring-Playback.md)。
- Core **360 passed / 0 failed**: `Logs/core-vrm0-preview.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-df8da434d3c544b0a89b385f7ba153ad`。合成VRM0 reader/session→owner、通常nodeのcenter/collider、重力Z、12step、停止/再開、外部姿勢更新、時間不正/scale変更時の状態保持とResetを確認。
- Windows-Vrm0Playback build **PASS**: `Logs/build-player-20260912-160702-715.log`。Player suite **PASS**: `Artifacts/Authoring-20260912-160736-8e0e5bae1c24440082b9c84dd1b27fc4/report.json`。VRM0/1両方で表示変化・Mesh/Object再利用・編集点復元・停止/再開・Save/Openで一時姿勢非保存・編集時破棄のhandler検証を通した。実クリック/実素材/画像目視は未受入。
- T02は対応profileの接続完了。次はT03→必要なI04→T05。256実行骨の予算、translation-only取込、base pose時点のcollider snapshot、固定avatar座標、0.25秒超frameでの停止を制限として明示する。揺れるnodeに付いたcolliderの逐次更新や可動world rootはI03-Bの比較/拡張残件。C0〜C5全体は未完了。

### T02: 通常nodeを含む一時骨格・skin姿勢の往復（2026-09-12）

- `ImportedPreviewRig`へ必要なsource node/skin joint/全祖先から一時骨格を作る責務と、制作poseとの相互変換を分離。skinのBoneIdを維持し、通常nodeは決定的IDを持つ。source原点とinverse-bind Headの差を両方向へ補正。元graph/skin/skeletonは変更しない。[契約](docs/Imported-Preview-Rig.md)。
- Core **359 passed / 0 failed**: `Logs/core-preview-rig.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-799f80a8863c4dc38d01556bbda60956`。回転姿勢往復・必要祖先・無関係枝除外・ID再現・stale拒否を確認。非joint helperを12stepシミュレーションして子skin骨へ回転を投影し、元姿勢を保持した。
- Windows-PreviewRig build **PASS**: `Logs/build-player-20260912-160115-422.log`。Coreの姿勢変換追加で、今回Player GUI suiteは再実行していない。実素材/実操作の受入は未実施。
- 次はVRM0展開targetから実行chainを作り、この一時骨格のcenter/collider変換、固定step所有者、Workbenchへ接続する。呼出側は展開targetとcenter/collider nodeを必要集合へ含める。必要node+祖先が実行Coreの256骨予算を超える場合は明示拒否。T02全体は未完了。

### T02前段: VRM0 source subtree展開（2026-09-12）

- `Vrm0SpringExpansion`を独立adapterとして追加。root/children順で全子孫（非jointを含む）を展開し、最初の子または親からの方向へ7cm延ばした仮想末端を保持。元設定・center/collider参照を残し、重複subtreeや方向不明の末端は明示拒否する。[契約と公式根拠](docs/VRM0-Spring-Expansion.md)。
- Core **357 passed / 0 failed**: `Logs/core-vrm0-expansion.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-9975b1e36afb460abcd75c44d75d6d54`。分岐順・全子孫・仮想末端数値・設定継承・拒否条件、合成VRM0 reader→rig codec→Resolveで非joint末端まで確認。
- Windows-Vrm0Expansion build **PASS**: `Logs/build-player-20260912-155712-181.log`。今回はCore adapter追加で、Player GUI suiteは再実行していない。実素材受入は未実施。
- T02は継続中。次は通常nodeを含む実行骨格/poseとsource mappingの接続。揺れの実行、center/collider、skinへの投影、Workbench所有者とGUIは未接続。VRM0を再生可能とは表示しない。

### T01: 全source階層の保存（2026-09-12）

- ImportedSourceHierarchyに全nodeの親・元children順・source rest原点を保持し、検査とtranslation合成を反復処理へ分離。4096node上限、循環/不正親/子の欠落・重複・原点不一致を拒否する。JSON処理はImportedSourceHierarchyJsonへ分離し、rig session v3へ保存。v1/v2では階層を推定しない。[契約](docs/Imported-Source-Hierarchy.md)。
- Core **354 passed / 0 failed**: `Logs/core-source-hierarchy.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-10d4a7cce4cf490e9daddf391e126fa7`。非joint末端/分岐とchildren順のnative往復、旧形式移行、壊れたpayload、4096段の計算を確認。
- Windows-SourceHierarchy build **PASS**: `Logs/build-player-20260912-154816-246.log`。Player suite **PASS**: `Artifacts/Authoring-20260912-154853-fa716e2a8d8a4c9faf552796ca1e7869/report.json`。VRM0/1取込Save/Openの全node保持をhandler検証へ追加。実素材・実操作・画像の目視受入は未実施。
- 次はT02のVRM0 root/末端展開と通常nodeの実行対応。データ保持だけでVRM0再生や一般transform取込を完了扱いにしない。I03-B全体、I04、A01、C0〜C5の残件は維持する。

### I03-C: 再生projectionの再利用（2026-09-12）

- `SpringMeshBuffers`へ再利用するposition/normal/tangent/Points配列とbounds検査を分離。同じtopology・transform・属性数・UV・材質ならUnity Mesh/GameObjectを保持し、頂点データだけ更新する。構成変更時は通常のprojection再構築へ戻す。
- 再生中は編集点バッチを作らず、終了/Resetで編集中の表示と編集点を復元する。表示が初期姿勢と一致していても復元を省略しない。graphは従来どおり一時評価し、制作文書へは書かない。
- Windows-SpringMeshReuse build **PASS**: `Logs/build-player-20260912-153043-091.log`。初回suiteは120秒で時間切れ (`Artifacts/Authoring-20260912-153122-17d1650d16ca40148eca6e10794714d8/player.log`)。検証process終了後にTimeoutSeconds=300で再実行し、Player suite **PASS**: `Artifacts/Authoring-20260912-153336-c66834f83e614fecb6278247149ea256/report.json`。Core変更なし、直近353件合格を参照。
- 専用Player検証へ12stepのMesh/Object同一性、表示頂点変化、再生中の編集点非表示とReset復元を追加。[再利用契約](docs/Workbench-Spring-Playback.md)参照。大規模モデルの速度/メモリ測定は未実施。

### I03-C: Workbench再生GUIと一時表示（2026-09-12）

- `AuthoringWorkbench.SpringPlayback`に折りたたみの再生/一時停止/リセットを追加。`OwnedMeshProjection.Spring`で一時graph評価の表示を分離する。保存graph/attachments/Undoへ揺れたposeを書かず、編集・作品・metadata・stage変更時はownerを破棄する。
- 新規Player検証で、取込後のprojectionが更新されない問題を検出（`Artifacts/Authoring-20260912-152432-0b03c51c51434b669e0a26b8f82e97e8/report.json`）。取込AddGraph commandへprojectionを渡し、成功した取込を即表示する共通経路へ修正。
- Windows-SpringPlaybackUi build **PASS**: `Logs/build-player-20260912-152520-212.log`。Player suite **PASS**: `Artifacts/Authoring-20260912-152603-441f4a62fa4d48e4bd284d07396ee2f1/report.json`。自作VRM1の取込→12stepで表示頂点変化、graph/metadata不変、停止/再開/リセット、Save/Openで一時姿勢非保存、編集時破棄を専用handler検証で確認した。
- [Workbench再生契約](docs/Workbench-Spring-Playback.md)。今回Coreは未変更で直近353件合格を参照。実クリック、文字の隠れ、実アバター、負荷は未受入。大きなmeshでの一時graph評価/projection再構築コストを今後確認する。
- 次はVRM0 root/末端展開と、実素材に必要な一般node変換を進め、対応profileでWindows実操作受入を行う。I03-B/C全体、I04、A01とC0〜C5の製品目標は未完了部分を維持する。

### I03-B/C: VRM1 preview所有者への統合（2026-09-12）

- `Vrm1SpringPreview`がsourceを固定して実行chainとcontrollerを所有する。`VrmSpringCenterAdapter`へ元node原点からのcenter変換を分離。Advanceごとに現在poseのcollider/centerを解決し、scale変更は明示Resetを要求する。候補の生成に失敗したResetでは旧previewを保持する。
- Core **353 passed / 0 failed**: `Logs/core-vrm-preview.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-ea39c6e0c6d84f37800dd1d92f5dba23`。自作VRMファイル→reader→session codec→owner→再生、停止中center移動と再開、collider pose更新、scale変更拒否/Reset、stale骨格での旧状態保護を確認。
- Windows-VrmPreviewOwner build **PASS**: `Logs/build-player-20260912-151924-482.log`。Core所有者の追加のためPlayer GUI suiteは今回再実行していない。実モデル受入は未実施。
- [VRM1 preview所有者](docs/VRM1-Preview-Owner.md)。次はWorkbenchでbase poseを取得し出力Poseを一時表示する接続と再生/停止/リセットGUI。作品・session変更時の破棄、VRM0展開、一般node transform、実素材受入も未完了。I03-B/C全体の完了にはしない。

### I03-B/C前段: 固定step再生controller（2026-09-12）

- `SpringPreviewController`へState/center/Pose/端数時間/再生状態の所有を集約し、`SpringFixedClock`へ1/60秒の時計を分離。停止中は壁時計を蓄積せず、描き直しは履歴をcommitしない。全substep成功後だけ新状態を公開し、失敗時は再試行可能な旧状態を保つ。
- floatの境界誤差で1step不足する新規ケースを検出（`Logs/core-spring-preview.txt`、349件合格/2件失敗）。1e-7秒の境界許容差を設け、修正後Core **351 passed / 0 failed**: `Logs/core-spring-preview-final.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-a6ba63c364464e1c816aafad54b2a7be`。
- 0.2秒の一括/分割一致、停止中のcenter移動と再開、Reset、衝突失敗・不正時間・不正centerでの全状態保持と再試行を検証。[controller契約](docs/Spring-Preview-Controller.md)参照。
- Windows-SpringPreviewCore build **PASS**: `Logs/build-player-20260912-151436-987.log`。Core controller追加のためPlayer GUI suiteは今回再実行していない。実モデル受入は未実施。
- 次はVRM sessionのcenter/colliderを毎frame解決し、graph base poseとpreview表示へ接続する所有者/GUI。VRM0展開、設定変更reset、一般node transform、実素材受入も未完了。I03-B/C全体は引き続き未完了。

### I03-B: VRM1実行chainと参考時間式（2026-09-12）

- `Vrm1SpringRuntimeAdapter`がtopology解決を共用し、元head/tail原点をCoreの明示offsetへ変換する。radiusへ一様scaleを適用し、stiffness>1も保持。重力情報不足・未対応scale・予算超過は拒否する。
- `VrmSpringIntegration`へVRM参考式の時間計算を分離し、既存Authoring modeは維持。modeはchain hash version4へ含む。VRM modeのdragはstep単位で、再生controllerでは固定stepを管理する。native/session schemaは変更しない。
- Core **348 passed / 0 failed**: `Logs/core-vrm-runtime-chain.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0706310605cb42df84fd619b3aed785f`。参考式の解析値、元重力ベクトル・stiffness25保持、session→実行chain→12stepの中心固定/Pose/State一致を確認。
- Windows-VrmRuntimeChain build **PASS**: `Logs/build-player-20260912-151110-601.log`。Core adapter/計算変更のためPlayer GUI suiteは今回再実行していない。実モデル受入は未実施。
- [実行chain・時間契約](docs/VRM-Spring-Dynamics.md)。次はcenter/固定step/失敗時状態を所有するcontrollerとVRM0展開。I03-B全体、I03-CのGUI、一般node transform、実素材受入は未完了。参考アルゴリズムと全runtimeの挙動同一性を保証しない。

### I03-B前段: 元node原点を回転中心に保持（2026-09-12）

- `RestHeadOffset`でCoreの回転中心を明示可能にし、`SpringJointTarget`でhead/tailと長さの検査を共用する。回転後のtranslationを補正してhead位置を維持し、bone Headとsource原点を同一視しない。未指定は従来どおりゼロ。
- Core **347 passed / 0 failed**: `Logs/core-spring-pivot.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-d2c8fd00c7c44524a0a84a8677d4e38a`。2倍scale、ずれた原点、12stepのhead固定・tailと子joint一致・長さ、head変更時のstale state、明示/暗黙先端との一致拒否を検証。
- Windows-SpringPivot build **PASS**: `Logs/build-player-20260912-150727-272.log`。Core計算変更のためPlayer GUI suiteは今回再実行していない。実モデル受入は未実施。
- [明示先端契約の回転中心拡張](docs/SpringBone-Endpoints.md)参照。計算hash version3へheadを含めたが保存schemaは変更しない。次はVRMの元設定からhead/tailと力を持つ実行用chainを構成する接続。I03-B/C、VRM0展開、runtime所有、再生GUI、実素材受入は未完了。

### I03-B: VRM1 chainのtopology解決（2026-09-12）

- `Vrm1SpringChainResolver`へVRM1 joint列の祖先/重複/center/group検査を分離し、`VrmSpringChainBinding` / `VrmSpringPairBinding`へ不変の結果を保持する。末尾jointはtail専用で、headの設定を各pairへ保持。中間jointを飛ばす場合も重複範囲に含める。
- Core **346 passed / 0 failed**: `Logs/core-vrm-chain.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-12e506fb725147b9bd716b181049a993`。codec往復、pair数、元node原点とbone Headの差、stiffness>1保持、祖先/重複/center/短いchain/未対応node/source不一致を検査。
- Windows-VrmChain build **PASS**: `Logs/build-player-20260912-150445-722.log`。Core resolver追加のためPlayer GUI suiteは今回再実行していない。実VRM受入は未実施。
- [VRM1 chain契約](docs/VRM1-Spring-Chain-Binding.md)。戻り値は元設定を保持するtopologyであり、実行可能なCore chainではない。I03-B全体は未完了。次はVRM0 root/末端の展開、元node原点を回転中心へ反映する接続、重力/時間の係数契約を実装する。runtime/GUIと実素材受入も未完了。

### I03-B前段: head/tail pairを受け取るCore先端（2026-09-12）

- `SpringBoneJointSettings.RestTailOffset`で表示用bone Tailと計算用先端を分離し、`SpringJointTarget`を初期stateとStepで共用。明示offsetから長さ・回転・衝突を計算する。未指定は既存のbone Tailを維持する。
- Core **345 passed / 0 failed**: `Logs/core-spring-endpoint.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-74112fb0d4174dfba6c403f12717c1a6`。2倍scale、表示Tailと異なる先端、非simulated中間骨と末尾joint、球衝突を12step検証。設定変更時のstale stateとゼロ/非有限offsetを拒否。
- Windows-SpringEndpoint build **PASS**: `Logs/build-player-20260912-150050-221.log`。Core変更のためPlayer GUI suiteは今回再実行していない。実アバター受入は未実施。
- 計算用chain hashはversion2でoffset指定有無/値を含む。native/sessionの保存schemaは変更しない。[明示先端契約](docs/SpringBone-Endpoints.md)。VRM chainの解決自体は次の工程で、I03-B全体は未完了。VRM1末尾jointを回転対象にしない展開、VRM0末端、center祖先検査、重力/時間を引き続き接続する。

### I03-B前段: center履歴追従（2026-09-12）

- `SpringCenterFrame`がsimulated boneごとのcenter変換を不変保持し、`SpringCenterMotion`が前回/current tailを両方現在centerへ移す。設定・skeletonの一致と可逆基底を検査し、state/timeは入力を変更せず保持する。異なるchainのcenterを混ぜない。
- Core **343 passed / 0 failed**: `Logs/core-center-motion.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-92f48ebeaacf43d9ba2872458833b83d`。複数center、移動/回転/scale、履歴両点、逆変換、停止/再開後の実Step、入力独立性・不正frame拒否を確認。
- Windows-SpringCenter build **PASS**: `Logs/build-player-20260912-145755-883.log`。Core計算追加のためPlayer GUI suiteは今回再実行していない。実マウス・実アバター受入は未実施。
- [center契約](docs/SpringBone-Center-Motion.md)にruntime所有者のcommit順と重力との分担を記録。I03-Bは未完了。次はVRM1のhead/tail pair（末尾jointは回転対象にしない）とVRM0 root/末端展開、center参照の祖先検査、設定値/時間契約を統合する。runtime/GUIと実素材受入も未接続。

### I03-A: collider座標adapter（2026-09-12）

- `VrmSpringColliderAdapter`へsource座標から現在poseへのsphere/capsule変換を分離し、`ImportedNodeSpace`を共用する。VRM0の拡張offsetは標準出力のZ反転を吸収し、VRM1は元glTF node-local値を使う。source/skeleton、形状詳細、未対応nodeを検査し、groupとcolliderの順序・重複を保持する。
- `PoseUniformScale`へ球/カプセルの半径scale検査を分離。直交した一様scale（鏡映含む）だけを受け入れ、非一様scale/shearは診断する。Coreの半径/件数予算は無言で切り詰めない。
- Core **340 passed / 0 failed**: `Logs/core-collider-adapter.txt`、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-3dec6b4cbbd7497fa948f799bf9f91ea`。session codec往復、原点とbone Headの差、90度回転・2倍scale・鏡映、sphere/capsule、未対応入力を検証した。
- Windows-ColliderAdapter build **PASS**: `Logs/build-player-20260912-145343-529.log`。Core adapter追加のためPlayer GUI suiteは今回再実行していない。実VRMの見た目受入は未実施。
- 契約と公式実装の根拠は [collider adapter](docs/VRM-Collider-Adapter.md)。これはsnapshot生成までで、再生previewは未接続。次はI03-Bのchain/center/重力/時間契約、I03-Cのruntime所有とGUI。一般node変換と実素材受入も未完了。

### R12: 取込候補と状態公開の分離（2026-09-12）

- `AuthoringWorkbench.ImportPublication.cs`へ候補の所有と公開を分離。rig/expression/Spring sessionとattachment bytesをgraph追加前に検査・準備する。追加command成功後に共有フィールドと表示を反映する。expressionの既存「未対応mappingを警告して未設定とする」動作は維持する。
- `ImportFailureVerification`は不正skinを空projectおよび取込Undo後に読み込み、文書/attachments/session参照・表示・dirty・Undo/Redo可否が不変であることを確認する。duplicate graphを実commandへ渡す拒否経路と、有効ファイルによる再試行も対象。
- Windows-ImportIsolation build **PASS**: `Logs/build-player-20260912-144923-727.log`。変更はUnityRuntimeのみで、Coreは前段R11の338件合格を参照し今回再実行していない。最終Player suite **PASS**: `Artifacts/Authoring-20260912-144950-6cef15f6584c4e30b33ce5deb691d6be/report.json`。専用Import failure isolation検査を含む。実素材・実マウス受入は別途。

### R11実装の証拠（2026-09-12）

- `ImportedJointHierarchy`へ元node木からskin joint木への変換を分離。入力木の循環/複数親検査とtranslation合成は既存importerで先に完了させる。子tail候補はsource node index順で選び、skin slot登録順に依存させない。
- 修正前は既存334件合格・追加4件失敗 (`Logs/core-hierarchy-before.txt`)。修正後 **338 passed / 0 failed** (`Logs/core-hierarchy-after.txt`)。成果物: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4250aa56bf3a4229a762cce1797b61d9`。
- Windows-JointHierarchy build **PASS**: `Logs/build-player-20260912-144506-592.log`。今回はCore import修正のためPlayer GUI suiteは再実行していない。実マウス・実アバター受入も未実施。
- nodeを骨へ追加する変更ではなく、元joint間の祖先関係を保つ変更。非joint自体へのcollider追従、一般node回転/scale、再生previewは引き続きI03/I04で扱う。

## 過去レビューR01〜R10の修正・検証記録（2026-09-12）

ユーザー依頼「ここまでチェック」「チェック後current_task更新してタスク化」に対応したレビューを起点に修正を進める。詳細な再現条件、対象行、完了条件、証拠は [Rig / VRMレビュー](docs/reviews/2026-09-12-Rig-Vrm-Review.md) を参照する。R01/R02/R03/R09はCore/Windows自動検証まで完了し、次はVRM入力契約・mapping永続化。実マウス・実VRMの受入は各自動検証と区別する。

- [x] **R03 / P1 — 保存全体の成功判定と失敗時の保護**。schema 4の単一manifestで本体とVRM設定のblob参照を一括公開し、失敗時の旧作品・設定・dirty・versionを保持。Core共通保存service、Windows GUI/MCP handlerの再試行・終了防止・Save As/Openを検証した。外部MCP transport経由のmetadata専用試験と実マウスの受入は未実施。
- [x] **R01 / P1 — VRM1 authors配列**。文字列として扱うreaderと誤ったfixtureを修正する。複数作者をimportからsession保存・再読込まで保持し、既存形式の移行も定める。
- [x] **R02 / P1 — 同一nodeの複数コライダーの往復**。`nodes:[0,0]`を重複禁止readerで拒否する問題を直す。順序・件数を保持し、VRM0/1のimport→Save→Openを確認する。
- [x] **R09 / P2 — VRM1 SpringBoneの既定値**。省略stiffness/dragForceを1.0/0.5にし、明示0と区別する。session再読込でも一致させる。
- [x] **R04 / P1 — 連続stepのPose/State整合**。step2でtailが約0.079809 mずれる回転合成を直す。同じbase pose／変化するbase poseで、出力PoseとStateが連続して一致することを確認する。
- [x] **R05 / P1 — chainの親子変換伝播**。親tailと子headが約0.079807 m離れる問題を直す。親から子へ相対offsetを保って評価し、2〜3jointと子孫追従を確認する。
- [x] **R06 / P2 — chainごとの衝突参照**。全chainのgroup参照を混合せず、所属chainの参照だけをjointへ渡す。参照なしBが別chain Aの追加で約0.290426 m動く再現を回帰化する。
- [x] **R07 / P2 — 長さと衝突の同時制約**。押し出し後の長さ制約でsphere内へ戻る問題を直す。複数sphere、hitRadius、同軸例、解なし／反復上限の診断を確認する。
- [x] **R08 / P2 — 停止・再開と時間刻み**。dt=0でもtailが約0.079304 m動く問題を直す。物理履歴を停止中に進めず、固定step / 可変stepの契約と再開を検証する。
- [x] **R10 / P2 — 入力検査と実際の経路を通る回帰**。null collider groupをdomain errorで検出する。空groupの衝突テスト、初期Stateからの反復だけのテストを改め、R01〜R09の回帰を各修正と同時に追加する。保存失敗・終了防止はWindows Playerの専用検証も必要。

検証済み: このレビューでCore **297 passed / 0 failed**を再実行。別fixtureで作者情報拒否、session往復失敗、保存失敗後dirty=False、step2姿勢ずれ、親子gap、chain間衝突混入、貫通、dt=0の進行、null参照例外、省略値の相違を確認した。Player/Bridgeは今回再実行していない。実VRM全体・手動見た目受入も未確認。

最新の再開順は冒頭のタスク表を参照する。以下はR01〜R10とI01〜I03の実装履歴で、各節の「次」「未完了」は記録時点の状況を含む。R03の保存保護は自動検証範囲で完了。設定・状態／計算／衝突／import adapter／保存coordinatorを役割ごとのモジュールへ分ける。

### 実行単位と完了判定

- [x] **R01 + R02 + R09（取込・保存、自動検証完了）**。作者名の配列保持と旧session移行、コライダーnode列の重複保持、省略値と明示0の区別を同じ段階で直す。正規の合成VRM0/1を使うCore往復テストとWindows Workbench Save/Openを完了条件とする。
- [x] **R04 + R05（姿勢・階層、Core検証完了）**。連続stepのPose/State一致と親子変換を修正する。2〜3joint、変化するbase pose、登録順、非simulated子孫と初期offsetを回帰対象にする。
- [x] **R06 + R07 + R08（衝突・時間、自動検証完了）**。chain単位の参照分離、長さと衝突の同時制約、停止・再開の契約を修正する。解なしの診断、有限値、可変dtを含めて検証する。
- [x] **各実装に同梱: R10（検証強化、自動検証完了）**。修正前に失敗する再現を正式テストへ移す。保存・OpenはPlayer経路も通し、数値計算は前stepのStateを引き継ぎ、実際の衝突参照を指定する。null groupのdomain errorも確認する。
- [ ] **基礎修正後の受入**。実VRMの読込・保存・再読込、GUIの保存して終了、文字サイズ・隠れ、姿勢と見た目をWindowsで確認する。外部MCP transportのmetadata保存も別途確認する。各記録に対象buildと確認方法を残す。

状況照合時点では未修正だったR01/R02/R09を、下記の実装と自動検証で更新した。チェック済みは自動検証範囲であり、実素材と実マウスによる受入は別タスクのまま維持する。

修正後は、未保持のgravityDir・collider shape値と保存移行を含むVRM入力契約を整え、node→stable BoneId、preview接続へ進む。一般node transform、skin/morph出力、実アバター受入、C1〜C5の全体目標は維持する。

### I03座標基盤: 元node原点の保存と変換（2026-09-12）

- GLB skin取込で親translationを合成したSourceNodeOriginsを保持する。inverse-bind由来のbone Headと同一視しない。ImportedRigSession v2へ保存し、v1読込はorigin不明として保持する。旧mapping自体は引き続き利用可能。
- `ImportedNodeSpace`へsource-local→source-rest→bone-local→posed avatarの変換を分離。source/skeleton/graphの検査と、origin不足・未対応nodeの拒否を含む。旧作品のGUIには元node座標不足を表示する。
- Core **334 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-a40c1a16d57e487083686e63ee83d0bb`。元node原点とinverse-bind Headが異なるfixtureをnative保存/Openし、移動/90度回転後の解析値と比較。v1移行で不明を維持、欠落node原点・未対応nodeを拒否した。
- 契約は [元node空間](docs/Imported-Node-Space.md)。次はVRM0/1の座標表現をsource-localへ変換するcollider adapter、半径scale、center空間と重力/時間設定を接続する。非joint nodeや一般node回転/scale、再生GUIは未完了。

- Windows-NodeSpace build **PASS**: `Logs/build-player-20260912-143434-782.log`。Authoring suite **PASS**: `Artifacts/Authoring-20260912-143546-e00b3bf1383140b0a3d863c045a4e98d/report.json`。VRM0/1のnode原点を保持してSave/Openし、local offsetからの変換結果を確認。実素材preview・実マウス受入は未実施。

### I03前段: capsule衝突コア（2026-09-12）

- `SpringBoneCollider`に任意のTailを追加し、sphere/capsuleを同じ不変型で保持する。`SpringColliderGeometry`へ最短軸点・距離を分離し、solverの各passと全形状の最終検査で使う。長さ0はsphereと等価。
- 拡張前は新規中央接触ケースが失敗（`Logs/core-capsule-before.txt`、332 passed / 1 failed）。拡張後はCore **333 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f9ae8ff572dc4be69db33617c2529b4c`。中央/端/ゼロ長、sphere混在、hitRadius、長さ・Pose/State一致、有限値・決定性を確認した。
- bounded探索と未収束診断はsphereと共通。契約は [制約solverのCapsule拡張](docs/SpringBone-Constraints.md)。実VRMのoffset/tailからavatar座標へ変換するadapterは未接続。
- 次は元nodeのrest座標と骨格rest座標の関係を保持し、source-local offsetを正しく変換する。inverse bindから得たbone headを元node座標と無条件に同一視しない。その後、center追従・時間パラメータ変換・previewの再生/停止/リセットを接続する。I03全体は未完了。

- Windows-CapsuleCore build **PASS**: `Logs/build-player-20260912-143007-334.log`。今回の変更はCore計算のみで、Player GUI suiteは再実行していない。実VRMのcapsule見た目受入も未実施。

### I02: 取込骨対応のsnapshot保存（2026-09-12）

- `ImportedRigSession` / Codec v1へsource/skeleton hash、GraphId/SkeletonNodeId、node/BoneIdとhumanoid対応を保存する責務を分離。許可attachmentにrigを追加して最大3件とし、既存の単一manifest公開・失敗保護を共用する。
- GUI skin取込でsessionを所有し、Open前にsource整合性を確認する。骨格変更時は取込パネルへstaleを表示し、対応解決を拒否する。Undoで元骨格へ戻れば対応も再び有効になる。静的mesh取込は古いrig情報を継承しない。
- Core **330 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-427a087e42fc48f997e750c19f00a55f`。session/native往復、source/graph/skeleton不一致拒否、未知field/重複拒否、rig blob失敗時の旧snapshot維持と再試行を確認。
- 契約は [取込骨対応の保存](docs/Imported-Rig-Sessions.md)。旧NyaForgeはrig attachment付き作品を開けない。元ファイルのない旧作品から対応を推測しない。I03の一般node/center/collider座標・時間変換とpreview接続は未完了。

- Windows-RigSession build **PASS**: `Logs/build-player-20260912-142438-076.log`。Authoring suite **PASS**: `Artifacts/Authoring-20260912-142507-e3f1bb9d5e044c76978b7b06987b1176/report.json`。同一VRM0/1のmapping復元、骨格編集時stale表示、Undo復帰、rigを含む3種類のblob失敗保護とGUI/MCP handler再試行を確認。実マウス・実アバター受入は未実施。

### I01: Spring詳細を欠落なく保存（2026-09-12）

- `VrmSpringColliderShape`と`VrmSpringDetailJson`へtyped shapeとJSON vector検査を分離。重力方向、sphere/capsuleのoffset/radius/tailを元の座標系で保持し、VRM1の省略値とVRM0の不明値を区別する。
- Spring sessionはversion 3 writer、version 1/2/3 reader。旧sessionで失われた方向・形状はnullで保持し、再書出し時にも推測値で埋めない。expression v2・project schema4は維持。Workbenchは詳細不足を表示する。
- Core **328 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-14f6fee4f70e4a5eb99411298cdc29f9`。VRM0/1の非default詳細・codec往復、旧v1/v2移行、不明の保持、default capsule、vector不正・件数不一致・sphere tail拒否を確認。契約と出典は [Spring詳細](docs/VRM-Spring-Details.md)。
- 元VRM0の省略vectorは不明として残る。HasCompleteDetailsは設定値の充足であり、runtime対応の保証ではない。I02のmapping永続化、I03の座標/時間変換・capsule物理・previewは未完了。

- Windows-SpringDetails build **PASS**: `Logs/build-player-20260912-141632-673.log`。Authoring suite **PASS**: `Artifacts/Authoring-20260912-141755-08f376dbcbac4e2f8f8c670351f75395/report.json`。同じVRM0/1の詳細値取込→Save/Openと、旧sessionの不明値維持を検証した。詳細不足表示の実マウス/見た目受入は未実施。

### R10完了: 同じVRMの取込→Workbench保存→Open（2026-09-12）

- `VrmVerificationFixture`は第三者素材を含まない合成GLB/VRM bytesを生成する。skin、morph、表情、VRM0同一node複数sphere、VRM1同一/異なるnodeとcapsule inventoryを含む。完全な製品アバターのVRM仕様適合fixtureではなく、取込対応profileを通す検証データ。
- `AuthoringWorkbench.VrmImportVerification`は実ファイルへ書出し、GUIの`ImportModel`→`TrySaveProject`→空workspace→`OpenProject`をVRM0/1それぞれで実行。graph hash、attachment hash、source identity、作者列、表情weight、collider node列、VRM1省略値/明示0を確認した。
- Windows-VrmImportRoundtrip build **PASS**: `Logs/build-player-20260912-140900-452.log`。Authoring suite **PASS**: `Artifacts/Authoring-20260912-140928-5c88b52461154cfe828d5a6bb9cc23cf/report.json`。専用の`VRM0/1 file import to Workbench graph...` checkが成功。追加検証はUnityRuntimeのみでCore本体は変更せず、Coreは前段の326件合格記録を維持。
- [検証範囲の照合](docs/reviews/2026-09-12-Repair-Coverage.md)で残っていたR02/R10の分断された経路を埋めた。実ファイルpickerのマウス操作、実VRMの見た目、外部MCP transportのmetadata専用受入は別タスクのまま。R01〜R10の終了はC0〜C5全体の完成ではない。

### 次の実装単位: VRM入力契約とpreview接続

- [x] **I01 — Spring入力の詳細保持（対応profileの自動検証完了）**。gravityDirとsphere/capsuleのoffset・radius・tailをtyped metadataへ保持する。旧sessionは不明値を捏造せず、移行方針と再取込の必要性を定める。Core solverのsphere対応とVRM capsule対応は区別する。
- [x] **I02 — 骨対応の保存（Core/Windows自動検証完了）**。ImportedBoneMap/humanoid bindingをnative保存へつなぎ、同一snapshotで保存・Openできるようにする。source/skeleton hashとnode参照を検査し、骨格編集時のstaleを明示する。
- [ ] **I03 — runtime preview**。VRM node→BoneId、centerとcollider座標系、VRM設定の時間的意味をadapterで変換する。未対応node/shapeを黙って除外しない。Workbenchの再生・停止・リセットは保存/Undoとは分離して接続する。

### VRM骨対応adapterと修正範囲の照合（2026-09-12）

- `ImportedBoneMap`はGLB skin取込時のnode→BoneId割当をsource/skeleton hashとともに不変保持する。`VrmHumanoidBinding`はVRM0/1のsemantic→nodeをstable BoneIdへ変換し、違うsource・変更された骨格・skin外nodeを拒否する。骨名やskin slotから推測しない。
- Core **326 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-bc76ad1859954e019babddfabf03ca08`。同名2骨、node番号とskin slotのずれ、VRM0/1、weight参照、再取込、骨格codec往復、誤った対応の拒否を確認した。
- BoneMapのnative保存/Open復元とGUIへの接続は未実装。保持中のmapを骨格codec往復後に再利用できる検証と、map自身の永続化を混同しない。詳細は [取込骨対応](docs/Imported-Bone-Mapping.md)。
- [修正後の検証範囲](docs/reviews/2026-09-12-Repair-Coverage.md)へR01〜R10の正式回帰と未確認境界を照合した。R10の残件として、VRM0/1それぞれの同一fixtureをファイル取込からWorkbench保存・Openまで通す専用検証を追加する。既存のCore import検査とPlayer session往復は別fixtureである。
- `AuthoringWorkbench.SpringVerification`を追加し、Player環境で連続12stepのPose/State、親子追従、sphere距離、交互dtと停止state保持を数値検査する。Springの表示GUIや実素材受入は未実装/未確認のまま。

- Windows-BoneMap build **PASS**: `Logs/build-player-20260912-140337-843.log`。Authoring suite **PASS**: `Artifacts/Authoring-20260912-140446-45def4a5183349bbbc3e4158e4f8498d/report.json`。専用Spring Core in Player checkも成功。実アバター見た目受入は未実施。

### R08: 停止と可変時間積分（2026-09-12）

- `SpringTimeIntegration`へ慣性・stiffness・重力・減衰の予測を分離。stateが前回の正の時間幅を保持し、今回との比率で慣性変位を補正する。初回は静止、不等間隔Verletの重力項を使う。
- dt=0は同じstateを返し、物理履歴を進めない。表示だけは編集されたbase poseへ再投影する。表示と物理状態の停止中の違い、dragForceの1/60秒基準、離散近似の限界は [時間契約](docs/SpringBone-Time.md) を参照する。
- 修正前は停止の追加2テストが失敗（`Logs/core-spring-time-before.txt`、既存318件合格）。修正後はCore **323 passed / 0 failed**、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-a588d8e40e564566b844541e4d15d8c6`。停止反復・再開、停止中編集、可変時間の慣性/重力解析値、減衰残存率、固定/交互時間の比較を確認。
- R10の各回帰はR01〜R09へ同梱したが、最終的な検証範囲の照合、実素材/GUIの受入は未完了。新しいpreview接続では時間とVRM設定値の対応も確認する。

- Windows-SpringTime build **PASS**: `Logs/build-player-20260912-135843-295.log`。GUI/保存経路は未変更のためPlayer suiteは今回再実行していない。実アバターの揺れ具合は未受入。

### R07とR10の衝突回帰: 長さ・衝突の同時制約（2026-09-12）

- `SpringConstraintSolver`へ計算を分離。骨の長さ球面上でcollider境界円へ投影し、各pass後に全colliderと長さを再検査する。hitRadiusを含み、許容差は0.00001。候補方向＋6軸方向、各32passのbounded探索とする。
- 解なしの包囲球と、反復上限による未収束を`SPRING_CONSTRAINT_UNRESOLVED`で診断。Stepは入力pose/stateを変更せず、失敗した途中状態を公開しない。任意の球集合で解が必ず見つかる保証や、連続衝突検出ではない。契約は [SpringBone制約](docs/SpringBone-Constraints.md)。
- 修正前は追加3ケースが失敗（`Logs/core-spring-constraint-before.txt`、既存313件合格）。複数球での循環も検出し、固定した開始方向の再探索を追加。修正後はCore **318 passed / 0 failed**、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-cf49b74bf4f4418a88357d66e89d414b`。
- 新規6ケースを追加し、実際にはgroupを参照しなかった旧衝突テスト1件を削除。実Stepで衝突なし/ありを比較し、同軸、hitRadius、長さ、有限性、Pose/State一致、複数球、中心一致、解なし・反復上限を確認した。R10の残件はR08の停止・時間刻み回帰と受入範囲の最終照合。

- Windows-SpringConstraints build **PASS**: `Logs/build-player-20260912-135403-871.log`。GUI経路は未変更のためPlayer suiteは今回再実行していない。Spring表示や実VRM見た目受入は未実施。

### R06とR10の入力検査: chain別コライダー（2026-09-12）

- `SpringSimulationInputs`へ入力検査・chain hash・参照解決を分離。各chainのgroup参照を昇順・重複なしで解決し、同じchain内のjointだけで不変リストを共有する。全chainの参照unionは廃止した。
- collider groupのnullは参照有無に関係なく`INVALID_SPRING`として初期化・Step入口で拒否。group数は最大256とし、group内最大64件の既存予算と組み合わせる。参照範囲外は従来の`SPRING_COLLIDER_MISSING`を維持する。
- 修正前: 既存311件合格、新規2件失敗（`Logs/core-spring-scope-before.txt`）。独立bone BがAのgroup参照で動く距離は **0.29042628 m**。修正後: Core **313 passed / 0 failed**、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-ac2eda790ccd47f88c273cd998e3476b`。
- A追加/削除・A専用コライダー変更で参照なしBが不変、共有groupを明示した場合は両骨へ作用、null groupを初期状態公開前に拒否することを確認。R07の最終貫通解消・R08の時間刻みは未修正。R10全体はそれらの回帰追加まで未完了とする。

- Windows-SpringColliderScope build **PASS**: `Logs/build-player-20260912-134944-700.log`。今回の変更はCore参照解決で、GUI/保存経路は変更していないためPlayer suiteは再実行していない。実VRMの衝突見た目受入も未実施。

### R04/R05: SpringBone姿勢と階層（2026-09-12）

- `SpringPoseHierarchy`へ親先行の走査と相対transform継承を分離。simulated jointだけでなく全骨を評価し、未登録子孫も追従させる。入力のhead offsetとbasisを維持し、chain/skeleton登録順に依存しない。
- 次tailへの回転は前stateではなく、親の変更を反映した入力poseのtail方向を起点とする。長さはその入力poseのhead-tail距離を使い、scaleを保持する。小角度の`acos`精度問題も3joint回帰で検出し、`atan2`へ変更した。
- 契約は [SpringBone pose](docs/SpringBone-Pose-Contract.md)。呼出し側は各フレームのbase poseと前Stateを別々に渡す。R06/R07/R08、null group等のR10残件と実VRM preview接続は未完了。
- 修正前: 新規4回帰が失敗、既存306件は合格（`Logs/core-spring-pose-before.txt`）。修正後: Core **311 passed / 0 failed**、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f6973616302f4d3f95da0b258af00d19`。固定/変化するbase poseの連続step、入力不変、2〜3joint、逆登録順、offsetあり、未登録子孫、移動・回転・非一様scale、有限値とPose/State tail一致を確認した。

- Windows-SpringPoseRepair build **PASS**: `Logs/build-player-20260912-134557-817.log`。Authoring suite **PASS**: `Artifacts/Authoring-20260912-134641-fa8073058be543af86d2913b37c6ecf6/report.json`。これは既存GUI/保存経路の回帰であり、SpringBone runtime表示や実VRMの手動受入は未検証。

### R01/R02/R09: VRM取込とsession互換性（2026-09-12）

- `VrmAuthorNames`へ作者リストの検査・不変コピー・旧単独名の変換を分離。VRM1は必須の非空配列を読み、各作者名と順序を保持する。上限は256名・各256文字。`Author`は表示用連結文字列、`Authors`は保存する原情報であり、カンマで再分割しない。
- expression / Spring session writerはversion 2の`authors`配列へ変更し、readerはversion 1の`author`も受け入れる。旧空名は空リスト、旧非空名は丸ごと1名として扱う。snapshot schema 4は変更しない。旧payloadは開くだけでは書き換えず、codecで再書出しするとv2になる。
- コライダーごとのnode列は重複と順序を保持し、rootやgroup参照の一意検査は維持。合成VRM0の同一node複数sphereとVRM1の同一/異なるnode・sphere/capsule inventoryを往復検証した。shape詳細値の保持は別の未完了事項。
- VRM1の省略stiffness / dragForceを1 / 0.5へ修正し、明示0と区別。VRM0の既定値は変更していない。
- Core **306 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-1f29d4a482f7474d839565a64cc827b5`。複数作者import→session往復、v1移行・v2保存、型違い・欠落・空作者拒否、複数node列、設定既定値、native snapshot Save/Openを検証。
- Windows-VrmMetadataRepair build **PASS**: `Logs/build-player-20260912-134009-576.log`。Authoring suite **PASS**: `Artifacts/Authoring-20260912-134033-211dbe64c427447b93017ac79d8a3861/report.json`。複数作者・`nodes:[0,0,2]`・設定値を持つsessionで、保存失敗保護・GUI/MCP handler再試行・Workbench Openを確認した。実VRMファイルpicker・実クリック・見た目受入は未実施。

### R03: 作品本体とmetadataの一括公開（2026-09-12）

- `ProjectAttachments`は所有するsession bytesとhashを不変データとして保持し、workspaceのdirty判定へ加える。`ProjectSnapshotCodec`はschema 2/3本体をschema 4 envelopeで包み、設定blob参照を同じmanifestへ保存する。`ProjectStore`の既存writer lock / expectedVersion境界でblobを準備・検証し、manifestを最後に一回だけ置き換え、その後にsaved状態を更新する。
- 旧schema 1/2/3のsidecarはOpen時に取り込む。schema 4は古いsidecarを参照せず、設定を除去しても復活しない。schema 1は従来どおり別保存先への移行が必要。古いバージョンのNyaForgeはschema 4を開けない。形式と責務の詳細は [metadata snapshots](docs/Project-Metadata-Snapshots.md) を参照。
- GUI import時点でmetadataをworkspaceへ渡し、GUI/MCPともに共通ProjectStoreで一括保存する。MCP handlerの保存成功でGUIの失敗表示も解除する。この時点ではR01/R02/R09は未修正だった。後続修正は下記の記録を参照し、保存transactionの修正をVRM互換性全体の合格とはしない。
- Core **303 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-3d890be653834863b2859c79e76939a6`。expression/Spring書込み失敗で旧manifest・本体・metadata・dirty・versionを維持、Save As公開失敗と再試行、legacy移行/削除、corrupt blob、未知field、graphの共通save service往復とstale writer拒否を検証。
- Windows-AtomicMetadata build **PASS**: `Logs/build-player-20260912-132607-475.log`。Authoring suite **PASS**: `Artifacts/Authoring-20260912-132736-efd679b908084601afb033b0e7681dce/report.json`。新規/既存の両方でblob排他ロックによる失敗を起こし、旧設定保持、終了/無確認切替防止、GUI/MCP handler再試行、設定を含むOpenを確認した。終了ボタンが使う判定関数の自動検証であり、実クリック・外部MCP transport・実VRMの手動受入は別途。

### R03前段: Windows GUIの保存失敗保護（履歴・2026-09-12）

- 保存の呼出しと成否判定を`AuthoringWorkbench.Saving.cs`へ分離した。保存全工程の成功をboolで返し、失敗後はWorkbenchの`saveIncomplete`を保持して未保存表示と終了／作品切替の確認を維持する。「保存して終了」は保存成功の明示結果を必須にした。
- 本体が保存できた時点でGUIの保存先も更新し、付属設定が失敗したSave Asの再試行に正しい保存versionを使う。再試行成功か、ユーザーが確認して作品を切り替えるまで失敗状態を解除しない。
- `AuthoringWorkbench.SaveFailureVerification.cs`で、専用fixtureのexpression / Spring sidecarを排他ロックして削除失敗を起こした。main保存後にも終了・無確認切替を拒否し、ロック解除後にversion 2へ保存でき、Openも成功することをPlayerで検証した。実マウスクリックではなく終了ボタンが使う判定関数の検証。
- Windows-SaveFailureGuard build **PASS**: `Logs/build-player-20260912-131616-600.log`。Authoring suite **PASS**: `Artifacts/Authoring-20260912-131645-43dcd4028e8c4f72988dd960d8e5a921/report.json`。専用の`Save failure guard` checkが成功している。本変更はUnityRuntimeのみのためCore/Bridgeの新規実行は行っていない。
- **この前段の時点の残件**: GUI終了・再試行だけの保護で、main/sidecarの一括公開は未実装だった。現在は上段のR03 snapshot実装と検証を参照する。

## 出力予算検証 `forge_validate`（2026-09-12）

- 読み取り専用の `forge_validate` を追加。`documentId` と `expectedRevision` を固定してから、`pc` / `mobile` プロファイルの三角形数・材質数・最大テクスチャ寸法を確認する。
- 結果は全体 `pass` / `fail` / `unknown` と個別checkを返す。骨変形とアバターへのfitは静的制作プロファイルの範囲外として `unknown` にする。合格をVRChatランクや目視品質の保証には読み替えない。
- Core **265 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-c212e2d98f15447cbb1cabff4275c9a6`。空出力のunknown、2材質slotのmobile fail、stale revision拒否を確認。
- Windows-Validation build/Player suite **PASS**: `Logs/build-player-20260912-090956-750.log`、`Artifacts/Authoring-20260912-091026-c10b50cb99a34e7e82dedbf7d8e45a78/report.json`。外部MCPからlive Playerへの検証呼出しも成功。
- 残件はpose別fit検査、実アバターimport、skin/morph出力、複数object。C2の骨・weight・pose基盤は着手済みで、実アバター接続を次の段階とする。

## 出力チェックGUI（2026-09-12）

- 材質パネルの下に折りたたみ式の「出力チェック」を追加。PC / モバイルの目安を選び、現在の文書revisionを自動で固定して検査できる。
- `AuthoringValidationRequest.Create` と `AuthoringValidationReader` を共用し、GUI・MCP・Core検証で判定ロジックを分けない。結果は個別checkの `pass` / `fail` / `unknown` を短く表示する。
- `AuthoringWorkspace.IsExecuting` を公開し、検査ボタンをcommand transaction中だけ無効化。検査自体は文書・履歴を変更しない。
- Windows-ValidationUi build/Player suite **PASS**: `Logs/build-player-20260912-091321-826.log`、`Artifacts/Authoring-20260912-091345-cce0d209c4574c12943ccc16df92f04f/report.json`。
- 目視での文字サイズ・折りたたみ操作は未受入。C2 Rigの実マウス見た目受入と、実アバター接続を次に進める。

## C2 Rigコア基盤（2026-09-12）

### MorphコアとGraph/UI接続（2026-09-12）

- `MorphTarget` はmesh topology hashに紐づく疎なrest-space頂点差分をstable target ID・名前とともに保持し、同一頂点の重複・範囲外・非有限値を拒否する。`MorphSet` は最大256ターゲット、ID/名前重複、別topologyを拒否する。
- `MorphDeformer` は0..1のtarget weightを検査し、複数targetをrest meshへ加算適用する。pose済みmeshへ焼き込む設計ではなく、属性（normal/tangent/UV/submesh）を元meshから保持する。
- `MorphCodec` は疎payloadを厳密な`NYRM` v1へ保存・復元する。mesh topology hash、件数、UTF-8、末尾bytesを検査し、壊れたassetや別meshを公開しない。
- Core **282 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f817e5ee174e410ca5c2f78063889c91`。Morph変形の半量適用、属性保持、決定的codec往復、未知target／範囲外weight／stale topology／末尾bytes拒否を確認。
- Windows-MorphCore build / Authoring Player suite **PASS**: `Logs/build-all-20260912-110016-285.log`、`Artifacts/Authoring-20260912-110055-aa6d942115e2424a930f9d3e39c352a3/report.json`。既存Rig GUI lifecycleも再回帰した。

### Morph Graph/UI接続（2026-09-12）

- `PortType.MorphSet`、`rig.morph-set`、`rig.morph-deform`を追加。MorphSetはmesh topology hashを保持し、MorphDeformはtarget IDごとの0..1 weightを入力payloadとして正規化・保存する。
- Graph evaluatorはMorphSetのtyped outputと、rest-space `MorphDeformer`によるmesh outputを提供する。変形後もmaterial/baseColor/slot参照を保持し、別topologyでは診断を出して公開しない。
- native graph schema 3へ`NYRM` blob参照とcanonical sorted weight payloadを接続。`graph_inspect`はmorph hash、target数、delta数、targetごとのcontent hashを返す。
- Graph canvasに「Morphサンプル」、Workbenchに折りたたみ式target選択／weight編集を追加。既存のcommand・Undo・保存経路でnode更新を行う。
- Core **284 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-52240aca170840e9b350e6859ddd0241`。Morph graphの評価、inspection、native保存→再読込、半量変形、topology変更拒否を確認。
- Windows-MorphUi Player build / Authoring suite **PASS**: `Logs/build-player-20260912-111005-833.log`、`Artifacts/Authoring-20260912-111029-eb76635897054006a74c1d0117fd5872/report.json`。標準fixtureの画像を目視し、追加UIは折りたたみ領域のため実クリック受入は未実施。
- 次は実アバターの編集可能mesh import境界を確定し、normal再計算とMorph/skin exportを別段階で接続する。

### GLB import境界（2026-09-12）

- `Authoring.Import.GlbImport` を追加。GLB v2のheader／chunk長／UTF-8 JSON／単一BINを検査し、16MiB予算内でPOSITION、任意のNORMAL/TANGENT/TEXCOORD_0、triangle indexをCPU可読`MeshData`へ変換する。
- 1 meshあたり最大32 primitiveまでをsubmeshとして結合し、primitiveごとの頂点順序とindex範囲を保持する。skin、属性有無の混在、morph target数の混在、sparse accessor、非対応component/type、未知chunkは`UNSUPPORTED_FORMAT`等で拒否する。元のscene階層・material・quadを復元したとは主張しない。
- primitiveのPOSITION morph targetを`MorphSet`へ変換し、`extras.targetNames`を名前へ反映、source hashから決定的target IDを生成する。normal/tangent morph差分、humanoid、VRM metadataは未対応としてwarningsに残す。
- WorkbenchへWindows Explorer経由の「GLBモデルを取り込む」を追加。空の制作projectだけに新規graph（Source→MorphDeform任意→Output）を作り、既存作品を置き換えない。元GLBをprivateへコピーせず、取り込み済みmesh/blobのみnative graphへ保存する。
- Core **286 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-8a99a1b52c014bda9edfe8a06a9ab595`。合成GLBの複数primitive結合・submesh分離・morph差分結合、skin拒否、破損header拒否を確認。
- Windows-GlbMulti Player build / Authoring suite **PASS**: `Logs/build-player-20260912-113225-389.log`、`Artifacts/Authoring-20260912-113250-94a7a54f014948bf9904b11fcb901beb/report.json`。標準fixtureの起動・描画・既存GUI回帰を確認し、画像も目視した。Unity Bridge receiverも **PASS**: `Artifacts/BridgeReceiver-20260912-113320-467-aecbf12069194bd49a2a6fe6a16ed16d/bridge-report.json`。GLB pickerの実マウス選択とRadDollV3/VRM実データ受け入れは未確認。

### GLB skin importとGUI接続（2026-09-12）

- `GlbDocumentReader` を共通化し、header／chunk／JSON／BINのbounded検査をmesh adapterとskin adapterで共有する。静的mesh側のsource hashと既存Morph保持を変えない。
- `Authoring.Import.GlbSkinImport` を追加。1 mesh／1 skin、最大256 joints、JOINTS_0＋WEIGHTS_0の4 influence、translation-onlyのnode／inverse-bindを`SkeletonDefinition`／`SkinBinding`へ変換する。複数skin、回転・非unit scale、JOINTS_1、未weight、sparse／非対応accessorは理由付きで拒否する。
- Windows WorkbenchのGLB取り込みは、skin配列の有無をboundedに判定し、skin付きならSource→MorphDeform任意→Skeleton／SkinBind／Pose→SkinDeform→Output graphを生成する。初期poseはimportしたbone headをtranslationへ置き、既存のRig UI／Undo／native保存経路を利用する。
- Core **288 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-03d1dfa8995d4643b115d34d1e8910db`。合成skinのjoint階層・normalized weight・source hash保持、回転拒否を確認。
- Windows-GlbSkin Player build / Authoring suite **PASS**: `Logs/build-player-20260912-114347-992.log`、`Artifacts/Authoring-20260912-114413-637c0868011d4e329b83a5a2ddb77d74/report.json`。標準fixtureの起動・描画・GUI回帰を目視した。Unity Bridge receiverも **PASS**: `Artifacts/BridgeReceiver-20260912-114608-484-b8f097c1eee54d34a015286334605c27/bridge-report.json`。skin付きGLBの実ファイルpicker操作とRadDollV3／VRM実データは未確認。

### VRM metadata境界（2026-09-12）

- `VrmMetadataReader` を追加。VRM 1.0の`extensions.VRMC_vrm`とVRM 0.xの`extensions.VRM`から、spec version、title/name、author、humanoid semantic→glTF node indexをsource hash付きで読む。UniVRMやUnity APIへ依存しない。
- malformed mapping、node範囲外、重複human bone、未対応spec versionは`INVALID_VRM`／`UNSUPPORTED_FORMAT`で拒否する。expression、look-at、spring bone、MToon、一般transform、VRM exportは別adapterのまま保持する。
- GLB/VRM pickerのfilterを`.glb`／`.vrm`へ拡張し、取り込み時にmetadataを検査する。skin付きなら既存skin graphへ接続し、statusへVRM format/title/humanoid数を表示する。
- Core **291 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-eb7fc75824464221943c9207fe446012`。VRM 1.0／0.x mapping、source identity、未対応versionと欠落extension拒否を確認。
- Windows-VrmMetadata Player build / Authoring suite **PASS**: `Logs/build-player-20260912-115103-670.log`、`Artifacts/Authoring-20260912-115128-ad0dc9697de74b4798fb64c9633adfe4/report.json`。標準fixtureの起動・描画・GUI回帰を目視した。Unity Bridge receiverも **PASS**: `Artifacts/BridgeReceiver-20260912-115200-239-6157d093ce40429f85b7a9745da4db30/bridge-report.json`。実VRMファイル、humanoid姿勢、expression/springの受け取り先は未確認。

### VRM expression inventory境界（2026-09-12）

- `VrmExpression` と `VrmMetadata.Expressions` を追加。VRM 1.0 `expressions.preset/custom` と VRM 0.x `blendShapeMaster.blendShapeGroups` から、名前、preset/custom区分、morph/material bind件数だけを最大256件まで読む。source内のbindをNyaForgeのmorph/materialへ自動適用する処理はまだ持たない。
- GUIのモデル取り込みstatusにVRM expression件数を表示し、対応範囲ラベルをidentity・humanoid・expression inventoryまで更新した。
- Core **291 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-26953d296bb74bd1a44480faeaa939b3`。VRM 1.0／0.xテストでexpression名称、preset/custom区分、morph/material bind件数を確認した。
- 追加のCore **291 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-08598e8437b14a6aa9741ffc44bd1cef`。source morph bindのindex/weight保持と、単一ownerから`MorphSet` stable IDへの解決を確認した。
- Windows-VrmExpressions Player build / Authoring suite **PASS**: `Logs/build-player-20260912-115915-345.log`、`Artifacts/Authoring-20260912-115941-fe34fb0e48c749fe9b2d7d2701d315d8/report.json`。標準fixtureの起動・描画・既存GUI回帰を目視した。Unity Bridge receiverも **PASS**: `Artifacts/BridgeReceiver-20260912-120017-323-9b0849f34ded4f3db2c762fe93bcd6da/bridge-report.json`。実VRMファイルの表情適用、MToon、spring、look-at、一般transformは未確認・未対応のまま。

### VRM expression → Morph対応境界（2026-09-12）

- `VrmMorphBinding` がVRM 1.0のnode/index/weightとVRM 0.xのmesh/index/0〜100 weightを正規化して保持する。`VrmExpressionMapper.ResolveForSingleOwner` は1つのownerと既存`MorphSet`のtarget indexを照合し、stable target IDと0..1 weightの辞書へ変換する。owner違い、範囲外index、同一targetの重複bindは推測せず拒否する。
- Core VRMテストでVRM 1.0／0.xのsource bind値と、GLB morph targetへのweight解決（0.5）を確認した。これはexpression payloadをnative graphへ保存・適用するUIではなく、次段の表情編集へ渡すための純粋なadapterである。
- 実VRMの複数mesh owner、material bind適用、表情スライダー、spring、look-at、一般transform、skin exportは未確認・未対応のまま。
- Windows-VrmExpressionMap Player build / Authoring suite **PASS**: `Logs/build-player-20260912-120714-065.log`、`Artifacts/Authoring-20260912-120743-92d014c8543840c89f8a81945b1f1459/report.json`。新Import module追加後も標準fixtureの起動・描画・既存GUI回帰を目視した。Unity Bridge receiverも **PASS**: `Artifacts/BridgeReceiver-20260912-120817-875-6fde4712d8d3474b83139201c12a4326/bridge-report.json`。

### VRM expression UI接続（2026-09-12）

- `MorphSet`のstable ID生成を`GlbImporter.MorphTargetId`へ共通化し、`VrmExpressionMapper`がMorphSetの並び順ではなく元のsource morph indexからtarget IDを解決するよう修正した。複数Morphを含むfixtureでindex 0の対応を確認した。
- WorkbenchのMorphパネルへ「VRM表情」選択と「選択したVRM表情を適用」を追加。GLB/VRM取り込み時に単一mesh ownerを解決できた表情だけを一覧にし、選択すると全Morph weightを更新して既存の`UpdateNode`とUndoへ渡す。新規／開き直したprojectへVRM metadataを永続化する処理はまだ持たない。
- Core **291 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-c9e2b12978f543c583125e3dadb12fdb`。Windows-VrmExpressionUi2 Player build / Authoring suite **PASS**: `Logs/build-player-20260912-121732-971.log`、`Artifacts/Authoring-20260912-121804-8ece484896984c1d970e44ba2b804a66/report.json`。Unity Bridge receiverも **PASS**: `Artifacts/BridgeReceiver-20260912-121838-049-71972bc3cce24c78b7e8f09e398683f7/bridge-report.json`。実VRMをpickerから選び表情適用する手動受入は未確認。

### VRM expression session sidecar（2026-09-12）

- `VrmExpressionSession`／`VrmExpressionSessionCodec`を追加し、mapped expressionの名前・preset/custom・stable Morph ID weightを`vrm-expression-session.nyaforge.json`へbounded deterministic JSONとして保存する。`AuthoringWorkbench`のSave/Openへ接続したため、同じprojectを開き直しても表情一覧と適用対象を復元できる。元VRMファイルやprivate assetはコピーしない。
- Codec往復・決定性と複数Morph index解決をCoreで確認した。現在のgraphとsidecarのtarget IDが一致しない場合は適用時に再取り込みを要求する。
- 追加Core **291 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-42e7fc51a1354f728929a4d4c2d054c7`。Windows-VrmSession Player build / Authoring suite **PASS**: `Logs/build-player-20260912-122449-685.log`、`Artifacts/Authoring-20260912-122518-a0c65904ada24891b5237649164be16e/report.json`。Unity Bridge receiverも **PASS**: `Artifacts/BridgeReceiver-20260912-122554-137-24a5c4fa1a6941a0998ada473b086944/bridge-report.json`。実VRMによるSave/Open手動受入は未確認。

- `Authoring.Rig` を独立モジュールとして追加。`SkeletonDefinition` はcanonical UUIDのbone、親子階層、head/tailのrest座標を不変データとして保持し、循環・欠落親・重複IDを公開前に拒否する。
- `SkinBinding` はmeshのtopology hashとskeleton hashを固定し、全頂点に1〜4本の明示boneを要求して、重みを降順・決定的順序で正規化する。同一boneの重複、未知bone、未weight、上限超過を拒否する。
- `PoseTransform` と `SkinDeformer` を追加。bone headを基準にしたrest-relative affine poseを適用し、最大4 influenceの位置を線形ブレンドする。mesh topology hash / skeleton hash / 全bone poseを毎回照合し、normal・tangent・UVは元mesh所有のまま保持する。
- `RigCodec` を追加。`NYRG` v1の専用バイナリでskeletonとbindingを保存し、復元時に元mesh topology hash・skeleton hash・厳密な件数/UTF-8/末尾bytesを検査する。
- `rig.skeleton` typed graph nodeを接続。`PortType.Skeleton` と `GraphSkeletonValue` を追加し、native graph schema 3のblobへ `NYRS` v1 skeletonを保存する。`graph_inspect` はnodeごとにskeleton hash、bone数、親子・head/tailを返す。保存→再読込のCore往復を固定した。
- `rig.skin-bind` typed graph nodeを追加。mesh＋skeleton入力の型を検査し、topology hash・skeleton hash・1〜4 influence／全頂点weightを照合して `GraphSkinBindingValue` を出力する。`NYRB` v1 binding blobをnative graphへ保存し、`graph_inspect` は頂点数・influence数・最大influence数を返す。
- この段階ではhumanoid自動配置、MCPからのskin bind生成、skin exportは未接続。rest定義・binding・姿勢評価の正本と、bone移動時のstale／明示再bindを共通commandへ接続した。normal再計算と補正morphは別工程。
- Graph canvasの「Rigサンプル」をPlane＋Root/Child骨＋左右weight＋25度pose＋skin-deformまで拡張。Rigパネルにbone選択、選択頂点への指定weight混合、Root 100%割当、brush weight paint、選択boneのXYZ pose回転、rest bone移動、stale依存の明示再bindを接続した。weight paintは画面上のブラシ半径で頂点を集め、1ドラッグを1つのUndoにまとめる。実アバターのimportはまだ別工程。
- `rig.pose` と `rig.skin-deform` を追加。NYRP v1 pose blob、skeleton/binding/pose hash照合、rest-relative変形meshをnative graphへ保存できる。`graph_inspect` はpose hash／bone数を返す。
- Core **280 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-ffb7f155a0cb4a5cbc77cb59b81c060d`（既存target boneの再weight置換、XYZ Euler pose、Euler pose codec往復の回帰確認を追加）。Windows-MultiAxisPose Player build / suite（MCPなし）**PASS**: `Logs/build-all-20260912-104615-663.log`、`Artifacts/Authoring-20260912-104645-9058853eb3c74d968935d967a1768e63/report.json`。Player内のRig graph評価・inspection・schema3再読込、weight paintのブラシ半径・1操作1Undo、XYZ poseのUndo/Redo lifecycleを確認した。MCP付き全体suiteは外部save transportの一時失敗で未合格だが、`dotnet build Tests/Mcp.Transport/Mcp.Transport.Tests.csproj --nologo` は0警告・0エラー。実マウスでの手動見た目受入、実アバターimportは未確認。
- Unity Bridge受け取り側も `Artifacts/BridgeReceiver-20260912-104935-490-6ec4f5282eb74f96b1615e8520802e95/bridge-report.json` でscale 1/100のBake往復 **PASS**。Rig poseのEditor/VRChat実表示を確認したものではなく、既存Bake受け渡しの回帰確認。

## 面材質割り当ての実通信確認（2026-09-12）

- `mesh.assign-materials` の wire を追加し、整数の疎なslot番号を保持したまま複数材質を登録できるようにした。
- `graph_inspect` の各nodeへ `materialSlots` と `assignedMaterials` を追加。slotごとの材質値とcontent hashを確認でき、画像本体は返さずidentity/hash/寸法だけを返す。
- `polygon.faces.material` の wire を `MaterialFaceEditing` へ接続。`context`・`elementIds`・`materialSlot`・`materialNodeId` を受け、面slotと材質nodeの接続を同一Undo単位で更新する。slot型違い・材質node ID欠落は拒否する。
- Core **262 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-371bfc07471e411884eb35dc28d0ed9b`。疎slot 3/9のinspection、assign-materials wire、面材質割り当てのUndo/Redoと再送を確認。
- Windows-FaceMaterial build/Player suite **PASS**: `Logs/build-player-20260912-090000-851.log`、`Artifacts/Authoring-20260912-090123-58119a05cc614d779e4274d3e32c066a/report.json`。既存のMCP生成・inspect・paint・layer・import・capture・export・save一周も継続成功。
- 残件は材質値そのものの編集UI、面選択からの実操作導線、schema registry、複数object/rig/weight/morph。目視受入と販売品質判定は別途必要。

## 現在地

| 範囲 | 現物と確認状況 |
|---|---|
| C0-R／C1-A | 空project、最大1object、typed graph、共通commandとUndo、native保存、node canvas、static Bake／Bridge |
| C1-B | polygon/corner ID、面選択・押出し・削除・境界cap・厚み、Mirror、編集ケージと最終結果、UV投影と島の数値編集 |
| C1-C Paint | 2D brush、Image port、UV binding、1stroke Undo、native画像保存、3D baseColor、PNG／Surface Bake |
| Layer/mask GUI | 移行・追加・選択・並替・削除・表示・不透明度・名前、mask追加/削除と描画、取消、Undo、保存を検証 |
| Image import | PNG検査・展開・縦横比保持サイズ調整・新layer追加・Undo・保存を検証。Windows pickerの実操作は手動未確認 |
| 3D paint | BVH ray、論理edge/UV連続判定、screen補間、切れた区間の描画、GUI色/mask・仮表示・取消・Undo・保存/出力を実装 |
| 今後 | 3D複数面/細かなseamの精度と操作、一般Material graph、UV再投影、出力identity更新、Evidence/MCP、rig/weight/morphと全身制作 |

保存は設定なしstatic writer schema2、graph writer schema3、metadata付きsnapshot writer schema4、reader 1/2/3/4。Paintは共有画像と部位別の独立画像に対応。材質未割当は不透明preview、標準材質は3alpha modeを選択できる。UV変更時は旧payloadを未解決として保持し、明示rebindで対応を更新する。見た目を保つ再投影ではない。旧mesh-only Bakeは画像を拒否。画像付きmeshは別のSurface Bake profile、単一標準材質はMaterial Bakeを利用。複数材質BakeはGUI/Bridgeまで接続済み。

## PNG importのWindows実通信

- PlayerImportVerificationを分離追加。専用8x4青PNGをアプリ側fixtureで生成し、MCPからSHA256/絶対path/fitで64x64 layerへ取り込み。layer ID/合成hash変化、同command再送、元PNG改変拒否、Undo/Redoを確認。
- 改変は専用fixtureのみでfinallyに元bytesへ戻す。import後の試験layerは削除し、後続のlayer/paint/output/save検証も成功。
- Windows-McpImportLive build/Player suite PASS: Logs/build-player-20260912-084305-490.log、Artifacts/Authoring-20260912-084327-30a6d0c530d64406b2adf0a9513d5613/report.json。今回Core変更なし（直近260）。
- imported pixelの個別実通信readbackは未実施（Core/GUI共通pipelineでは検証済み）。schema/通信回復/取り込み再送の元file依存解消、複数object/rig等の全体残件を継続。
## 複数材質スロットinspection

- mesh.assign-materialsのwire生成（parameters.slots整数配列）をCommandWireGraphReaderへ追加。重複/型違いは拒否し、GraphNodeの疎なslot番号を維持する。
- graph_inspectにnodeのmaterialSlotsとassignedMaterialsを追加。スロットごとに材質値（linear RGBA/metallic/roughness/emission/alpha/hash）を返す。画像はhash/寸法のみで、面IDやスロット番号の欠落を避ける。
- polygon.faces.materialを既存MaterialFaceEditingへ接続するwireを追加し、観測済みcontext/face IDsとmaterialSlotを明示する。
- Core261 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-5aaf49ad31f04d95a5f5dbe693651c46。疎slot 3/9のinspection、assign-materials node wireの並べ替え/文字列拒否を確認。
- Windows-MultiMaterialInspect build/Player suite PASS: Logs/build-player-20260912-085516-488.log、Artifacts/Authoring-20260912-085555-0a7329c440c94cc3a6ff75ca944383cd/report.json。MCP生成/材質inspection/既存制作一周も成功。全体目標継続。
## VRM SpringBone inventory境界（2026-09-12）

- `VrmSpringBoneGroup`／`VrmSpringJoint`／`VrmSpringColliderGroup`を追加し、VRM 1.0の`extensions.VRMC_springBone`（colliders、colliderGroups、springs）とVRM 0.xの`secondaryAnimation`（boneGroups、colliderGroups）から、node参照、chain/root数、collider数、boundedな基本パラメータを読む。VRM 1.0ではshapeのsphere/capsuleとradius、0.xではsphere radiusの形式を検証する。
- SpringBoneはinventory専用で、揺れの物理計算、姿勢適用、collider形状のruntime化はまだ行わない。node範囲外、重複joint、重複index、未対応spec version、予算超過は推測せず拒否する。取り込みstatusへ`spring <chain>/<colliderGroup>`を表示する。
- 公式仕様の構造に合わせた境界である（[VRMC_springBone 1.0](https://github.com/vrm-c/vrm-specification/blob/master/specification/VRMC_springBone-1.0/README.md)、[VRM 0.x secondaryAnimation](https://github.com/vrm-c/vrm-specification/blob/master/specification/0.0/README.ja.md)）。
- Core **292 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-d30603607f7d46c696603787788cf80a`。VRM 1.0／0.xのchain、joint、collider inventoryを追加検証した。Player/Authoring/BridgeのSpringBone表示を含むWindows実ビルド確認は次に行う。実VRMの実データ、SpringBone runtime挙動、手動見た目受入は未確認。
### VRM SpringBone session sidecar（2026-09-12）

- `VrmSpringSession`／`VrmSpringSessionCodec`を追加し、SpringBoneのchain、joint、root、center、collider group、基本パラメータを`vrm-spring-session.nyaforge.json`へbounded deterministic JSONとして保存する。元VRM bytesとruntimeの物理状態は保存しない。
- Workbenchのモデル取り込み時にsessionを作り、Save/Open／新規project切替で表情sessionと同じライフサイクルを通す。モデル取り込みパネルに復元済みのchain／joint／collider group数と「物理未実装」を表示する。
- Core **292 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-7f9bd35157d24405a8fba0bf168dbd5a`。session codecの往復・決定性とmodern collider node保持を追加確認した。Windows-VrmSpringSession2 Player build / Authoring suite **PASS**: `Logs/build-player-20260912-124641-743.log`、`Artifacts/Authoring-20260912-124711-2f4bf3dc9cae41ae8f7daa33ea743bb5/report.json`。Unity Bridge receiverも **PASS**: `Artifacts/BridgeReceiver-20260912-124750-959-ec88c07451b74aed908178b9bd7409df/bridge-report.json`。実VRMのSave/Open手動受入とSpringBone runtime挙動は未確認。
### SpringBone preview core（2026-09-12）

- `SpringBoneJointSettings`、`SpringBoneChain`、`SpringBoneColliderGroup`、`SpringBoneState`、`SpringBoneSimulator`を`Authoring/Rig`へ追加した。stable `BoneId`のchainを入力に、detachedなVerlet 1ステップ、stiffness/gravity/drag、rest length制約、sphere collider解決を計算し、完全な`PoseSet`と次状態を返す。Unity、VRM node index、シーン状態、元VRM bytesには依存しない。
- 入力はchain 256、joint 1,024、collider group参照、delta time 0〜0.25秒などの予算と範囲を検証し、重複joint、未知bone、別skeleton/chainのstateを拒否する。`CreateInitialState`は状態をコピーして保持し、同じ入力の反復結果を決定的にする。
- この段階はVRM node→stable `BoneId` adapter、Workbench/Unity runtimeへの姿勢接続、VRM形式への物理設定保存、実VRMの見た目受入を含まない。次段で受け取り先を決めて接続する。
- Core **297 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-676025c66f614051bc750de1b7a6cf70`。初期状態、gravityによる姿勢回転と長さ制約、sphere collider、入力拒否、状態コピー・決定性を確認した。Windows-SpringPreviewCore Player build **PASS**: `Logs/build-player-20260912-125700-714.log`、`Builds/Windows-SpringPreviewCore/NyaForge.exe`。Authoring suite **PASS**: `Artifacts/Authoring-20260912-125726-ea62fbeeb288427ab8aefc0ab045afbb/report.json`、screenshot `authoring.png`を目視した。Unity Bridge receiverも **PASS**: `Artifacts/BridgeReceiver-20260912-125809-882-6a66d11236df481794f08776fbaad3c2/bridge-report.json`。実VRMのSave/Open、VRM node mapping、SpringBoneの実ランタイム挙動、手動見た目受入は未確認。
## 材質inspection

- AuthoringMaterialReaderを分離追加。graph_inspectの各nodeにmaterialOutput（linear RGBA、metallic、roughness、emission、alphaMode/cutoff、contentHash、画像identity）とassignedMaterialを返す。画像本体は返さずhash/寸法のみ。
- Windows-MaterialInspect build/Player suite PASS: Logs/build-player-20260912-085038-291.log、Artifacts/Authoring-20260912-085111-d01856be26f947f8a47dbc40c08092c2/report.json。MCP生成時の標準材質値と割当先contentHash一致を実通信で確認。
- Core260 passed/0 failed（直近再確認: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f14a0b7718334e52afef49de9f70dcc5）。MCP transport build成功。
- 次は材質値更新操作、複数材質slotのinspection/編集、schema registry/通信lifecycle、複数object/rig等を継続。全体目標は未完了。
## MCP通信回復検証

- AuthoringPipeRecoveryVerificationを分離追加。空frame、UTF-8不正、壊れたJSON、method型不正、改行前切断を送った後にnamed pipe listenerが次接続を受けられることをWindows Playerで確認。
- AuthoringIpcRequestはmethod型をキャスト前に検査し、field集合比較をordinalへ統一。import envelopeのinstance/kind型も明示的に検査する。
- Windows-McpRecovery build成功: Logs/build-player-20260912-084547-299.log。Player suite PASS: Artifacts/Authoring-20260912-084653-e9a13f9cd5d44a8d9e79bc2ce6a5dace/report.json。既存MCP生成/頂点/面/paint/layer/import/capture/export/saveも継続PASS。
- malformed frameは相手へ構造化errorを返さず切断し、次の接続を受ける契約。正常なget_state継続を実通信で確認。送信側のdeadlineと1要求1接続は維持する。ACL readback、複数同時client、バイト単位read性能、job/cancelは残件。
## MCP画像import endpoint

- forge_import_image / import_imageを追加。1個のlayers.importと明示command envelopeのみ受付。path/sourceHash/fit/サイズ/index/layer ID/contextを指定し、Unity main threadでPaintPngImporterと共有PaintLayerImportを通して既存layer commandへ変換。
- CommandWireImageImportを分離しhost decoderを注入。通常forge_applyではlayers.importは非公開。任意pathは読み取りのみ、入力PNGは16MiB/1024制限とprofile検査。sourceHashは元bytesのSHA256。
- 再送は元fileが存在しhash一致する場合に同じdecoded commandとして再送可能。file消失/変更はdecode前に失敗するため、配送不明時はstate/layersを再確認。元PNGのpath/hashではなくdecoded操作のfingerprintで既存command cacheが比較する点を明示。
- Core260 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-383e0e47df64419a8052ca58985466b5。import envelope/loader注入/fit/追加ID/再送/元bytes変更拒否を確認。MCP transport suite（10 tools）成功。
- Windows-McpImport build成功: Logs/build-player-20260912-084122-167.log。今回importの実PNG実通信は未検証、次に追加する。全体開発継続。
## 画像import共通基盤

- PaintLayerImport.Prepareを分離し、fit/寸法/レイヤー作成をGUIと今後のMCPで共有する構成へ移行。GUI ImportPaintImageは同helperを使用。
- PaintPngImporterをファイル読取と検査済みPaintPngInputのDecodeへ分割。任意のexpectedHashを渡した場合は読んだ元bytesに対しCore RequireSourceHashで検査してからdecodeする。GUI従来呼出はhash指定なし。
- Core260 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0edf8e03fb25446480784ba08c8a29b6。透明余白/色/寸法不一致/元bytes変更拒否を確認。
- Player suite PASS: Artifacts/Authoring-20260912-083720-3f53786d0cff438d9d7e74035234be52/report.json。共通化後のGUI PNG取込/fit/Undo/保存/Surface出力および既存MCP suite成功。
- Windows-SharedImport build成功: Logs/build-player-20260912-083654-162.log。MCP import endpoint自体は次工程。file identityとcommand再送仕様を接続し、画像import実通信を確認する。全体目標継続。
## レイヤー/マスク描画のWindows実通信

- PlayerLayerVerificationを拡張。overlayへの緑描画→ゼロmaskで元画像へ復帰→mask strokeで一部表示→mask除去で描画画像へ復帰→Undo/Redoを実MCPで検証。
- 合成image hashとhasMaskを照合。後続のレイヤー管理、5方向撮影、Surface Bake/readback、native保存も成功。
- Windows-McpMasks build/Player suite PASS: Logs/build-player-20260912-083344-733.log、Artifacts/Authoring-20260912-083412-88944e4245394448907993fce5cc05ac/report.json。今回は検証拡張でCore変更なし（直近259）。
- 継続残件: 画像import、複数経路stroke/再bind、MCP schema/通信lifecycle、複数object/rigなど。全体目標は継続、目視受入は未完了。
## MCPレイヤー/マスク描画

- CommandWireLayerDrawingを分離。layers.stroke、layers.mask.fill（uniform target byte/明示サイズ）、layers.mask.stroke（target byte/strength）、layers.mask.clearを追加。fillは置換、clearはmask除去。既存PaintLayerChange/LayerEditing/Undoへ接続。
- UV点列とRGBA byte検査をCommandWirePaintReaderの共通helperへ抽出。sidecar target/strengthを追加しcapabilitiesと説明を同期。
- Core259 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-67be22bb59bb45a99055f74b0753a7a7。既存layer wire試験を拡張し緑pixel描画→mask追加/描画で背景白→clearで緑復帰→Undo/Redoを検証。sidecar/probe build成功。
- 今回Windows Player再build/実通信は未実施。次にmask描画実通信、画像import、schema整理/通信lifecycle等を継続。最新実通信版はWindows-McpLayers。全体目標継続。
## レイヤー管理のWindows実通信

- PlayerLayerVerificationを独立追加し実MCPで移行/透明layer追加/背景非表示/再表示/名前/順序/削除/Undo/Redoを実行。layerStackのID/名前/順序とimage hashで結果を確認。
- 管理操作後も描画画像hashを保持。layered Paintを最終Outputへ接続して五方向撮影/Surface Bake/native保存が成功。Bakeの中心赤/背景白のpixel readbackも既存fixtureで成功。
- Windows-McpLayers build/Player suite PASS: Logs/build-player-20260912-082938-642.log、Artifacts/Authoring-20260912-083005-1e70c849486b4b768eb9298ee45566c6/report.json。今回はCore変更なし（直近259）。
- layer/maskの描画操作、画像import、schema統合と通信lifecycle、複数object/rig等の全体開発を継続。人間の目視受入とは区別する。
## MCPレイヤー管理の接続

- AuthoringLayerReaderを分離しgraph_inspectにbottom-to-topのlayerStack（ID/name/opacity/visible/hasMask/サイズ/context）を追加。未解決UVではcontextを返さず既存stack情報を保持。
- CommandWireLayerReaderとLayerContextCommandを分離。layers.migrate/add/appearance/remove/move/renameを公開。空の追加layerは透明、サイズとindexを明示。既存LayerEditingでstack/UV/domain競合を検査。
- Core259 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-53bfe88c204f4de09abc87367d8872c4。移行再送/画素保持、透明追加/古いcontext拒否、可視性/並替/名前/削除/Undo/native往復を確認。sidecar/probe build成功。
- 今回Windows Player再build/実通信は未実施。次はレイヤー管理の実通信とlayer/mask描画接続を検証。最新実通信版はWindows-McpPaintOutput。全体目標継続。
## MCP塗装モデルの撮影・出力・保存一周

- PlayerPaintOutputVerificationを分離。実MCPでpolygon edit meshとPaint imageを新Outputに接続→最終出力を変更→5方向撮影→Surface Bake→native保存→元の出力へ復帰を検証。
- Unity側でSurface Bakeを読み戻し64x64、中心pixel赤/背景pixel白を確認。画像付き出力がmesh-onlyに落ちないことを実ファイルで確認。
- Windows-McpPaintOutput build/Player suite PASS: Logs/build-player-20260912-082437-913.log、Artifacts/Authoring-20260912-082459-09687042abb24a7cbd2f5dbf80cec518/report.json。mcp-create.logに塗装モデル一周と既存生成/編集/Undo/保存の成功を記録。
- 今回は既存本体機能の実通信を接続して検証を強化。Core変更なし、直近258passed。native保存要求成功とversionを確認、塗装状態のnativeファイルは後続fixture保存で更新される。塗装Surface Bakeは独立出力として残る。
- Layer/mask/importのMCP対応、撮影の人間目視受入/Unity Bridge実受入、schema/通信回復/複数object/rigなど全体残件を継続。
## MCPペイント接続

- CommandWirePaintReaderとPaintContextCommandを分離。image.paintノード作成（整数width/height）、paint.stroke（paintContext/UV points/pixel radius/sRGB RGBA bytes）を公開。
- PaintEditContext.FromIdentityは明示image/UV/domain hashを検査。graph_inspectにpaintContextとimageOutput概要を追加し、現在の描画対象を観測可能にした。既存PaintEditingによる古いimage/UV拒否とcommand replayを共用。
- Core258 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-e3e2a58f7bd7418ea0b6c25cf85cd960。描画pixel、二重再送、古いcontext拒否、Undo/Redo、native画像hash保存往復、byte範囲とnodeサイズ検査。
- Windows実MCPでPaint node作成/接続→描画image hash変化→再送一回性→古いcontext拒否→Undo/Redo一致が成功。Player suite PASS: Artifacts/Authoring-20260912-082240-ec626bde941143bf89cbd30fff030c0b/report.json。
- Windows-McpPaint build成功: Logs/build-player-20260912-082215-168.log。単層Paintのみ。Layer/mask/画像importや最終出力への画像接続の実通信一周、schema等は引続き残件。全体開発継続。
## MCP頂点/面追加・厚み・UV

- polygon.vertices.add（position）、polygon.faces.create（順序付きelementIds/materialSlot）、polygon.solidify（thickness）、polygon.uv.project（既存投影）を公開。すべて明示contextと既存command/Undo実装を共用。生成IDはedit outputのinspectionで取得する。
- Core257 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f8a3ca08f7a945d9be2315654e23fc73。空polygon→3頂点→取得IDで面作成→厚み→全corner UV→native保存/reopen一致を確認。文字列thickness拒否。
- Windows実MCPで押出し後のUV投影→face inspectionの全corner UV読取→Undo/Redoが成功。Player suite PASS: Artifacts/Authoring-20260912-081844-68b03af346284c3594f5240a75ce3d90/report.json。頂点/面追加と厚みの個別実通信はCore確認と区別して残る。
- Windows-McpPolygonUv build成功: Logs/build-player-20260912-081822-267.log。全体目標継続、Paint接続/追加geometry操作/schema等は残件。
## MCPポリゴン編集

- CommandWirePolygonOperationsを独立追加。polygon.vertices.translate / polygon.faces.extrude / polygon.faces.deleteを既存AuthoringOperationへ接続。contextとcanonical string elementIds、必要時rest-space deltaを指定。重複IDを拒否。
- graph_inspectでpolygon-editの入力contextを公開。face/vertex対象IDはedit nodeの現在outputから取得し、input snapshotのcontextと混同しない。faceless polygonのcontextも取得可能。
- Core256 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-74e2cfde1d1a4aed9ba42f1d5115823d。押出し/移動/削除、再送一回性、Undo/Redo、重複と数値ID拒否を確認。
- 実MCPでpolygon生成→面取得→取得IDで押出し→面数増加確認→Undo/Redo成功。Player suite PASS: Artifacts/Authoring-20260912-081540-80846a1d0dc34223a74e1394d1c0b7c2/report.json。移動/削除の個別実通信はCore試験とは別に残る。
- Windows-McpPolygonEdit build成功: Logs/build-player-20260912-081515-472.log。追加polygon操作、UV/Paint、schema統合、job/通信回復、複数object/rig等の全体残件を継続。
## MCPポリゴン生成wire

- CommandWirePolygonReaderを分離追加。mesh.polygon-sourceのdomainId、明示頂点/面/corner ID、座標、materialSlot、scale/translationを解析。mesh.polygon-editの空payload生成も公開。
- IDはゼロなしcanonical decimal string。既存PolygonMeshの参照/重複/予算検査を共用。wireは4096頂点/1024面、実IPCは従来65536bytes上限。現profileはcorner属性なし。UV/normal/tangent付sourceは未公開。
- sidecar PolygonCommand DTOを分離。capabilitiesとforge_apply説明を更新。
- Core255 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-3b591cdbac2b4ec7be7726b2f2f26bc5。完全IPC envelopeから生成/同ID再送/面取得、2^53超ID保持、数値ID/非canonical ID/欠損参照拒否を確認。
- Windows実MCPでpolygon node生成→face取得→node削除が成功。大きなface ID/corner順保持を確認。Player suite PASS: Artifacts/Authoring-20260912-081231-e895d4a043604767b2ec546253d9594e/report.json。
- Windows-McpPolygon build成功: Logs/build-player-20260912-081207-569.log。polygon編集操作wire、context取得、UV/Paintなどは継続残件。
## MCP面inspection公開

- forge_faces_inspect / faces_inspectを公開。既存頂点要求をMeshPageRequest/MeshPageQueryへ改名して共用、Unity meta GUIDを維持。面は64件上限、document/revision/snapshotで取得対象を固定する。
- Core254 passed/0 failed（C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-084747b4431f40e983cb86f03700cd2a）。face envelope解析と65件拒否を追加。MCP transport suiteと9 tool registry成功。
- Windows実MCP Player suite PASS: Artifacts/Authoring-20260912-080853-c8a678be47544305a30b33d498122370/report.json。triangle-onlyへの面要求拒否、その後のstate取得と編集/撮影/出力/保存継続を確認。
- Windows-McpFaces build成功: Logs/build-player-20260912-080823-241.log。
- polygon面の正常読取/疎ID/UVはCore検証。実MCPのpolygon正常読取と面編集は次のpolygon生成wire接続で確認する。全体開発は継続。
## 面inspectionと共通mesh解決

- AuthoringFaceReaderを独立追加。polygon面を安定ID順に最大64面/ページ、各面のmaterialSlotと順序を保持したcorner ID/vertex ID/UVで返す。64bit IDは文字列。最大256corner/面の既存制約と合わせページ量を制限。
- InspectionMeshへdocument/revision/node/current snapshot検証を共通化し、頂点/面readerが同じ現在評価を参照。triangle-only meshには架空のpolygon IDを与えずPOLYGON_REQUIRED。faceless polygonは空ページ。
- Core254 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b055072b7d13434fab76e84e1c3ea403。2^53超の疎ID、corner順/UV、位置transform、結果の分離、範囲/空面を確認。
- 面APIはCoreのみ。MCP公開とpolygon graph生成/編集のwire接続を次に進める。Windows再buildは未実施、最新実通信成功版はWindows-McpVertices。全体目標は継続。
## MCP頂点inspection接続

- VertexPageRequest（Core wire検査）とVertexPageQuery（sidecar DTO）を分離しforge_vertices_inspectを公開。GUI main threadからAuthoringVertexReaderへ接続。document/revision、node input/output、snapshotHash、offset/countを明示する。
- Windows-McpVertices build成功。実MCPで2ページから4頂点のID/位置/identity/終端を確認し、取得IDで頂点編集→撮影→出力→native保存まで成功。
- Player suite PASS: Artifacts/Authoring-20260912-080338-d923003d07994fa1b3f40cc3422f4643/report.json。build log: Logs/build-player-20260912-080314-800.log。
- Core253 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-04a45b911866462e8cbe609511110862。wireの文字列数値、巨大整数、範囲、port拒否を追加。MCP transport suite成功。
- polygon疎ID/transformの追加検証、face情報/selection、polygon/PaintのMCP編集、schema統合、通信回復・job管理、複数objectとrig等は継続残件。全体完成ではない。
## 頂点inspectionのCore基盤

- AuthoringVertexReaderを独立追加。node input/outputの現在評価から、rest/avatar位置を最大1024頂点ずつ返す。document/revision/snapshotを毎ページ検証し、未解決入力には古いpreviewを代用しない。
- mesh頂点indexとpolygon安定IDを区別。polygon IDはulong精度を失わない10進文字列。offsetはIDではなくID順の位置。空の末尾ページを許可しnextOffsetで終了を示す。
- Core252 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-cf95f3667fdf47dfaaa4bdd39162b3ad。ページ境界、位置、snapshot不一致、revision変更、未解決入力拒否を確認。
- この段階はCore APIのみ。MCP要求/sidecar公開、polygon疎IDとtransformの追加確認、Windows実通信は次の作業。Player再buildは未実施。全体開発は継続。
## MCP標準材質と材質付き出力

- CommandWireMaterialReaderを分離しmaterial.standard/mesh.assign-materialを公開。linear RGBA/metallic/roughness/emission/alphaMode/cutoffを必須型で指定し、既存MaterialParametersの範囲検査を共用。sidecar NodeParametersとcapabilitiesを拡張。
- 初回Windows-McpMaterialの外部生成はIPC切断で失敗。材質配列を含むgraph envelopeが旧depth8を越えるためdepth12へ修正し、完全なIPC envelopeのCore解析回帰を追加。
- Core251 passed/0 failed（C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-a9fa054209bc4065a7c59c7dd7180a94）。材質値/alpha保持、不正値・文字列enum拒否、nested envelope解析を確認。
- Windows-McpMaterialDepth build/McpProbe付きPlayer suite PASS: `Logs/build-player-20260912-075303-189.log`、`Artifacts/Authoring-20260912-075327-688685f7948844a0858175607e11c207/report.json`。外部MCPで材質付きgraph生成→頂点編集→5方向画像→Material Bake→保存。PlayerからBakeを読み直しRGBA主要RGB/metallic/roughness一致を確認。既存suite成功。
- 今回は画像textureなしの単一材質。画像/Paintや複数材質のMCP生成・個別往復、材質表示の目視確認、polygon編集等は残件。次は選択に必要な頂点情報/inspectionとpolygon/paint接続を進める。全体goal継続。
## MCP export公開

- ProjectExportRequest/ExportProjectCommandとWorkbench.McpExportを分離しforge_export公開。GUIで選択済みproject directoryとdocument/revision/明示GUID exportIdを指定。出力先はproject/exports/exportId。既存directory/fileはEXPORT_DESTINATION_EXISTSとして拒否し、同ID再送で上書きしない。
- ProjectExportServiceで現在revisionとcomplete状態を検査し属性に応じ既存Bake形式へ出力。resultはsuccess/code/manifestPath/kind/document/revision/hash。native保存や文書変更とは独立。
- Core250 passed/0 failed（C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4884ba4e2e314a9aa434095eac00ca5e）、MCP transport suite成功。
- Windows-McpExport build/McpProbe付きPlayer suite PASS: `Logs/build-player-20260912-074737-361.log`、`Artifacts/Authoring-20260912-074804-264ec9eb97c641b08519cbff1f20cc16/report.json`。外部MCPでstatic出力、再送拒否/bytes保持/state不変、Player側Bake再読込mesh hash一致を確認。既存生成/編集/撮影/保存/GUI suiteも成功。
- MCPでのSurface/Material/MultiMaterialの個別往復、外部Unity Bridge実受入、directory競合の別process同時操作/reparse point境界は未検証。export失敗時の部分blob清掃やidempotent成功再取得も未実装（既存先拒否）。次はpolygon/paint等の制作範囲とschema/inspection/通信回復の残件を進める。全体goal継続。
## 明示revisionの共通export service

- Persistence/ProjectExportServiceを追加。instance/document/revisionを確認し、現行complete評価の属性からMesh/Surface/Material/MultiMaterialの既存BakeStoreへ振分け。書込実装は重複せず、材質/画像を落としてmesh-onlyへfallbackしない。結果はmanifest path/kind/document/revision/hash。
- GUI Exportを同serviceへ移行。Core250 passed/0 failed（C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-1bd278dfc7e2485faae234b1056ad7a6）。mesh/material出力と読込、状態/dirty不変、古いrevisionではdirectory未作成を確認。Surface/MultiMaterialの既存試験もsuiteに含む。
- Windows-ExportService build/McpProbe付きPlayer suite PASS: `Logs/build-player-20260912-074404-388.log`、`Artifacts/Authoring-20260912-074429-fcc28d78d95747aca8411e69b2040d70/report.json`。既存GUI書き出し、MCP編集/撮影/保存/生成など成功。
- MCP export toolはまだ未公開。次は同serviceへtyped export要求をつなぎ、GUI project配下の新規出力directoryとmanifest応答を検証する。出力先アプリの実受入は別gate。polygon/paint/schema/通信回復等の全体残件を継続。
## MCP native保存の実Player接続

- ProjectSaveWireReaderとSaveProjectCommand、Workbench.McpSaveを分離追加。forge_save_projectはget_stateのsaveTarget（GUIで選んだdirectory/期待saveVersion）とdocument/revisionを指定。別directoryはSAVE_TARGET_CHANGED。ProjectSaveService経由で保存しGUIのsavedDirectory/path/dirty表示を同期。
- 保存は既存projectの破棄/openや任意path選択をしない。保存直前の文書identity/revisionとdisk saveVersionを検査。通信成功とは別にsuccess/codeを返し、同要求再送はSAVE_CONFLICT。
- Core249 passed/0 failed（C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-7effe577ce2c49a29408f8a812f3b278）、MCP transport suite成功。
- Windows-McpSave build/McpProbe付きPlayer suite PASS: `Logs/build-player-20260912-074103-215.log`、`Artifacts/Authoring-20260912-074128-8c20b0343904493a854a92107a0e0e44/report.json`。static/graph両方で実MCP保存、再送競合、dirty解除/revision不変を確認。Playerから保存物を再openしてstateHash一致とGUI savedDirectory一致を確認。出力は各fixtureのmcp-static-native/mcp-created-native。
- 保存先はGUI選択済みpathに限定。ファイルpicker/保存先変更のMCP機能、layout保存のMCP同期、入力破損・停止等の通信回復試験は残件。次はexport接続とpolygon/paint/schema等の制作一周に必要な機能を進める。全体goal継続。
## 明示revisionの保存service

- Persistence/ProjectSaveServiceを追加。不変ProjectSaveRequestでinstance/document/revision/directory/expectedSaveVersionを指定し、workspace.Gate内で一致確認してから既存ProjectStore.Saveへ渡す。保存結果はdocument/revision/hash/version/正規化directoryの不変record。書込仕様とblob処理を重複しない。
- Core249 passed/0 failed（C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-eb3098bf58a647d08e9af01eb5a829c7）。編集後の古いrevision拒否とmanifest未作成、保存/再読込hash一致、同expectedSaveVersionの再要求拒否と既存bytes不変、別instance/document拒否を確認。
- 保存再送は現状SAVE_CONFLICTとなる。編集commandのidempotencyと混同しない。次は保存要求のwire/sidecar/GUI同期を接続し、保存先境界と明示version、保存結果の再取得を定義する。MCP保存はまだ未公開、GUIは従来ProjectStore呼出のまま。Playerは今回未build。全体計画継続。
## MCPへ五方向の画像コンテンツ返却

- Workbench.McpCaptureを分離。最終Ready snapshotを一回取得し、既存EvidenceModelCaptureで正面/背面/左/右/斜め256pxを撮影。metadataとcapture record、PNGをinline返却。ディスクへ保存せず、記録内filenameはinline画像のhash識別用。
- sidecar CaptureResultが5つのImageContentBlockとmetadata/cameraのText/StructuredContentへ変換しforge_capture公開。base64をtextへ重複掲載しない。IPC応答上限は4MiB。
- Windows-McpCapture buildとMcpProbe付きPlayer suite PASS: `Logs/build-player-20260912-073529-842.log`、`Artifacts/Authoring-20260912-073554-6d0a0c0e9e9f4d2ea6de0d3b32b2e29f/report.json`、mcp-external.log。外部MCPから編集後モデルを撮影し5画像block/PNG signature/256px/sha256/取得stateHash/snapshot一致を検証。既存編集/生成/GUI suiteも成功。Core suiteは今回は未再実行（直近248）。
- 最終結果/固定256pxのみ。node対象・カメラ指定/比較・job管理・途中取消はMCP未接続。同期main-threadで5枚撮るため重いモデルではtimeout検証が必要。metadataのcaptureStatus=not_requestedは取得記録で、別capture recordのcompleteが撮影結果。実AI clientで画像が表示された人間の目視受入とは区別する。
- 次はnative保存/exportのMCP接続と頂点情報等のinspectionを進め、空から小物制作を一周させる。schema/通信回復/ACLや全体制作計画の残件も継続。
## MCP graph頂点編集と上流identity検査

- GraphEditContext.FromIdentityとCommandWireContextReaderを追加。明示graph/node/inputSnapshot/domainを検査し、既存TranslateGraphVerticesへ渡す。現在のcontextで勝手に補完しない。graph_inspectは対応EditMeshのeditContextと入力vertexCountを返す。
- sidecarへEditContextCommand、graph.vertices.translateを接続。deltaはrest空間、vertexIdsは入力render index。polygonのstable IDは別の未公開経路。
- Core248 passed /0 failed（C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f96e96a839c545cdb8c8f0aa11377289）。直接GraphEditingとのmesh hash一致、上流変更後のEDIT_CONTEXT_STALE拒否と文書不変を確認。
- Windows-McpGraphVerticesのbuild/McpProbe付きPlayer suite PASS: `Logs/build-player-20260912-073204-590.log`、`Artifacts/Authoring-20260912-073229-61f9ed1a9276475f93d36feb73ed8d07/report.json`。実外部MCPでplane→edit→output生成、inspectからcontext取得、頂点移動、上流サイズ変更、旧context拒否を確認。既存suiteも成功。
- 頂点位置のchunk取得、専用forge_edit_context/selection ID、polygon/paint/Mirror等のMCP拡張、Evidence画像返却と保存/export、schema統合、通信回復/ACL検証などは残件。全体計画継続。
## 外部MCPから空Playerへgraph生成

- sidecarのGraphCommand/NodeCommand/NodeParameters/EdgeCommandを別ファイルへ追加し、ApplyOperationへgraph/node/newObjectIdを接続。capabilitiesに生成/node追加更新削除/output指定と対応node3種を掲載。node更新はpayload置換であることをtool説明に明示。
- PlayerCreateVerificationを独立追加。実外部MCPで空→明示IDのplane/output graph生成→同command再送→Undoで空→Redo同hash→plane幅更新を確認。元workspaceはPlayer verifierのfinallyで復元。
- Windows-McpCreateGraph buildとMcpProbe付きPlayer suite PASS: `Logs/build-player-20260912-072835-290.log`、`Artifacts/Authoring-20260912-072901-934ff339c4734ba6b5896f233bd17879/report.json`、mcp-create.log。既存static編集/replay/conflict/UndoとGUI suiteも成功。Coreは前回247件、今回は再実行なし。
- 対応nodeはplane/edit/outputに限定。他のnode、graph頂点context、polygon/paint、native保存/export/captureのMCP公開は次。空projectはユーザーが開いたものを利用し、既存制作物を破棄するproject.create/openはまだ公開していない。全体計画継続。
## Graph生成wireのCore拡張

- AddGraph(graph,objectId) overloadを追加。既存GUI用AddGraph(graph)はGUID生成を維持し、transportは明示object IDを使用して再読込/再送時のfingerprintを安定化。
- CommandWireGraphReaderを別partialファイルへ分離。object.add_graph、graph.node.add/update/remove、graph.outputを解釈。graphId/outputNodeId/nodes/edgesとnodeId/typeId/version/parametersを厳密に検査。初期node wire対応はprimitive.plane、mesh.edit、mesh.output。parameter payloadの編集履歴や他node種は未対応。
- Core247 passed/0 failed（C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-2af4495646d544ba815592de22bc63b7）。空workspaceへ明示IDでplane→outputを生成、再送でrevision不変、Undoで空に復帰、Redoで同object ID/mesh hash、version型拒否を確認。
- Sidecar ApplyOperation DTOはまだgraph/node payload未対応。capabilitiesのremoteOperationsも前回5種のままで、新生成経路は未公開扱い。次はtyped DTO/schemaとcapabilitiesを接続し外部MCPから空→graph生成→編集をPlayerで検証する。Player再buildは今回は未実行。全体計画は継続。
## MCP applyの実Player接続

- AuthoringReadRequestをAuthoringIpcRequestへ改名。apply時のみcommand envelopeを受け、outer/inner instance一致を検査。上限64KiB。typed ApplyCommand/ApplyOperationをsidecarへ追加しforge_apply公開。任意コード実行や現在revision補完はしない。
- Workbench.McpCommandsがmain-threadでcommands.Execute(envelope,projection)を呼び、選択/GUIをrefresh。個別CommandResultのsuccess/code/revision/hash/evaluationを返す。通信成功とcommand成功は別。再送はcommand IDを維持し、sidecar自動再試行なし。
- capabilitiesへapplyと5種のremoteOperationsを掲載。static vertices.translate、graph.connect/disconnect、history.undo/redoのみ。グラフ頂点編集・node生成・polygon/paint/project作成等はまだ未公開。
- Core246 passed/0 failed（C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b7e2322ec4314686abe0307de3cbc581）。MCP transport suite PASS。
- Windows-McpApply buildと外部McpProbe付きPlayer suite PASS: `Logs/build-player-20260912-072334-663.log`、`Artifacts/Authoring-20260912-072359-59a2e8b6e5ed4c768fa38454fba4c92a/report.json` と mcp-external.log。実Playerを外部MCPから頂点移動→同ID再送→旧revision拒否→Undoし、stateHash復元を確認。既存GUI suiteも成功。編集中画面の目視は別途。
- 次はgraph edit context/typed node生成等を接続し、空から小物を作るMCP経路を実証する。schema正本の統合、タイムアウト/停止/不正入力回復の追加検証、Evidence画像返却、全体制作計画は継続。
## 型付きcommand wire reader

- Commands/CommandWireReaderを独立追加。expectedInstanceId/documentId/revision/commandId/objectId/baselineHashを必須指定し、現在値による補完やcommand ID再生成をしない。operationごとのfield/type検査を経て既存AuthoringOperation factoryへ変換。
- 初期対応はhistory.undo/redo、static vertices.translate、graph.connect/disconnect。1..64 operation、頂点ID最大4096。未知operation、余剰field、文字列revision等を拒否。graph編集context・node生成・polygon/paint等は次の拡張対象で、MCPへはまだ公開していない。
- Core246 passed / 0 failed。wire経由と通常編集のmesh hash一致、同command再送がrevisionを増やさない、古いrevision拒否、Undo、未知payload/type拒否を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-260192484c734482abbe1685a73e6475`。
- 次はwire envelopeをMCP/IPCへ接続し、projection付き既存command serviceをmain-threadで呼びGUIを同期する。成功/失敗はcommand結果を返し、timeout後の再送は同command IDを維持。remoteEditingは接続完了までfalseのまま。Playerビルドは今回は未実行。全体計画継続。
## MCP graph inspection

- AuthoringGraphReaderを独立追加。workspace.Gate内でdocument/revision/hashとgraph node/edge/port/diagnosticsを取得。instance依存portはBuiltinNodes.Findで解決。現在のevaluationのmesh input/output hash/domain/renderableだけを返し、stale previewを代入しない。空文書はgraph=null。
- forge_graph_inspectをsidecarへ接続。現在の単一graph全体（最大128node/512edge）の概要を返す。形状payload/parameter値/parameter schema/対象filter/cursorはまだ未提供。
- Core244 passed/0 failed（C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-5eae18a3a01c497a80b8c73558045c36）。切断後のdiagnostic/current nullと古い取得値の不変性、空/別instance拒否を確認。MCP transport suite PASS。
- 外部probeは一時的な非空fixtureを読み、元workspaceをfinallyで復元。Windows-McpGraphInspect buildと外部McpProbe付きPlayer suite PASS。`Logs/build-player-20260912-071649-194.log`、`Artifacts/Authoring-20260912-071714-7fa60a36b0ab4b8a8ed2f536a68fd011/report.json`。実graphのnodes/edges/evaluationとdocument/hash、capabilities/stateも確認。
- 次はtyped編集要求と既存command envelopeを接続し、revision/commandIdの契約を維持してGUIと同じ結果を実証する。対象filter/schema等のinspection残件、Evidence captureのMCP公開、通信回復/ACLの追加検証、全体制作計画も継続。
## MCP capabilitiesとread protocol分離

- Core Inspection/AuthoringReadRequestとAuthoringReadServiceを追加。requestはUTF8/4KiB/depth8/重複field/余剰field/必須型/GUID/versionを検査し、文字列version等の暗黙変換を拒否。listenerはrequest解析と業務処理を委譲し、methodをmain-threadへ渡す。
- forge_capabilitiesを公開。アプリのBuiltinNodes.Definitionsからtype/version/portを生成し、instance依存portを明示。remoteMethods、remoteEditing=false、単位/空間、graph上限を返す。ローカルnode対応とMCP編集対応を混同しない。
- Core 243 passed/0 failed（C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-3a17ffb5303b4336abf6ee4db7b25964）。MCP transport/protocol suite PASS。Windows-McpCapabilities buildと外部McpProbe付きPlayer suiteもPASS: `Logs/build-player-20260912-071336-472.log`、`Artifacts/Authoring-20260912-071402-0d046bedc5fd4f338c7a86ff6bc66428/report.json`。実capabilities＋3回state取得を確認。
- capabilitiesのparameter schema/出力profile属性契約はまだ未掲載。未知/破損request後の実Player回復、停止/切替/ACL検証も残件。次はgraph inspectionを同read serviceへ接続し、共通command/Evidence接続へ進む。全体goal継続。
## 実外部MCPからPlayerへの状態取得成功

- Tests/Mcp.Transport/PlayerStateVerificationとWorkbench.McpExternalVerificationを分離追加。明示McpProbe時のみ、Playerが外部.NET probeを起動→公式SDK client→stdio sidecar→named pipe→実Player main-thread readerへ接続する。3回連続でinstance/document/revision/stateHashを期待値と照合。
- 初回Windows-McpEndToEndはstate call失敗。応答送信直後のDisconnectNamedPipeで未読outputが失われ得るため、応答後はclient closeまでread待機し接続deadlineで上限を設定。外部probeへserver stderrも記録。
- 修正後Windows-McpResponseLifetimeのPlayer suite PASS。`Logs/build-player-20260912-070956-459.log`、`Artifacts/Authoring-20260912-071017-c76730a77cc5402cae7f3dd31e8f4759/report.json` と `mcp-external.log`。3連続get_state成功、既存GUI suiteも成功。
- 再現: `dotnet build Tests/Mcp.Transport/Mcp.Transport.Tests.csproj` 後、`Tools/Test-NyaForgeAuthoring.ps1 -BuildName Windows-McpResponseLifetime -McpProbe Tests/Mcp.Transport/bin/Debug/net10.0/Mcp.Transport.Tests.dll -TimeoutSeconds 600`。
- 接続の実証範囲は状態取得のみ。次はprotocol入力検査/切断・停止・切替の回復、capabilities/graph inspectionと共通command/Evidence接続を進める。MCP全体と制作全体は未完了。
## Unity named pipe互換性修正・Player成功

- 診断buildのstackでSystem.Security.Principal.WindowsIdentity.get_OwnerのNotImplementedを特定。`Artifacts/Authoring-20260912-070417-a2479b023c2f4da5999ffb0d78241392/player.log`。CurrentUserOnlyの内部処理で発生していた。
- Platform/WindowsAuthoringPipeへnative作成を分離。process tokenのTokenUser SIDを取得し、そのSIDだけへのprotected DACLを指定。CreateNamedPipeWはduplex/overlapped/first-instance/remote拒否。SafePipeHandleをNamedPipeServerStreamへ渡して以後の通信を共用。SID/descriptor/tokenのnative資源をfinallyで解放。
- listenerは1個のserverを保持し、各要求後Disconnectして次の接続を待つ。作り直しと旧client handleの競合を避ける。
- Windows Player build/suite PASS: `Builds/Windows-McpNativePersistent/NyaForge.exe`、`Logs/build-player-20260912-070600-589.log`、`Artifacts/Authoring-20260912-070621-e5fea6101463414db4c2671103b51389/report.json`。Player内の別thread client→named pipe→main-thread state readerのinstance/document一致を確認。既存suiteも成功。
- 実外部MCP client→sidecar→このPlayerの一気通貫は次。複数要求/停止再開/文書切替/不正入力回復/ACL読戻しと別user拒否はまだ追加検証が必要。MCP編集/画像など全体計画は未完了。
## Unity受信処理の初回実装・互換性失敗

- Platform/AuthoringPipeServerとAuthoringWorkbench.Mcpを分離追加。単一client/要求上限4096byte/8秒timeout/main-thread Pump/停止時dispose、明示開始/停止とinstance欄を実装。現在はget_stateのみ。
- Windows-McpListenerVerifiedはビルド成功。しかしPlayer実通信は失敗。`Artifacts/Authoring-20260912-070222-204d7604dce743ae93f6c8145cd38132/report.json` は passed=false、IPC test canceled。player.logのAI接続停止理由は「The method or operation is not implemented.」。Unity Monoで使用したNamedPipeServerStream/CurrentUserOnly周辺の互換性問題が疑われるが、正確な呼出箇所のstackはまだ未取得。失敗を接続成功と扱わない。
- 次はFailureにstackを残して箇所を確定し、Windows native named pipeと明示ACLのadapter等で互換性を解決する。CurrentUserOnlyを単に外す回避はしない。新GUIからのMCP接続はまだ利用不可。最後の機能確認済みPlayerはWindows-EvidenceCamera。
- Core/MCP側既存試験の成功とは別のUnity runtime失敗。全体goalは継続、外部入力待ちのblockerではない。
## MCP protocol往復と状態取得Core

- Tests/Mcp.Transport/McpProtocolVerificationは公式SDK clientで実sidecar子processを起動。stdio接続、forge_get_stateの一覧、tool call→named pipe→結果JSON、IPCエラーがMCP IsErrorへ伝わることをPASS。受信側はテストpipeでありUnityではない。旧通信試験もPASS。
- Authoring/Inspection/AuthoringStateReaderを独立追加。workspace.Gate内でinstance/document/revision/hash/dirty/saveVersion/Undo/Redo/評価状態/object概要を同時取得し、コピーJSONを返す。sourceEpochは未対応のnull。別instanceとcommand実行中の再入を拒否する。
- Core 242 passed / 0 failed。空/編集前後/コピー隔離/別instance拒否を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-be5cac2a60b8461ea4481b170e432a97`。Playerビルドは今回未実行。
- 次はUnity側pipe listener、main-thread dispatch、instance表示/寿命管理へ接続し、実Player→MCPでstateを確認する。現時点では実アプリ接続/編集は未完成。全体計画は継続。
## MCP sidecarの初期実装

- Tools/NyaForge.Mcpを独立.NET10プロジェクトとして追加。公式SDK 2.2.0、lockfile、stdio host、forge_get_state入口、instance GUID必須のnamed pipe client。Unity listenerは未実装なので実アプリにはまだ接続できない。
- build 0 warning/error。Tests/Mcp.Transportは実named pipeで正常応答/別instance拒否/接続不可時取消をPASS。実MCP clientとのprotocol往復は未検証。
- [MCP実装メモ](docs/Mcp-Integration.md)へ依存の版/license、IPC上限/相関ID、残件を記録。次はMCP protocol往復とUnity側main-thread dispatch/listenerを実装する。既存全体計画は継続。
## 保存済みカメラでの比較撮影

- AuthoringWorkbench.EvidenceCameraを独立追加。比較toggle、比較元capture manifestのpath欄、最後の成功セットを比較元にするボタンを提供。再起動後も保存済みpackageのpathから同条件の撮影が可能。
- 比較時はEvidenceCaptureReaderでpackage全体の整合を確認し、記録済みviewの位置/注視点/up/倍率/clip/解像度/順序を再使用する。自動fitしない。元packageが欠落/破損していれば失敗とし、新規自動fitに切り替えない。撮影開始時にviewを取得し入力欄をlockする。
- Windows Player build/suite PASS: `Builds/Windows-EvidenceCamera/NyaForge.exe`、`Logs/build-player-20260912-065316-388.log`、`Artifacts/Authoring-20260912-065341-0956acac415343fc9f7b301e955862e9/report.json`。GUI開始実クリック、編集後の全camera値一致、PNGの変化、入力lockと欠落source拒否を検証。既存suite成功、Core変更なし。
- 比較元はpath指定（ファイルpicker未接続）。元の画像/metadataを含むpackage一式が必要。新画像にcamera条件は保存されるが比較元capture IDの専用関連付け、並列画像比較UI、実際の再起動操作は未検証/未実装。形状変更ではみ出す場合は自動補正しない旨をGUIに表示。
- 次はEvidence/MCP接続計画に従いMCPの依存/通信方式を確認してadapterへ進む。全身/rig/weight/morph等を含む全体計画は継続。
## 撮影対象のノード入力・出力選択

- AuthoringWorkbench.EvidenceTargetを独立追加。撮影対象dropdownに最終結果と各対応mesh portの入力/出力を表示。node IDで選択を維持し、対象削除/文書切替で最終結果へ戻す。未解決/面なし対象は撮影不可、撮影中は選択不可。
- 撮影開始時に選択対象のEvaluatedSnapshotを取得し、以後の編集から独立して保存する。最終結果が未完了でも解決済みnodeを撮影できるCore契約を使用。
- Windows buildとPlayer suite PASS: `Builds/Windows-EvidenceTarget/NyaForge.exe`、`Logs/build-player-20260912-065044-155.log`、`Artifacts/Authoring-20260912-065106-5351402f39444057b8df251c5c949b4e/report.json`。新GUI検証ではnode出力を選択→開始実クリック→選択ロック→保存metadataのnode ID一致と文書不変を確認。既存suiteも成功。
- Core変更なし。入力port選択/未解決node/削除時fallbackのGUI実操作は未検証。次は固定cameraの比較撮影とMCPへ進む。全体計画の未実装範囲は継続。
## 厚み・テクスチャ付き五方向撮影の検証

- EvidenceTexturedVerificationを独立追加。自作planeへ厚みを付けた6面モデル、赤/青の8x8 texture、標準材質を使い、正面・背面・左・右・斜めの撮影を検証。
- 全方向に着色pixelがあり、正面/背面に赤と青が残ること、材質数1・画像hash数1・polygon面数6、capture packageの保存読込とPNG hash一致を確認。斜め画像を目視し厚みと色分けを確認。
- 最新確認済みPlayer: `Builds/Windows-EvidenceTextured/NyaForge.exe`。build log: `Logs/build-player-20260912-064446-034.log`。PASS: `Artifacts/Authoring-20260912-064510-2fd5d2b4c880499baff906746e8b2fb7/report.json`。既存Player suiteも成功。Core変更なし（直近241件、今回は再実行なし）。
- 簡単な自作fixtureの検証であり、アバター全体や複雑な材質の品質保証ではない。次はnode対象指定・固定camera比較のGUI接続、MCP。全身制作・rig/weight/morphを含む全体計画は未完了。
## 保存結果へのGUI導線

- AuthoringWorkbench.EvidenceResultを独立追加。最後に成功したcapture manifestのreadonly欄、保存先フォルダを開く、パスコピーを提供。現在の試行のlastEvidencePathと成功済みsuccessfulEvidencePathを分離し、中止/保存失敗で成功結果を消さない。
- ファイル存在時だけ結果ボタンを有効化。フォルダは成功結果の親directoryをUseShellExecuteで開き、パスを引数文字列へ連結しない。コピーはユーザーのボタンクリック時にのみ行う。
- 最新確認済みPlayer: `Builds/Windows-EvidenceResult/NyaForge.exe`。log: `Logs/build-player-20260912-064221-184.log`。PASS: `Artifacts/Authoring-20260912-064246-b344be026d424d3ab6391368697fce3f/report.json`。保存結果path/ボタン有効性/親directory、中止・失敗後の成功結果保持と既存suite成功。Core変更なし（直近241件）。
- OS Explorerの起動とclipboardは自動fixtureで実行していない。結果欄の長いパス表示、削除済みファイル時の操作、再起動後の履歴保持は追加対象。成功結果は現在の起動中だけ保持する。
- 次は厚み付き・材質/texture付きモデルの五方向captureを検証し、node対象指定/固定camera比較をGUIへ接続する。MCP等の全体計画も継続。

## Evidence中止/失敗/文書切替の検証

- AuthoringWorkbench.EvidenceLifecycleVerificationを分離。開始/中止の実クリック→directory未作成/manifest未公開、既存fileを保存directory指定→失敗/既存保持/controls復帰、正しいdirectoryへ再試行を確認。
- 撮影開始直後に表裏頂点を移動し、保存metadataが開始時stateHashを持ち、現在の編集が巻き戻されないことを確認。さらに撮影中に空workspaceへ切替え、保存は旧document ID、GUIは空のままを検証。
- 別文書に切り替わった場合は完了欄へ「撮影開始時の文書の結果です。現在の文書とは異なります。」を表示。改行依存の初回置換が適用されなかったことをrgで検出し、finallyの実コードへ適用して再build。
- 最新確認済みPlayer: `Builds/Windows-EvidenceLifecycleStatus/NyaForge.exe`。log: `Logs/build-player-20260912-064006-489.log`。PASS: `Artifacts/Authoring-20260912-064029-ea5e588ebf884eef82254a581ae4faee/report.json`。上記と既存suite成功。Core変更なし（直近241件）。
- 次は材質/textureを持つ厚みのあるモデルの5方向capture検証、保存結果へアクセスするGUI、現view固定比較/対象node指定へ進む。現在中止は最初のframe待ちで試験、描画後/保存中断やOS終了時の中断復旧は未検証。全体計画/MCP/rig等も継続。

## Evidence五方向プリセットとGUI

- EvidenceViewPresets.FiveViewsはbounds中心と共通倍率で正面(+Z)/背面/左(-X)/右/斜めを生成。固定球半径に余白を付け、各viewで個別にfitしない。
- AuthoringWorkbench.EvidenceCaptureを分離。「モデルの確認画像を保存」foldout、保存directory、開始/中止、進捗/保存先を提供。最終Ready meshのsnapshotを1回取得し、各frameに1枚撮影。二重起動禁止。途中で制作が変わっても取得済みsnapshotを使う。全画像が揃うまでset保存しない。
- 最新確認済みPlayer: `Builds/Windows-EvidenceGuiVerified/NyaForge.exe`。log: `Logs/build-player-20260912-063701-611.log`。PASS: `Artifacts/Authoring-20260912-063727-6cc4eddfe67c4436a876e6866f4aba93/report.json`。開始ボタン実クリック、busy排他、5枚/異なるcamera位置/共通倍率、set読込と文書hash不変、既存suite成功。evidence-gui.pngを目視し説明/保存欄/開始ボタンを確認。
- Core単独suiteは今回未再実行（直近241件、Player compileとsuite成功）。平面fixtureなので側面が線/背景になる場合は正常。厚みのある物体/材質付き5方向の画像品質は次。
- 次は中止の実クリック、撮影中の編集/新規切替、保存失敗後の再試行、保存完了欄の表示/フォルダを開く導線を確認。Unity unload完了待ち/複数job queue、空/面なし撮影、GUIのnode対象指定、MCPは残件。全体計画も継続。

## Capture readerと実画像保存往復

- EvidenceCaptureReaderは専用schema/項目型、1..8枚、index順、profile/背景/照明識別、camera軸/clip/解像度を検証。metadataの固定filename/ID/hash/Ready状態、PNGのhash名/内容hash/構造/寸法を照合。結果はコピー隔離したrecordでありgeometryを再構築しない。
- Core **241 passed / 0 failed**（既存テスト拡張）。保存読込のbytes/ID一致とコピー隔離、../file拒否、解像度不一致、PNG改変拒否を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-5cef58a4b56e4c3c86dca91a6f7d68c2`。
- 最新確認済みPlayer: `Builds/Windows-EvidencePackage/NyaForge.exe`。log: `Logs/build-player-20260912-063348-006.log`。PASS: `Artifacts/Authoring-20260912-063411-07ec79e130f746279ef605cfd1ee72fe/report.json`。実capture画像2枚（同じview再撮影）をevidence-packageへ保存/読込しsnapshot ID/枚数/PNG bytes一致。既存suiteも成功。
- 実画像の異なる多方向view、材質/画像付きモデル、GUI入口/queue/取消、環境識別（graphics/color space）と各profile拡張は次工程。Readerはhash整合を検査するが署名認証ではない。同じview2枚のテストを多方向検証と扱わない。全体計画の残件も継続。

## Evidence画像セットの保存Core

- EvidenceCaptureSetは1..8枚、同じ取得snapshot参照のみを許容。入力配列はclone。EvidenceCaptureCodecは専用kind=nyaforge.evidence.captureとしてcamera/解像度/profile/背景/照明識別/PNG hashと元metadata file/hashを記録。単独metadataは撮影なしの取得記録として不変、撮影完了は別capture recordで示す。
- EvidenceCaptureStoreはmetadata→hash名PNG→capture manifestの順で公開。既存ファイルは同内容のみ再利用し異内容を拒否。失敗時は公開済みblobが残り得るが、新capture manifestを先に公開しない。既存captureを置換しない。
- Core **241 passed / 0 failed**。metadata hash/ID、PNG bytes/camera参照、同set再保存、配列隔離、異snapshot混在/9枚/PNG寸法不一致拒否、既存PNG不一致時の新manifest未公開と既存保持を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-79ab586bc70446eb85afe093c85bb3b0`。
- 今回は合成PNGによる保存契約テストで、実撮影画像のstore接続は次。Playerビルドなし。capture reader、camera/画像hashの再読込検証、実撮影→保存のUnity fixture、GUI複数view/queue/MCPは未実装。撮影内容がsnapshotと一致する保証はcapture実装/fixtureによるもので、公開constructorは画像の出所を認証しない。
- 次はcapture readerで相対file名/metadata IDとhash/PNG hashと寸法を照合し、実画像capture fixtureの保存往復へ接続する。全体計画の残件も継続。

## モデル専用Evidence capture

- EvidenceViewに固定正投影camera/解像度/clip、EvidenceImageにsnapshot/view/PNGコピー/hash/render profileを保持。PNGはPaintPngInputで構造/寸法を検査。
- EvidenceModelCaptureを独立追加。専用scene/camera/mesh/material/RenderTextureをsnapshotから作り、camera.sceneで分離。GUI/編集markerを生成しない。既存MaterialSurfaceSetを再利用。Unity main threadで同期描画、finallyで表示停止/資源破棄/scene unloadを要求。
- 最新確認済みPlayer: `Builds/Windows-EvidenceCaptureSeam/NyaForge.exe`。log: `Logs/build-player-20260912-062838-457.log`。PASS: `Artifacts/Authoring-20260912-062902-b7c4a61ebc814c38a08cec91f80e322f/report.json`。専用512px画像のdecode、live編集後の旧snapshot画像一致、別scene同layer巨大cube非混入、固定cameraの新snapshot画像差、PNGcopy隔離と既存suite成功。
- 初回は移動量.2で退化面を作り編集拒否。次は片面だけ移動し表裏seamの裏面が輪郭を埋め画像差なし。fixtureの対応頂点0/4を.03動かして修正。失敗記録はArtifacts/Authoring-20260912-062620-eafb8aa7732140aabe9345c8cda16cb4と062728-e98d5d49bcac49e1b1d38bdc7118e231。後者のevidence-model.pngを目視しUIなしのパネルを確認。
- Core testは今回未再実行（Player全体compile/GUI suiteで検証、直近240件）。Ready meshだけ対応。空/faceless capture、複数view、材質/画像付きcapture検証、queue/取消、専用manifestとartifact公開、GUI入口、MCPは次工程。PNG構造検査はdecodeの代替ではない。

## Evidence metadata reader

- `EvidenceManifestReader` と外部変更から隔離した `EvidenceRecord` を追加。UTF8厳密/byte budget/depth12/重複property/末尾JSON、項目集合、型、ID/hash、非負数、bounds順序、target/state/final整合を検査。CopyMetadataはdeep cloneで返す。
- metadata schema1だけを許容し、captureStatusやvalidationを撮影済み/passへ変えた入力を拒否。artifactsは空のみ。値なし状態にmesh/hash/metricsを付けた不整合を拒否する。これは形状payloadのhash照合や実際のfit検証ではなく、証跡JSONの構造/整合検証。
- Core **240 passed / 0 failed**。Ready/Empty/Incomplete読込、copy隔離、不正capture/fit/schema型/未知項目/負数/bounds/mesh欠落/状態/final状態、末尾JSON/重複key/不正UTF8拒否を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-9847ebbe9dc848e3b443de50d884a321`。
- 初回編集スクリプトはPowerShellの引用符でparse失敗し、変更は未適用。修正後に実行しCore成功。今回Playerビルドなし。
- 残る読込検証：Faceless/途中Ready/複数材質/最大予算/巨大整数の組合せ。次はモデル専用captureのcamera/profile/artifact契約を実装する。既存metadataを撮影成功扱いに変更せず、別capture resultから画像hashを結ぶ。全体計画/MCP/rig等も継続。

## Evidence metadataの保存

- `EvidenceManifestCodec` はschemaVersion1/kind=nyaforge.evidence.metadataの明示JSONを出力。snapshot/document/target/final状態/計測/診断/hashを記録し、Mesh等の全payloadを暗黙serializeしない。captureStatus=not_requested、validation各項目=not_run、artifacts=[]。未実装sourceEpoch/workspaceRevision/poseはnull。
- `EvidenceStore.SaveMetadata` は指定directory内へsnapshotId.evidence.jsonを新規公開する。既存Storage.Lock/AtomicWriteを利用し、同内容の再保存はidempotent、異なる既存内容はEVIDENCE_CONFLICTで保持。既存証跡を置換しない。
- Core **238 passed / 0 failed**。同snapshotの決定的bytes、ID/hash、未取得/未検証/null表現、atomic保存と再保存、編集後の別ID保存と旧証跡保持、異内容拒否/一時file残留なし、Empty/Incompleteのgeometry非捏造と診断保持を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-cefcdb3c2e0c44118c58c7f9cc842e88`。
- 今回Playerビルドなし。metadata writerのみで、reader/schema厳密検証/画像artifact付きpublicationは未実装。撮影済みと偽装する入力は受け付けないAPIにしている。電源断/SMB中断の実機注入は未検証。
- 次はmanifest readerの型/ID/hash/状態整合と予算検査を追加し、モデル専用captureのcamera/profile/artifactを別契約で接続する。画像失敗とcommand commitは分離したまま。全体計画の残件も継続。

## Evidenceの処理段指定

- `EvidenceTarget` を独立追加。Final既定、NodeInput/NodeOutputはobject/graph/node/mesh portを明示する。対象object/graphの一致、node存在、対応mesh portを検査。値が未解決ならIncompleteでValueなし。
- `EvaluatedSnapshot.Target` と `FinalEvaluationComplete` を追加。最終評価未完了でも取得できる途中のmeshはReady/Facelessとして返すが、最終評価未完了・graph診断・stale preview revisionを別に保持。OutputNodeIdはgraphの最終node、Target.NodeIdは選択段。混同しない。
- Core **236 passed / 0 failed**。編集nodeのinput/output hash差、最終接続切断後の途中output保持、最終Incomplete、対象不一致/非対応port拒否、入力未解決、材質編集後の取得済みmaterial/画像bytes保持を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-291c856b43a447298a5c168c55a0b83a`。
- 今回Playerビルドなし。対応portは現行のmeshのみ。Image/Material単独、複数object、pose、generator version/source/workspaceの追加identity、manifest/画像capture/MCPは残件。
- 次はEvidence manifestを独立codecとして作り、snapshot/対象/metrics/diagnosticsと未検証項目を保存できるようにする。その後同じsnapshot由来のモデル画像を紐づける。既存UI検証captureとは分ける。全体計画の残件も継続。

## Evidence snapshotとmetricsのCore

- `Evidence/EvaluatedSnapshot` と `EvidenceMetrics` を分離。workspace.Gate内でcommitted final outputを取得し、instance/document/revision/stateHash/object/graph/output nodeと一意snapshot IDを保持。内容比較はOutputContentHashで別管理。command実行中の再入取得は拒否。
- Empty/Ready/Faceless/Incompleteを区別。IncompleteはValue/metricsを返さず、診断と古いpreview revisionだけを保持。古いmeshを現revisionとして扱わない。空projectにも架空のgeometryを作らない。
- metricsはrender頂点/triangle/submeshと論理頂点/faceを区別。world boundsはrender positions、faceless時だけ全loose点から取得。論理topologyなしはnull、0とは区別。assigned material数と重複なし画像hashも収集。
- Core **234 passed / 0 failed**。取得後の編集でsnapshot/metrics不変、再取得のID差/内容hash一致、Undoで内容同一かつ新revision、未完了時のstale preview隔離、空/0点/1点faceless、scale100+translationのboundsを確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b841a2ba05b542699eee0a56b0653232`。
- 今回Playerビルドなし。finalのみを取得するCoreで、node input/output指定・manifest保存・画像/専用camera・pose・capture queue・MCPはまだ実装していない。SourceEpoch/workspaceRevision等は現行native Coreに正本がないため捏造していない。fit/pose validationも未取得。
- 次は材質/画像付きsnapshotの保持と選択node outputのidentity/診断契約を検証し、Evidence manifestへ進む。Unity captureはこの不変snapshotを入力とする。全体計画の残件は維持。

## 操作案内統一とC1次工程

- `AuthoringWorkbench.ViewportHint` に案内の責務を集約。3Dpaint→切断指定→面/点選択の順で判定し、各mode更新とpanel閉じに追従。切断中の通常点選択案内を修正。
- 最新確認済みPlayer: `Builds/Windows-ViewportHint/NyaForge.exe`。log: `Logs/build-player-20260912-061104-942.log`。PASS: `Artifacts/Authoring-20260912-061132-e773f2822af64a20921e365cb17601d4/report.json`。既存suite成功。cut-visibility-hover.pngで上部が切断位置追加/Escape終了へ変わり、文字が収まることを目視確認。Core変更なし（直近231件）。
- Development-Planと設計v2 §8〜9を読み直し、C1の残るEvidence/MCPへ進む。手動受入やC1全体の完了を宣言しない。[Evidence接続計画](docs/Evidence-Integration-Plan.md) に現物のGate/Preview/WorkbenchCaptureの境界と検証順を記録。
- 次のコード変更はCoreの不変EvaluatedSnapshot取得とmetrics。empty/faceless/incomplete/staleを区別し、取得後の編集で結果が変わらないことを確認。その後専用モデルcapture/画像対応、MCPへ進む。形状/UV/Paint残件と全身制作等は継続。

## 重なった面の可視性GUI検証

- `AuthoringWorkbench.CutVisibilityVerification` を分離。正投影で2枚の面を重ね、ON時の可視辺fallback、toggle実クリックでOFF→最短の隠れた辺、各viewportクリックとhoverの一致、登録済み説明を確認。
- BVH同形状再利用、手前面の削除による再構築/奥辺選択、Undo後の遮蔽復帰を確認。候補・設定・draftでは文書hashを変更しない。
- `UpdateCamera` にClearCutPathHoverが実際には未接続だったため追加。前段の記録の意図と現物の不一致を修正し、カメラ更新時の線/候補消去を検証。
- 最新確認済みPlayer: `Builds/Windows-CutVisibilityVerified/NyaForge.exe`。log: `Logs/build-player-20260912-060856-515.log`。PASS: `Artifacts/Authoring-20260912-060922-6b79c76f3c2b496fb4a5c8013f496e7e/report.json`。既存suiteも成功。Core変更なし（直近231件）。
- `cut-visibility-hover.png` を目視。手前底辺の黄色線、中央マーカー、候補5–6/50.0%/登録済み、可視性説明の折返しを確認。上部のselection-hintは通常の点選択案内が残り、切断指定modeへの追従は次に整える。
- 残件：透視で斜め/部分遮蔽/近接面/透明材質、変換変更によるcache再構築、drag/paint競合、部分遮蔽辺の別区間探索、大規模性能。全体計画の複数object、rig/weight/morph、各形式入出力、Evidence/MCP等も継続。

## 切断候補の可視性option

- `PolygonEdgeScreenPicker` に候補world位置のpredicateを追加。拒否された候補では最短距離を更新しないため、次の可視候補を探索できる。旧呼出はpredicateなしで互換。
- `Geometry/MeshVisibility` はray origin→候補点の両面geometry判定。候補そのものの面は許容（距離許容差max(1e-6, distance*1e-5)）。材質alphaを判定しない。
- `AuthoringWorkbench.CutVisibility` に「隠れた位置を除外」（既定ON）と共通FindCutPathHitを分離。hover/clickとも同じ判定を使う。Unity viewport rayのnear-plane originから判定し正投影にも対応。MeshData参照/RestTransformが同じ間はBVHを再利用。
- Core **231 passed / 0 failed**。UVなし2枚の重なりで、最短の奥辺を除外して手前の別辺へfallback、狭い半径では候補なし、自己面許容/奥の点拒否/距離0を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-928c681d0ffe4112a7bdf08f43e607cb`。
- 最新確認済みPlayer: `Builds/Windows-CutVisibility/NyaForge.exe`。log: `Logs/build-player-20260912-060615-356.log`。PASS: `Artifacts/Authoring-20260912-060639-3d571ed0e5de4f8fa385f41085841772/report.json`。可視性ONで既存の平面クリック/hover/確定/Undo/native/Bakeとpaint含むsuite成功。
- GUI上で重なる2枚の切替比較、toggle実クリック、BVH cache更新、hover画像は次の検証。透明材質も面として扱う旨をGUI表示。判定対象は編集中Mesh。辺ごとの最近点だけを判定し、部分遮蔽辺の別区間探索は未対応。全体計画の残件も継続。

## UVに依存しない面レイ判定

- 遮蔽判定をペイントのUV必須条件へ依存させないため、`Geometry/MeshRaycast` を抽出。既存median BVH/両面/距離制限/triangle安定順を維持し、triangle/submesh/頂点indices/barycentric U,V/距離/向きを返す。UV不要。
- `SurfacePaintMesh` はgeometryを所有し、UV補間・Owner・UV continuityの責務だけを保持。RayGeometry数値helperをGeometryへ移動（Unity metaも移動）。既存INVALID_PAINT_RAY診断コードは互換維持。
- Core **230 passed / 0 failed**。新規UVなしmeshのscale1/100/微小scale、indices/barycentric/距離制限と、既存paint/BVH/seam/全suite成功。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-3be311476b604d3c9b984a321617f61b`。
- 最新確認済みPlayer: `Builds/Windows-GeometryRaycast/NyaForge.exe`。log: `Logs/build-player-20260912-060314-627.log`。PASS: `Artifacts/Authoring-20260912-060337-6a77231137b34751aee16184e213d6d1/report.json`。ペイントを含む既存GUI suite成功。
- これは遮蔽判定の共通基盤で、辺pickerの可視性optionはまだ未接続。次は候補点へのrayを使うpredicateをpicker探索へ加え、遮蔽された最短候補を飛ばして次の見える辺を選べるようにする。表示polygon/transform単位でBVHを再利用し、透明材質の扱いを明示する。hover画像確認も残件。
- 全体計画の複数object、rig/weight/morph、各形式入出力、Evidence/MCP等も継続。

## 切断候補のhover表示

- `AuthoringWorkbench.CutPathHover` を分離。切断指定modeのpointer moveで候補辺を黄色い線、透視補正後の指定位置を10px角の黄色いUIマーカーで表示。ラベルに辺ID/割合/登録済みを表示する。UIマーカーはPickingMode.Ignoreでクリックを遮らない。
- hover時は文書や選択へ書き込まない。pointer leave/down、カメラ変更、draft refresh、mode終了で候補を消去。ドラッグ中は候補判定を行わない。色/割合表示と登録済み経路のピンク線は別owner。
- 最新確認済みPlayer: `Builds/Windows-C1B-CutHover/NyaForge.exe`。log: `Logs/build-player-20260912-055938-366.log`。PASS: `Artifacts/Authoring-20260912-060001-e6b9ac7556dd4558a4261547715203df/report.json`。
- viewportの3辺へPointerMoveを送り候補edge ID/2点line/marker表示/文書と選択不変を検査。実クリック後のhover消去と画面外move時の消去も確認。既存の経路確定/Undo/native/Bakeを含めsuite成功。Core変更なし（直近229件）。hoverの画像を残す専用captureは今回未追加。
- 次はhover画像と登録済み表示/カメラ変更時消去を確認し、面による遮蔽のある辺を選ばない可視性optionへ進む。現状は全辺候補（GUI明記）、面遮蔽なし。全体計画の残件は継続。

## 切断経路の画面クリック指定

- `AuthoringWorkbench.CutPathPicking` を分離。連続切断パネルのtoggleで有効化し、viewportの短い左クリックをPolygonEdgeScreenPicker（半径12 panel px）へ送る。割合はクリック位置から計算して経路末尾へ追加。重複と上限拒否。通常の頂点/面選択を変更しない。
- 左ドラッグ回転/右ドラッグ移動は既存経路を維持。クリック時にviewをfocusしEscapeで指定モード終了（draft保持）。パネル閉じ/文書変更/編集段変更でmode解除。3Dpaint有効化は最終段へ移るため解除、切断mode有効化時はpaintをoff。
- 最新確認済みPlayer: `Builds/Windows-C1B-CutPathPickVerified/NyaForge.exe`。log: `Logs/build-player-20260912-055711-541.log`。PASS: `Artifacts/Authoring-20260912-055733-55c978e90d4a429f90ede7fca2e4af6d/report.json`。
- `AuthoringWorkbench.CutPathPickingVerification` で3辺中点へのviewport実クリック、3地点の割合/preview、選択/文書不変、Escape送信後の通常点選択復帰、パネル閉じでmode終了/再開時draft保持を確認。その後の切断確定/Undo/Redo/native/Bakeと既存suiteも成功。Core変更なし（直近229件成功）。
- 面遮蔽なしで裏側の辺も候補になることをGUIへ明記。現在hover候補線/可視面限定なし。次は候補hoverと可視性の扱いを改善し、奥行き重なり/端点/斜めの面/ドラッグ・paint競合を追加確認する。全体計画のrig/weight/morph等は未完了。

## 画面上の辺判定Core

- `PolygonEdgeScreenPicker` を独立追加。Polygonの論理辺を画面距離で検索し、EdgeCutLocation（元辺の割合）/距離/深度を返す。頂点はRestTransformで変換、near/farで線分をclipしてから投影する。
- `SurfaceCameraSnapshot.ProjectionWeight` を追加し、投影の同次座標wを使って画面上の割合を元の3D辺の割合へ補正。正投影は一定w、透視投影は異なるwに対応。距離同点は手前の深度、さらに同点なら小さい辺ID順。点に潰れた投影辺は手前の端点を選ぶ。
- Core **229 passed / 0 failed**。奥行きの違う辺の画面中点→3D割合1/3、scale100、正投影、半径外/負半径、near/farをまたぐ辺と元割合の再投影、カメラ後方の除外を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-65c2f9c62d314ff594e4ce1f6df739d2`。
- 今回Playerビルドなし。画面クリックGUIは未接続。次は深度同点/重なり/端点/平行投影の追加確認と、切断用クリックモードを通常の点/面選択やカメラ操作から分離して接続する。
- 現在のpickerは面による遮蔽判定なし（X-ray相当）、画面距離優先で探索は全辺O(E)。可視面だけの選択/裏面/大規模性能/viewport clippingは今後。全体計画の残件も継続。

## 連続切断の操作検証と入力復旧

- `AuthoringWorkbench.CutPathControlsVerification` を分離。選択した3辺を順番に追加する実クリック、順序/preview確認、重複辺と非edge選択の拒否、不正行で確定/線を無効化、末尾取消/クリアの実クリック、編集段変更でdraft消去を確認。
- 「最後の辺を取り消す」は経路全体の解析に依存していたため、不正な末尾行を取り消せなかった。生の非空行から末尾を取り除く処理へ変更。修復後のpreview復帰、2点→1点時の確定無効化を検証。
- 最新確認済みPlayer: `Builds/Windows-C1B-CutPathControls/NyaForge.exe`。log: `Logs/build-player-20260912-055147-222.log`。PASS: `Artifacts/Authoring-20260912-055212-d61349e1fe7e4df7b8cb85bc3192ac5b/report.json`。上記の操作後、連続切断確定/Undo/Redo/native/Bakeと既存suiteも成功。Core変更なし（直近227件成功）。
- 次は画面上の辺を直接指定して経路へ追加できるようにする。現状は両端頂点の選択→登録が必要。辺hit testを表示から分離し、画面距離・割合・変換と重なりを検証してからGUIモードを接続する。キーボード取消と通常の点/面選択・カメラ操作との競合を確認する。
- 再訪/閉loop/曲線、複雑なseam/凹面/端点混在のGUIは引き続き残件。全体計画の複数object、rig/weight/morph、入出力、Evidence/MCP等も未完了。

## 連続切断GUI

- `AuthoringWorkbench.CutPath` を分離。選択した辺を割合指定で末尾追加、複数行の順序付き入力、末尾取消、クリア、経路全体確定を提供。文字欄は1行に「頂点ID 頂点ID 位置%」。辺の位置は小ID→大ID、端点0/100を許容。
- 候補全体のPolygonCutPath検証に成功した場合のみピンクの折れ線と確定ボタンを有効にする。入力不正では文書を変更しない。foldout閉じで線消去、文書hash/編集段変更でdraft消去。確定は単一共通command、成功時のみ選択をクリア。
- 最新確認済みPlayer: `Builds/Windows-C1B-CutPath/NyaForge.exe`。log: `Logs/build-player-20260912-054921-238.log`。PASS: `Artifacts/Authoring-20260912-054946-3df604f130c9497ca1c0343b6ad78ae7/report.json`。`AuthoringWorkbench.CutPathVerification` で3地点preview、開閉、後半無効経路、確定の実クリック、2面→4面/6点→9点、1回Undo/Redo、native/Bakeを確認。既存suiteも成功。
- `cut-path.png` を目視し9点の配置と折返し説明、入力欄を確認。切断後の内側の辺は描画しないため、4面化の証拠はgeometry assertion。今回Core変更なし（直近227件成功）。
- 次は選択からの末尾追加、重複拒否、末尾取消/クリアの実クリック、編集段変更時のdraft消去を追加検証する。現状テストは経路文字欄を直接設定して確定する。複雑なseam/凹面/端点混在のGUI、経路の画面クリック直接指定、同面再訪/閉loop/曲線などは残件。

## 連続切断の共通command

- `PolygonCutPathOperation` を独立モジュールとして追加。2〜256個の不変EdgeCutLocationを読み取り専用配列へ複製し、順序付き経路を共通commandからPolygonEditingへ渡す。
- fingerprintには経路長・各辺の両端ID・正規化した割合を順番どおり格納する。既存operationのfingerprintは変更しない。同一command IDの同内容再送は再適用せず、位置や順序を変えた再送はCOMMAND_ID_REUSEDで拒否。
- Core **227 passed / 0 failed**。呼出元配列変更からの隔離、null/上限拒否、2面を横断する切断、後半で既存辺と重なる失敗時の文書hash/revision/頂点数保持、1回Undo/Redo、native polygon binary一致、Bake一致を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4b2dd4142810484d9717a0f3d8af1c38`。
- 初回は検証コードがDocumentRevisionをRevisionと誤記してコンパイル失敗。検証コードを修正し、全件成功を確認。
- 今回Playerビルドなし。最新確認済みPlayerは下記EdgeCutControlsで、連続切断はGUI未接続。次は順序付き辺位置の登録・削除・preview・確定/取消をGUIモジュールとして接続し、実操作と1回Undo/保存を確認する。
- 現在の経路は各元面を1回横断する範囲。閉loop/同面再訪/曲線、一般的なナイフ操作、複雑な非平面形状などは残件。全体計画のrig/weight/morph、複数object、各形式入出力、Evidence/MCP等も継続。

## 切断GUI追加検証と連続経路Core

- `AuthoringWorkbench.EdgeCutControlsVerification` を分離。選択した両端から辺A/Bへ実登録、0/100端点のpreview/確定（新頂点なし）、Undo、クリアボタン、foldout開閉、編集段変更時draft消去を確認。
- 最新確認済みPlayer: `Builds/Windows-C1B-EdgeCutControls/NyaForge.exe`。log: `Logs/build-player-20260912-053932-668.log`。PASS: `Artifacts/Authoring-20260912-053951-f9e3b76c329648c3be532b76e6f5bdc7/report.json`。既存suite成功。このbuildは下記CutPath Core追加前。
- `EdgeCutLocation` と `PolygonCutPath` を追加。2〜256箇所の順序付き辺交差位置を受け取り、各元面を1回だけ横断する。全点を候補へ挿入して隣接地点をFaceSplitで順に結ぶ。辺再訪/共通面なし/同面再訪を拒否。共有辺は1回だけ挿入し、既存の面ごとのcorner補間を再利用。
- Core **226 passed / 0 failed**。2quad横断→4quad/追加3頂点、共有点1個に両側UV seam保持、切断辺の共有、binary往復、辺再訪/飛び越し拒否、元mesh不変を確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4951aecb64064a529cd302572b298301`。
- CutPathはまだ共通command/GUIへ未接続。次は経路payloadの複製/command fingerprint/replay/Undo/nativeを整える。複数回同面横断、閉じたloop、辺上再訪、非平面face、複雑な経路は未対応/未検証。単一面切断GUIは維持。
## 辺上切断GUI（前段記録）

- `AuthoringWorkbench.EdgeCut` を分離。2辺のID入力/選択2点から登録、百分率、候補検証、ピンク線、確定/クリアを提供。文書hash/編集段変更でdraftを破棄、foldout閉じで線を非表示。成功時だけ選択をクリアする。
- `BoundaryHighlightProjection` は既定の閉loopを維持し、開いた2点線も扱えるように拡張。線は候補の頂点から算出し、文書へは確定時の共通commandで反映する。
- 最新Player: `Builds/Windows-C1B-EdgeCut/NyaForge.exe`。log: `Logs/build-player-20260912-053717-140.log`。PASS: `Artifacts/Authoring-20260912-053736-0a15846425154bcf820bdb18132c55b4/report.json`。25%/75%線preview、2quad/6頂点、確定時線/選択消去、無効指定で文書/選択不変、Undo/Redo/native/Bakeと既存suite成功。`edge-cut.png`を目視し追加点と入力欄の折返しを確認。面内の線は保存後には表示しないため、切断の証拠は面数/頂点数のassertionによる。
- Core変更なし（直近225件）。選択からの辺登録ボタン/クリアの実クリックと、端点0/100、凹面/共有UV seamのGUIは追加確認対象。複数面横断と連続切断は未対応。
## 辺上の点を結ぶ切断Core（前段記録）

- `PolygonEdgeCut` / `PolygonEdgeCutOperation` を追加。異なる2辺が共有する1面を、各辺の指定位置で切る。割合は小vertex ID→大IDの0〜1、0/1なら既存端点を使い、内部ならPolygonEdgeInsertionで点/cornerを追加してPolygonFaceSplitへ接続する。
- 点追加と分割を不変の候補上で行い、1つの共通commandで確定する。途中の失敗は文書/ID履歴を変えない。既存のUV seam補間・planar/対角線検証を再利用。単一平面の切断であり、連続ナイフ/複数面横断はまだ実装していない。
- Core **225 passed / 0 failed**。quadの対辺25%/75%→2quad/追加2点、外周corner参照保持、切断辺共有、端点使用、同一辺/範囲外拒否、点追加後の無効分割で文書不変、再送/1回Undo/Redo/native/Bakeを確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-1ac5696bf6c0441c977a6b6678adad1e`。
- 今回Playerビルドなし。次はGUIで辺2つと位置を指定し、preview/確定/取消を扱う。shared seamや非平面/凹面の組合せ、大きいID/予算境界、複数面の連続切断は残件。
## 複数面dissolve GUI

- 既存FaceMerge GUIを「選択した面を1面にまとめる」に拡張し、DissolvePolygonFacesへ接続。2面以上で有効。既存2面の操作検証も継続。旧Merge core/commandは互換用に保持。
- `AuthoringWorkbench.FaceDissolveVerification` を分離。非連結2面の拒否時に文書/選択が保持されること、4面fan→1quad/中央点除去/残存面選択、Undo/Redo/native/Bakeを検証。
- 最新Player: `Builds/Windows-C1B-Dissolve/NyaForge.exe`。log: `Logs/build-player-20260912-053156-238.log`。PASS: `Artifacts/Authoring-20260912-053215-a665b305c67b4bc1b4cb882e86e4516a/report.json`。既存suiteも成功。`dissolved-faces.png`を目視し四角面/点4個と操作ボタンを確認。
- Core変更なし（直近223件）。Paint付き非線形UV、大きな選択、凹外周のGUIは残件。
## 複数面dissolveのCore（前段記録）

- `PolygonFaceDissolve` / `PolygonDissolveOperation` を追加。連続する同一平面/向き/材質の2面以上から内部辺を除去する。内部辺のUV/normal/tangent一致とmanifold/向きを検査。選択領域が接続し、外周が3〜256角の単純な1loopになる場合のみ確定。穴/分岐/離れた領域を拒否する。
- 最小face IDと残存cornerを保持し、内部で不要になった頂点を除去。別の面や元から独立している点は保持。ID履歴を維持し、候補の三角分割後に共通commandへ確定。既存2面Mergeは互換のため維持。
- Core **223 passed / 0 failed**。4triangle fan→1quad/中央点除去/外周corner参照保持/入力順不変/UV seam拒否/非連結拒否/穴付きring拒否、command無効時状態保持/Undo/Redo/native/Bakeを確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-fbce87e4252544ec9813ff68b724ac21`。
- GUI未接続、今回Playerビルドなし。非線形UVの内部補間は再三角分割で変わり得る。複雑な凹外周/大規模選択/全属性形式/Paint付き形状は未検証。
## 面なし下流の診断と復旧案内

- `AuthoringWorkbench.FacelessRecovery` に案内と「面がない編集段へ戻る」ボタンを分離。最終評価が未完了で、評価済みの面なしPolygonEditがある場合に表示。編集段リストの最初の該当段を選ぶ。文書は変更しない。面の作成またはUndoを案内する。面がない出力に材質を追加するボタンも無効化。
- Core **220 passed / 0 failed**。Mirror/Paint/材質の3経路で全面削除後のNO_RENDERABLE_FACES、編集値保持、後続node参照保持、native保存再読込、未完了Bake拒否、Undoによる復旧/Redoを確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4ae9064b4f164e979491ceb15d932dfb`。
- 最新Player: `Builds/Windows-C1B-FacelessRecoveryEdit/NyaForge.exe`。log: `Logs/build-player-20260912-052657-930.log`。PASS: `Artifacts/Authoring-20260912-052717-0b20fbda67bb4c1e9bc4cc6cb374cd15/report.json`。Mirrorの削除後に復旧ボタン実クリック/文書不変/編集可能/Undoで完了/Redo/native案内復帰と既存suiteを確認。
- 初回テストはMirror作成後の最終出力表示のままDeleteを呼び、編集contextなしで失敗。検証側でSelectEditStage(1)を明示して修正。初回記録: `Artifacts/Authoring-20260912-052618-a14af27d1abf4a01bd5da94eac013737/report.json`。
- 未確認: 保存済みPaint画像付きの新しい面へのrebind、複数の面なし編集段がある場合の選択導線、全UIの診断文言。今回Paint fixtureは既定画像生成nodeで、独自stroke画像の網羅試験ではない。
## 全ての面の削除（前段記録）

- PolygonDeletionは最後の面も削除可能。削除で不要になった頂点を除去し、元から独立していた点とID割当履歴を保持する。面なし時はrender生成を呼ばない。GUIの全選択時無効化を解除。
- 属性はcornerに属するため全削除時に消える。再作成の最初の面は既定UV/normal/tangentを生成する仕様。以前の属性はUndoで復元する。空状態に旧属性形式を持ち越す機能は実装していない。Weldによる全collapseは引き続き拒否。
- Core **217 passed / 0 failed**。独立点保持/ID履歴、再追加から面作成、最終面削除のgraph/native、面なしBake拒否、Undo/Redoを検証。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-5152d96c71ef445caa70f3445d259479`。
- 最新Player: `Builds/Windows-C1B-DeleteAll/NyaForge.exe`。log: `Logs/build-player-20260912-052210-125.log`。PASS: `Artifacts/Authoring-20260912-052228-1b324910f8b845539f5b045b213cb375/report.json`。最後の面の削除ボタン/空表示/操作有効性/native再読込/Undo/Redo、既存suite成功。検証処理は `AuthoringWorkbench.DeleteAllVerification` に分離。
- 未確認: Paint/材質付き作品の全削除後の下流診断、全属性形式での再作成、scale100/translation。次は面なし時の下流nodeとGUI操作の対応を確認し、ユーザーに回復方法を示せるようにする。
## 空PolygonのGUI統合（前段記録）

- 「空の形状から始める」を追加。面なしpreviewでもOwnedMeshProjectionは編集点を生成する。最終結果の重ね表示はMeshがある場合のみ。0点のFrameはdefaultへ戻す。GUIの未完了表示と面なし案内を分け、移動判定はPolygonの有無を使う。UV投影/厚み/Paint追加は面なし時に無効化。
- 最新Player: `Builds/Windows-C1B-EmptyPolygon/NyaForge.exe`。log: `Logs/build-player-20260912-051859-316.log`。PASS: `Artifacts/Authoring-20260912-051918-36d0f04cd13e4640b52b537f5a9cc4a2/report.json`。空開始ボタン、1/2/3点追加と各native再読込、最初の面、Undoで点のみ/Redo、面の再読込/Bakeを確認。既存suiteも成功。`first-face.png`を目視し三角面と編集欄を確認。
- Core変更なし（直近215件）。まだ全削除を許容していない。面なし状態で材質など全機能のボタン有効性/診断を網羅していない。scale100/translationや最初の面の別属性、面なしプロジェクトの旧版互換性説明は追加検証対象。
## 面なしgraph/command/native統合（前段記録）

- GraphMeshValueは面なしpolygonに限りMesh=nullを許容し、snapshotへ面なし識別とpolygon hashを含める。既存meshありのhashは維持。PolygonSource/PolygonEditで面なしを評価成功として流し、Outputでは画像接続なしの場合に通す。その他のnodeはNO_RENDERABLE_FACESで拒否する。
- PolygonEditingは点追加/移動/最初の面作成に限り面なし入力を許可。面あり候補のrender検証は維持。preview.IsCompleteは評価完了を示し、Mesh非nullとは独立。workspace.Evaluate()はこの状態でnullを返す。
- Bakeは面なし最終出力をNO_RENDERABLE_FACESで拒否。最初の面作成後は出力可能。空sourceの出力由来ハッシュはpolygon正本hashを使う（既存meshありsourceは従来通り）。初回試験でこのbaselineのnull参照を検出して修正。
- Core **215 passed / 0 failed**。空graph/点の追加ごとのnative保存再読込/移動/UV拒否時状態不変/最初の面/Undoで面なし/Redo/再読込/Bakeを確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-219820f0bc8f48c7b3c45d7b510f5942`。
- GUI/Playerはまだ面なし対応していない。今回は新規Playerビルドなし。次はOwnedMeshProjectionのmesh=null時の点生成とWorkbenchのrendering依存条件を修正し、空から始めるGUIを接続する。Mirror/材質/Paint等での診断、全削除、空初期属性の維持も残件。
## 空PolygonのCore対応（前段記録）

- PolygonMeshで0頂点/0面、1点/2点を許容し、空ID集合の上限を0にする。既存の面/参照/ID検査は保持。
- PolygonBinaryCodecは面なしだけv3（ID上限付き80byte header、属性flags0）として保存。v1/v2の非空条件と通常のwriterは保持。PolygonNewFaceは最初の面にUV/normal/tangentを生成。PolygonEditPointsは面なしにも対応。renderはNO_RENDERABLE_FACESで明示拒否。
- Core **214 passed / 0 failed**。空/履歴/1点/2点保存の往復、旧versionへの偽装拒否、truncated/flags拒否、最初の面とv1への移行を検証。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-5abea58ea32a4425ba14e022ca4db146`。
- 今回Playerビルドなし。graph/command/GUIは面なしのまま編集を継続できる段階ではない。GraphMeshValueがmesh非nullを前提とするため、次の統合を [空Polygon統合設計](docs/Empty-Polygon-Integration.md) に記録。native project schema3とpolygon blob v3を混同しない。全削除/属性形式の維持は残件。
## 点描画の集約と256点制限撤廃

- `EditPointProjection` に点表示の所有・描画・選択・破棄を分離。1点1GameObject/sphereから、2,048点を1meshにまとめた八面体マーカーへ変更。各pointは6頂点/8三角形、未選択/選択の2submesh。materialはownerから借り、mesh/rootは自身で破棄。面モードではrendererを非表示。選択不変時はindices再生成を省く。
- `OwnedMeshProjection` がこのモジュールを所有しtransactionの候補破棄/置換に追従。`PickVertex` は全編集点を探索し、最初の256点で打ち切らない。クリック探索はO(n)、選択変更は全バッチのindicesを更新するため、大規模性能の最終解ではない。
- 最新Player: `Builds/Windows-C1B-PointBatches/NyaForge.exe`。log: `Logs/build-player-20260912-050918-492.log`。PASS: `Artifacts/Authoring-20260912-050938-b223ab6f9c074ab3adfb4fbce6ee15a8/report.json`。2,053編集点/2バッチ、末尾2052番の実クリック→stable ID2053、全選択/解除、最終出力4点/1バッチ→編集2,053点/2バッチを確認。既存suiteも成功。`many-points.png`を目視し選択点と密集点描画を確認。
- Core変更なし（直近212件）。今回は通常scaleの2,053点。10万点等のCPU/GPU/メモリ計測、密集点の見やすさ、画面サイズ一定の点、近接/重なり選択、空polygonは残件。
## 追加点から面へのGUI一周

- `AuthoringWorkbench.LooseVertexWorkflowVerification` を分離。移動ボタンでX+10/Y-20mm、未接続点marker位置更新/三角形hash不変、Undo/Redoを確認。面作成GUIで4→1→5の三角面を追加し、編集対応表から未接続点が除かれることを確認。Undoで未接続点へ戻す、Redo/native再読込/Bakeまで検証。
- 初回suiteは次のUV検証で `UV wheel did not zoom`。前のfoldout変更後にlayout未確定のままScrollToしていたため、`AuthoringWorkbench.UvEditingVerification`で2frame後にScrollTo、さらに2frame待ってから操作するよう変更。製品の入力処理は変更なし。
- 最新Player: `Builds/Windows-C1B-LooseToFaceLayout/NyaForge.exe`。log: `Logs/build-player-20260912-050614-640.log`。PASS: `Artifacts/Authoring-20260912-050631-51fb3b907b66419980a9ec93b56e5c67/report.json`。`loose-to-face.png`を目視し追加点から伸びた三角面と操作欄を確認。初回失敗: `Artifacts/Authoring-20260912-050529-f3ca2f8a83024baea79d74bf975489d8/report.json`。
- Core変更なし（直近212件）。今回はscale1の小型fixture。scale100/translation、複雑なseam、256点超の描画/クリックは残件。
## 未接続頂点GUIと編集点対応

- `PolygonEditPoints` はrender頂点列をprefixとして保持し、未接続頂点をstable ID順で追加。正本/三角形mesh/UV/Bakeは変更しない。`OwnedMeshProjection` は編集段でこの点列を表示・framingし、`SelectedPolygonVertices`と移動commandは同じ対応表でstable IDへ変換。
- `AuthoringWorkbench.VertexCreation` に座標GUIを分離。モデル原点からのmm座標をscaleでmesh-localへ換算して共通commandへ渡し、成功時に追加点を選択する。
- 最初のPlayer検証でmesh hashが不変な際のColorUpdate再利用が新頂点表示を省く問題を検出。編集polygon参照も再利用条件に含め、未接続点追加/位置変更と最終出力への切替時には表示を再構築するよう修正。
- Core **212 passed / 0 failed**。既存試験へ編集点prefix不変/末尾stable ID検査を追加。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-c5f8c6a33ddc4810be616bb011b900d3`。
- 最新Player: `Builds/Windows-C1B-LooseVertexCache/NyaForge.exe`。log: `Logs/build-player-20260912-050245-504.log`。PASS: `Artifacts/Authoring-20260912-050302-191afb1243194c0d8bfbe5ff9b90cc40/report.json`。追加ボタン/編集5点/描画4頂点/クリックstable ID選択/Undo/Redo/native再読込/Bake、既存suite成功。`loose-vertex.png`を目視し独立点と座標欄を確認。初回失敗: `Artifacts/Authoring-20260912-050145-db3f6efe59ff4419bbf8c8dbd32fffbe/report.json`。
- 次の検証: 未接続点の移動→面登録→面生成のGUI一周、scale100/translation、seamを持つmeshでの対応表、final切替とcache再構築。既存の256点marker/クリック上限は残っており、大規模meshでは後方の追加点をクリックできない。点描画の集約と選択範囲拡張が必要。空polygon対応も残件。
## 面作成操作検証と新規頂点Core（前段記録）

- `AuthoringWorkbench.FaceDraftVerification` を分離。1点ずつ4→1→5の順で登録、重複追加拒否、反転2回、末尾削除、クリアを実クリックで確認。文書hash不変、foldout開閉の輪郭消去/再表示、編集段を離れた際のdraft破棄も確認。
- Player PASS: `Artifacts/Authoring-20260912-045703-f54c1311a1684d9f95da02c6a86fb97e/report.json`。build: `Builds/Windows-C1B-FaceDraftControls/NyaForge.exe`、log: `Logs/build-player-20260912-045639-794.log`。既存suiteも成功。このビルドは次項の新頂点Core追加より前。
- `PolygonVertexCreation.Add` / `PolygonAddVertexOperation` を分離。mesh-local位置に未接続頂点を追加し、割当履歴より大きいIDを使用。面と属性は保持。有限値/頂点予算/ID枯渇を検査し、共通commandのUndo/nativeへ接続。
- Core **212 passed / 0 failed**。面保持/render hash不変、追加頂点での面作成、面削除後のID再使用防止、非有限値拒否、command replay/Undo/Redo/nativeの未接続頂点保持、Bakeを確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-2c8be12fb01141c9be5ab61cf71de358`。
- 現状 `PolygonRenderAdapter` は面corner由来の頂点のみ出力するため、未接続頂点はBake/preview meshに出ない。正本には保存される。次はこれを表示/選択する編集ケージ側の機能と追加GUIを実装する。製品で使える頂点追加GUIが完成したとは扱わない。空polygonの初期面作成は引き続き残件。
## 順序付き面作成: Core/command/GUI実装済み

- `PolygonFaceCreation.Create` / `PolygonCreateFaceOperation` を追加。既存頂点3〜256個の指定順を周囲と面の向きとして保持。共有辺の向き/過剰共有、重複面、未知/重複頂点を検査する。新face/cornerは割当履歴より上、既存面/頂点は保持。材質slot指定、`PolygonNewFace`による新面UV/normal/tangent生成、候補三角分割を経て共通commandから確定。
- Core **210 passed / 0 failed**。既存辺への三角面追加、指定順/材質/ID/既存面保持、凹五角形、逆向き、自己交差拒否、command拒否時の文書保持/replay/Undo/Redo/native/Bakeを検証。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4708b8e90e2540d4b974589e104439fa`。
- GUI: `AuthoringWorkbench.FaceCreation`。1頂点ずつ末尾登録、カンマ区切り順序入力、反転/末尾削除/クリア、材質slot、妥当性メッセージ、緑の輪郭。文書確定は共通command。文書hash/graph/node変更時に登録クリア。輪郭資源はownerが破棄。輪郭は閉じた線のみで、塗りつぶし/向き矢印なし。
- Player: `Builds/Windows-C1B-FaceCreation/NyaForge.exe`。log: `Logs/build-player-20260912-045426-748.log`。PASS: `Artifacts/Authoring-20260912-045451-9fe3ba5208d94587912fb26dfdc4a96d/report.json`。入力順に対応した輪郭3点/文書不変、向き不一致の無効化、面追加/登録クリア、Undo/Redo/native/Bakeと既存suiteを確認。`created-face.png`を目視しUIの文字/折返しを確認。正面画像は追加面が側面のため面数の証拠はassertionによる。Core変更なし（直近210件）。
- 次の確認: 1頂点ずつの追加/重複拒否/反転/末尾削除/クリアを実クリックで確認、編集中の文書/段変更によるdraft破棄、複数行/長い入力のGUI、複雑な面でのpreview負荷。新規頂点の配置と空polygonへの最初の面は未実装。
- 制約: 頂点接触だけの非manifoldや全体交差を網羅検査しない。新面UVの重なり解消/Paint再投影なし。面積が相殺される自己交差輪郭は共有法線計算で `INVALID_EXTRUSION` となる場合があり、拒否はできるがエラー説明の改善が必要。初回テストはこのコード差で1件失敗し、非ゼロ面積の交差fixtureで三角分割の交差検出を別途確認した。
- 以下のWeld/BoundaryBridgeの確認済み機能を保持。
## 頂点weld: CoreとGUI実装済み

- `PolygonWeld.AtCenter` と `PolygonWeldOperation` を追加。選択頂点の平均位置へまとめ、最小vertex IDを残す。計算順序はID順に固定。連続して潰れるcornerは最小corner IDの属性を採用し、面ごとのUV/normal/tangentを保持。3角未満になった面は除去する。ID割当履歴と無関係な未使用頂点は保持。
- 非連続の同一頂点再訪、重複面、辺の過剰共有、分岐境界、切れたface fan、接続方向不整合を拒否。候補全体の三角分割後に共通commandから確定する。全削除は現行polygon制約により拒否。
- Core **207 passed / 0 failed**。quad→triangle/中心位置/入力順序/ID、潰れたtriangle除去、閉じたboxとcorner属性、未使用頂点保持、不正入力時の文書保持、command replay、Undo/Redo/native/Bakeを確認。証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b5c8786a24e0475fa3428e7aab3eb725`。
- GUI: `AuthoringWorkbench.Weld` に操作ボタンと残存IDのrender alias再選択を分離。点モード2頂点以上で有効。拒否時は文書/選択を保持し、成功時は残存頂点を選択（除去された場合は空選択）。Core変更なし、直近207件を引き継ぐ。
- 最新Player: `Builds/Windows-C1B-Weld/NyaForge.exe`。build log: `Logs/build-player-20260912-044825-652.log`。PASS: `Artifacts/Authoring-20260912-044849-91040a2fd3224764909e3d7a4d37cb55/report.json`。quad→triangle、残存頂点選択、不正weldの文書/選択保持、Undo/Redo/native/Bakeと既存suiteを確認。`weld.png`を目視し三角形とボタン文字を確認。保存再読込後の画像なので、選択保持の証拠は直後のGUI assertionによる。
- 未実装/未検証: 距離による自動weld、最後に選択した位置への統合、全体自己交差、Paint再投影、複雑なseamのGUI選択/大規模選択/OS/DPI手動受入。以下は前段BoundaryBridgeの検証記録。
## 今回の変更と証拠

- `PolygonBridge`（形状）/`PolygonBridgeOperation`（command）/`AuthoringWorkbench.Bridge`（GUI）を分離。等頂点数3〜256の独立した2境界を四角面で接続する。自動接続位置は距離最小の循環対応。境界方向、ID割当履歴、重複面、辺入射数、三角分割を検査する。
- `PolygonNewFace` にcap/bridge共通の新面属性生成を分離。既存面を保持し、新面は最初の境界の隣接材質を継承。UVは新面ごとの投影で、島の重複を解消しない。
- GUIは最初の境界を黄色、相手を水色で表示。接続ずれを指定可能。同じ境界の選択や閉じた形ではボタンを無効化。表示資源は所有者が破棄する。
- Core **203 passed / 0 failed**。閉じた形の向き、既存属性/ID、無効指定、command replay、Undo/Redo/native/Bakeを確認。結果: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-76672dba180a4549b71287523a0f04d3`。
- 最新Player: `Builds/Windows-C1B-BoundaryBridge/NyaForge.exe`。build log: `Logs/build-player-20260912-044039-672.log`。PASS: `Artifacts/Authoring-20260912-044102-fea66ef0ba674544ab758a7b102616d3/report.json`。GUI2境界表示/同境界拒否/6面12三角形/境界消去/Undo/Redo/native/Bakeと既存suite成功。
- `bridged-boundaries.png` を目視して操作欄の文字と閉じた結果の表示を確認。正面画像なので奥行きはこの画像単独の証拠ではない。閉じた形は境界数0/面数/三角形数/Core方向検査で確認。ビルド後に検証コードのエラー文と試験用出力フォルダ名のみを修正（製品コード変更なし）。
- 未対応/未検証: 不等頂点数bridge、全体自己交差、UV packing、複雑な非平面境界、Paint付き作品、OS/DPI実操作。全体の完成ではない。
- 以前の詳細は [前段履歴](docs/history/2026-09-12-C1B-Before-Boundary-Bridge.md)。頂点追加・面分割/結合・境界cap・UV・Paint・材質・出力復旧の成果と各残件を保持。Unity receiverの最新証拠は同履歴参照（今回再実行なし）。
## 次の作業

1. 次はPolygonCutPathを共通commandへ接続し、経路payload/fingerprint/失敗時原子性/Undo/replay/nativeを検証する。その後GUIの経路登録へ進む。連続した複数面の切断も引き続き目標として保持する。保存済みPaint画像の全削除後rebindと面なし複数編集段の導線も残件として保持する。点描画の大規模性能/密集時の選択も残件。render indexとstable IDの境界、既存Paint/UV/出力の意味を保護する。選択順/向き/既存辺の共有/UVとIDの扱いを明確にしてCoreから共通command/GUIへ接続する。既存stable ID/属性/共通command/Undo/native/GUIの境界を維持する。不等頂点数bridgeも残件。自由面作成・vertex weld/多面merge・連続knifeも維持。複数島/拡縮後のドラッグ、Paint付き作品の動作とUV回転/拡縮ハンドルも維持する。merge/bridge/cut、空polygonと削除後のID割当は残件。scene override、GUI実操作、staging登録前中断/未確認新規GUIDなども保持。復旧だけの類似テストを増やし続けない。複数材質の同UUID共有receiver fixture、部位別GPU定量/alpha、mask/重なり遮蔽は検証残件として保持。
2. ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。3D長曲線・多数区間・ray上限・退化交差・OS/DPIの残件も保持。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1B-EdgeCutControls -Width 1280 -Height 800 -TimeoutSeconds 600
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `Assets/NyaForge/Rendering/`: Playerとreceiverが共有する材質adapter/shader。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/create/curves、全削除・空polygon、UV回転/拡縮handle・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
































































