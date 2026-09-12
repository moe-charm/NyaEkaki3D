# Unity再取り込みと生成物の所有

2026-09-12。設計v2の再出力identity要件（第467行付近）を具体化する。既存参照を保つ更新が目標。初回importの所有記録、明示出力ID、読取専用の更新先照合まで実装。複数材質の更新適用と例外rollbackを追加した。残件は末尾参照。

## 現在の所有記録

初回import完了時に `NyaForgeImport.json` を保存する。文書UUID、現在の単一object UUID、revisionと、生成assetの相対path・Unity GUID・内容hash・meta hashを保持する。hashは変更検出に使用し、asset identityとして使用しない。Prefab、mesh、材質、PNG、部位対応表を記録し、receipt自身は対象外。

`ImportOwnership.Inspect` はファイル欠落、GUID対応変更、内容/meta変更、ロード済みassetのdirtyを返す。追加された未管理ファイルを削除しない。現状はPrefab全体の変更を検出するため、利用者が追加したcomponentも変更候補になる。これは競合調停の完成ではない。

Import画面の「既存インポートの確認」でフォルダを選び、「生成後の変更を調べる」で一覧を表示する。古いreceiptなしimportの所有をファイル名から推定しない。receiptの不正な相対pathや重複identityは拒否する。receiptはローカルの比較記録であり、改竄不能な権限証明ではない。

## 更新実装の順序と境界

1. 実装済み: 各Bakeに `<manifest名>.identity.json`（schemaVersion1）を同梱する。document/object/output UUID、outputKind、revision、manifestHashを保持。graphはOutputNodeId、staticはobjectIdを明示。既存Bakeのschemaは変更しない。receipt v2はこの出力IDを保存し、v1は未識別として読む。
2. 読取専用の更新候補を構築する。新Bakeを全検証してから既存receiptと照合し、追加・変更・削除・競合を表示。revisionだけで内容の一致を判定しない。別作品、別出力、Unity側の変更は自動適用しない。
3. 更新transactionは初回importと分離する。既存Mesh/Material/TextureのGUIDを保持し、更新対象を個別にバックアップ。commit直前にも比較をやり直す。失敗時は既存資源を復元し、新規生成物のみを除く。既存フォルダ丸ごとの削除は禁止。
4. Prefabの管理フィールド（mesh/material参照等）だけを更新し、利用者のcomponent、Transform設定、子object、scene overrideを保持する。管理フィールドの外部変更と削除候補をpreviewへ出す。所有外の設定を全体serialized copyで置換しない。
5. 成功した資源群を保存してからreceiptを更新する。例外・保存失敗・中断後の復旧を検証する。GUIから候補確認と適用へ接続する。

## 検証

Unity2022.3.22f1で初回receiptと文書/object/revisionの一致、未保存材質変更、保存済み変更、元bytes復元、traversal拒否を確認。既存mesh/Surface/単一材質/複数材質の初回importも回帰。詳細な証拠パスはcurrent_taskに保持する。

残件はGUIDを保持する実更新、更新中rollback/復旧、component等の選択的保存、出力schema移行、GUI操作検証。現在の緑のテストはこれらの完了を意味しない。

## 明示出力IDと候補照合

manifestとidentity sidecarは別ファイルで、manifestを書いた後にsidecarを置換する。二ファイル全体の原子性は主張しない。sidecar欠落はlegacy扱いになり、自動更新候補は拒否。sidecarが存在してhashや文書revisionが一致しなければ初回importも拒否する。出力フォルダ全体を保持する。旧readerは従来どおりmanifestを読める。

`BakeUpdatePreview.Materials` は複数材質Bakeを全読取し、所有receiptとdocument/object/output/kind、revision、manifest hashを照合する。欠落identity、別出力、旧revision、既存資源変更をconflictsへ返す。Import GUIの「選択Bakeと更新先を照合」で表示する。資源追加/削除の一覧、profile変更の調停、適用直前の同時変更再検査はまだ未実装。このpreviewに競合がないことは、適用可能性や更新完了の証明ではない。
## 更新適用の現在地（2026-09-12）

`BakeImporter.UpdateMaterials` を追加。staging新規import→直前再照合→owned bytes/meta退避→Mesh/材質/画像→Prefab参照→slot map→receiptの順。GUIは照合後「照合した出力を反映」で実行する。旧GUIDを使うMesh/材質/画像を更新し、Prefabは管理参照のみを更新する。不要資源は保持する。receipt更新は旧owned集合と新規資源に限定する。

Unityで二材質/二画像の変更とGUID保持、4段階の例外rollbackを検証。GUI手操作、新規材質/画像共有分岐、幾何変更、未管理componentを持つPrefabの更新は追加検証が必要。現ownership検査は外部Prefab変更を拒否するため、未管理componentの選択的保持は次の実装対象。プロセス中断に耐える永続journalと復旧はまだなく、今回のrollbackは通常例外のみ。
## Prefabの選択的照合（receipt v3）

`PrefabManagedBindings` が管理対象のroot GameObject/MeshFilter/MeshRendererのlocal file IDと、mesh/material参照のGUID+local IDをfingerprint化する。rootの識別を保ち、参照数・順序・差替えも検出する。meta/GUID変更は従来どおり競合。Prefabのそれ以外の保存済み変更は許容する。v1/v2のreceiptは全file比較を維持し、baselineのない管理境界を推測しない。

