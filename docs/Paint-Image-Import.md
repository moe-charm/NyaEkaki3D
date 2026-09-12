# Paint画像の取り込み

Windows先行のPNG取り込み。既存レイヤーを保持し、新しい層を1回の共通commandで追加する。

## 画像契約

- PNG、1〜1024pxの各辺、ファイル16 MiB以内。保存正本はRGBA8/sRGB RGB/straight alpha/下から上の行順。
- sRGB宣言付き、またはsRGBとして解釈可能なタグなし/標準gAMA・cHRM画像を扱う。ICC/HDR/アニメーションは明示拒否。JPEGやICC色変換は追加開発。
- native decoderへ渡す前にsignature、chunk長、CRC、header、寸法、終了、critical chunk、profileを検査。pixel decoderには画像構成chunkとsRGBだけを渡し、圧縮テキスト等は渡さない。全PNG仕様の適合性を自前で実装するものではなく、画素の展開と残るPNG整合性検査はUnityに委ねる。
- Unityで展開後に寸法を照合し、GetPixels32でRGBA8へ取り込む。16bit入力も編集正本は8bitになる。ファイルのメタデータや高bit精度を保存する画像アーカイブではない。
- サイズ調整オフではキャンバスと同寸法が必要。オンでは縦横比を保ち、中央配置と透明余白。縮小は面積平均、拡大はbilinear。色を線形化しalphaを掛けた値で補間してからstraight alphaへ戻す。これにより透明画素の色が可視境界へにじまない。
- 同寸法は元のRGBA bytesを保持する。サイズ調整で完全透明になる画素のRGBは0にする。既存層・maskは変更しない。

## 所有と操作

`PaintPngInput` は検査済み入力を所有する。`PaintPngImporter` はファイル読み取りとUnity textureの生成/破棄を担当し、`PaintImageFit` はUnity非依存のサイズ調整を担当する。`AuthoringWorkbench.PaintImport` はGUIと共通Add commandの接続を担当する。

外部PNGへのリンクではなく、取り込んだ画像をnative作品のblobとして保存する。元PNGの変更・削除は作品に影響しない。再取り込みは新しい層の追加であり、自動同期しない。

Windowsファイル選択は `UnityRuntime/Platform/WindowsFilePicker` に共有化した。旧ViewerのJSON選択は既存wrapperから同じ共有実装を呼び、filter/title/拡張子を保持する。画像選択中にworkspace・文書状態・Paint段が変わった場合、追加を取り消す。キャンセルは作品を変更しない。

## 確認範囲

Core: premultiplied線形補間、面積縮小、透明余白、入力byte所有、破損・CRC・巨大寸法・profile・アニメーションの拒否。
Player: RGBA/alpha0のRGB/上下方向の一致、寸法不一致・欠損・破損ファイルの変更なし、GUIからの追加/選択、1revision/Undo/Redo、元PNGを壊した後のnative再読込、合成Surface出力。

Explorerのネイティブダイアログ実操作は手動未確認。Player試験はファイルパス設定後にUI Toolkit pointerイベントで追加ボタンを操作する。ICC変換、1024超の取り込みは残件。最新結果はcurrent_task.md参照。

## PNG形式の代表入力検査（2026-09-12）

`Tools/Generate-PngVariants.py` が独立した数式パターンから30個のPNGと期待RGBAを生成する。`Assets/StreamingAssets/NyaForgeVerification/PngVariants` に保持し、`PngVariantVerification` が実Playerの製品importerを通して全画素を比較する。PNG生成は製品encoderを使用しない。

- gray: 1/2/4/8/16bit、RGB: 8/16bit、palette: 1/2/4/8bit、gray-alpha/RGBA: 8/16bit。
- 各形式に通常scanline（5種類のfilterを使用）とAdam7を用意。9×11で全passと端数のpacked rowを通す。
- paletteの部分tRNSと残りの不透明値、gray/RGBの透明色、独立alpha、透明画素のRGB、上下方向をbyte単位で検査。
- gray/RGB16では8bit化すると同色になるsample 0と1を含め、16bitの透明色一致を量子化前に判定することを確認する。他の16bit sampleは主に8bit値×257であり、任意の16bit値の丸め方式全体を保証するものではない。

`PaintPngInput` はpaletteの必須条件、grayへのpalette禁止、indexed bit depthに対するpalette数、tRNSの順序・重複・長さ・色形式をnative decode前に検査する。圧縮pixel内部の全検証は引き続きUnity側の責務。

Core164件と `Windows-C1C-PngVariantsValidated` のPlayer回帰が合格。`Artifacts/Authoring-20260912-004922-68b5a9bf5f8e418f9734e151302c6207/png-variants.txt` は30形式の全画素一致を記録し、同directoryのreport.jsonは既存GUIの追加/Undo/保存/出力も含む。今回dense paintは再実行していない。

## 参照

[Unity LoadImage](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/ImageConversion.LoadImage.html) は色補正を行わず、入力に対応したsRGB/linear textureを作る必要がある。この実装はsRGB textureを使用する。
[PNG仕様](https://www.w3.org/TR/png-3/) のchunk framing、CRC、profile、unassociated alphaを参照。これは入力対応範囲を限定した実装方針であり、全PNG profileの色変換対応を意味しない。
