using System;
using UnityEngine;

namespace NyaForge.Rendering
{
    /// <summary>Completes an owned Unity display mesh without modifying the authoring source.</summary>
    public static class DisplayMeshNormals
    {
        public static void Ensure(Mesh mesh)
        {
            if (mesh == null) throw new ArgumentNullException(nameof(mesh));
            if (!mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.Normal))
                mesh.RecalculateNormals();
        }
    }
}
