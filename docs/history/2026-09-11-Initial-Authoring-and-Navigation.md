# NyaForge 初期制作・GUI整理の実装履歴

> 2026-09-11にcurrent_task.mdから退避した当時の記録。以降の製品方針・実装順は [設計v2](../NyaForge-Authoring-Design2.md) と [開発計画](../Development-Plan.md) を参照。「後続の開発」は旧設計当時の予定であり、現在の着手指示ではない。

この文書内のコード・Artifacts・Logs等のパス表記はリポジトリルート基準。

更新: 2026-09-11 — GUI整理・Windowsファイル選択を実装、Player再ビルド済み

## 完了: GUI整理とWindowsファイル選択

- [x] 上部を「パックを開く・最近・確認セット・設定・制作」に整理。視点操作をviewport側、再生詳細を設定内へ移動。
- [x] WindowsのExplorer形式のファイル選択を追加。JSONパック／確認セットを選択。キャンセルで表示・履歴を変更しない。
- [x] GUIから別パックを開く際は、そのパックの初期状態を使用。保存済みセットの読み込みと通常の更新では既存の意味を維持。
- [x] Windows Playerビルド、GUI・キャンセル・パック再オープンの検証。
- [x] 使い方と検証結果の更新。

最新ビルド: `Logs/build-player-20260911-173800-924.log`。`Builds/Windows/NyaForge.exe` を起動し直すと反映される。

ネイティブ選択テスト: `dotnet run --project Tests/WindowsPicker/WindowsPicker.Tests.csproj` は2項目pass。実際のWindows共通ダイアログでキャンセルと、日本語・空白・アポストロフィを含むパスを選択。初期実装のStringBuilderフィールドのマーシャリング不具合を検出し、明示したUTF-16バッファへ修正した。検証は.NET 10から本番ヘルパーを呼ぶ形で、Unity Player内の手動クリック操作は別途受入確認とする。

Player: `Artifacts/Navigation-20260911-173906-72a0e4f597c7424c9871585225a2b36d/report.json` はpass。隔離設定と公開fixtureを使用し、選択後ロード、キャンセル時保持、未保存ガード、無効パス時のセッション・履歴保持、初期表示への復元、保存セットの表示復元を検証。1280×800で上部ボタンの収まりと切替パネルを検証し、`main.png` の描画を開いて確認済み。先行実行で`sets.png`・`settings.png`も目視確認。画像はUI Toolkitの描画先にテスト用camera画像を載せたもので、物理モニターのキャプチャではない。別アバター間の切替や全既存Acceptance suiteの再実行は今回の検証に含めない。

## 今回の作業

独立した `NyaForge` リポジトリで、制作機能の最初の往復を実装する。
設計の正本は [制作アプリ拡張設計](../NyaForge-Authoring-Design.md)。元プロジェクトに保存されていた設計書を、このリポジトリの `docs/` にコピーした。

今回の完了条件は、自作の単純メッシュの頂点をメートル単位で変更し、GUIで確認し、保存・再読込・Undoを行い、version付き出力を別のUnityプロジェクトへ渡せること。設計全体のNF-0とNF-1の最小部分を扱う。任意アバターの編集、MCP、チョーカー生成、skin/morph編集は後続。

## 実装上の決定

- 元アバターや制作素材は取り込まず、自作の公開用fixtureで試す。
- 初期はstatic meshと正の一様scale・平行移動を対象とし、対応外の変換や属性を黙って捨てない。
- 頂点deltaはrest空間のメートル。scale 1と100の両方で1cmの編集を検証する。
- 編集正本はUnityから独立したC#データ。表示用Meshを所有し、元データを直接変更しない。
- Undo/Redoは起動中の履歴。プロジェクトを開き直した後は保存済み編集状態から新しい履歴を始める。
- 操作IDによる再送判定をrevision検証より先に行う。同一IDの別payloadは拒否する。
- native保存はmanifest + 不変blob。書込排他と保存version検証を行う。
- 公開用のコード・設計・自作fixtureのみをこのリポジトリで管理する。生成物・ローカル制作データは追跡しない。

## 進捗

- [x] 独立repoの場所、cleanな開始状態、Unity 6000.4.3f1 / 2022.3.22f1のインストールを確認。
- [x] 設計書を `docs/` へコピーし、今回の実装範囲を確定。
- [x] AuthoringDocument、頂点delta、command、履歴、native保存・出力。
- [x] 公開用fixture、Windows Playerの再現可能なビルド手順。
- [x] 制作GUI、所有Meshへの反映、選択・移動・保存・出力。
- [x] 別Unity向けEditor Bridge、出力の再読込検証。
- [x] 回帰検証、Playerビルド、実描画の確認。
- [x] README・設計の実装状況・この記録を最終結果へ更新。

