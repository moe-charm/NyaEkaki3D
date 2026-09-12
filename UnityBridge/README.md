# NyaForge Unity Bridge (NF-0)

NyaForge の静的メッシュを、別の Unity プロジェクトで通常の `Mesh.asset`・確認用 `Material`・`Prefab` として使う Editor パッケージです。Unity 2022.3 以降を対象とするコードです。実際に検証した Editor 版はリポジトリの `current_task.md` に記録します。

## 導入

受け取り側プロジェクトで、Package Manager の **Add package from disk** を次の順で実行します。

1. NyaForge checkout の `Assets/NyaForge/Authoring/package.json` — `com.nyaforge.authoring`
2. 同 checkout の `Assets/NyaForge/Rendering/package.json` — `com.nyaforge.rendering`
3. 同 checkout の `UnityBridge/package.json` — `com.nyaforge.unity-bridge`

3パッケージは同じ checkout/revision を使ってください。外部に渡す場合は3パッケージを配置します。Core の依存パッケージは Unity Package Manager が解決します。アバター、private データ、AssetBundle は不要です。

## インポート

1. NyaForge から静的 Bake パッケージを出力します。manifest と `blobs` を含むディレクトリ全体を保持します。
2. 受け取り側 Unity で **Tools > NyaForge > Import Static Bake...** を開き、出力 manifest JSON を選びます。
3. Assets 内の保存先の親フォルダーを指定し、インポートします。

毎回 `NyaForgeImport-<GUID>` という新しいフォルダーを作ります。既存ファイルへの上書きはありません。失敗した場合は、その処理で作った専用フォルダーを取り消します。

Prefab の頂点は、Bake の正の均一スケールと平行移動を一度だけ適用したメートル座標です。Prefab の Transform は位置ゼロ・回転ゼロ・スケール 1 です。UV0、法線、接線、頂点順、サブメッシュと三角形順を保持します。法線の自動再計算・頂点結合・最適化は行いません。

「シーンにも配置する」を有効にすると、読み込んだ Prefab を配置します。親は任意のシーン Transform を明示して選べます。親子関係の変更時にはワールド位置を維持するため、骨名や首の位置からの推測はありません。親のスケールは正の均一値に限定します。これは首へのフィットや rest-pose の骨対応を実装した機能ではなく、通常の Unity の親子付けです。位置を調整して使ってください。シーン配置は Unity Undo で取り消せますが、書き出したアセットの削除は行いません。

## NF-0 で扱う内容

- 静的三角形メッシュ、UV0、法線、接線、複数サブメッシュ。
- 通常の Transform / MeshFilter / MeshRenderer のみを持つ Prefab。
- 各サブメッシュに確認用の単色材質。元の shader、texture、材質パラメーターの変換は未実装です。

skin、blendshape、任意回転・非一様/負スケール、texture、リグ対応、VRChat SDK 設定、FBX/GLB 出力は含みません。Bake codec が未対応 feature を拒否します。受け取りに NyaForge 用の実行時 MonoBehaviour は不要です。Bridge の成功は VRChat の Build & Test や見た目の受け入れ確認を代替しません。

## 受け取り側の自動検証

通常は、Windows Player の制作検証で生成した `report.json` があるディレクトリを、次のスクリプトへ渡します。

```powershell
.\Tools\Test-NyaForgeUnityBridge.ps1 -PlayerCheckDirectory .\Artifacts\authoring-check
```

スクリプトは Player の成功レポートと `bakeManifests` の 2 件を読み、新しい `Artifacts/BridgeReceiver-<日時>-<GUID>` プロジェクトへ Core と Bridge のローカルパッケージを登録します。受け取り側の Unity は非表示の batch mode で起動し、終了コードと JSON レポートとログの成功マーカーを確認します。生成プロジェクト、インポートしたアセット、`bridge-report.json`、`bridge.log` は削除せず、そのフォルダーに残します。`Artifacts` は Git 追跡対象外です。

既定の Editor パスはローカルの互換性試験対象である `2022.3.22f1` です。VRChat の最新指定版を意味する値ではありません。別の 2022.3 以降の Editor を試す場合は `-UnityPath 'C:\...\Editor\Unity.exe'` を指定します。検証先は毎回新しく作り、既存の Unity プロジェクトを書き換えません。

既存の使い捨て検証プロジェクトを手動で用意する場合の呼び出しは次のとおりです。

使い捨ての受け取り側 Unity プロジェクトに上記パッケージを登録してから実行します。Unity Editor を起動中の同じプロジェクトに二重で起動しないでください。

```powershell
& $unityEditor -batchmode -nographics -projectPath $receiverProject `
  -executeMethod NyaForge.UnityBridge.Editor.BridgeBatch.VerifyRoundTrip `
  --nyaforge-bake-scale1 $scale1Manifest `
  --nyaforge-bake-scale100 $scale100Manifest `
  --nyaforge-report $reportPath `
  -logFile $logPath
