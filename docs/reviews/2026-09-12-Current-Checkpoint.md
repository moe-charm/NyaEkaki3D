# 現在地点の追加レビュー

対象: `023d1cf`。2026-09-12、ユーザー依頼「チェック後 current_task 更新してタスク化」。今回は確認と文書更新のみで、以下の不具合は未修正。

## 検証範囲

- Coreを再実行: **334 passed / 0 failed**。ログ `Logs/core-check-20260912.txt`、成果物 `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-5574074d05ed40d0a609ca1cbc83ad9d`。
- 取込骨対応、元node空間、Spring計算、Workbench取込と保存のコード、開発計画を照合。
- 既存Windows-NodeSpaceの `Artifacts/Authoring-20260912-143546-e00b3bf1383140b0a3d863c045a4e98d/report.json` は passed=true。今回Player/buildは再実行していない。実マウス、文字サイズ・欠け、実アバターの受入は未確認。
- 追加の合成入力で下記R11を再現。既存334件の成功だけではこの入力をカバーできない。

## R11 / P1 — 非joint nodeを挟むと骨の親子関係を失う

対象: `Assets/NyaForge/Authoring/Import/GlbSkinImport.cs:124`。

`BuildSkeleton` は直親がskinのjointにある場合だけParentBoneIdを設定し、それ以外を空文字にする。translation-onlyの通常nodeを間に置いた有効な木も拒否せず読み込むため、元の祖先jointとの接続が失われる。取込後の階層依存のpose操作・Spring追従が、元の木と異なる。

再現: 既存 `BuildMappedVrm(false)` のjoint列 `[1,2]` は保ち、node1の子をnode3へ変更。通常node3（identity transform）の子をnode2にする。`GlbSkinImporter.Read` は成功するが、node2のParentBoneIdはnode1のBoneIdではなく `""` になった。ログ `Logs/review-current-repro.txt`。この再現には外部モデルを使っていない。

完了条件: 中間nodeの変換を保持した最近傍祖先jointへの接続、または対応前の明示拒否を実装する。1個・複数個の中間node、登録順違い、親を動かした子の追従、native保存/Openを回帰化する。対応外を独立rootとして黙って受理しない。

## R12 / P2 — 読込失敗でSpringのセッションと表示が先行更新される

対象: `Assets/NyaForge/UnityRuntime/AuthoringWorkbench.Import.cs:49`、同 `:74`、`AuthoringWorkbench.VrmSpring.cs` の `SetImportedVrmSpring`。

metadataを読んだ直後、mesh/skinの検査前に共有フィールドとLabelを書き換える。その後、非identity回転などでskin readerが拒否すると、graph取込は失敗してもSpring設定の表示が残る。ここはコード経路で確認し、今回Playerでの再現は未実施。保存はworkspace attachmentsを使うため、この経路だけで失敗したモデルの設定が保存されるとは断定しない。

完了条件: graph・rig・expression・Springの候補を先に検査し、成功時にまとめて公開する。metadataが有効でもskinが不正な入力、再取込、command失敗について、失敗前の文書・attachments・sessionフィールド・表示・dirty/Undoを保持するPlayer回帰を追加する。

## 残作業の扱い

上記は不具合。collider座標adapter、center/時間契約、再生GUI、一般node変換、実素材受入は未実装・未受入の作業として区別する。具体的な順序と完了条件は `current_task.md` 冒頭のチェックリストを正本とする。過去のR01〜R10の修正履歴は保持し、追加レビューを理由に未実施の受入まで完了にはしない。

## R11修正追記

最近傍祖先jointを解決するImportedJointHierarchyを追加。2026-09-12の修正後Coreは338件合格。1/3個の中間node、skin slot登録順違い、親の移動/回転に伴う子の追従、native保存/Openを検証した。上記本文は修正前のレビュー記録として保持する。R12は未修正、実素材受入は未実施。最新のbuild証拠と再開順はcurrent_taskを参照。
