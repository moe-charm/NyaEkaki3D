using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Import
{
    /// <summary>Persistable mapped VRM expressions. It contains no source model bytes.</summary>
    public sealed class VrmExpressionSession
    {
        public string SourceHash { get; }
        public string Format { get; }
        public string Title { get; }
        public string Author { get; }
        public IReadOnlyList<string> Authors { get; }
        public IReadOnlyList<MappedVrmExpression> Expressions { get; }

        internal VrmExpressionSession(string sourceHash, string format, string title, string author, IEnumerable<MappedVrmExpression> expressions, IEnumerable<string> authors = null)
        {
            Checks.HashText(sourceHash); Checks.Name(format); var values = (expressions ?? Array.Empty<MappedVrmExpression>()).ToArray(); Checks.Require(values.Length <= VrmMetadata.MaxExpressions, "BUDGET_EXCEEDED", "VRM expression session exceeds capacity.");
            var names = new HashSet<string>(StringComparer.Ordinal); foreach (var value in values) Checks.Require(value != null && names.Add(value.Name), "DUPLICATE_MORPH", "VRM expression session names must be unique.");
            SourceHash = sourceHash; Format = format; Title = title ?? ""; Authors = authors == null ? VrmAuthorNames.Legacy(author) : VrmAuthorNames.Copy(authors); Author = string.Join(", ", Authors); Expressions = Array.AsReadOnly(values);
        }

        public static VrmExpressionSession Create(VrmMetadata metadata, IEnumerable<MappedVrmExpression> expressions)
        {
            Checks.Require(metadata != null, "INVALID_VRM", "VRM metadata is required."); return new VrmExpressionSession(metadata.SourceHash, metadata.Format, metadata.Title, metadata.Author, expressions, metadata.Authors);
        }
    }

    /// <summary>Strict bounded JSON codec for the mapped expression sidecar.</summary>
    public static class VrmExpressionSessionCodec
    {
        const int Version = 2;

        public static byte[] Write(VrmExpressionSession session)
        {
            Checks.Require(session != null, "INVALID_VRM", "VRM expression session is required.");
            var expressions = new JArray(session.Expressions.Select(expression => new JObject
            {
                ["name"] = expression.Name, ["preset"] = expression.Preset, ["isCustom"] = expression.IsCustom,
                ["weights"] = new JArray(expression.Weights.Select(pair => new JObject { ["targetId"] = pair.Key, ["weight"] = pair.Value }))
            }));
            var root = new JObject { ["version"] = Version, ["sourceHash"] = session.SourceHash, ["format"] = session.Format, ["title"] = session.Title, ["authors"] = new JArray(session.Authors), ["expressions"] = expressions };
            var bytes = new UTF8Encoding(false).GetBytes(root.ToString(Formatting.Indented) + "\n"); Checks.Require(bytes.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "VRM expression session exceeds capacity."); return bytes;
        }

        public static VrmExpressionSession Read(byte[] bytes)
        {
            Checks.Require(bytes != null && bytes.Length > 0 && bytes.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "VRM expression session exceeds capacity.");
            try
            {
                JObject root;
                using (var text = new StringReader(new UTF8Encoding(false, true).GetString(bytes))) using (var reader = new JsonTextReader(text) { MaxDepth = 12, DateParseHandling = DateParseHandling.None })
                {
                    root = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error }); Checks.Require(!reader.Read(), "INVALID_VRM", "Trailing VRM expression session data is not allowed.");
                }
                int version = Int(root, "version");
                RequireFields(root, "version", "sourceHash", "format", "title", version == 1 ? "author" : "authors", "expressions"); Checks.Require(version == 1 || version == Version, "UNSUPPORTED_FORMAT", "VRM expression session version is unsupported.");
                string sourceHash = String(root, "sourceHash", 64); string format = String(root, "format", 32); string title = String(root, "title", 256, true); var authors = version == 1 ? VrmAuthorNames.Legacy(String(root, "author", 256, true)) : VrmAuthorNames.Read(root, false);
                var expressionArray = root["expressions"] as JArray; Checks.Require(expressionArray != null && expressionArray.Count <= VrmMetadata.MaxExpressions, "INVALID_VRM", "VRM expression session expressions are invalid.");
                var expressions = new List<MappedVrmExpression>();
                foreach (var token in expressionArray)
                {
                    var expression = token as JObject; Checks.Require(expression != null, "INVALID_VRM", "VRM expression session entry is invalid."); RequireFields(expression, "name", "preset", "isCustom", "weights");
                    string name = String(expression, "name", 128); string preset = String(expression, "preset", 128, true); bool isCustom = Bool(expression, "isCustom"); var weightsArray = expression["weights"] as JArray; Checks.Require(weightsArray != null && weightsArray.Count <= MorphSet.MaxTargets, "INVALID_VRM", "VRM expression session weights are invalid.");
                    var weights = new Dictionary<string, float>(StringComparer.Ordinal); foreach (var weightToken in weightsArray) { var weight = weightToken as JObject; Checks.Require(weight != null, "INVALID_VRM", "VRM expression session weight is invalid."); RequireFields(weight, "targetId", "weight"); string targetId = String(weight, "targetId", 36); Checks.Id(targetId); float value = Number(weight, "weight"); Checks.Require(weights.TryAdd(targetId, value), "DUPLICATE_MORPH", "VRM expression session target repeats."); }
                    expressions.Add(new MappedVrmExpression(name, preset, isCustom, weights));
                }
                return new VrmExpressionSession(sourceHash, format, title, "", expressions, authors);
            }
            catch (JsonException error) { throw new AuthoringException("INVALID_VRM", error.Message); }
            catch (DecoderFallbackException error) { throw new AuthoringException("INVALID_VRM", error.Message); }
        }

        static void RequireFields(JObject owner, params string[] names)
        {
            var expected = new HashSet<string>(names, StringComparer.Ordinal); foreach (var name in names) Checks.Require(owner[name] != null, "INVALID_VRM", "VRM expression session field is missing: " + name); foreach (var property in owner.Properties()) Checks.Require(expected.Contains(property.Name), "INVALID_VRM", "VRM expression session field is unknown: " + property.Name);
        }
        static int Int(JObject owner, string name) { var token = owner[name]; Checks.Require(token != null && token.Type == JTokenType.Integer, "INVALID_VRM", "VRM expression session integer is invalid: " + name); return (int)token; }
        static bool Bool(JObject owner, string name) { var token = owner[name]; Checks.Require(token != null && token.Type == JTokenType.Boolean, "INVALID_VRM", "VRM expression session boolean is invalid: " + name); return (bool)token; }
        static string String(JObject owner, string name, int maximum, bool optional = false) { var token = owner[name]; Checks.Require(token != null && token.Type == JTokenType.String && ((string)token).Length <= maximum && (optional || !string.IsNullOrWhiteSpace((string)token)), "INVALID_VRM", "VRM expression session text is invalid: " + name); return (string)token; }
        static float Number(JObject owner, string name) { var token = owner[name]; Checks.Require(token != null && (token.Type == JTokenType.Float || token.Type == JTokenType.Integer), "INVALID_VRM", "VRM expression session number is invalid: " + name); float value = (float)token; Checks.Finite(value); Checks.Require(value >= 0 && value <= 1, "INVALID_MORPH_WEIGHT", "VRM expression session weight is out of range."); return value; }
    }
}
