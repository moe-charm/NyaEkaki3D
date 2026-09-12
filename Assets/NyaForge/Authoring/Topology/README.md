# Polygon authoring foundation

## Edge vertex insertion

`PolygonEdgeInsertion` inserts a single shared vertex into a selected boundary/manifold edge and adds a separate corner to every incident face. The fraction is measured from the smaller endpoint ID to the larger. `CornerInterpolation` interpolates UVs per face, preserving UV seams; normals are normalized and tangents orthogonalized against them. Degenerate directions and opposing tangent handedness within an edge are rejected. Existing IDs/attributes are retained and new IDs use allocation watermarks. Rendering validates the collinear inserted corners. This does not weld edges or remesh adjacent faces beyond inserting their edge corner; raster interpolation and triangulation can change.

## Face diagonal split

`PolygonFaceSplit` accepts two nonadjacent vertices belonging to exactly one common planar face. It keeps positions and corner attributes, retains one face ID, and allocates one new face plus two endpoint corner IDs above the stored watermarks. An already-existing diagonal is rejected. Each resulting region is triangulated and checked for positive orientation and conserved projected area to reject exterior concave diagonals. The command uses the existing snapshot/context validation and native storage. This is not an arbitrary knife, new edge-point insertion or nonplanar surface cutting. Retriangulation may change interior attribute interpolation.

## Adjacent face merge

`PolygonFaceMerge` merges exactly two coplanar, consistently oriented faces sharing one manifold edge. Material differences and exact UV/normal/tangent discontinuities at the shared endpoints are rejected. The outer directed boundary becomes one face (maximum 256 corners), retaining the smaller face ID, surviving corner objects and allocation watermarks. Candidate triangulation validates the resulting face before command publication. Positions and boundary attributes remain, but triangulation can change interpolation inside the face; this is not an image-preserving rebake or a polygon-count optimization guarantee. Multiple-face dissolve, nonplanar merging and vertex welding are separate operations.

## Allocated element IDs

`PolygonIdWatermarks` stores the maximum allocated vertex/face/corner IDs in each polygon snapshot's edit lineage. Deletion retains these maxima. Move, UV, material and derived tangent operations preserve them; extrusion, solidify, mirror and cap allocate above them. This prevents deletion followed by recreation from reusing IDs after save/reopen. New source construction without prior metadata starts a new allocation history. Undo restores a prior snapshot (including its history); this is not a process-global allocator across divergent Undo branches. External references must continue to validate graph snapshot/revision and domain.

Polygon binary NYFP readers support v1 and v2. The writer retains exact v1 encoding when live maxima already account for allocation history; otherwise v2 adds three UInt64 maxima after the attribute flags (header 80 bytes instead of 56). v1 derives maxima from live IDs because older deleted IDs are unknowable. v2 maxima below live IDs and truncated headers are rejected. Allocation exhaustion is an error, never wraparound. Project/graph schema numbers are unchanged; old programs cannot open the new v2 polygon blobs.

## Boundary capping

`PolygonBoundaries.Find` derives directed loops opposite to their incident face edges. Branching and nonmanifold boundaries are rejected. `PolygonCap.Fill` checks a complete loop of 3..256 vertices, rejects an existing face with the same vertex set, allocates cap face/corner IDs, inherits the material of the first boundary edge's adjacent face, and projects only the new cap. Surviving vertices/faces/attributes remain unchanged. New UVs may overlap existing islands; this is not atlas packing. Candidate triangulation must succeed before command publication. This does not guarantee global self-intersection freedom, nor implement boundary bridging or free-form face creation.

## Face deletion

`PolygonDeletion.DeleteFaces` removes a distinct nonempty set of existing face IDs and vertices that become unused through that deletion. Surviving faces, corner attributes and IDs are retained; pre-existing loose vertices remain. At least one face is required by the current polygon format. Empty-result/invalid selection failures occur before command publication. `PolygonEditing` uses the same input context validation as other edits; `DeletePolygonFaces` participates in the shared command fingerprint, replay, Undo, native storage and Bake. Paint reprojection and topology-wide ID allocation policy remain separate work.

