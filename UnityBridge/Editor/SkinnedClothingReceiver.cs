using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace NyaForge.UnityBridge.Editor
{
    /// <summary>
    /// Creates one explicitly bound clothing object in a receiving Unity scene.
    /// The receiver does not guess bones by name: callers provide every stable
    /// NyaForge BoneId and the corresponding avatar Transform.
    /// </summary>
    public static class SkinnedClothingReceiver
    {
        public sealed class Result
        {
            public GameObject GameObject { get; internal set; }
            public SkinnedMeshRenderer Renderer { get; internal set; }
            public Mesh Mesh { get; internal set; }
            public int BoneCount { get; internal set; }
        }

        public static Result ApplyGlb(
            byte[] glb,
            Transform avatarRoot,
            IReadOnlyDictionary<string, Transform> boneMap,
            string objectName = "NyaForge Clothing",
            Material[] materials = null)
        {
            if (glb == null) throw new ArgumentNullException("glb");
            var imported = GlbSkinImporter.Read(glb);
            return Apply(imported.Mesh, new RestTransform(1f, new Vec3()), imported.Skeleton,
                imported.Binding, avatarRoot, boneMap, objectName, materials);
        }

        /// <summary>Reads and validates a clothing-only sidecar before creating the scene object.</summary>
        public static Result ApplyPackage(
            string manifestPath,
            Transform avatarRoot,
            IReadOnlyDictionary<string, Transform> boneMap,
            string objectName = null,
            Material[] materials = null)
        {
            if (string.IsNullOrWhiteSpace(manifestPath)) throw new ArgumentException("Package manifest is required.", "manifestPath");
            var package = SkinnedClothingPackage.Read(manifestPath);
            return Apply(package.Mesh, new RestTransform(1f, new Vec3()), package.Skeleton, package.Binding,
                avatarRoot, boneMap, string.IsNullOrWhiteSpace(objectName) ? package.ObjectId : objectName, materials);
        }

        public static Result Apply(
            MeshData mesh,
            RestTransform meshTransform,
            SkeletonDefinition skeleton,
            SkinBinding binding,
            Transform avatarRoot,
            IReadOnlyDictionary<string, Transform> boneMap,
            string objectName = "NyaForge Clothing",
            Material[] materials = null)
        {
            ValidateInputs(mesh, meshTransform, skeleton, binding, avatarRoot, boneMap, objectName, materials);
            var boneTransforms = new Transform[skeleton.Bones.Count];
            var boneIndex = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < skeleton.Bones.Count; i++)
            {
                var bone = skeleton.Bones[i];
                boneTransforms[i] = boneMap[bone.BoneId];
                boneIndex.Add(bone.BoneId, i);
            }

            // The source mesh is authored in avatar-rest space. Convert once to
            // the receiving avatar root's local space before assigning bindposes.
            var rootWorldToLocal = avatarRoot.worldToLocalMatrix;
            var positions = new Vector3[mesh.VertexCount];
            for (int i = 0; i < positions.Length; i++)
            {
                Vec3 point = meshTransform.ToAvatarPoint(mesh.Positions[i]);
                positions[i] = rootWorldToLocal.MultiplyPoint3x4(new Vector3(point.X, point.Y, point.Z));
            }
            var resultMesh = new Mesh
            {
                name = objectName + " Mesh",
                indexFormat = mesh.VertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
            };
            GameObject go = null;
            Material[] ownedMaterials = null;
            try
            {
                resultMesh.vertices = positions;
                CopyAttributes(resultMesh, mesh);
                resultMesh.subMeshCount = mesh.Submeshes.Count;
                for (int i = 0; i < mesh.Submeshes.Count; i++)
                    resultMesh.SetTriangles(mesh.Submeshes[i], i, false);
                resultMesh.bindposes = boneTransforms.Select(b => b.worldToLocalMatrix * avatarRoot.localToWorldMatrix).ToArray();
                resultMesh.boneWeights = BuildBoneWeights(mesh.VertexCount, binding, boneIndex);
                resultMesh.RecalculateBounds();

                ownedMaterials = materials == null ? BuildMaterials(null, mesh.Submeshes.Count) : null;
                go = new GameObject(objectName);
                go.transform.SetParent(avatarRoot, false);
                var renderer = go.AddComponent<SkinnedMeshRenderer>();
                renderer.sharedMesh = resultMesh;
                renderer.bones = boneTransforms;
                renderer.rootBone = FindRootBone(skeleton, boneTransforms);
                renderer.sharedMaterials = materials == null ? ownedMaterials : (Material[])materials.Clone();
                Undo.RegisterCreatedObjectUndo(go, "Apply NyaForge skinned clothing");
                return new Result { GameObject = go, Renderer = renderer, Mesh = resultMesh, BoneCount = boneTransforms.Length };
            }
            catch
            {
                if (go != null) Object.DestroyImmediate(go);
                if (ownedMaterials != null)
                    foreach (var material in ownedMaterials) if (material != null) Object.DestroyImmediate(material);
                Object.DestroyImmediate(resultMesh);
                throw;
            }
        }

        static void ValidateInputs(MeshData mesh, RestTransform meshTransform, SkeletonDefinition skeleton,
            SkinBinding binding, Transform avatarRoot, IReadOnlyDictionary<string, Transform> boneMap,
            string objectName, Material[] materials)
        {
            if (mesh == null || skeleton == null || binding == null) throw new ArgumentNullException("mesh/skeleton/binding");
            if (avatarRoot == null) throw new ArgumentNullException("avatarRoot");
            if (boneMap == null) throw new ArgumentNullException("boneMap");
            if (string.IsNullOrWhiteSpace(objectName) || objectName.IndexOf('\0') >= 0) throw new ArgumentException("Object name is invalid.", "objectName");
            // Reconstructing the value runs the public transform invariants;
            // Validate() itself is intentionally internal to the Core assembly.
            new RestTransform(meshTransform.Scale, meshTransform.Translation);
            binding.ValidateFor(mesh, skeleton);
            if (materials != null && materials.Length != mesh.Submeshes.Count)
                throw new AuthoringException("MATERIAL_SLOT_MISMATCH", "Material count must match clothing submeshes.");
            if (materials != null && materials.Any(material => material == null))
                throw new AuthoringException("MATERIAL_SLOT_MISMATCH", "Clothing material slots cannot be null.");
            foreach (var bone in skeleton.Bones)
            {
                Transform target;
                if (!boneMap.TryGetValue(bone.BoneId, out target) || target == null)
                    throw new AuthoringException("BONE_MAP_MISSING", "Avatar bone map is missing BoneId " + bone.BoneId + ".");
                if (target != avatarRoot && !target.IsChildOf(avatarRoot))
                    throw new AuthoringException("BONE_MAP_OUTSIDE_ROOT", "Mapped bone is outside the avatar root.");
            }
            foreach (var pair in binding.Weights)
                if (pair.Value.Count > 4)
                    throw new AuthoringException("SKIN_INFLUENCES_UNSUPPORTED", "Unity v1 receiver supports at most four influences per vertex.");
        }

        static void CopyAttributes(Mesh target, MeshData source)
        {
            if (source.Normals.Count == source.VertexCount)
                target.normals = source.Normals.Select(v => new Vector3(v.X, v.Y, v.Z)).ToArray();
            if (source.Tangents.Count == source.VertexCount)
                target.tangents = source.Tangents.Select(v => new Vector4(v.X, v.Y, v.Z, v.W)).ToArray();
            if (source.Uv0.Count == source.VertexCount)
                target.uv = source.Uv0.Select(v => new Vector2(v.X, v.Y)).ToArray();
        }

        static BoneWeight[] BuildBoneWeights(int vertexCount, SkinBinding binding, IReadOnlyDictionary<string, int> boneIndex)
        {
            var result = new BoneWeight[vertexCount];
            for (int vertex = 0; vertex < vertexCount; vertex++)
            {
                IReadOnlyList<VertexWeight> weights;
                if (!binding.Weights.TryGetValue(vertex, out weights) || weights.Count == 0)
                    throw new AuthoringException("UNWEIGHTED_VERTEX", "Every clothing vertex needs a weight.");
                var indices = weights.Select(w => boneIndex[w.BoneId]).ToArray();
                var values = weights.Select(w => w.Weight).ToArray();
                result[vertex].boneIndex0 = indices[0]; result[vertex].weight0 = values[0];
                if (indices.Length > 1) { result[vertex].boneIndex1 = indices[1]; result[vertex].weight1 = values[1]; }
                if (indices.Length > 2) { result[vertex].boneIndex2 = indices[2]; result[vertex].weight2 = values[2]; }
                if (indices.Length > 3) { result[vertex].boneIndex3 = indices[3]; result[vertex].weight3 = values[3]; }
            }
            return result;
        }

        static Transform FindRootBone(SkeletonDefinition skeleton, Transform[] transforms)
        {
            for (int i = 0; i < skeleton.Bones.Count; i++)
                if (skeleton.Bones[i].ParentBoneId == "") return transforms[i];
            throw new AuthoringException("BONE_ROOT_MISSING", "Skeleton has no root bone.");
        }

        static Material[] BuildMaterials(Material[] supplied, int count)
        {
            if (supplied != null) return (Material[])supplied.Clone();
            var shader = Shader.Find("Standard");
            if (shader == null) throw new AuthoringException("SKIN_SHADER_UNAVAILABLE", "Unity Standard shader is unavailable.");
            var result = new Material[count];
            for (int i = 0; i < result.Length; i++) result[i] = new Material(shader) { name = "NyaForge Clothing Material " + i };
            return result;
        }
    }
}
