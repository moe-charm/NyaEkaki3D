# VRM1 previewの所有者

`Vrm1SpringPreview` は一つのSpring/rig sessionを固定し、topology、実行chain、collider変換、center変換、固定step controllerを接続する。作品やsource poseを変更せず、出力Poseと一時Stateを公開する。

## フレームと再構成

- constructor/Resetでsource・graph・骨対応を検査し、chain、半径、center frame、初期controllerを構成する。全候補が成功してから置き換える。Springなし・未対応形式のモデルは再生成功に見せず診断する。
- Advanceで現在graphのskeleton対応と入力poseを検査し、colliderを現在poseへ変換し直す。`VrmSpringCenterAdapter` は元node原点を用いてcenter座標を解決する。bone Headとの差を無視しない。
- center指定なしはIdentityで、現在の固定avatar座標をworldとみなす。world内を移動するavatar rootの取扱いは、GUI/runtime側で別途明示する必要がある。
- simulated headのscaleが開始時から相対1e-5を超えて変われば `SPRING_SCALE_CHANGED` を返す。ResetでchainのhitRadiusと履歴を再構成する。非一様scale/shearは既存adapterで拒否する。
- collider/center構成またはStepの失敗では、最後に成功したState・Pose・時計を保持する。設定sessionそのものを変更する場合は別ownerを作る。骨格変更後は古いsource対応でresetせず、取込対応の再検証が必要。

## 検証と範囲

自作VRM1のGLBを実readerで読み、session codec往復からownerを作って再生。原点とbind Headが異なる入力、停止中のcenter移動、再開、別bone上のcollider移動、scale変更拒否とReset、stale骨格によるReset失敗の保護を確認した。Core353件合格。

現段階はUnity非依存のowner。Workbenchのgraphからbase poseを取得して出力表示へ適用する処理、GUI再生/停止/リセット、作品切替やsession変更時の破棄、VRM0 root展開、一般node変換、実アバター受入は未完了。