## UV projection

UV island editing is separated into `UvIslands` (exact shared-edge endpoint UV connectivity), `UvIslandTransform` (translation, rotation and positive uniform scaling around selection UV bounds), and `PolygonTangents` (triangle UV derivative accumulation and orthogonalization against retained normals). UVs outside 0..1 remain valid document data. Transform commands expand seed face IDs to connected islands, preserve positions/IDs, share Undo/native/Bake and fingerprint immutable transform settings. Rotation rebuilds existing tangents instead of rotating a normalized frame, which would be wrong for anisotropic UV mappings. Degenerate UV triangles with no reconstructible tangent are rejected. No approximate seam matching, negative scale, island packing or paint rebaking is implied.

`PolygonUvProjection` projects each face onto its dominant geometric-normal plane and puts that face into a separate square grid cell within UV 0..1. Face aspect ratio is retained, with 5% cell padding on each side. This is independent-face projection, not connected-island unwrap or density-preserving atlas packing. Positions and stable IDs are retained; all corner UVs are replaced and existing tangents are rederived against the retained corner normal (or geometric normal when absent). Missing tangent channels remain absent. Mesh attributes and source immutability are preserved through PolygonEdit and the shared command/persistence/Bake path. Projection runs in mesh-local coordinates and is independent of positive uniform reference scale. Nonplanar faces use their dominant projection, so arbitrary surface distortion is not eliminated.

## Mirror

`PolygonMirror` retains the input and adds a reflected copy along X/Y/Z. Copies receive fresh vertex/face/corner IDs, reversed winding, reflected normals/tangent directions and negated tangent handedness. UV and material slots are copied. The Mirror graph adapter converts the reference-space plane coordinate to mesh-local coordinates and assigns a separate node-owned output domain. Deterministic IDs are stable for unchanged input topology; input topology changes require downstream snapshot validation. Center clipping, welding, duplicate removal and skin/morph transfer are not implemented. Both halves may overlap when source geometry crosses the plane. The node is reevaluated from input and parameters rather than committing a fixed duplicate into the upstream edit payload.

## Thickness

`PolygonSolidify.Apply` builds a closed shell from an oriented surface. It preserves original vertices/faces/corners and adds inward-offset vertices, reversed inner faces and boundary walls, with fresh IDs in the same asset domain. Offsets use the normalized sum of incident geometric face normals. Thickness is vertex displacement length; sharp bends do not have guaranteed uniform perpendicular thickness. Self-intersection repair and collision tests are not included. Every result edge has two incident faces, which is a topology check rather than proof of a physically valid volume.

Adjacent winding, nonmanifold edges and disconnected vertex fans are checked before allocation. Inner UVs retain their original coordinates, normals reverse, and tangent handedness reverses with winding; boundary walls use length-based UVs and geometric frame attributes. Absent attributes stay absent. Deformed inner normals are not generally recalculated, UVs can overlap, and skin/morphs remain unsupported. The common `graph.polygon.solidify` operation converts metres to local thickness using the reference scale, validates before commit and shares Undo/native persistence/Bake with PolygonEdit. The GUI operation targets the entire selected edit stage, independently of face selection. Repeating it adds another shell; it is not an adjustable modifier node yet.

This module owns polygon/corner data independently of Unity render arrays. PolygonSource/PolygonEdit graph payloads, stable-ID vertex movement, face-region extrusion and native project persistence are connected. Windows face picking and extrusion use the common command/Undo path.

`PolygonExtrusion` extrudes a selected region by one local vector. Caps retain face/corner IDs and reference newly allocated vertices. Only region boundary edges receive side walls; original vertices remain in the asset. Newly created walls inherit their boundary face material, use edge length/extrusion length UV coordinates when UVs exist, and geometric normals/tangents when those channels exist. Cap attributes are retained. Missing channels remain absent. This is an open-region extrusion, not automatic solidification: an isolated quad produces five faces and an open bottom. It does not resolve self-intersections, generate a UV atlas, or transfer skin/morph data. Nonmanifold incident edges and inconsistent shared-edge winding are rejected. Element IDs remain in the existing asset domain; the graph snapshot changes with the payload.

