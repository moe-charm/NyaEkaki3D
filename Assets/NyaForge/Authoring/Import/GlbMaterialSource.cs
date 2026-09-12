using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using NyaForge.Authoring.Graph;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring.Import
{
    /// <summary>One glTF material assignment for an imported submesh.</summary>
    public sealed class GlbMaterialSource
    {
        public int SubmeshIndex { get; }
        public int SourceMaterialIndex { get; }
        public string Name { get; }
        public MaterialParameters Parameters { get; }
        public bool HasTextureReferences { get; }

        internal GlbMaterialSource(int submeshIndex, int sourceMaterialIndex, string name, MaterialParameters parameters, bool hasTextureReferences)
        {
            Checks.Require(submeshIndex >= 0 && sourceMaterialIndex >= 0 && parameters != null, "INVALID_IMPORT", "GLB material source is incomplete.");
            Checks.Require(name != null && name.Length <= 256, "INVALID_IMPORT", "GLB material name is invalid.");
            SubmeshIndex = submeshIndex; SourceMaterialIndex = sourceMaterialIndex; Name = name; Parameters = parameters; HasTextureReferences = hasTextureReferences;
        }
    }

    internal static class GlbMaterialSourceReader
    {
        internal static IReadOnlyList<GlbMaterialSource> Read(JObject root, JObject mesh, IReadOnlyList<int> materialIndices)
        {
            var materials = root["materials"] as JArray;
            if (materials == null || materials.Count == 0 || materialIndices == null || materialIndices.Count == 0)
                return Array.AsReadOnly(Array.Empty<GlbMaterialSource>());
            var result = new List<GlbMaterialSource>();
            for (int submesh = 0; submesh < materialIndices.Count; submesh++)
            {
                int sourceIndex = materialIndices[submesh];
                if (sourceIndex < 0) continue;
                Checks.Require(sourceIndex < materials.Count, "INVALID_IMPORT", "GLB primitive material reference is out of range.");
                var token = materials[sourceIndex] as JObject;
                Checks.Require(token != null, "INVALID_IMPORT", "GLB material is invalid.");
                result.Add(Parse(submesh, sourceIndex, token));
            }
            return new ReadOnlyCollection<GlbMaterialSource>(result);
        }

        static GlbMaterialSource Parse(int submeshIndex, int sourceIndex, JObject token)
        {
            string name = token["name"]?.Type == JTokenType.String ? (string)token["name"] : "Material " + sourceIndex.ToString(CultureInfo.InvariantCulture);
            Checks.Require(name.Length <= 256, "INVALID_IMPORT", "GLB material name is too long.");
            var pbr = token["pbrMetallicRoughness"] as JObject;
            var baseColor = Color4(pbr?["baseColorFactor"], new[] { 1f, 1f, 1f, 1f }, "baseColorFactor");
            var metallic = Number(pbr?["metallicFactor"], 0f, "metallicFactor");
            var roughness = Number(pbr?["roughnessFactor"], 1f, "roughnessFactor");
            var emission = Color3(token["emissiveFactor"], new[] { 0f, 0f, 0f }, "emissiveFactor");
            MaterialAlphaMode alpha = MaterialAlphaMode.Opaque;
            if (token["alphaMode"]?.Type == JTokenType.String)
            {
                string mode = (string)token["alphaMode"];
                if (mode == "MASK") alpha = MaterialAlphaMode.Cutout;
                else if (mode == "BLEND") alpha = MaterialAlphaMode.Blend;
                else Checks.Require(mode == "OPAQUE", "UNSUPPORTED_FORMAT", "GLB material alphaMode is unsupported.");
            }
            float cutoff = Number(token["alphaCutoff"], .5f, "alphaCutoff");
            bool textures = pbr?["baseColorTexture"] != null || pbr?["metallicRoughnessTexture"] != null || token["normalTexture"] != null || token["occlusionTexture"] != null || token["emissiveTexture"] != null;
            return new GlbMaterialSource(submeshIndex, sourceIndex, name,
                new MaterialParameters(new Vec4(baseColor[0], baseColor[1], baseColor[2], baseColor[3]), metallic, roughness, new Vec3(emission[0], emission[1], emission[2]), alpha, cutoff), textures);
        }

        static float Number(JToken token, float fallback, string name, float maximum = 1f)
        {
            if (token == null) return fallback;
            Checks.Require(token.Type == JTokenType.Float || token.Type == JTokenType.Integer, "INVALID_IMPORT", "GLB material number is invalid: " + name);
            float value = (float)token; Checks.Require(!float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f && value <= maximum, "INVALID_IMPORT", "GLB material number is out of range: " + name); return value;
        }

        static float[] Color4(JToken token, float[] fallback, string name)
        {
            var values = Components(token, 4, fallback, name, 1f); return new[] { SrgbToLinear(values[0]), SrgbToLinear(values[1]), SrgbToLinear(values[2]), values[3] };
        }

        static float[] Color3(JToken token, float[] fallback, string name) => Components(token, 3, fallback, name, 64f);

        static float[] Components(JToken token, int count, float[] fallback, string name, float maximum)
        {
            if (token == null) return (float[])fallback.Clone();
            var array = token as JArray; Checks.Require(array != null && array.Count == count, "INVALID_IMPORT", "GLB material color is invalid: " + name);
            var result = new float[count]; for (int i = 0; i < count; i++) result[i] = Number(array[i], fallback[i], name + "[" + i.ToString(CultureInfo.InvariantCulture) + "]", maximum); return result;
        }

        static float SrgbToLinear(float value)
        {
            return value <= .04045f ? value / 12.92f : (float)Math.Pow((value + .055f) / 1.055f, 2.4);
        }
    }
}
