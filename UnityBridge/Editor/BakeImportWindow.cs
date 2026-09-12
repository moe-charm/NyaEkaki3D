using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace NyaForge.UnityBridge.Editor
{
    public sealed class BakeImportWindow : EditorWindow
    {
        string manifestPath = "";
        DefaultAsset outputParent;
        DefaultAsset existingImport;
        string reviewedManifestHash,reviewedImportHash,reviewedFolder;
        MaterialUpdatePlan reviewedPlan;
        Vector2 planScroll;
        bool placeInScene;
        enum BakeProfile { Mesh, Surface, Material, Materials }
        BakeProfile profile;
        Transform sceneParent;
        string status = "";
        MessageType statusType = MessageType.Info;

        [MenuItem("Tools/NyaForge/Import Static Bake...")]
        static void Open() { GetWindow<BakeImportWindow>("NyaForge Import").minSize = new Vector2(480, 360); }

        void OnEnable() { if (outputParent == null) outputParent = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets"); }

        void OnGUI()
        {
            EditorGUILayout.LabelField("NyaForge Static Bake", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("静的メッシュを新しい専用フォルダーへ読み込みます。位置・法線・接線・UV・サブメッシュを保持します。", MessageType.Info);
            EditorGUILayout.BeginHorizontal();
            manifestPath = EditorGUILayout.TextField("Bake manifest", manifestPath);
            if (GUILayout.Button("選択", GUILayout.Width(56)))
            {
                string chosen = EditorUtility.OpenFilePanel("NyaForge bake manifest", "", "json");
                if (!string.IsNullOrEmpty(chosen))
                {
                    manifestPath = chosen;
                    profile = Path.GetFileName(chosen)=="materials.nyaforge-bake.json" ? BakeProfile.Materials : Path.GetFileName(chosen)=="material.nyaforge-bake.json" ? BakeProfile.Material :
                        Path.GetFileName(chosen)=="surface.nyaforge-bake.json" ? BakeProfile.Surface : BakeProfile.Mesh;
                }
            }
            EditorGUILayout.EndHorizontal();
            profile = (BakeProfile)EditorGUILayout.Popup("出力形式",(int)profile,new[]{"メッシュのみ","画像付きSurface","標準PBR材質付き","部位別PBR材質付き"});
            if (profile==BakeProfile.Surface) EditorGUILayout.HelpBox("PNGのbase colorを全材質スロットへ割り当てます。不透明表示です。",MessageType.Info);
            if (profile==BakeProfile.Material || profile==BakeProfile.Materials) EditorGUILayout.HelpBox("色・金属度・粗さ・発光・透明方式と任意の画像を保持します。Built-In / Linearのプロジェクトが必要です。",MessageType.Info);
            outputParent = (DefaultAsset)EditorGUILayout.ObjectField("保存先の親フォルダー", outputParent, typeof(DefaultAsset), false);
            placeInScene = EditorGUILayout.Toggle("シーンにも配置する", placeInScene);
            using (new EditorGUI.DisabledScope(!placeInScene))
                sceneParent = (Transform)EditorGUILayout.ObjectField("親（明示した場合のみ）", sceneParent, typeof(Transform), true);
            if (placeInScene)
                EditorGUILayout.HelpBox("親を指定しても現在のワールド位置を保ちます。骨名の自動検索や首への自動フィットは行いません。配置後に位置を調整できます。", MessageType.Info);
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!File.Exists(manifestPath)))
            {
                if (GUILayout.Button("新規フォルダーへインポート", GUILayout.Height(32)))
                {
                    try
                    {
                        string path = outputParent == null ? "Assets" : AssetDatabase.GetAssetPath(outputParent);
                        var result = profile==BakeProfile.Materials ? BakeImporter.ImportMaterials(manifestPath,path,placeInScene ? sceneParent : null,placeInScene) : profile==BakeProfile.Material ? BakeImporter.ImportMaterial(manifestPath,path,placeInScene ? sceneParent : null,placeInScene) :
                            profile==BakeProfile.Surface ? BakeImporter.ImportSurface(manifestPath,path,placeInScene ? sceneParent : null,placeInScene) :
                            BakeImporter.Import(manifestPath, path, placeInScene ? sceneParent : null, placeInScene);
                        Selection.activeObject = result.SceneInstance != null
                            ? (UnityEngine.Object)result.SceneInstance : AssetDatabase.LoadAssetAtPath<GameObject>(result.PrefabPath);
                        EditorGUIUtility.PingObject(Selection.activeObject);
                        status = "インポート完了: " + result.AssetDirectory;
                        statusType = MessageType.Info;
                    }
                    catch (Exception error)
                    {
                        status = "インポートできませんでした: " + error.Message;
                        statusType = MessageType.Error;
                        Debug.LogException(error);
                    }
                }
            }
            EditorGUILayout.Space();
            existingImport=(DefaultAsset)EditorGUILayout.ObjectField("既存インポートの確認",existingImport,typeof(DefaultAsset),false);
            using(new EditorGUI.DisabledScope(existingImport==null))
            {
                if(GUILayout.Button("生成後の変更を調べる"))
                {
                    try
                    {
                        var inspection=ImportOwnership.Inspect(AssetDatabase.GetAssetPath(existingImport));
                        status=inspection.IsUnchanged ? "管理対象への変更は見つかりませんでした。Bakeを選んで更新先を照合できます。" : string.Join("\n",inspection.Conflicts);
                        statusType=inspection.IsUnchanged ? MessageType.Info : MessageType.Warning;
                    }
                    catch(Exception error) { status="確認できませんでした: "+error.Message;statusType=MessageType.Error; }
                }
            }
            using(new EditorGUI.DisabledScope(existingImport==null || profile!=BakeProfile.Materials || !File.Exists(manifestPath)))
            {
                if(GUILayout.Button("選択Bakeと更新先を照合"))
                {
                    try
                    {
                        var preview=BakeUpdatePreview.Materials(manifestPath,AssetDatabase.GetAssetPath(existingImport));
                        reviewedManifestHash=preview.Conflicts.Count==0 ? preview.Source.ManifestHash : null;
                        reviewedImportHash=preview.Existing.ManifestHash;reviewedFolder=AssetDatabase.GetAssetPath(existingImport);
                        reviewedPlan=preview.Plan;
                        status=preview.Conflicts.Count>0 ? string.Join("\n",preview.Conflicts) : preview.SameContent ? "同じ出力内容です。" : "出力先のIDは一致しています。既存のMesh・材質・画像・Prefabを更新します。不要になった資源は保持します。";
                        statusType=preview.Conflicts.Count>0 ? MessageType.Warning : MessageType.Info;
                    }
                    catch(Exception error) { status="照合できませんでした: "+error.Message;statusType=MessageType.Error; }
                }
            }
            using(new EditorGUI.DisabledScope(existingImport==null || string.IsNullOrEmpty(reviewedManifestHash) || profile!=BakeProfile.Materials))
            {
                if(GUILayout.Button("照合した出力を反映"))
                {
                    try
                    {
                        string folder=AssetDatabase.GetAssetPath(existingImport);var preview=BakeUpdatePreview.Materials(manifestPath,folder);
                        if(folder!=reviewedFolder || preview.Conflicts.Count!=0 || preview.Source.ManifestHash!=reviewedManifestHash || preview.Existing.ManifestHash!=reviewedImportHash || reviewedPlan==null || preview.Plan.ComparisonKey!=reviewedPlan.ComparisonKey)
                            throw new InvalidOperationException("照合後に入力または更新先が変わりました。もう一度照合してください。");
                        var result=BakeImporter.UpdateMaterials(manifestPath,folder);
                        status="更新しました: "+result.PrefabPath;statusType=MessageType.Info;
                    }
                    catch(Exception error) { status="更新できませんでした: "+error.Message;statusType=MessageType.Error; }
                    finally { reviewedManifestHash=null; }
                }
            }
            if(reviewedPlan!=null)
            {
                EditorGUILayout.LabelField("照合した更新内容（未使用の旧資源は保持）",EditorStyles.boldLabel);
                planScroll=EditorGUILayout.BeginScrollView(planScroll,GUILayout.Height(140));
                foreach(var entry in reviewedPlan.Entries) EditorGUILayout.LabelField(entry.Action+"  "+Path.GetFileName(entry.Path));
                EditorGUILayout.EndScrollView();
            }
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, statusType);
        }
    }
}
