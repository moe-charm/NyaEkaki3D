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
        public Vector3[] WorldPoints => root == null ? Array.Empty<Vector3>() : Points.Select(point => root.transform.TransformPoint(point)).ToArray();
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
                if (attachmentPose.HasValue)
                {
                    root.transform.localPosition = Vector3.zero;
                    root.transform.localRotation = Quaternion.identity;
                }
                root.AddComponent<MeshFilter>().sharedMesh = Mesh;
                root.AddComponent<MeshRenderer>().sharedMaterials = Enumerable.Repeat(material, value.Mesh.Submeshes.Count).ToArray();
                Points = value.Mesh.Positions.Select(p => OwnedMeshProjection.ToUnity(value.Transform.ToAvatarPoint(p))).ToArray();
            }
            catch { UnityEngine.Object.Destroy(Mesh); throw; }
        }

        public void Dispose() => UnityEngine.Object.Destroy(Mesh);
    }
}
