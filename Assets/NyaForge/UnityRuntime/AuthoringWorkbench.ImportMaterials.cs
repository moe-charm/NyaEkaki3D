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
                nodes.Add(GraphNode.StandardMaterial(materialId, material?.Parameters ?? MaterialParameters.Default));
                edges.Add(new GraphEdge(materialId, "material", assignmentId, GraphNode.MaterialSlotPort(slot)));
                if (material?.HasEmbeddedBaseColorImage == true)
                {
                    PaintImage image;
                    try { image = DecodeEmbeddedImage(material); }
                    catch (AuthoringException error)
                    {
                        string message = "material " + material.SourceMaterialIndex + " のbase color画像をnative Paintへ保持できないため省略しました（" + error.Code + "）。";
                        warnings?.Add(message); Debug.LogWarning(message);
                        continue;
                    }
                    string imageId = Guid.NewGuid().ToString("D");
                    nodes.Add(GraphNode.Paint(imageId, image.Width, image.Height, image));
                    edges.Add(new GraphEdge(meshNodeId, "mesh", imageId, "mesh"));
                    edges.Add(new GraphEdge(imageId, "image", materialId, "baseColor"));
                }
            }
            return assignmentId;
        }

        static PaintImage DecodeEmbeddedImage(GlbMaterialSource material)
        {
            byte[] bytes = material.CopyBaseColorImageBytes();
            if (bytes == null || bytes.Length == 0) throw new AuthoringException("INVALID_IMAGE", "base color画像が空です。");
            Texture2D texture = null;
            try
            {
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
                if (!texture.LoadImage(bytes, false)) throw new AuthoringException("INVALID_IMAGE", "base color画像を読み込めませんでした。");
                if (texture.width < 1 || texture.height < 1)
                    throw new AuthoringException("IMAGE_DIMENSION_EXCEEDED", "base color画像の寸法が不正です（" + texture.width + "x" + texture.height + "）。");
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