ユーザーのTransform、Renderer.enabled、子object、Light設定を追加したPrefabで、更新成功と4段階rollbackを確認した。管理mesh参照の差替えは拒否、未管理txtは残して新receiptへ所有登録しない。root名はUnityが実際に保存した名前を基準に検証する。scene override/variant/nested prefabと未保存Prefab Stageの操作テストはまだ残る。以前の全Prefab変更拒否という制限はreceipt v3で解消した。
## 永続journal（2026-09-12）

`UpdateJournal` を `Library/NyaForgeUpdates/<UUID>/` に保存する。変更前のファイル/metaをflushし、journalをatomic置換してからmutationへ進む。commit後も記録を保持。既存transactionはactive.lockで同時復旧を防ぐ。復旧は全backup/hash/対象のGUIDとproject/folderを先に検証する。元fileのatomic置換と新規owned assetの限定削除を行い、成功後にrecovered印を保存する。

新規assetのGUID確認前に中断した場合は、そのpathを自動削除しない。未確認の所有と表示して記録を残す。元フォルダそのものは削除しない。Libraryを削除するとbackupも消えるため、pending中は保持が必要。古い完了記録の整理機能は未実装。

Editorロード時にpendingをログ通知し、`Tools > NyaForge > Recover Interrupted Updates...` で対象を選び復元する。復元は更新開始後に保存した変更も巻き戻すため、対象を表示し明示ボタンで実行。自動書換えはしない。dirty資源がある場合は停止する。

disk-only再読取、backup破損拒否、正常backupへ戻した後の再試行、GUIDとbytes/metaの一致、二重復元、通常例外rollbackを検証済み。実プロセス強制終了/別Editor再起動、復旧途中の終了、disk障害とGUI手操作は未検証。従来のメモリbackupのみという制限は解消したが、全中断点の復旧保証はまだない。

## 実プロセス再起動検証（2026-09-12）

`Tools/Test-NyaForgeCrashRecovery.ps1` が新規receiverを作り、明示sentinelを確認するtest Entryで自分のprocessだけをKillする。materials/prefab/receiptの3地点で中断し、それぞれ別PIDで再起動して旧file集合/bytes/metaと所有照合の復元を確認した。最新resultはcurrent_task参照。二重復旧も検証する。

Unityが保持するファイルとの共有違反に対応するため、復元は同内容skip→atomic replaceを優先→非metaのみflush付き上書きfallbackを使う。全assetのatomic書換えとは主張しない。OS電源断、復元途中Kill、GUID未確認の新規資源は残件。Kill後にはstaging importが残るので、journalにstaging所有を記録して掃除する処理を次に追加する。

## stagingの所有と掃除（journal v2）

`StagingOwnership` が初回importで作った一時フォルダのGUIDと全file hashを記録する。復旧後に一致を確認して限定削除し、外部変更があれば削除しない。commit済みjournalでもstagingが残る場合はPendingへ表示し、更新先を復元せずcleanupする。通常finallyも同じ確認を使う。

3地点の実プロセス中断/再起動で、復元後のAssetsフォルダ集合が開始前に一致することを確認した。journal作成前中断、復旧中断、外部変更/cleanup失敗とcommit直後中断はまだ追加試験が必要。File.Replaceの一時的な失敗には有界な再試行を行う。

## 資源計画と復旧再試行（2026-09-12）

`MaterialUpdatePlan` をpreviewと適用で共有する。材質/画像のpathを計画時に決め、追加・更新・保持をGUIへ表示。source/target hashに加えて資源計画のComparisonKeyをcommit前に確認する。旧資源は削除しない。新規材質UUIDへの交換で、新材質/PNGの追加と既存資源保持、preview/apply一致、追加中失敗のrollbackを検証した。

強制終了試験は更新中→復旧中→最終復旧の3processを通す経路を追加。復元した材質と、まだ未復元のfileがあるcheckpointでKillし、さらに別PIDからの復旧完了と全file/meta一致を確認。途中stop markerのpassed=falseは成功判定用reportではなく中断証拠。最終recovery reportを確認する。

画像共有の分岐/再共有と幾何/slot数変更は以下の追加検証で確認。scene overrideとEditor GUI手操作は引き続き残る。

## 共有画像・形状更新の検証（2026-09-12）

`SharedImageUpdateVerification` は共有PNG→独立PNG→再共有を通し、分岐時の追加がPNG1個であること、再共有時の未使用PNG保持、材質/GUID/PNG bytesとPrefab参照を照合する。入力manifestを変更するreceiver fixtureであり、GUIからの書き出し操作は対象外。

`GeometryUpdateVerification` はCore graphのSource変更commandから正規Bakeを生成し、submesh数1→3→2と位置・頂点/index数を変更する。受取meshの位置・UV・indices・生成normalとPrefabの材質順序、既存mesh/prefab GUID、未使用材質の保持を確認する。画像なしで検証しており、Paint再投影の対応を意味しない。

両moduleを既存Bridge suiteへ接続。成功report: `Artifacts/BridgeReceiver-20260912-033631-177-a3094b2c12fb40238ae1a8d14c59ca15/bridge-report.json`。復旧suiteの追加反復ではなく、次は造形/UV編集へ進める。
