using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
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
            Material[] packageMaterials = null;
            Result result = null;
            try
            {
                packageMaterials = materials == null ? BuildPackageMaterials(package.Materials, package.Mesh.Submeshes.Count) : null;
                result = Apply(package.Mesh, new RestTransform(1f, new Vec3()), package.Skeleton, package.Binding,
                    avatarRoot, boneMap, string.IsNullOrWhiteSpace(objectName) ? package.ObjectId : objectName,
                    materials ?? packageMaterials);
                var marker = result.GameObject.AddComponent<NyaForgeSkinnedClothingManaged>();
                marker.Bind(package.ObjectId, package.StateHash, result.Mesh, packageMaterials);
                return result;
            }
            catch
            {
                if (result != null)
                {
                    var marker = result.GameObject == null ? null : result.GameObject.GetComponent<NyaForgeSkinnedClothingManaged>();
                    if (marker != null) marker.ReleaseOwnedAssets();
                    else if (result.Mesh != null) Object.DestroyImmediate(result.Mesh);
                    if (result.GameObject != null) Object.DestroyImmediate(result.GameObject);
                }
                if (packageMaterials != null)
                    DestroyOwnedMaterials(packageMaterials);
                throw;
            }
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

            // The source mesh is authored in avatar-rest (avatar-root local)
            // space. Keep those coordinates local to the receiving root. The
            // generated object is parented to avatarRoot and the bindposes
            // already express each mapped bone relative to that same space;
            // applying avatarRoot.worldToLocalMatrix here would cancel the
            // receiver's translation/rotation/scale a second time.
            var positions = new Vector3[mesh.VertexCount];
            for (int i = 0; i < positions.Length; i++)
            {
                Vec3 point = meshTransform.ToAvatarPoint(mesh.Positions[i]);
                positions[i] = new Vector3(point.X, point.Y, point.Z);
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

        static Material[] BuildPackageMaterials(IReadOnlyList<GlbMaterialSource> sources, int submeshCount)
        {
            var shader = Shader.Find("Standard");
            if (shader == null) throw new AuthoringException("SKIN_SHADER_UNAVAILABLE", "Unity Standard shader is unavailable.");
            var result = new Material[submeshCount];
            try
            {
                for (int i = 0; i < result.Length; i++)
                {
                    var source = sources == null ? null : sources.FirstOrDefault(value => value != null && value.SubmeshIndex == i);
                    var parameters = source == null ? MaterialParameters.Default : source.Parameters;
                    var material = result[i] = new Material(shader) { name = string.IsNullOrWhiteSpace(source?.Name) ? "NyaForge Clothing Material " + i : source.Name };
                    var tint = parameters.BaseColor;
                    material.SetColor("_Color", new Color(tint.X, tint.Y, tint.Z, tint.W));
                    if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", parameters.Metallic);
                    if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 1f - parameters.Roughness);
                    if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 1f - parameters.Roughness);
                    if (source != null && source.HasEmbeddedBaseColorImage)
                    {
                        var bytes = source.CopyBaseColorImageBytes();
                        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, false) { name = material.name + " BaseColor" };
                        if (!texture.LoadImage(bytes, false))
                        {
                            Object.DestroyImmediate(texture);
                            throw new AuthoringException("IMAGE_DECODE_FAILED", "Clothing base-color image could not be decoded.");
                        }
                        texture.wrapMode = TextureWrapMode.Repeat;
                        texture.filterMode = FilterMode.Bilinear;
                        material.mainTexture = texture;
                    }
                    if (source?.NormalTexture != null && source.NormalTexture.HasImageBytes)
                    {
                        var texture = DecodeSemanticTexture(source.NormalTexture.CopyImageBytes(), source.NormalTexture.MimeType, source.NormalTexture.Sampler, material.name + " Normal", true);
                        if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", texture);
                        if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", source.NormalTexture.NormalScale);
                        material.EnableKeyword("_NORMALMAP");
                    }
                    if (source?.MetallicRoughnessTexture != null && source.MetallicRoughnessTexture.HasImageBytes)
                    {
                        // Standard shader's map is already converted to
                        // metallic (R) and smoothness (A). Bake the glTF
                        // scalar factors into that owned texture so the
                        // receiver does not depend on shader-specific map
                        // multiplication semantics.
                        var texture = DecodeMetallicRoughness(source.MetallicRoughnessTexture.CopyImageBytes(), source.MetallicRoughnessTexture.MimeType, source.MetallicRoughnessTexture.Sampler, material.name + " MetallicRoughness", parameters.Metallic, parameters.Roughness);
                        if (material.HasProperty("_MetallicGlossMap")) material.SetTexture("_MetallicGlossMap", texture);
                        // The factors are now represented in the texture.
                        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 1f);
                        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 1f);
                        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 1f);
                        material.EnableKeyword("_METALLICGLOSSMAP");
                    }
                    if (parameters.Emission.X > 0f || parameters.Emission.Y > 0f || parameters.Emission.Z > 0f)
                    {
                        material.EnableKeyword("_EMISSION");
                        if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", new Color(parameters.Emission.X, parameters.Emission.Y, parameters.Emission.Z, 1f));
                    }
                    ConfigureAlpha(material, parameters);
                }
                return result;
            }
            catch
            {
                DestroyOwnedMaterials(result);
                throw;
            }
        }

        static void DestroyOwnedMaterials(IEnumerable<Material> materials)
        {
            var textures = new HashSet<Texture>();
            foreach (var material in materials)
            {
                if (material == null) continue;
                AddOwnedTexture(textures, material.mainTexture);
                if (material.HasProperty("_BumpMap")) AddOwnedTexture(textures, material.GetTexture("_BumpMap"));
                if (material.HasProperty("_MetallicGlossMap")) AddOwnedTexture(textures, material.GetTexture("_MetallicGlossMap"));
                Object.DestroyImmediate(material);
            }
            foreach (var texture in textures) Object.DestroyImmediate(texture);
        }

        static void AddOwnedTexture(HashSet<Texture> textures, Texture texture)
        { if (texture != null && texture != Texture2D.whiteTexture) textures.Add(texture); }

        static Texture2D DecodeSemanticTexture(byte[] bytes, string mimeType, MaterialTextureSampler sampler, string name, bool linear)
        {
            if (bytes == null || bytes.Length == 0) throw new AuthoringException("INVALID_IMAGE", "Semantic texture image is empty.");
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, linear) { name = name };
            if (!texture.LoadImage(bytes, false)) { Object.DestroyImmediate(texture); throw new AuthoringException("IMAGE_DECODE_FAILED", "Semantic texture image could not be decoded."); }
            texture.wrapMode = ToWrapMode(sampler?.WrapS ?? MaterialTextureSampler.DefaultWrap);
            texture.filterMode = ToFilterMode(sampler?.MagFilter ?? MaterialTextureSampler.DefaultMagFilter);
            return texture;
        }

        static Texture2D DecodeMetallicRoughness(byte[] bytes, string mimeType, MaterialTextureSampler sampler, string name, float metallicFactor, float roughnessFactor)
        {
            var texture = DecodeSemanticTexture(bytes, mimeType, sampler, name, true);
            var pixels = texture.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                byte metallic = (byte)Mathf.Clamp(Mathf.RoundToInt(pixels[i].b * metallicFactor), 0, 255);
                byte smoothness = (byte)Mathf.Clamp(Mathf.RoundToInt(255f - pixels[i].g * roughnessFactor), 0, 255);
                pixels[i] = new Color32(metallic, 0, 0, smoothness);
            }
            texture.SetPixels32(pixels); texture.Apply(false, false); return texture;
        }

        static TextureWrapMode ToWrapMode(int value)
        { return value == 33071 ? TextureWrapMode.Clamp : value == 33648 ? TextureWrapMode.Mirror : TextureWrapMode.Repeat; }
        static FilterMode ToFilterMode(int value)
        { return value == 9728 || value == 9984 || value == 9986 ? FilterMode.Point : FilterMode.Bilinear; }

        static void ConfigureAlpha(Material material, MaterialParameters parameters)
        {
            if (parameters.AlphaMode == MaterialAlphaMode.Cutout)
            {
                material.SetFloat("_Mode", 1f);
                material.SetFloat("_Cutoff", parameters.AlphaCutoff);
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.EnableKeyword("_ALPHATEST_ON");
                material.renderQueue = (int)RenderQueue.AlphaTest;
            }
            else if (parameters.AlphaMode == MaterialAlphaMode.Blend)
            {
                material.SetFloat("_Mode", 3f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                material.SetFloat("_Mode", 0f);
                material.SetOverrideTag("RenderType", "Opaque");
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.renderQueue = (int)RenderQueue.Geometry;
            }
        }
    }
}
