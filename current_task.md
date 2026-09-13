# 2026-09-13 multi-skin GLB regression lock

同一sourceの共通安定 `BoneId` を持ちながらrest定義が異なる2つのskinを合成し、拡張skinned GLBがskin resourceを2つに分離してmesh nodeのskin参照を保持するCore回帰を追加した。各skinをmesh index／skin indexで再読込できること、異なるsourceの骨は従来どおり拒否することを同じ出力契約へ固定した。

Coreは **470 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-554fa4d20d504cc6bcd46a4d81ed0fb5`）。この回帰は標準GLB出力が同一hashのshared skinだけに依存せず、同一source内の複数skin資源を安全に保持することを検証する。実RadDollV3の10 mesh／9 skin出力は前項の `AllMeshRealV3` artifactで確認済み。

# 2026-09-13 latest Windows viewer navigation recheck

`Builds/FeedbackRecheckV1/NyaForge.exe`を1280x800で起動し、パック選択、キャンセル時の文書／履歴保持、named session保存・再開、未保存変更の破棄確認、不正パス保持、最近／確認セット／設定／制作画面への遷移、utility panelの折りたたみ、viewportの利用可能領域を再確認した。Navigation suiteは **PASS**（`Artifacts/Navigation-20260913-125208-90befd32b95d416690cf89634a21c1f1/report.json`）。`main.png`／`sets.png`／`settings.png`は1280x800で文字欠けなし、制作入口の表示領域も確保されている。

これは自動Playerナビゲーションと画面画像の証拠で、実マウス・DPI個体差・Explorerの実クリックは別手動受入境界とする。
# 2026-09-13 multi-skin GLB output recheck

前回の実RadDollV3全mesh取込では、native projectの10 objectとgraph単位rig sessionを保存・再読込できた。複数のsource skin resourceを一つのglTF skinへ無理に統合すると、同じsource由来でもrest定義やinverse-bindが異なるため出力時に停止する課題が残っていた。

`GlbExportService`を、同一 `Skeleton.ContentHash` なら従来どおり一つのskinを共有し、hashが異なる場合は全object間の共通安定 `BoneId` を検証してから、skeleton hashとinverse-bind行列の組合せごとに別skin resourceを生成する方式へ変更した。共通BoneIdがない異なるsourceの結合は従来どおり `GLB_SKIN_SHARED_SKELETON` で拒否する。skin resourceを分けてもmesh node・node affine・4/32 influence・source inverse-bindを保持し、native制作データは変更しない。

Coreは **469 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-62af12a2a9a6453dafdeb24bfdf9e2be`）。`Builds/AllMeshRealV3/NyaForge.exe`でprivate一時RadDollV3 VRMを `-ImportAllModel` 実行し、10 mesh instanceの編集可能化、native Save/Open、feature-preserving native export、複数skinを含む拡張skinned GLB出力まで **PASS**（`Artifacts/Authoring-20260913-132813-15c2514e54da41ef8f7a104643df84ab/report.json`、GLB `all-model-skinned-glb/model.glb`）。Unity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-133031-021-d7d97a099abe4b0fbb7fdd6d4b5eaf7c/bridge-report.json`）。private素材はpublic repositoryへ追加していない。

実VRChatでの見た目・挙動、異なるsourceのskeleton結合、共有mesh／morph参照、完全なtexture／animation／VRM拡張保持、標準VRM出力は別の受入境界として残る。

# 2026-09-13 pasted feedback recheck on current HEAD d440900

添付されたレビュー（基準 `0d1e957`）を現行HEAD `d440900`へ再照合した。レビューのP1（複数graph metadataの混線、source skin二重変形、skinned node affine、inverse-bind欠落、装着後クリックずれ）は、現行のgraph単位session、SkinDeform入力差し替え後の再評価、skinned affineの監査保持、元行列の標準／拡張GLB出力、`WorldPoints`統一で対応済み。P2（linear material／metallic既定値、局所material slot、装着先保持、PhysBones source hash、揺れUndo、GLB共通root・morph bounds、normal/tangent morph）も回帰へ含まれている。

現行HEADで `dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore` を再実行し **469 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-1d49dba164734af09d72b54d1fd85b6f`）。`Builds/FeedbackRecheckV1/NyaForge.exe` のprivate一時RadDollV3 VRM取込→EditMesh→native Save/Open→標準skinned GLB出力→再取込を含むWindows Authoring suiteは **82 checks PASS**（`Artifacts/Authoring-20260913-124716-f48f3153e47d44cb8b3d10c6ef2ff315/report.json`、画面 `authoring.png`）。同成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-124847-559-1100e92cfca44765b56a0a95d61954be/bridge-report.json`）。private素材はpublic repositoryへ追加していない。

レビュー本文にある未修正という判定は古いcommit基準のため、同じ修正を重ねていない。実マウス／DPI差／Explorer実クリック、実VRChat内の見た目・PhysBones、実SDK受入、自動fit・貫通修正、異なるskeletonの結合、完全な外部texture・animation・VRM拡張保持、標準VRM出力は引き続き別境界とする。
# 2026-09-13 real-model external base-color import recheck

private一時RadDollV3 VRMを元データのままリポジトリへ追加せず、base-color画像1枚を`textures/rad-doll-base.png`へ分離したGLB/VRM入力（相対URI、画像2,713,743 bytes、`bufferView`なし）を生成して、外部base-color経路を実モデルで確認した。Windows Player `Builds/ExternalImageRealV1/NyaForge.exe` の800x600 Authoring suiteは **PASS、全チェック完了**（`Artifacts/Authoring-20260913-122518-c59cfd830965477faae7b40c245413e1/report.json`、画面 `authoring.png`）。この経路では候補選択、外部画像付きVRM取込、既存の編集、native Save/Open、標準GLB出力まで通過した。同成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-122636-976-32e1c3b7b48b4226bca2ab84bbe41c9b/bridge-report.json`）。元VRM・分離画像・生成入力は`C:\Users\tomoaki\AppData\Local\Temp\NyaForge-RealModelExternal-bdae4b667e6f41c792b056f8a23a9ab2`に置き、public repositoryへ追加していない。実VRChat内の見た目・挙動、標準VRM出力、完全な追加texture map/animation/VRM拡張保持、自動fit・貫通修正、実マウス/DPI差は引き続き未検証境界とする。

# 2026-09-13 current acceptance recheck

現行HEADのCore回帰を再実行し、**469 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-72e7c3c8bf884aa8bf08783a10569021`）。`Builds/ExternalImageRealV1/NyaForge.exe`の1280x800 Navigation smokeも **PASS**（`Artifacts/Navigation-20260913-123128-8566e55139864513997702cc86995cb1/report.json`）で、パック選択、キャンセル時の状態保持、確認セット、設定画面、制作画面への遷移を再確認した。`main.png`／`sets.png`／`settings.png`で文字欠けのない表示を確認した。Computer Useのネイティブアプリ列挙はこの環境で`apps: []`かつPlayer起動API未提供だったため、実マウス／DPI差の受入は自動画像・UI smokeと分離して未検証のままとする。レビュー対応と実モデル外部画像経路の証拠は前項へ記録し、次は実SDK受け取りまたは共有参照の仕様化へ進む。

# 2026-09-13 encoded external image URI hardening

外部base-color画像URIのパーセントエンコードを復号してから相対パス検査するようにした。`%2E`などの通常のファイル名表記を受け入れ、復号後の絶対パス・remote URI・NUL・`..`によるモデルフォルダ外への脱出は従来どおり拒否する。Coreは **469 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b8622a14fdb142c39010cbe5384e2ddc`）。Windows Player `Builds/UriV1/NyaForge.exe`の800x600 Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-123450-fc13e67f9c3e4363bd1c0caf022fdc99/report.json`）、Unity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-123523-876-0e11abb8f3a742218f2efb42f0cbc413/bridge-report.json`）。

# 2026-09-13 bounded external image read and real-model recheck

外部画像の読み込みを16MiB上限付きFileStreamへ変更し、読み込み中のファイル差し替えや権限／I/O失敗を未処理例外にせず、`IMAGE_BUDGET_EXCEEDED`／`EXTERNAL_RESOURCE_MISSING`／`EXTERNAL_RESOURCE_UNAVAILABLE`として返すようにした。Core回帰は **469 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-850b459bf65e440e9ef0d7ca4d4f93ab`）。`Builds/UriV2/NyaForge.exe`でprivate一時RadDollV3の外部base-color画像付きVRMを再取込し、EditMesh、native Save/Open、標準GLB出力まで **PASS**（`Artifacts/Authoring-20260913-123800-52cd3d9e8cf54109836ec1e79f6d2d89/report.json`）。同成果物のUnity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-123916-047-006f669c0bf4403491a4577693bd5ac2/bridge-report.json`）。

# 2026-09-13 bounded GLB/VRM model read

GLB/VRM本体の取込もFileInfo確認後の無制限`ReadAllBytes`をやめ、128MiB上限付きFileStreamと読み込み失敗の明示診断へ揃えた。`Builds/BoundedImportV1/NyaForge.exe`の800x600 Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-124309-7268891a990d48588306e34cb69f8f6c/report.json`）、Unity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-124340-491-826e8d7c991340bf9fba45dcf2be42bf/bridge-report.json`）。外部画像経路のCore 469件回帰は前項の結果を正とする。

# 2026-09-13 external base-color image import

GLB/VRM取込へ、モデルファイルと同じフォルダ配下の安全な相対URIによるbase-color画像を追加した。外部画像はnative Paintへ即時コピーしてSave/Open後も元ファイルへ依存しない。モデルフォルダ外への`..`、data URI、remote URI、欠落ファイル、16MiB超は明示エラーにする。埋め込み画像と既存のサイズ縮小経路は維持し、警告文と交換仕様書を実際の保持範囲へ更新した。

Core回帰は **469 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-70d826e4f3034f6783a84eb881a41b34`）。新規テストでローカルPNGの読込・MIME・バイト一致、path traversal、非対応WebP拒否を確認した。Windows Player `Builds/ExternalImageV3/NyaForge.exe` の800x600 Authoring suiteは **PASS、79 checks**（`Artifacts/Authoring-20260913-121649-4baab4f47e744278af3c3fc1cab1922f/report.json`）、Unity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-121721-131-45d72974b08d46ffaf28e75b0c800dd6/bridge-report.json`）。Navigationは同じ実行内容を先行PlayerでPASS済み（`Artifacts/Navigation-20260913-121452-c9e3477d673549bd92a4f9e6b5b2b8a8/report.json`）。実モデルの外部URIは未観測のため、次回はprivate一時入力でこの経路を目視確認する。標準VRM出力、完全な追加texture map/animation/VRM拡張保持、実VRChat内受入は引き続き未完了境界とする。

# 2026-09-13 Windows navigation smoke recheck

最新の`Builds/ExportReportV4/NyaForge.exe`で`Tools/Test-NyaForgeNavigation.ps1 -BuildName ExportReportV4 -Width 1280 -Height 800`を実行し、**PASS**（`Artifacts/Navigation-20260913-120737-e54944adc0dc4bd89cc03ea2a5cf7060/report.json`）。パック選択、キャンセル時の現状態保持、不正パス保持、named sessionの保存/再読込、utility panelの折り畳み、制作画面への遷移とusable viewportを確認した。これはスクリプト化されたWindows Playerナビゲーション証拠で、実マウスの個体差・DPI設定差は別の手動受入境界とする。公開READMEにもGLBレポートの`glbHash`照合方法を追記した。

# 2026-09-13 GLB export report hash binding recheck

標準GLBの出力レポートへ `glbHash`（`model.glb`本体のSHA-256）を追加し、レポートとバイナリが同じ成果物か機械的に照合できるようにした。Core回帰の静的GLBレポート、Workbenchのskin-bound衣装検証、外部MCP→Player検証で本体ハッシュを確認する契約を揃えた。クイックスタートにも照合方法を記載した。

最新Core回帰は **468 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f75abb38c9684f498a2b7d9dfa5295b9`）。Windows Player `Builds/ExportReportV4/NyaForge.exe` の800x600 Authoring suiteは **PASS、82 checks**（`Artifacts/Authoring-20260913-115430-d76ec906a1144a26849c256fe25a28f5/report.json`）。外部MCPのrevision-pinned GLB出力、`reportPath`、同一出力先の再送拒否、レポート内state hash／GLB hash照合、文書状態不変を含む。private一時RadDollV3 VRM（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`）でも **PASS、82 checks**（`Artifacts/Authoring-20260913-115830-b9f27f6f5b8c41cdae29dcae272fbe0f/report.json`）。両成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（最新実モデル経路: `Artifacts/BridgeReceiver-20260913-120250-409-d0658a1587154f43b0eb62325454c9c7/bridge-report.json`）。実VRChat内の見た目・挙動、標準VRM出力、実マウス／DPI差、衣装自動fit・貫通修正は引き続き未検証境界とする。

# 2026-09-13 final export-report real-model recheck

最新のWindows Player `Builds/ExportReportV3/NyaForge.exe` で、private一時RadDollV3 VRM（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`、public repositoryへ追加しない）を読み取り専用で再検証した。800x600 Authoring suiteは **PASS、82 checks**（`Artifacts/Authoring-20260913-114549-e48c45f2dcf24379a057688838b8510b/report.json`）。実モデルの候補選択、EditMesh頂点編集、native Save/Open、標準skinned GLB出力、出力GLB再取込、材質画像の既存予算処理に加え、skin-bound衣装導線も通過した。GLB出力フォルダには`export-report.json`が生成され、document ID・revision・state hash・profile・対象件数・保持しないVRM/graph metadataを確認した。同成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-115021-716-c4dcd03ad3f14941a1c67e8671f82bd1/bridge-report.json`）。これは実モデルの自動smokeと出力記録の証拠で、実マウス/DPI差、実VRChat内の見た目・挙動、標準VRM出力、自動fit・貫通修正は引き続き未検証境界とする。

# 2026-09-13 GLB export snapshot report (final verification)

標準GLB出力の同じフォルダへ `export-report.json` を原子的に同梱するようにした。レポートはversion、profile、単位・座標、document ID、document revision、state hash、対象ごとの頂点数・三角形数・submesh数・材質slot数、標準GLBで保持しないgraph／VRM metadataを記録する。GUIのステータスと外部MCPの成功レスポンスにもreportPathを返すため、GLB単体を別スナップショットの成果物と取り違えにくい。GLB本体とレポートは同一stagingから移動し、出力失敗時に中途半端な公開物を残さない。

Coreは **468 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-9e9f78478bc945b6af161ff9d0a47c9c`）。静的GLB出力のレポートがdocument ID・state hash・profile・objectCountを保持すること、skin-bound衣装の標準GLBでもレポートを生成することを回帰した。Windows Player `Builds/ExportReportV3/NyaForge.exe` の800x600 Authoring suiteは **PASS、80 checks**（`Artifacts/Authoring-20260913-113941-7f5c0d76a5f846649c55f74f0d44bbbc/report.json`）で、外部MCP client→stdio sidecar→named pipe→PlayerのGLB export検証、success responseのreportPath、同一exportId再送拒否、document state不変を含む。Unity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-114329-943-c2610adafca54487be6c1717f853a994/bridge-report.json`）。reportのlimitationsにはrest pose、influence上限、graph/native metadata非埋込み、VRM extensions非出力を明記する。実VRChat受け入れ、標準VRM出力、実マウス/DPI差は引き続き別境界とする。

# 2026-09-13 persist avatar pose-copy source across reopen

skin-bind衣装へavatarの現在poseをコピーした後、複数avatar候補がある状態でnative projectを再読込しても同じ対象を選べるよう、graph metadata node `rig.pose-source` を追加した。PoseSourceはavatarのstable object IDだけを保持し、WorkbenchのRefresh時にattachment targetのfallbackとして使う。skin-bind時とposeコピー時に更新し、GraphBinaryCodec・Inspection・標準skinned GLB whitelistへ対応した。標準GLBはこの制作メタデータを持たないため、native project側でのみ復元する。

Coreは **454 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-2e1aa8cd7b834460b53cb64a49bef8ee`）。回帰ではPoseSourceのavatar object IDがnative Save/Openで保持され、pose-copy済み衣装の標準skinned GLB exportが成功することを確認した。Windows Player `Builds/AccessoryPoseSourceV1/NyaForge.exe` の800x600 Authoring suiteは **PASS、79 checks**（`Artifacts/Authoring-20260913-111322-4fde8bc8f8ef4e97a3209998b17b3282/report.json`、画面 `authoring.png`）。このsuiteでは別avatar＋static accessoryのskin-bind、Root pose変更→明示poseコピー、native Save/Open後のpose hash照合、rest復帰とGLB出力まで通過した。同成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-111704-717-0a3f174945384229aa5369868688eee4/bridge-report.json`）。実マウス/DPI差、実VRChat SDK・実アバター内の見た目、衣装の自動fit・貫通修正、完全なVRM出力は引き続き別境界とする。

# 2026-09-13 explicit avatar pose copy for skin-bound clothing

skin-bindした衣装をavatarと同じ姿勢で確認できるよう、Workbenchの小物パネルへ「avatarの現在poseを衣装へコピー」を追加した。選択したavatarの評価済みPoseを、同じstable skeletonを持つ衣装側Pose nodeへ明示的に再bindして保存する。avatarとのライブ共有ではなく、姿勢を変更した場合は再度コピーする運用とし、異なるskeletonの自動結合や自動fit・貫通修正は対象外とする。Rig panelのweight編集と組み合わせ、Root初期化後の袖・裾などの確認をしやすくする。

Coreは **468 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b7ea42deef8749aba7e5b2d4e9cbca98`）。新規回帰ではavatar poseを衣装skeletonへ明示rebindした変形がnative Save/Open後も一致することを確認した。Windows Player `Builds/AccessorySkinV7/NyaForge.exe` の800x600 Authoring suiteは **PASS、79 checks**（`Artifacts/Authoring-20260913-110144-8f97721ee38d4c8ab91f23fcc2b90c77/report.json`、画面 `authoring.png`）。このsuiteではavatar poseを変更→衣装へコピー→pose hashをnative Save/Openで照合→restへ戻してGLB出力まで確認した。同成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-110519-475-d0a0b7d6b45b42bd92e6c657e521d56d/bridge-report.json`）。これはskin-bind後の姿勢確認導線を示す自動証拠であり、ボタン操作そのものの実マウス受入、DPI差、実VRChat SDK・実アバター内の見た目受入は別境界として記録する。

# 2026-09-13 accessory skin-binding workflow

衣装制作の次段として、別graph objectで取り込んだstatic GLBを、選択したVRM/GLB avatarの骨格へ変換する導線を追加した。`AccessorySkinBindingAdapter`は既存のSource／Morph／EditMesh／材質経路を保ったまま、avatarのskeleton・rest pose・skin-bind・skin-deformを追加し、全頂点をRoot boneへ100%で初期化する。Workbenchの「衣装をavatar骨格へskin-bind（Root初期化）」から実行でき、以降はRig panelのweight混合／weight paintで袖・裾などを割り当てられる。既存の剛体attachmentとの同時使用は拒否して二重変形を防ぐ。

Coreは **468 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-37c2a84eee484c7e97ce92ae1f4652b4`）。新規回帰ではRoot初期化、pose変形、native Save/Open、剛体attachment競合拒否を確認した。Windows Player `Builds/AccessorySkinV2/NyaForge.exe` のAuthoring suiteは **PASS、79 checks**（`Artifacts/Authoring-20260913-103030-f7976291411648c4b15338855d36ad29/report.json`）で、VRM avatar＋static accessoryのEditMesh→剛体装着→Save/Open→skin-bind→native exportを通過した。同成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-103409-141-90be8f007e9443bea647d660eb64bf0d/bridge-report.json`）。これはRoot初期化とweight編集準備までの証拠で、avatar poseの自動共有、自動fit・貫通修正、実VRChat内受入は別境界として残す。

# 2026-09-13 pasted feedback recheck on current HEAD

