using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Geometry;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    /// <summary>Read-only orange overlay for the avatar surface region used by clothing fit.</summary>
    sealed class AvatarSurfaceSelectionProjection : IDisposable
    {
        readonly Material material;
        readonly GameObject root;
        readonly MeshFilter filter;
        readonly MeshRenderer renderer;
        readonly Mesh mesh;

        internal AvatarSurfaceSelectionProjection(Transform parent)
        {
            var shader = Resources.Load<Shader>("AuthoringSurface");
            if (!shader) throw new InvalidOperationException("Authoring surface shader missing");
            material = new Material(shader) { color = new Color(1f, .48f, .08f, .78f), renderQueue = 2001 };
            material.SetFloat("_DepthBias", -2);
            root = new GameObject("Avatar surface selection") { layer = OwnedMeshProjection.PreviewLayer };
            root.transform.SetParent(parent, false);
            mesh = new Mesh { name = "Selected avatar surface" };
            filter = root.AddComponent<MeshFilter>(); filter.sharedMesh = mesh;
            renderer = root.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.enabled = false;
        }

        internal void Refresh(MeshData source, RestTransform transform, IEnumerable<int> triangleIndices)
        {
            if (source == null || triangleIndices == null)
            {
                renderer.enabled = false;
                return;
            }
            var selected = new HashSet<int>(triangleIndices);
            if (selected.Count == 0)
            {
                renderer.enabled = false;
                return;
            }
            var points = source.Positions.Select(position => OwnedMeshProjection.ToUnity(transform.ToAvatarPoint(position))).ToArray();
            var triangles = new List<int>();
            int triangle = 0;
            foreach (var submesh in source.Submeshes)
            {
                for (int i = 0; i < submesh.Length; i += 3, triangle++)
                {
                    if (!selected.Contains(triangle)) continue;
                    triangles.Add(submesh[i]); triangles.Add(submesh[i + 1]); triangles.Add(submesh[i + 2]);
                }
            }
            mesh.indexFormat = points.Length > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.Clear();
            mesh.vertices = points;
            mesh.SetTriangles(triangles, 0, false);
            mesh.RecalculateBounds();
            renderer.enabled = triangles.Count > 0;
        }

        public void Dispose()
        {
            if (root != null) UnityEngine.Object.Destroy(root);
            if (mesh != null) UnityEngine.Object.Destroy(mesh);
            if (material != null) UnityEngine.Object.Destroy(material);
        }
    }
}
