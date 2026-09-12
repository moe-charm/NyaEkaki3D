# source affine数値基盤（I04-A）

## GLB weight decode

`GlbSourceSkinImporter`は一つのmeshと指定skinから、全primitiveの`JOINTS_0..n`/`WEIGHTS_0..n`をsource slotのまま読み、`SourceSkinBinding`へ渡す。setは0から連続し、各JOINTS/WEIGHTSが対応すること、POSITION数との一致、skin slot範囲、primitive間のvertex offsetを検査する。static mesh/morphは既存`GlbImporter`へcloneした属性を渡し、geometry decodeを二重実装しない。

現段階のweight decoderはdense VEC4、JOINTSのUNSIGNED_BYTE/UNSIGNED_SHORT、WEIGHTSのFLOATに限定する。sparse、accessor/view拡張、外部buffer、normalized値、非4要素のattributeは未対応として拒否する。strideは4-byte alignedでbuffer宣言範囲内を検査し、JSON巨大整数を先にintへcastしない。全source値をnativeの4影響/256骨へ黙って切り詰めない。

mesh nodeのlocal/world transformはこの候補でgeometryへ追加しない。skin paletteの`jointWorld × inverseBind`は`SourceSkinDeformer`が担当し、skin外のscene transformと出力先座標変換は後段で明示する。複数mesh/instance・異なるskin参照・normalized/sparse weightはI04-B/Cへ残る。

## source skin palette と weight

`SourceSkinBinding`はmesh topology hashとsource hashを持ち、各vertexの元joint slot番号とweightを保持する。制作BoneIdへ先に変換せず、元のslot順を失わない。最大32影響/vertexを受け付ける（実素材で確認した18影響を含む）。Create時に全vertexを要求し、正規化は新しい不変配列で行う。topology/sourceが変わった入力は`SKIN_SOURCE_CHANGED`で拒否する。

`SourceSkinDeformer.Apply`は各slotの`jointWorld × inverseBind`を作り、weightで行列をブレンドする。positionsはpoint、normalsは合成行列の逆転置、tangentsは法線への直交化、UVとtriangle topologyは維持する。restのjointWorldを渡すとinverse-bindが相殺される。制作BoneIdやBoneDefinitionのHead差分を参照しないため、回転・非一様scaleを含むsourceへ接続できる基礎になる。

これはsource mesh一枚の変形候補であり、複数mesh/instance、normal/tangent morphは未完了。`SourceSkinGraphAdapter`とWorkbench表示経路は、現時点では一つのSkinDeformのsource入力を対象に接続済みで、複数deform/共有instance/別skinの一般化は残る。GLBの追加JOINTS_nはdense FLOAT/UBYTE/USHORTの候補読取まで、normalized/sparseは未対応。行列ブレンドが特異になる入力や方向が退化する入力は部分結果を返さず拒否する。skin変形でtriangle windingを変更しないため、鏡映を含むsourceの表示規則は別の出力adapterで確定する。

## mesh座標変換とPOSITION morph

`SourceMeshTransform.Apply(mesh, affine, morphs)`は1つの座標変換をmeshと対応するPOSITION morphへ原子的に適用する独立モジュール。positionsはPoint、normalsは逆転置と正規化、tangentsはベクトル変換と法線への直交化、UVは維持する。負determinantの場合、各submeshのtriangleの第2/第3indexを交換し、tangent Wも反転する。vertex順・submesh順・morph ID/名前は変更しない。

POSITION morph差分にはtranslationを加えない。鏡映でTopologyHashが変わるため、差分を同じvertex indexのまま新しいmeshへ正しくpinし直す。入力morphのtopology不一致は先に拒否する。入力のmesh・morphは不変で、結果に元ContentHashを記録する。

属性なしは空のまま維持する。tangentあり/normalなしは正しい直交化を保証できないため未対応として拒否。平行normal/tangentなど変換後に方向が確定できない場合も拒否し、部分的な結果を返さない。normal/tangent morphは既存MorphSetで表現できず、本処理の対応範囲に含まない。

これは全頂点共通の座標変換であり、jointごとのweight混合ではない。一般skinは別途bind/pose paletteとdeformerを接続する。既存translation-only importerをまだ解除しない。回帰は解析値、鏡映往復、UV/面順/ID保持、morph適用との可換性、stale入力と退化方向の拒否を確認する。

## 完全source skinの保存payload

`SourceSkinCodec`は`NYFS` magicとversion 1を持つbounded binary。sourceHash（ASCII 64byte）、全nodeのparent/local matrix/元children順、skin index、任意skeleton root、joint slot列、明示inverse-bind全entryを保存する。matrixはcolumn-majorの16×IEEE754 double、整数はlittle-endian int32。計算済みworldは重複保存せず、読込時にlocalと親子関係から再合成する。SourceAffineの数値精度を保存時にfloatへ縮小しない。