今回の貼り付けレビュー（基準 `0d1e957`）を現行HEAD `c4fd4f4`へ再照合した。レビューにある5件のP1（複数graph metadataの所属、source skinの二重変形、skinned node affine、inverse-bind出力、装着後の選択判定）は、現行コードでそれぞれgraphId単位のsession table、SkinDeform入力差し替え後の再評価、skinned affineの監査metadata化、保持したinverse-bind行列の標準／拡張GLB出力、`WorldPoints`による描画・Frame・選択の統一として実装済み。P2の材質linear値・metallic既定値、primitive単位の材質、省略texture、装着先保持、PhysBones source hash、揺れUndo／再bind、GLB共通root・morph bounds、normal/tangent morph変換も現行回帰へ含まれている。静的小物の頂点編集導線も追加済みで、別VRM avatar＋static GLB accessoryのEditMesh→stable BoneId装着→native Save/Open→feature-preserving exportを確認している。

再実行した `dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore` は **466 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f7d63fcb0ae944dca2e73c04bc182ba8`）。この結果はCore回帰の証拠で、実マウス／DPI差／Explorer実クリック、実Unity SDK・実VRChat内の見た目とPhysBones、衣装の自動fit・貫通修正、異なるskeletonの結合、完全なVRM出力は別境界として残す。次の開発カードは、共有mesh／skin／morph参照の仕様化か、固定した実SDK受け取り検証のどちらか一つに絞る。

# 2026-09-13 pasted feedback recheck on current main

## Separate avatar and accessory authoring workflow

静的GLBをgraph objectへ取り込む経路にEditMesh段が無く、保存はできても頂点編集できない穴を修正した。static／skinの両方でSource（必要ならMorph）→EditMesh→Material/Outputを構成し、別ファイルのVRM avatarとstatic GLB小物を同じ作品へ追加できるようにした。小物をrest-spaceで頂点編集した後、GUIからavatar objectとstable BoneIdを選んで`object.attachment`を作成し、native Save/Openではtarget・BoneId・編集結果を保持する。attachment付きのUnity出力は情報を落とさないnative project packageへルーティングする。

Coreは再実行して **466 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-8ab2aaa111a84a5c972dd2855fb0df67`）。`Builds/ImportedAccessoryV2/NyaForge.exe` の800x600 Authoring suiteは **PASS、82 checks**（private一時RadDollV3 VRMを含む取込→EditMesh頂点編集→stable BoneId装着→native Save/Open→feature-preserving export、report `Artifacts/Authoring-20260913-101222-7c49de2909e0473990c6eaa4364c2e0d/report.json`）。同成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-101342-514-202f68aad9c94bd08fdb86c8c8b10c9c/bridge-report.json`）。画面キャプチャ `Artifacts/Authoring-20260913-101222-7c49de2909e0473990c6eaa4364c2e0d/authoring.png` はUIと制作cameraの非空描画を確認した。これは合成static accessoryとprivate avatarの自動経路であり、実マウス/DPI差、実VRChat内の見た目、衣装の自動fit・貫通修正は別境界として残る。

## Graph-keyed secondary-motion attachment

複数graphを一つのnative projectへ追加した際、旧来の単一 `secondary-motion.nyaforge.bin` が別graphの揺れ設定として表示される境界を閉じた。`SecondaryMotionSessionsCodec`（`NVSX` v1）で共通揺れassetをGraphIdごとに保存し、active object切替時はそのgraphのassetだけを表示・再生・再bind対象にする。旧単一assetは読込時にactive graphへ互換移行し、次回保存時にtableへ変換する。既存のVRM expression／Spring／rig session tableと同じ所有単位へ揃えた。

Coreは **466 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-09cbb83127594ddf9dc48dc24f570994`）。`Builds/SecondaryMotionSessionsV2/NyaForge.exe` の800x600 Authoring suiteは **PASS、81 checks**（private一時RadDollV3 VRMの取込→候補選択→EditMesh→Save/Open→標準skinned GLB再取込、VRM0/1 playback、MCP lifecycleを含む、report `Artifacts/Authoring-20260913-100432-be47d5cabfa24764a85d18e290226470/report.json`）。同成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-100559-883-c623b841ae61434eb75b6732de6c7a12/bridge-report.json`）。private素材はpublic repositoryへ追加していない。

## README capability alignment

公開READMEとAuthoring Core READMEに残っていた古い能力表記を現行実装へ更新した。root READMEはnative schema 4（旧schema読込互換）、standard static/skinned GLB profile、MCP inspection/exportを明記し、Authoring READMEはstandard skinned GLB／外部MCPを実装済みとして、完全VRM・実VRChat SDK・自動fit等の境界を残した。ドキュメント変更後も `dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj` は **465 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-ee87e9a0c12e40c8838a497d8f3b154a`）。

貼り付けられたレビュー（基準 `0d1e957`）を、現行 `main` の `f5b908a` に再照合した。レビューの5件のP1は、複数graphのrig／expression／spring所属保存、source skinを`SkinDeform`入力へ組み込む評価順、skinned node affineの監査metadata化、元のinverse-bind行列の標準／拡張GLB出力への継承、装着後`WorldPoints`による描画・Frame・選択判定の統一で対応済み。P2の材質linear値・metallic既定値、primitive単位の材質、省略textureの扱い、装着先保持、PhysBones source hash、揺れUndo／再bind、複数rootとmorph bounds、normal/tangent morph変換も回帰へ含まれている。

現行Core回帰は **465 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-50ab507c88d04f23b0f72eeda0c001a6`）。Windows Playerの800x600 Authoring suiteとUnity **2022.3.22f1** synthetic Bridge、private一時RadDollV3 VRMの取込→EditMesh→native Save/Open→標準skinned GLB出力→再取込は直前カードのPASSを正とする。レビュー文面の古いhashや「未修正」という判定を現行状態へ持ち込まない。

残る境界は、実マウス／DPI差／Explorer実クリック、実VRChat SDK・実アバター内の見た目とPhysBones動作、自動fit・貫通修正、異なるskeletonの結合、共有mesh／skin／morph参照、完全な外部texture・animation・VRM拡張保持、標準VRM出力。次の開発カードは、共有参照の明示仕様化か、実SDK版を固定した受け取り検証のどちらか一つに絞る。

# 2026-09-13 goal audit Player acceptance

現行HEADから別フォルダへ作成した `Builds/GoalAuditV1/NyaForge.exe` を、公開fixtureとprivate一時RadDollV3 VRM（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`）で再検証した。1280x800のAuthoring suiteは **81 checks PASS**（real GLB/VRM command-line import、候補選択、生成EditMesh、頂点編集、native Save/Open、標準skinned GLB出力と再取込を含む、report `Artifacts/Authoring-20260913-093714-3087afd197c3424fb519575d5641e6a6/report.json`）。同成果物をUnity **2022.3.22f1** synthetic Bridgeへ渡した検証も **PASS**（`Artifacts/BridgeReceiver-20260913-093847-492-b8cfb82dc0064581bdc0a78e38514dbc/bridge-report.json`）。最終画面 `Artifacts/Authoring-20260913-093714-3087afd197c3424fb519575d5641e6a6/authoring.png` はUI Toolkitと制作cameraが同時に描画され、右パネルはスクロール可能だった。これは自動検証と画面画像の確認であり、実マウス/DPI差、実VRChat内の見た目、実SDK受入を代替しない。

# 2026-09-13 goal audit dense-paint performance

同じ `Builds/GoalAuditV1/NyaForge.exe` の高密度Paint合成fixture（131,072 triangles、30 frame samples）を観測した。`dense-paint-profile.json`（`Artifacts/Authoring-20260913-093945-3fdc0fe97a464f3892f01c7d5808d139/dense-paint-profile.json`）では平均 **66.6668 ms**、P95 **66.6672 ms**、最大 **66.6673 ms**、観測予算250 msを超過しなかった。設定はtargetFrameRate 15、vSync 0、RTX 4090、Unity 6000.4.3f1。GC generation 0は305→335、managed heapは152,080,384→207,351,808 bytesだった。同成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-094052-865-3fb874064601480299724519f74346cf/bridge-report.json`）。これは合成fixtureと現行GPUでの観測であり、実アバター・別GPU・通常の60fps設定の性能保証ではない。

# 2026-09-13 goal audit external MCP acceptance

同じ `Builds/GoalAuditV1/NyaForge.exe` と `Tests/Mcp.Transport/bin/Debug/net10.0/Mcp.Transport.Tests.dll` を使い、外部MCP client→sidecar→live Playerの経路を再検証した。Authoring suiteは **PASS**（report `Artifacts/Authoring-20260913-094343-a58800080c474e95b92512b941da0228/report.json`）。revision固定の標準GLB出力、既存出力先の再実行拒否、文書状態不変を確認した。別実行ではsecondary-motionのstate/play/pause/rebuild/fixed-step/resetも **PASS**（report `Artifacts/Authoring-20260913-094233-ca3a879a1b2a461bb4e129a21e816145/report.json`、`mcp-external.log`）。GLB export成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-094512-926-b7d3cd6946e84fceb09827135c45f99c/bridge-report.json`）。

# 2026-09-13 attachment target retention fix

装着先Dropdownの変更イベントが表示ラベル（`graph · <id>`）をstable object IDとして保持していたため、Refresh後に選択が先頭へ戻る経路を修正した。選択肢のindexから内部IDを保存し、表示ラベルと契約IDを分離した。回帰では異なるskeleton sessionを持つ2つのavatar graph objectを用意し、2番目の対象を選択→Refreshしても選択値が保持されることを確認した。`Builds/AttachmentTargetRetentionV1/NyaForge.exe` の800x600 Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-095046-9bd3831ed6124ff7bfd59ea819ab6f37/report.json`）。同成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-095216-432-f3e7c59ba0794f8ab91d312b43f99e28/bridge-report.json`）。

# 2026-09-13 attachment target retention real-model recheck

Dropdown保持修正後の `Builds/AttachmentTargetRetentionV1/NyaForge.exe` でprivate一時RadDollV3 VRM（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`）を再検証した。800x600 Authoring suiteは **81 checks PASS**（取込候補選択、EditMesh頂点編集、native Save/Open、標準skinned GLB出力と再取込を含む、`Artifacts/Authoring-20260913-095259-96c5ddad45374685ac91e3fc8f4bc3d3/report.json`）。同成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-095436-336-6aa7dd710b6442c382c2746cbdcf75a6/bridge-report.json`）。private素材はpublic repositoryへ追加していない。

# 2026-09-13 native UI acceptance bridge check

提示されたレビュー（基準 `0d1e957`）のP1/P2は、実装・Core回帰・Windows Player/Unity Bridge検証で閉じている。追加でWindowsの実マウス/DPI受入を確認するため、既存の `Builds/ValidationSkinV3/NyaForge.exe` を起動してComputer UseのネイティブUI列挙を試したが、このセッションのブリッジは `apps: []`（ブラウザのみ）を返し、Playerのアクセシビリティ状態やクリック結果を取得できなかった。したがって実マウス、DPI差、Explorer実クリックの受入証拠は作成していない。自動Authoring suiteのPASSを実操作受入へ読み替えず、次回はネイティブUIブリッジが有効な環境で、起動画面→制作画面→スクロール→候補選択→保存導線を一操作ずつ確認する。

# 2026-09-13 model import budget preflight acceptance

モデル読込前のファイルサイズ検査を含む `Builds/ImportBudgetV1/NyaForge.exe` で、公開GLB fixtureの候補確認→取込→EditMesh→native Save/Open→標準skinned GLB出力→再取込を実行した。800x600 Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-092556-4cd5f819b60e457aaad0727bf158020c/report.json`）。同Playerでprivate一時RadDollV3 VRM（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`）も同じ一周が **PASS**（`Artifacts/Authoring-20260913-092645-27e7a7c2541043f08144e248e38d4538/report.json`）。Unity **2022.3.22f1** synthetic Bridgeは両成果物で **PASS**（`Artifacts/BridgeReceiver-20260913-092629-232-47ea92d3e9ce4c9db054a917bd651be1/bridge-report.json`、`Artifacts/BridgeReceiver-20260913-092758-792-5ae5d0b2442047648736278ec2e77f7e/bridge-report.json`）。

# 2026-09-13 model import size preflight

GLB/VRM取込の候補確認と本取込で、`File.ReadAllBytes` より前にファイル存在と128 MiB import budgetを検査する共通 `ReadModelFile` を追加した。上限超過ファイルを不要にメモリへ載せず、パス不存在も取込処理の診断へ統一する。新Player `Builds/ImportBudgetV1/NyaForge.exe` の800x600 Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-092556-4cd5f819b60e457aaad0727bf158020c/report.json`）。private一時RadDollV3 VRMの候補選択→EditMesh→native Save/Open→標準skinned GLB出力→再取込も **PASS**（`Artifacts/Authoring-20260913-092645-27e7a7c2541043f08144e248e38d4538/report.json`）。Unity **2022.3.22f1** synthetic Bridgeも両成果物で **PASS**（`Artifacts/BridgeReceiver-20260913-092629-232-47ea92d3e9ce4c9db054a917bd651be1/bridge-report.json`、`Artifacts/BridgeReceiver-20260913-092758-792-5ae5d0b2442047648736278ec2e77f7e/bridge-report.json`）。

# 2026-09-13 GLB / VRM import labels

取込機能はGLBとVRMの両方に対応しているため、制作画面の折りたたみ見出し、ファイル選択ボタン、graph object取込ボタンの表示を「GLB / VRM」へ統一した。内部の候補選択・保存・出力契約は変更していない。`Builds/ImportLabelV1/NyaForge.exe` の800x600 Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-092256-409e806db2ca462c875c481c54b16f22/report.json`、画面 `authoring.png`）。同じ成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-092326-908-354689949db74e6a869bb488cab0d506/bridge-report.json`）。

# 2026-09-13 real RadDollV3 recheck after validation fix

private一時素材 `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm` を、画像リソース重複計上修正後の `Builds/ValidationTextureDedupV1/NyaForge.exe` で再取込した。候補選択→rest-space EditMesh頂点編集→native Save/Open→標準skinned GLB出力→再取込までのAuthoring suiteは **PASS**（800x600、`Artifacts/Authoring-20260913-091957-e8571df5858c48958b862af3a3ebcfc5/report.json`、画面 `authoring.png`）。同じ成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-092110-708-47b95c99f5ea44bd9645d430261a0f04/bridge-report.json`）。private素材はpublic repositoryへ追加していない。実マウス/DPI差、実VRChat SDK/VRChat内の見た目受入とは分けて扱う。

# 2026-09-13 validation texture resource deduplication

材質接続後の評価値は同じbase-color画像を出力のlegacy `BaseColor` と `Material.BaseColor` の両方へ保持するため、出力チェックのtexture数がグラフ参照数を数えて実リソース数より多くなる場合があった。`AuthoringValidationReader` は画像hashを一度だけ数えるようにし、同一画像を材質から複数参照しても容量判定が過大にならないよう修正した。Coreへ重複参照の回帰を追加し、**465 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-2ce6c7d8eaf547fbad1d9c8d09714515`）。Windows Player `Builds/ValidationTextureDedupV1/NyaForge.exe` の800x600 Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-091728-f26ad2de57b24a038163c89a57e89b8e/report.json`、画面 `authoring.png`）。同じ成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-091801-690-5b07e4e16c6e4f1e91c24785c6675491/bridge-report.json`）。

# 2026-09-13 latest automated acceptance recheck

現行HEAD `e999ec2` でCoreを再実行し、**465 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-2ce6c7d8eaf547fbad1d9c8d09714515`）。画像リソース重複計上修正を含む `Builds/ValidationTextureDedupV1/NyaForge.exe` の800x600 Authoring suiteも **PASS**（`Artifacts/Authoring-20260913-091728-f26ad2de57b24a038163c89a57e89b8e/report.json`、画面 `authoring.png`）。同じPlayer検証成果物をUnity **2022.3.22f1** synthetic Bridgeへ渡した結果も **PASS**（`Artifacts/BridgeReceiver-20260913-091801-690-5b07e4e16c6e4f1e91c24785c6675491/bridge-report.json`）。これは自動回帰・合成receiverの証拠であり、実マウス/DPI差、実VRChat SDK/実アバター内の見た目受入とは分けて扱う。

# 2026-09-13 material output guidance

材質パネルに残っていた「材質付きUnity出力は開発中」という古い案内を、対応するUnity用profileへ書き出せる説明へ更新した。実装範囲とGUI案内の不一致を解消し、Windows Player `Builds/MaterialMessagingV1/NyaForge.exe` の800x600 Authoring suite **78 checks PASS**（`Artifacts/Authoring-20260913-090623-889afe772a2a44dba831a8fab5c80adf/report.json`）で既存操作の回帰がないことを確認した。

# 2026-09-13 authoring validation skin capacity

「出力チェック」でスキン付きgraphの骨数と頂点あたり最大influenceが常にunknownになる境界を修正した。評価済みの出力到達graphにある`SkinBindingOutputs`／`SkeletonOutputs`から実値を集計し、NyaForgeのauthoring容量（512骨／32 influence）へ明示判定する。未接続の作業用rigは集計から除外する。GUIには`actual/limit`も表示する。skin bindingが無い静的graphは従来どおりunknown、`fit`は実アバター受入を含むためunknownのままとする。Core **464 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-bce4bb3011f945c48f007146f1832ddf`）。Windows Player `Builds/ValidationSkinV3/NyaForge.exe` の800x600 Authoring suiteは **78 checks PASS**（`Artifacts/Authoring-20260913-090034-98ebf69a86954db2a008d2d70b6d785e/report.json`）。同成果物のUnity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-090105-766-345e4b00828e4a0e8926294900142ae7/bridge-report.json`）。さらに同じValidationSkinV3でprivate一時素材のRadDollV3 VRMを候補選択→EditMesh頂点編集→native Save/Open→標準skinned GLB出力→再取込まで再確認し、Authoring report `Artifacts/Authoring-20260913-090318-0e84c8ce478a4d03b9f76660af3b2802/report.json` とUnity Bridge `Artifacts/BridgeReceiver-20260913-090435-743-a95c669a3bad4760a2d6f6bd5f5ea377/bridge-report.json` はともに **PASS**。private素材はpublic repositoryへ追加していない。

# 2026-09-13 GLB skeleton/morph conformance

GLB出力の仕様穴を追加で閉じた。複数の親なしboneを持つskinned出力では `NyaForgeSkeletonRoot` を生成し、skinの `skeleton` から全joint rootへ到達できる共通祖先を出力する。morphの `POSITION` accessorにはglTF必須の `min/max` を付け、slot primitive側も同じ規則に揃えた。新しいCore回帰を含め **463 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f891479b2fe344a1a8935eb6974232c6`）。

Windows Player `Builds/MorphBoundsV1/NyaForge.exe` の800x600 Authoring suiteも **PASS**（`Artifacts/Authoring-20260913-084200-71702ef26aa14b9389f34352f292694a/report.json`、画面 `authoring.png`）。同成果物をUnity **2022.3.22f1** Bridgeへ渡した検証も **PASS**（`Artifacts/BridgeReceiver-20260913-084245-214-4bdf3a3c031a/bridge-report.json`）。Bridgeは合成receiverであり、実VRChat SDK/実アバター受入とは分ける。

# 2026-09-13 feedback recheck (`0d1e957`)

提示されたレビュー（基準 `0d1e957`）を現行HEAD `51eb4af`へ再照合し、Coreを再実行した。結果は **461 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4aec0876bd0c47cbb63c11a58443b069`）。5件のP1は重複実装せず、次の対応を現行の正として記録する。

| 指摘 | 現行対応 | 回帰・残る境界 |
| --- | --- | --- |
| 複数モデル後のSave/Openで情報が混ざる | rig、expression、springをgraphId単位のsession tableへ保存し、旧single attachmentはgraphIdへ移行する | 異なるskeletonの自動結合、共有mesh/skin/morph参照は未対応 |
| source skinの二重変形／下流編集消失 | `SkinDeform`入力位置へsource paletteを差し替えてgraphを再評価し、EditMesh/Morph/材質/Outputを同じ順序で保持 | 実アバターの自動fit・貫通とVRChat内見た目は未受入 |
| skinned node affineの二重適用 | skinned表示と標準GLB出力はjoint world frame＋保持したinverse-bindを正本にし、mesh node affineは監査metadataだけにする | 任意GLBの全node意味保存は未完了 |
| 出力時のinverse-bind欠落 | sourceの一般inverse-bind行列をnative rig sessionから標準／拡張skinned GLB writerへ渡す。scale・回転を含む行列回帰を維持 | 実Unity/VRChat receiverでの拡張profile受入は未確認 |
| 装着後のクリック判定ずれ | 描画・Frame・選択判定をroot変換後の`WorldPoints`で統一し、ローカル点とは分離する | 実マウス、DPI差の受入は未実施 |

