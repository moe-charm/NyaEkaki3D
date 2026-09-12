# NyaForge 開発タスク

更新: 2026-09-11。C1-Bで面全体の厚み付けを追加し、閉じた殻の作成から保存・BakeまでWindows Playerで検証。製品全体は開発中。

## 開発の入口

作業先: `Z:/TextureVoice_local/git/NyaForge`。Windows先行、macOSは将来。
読む順: このファイル → [開発計画](docs/Development-Plan.md) → [設計v2](docs/NyaForge-Authoring-Design2.md)の対象節 → コード。[文書一覧](docs/README.md)参照。
製品目標はVRキャラ・衣装・小物の造形から出力までの制作。小物の一周、低ポリ全身キャラの一周、品質向上の順。設計の記載だけを実装済み・依存採用済み・公開承認済みとしない。

## 現在の実装

- C0-R: 空project、最大1object、static頂点編集、共通Undo/Redo、保存、Bake／Bridge。
- C1-A: 型付きgraph、共通command、native schema3、runtime canvas、編集段、配置保存、graph Bake。自動制作往復の記録あり。手動受入は未確認。
- C1-B: polygon/cornerと安定ID、三角形化、RenderVertexMap／RenderTriangleMap、PolygonSource／PolygonEdit、stable-ID頂点移動、保存・出力。
- 領域押出し: 元の面・corner IDをcapへ引き継ぎ、頂点と境界壁に新規IDを割当。選択領域内部には壁を作らない。共通command・Undo・保存へ接続。
- GUI: 面モードのクリック選択、Shift追加／解除、mm入力、押出し。選択面の最小IDの法線を方向に使用。再読込後はPolygonEditを選び直して編集。

保存はstatic writer schema2、graph writer schema3、readerは1/2/3。profile変更は別フォルダ保存。旧triangle入力からquadやseamを推測復元しない。

## 今回の変更と証拠

最新は厚み付け。`Topology/PolygonSolidify.cs`、共通command、`UnityRuntime/AuthoringWorkbench.Solidify.cs`、専用Player検証を分離。元の面を保持し、平均頂点法線に沿う内側の面と境界壁を追加する。GUIは選択面だけでなく編集段全体が対象。鋭角の均一肉厚・自己交差修正・調整可能なmodifier nodeは未実装。

- Core: **89 passed / 0 failed**。`C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-553e4885aafd49259fc7d82c1e5e44fb`。四角面・折れ面の閉鎖、ID／属性、非manifoldと逆向き不整合拒否、scale1/100、再送・Undo・保存・Bakeを確認。
- 最新ビルド: `Builds/Windows-C1B-Solidify/NyaForge.exe`、`Logs/build-player-20260911-195931-199.log`。
- 最新Player pass: `Artifacts/Authoring-20260911-200016-d0c041a5d9204a61a993294560f34d08/report.json`。GUIクリックから20mmの厚み、6面・12三角形、全辺の面共有数2、Undo/Redo、保存・再読込、Bakeを確認。既存回帰も成功。
- 同ディレクトリの`solidify.png`を確認。厚みの操作欄と選択面・側面を描画。OS入力の手動受入、今回の形状の別Unity受け取りは未実施。

以下は前回までの表示・押出しの記録（最新値は上記）:

最新の変更は制作プレビュー表示。`FaceHighlightProjection.cs`が選択面の一時meshを所有し、`Resources/AuthoringSurface.shader`が画面上の幾何法線による陰影を担当する。元meshの法線・材質・制作文書・Bakeは変更しない。選択overlayはprojectionと一緒に破棄する。
面モードでは頂点マーカーを隠し、頂点IDと移動入力を無効化。「すべて選択」「選択解除」は面に作用し、画面上の操作案内も切り替わる。

