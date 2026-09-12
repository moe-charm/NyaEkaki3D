using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring.Import
{
    /// <summary>Ordered author identity shared by metadata and session codecs.</summary>
    internal static class VrmAuthorNames
    {
        const int MaxCount = 256;
        public static IReadOnlyList<string> Legacy(string author)
            => Copy(string.IsNullOrEmpty(author) ? Array.Empty<string>() : new[] { author });

        public static IReadOnlyList<string> Copy(IEnumerable<string> authors)
        {
            var values = authors.Take(MaxCount + 1).ToArray();
            Checks.Require(values.Length <= MaxCount, "BUDGET_EXCEEDED", "VRM author count exceeds capacity.");
            foreach (var value in values)
                Checks.Require(value != null && value.Length > 0 && value.Length <= 256, "INVALID_VRM", "VRM author name is invalid.");
            return Array.AsReadOnly(values);
        }

        public static IReadOnlyList<string> Read(JObject owner, bool required)
        {
            var values = owner?["authors"] as JArray;
            Checks.Require(values != null && (!required || values.Count > 0), "INVALID_VRM", "VRM authors must be an array of names.");
            Checks.Require(values.Count <= MaxCount, "BUDGET_EXCEEDED", "VRM author count exceeds capacity.");
            foreach (var value in values)
                Checks.Require(value.Type == JTokenType.String, "INVALID_VRM", "VRM author must be text.");
            return Copy(values.Select(value => (string)value));
        }
    }
}
