using System.Linq;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring.Import
{
    internal static class ImportedSourceHierarchyJson
    {
        // Array position is the source node index; no sparse indices or inferred nodes.
        internal static JToken Write(ImportedSourceHierarchy hierarchy) => hierarchy == null ? (JToken)JValue.CreateNull()
            : new JArray(hierarchy.Parents.Select((parent, i) => new JObject { ["parent"] = parent,
                ["children"] = new JArray(hierarchy.Children[i]), ["origin"] = new JArray(hierarchy.Origins[i].X, hierarchy.Origins[i].Y, hierarchy.Origins[i].Z) }));

        internal static ImportedSourceHierarchy Read(JToken token)
        {
            Checks.Require(token != null, "INVALID_IMPORT", "Source hierarchy field is missing.");
            if (token.Type == JTokenType.Null) return null;
            var array = token as JArray;
            Checks.Require(array != null && array.Count > 0 && array.Count <= ImportedSourceHierarchy.MaxNodes, "INVALID_IMPORT", "Source hierarchy exceeds capacity.");
            var children = new System.Collections.Generic.IReadOnlyList<int>[array.Count];
            var parents = new int[array.Count]; var origins = new Vec3[array.Count];
            for (int i = 0; i < array.Count; i++)
            {
                var item = array[i] as JObject;
                Checks.Require(item != null && item.Count == 3 && item["parent"]?.Type == JTokenType.Integer, "INVALID_IMPORT", "Source hierarchy fields are invalid.");
                long parent = (long)item["parent"];
                Checks.Require(parent >= -1 && parent < array.Count, "INVALID_IMPORT", "Source parent is out of range.");
                parents[i] = (int)parent;
                var childList = item["children"] as JArray;
                Checks.Require(childList != null && childList.Count <= array.Count, "INVALID_IMPORT", "Source children are invalid.");
                children[i] = childList.Select(child => {
                    Checks.Require(child.Type == JTokenType.Integer, "INVALID_IMPORT", "Source child must be an index.");
                    long index = (long)child; Checks.Require(index >= 0 && index < array.Count, "INVALID_IMPORT", "Source child is out of range."); return (int)index;
                }).ToArray();
                var vector = item["origin"] as JArray;
                Checks.Require(vector != null && vector.Count == 3, "INVALID_IMPORT", "Source origin requires three numbers.");
                origins[i] = new Vec3(Number(vector[0]), Number(vector[1]), Number(vector[2]));
            }
            return new ImportedSourceHierarchy(parents, origins, children);
        }

        static float Number(JToken token)
        {
            Checks.Require(token.Type == JTokenType.Float || token.Type == JTokenType.Integer, "INVALID_IMPORT", "Source origin must be numeric.");
            float value = (float)token; Checks.Finite(value); return value;
        }
    }
}
