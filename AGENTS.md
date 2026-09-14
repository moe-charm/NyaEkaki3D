# Nya Ekaki 3D 開発メモ

## Windows Computer Use

NyaForgeはWindowsネイティブアプリなので、実ウィンドウの確認・クリック・ドラッグには`mcp__node_repl__js`からWindows用の`@oai/sky`経路を使う。ブラウザ用の`cua`だけを見て`apps: []`と判断しない。

操作経路を迷ったら、対象がNyaForge・Unity・ファイルダイアログなどのWindowsネイティブ画面なら`@oai/sky`、Webページやブラウザ内タブなら`mcp__cua_repl`を選ぶ。ネイティブ画面を操作できないと報告する前に、`@oai/sky`を初期化して`sky.list_windows()`で対象を再確認する。`apps: []`はその接続で列挙されなかったという結果であり、Windows操作機能が存在しない根拠にはしない。

新しい`node_repl`セッションでは、最初に一度だけ次を実行する。

```js
if (!globalThis.sky) {
  const { sky } = await import("@oai/sky");
  globalThis.sky = sky;
}
```

その後、`sky.list_apps()`または`sky.list_windows()`で返されたアプリ・ウィンドウだけを対象にする。NyaForgeを見つける例は次のとおり。

```js
globalThis.windows = await sky.list_windows();
globalThis.candidates = windows.filter((w) => /NyaForge/i.test(w.app));
```

対象が一つに確定してから`sky.get_window()`と`sky.get_window_state()`を呼ぶ。クリック、入力、ドラッグの後は必ず新しいwindow stateを取得し、古い座標・要素番号・screenshot IDを再利用しない。NyaForgeを起動する場合は、既存のビルドへ明示パスで行う。

```js
await sky.launch_app({
  app: "Z:\\TextureVoice_local\\git\\NyaForge\\Builds\\BoneSubsetV16\\NyaForge.exe",
});
```

Windows用Computer Useでターミナルを操作してはいけない。PowerShell・ビルド・テストは`functions.exec`のターミナルで実行し、Computer UseはNyaForgeの画面操作だけに使う。ファイル選択を含む操作は、対象windowを再取得してから続ける。

## 手動受入の入口

現行のWindows候補は`Builds/Windows/NyaForge.exe`。起動ヘルパーは次のコマンドで実行できる。

```powershell
powershell -ExecutionPolicy Bypass -File "Tools/Start-NyaForgeAuthoring.ps1" -Wait
```

実EditorWindowの確認項目は`docs/Windows-v1-Manual-Acceptance.md`に従う。自動Player、Core、Unity BridgeのPASSを、実マウス・実アバター・VRChat内表示の受入へ読み替えない。

## 公開リポジトリの境界

`private/`、`Builds/`、`Artifacts/`、`GeneratedPacks/`、`Library/`、`UserSettings/`には入力モデル、生成物、ログ、個人データが含まれ得る。公開commitへ追加しない。公開対象の変更前に`git status --short`と`git ls-files private`を確認する。

## 基本検証

Core回帰は次で実行する。

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj --no-restore
```

Player／Bridge／実モデルの結果は`current_task.md`へ対象build、artifact、検証範囲を記録する。証跡の存在だけで見た目や販売品質を合格扱いしない。

## 忘れない開発手順

作業を始める前に、対象リポジトリが`Z:\TextureVoice_local\git\NyaForge`で、branchが`main`、remoteが`https://github.com/moe-charm/NyaEkaki3D.git`であることを確認する。別のcheckoutや`private/`の素材へ変更を広げない。

実装した内容と未完了の受入条件は、同じ作業単位で`current_task.md`へ追記する。Core／Player／Bridgeの自動結果、実マウスでの画面確認、実アバターやVRChatの確認は別の証跡として書き、前者を後者の合格へ読み替えない。

区切りがついたら、まず`git diff --check`、`git status --short`、対象テストを実行する。公開commitの前に`git ls-files private`が空であることと、`Builds/`・`Artifacts/`などの生成物が追跡対象でないことを確認する。commit後は`git rev-parse HEAD`と`git ls-remote origin refs/heads/main`を照合し、push結果を`current_task.md`へ記録する。

ユーザーが実行した操作や目視確認を、こちらの自動検証結果として補わない。未確認の項目は「未受入」と書いたまま、次の具体的な手動手順を残す。
