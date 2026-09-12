# NyaForge 開発タスク

更新: 2026-09-11。投影三角形とドラッグ線の交差からsampleを追加し、粗いsample間の極細遮蔽物を検出。near/far clippingと共通予算を維持。Core149件とPlayer合格。製品全体の開発は継続中。

## 開発の入口

作業先: `Z:/TextureVoice_local/git/NyaForge`。Windows先行、macOSは将来。
読む順: このファイル → [開発計画](docs/Development-Plan.md) → [設計v2](docs/NyaForge-Authoring-Design2.md)の対象節 → コード。[文書一覧](docs/README.md)参照。
製品目標は小物の制作・出力を一周し、低ポリ全身キャラ、品質向上へ進むこと。設計v2は製品方針、v1は背景資料。設計中の外部依存・機能は採用済みや実装済みを意味しない。

## 現在地

| 範囲 | 現物と確認状況 |
|---|---|
| C0-R／C1-A | 空project、最大1object、typed graph、共通commandとUndo、native保存、node canvas、static Bake／Bridge |
| C1-B | polygon/corner ID、面選択・押出し・厚み、Mirror、編集ケージと最終結果、UV投影と島の数値編集 |
| C1-C Paint | 2D brush、Image port、UV binding、1stroke Undo、native画像保存、3D baseColor、PNG／Surface Bake |
| Layer/mask GUI | 移行・追加・選択・並替・削除・表示・不透明度・名前、mask追加/削除と描画、取消、Undo、保存を検証 |
| Image import | PNG検査・展開・縦横比保持サイズ調整・新layer追加・Undo・保存を検証。Windows pickerの実操作は手動未確認 |
| 3D paint | BVH ray、論理edge/UV連続判定、screen補間、切れた区間の描画、GUI色/mask・仮表示・取消・Undo・保存/出力を実装 |
| 今後 | 3D複数面/細かなseamの精度と操作、一般Material graph、UV再投影、出力identity更新、Evidence/MCP、rig/weight/morphと全身制作 |

保存はstatic writer schema2、graph writer schema3、reader 1/2/3。Paintは現在mesh全体に1合成画像、不透明preview。UV変更時は旧payloadを未解決として保持し、明示rebindで対応を更新する。見た目を保つ再投影ではない。旧mesh-only Bakeは画像を拒否。画像付きmeshは別のSurface Bake profileで出力する。

## 今回の変更と証拠

