# standard材質のBake契約

2026-09-12。Core出力/読取、共通RenderingパッケージとUnity Bridge受取を実装。独立Unity 2022.3.22f1のBuilt-In / Linear環境で材質・画像・Prefabの保存、描画と失敗rollbackを確認。製品GUIの出力ボタンから同じデータを渡すPlayer/receiver回帰にも合格。

## 形式

manifest名は `material.nyaforge-bake.json`、schemaVersionは1、profileは `static-standard-pbr-linear-straight-fade-all-slots-v1`。

- `mesh`: 従来のBakeManifestを埋め込み、mesh blob・rest変換・document/object identity・revision・baselineなどを保持する。静的triangle、正のuniform scaleとtranslationが対象。
- `materialHash`: standard材質parametersのcanonical binary blobのSHA-256。全submeshに同一材質を割り当てる。複数slotごとの異なる材質は次のprofileで扱う。
- `baseColor`: 画像descriptorの配列。0件が画像なし、1件がbaseColor画像あり。必須fieldで、null・省略・2件以上を認めない。共通Storageの厳密JSONルールを変更しない。
- 画像descriptorは `imageHash` / `pngHash` / `uvHash` / `meshDomain` / `width` / `height`。PNGと編集用RGBA blobを両方保持する。画像なしを透明な画像や黒画像で代用しない。

全field名はcamelCase。未知field、欠落、型違い、nullは既存Storage validatorが拒否する。未対応schema/profileも拒否する。旧mesh-only/画像Surfaceのreaderはこのmanifestを受け付けず、画像Surfaceのwire自体は変更しない。

## 材質値と描画の意味

`MaterialParametersCodec` は44byteの固定payload。little endianでlinear tint RGBA4float、metallic1float、roughness1float、linear emission RGB3float、alpha mode int32、cutoff1floatを順に保持する。finite/range/enumを検査し、negative zero等の非canonical表現を拒否する。独立のwire headerはなく、このprofile v1がpayload versionを定義する。

値の範囲は [Material graph](Material-Graph.md) と共通。textureはsRGB RGB/straight alpha、tintとemissionはlinear。texture×tintで色とalphaを合成する。Opaqueは不透明、Cutoutはcutoffでdiscard、Blendはfade。previewと同じ両面表示をreceiverでも明示的に実装する必要がある。現在のpreview専用light・camera・SDR targetは作品材質の一部として出力しない。

## 所有・検証・公開順

`MaterialBakeStore` は確定したworkspaceをlock内で評価し、未確定command中の出力を拒否する。`BakeSource.CaptureMaterial` だけが材質割当を含むgeometry ancestryを辿る。旧Bake経路は材質を明示拒否する。

画像の保存/読取は `BakeImagePayload` へ共有化し、既存Surface Bakeも同じ検査を使う。mesh・材質・画像blobの名前とhash、PNGとRGBAから再生成したPNGの一致、UV0の存在、画像寸法を検査する。読取結果はimmutable材質/画像とコピーを返すPNGで所有する。

directory lock下でcontent-addressed blob/PNGを書き、全て揃ってからmanifestをatomic replaceする。途中で既存PNGの改変等を検出した場合、以前のmanifestを置き換えない。既存Storageと同じく、公開前の未参照blobが残る場合はある。

## 確認範囲

Core173件に、3alpha modeのmesh/材質/画像保持、native reopen後の再出力、同じ入力の再出力、PNGコピーの所有、PNG破損時の旧manifest保持、画像なし、schema/profile/field/payload異常、未割当/未解決時の非作成を追加した。既存Surfaceのscale1/100往復と破損検査も再実行している。

Playerの材質GUI回帰ではnativeを開き直した作品からこのAPIで出力し、mesh・材質hash・全画像byteを読み戻して比較する。結果directoryの `material-export-manifest.txt` を次のreceiver検証の入力に使える。BridgeではAssetDatabase/Prefab作成・読戻し・所有フォルダのrollbackと描画を確認済み。receiver fixtureは画像付きCutoutに加え、画像なし・法線なしの平面でOpaque/Cutout/Blendを確認。全alpha modeのreceiver描画一致や任意照明での外観一致を証明したものではない。

## Unityへの接続

`Assets/NyaForge/Rendering` の `com.nyaforge.rendering` がshaderとadapterを所有し、PlayerとBridgeが共用する。CoreはUnityに依存しない。材質の受取APIは `BakeImporter.ImportMaterial`。全payload・pipeline・shaderを検査してから独立asset folderを作成する。

製品の「Unity用に書き出す」は最終出力の材質、画像、mesh-onlyの順で適切な形式を選ぶ。Bridge windowのファイル選択は既定manifest名から形式を選び、手動でも形式を切り替えられる。名前による選択は検証の代わりではなく、各readerが内容を検査する。

Blendはpreviewの合成済み背景alphaを保持するためRGBのみ、受け取り側は通常のRGBA書込みとする。色とalphaの材質値自体は共通。現在はBuilt-In / Linearのみで、未対応pipelineはasset作成前に拒否する。

材質付きmeshに法線属性がない場合、共有 `DisplayMeshNormals.Ensure` が生成したUnity Mesh.assetにだけ法線を補う。頂点をweldせず、既存の頂点分割を使うため、UV seamなどの分割がhard edgeになる場合がある。既存法線は保持し、Core meshとBake blobを更新しない。旧mesh-only/Surface profileの属性保持契約も変えない。

receiver画素検証は、保存したMaterial/Prefabを再importしてlinear float targetへ描画する。画像alpha128/255とtint alpha0/0.5/1、Opaqueでalpha0、Cutoutの閾値0.249/0.253を表裏で確認。RGB期待値はemissionと既知背景の線形合成から独立計算し、12ケースを測定する。カメラ背景色はsRGBとして解釈されるため、既知linear背景をgamma変換して指定する。

この検証はstraight fadeのRGB合成を対象とし、render target alphaのcoverage意味、重なった透明面のsort、影、MSAA、透明輪郭filteringは証明しない。残件はそれらと曲面/seamの生成法線品質、URP、異なる複数材質、出力identityを保持する更新。成功したインポートは毎回別folderに保存される。