using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NyaForge.UnityBridge.Editor
{
    /// <summary>Only the root mesh/material bindings belong to the static Bridge updater.</summary>
    internal static class PrefabManagedBindings
    {
        internal static string Fingerprint(string path)
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(root==null) throw new InvalidDataException("Managed prefab is unavailable.");
            var filters=root.GetComponents<MeshFilter>();var renderers=root.GetComponents<MeshRenderer>();
            if(filters.Length!=1 || renderers.Length!=1) throw new InvalidDataException("Managed prefab root components changed.");
            var text=new StringBuilder("static-root-bindings-v1\n");
            Append(text,root);Append(text,filters[0]);Append(text,renderers[0]);Append(text,filters[0].sharedMesh);
            text.Append(renderers[0].sharedMaterials.Length).Append('\n');
            foreach(var material in renderers[0].sharedMaterials) Append(text,material);
            using(var hash=SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString()))).Replace("-","").ToLowerInvariant();
        }
        static void Append(StringBuilder text,UnityEngine.Object value)
        {
            if(value==null) { text.Append("null\n");return; }
            if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value,out string guid,out long id)) throw new InvalidDataException("Managed reference has no persistent identity.");
            text.Append(guid).Append(':').Append(id.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
        }
        internal static bool IsDirty(string path)
        {
            var stage=PrefabStageUtility.GetCurrentPrefabStage();
            if(stage!=null && stage.assetPath==path && stage.scene.isDirty) return true;
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            return root!=null && root.GetComponentsInChildren<Transform>(true).Any(t=>EditorUtility.IsDirty(t.gameObject) ||
                t.GetComponents<Component>().Any(c=>c!=null && EditorUtility.IsDirty(c)));
        }
    }
}
