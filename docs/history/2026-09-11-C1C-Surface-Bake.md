# NyaForge 開発タスク

更新: 2026-09-11。画像・材質付きSurface BakeとUnity Bridgeを実装。Core116件、Player、別Unity2022.3の受け取りが合格。全体は開発中。

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

- `Persistence/SurfaceBakeStore.cs`: 独立したsurface schema1。mesh metadata、確定画像blob、PNG、UV/domain、固定材質profileを保存。共通BakeSourceとmesh検査を再利用し、旧mesh-only writer/readerの契約を保持。
- 全依存を先に保存しmanifestを最後に原子的に公開。readはschema/profile、hash、寸法、UV0、PNGと元画素の一致を検査。未解決graphと画像なしを出力前に拒否。
- `UnityBridge/Editor/SurfaceImporter.cs`: 新規所有フォルダにPNG・Texture・Material・Prefabを生成。sRGB、無圧縮、mipmapなし、clamp/bilinear、白tint、不透明。全submeshに同じbase color。再import更新は今後。
- Viewerの「Unity用に書き出す」は画像付き最終出力ならSurface形式を選ぶ。受け取りウィンドウでは「画像付きSurface形式」をオンにする。操作と仕様は[使い方](docs/Authoring-Quickstart.md)・[Surface Bake形式](docs/Surface-Bake-Format.md)。
- Core **116 passed / 0 failed**。`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0b15c045426540c9afaa0eefc999a8f1`。scale1/100、mesh/image/hash、改変PNG、旧reader拒否、未解決拒否、既存manifest保持を検証。
- 最新ビルド `Builds/Windows-C1C-SurfaceBake/NyaForge.exe`。成功ログ `Logs/build-player-20260911-211917-292.log`。
- 最新Player pass `Artifacts/Authoring-20260911-211957-274b0069ef164537a5a24c9dd47d40ed/report.json`。保存再読込済みのPaint作品を `surface-export/surface.nyaforge-bake.json` へ出力し、mesh/PNGを照合。既存回帰も成功。
- **Unity 2022.3.22f1受け取り検証合格**: `Artifacts/BridgeReceiver-20260911-212013-269-c2e6d8f9bd61415fa9b95fdee8104797/bridge-report.json`。既存scale1/100を維持し、surfaceのmesh/UV、PNG byte、sRGB設定、寸法、Materialのtexture参照、serialized Prefabまで確認。
- この受け取り試験は技術fixtureであり、販売用小物の完成・ユーザーのOS操作受入・VRChat受入ではない。受け取り側の実描画の目視確認は未実施。

PNG単体出力の記録は[履歴](docs/history/2026-09-11-C1C-Png-Export.md)、それ以前のPaint GUI等は[依存表示までの履歴](docs/history/2026-09-11-C1C-Paint-Dependencies.md)。

## 次の作業: 小物制作の一周を確認し、C1残件を進める

1. GUIで形状編集→UV→色塗り→保存→Surface出力を通す、小物形状の統合fixtureを用意。今回のSurface出力はAPI検証なので、出力ボタンを含むpointer操作と受け取り側の描画も確認する。
2. Surface profileの材質/PNG改変・未知profile・画像blob破損・失敗時の受け取り側asset非作成を拡充。URPは実装経路ありだが専用受け取り試験は未実施。
3. 複数Paint段・複数材質、layer/opacity/mask、画像取込、3D paintを設計v2 §20.2に沿って拡張する。現在の1画像固定を完成要件にしない。
4. geometryの模様を保つUV再投影/rebake、再export identityの更新・利用者設定の保護を実装。明示UV再割当とは別機能。
5. C1のEvidence/MCP共通commandと残る造形操作を進め、C2の低ポリ全身・rig/weight/morph制作へつなぐ。全体目標は未完了。

再検証:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeAuthoring.ps1 -BuildName Windows-C1C-SurfaceBake -Width 1280 -Height 800
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeUnityBridge.ps1 -PlayerCheckDirectory <今回のPlayer検証出力> -SurfaceFixture
```

## モジュールと作業境界

- `Assets/NyaForge/Authoring/`: Unity非依存の文書・graph・command・geometry・paint・保存。
- `Assets/NyaForge/UnityRuntime/`: GUI、pointer入力、一時preview、Unity資源所有、Player検証。確定変更は共通commandを通す。
- `UnityBridge/`: 受け取り先Editor処理。Viewerの表示状態と制作正本を混ぜない。
- privateモデル・画像・packは `Z:/TextureVoice_local/git/RadDollV3-clothing` 側に保持し、公開repoへコピーしない。
- 既存の大量の未コミット変更を保持。今回commit/pushなし。既存Playerを終了・上書きしない。

保持する残件: Mirror中心切断・結合、厚みの品質と自己交差、UV pan/zoom・drag・seam・unwrap、複数object、skin/morph・rig・target出力。初期予算のUV表示2048面・画像1024角を最終製品要件と読み替えない。

詳細な過去記録は[文書一覧](docs/README.md)から参照。history内の「次」「未実装」は当時の状態であり、再開指示はこのファイルを優先する。






