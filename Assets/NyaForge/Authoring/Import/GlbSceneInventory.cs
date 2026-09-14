using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Import
{
    /// <summary>Stable, source-indexed description of one GLB mesh resource.</summary>
    public sealed class GlbMeshLocator
    {
        public int MeshIndex { get; }
        public string Name { get; }
        public int PrimitiveCount { get; }

        internal GlbMeshLocator(int meshIndex, string name, int primitiveCount)
        {
            MeshIndex = meshIndex; Name = name; PrimitiveCount = primitiveCount;
        }
    }

    /// <summary>One node reference to a mesh resource, including its optional skin and source-world frame.</summary>
    public sealed class GlbMeshInstance
    {
        public int NodeIndex { get; }
        public string Name { get; }
        public int MeshIndex { get; }
        public int? SkinIndex { get; }
        public SourceAffine WorldTransform { get; }

        internal GlbMeshInstance(int nodeIndex, string name, int meshIndex, int? skinIndex, SourceAffine worldTransform)
        {
            NodeIndex = nodeIndex; Name = name; MeshIndex = meshIndex; SkinIndex = skinIndex; WorldTransform = worldTransform;
        }
    }

    /// <summary>Source-indexed skin reference. Full inverse-bind and weights remain in the skin adapters.</summary>
    public sealed class GlbSkinLocator
    {
        public int SkinIndex { get; }
        public int? SkeletonRoot { get; }
        public IReadOnlyList<int> Joints { get; }

        internal GlbSkinLocator(int skinIndex, int? skeletonRoot, IEnumerable<int> joints)
        {
            SkinIndex = skinIndex; SkeletonRoot = skeletonRoot; Joints = Array.AsReadOnly(joints.ToArray());
        }
    }

    /// <summary>References to one mesh resource, kept separate from node instances.</summary>
    public sealed class GlbMeshResourceReferences
    {
        public int MeshIndex { get; }
        public IReadOnlyList<int> NodeIndices { get; }
        public IReadOnlyList<int> SkinIndices { get; }
        public bool IsShared { get { return NodeIndices.Count > 1; } }
        public bool HasMultipleSkins { get { return SkinIndices.Count > 1; } }

        internal GlbMeshResourceReferences(int meshIndex, IEnumerable<GlbMeshInstance> instances)
        {
            MeshIndex = meshIndex;
            var values = (instances ?? Enumerable.Empty<GlbMeshInstance>()).OrderBy(item => item.NodeIndex).ToArray();
            NodeIndices = Array.AsReadOnly(values.Select(item => item.NodeIndex).ToArray());
            SkinIndices = Array.AsReadOnly(values.Where(item => item.SkinIndex.HasValue).Select(item => item.SkinIndex.Value).Distinct().OrderBy(value => value).ToArray());
        }
    }

    /// <summary>Source resource aliases are reported without collapsing editable graph objects.</summary>
    public sealed class GlbSharedResourceReferences
    {
        public IReadOnlyList<GlbMeshResourceReferences> Meshes { get; }
        internal GlbSharedResourceReferences(IEnumerable<GlbMeshResourceReferences> meshes)
        {
            Meshes = new ReadOnlyCollection<GlbMeshResourceReferences>((meshes ?? Enumerable.Empty<GlbMeshResourceReferences>()).ToArray());
        }
        public GlbMeshResourceReferences Mesh(int meshIndex)
        {
            return Meshes.FirstOrDefault(item => item.MeshIndex == meshIndex);
        }
    }

    /// <summary>Immutable scene-level references used before a selected mesh is published to an authoring graph.</summary>
    public sealed class GlbSceneInventory
    {
        public const int MaxMeshes = 128, MaxInstances = 256, MaxSkins = 128;
        public string SourceHash { get; }
        public SourceNodeTransforms NodeTransforms { get; }
        public IReadOnlyList<GlbMeshLocator> Meshes { get; }
        public IReadOnlyList<GlbMeshInstance> Instances { get; }
        public IReadOnlyList<GlbSkinLocator> Skins { get; }
        /// <summary>Explicit source mesh aliases. Repeated instances remain independent graph objects after import.</summary>
        public GlbSharedResourceReferences SharedResources { get; }

        internal GlbSceneInventory(string sourceHash, SourceNodeTransforms nodeTransforms,
            IEnumerable<GlbMeshLocator> meshes, IEnumerable<GlbMeshInstance> instances, IEnumerable<GlbSkinLocator> skins)
        {
            Checks.HashText(sourceHash);
            SourceHash = sourceHash; NodeTransforms = nodeTransforms;
            Meshes = new ReadOnlyCollection<GlbMeshLocator>(meshes.ToArray());
            Instances = new ReadOnlyCollection<GlbMeshInstance>(instances.ToArray());
            Skins = new ReadOnlyCollection<GlbSkinLocator>(skins.ToArray());
            SharedResources = new GlbSharedResourceReferences(Meshes.Select(mesh =>
                new GlbMeshResourceReferences(mesh.MeshIndex, Instances.Where(instance => instance.MeshIndex == mesh.MeshIndex))));
        }
    }

    /// <summary>Reads scene references without decoding geometry or silently selecting the first mesh.</summary>
    public static class GlbSceneInventoryReader
    {
        public static GlbSceneInventory Read(byte[] bytes)
        {
            return Read(GlbDocumentReader.Read(bytes));
        }

        internal static GlbSceneInventory Read(GlbDocument document)
        {
            Checks.Require(document != null, "INVALID_IMPORT", "GLB document is required.");
            var root = document.Root;
            var meshTokens = Array(root, "meshes");
            Checks.Require(meshTokens.Count > 0 && meshTokens.Count <= GlbSceneInventory.MaxMeshes,
                "BUDGET_EXCEEDED", "GLB mesh inventory exceeds capacity.");
            var meshes = new List<GlbMeshLocator>(meshTokens.Count);
            for (int i = 0; i < meshTokens.Count; i++)
            {
                var mesh = meshTokens[i] as JObject;
                Checks.Require(mesh != null, "INVALID_IMPORT", "GLB mesh inventory entry is invalid.");
                var primitives = mesh["primitives"] as JArray;
                Checks.Require(primitives != null && primitives.Count > 0 && primitives.Count <= AuthoringLimits.MaxSubmeshes,
                    "BUDGET_EXCEEDED", "GLB primitive inventory exceeds capacity.");
                for (int p = 0; p < primitives.Count; p++)
                    Checks.Require(primitives[p] is JObject, "INVALID_IMPORT", "GLB primitive inventory entry is invalid.");
                meshes.Add(new GlbMeshLocator(i, Name(mesh, "mesh-" + i), primitives.Count));
            }

            var nodeToken = root["nodes"];
            JArray nodeTokens = null;
            if (nodeToken != null)
            {
                nodeTokens = nodeToken as JArray;
                Checks.Require(nodeTokens != null, "INVALID_IMPORT", "GLB nodes must be an array.");
            }
            SourceNodeTransforms nodeTransforms = null;
            if (nodeTokens != null && nodeTokens.Count > 0)
                nodeTransforms = GlbNodeTransformReader.Read(nodeTokens, document.SourceHash);

            var skinToken = root["skins"];
            JArray skinTokens = null;
            if (skinToken != null)
            {
                skinTokens = skinToken as JArray;
                Checks.Require(skinTokens != null, "INVALID_IMPORT", "GLB skins must be an array.");
            }
            if (skinTokens == null) skinTokens = new JArray();
            Checks.Require(skinTokens.Count <= GlbSceneInventory.MaxSkins, "BUDGET_EXCEEDED", "GLB skin inventory exceeds capacity.");
            if (skinTokens.Count > 0) Checks.Require(nodeTransforms != null, "INVALID_IMPORT", "GLB skins require source nodes.");
            var skins = new List<GlbSkinLocator>(skinTokens.Count);
            for (int i = 0; i < skinTokens.Count; i++)
            {
                var skin = skinTokens[i] as JObject;
                Checks.Require(skin != null, "INVALID_IMPORT", "GLB skin inventory entry is invalid.");
                var joints = skin["joints"] as JArray;
                Checks.Require(joints != null && joints.Count > 0 && joints.Count <= SkeletonDefinition.MaxBones,
                    "BUDGET_EXCEEDED", "GLB skin joint inventory exceeds capacity.");
                var values = new int[joints.Count]; var seen = new HashSet<int>();
                for (int j = 0; j < joints.Count; j++)
                {
                    values[j] = Index(joints[j], nodeTransforms.Local.Count - 1, "skin joint");
                    Checks.Require(seen.Add(values[j]), "INVALID_IMPORT", "GLB skin repeats a joint node.");
                }
                int? skeletonRoot = null;
                if (skin["skeleton"] != null) skeletonRoot = Index(skin["skeleton"], nodeTransforms.Local.Count - 1, "skin skeleton");
                skins.Add(new GlbSkinLocator(i, skeletonRoot, values));
            }

            var instances = new List<GlbMeshInstance>();
            if (nodeTokens != null)
            {
                for (int i = 0; i < nodeTokens.Count; i++)
                {
                    var node = nodeTokens[i] as JObject;
                    Checks.Require(node != null, "INVALID_IMPORT", "GLB node inventory entry is invalid.");
                    if (node["skin"] != null)
                        Checks.Require(node["mesh"] != null, "INVALID_IMPORT", "A GLB skin reference requires a mesh reference.");
                    if (node["mesh"] == null) continue;
                    int meshIndex = Index(node["mesh"], meshes.Count - 1, "node mesh");
                    int? skinIndex = node["skin"] == null ? null : Index(node["skin"], skins.Count - 1, "node skin");
                    Checks.Require(instances.Count < GlbSceneInventory.MaxInstances, "BUDGET_EXCEEDED", "GLB mesh instance inventory exceeds capacity.");
                    instances.Add(new GlbMeshInstance(i, Name(node, "node-" + i), meshIndex, skinIndex, nodeTransforms.World[i]));
                }
            }
            return new GlbSceneInventory(document.SourceHash, nodeTransforms, meshes, instances, skins);
        }

        static JArray Array(JObject owner, string property)
        {
            var value = owner[property] as JArray;
            Checks.Require(value != null, "INVALID_IMPORT", "GLB property is missing: " + property);
            return value;
        }

        static int Index(JToken token, int maximum, string label)
        {
            Checks.Require(token != null && token.Type == JTokenType.Integer, "INVALID_IMPORT", label + " index is invalid.");
            long value = (long)token;
            Checks.Require(value >= 0 && value <= maximum, "INVALID_IMPORT", label + " index is out of range.");
            return (int)value;
        }

        static string Name(JObject owner, string fallback)
        {
            if (owner["name"] == null) return fallback;
            Checks.Require(owner["name"].Type == JTokenType.String, "INVALID_IMPORT", "GLB display name is invalid.");
            string value = (string)owner["name"];
            Checks.Require(!string.IsNullOrWhiteSpace(value) && value.Length <= 128, "INVALID_IMPORT", "GLB display name is invalid.");
            return value;
        }
    }
}
