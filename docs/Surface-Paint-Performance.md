# 3Dペイントの計測と残件

2026-09-11。Windowsローカルの.NET Core計測。Unity Playerや実アバターでのフレーム時間を保証する値ではない。

## 再現

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj -- --surface-profile
```

`SurfacePaintProfile` は格子planeを生成し、ray BVHと投影coverageの構築、50px/800pxの直線gestureを測る。結果は表示された一時フォルダの `surface-profile.json`。profileは計測専用であり、strokeの失敗も結果として記録する。プロセスのexit 0を全ケース成功と読み替えない。ウォームアップ・複数回の統計処理は未実装。

初回証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4f66f8ec38094800bdb9fb9bd77d58e4/surface-profile.json`。

| 三角形 | ray BVH構築ms | coverage構築ms | 50px stroke ms / UV点 | 800px stroke |
|---|---:|---:|---:|---|
| 2,048 | 9.48 | 9.69 | 6.17 / 51 | 807点、成功 |
| 8,192 | 24.70 | 13.77 | 0.28 / 81 | 1024点上限で取消 |
| 32,768 | 111.60 | 92.93 | 0.46 / 135 | 1024点上限で取消 |
| 131,072 | 577.05 | 270.15 | 0.74 / 226 | 1024点上限で取消 |

初回JITの影響も含む単回値。50pxで高密度側が高速だという順位付けはしない。UV/ray予算の上限到達は失敗として扱い、途中の筆跡は保存しない。

## 今回の対処

当時の同期 `SurfaceProjectionCache`（2026-09-12に非同期準備へ置換）はmesh content hash、配置、camera view/projection matrix、viewport矩形、near/farをキーに1つのcoverage snapshotを所有する。色だけが変わる連続strokeでは投影BVHを再構築しない。行列は近似比較でなく値の一致で判定。workspace交換時は解放する。カメラ・形状・viewportが変われば再構築するため、初回や回転直後の構築費は残る。

`SurfaceViewportVerification` は描きかけ中にUI viewportを幅/高さ75%へ縮小し、実layoutイベントで取消・pointer解放・render target交換が起きることを確認する。古い座標での遅いpointer upは文書を変えず、新しい座標で期待UVへ描いて1command/Undoできることまで検査する。coverageの再利用とresize後の交換も対象。OSウィンドウ枠のドラッグやDPI変更を手動確認したという意味ではない。

## 次の課題

1. 直線の点数上限は下記の簡略化で改善した。曲がった筆跡、多数の区間、長時間の連続入力でray上限へ達するケースは、実際の操作と負荷を追加確認する。上限を無制限に増やす対応はしない。
2. camera変更後の投影再構築、近接した多数面、実モデルでのGC/メモリとPlayerの処理時間を測る。今回のcacheヒット確認だけで負荷改善の割合を主張しない。
3. float screen座標の限界、接線・頂点ちょうどの交差、DPI/OS resizeの手動受入を残す。

## 筆跡簡略化後の比較

`PaintPathSimplifier` を導入し、探索したraw UV（最大4096点）とcommandへ渡す筆跡（最大1024点）を分離した。各区間を独立にDouglas-Peucker法で簡略化する。UV誤差は `1/(8*1024)` 以下で、対応画像サイズでは最大1/8 texture px。区間は結合せず、端点と許容誤差を超える折返しを保つ。簡略化しても1024点へ収まらない複雑なgestureはinvalidとして取消する。

修正後証拠: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-b49386d1c0fb46d688ee711a0c85773e/surface-profile.json`。

| 三角形 | 800px探索ray | raw UV | 保存するUV | stroke ms | 結果 |
|---|---:|---:|---:|---:|---|
| 2,048 | 809 | 807 | 2 | 1.81 | 成功 |
| 8,192 | 1,223 | 1,221 | 2 | 2.88 | 成功 |
| 32,768 | 1,997 | 1,995 | 2 | 5.23 | 成功 |
| 131,072 | 3,576 | 3,574 | 2 | 9.61 | 成功 |

このstroke時間はCore探索と最終snapshotまで。Unity texture更新、合成、GUI、Undoの時間は含まない。Core153件では曲線の全入力点が誤差以内に収まること、256角の比較画素のalpha差が10/255以下であることも検証した。任意の画像で画素値が完全一致するという保証ではない。

## Playerでの高密度測定

`AuthoringWorkbench.DensePaintVerification` は65,536 quad / 131,072三角形の合成polygon graphに256角のPaint layerを追加し、viewport幅80%の直線を16回のpointer moveで描く。2本を順番に描き、初回/連続描画のpointer down、move合計/最大、pointer upを記録する。moveには仮画像の合成とtexture更新、upにはcommand確定とGUI/projection更新が含まれる。GPU完了や表示フレーム遅延まで測るものではない。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-DensePolygonPaint -Width 1280 -Height 800 -DensePaint -TimeoutSeconds 240
```

通常の回帰へ追加するopt-in試験。結果は同じartifactフォルダの `dense-paint-profile.json`、画像は `dense-paint.png`。通常の `report.json` へ合否をまとめる。初回はMeshSourceとして作ってPaintのpolygon UV契約を満たさず失敗したため、測定fixtureをPolygonSourceへ修正した。これは製品側の対応範囲を変える修正ではない。

