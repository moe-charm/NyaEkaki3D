# VRM0 source subtree展開

`Vrm0SpringExpansion`は元node階層を展開する純粋なadapterで、skin jointへの対応付けや再生状態を所有しない。公開Resolveはsource hash・graph・skeletonを検査し、rig sessionのHierarchyを使用する。旧sessionの階層不明は再取込の診断とし、投影済みの骨階層から代用しない。

translation-only / unit-scaleの取込profileに対し、root順と元children順で全子孫を深さ優先に展開する。各nodeの先端は最初の子の原点。葉は親→葉の方向へsource空間で0.07m延ばした仮想先端（TailNodeIndex=-1）を持つ。通常nodeも省略しない。rootの設定値はその子孫へ継承する。重力はこの段階ではraw値のまま保持する。

根拠: [UniVRM SpringBoneSystem.SetupRecursive](https://raw.githubusercontent.com/vrm-c/UniVRM/master/Packages/VRM/Runtime/SpringBone/Logic/SpringBoneSystem.cs)（2026-09-12確認）。この参照実装の分岐/末端選択に基づくが、一般scaleや全runtimeの挙動一致を意味しない。

NyaForgeで明示拒否する条件: 重複/重なったroot subtree、親のない葉、親と同位置の葉、最初の子と同位置のhead、範囲外root/center/collider参照、root設定列の不整合。重複rootの多重シミュレーションや長さ0は実行Coreの対応外であり、VRM仕様全般の禁止条件とは区別する。VRM1のcenter祖先制限をVRM0へ流用せず、この段階は参照範囲のみ検査する。

検証は元children順と全分岐、仮想末端の方向/長さ、設定継承、不正入力、合成VRM0のreader→rig codec往復→公開Resolveを対象にする。非joint末端を実際に含み、source不一致も拒否する。

次の実装は通常nodeを含む実行骨格/poseへの対応。現状の実行Coreはskin jointを主体とし、この展開結果をそのまま再生へ渡せない。元source nodeと実行用BoneIdの対応、center/collider追従、skin投影と一時poseの寿命を独立したモジュールで接続する。T02全体・VRM0 GUIの完了にはしない。
