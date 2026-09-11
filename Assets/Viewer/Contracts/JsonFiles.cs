using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Viewer.Contracts
{
    public static class JsonFiles
    {
        public const int MaxBytes = 8 * 1024 * 1024;
        static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Error,
            DateParseHandling = DateParseHandling.None,
            TypeNameHandling = TypeNameHandling.None,
            Culture = System.Globalization.CultureInfo.InvariantCulture,
            Formatting = Formatting.Indented
        };
        public static string Encode<T>(T value) => JsonConvert.SerializeObject(value, Settings);
        public static T Clone<T>(T value) => Decode<T>(Encode(value));
        public static T Decode<T>(string text)
        {
            if (Encoding.UTF8.GetByteCount(text) > MaxBytes) throw new ContractException("JSON_TOO_LARGE", "JSONが大きすぎます。");
            try
            {
                using var sr = new StringReader(text);
                using var reader = new JsonTextReader(sr) { DateParseHandling = DateParseHandling.None, MaxDepth = 40 };
                var token = JToken.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                if (reader.Read()) throw new JsonException("Trailing JSON content");
                CheckShape(typeof(T), token, "$", 0);
                return token.ToObject<T>(JsonSerializer.Create(Settings));
            }
            catch (ContractException) { throw; }
            catch (Exception e) { throw new ContractException("JSON_INVALID", "JSON形式が不正です: " + e.Message); }
        }
        static void CheckShape(Type type, JToken token, string path, int depth)
        {
            if (depth > 30 || token == null || token.Type == JTokenType.Null) throw new JsonException(path + ": null/depth");
            if (type.IsArray)
            {
                if (!(token is JArray array) || array.Count > 20000) throw new JsonException(path + ": array limit/type");
                foreach (var item in array) CheckShape(type.GetElementType(), item, path + "[]", depth + 1);
                return;
            }
            if (type == typeof(string))
            {
                if (token.Type != JTokenType.String) throw new JsonException(path + ": string expected");
                return;
            }
            if (type == typeof(bool))
            {
                if (token.Type != JTokenType.Boolean) throw new JsonException(path + ": bool expected");
                return;
            }
            if (type.IsPrimitive)
            {
                if (token.Type != JTokenType.Integer && token.Type != JTokenType.Float) throw new JsonException(path + ": number expected");
                double n = token.Value<double>();
                if (double.IsNaN(n) || double.IsInfinity(n)) throw new JsonException(path + ": finite number expected");
                if ((type == typeof(int) || type == typeof(long)) && token.Type != JTokenType.Integer) throw new JsonException(path + ": integer expected");
                return;
            }
            if (!(token is JObject obj)) throw new JsonException(path + ": object expected");
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var property in obj.Properties())
                if (!fields.Any(f => f.Name == property.Name)) throw new JsonException(path + ": unknown field " + property.Name);
            foreach (var field in fields)
            {
                if (!obj.TryGetValue(field.Name, StringComparison.Ordinal, out var child)) throw new JsonException(path + ": missing " + field.Name);
                CheckShape(field.FieldType, child, path + "." + field.Name, depth + 1);
            }
        }
        public static T Read<T>(string path) => Read<T>(path, out _);
        public static T Read<T>(string path, out string sha256)
        {
            // Decode and fingerprint one bounded file snapshot. Reading the
            // path again for its hash could bless different bytes after an
            // external save, or validate a different manifest than we load.
            byte[] bytes;
            using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (input.Length > MaxBytes) throw new ContractException("JSON_TOO_LARGE", "JSONが大きすぎます。");
                using var output = new MemoryStream((int)input.Length);
                var buffer = new byte[16384];
                int count;
                while ((count = input.Read(buffer, 0, buffer.Length)) != 0)
                {
                    if (output.Length + count > MaxBytes) throw new ContractException("JSON_TOO_LARGE", "JSONが大きすぎます。");
                    output.Write(buffer, 0, count);
                }
                bytes = output.ToArray();
            }
            using (var hash = SHA256.Create()) sha256 = Hex(hash.ComputeHash(bytes));
            // A UTF-8 BOM participates in the file hash, but is not JSON text.
            int offset = bytes.Length >= 3 && bytes[0] == 0xef && bytes[1] == 0xbb && bytes[2] == 0xbf ? 3 : 0;
            try { return Decode<T>(new UTF8Encoding(false, true).GetString(bytes, offset, bytes.Length - offset)); }
            catch (DecoderFallbackException) { throw new ContractException("JSON_INVALID", "JSONは有効なUTF-8で保存してください。"); }
        }
        public static string Sha256(string path)
        {
            using var input = File.OpenRead(path);
            using var hash = SHA256.Create();
            return Hex(hash.ComputeHash(input));
        }
        public static string TextHash(string text)
        {
            using var hash = SHA256.Create();
            return Hex(hash.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }
        static string Hex(byte[] bytes) => BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        public static string AtomicWrite<T>(string path, T value, string expectedHash = null, bool allowReplace = false)
        {
            path = Path.GetFullPath(path);
            var parent = Path.GetDirectoryName(path);
            Directory.CreateDirectory(parent);
            if (File.Exists(path) && !allowReplace && (expectedHash == null || Sha256(path) != expectedHash))
                throw new ContractException("SESSION_SAVE_CONFLICT", "保存先が変更されています。別名で保存してください。");
            var tmp = Path.Combine(parent, "." + Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                var bytes = Encoding.UTF8.GetBytes(Encode(value));
                using (var output = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { output.Write(bytes, 0, bytes.Length); output.Flush(true); }
                Read<T>(tmp);
                if (File.Exists(path))
                {
                    if (!allowReplace && Sha256(path) != expectedHash) throw new ContractException("SESSION_SAVE_CONFLICT", "保存中に変更されました。");
                    File.Replace(tmp, path, path + ".bak");
                }
                else File.Move(tmp, path);
                return Sha256(path);
            }
            finally { if (File.Exists(tmp)) File.Delete(tmp); }
        }
        public static string PackChild(string directory, string relative)
        {
            if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative) || relative.Contains('\\') || relative.Split('/').Any(s => s == ".." || s == "." || s == ""))
                throw new ContractException("PACK_PATH_INVALID", "パック内のパスが不正です。");
            var root = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var path = Path.GetFullPath(Path.Combine(root, relative));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new ContractException("PACK_PATH_INVALID", "パックの外は参照できません。");
            var check = new FileInfo(path) as FileSystemInfo;
            while (check != null && check.FullName.Length >= root.Length)
            {
                if (check.Exists && (check.Attributes & FileAttributes.ReparsePoint) != 0) throw new ContractException("PACK_PATH_INVALID", "リンクはパックに使えません。");
                check = check is FileInfo file ? file.Directory : ((DirectoryInfo)check).Parent;
            }
            return path;
        }
    }
}
