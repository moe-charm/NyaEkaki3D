# C1-A: graphのstatic BakeとUnity受け取り

2026-09-11。`Persistence/BakeSource.cs`で評価・出力元の解決を分離。既存Bake schema 1を維持してgraphの最終出力meshとtransformを書き出す。

現在のMeshSource／Plane→EditMesh→Outputのmesh経路をたどり、baseline hashには上流の生成結果またはsource meshを記録する。法線を保持した位置編集の有無は最終出力の経路だけで判定する。将来の別種ノードは専用adapterを必要とする。任意graphの編集履歴をBakeへ平坦化して保存し直すことはせず、native schema 3の制作データを残す。

評価はwriter lockと出力ディレクトリ作成より前に行う。未完了graphはGRAPH_INCOMPLETEで拒否し、last-good previewを代わりに書き出さない。

## 検証

- Core **67/67**。Plane編集後のnative再読込・Bake、baseline、法線編集フラグ、未完了の出力先未作成、source scale1/100・平行移動・submeshを追加確認。
- Player: `Builds/Windows-C1A-GraphBake/NyaForge.exe`。build log `Logs/build-player-20260911-190735-087.log`。
- Player report: `Artifacts/Authoring-20260911-190754-058d5d1a039f4d3d978ca79041cf075b/report.json` pass。graph scale1/100のGUI 10mm編集、schema3保存・再読込、Bakeと未完了拒否を確認。
- `Tools/Test-NyaForgeUnityBridge.ps1 -GraphFixtures`は同reportのgraphBakeManifestsを検証する。従来のbakeManifestsはstatic回帰用として保持。
- Receiver: `Artifacts/BridgeReceiver-20260911-190806-977-4fcbd9da9108443ab765b13236d61f86/bridge-report.json` pass。Unity 2022.3.22f1で6項目。位置・UV0・normal・tangent・submesh・prefab参照、scale一致、+0.01mの既知編集、親付き配置、出力先検査を確認。

残件: canvas配置の永続化、pointer受入、parameter変更を含むC1-A完成経路の最終確認。全体のC1〜C5は引き続き未完成。
