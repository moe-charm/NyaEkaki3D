using System;
using System.Collections;
using System.IO;
using NyaForge.Authoring.Graph;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator PickSemanticTexture(MaterialTextureSemantic semantic)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var previous = workspace;
            string state = workspace.Document.StateHash;
            semanticPickerOpen = true;
            try
            {
                var field = semantic == MaterialTextureSemantic.Normal ? normalTexturePath : metallicRoughnessTexturePath;
                var picker = Platform.WindowsFilePicker.Open(Platform.WindowsFilePicker.GetActiveWindow(), SemanticTextureDirectory(field.value),
                    "画像 (*.png;*.jpg;*.jpeg)\0*.png;*.jpg;*.jpeg\0\0", "Nya Ekaki 3D — semantic textureを選択", "png");
                while (!picker.IsCompleted) yield return null;
                if (picker.IsFaulted) { SetStatus(picker.Exception.GetBaseException().Message); yield break; }
                if (string.IsNullOrEmpty(picker.Result)) yield break;
                if (!ReferenceEquals(previous, workspace) || state != workspace.Document.StateHash)
                {
                    SetStatus("選択中に作品が変わったため、画像の指定を取り消しました。");
                    yield break;
                }
                field.SetValueWithoutNotify(picker.Result);
            }
            finally { semanticPickerOpen = false; }
#else
            SetStatus("semantic textureのファイル選択はWindows版に対応しています。パスを指定して適用できます。");
            yield break;
#endif
        }

        static string SemanticTextureDirectory(string path)
        {
            try { return Path.GetDirectoryName(path); }
            catch (ArgumentException) { return null; }
        }
    }
}