- `SurfaceScreenProjection` にworld面のnear/far clipping、`SurfaceScreenCoverage` に投影面の2D BVHとドラッグ線の区間交差を分離した。粗いsample間の出入りと中点を追加し、最前面は既存rayで確定する。
- Unity側はpointer downでcamera/viewportのcoverage snapshotを作る。camera更新・viewport変更ではgesture取消。rayの到達距離もfar平面へ合わせた。追加sampleは既存ray/UV予算を共有し、超過時は部分確定しない。
- Core **149 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-9befd50d6dab48c3b9c78f92d27cc9d5`。0.02画面pxの遮蔽物でcoverageなし1区間/あり2区間を比較。near/far clipping、投影予算とgesture invalid化を追加検証。
- `thin-occluder` Player fixtureは幅0.00002mの範囲外UV面。中央クリックで奥を塗らずrevisionも増えないこと、横切るstrokeを2区間に分けて1commandにすること、Undo/Redo・native保存・Surface出力を確認。筆の半径より細いため、両側の画素coverageが重なることとrayの塗り抜けを混同しない。
- 最新ビルド `Builds/Windows-C1C-ScreenCoverage/NyaForge.exe`。成功ログ `Logs/build-player-20260911-232821-703.log`。
- 最新Player合格 `Artifacts/Authoring-20260911-232854-e9b3879e4e1a4f928b5e4c1198d54249/report.json`。`surface-boundary-thin-occluder` / `-export` / `.png` が追加証拠。既存色/mask・境界・取消・保存/出力の回帰も合格。
- 投影BVHは現状gestureごとに構築。高密度モデルの負荷・float座標限界・viewport resizeの専用試験は残る。手動受入や製品完成とは区別する。変更前の記録は [履歴](docs/history/2026-09-11-C1C-Before-Screen-Coverage.md)。commit/pushなし。

## 前段: 検出した境界の精密化

- `SurfaceBoundaryRefinement` をCoreの独立モジュールとして追加。三角形/UV有効性/背景の変化を検出した区間を1/64画面px（深さ12まで）へ二分。細かな中間面をたどり、境界の手前で筆跡が途切れすぎる問題を軽減する。
- `SurfaceStrokeSampler` は粗いsampleと出力UVを分けて保持。追加探索も既存4096ray/1024UV予算とsnapshot所有検査を共有。追加探索が失敗するとgesture全体がinvalidになる。
- Core **146 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-432366b0cede4db18dcf9714a3136543`。隙間の両側へ0.02px以内まで近づくこと、16px幅の32帯/64三角形を1本で塗ること、追加探索で初めて遭遇するforeign hit拒否を追加確認。
- 初回の隙間fixtureでは粗いsampleの両端が同じ三角形に当たり、隙間が完全に隠れて見逃されることを確認した。今回の処理は観測した境界の精密化なので、検出可能な境界fixtureへ修正して試験を分離。隠れた形状の検出自体は次の作業に維持する。失敗証拠 `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0e20648844a64c38abe0e022ff76fee8`。
- 当時のビルド `Builds/Windows-C1C-SurfaceRefinement/NyaForge.exe`。成功ログ `Logs/build-player-20260911-232043-169.log`。
- 当時のPlayer合格 `Artifacts/Authoring-20260911-232115-8621ef2604cc4174ac5c9ea894f3dd9d/report.json`。色/mask・境界3種・取消・Undo・native保存・Surface出力と不透明camera画素の回帰を確認。
- 前段の詳細は [整理前の記録](docs/history/2026-09-11-C1C-Before-Surface-Refinement.md)。UI手動受入や密な実モデルの描き心地は未確認。commit/pushなし。

## 前段: 複数面と不透明preview

- `SurfacePaintFixtures.cs` と `AuthoringWorkbench.SurfaceBoundaryVerification.cs` を分離して追加。共有edgeのUV seam、背景の隙間、範囲外UVの手前の面を使い、実カメラの3D pointer入力で2区間への分離を検証。両側の色画素、中央の未描画texel、1revision、Undo/Redo、native保存とSurface出力を確認。
- 手前の塗れない面への単独クリックで、奥の面を塗らずrevisionも増えないことを確認。幅のあるfixtureの結果であり、subpixel遮蔽物の精度は未達。
- スクリーンショットから不透明profileのshaderが画像alphaをviewportに渡していた不具合を発見。`AuthoringSurface.shader` の出力alphaを1に固定。画像データと出力のalphaは保持。camera target中央のRGB黒/alpha255を読み、修正後の画像でも背景の色が消えたことを目視確認。
- 当時のビルド `Builds/Windows-C1C-SurfaceBoundariesOpaque/NyaForge.exe`。成功ログ `Logs/build-player-20260911-231541-493.log`。
- 当時のPlayer合格 `Artifacts/Authoring-20260911-231603-25b6c0b288ad41368b6a4ed5338b0b10/report.json`。同ディレクトリの `surface-boundary-{seam,gap,occluder}.png` / native保存フォルダ / `-export` が証拠。
- 修正前の境界GUI試験自体も合格: `Artifacts/Authoring-20260911-231158-4d67ce669e95485aa513aa6204a2e8dd/report.json`。この時の画像で表示不具合を発見し、上記の描画画素試験を追加して再検証した。単なる試験合格を描画品質の保証とはしない。
- Coreの変更なし。143件は前段の既存結果。今回はPlayer試験で新しい入力経路とshaderを確認。変更前の記録は [履歴](docs/history/2026-09-11-C1C-Before-Surface-Boundaries.md)。commit/pushなし。

## 前段: 描きかけの保存と取消