列挙されたP2（linear `baseColorFactor`／metallic既定値、局所material slot、装着先選択保持、PhysBones source hash、揺れUndo revision、GLB共通root・morph境界、normal/tangent morph変換）も現行回帰で閉じている。埋め込みbase-color画像はnative Paintへ縮小保持し、Save/Openと標準skinned GLB再取込まで確認済み。完全な外部texture・animation・VRM拡張保持、実VRChat SDK/PhysBones受入は引き続き別タスクとする。

# 2026-09-13 embedded base-color GLB output recheck

実RadDollV3で、native Paintへ縮小保持した埋め込みbase-color画像が標準skinned GLB出力で失われないことを追加確認した。出力GLBをskin importerで再読込し、画像付きmaterialが存在することを検証している。Coreは直前の **461 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-a7b92fd3fb184402a46aef4f4dfa676c`）を正とする。Windows Player `Builds/MaterialResizeV3/NyaForge.exe` のAuthoring **80 checks PASS**（`Artifacts/Authoring-20260913-082925-87045e69ee724ec791289be206d54dbe/report.json`）、Unity **2022.3.22f1** Bridge **PASS**（`Artifacts/BridgeReceiver-20260913-083342-809-9f1c2a45fffe418e81ce36a04541093f/bridge-report.json`）。

# 2026-09-13 embedded base-color Save/Open hash recheck

実RadDollV3の埋め込みbase-color画像をnative Paintへ縮小保持する経路について、取込直後だけでなくnative Save/Open後のPaint画像hash一致も検証条件へ追加した。private一時素材はリポジトリへ入れていない。Coreは直前の **461 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-a7b92fd3fb184402a46aef4f4dfa676c`）を正とする。Windows Player `Builds/MaterialResizeV2/NyaForge.exe` のAuthoring **80 checks PASS**（`Artifacts/Authoring-20260913-082108-0f81ed3a0c5d485f8cfb874f8b467fb4/report.json`）、Unity **2022.3.22f1** Bridge **PASS**（`Artifacts/BridgeReceiver-20260913-082532-936-f38ed0cdd41e46b4ba92be111f08e16c/bridge-report.json`）。

# 2026-09-13 embedded base-color image budget retention

GLB/VRMの埋め込みbase-color画像が1024pxを超える場合、取込を白一色へ退避せず、最大8192pxまでデコードしてアスペクト比を保った最近傍縮小を行い、native Paintの1024px上限へ所有データとして保存するようにした。8192px超・壊れた画像・16MiB超は従来どおり明示診断で省略し、外部URIやbase-color以外のtexture mapは推測取得しない。実RadDollV3（private一時素材、埋め込み画像6件）で縮小後のPaintノードとSave/Open依存なしを確認した。Core **461 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-a7b92fd3fb184402a46aef4f4dfa676c`）。Windows Player `Builds/MaterialResizeV1/NyaForge.exe` Authoring **80 checks PASS**（`Artifacts/Authoring-20260913-081327-dc634884b6f0475e9ebbcabff60eff72/report.json`）、Unity **2022.3.22f1** Bridge **PASS**（`Artifacts/BridgeReceiver-20260913-081803-691-55c8fee7b2794de8a139d297b4ca5482/bridge-report.json`）。

# 2026-09-13 glTF material color/default alignment

glTF `baseColorFactor` は線形値として読み書きするよう修正し、取込時の不要なsRGB変換と出力時の逆変換を廃止した。`metallicFactor` の省略値もglTF仕様の1へ合わせ、linear RGBと省略既定値の回帰を追加した。Core **460 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-66295ed6bf6a4d98815455f15972ac53`）。Windows Player `Builds/MaterialLinearV1/NyaForge.exe` Authoring suite（`Artifacts/Authoring-20260913-074940-af62bbf5049a4276bccce18c480d93e2/report.json`）とUnity Bridge（`Artifacts/BridgeReceiver-20260913-075048-365-c4c2956b407148a6afe5110e50f45e0f/bridge-report.json`）はPASS。

# 2026-09-13 skinned node affine policy

skinned表示とWorkbench/MCPの標準skinned GLB出力で、mesh node affineをスキン結果へ二重適用しない方針へ揃えた。joint world frameとinverse-bindを正本にし、選択nodeのaffineはsource sessionの監査metadataとして保持する。Core **461 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-a7b92fd3fb184402a46aef4f4dfa676c`）。Windows Player `Builds/SkinnedAffinePolicyV2/NyaForge.exe` で、private一時素材 `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm` を使った Authoring **79 checks PASS**（`Artifacts/Authoring-20260913-080806-87aacf4f39c74048b4c00a8a11c52212/report.json`）、Unity **2022.3.22f1** Bridge **PASS**（`Artifacts/BridgeReceiver-20260913-080912-368-fd387a22cef74d5faf30b6e7ab6e88f0/bridge-report.json`）。実モデルは取込・頂点編集・native Save/Open・標準skinned GLB出力と再取込の一致を確認した。材質画像はサイズ/形式制限でnative Paintへ保持せず、明示診断を残す。

# 2026-09-13 attachment selection and MCP bind parity

装着先Dropdownの選択値をRefresh間で保持し、別avatarを選んだ直後に先頭候補へ戻る経路を修正した。WorkbenchのGUIとMCPのskinned GLB出力へ、graph objectごとのimported inverse-bind行列を同じ経路で渡すよう統一した。Windows Player `Builds/AttachmentSelectionV1/NyaForge.exe` のAuthoring suite（`Artifacts/Authoring-20260913-075348-262a37a7fa964c90b373e7ce80254da5/report.json`）とUnity Bridge（`Artifacts/BridgeReceiver-20260913-075417-960-ac4b7ab2f333483ba5963fc238e1586b/bridge-report.json`）はPASS。

## 2026-09-13 consistency feedback final recheck (`5826247`)

提示されたレビュー（基準 `1c76e4a`）を現行 `main` の `5826247` へ再照合した。4件のP1と列挙されたP2は後続実装で閉じており、重複修正は行わず受入証拠を更新した。16bit `JOINTS_n` は2バイト幅で復号し、8bit/16bit同値・負weight拒否を回帰。source skin表示はgraph評価後の編集結果を保持し、PhysBones target packageはprofileが参照するsource assetを同梱してhash検証する。静的GLBのskin判定は選択meshに限定し、同居する小物を取り込める。

Core **459 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f3e6edffd8e847178fe7b97c4dd740e7`）。同一skeleton hashの複数skinned mesh、objectごとのinstance affineを含むGLB出力も回帰済み。実RadDollV3はprivate一時素材で取込→編集→native Save/Open→GLB出力を再確認し、public repositoryへ追加していない（Authoring report `Artifacts/Authoring-20260913-071034-98196e26e7af47cfa3df95cd3eab7bbd/report.json`、Bridge report `Artifacts/BridgeReceiver-20260913-071148-485-e6720b8b0f8d49858a5198eafa261b7/bridge-report.json`）。

残る受入境界は、異なるskeletonの結合、共有mesh/skin/morph参照、完全な材質・animation保持、実VRChat SDK/実アバター/VRChat内のPhysBones動作、実マウス/DPI差。次のタスクはこの境界を混ぜず、共有参照の仕様化または実SDK版固定の受け取り検証へ進める。

## 2026-09-13 source skin downstream evaluation (`SourceSkinOverrideV1`)

source skin表示を、評価済みの最終出力へ後掛けする経路から、`SkinDeform`ノード出力をsource paletteで差し替えて通常のgraph評価を再実行する経路へ変更した。これによりSkinDeform後のEditMesh、Morph、材質、Outputが同じ順序で評価され、非rest poseで編集差分が二重変形されない。通常評価のstale EditMesh保護は維持し、source projection時だけ同一domainの編集snapshotを現在の差し替えmeshへ再基準化する。

Core **459 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-bae7e375d86048a78ee5450d8cdeb43a`）。非rest pose＋SkinDeform後EditMeshのsource projection回帰を追加。Windows Player `Builds/SourceSkinOverrideV1/NyaForge.exe` のAuthoring suiteは実RadDollV3 private temp素材でPASS（`Artifacts/Authoring-20260913-072229-179ff5fd12ab4d18926fe2029b293dd9/report.json`）。同Player成果物のUnity 2022.3.22f1 BridgeもPASS（`Artifacts/BridgeReceiver-20260913-072338-010-fc41053f0aa542e6a35dbaf0f0a0176b/bridge-report.json`）。

## 2026-09-13 GLB候補選択Dropdown

GLB/VRM取込パネルの数値index入力を通常画面では折りたたみ、候補確認後にmesh resource・skin resource・node instanceを名前付きDropdownから選べるようにした。既存のIntegerFieldは自動検証と互換操作のため内部保持し、Dropdown選択を同じindex契約へ同期する。node instanceを選んだ場合は、従来どおりそのmesh／skin／配置を優先する。候補確認前の未確定状態は明示し、推測選択はしない。

Windows Player `Builds/ImportChoiceV2/NyaForge.exe` の800x600 Authoring suite **PASS**（report `Artifacts/Authoring-20260913-065009-b9aa3210c8cc4c30a05bb0c4c142e5b7/report.json`）。合成multi-mesh fixtureでmesh／skin／node候補名の表示、skinned graph取込、頂点編集、複数rig session切替を回帰した。Unity Bridge **PASS**（Unity 2022.3.22f1、`Artifacts/BridgeReceiver-20260913-065043-006-c42930cffe984517a674d26c96f817c2/bridge-report.json`）。Coreは前カードの **458 passed / 0 failed** を正とする。実マウス/DPI差、任意実モデルの全候補目視、実VRChat受入は別境界として残る。

## 2026-09-13 NativeLocatorV1 実RadDollV3再回帰

native manifest locatorを含む最新Windows Playerで、private一時素材 `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`（45,341,584 bytes）を再取込した。候補確認、rest-space頂点編集、native Save/Open、標準SkinnedGeometry GLB出力までのAuthoring suiteが **PASS**（800x600、report `Artifacts/Authoring-20260913-064533-45e34c13d02f42b3b7691b7dbbb0288f/report.json`）。同じPlayer出力をUnity **2022.3.22f1** Bridgeへ渡した検証も **PASS**（`Artifacts/BridgeReceiver-20260913-064639-406-78812740b9e34d00bccdc1869d314fe8/bridge-report.json`）。private素材はpublic repositoryへ追加していない。

これは実素材の取込・保存・出力smokeであり、実マウス操作、実VRChat内の見た目、PhysBones実SDK受入、自動fit・貫通修正の証拠ではない。

## 2026-09-13 native manifest locator contract

native projectの再開パス検証を`NativeProjectLocator.RequireManifestDirectory`へAuthoring層として切り出した。`project.nyaforge.json`以外、存在しないmanifest、空パス、未対応パスを同じerror codeで拒否し、Explorer GUIは既存の未保存確認と`ProjectStore.Open`へ渡す親フォルダだけを受け取る。manifest内容の検証やworkspace置換はlocatorへ混ぜず、既存責務を維持した。

Core **458 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-6a575829f424468d91cb8827b93aef37`）。Windows Player `Builds/NativeLocatorV1/NyaForge.exe` の800x600 Authoring suite **PASS**（report `Artifacts/Authoring-20260913-064253-149db09bb4a24715bc62d8a94ac93981/report.json`）。Unity Bridge **PASS**（report `Artifacts/BridgeReceiver-20260913-064327-846-0b11677610834177860b0cea90edfd4e/bridge-report.json`）。実Explorerクリック、DPI差、実VRChat SDK受入は別境界として残る。

## 2026-09-13 consistency feedback recheck (`13bc959`)

提示されたconsistency review（基準 `1c76e4a`）を、最新main `13bc959`へ再照合した。P1の4件は現行実装とCore回帰で閉じている。16bit `JOINTS_n` は2バイト幅で復号し、8bit/16bit同値を確認済み。source skin表示は`SourceSkinGraphAdapter.ApplyToEvaluation`で最終graph outputへ適用するため、SkinDeform後のEditMeshを保持する。PhysBones target packageはprofileが参照するsource assetを同梱し、receiver側でhashを検証する。静的GLB取込のskin判定は選択meshのnode instanceに限定し、同じファイル内の静的小物を拒否しない。

P2も、揺れreset時のsource-skin projection cache無効化、PhysBones managed markerのstable root/name再利用、削除curveの初期化、secondary-motion再bindのUndo、MCP captureのcamera metadata保持、負weightの事前拒否を実装・回帰済み。今回の再実行は **Core 454 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-40f038c9de1a437994666a376c0af406`）。Explorer project pickerを含むWindows PlayerとUnity Bridgeの直近PASSは前カードの記録を正とする。

残る受入境界は、実VRChat SDKでのPhysBones component生成・更新、実マウス/DPI差、実アバターの自動fit・貫通修正、VRChat内の見た目、異なるskeletonの結合・共有参照・完全な材質/animation保持。同一skeleton hashの複数skinned mesh出力は最新カードで回帰済み。次の作業はこの境界を混ぜず、実SDKの版・完全修飾型を固定した受け取り検証、または共有参照の明示仕様化から選ぶ。

## 2026-09-13 native project Explorer picker

制作フォルダを手入力せず再開できるよう、Windows Authoring画面へ「Explorerで選ぶ…」を追加した。Explorerで`project.nyaforge.json`を選ぶと親フォルダをnative projectとして検証し、別JSON・manifest欠落・存在しない選択は開かない。ダイアログ中にworkspaceのdocumentが変わった場合も置換せず、既存の未保存確認（保存／破棄／キャンセル）を通してから開く。保存形式、nativeの正本、GLB/VRM取込経路は変更していない。

Windows Player `Builds/ProjectPickerV1/NyaForge.exe` のAuthoring suite **PASS**（report `Artifacts/Authoring-20260913-063116-c9ba956e8e8d436cb14271bb46729ed5/report.json`、800x600）。GUI control存在を回帰し、既存の取込・頂点編集・Undo/Redo・native Save/Open・GLB出力・MCP・PhysBones表示を含む全チェックを通過した。Unity Bridge **PASS**（Unity 2022.3.22f1、report `Artifacts/BridgeReceiver-20260913-063147-237-c179a2dc469943feb5969a33dbcad0da/bridge-report.json`）。Core **454 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0611989018b34eb2863ba4e713f3a35c`）。自動検証はExplorerの実クリックやDPI差、実VRChat内受入を代替しない。

## 2026-09-13 attachment GUI verification

装着ノードの制作導線を、GUIの実コントロールまで検証できるようにした。Authoring suiteは対象`object-attachment` foldout、対象graph objectのDropdown、stable BoneId Dropdownの存在を確認し、解決済みtarget/BoneIdが選択肢へ表示されることを回帰する。既存のroot pose追従、native Save/Open、未解決target拒否、標準GLBのmetadata loss guardも同じPlayerで継続確認した。

Core **454 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-fbf4a508696c4b35b50231014cc07f6a`）。Windows Player `Builds/AttachmentV9/NyaForge.exe` のAuthoring suite **PASS**（report `Artifacts/Authoring-20260913-062610-55a5b5cacad344b98de2791138163e89/report.json`、800x600）。GUIがstable IDを表示することを確認したが、実マウス/DPI差・実アバターの自動fit/貫通・実VRChat SDK受け入れは別境界として残る。

## 2026-09-13 GLB attachment loss guard

標準GLBにはNyaForgeのobject attachmentを表す共通フィールドがないため、装着ノードを含む作品のGLB出力を続行するとBoneId・target object・offsetが失われる。`GlbExportService`は装着ノードを検出した時点で`GLB_ATTACHMENT_METADATA_UNSUPPORTED`を返し、出力先を作らずnative project exportを案内する。single/multi-objectのnative routingと未解決target拒否は前カードの契約を維持する。

Core **454 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-2e1aa8cd7b834460b53cb64a49bef8ee`）。Windows Player `Builds/AttachmentV8/NyaForge.exe` のAuthoring suiteもPASS（report `Artifacts/Authoring-20260913-062245-9cb8fb0762e84c24bb7b9e6eb9780e30/report.json`）。装着ノードを黙ってGLBへ落とさないこと、native packageへ保存することを回帰した。
## 2026-09-13 attachment export routing

監査で、装着ノードだけを含むgraphが通常のMesh/Surface Bakeへ誤ルーティングされると、GLB等の標準交換形式では表現できない装着メタデータが失われる経路を確認した。`ProjectExportService`が`object.attachment`を含むgraphをfeature-preserving native projectへ送るよう修正し、再読込後のtarget object・stable BoneId・offsetを回帰した。

Core **453 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-65771e9def9948ddb08ca63778a6ebd9`）。Windows Player `Builds/AttachmentV7/NyaForge.exe` のAuthoring suiteもPASS（report `Artifacts/Authoring-20260913-061851-56d32d40d3d84ddba7fb269cfa44f67d/report.json`）。avatarのPose変更後も小物rootが追従し、複数object attachmentのnative exportと未解決target拒否を検証した。標準GLBはattachment metadataを表現しないため、小物の位置情報を保持したい場合はnative project exportを使用する。この境界をGUI/MCPの出力案内へ明示した。
## 2026-09-13 object.attachment follow-up

前回のボーンフィードバックを受け、チョーカーなどの小物を明示したavatarのstable `BoneId`へ装着する経路を実装した。graphへ`object.attachment`ノードを追加し、`targetObjectId`、`boneId`、`skeletonHash`、bone-local rest offset（メートル）を型付き・ハッシュ付きで保存する。GUIの「小物をボーンへ装着」は対象graphとBoneIdを選択して設定でき、名前推測・自動fit・貫通修正は行わない。MCPの`graph.node.add/update`でも同じpayloadを使える。

Workbenchは対象avatarのimported rig sessionをstable hashとmappingで検証し、pose評価から小物rootの位置・回転を再投影する。active/inactive graph objectの表示投影、最終結果投影、Undo対象の追加/更新/解除、native schema 4 Save/Openのrig session復元まで接続した。装着poseをmesh projectionのcache判定へ含め、pose変更時に古いrootを再利用しないようにした。

検証結果: Core **451 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b89a243dbb924b979d94173ee1f2bb11`）、Windows Player `Builds/AttachmentV3/NyaForge.exe` のAuthoring suite **PASS**（report `Artifacts/Authoring-20260913-060504-29017a8a29be484ab5e6c6540278b84a/report.json`）、MCP transport **3/3 PASS**。Playerではstable BoneId/root pose、Save/Open、rig-session restoreを含む全チェックを確認した。

この検証は合成skeletonとNyaForge内の表示・保存経路が対象で、実VRChat SDK、実アバターでの自動fit/貫通、VRChat内の見た目受入は未完了。次は実SDK受け取りと実アバターでの装着確認を別証拠として進める。
# NyaForge 開発タスク

## 2026-09-13 consistency review follow-up

