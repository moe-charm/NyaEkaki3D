using System;
using UnityEngine;

namespace NyaForge.UnityBridge
{
    /// <summary>Ownership marker for generated package mesh/material/texture assets.</summary>
    [DisallowMultipleComponent]
    public sealed class NyaForgeSkinnedClothingManaged : MonoBehaviour
    {
        [SerializeField] string objectId = "";
        [SerializeField] string stateHash = "";
        [SerializeField] Mesh mesh;
        [SerializeField] Material[] materials = Array.Empty<Material>();

        public string ObjectId { get { return objectId; } }
        public string StateHash { get { return stateHash; } }
        public Mesh Mesh { get { return mesh; } }
        public Material[] Materials { get { return materials ?? Array.Empty<Material>(); } }

        public void Bind(string authoredObjectId, string authoredStateHash, Mesh ownedMesh, Material[] ownedMaterials)
        {
            objectId = authoredObjectId ?? "";
            stateHash = authoredStateHash ?? "";
            mesh = ownedMesh;
            materials = ownedMaterials == null ? Array.Empty<Material>() : (Material[])ownedMaterials.Clone();
        }

        /// <summary>Destroys only assets created by the package receiver; caller supplied materials are never marked.</summary>
        public void ReleaseOwnedAssets()
        {
            if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
            foreach (var material in Materials)
            {
                if (material == null) continue;
                var texture = material.mainTexture;
                if (texture != null && texture != Texture2D.whiteTexture) UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(material);
            }
            mesh = null;
            materials = Array.Empty<Material>();
        }
    }
}
