# 複数材質 Bake

2026-09-12: Core、GUI出力、Unity receiver接続済み。Windows PlayerとUnity2022.3.22f1 Built-In/Linearで検証。

`MultiMaterialBakeStore` が `materials.nyaforge-bake.json` を生成する。profileは `static-standard-pbr-linear-straight-fade-material-slots-v1`、schemaVersionは1。既存mesh/Surface/単一Material profileは変更しない。

## 対応と所有

- `mesh`: 既存BakeManifest。文書/オブジェクトUUID、revision、rest transform、geometry/provenanceを保持。
- `submeshSlots`: dense render submesh順の元slotキー。例 `[3,9]`。meshのsubmesh数と一致し、重複/未定義参照を拒否。
- `slots`: 元slotキー順。各項目に `slot`, `materialNodeId`, `materialHash`, `baseColor`。未使用slot接続も保持。
- `materialHash`: canonical44-byte材質parameters blob。色、metallic、roughness、emission、alpha mode/cutoffは単一Materialと同じ契約。
- `baseColor`: 0または1descriptor。RGBA blob、PNG、UV binding/domainを保持。同一画像はcontent hashを共有し、別材質UUIDも保持。

同じ材質UUIDが異なるparameters/画像bindingを指すmanifestは拒否する。submesh番号で元slotを上書きしない。読取結果の配列はreadonly、PNG取得はcopy。全画像の検証が完了するまで読取結果を返さない。

## 検証と次工程

Coreで疎なslot3/9、画像あり/なし、native経由の再export、共有UUID、不正map/slot/型、破損PNGを確認。既存manifestは書込み失敗時に保持する。

GUIは出力を自動判別し、receiverは全payload検証後にowned folderを作る。使用slotをsubmesh順に割当、材質UUID/PNG hashごとに資源を共有。MaterialSlots.jsonが元slot/UUID/材質pathを保持する。保存したPrefab/資源と対応表、異なる二部位の画像/材質、可視描画、途中失敗の限定rollbackを検証済み。最新証拠はcurrent_task参照。既存importの更新、部位別GPU定量検査と全alpha組合せは未実装/未検証。