rootの-1は省略、bind数の-1はfresh sourceでの省略。他の負値やゼロbind数は不正。identity行列を明示した入力と省略入力を区別する。旧rig session v1/v2/v3の情報不足をこの省略に読み替えない。

総payloadは16MiB以下。writeは必要byte数を事前計算し、readは残りbyte数とnode/edge予算を検査してから配列を確保する。trailing/truncated data、不正version、特異matrixを拒否し、復元候補の全検証が成功してから返す。正規payloadはread→writeでbyte同一、local/world値とchildren順が維持されることをCoreで確認する。

rig session v4の明示`sourceSkin` fieldへNYFSをbase64で格納し、元weightまで揃う場合はv5の`sourceSkinPackage`へNYSP（NYFS＋source binding）をbase64で格納する。既存Rig attachmentの型付き拡張であり、任意metadataを追加する入口にはしない。JSON/base64化後の全sessionも16MiB以下を要求するため、NYFS/NYSP単体の上限いっぱいのデータはsessionとして保存できない場合がある。上限超過は拒否し、nativeへの公開前に診断する。

`ImportedRigSession.WithSourceSkin`と`WithSourceSkin(skin,binding)`が不変な新sessionを返す。source hash、joint集合とnode→bone対応、全parent/children順/world原点が既存sessionと一致することを独立validationモジュールで検査する。元のjoint slot順と全基底/bindはNYFS側、mesh topology hashと全元weightはNYSP側に保持する。任意の異素材データの付替えを許可しない。

writerは元weightなしの完全sourceでv4、weightありでv5、なしでv3。readerはv1〜v5に対応。旧版に完全transform/bind/weightを捏造せず、`SourceSkin == null`を維持する。v4はsourceSkin、v5はsourceSkinPackage必須でnull/不正base64/不正NYFS/NYSPを拒否する。既存native projectのRig attachment保存・hash検査を利用し、Save/Open後のNYFS/NYSP byte一致をCoreで確認済み。

GUIのskinned取込は同じbytesからGlbSourceSkinImporterでsourceと全dense weightを読み、WithSourceSkin(skin,binding)で検証してからCommitImportedGraphへ渡す。新規skinned取込はv5を保存する。source decode/対応検査が失敗した場合はgraph公開前に拒否し、完全情報を省いたv3/v4へ黙ってfallbackしない。既存nativeのv1〜v4はそれぞれの情報範囲で利用できる。

Player検証はVRM0/1でsource payloadと元GLBの一致、再生中の保存、原本パスを利用できない状態でのOpen、完全payloadの維持、制作姿勢の復元を対象とする。結果はcurrent_task参照。一般geometry/skin座標対応はまだ未完了で、v5 package保存成功だけで一般mesh/skinを正しく描画できるとは扱わない。

## source skin候補

`SourceSkin`はsource node transformsとskin index、元のjoint slot順、任意のskeleton root、一般inverse-bind行列を不変保持する。制作骨格の256骨制限とは独立し、source node予算内の257骨以上も表現する。JOINTS属性の値はこのslotからsource nodeへ引く。配列順をソートしたり、node名から対応を推測しない。

