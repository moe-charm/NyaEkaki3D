using System;
using System.Collections.Generic;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    /// <summary>Disposable selected-face overlay. Never modifies the source display mesh or document.</summary>
    sealed class FaceHighlightProjection : IDisposable
    {
        readonly Mesh mesh;
        readonly MeshRenderer renderer;
        readonly Mesh source;
        public int TriangleCount { get; private set; }

        public FaceHighlightProjection(Transform parent, Mesh source, Material material)
        {
            this.source = source;
            mesh = new Mesh { name = "Selected authoring faces", indexFormat = source.indexFormat };
            mesh.vertices = source.vertices;
            var overlay = new GameObject("Face selection") { layer = OwnedMeshProjection.PreviewLayer };
            overlay.transform.SetParent(parent, false);
            overlay.AddComponent<MeshFilter>().sharedMesh = mesh;
            renderer = overlay.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material; renderer.enabled = false;
        }

        public void Select(IReadOnlyList<ulong> triangleFaces, ISet<ulong> selected)
        {
            var indices = new List<int>(); int triangle = 0;
            if (triangleFaces != null && selected.Count > 0)
                for (int submesh = 0; submesh < source.subMeshCount; submesh++)
                {
                    var original = source.GetTriangles(submesh);
                    for (int i = 0; i < original.Length; i += 3, triangle++)
                        if (triangle < triangleFaces.Count && selected.Contains(triangleFaces[triangle]))
                        { indices.Add(original[i]); indices.Add(original[i + 1]); indices.Add(original[i + 2]); }
                }
            mesh.SetTriangles(indices, 0); TriangleCount = indices.Count / 3;
            renderer.enabled = TriangleCount > 0;
        }

        public void Dispose() { UnityEngine.Object.Destroy(mesh); }
    }
}
