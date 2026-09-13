using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Paint;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        static string AppendImportedMaterials(List<GraphNode> nodes, List<GraphEdge> edges, string meshNodeId, int submeshCount, IReadOnlyList<GlbMaterialSource> materials, List<string> warnings = null)
        {
            if (materials == null || materials.Count == 0 || submeshCount <= 0) return meshNodeId;
            // A glTF primitive may omit its material. Build the assignment for
            // every slot and fill only the omitted slots with the standard
            // default, preserving all explicitly imported material/image links.
            var bySlot = new Dictionary<int, GlbMaterialSource>();
            foreach (var material in materials)
            {
                Checks.Require(material.SubmeshIndex >= 0 && material.SubmeshIndex < submeshCount && !bySlot.ContainsKey(material.SubmeshIndex),
                    "INVALID_IMPORT", "Imported material slot is duplicated or outside the mesh.");
                bySlot.Add(material.SubmeshIndex, material);
            }
            var slots = Enumerable.Range(0, submeshCount).ToArray();
            string assignmentId = Guid.NewGuid().ToString("D");
            nodes.Add(GraphNode.AssignMaterials(assignmentId, slots));
            edges.Add(new GraphEdge(meshNodeId, "mesh", assignmentId, "mesh"));
            foreach (int slot in slots)
            {
                bySlot.TryGetValue(slot, out var material);
                string materialId = Guid.NewGuid().ToString("D");
                var textures = BuildSemanticTextures(material, warnings);
                nodes.Add(GraphNode.StandardMaterial(materialId, material?.Parameters ?? MaterialParameters.Default, textures));
                edges.Add(new GraphEdge(materialId, "material", assignmentId, GraphNode.MaterialSlotPort(slot)));
                if (material?.HasEmbeddedBaseColorImage == true)
                {
                    string imageId = Guid.NewGuid().ToString("D");
                    PaintImage image; int sourceWidth, sourceHeight; GraphOriginalImage original;
                    try
                    {
                        image = DecodeEmbeddedImage(material, out sourceWidth, out sourceHeight);
                        string sourceMime = ImageMime(material);
                        original = new GraphOriginalImage(imageId, sourceWidth, sourceHeight, sourceMime, material.CopyBaseColorImageBytes(), Checks.Hash(PaintImageCodec.Write(image)));
                    }
                    catch (AuthoringException error)
                    {
                        string message = "material " + material.SourceMaterialIndex + " のbase color画像をnative Paintへ保持できないため省略しました（" + error.Code + "）。";
                        warnings?.Add(message); Debug.LogWarning(message);
                        continue;
                    }
                    nodes.Add(GraphNode.Paint(imageId, image.Width, image.Height, image));
                    edges.Add(new GraphEdge(meshNodeId, "mesh", imageId, "mesh"));
                    edges.Add(new GraphEdge(imageId, "image", materialId, "baseColor"));
                    nodes.Add(GraphNode.OriginalImageNode(Guid.NewGuid().ToString("D"), original));
                    if (sourceWidth != image.Width || sourceHeight != image.Height)
                        warnings?.Add("material " + material.SourceMaterialIndex + " のbase color画像を " + sourceWidth + "x" + sourceHeight + " から " + image.Width + "x" + image.Height + " へ縮小しました。native projectは作業画像と原画像bytesを別保持します。");
                }
            }
            return assignmentId;
        }

        static PaintImage DecodeEmbeddedImage(GlbMaterialSource material, out int sourceWidth, out int sourceHeight)
        {
            sourceWidth = sourceHeight = 0;
            byte[] bytes = material.CopyBaseColorImageBytes();
            if (bytes == null || bytes.Length == 0) throw new AuthoringException("INVALID_IMAGE", "base color画像が空です。");
            Texture2D texture = null;
            try
            {
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
                if (!texture.LoadImage(bytes, false)) throw new AuthoringException("INVALID_IMAGE", "base color画像を読み込めませんでした。");
                if (texture.width < 1 || texture.height < 1)
                    throw new AuthoringException("IMAGE_DIMENSION_EXCEEDED", "base color画像の寸法が不正です（" + texture.width + "x" + texture.height + "）。");
                sourceWidth = texture.width; sourceHeight = texture.height;
                // Keep the native Paint budget deterministic while retaining the
                // visual reference of common 2K/4K avatar textures. Decode only
                // up to a bounded source size, then downsample in CPU space so
                // the persisted project owns the resulting pixels and never
                // depends on the original GLB after Save/Open.
                if (texture.width > 8192 || texture.height > 8192)
                    throw new AuthoringException("IMAGE_DIMENSION_EXCEEDED", "base color画像は8192px以内で読み込みます（" + texture.width + "x" + texture.height + "）。");
                var colors = texture.GetPixels32();
                int targetWidth = texture.width, targetHeight = texture.height;
                if (targetWidth > 1024 || targetHeight > 1024)
                {
                    float scale = Mathf.Min(1024f / targetWidth, 1024f / targetHeight);
                    targetWidth = Mathf.Max(1, Mathf.RoundToInt(targetWidth * scale));
                    targetHeight = Mathf.Max(1, Mathf.RoundToInt(targetHeight * scale));
                }
                var rgba = ResizeRgba(colors, texture.width, texture.height, targetWidth, targetHeight);
                return PaintImage.FromRgbaBottomLeft(targetWidth, targetHeight, rgba);
            }
            finally { if (texture != null) UnityEngine.Object.Destroy(texture); }
        }

        static string ImageMime(GlbMaterialSource material)
        {
            string mime = material?.BaseColorImageMimeType ?? "";
            if (mime == "image/png" || mime == "image/jpeg") return mime;
            throw new AuthoringException("UNSUPPORTED_FORMAT", "base color画像のMIME typeがPNG/JPEGではありません。");
        }

        static MaterialTextureSet BuildSemanticTextures(GlbMaterialSource material, List<string> warnings)
        {
            if (material == null) return null;
            var normal = BuildSemanticTexture(material.NormalTexture, warnings);
            var metallicRoughness = BuildSemanticTexture(material.MetallicRoughnessTexture, warnings);
            return normal == null && metallicRoughness == null ? null : new MaterialTextureSet(normal, metallicRoughness);
        }

        static MaterialTextureSlot BuildSemanticTexture(GlbTextureImage source, List<string> warnings)
        {
            if (source == null) return null;
            if (!source.HasImageBytes)
            {
                warnings?.Add((source.Semantic == MaterialTextureSemantic.Normal ? "normal" : "metallic-roughness") + " texture reference was not retained because its local image bytes were unavailable.");
                return null;
            }
            return new MaterialTextureSlot(source.Semantic, source.CopyImageBytes(), source.MimeType, source.TexCoord, source.NormalScale, source.Sampler);
        }

        static byte[] ResizeRgba(Color32[] source, int sourceWidth, int sourceHeight, int targetWidth, int targetHeight)
        {
            Checks.Require(source != null && source.Length == sourceWidth * sourceHeight, "INVALID_IMAGE", "Decoded image pixels are incomplete.");
            Checks.Require(targetWidth > 0 && targetHeight > 0 && targetWidth <= 1024 && targetHeight <= 1024, "IMAGE_DIMENSION_EXCEEDED", "Native Paint image dimensions exceed 1024px.");
            var rgba = new byte[targetWidth * targetHeight * 4];
            for (int y = 0; y < targetHeight; y++)
            {
                int sourceY = Mathf.Min(sourceHeight - 1, Mathf.FloorToInt((y + .5f) * sourceHeight / targetHeight));
                for (int x = 0; x < targetWidth; x++)
                {
                    int sourceX = Mathf.Min(sourceWidth - 1, Mathf.FloorToInt((x + .5f) * sourceWidth / targetWidth));
                    var pixel = source[sourceY * sourceWidth + sourceX];
                    int at = (y * targetWidth + x) * 4;
                    rgba[at] = pixel.r; rgba[at + 1] = pixel.g; rgba[at + 2] = pixel.b; rgba[at + 3] = pixel.a;
                }
            }
            return rgba;
        }
    }
}
