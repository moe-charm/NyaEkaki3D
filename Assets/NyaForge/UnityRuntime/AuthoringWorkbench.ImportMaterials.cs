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
        static string AppendImportedMaterials(List<GraphNode> nodes, List<GraphEdge> edges, string meshNodeId, int submeshCount, IReadOnlyList<GlbMaterialSource> materials)
        {
            if (materials == null || materials.Count != submeshCount || materials.Count == 0) return meshNodeId;
            var slots = materials.Select(item => item.SubmeshIndex).Distinct().OrderBy(item => item).ToArray();
            if (!slots.SequenceEqual(Enumerable.Range(0, submeshCount))) return meshNodeId;
            string assignmentId = Guid.NewGuid().ToString("D");
            nodes.Add(GraphNode.AssignMaterials(assignmentId, slots));
            edges.Add(new GraphEdge(meshNodeId, "mesh", assignmentId, "mesh"));
            foreach (var material in materials)
            {
                string materialId = Guid.NewGuid().ToString("D");
                nodes.Add(GraphNode.StandardMaterial(materialId, material.Parameters));
                edges.Add(new GraphEdge(materialId, "material", assignmentId, GraphNode.MaterialSlotPort(material.SubmeshIndex)));
                if (material.HasEmbeddedBaseColorImage)
                {
                    var image = DecodeEmbeddedImage(material);
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
                PaintImage.ValidateDimensions(texture.width, texture.height);
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
