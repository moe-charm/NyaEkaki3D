# 材質graphの実装契約

2026-09-12。設計v2/C1-Cの一般Material graphを実装中。Core・Unity表示と材質GUI/3D Paintを接続。材質付きGUI出力と独立Unity Bridge受取を接続・検証済み。複数材質など一般化は継続中。

## データの責務

`MaterialParameters` はUnity非依存でimmutableなstandard PBR値。base colorはlinear RGBA各0〜1、metallic/roughnessは0〜1（roughness 0=滑らか）、emissionはlinear RGB各0〜64、alpha modeはOpaque/Cutout/Blend、cutoffは0〜1。有限値と範囲を構築時に検査し、canonical binaryによるContentHashを持つ。既定は白・metallic0・roughness0.5・emission0・Opaque・cutoff0.5。

これは最初のstandard材質型の制約。toon、normal/metallic等のtexture入力、複数材質slot、任意shader/profileは別の明示的な型・契約として追加する。見かけだけの設定値を保存して描画に無視する運用にはしない。

`GraphMaterialValue` は評価済みparametersと任意のbaseColor画像bindingを組にする。画像は従来の `GraphImageValue` を再利用し、UV hash・mesh domainを保持する。画像正本は引き続きPaint/LayeredPaint側が所有し、材質ノードが複製・flattenしない。

## 接続

```text
最終geometry ──────────────→ mesh.assign-material → mesh.output
       └→ image.paint-layers → material.standard ────┘
```

- `material.standard` v1: 任意Image入力 `baseColor`、Material出力 `material`、parameters payload。
- `mesh.assign-material` v1: 必須Mesh入力 `mesh`、必須Material入力 `material`、Mesh出力 `mesh`。現在はmesh全体への一括割当。
- assign時に画像のUV/domainを従来と同じ規則で照合する。画像なしの単色材質も評価できる。
- assignmentはgeometry操作とPaint入力の後に置く。現時点で後段のgeometry操作は `MATERIAL_ORDER_UNSUPPORTED`、二重割当とOutput.baseColorによる上書きは `MATERIAL_ALREADY_ASSIGNED` と診断する。材質を黙って消す伝播にはしない。
- 既存のPaint→Output.baseColorは変更しない。新しいMaterial portは新ノード間に追加し、旧Outputのport定義やpayloadは変えない。

`GraphMeshValue` はMaterialを保持し、材質parametersのhashをsnapshotへ追加する。材質なしのsnapshot構成は従来どおり。色だけの変更でmesh/contentや画像正本のhashを変更しない。

## 保存・command・互換性

graph wire version1、native graph schema3を維持。新しいnode type/versionでparametersをexplicit binaryとして記録する。旧node payloadにfieldを足さない。Materialの画像は既存の上流画像blobで所有する。新ノードの未知type/version保持は既存opaque-node機構の対象だが、古い実行ファイルによる往復試験は未実施。

作成/変更は既存AddGraph/AddNode/UpdateNodeと共通Undo/Redoを使用する。別の材質専用履歴は持たない。現在の静的Bake/Surface Bakeは材質parametersを運べないため明示拒否。材質付き出力には別のMaterial Bake profileを使用する。

## Unityの表示と所有

`StandardMaterialAdapter` は現在のLinear/Built-In pipeline向けの材質を生成する。値をlinear Vectorとしてshaderへ渡し、roughnessをsmoothness=1-roughnessへ変換する。shaderの欠損/非対応や別pipelineでは明示エラーにする。URP対応済みとは扱わない。

Resourcesの `AuthoringPbrOpaque` / `AuthoringPbrBlend` はUnity Standard lightingのSurface Shader。共有 `AuthoringPbr.cginc` がtexture×tint、metallic、smoothness、emission、alphaを評価する。両面表示。Opaqueはcutoff0でtarget alpha1、Cutoutはtexture alpha×tint alphaの閾値でdiscard、Blendはfade透明・ZWrite off。BlendはpreviewではRGBだけを書き、camera背景との合成済みtarget alpha1を保持してUI表示時の二重透過を防ぐ。BridgeではRGBAを書き込む。

`BaseColorSurface` が材質と確定/仮textureを所有する。画像のない材質は共有white textureを参照し、共有textureを破棄しない。Paintの仮画像だけを差し替えても材質parametersを保持する。`ColorUpdate` は材質変更にも使い、mesh/rootを作り直さずprepare/commit/rollback/Disposeを適用する。

