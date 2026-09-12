# C1-C 切れたUV線分をまとめる描画基盤

2026-09-11。3D入力をつなぐ前の基盤。3DブラシGUIはまだ未接続。

## 変更

- `PaintStrokePath`: 入力polylineをコピーしてreadonlyに保持。空区間・範囲外UVを拒否。全区間合計の点数を1024以内に制限。
- `BrushCoverage.RasterizePaths`: 区間の境界では線をつながず、全区間のcoverage最大値をまとめる。pixel work budgetは16Mで全区間共有。
- `PaintStroke.ApplyPaths` / `PaintMaskStroke.ApplyPaths`: 色/linear maskの既存合成を1度だけ実行。旧単一路径APIも共通範囲計算を使う。
- `PaintLayerChange.PathStroke` / `MaskPathStroke`: 共通layer commandに接続。境界・mask target/strengthを再送fingerprintへ含める。旧操作のenum値とfingerprintは保持。

## 証拠

Core **140 passed / 0 failed**。
`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-67c3bacd8d034729b0d8c430017792bb`

離れた区間の間が塗られないこと、重複区間と単一区間の結果一致、色/mask両方のopacity、入力のコピー、全区間の点数/作業量制限、1command/Undo/Redo/native往復、区間境界やstrengthを変えた同一command IDの拒否、stale contextを検証。

Player確認は `SurfacePaintVerification` に追加。最新結果はcurrent_task.md参照。

## 次

3D rayのhit列からpolyline境界を作る処理、seam/背景/遮蔽の判定、3D brush GUIと一時preview。API試験を3D操作の完成と読み替えない。
