# NyaForge 開発タスク

更新: 2026-09-11。高密度Player測定を追加し、同じpolygonの派生値を再利用して描き始め/確定を短縮。Core155件とPlayer合格。初回構築と確定時の待ちは残る。製品全体の開発は継続中。

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

- 高密度Player試験を `AuthoringWorkbench.DensePaintVerification` へ分離。256×256 quad / 131,072三角形で約739pxを16回のpointer moveで描く。初回/連続描画、仮表示と文書の分離、raw約3593点→保存2点、画素の連続性、1command、Undo/Redoを検証。
- `Tools/Test-NyaForgeAuthoring.ps1` に `-DensePaint` と `-TimeoutSeconds` を追加。既定は通常試験・120秒のまま。追加測定では240秒を明示。測定は `dense-paint-profile.json` へ書き、数値に速度合格の閾値は設けない。
- 実測から同じpolygonの三角形化・描画変換・形状/UV hash再計算を発見。`PolygonDerivedData` に遅延生成をまとめ、ConditionalWeakTableでimmutable polygon snapshotの寿命に合わせて再利用。形状/UV変更は新instanceなので別の結果。保存形式・hash値は維持。
- Core **155 passed / 0 failed**: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-e4ca0e69b6714c8e94eca452070cd699`。派生値と未cache値の一致、同一snapshot再利用、頂点移動/UV変更、元snapshot保持、同時読み取りを追加検証。
- 最新ビルド `Builds/Windows-C1C-PolygonCache/NyaForge.exe`。成功ログ `Logs/build-player-20260911-235505-847.log`。
- 最新Player合格 `Artifacts/Authoring-20260911-235528-bb5f08f2be8748289e10565f6137d14b/report.json`。同ディレクトリ `dense-paint-profile.json` / `dense-paint.png` が測定と表示の証拠。2本の連続した筆跡を目視確認。
- 修正前 `Artifacts/Authoring-20260911-235103-bb86db3212824e598c0fea42b2a5ec32/dense-paint-profile.json` と比較し、2本目down約929→13ms、up約2262→403ms。初回down約1046ms、初回up約425msは残る。単回測定であり一般的な速度保証ではない。[計測の条件と値](docs/Surface-Paint-Performance.md)参照。
- 初回の測定fixtureはMeshSourceでpolygon UV契約を満たさず失敗。PolygonSourceへ修正して再試験した。失敗証拠 `Artifacts/Authoring-20260911-234753-4cb6ca361f9d4eb2b85b4b3df8139f6d/report.json` を保持。
- 過去の詳細は [今回の整理前](docs/history/2026-09-11-C1C-Before-Dense-Player.md)、[3D入力契約](docs/Surface-Paint-Development.md)、[画像取り込み](docs/Paint-Image-Import.md)、[小物一周](docs/history/2026-09-11-C1C-Item-Workflow.md)へ集約。history内の「次」「未実装」は当時の状態。

## 次の作業

1. 高密度Playerの機能と負荷を確認し、polygon派生cacheを追加済み。[負荷計測](docs/Surface-Paint-Performance.md)で残った初回構築約1秒/確定約0.4秒の内訳を確認し、形状が変わらないPaint確定で不要なprojection再作成を減らす。曲がった長いstroke、多数の区間、ray4096回上限、camera変更後の構築費、退化したscreen交差とOS/DPI手動操作は残る。
2. Paintの代表PNG（palette/gray/16bit/interlace）、ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. 一般Material graph、複数材質、UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-PolygonCache -Width 1280 -Height 800 -DensePaint -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
