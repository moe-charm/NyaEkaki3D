# NyaForge 開発タスク

更新: 2026-09-12。Paintの色だけの変更で表示mesh/rootを保持する交換処理を追加し、取消とUndo/Redoを検証。Player合格。確定約0.4秒の待ちは残り、次は文書/GUI更新の内訳を計測。製品全体の開発は継続中。

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

- `OwnedMeshProjection.ColorUpdate` を別partialモジュールとして追加。mesh content hash/配置が同じで最終結果overlayがない場合、mesh/root/markerを再作成せず材質だけ交換する。形状変更やoverlay表示は従来の全体交換を使う。
- Prepareで独立texture/materialを作り、Commitで交換。Rollbackは旧BaseColorSurfaceと描きかけtextureを含めて復元。成功時は旧surface、取消/未commit時は候補surfaceだけを破棄する。先行commit後の古い候補は拒否。[所有権の契約](docs/Projection-Updates.md)参照。
- `ColorProjectionVerification` を追加し、Prepare隔離、参照再利用、描きかけを含むRollback、候補放棄/stale拒否、色削除、形状変更とRollbackを検証。DensePaintでもPaint確定/Undo/Redoでmesh/rootが同一参照のままか検査。
- 最新ビルド `Builds/Windows-C1C-ColorProjection/NyaForge.exe`。成功ログ `Logs/build-player-20260912-000212-152.log`。
- 最新Player合格 `Artifacts/Authoring-20260912-000254-cfa65629d16e4b8bb3248b6d273e6869/report.json`。同フォルダの `dense-paint-profile.json` が再計測の証拠。初回down約1038ms/up約412ms、2本目down約13.2ms/up約409ms。
- 旧up約425/403msと比べ、明確な速度改善はない。不要なmesh/root交換は解消したが、約0.4秒の確定待ちは残る。次は文書/hash/評価/GUI更新の内訳を測る。[計測資料](docs/Surface-Paint-Performance.md)参照。
- Core変更なし。Core155件は前段の既存結果 `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-e4ca0e69b6714c8e94eca452070cd699`。今回はUnity側の資源交換をPlayerで検証した。
- 前段の詳細は [整理前の記録](docs/history/2026-09-12-C1C-Before-Color-Projection.md)、[3D入力契約](docs/Surface-Paint-Development.md)、[画像取り込み](docs/Paint-Image-Import.md)、[小物一周](docs/history/2026-09-11-C1C-Item-Workflow.md)。history内の「次」は当時の状態。

## 次の作業

1. 高密度Playerの機能と負荷を確認し、polygon派生cacheと色だけのprojection交換を追加済み。[負荷計測](docs/Surface-Paint-Performance.md)で残った初回構築約1秒/確定約0.4秒について、command内の文書/hash/評価とGUI更新の内訳を計測する。曲がった長いstroke、多数の区間、ray4096回上限、camera変更後の構築費、退化したscreen交差とOS/DPI手動操作は残る。
2. Paintの代表PNG（palette/gray/16bit/interlace）、ネイティブpicker、ICC変換/他形式/1024超と、大きなlayer stackの性能は残件として保持。
3. 一般Material graph、複数材質、UV再投影/再ベイク、Surface異常入力とreceiver rollback・URP確認、出力identityを保持する更新を進める。
4. C1 Evidence/MCP、造形残件、C2 rig/weight/morphと全身制作を維持する。小物バリエーションだけを増やし続けない。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-ColorProjection -Width 1280 -Height 800 -DensePaint -TimeoutSeconds 240
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚み品質と自己交差、cut/merge/bridge/delete/create/curves、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。UV表示2048面・画像1024角・layer16枚/保持payload32MiBを最終製品要件と読み替えない。
