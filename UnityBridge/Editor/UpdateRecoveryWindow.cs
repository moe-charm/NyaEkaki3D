using System;
using UnityEditor;
using UnityEngine;

namespace NyaForge.UnityBridge.Editor
{
    public sealed class UpdateRecoveryWindow : EditorWindow
    {
        string[] pending=Array.Empty<string>();string status="";
        [MenuItem("Tools/NyaForge/Recover Interrupted Updates...")]
        static void Open()=>GetWindow<UpdateRecoveryWindow>("NyaForge Recovery");
        [InitializeOnLoadMethod]
        static void NotifyPending()
        {
            EditorApplication.delayCall+=()=>
            {
                try { if(UpdateJournal.Pending().Length>0) Debug.LogWarning("NyaForge: 未完了の更新記録があります。Tools > NyaForge > Recover Interrupted Updates で確認できます。"); }
                catch(Exception error) { Debug.LogWarning("NyaForge recovery journal inspection failed: "+error.Message); }
            };
        }
        void OnEnable()=>Refresh();
        void Refresh() { try { pending=UpdateJournal.Pending(); } catch(Exception error) { status=error.Message; } }
        void OnGUI()
        {
            EditorGUILayout.HelpBox("未完了の更新を、更新開始前の保存内容へ戻します。対象資源にその後保存した変更も元に戻るため、対象を確認してください。バックアップ記録は復元後も保持します。",MessageType.Info);
            if(GUILayout.Button("再読み込み")) Refresh();
            foreach(string path in pending)
            {
                EditorGUILayout.LabelField(UpdateJournal.TargetFolder(path),EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(path,GUILayout.Height(36));
                if(GUILayout.Button("この更新を開始前へ戻す"))
                {
                    try { UpdateJournal.Recover(path);status="復元しました: "+path; }
                    catch(Exception error) { status="復元できませんでした。記録は保持されています: "+error.Message; }
                    Refresh();break;
                }
            }
            if(pending.Length==0) EditorGUILayout.LabelField("未完了の更新はありません。");
            if(!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status,MessageType.Info);
        }
    }
}
