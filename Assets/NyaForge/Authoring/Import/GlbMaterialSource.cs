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
        public bool HasEmbeddedBaseColorImage { get { return baseColorImage != null; } }
        public int BaseColorImageIndex { get; }
        public string BaseColorImageMimeType { get; }
        readonly byte[] baseColorImage;

        internal GlbMaterialSource(int submeshIndex, int sourceMaterialIndex, string name, MaterialParameters parameters, bool hasTextureReferences, int baseColorImageIndex = -1, string baseColorImageMimeType = "", byte[] baseColorImage = null)
        {
            Checks.Require(submeshIndex >= 0 && sourceMaterialIndex >= 0 && parameters != null, "INVALID_IMPORT", "GLB material source is incomplete.");
            Checks.Require(name != null && name.Length <= 256, "INVALID_IMPORT", "GLB material name is invalid.");
            Checks.Require(baseColorImageIndex >= -1, "INVALID_IMPORT", "GLB base color image index is invalid.");
            Checks.Require(baseColorImageMimeType != null && baseColorImageMimeType.Length <= 128, "INVALID_IMPORT", "GLB image mime type is invalid.");
            Checks.Require(baseColorImage == null || baseColorImage.Length > 0 && baseColorImage.Length <= 16 * 1024 * 1024, "IMAGE_BUDGET_EXCEEDED", "Embedded GLB image exceeds the 16 MiB image budget.");
            SubmeshIndex = submeshIndex; SourceMaterialIndex = sourceMaterialIndex; Name = name; Parameters = parameters; HasTextureReferences = hasTextureReferences;
            BaseColorImageIndex = baseColorImageIndex; BaseColorImageMimeType = baseColorImageMimeType; this.baseColorImage = baseColorImage == null ? null : (byte[])baseColorImage.Clone();
        }

        public byte[] CopyBaseColorImageBytes() { return baseColorImage == null ? null : (byte[])baseColorImage.Clone(); }
    }

    internal static class GlbMaterialSourceReader
    {
        internal static IReadOnlyList<GlbMaterialSource> Read(JObject root, JObject mesh, IReadOnlyList<int> materialIndices, byte[] bin, JArray views)
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
                result.Add(Parse(submesh, sourceIndex, token, root, bin, views));
            }
            return new ReadOnlyCollection<GlbMaterialSource>(result);
        }

        static GlbMaterialSource Parse(int submeshIndex, int sourceIndex, JObject token, JObject root, byte[] bin, JArray views)
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
            int imageIndex; string imageMimeType; byte[] imageBytes;
            ReadBaseColorImage(root, pbr?["baseColorTexture"] as JObject, bin, views, out imageIndex, out imageMimeType, out imageBytes);
            return new GlbMaterialSource(submeshIndex, sourceIndex, name,
                new MaterialParameters(new Vec4(baseColor[0], baseColor[1], baseColor[2], baseColor[3]), metallic, roughness, new Vec3(emission[0], emission[1], emission[2]), alpha, cutoff), textures, imageIndex, imageMimeType, imageBytes);
        }

        static void ReadBaseColorImage(JObject root, JObject textureReference, byte[] bin, JArray views, out int imageIndex, out string mimeType, out byte[] bytes)
        {
            imageIndex = -1; mimeType = ""; bytes = null;
            if (textureReference == null) return;
            var textures = root["textures"] as JArray; var images = root["images"] as JArray;
            if (textures == null || images == null || textureReference["index"] == null) return;
            Checks.Require(textureReference["index"].Type == JTokenType.Integer, "INVALID_IMPORT", "GLB base color texture index is invalid.");
            int textureIndex = (int)textureReference["index"]; Checks.Require(textureIndex >= 0 && textureIndex < textures.Count, "INVALID_IMPORT", "GLB base color texture index is out of range.");
            var texture = textures[textureIndex] as JObject; Checks.Require(texture != null, "INVALID_IMPORT", "GLB texture is invalid.");
            if (texture["source"] == null) return;
            Checks.Require(texture["source"].Type == JTokenType.Integer, "INVALID_IMPORT", "GLB texture source index is invalid.");
            imageIndex = (int)texture["source"]; Checks.Require(imageIndex >= 0 && imageIndex < images.Count, "INVALID_IMPORT", "GLB texture source index is out of range.");
            var image = images[imageIndex] as JObject; Checks.Require(image != null, "INVALID_IMPORT", "GLB image is invalid.");
            mimeType = image["mimeType"]?.Type == JTokenType.String ? (string)image["mimeType"] : "";
            if (image["bufferView"] == null) return; // external URI: report the reference but do not fetch it.
            Checks.Require(image["bufferView"].Type == JTokenType.Integer, "INVALID_IMPORT", "GLB image bufferView is invalid.");
            int viewIndex = (int)image["bufferView"]; Checks.Require(viewIndex >= 0 && viewIndex < views.Count, "INVALID_IMPORT", "GLB image bufferView is out of range.");
            var view = views[viewIndex] as JObject; Checks.Require(view != null, "INVALID_IMPORT", "GLB image bufferView is invalid.");
            int offset = view["byteOffset"] == null ? 0 : Integer(view["byteOffset"], "image byteOffset");
            int length = Integer(view["byteLength"], "image byteLength");
            Checks.Require(offset >= 0 && length > 0 && length <= 16 * 1024 * 1024 && (long)offset + length <= bin.Length, "IMAGE_BUDGET_EXCEEDED", "Embedded GLB image exceeds the image budget or BIN chunk.");
            bytes = new byte[length]; Buffer.BlockCopy(bin, offset, bytes, 0, length);
        }

        static int Integer(JToken token, string name)
        {
            Checks.Require(token != null && token.Type == JTokenType.Integer, "INVALID_IMPORT", "GLB material integer is invalid: " + name);
            long value = (long)token; Checks.Require(value >= 0 && value <= int.MaxValue, "INVALID_IMPORT", "GLB material integer is out of range: " + name); return (int)value;
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
            // The glTF endpoint value 1 can round slightly above one when the
            // exponent is evaluated in double precision and converted to float.
            // MaterialParameters treats the normalized range as a hard contract.
            double converted = value <= .04045f ? value / 12.92 : Math.Pow((value + .055) / 1.055, 2.4);
            return (float)Math.Max(0d, Math.Min(1d, converted));
        }
    }
}