`AuthoringPreviewLights` のkey/fillはworkbench stageが所有する。PreviewLayerだけを照らし、workbenchを閉じるとcameraと一緒に無効になる。sourceにnormalがない場合は表示meshだけへ法線を生成し、制作正本は変更しない。編集段階で使う灰色の最終形状backdropは形状比較用のままで、材質の外観確認は最終Output表示で行う。現在のviewportはSDRで、1を超えるemissionの全域比較やtone mappingのUIは未実装。

Surface Shaderの透明方式は [Unity公式directives](https://docs.unity3d.com/6000.0/Documentation/Manual/surface-shaders-language-reference-optional-directives.html) に従う。Blendは現時点でfadeを意味し、反射だけ不透明に残すガラスprofileとは別。

## 次の実装順

1. 材質を保持するBake DTO/profileとBridge受取、rollback、native→再読込→Unity出力を通す。既存mesh-only/画像Surface profileの互換を維持する。
2. 複数角度と実モデルで外観を確認する。半透明面どうしのsorting、HDR表示、環境光/reflection、shadowは品質面の残件として保持する。
3. ノードcanvasからの任意材質接続編集を拡張する。現在の専用GUIは最終Outputへの作成と登録済み材質の値編集を担当する。
4. 複数材質、toon、追加texture slots、UV再投影、Evidence/MCPなどを製品範囲として継続する。

## 検証

Core167件でparameters範囲/canonical identity、材質型接続、同色画像の別mesh domain拒否、競合接続/後段geometryの診断、材質変更でmesh/画像を保持しsnapshotを更新、共通Undo/Redo、native保存・再読込、graph byte roundtrip、現行Bakeの明示拒否を検査。これは描画品質やGUI操作の検査ではない。最新buildと回帰結果は [current_task](../current_task.md) に記録する。

Unity表示の検証: `Windows-C1C-MaterialLighting` / `Artifacts/Authoring-20260912-010701-ec12192939944379a9a6d7c04e89c7c9/report.json`。PBR各値の設定とGPUのalpha/色/光沢応答、仮textureを含むrollback、Undo/Redo、mesh再利用に合格。旧GUI、PNG30形式、準備競合、DensePaintも回帰。既存167件のCore結果は前段のままで、今回Core本体の変更なし。この時点では材質GUI/Bake/Bridgeが未完成だった。以下に現在の接続を記す。

## GUIと出力経路の接続（2026-09-12追記）

`AuthoringWorkbench.Materials` は折り畳める材質欄、登録済み材質の選択、作成、色/不透明度/metallic/roughness/emission/alpha/cutoffの編集を担当する。基本色と発光色はsRGBの#RRGGBB入力。共通UpdateNodeで適用し、不正値と古いparameters hashは変更前に拒否する。何も変えずに適用した場合はcommandを作らない。色コード表示に丸めがあっても、色を編集していなければ元のlinear RGBを保持する。

発光の正本はlinear RGBであり、再表示は最大成分を強さ、正規化した色を発光色として分解する。入力した色/強さの組が再表示で変わる場合も、積としての発光値は変えない。材質がないときは編集欄を隠し、数値はラベルの下に配置して長い値を読める幅を確保する。

`OutputSurfaceConnections` が旧Output.baseColorとMaterial経由を共通に解決する。作成時は既存Paint node/imageをそのまま残して画像接続を材質へ移し、mesh.assign-materialを最終出力前に挿入する。ReplaceGraphを1commandとして行い、Undo1回で旧経路へ戻る。材質作成後にPaintを追加する場合もgeometryからPaintへ接続し、assignment後のmeshをPaintへ誤接続しない。

3D Paintの適格判定も同じ経路解決を使う。描画は従来のlayer commandで画像を更新し、材質値は保持する。GUI検証は作成/画像保持/Undo/Redo、不正値/無変更適用、材質経由の3D stroke、native保存/再読込、単色材質への後付けPaintを実際のpointerイベントで確認する。最新結果はcurrent_task参照。

材質Bakeの続報: [Material-Bake](Material-Bake.md) のCore出力/読取を実装し、Core173件とWindows Playerのnative→材質Bake→読戻しが合格。GUIの出力ボタンからMaterial Bakeを生成し、共有Renderingパッケージ経由で別UnityのMaterial/Texture/Prefabへ保存する。受け取り先で読戻し・失敗rollback・照明付き描画・透明率のGPU画素を検証済み。現在Built-In / Linearのみで、複数slot別材質とURPなどは残件。