- `UpdateCamera` とmask筆の変更で3D gestureを取消。`PointerProbe` にbutton/modifier指定を加え、Alt回転・右移動を実際のUI Toolkit入力で検証。
- `AuthoringWorkbench.SurfaceLifetimeVerification.cs`: 描きかけ中のnative save/PNG/Surface APIが確定画像のみを使うこと、画像/mask・layer・mask筆・全体表示の変更で取消されること、取消後のpointer upが文書を変えないことを確認。右ドラッグで描画を中断して移動できることも確認。
- 当時のビルド `Builds/Windows-C1C-SurfaceLifetime/NyaForge.exe`。成功ログ `Logs/build-player-20260911-230207-545.log`。
- 当時のPlayer合格 `Artifacts/Authoring-20260911-230303-c321c5c9a7a34feeacaf15a3f70a3a7e/report.json`。`surface-pending-save` / `surface-pending-png` / `surface-pending-export` は描きかけ中の出力証拠。保存/出力はpointer capture中のAPI操作として検証し、同時にGUIボタンをクリックできたとは主張しない。
- 今回Core実装の変更なし。Core143件の結果は下記の3D GUI導入時のもの。新しいPlayer検証と混同しない。

## 前段: 3D GUIの導入と検証

- `SurfaceUvContinuity` は論理頂点IDとedge両端UVを比較。分裂したrender頂点を扱い、位置が一致するだけの別面をつながない。`SurfacePaintHit` は生成元snapshotを保持。
- `SurfaceStrokeSampler` は画面入力を既定2px間隔で補間。背景・範囲外UV・不連続edgeで区間を切る。4096 ray samples/1024 UV pointsを共有し、失敗したgestureは部分確定できない。
- `AuthoringWorkbench.SurfacePaint` は最終出力へ直接つながるlayered Paintを3D入力へ接続。左で描画、Alt+左で回転、右で移動。半径はtexture px。BVHはmesh/transform/UV対応が変わるまで再利用。
- `BaseColorSurface` は仮textureを正本と分離して所有。取消で復元、確定前の文書や出力は変えない。gesture取消時に参照/captureを解放。
- Core **143 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-9d4c1b05f623407090a71e2b7a4ae276`。
- 3D GUI導入時ビルド `Builds/Windows-C1C-SurfacePaint/NyaForge.exe`。成功ログ `Logs/build-player-20260911-225606-828.log`。
- 3D GUI導入時Player合格 `Artifacts/Authoring-20260911-225638-a7742041e41a4a58a292cab95df8c380/report.json`。3Dドラッグ仮表示と文書隔離、カメラ不変、1revision/Undo/Redo、Escape/capture喪失、mask描画と色画素保持、native再読込、合成Surfaceを確認。
- 同ディレクトリ `surface-paint.png` で2層の色strokeをmaskで薄くした表示を目視確認。`surface-paint-project` は編集可能、`surface-paint-export` は出力。
- 初回回帰は旧UV rebind試験がパネル展開前の位置へScrollToして失敗。展開のlayout後にscrollし直すよう検証を修正して再合格。3D専用検証は初回も通過。証拠 `Artifacts/Authoring-20260911-225305-74ff9efc552c4983bd0a791a6baeb725/report.json` はfailedとして保持。
- GUI試験はUI Toolkit pointerイベントとコードによる設定を使う。OSマウス手動受入、複雑なmeshでの描き心地、製品完成の証拠とは区別する。

前段の詳細は[整理前の記録](docs/history/2026-09-11-C1C-Before-Surface-Gui.md)、[切れた線分](docs/history/2026-09-11-C1C-Stroke-Paths.md)、[画像取り込み](docs/Paint-Image-Import.md)、[小物一周](docs/history/2026-09-11-C1C-Item-Workflow.md)。history内の「次」「未実装」は当時の状態。

## 次の作業

1. [3Dペイント開発契約](docs/Surface-Paint-Development.md)に沿い、viewport寸法変更の専用試験と、細い面/遮蔽物の精度を進める。複数面・UV seam・背景・範囲外UVの手前の面を横切るgestureは幅のある合成fixtureでPlayer確認済み。検出した境界の適応的sampleは追加済み。投影面との交差による区間内部の検出も追加済み。次はviewport寸法変更の専用試験、screen座標の退化ケースと高密度meshでの負荷を確認する。精密化の限界でも非隣接面へ飛ぶ場合は保守的に区間を切る。
2. Paintの代表PNG（palette/gray/16bit/interlace）、ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. 一般Material graph、複数材質、UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-ScreenCoverage -Width 1280 -Height 800
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。



