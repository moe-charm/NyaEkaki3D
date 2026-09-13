using System;
using System.Linq;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    /// <summary>Read-only final-result backdrop. No colliders, markers or selection mappings.</summary>
    sealed class FinalResultProjection : IDisposable
    {
        public Mesh Mesh { get; }
        public Vector3[] Points { get; }
        public Vector3[] WorldPoints { get; }
        readonly GameObject root;

        public FinalResultProjection(Transform parent, GraphMeshValue value, Material material, PoseTransform? attachmentPose = null)
        {
            Mesh = OwnedMeshProjection.CreateMesh(value.Mesh);
            try
            {
                root = new GameObject("Final result (read only)") { layer = OwnedMeshProjection.PreviewLayer };
                root.transform.SetParent(parent, false);
                root.transform.localScale = Vector3.one * value.Transform.Scale;
                root.transform.localPosition = OwnedMeshProjection.ToUnity(value.Transform.Translation);
                root.AddComponent<MeshFilter>().sharedMesh = Mesh;
                root.AddComponent<MeshRenderer>().sharedMaterials = Enumerable.Repeat(material, value.Mesh.Submeshes.Count).ToArray();
                // The final root carries the authored value transform. Keep the
                // points local so FramingPoints and rendering apply that transform
                // exactly once; the parent already carries any attachment pose.
                Points = value.Mesh.Positions.Select(OwnedMeshProjection.ToUnity).ToArray();
                WorldPoints = Points.Select(point => root.transform.TransformPoint(point)).ToArray();
            }
            catch { UnityEngine.Object.Destroy(Mesh); throw; }
        }

        public void Dispose() => UnityEngine.Object.Destroy(Mesh);
    }
}
