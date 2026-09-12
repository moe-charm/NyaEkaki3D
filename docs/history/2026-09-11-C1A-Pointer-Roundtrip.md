# C1-A: UIイベント経由の制作往復

2026-09-11。PointerProbeはUnity runtimeのpanel.Pickで位置が対象に当たることを確認し、PointerDown/Move/Upイベントを送る。OSの実マウスを操作するものではなく、キーボード・IME・手動の使い勝手も検証範囲外。

Playerでノードの追加、出力→入力接続、Output指定、Plane寸法のApply、EditMesh選択、見出しドラッグ、配置保存、3D頂点pick、10mm移動、保存、再読込、書出しを一周。数値fieldの値は検証コードで設定し、Applyや保存等はUIイベントを使う。

この経路でWindows Playerの深い保存先への出力失敗を発見した。AtomicWriteが元ファイル名へGUIDを追加していたため、hash付きblobの一時名が長くなっていた。同じディレクトリに短い一意な一時名を作る方式へ修正し、atomic置換・CreateNew・Flushと掃除の契約を維持。

## 証拠

- Core **69/69** 再実行成功。
- Player `Builds/Windows-C1A-Pointer/NyaForge.exe`、build log `Logs/build-player-20260911-191853-902.log`。
- report `Artifacts/Authoring-20260911-191915-685099809a7a423b9a54873a2600751d/report.json` pass。
- 同artifactのpointer-projectはUI経由で保存したschema3 project。exports以下のBakeを読み、現在文書のmesh hashと一致することを確認。
- 不具合を再現した失敗reportは `Artifacts/Authoring-20260911-191759-e75b82196c4647a585faad5f287bddb4/report.json` に保持。

C1-Aの自動検証経路は成立。手動操作の受入と複数段の追加回帰は区別して残す。次のC1-Bは安定した制作vertex/edge/face/cornerと表示対応表の設計・実装。C1全体や製品全体の完成ではない。
