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
                        string message = "material " + material.SourceMaterialIndex + " の埋め込みbase color画像をnative Paintへ保持できないため省略しました（" + error.Code + "）。";
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
            if (bytes == null || bytes.Length == 0) throw new AuthoringException("INVALID_IMAGE", "埋め込みbase color画像が空です。");
            Texture2D texture = null;
            try
            {
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false);
                if (!texture.LoadImage(bytes, false)) throw new AuthoringException("INVALID_IMAGE", "埋め込みbase color画像を読み込めませんでした。");
                if (texture.width < 1 || texture.height < 1 || texture.width > 1024 || texture.height > 1024)
                    throw new AuthoringException("IMAGE_DIMENSION_EXCEEDED", "埋め込みbase color画像は1024x1024以内で保持します（" + texture.width + "x" + texture.height + "）。");
                var colors = texture.GetPixels32();
                var rgba = new byte[colors.Length * 4];
                for (int i = 0; i < colors.Length; i++)
                {
                    int at = i * 4; rgba[at] = colors[i].r; rgba[at + 1] = colors[i].g; rgba[at + 2] = colors[i].b; rgba[at + 3] = colors[i].a;
                }
                return PaintImage.FromRgbaBottomLeft(texture.width, texture.height, rgba);
            }
            finally { if (texture != null) UnityEngine.Object.Destroy(texture); }
        }
    }
}
