# MCP sidecar 初期実装

2026-09-12。設計v2 §8の外部C#プロセス構成。Unity側listenerとcommand/Evidence接続は未実装であり、現時点では実アプリを操作できない。

## 面材質割り当てと複数slot inspection（2026-09-12）

- `mesh.assign-materials` は `parameters.slots` の整数配列を受け、重複・型違いを拒否し、疎なslot番号を保持する。
- `graph_inspect` のnode DTOは `materialSlots` と `assignedMaterials` を返す。各slotの材質値（linear RGBA、metallic、roughness、emission、alpha、hash）を確認でき、画像本体は返さずidentity/hash/寸法だけを返す。
- `polygon.faces.material` は `context`・`elementIds`・`materialSlot`・`materialNodeId` を受け、対象面のslotと材質node接続を一つのUndo単位で更新する。wireの必須値と整数型を厳密に検証する。
- Core 262 passed / 0 failed: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-371bfc07471e411884eb35dc28d0ed9b`。
- Windows-FaceMaterial build/Player suite PASS: `Logs/build-player-20260912-090000-851.log`、`Artifacts/Authoring-20260912-090123-58119a05cc614d779e4274d3e32c066a/report.json`。
- ここでのPlayer suiteは既存のMCP制作一周と通信を検証するもの。面材質をGUIから選択する導線、材質値の編集UI、複数object/rig/weight/morphは次工程。

## 出力予算検証 `forge_validate`（2026-09-12）

- `forge_validate` は `documentId`、`expectedRevision`、`profile`（`pc` または `mobile`）を含む読み取り専用payloadを要求する。古いrevisionや別文書へは検証結果を返さず拒否する。
- `pc` は三角形70,000・材質8・最大テクスチャ2,048px、`mobile` は三角形20,000・材質1・最大テクスチャ1,024pxをNyaForgeの目安として検査する。個別結果は `pass` / `fail` / `unknown`、骨とfitは静的profileではunknown。
- Core 265 passed / 0 failed: `C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-c212e2d98f15447cbb1cabff4275c9a6`。Windows-Validation Player suite PASS: `Artifacts/Authoring-20260912-091026-c10b50cb99a34e7e82dedbf7d8e45a78/report.json`。
- これは出力前の数値スクリーニングであり、受け取り先SDKの性能ランク、姿勢変形、アバター適合、見た目の受入を自動で確定するものではない。

## GUIとの判定ロジック共有（2026-09-12）

- 制作画面の折りたたみ式「出力チェック」も `AuthoringValidationReader` を呼び、MCPと同じrevision固定・profile判定を使う。
- GUIはPC / モバイルを選んで個別checkの状態を表示する。検査は読み取り専用で、`AuthoringWorkspace.IsExecuting` がtrueの間はボタンを無効化する。
- Windows-ValidationUi Player suiteで既存制作一周とMCP `forge_validate` 実通信を継続確認した。GUIの手動操作・表示受入は別ゲートとして残す。

## Rig graph inspection（2026-09-12）

- `rig.skeleton` をtyped graph nodeとしてnative graphへ保存できる。graph schema 3のblobに `NYRS` v1 skeletonを格納し、Coreの保存→再読込でhashとbone階層を照合する。
- `forge_graph_inspect` のnode DTOは、skeleton nodeに `skeletonOutput`（skeleton hash、bone数、bone ID/name/parent/head/tail）、skin bind nodeに `skinBindingOutput`（mesh topology hash、skeleton hash、頂点数、influence集計）を返す。大きなmeshや画像本体は返さない。
- MCPからskeleton／skin bind nodeを新規生成するwireはまだ公開していない。bone/weight編集、pose、exportは後続工程であり、Player suite PASSはこのinspection契約と既存制作一周の確認を示す。

## 依存

- .NET 10（ローカルSDK 10.0.202）。ModelContextProtocol 2.2.0を固定しpackages.lock.jsonを生成。SDK/CoreはUnityへ持ち込まない。
- 取得した公式NuGet nuspecでApache-2.0を確認。repository commit: 6fa3825973949a9c4f0cd8af344e15a8db09dc35。公式Web文書の古いMIT表記と混同しない。配布時は全推移依存のlicense/noticeを同梱確認する。
- 公式資料: https://github.com/modelcontextprotocol/csharp-sdk 、https://api.nuget.org/v3-flatcontainer/modelcontextprotocol/2.2.0/modelcontextprotocol.nuspec

## 構成と契約

Programはstdio MCP hostとDIだけを担当。ForgeToolsは型付きtool入口、InstanceConnectionはローカルnamed pipe通信だけを担当。現在公開するtoolはforge_get_stateのみ（listener未接続）。stdoutはMCP専用、logはstderr。

起動引数は --instance <GUID> 必須。接続先は NyaForge.Authoring.<GUID>。自動探索/別instanceへのfallbackなし。1接続1要求、UTF-8 JSON＋LF、version=1、requestId、expectedInstanceId、methodを送る。応答はversion/requestId/instanceIdを照合し、ok/resultまたはerrorを処理する。応答上限1MiB、深さ32、10秒timeout、CancellationToken対応。再試行なし。

## 検証と次工程

- dotnet build Tools/NyaForge.Mcp/NyaForge.Mcp.csproj: 0 warning / 0 error。
- dotnet run --project Tests/Mcp.Transport/Mcp.Transport.Tests.csproj: PASS。実named pipeで正常応答、別instance拒否、接続不可時の取消を検証。
- MCP clientとのinitialize/tool list/tool call往復は次。Unity側のsame-user pipe listener、bounded main-thread queue、起動/終了寿命、state DTO、instance GUI表示、共通command service接続が必要。現時点の通信試験を実アプリ接続の成功とは扱わない。
## 追加検証 2026-09-12
公式SDK clientによる実stdio起動、tool list、forge_get_state call、IPC拒否のIsError伝播がPASS。テスト用named pipeからrevision=23を返しMCP結果のJSON値を照合した。実Unity listenerはまだ未接続。CoreのAuthoringStateReaderは状態要約の正本として追加し、242件のCore suiteが成功。次はUnity main-thread dispatchからこのreaderを呼ぶ。

## Unity Mono互換性 2026-09-12
初回listenerはbuild成功、実Player通信失敗。NamedPipeServerStreamのCurrentUserOnly等を含む作成/待機経路でNotImplemented。正確なstack採取とWindows native ACL付きadapter検討が次。実アプリ接続は未完成。失敗report: Artifacts/Authoring-20260912-070222-204d7604dce743ae93f6c8145cd38132/report.json。

## Native pipe adapter修正
Unity MonoのWindowsIdentity.Owner未実装をstackで確認。WindowsAuthoringPipeはtoken user限定ACL付きCreateNamedPipeWへ分離し、remote拒否とfirst-instanceを指定。Windows-McpNativePersistentのPlayer suiteが成功し、Player内worker clientからmain-thread state取得を確認。実外部sidecarとの一気通貫は次。仕様根拠: https://learn.microsoft.com/ja-jp/windows/win32/api/namedpipeapi/nf-namedpipeapi-createnamedpipew 。

## 外部clientから実Playerへの確認
Windows-McpResponseLifetimeで外部.NET probe→公式SDK stdio client→sidecar→実Playerの3連続get_stateが成功。文書ID/revision/hashをPlayerの期待値と比較。初回は応答直後切断で失敗し、clientが応答を消費して閉じるまで待つよう修正。McpProbeを明示したPlayer検証で再現できる。test report: Artifacts/Authoring-20260912-071017-c76730a77cc5402cae7f3dd31e8f4759/report.json。通常利用のAI client設定は自動変更していない。

## Capabilities/read service
forge_capabilities追加。アプリregistry由来のnode/portとremoteEditing=falseを返す。AuthoringReadRequestは厳密なfield/type/version/IDとサイズ制限、AuthoringReadServiceがmain-thread上のmethod振分けを所有する。Core243、MCP試験、実Playerのcapabilities＋state往復成功。capabilitiesのparameter schemaとprofile属性の完全掲載は未実装。

## Graph inspection
forge_graph_inspectを追加。Core readerが同一revisionのnode/edge/portと未解決診断、current mesh hash/domainを返す。stale previewは返さない。最大128node/512edgeの単一graph概要で、parameter schema/値やfilter/cursorは残件。Core244と非空fixtureを使う外部MCP→Player往復が成功。

## Command wireの準備
CommandWireReaderが明示envelopeとoperation別shape/typeを既存CommandEnvelope/AuthoringOperationへ変換する。履歴/static頂点移動/graph接続切断から開始。通常編集とのhash一致・同ID再送・revision拒否・UndoをCore246で確認。MCP tool/IPC applyは未接続でremoteEditing=falseを維持。全operation対応やschema公開は今後。

## forge_apply公開
型付きenvelope/operationを64KiB以内でIPCへ渡し、main-threadの既存command serviceへprojection付きで実行。stateのeditSourceHashをexpectedBaselineHashへ指定する。通信okとは別にresult.success/codeを確認。現在5種（static頂点移動、graph接続/切断、Undo/Redo）。外部MCP→実Playerの編集/同ID再送/競合拒否/Undo復元がWindows-McpApplyで成功。現時点のoperation DTOは明示型＋Core別shape検査で、schema自動生成の一元化は残件。

## Graph生成wireの準備
CommandWireGraphReaderが明示graph/node/object IDで生成を解釈。初期node種はplane/edit/output。AddGraphの明示objectId overloadにより同じJSONを再parseしてもcommand再送のfingerprintが変わらない。Core247で空→生成→再送→Undo/Redoを確認。MCP DTO/capabilities/実Player接続は次。

## Graph生成のMCP公開
GraphCommand等のtyped DTOを追加。empty Playerへ外部MCPからgraph生成し、同ID再送/Undo/Redo/plane幅更新を検証。Windows-McpCreateGraphのMcpProbe付きsuite成功。公開node種はplane/edit/output。既存制作物を破棄するproject作成/openは未公開、ユーザーが開いた空文書を利用する。node.updateは既存payloadの置換。

## EditMesh context編集
graph_inspectのeditContext（graphId/nodeId/inputSnapshot/domainId）と入力vertexCountを使い、graph.vertices.translateを送れる。deltaはrest空間、vertexIdsは入力render index。既存Coreのcontext一致検査を使用し、上流変更後の旧contextはEDIT_CONTEXT_STALE。Core248、Windows-McpGraphVerticesの外部MCP生成/編集/旧context拒否が成功。頂点位置取得/専用context tool/selection管理は未実装。

## forge_capture
最終Ready結果を5方向256pxで撮影し、5 PNG image content＋metadata/capture recordをstructured/textで返す。inline配信でディスク保存なし。実MCPでPNG/hash/解像度/snapshot/state一致がWindows-McpCaptureで成功。node対象/camera指定/job/途中取消は未接続。処理はmain-thread同期であり大規模モデルのtimeout対応は残件。

## 保存のCore準備
ProjectSaveServiceはinstance/document/revision一致確認をProjectStore.Saveの前に行う。不変request/resultを使い、既存version/atomic保存を共用。Core249で古い要求の無書込、保存往復、競合時のbytes保持を確認。MCP保存tool/保存先境界/GUI同期は次。再送はsave version競合を返す現仕様で、commandの同ID再送とは異なる。

## forge_save_project公開
get_stateのsaveTargetを用い、GUI選択済みdirectoryへdocument/revision/saveVersionを検査して保存。別directoryはSAVE_TARGET_CHANGED。保存成功後GUIのdirty/pathを同期。static/graphの実MCP保存・再送競合・Player再読込hash一致がWindows-McpSaveで成功。layoutはMCP保存の対象外、保存先をMCPで変更する機能も未公開。

## Export service準備
ProjectExportServiceがinstance/document/revisionを検査し、属性に応じ既存BakeStoreへ振分け。GUI Exportを同serviceへ移行。Core250/Windows-ExportServiceのPlayer suite成功。MCP export要求は次。

## forge_export公開
GUI project配下exports/exportIdへ書き出す。GUID exportIdとdocument/revisionを指定し、既存出力先は拒否。native保存とは独立。実外部MCPでstatic Bake出力・再送拒否・Player読込mesh一致がWindows-McpExportで成功。各材質profileのMCP往復/外部Bridge受入/別process競合等は残件。

## 標準材質対応
material.standardとmesh.assign-materialを公開。linear色/metallic/roughness/emission/alpha/cutoffを明示。graph内材質配列がdepth8を越えたためIPC depth12へ修正、nested envelope回帰追加。Core251/Windows-McpMaterialDepthでMCP生成・画像・Material Bake・native保存が成功し、Bake材質値を再読込確認。texture/Paintと複数材質は未接続。
## MCP頂点inspection接続

- VertexPageRequest（Core wire検査）とVertexPageQuery（sidecar DTO）を分離しforge_vertices_inspectを公開。GUI main threadからAuthoringVertexReaderへ接続。document/revision、node input/output、snapshotHash、offset/countを明示する。
- Windows-McpVertices build成功。実MCPで2ページから4頂点のID/位置/identity/終端を確認し、取得IDで頂点編集→撮影→出力→native保存まで成功。
- Player suite PASS: Artifacts/Authoring-20260912-080338-d923003d07994fa1b3f40cc3422f4643/report.json。build log: Logs/build-player-20260912-080314-800.log。
- Core253 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-04a45b911866462e8cbe609511110862。wireの文字列数値、巨大整数、範囲、port拒否を追加。MCP transport suite成功。
- polygon疎ID/transformの追加検証、face情報/selection、polygon/PaintのMCP編集、schema統合、通信回復・job管理、複数objectとrig等は継続残件。全体完成ではない。

## MCP面inspection公開

- forge_faces_inspect / faces_inspectを公開。既存頂点要求をMeshPageRequest/MeshPageQueryへ改名して共用、Unity meta GUIDを維持。面は64件上限、document/revision/snapshotで取得対象を固定する。
- Core254 passed/0 failed（C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-084747b4431f40e983cb86f03700cd2a）。face envelope解析と65件拒否を追加。MCP transport suiteと9 tool registry成功。
- Windows実MCP Player suite PASS: Artifacts/Authoring-20260912-080853-c8a678be47544305a30b33d498122370/report.json。triangle-onlyへの面要求拒否、その後のstate取得と編集/撮影/出力/保存継続を確認。
- Windows-McpFaces build成功: Logs/build-player-20260912-080823-241.log。
- polygon面の正常読取/疎ID/UVはCore検証。実MCPのpolygon正常読取と面編集は次のpolygon生成wire接続で確認する。全体開発は継続。


## MCPポリゴン生成wire

- CommandWirePolygonReaderを分離追加。mesh.polygon-sourceのdomainId、明示頂点/面/corner ID、座標、materialSlot、scale/translationを解析。mesh.polygon-editの空payload生成も公開。
- IDはゼロなしcanonical decimal string。既存PolygonMeshの参照/重複/予算検査を共用。wireは4096頂点/1024面、実IPCは従来65536bytes上限。現profileはcorner属性なし。UV/normal/tangent付sourceは未公開。
- sidecar PolygonCommand DTOを分離。capabilitiesとforge_apply説明を更新。
- Core255 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-3b591cdbac2b4ec7be7726b2f2f26bc5。完全IPC envelopeから生成/同ID再送/面取得、2^53超ID保持、数値ID/非canonical ID/欠損参照拒否を確認。
- Windows実MCPでpolygon node生成→face取得→node削除が成功。大きなface ID/corner順保持を確認。Player suite PASS: Artifacts/Authoring-20260912-081231-e895d4a043604767b2ec546253d9594e/report.json。
- Windows-McpPolygon build成功: Logs/build-player-20260912-081207-569.log。polygon編集操作wire、context取得、UV/Paintなどは継続残件。


## MCPポリゴン編集

- CommandWirePolygonOperationsを独立追加。polygon.vertices.translate / polygon.faces.extrude / polygon.faces.deleteを既存AuthoringOperationへ接続。contextとcanonical string elementIds、必要時rest-space deltaを指定。重複IDを拒否。
- graph_inspectでpolygon-editの入力contextを公開。face/vertex対象IDはedit nodeの現在outputから取得し、input snapshotのcontextと混同しない。faceless polygonのcontextも取得可能。
- Core256 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-74e2cfde1d1a4aed9ba42f1d5115823d。押出し/移動/削除、再送一回性、Undo/Redo、重複と数値ID拒否を確認。
- 実MCPでpolygon生成→面取得→取得IDで押出し→面数増加確認→Undo/Redo成功。Player suite PASS: Artifacts/Authoring-20260912-081540-80846a1d0dc34223a74e1394d1c0b7c2/report.json。移動/削除の個別実通信はCore試験とは別に残る。
- Windows-McpPolygonEdit build成功: Logs/build-player-20260912-081515-472.log。追加polygon操作、UV/Paint、schema統合、job/通信回復、複数object/rig等の全体残件を継続。


## MCP頂点/面追加・厚み・UV

- polygon.vertices.add（position）、polygon.faces.create（順序付きelementIds/materialSlot）、polygon.solidify（thickness）、polygon.uv.project（既存投影）を公開。すべて明示contextと既存command/Undo実装を共用。生成IDはedit outputのinspectionで取得する。
- Core257 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f8a3ca08f7a945d9be2315654e23fc73。空polygon→3頂点→取得IDで面作成→厚み→全corner UV→native保存/reopen一致を確認。文字列thickness拒否。
- Windows実MCPで押出し後のUV投影→face inspectionの全corner UV読取→Undo/Redoが成功。Player suite PASS: Artifacts/Authoring-20260912-081844-68b03af346284c3594f5240a75ce3d90/report.json。頂点/面追加と厚みの個別実通信はCore確認と区別して残る。
- Windows-McpPolygonUv build成功: Logs/build-player-20260912-081822-267.log。全体目標継続、Paint接続/追加geometry操作/schema等は残件。


## MCPペイント接続

- CommandWirePaintReaderとPaintContextCommandを分離。image.paintノード作成（整数width/height）、paint.stroke（paintContext/UV points/pixel radius/sRGB RGBA bytes）を公開。
- PaintEditContext.FromIdentityは明示image/UV/domain hashを検査。graph_inspectにpaintContextとimageOutput概要を追加し、現在の描画対象を観測可能にした。既存PaintEditingによる古いimage/UV拒否とcommand replayを共用。
- Core258 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-e3e2a58f7bd7418ea0b6c25cf85cd960。描画pixel、二重再送、古いcontext拒否、Undo/Redo、native画像hash保存往復、byte範囲とnodeサイズ検査。
- Windows実MCPでPaint node作成/接続→描画image hash変化→再送一回性→古いcontext拒否→Undo/Redo一致が成功。Player suite PASS: Artifacts/Authoring-20260912-082240-ec626bde941143bf89cbd30fff030c0b/report.json。
- Windows-McpPaint build成功: Logs/build-player-20260912-082215-168.log。単層Paintのみ。Layer/mask/画像importや最終出力への画像接続の実通信一周、schema等は引続き残件。全体開発継続。


## MCP塗装モデルの撮影・出力・保存一周

- PlayerPaintOutputVerificationを分離。実MCPでpolygon edit meshとPaint imageを新Outputに接続→最終出力を変更→5方向撮影→Surface Bake→native保存→元の出力へ復帰を検証。
- Unity側でSurface Bakeを読み戻し64x64、中心pixel赤/背景pixel白を確認。画像付き出力がmesh-onlyに落ちないことを実ファイルで確認。
- Windows-McpPaintOutput build/Player suite PASS: Logs/build-player-20260912-082437-913.log、Artifacts/Authoring-20260912-082459-09687042abb24a7cbd2f5dbf80cec518/report.json。mcp-create.logに塗装モデル一周と既存生成/編集/Undo/保存の成功を記録。
- 今回は既存本体機能の実通信を接続して検証を強化。Core変更なし、直近258passed。native保存要求成功とversionを確認、塗装状態のnativeファイルは後続fixture保存で更新される。塗装Surface Bakeは独立出力として残る。
- Layer/mask/importのMCP対応、撮影の人間目視受入/Unity Bridge実受入、schema/通信回復/複数object/rigなど全体残件を継続。

## MCPレイヤー管理の接続

- AuthoringLayerReaderを分離しgraph_inspectにbottom-to-topのlayerStack（ID/name/opacity/visible/hasMask/サイズ/context）を追加。未解決UVではcontextを返さず既存stack情報を保持。
- CommandWireLayerReaderとLayerContextCommandを分離。layers.migrate/add/appearance/remove/move/renameを公開。空の追加layerは透明、サイズとindexを明示。既存LayerEditingでstack/UV/domain競合を検査。
- Core259 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-53bfe88c204f4de09abc87367d8872c4。移行再送/画素保持、透明追加/古いcontext拒否、可視性/並替/名前/削除/Undo/native往復を確認。sidecar/probe build成功。
- 今回Windows Player再build/実通信は未実施。次はレイヤー管理の実通信とlayer/mask描画接続を検証。最新実通信版はWindows-McpPaintOutput。全体目標継続。

## レイヤー管理のWindows実通信

- PlayerLayerVerificationを独立追加し実MCPで移行/透明layer追加/背景非表示/再表示/名前/順序/削除/Undo/Redoを実行。layerStackのID/名前/順序とimage hashで結果を確認。
- 管理操作後も描画画像hashを保持。layered Paintを最終Outputへ接続して五方向撮影/Surface Bake/native保存が成功。Bakeの中心赤/背景白のpixel readbackも既存fixtureで成功。
- Windows-McpLayers build/Player suite PASS: Logs/build-player-20260912-082938-642.log、Artifacts/Authoring-20260912-083005-1e70c849486b4b768eb9298ee45566c6/report.json。今回はCore変更なし（直近259）。
- layer/maskの描画操作、画像import、schema統合と通信lifecycle、複数object/rig等の全体開発を継続。人間の目視受入とは区別する。

## MCPレイヤー/マスク描画

- CommandWireLayerDrawingを分離。layers.stroke、layers.mask.fill（uniform target byte/明示サイズ）、layers.mask.stroke（target byte/strength）、layers.mask.clearを追加。fillは置換、clearはmask除去。既存PaintLayerChange/LayerEditing/Undoへ接続。
- UV点列とRGBA byte検査をCommandWirePaintReaderの共通helperへ抽出。sidecar target/strengthを追加しcapabilitiesと説明を同期。
- Core259 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-67be22bb59bb45a99055f74b0753a7a7。既存layer wire試験を拡張し緑pixel描画→mask追加/描画で背景白→clearで緑復帰→Undo/Redoを検証。sidecar/probe build成功。
- 今回Windows Player再build/実通信は未実施。次にmask描画実通信、画像import、schema整理/通信lifecycle等を継続。最新実通信版はWindows-McpLayers。全体目標継続。

## レイヤー/マスク描画のWindows実通信

- PlayerLayerVerificationを拡張。overlayへの緑描画→ゼロmaskで元画像へ復帰→mask strokeで一部表示→mask除去で描画画像へ復帰→Undo/Redoを実MCPで検証。
- 合成image hashとhasMaskを照合。後続のレイヤー管理、5方向撮影、Surface Bake/readback、native保存も成功。
- Windows-McpMasks build/Player suite PASS: Logs/build-player-20260912-083344-733.log、Artifacts/Authoring-20260912-083412-88944e4245394448907993fce5cc05ac/report.json。今回は検証拡張でCore変更なし（直近259）。
- 継続残件: 画像import、複数経路stroke/再bind、MCP schema/通信lifecycle、複数object/rigなど。全体目標は継続、目視受入は未完了。

## 画像import共通基盤

- PaintLayerImport.Prepareを分離し、fit/寸法/レイヤー作成をGUIと今後のMCPで共有する構成へ移行。GUI ImportPaintImageは同helperを使用。
- PaintPngImporterをファイル読取と検査済みPaintPngInputのDecodeへ分割。任意のexpectedHashを渡した場合は読んだ元bytesに対しCore RequireSourceHashで検査してからdecodeする。GUI従来呼出はhash指定なし。
- Core260 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-0edf8e03fb25446480784ba08c8a29b6。透明余白/色/寸法不一致/元bytes変更拒否を確認。
- Player suite PASS: Artifacts/Authoring-20260912-083720-3f53786d0cff438d9d7e74035234be52/report.json。共通化後のGUI PNG取込/fit/Undo/保存/Surface出力および既存MCP suite成功。
- Windows-SharedImport build成功: Logs/build-player-20260912-083654-162.log。MCP import endpoint自体は次工程。file identityとcommand再送仕様を接続し、画像import実通信を確認する。全体目標継続。


## MCP画像import endpoint

- forge_import_image / import_imageを追加。1個のlayers.importと明示command envelopeのみ受付。path/sourceHash/fit/サイズ/index/layer ID/contextを指定し、Unity main threadでPaintPngImporterと共有PaintLayerImportを通して既存layer commandへ変換。
- CommandWireImageImportを分離しhost decoderを注入。通常forge_applyではlayers.importは非公開。任意pathは読み取りのみ、入力PNGは16MiB/1024制限とprofile検査。sourceHashは元bytesのSHA256。
- 再送は元fileが存在しhash一致する場合に同じdecoded commandとして再送可能。file消失/変更はdecode前に失敗するため、配送不明時はstate/layersを再確認。元PNGのpath/hashではなくdecoded操作のfingerprintで既存command cacheが比較する点を明示。
- Core260 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-383e0e47df64419a8052ca58985466b5。import envelope/loader注入/fit/追加ID/再送/元bytes変更拒否を確認。MCP transport suite（10 tools）成功。
- Windows-McpImport build成功: Logs/build-player-20260912-084122-167.log。今回importの実PNG実通信は未検証、次に追加する。全体開発継続。

## PNG importのWindows実通信

- PlayerImportVerificationを分離追加。専用8x4青PNGをアプリ側fixtureで生成し、MCPからSHA256/絶対path/fitで64x64 layerへ取り込み。layer ID/合成hash変化、同command再送、元PNG改変拒否、Undo/Redoを確認。
- 改変は専用fixtureのみでfinallyに元bytesへ戻す。import後の試験layerは削除し、後続のlayer/paint/output/save検証も成功。
- Windows-McpImportLive build/Player suite PASS: Logs/build-player-20260912-084305-490.log、Artifacts/Authoring-20260912-084327-30a6d0c530d64406b2adf0a9513d5613/report.json。今回Core変更なし（直近260）。
- imported pixelの個別実通信readbackは未実施（Core/GUI共通pipelineでは検証済み）。schema/通信回復/取り込み再送の元file依存解消、複数object/rig等の全体残件を継続。

## MCP通信回復検証

- AuthoringPipeRecoveryVerificationを分離追加。空frame、UTF-8不正、壊れたJSON、method型不正、改行前切断を送った後にnamed pipe listenerが次接続を受けられることをWindows Playerで確認。
- AuthoringIpcRequestはmethod型をキャスト前に検査し、field集合比較をordinalへ統一。import envelopeのinstance/kind型も明示的に検査する。
- Windows-McpRecovery build成功: Logs/build-player-20260912-084547-299.log。Player suite PASS: Artifacts/Authoring-20260912-084653-e9a13f9cd5d44a8d9e79bc2ce6a5dace/report.json。既存MCP生成/頂点/面/paint/layer/import/capture/export/saveも継続PASS。
- malformed frameは相手へ構造化errorを返さず切断し、次の接続を受ける契約。正常なget_state継続を実通信で確認。送信側のdeadlineと1要求1接続は維持する。ACL readback、複数同時client、バイト単位read性能、job/cancelは残件。

## 材質inspection

- AuthoringMaterialReaderを分離追加。graph_inspectの各nodeにmaterialOutput（linear RGBA、metallic、roughness、emission、alphaMode/cutoff、contentHash、画像identity）とassignedMaterialを返す。画像本体は返さずhash/寸法のみ。
- Windows-MaterialInspect build/Player suite PASS: Logs/build-player-20260912-085038-291.log、Artifacts/Authoring-20260912-085111-d01856be26f947f8a47dbc40c08092c2/report.json。MCP生成時の標準材質値と割当先contentHash一致を実通信で確認。
- Core260 passed/0 failed（直近再確認: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-f14a0b7718334e52afef49de9f70dcc5）。MCP transport build成功。
- 次は材質値更新操作、複数材質slotのinspection/編集、schema registry/通信lifecycle、複数object/rig等を継続。全体目標は未完了。

## 複数材質スロットinspection

- mesh.assign-materialsのwire生成（parameters.slots整数配列）をCommandWireGraphReaderへ追加。重複/型違いは拒否し、GraphNodeの疎なslot番号を維持する。
- graph_inspectにnodeのmaterialSlotsとassignedMaterialsを追加。スロットごとに材質値（linear RGBA/metallic/roughness/emission/alpha/hash）を返す。画像はhash/寸法のみで、面IDやスロット番号の欠落を避ける。
- polygon.faces.materialを既存MaterialFaceEditingへ接続するwireを追加し、観測済みcontext/face IDsとmaterialSlotを明示する。
- Core261 passed/0 failed: C:/Users/tomoaki/AppData/Local/Temp/NyaForge-Core-Tests-5aaf49ad31f04d95a5f5dbe693651c46。疎slot 3/9のinspection、assign-materials node wireの並べ替え/文字列拒否を確認。
- Windows-MultiMaterialInspect build/Player suite PASS: Logs/build-player-20260912-085516-488.log、Artifacts/Authoring-20260912-085555-0a7329c440c94cc3a6ff75ca944383cd/report.json。MCP生成/材質inspection/既存制作一周も成功。全体目標継続。

