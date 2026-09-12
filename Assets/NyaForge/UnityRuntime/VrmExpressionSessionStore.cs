using System;
using System.IO;
using NyaForge.Authoring.Import;

namespace NyaForge.UnityRuntime
{
    /// <summary>Owns only the mapped VRM expression sidecar; the original model remains outside the project.</summary>
    static class VrmExpressionSessionStore
    {
        internal const string FileName = "vrm-expression-session.nyaforge.json";

        internal static void Save(string directory, VrmExpressionSession session)
        {
            directory = Path.GetFullPath(directory); Directory.CreateDirectory(directory); string path = Path.Combine(directory, FileName);
            if (session == null) { if (File.Exists(path)) File.Delete(path); return; }
            byte[] bytes = VrmExpressionSessionCodec.Write(session); string temporary = Path.Combine(directory, ".nyaf-vrm-" + Guid.NewGuid().ToString("N") + ".tmp");
            try { File.WriteAllBytes(temporary, bytes); if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path); }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        internal static VrmExpressionSession Load(string directory)
        {
            string path = Path.Combine(Path.GetFullPath(directory), FileName); if (!File.Exists(path)) return null;
            var info = new FileInfo(path); if (info.Length < 1 || info.Length > NyaForge.Authoring.AuthoringLimits.MaxBlobBytes) throw new NyaForge.Authoring.AuthoringException("BUDGET_EXCEEDED", "VRM expression session exceeds capacity.");
            return VrmExpressionSessionCodec.Read(File.ReadAllBytes(path));
        }
    }
}