- `PolygonMesh`: immutable vertices, faces, corners and derived edge incidence. Nonzero 64-bit IDs are stable within the UUID domain and element kind. Edge identity is the unordered pair of stable endpoint IDs; this profile has no parallel distinct edges between the same endpoints. Incidence may expose nonmanifold edges; it does not claim manifold validation.
- `PolygonTriangulator`: dominant-axis projection with normalized double coordinates, boundary intersection checks and ear clipping. Simple concave faces are supported; ambiguous/degenerate projections are rejected. Nonplanar faces use the selected projection; this is not a surface fitting algorithm. Maximum 256 corners per face bounds the quadratic work.
- `PolygonRenderAdapter`: groups only corners with the same authoring vertex and exact present UV/normal/tangent attributes. RenderVertexMap lists every contributing corner; RenderTriangleMap uses concatenated submesh triangle order. Morphs and weights are not represented yet, so merging cannot be reused for morph-bearing data without extending the key.
- `TriangleMeshAdapter`: preserves one authoring vertex per original render vertex and one face per source triangle. It never infers quads, seams or welded identity from coincident positions. Empty attributes stay absent.

Position edits preserve all IDs and corner attributes. Rendering validates geometric candidates; the future command adapter must render/validate before committing an edit. Polygon construction alone validates structure, not polygon simplicity. Domain evolution policies for topology changes remain pending. Current static APIs remain unchanged.

`PolygonEditing.Translate` builds and renders a candidate before command publication. PolygonEdit stores an immutable edited cage with the expected input snapshot/domain; the source remains unchanged. A changed upstream input retains the payload as unresolved rather than reusing IDs against a different input. The current payload is a full cage snapshot, not a compact delta log; blob limits still apply. The GUI maps selected render vertices to distinct stable IDs before dispatch.

`PolygonBinaryCodec` writes NYFP v1/v2 as described above: exact domain ID, sorted stable vertices/faces, original cyclic corner order, and uniformly present/absent UV0/normal/tangent arrays. Counts and complete lengths are checked before object allocation. `PolygonBlobStore` stores hash-named immutable blobs. `mesh.polygon-source` references this blob through the existing native schema 3 graph asset. Graph evaluation retains both the cage and its render mapping; its snapshot includes the polygon payload identity. Bake emits only the evaluated mesh. Legacy render-index EditMesh offsets cannot modify polygon data; an empty or disabled legacy edit can pass the input through without discarding the cage.

`PolygonBridge.Connect` は等頂点数の2境界を逆向きに対応させたquad stripで接続する。`PolygonNewFace.Project` はcap/bridge共通の新面属性生成で、既存cornerを変更しない。境界検出・ID上限・重複面・辺の入射数・三角分割を検査する。全体の自己交差やUV island packingは対象外。`PolygonBridgeOperation` が共通commandへ接続し、Unity側の選択/表示は `AuthoringWorkbench.Bridge` が担当する。

`PolygonWeld.AtCenter` は選択頂点を平均位置へ統合し、最小vertex IDを保持する。面内の連続する統合cornerは最小corner IDの属性を採用し、面間のseamは保持。潰れた面は除去、全削除は拒否する。再訪/重複面/辺過剰共有/分岐/切れたfan/方向不整合を検査し、三角分割で候補を検証。ID履歴と無関係な未使用頂点を保持する。commandは `PolygonWeldOperation`、GUIは `AuthoringWorkbench.Weld`。成功後は残存IDのrender aliasを選択、拒否時は選択を保持する。一般的な自己交差検出・Paint再投影・距離による自動weldではない。