提示されたレビュー指摘を現行 `main`（`dc31fa3`）へ再照合した。P1の4件（16bit `JOINTS_n` の幅、source skin後の下流編集、PhysBones packageのsource asset、選択mesh単位のskin判定）は、既存の回帰テストと実装で閉じた状態を維持している。追加の再検証では Core **448 passed / 0 failed**（artifact: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4bbb7b6347fd456ead93289028b5571a`）。`extensionsRequired` の未対応拡張を取込前に `UNSUPPORTED_EXTENSION` で停止するガード、材質slotの局所リマップ、実RadDollV3のGLB往復も現行mainに含まれる。

残る受入境界は、実VRChat SDKを導入したPhysBones component生成・更新、実アバター／VRChat内の動作、任意GLBの複数mesh・共有参照・完全材質／animation保持である。合成UnityBridgeやPlayer自動検証の合格を、これらの実環境受入へ読み替えない。次の実装単位は対象SDKの版・完全修飾型を固定したSIM-02B実SDK受け取りで、SDKが無いpublic buildの起動・保存・診断は引き続き維持する。

MCP transportも再ビルドして確認した。新しい`forge_export_glb`を含む19 toolのregistry、named pipeのinstance検証、captureのcamera metadata保持とPNG bytes非重複が **PASS**。古い18 tool期待値をテスト側で更新した。

更新: 2026-09-13。標準GLB出力のStaticGeometry／SkinnedGeometry profileとGUIボタンを追加し、normal/tangent morph、容量拡張、全weight保持の拡張GLBを含むCore448件合格。レビュー再照合後の最新HEADは`dc31fa3`で、P1/P2の回帰を再実行済み。次の実装カードは、実SDK受け取りの版固定、複数mesh/共有参照の明示設計、チョーカー小物のbone固定メタデータとプレビュー追従である。提示された2026-09-13 consistency review（基準 `1c76e4a`）を現行mainへ照合し、4件のP1（16bit JOINTS、source skin後の下流編集、PhysBones source asset、選択mesh単位のskin判定）は実装・回帰済みとして [review receipt](docs/reviews/2026-09-13-Consistency-Review.md) に固定した。最新Core artifactは `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4bbb7b6347fd456ead93289028b5571a`。MCPのinline captureはPNG base64だけをImageContentBlockへ渡し、structured/text側には画像ごとのcamera・撮影条件・hashを残すよう修正した。PhysBones再適用時の削除curve初期化と、chain配列順に依存しないmanaged marker再利用（stable target/name/root優先）も追加した。I04-Aの完全source保存/GUI接続、mesh属性/POSITION/NORMAL/TANGENT morph変換、source slot weight/一般bind deformer、GLB全JOINTS_n/WEIGHTS_n候補読取、native weight packageとrig session v5・GUI接続、評価済みgraph meshへのsource skin adapter、authored poseからのsource palette生成、Workbench取込後/揺れ再生中のsource skin表示接続まで追加。複数graph objectのsource rig sessionをgraph IDで束ねる`imported-rig-sessions.nyaforge.bin` attachmentとactive object連動表示も追加。I04-Bの入口として複数mesh/instance/skin参照を元indexで保持するGLB scene inventory、指定mesh/skinを選ぶ静的・skinned候補読取、Workbenchの候補確認・選択GUIを追加した。今回、native documentへ複数objectとactive objectを追加し、`object.select`、graph objectの追加、Save/Open、Workbench対象切替、既存1object互換を実装した。GLB取込は既存graph projectへ新しいgraph objectとして追加し、取り込んだobjectをactiveにする。さらに非active objectを読み取り専用の背面として同じviewportへ表示し、表示切替と全対象Frameを追加した。複数objectを個別に読み戻せる`multi-object.nyaforge-bake.json`パッケージ出力をGUI/MCPへ接続した。SIM-01の共通secondary-motion契約（安定ID、固定頂点、collider、出力種別、adapter能力、unknown version保持付きNYSM v1 codec）とVRM1 resolved spring migration、SIM-02のPhysBones target DTO／NYPP v1 codec／loss report境界／schema 4 attachment保存／Workbench状態表示、PhysBones target package、UnityBridgeの管理対象限定writer／reflection backend／合成receiver検証、receiverの明示stable binding保存を追加した。SIM-03Aとして揺れプレビューのGUI/MCPライフサイクル（play/pause/reset/rebuild/fixed-step/state）を共通ownerと外部MCP transportへ接続し、SIM-03Bとして固定step連続PNG・hash・実行条件のrun記録と外部MCP経路を追加した。今回はGLB primitiveの材質slotと基本PBR係数（linear baseColor、metallic/roughness、emissive、alpha）をnative `StandardMaterial`／`AssignMaterials`へ接続し、埋め込みbase colorのPNG/JPEGをnative Paintノードとして保存・表示できるようにした。外部texture/image/sampler等の未保持診断は残した。複数objectのmesh結合、共有mesh/skin/morph参照は未完了。同一skeleton／poseを共有する複数SkinDeformの同時評価は回帰済み。P1レビュー対応として16bit JOINTS読取、選択mesh単位のskin判定、source skin後の全graph編集保持、PhysBones source asset同梱を実装した。さらにrig/morphを含むgraphの`project.nyaforge.json` feature-preserving exportを追加し、静的Bakeとの出力境界を明示した。直近Core448件合格。Windows Playerの埋め込み材質画像接続を含むbuildとAuthoring suiteも合格した。実RadDollV3 VRMでWindows Playerの取込→EditMesh頂点編集→native Save/Open→標準SkinnedGeometry GLB出力を一周し、Core426件とUnity Bridgeも合格した。高密度PaintのWindows Playerで30 frameの実間隔、min/average/p95/max、GC回数、managed heap、Unity allocated/reserved bytes、targetFrameRate/vSync/render間隔/GPU名を `dense-paint-profile.json` へ保存した。合成fixtureでは平均・p95・最大約66.67ms、250ms観測境界超過なし。実RadDollV3 VRMでは埋め込みbase-color画像の上限超過を診断付きで省略し、sRGB端点をクランプ、複数material slotの局所頂点リマップを通して取込→EditMesh→Save/Open→標準skinned GLB→再取込を合格した（Player `Builds/RealModelMaterialClampV4/NyaForge.exe`、report `Artifacts/Authoring-20260913-045250-aab62f6896e0479b93da4a562d8573eb/report.json`、Core446）。任意モデル全般、実操作、性能、実VRChat受入は未完了。

### 直近の実装カード（2026-09-13）

- **T07 / 小物テンプレートと複数対象出力**: `PolygonPrimitives.Choker` に低ポリ輪形状（24×8、192頂点・192 quad、UV/法線/接線付き）を追加し、制作画面の **チョーカー形状を追加** から空projectまたは既存graph projectへgraph objectとして追加できるようにした。頂点編集、Undo対象の文書revision、Save/Open、複数対象Bakeを同じcommand経路で確認。Player `Builds/ChokerTemplate3/NyaForge.exe` の800x600 suite（`Artifacts/Authoring-20260913-054143-72427e21d3784698b20b2456ac133dba/report.json`）とUnity 2022.3.22f1 Bridge（`Artifacts/BridgeReceiver-20260913-054215-808-287af6dc0f3f438b9192877d94fe8d7b/bridge-report.json`）がPASS、Core **448 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-5aa6b693725845e099538e7e6d4f7ced`）。複数対象Bakeのobjectフォルダ名を連番だけへ短縮し、Windowsの長いUUID階層でatomic blob writeが失敗する経路も修正した。実アバターへの自動fit・実マウス受入・VRChat内の見た目は別境界として残る。

- **T06 / 公開GLB取込スモーク**: privateアバターに依存せずWindows取込経路を再現できる `Tools/New-NyaForgeGlbFixture.ps1` を追加した。静的mesh 0と2骨skinned mesh 1、PBR赤材質を含む小さなGLBを生成し、`Tools/Test-NyaForgeAuthoring.ps1 -ImportModel` で候補確認→mesh 1取込→EditMesh頂点編集→native Save/Open→標準skinned GLB出力→再取込を実行。Player `Builds/UiNarrowStatusV2/NyaForge.exe` の800x600 report `Artifacts/Authoring-20260913-052931-1a491563b9374f8dbbf19fd14d4298b5/report.json` がPASS、Unity 2022.3.22f1 Bridgeも `Artifacts/BridgeReceiver-20260913-053019-067-4cc5e17e55284e64b2364188415e7b09/bridge-report.json` でPASS。fixture生成後にGLB宣言長・JSON chunk・mesh/skin属性を検証済み。実マウス操作、任意実素材、実VRChat受入は別境界として残る。

- **T05 / Windows UI可読性**: 制作対象の長いGUIDは先頭8文字をボタンへ表示し、完全なobject identityはツールチップへ残して横方向の文字欠けを避けた。ステータス欄を狭い画面でも1行に固定し、表示更新でviewportが縮む問題を防いだ。ステータス全文もtooltipへ保持する。Player `Builds/UiNarrowStatusV2/NyaForge.exe` の800x600 Authoring suite（report `Artifacts/Authoring-20260913-052112-6c8f4cfef1e1407c9e807eaa5ba711eb/report.json`）と1600x1000 suite（report `Artifacts/Authoring-20260913-052214-0691af25c0ad410183470d1c5c8fb86e/report.json`）が合格。Unity 2022.3.22f1 BridgeもV1で合格（`Artifacts/BridgeReceiver-20260913-051636-908-559437b2ca5d48779a1f89f38eadcc7d/bridge-report.json`）。実マウス・DPI差・実VRChat受入は別境界として残る。

- **I04-E / 必須拡張ガード**: 完全なadapterがない `extensionsRequired` はGLB/VRM取込前に `UNSUPPORTED_EXTENSION` で拒否し、`extensionsUsed` は従来どおり partial 診断として保持する。Core 446件、Windows Authoring suite（Player `Builds/RequiredExtensionGuardV1/NyaForge.exe`、report `Artifacts/Authoring-20260913-045811-6f323347842b4d548e95a60aebd4700b/report.json`）合格。

- **I04-E / 実モデル材質診断と複数slot GLB往復**: 埋め込みbase-color画像は1024x1024以内だけnative Paintへ保持し、超過/不正形式は明示warningで省略する。sRGB係数は0〜1へクランプし、skinned GLBの複数material slotはスロットごとの局所頂点アクセサへ分割して再取込時の重複計上を防ぐ。Core 446件、RadDollV3 Windows Player smoke（取込→編集→保存/Open→標準skinned GLB→再取込）合格。

- **I04-C / GLB予算分離**: GLB/VRMの取込・標準GLB出力は128 MiB、1 mesh 200,000頂点までを専用予算で検査する。native blobの16 MiB予算は維持し、実素材を通すための拡張を他形式へ波及させない。
- **I04-C / 編集済みskin出力**: `SkinnedGeometry` profileは単一graphのrest pose・identity transform・4 influenceを対象に、`SkinnedGeometryExtended` profileは最大32 influenceを全JOINTS_n/WEIGHTS_n setで保持し、どちらもトポロジー不変の`EditMesh`頂点編集をGLBへ出力する。任意pose、非ゼロmorph変形、未対応nodeは二重適用を避けて拒否する。
- **I04-E / 取込診断**: `ImportedMeshSource.Diagnostics` / `ImportedSkinnedMeshSource.Diagnostics` に材質、アニメーション、extensionsRequired、extensionsUsedをコード付きで保持し、既存Warningsにも同じコードを表示する。`import-diagnostics.nyaforge.json` attachmentへgraphId単位で保存し、MCP graph inspection・取込後status・GUI詳細パネルへ公開する経路まで実装済み。Core 441件とWindows Playerでsnapshot往復、active graph表示、blocking/partialの区別を回帰した。依存資源を含む完全な材質・animation・未知拡張の保存は未完了。
- **I04-E / GLB材質出力往復**: 標準GLBのStatic/Skinned/Extended writerへ評価済みStandardMaterialと埋め込みbase-color画像を接続し、material slotがある場合はprimitiveを分離してmaterial indexを保持する。画像は決定的PNGとしてBIN bufferViewへ埋め込み、linear PBR係数をglTFのsRGB factorへ戻す。Core 446件でwriter→readerのPBR/embedded image往復を確認。外部画像・追加texture map・sampler・animation・VRM拡張は未対応。
  MCPの `forge_export_glb`（static / skinned / skinned_extended）を追加し、saveTarget配下への出力、revision固定、既存出力先の再実行拒否、文書state不変を外部sidecar経由で確認。Windows Player `Builds/McpGlbExportV3/NyaForge.exe`、Authoring suite PASS、MCP transport build PASS。
- **I04-E / 基本PBR材質ルーティング**: GLB primitiveのmaterial indexを選択mesh単位で検証し、baseColorFactor（sRGB→linear）、metallic/roughness、emissive、alpha mode/cutoffを`GlbMaterialSource`として保持する。Workbenchの静的／skinned取込で全submesh slotが揃う場合、`StandardMaterial`／`AssignMaterials`へ接続する。埋め込みbase colorのPNG/JPEGはUnityでRGBAへ変換し、未バインドのnative Paintノードとして保存・表示する。未バインド画像は誤編集を避けてGUI表示専用とし、texture/image/samplerの未解決参照・追加拡張は診断を残す。Core 446件、Windows Player `Builds/EmbeddedMaterialImageV2/NyaForge.exe` build、Authoring suite 74 checksで回帰済み。依存資源込みの完全材質往復は未完了。
- **I04-C / 拡張GLB回帰**: `GlbExportService.ExportSkinnedExtended`とWorkbenchの「拡張GLB（全weight保持）」ボタンを追加。8 influence fixtureをGLB writer→`GlbSkinImporter`で往復し、全weight set・骨数・正規化合計を確認した。Windows Player `Builds/ExtendedGlbV1/NyaForge.exe` と Authoring suiteもPASS。
- **保存往復回帰**: serialized skin bindingを再正規化せずfloat32値を保持するよう修正し、4 influenceのbyte identity回帰を追加。実RadDollV3 `RadDollV3_VRM.vrm`（private ZIP内、45,341,584 bytes、body 129,348 vertices、171 bones）を読み取り専用に選択取込し、ProjectStore Save/Open後のdocument state hash一致を確認した。private素材はpublic repoへ同梱しない。
- **active object編集経路**: Workbenchのグラフ、表示投影、材質、paint、rig、morph、UV、MCPを`Document.ActiveObject`基準へ統一し、複数対象で切替後の頂点編集が非active graphを変更しないPlayer回帰を追加した。非active objectは従来どおり読み取り専用backdropとして扱う。
- **GLB skin編集導線**: 選択したGLB/VRM skin graphへrest-space `EditMesh`を自動挿入し、取込直後に既存の頂点選択・移動・Undo/保存経路へ入れるようにした。FBX/BLEND直接取込、humanoid自動配置、任意poseを編集段へ焼き込む処理は対象外。
- **T05 / 実モデル一周スモーク**: Tools/Test-NyaForgeAuthoring.ps1 -ImportModel の任意GLB/VRM検証入口を追加。private tempのRadDollV3 VRM（mesh 1 / skin 1、129,348 vertices、171 bones、35 morphs）で候補確認、EditMesh頂点編集、native Save/Open、標準skinned GLB出力を確認した。実マウス・実VRChat受入とは分けて記録する。

## 開発の入口

今回のSIM-02B実装では、receiver GUIの保存・読込・適用前に`PhysBonesBindingValidator`を通す。必要stable BoneId／collider groupの欠落、avatar root外、重複Transform、空groupを共通診断し、vendor SDKの型判断はbackendへ委譲する。
target package manifestには受け取り側の完全修飾`ComponentTypeName`を保存し、receiverはその型を解決する。旧manifestだけ既定のVRChat PhysBone型へフォールバックする。
reflection型解決は`VrcPhysBonesReflectionResolver`へ分離し、assembly-qualified nameの厳密解決、未導入、非Component、曖昧な複数assemblyを非破壊診断する。

- **標準skin出力の安全境界**: 4 influenceを超える入力をGLBへ黙って切り捨てないよう、`GLB_SKIN_INFLUENCES`で明示拒否する。native出力は高影響数を保持する経路として残す。
作業先: `Z:/TextureVoice_local/git/NyaForge`。Windows先行、macOSは将来。
読む順: このファイル → [モデル交換仕様](docs/Model-Interchange-Spec.md) → [実素材調査](docs/Real-Asset-Import-Plan.md) → 対象コード。製品全体の範囲は [開発計画](docs/Development-Plan.md) と [設計v2](docs/NyaForge-Authoring-Design2.md) を参照。[文書一覧](docs/README.md)参照。
製品目標は小物の制作・出力を一周し、低ポリ全身キャラ、品質向上へ進むこと。設計v2は製品方針、v1は背景資料。設計中の外部依存・機能は採用済みや実装済みを意味しない。

## 次に実行するタスク

採用した方針: **nativeを制作の正本、GLB/VRMを交換形式、FBX/BLEND/Unityを原本として区別する。** 未対応データを黙って削らず、情報ごとの能力と保持結果を報告する。Blender調査は任意の開発ツールで、標準制作の必須依存へ変更しない。詳細・完了条件は [モデル交換仕様](docs/Model-Interchange-Spec.md)。ボーンのフィードバックは下記のSIM-02B→SIM-07Aを主経路にし、完了済みSIM-03A/03BのGUI/MCPライフサイクルと固定step証拠を共通土台として使う。SIM-04〜06は主経路を遅らせない任意評価へ分離する。

| 状態 / ID | 実行する作業 | 完了条件・依存 |
|---|---|---|
| [ ] I04-A / P1 **継続** | source local/world、一般TRS/matrix、inverse-bind、法線/接線変換の基盤 | 数値/reader/codec/GUI生成/native原本なしOpen、mesh/POSITION/NORMAL/TANGENT morph変換、SourceSkinBinding/SourceSkinDeformer、GLB全JOINTS_n/WEIGHTS_n候補、NYSPとrig v5/native/GUI接続、評価済みGraphMeshValueへのSourceSkinGraphAdapter、authored poseからのSourceSkinPosePalette、Workbench取込後/揺れ再生中の自動表示を追加済み。同一skeleton hashの複数graph objectをshared skinでSkinnedGeometry／Extended GLBへ出力する回帰を追加。次は異なるskeletonの明示的な出力境界、再利用mesh/objectと失敗時原子性を検証。sparse weightは未対応。float weightとnormalized UBYTE/USHORT weightを読取可能 |
| [ ] I04-B / P1 **継続** | 複数mesh/instance/skin、source→制作ID対応 | `GlbSceneInventoryReader`でmesh/primitive数、node instance、skin joint参照、node world transformを元indexのまま候補化し、候補確認・node instance選択GUIを追加した。native documentは最大64 objectのactive object方式へ拡張し、`object.select`、graph object追加、Save/Open、Workbench対象切替を検証済み。非active objectは読み取り専用の背面表示とFrame対象にでき、複数object package出力も追加した。GLB取込はgraph projectへ新objectとして追加する。複数graph objectのrig sessionをgraph IDで保存・active objectへ再選択する経路を追加済み。同一skeleton hashの複数skinned meshはshared skin出力へ対応。残りは異なるskeletonの結合、同名morph/共有mesh・skin参照と実素材受入。 |
| [ ] I04-C / P1 **継続** | rig/weight/morph容量とcodec/hash/表示/出力 | nativeは512骨・32 influence・512 morphへ拡張し、257骨・18weight・単一mesh262morphの削減なし往復、GLB全JOINTS_n/WEIGHTS_n取込、`SkinnedGeometryExtended`出力を回帰済み。標準SkinnedGeometryは互換上4 influenceを明示拒否する。実GLB受取先・VRChat側確認が残る |
| [ ] I04-D / P1 | 標準FBX Bridge入力と任意の変換adapter | Blender必須化なし。依存検出・変換前後比較・原本保護・失敗/取消を確認。実取込はA〜Cに依存 |
| [ ] I04-E / P1 **継続** | 機能report、材質/animation/VRM意味情報/未知拡張の保持とGUI/MCP | GLB importerのコード付きdiagnosticsをnative attachmentへ保存し、MCP graph inspection・取込後status・GUI詳細パネルで表示。未実装の `extensionsRequired` は取込前拒否済み。依存資源込みopaque保持、既知VRM内の未保持field、完全材質/animation保持が残件 |
| [ ] T04 / P2 **継続** | 時間超過・性能受入 | 高密度Paint合成fixtureでframe時間（30 samples、p95/max）、GC/managed heap/Unity allocatorを `dense-paint-profile.json` へ保存し、250ms観測境界を記録した。次は実アバター比較、長時間working-set、停止ポリシーを別試験で定める |
| [ ] T05 / P1 **継続** | Windows実素材・実操作と受取側 | RadDollV3 VRMで取込→EditMesh→Save/Open→標準skinned GLB→再取込を合格（`RealModelMaterialClampV4`）。残りは実マウス・DPI/文字欠け・pose/揺れ・実VRChatの出力受取確認、追加texture mapと大画像の完全保持 |
| [ ] T06 / P2 | 外部MCP metadata保存受入 | 内部handlerと区別し、transport経由で保存/Open・失敗保護・再試行を確認 |

