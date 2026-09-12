# source affine数値基盤（I04-A）

`SourceAffine`はUnityに依存しない不変のcolumn-major 4×4行列。TRSの合成はT×R×S、`parent.Compose(local)`はparent×local。doubleで計算し、公開するVec3/Vec4はfinite float範囲を検査して返す。入力配列はコピーし、公開配列もコピーする。

## 計算契約

- Pointには平行移動を含め、Vectorには含めない。morph POSITION deltaはVectorを使用する。
- Inverseはaffine逆行列。Normalは基底の逆転置を適用して正規化する。法線と頂点を同じ基底で変換しない。
- Tangentはベクトル変換後に変換済みnormalへ直交化・正規化する。鏡映の場合はWのhandednessを反転する。三角形のwinding変更はmesh adapterの責務。
- TRSのquaternionは長さ1との差が1e-5以内のみ受け入れ、その範囲の丸めを正規化する。ゼロquaternionや不正値は拒否する。
- matrixの最終行は厳密に0,0,0,1。特異基底、列長の積で正規化したdeterminantが1e-12以下のほぼ共線な基底、非有限値、float範囲外の成分/出力を `INVALID_AFFINE` で拒否する。絶対determinant閾値ではないため、小さい一様scaleだけを理由に拒否しない。
- ゼロ長normal/tangentや不正handednessは拒否する。shearや非一様scaleの行列計算は可能だが、Springの球/capsuleが同じ変換に対応するという意味ではない。

## 接続・保存の次の変更単位

この段階は数値基盤。GLB取込のtranslation-only制限、RestTransform、PoseTransform、既存native codecは変更していない。

1. node JSONのTRS/matrixのdecodeを別モジュールへ分離し、混在・配列長・型を検査する。source木の親合成は反復処理で行い、local/worldの両方を保持する。
2. source scene候補へ完全なnode transformとskin slotごとのinverse-bindを持たせる。原点だけのImportedSourceHierarchyを、完全な基底を保持した型と呼び替えない。
3. rig payloadの新versionを設計する。既存v1/v2/v3は既存経路で読めるようにし、元の一般transform/IBMがない旧作品へそれらを捏造しない。新情報が必要な操作は再取込を診断する。translation-onlyで保存済みの原点と骨対応は引き続き利用する。
4. mesh/skin/morphの座標変換を新候補へ接続し、rest/複数pose/方向属性・保存/Openを確認してから一般取込の能力表示を有効にする。現在の表示制限を先に外さない。

Core回帰は非一様TRSと親子合成の解析値、joint global×inverse-bind、point/vectorの差、shear/鏡映の逆変換、normal/tangent直交性、入力独立性、不正matrix/quaternion/方向、小さいscaleの往復を含む。実行結果はcurrent_taskへ記録。一般GLBの取込・実素材受入の証拠とは区別する。