- 最新ビルド: `Builds/Windows-C1B-FaceDisplay/NyaForge.exe`、`Logs/build-player-20260911-195417-780.log`。
- 最新Player pass: `Artifacts/Authoring-20260911-195443-d6f55c8e846b4dd9b7375daa9e1ff2db/report.json`。
- 同ディレクトリの`extruded.png`を確認。選択capがオレンジ、上と右の側面が陰影付きの青緑で分離して見える。面の選択数と入力欄は読める。
- 面pickで2三角形がhighlightされること、モード切替でoverlay解除・入力復帰すること、再読込後の選択で文書hashと保存状態が変わらないことをPlayer検証。既存の押出し・Undo・保存・Bake回帰も成功。
- Core変更なし。以下の83項目は直前の押出し実装時の結果であり、今回再実行していない。

前回の押出し基盤の証拠:

- `UnityRuntime/AuthoringWorkbench.FaceVerification.cs`を分離し、面pick・押出し・Undo/Redo・native保存再読込・BakeをPlayer検証へ追加。
- Coreに押出しscale100回帰を追加。既存scale1と合わせ、公開空間で50mmの押出しがlocal量へ変換されることを確認。
- Core: **83 passed / 0 failed**。出力: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-4e011e5d21384edb85fa5327fee054d7`。
- ビルド成功: `Logs/build-player-20260911-194945-742.log`。
- Player: `Builds/Windows-C1B-Extrude/NyaForge.exe`。
- Player pass: `Artifacts/Authoring-20260911-195041-e198e5d7527148f692a0527987eec23e/report.json`。四角面のクリック→20mm押出し→5面/10三角形→Undo/Redo→保存・再読込→Bakeを追加検証。既存static/graph回帰も成功。
- 同ディレクトリの`extruded.png`を1280×800で確認。斜め表示と面操作欄を撮影できた。単色unlit描画のため側面と上面の境界は見分けにくく、選択表示と合わせて改善が必要。
- 今回のPlayer検証はUI Toolkit pointerイベント。OS入力・手動の使い勝手・VRChat動作・押出し形状の別Unity受け取り確認は未実施。

## 押出しの制限

四角面からの押出しは上面と側面4枚、底が開いた5面。閉じた箱やsolidifyではない。側面UVは長さ基準の暫定配置。既存cap属性を保持し、sideの法線・接線を生成する。skin/morph転送、自己交差の解決、UV atlasは未実装。詳細は[Topology契約](Assets/NyaForge/Authoring/Topology/README.md)。

## 次の作業

1. 制作専用の陰影・選択面ハイライト・モード別案内は実装済み。次は辺編集・選択拡張などの工程に合わせて操作欄を整理する。陰影は制作確認用で、出力先の材質再現とは区別する。
2. 押出し境界条件を追加検証: 非manifold、共有辺の逆向き不整合、属性対応、再押出し、複数面・複数材質。無効候補の文書・Undo保持を確認。
3. C1-Bを継続: 厚み付けの初期実装は成立。次はmirror、単純UV、base color paintを小物完成例に必要な順で追加。厚みの法線・鋭角・自己交差とmodifier化は品質／再編集の残件として保持。EvidenceとMCPを共通commandへ接続する。小物のバリエーションだけに留まらずC2全身制作へ進む。

新しいコード変更ではCore、別BuildNameのPlayer、必要な受け取り検証を実施する。完了条件は設計v2の第5・6・19節と開発計画を維持し、未実装機能を完成扱いしない。

## モジュール境界と作業制約

`Assets/NyaForge/Authoring/`はUnity非依存のDomain・Graph・Topology・Commands・Projection・Persistence。`Assets/NyaForge/UnityRuntime/`は表示・選択・GUI・Player検証。`UnityBridge/`は受け取り先Editor処理。
多数の既存未コミット変更を保持。privateモデル・画像・packを元プロジェクトからコピーしない。commit/push未実施。ユーザーの既存Playerは終了せず、別BuildNameを使う。

旧詳細は[整理前スナップショット](docs/history/2026-09-11-C1B-Before-Preparation.md)に保持。その「次」「未実装」は当時の記述。現在の進捗の正本はこのファイル。
