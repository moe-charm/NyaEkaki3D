# commandの計測とグラフ内容identity

2026-09-12。Paint確定の待ち時間を、文書側とUnity側の処理に分けて測る。

## 明示した計測のみ

`AuthoringCommandService.Execute` の省略可能な `CommandTimings` は、validation、candidate作成、評価、projection prepare/commit、文書/history commit、cleanupの時間を記録する。成功/失敗を変えるcallbackは持たず、指定しなければ時計を読まない。lock取得後から測るため、他threadのlock待ちは含まない。途中失敗時には未到達phaseが0になり、残りの時間がcleanupへ入ることがあるため、phase比較には成功したcommandを用いる。

Unity側の `AuthoringWorkbench.CommandMeasurement` はenvelope作成とGUI更新を追加計測する。高密度試験のpointer up中だけ有効化し、結果を `dense-paint-profile.json` に含める。GUI計測は同期のRefresh/選択/status更新で、後続layoutやGPU完了までの時間ではない。

初回証拠: `Artifacts/Authoring-20260912-000822-8cbefde3551d43e89562c4a7492a8b72/dense-paint-profile.json`。2本目のup約404msの内訳はenvelope約116ms、validation約130ms、candidate約141ms、評価約10ms、projection約0.7ms、GUI約5ms。描画資源の交換が主因という推測は支持されず、同じgraph identityの再計算が支配的だった。

## identity専用cache

`GraphContentIdentity` はimmutableなAuthoringGraph instanceごとにcanonical hashを遅延生成して再利用する。新しいgraphへ変更すると別entryとなる。

`GraphBinaryCodec.EncodeIdentity` はimmutable GraphNodeごとの小さなwire payloadを再利用する。Paintだけ変更したgraphでは、変わらないPolygonSourceをidentity確認のたびにbinary化し直さない。最終graph bytesの並び・payload上限・magic/versionは従来どおり。

実際の保存用 `GraphBinaryCodec.Encode(graph, addBlob)` は別経路で、毎回すべての参照blobについてcallbackを呼ぶ。identity cacheを理由に新しい保存先へのmesh/画像/stackの書き込みを省略しない。cacheはConditionalWeakTableでinstanceの寿命に対応し、内容hashをキーとする永続registryにはしない。

## 検証

Coreではcanonical writerとのhash一致、2回の保存でcallback回数が変わらないこと、保存blobからの復元、Paint変更時のidentity更新と元graph保持を検査する。計測付きcommandのcommit/再送も従来どおり成立することを確認する。最新結果と残件はcurrent_task.mdを参照する。

修正後はCore157件、Player `Artifacts/Authoring-20260912-001203-80ab647377c34feeab7efe4bf1c29e05/report.json` が合格。2本目upは約404→33ms、envelopeは約116→0.009ms、validationは約130→0.033ms、candidate作成は約141→14.75ms。保存/出力回帰も同じPlayer試験に含めた。初回ray/coverage構築の待ちは別の残件。
