# 部位別材質の実装契約

2026-09-12。設計v2の材質slot／face group継承に向けた実装。描画submeshと制作slot対応、複数割当graph/Core評価/nativeを実装。表示をMaterialSurfaceSetへ接続。GUIの部位割当・複数材質Bakeは未接続。

## 正本と描画の対応

既存の `CageFace.Material` は制作mesh内のslotキーで、材質そのもののidentityやGUI表示順ではない。材質identityはgraph material nodeのUUIDを使う。将来の割当ノードは「制作slotキー→Material port接続」を所有し、slotキーとノードUUIDの関係を履歴とnative graphへ保存する。材質を並べ替えてもslotキーや接続先UUIDを変更しない。

`PolygonRenderMesh.MaterialSlotMap` は描画submesh番号から制作slotキーへの読取専用対応表。使用中のキーを昇順に圧縮する。例えば制作slot 3/9なら、描画submesh 0/1の対応は `[3,9]`。slot 3を使う面がなくなると描画は `[9]` になるが、残った面の制作キー9は変更しない。全slotが連続する従来データの描画mesh/hashは維持される。

空のsubmeshを作ってMeshDataの検査を緩める方法は取らない。材質なしで描画する旧経路や全slot同一材質のv1は同じ見た目を維持できるが、別々の材質を並べる経路は必ずこの対応表を使う。triangle sourceは元のsubmesh番号を制作slotキーとする。

## 次の実装

1. 明示的な複数割当ノードを追加する。既存 `mesh.assign-material` v1の意味やpayloadを変更しない。slotキー集合をcanonical順で保持し、各slotのMaterial入力を個別のtyped portとして接続する。使用中slotの未割当は未解決とし、白材質で成功したように扱わない。
2. 評価値にimmutableなslot別材質bindingを追加する。各画像のUV/domain照合、snapshotへの材質/画像hash反映、geometry後段の保持・拒否をそろえる。旧単一Material/BaseColorとの競合は明示診断する。
3. native/command/Undo往復を確認し、共有Renderingの材質配列とtexture所有へ接続する。材質値だけの変更でgeometryを再生成しない。準備失敗は旧材質・仮Paintを保持する。
4. GUIで面選択→slot割当とslot→材質選択を追加する。既存のPaint選択は複数の画像接続先を曖昧にせず、対象slotを明示する。
5. 専用Bake profileに制作slotキー、材質identity、描画submesh対応、個別parameter/画像を記録する。旧all-slots profileへ複数材質を押し込まない。Bridgeで保存後のsubmesh/材質/texture対応と描画を検査する。

## 面の継承

押出しcapは元face/slotを保持し、境界壁は境界元faceのslotを継承する。Mirrorの複製面、Solidifyの裏面と境界壁も元faceのslotを継承する既存実装を使う。異なる材質にまたがる領域、新face作成、merge/bridgeなどは操作ごとに規則を決める。未知の新faceを配列先頭の材質へ暗黙に割り当てない。

## 現在の検証

Core175件: sparse 3/9のtriangle→face→slot対応、face/vertex順反転で同じ描画hash、slot 3の面除去後も9を保持、native保存/再読込、slot 7だけの押出しとMirrorの新face継承を確認。Solidifyの材質継承は実装を読んだ段階で、この追加fixtureではまだ検査していない。

これは異なる材質の見た目やGUI操作の完成証拠ではない。最新Player/receiver結果と未実装範囲は [current_task](../current_task.md) に記録する。

## 複数割当Core（2026-09-12）

`mesh.assign-materials` v1は `GraphNode.AssignMaterials(id, slots)` で作る。slotは重複なし・予算内の整数で、canonical昇順のpayload（count + int32列）を持つ。入力はmeshと各slotの `material-<key>`、出力はmesh。Material port接続元のUUIDが材質identityとなる。旧assign-materialのpayloadは変えない。

評価後の `GraphMeshValue.SlotMaterials` はslotキー→MaterialSlotBindingのimmutable辞書。bindingにノードUUID、standard parameters、任意画像とUV/domainを保持する。全使用slotの割当が必要で、使用していないslotの接続も保持する。未接続portはINPUT_MISSING、使用slot不足はMATERIAL_SLOT_UNASSIGNED。単一材質との重複、Output画像による上書き、後段geometryでの材質消失を拒否する。

snapshotはcanonical slot順でノードUUID、parameter hash、画像hash/UV/domainを含む。別材質の値変更はgeometry hashを変えず、snapshotを更新する。native graph schema3/wire1内の新node typeとして保存し、既存node payloadと単一材質snapshotは保持する。