`PolygonFaceCreation.Create` は既存頂点の順序付きperimeterから面を追加する。指定順を維持し、共有辺の向き/過剰共有、重複面、IDを検査。新面の属性は `PolygonNewFace`、確定は `PolygonCreateFaceOperation` と `PolygonEditing.CreateFace`。凹面/逆向きも扱うが、全体交差や頂点接触だけの非manifoldを網羅検出するものではない。順序入力/1点ずつの末尾登録/反転/輪郭表示は `AuthoringWorkbench.FaceCreation`。GUIで候補を検証し、確定は共通command。


`PolygonVertexCreation.Add` はmesh-local位置に未接続頂点を追加。`PolygonAddVertexOperation`/`PolygonEditing.AddVertex`経由でUndo/nativeを扱い、IDは履歴より上へ割り当てる。未接続頂点は現行 `PolygonRenderAdapter` の三角形表示/Bakeには含まれない。編集ケージでの表示/選択とGUI配置は未接続。既存面があるpolygonが前提。

`PolygonEditPoints` は編集専用の対応表。render vertex prefixを保持し、未接続vertex IDを昇順で追加する。Unityの点表示/選択/移動に使用し、出力meshへ点を混ぜない。`AuthoringWorkbench.VertexCreation` のGUI追加はこの対応表で新頂点を選択する。表示再利用は編集polygon参照も検査する。現行256点marker/クリック上限の撤廃は残件。

点表示はUnity側 `EditPointProjection` が2,048点単位のmeshで集約し、全編集点を対象にする。クリックの256点打切りも撤廃。面モード非表示と所有者による資源破棄に対応。CPUクリック探索は全点線形走査、選択変更は全バッチindices更新なので、巨大meshの性能検証は別途必要。

面なしPolygonMeshは0頂点から保持可能。保存は専用blob v3、最初の面はUV/normal/tangentを生成。render adapterはNO_RENDERABLE_FACESで拒否し、edit pointsはstable ID順の点列を返す。graph/GUIへの面なし状態統合は未完了。詳細は `docs/Empty-Polygon-Integration.md`。

`PolygonFaceDissolve` は同一平面・向き・材質の接続した複数面を1つの外周へまとめる。内部edgeの属性seamとmanifoldを検査し、穴や複数loopは拒否。残存cornerと最小face ID/割当履歴を保持、不要になった内部点を除去する。内部補間は再三角分割で変わる可能性がある。`PolygonDissolveOperation`で共通commandへ接続済み、GUIは未接続。

DissolveのGUIは `AuthoringWorkbench.FaceMerge` の結合ボタンから呼ぶ。2面以上を許可し、残存面のみ選択を維持。4面結合/無効選択保持/保存/Undo/BakeはWindows-C1B-Dissolveで検証済み。旧2面Merge APIは保持する。

`PolygonEdgeCut` は異なる2辺の指定位置（小ID→大IDの0〜1）を結ぶ単一面の切断。0/1は既存端点、内部はEdgeInsertion、その後FaceSplitへ接続。`PolygonEdgeCutOperation`で全体を1commandにまとめるため中間形状は文書へ出ず、Undoも1回。GUIと複数面の連続切断は未接続。

辺上切断のGUIは `AuthoringWorkbench.EdgeCut`。ID/割合指定と候補線表示を提供し、確定時のみ共通commandを呼ぶ。Windows-C1B-EdgeCutで単一面の切断/Undo/native/Bakeを検証済み。連続する複数面の切断は残件。

`PolygonCutPath` / `EdgeCutLocation` は元meshの辺交差地点を順序付きで受け取り、各元faceを1回ずつ切断する。共有辺点を重複作成せず、面ごとのUV seamを保持。辺/面再訪や飛び越しを拒否する。候補のみを返し、command/GUIは未接続。

PolygonCutPath は PolygonCutPathOperation / PolygonEditing.CutPath を通じ共通commandへ接続済み。順序付き不変payload、再送照合、経路全体Undo、途中失敗時の無変更、native/BakeをCoreで検証。GUI接続は次工程。

PolygonEdgeScreenPicker: immutable cameraによる論理辺の画面距離判定。near/far clipと同次wによる割合補正。面遮蔽なし、全辺走査。GUI接続前。