## 検証結果

Unity 6000.4.3f1で公開用fixtureのAssetBundle生成とWindows Playerビルドが成功。最終Playerビルドは `Logs/build-player-20260911-165109-917.log`。実行ファイルは `Builds/Windows/NyaForge.exe`。Unityロゴは無効。

`dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj`: **30 passed / 0 failed**。scaleと平行移動、元データ保護、Undo/Redo、同一操作の再送、古いrevision、再入、projection失敗のrollback、保存排他・競合、破損blob、schema・属性・容量制限を検証。

`Artifacts/authoring-render/report.json`: Windows Player / RTX 4090で、scale 1 / 100の頂点0をXへ10mm移動、基準mesh保持、Undo/Redo、layer切替、保存・再読込、Bake出力、GUI上の頂点選択、新規保存先、未保存終了ガードを検証済み。

最初のScreenCaptureは透明な画像だったため、画像の存在だけで描画成功とは扱わず修正。制作viewportをRenderTexture表示とし、UI ToolkitのtargetTextureから撮影、画素の多様性を検証した。1600×1000の `Artifacts/authoring-render/authoring.png` と、最終Playerの1280×800の `Artifacts/Authoring-20260911-165238-8d1d6a705e4c4acd916100248e424342/authoring.png` を実際に開いて確認済み。後者の `report.json` もpass。

Unity Bridge: `Artifacts/BridgeReceiver-20260911-164938-243-6181fdaee613453392536af525aaf277/bridge-report.json` は **passed / 6項目**。独立したUnity **2022.3.22f1** プロジェクトへCore/Bridgeをlocal packageとして導入し、通常のMesh/Material/Prefabを生成。scale1/100で同一メートル座標、頂点0だけの+0.01m、UV0/法線/接線/submesh、明示した親への取り付けを、保存後のasset再読込で確認。位置許容誤差は1e-6m。受け取り用プロジェクトは確認のため残している。

既存Viewerの公開用fixture読込: `Artifacts/fixture-load/player.log` の `VIEWER_STARTUP_PROBE_FINISHED RECORDED mode=loaded` を確認。旧アバター用Acceptance suite全体の合格は今回の確認範囲に含めない。

レビューで見つけた再入commandのガード解除、新しい保存先へのversion適用、重複位置の頂点選択、読み込み中の制作遷移を修正。頂点ID指定と終了時の未保存確認も追加。

## すぐ試す

`Builds/Windows/NyaForge.exe` を起動し、**制作へ**。自作プレートの点を選び、X=10mmで移動、Undo、保存、Unity用出力を試せる。制作画面の右側はスクロールできる。

ローカル設定・制作フォルダの初期位置は `%USERPROFILE%/AppData/LocalLow/NyaForge/NyaForge/`。元のClothing Viewerとは別の保存先。詳しい操作・再検証コマンドは [使い方](../Authoring-Quickstart.md) と [Unity Bridge](../../UnityBridge/README.md)。

パック読み込みは上部の **パックを開く…** でWindowsのファイル選択画面を開き、`current.StandaloneWindows64.json` を選ぶ。成功後は **最近** から開き直せる。履歴はNyaForge専用の `recent-packs.json` に最大8件保存し、公開repoへは入らない。FBX・BLENDの直接読込は未実装。

前回の履歴実装では、RadDollV3 private packを`--library`で起動して読み込み成功とmanifestの履歴保存を確認した。この時のStartupProbe画像は透明であり、読み込み確認と表示品質の確認は区別する。今回の描画検証は上記のNavigation検証を参照。

## 実装の境界

現在は1文書・1static mesh・1offset layer。正の一様scaleと平行移動のみ。法線/接線は保持し、再計算やskin/morphは未対応。履歴は最大128件、操作IDは1instanceで最大10,000件、上限到達時は新規操作を拒否する。点表示・マウス選択は先頭256頂点までで、ID指定では全頂点を選択できる。

設計全体の完成ではない。実アバターの編集、source reloadとの連携、MCP、生成器、VRChatでのBuild & Test、マウス操作の人間による最終受け入れは後続。元データ・画像・私有packのコピーやGitHubへのpushは行っていない。

## 次に読むファイル

1. このファイル
2. [制作アプリ拡張設計](../NyaForge-Authoring-Design.md)
3. [README](../../README.md)

## 後続の開発

既存packの編集可能性検査とsource binding、source reload後の制作状態保持、restでの領域選択、MCP/evidence、基本チョーカー、材質・装着を設計順に進める。今回のstatic fixtureの成功を、実アバター・VRChatへの対応完了とは扱わない。