Core177件でsparse 3/9への異なる材質接続、順序変更、parameter編集/Undo/Redo、native往復、不正slot・未接続・不足・二重割当、旧3export profileの拒否を確認。画像別のbinding/失効・未知node payload・動的portのGUI表示は追加確認が必要。

Player projectionの一時拒否を除去し、次の共有surface集合で複数材質を表示する。
## 複数材質の表示と所有

`MaterialSurfaceSet` が表示submesh配列と部位別BaseColorSurfaceを所有する。PolygonはMaterialSlotMap、triangle sourceはsubmesh番号からslotを解決する。画像なしの旧表示、単一画像/材質表示も同じ集合として扱い、既存の所有ロジックを重複させない。

`ColorUpdate` は集合をまとめてprepareし、rendererの材質配列と所有者をcommitで交換する。rollbackでは旧集合を戻し、旧集合の仮textureも保持する。材質のみの更新はmesh/root/markerを再生成しない。未使用slotの材質は描画資源を作らず、Core接続として保存する。

`ShowPaintPreview(image, authoredSlot)` で部位を指定できる。複数材質時の非null画像には部位指定が必須。`ShowPaintPreview(null)` は全仮画像を解除する。現在のGUIはこの複数slot APIへ未接続。1つのPaintが複数slotへ接続された場合の同時仮表示と、画像の個別native往復は次の検査対象。

編集stage後ろの灰色最終形状は従来の形状比較表示を維持し、材質の外観は最終Outputで確認する。複数材質のGUI割当・画像対象選択・専用Bake/Bridgeが完成するまでは製品の一周完成とは扱わない。
## 部位別GUIの入口

`MaterialSlotEditing.ConvertOutput` は最終単一割当を、使用slotすべてが同じ材質UUIDへ接続された複数割当へ変える。割当ノードUUIDと画像ノードを保持する。`MakeIndependent` は選択slotだけ新しい材質UUIDへ接続し、parametersと入力画像接続を引き継ぐ。どちらも新しいgraphを返し、GUIがReplaceGraphの1commandで適用する。

材質欄の「部位別の材質編集を有効にする」→「材質を分ける部位」→「この部位の材質を複製して分ける」で、既存の部位を独立した材質にできる。部位の選択時は接続中の材質を編集対象にする。複製直後もその新材質を編集対象にする。画像ノードは共有するので、材質の色は独立するが、同じ画像へのPaint編集は共有される。

`OutputSurfaceConnections.Resolve(graph, slot)` はその部位のgeometry/材質/画像の経路を返す。複数材質でslot未指定ならnullを返し、別の部位を暗黙に選ばない。現在のPaint GUIはslot経路へ未接続。選択面を新しい部位へ分ける操作、既存材質の再割当、複数材質Bake/Bridgeは次の実装。
## 選択面のslot変更Core

`PolygonMaterialAssignment.Assign` は安定face IDの選択だけCageFace.Materialを変更し、頂点・corner・UV・normal・tangent・domainを保持する。選択は重複/存在/上限を検査する。`PolygonEditing.AssignMaterial` は既存PolygonEdit contextの検証とpayload更新へ接続し、`AuthoringOperation.AssignPolygonMaterial` で共通履歴に参加する。slot値はこの新commandのfingerprintだけに追加し、旧commandのfingerprintを変えない。

Core179件で選択配列の所有、選択面だけ変更、corner/座標/domain保持、sparse slot表示、Undo/Redo/native、不正選択/slotの無変更を確認。GUIからの面割当は未接続。

接続前に扱う必要がある点: 現 `PaintUvBinding` のpaint-uv-v1はface.Materialもhashに含む。UV座標が同一でもslot変更で画像が未解決になる。wire hashを突然変更せず、GUIの部位変更commandで「face/corner/vertex/UVの対応が変わらずslotだけ変わった」ことを検証したうえで、該当Paintのbindingを同一トランザクションで移す経路を実装する。既に未解決の画像や実際にUVが変わった画像は自動rebindしない。使用slotの材質接続も同時に補う。これらが揃うまでGUIの面割当ボタンは追加しない。
## 面変更と画像対応の一括更新

`MaterialFaceEditing.Assign(graph, context, faceIds, slot, materialNodeId)` を追加。変更前のgraphが解決済みか確認し、PolygonEditのslot変更、最終複数割当へのslot追加/材質接続、保存済みPaintのbinding更新を新graphとしてまとめて返す。ReplaceGraphの1commandで適用する。

Paint入力について前後のdomain/transform、頂点IDと座標、face ID、corner順・ID・vertex参照・UV・法線・接線を比較し、slot以外の変化がない場合だけ既存PaintRebindingへ進める。既に未解決のgraph/画像は拒否。途中で失敗しても元graph/画像はimmutableなまま保持する。最終候補も完全評価してから返す。

