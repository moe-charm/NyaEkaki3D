# Rig / VRM 実装レビュー（2026-09-12）

対象コミット: `bb1d89cf2c076e310620298f2775527eb53142f3`。ユーザー依頼により、直近のSpringBoneコア、VRM metadata / expression / SpringBone sessionとWorkbenchの保存・終了経路を確認した。修正タスクの状態管理は [current_task.md](../../current_task.md) を正本とし、この文書には再現条件と完了条件を残す。製品全体の受入レビューではない。

## 判定と検証範囲

Unityに依存しないRigコアとimport / runtime adapterの分離は維持できている。一方、現在のVRM読込・保存とSpringBone計算には、次のGUI接続前に修正すべき不具合がある。現状を実VRMで使えるSpringBoneとして合格とはしない。

- 現行Core suiteを再実行し **297 passed / 0 failed**。出力先は `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-5118069e60f24b96af535ddcd6481513`。
- その後、独立した再現プログラムで作者情報、session往復、保存失敗、連続step、親子chain、コライダー分離・貫通、時間停止を検査した。以下の数値・例外は現在のソース／そのビルドで再現した。
- Unity Playerを新たに起動する検証、保存して終了の実クリック、実VRMの全体スキーマ適合・見た目受入は今回行っていない。保存失敗後のdirty値は実測し、その後の終了可否はWorkbenchコードで確認した。
- 本番コードと既存テストは変更していない。再現プログラムは追跡除外の `Artifacts/Review-20260912-*` に保存した。

P1は読込・保存またはSpringBoneの基本契約に関わる優先修正、P2もruntime接続前の修正対象とする。既存のPlayer / Bridge成功は過去の回帰記録として有効だが、今回の不具合がないことを示す証拠ではない。

## R01 / P1: VRM 1.0のauthors配列を読めない