## 完了した前提と残る境界

### ボーンの追加フィードバック: 実行タスク（2026-09-12）

[揺れ・布adapter計画](docs/Secondary-Motion-Plan.md)へ採用方針・依存・完了条件を整理した。**PhysBones優先、MagicaCloth2は任意adapter**。SIM-01のUnity非依存契約とcodec、SIM-02AのPhysBones target DTO／loss report境界、schema 4 attachment保存、Workbench状態表示、target package、UnityBridgeの管理対象限定writerとreflection backend、受け取り側の明示stable binding保存を実装した。実VRChat SDK／アバター動作は未確認で、直近はSIM-02Bの実SDK受け取り側を先に進める。

今回の判断を次の一本の流れで固定する。**native制作データ → 版付きtarget profile → 交換可能なsimulation adapter → GUI/MCPの共通実行所有者 → 固定stepの証拠 → 出力先ごとの受入**。PhysBonesはVRChat向けのP1経路として扱い、MagicaCloth2はUnityアプリ用の任意評価へ隔離する。MeshClothはBlendShapeとの重複と負荷を確認してからC3へ進める。vendor componentや独自scriptをVRChat出力へ持ち込まず、未対応項目はloss reportで停止・表示する。

### フィードバックから分解した実装カード

- **SIM-02B / 実SDK受け取り**: SDKの版と完全修飾型を先に固定する。target packageのmanifest/profile/skeletonを読み、stable BoneIdとcollider groupを手動割当できる状態から、managed componentだけを生成・更新する。書込み前にunsupported/warningを検査し、失敗時は原子 rollback。SDKが無いpublic buildは起動・保存・診断を維持する。
- **SIM-03B / 連続撮影と証拠**: 1 run に `input/config hash`、adapter・package版、target、Unity/build、fixed step・warmup、pose/root/collider条件、各frameの画像hash、失敗ログを束ねる。撮影はtransient previewだけを使い、native revision・保存ファイル・制作姿勢を変更しない。実VRChat受入の証拠と混ぜない。
- **SIM-07A / PhysBones受入**: 同じ髪束fixtureでroot移動、停止、旋回、pose、colliderを確認し、NyaForge preview、受取Unity、VRChat内を別々の結果として記録する。preview画像だけでVRChatの再現を判定しない。
- **SIM-04〜06 / 任意評価**: MagicaCloth2のBoneCloth→MeshCloth→BoneSpringを独立adapterとして順に評価する。vendor packageはpublic repoへ同梱せず、runtime構築待ち・固定根・衝突・破棄・性能・morph境界を小さな髪束/布で確認する。PhysBonesのP1経路を置換しない。

| 状態 / ID | 作業 | 完了条件・依存 |
|---|---|---|
| [x] SIM-01A / P1 | 共通secondary-motion契約 | `SecondaryMotionAsset`、stable chain、fixed vertex、collider group、bone/mesh output、adapter capability、`NYSM` v1 codec、VRM1 resolved spring migrationを実装し、unknown versionとstale skeleton/topologyを安全に扱う |
| [x] SIM-01B / P1 | native attachmentとVRM0移行 | `secondary-motion.nyaforge.bin` として `NYSM` をschema 4へ接続し、VRM0 source-node subtree→stable BoneId移行、未知wire版の保持・GUI表示、保存後のskeleton/topology stale診断、明示的なbone/vertex rebind API、同一BoneIdのGUI再bindまで実装。残りは実素材確認。SIM-03Aの前提 |
| [x] SIM-01B-R / P1 | stale時の明示rebind操作 | `SecondaryMotionRebind.Apply`でbone/vertex対応表を必須化し、Workbenchは同一BoneIdの安全な場合だけボタンを有効化。リグのPose/Binding再bindと二次運動attachment再bindを分離し、推測による自動対応をしない |
| [x] SIM-02A / P1 | PhysBones target packageと合成Bridge | `PhysBonesTargetProfile`／`PhysBonesChain`、`NYPP` v1、schema 4 attachment、loss report、target package、reflection writer、managed-only、branch preflight、receiver Windowを追加し合成fixtureで検証済み |
| [x] SIM-02B-1 / P1 | receiver binding事前検証 | `PhysBonesBindingValidator`をGUIの保存／読込／適用前へ接続し、必須stable BoneId／collider group、avatar root配下、重複Transform、空groupを共通診断。vendor SDKの型判断はbackendへ委譲 |
| [x] SIM-02B-2 / P1 | receiver component型の固定 | target package manifestへ完全修飾`ComponentTypeName`を保存し、receiverのreflection resolverへ渡す。旧manifestは既定型へフォールバックし、custom型と旧形式をCore／Bridgeで往復確認 |
| [x] SIM-02B-3 / P1 | receiver型解決診断 | `VrcPhysBonesReflectionResolver`を分離し、assembly-qualified型、未導入、非Componentの診断をscene変更なしで確認。GUIの適用失敗へ理由を返す |
| [x] SIM-02B-4 / P1 | receiver事前診断 | `PhysBonesBridge.Inspect`／`InspectPackage`を追加し、本適用と同じtarget／capability／scene／managed preflightをcomponent生成なしで実行。GUIの「事前診断（書き込みなし）」へ接続 |
| [ ] SIM-02B / P1 **次に実行** | 実SDK受け取り側 | `NyaForgePhysBonesBinding`へpackage identity付きのstable BoneId／collider group手動割当を保存・読込できるようにした。reflection member catalogは継承元private field/propertyも対象にする。次は対象SDKの版・型を固定し、package読込→実component生成・更新を実SDKで確認。未対応項目は書込み前にloss reportで停止し、未管理componentを変更しない。SDK未導入時のpublic buildは維持 |
| [x] SIM-03A / P1 | GUI/MCPの設定と再生所有者 | GUI・内部MCP handler・外部sidecar toolにplay/pause/reset/rebuild/fixed-step/stateを接続し、同じtransient owner／generationで保存対象外、編集・作品切替時の破棄を確認済み。非同期vendor構築の実SDK接続は後続タスク |
| [x] SIM-03B / P1 | 連続撮影とbackend証拠 | `SecondaryMotionCaptureRecord`／codec、固定1/60秒・warmup・最大8frame・pixel budget、input/config hash、adapter／package版、target、Unity/build、pose/root/collider条件、各PNG hashと失敗statusを1 runへ束ね、`forge_secondary_motion_capture`の実MCPで3frameを確認済み。native revision／保存／制作姿勢は不変。実VRChat受入の証拠とは分ける |
| [ ] SIM-04 / P2・任意 | MagicaCloth2 BoneCloth最小評価 | vendor packageをpublic repoへ入れず任意assemblyへ隔離。自作髪束1本でruntime生成・構築完了待ち・固定根・sphere衝突・rebuild/reset/破棄・写真列を確認し、未導入buildも成功させる。SIM-02B/03Aを置換しない |
| [ ] SIM-05 / P2 | MeshClothとmorph境界 | 小さい布で固定／可動領域を指定し、BlendShape変形頂点との重複を拒否または分離案内する。法線更新と時間／GC／メモリをSIM-04と比較 |
| [ ] SIM-06 / P2 | BoneSpringとUnityアプリ向け出力 | BoneSpring fixtureの保存・再構築と、Magica用profileの依存不足診断・版照合・再出力を確認 |
| [ ] SIM-07A / P1 | PhysBones target受入 | SIM-02B＋SIM-03A/B後。同じ髪束でroot移動・停止・旋回・pose・colliderを受入し、対応SDK／受取Unity／VRChat内の結果を別々に記録 |

SIM-02Bのbinding validationは実装済み。次は対象SDKの版・完全修飾型を固定し、実componentの生成／更新を同じ明示mappingで確認する。

実行順は **SIM-01B残り → SIM-02B → SIM-07A**（SIM-03A/03Bは完了）。SIM-01BはI04-Aと並行し、SIM-04〜06は主経路の受入を置換しない。C2の必須は選んだ出力先で髪束1本が動くこと。Magica導入時の購入・vendorソース取得／配布はこのタスク化では実行せず、public repoには自作adapter・fixture・設定schemaだけを置く。

### 既存工程

- [x] **T01**: 全source node階層と元children順をrig session v3へ保存。v1/v2は階層不明を維持。
- [x] **T02**: VRM0 subtree/一時骨格/実行所有者/共通再生GUIを対応profileで接続。両形式のPlayer handler往復合格。
- [x] **T03**: ローカルFBX3件を読取調査。アバター20mesh、全3件257骨、アバター最大18deform bone影響/頂点・単一mesh262morphを確認。生データはprivate。変換・実importの成功はまだ確認していない。
- [x] **R01〜R12 / I03-A**: 記録した自動検証範囲で修正・adapter接続済み。[Rig/VRMレビュー](docs/reviews/2026-09-12-Rig-Vrm-Review.md)、[R11/R12](docs/reviews/2026-09-12-Current-Checkpoint.md)、[再生checkpoint](docs/reviews/2026-09-12-Playback-Checkpoint.md)。
- [ ] **I03-B/C / A01**: 可動world root、揺れるnodeのcollider更新順と参照runtime比較、実素材/実操作/性能の受入は残る。I03-CはVRM0/1両方のGUI/handler接続済みだが、受入全体は未完了。
- [ ] **I04 / C0〜C5**: 任意モデル取込と制作/出力の製品全体は未完了。skin/morph出力・受取側確認などを [開発計画](docs/Development-Plan.md) から省かない。

## 直近の証拠


