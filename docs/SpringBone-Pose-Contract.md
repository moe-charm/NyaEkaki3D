# SpringBone preview: poseと階層の契約

更新: 2026-09-12。R04/R05の修正。Unity非依存のCore契約であり、VRM runtime接続や衝突solver全体の完成を意味しない。

## 入力と出力

`Step`のposeは、そのフレームのabsolute base poseを渡す。前フレームの揺れを含む出力poseを次のbase poseとして累積させない。物理の履歴は別の`SpringBoneState`で引き継ぐ。入力poseと入力stateは変更しない。

各骨のtransformは、restのheadを原点にした骨ローカル座標からavatar座標への写像である。headはtranslation、tailは`TransformPoint(bone.Tail - bone.Head)`で求める。長さ制約は現在の入力を親の変更に追従させた後のhead-tail距離を使い、入力の拡大率を維持する。

出力回転は、現在の入力poseのtail方向から、計算した次tail方向へ向ける回転を入力basisへ掛ける。前stateのtail方向からの差分をbase poseへ掛けると、2step目以降のposeとstateが一致しなくなる。小角度も安定して評価するため、回転角は外積の長さと内積から`atan2`で求める。

## 親子変換

`SpringPoseHierarchy`がstable BoneIdを起点に親から子へ走査する。chainの登録順、skeleton配列順には依存しない。登録していない子孫も同じ走査で更新する。

子の入力transformをC、親の入力をP、評価済みの親をP'とすると、子の評価開始transformは`P' * inverse(P) * C`である。これにより元のhead offset・basisを保持する。子headを親tailへ強制的に移動しない。親が変更されていない場合は元のBonePoseを保持する。

親の評価後に子のtarget tailとheadを求め、子自身の物理を評価する。非simulated子孫は親の変更だけを継承する。返却するposeは全骨を含み、各simulated boneの出力tailとstate tailが一致する。

## 検証と残件

`SpringPoseTests`は前stateを25stepまで引き継ぎ、固定/変化するbase poseでtail一致を検査する。2jointの接続・offsetあり・未登録子孫・逆登録順、3jointの移動/回転/非一様scaleを含む入力を確認する。座標は有限値であることも明示検査する。最初の4ケースは修正前に失敗した。

R06のchain別コライダー分離とnull group診断は下記の入力モジュールで修正済み。R07の衝突と長さの同時制約は [制約solver](SpringBone-Constraints.md) を参照。R08のdt=0と可変時間刻みは未完了事項である。特に姿勢修正を、衝突解決や時間積分の正しさの証拠として扱わない。

## Chain別の衝突参照（R06）

`SpringSimulationInputs`がskeleton/pose/stateの整合性を検査し、jointごとのコライダーリストを解決する。各chainでgroup参照の重複を除き、index昇順に展開する。同じchainのjointは不変リストを共有する。別chainへ参照を伝播しない。共有groupを使う場合は、それぞれのchainが明示的に参照する。

コライダーgroup配列は最大256件で、null要素を参照有無に関係なく拒否する。null配列は参照を持たないchainだけの場合に許可し、範囲外の参照を黙って無視しない。これらは初期state生成とStepの両方で検査する。

独立した2本の骨で、Aの追加・専用コライダー変更が参照なしBへ影響しないことと、明示共有の場合は両者が影響を受けることを数値検証した。階層の親子伝播による子の移動は、この衝突参照の混入とは区別する。
