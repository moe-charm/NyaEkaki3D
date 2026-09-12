# source affine数値基盤（I04-A）

## source skin候補

`SourceSkin`はsource node transformsとskin index、元のjoint slot順、任意のskeleton root、一般inverse-bind行列を不変保持する。制作骨格の256骨制限とは独立し、source node予算内の257骨以上も表現する。JOINTS属性の値はこのslotからsource nodeへ引く。配列順をソートしたり、node名から対応を推測しない。

入力にinverse-bindがある場合は全joint分以上を要求し、余剰accessor entryも保持する。fresh sourceで省略された場合だけidentityを補い、`HasExplicitInverseBindMatrices`で明示値と区別する。これは[glTF skin schema](https://raw.githubusercontent.com/KhronosGroup/glTF/main/specification/2.0/schema/skin.schema.json)に従う。旧nativeで不明なbindをこの省略扱いへ変換してはならない。skeleton rootを指定する場合は全jointの祖先であることを検査する。

`JointMatrix(slot, jointWorld)`はjointWorld×inverseBindを返す。出力はsource worldへ写す行列で、mesh node変換をさらに掛けない。描画先のlocal空間への変換、weight混合、normal/tangent処理は後段adapterが所有する。world行列は呼出側が同じsource座標系で渡す契約であり、任意pose配列のsource identity検証は今後のscene adapterで行う。

実装範囲は候補型と数値検証まで。GLB accessorのdecode、native codec、一般skinのGUI取込はまだ接続していない。既存translation-only制限を解除する証拠ではない。

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

1. **実装済み**: `GlbNodeTransformReader`がnode JSONのTRS/matrixをdecodeし、混在・配列長・型を検査する。`SourceNodeTransforms`がsource木の親合成を反復処理で行い、local/worldの両方を保持する。詳細は下段。
2. source scene候補へ完全なnode transformとskin slotごとのinverse-bindを持たせる。原点だけのImportedSourceHierarchyを、完全な基底を保持した型と呼び替えない。
3. rig payloadの新versionを設計する。既存v1/v2/v3は既存経路で読めるようにし、元の一般transform/IBMがない旧作品へそれらを捏造しない。新情報が必要な操作は再取込を診断する。translation-onlyで保存済みの原点と骨対応は引き続き利用する。
4. mesh/skin/morphの座標変換を新候補へ接続し、rest/複数pose/方向属性・保存/Openを確認してから一般取込の能力表示を有効にする。現在の表示制限を先に外さない。

Core回帰は非一様TRSと親子合成の解析値、joint global×inverse-bind、point/vectorの差、shear/鏡映の逆変換、normal/tangent直交性、入力独立性、不正matrix/quaternion/方向、小さいscaleの往復を含む。実行結果はcurrent_taskへ記録。一般GLBの取込・実素材受入の証拠とは区別する。

## node読取とworld合成

`GlbNodeTransformReader.Read(bytes)`は既存のbounded GLB container readerを通し、元ファイルのSourceHashを引き継ぐ。nodeにmatrixがある場合はTRSとの混在を拒否。TRS省略はtranslation=0、rotation=identity、scale=1とし、明示nullや不正な型を省略扱いにしない。

node最大4096、childrenの順序保持、範囲/重複/複数親/循環を検査する。source nodeはskin登録の有無に関係なく全件対象。`SourceNodeTransforms`はparent配列順に依存せず反復合成し、全local/world frameと、world原点付きImportedSourceHierarchyを不変保持する。matrixをTRSへ再分解しないためshearを無理に消さない。

GLB geometry/skin importerへの接続は未実施。このreader単体が成功しても、mesh/skin/morph/材質を制作projectへ取り込めるという意味ではない。保存payloadもまだ変更していない。次は一般inverse-bindを含むsource skin候補と、完全な変換情報の保存/移行。

追加Core回帰: 実GLB containerからのmatrix/TRSと配列後方の親、元children順、非jointを含むworld合成、4096段の木、translation-profileとの原点一致、明示null/型/配列長/範囲/重複/循環/特異scaleの拒否。入力JSON変更からの独立性も確認する。
