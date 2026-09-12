# 取込骨対応のnative保存

更新: 2026-09-12。I02。`ImportedRigSession`は元ファイルのsource hash、取込時skeleton hash、対象GraphId/SkeletonNodeId、node→BoneId、humanoid semantic→nodeを保持する。元モデルのbytesは含まない。

## 保存形式と境界

`ImportedRigSessionCodec`のversion 1 JSONは上記identityと対応列を持つ。node列はnode番号、humanoid列はordinal name順で決定的に書く。最大256骨・256semantic、UUID/hash形式、重複node/boneId/semantic、未知field、UTF-8、JSON深さ・末尾データを検査する。

`ProjectAttachments.Rig`（`imported-rig-session.nyaforge.json`）を3つ目の許可されたattachmentとして追加する。schema 4 envelopeは維持し、expression/Spring/rigの最大3件を同じmanifestで一括公開する。writer lock、保存version、失敗時の旧snapshot保持を共用する。旧schemaの外部sidecar探索対象は従来のexpression/Springだけで、rig名の任意ファイルは取り込まない。

このattachmentを持つ作品は、rig名を知らない旧NyaForgeでは開けない。既存作品に対応表がなければ、勝手に骨名から復元しない。必要なら元ファイルから再取込する。

## GUIのライフサイクル

GLB skin取込は、実際に作ったgraph/skeleton nodeのIDでsessionを構築し、command成功後に所有するmetadataへ追加する。VRMがないGLBでもnode対応を保存する。静的mesh取込では古いrig sessionを継承しない。

Openは3種類のpayloadをdecodeし、rigとexpression/Springが同じsource hashを指すか確認してからworkspaceを切り替える。壊れたpayloadやsource不一致では現在の作品を置き換えない。

骨格編集は許可する。`Resolve`は対象graph/node、skeleton hash、全boneIdの対応を検査する。変わった骨格や削除済みnodeへ古いmapを適用しない。取込パネルに更新が必要と表示し、Undoで元の骨格へ戻れば同じmapが再び有効となる。stale状態を保存しても作品自体は開け、map利用時の検査と表示を維持する。

skin外のhumanoid参照も元のnode番号を保存し、黙って削除しない。`ResolveHumanoid`時に未対応nodeとして診断する。source hashは同じ取込への関連づけであり、編集後のモデルを元VRMそのものと保証するものではない。

## 検証と残件

Coreでsessionとnative snapshotの往復、対応骨ID・sourceの保持、別graph・編集後skeletonの拒否、元骨格への復帰、未知field/重複を確認した。rig blobの書込み失敗で旧manifest・dirty/versionを守り、再試行できることも既存保存回帰へ追加した。

Windows検証は同じVRM0/1を取込→Save→空workspace→Openし、復元したmapでhipsを解決する。骨格変更時のstale表示とUndo復帰を確認する。保存失敗試験はexpression/Spring/rigそれぞれを排他ロックし、新規/上書きとGUI/MCP handler再試行を通す。

session自身の編集command/Undo、一般node transform、skin外Spring node、center/collider座標変換、collider座標adapter、再生UIは別段階。capsule衝突Coreは後続実装済み。metadataは従来どおりworkspace単位で保持し、graph Undoによる消去はせず、参照対象がない間はstaleとして扱う。
