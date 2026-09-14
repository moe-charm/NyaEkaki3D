# Nya Ekaki 3D Windows v1 手動受入チェック

この文書は、自動fixtureの合格を実アプリ・実アバター・VRChatの受入へ読み替えないための記録用チェック表。対象candidateは `Builds/BoneSubsetV5/NyaForge.exe`（コード `b55f42c`）。Authoring suiteの自動navigation／Save As・保存状態・再開・viewport確認は50回反復し、`Artifacts/Navigation-Repeated-PerformanceV39-official-50.json`へ記録した。実RadDollV3一周はBoneSubsetV3のprivate候補で確認した。証跡は`Artifacts/Authoring-20260914-152725-f668eee74c7f41a08712e014c37c2619/report.json`、`Artifacts/BridgeReceiver-20260914-153001-201-d4c23163fb89454a8c9ad0bcfaf411a9/bridge-report.json`。入力モデルは公開ツリーへコピーせず、privateの作業場所から読み込む。

## 1. 実EditorWindow（Windows）

- [ ] Windows DPI 100%で起動し、右側controlsのラベル・ボタン・statusが読める
- [ ] DPI 150%で同じ操作を行い、折りたたみとスクロールで下段が操作できる
- [ ] 日本語IME入力、空白を含むプロジェクトパス、Explorerのファイル選択が通る
- [ ] `GLBモデルを取り込む` から対象VRM/GLBを選び、候補mesh／skin／node instanceを確認する
- [ ] bodyを参照保護へ切り替え、頂点編集と納品対象追加が止まることを確認する

## 2. 衣装一周（実RadDollV3）

1. avatarを取り込み、bodyのgraph objectを参照保護にする。
2. Cuffまたはチョーカーを新規作成するか、別static GLBを読み込む。
3. 頂点を実マウスで選択・移動し、材質／UV／Paintを調整する。
4. `衣装をavatar骨格へskin-bind` → `自動weight初期化（avatar表面）` → Rigでweightを手修正する。
5. avatar面領域と衣装頂点を必要な範囲だけ指定し、`fit状態を測定（変更なし）` を押す。評価頂点数、移動量、裏側候補数を記録する。
6. 裏側候補がある場合は、面領域・offset・頂点選択を見直してからfitし、front/back/left/right/斜めで目視する。
7. 肩上げ、肘曲げ、前屈、着座相当のposeで、袖・襟・裾・胸周りの交差とweight崩れを確認する。
8. native Save → アプリを閉じる → Explorerから再Openし、形状・材質・weight・参照保護・対象allowlistが一致することを確認する。
9. 選択衣装だけのskinned packageを出力し、manifestのobjectId／mesh hash／skeleton hashを記録する。
   - 新規packageでは、実際にweightが参照する骨と祖先だけがskeleton sidecarへ入る。古いpackageで171本など不要な骨が残っている場合は、最新Playerで再出力する。
   - Unity受け取りでは `候補を生成（名前・階層）` → 候補一覧を確認 → `候補を割当に反映` の順に進め、曖昧・未検出のBoneIdは手動で確定する。
10. base-colorが取込時に縮小された場合、原画像サイズ・作業画像サイズ・MIME・hashをinspectionで確認する。Paintを編集しない状態ではGLB内の画像bytesが原画像と一致し、編集後はbounded previewへ切り替わることを確認する。

## 3. Unity受け取り

- [ ] Unity 2022.3.22f1の受け取り側でmanifestを読み込み、stable BoneIdを手動割当する
- [ ] avatar rootを移動・回転・scale変更した状態で適用し、衣装がavatar-local位置を保つ
- [ ] 衣装Aを適用 → Bへ更新 → Unity再起動 → 割当を読込し、Aの重複やownership喪失がない
- [ ] 削除後に割当だけを読み込め、Undoで管理objectと資産が戻る
- [ ] normal／MRの見た目、Repeat／Clamp、alphaを受け取り側で確認する
- [ ] 未編集base-colorの原画像再出力と、編集後previewへのフォールバックをmanifest／GLBの画像MIME・寸法・hashで確認する

## 4. VRChat

- [ ] 対応するVRChat SDK／Unityプロジェクトを明記し、Build & Testが成功する
- [ ] PC向け実機または試験アバターで、rest pose・肩上げ・肘曲げ・前屈・着座を確認する
- [ ] PhysBonesは許可コンポーネントだけで構成され、未対応componentを持ち込まない
- [ ] VRChat内の表示、貫通、負荷、アップロード後の再現を記録する

## 記録欄

- candidate build / commit:
- Windows / DPI / GPU:
- avatar source hash（private記録）:
- fit inspection JSON / screenshot:
- native project path:
- clothing package manifest / hash:
- Unity SDK / receiver project:
- VRChat Build & Test結果:
- 未受入・再現条件:

「fit状態を測定」の裏側候補値は最近面のwindingによる保守的なサンプルであり、三角形交差・閉じた体積の内外判定・貫通ゼロを証明しない。画像の可愛さや販売品質も、数値検査とは別に人間またはAIの確認結果として記録する。