対象: [VrmMetadata.cs:148](../../Assets/NyaForge/Authoring/Import/VrmMetadata.cs#L148)、[VrmMetadataTests.cs:12](../../Tests/Authoring.Core/VrmMetadataTests.cs#L12)。

`meta.authors`を`OptionalString`へ渡している。`authors: ["Nya"]`で `VRM text property is invalid: authors` を再現した。[公式meta schema](https://raw.githubusercontent.com/vrm-c/vrm-specification/master/specification/VRMC_vrm-1.0/schema/VRMC_vrm.meta.schema.json)ではauthorsは必須の文字列配列である。現在のテストが文字列 `"Nya"` を与えるため、この入口の不具合を見逃している。再現fixtureはこの読込経路を切り出した最小データで、実VRM全体の検証ではない。

完了条件: 正規のauthors配列を読み、複数作者とその順序を欠落なく保持する。VRM0のauthor単独文字列との対応、UI表示、expression / Spring sessionの保存・再読込を確認する。保存形式を変えるなら既存sidecarの移行を明示する。空配列や型違いの扱いも公式仕様と選択した対応範囲に合わせ、誤った既存fixtureを修正する。

## R02 / P1: 同じnodeの複数コライダーを保存後に復元できない

対象: [VrmSpringSession.cs:66](../../Assets/NyaForge/Authoring/Import/VrmSpringSession.cs#L66)、同ファイルの`IntArray`（79行）、[VrmMetadata.cs:179](../../Assets/NyaForge/Authoring/Import/VrmMetadata.cs#L179)。

1つのnodeに2つのsphereを置くと、metadataはコライダーごとのnode配列 `[0,0]` を作り、codecのWriteは成功する。Readは重複禁止の`IntArray`を使うため `VRM SpringBone session array item is out of range: nodes` で失敗する。WorkbenchのOpenもsidecar読込完了後に置き換えるため、作品を開く経路が止まる。

完了条件: コライダーごとのnode列は順序・重複を保持し、一意性が必要な参照集合とはreaderを分ける。VRM0の同一node複数sphere、VRM1の同一node複数shape・異なるnodeの混在についてimport→session Write→Read→WriteとWorkbench Save/Openを確認する。単純な重複除去でコライダー件数を減らさない。

## R03 / P1: sidecar保存に失敗しても保存済みとして終了できる

対象: [AuthoringWorkbench.ProjectActions.cs:52](../../Assets/NyaForge/UnityRuntime/AuthoringWorkbench.ProjectActions.cs#L52)、[AuthoringWorkbench.cs:350](../../Assets/NyaForge/UnityRuntime/AuthoringWorkbench.cs#L350)、[ProjectStore.cs:51](../../Assets/NyaForge/Authoring/ProjectStore.cs#L51)。

`ProjectStore.Save`が先にmanifestを公開し、SaveVersionとSavedStateHashを更新する。その後のexpression / Spring sidecar保存で失敗しても、この更新は戻らない。専用一時projectでsidecarのファイル名と衝突するディレクトリを作ると、保存はIOExceptionになるが `IsDirty=False / SaveVersion=1` になった。`SaveProject`は`Try`で例外を吸収し、「保存して終了」は`!HasUnsaved`のみで終了を決める。したがってコード上、設定が未保存のまま終了できる。実際の終了クリックは未実施。

完了条件: project本体と所有するVRM設定を同じ保存成功契約へまとめる。全体成功前に未保存表示を解除せず、失敗した「保存して終了」は終了しない。単にsidecarを先に書く順序変更だけで済ませず、旧manifestと設定の組を壊さない公開・復旧方法と、writer lock / 保存versionの境界を設計する。expression保存失敗、Spring保存失敗、Save As途中失敗、再試行を検証し、既存保存先でも旧作品を復元できることを示す。

## R04 / P1: 2フレーム目の出力姿勢とシミュレーション状態が一致しない

対象: [SpringBone.cs:170](../../Assets/NyaForge/Authoring/Rig/SpringBone.cs#L170)。

前stepのtail方向から次tail方向への差分回転を、今回入力されたbase poseへ掛けている。同じbase pose、gravity=8で+X、dt=0.1秒、前回Stateを次に渡すと、step2で出力PoseのtailとStateのtailが **0.07980909 m** ずれる。step1の誤差は0.00000059 mであり、1stepだけのテストでは検出しにくい。

完了条件: 入力poseの実際の軸から次tailへの回転を計算し、base poseと物理状態の契約を明示する。連続stepとbase pose変化の両方で、返したPoseから算出したtailがStateと許容誤差内で一致する。入力pose・前Stateの不変性も維持する。

## R05 / P1: 親の変形が子へ伝わらずchainが離れる

対象: [SpringBone.cs:156](../../Assets/NyaForge/Authoring/Rig/SpringBone.cs#L156)。

各jointを元のabsolute poseで独立に評価しており、親の更新や`ParentBoneId`を子のheadへ反映しない。接続した2boneの両方を登録し、親だけにgravity=8で+X、dt=0.1秒を与えると、親tailと子headに **0.07980748 m** の隙間ができる。SkinDeformerはabsolute poseを受け取るため、後段が自動で修復する構造でもない。

完了条件: 親から子の順に評価し、元poseにある相対変換を保って親の変更を伝える。接続した2〜3joint、登録順の違い、親の移動・回転、必要な非simulated子孫の追従を確認する。骨のhead/tailがもともと一致しない場合は、初期offsetを保持し、すべての子headを機械的に親tailへ寄せない。

## R06 / P2: 別chainのコライダーが全jointへ適用される

対象: [SpringBone.cs:195](../../Assets/NyaForge/Authoring/Rig/SpringBone.cs#L195)、同ファイル167行。

全chainのColliderGroupIndicesを1つのHashSetへ結合し、それを全jointで使う。Aだけにgroup 0を指定し、Bには参照を持たせない比較で、BはAが存在するときだけ **0.290426 m** 動いた。髪束等ごとの衝突対象の分離が成立しない。

完了条件: jointと所属chainのコライダー参照を一緒に保持して評価する。参照なしBの結果が、Aの追加・削除・コライダー変更で変わらないことを数値検証し、明示的な共有groupは両chainへ作用することも確認する。

## R07 / P2: 長さ制約でtailをコライダー内へ戻してしまう

対象: [SpringBone.cs:167](../../Assets/NyaForge/Authoring/Rig/SpringBone.cs#L167)。

衝突による押し出し後、長さだけを再制約する。head=(0,0,0)、length=1、tail=(0,1,0)、sphere center=(0,1.2,0)、radius=0.4では、最終距離が **0.200000 m** に戻り、必要な0.4 mを満たさない。(1,0,0)等の解が存在するため、制約自体が不可能な例ではない。

完了条件: 長さと衝突を同時に満たすbounded solverへ分離する。実際に侵入する初期値、同軸／中心一致、hitRadius、複数コライダーを検証する。反復上限と、解がない／収束しない場合の結果・診断を決め、貫通した状態を解決済みと扱わない。

## R08 / P2: dt=0でも慣性が進む

対象: [SpringBone.cs:162](../../Assets/NyaForge/Authoring/Rig/SpringBone.cs#L162)。

dt=0で力は0になるが、慣性変位はそのまま加算される。1step動かした後に0秒stepを呼ぶとtailが **0.07930448 m** 進み、履歴も更新される。停止中の再描画で動く実装につながる。

完了条件: base poseが同じでdt=0なら物理状態と慣性履歴を進めない。base poseだけ変わる場合の描画更新と物理積分を分ける。固定step / substepか前回dt補正かを決め、可変フレーム時間・一時停止・再開について数値検証する。

## R09 / P2: VRM1の省略パラメータが誤った値で保存される

対象: [VrmMetadata.cs:171](../../Assets/NyaForge/Authoring/Import/VrmMetadata.cs#L171)、同ファイルの`OptionalNumber`（265行）。

省略したstiffnessとdragForceがどちらも0になる。[公式joint schema](https://raw.githubusercontent.com/vrm-c/vrm-specification/master/specification/VRMC_springBone-1.0/schema/VRMC_springBone.joint.schema.json)の既定値はそれぞれ1.0と0.5である。R01だけを迂回するため作者を現readerが通す文字列にした切り出しfixtureで **stiffness=0 / drag=0** を確認した。runtimeへ渡す前に既に意味が変わり、sidecarにもそのまま保存される。

完了条件: 最小値と省略時の既定値を別引数／処理として扱う。正規authorsのVRM1について、省略時に1.0/0.5、明示的な0は0のまま保持し、session往復後も一致する。VRM0とVRM1の既定値を一括で推測せず分けて確認する。

## R10 / P2: 入力検査と、実際に問題経路を通る回帰テスト

対象: [SpringBone.cs:195](../../Assets/NyaForge/Authoring/Rig/SpringBone.cs#L195)、[SpringBoneTests.cs:29](../../Tests/Authoring.Core/SpringBoneTests.cs#L29)、[SpringBoneTests.cs:47](../../Tests/Authoring.Core/SpringBoneTests.cs#L47)。

参照先groupがnullでも初期化は通り、Stepで`NullReferenceException`が発生した。また現行の「sphere collider keeps...」テストは、fixtureがColliderGroupIndicesを空にするため衝突処理を通らない。「repeated simulation」も同じ初期Stateから2回呼ぶだけで、連続stepではない。dt=0は静止初期値、全fixtureは1jointのみである。

完了条件: null group等の不正入力を公開APIの検査で診断し、状態を公開しない。R01〜R09の失敗再現を修正箇所ごとの正式テストへ移す。衝突テストはgroupを実際に参照し、衝突なし計算なら侵入することと衝突あり計算で外へ出ることを対で確認する。有限性も検査し、NaNが誤差比較をすり抜けないassertionを使う。CoreだけでなくR02/R03のSave/Open/終了経路をWindows Playerの専用検証へ加える。

## 修正後の開発への接続

修正順はR03（保存保護）→R01/R02/R09（import/session）→R04/R05/R06/R07/R08（simulation）、R10の回帰追加は各修正に同梱する。設定・状態、ベクトル／回転計算、chain評価、衝突solver、VRM対応、保存coordinatorは役割ごとのモジュールに分け、Workbenchへ計算や保存の実体を増やさない。

その後、VRM node→stable BoneId adapterとruntime previewへ進む。現metadataのgravityDir、sphere/capsuleのoffset・radius・tail等はinventoryに十分保持されていないため、完全な入力契約と保存移行も必要となる。一般node transform、skin/morph export、実アバター受入、C1〜C5の製品目標は既存計画の未完了事項として維持する。

## ローカル再現証拠

次はNyaForgeルートで実行する。これらの再現fixtureは確認用の自作データで、アバター素材を使用しない。`Artifacts/`はGit追跡対象外であり、別checkoutでは正式回帰テストへの移植時に上記条件から再構成する。

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore
dotnet run --project Artifacts/Review-20260912-SpringBone/CollisionRepro.csproj
dotnet run --project Artifacts/Review-20260912-SpringMath/Review.csproj
dotnet run --project Artifacts/Review-20260912-Vrm/Review.csproj
```

- [Collision出力](../../Artifacts/Review-20260912-SpringBone/collision-results.txt): 3件の期待違反でexit 1。既知バグの再現結果であり、本体ビルド失敗ではない。
- [Math出力](../../Artifacts/Review-20260912-SpringMath/repro.txt): step2、停止、親子gapの実測。exit 0は計測終了であって合格を意味しない。
- [VRM出力](../../Artifacts/Review-20260912-Vrm/repro-output.txt): 作者配列拒否、既定値、[0,0]再読込失敗、sidecar保存失敗後のdirty値。exit 0は計測終了であって合格を意味しない。

レビュー前の「297件合格・衝突対応確認済み」という記録のうち、テスト合格数は今回も再現した。衝突機能とVRM互換性の品質保証としては検証が不足していたため、本レビューを現在の評価として優先する。
