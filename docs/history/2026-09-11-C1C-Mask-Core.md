# C1-C マスク描画Core

2026-09-11。Windows先行、公開repo内の合成fixtureのみ使用。

## 実装

- `BrushCoverage`: 色/マスクの共有UVストローク範囲計算。点数・半径・UV・pixel work budgetを事前検査し、1ストローク内ではcoverageの最大値を使う。
- `PaintMaskStroke`: immutableなlinear coverage8を目標値へ補間。strength 0〜1、target 0〜255。丸めは画素ごとに最後の1回、色画像とsRGB変換は使わない。
- `PaintLayerChange.MaskStroke` と `LayerEditing`: 存在するmaskへの描画を共通commandに接続。画像を保持し、stack/UV/domainの古いcontextを拒否。target/strengthもfingerprintに含める。
- `PaintStroke` は同じcoverage計算を使い、既存のlinear-light色合成を継続。

## 検証

Core **130 passed / 0 failed**。
`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-5b74f981a4764b149bd7eddb87574217`

新規試験: 255→0のstrength .5で128、0→255でも128、重複入力点の結果一致、入力mask不変、白で復元、無効strength/半径/UV/点数/作業量の拒否、maskなし拒否、色画素保持、確定1revision、再送、stale context、Undo/Redo、native保存再読込。

## 残件

Windows Playerビルド `Builds/Windows-C1C-MaskCore/NyaForge.exe` 成功。`Logs/build-player-20260911-220647-244.log`。
既存操作の回帰検証も合格: `Artifacts/Authoring-20260911-220754-4bab83a8cfbb4e66b40ce3fc6b9d3e71/report.json`。色ブラシ・レイヤーGUIを含む。mask操作のPlayer検証は未実施。

マスク追加/削除・画像/マスク編集対象の切替・白黒表示・マスクブラシ・名前変更GUI。未確定previewと確定commandで同じマスク計算を用い、対象変更で描きかけを破棄する。1gesture Undo、取消、保存、合成PNG/SurfaceをPlayerで検証する。
