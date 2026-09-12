# Surface Bake v1

更新: 2026-09-11。初期の画像付きstatic出力。全機能のshader変換やアバター出力ではない。

## 形式と責務

`surface.nyaforge-bake.json`はschemaVersion 1、profile `static-surface-basecolor-v1`。従来の `mesh.nyaforge-bake.json` schema1とは別形式。従来readerは新形式を拒否する。

- `mesh`: 既存BakeManifestを内包。文書ID/revision・object ID・rest transform・mesh hash・normal方針などを保持。共通の検査処理を使用する。
- `material`: `opaque-basecolor-srgb-straight-all-slots-v1`。全submeshへ同じbase colorを割当。白tint、非金属、smoothness 0、不透明。画像のalpha画素自体は保持する。
- `imageHash`: 確定画像のNYFI blob hash。`blobs/<hash>.bin`。
- `pngHash`: Core PNG encoderの出力hash。`<hash>.png`。
- `uvHash`／`meshDomain`: graph評価時の対応情報。domainはUUIDそのものではなくgraph側のhash。
- `width`／`height`: 1〜1024。現在のPNGはstored DEFLATEで圧縮率が低い。

NYFIは正確な画素照合用、PNGはUnityや外部ツールへの受け渡し用。read時には両方を読み、PNGを現在のv1 encoderで生成したbyte hashと比較する。一般PNG画像のimport APIではなく、この出力profile専用reader。

## 保存と読込

`SurfaceBakeStore`はworkspaceの確定文書をlock中に評価する。旧previewや未確定strokeを使用しない。graphが未解決、画像なし、未対応の出力経路ならフォルダ作成前に拒否する。mesh/画像blob→PNG→manifestの順で保存し、manifestを最後に原子的に更新する。失敗後の未参照blobは自動削除しない。

readerはschema/profile、hash、寸法、UV0有無、メッシュ属性・座標・予算を検査し、全ファイルが一致してから結果を返す。未知field、pathの埋込、改変PNGを受け入れない。BaseColorはimmutable、PNG取得はcopy。

`BakeSource`は共通の出力元解決を担う。従来Bakeは画像を拒否し、新Surface profileだけ画像を保持する。native projectのschemaは変更しない。

## Unity Bridge

`BakeImporter.ImportSurface`は検査後に新しい所有フォルダを作り、Mesh.asset、BaseColor.png、Material、Prefabを生成する。textureはsRGB、無圧縮、mipmapなし、NPOT拡縮なし、clamp/bilinear。StandardまたはURP/Litのbase colorへ割当し、全slotへ保存する。

初期importは毎回新規フォルダ。安定した出力identityによる再import更新、利用者componentとの競合解決、透明/cutout/outline、複数の独立材質、skin/morph・rigは今後。受け取り側のshaderの照明結果はNyaForgeの確認用shaderと完全一致する保証を置かない。

## 検証

Coreはscale1/100のmesh/画像/hash、PNG改変、未解決拒否、旧reader拒否を検証。Playerはnative再読込済み作品から出力する。受け取り先検証は次の追加optionで実行する。

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Test-NyaForgeUnityBridge.ps1 -PlayerCheckDirectory <Player検証フォルダ> -SurfaceFixture
```

既存scale1/100のメッシュ試験を維持し、surfaceのPNG byte・import設定・寸法・材質とPrefabのtexture参照を追加検査する。最新の合否と証拠はroot current_task.md。自動試験は手動の見た目・操作受入やVRChat受入と区別する。