入力にinverse-bindがある場合は全joint分以上を要求し、余剰accessor entryも保持する。fresh sourceで省略された場合だけidentityを補い、`HasExplicitInverseBindMatrices`で明示値と区別する。これは[glTF skin schema](https://raw.githubusercontent.com/KhronosGroup/glTF/main/specification/2.0/schema/skin.schema.json)に従う。旧nativeで不明なbindをこの省略扱いへ変換してはならない。skeleton rootを指定する場合は全jointの祖先であることを検査する。

`JointMatrix(slot, jointWorld)`はjointWorld×inverseBindを返す。出力はsource worldへ写す行列で、mesh node変換をさらに掛けない。描画先のlocal空間への変換、weight混合、normal/tangent処理は後段adapterが所有する。world行列は呼出側が同じsource座標系で渡す契約であり、任意pose配列のsource identity検証は今後のscene adapterで行う。

`GlbSourceSkinReader.Read(bytes, skinIndex)`で指定skinを読取可能。全source node transformsを読み、source hash一致を検査して候補へ接続する。複数skinを個別に指定できるが、複数meshの制作projectへの取込を意味しない。

`GlbMatrixAccessorReader`はembedded BINのdense FLOAT/MAT4をdecodeする。buffer/view/accessorの宣言範囲をlong演算で検査し、4byte alignmentとBIN padding最大3byteを確認してからlittle-endian floatを読む。joint数以上のaccessor entryをすべて保持。正規化FLOAT、vertex stride/target付きview、不正参照・型・明示nullを拒否する。sparse、外部buffer、対象accessor/view/skinの拡張は現段階では未対応として拒否し、黙って解釈を省かない。JSON数値は範囲確認前にintへcastしない。

実装範囲は候補型・dense accessor decodeと数値検証、NYFS/NYSP native codec、rig session v4/v5、skinned GUIのsource payload接続まで。既存translation-only graph表示の制限を解除する証拠ではない。readerはglTF全体のvalidatorではなく、材質や必須未知拡張等の文書全体の検証はI04-Eに残る。

`SourceAffine`はUnityに依存しない不変のcolumn-major 4×4行列。TRSの合成はT×R×S、`parent.Compose(local)`はparent×local。doubleで計算し、公開するVec3/Vec4はfinite float範囲を検査して返す。入力配列はコピーし、公開配列もコピーする。

## 計算契約

- Pointには平行移動を含め、Vectorには含めない。morph POSITION deltaはVectorを使用する。
- Inverseはaffine逆行列。Normalは基底の逆転置を適用して正規化する。法線と頂点を同じ基底で変換しない。
- Tangentはベクトル変換後に変換済みnormalへ直交化・正規化する。鏡映の場合はWのhandednessを反転する。三角形のwinding変更はmesh adapterの責務。
- TRSのquaternionは長さ1との差が1e-5以内のみ受け入れ、その範囲の丸めを正規化する。ゼロquaternionや不正値は拒否する。
- matrixの最終行は厳密に0,0,0,1。特異基底、列長の積で正規化したdeterminantが1e-12以下のほぼ共線な基底、非有限値、float範囲外の成分/出力を `INVALID_AFFINE` で拒否する。絶対determinant閾値ではないため、小さい一様scaleだけを理由に拒否しない。
- ゼロ長normal/tangentや不正handednessは拒否する。shearや非一様scaleの行列計算は可能だが、Springの球/capsuleが同じ変換に対応するという意味ではない。

## 接続・保存の次の変更単位

この段階はsource payloadを失わずに候補deformerへ渡し、Workbenchの取込直後・揺れ再生中表示へ接続する基盤。RestTransform、PoseTransformは変更していない。native保存はNYFS/NYSPとrig session v4/v5へ拡張済みで、旧v1〜v4の情報境界は維持する。

1. **実装済み**: `GlbNodeTransformReader`がnode JSONのTRS/matrixをdecodeし、混在・配列長・型を検査する。`SourceNodeTransforms`がsource木の親合成を反復処理で行い、local/worldの両方を保持する。詳細は下段。
2. **実装済み**: `GlbSourceSkinImporter`が全dense JOINTS/WEIGHTS setをslot順の`SourceSkinBinding`へ渡し、`SourceSkinDeformer`がbind相殺とpose paletteを計算する。`SourceSkinPackageCodec`とrig session v5が行列・bind・元weightを保存する。
3. **一部接続済み / 次に実装**: `SourceSkinGraphAdapter`が評価済み`GraphMeshValue`のSkinDeform前入力へsource paletteを適用し、`SourceSkinPosePalette`がsessionのauthored poseからsource joint worldを生成する。Workbenchは取込直後とspring playbackでこの表示値を使い、edit stageの値・domain・RestTransform・属性・topologyを保持する。Core/Playerでrest・動的pose・Save/Open・再利用mesh/object・失敗保護を確認済み。次は複数mesh/instance/skin、複数SkinDeform/一般deform graph、normal・tangentと失敗時原子性を検証する。
4. 複数mesh/instance/skin、normalized/sparse weight、未知拡張・材質・animationの保持はI04-B〜Eへ残す。既存translation-only作品を新source情報ありと捏造せず、能力表示は実装済み範囲だけを示す。

Core回帰は非一様TRSと親子合成の解析値、joint global×inverse-bind、point/vectorの差、shear/鏡映の逆変換、normal/tangent直交性、入力独立性、不正matrix/quaternion/方向、小さいscaleの往復を含む。実行結果はcurrent_taskへ記録。一般GLBの取込・実素材受入の証拠とは区別する。

## node読取とworld合成

`GlbNodeTransformReader.Read(bytes)`は既存のbounded GLB container readerを通し、元ファイルのSourceHashを引き継ぐ。nodeにmatrixがある場合はTRSとの混在を拒否。TRS省略はtranslation=0、rotation=identity、scale=1とし、明示nullや不正な型を省略扱いにしない。

node最大4096、childrenの順序保持、範囲/重複/複数親/循環を検査する。source nodeはskin登録の有無に関係なく全件対象。`SourceNodeTransforms`はparent配列順に依存せず反復合成し、全local/world frameと、world原点付きImportedSourceHierarchyを不変保持する。matrixをTRSへ再分解しないためshearを無理に消さない。

GLB geometry/skin importerへの接続は未実施。このreader単体が成功しても、mesh/skin/morph/材質を制作projectへ取り込めるという意味ではない。保存payloadもまだ変更していない。次は一般inverse-bindを含むsource skin候補と、完全な変換情報の保存/移行。

追加Core回帰: 実GLB containerからのmatrix/TRSと配列後方の親、元children順、非jointを含むworld合成、4096段の木、translation-profileとの原点一致、明示null/型/配列長/範囲/重複/循環/特異scaleの拒否。入力JSON変更からの独立性も確認する。
