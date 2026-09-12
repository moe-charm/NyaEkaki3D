using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace NyaForge.Authoring
{
    internal static class Storage
    {
        internal const string Profile = "static-triangles-uniform-v1";
        internal const string Coordinates = "unity-y-up-left-handed";
        internal static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(), MissingMemberHandling = MissingMemberHandling.Error,
            TypeNameHandling = TypeNameHandling.None, MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            MaxDepth = 12, FloatParseHandling = FloatParseHandling.Double, Culture = System.Globalization.CultureInfo.InvariantCulture
        };
        internal static byte[] ReadBounded(string path, int maximum)
        {
            using (var stream = new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))
            {
                Checks.Require(stream.Length >= 1 && stream.Length <= maximum,"BUDGET_EXCEEDED","File length exceeds supported budget.");
                var bytes = new byte[(int)stream.Length]; int position = 0;
                while (position < bytes.Length) { int n = stream.Read(bytes,position,bytes.Length - position); Checks.Require(n > 0,"INVALID_FILE","File was truncated during read."); position += n; }
                return bytes;
            }
        }
        internal static JObject ReadObject(string path)
        {
            try
            {
                var bytes = ReadBounded(path,AuthoringLimits.MaxManifestBytes);
                using (var text = new StringReader(new UTF8Encoding(false,true).GetString(bytes))) using (var reader = new JsonTextReader(text) { MaxDepth = 12, DateParseHandling = DateParseHandling.None })
                {
                    var token = JObject.Load(reader,new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error, CommentHandling = CommentHandling.Load });
                    Checks.Require(!reader.Read(),"INVALID_MANIFEST","Trailing JSON content is not allowed.");
                    RejectNonData(token);
                    return token;
                }
            }
            catch (JsonException e) { throw new AuthoringException("INVALID_MANIFEST",e.Message); }
            catch (DecoderFallbackException e) { throw new AuthoringException("INVALID_MANIFEST",e.Message); }
        }
        internal static T ReadJson<T>(string path) { return Decode<T>(ReadObject(path)); }
        internal static T Decode<T>(JObject value)
        {
            try { ValidateShape(value, typeof(T)); return value.ToObject<T>(JsonSerializer.Create(Settings)); }
            catch (JsonException e) { throw new AuthoringException("INVALID_MANIFEST", e.Message); }
        }
        internal static int SchemaVersion(JObject value)
        {
            var token = value["schemaVersion"];
            Checks.Require(token != null && token.Type == JTokenType.Integer, "INVALID_MANIFEST", "Missing or invalid schemaVersion.");
            long version;
            Checks.Require(long.TryParse(token.ToString(), out version) && (version == 1 || version == 2 || version == 3), "UNSUPPORTED_FORMAT", "Unsupported native project version.");
            return (int)version;
        }
        private static void RejectNonData(JToken token)
        {
            Checks.Require(token.Type != JTokenType.Comment && token.Type != JTokenType.Null && token.Type != JTokenType.Undefined,"INVALID_MANIFEST","Null and comments are not manifest data.");
            if (token.Type == JTokenType.Float) { double value = token.Value<double>(); Checks.Require(!double.IsNaN(value) && !double.IsInfinity(value),"NON_FINITE","Manifest numbers must be finite."); }
            foreach (var child in token.Children()) RejectNonData(child);
        }
        private static void ValidateShape(JObject value, Type type)
        {
            var fields = type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in fields)
            {
                string name = char.ToLowerInvariant(field.Name[0]) + field.Name.Substring(1); names.Add(name);
                JToken token; Checks.Require(value.TryGetValue(name,StringComparison.Ordinal,out token),"INVALID_MANIFEST","Missing field: " + name);
                Type target = field.FieldType;
                if (target.IsArray)
                {
                    Checks.Require(token.Type == JTokenType.Array, "INVALID_MANIFEST", "Expected array: " + name);
                    Checks.Require(((JArray)token).Count <= 64, "BUDGET_EXCEEDED", "Manifest array budget exceeded.");
                    foreach (var item in (JArray)token)
                    {
                        if (target.GetElementType() == typeof(int))
                        {
                            Checks.Require(item.Type == JTokenType.Integer, "INVALID_MANIFEST", "Expected integer array item.");
                            continue;
                        }
                        Checks.Require(item.Type == JTokenType.Object, "INVALID_MANIFEST", "Expected array object.");
                        ValidateShape((JObject)item, target.GetElementType());
                    }
                    continue;
                }
                bool correct = target == typeof(string) ? token.Type == JTokenType.String : target == typeof(bool) ? token.Type == JTokenType.Boolean : target == typeof(int) || target == typeof(long) ? token.Type == JTokenType.Integer : target == typeof(float) ? token.Type == JTokenType.Float || token.Type == JTokenType.Integer : token.Type == JTokenType.Object;
                Checks.Require(correct,"INVALID_MANIFEST","Wrong JSON type: " + name);
                if (token.Type == JTokenType.Object) ValidateShape((JObject)token,target);
            }
            foreach (var property in value.Properties()) Checks.Require(names.Contains(property.Name),"INVALID_MANIFEST","Unknown field: " + property.Name);
        }
        internal static byte[] JsonBytes(object value)
        {
            var bytes = new UTF8Encoding(false).GetBytes(JsonConvert.SerializeObject(value,Formatting.Indented,Settings) + "\n");
            Checks.Require(bytes.Length <= AuthoringLimits.MaxManifestBytes,"BUDGET_EXCEEDED","Manifest exceeds budget."); return bytes;
        }
        internal static FileStream Lock(string directory)
        {
            Directory.CreateDirectory(directory);
            try { return new FileStream(Path.Combine(directory,".nyaforge.writer.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None); }
            catch (IOException) { throw new AuthoringException("PROJECT_LOCKED","Another writer is saving this directory."); }
        }
        internal static void WriteBlob(string directory, string hash, byte[] bytes)
        {
            Checks.HashText(hash); Checks.Require(bytes.Length <= AuthoringLimits.MaxBlobBytes && Checks.Hash(bytes) == hash,"INVALID_BLOB","Blob hash or length is invalid.");
            string blobs = Path.Combine(directory,"blobs"); Directory.CreateDirectory(blobs); string path = Path.Combine(blobs,hash + ".bin");
            if (File.Exists(path)) { ReadBlob(directory,hash); return; }
            AtomicWrite(path,bytes,false); ReadBlob(directory,hash);
        }
        internal static byte[] ReadBlob(string directory, string hash)
        {
            Checks.HashText(hash); var bytes = ReadBounded(Path.Combine(directory,"blobs",hash + ".bin"),AuthoringLimits.MaxBlobBytes);
            Checks.Require(Checks.Hash(bytes) == hash,"HASH_MISMATCH","Blob content does not match its name."); return bytes;
        }
        internal static void AtomicWrite(string path, byte[] bytes, bool replace)
        {
            // Keep the temporary sibling shorter than hash-named blobs. Appending
            // another GUID to the destination can exceed Windows Player path limits.
            string temporary = Path.Combine(Path.GetDirectoryName(path), ".nyaf-" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var stream = new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)) { stream.Write(bytes,0,bytes.Length); stream.Flush(true); }
                if (replace && File.Exists(path)) File.Replace(temporary,path,null); else File.Move(temporary,path);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        internal static string DirectoryPath(string directory) { Checks.Require(!string.IsNullOrWhiteSpace(directory),"INVALID_PATH","A project directory is required."); return Path.GetFullPath(directory); }
        internal static void ValidateProfile(string profile, string units, string coordinates, string normalPolicy)
        {
            Checks.Require(profile == Profile && units == "meters" && coordinates == Coordinates && normalPolicy == "preserve","EXPORT_UNSUPPORTED_FEATURE","Only static triangles, positive uniform scale, translation and preserved normals are supported. Skin, morph, animation and shader conversion are not supported.");
        }
    }
}