- GLB normalized weight Player regression: Windows Player **PASS**（build `Logs/build-all-20260913-012119-799.log`、`Builds/Windows-NormalizedWeights/NyaForge.exe`）、Authoring **PASS**（`Artifacts/Authoring-20260913-012142-6ed7e6ab7d84430196e7863c51d2b4ca/report.json`）、Unity Bridge **PASS**（`Artifacts/BridgeReceiver-20260913-012215-147-4bb7328c3ab4483db3e449a60a747220/bridge-report.json`）。既存のVRM0/1・GUI・保存/出力回帰を維持した。
- GLB weight validation Player regression: Windows Player **PASS**（build `Logs/build-all-20260913-012647-017.log`、`Builds/Windows-WeightValidation/NyaForge.exe`）、Authoring **PASS**（`Artifacts/Authoring-20260913-012705-60078ac2fcfe4a04b05ba8764911e9ac/report.json`）、Unity Bridge **PASS**（`Artifacts/BridgeReceiver-20260913-012737-339-72f1beb1a02c46acb2ec5d241329e959/bridge-report.json`）。既存のGLB/VRM import failure isolationを維持した。
- GLB normalized weight regression: Core **425 passed / 0 failed**（`Logs/core-weight-validation-20260913.txt`、artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0431fca3ee01454eba28285e93dbfcf7`）。source skin importerとskin importerでnormalized UBYTE weightをfloatへ復元し、既存のfloat形式と同じnormalized bindingになることを確認。
- GLB/VRM実素材・標準GLB回帰: Core **423 passed / 0 failed**（`dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore`、artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-98124952dc914a0c9ce5aeb112e131ac`）。16bit JOINTS、選択mesh単位のskin判定、編集済みrest-pose skin出力、4 influence serialized bindingのbyte identityを確認。privateのRadDollV3 VRMを読み取り専用に監査し、全10 mesh/skin pair（body 129,348 vertices、171 bones）を取込、ProjectStore Save/Open後のstate hash一致を確認した。private素材はpublic repoへ同梱していない。
- 実RadDollV3 Playerスモーク: Windows Player **PASS**（build `Logs/build-all-20260913-011120-036.log`、`Builds/Windows-RealModelSmoke7/NyaForge.exe`）、Authoring **PASS**（`Artifacts/Authoring-20260913-011139-6d930c47c0c34967b92a95aa5df83ff3/report.json`）。private tempの`RadDollV3_VRM.vrm`を読み取り専用に使用し、mesh 1 / skin 1（129,348 vertices、171 bones、35 morphs）で候補確認、rest-space EditMeshの頂点編集、native Save/Open後のhash一致、標準skinned GLB出力を確認した。Unity Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-011244-686-8672912d9a884e5bbb10e72bcfa51eda/bridge-report.json`）。実マウス・実VRChat内動作・private素材の公開を意味しない。
- Active-object編集回帰: Windows Player **PASS**（`Logs/build-all-20260913-004333-596.log`、`Builds/Windows-ActiveObject1/NyaForge.exe`）、Authoring **PASS**（`Artifacts/Authoring-20260913-004356-d55db89466a9437db514dadad265b285/report.json`）。2つのgraph objectをSave/Openし、active objectを切り替えて頂点編集した結果、対象以外のgraph hashが不変であること、backdrop表示切替・Frameが維持されることを確認。Unity Bridge **PASS**（`Artifacts/BridgeReceiver-20260913-004426-425-63f6bf9efce640a09bb50dc67064bc3d/bridge-report.json`）。
- GLB skin取込→頂点編集回帰: Windows Player build **PASS**（`Logs/build-all-20260913-004702-147.log`、`Builds/Windows-ImportedEdit1/NyaForge.exe`）、Authoring **PASS**（`Artifacts/Authoring-20260913-004723-8b656f46809f4b31993f0cbd6aa5700f/report.json`）、Unity Bridge **PASS**（`Artifacts/BridgeReceiver-20260913-004759-056-1a634ad110c04d088a00d8ecb69a978a/bridge-report.json`）。選択mesh/skin取込時のEditMesh自動生成、頂点移動によるgraph output hash変化、既存候補・複数rig session回帰を確認。実RadDollV3のGUI目視受入ではない。
- Windows Player **PASS**: `Logs/build-all-20260913-003858-381.log`、`Builds/Windows-GlbProfiles6/NyaForge.exe`（`NYAFORGE_PLAYER_OK`）。Authoring **PASS**: `Artifacts/Authoring-20260913-003917-d47bd96805f14dc38ae049172c9aea12/report.json`。Unity Bridge **PASS**: `Artifacts/BridgeReceiver-20260913-003948-609-589ce9c598e347dd99e6061fbd3f6e2e/bridge-report.json`。

- Multi-object authoring foundation: Core **413 passed / 0 failed** (`Logs/core-multi-object-foundation-v6.txt`、temporary output `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-1b7178a1a74946b6b3fb680843792dcd`)。graph/staticの2 object追加・active選択・`object.select`・state hash・Save/Open・active object評価と、複数static objectでの編集保持を確認。最終Windows Player build **PASS** (`Logs/build-player-20260912-230432-122.log`、`Builds/Windows-MultiObjectBackdrop/NyaForge.exe`)。Player Authoring **PASS** (`Artifacts/Authoring-20260912-230451-673c5685904b451e91b19f0f469888e3/report.json`)、Bridge **PASS** (`Artifacts/BridgeReceiver-20260912-230523-234-756d4f789c794e14bae97eb3c343130f/bridge-report.json`)。非active objectの読み取り専用backdrop、表示切替、Frame、Save/Openを検証した。これはactive objectを一度に編集する基盤の証拠で、結合出力、共有参照、実素材、実操作・画像目視、実VRChat受入を含まない。
- Multi-object package export: Core **414 passed / 0 failed** (`Logs/core-multi-object-export-v2.txt`、temporary output `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0a53bf40af3142579164c8697d022158`)。複数graph objectを個別Bakeへ出力し、`multi-object.nyaforge-bake.json`から全manifestを検証、文書revision/state hash不変と既存出力先の拒否を確認。mesh結合やskin/morphの統合出力ではない。
- Feature-preserving graph export: Core **418 passed / 0 failed** (`Logs/core-native-feature-export.txt`、temporary output `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-20bbfabc91d94dba9293bec879dd533a`)。rig/morph nodeを含むgraphは`project.nyaforge.json` native packageへルーティングし、source workspaceのdocument/revision/dirty状態を変更せず、再読込後もMorphSetを保持することを確認。標準GLBのStaticGeometry／SkinnedGeometry profileは別途追加済み。Windows Player **PASS** (`Logs/build-player-20260912-233249-128.log`、`Builds/Windows-NativeFeatureExport/NyaForge.exe`)、Authoring **PASS / 73 checks** (`Artifacts/Authoring-20260912-233319-d7adb0e572cd4e2c8ef097160e9dfba0/report.json`)、Bridge **PASS** (`Artifacts/BridgeReceiver-20260912-233420-732-a4c6ac0538ca4a80ac774259d3db1ed6/bridge-report.json`)。静的Bake形式は維持し、標準GLBは今回追加した明示profileで出力する。標準VRM、任意pose/morphのskin変換、材質・animation、mesh結合は未完了。
- Multi-rig session table: Core **418 passed / 0 failed** (`Logs/core-multi-rig-sessions.txt`、temporary output `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-a074d326f26a420cb928d780ba0a854a`)。graph IDごとのImportedRigSessionを`imported-rig-sessions.nyaforge.bin`へ版付き保存し、複数sessionの順序・source skin/binding・identity往復を確認。Windows Player **PASS** (`Logs/build-player-20260912-235330-393.log`、`Builds/Windows-MultiRigSessions2/NyaForge.exe`)、Authoring **PASS** (`Artifacts/Authoring-20260912-235349-f97c7235001d421eb588b19ff602a71e/report.json`)、Bridge **PASS** (`Artifacts/BridgeReceiver-20260912-235419-288-43be89d36c63426e979704635857576e/bridge-report.json`)。旧single rig attachmentはfallbackで読める。複数meshの結合・共有参照・複数SkinDeform同時評価は未完了。
- P1 consistency fixes: Core **417 passed / 0 failed** (Logs/core-p1-final.txt、temporary output C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-3c1404b7cc604ef0bf33e4b52ddb4563)。8/16bit JOINTSの同値読取、skin付きmeshと同居する静的小物の選択取込、source skin適用後も下流graph編集を保持する評価、PhysBones target packageのsource asset同梱とreceiver側自動検証を追加。Windows Player **PASS** (Logs/build-player-20260912-p1-final2.log、Builds/Windows-P1ConsistencyFinal2/NyaForge.exe)、Authoring **PASS** (Artifacts/Authoring-20260912-232526-29c4d7d74e9d43f8b2aae80c26032d6c/report.json, 73 checks)、Bridge **PASS** (Artifacts/BridgeReceiver-20260912-232610-682-ea55fcb213e149e4aca246b668ed4e1c/bridge-report.json)。実SDK・実アバター・実VRChatの受入、mesh結合・共有参照・複数SkinDeformは未完了。

- SourceMeshTransform: Core **378 passed / 0 failed** (`Logs/core-source-mesh-transform.txt`)。非一様/shear/鏡映の位置・方向・面順/UV保持、morph ID維持とtopology再pin、morph適用との可換性、不正方向/stale入力を検証。一般skinのweight混合・GUI取込はまだ未接続。
- Windows-SourceMeshTransform build **PASS** (`Logs/build-player-20260912-170453-386.log`)。独立Coreモジュール追加のためPlayer GUI suiteは今回再実行していない。
- SourceSkinBinding/Deformer: Core **381 passed / 0 failed** (`Logs/core-source-skin-deformer.txt`)。slot順、32影響/vertex、rest inverse-bind相殺、pose paletteの位置/法線/接線/UV/topology保持、stale domain/未weight/不正paletteを確認。GLB weight decoder・native binding・GUI保存接続まで完了。既存graphの一般deform表示接続は未完了。
- GLB source skin importer: Core **384 passed / 0 failed** (`Logs/core-glb-source-skin-importer.txt`)。一meshの全primitiveでdense JOINTS/WEIGHTS set、slot順・vertex offset・skin参照をSourceSkinBindingへ接続し、unpaired/noncontiguous/type/sparse/range/宣言buffer不整合を拒否。normalized integer weightをfloatへ変換して保持し、sparseは未対応。
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
- SIM-01 secondary-motion contract: Core **400 passed / 0 failed** (`Logs/core-secondary-motion.txt`)。`NYSM` v1のprofile／stable chain／fixed vertex／collider group往復、bone-poseとmesh-deformation出力の分離、unknown versionのopaque保持、skeleton/topology stale拒否、resolved springの共通topology移行、VRM1 sessionからのsource node→authored BoneId移行を確認。Windows-SecondaryMotionCore2 build **PASS** (`Logs/build-player-20260912-184902-690.log`)、Player **PASS** (`Artifacts/Authoring-20260912-184923-60fa1a94c49d4c67833fcb32277bdfdf/report.json`, 70 checks) で既存Workbench回帰も確認。native attachmentとVRM0 source-node移行はSIM-01Bで接続済み。PhysBones/MagicaCloth2実adapterは未接続。
- SIM-02 PhysBones target boundary: Core **404 passed / 0 failed** (`Logs/core-physbones-attachment.txt`)。`NYPP` v1のroot/endpoint/exclusion/branch/collider/limits/curve/interaction DTO往復、stable bone・stale skeleton拒否、unknown versionのopaque保持、SDK capabilityとloss reportのsupported/unsupported/warning分離、schema 4 PhysBones attachmentのbyte/hash一致Openを確認。Unity SDK component writer、実VRChat SDK/アバター動作、実操作・画像目視の受入は未確認。Windows-SecondaryMotionPhysBonesAttachment build **PASS** (`Logs/build-player-20260912-190938-424.log`)、Player **PASS** (`Artifacts/Authoring-20260912-190959-488cdb25d1124cb0b86f86853b0891c2/report.json`, 70 checks)。
- SIM-02 PhysBones Workbench status: Windows-PhysBonesStatusGui2 build **PASS** (`Logs/build-player-20260912-191727-520.log`)、Player **PASS** (`Artifacts/Authoring-20260912-191748-5e9371fb9e454c29b618a74947a443ce/report.json`, 71 checks)。保存済みtargetを新PlayerでOpenし、対応版のchain/SDK表示、unknown wire version 99の保持のみ表示を確認。Unity SDK component writer、実VRChat SDK/アバター動作、実マウス・画像目視の受入は未確認。
- SIM-02 PhysBones target package: Core **406 passed / 0 failed** (`Logs/core-physbones-bridge-package.txt`)。`physbones.nyaforge-target.json`、`physbones-target.nyaforge.bin`、`skeleton.nyaforge.bin`をhash検査付きで往復し、改ざんprofileを拒否した。
- SIM-02 UnityBridge writer: Windows-PhysBonesBridgeGui4 build **PASS** (`Logs/build-all-20260912-195001-224.log`)、Player **PASS / 71 checks** (`Artifacts/Authoring-20260912-195021-2abdd80c693f4d5ba51942a327f3e29b/report.json`)。receiver **PASS / 10 checks** (`Artifacts/BridgeReceiver-20260912-202001-392-7a27c7a4d1f647868118e83a3ca6bdc0/bridge-report.json`)、Unity 2022.3.22f1。Workbenchのtarget package書き出し、`ApplyPackage`経由のmanifest/profile/skeleton読込、stable bone mapping、managed-only更新、未管理component保護、unsupported preflight停止、SDK型形状のreflection mapping、branch表現可能性の事前検査、遅いconfigure失敗のrollback、package identity付きstable bindingの保存／stale manifest拒否を合成fixtureで確認した。受け取り側EditorWindow（stable BoneIdとcollider groupの手動割当、avatar rootへの保存／読込）もコンパイル確認済み。実VRChat SDK／実アバター／VRChat内動作の受入ではない。
- SIM-03A secondary-motion lifecycle: Windows-SIM03B build **PASS** (`Logs/build-player-20260912-204039-364.log`)、Player **PASS / 72 checks** (`Artifacts/Authoring-20260912-204100-9a3d2feb718345d880ae3b9f998f27ca/report.json`)。VRM0/1の既存Spring previewへGUI・内部MCP handler・外部sidecar toolの`secondary_motion_play`／`pause`／`reset`／`rebuild`／`step`／`state`を接続し、実MCP client→sidecar→named pipe→Player main threadの経路で再生、pause/resume、1固定step、再構築、reset、Save/Openでシミュレーションを保存しないこと、編集時停止を確認した。外部MCP fixtureはVRM1で固定し、Playerの自動tickを停止してstep数を決定的に照合した。実SDK／実アバター／VRChat内動作、実マウス操作・画像目視の受入ではない。
- SIM-03B secondary-motion capture: Core **408 passed / 0 failed**。`SecondaryMotionCaptureRecord`／codecとstrict requestを追加し、固定1/60秒、warmup、連続frameのpose/mesh/PNG hash、input/config hash、adapter/package/Unity/build、target/root/collider条件、complete/failed statusを記録する。`EvidenceModelCapture`のPNG描画をtransient `GraphMeshValue`でも共有し、`forge_secondary_motion_capture`をsidecarへ公開した。Windows-SIM03B-Capture3 build **PASS**（`Logs/build-player-20260912-210411-934.log`）、Player **PASS / 72 checks**（`Artifacts/Authoring-20260912-210441-f5a69f71558041289ab661a829c9958f/report.json`）。実MCPで128px・warmup2・3frameを取得し、PNG hash／寸法、completedSteps 3/4/5、撮影後のpreview reset、documentId/revision/stateHash不変を確認した。これはtransient previewの証拠であり、実SDK／実アバター／VRChat内動作、実マウス操作・画像目視・VRChat受入とは別である。
- SIM-01B native attachment / VRM0 migration: Core **411 passed / 0 failed**（`Logs/core-secondary-motion-rebind.txt`）。`secondary-motion.nyaforge.bin` をschema 4 attachmentへ追加し、NYSM bytes/hashのSave/Open、VRM0 source-node subtreeのchildren順展開→stable BoneId移行、VRM0/1移行結果、skeleton/topology stale時の再bind診断、明示bone/vertex mappingによるrebindを確認。未知wire版は既存codecのraw bytes保持をWorkbench表示へ接続した。Windows-SIM01B-Rebind build **PASS**（`Logs/build-player-20260912-212944-116.log`）。実素材のrebind、実SDK／実アバター／VRChat受入は未完了。
- SIM-01B-R GUI rebind: Windows-SIM01B-RebindGui5 build **PASS**（`Logs/build-player-20260912-215039-912.log`）、Player **PASS / 73 checks**（`Artifacts/Authoring-20260912-215102-0d9808eabc394e84a899bf0b1a614cd7/report.json`）。stale骨格で「同じBoneIdで再bind」ボタンが有効になり、明示stable IDでskeleton hashを再固定、保存要求を出すことを確認。別のリグPose/Binding再bindと二次運動attachment再bindを分離し、骨格stale中もtopology witnessで操作可能にした。実素材のrebind、実SDK／実アバター／VRChat受入は未完了。
- SIM-02B receiver regression: 最新Player出力を `Tools/Test-NyaForgeUnityBridge.ps1` へ接続し、Unity **2022.3.22f1** receiver **PASS**（`Artifacts/BridgeReceiver-20260912-213134-704-b2d9a3661628485d88bca9c1b40e0c19/bridge-report.json`）。PhysBonesのmanaged-only、未管理component保護、capability不足の事前停止、reflection field mapping、branch preflight、binding identity確認を回帰した。これは合成fixture受入であり、実VRChat SDK／実アバター／VRChat内動作ではない。
- SIM-02B receiver regression (RebindGui5): 最新Player成果物を同じBridge検証へ接続し、Unity **2022.3.22f1** receiver **PASS**（`Artifacts/BridgeReceiver-20260912-215208-952-9086fd84d8f541499ee8873c5e073a99/bridge-report.json`）。GUI再bind追加後もmanaged-only、未管理component保護、capability preflight、reflection mapping、branch preflight、binding identityを維持した。これは合成fixture受入であり、実VRChat SDK／実アバター／VRChat内動作ではない。
- SIM-02B reflection catalog regression: Unity **2022.3.22f1** receiver **PASS**（`Artifacts/BridgeReceiver-20260912-220031-153-b6d83d0cd2ef48ba94dc95e896ab488e/bridge-report.json`）。別component型のmarker干渉を分離したうえで、継承元private fieldを含むreflection discovery、package apply、managed-only、rollbackを確認した。これはSDK形状fixtureの受入であり、実VRChat SDK／実アバター／VRChat内動作ではない。
- SIM-02B binding validation: Unity **2022.3.22f1** receiverで`PhysBonesBindingValidator`の有効なdescendant mapping、root外bone、不足collider groupを確認した。保存／読込／適用前の共通検証としてGUIへ接続済み。これは合成fixtureの受入であり、実VRChat SDK／実アバター／VRChat内動作ではない。
- SIM-02B binding validation checkpoint: Windows Player build **PASS**（`Logs/build-player-20260912-220850-504.log`）、Player **PASS**（`Artifacts/Authoring-20260912-220901-ad5f61277a8d434a822f0ea09c3c2f43/report.json`）。同成果物をUnity **2022.3.22f1** receiverへ渡し、**10 checks PASS**（`Artifacts/BridgeReceiver-20260912-220932-900-7db78aa674cd4198849405029fd7accb/bridge-report.json`）。validatorの有効mapping、root外bone、不足collider groupを回帰した。これは合成fixtureの受入であり、実VRChat SDK／実アバター／VRChat内動作ではない。
- SIM-02B component identity checkpoint: Core **411 passed / 0 failed**（`Logs/core-physbones-component-type.txt`）。custom完全修飾型のmanifest往復と、型名を省略した旧manifestの既定型フォールバックを確認。Windows Player build **PASS**（`Logs/build-player-20260912-221503-050.log`）、Player **PASS**（`Artifacts/Authoring-20260912-221524-3e04602763934b2d82a483965a3d197b/report.json`）、Unity **2022.3.22f1** receiver **10 checks PASS**（`Artifacts/BridgeReceiver-20260912-221557-826-3ecd6b78c46743cd8321f2f3981d147b/bridge-report.json`）。これはcomponent型fixtureの受入であり、実VRChat SDK／実アバター／VRChat内動作ではない。
- SIM-02B resolver checkpoint: `VrcPhysBonesReflectionResolver`のassembly-qualified型の厳密解決、未導入型、非Component型の非破壊診断をBridge合成fixtureで確認。初回のnamespace import漏れを修正後、Unity **2022.3.22f1** receiver **10 checks PASS**（`Artifacts/BridgeReceiver-20260912-222102-611-57859d8c86c24b6ca4c52e07b29ac032/bridge-report.json`）。実VRChat SDK／実アバター／VRChat内動作ではない。
- SIM-02B inspection checkpoint: Windows Player build **PASS**（`Logs/build-player-20260912-222445-126.log`）、Player **PASS**（`Artifacts/Authoring-20260912-222457-93f8c1036d7a4da5bdab9aac1f4fc994/report.json`）。Unity **2022.3.22f1** receiver **10 checks PASS**（`Artifacts/BridgeReceiver-20260912-222531-558-e72dc25468864eef945e062f6d08dae9/bridge-report.json`）。`Inspect`／`InspectPackage`の書き込みなし、作成／更新件数、既存preflight共有を確認した。実VRChat SDK／実アバター／VRChat内動作ではない。
- 追試: Core **411 passed / 0 failed**（`Logs/core-physbones-inspection.txt`、output `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-1dadfa8c2faa40cf9ee5dcb423fb65e0`）。事前診断追加後もCore回帰なし。Unity／実SDK／実アバター／VRChat内動作の受入範囲は上記のまま。
- 受け取り側の再追試: 現在のUnityBridgeソースで `Test-NyaForgeUnityBridge.ps1 -PlayerCheckDirectory Artifacts/Authoring-20260912-222457-93f8c1036d7a4da5bdab9aac1f4fc994` を実行し、Unity **2022.3.22f1** の **PASS** を確認（`Artifacts/BridgeReceiver-20260912-222916-384-59578864671a402f8ef5e1c383eef92d/bridge-report.json`）。
- I04-B instance-aware import checkpoint: Windows Player build **PASS**（`Logs/build-player-20260912-223251-070.log`）、Authoring Player **PASS**（`Artifacts/Authoring-20260912-223313-162dccb9745c4947abe511a0bb83d3ec/report.json`）、Unity **2022.3.22f1** receiver **PASS**（`Artifacts/BridgeReceiver-20260912-223348-801-46bf29fe3a604a11b9adff8f7aa712a1/bridge-report.json`）。WorkbenchのGLB選択へnode instance indexを追加し、選択nodeのmesh／skinを採用、未skinned instanceを強制skinned importしない分岐を接続した。合成Player fixtureの回帰であり、任意実GLB／複数制作対象／実アバター受入ではない。

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
- 次はVRM0展開targetから実行chainを作り、この一時骨格のcenter/collider変換、固定step所有者、Workbenchへ接続する。呼出側は展開targetとcenter/collider nodeを必要集合へ含める。必要node+祖先が実行Coreの512骨予算を超える場合は明示拒否。T02全体は未完了。

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
- `Authoring.Import.GlbSkinImport` を追加。1 mesh／1 skin、最大512 joints、連続するJOINTS_n＋WEIGHTS_nの最大32 influence、translation-onlyのnode／inverse-bindを`SkeletonDefinition`／`SkinBinding`へ変換する。複数skin、回転・非unit scale、JOINTS_1、未weight、sparse／非対応accessorは理由付きで拒否する。
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

## レビュー追従の回帰証拠 2026-09-13

- MCP sidecarはPNGの`data`だけをstructured/textから除去し、画像ごとのcamera・解像度・hash・撮影条件を保持する。`Tests/Mcp.Transport/CaptureResultVerification.cs`でbytes非重複とcamera保持を検証し、transport suite 3項目PASS。
- PhysBonesは削除curve初期化とstable target/root/name marker再利用を実装済み。Unity Bridge 2022.3.22f1の最新検証はPASS。
- 実VRChat SDK内の受入と揺れリセットキャッシュは未完了。secondary-motion再bindのUndoはCore回帰で確認済み。

## secondary-motion再bind Undo 実素材検証 2026-09-13

- Core **426 passed / 0 failed**。metadata rebindをUndo/Redoすると、Document geometryを変えずにattachment bytesと再読込可能なmetadataを元へ戻し、Redoでrebound bytesへ戻す。
- Windows Player **PASS / 74 checks**（`Builds/RebindUndo/NyaForge.exe`, `Logs/build-all-20260913-014245-743.log`, `Artifacts/Authoring-20260913-014317-08b047e74a554ba88b3f760ba11a05e1/report.json`）。実RadDollV3 VRMでstale skeleton→same-BoneId rebind→Undo/Redoのattachment復元を確認した。
- Unity Bridge **PASS**（Unity 2022.3.22f1, `Artifacts/BridgeReceiver-20260913-014429-987-190b810cf80a499cba2ed3c8ca51a256/bridge-report.json`）。実VRChat SDK内の動作受入と揺れリセットcacheは未完了。

## 揺れリセット表示cache検証 2026-09-13

- `ClearSpringPlayback`でsource-skin投影cache keyを無効化し、transient spring meshから通常graph表示へ戻すとき同じevaluation hashでも再投影するよう修正した。
- Windows Player **PASS / 74 checks**（`Builds/ResetCache/NyaForge.exe`, `Logs/build-all-20260913-014541-107.log`, `Artifacts/Authoring-20260913-014601-ad53de01f4a34333926580298d3f1c36/report.json`）。VRM1/VRM0で再生→reset後のsource-skin表示がbaselineへ戻る回帰を確認した。
- Unity Bridge **PASS**（Unity 2022.3.22f1, `Artifacts/BridgeReceiver-20260913-014712-999-fcdbade261d64afe917d7e3c338f925a/bridge-report.json`）。

## 一般TRS / inverse-bind取込の拡張 2026-09-13

- `GlbSkinImporter`のskinned GLB入口をtranslation-onlyからsource affine対応へ更新した。`GlbNodeTransformReader`のnode TRS/matrixをそのまま階層へ接続し、inverse-bindは`GlbMatrixAccessorReader`で一般4x4 affineとして読み、逆行列の原点からportable skeletonのHeadを求める。source skin側のNYFS/NYSPは従来どおりlocal/world、bind、normal/tangent変換に使う。
- IBM accessorの有効な`byteStride`とstorage `target`を拒否せず、MAT4 element幅64 bytes、4-byte alignment、範囲、strideを検証する。stride 64の標準出力と不正stride 60を回帰した。
- GUIの説明、Import README、Model Interchange、Source Affine、node hierarchyの記述を現行能力へ更新した。FBX直接取込、複数mesh結合、未知拡張・材質・animationの完全保持、標準VRM出力、実VRChat受入は未完了。
- Core **426 passed / 0 failed** (`dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore`、artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0a39624ef96840098816b23f58061815`)。追加回帰は一般node TRSを保持したskinned取込、source matrix accessorのstride互換性と不正stride拒否。実RadDollV3の再取込は未実施で、下記のPlayer/Bridge回帰のみ実施した。
- Windows Player / Bridge再回帰: Player build `Logs/build-player-20260913-015653-885.log` (`Builds/GeneralTrs/NyaForge.exe`)、Authoring **PASS / 74 checks** (`Artifacts/Authoring-20260913-015713-d25dd1013a9548e5adfe804ffba7a74b/report.json`)、Bridge **PASS** (`Artifacts/BridgeReceiver-20260913-015746-828-dbeb9bd2b7fb4c7dad00c00267f5792d/bridge-report.json`)。一般TRS対応後も既存GUI・保存・Bake・Bridge回帰は通過した。invalid fixtureは有効なnon-unit scaleを拒否テストに使わないよう、特異scale 0へ更新した。
- 実RadDollV3再回帰: private temp `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`（45,341,584 bytes）を`-ImportModel`で再取込し、Player **PASS / 74 checks** (`Artifacts/Authoring-20260913-015937-4c3dc3c8c3114d2585dc4d5d3e11cc8c/report.json`)。mesh 1 / skin 1、129,348 vertices、171 bones、35 morphsの候補確認、rest-space頂点編集、native Save/Open、SkinnedGeometry GLB出力と再読込を確認した。Unity Bridge **PASS** (`Artifacts/BridgeReceiver-20260913-020039-628-8b0e715c5a4a4fa48331586332bb0460/bridge-report.json`)。素材はpublic repoへ同梱していない。

## 静的node instanceのaffine適用 2026-09-13