```

引数の 2 つの入力は、自作 `AuthoringFixtures.Panel` の source scale 1 / 100 に対して、頂点 0 だけにメートル単位で X +0.01 を適用して出力した Bake です。両方の平行移動は同じにします。検証は serialized Mesh / Prefab を再読込し、既知の位置変化、尺度の同値性、UV・法線・接線・サブメッシュ、通常コンポーネント、明示した親への装着、無効な保存先の拒否を確認します。

成功時のログは `NYAFORGE_BRIDGE_ROUNDTRIP_PASSED`、プロセス終了コードは 0 です。JSON レポートには実行した Unity 版と個別の通過項目を記録します。検証で作ったアセットは受け取り側プロジェクト内に残して確認できます。描画確認は別途必要です。

## Surface形式（画像付きstatic mesh）

Importウィンドウで `surface.nyaforge-bake.json` を選び出力形式「画像付きSurface」を選ぶ。Coreが全データを検査した後、PNG・不透明base color材質・Mesh・Prefabを新規の所有フォルダに生成する。既存mesh-only形式とはreaderを分けている。

C# APIは `BakeImporter.ImportSurface`。検証scriptには `-SurfaceFixture` を追加できる。schemaと制限は [Surface Bake仕様](../docs/Surface-Bake-Format.md)を参照。再import時の既存asset更新、透明材質、Toon shaderの再現は今後。

小物のGUI出力を検査する場合は `-ItemFixture`、描画画像まで確認する場合は `-RenderSurface` も指定する。後者はgraphicsを有効にして起動し、receiver直下の `surface.png` に保存する。`-SurfaceFixture`と`-ItemFixture`は同時指定しない。

## 標準PBR材質付き形式

「Unity用に書き出す」で生成した `material.nyaforge-bake.json` をImportウィンドウで選ぶと「標準PBR材質付き」が選択される。手入力時は出力形式も合わせる。APIは `BakeImporter.ImportMaterial`。

Built-In render pipeline / Linear color spaceが必要。共有Renderingパッケージの両面shaderで、linear基本色、metallic、roughness、emission、不透明/切り抜き/半透明と任意のbase color画像を保持する。全slotに同じ材質を割り当てる。旧mesh-only形式の確認用材質とは別のprofileで、任意の外部shaderを変換する機能ではない。

`-MaterialFixture -SurfaceFixture` でPlayerの材質GUI出力を独立receiverへ渡せる。graphics/Linearを有効にし、材質値・texture・Prefabの読戻し、注入した失敗での所有folder rollback、material.pngへの描画を検査する。受取側の照明やカメラは作品に含まれず、外観の受け入れは別に確認する。

材質付き形式で法線が未指定なら、Unity側の派生Meshに表示用法線を生成する。元のBakeは法線なしのまま保持される。既存法線は変更しない。頂点を結合する処理ではないため、分割された頂点の境界は別々に法線を計算する。旧mesh-only/Surface形式では元の属性をそのまま保持する。

## 部位別標準材質（追加profile）

`materials.nyaforge-bake.json` は「部位別PBR材質付き」で読み込みます。Built-In / Linearが必要です。使用slotごとの色・金属度・粗さ・発光・alpha設定と任意PNGを保持します。材質UUID/PNG hashの共有を維持し、元slotとUUIDの対応は `MaterialSlots.json` に保存します。材質profileで法線がない場合は、受け取り側の派生Meshに法線を補います。

GUI出力を含む検証は `Tools/Test-NyaForgeUnityBridge.ps1 -PlayerCheckDirectory <Player結果> -MultiMaterialFixture`。初回の新規importのみで、既存Prefabの更新はまだありません。前述NF-0のtexture非対応は旧mesh-only profileの範囲です。

初回importには `NyaForgeImport.json` も保存します。Import画面の「既存インポートの確認」でフォルダを指定し、「生成後の変更を調べる」を押すと生成後の資源変更を検出します。読取専用で、再取り込みを適用する機能はまだありません。詳細は `docs/Unity-Import-Updates.md` を参照してください。

新しいBakeには `<manifest名>.identity.json` が付属します。省略せずフォルダ全体を保持してください。Import画面の「選択Bakeと更新先を照合」は、複数材質Bakeと既存importの出力ID、revision、生成後の変更を比較します。旧形式は初回importできますが、明示IDなしの自動更新先照合は行いません。照合は読取専用で、更新適用は未実装です。

複数材質は照合後「照合した出力を反映」で既存importを更新できます。既存参照を保持し、不要資源は残します。生成資源にUnity側の変更がある場合は競合として止まります。通常例外での復元を検証済みですが、強制終了からの復旧、利用者追加componentを含むPrefabの選択的更新は今後の対応です。上の「更新適用は未実装」はこの機能追加前の記録です。

receipt v3では、Prefabに追加した保存済みのcomponent・子object・Transform/表示設定を保持して更新します。管理対象のMesh/材質参照が変わった場合は競合です。未保存のPrefab編集は先に保存してください。古いreceiptは全file比較を維持します。scene override、variant、nested prefabは引き続き未検証です。

更新開始前のbackupは `Library/NyaForgeUpdates` に保存します。未完了更新は `Tools > NyaForge > Recover Interrupted Updates...` から対象を確認して復元できます。pendingがある間はLibraryを削除しないでください。新規資源のGUIDが未確認の場合は自動削除せず停止します。実際の強制終了/再起動試験はcurrent_task記載の残件です。

隔離receiverの材質/Prefab/記録更新後に実プロセスを終了し、別Editorプロセスから復旧する試験は `Tools/Test-NyaForgeCrashRecovery.ps1 -PlayerCheckDirectory <Player結果>` で実行できます。3地点で元file/メタデータの復元を確認済み。OS電源断や復元中断への全面的な保証を意味しません。