この測定は速度の合格閾値を設けず、数値を記録する。managed heapは `GC.GetTotalMemory(false)` の前後値で、割当量やピーク使用量ではない。出力点の簡略化、連続した描画画素、仮表示と文書の分離、1command、Undo/Redoは機能として検査する。これまでの通常試験の時間上限120秒は既定のまま、追加計測では明示的に240秒を指定する。

PolygonSourceへ修正した初回の合格証拠: `Artifacts/Authoring-20260911-235103-bb86db3212824e598c0fea42b2a5ec32/dense-paint-profile.json`。739.2pxのstrokeでraw UV約3593点→保存2点。pointer downは初回1982ms/2本目929ms、16 move合計は37.1ms/28.2ms、pointer upは2267ms/2262msだった。機能は合格したが、描き始めと確定の待ち時間は実用上長い。

調査すると、graph評価のたびに同じimmutable polygonの三角形化・描画頂点への変換・polygon binary hash・UV hashを繰り返していた。`PolygonDerivedData` にこれらの遅延生成をまとめ、`ConditionalWeakTable` でpolygon snapshotの寿命に対応させる。内容hashをキーとする永続辞書にはしない。モデルを変更すると新しいpolygon instanceになるため別の結果を生成する。生きているUndo履歴のsnapshotに属する派生データはその間保持される。

Core155件では未cacheのbinary/UV hashとの一致、同一snapshotの再利用、頂点移動・UV差替えによる結果更新、元snapshot保持、同時読み取りを検証。保存形式やhashの意味は変更しない。

修正後のPlayer合格証拠: `Artifacts/Authoring-20260911-235528-bb5f08f2be8748289e10565f6137d14b/dense-paint-profile.json`。

| 処理 | 初回 修正前→後 ms | 2本目 修正前→後 ms |
|---|---:|---:|
| pointer down | 1982→1046 | 929→13 |
| 16 move合計 | 37.1→36.7 | 28.2→29.7 |
| pointer up | 2267→425 | 2262→403 |

同じ合成fixtureの単回比較。初回ray/coverage構築はまだ約1秒、確定時も約0.4秒を要する。次は形状不変のPaint確定でUnity projectionの作り直しがどれだけ含まれるかを切り分ける。画像の2本の筆跡と通常GUI/保存/出力回帰も確認したが、任意の実モデルの快適さを保証する結果ではない。

## 色だけのprojection交換（2026-09-12）

[ColorUpdate](Projection-Updates.md)で形状不変のPaint確定とUndo/Redoは同じUnity mesh/rootを保持し、texture/materialのみ交換する。Playerでその参照一致を検査した。Prepare/Commit/Rollback、描きかけの復元、古い候補の拒否と形状変更時の全体交換も別の試験で確認した。

証拠: `Artifacts/Authoring-20260912-000254-cfa65629d16e4b8bb3248b6d273e6869/report.json` と `dense-paint-profile.json`。

- 初回: down約1038ms、move合計36.2ms、up約412ms。
- 2本目: down約13.2ms、move合計29.9ms、up約409ms。

従来のup約425/403msと比べ、明確な速度改善は確認できない。不要なmesh/root交換はなくなったが、確定の待ち時間の主因は未特定。次はcommand内部の文書作成/hash/評価と、GUI刷新を個別に測る。表示資源の再利用を「確定待ちが解消した」とは扱わない。

## command identityの再利用（2026-09-12）

[phase別計測](Command-Performance.md)でenvelope作成/validation/candidate作成のgraph identity再計算が主因と確認し、identity専用のgraph/node cacheを追加した。実保存のblob callbackは省略しない。

修正前: `Artifacts/Authoring-20260912-000822-8cbefde3551d43e89562c4a7492a8b72/dense-paint-profile.json`。
修正後: `Artifacts/Authoring-20260912-001203-80ab647377c34feeab7efe4bf1c29e05/dense-paint-profile.json`。

| 2本目の処理 | 修正前 ms | 修正後 ms |
|---|---:|---:|
| envelope作成 | 115.68 | 0.009 |
| validation | 130.17 | 0.033 |
| candidate作成 | 140.73 | 14.75 |
| graph評価 | 10.03 | 11.49 |
| projection交換 | 0.68 | 0.67 |
| GUI更新 | 5.16 | 4.92 |
| pointer up全体 | 403.78 | 33.14 |

初回upは412.65→41.39ms。2本目downは約13ms、move合計は約30msでほぼ同じ。初回down約1065msはray/coverage構築を含み、今回も残る。単回の合成モデル測定であり、任意の実モデルで同じ短縮率になる保証ではない。Core157件と通常/高密度Player試験は合格。

## 2026-09-12 GUIから非同期準備

`Windows-C1C-PreparationGuiFinal` / `Artifacts/Authoring-20260912-003814-87c823d218ba4c8e87b030b470839e03/report.json` は全GUI回帰とdense paintに合格。131,072tri、1280×800の同fixtureで準備待ち1,349.9ms、Ready後の初回down13.3ms、up37.5ms。前段snapshot版の初回down約984msから構築を切り離した。GUIは準備中・完了・失敗と再試行を表示する。

`preparationWaitMs` は最終camera調整の後、各stroke前にReadyを待った実時間の合計。先行したworker計算や最初のmodel setupを含む総準備CPU時間ではない。準備計算が消えたわけではなく、初回クリックへ同期構築を持ち込まなくなった。frame最大時間・GPU完了・実アバターの操作感はこの数値から保証しない。