using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring.Import
{
    /// <summary>Decodes all GLB source node transforms, independently of mesh/skin import profiles.</summary>
    public static class GlbNodeTransformReader
    {
        public static SourceNodeTransforms Read(byte[] bytes)
        {
            var document = GlbDocumentReader.Read(bytes);
            return Read(document.Root["nodes"] as JArray, document.SourceHash);
        }

        internal static SourceNodeTransforms Read(JArray nodes, string sourceHash)
        {
            Checks.Require(nodes != null && nodes.Count > 0 && nodes.Count <= ImportedSourceHierarchy.MaxNodes,
                "BUDGET_EXCEEDED", "Node transform inventory requires 1..4096 nodes.");
            var parents = Enumerable.Repeat(-1, nodes.Count).ToArray();
            var local = new SourceAffine[nodes.Count];
            var children = new IReadOnlyList<int>[nodes.Count];
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i] as JObject;
                Checks.Require(node != null, "INVALID_IMPORT", "Source node must be an object.");
                local[i] = ReadLocal(node);
                var childList = new List<int>();
                if (node["children"] != null)
                {
                    var array = node["children"] as JArray;
                    Checks.Require(array != null && array.Count <= nodes.Count, "INVALID_IMPORT", "Source children must be a bounded array.");
                    foreach (var token in array)
                    {
                        Checks.Require(token.Type == JTokenType.Integer, "INVALID_IMPORT", "Child index must be an integer.");
                        double child = (double)token;
                        Checks.Require(child >= 0 && child < nodes.Count, "INVALID_IMPORT", "Child index is out of range.");
                        Checks.Require(parents[(int)child] < 0, "INVALID_IMPORT", "Source child repeats or has multiple parents.");
                        parents[(int)child] = i; childList.Add((int)child);
                    }
                }
                children[i] = childList.AsReadOnly();
            }
            return new SourceNodeTransforms(sourceHash, parents, local, children);
        }

        static SourceAffine ReadLocal(JObject node)
        {
            if (node["matrix"] != null)
            {
                Checks.Require(node["translation"] == null && node["rotation"] == null && node["scale"] == null,
                    "INVALID_IMPORT", "Node cannot mix matrix and TRS fields.");
                return new SourceAffine(Numbers(node["matrix"], 16));
            }
            var translation = node["translation"] == null ? new[] { 0d, 0d, 0d } : Numbers(node["translation"], 3);
            var rotation = node["rotation"] == null ? new[] { 0d, 0d, 0d, 1d } : Numbers(node["rotation"], 4);
            var scale = node["scale"] == null ? new[] { 1d, 1d, 1d } : Numbers(node["scale"], 3);
            return SourceAffine.FromTrs(Vector(translation), new Vec4((float)rotation[0], (float)rotation[1], (float)rotation[2], (float)rotation[3]), Vector(scale));
        }

        static Vec3 Vector(double[] values) => new Vec3((float)values[0], (float)values[1], (float)values[2]);

        static double[] Numbers(JToken token, int count)
        {
            var array = token as JArray;
            Checks.Require(array != null && array.Count == count, "INVALID_IMPORT", "Node transform has the wrong number of components.");
            var result = new double[count];
            for (int i = 0; i < count; i++)
            {
                Checks.Require(array[i].Type == JTokenType.Integer || array[i].Type == JTokenType.Float,
                    "INVALID_IMPORT", "Node transform components must be numeric.");
                double value = (double)array[i];
                Checks.Require(!double.IsNaN(value) && !double.IsInfinity(value) && Math.Abs(value) <= float.MaxValue,
                    "INVALID_IMPORT", "Node transform component exceeds finite float range.");
                result[i] = value;
            }
            return result;
        }
    }
}
