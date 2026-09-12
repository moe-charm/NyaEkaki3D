# C0-R: 空プロジェクトと保存移行

2026-09-11。C0-Rの実装記録。設計全体の完成ではない。次はC1-Aの型付きグラフ。

## 実装

制作CoreをDomain、Commands、Projection、Persistenceへ分離した。文書はobjectゼロを表現し、空meshを生成しない。static profileは現在最大1object。既存offsetの単位・基準hash・正常／失敗commandの再送・transaction・Undoを保持し、メッシュ追加も同じ操作経路に入れた。

native schema 2はobjects配列を持つ。空projectではblob不要。旧schema 1の読み取りは専用codecに残し、同じフォルダへの保存はMIGRATION_REQUIREDで拒否する。別フォルダへ移行後も文書とgeometryのhashが一致する。Bake schemaは1を維持した。移行元のfixtureは変更前のCoreが生成した自作データで、第三者モデルを含まない。

制作画面は空から始まり、プレート追加と新規作成を分離。追加をUndoするとMeshと頂点マーカーも消える。空projectのexportは対象なしとして拒否し、出力を作らない。プロジェクトの操作と、UI検証撮影も専用ファイルへ分けた。

## 検証

- Core 35/35。元の30項目に空の保存・読込、追加Undo/Redo、追加batch／projection失敗、旧schema 1のscale 1/100移行を追加。固定Bakeとgeometry hashを比較。
- ビルド: `Logs/build-player-20260911-180700-968.log`、Unity 6000.4.3f1、`Builds/Windows-C0R/NyaForge.exe`。
- Player: `Artifacts/Authoring-20260911-180834-b5d246459f4c42d79bd8cdfc8f5b8825/report.json` pass。別プロセスで先行実行の空projectを読込。追加、未保存キャンセルの同一GUIハンドラー、Undoによる空表示、scale 1/100の1cm編集、再読込、Bakeを検証。手動マウス受入とは分ける。
- 1280×800の`empty.png`を開いて案内と空表示を確認。先行実行の`authoring.png`でplateと編集UIも確認。UI Toolkit描画先とcamera画像による検査であり物理モニター撮影ではない。
- Unity Bridge: `Artifacts/BridgeReceiver-20260911-180525-606-4134f8a73e6c459980f27d211db1ab6d/bridge-report.json` pass、Unity 2022.3.22f1。geometry/attributeの受渡しを再検証。後続の変更はGUIと検証コードだけで出力Coreは変えていない。
- Viewer: `Artifacts/Navigation-20260911-180838-a0ad4682e81a4f99989ac3838c0c03c9/report.json` pass。

## ビルド先

ユーザーの既定Playerが起動中で、最初のビルドは使用中のPDB置換に失敗した。既定出力は更新完了版とせず、別出力Windows-C0Rを使用する。ユーザーのプロセスは停止しなかった。失敗したbatch Unityの終了とUnityプロセス不在を確認して、残った単一のUnityLockfileを解放確認後に除去した。

`Build-NyaForge.ps1`へ`-BuildName`と、起動中の同じ出力先をビルド前に拒否する検査を追加した。Authoring/Navigationの検証scriptもBuildNameを受け取る。既定先の事前拒否を実際に確認済み。

## 再実行

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Build-NyaForge.ps1 -Target Player -BuildName Windows-C0R
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C0R -Width 1280 -Height 800
# 前の実行で保存したemptyフォルダを、新しいPlayerプロセスで開く
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C0R -Width 1280 -Height 800 -ReopenProject '<前の出力>/empty'
```

C1のgraph、複数object、polygon/corner、UV/paint、rig/morph、MCP、最終ターゲットの確認は未完了。ユーザーのprivateアセットの移動・追加やcommit/pushは実施していない。
