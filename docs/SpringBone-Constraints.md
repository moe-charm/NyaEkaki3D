# SpringBoneの長さ・衝突制約

更新: 2026-09-12。R07のCore修正。対象は各jointのtail球とavatar座標のsphere colliderであり、capsuleや骨全体の連続衝突検出ではない。

## 計算の責務

`SpringConstraintSolver`はhead、候補tail、長さ、fallback方向、hitRadius、解決済みのchain専用colliderリストを受け取る。Unity・VRM・保存・物理履歴へ依存せず、成功時はtail座標だけを返す。

headを中心とする半径Lの球面上へtailを置く。colliderの中心までの距離をd、中心方向をn、collider半径とhitRadiusの合計をRとすると、許されるtail方向uは `dot(u,n) <= (L²+d²-R²)/(2Ld)` を満たす。侵入時はこの境界円へ方向を移すため、押し出し後に長さを戻して再侵入する順序依存を避けられる。

同軸や候補が中心にある場合は、最も平行でない軸から決定的な垂直方向を作る。内部の距離・内積計算はdouble、出力座標は既存Coreのfloatを使う。

## 複数球と計算上限

各passで順に境界へ移し、その後、長さと**すべてのcollider**との距離を再検査する。許容差はavatar座標で`0.00001`。球面の別領域に解があっても単一経路で循環する場合があるため、候補方向に続いて±X/±Y/±Zを試す。最大7方向、それぞれ32passで打ち切る。処理順と開始方向は固定し、同じ入力では同じ結果になる。

これは任意の球集合について解の発見を保証する完全探索ではない。収束した場合だけ返し、未収束を成功扱いしない。球が全候補位置を包む場合は早期に、反復が上限に達した場合は上限診断付きで`SPRING_CONSTRAINT_UNRESOLVED`を返す。診断文は「解なし」と「未収束」を区別する。

`SpringBoneSimulator.Step`は結果をローカルに組み立てるため、途中のjointで失敗しても入力pose/stateを変更せず、新しいstateを公開しない。将来のpreview接続ではこのdomain errorを表示し、最後に成功したフレームの扱いを決める必要がある。

## 検証

- Step経由で、衝突なしなら侵入する同軸fixtureと、group参照を有効にした計算を比較。hitRadius=0/0.1、長さ維持、stateと出力poseのtail一致を検査。
- collider中心と候補の一致、headと候補の一致、複数球の全距離・有限性・決定性を確認。
- head中心の大きな球で解なし診断と入力stateの不変を確認。
- 個別には解があっても全体で矛盾する対向球で、反復上限の診断を確認。
- groupを参照しない旧「sphere collider keeps...」テストは削除し、実際に衝突処理を通る上記テストへ置換。

時間停止・可変dtのR08は [時間契約](SpringBone-Time.md) を参照する。実VRMへの接続、見た目の受入は未完了事項である。
