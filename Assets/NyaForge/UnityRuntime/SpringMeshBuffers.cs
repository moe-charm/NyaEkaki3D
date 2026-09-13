using System;
using NyaForge.Authoring;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    /// <summary>Reusable upload arrays for transient deformation of an unchanged mesh layout.</summary>
    internal sealed class SpringMeshBuffers
    {
        readonly Vector3[] vertices, normals;
        readonly Vector4[] tangents;
        internal readonly Vector3[] Points;
        internal readonly Vector3[] WorldPoints;

        internal SpringMeshBuffers(MeshData source)
        {
            vertices = new Vector3[source.VertexCount]; Points = new Vector3[source.VertexCount]; WorldPoints = new Vector3[source.VertexCount];
            normals = new Vector3[source.Normals.Count]; tangents = new Vector4[source.Tangents.Count];
        }

        internal void Apply(Mesh mesh, MeshData source, RestTransform transform)
        {
            var minimum = OwnedMeshProjection.ToUnity(source.Positions[0]); var maximum = minimum;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = OwnedMeshProjection.ToUnity(source.Positions[i]);
                Points[i] = OwnedMeshProjection.ToUnity(transform.ToAvatarPoint(source.Positions[i]));
                minimum = Vector3.Min(minimum, vertices[i]); maximum = Vector3.Max(maximum, vertices[i]);
            }
            var size = maximum - minimum;
            if (!Finite(size)) throw new InvalidOperationException("Spring mesh bounds exceed the supported range.");
            var bounds = new Bounds(minimum + size * .5f, size);
            for (int i = 0; i < normals.Length; i++) normals[i] = OwnedMeshProjection.ToUnity(source.Normals[i]);
            for (int i = 0; i < tangents.Length; i++) { var v = source.Tangents[i]; tangents[i] = new Vector4(v.X, v.Y, v.Z, v.W); }
            mesh.vertices = vertices;
            if (normals.Length > 0) mesh.normals = normals; else mesh.RecalculateNormals();
            if (tangents.Length > 0) mesh.tangents = tangents;
            mesh.bounds = bounds;
        }

        internal void UpdateWorldPoints(Transform root)
        {
            for (int i = 0; i < Points.Length; i++) WorldPoints[i] = root.TransformPoint(Points[i]);
        }

        static bool Finite(Vector3 value) => !float.IsInfinity(value.x) && !float.IsInfinity(value.y) && !float.IsInfinity(value.z)
            && !float.IsNaN(value.x) && !float.IsNaN(value.y) && !float.IsNaN(value.z);
    }
}
