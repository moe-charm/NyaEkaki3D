# NyaForge 開発タスク

更新: 2026-09-11。確定Paint画像のPNG出力を実装。Core 113件・Player検証が合格。画像・材質付きmesh出力は次の作業。

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
| 今後 | texture付き外部出力、layer/mask、UV変更前の依存表示・再投影、一般Material graph、MCP、rig/weight/morphと全身制作 |

保存はstatic writer schema2、graph writer schema3、reader 1/2/3。設計versionと保存schemaは別物。Paintは現在mesh全体に1画像、不透明preview。UV変更時は旧payloadを未解決として保持する。画像付きmeshの既存Bakeは非対応を明示して拒否する。

## 今回の変更と証拠

- `Paint/PaintPng.cs`: Unity非依存のRGBA8 PNG encoder。sRGB chunk、straight alpha、PNG上からの行順。stored DEFLATEで実行環境に依存せず同じbyteを生成。圧縮率は低いが初期最大1024角を保証範囲とする。
- `Persistence/PaintPngExport.cs`: 確定文書から選択Paint段を再評価し、PNGとrevision／画像hash／UVhash／domain付きmanifestを書き出す。画像を先、manifestを最後に原子的に公開。未解決入力はフォルダ作成前に拒否。既存PNG改変はhash検査で拒否。
- `UnityRuntime/AuthoringWorkbench.PaintExport.cs`: 「確定画像をPNGで書き出す」を追加。制作パス内exportsへ出力しフルパスを表示。編集・Undoには影響しない。
- Core **113 passed / 0 failed**。`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-ee6d7531ff424691b37d34452bc9cb91`。独立したZLibStreamで複数DEFLATE blockを展開し上下・RGBA全画素を比較。出力のrevision/hash、同内容再出力、改変検出、未解決拒否も確認。
- 最新ビルド `Builds/Windows-C1C-PngExport/NyaForge.exe`。成功ログ `Logs/build-player-20260911-211230-342.log`。
- 最新Player pass `Artifacts/Authoring-20260911-211258-640dca04761a4166886aaa78c271367b/report.json`。Core PNGをUnity画像decoderで開き全画素を比較。GUIボタンからのPNGと確定画像が一致し、文書不変を確認。既存回帰も成功。
- 同フォルダの `paint-ui-project/exports/paint-d1a8fe74849141a78edad3633496c36e/37dce3b74b9dc6f1a7e2110acf6df454c2e301af5fa46d2a12e862427bed523c.png` を目視確認。ピンクの線の単独PNGとして開けた。
- [使い方](docs/Authoring-Quickstart.md)を更新。今回のPNG単体出力は画像・材質付きmesh出力の完成を意味しない。OS手動受入、画像付きBridge、VRChatは未確認。

Paint GUI・UV再割当・依存表示の既存検証は[履歴](docs/history/2026-09-11-C1C-Paint-Dependencies.md)へ移動した。

## 次の作業: 画像・材質付き出力

対象設計: v2 §5.3、§7、§19、§20.2、§21。

1. `BakeStore.cs`／`Persistence/BakeSource.cs`とBridgeのschema1契約を確認。画像付きprofileは別schemaまたは明示profileとして設計し、既存schema1 readerとfixtureを保護する。
2. 明示rebindは初期実装・検証済み。複数Paint段・複数の依存経路と、上流編集を含むGUI回帰を追加する。画像の再投影・rebakeとは呼ばない。
3. PNG単体出力は実装済み。画像・材質付き出力profileを実装し、保存画像と受け取り側の対応を検証する。既存mesh-only Bakeの拒否を黙った画像欠落に変えない。
4. geometryへ同じ模様を保つrebakeは別途snapshotと投射契約を設計・実装する。一般Material graph、layer/mask、texture付き出力の残件を維持する。
5. 色付き小物の制作→保存→画像・材質付き外部出力を一周させ、Evidence／MCP、C2全身制作へ進む。

再検証:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-PngExport -Width 1280 -Height 800
```

次の変更でCoreを触ればCore試験、出力を触れば対応するBridge試験を追加。既存Playerは終了せず別BuildNameを使う。

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚みの品質と自己交差、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。初期予算のUV表示2048面・画像1024角を最終製品要件と読み替えない。

詳細な過去記録は[文書一覧](docs/README.md)から参照。history内の「次」「未実装」は当時の状態であり、再開指示はこのファイルを優先する。





