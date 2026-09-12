using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;

namespace NyaForge.UnityBridge.Editor
{
    [Serializable] internal sealed class StagingOwnership
    {
        public string path,guid,fingerprint;
        internal static StagingOwnership Capture(string path)
        {
            Validate(path);
            return new StagingOwnership { path=path,guid=AssetDatabase.AssetPathToGUID(path),fingerprint=Fingerprint(path) };
        }
        internal void Delete(string target)
        {
            Validate(path);
            if(path==target) throw new InvalidDataException("Staging cannot be the update target.");
            if(!Directory.Exists(path) && !File.Exists(path+".meta")) return;
            if(!Guid.TryParseExact(guid,"N",out _) || AssetDatabase.AssetPathToGUID(path)!=guid || Fingerprint(path)!=fingerprint)
                throw new IOException("Staging changed after creation; retained for review: "+path);
            if(!AssetDatabase.DeleteAsset(path)) throw new IOException("Could not remove owned staging: "+path);
        }
        static void Validate(string path)
        {
            const string prefix="Assets/NyaForgeImport-";
            if(path==null || !path.StartsWith(prefix,StringComparison.Ordinal) || !Guid.TryParseExact(path.Substring(prefix.Length),"N",out _))
                throw new InvalidDataException("Invalid staging directory.");
        }
        static string Fingerprint(string path)
        {
            using(var hash=SHA256.Create())
            {
                var text=new StringBuilder();
                foreach(string file in Directory.GetFiles(path,"*",SearchOption.AllDirectories).OrderBy(f=>f,StringComparer.Ordinal))
                    text.Append(file.Substring(path.Length+1).Replace('\\','/')).Append(':').Append(Convert.ToBase64String(hash.ComputeHash(File.ReadAllBytes(file)))).Append('\n');
                return Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())));
            }
        }
    }
}
