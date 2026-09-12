# NyaForge 開発タスク

更新: 2026-09-11。Layer/maskのasset保存・読込を実装。Core122件合格。graph/native/Undo/GUI統合は未完了。

## 開発の入口

作業先: `Z:/TextureVoice_local/git/NyaForge`。Windows先行、macOSは将来。
読む順: このファイル → [開発計画](docs/Development-Plan.md) → [設計v2](docs/NyaForge-Authoring-Design2.md)の対象節 → コード。[文書一覧](docs/README.md)参照。
製品目標は小物の制作・出力を一周し、低ポリ全身キャラ、品質向上へ進むこと。設計v2は製品方針、v1は背景資料。設計中の外部依存・機能は採用済みや実装済みを意味しない。

## 現在地

| 範囲 | 現物と確認状況 |
|---|---|
| C0-R／C1-A | 空project、最大1object、typed graph、共通commandとUndo、native保存、node canvas、static Bake／Bridgeの既存検証記録あり |
| C1-B | polygon/corner ID、面選択・押出し・厚み、Mirror、編集ケージと最終結果、UV投影と島の数値編集まで実装 |
| C1-C Paint基盤 | immutable画像、Image port、Paint node、UV binding、1stroke Undo、画像blob付きnative保存、3D baseColorの既存Player合格レポートあり |
| Paint GUI | 色・半径・不透明度と2D brush、確定時の3D更新を実装。pointerドラッグ、1command／Undo、キャンセル・取得喪失・Escape、native再読込のPlayer検証合格。OS手動受入は未確認 |
| 今後 | layer/mask、UV再投影、一般Material graph、出力identityの更新、MCP、rig/weight/morphと全身制作 |

保存はstatic writer schema2、graph writer schema3、reader 1/2/3。設計versionと保存schemaは別物。Paintは現在mesh全体に1画像、不透明preview。UV変更時は旧payloadを未解決として保持する。旧mesh-only Bakeは画像を拒否。画像付きmeshは別のSurface Bake profileで出力する。

## 今回の変更と証拠

- `Persistence/PaintLayersCodec.cs`: NYFL v1 metadataとPaintLayersStore。canvas寸法、下→上の順序、ID/名前/opacity/visible、image/mask hashを保存。依存blobを先、metadataを最後に公開。
- `PaintMaskCodec.cs`: NYFM v1、linear coverage8、左下原点。既存NYFI v1画像blobを共用する。
- metadata全体の長さ、ID、UTF8、bool、hash、重複、総payload予算を依存読込前に検査。画像とmaskの寸法はpixel確保前にcanvasと照合する。
- PaintLayersの予算計算を共有し、codecとモデルで別々の基準を持たない。32MiBは保持payload見積りで、decode/合成の一時allocationは別途。
- Core **122 passed / 0 failed**。`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-230bc6d14c8f49b9a5fdaabf488976bb`。非表示画像、mask、順序、名前、合成画素とhashの往復、改変画像、metadata破損、未知version、不正flag、mask profileを検証。予算超過・metadata不正でdependency読込0回を確認。
- `UnityRuntime/PaintLayerAssetsVerification.cs`: 3layer、gradient mask、非表示画像をasset保存し、Playerで再読込・hash・合成画素を確認する検証を追加。
- 最新ビルド `Builds/Windows-C1C-LayerAssets/NyaForge.exe`、成功ログ `Logs/build-player-20260911-214137-720.log`。Player pass: `Artifacts/Authoring-20260911-214211-ba62ecfc828c4b5f918050d43de0ddb0/report.json`。`layer-composite.png`を目視確認し、左から右へ強くなるmask付きの線を確認。既存GUI/PNG/小物出力回帰も合格。
- **PaintLayersStoreは単体asset API**。layer付きnative project、graph所有、共通Undo、GUI一覧は未接続。旧image.paint v1と1画像GUIは維持。

Core合成の記録は[Layer Core履歴](docs/history/2026-09-11-C1C-Layer-Core.md)。小物GUIから別Unity描画までの証拠は[小物一周の履歴](docs/history/2026-09-11-C1C-Item-Workflow.md)。

## 次の作業: Layerを制作正本へ接続する

対象: 設計v2 §5.1／5.3／7／20.2。

1. layer/maskのasset codecは実装済み。GraphBinaryCodecのblob callbackへ接続し、ProjectStoreが全依存を所有して作品ごと保存・再読込できるようにする。単体asset保存をnative project保存と取り違えない。
2. version付きのlayered Paint nodeをgraphへ統合。旧image.paint v1の保存byteと読込を保護する。未知versionは既存どおりopaque保持。移行は旧画像を1layerとして明示的に行う。
3. layerの追加／削除／順序／表示／opacity／mask／strokeを共通commandへ接続。contextはlayer IDとstack hash、UV/domainを固定。1strokeのUndoと再送、native保存再読込を確認。
4. GUIのlayer一覧と編集対象を追加。表示・PNG・Surface出力は同じ合成結果を使用。未確定stroke previewは確定stackへ混ぜない。非表示layerやmaskを保存後も保持する。
5. image import・3D paint・複数材質・UV再投影、Surfaceの異常入力と受け取り側rollback、出力identity更新を進める。C1のEvidence/MCP・造形残件とC2 rig/weight/morphを維持する。

再検証:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-LayerAssets -Width 1280 -Height 800
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚みの品質と自己交差、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。初期予算のUV表示2048面・画像1024角を最終製品要件と読み替えない。

詳細な過去記録は[文書一覧](docs/README.md)から参照。history内の「次」「未実装」は当時の状態であり、再開指示はこのファイルを優先する。