- `GlbImporter.Read(bytes, meshIndex, instanceWorld)`を追加し、選択したGLB node instanceのworld affineを静的meshのpositions、normal、tangent、POSITION morphへ`SourceMeshTransform`で適用する。Workbenchのnode instance選択から静的小物を取り込むと、元sceneの配置・回転・拡縮を失わず編集対象へ入る。source resource選択（instanceなし）は従来どおりidentityで読む。
- Core **427 passed / 0 failed** (`dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore`、artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-c77095bc73ec4ed39dc77977e3ba7ba6`)。回転＋非一様scale＋translationがgeometryとmorphへ同じaffineで適用されること、source meshのidentityと警告を確認した。
- skinned node instanceのmesh-node affine、mesh結合、shared mesh/skin参照、material/animation/未知拡張保持は引き続きI04-B/Eの残件。標準VRM出力と実VRChat受入も未完了。
- 静的node affine変更後のPlayer確認: `Builds/InstanceAffine/NyaForge.exe`、log `Logs/build-player-20260913-020315-915.log`。Authoring **PASS / 74 checks** (`Artifacts/Authoring-20260913-020337-727ddfb457c540d18788a1b6c47267c8/report.json`)、Bridge **PASS** (`Artifacts/BridgeReceiver-20260913-020409-148-0cad4b3e3ab54e23839d8e70ed3d432d/bridge-report.json`)。さらにRadDollV3 VRMを同buildで再取込し、Player **PASS / 74 checks** (`Artifacts/Authoring-20260913-020423-8731649659a7441d942ac79d343de2f3/report.json`)、Bridge **PASS** (`Artifacts/BridgeReceiver-20260913-020528-915-0be4ce394f9f435dbc5fc5c95c1be047/bridge-report.json`)。

## スキン付きnode instance affineの保存・表示・出力（2026-09-13）

- `GlbSkinImporter.Read(bytes, meshIndex, skinIndex, instanceWorldTransform)` を追加し、選択したskinned nodeのworld affineをsource skinとは分離して `ImportedSkinnedMeshSource.InstanceWorldTransform` へ保持する。Workbenchのnode instance選択から渡し、頂点編集はsource-local空間のまま扱う。
- `ImportedRigSession` と codecをv6へ拡張し、`meshInstanceTransform` を16要素のcolumn-major affineとしてnative Save/Openで往復する。旧v1〜v5は従来どおり読める。
- `SourceSkinGraphAdapter` はsource paletteで変形した最終graph outputへinstance affineを最後に適用する。これにより下流EditMeshを維持した表示と、node transformを含む表示座標が一致する。
- 標準SkinnedGeometry GLB出力はmesh nodeの `matrix` としてaffineを保持する。Core **428 passed / 0 failed**（`dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore`、artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f32e8173dfcc4dada13ab9bacfd28e7d`）。v6 codec、表示投影、matrix出力を合成fixtureで確認した。
- 残る境界は実RadDollV3でのnode instance選択を含むPlayer GUI受入、複数SkinDeform/共有参照、VRChat実機でのmatrix解釈確認。通常のVRChat受入完了とは扱わない。

## skinned instance v6 Player / Bridge検証（2026-09-13）

- Windows Player build **PASS**: `Logs/build-player-20260913-021725-624.log`, `Builds/SkinnedInstanceAffineV6/NyaForge.exe`。
- 合成Authoring suite **PASS / 74 checks**: `Artifacts/Authoring-20260913-021746-1f7f7e88b7964f6ebbae701adf735ec0/report.json`。
- private RadDollV3 VRMを使ったAuthoring suite **PASS / 74 checks**: `Artifacts/Authoring-20260913-021825-ea371b80cb854b9ba8d31247e4e30471/report.json`。実素材の候補取込、EditMesh、native Save/Open、標準skinned GLB出力・再取込を確認した。素材はpublic repositoryへ入れていない。
- Unity Bridge **PASS**（Unity 2022.3.22f1）: `Artifacts/BridgeReceiver-20260913-021930-911-a5659f6dc6c24f98bbd8fe74410feac1/bridge-report.json`。
- これはPlayer自動検証とprivate実素材smokeの証拠であり、実マウスで特定node instanceを選び、matrixを含む出力をVRChat内で受入した証拠ではない。SIM-02B/SIM-07Aと実操作受入は継続する。

## skinned instance status表示（2026-09-13）

Workbenchの取込骨対応statusに「node affine保存済み」を表示し、選択nodeの配置情報がsessionへ保持されていることをGUIで確認できるようにした。Windows Player build **PASS**: `Logs/build-player-20260913-022259-574.log`, `Builds/SkinnedInstanceAffineV6Ui/NyaForge.exe`。

## normal/tangent morphの実素材境界（2026-09-13）

private RadDollV3 VRMにNORMAL/TANGENT morphが含まれることを確認した。現行MorphSetはPOSITION deltaだけを表現するため、取込は継続しつつ未対応属性をwarningへ明記する。実モデルを拒否する変更は採用しない。専用attribute morph schemaでは、normal/tangent deltaの変換・保存・表示・出力を同じsource hashで往復できることを完了条件にする。Core **429 passed / 0 failed**（`dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore`、artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f32e8173dfcc4dada13ab9bacfd28e7d`）。

## normal/tangent morph v2 接続（2026-09-13）

`MorphTarget`／`MorphSet`へ疎なNORMAL/TANGENT差分を追加し、`NYRM v2`で保存した。旧`NYRM v1`は属性差分なしとして読み続ける。`MorphDeformer`はweight適用時に法線を再正規化し、接線のXYZ差分とhandednessを保持する。`SourceMeshTransform`はnormal差分を逆転置の非正規化ベクトル、tangent差分を線形ベクトルとして変換する。

GLBのprimitive target `NORMAL`／`TANGENT`を読み、対応するbase属性がある場合はnative morphへ保持する。base属性が無い既存素材はPOSITIONを保持したまま明示warningを出す。標準GLB出力も保持済み差分をtarget accessorへ書く。回帰として属性差分のcodec/deformer、属性欠落warning、既存POSITION-only GLB、選択node affine、実RadDollV3 VRM取込を確認した。

- Core: **430 passed / 0 failed** (`Logs/core-morph-v2.txt`, `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-6dacbb61170542f089fc127ae2a66790`)
- Windows Player: **PASS** (`Logs/build-player-20260913-023614-206.log`, `Builds/MorphV2/NyaForge.exe`)
- 実RadDollV3 VRM Authoring smoke: **PASS** (`Artifacts/Authoring-20260913-023708-6afa00ae146341d59023d66281e62b43/report.json`, Unity 6000.4.3f1)。これは実マウス操作・実VRChat内受入・private素材公開を意味しない。
- 残り: normal/tangent差分を含む実GLBの再出力→再取込で数値一致を追加確認し、corner domain／複数mesh共有参照と標準VRM出力へ広げる。

## I04-C source rig容量拡張（2026-09-13）

実素材調査で確認した単一mesh 262 morph、257骨、18 influenceを削減せず扱えるよう、native rigの予算を拡張した。`SkeletonDefinition.MaxBones`は512、`SkinBinding.MaxInfluencesPerVertex`は32、`MorphSet.MaxTargets`は512。`GlbSkinImporter`は連続する全JOINTS_n/WEIGHTS_n setを読み、32 influenceまで`SkinBinding`へ渡す。標準`SkinnedGeometry` GLB出力は受取先互換のため4 influence制限を維持し、超過時は明示的に拒否する。

Coreの容量回帰で257骨・18 influence・262 morphのnative codec往復を確認した。実RadDollV3で使った既存Windows smokeとは別に、次は257骨／複数weight setを含む実GLB受入を追加する。PhysBones／secondary-motionの1024骨予算とは別の、通常rig authoring予算である。

- Core: **432 passed / 0 failed** (`Logs/core-capacity.txt`, `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-2000423f2af54ce4ad3f2b3ab2c2ed68`)
- 残り: 複数mesh共有参照、corner domain morph、4 influenceを超える標準GLB出力の別profile、標準VRM出力、実VRChat受入。

- 追加回帰: `GlbSkinImporter`の合成fixtureでJOINTS_0/WEIGHTS_0とJOINTS_1/WEIGHTS_1を同一vertexへ統合し、重複なしの2 influenceとしてnative bindingへ公開できることを確認した。
- Windows Player容量拡張build **PASS** (`Logs/build-player-20260913-024616-295.log`, `Builds/RigCapacityV1/NyaForge.exe`)、RadDollV3 VRM Authoring smoke **PASS** (`Artifacts/Authoring-20260913-024636-5173918167f34b6cb6ffab85dd973313/report.json`, Unity 6000.4.3f1)。実素材での通常取込回帰は確認したが、257骨／18 influenceを含む別実GLBの実ファイル受入とVRChat内確認は未完了。
- `SkinnedGeometryExtended`の8 influence writer→GLB再取込回帰を追加。Core **434 passed / 0 failed** (`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-34e4f81e4e8f4c6597dd61acdf96bb65`)。Windows Player build **PASS** (`Logs/build-player-20260913-025706-941.log`, `Builds/ExtendedGlbV1/NyaForge.exe`)、Authoring suite **PASS** (`Artifacts/Authoring-20260913-025733-6d8dccdbe10949f8baa277d967cdec8e/report.json`)。実Unity/VRChat受取先での拡張profile受入は未確認。
- Source skin display now accepts multiple `SkinDeform` branches when they share skeleton and pose. Core回帰・Windows Player build **PASS** (`Logs/build-player-20260913-030252-754.log`, `Builds/MultiSkinV1/NyaForge.exe`)、Authoring suite **PASS** (`Artifacts/Authoring-20260913-030314-fddca601322a40ffb4bf2644ed0087ad/report.json`)。異なるmesh／pose／shared instanceの一般化は未完了。

## GLB取込診断（2026-09-13）

- `GlbImportDiagnostic` と `Diagnostics` を静的／skinned import結果へ追加。選択meshに材質・画像リソースがある場合は `MATERIALS_NOT_RETAINED`、animationは `ANIMATIONS_NOT_RETAINED`、`extensionsRequired` は `REQUIRED_EXTENSIONS_NOT_RETAINED`（blocking）、`extensionsUsed` は `EXTENSIONS_PARTIAL`（partial）として、path・理由とともに保持する。既存Warningsにも同じコードを出す。
- Core **436 passed / 0 failed**。自作GLBへ4種類を同居させ、Diagnosticsのblocking/partial分類と警告表示を確認した。Windows Player **PASS** (`Logs/build-player-20260913-031410-506.log`, `Builds/GlbDiagnosticsV2/NyaForge.exe`)、Authoring suite **PASS** (`Artifacts/Authoring-20260913-031430-b65d18fdb5a54ace9f04e7d21a903284/report.json`)。
- これは「完全取込成功」と表示しないための診断契約であり、材質・animation・未知拡張を依存資源ごとnativeへ保存する実装、GUI/MCPの詳細report、required拡張の網羅的拒否は未完了。

## 複数object inspection（2026-09-13）

- `AuthoringGraphReader`のgraph inspectionへ`activeObjectId`と`objects`一覧を追加。active objectは従来どおり詳細graphを返し、全objectについてgraphId、nodeCount、評価完了／stale、output要約、diagnosticsを返す。非active graphも純粋評価で確認するため、MCPから対象を選ぶ前に全体状態を読める。
- Core **437 passed / 0 failed** (`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-6bff3098e94c4eb2a0560bd6617b678e`)、Windows Player **PASS** (`Logs/build-player-20260913-031801-663.log`, `Builds/GraphInspectV1/NyaForge.exe`)、Authoring suite **PASS** (`Artifacts/Authoring-20260913-031821-7095e42ce7554e4587deb67f44e0ba35/report.json`)。
- objectごとの頂点ページ、編集context、材質／paint操作は引き続きactive objectを明示選択してから行う。

## GLB診断のnative保存（2026-09-13）

- `import-diagnostics.nyaforge.json` attachment（schema 4）を追加し、graphIdごとにsourceHash、mesh／skin locator、blocking／partialの`GlbImportDiagnostic`を保存する。既存attachmentと同じblob hash・writer lock・失敗時旧snapshot保持を使い、元GLBを再読込せず診断を復元する。
- `forge_graph_inspect`のactive graph詳細と全object summaryへ`importDiagnostics`を追加。Core **441 passed / 0 failed** (`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-95fd651be1094560b029f04b72aa1d7c`)、snapshot／inspection往復と旧GLB diagnostics sidecarの保存時schema 4移行を回帰した。
- Windows Player **PASS** (`Logs/build-player-20260913-032941-070.log`, `Builds/ImportDiagnosticsV2/NyaForge.exe`)、Authoring suite **PASS** (`Artifacts/Authoring-20260913-032959-ca8fa373b3ef4e56b32c0f6660fe4dcf/report.json`)。取込後status表示、Graph inspection一覧、診断attachment復元を含むPlayerで確認した。

## GLB診断のGUI詳細表示（2026-09-13）

- `取込診断（保存済み）` foldoutをGLB取込パネルへ追加。保存済みrecord数、保持不可／一部保持の件数、active graphを表示し、各graphのsource hash・mesh／skin locator・診断のseverity／code／path／messageを確認できる。attachmentが壊れていても作品を置き換えず、読込エラーを同じ領域へ表示する。
- Windows Player **PASS** (`Logs/build-player-20260913-033512-860.log`, `Builds/ImportDiagnosticsUiV2/NyaForge.exe`)、Authoring suite **PASS** (`Artifacts/Authoring-20260913-033534-e6ed3ced2eb04b8896208cde5f0f05ab/report.json`)。合成した保存済みdiagnosticsをGUIへ注入し、summary・active locator・詳細labelの表示を自動検証した。Core **441 passed / 0 failed** (`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-95fd651be1094560b029f04b72aa1d7c`)。旧GLB diagnostics sidecarの保存時schema 4移行も回帰した。
- GUIは保持結果の確認導線であり、材質・animation・未知拡張を依存資源ごとnativeへ完全保存する機能や、実モデルを使った目視受入を含まない。
## 2026-09-13 same-skeleton multi-mesh skinned GLB output

標準 `SkinnedGeometry`／`SkinnedGeometryExtended` GLB出力を、同じskeleton hashを共有する複数graph objectへ拡張した。各objectの評価済みmesh、頂点編集、binding、材質／slotを個別primitiveとして保持し、writerは一つの共有skinとmesh/nodeごとのskin参照を出力する。異なるskeletonを混ぜる場合は `GLB_SKIN_SHARED_SKELETON`、複数objectへinstance affineを同時指定する場合は `GLB_SKIN_MULTI_INSTANCE_TRANSFORM` で明示拒否し、単一objectの既存契約は維持する。

Core **459 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f8a072778bec466e98e7f0f98a3790d2`）。合成2mesh fixtureでGLBのmesh 2個・shared skin 1個、mesh node双方の同一skin index、各meshの再読込を回帰し、異なるskeletonの混在は `GLB_SKIN_SHARED_SKELETON` で出力先を作らず拒否することも確認した。Windows Player `Builds/MultiSkinnedV1/NyaForge.exe` の800x600 Authoring suite **PASS**（report `Artifacts/Authoring-20260913-065647-9698b768b4444eaaa9fda875f9e1934a/report.json`）。Unity Bridge **PASS**（Unity 2022.3.22f1、`Artifacts/BridgeReceiver-20260913-065719-599-977c683e22af42a3bdf635526c1c7557/bridge-report.json`）。異なるskeletonの自動統合、共有mesh／morph参照の完全保持、実VRChat内の見た目と実マウス／DPI受入は別境界として残る。
## 2026-09-13 per-object node affine in multi-mesh GLB output

複数graph objectをshared-skin GLBへ出力する際、`ImportedRigSession.MeshInstanceTransform`をgraph object IDごとに収集し、mesh nodeごとのcolumn-major `matrix`として保持する経路を追加した。GUIとMCPは`ExportSkinnedWithTransforms`／`ExportSkinnedExtendedWithTransforms`を使い、単一objectの既存API互換、未知object IDの拒否、異なるskeletonの拒否を維持する。これにより、同一skeletonを共有する複数meshを元node配置込みで一括出力できる。

Core **459 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-19be84b2f5824a7d96c40b8019560659`）。合成2mesh fixtureでobjectごとに異なるtranslationを与え、両mesh nodeのmatrix保持と再読込を回帰した。Windows Player `Builds/PerObjectAffineV1/NyaForge.exe` の800x600 Authoring suite **PASS**（report `Artifacts/Authoring-20260913-070744-4761096855254f5780f9773c388a29fd/report.json`）。Unity Bridge **PASS**（Unity 2022.3.22f1、`Artifacts/BridgeReceiver-20260913-070816-667-d581cf14d9734e499dae852fe97d49f7/bridge-report.json`）。実VRChat内の配置・見た目、異なるskeleton結合、共有mesh／morph参照、実マウス／DPI受入は別境界として残る。
## 2026-09-13 PerObjectAffineV1 実RadDollV3再回帰

`Builds/PerObjectAffineV1/NyaForge.exe`でprivate一時素材 `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm` を読み取り専用に使い、GLB/VRM候補確認、skin取込、rest-space頂点編集、native Save/Open、標準skin GLB出力を再回帰した。最新Player Authoring suite **PASS**（800x600、`Artifacts/Authoring-20260913-071034-98196e26e7af47cfa3df95cd3eab7bbd/report.json`）。同じ出力のUnity **2022.3.22f1 BridgeもPASS**（`Artifacts/BridgeReceiver-20260913-071148-485-e6720b8b0f8d49858a5198eafa261b7b/bridge-report.json`）。private素材はpublic repositoryへ追加していない。

これは実素材の取込・編集・保存・GLB出力smokeであり、実マウス/DPI差、実VRChat内の見た目・挙動、標準VRM出力、異なるskeleton結合と完全材質保持の証拠ではない。
# 2026-09-13 graph-keyed VRM sessions / display-hit consistency follow-up

レビュー指摘のうち、複数graphをまたぐ保存再開で表情・Springが別素材へ混ざる経路を閉じた。`Expressions` と `Springs` はGraphIdをキーにした bounded table (`NVXE`/`NVXS`) として保存し、旧単一blobはrigまたは単独graphへ移行して読める。Open時は全rig/sessionのsource hashを照合し、active object切替では対応graphのsessionだけを表示する。

source skin表示はSkinDeform位置のoverride後にgraphを再評価し、後段EditMeshを保持する。標準GLB出力にはWorkBenchのimported source skinからinverse-bind行列をgraph object単位で渡せる経路を追加した（従来APIのidentity fallbackと明示instance affine互換は維持）。装着小物の頂点hit testはpreview rootのworld座標を使い、描画位置とクリック判定を一致させた。

Core **460 passed / 0 failed**（artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-57272bc5a1d74d37aed5a0f21cfab31f`）。Windows Player `Builds/ConsistencyFollowupV1/NyaForge.exe` のAuthoring suite（private一時RadDollV3 import指定、report `Artifacts/Authoring-20260913-074524-bcf817fd606e477eae95415f5f1d7032/report.json`）とUnity 2022.3.22f1 Bridge（`Artifacts/BridgeReceiver-20260913-074633-351-fcfd3de0c8c146328d42e82de7541f1b/bridge-report.json`）はPASS。実VRChat SDK、実マウス/DPI差、任意モデルの完全なnode transform互換は別受入境界として残す。

## 2026-09-13 all-mesh-instance import workflow

実アバターをbody・hair・衣装などの複数meshへ分けて編集できるよう、WorkbenchのGLB/VRM取込パネルへ「このファイルの全mesh instanceを取り込む」を追加した。各node instanceのmesh／skin対応とworld affineを保ったまま、static／skinned graphを候補ごとに検査し、一つのcommand batchで既存graph projectへ原子的に追加する。途中失敗、object上限64件超過、未対応入力では文書・metadata・Undoを変更しない。従来の候補を一つずつ取り込む導線は維持した。

Core **469 passed / 0 failed**（`dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore`、artifact `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f9109c8d3e9d491598ae0a1716b16940`）。Windows Player `Builds/AllMeshImportV1/NyaForge.exe` のAuthoring suiteは **PASS / 82 checks**（private一時RadDollV3 VRM smoke、合成multi-mesh取込のnative Save/Openとgraph-keyed rig session保持を含む、`Artifacts/Authoring-20260913-125940-84b3ec59c2f44150914ea23da3e7f4ec/report.json`）。同成果物のUnity **2022.3.22f1 Bridge**も **PASS**（`Artifacts/BridgeReceiver-20260913-130102-541-67238901e17f4faa92fe130cdae8b146/bridge-report.json`）。GUIナビゲーションも **PASS**（`Artifacts/Navigation-20260913-130116-c93b7acb2d5e41e7abd85b9d50814441/report.json`）。

この回帰は合成fixtureとprivate実モデルの取込・保存・出力smokeであり、実マウス／DPI差、実VRChat内の見た目・PhysBones、異なるskeletonの自動結合、共有mesh／skin／morph参照、完全VRM出力は別受入境界として残す。次のカードは共有参照の明示仕様化か、版固定した実SDK受け取り検証のどちらか一つに絞る。

## 2026-09-13 all-mesh-instance real-model recheck

実モデルでも一括取込の入口を検証できるよう、`Tools/Test-NyaForgeAuthoring.ps1 -ImportModel <path> -ImportAllModel` を追加した。private一時RadDollV3 VRMを使い、ファイル内の全mesh instanceをstatic／skinned graphへ展開し、各graphのEditMesh、graph-keyed rig session、native Save/Open、feature-preserving native exportを確認する。public fixtureの通常Authoring suiteは従来どおり維持する。

Windows Player `Builds/AllMeshRealV1/NyaForge.exe` の実RadDollV3 Authoring suiteは **PASS / 84 checks**（`Artifacts/Authoring-20260913-130636-74e77cd8f5b143b5a62324d73c5e3669/report.json`）。全mesh instanceの取込・保存・再開・native export roundtripを含む。同成果物のUnity **2022.3.22f1 Bridge**も **PASS**（`Artifacts/BridgeReceiver-20260913-131142-286-11d7292fd3e74bb492946824f1af290d/bridge-report.json`）。

これはprivate実モデルの自動smokeであり、実マウス／DPI差、実VRChat内の見た目・PhysBones、異なるskeletonの自動結合、共有mesh／skin／morph参照、完全VRM出力は別受入境界として残す。
# 2026-09-13 multi-object output validation

出力前のValidationがactive objectだけを見ていたため、body＋衣装の組合せでtriangle・vertex・材質・画像・骨格数を過小評価する経路を修正した。現在は全objectを同じdocument revisionで評価し、どれか一つでも未完了／stale／面なしなら `unknown` とする。負荷値は全objectの合計、同一skeleton hashは一度だけ数え、別skeletonは重複出力分を加算する。既存のfit判定がunknownである境界は維持した。

Coreは **471 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-cffa6324e716435794fde74d1fefdfef`）。2つのstatic objectを作り、triangle 8・render vertex 16として集計される回帰を追加した。Windows Player `Builds/MultiObjectValidationV1/NyaForge.exe` のAuthoring suiteは **PASS / 80 checks**（`Artifacts/Authoring-20260913-134148-d06d092bc9ce4b538a559653169febaf/report.json`）。Unity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-134221-106-41ca7637e04d4e719c2ef6b97fdab020/bridge-report.json`）。
# 2026-09-13 validation panel readability

「出力チェック」の結果表示を、判定だけの横並び文字列から、実測metrics・各check・警告を一行ずつ表示する形式へ変更した。複数objectの集計値（triangles、renderVertices、materials、textures、bones等）が保存前に読み取りやすくなり、unknown時の理由も同じ欄へ表示する。ドキュメントや制作データは変更しない。

Unity 6000.4.3f1のWindows Player `Builds/ValidationUiV1/NyaForge.exe` をビルドし、1280x800 Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-134900-bff15d3d95da44119750676e20bff74a/report.json`、画面 `authoring.png`）。同じ成果物のUnity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-134937-440-25ae2cadbe9042eb8525367e45f91011/bridge-report.json`）。Coreは変更なしで直近 **471 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-ffdc185b64364eaca06099e90ddc39fb`）。画像は自動Playerの初期制作画面で、実マウス・DPI個体差の受入とは分けて扱う。
# 2026-09-13 validation object count

出力チェックの`metrics`へ制作対象object数を追加し、GUIの縦型結果表示でも先頭に表示するようにした。複数objectを合算した判定であることを画面上でも確認できる。Coreの検証は **471 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-ba80481a8387490f8bec0b941183a9dd`）。

Unity 6000.4.3f1のWindows Player `Builds/ValidationUiV2/NyaForge.exe` をビルドし、1280x800 Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-135121-c60399d2161043dc8320f267b52fc99e/report.json`、画面 `authoring.png`）。同じ成果物のUnity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-135154-187-26dfd082c99b4bd3a373c6250f6d83d9/bridge-report.json`）。
# 2026-09-13 validation UI real-model recheck

最新 `Builds/ValidationUiV2/NyaForge.exe` で、private一時RadDollV3 VRM（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`）を使った候補選択→EditMesh頂点編集→native Save/Open→標準skinned GLB出力→再取込を再確認した。Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-135407-87d2b19633744c218be890edbf327601/report.json`、画面 `authoring.png`）。同成果物のUnity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-135526-451-d3d9354f898c4191ac9cd27b56408543/bridge-report.json`）。private素材はpublic repositoryへ追加していない。これは自動Player経路の証拠で、実マウス・DPI差・実VRChat内受入とは分けて扱う。
# 2026-09-13 validation UI all-mesh recheck

最新 `Builds/ValidationUiV2/NyaForge.exe` でprivate一時RadDollV3 VRMの **全mesh instance取込** を実行し、body・hair等を複数graph objectとして公開した。全objectの頂点編集、native Save/Open、feature-preserving native export、拡張skinned GLB出力と再読込までのAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-135624-c8605946d44b464c94e50b95aa4a59d7/report.json`、画面 `authoring.png`）。同成果物のUnity **2022.3.22f1** Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-135831-583-5cc12364c6b6485c8902bb5320ae5c3e/bridge-report.json`）。private素材はpublic repositoryへ追加していない。
# 2026-09-13 per-object validation details

出力チェック結果に、制作対象ごとの状態と規模を追加した。集計の`metrics.objects`に加えて、結果JSONの`objects`へ`objectId`、`status`、`triangles`、`renderVertices`、`materials`、`textures`を保存する。未完成またはメッシュなしの対象は`status: unknown`として原因候補を残し、完成対象は評価済みメッシュの実測値を記録する。判定ロジックと合計値は変更していない。

Unityの出力チェック画面にも「対象別」欄を追加し、対象ID先頭8文字、状態、頂点数、三角形数を縦に表示する。これにより、複数オブジェクトの合計だけでは分からなかった「どの衣装／小物が未完成か」を画面上で確認できる。Core回帰では2オブジェクトの個別結果と合計値を固定した。

Coreは **471 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-3d71370b6873449e9888a1fe109e47ec`）。Windows Player `Builds/ValidationObjectsV1/NyaForge.exe`のAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-140207-6001fe322ecb477cb9333b5a243c6b94/report.json`、画面`authoring.png`）。Unity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-140240-211-18db2ee56ab14264af3fc677cb032408/bridge-report.json`）。実マウス、DPI個体差、実VRChat内の見た目・挙動は別の手動受入境界とする。
# 2026-09-13 initial VRM 1.0 humanoid export profile

標準VRM出力の最初の実装単位として、既存のrest-pose skinned GLBを基礎に`VRMC_vrm` extensionを付ける`VrmExportService`を追加した。WorkbenchのVRM1ボタンは、取込時のgraph単位`ImportedRigSession`をstable `BoneId`で出力GLTF nodeへ解決し、作品名・作者・作品license URLを明示して`model.vrm`を作る。1つのskinned avatar graph object、VRM 1.0 humanoid必須15骨、絶対HTTP(S) license URLを満たさない場合は出力先を作らず拒否する。

このprofileはmeta・humanoidと、MorphSetへ解決できるmorphTargetBindsを出力する。material bind、texture transform、LookAt、FirstPerson、SpringBone、MToon、animation、任意VRM拡張は未出力で、`export-report.json`へ制限を記録する。未解決の表情targetは黙って落とさず拒否する。既存のskinned GLBのBIN／geometryを再利用し、CoreでVRM extension再読込とmesh頂点・三角形数の一致を固定した。仕様書とAuthoring READMEにもこの境界を追記した。

Coreは **473 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-574e8b4ed9954863b12b364c0311e4eb`）。新規回帰はVRM1 packageの`VRMC_vrm`・meta・humanoid・morphTargetBinds再読込とGLB geometry不変、および`VrmExportService`の`model.vrm`／レポート生成を検証する。Windows Player `Builds/Vrm1HumanoidV2/NyaForge.exe`のAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-141819-d7cba444ef044b1c8daf70bd9cc13dfe/report.json`、画面`authoring.png`）。Unity **2022.3.22f1** synthetic Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-141855-421-2d8df4c7896b48e8bd7635873f296452/bridge-report.json`）。実RadDollV3でのVRM1出力、UniVRM／VRChatでの受取・外観・挙動は未検証境界とする。
# 2026-09-13 P1: 保存・skinned出力・ウェイト表示の整合性修正

古いcommit基準のレビューで再現された4件を現行mainへ再照合し、次を修正した。

- 複数objectのOpen時に、active graphの`ImportedRigSession`・Expressions・Springを同じGraphIdから解決する。legacy単一sessionを別objectのmetadataへ照合しない。
- source skinのinverse-bindをsource joint slot順のまま渡さず、出力skeletonのstable `BoneId`順へ並べ替える。対応できない骨は出力前に停止する。
- source nodeのworld matrixから、出力skeleton親子に合わせたjoint local matrixを構成してGLBへ出力し、元の回転・拡縮・中間helperを保持する。matrixがない制作graphは従来のrest-derived translationを使う。
- source skin表示を取込時の古いbindingで固定せず、現在の`SkinBind`評価結果をsource slotへ変換してから下流graphを再評価する。ウェイト編集後の表示と出力対象が一致する。
- VRM 1.0出力へ、詳細形状と`gravityDir`が揃ったVRM 1.0由来sessionに限り`VRMC_springBone`（sphere/capsule、collider group、spring joint）を追加した。VRM 0.x、詳細不足、空group、未解決BoneIdは黙って変換せず拒否する。

Coreは **474 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-ac362427c5f54bdb8a20ef3a6200159b`）。新規回帰はVRM SpringBone packageの再読込とshape保持を確認した。Unity 6000.4.3f1のWindows Player `Builds/P1FixV4/NyaForge.exe` はコンパイル・ビルド成功。Authoring全件自動suiteは600秒設定で **PASS**（`Artifacts/Authoring-20260913-144051-aa589a686bcd48f4a8893525316f52ea/report.json`、`authoring.png`）。同成果物のUnity 2022.3.22f1 Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-144125-134-189a9721c1684d6cbc8707751a808f89/bridge-report.json`）。

未完了境界は、実モデルでの今回の保存後matrix／ウェイト編集目視、実VRChat／UniVRM受入、VRM material bind・LookAt・FirstPerson・MToon・animation・任意拡張、異なるskeleton結合、自動fit・貫通修正。次回は短い専用fixtureでP1の保存→再読込→GLB再取込をPlayer検証し、長時間suiteと分けて証拠化する。

# 2026-09-13 P1修正後のprivate RadDollV3再確認

public repositoryへ素材を追加せず、`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`を入力にして、今回のP1修正後Playerで候補選択・編集・native Save/Open・標準skinned GLB出力・再取込を実行した。Windows Authoring suiteは **PASS**（`Artifacts/Authoring-20260913-144320-e0bdb1af8a344640884475842253746f/report.json`、`authoring.png`）。同成果物のUnity 2022.3.22f1 Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-144450-130-658ed8b59fbb4075ace026dd047b0161/bridge-report.json`）。これはprivate実モデルでの自動確認で、骨回転・拡縮と編集ウェイトの個別目視、実VRChat／UniVRM受入は引き続き別境界とする。
# 2026-09-13 VRM1実モデル書き出し確認とWindows一時パス修正

実RadDollV3 VRM（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-RealModelSmoke/RadDollV3_VRM.vrm`）を使い、候補選択→EditMesh頂点編集→native Save/Open→標準skinned GLB出力→再取込に続けて、保存済みプロジェクトを再オープンしたVRM 1.0出力を確認した。出力は `model.vrm` と `export-report.json` を持つ一つのrevision-pinned directoryとなり、`VrmMetadataReader`で`vrm1`・humanoid 29 nodeを再読込できた。RadDollV3はVRM 0.xのため、製品側の方針どおりVRM 0.x SpringBoneの自動変換はせず、検証ではそのsessionを明示的に除外した（出力reportの`springBone:false`）。

この確認中、長いfixture／projectパスにVRM用のsource GLBとstaging suffixを二重に付けると、GLB report書込みがWindowsのパス長制約で失敗することを再現した。`VrmExportService`の一時source／stagingを要求先の隣接ディレクトリに短い名前で作り、最終出力だけを指定先へ移動するよう修正した。失敗時はstagingを削除し、既存の出力先を作らない契約を維持する。

Core **474 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-854a809c2f964cc992be0c79f5e296f9`）。Unity 6000.4.3f1の最終Player `Builds/Vrm1RealFinal/NyaForge.exe` はビルド成功（`Logs/build-player-20260913-150428-392.log`）。実モデルのVRM1付きAuthoring suiteは `Builds/Vrm1RealV6/NyaForge.exe` で **PASS**（`Artifacts/Authoring-20260913-150125-27e7a01df65a458bbfee2ed5eec5fd18/report.json`、VRM出力を含む85 checks）。

これはVRM1パッケージの実モデル自動確認であり、UniVRM／VRChat SDKへの受け取り、実VRChat内の見た目・SpringBone挙動、material bind・LookAt・FirstPerson・MToon・animation・任意拡張の完全出力、実マウス／DPI差の受入ではない。
# 2026-09-13 静的GLB表示形状の整合性修正

レビューで指摘された「表示形状の静的GLBが表示と違う」経路を修正した。Workbenchの静的GLB出力とMCP静的GLB出力へ、source skin表示補正済みのgraph meshをobject単位で渡す`ExportStaticWithOverrides`を追加し、現在のSkinBind weightを使ってsource palette補正を再計算する。これにより、取込後の頂点編集・weight編集を含む最終表示形状を静的GLBへ反映する。材質slot出力はprimitiveごとに参照頂点を局所リマップするため、再読込時の共有頂点数が変わる場合を許容し、実モデル検証ではsubmeshごとの三角形座標を比較する。

Coreは **475 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-61489347c8a14bda811fa7cce7e2e1e6`）。`static GLB export accepts a display-corrected mesh override` 回帰を追加した。Windows Player `Builds/StaticDisplayV3/NyaForge.exe` はビルド成功（`Logs/build-all-20260913-151801-205.log`）。private一時RadDollV3 VRMで、候補取込→EditMesh頂点編集→native Save/Open→標準skinned GLB再取込に加え、静的GLBの表示補正済み三角形座標照合まで含むAuthoring suiteが **PASS**（`Artifacts/Authoring-20260913-151822-e69b71eec5214ac9a17d65808cdb916b/report.json`、画面`authoring.png`）。

今回の自動確認はCoreとWindows PlayerのCPU／GLB往復であり、実マウス・DPI差、UniVRM／VRChat SDK受け取り、実VRChat内の外観・挙動は別の手動受入境界として残す。private素材はpublic repositoryへ追加していない。

追補: 同じPlayer check directoryを入力にUnity 2022.3.22f1 Bridgeも **PASS**（`Artifacts/BridgeReceiver-20260913-152230-116-37b90f8d982042ad908a9036375297c3/bridge-report.json`）。

# 2026-09-13 装着後の面選択座標修正

チョーカーなどのrigid attachment後に面クリック判定がずれるP2を修正した。`PickFace`のレイ判定へ、描画と同じ`OwnedMeshProjection.WorldPoints`を渡し、装着rootの位置・回転を含むワールド座標で三角形を検査する。編集用のpolygon face ID対応は従来どおり維持する。

Unity 6000.4.3f1のWindows Player `Builds/StaticDisplayFinal/NyaForge.exe` はビルド成功（`Logs/build-all-20260913-152337-934.log`）。private一時RadDollV3 VRMを使ったAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-152359-5a2f78a3cbc740d19ef14517744e2a64/report.json`、画面`authoring.png`）。静的GLB表示形状照合を含む前回のBridge検証もPASS済み（`Artifacts/BridgeReceiver-20260913-152230-116-37b90f8d982042ad908a9036375297c3/bridge-report.json`）。

# 2026-09-13 最終結果projectionの二重Transform修正

レビューで指摘されたFrame中心／ズームずれを再確認し、`FinalResultProjection`の二重Transformを修正した。以前は頂点へ`RestTransform`を適用した`Points`を、同じ変換を持つrootで再度変換していた。現在はmesh頂点をローカル座標で保持し、root Transformを一度だけ適用する。attachment時は親のrigid poseをそのまま継承するため、位置・回転・拡縮が二重適用されない。Frameのboundsにも同じWorldPointsを使う。

Unity 6000.4.3f1のWindows Player `Builds/FrameFixV1/NyaForge.exe` はビルド成功（`Logs/build-all-20260913-152738-593.log`）。private一時RadDollV3 VRMでのAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-152800-7a096f00b6a4406aadd30f5653c68d14/report.json`）。1000x700のNavigation suiteも **PASS**（`Artifacts/Navigation-20260913-152926-06083e8c9bda4195b974481f8094d716/report.json`、`settings.png`で文字欠けなしを確認）。

# 2026-09-13 FrameFixV1 複数mesh実モデル再確認

Frame修正後の最新Playerで、private一時RadDollV3 VRMの全mesh instance取込を再実行した。body・hair等を複数graph objectへ展開し、各EditMesh、native Save/Open、graph-keyed metadata、feature-preserving native exportまで含むAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-153007-1f6a00200ada48a3949c7ecc8d206868/report.json`）。同じcheck directoryを使ったUnity **2022.3.22f1 Bridge**も **PASS**（`Artifacts/BridgeReceiver-20260913-153215-866-247a7f0038f04e1985530cd1123e3031/bridge-report.json`）。

# 2026-09-13 ドキュメントのVRM出力範囲同期

`docs/Authoring-Quickstart.md`の古い「標準VRM export未実装」という記述を、現行実装へ更新した。現在はVRM 1.0の初期profile（humanoid/meta、解決可能なmorph bind、詳細付きVRM1 SpringBone）を出力できる。一方、material bind・LookAt・FirstPerson・MToon・animation・任意拡張と、VRM 0.xの自動変換は引き続き対象外で、完全VRM出力とは扱わない。

# 2026-09-13 VRM1出力手順のQuickstart追記

現行WorkbenchにあるVRM 1.0 metadata入力と`VRM 1.0（humanoid）`出力ボタンの操作手順を`docs/Authoring-Quickstart.md`へ追加した。1つのskinned avatar graph、15必須humanoid骨、rest pose、license URL、出力先`exports/vrm1-日時-ID/`、SpringBone／未対応機能の境界を明記した。実VRChat／UniVRM受取確認は引き続き別受入項目である。

# 2026-09-13 Development-Plan現行到達点の同期

開始時点の比較表と現行mainの能力が混同されないよう、`docs/Development-Plan.md`へ現行到達点を追加した。実装・自動検証済みの範囲（graph／直接編集／Rig・weight／GLB・VRM取込／atomic全mesh／保存再開／GLB・初期VRM1出力／MCP／Bridge）と、手動受入・実VRChat受取・完全VRM・FBX/BLEND・自動fitを未完了境界として明記した。

# 2026-09-13 面選択のrender頂点／UV seam対応

面クリック判定を編集点配列から分離し、`PolygonRendering.Mesh`のrender頂点を使うよう補強した。UV seamで同じ制作頂点が複数render頂点へ分割される場合も配列範囲を誤らず、装着rootのワールドTransformを描画と同じく一度だけ適用する。Quickstartへ挙動を追記した。

Unity 6000.4.3f1のWindows Player `Builds/FacePickFixV2/NyaForge.exe` はビルド成功（`Logs/build-all-20260913-153738-772.log`）。private一時RadDollV3 VRMを使うAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-153802-08b46c08414f48148c6070fba145c34b/report.json`）。1000x700 Navigationも **PASS**（`Artifacts/Navigation-20260913-153928-dc07eec7a79c4c0ea09f7c2534f038aa/report.json`）。

# 2026-09-13 面選択ワールド頂点キャッシュ

高密度モデルでの軽量性を保つため、面クリックごとのrender頂点ワールド座標配列生成をやめ、`OwnedMeshProjection`構築時に`RenderWorldPoints`をキャッシュするようにした。UV seam分割を含むrender domainと、頂点編集用の`WorldPoints`を別々に保持し、通常クリックは追加割り当てなしで判定する。projection再構築（mesh／Transform／attachment変更）時だけキャッシュを更新する。

Coreは **475 passed / 0 failed**（`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-6d3e3e05bc934e43900a2ccad27b6101`）。Unity 6000.4.3f1のWindows Player `Builds/FacePickPerfV1/NyaForge.exe` はビルド成功（`Logs/build-all-20260913-154110-759.log`）。RadDollV3全mesh取込を含むAuthoring suiteは **PASS**（`Artifacts/Authoring-20260913-154132-e96fc21de38f4fa59f67b3b123813c5a/report.json`）。1000x700 Navigationも **PASS**（`Artifacts/Navigation-20260913-154343-66577ae7461f400d9b3d0249ef459056/report.json`）。

# 2026-09-13 Save/Recovery crash検証

保存・再開の安全性を強めるため、最新のFacePickPerfV1全mesh実モデルcheckを入力に`Test-NyaForgeCrashRecovery.ps1`を実行した。Unity 2022.3.22f1 Bridgeの通常検証に加え、materials／prefab／receiptの更新途中停止後にプロセスを再起動し、recovery状態を再読込できることを確認した。Bridge reportは **PASS**（`Artifacts/BridgeReceiver-20260913-154451-682-e8552774ca7b4d10ad51b1d0fc684fe3/bridge-report.json`）。recovery証跡3件（`materials-recovery.json`、`prefab-recovery.json`、`receipt-recovery.json`）もすべて **PASS**。元のチェック成果物・制作データは変更していない。
