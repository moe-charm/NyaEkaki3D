# 制作previewの更新と所有権

2026-09-12。文書/commandを正本とし、Unity側の表示資源は破棄・再構築可能なprojectionとして扱う。

## 形状の変更

`OwnedMeshProjection.Prepared` がmesh、root、材質、face highlight、頂点markerを作る。Prepare中の候補rootは非表示。Commitで旧rootを隠し、新rootへ交換する。Rollbackでは旧rootを再表示し、Disposeは成功時に旧資源、取消時に候補資源を破棄する。

## 色だけの変更

入力meshのcontent hashと配置が同じで、現在/候補のどちらにも別の最終結果overlayがない場合、`OwnedMeshProjection.ColorUpdate` を使用する。mesh/root/marker/face highlightを保持し、新しいbaseColor texture/materialだけを準備する。mesh hashは位置だけでなくUV・normal・index等を含む。形状や配置が変わる場合、overlayがある場合は従来の全体交換へ戻す。

- Prepareは新しい材質配列を作り、表示中のrendererには触らない。
- Commit前に対象projectionと旧BaseColorSurfaceの参照を検査する。別の候補が先に反映されていたら拒否する。
- Commitはrendererの材質と所有BaseColorSurfaceを交換する。途中失敗でもRollbackできるよう、最初の変更前にcommit状態を立てる。
- Rollbackは旧材質配列と旧BaseColorSurfaceを戻す。旧surfaceに描きかけのtextureがあれば、それも維持する。
- Disposeは成功時に旧surface、取消/未commit時に新surfaceを破棄する。mesh/rootの所有権は元のPreparedが保持する。Disposeの再呼び出しは無操作。

baseColorの追加/削除にも同じ処理を使い、削除候補では共通の無着色材質へ切り替える。色の取消でも文書や画像の正本を変更しない。

## 検証

`ColorProjectionVerification` は専用のprojectionを作り、Prepareの隔離、材質交換、mesh/rootの再利用、描きかけを含むRollback、未commit候補の破棄、先行commit後の古い候補拒否、色の削除とRollback、形状変更時の全体交換とRollbackを確認する。

高密度Player試験でも、Paint確定とUndo/Redoの前後で同じUnity mesh/rootが使われていることを検査する。処理時間の測定条件は [3Dペイントの負荷計測](Surface-Paint-Performance.md)。Core commandの履歴/再送や保存形式を変える対応ではない。
