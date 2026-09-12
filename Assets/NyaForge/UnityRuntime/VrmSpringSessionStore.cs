using System;
using System.IO;
using NyaForge.Authoring.Import;

namespace NyaForge.UnityRuntime
{
    /// <summary>Owns only the mapped VRM SpringBone settings sidecar; runtime state is not persisted.</summary>
    static class VrmSpringSessionStore
    {
        internal const string FileName = "vrm-spring-session.nyaforge.json";

        internal static void Save(string directory, VrmSpringSession session)
        {
            directory = Path.GetFullPath(directory); Directory.CreateDirectory(directory); string path = Path.Combine(directory, FileName);
            if (session == null) { if (File.Exists(path)) File.Delete(path); return; }
            byte[] bytes = VrmSpringSessionCodec.Write(session); string temporary = Path.Combine(directory, ".nyaf-spring-" + Guid.NewGuid().ToString("N") + ".tmp");
            try { File.WriteAllBytes(temporary, bytes); if (File.Exists(path)) File.Replace(temporary, path, null); else File.Move(temporary, path); }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        internal static VrmSpringSession Load(string directory)
        {
            string path = Path.Combine(Path.GetFullPath(directory), FileName); if (!File.Exists(path)) return null;
            var info = new FileInfo(path); if (info.Length < 1 || info.Length > NyaForge.Authoring.AuthoringLimits.MaxBlobBytes) throw new NyaForge.Authoring.AuthoringException("BUDGET_EXCEEDED", "VRM SpringBone session exceeds capacity.");
            return VrmSpringSessionCodec.Read(File.ReadAllBytes(path));
        }
    }
}