未使用slotの接続は削除せず保持する。既存slotへの指定は、そのslotを使う全ての面の材質接続を変更するので、GUIでは新部位作成と既存部位への移動を区別する必要がある。下流の編集が未解決になる配置は明示拒否し、自動rebaseしない。

Core180件でPaint画像の参照/画素を保持したslot変更、UV binding hashの更新、材質接続、Undo/Redo/native往復、元々不一致のPaintを拒否することを確認。layer/mask、複数Paint入力、Mirror等の下流配置、GUIクリックは次の検査対象。
## 面割当GUIの接続

`AuthoringWorkbench.MaterialFaces` がPolygonEditの現在選択面とMaterialFaceEditingを接続する。新部位では未使用のslotキーを割り当て、材質を複製してから1つのReplaceGraphで適用する。既存slotへの移動ではそのslotの現在の材質UUIDを使用し、編集中の別材質で上書きしない。画像は共有接続を保持する。

有効条件はPolygonEdit context・面選択・解決済み複数材質出力。最終出力stageでは面割当ボタンを無効にする。既存の面pickと選択集合を使い、独自選択システムは作らない。

Player fixtureは押出し小物の面選択を検証コードで設定し、実ボタンで2部位へ分割・画像hash/材質UUID保持・Undo/Redo・元slotへ移動を確認する構成。新経路での面pickそのものや保存したlayer/mask付き小物のGUI操作は別途確認が必要。
## 共有Paintの仮表示基盤

`OutputSurfaceConnections.ImageSlots(graph, paintNodeId)` は最終出力で実際に使われる部位のうち、そのPaintノードへ接続された部位を返す。画素hashが同じ別Paintを共有扱いしない。未使用slotや未解決graphは対象にしない。

`OwnedMeshProjection.ShowPaintPreviews(image, slots)` / `MaterialSurfaceSet.ShowPreviews` は全対象を検査し、全textureを準備してから表示を切り替える。途中のtexture準備失敗は候補だけを破棄する。既存ColorUpdateのrollbackは複数の仮textureを保った旧surface集合へ戻る。取消は全slotの仮画像を解除する。

Core181件で同一Paintの3/9への接続、同じ画素を持つ別Paintの分離を確認。Player側では複数仮画像、不正slotを含む要求の無変更、commit/rollback/取消を検査する。GUIの3D strokeはまだこの複数slot経路へ接続していない。

次の接続ではstroke開始時に対象slotを固定し、対象Paintの画像/UVから準備する。ray hitのSubmeshIndexをMaterialSlotMapへ戻して、他の材質の面で別画像を塗らない。対象外の手前面を透過して奥へ描かない。既存の準備世代・取消ルールを維持し、毎mousemoveでgraphを再評価しない。
## 共有Paintの3D GUI接続

既存Paint段の選択から、最終出力の複数slotへつながるlayered Paintも「3Dで塗る」を使用可能にした。SurfacePreparationはOutput.BaseColorではなく選択Paintの評価画像/UVを使用する。stroke開始時にImageSlotsと描画submesh対応を固定し、mousemoveでgraphを再評価しない。

raycastは全meshの最前面を調べ、そのhitが対象Paintを使わないslotならnullとして区間を切る。対象外の手前面を飛ばして奥の対象面を探さない。仮画像は全対象slotへ一括表示し、確定は既存layer commandで1回、取消は全仮textureを解除する。対象切替・文書変更・カメラ変更の既存取消処理を維持する。

共有Paintの2部位についてGUIドラッグ中の両texture切替、確定image hash、Undo/Redo/nativeを新fixtureへ追加した。対象外面の遮蔽や別Paintへの独立描画、mask、部位別に新Paintを追加するGUIは継続検証/開発の対象。
## 部位別に独立したPaintを追加

`MaterialSlotPaintEditing.AddBlank` は選択slotの材質を必ず新UUIDへ複製し、その材質だけ新しい白紙Paintへ接続する。旧材質/旧画像ノードは削除しない。共有材質があっても別slotの画像接続を上書きしない。1つのReplaceGraphで適用しUndoできる。

色塗り欄に「部位 N に白紙Paint（旧画像は保持）」を追加。材質欄で部位を選んでから使用する。追加後は新Paintを編集対象にし、レイヤー編集へ移行すれば3Dで塗れる。従来の「最終出力に色塗りを追加」は複数材質時に無効にして、対象を曖昧にしない。

Core182件で指定slotのみの新接続、旧画像ノード保持、他slotの材質維持、Undo/Redo/nativeを確認。PlayerへGUI追加→layer化→対象面へのstroke→旧画像保持→別Paintを選んで対象外面の無変更を検査するfixtureを追加した。