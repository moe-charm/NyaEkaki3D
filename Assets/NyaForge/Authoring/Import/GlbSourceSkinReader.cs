using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring.Import
{
    /// <summary>Decodes one source skin without publishing geometry or modifying an authoring document.</summary>
    public static class GlbSourceSkinReader
    {
        public static SourceSkin Read(byte[] bytes, int skinIndex = 0)
        {
            var document = GlbDocumentReader.Read(bytes);
            var nodes = GlbNodeTransformReader.Read(document.Root["nodes"] as JArray, document.SourceHash);
            return Read(document, nodes, skinIndex);
        }

        internal static SourceSkin Read(GlbDocument document, SourceNodeTransforms nodes, int skinIndex)
        {
            Checks.Require(nodes != null && nodes.SourceHash == document.SourceHash, "INVALID_IMPORT", "Source skin and node transforms must share an input hash.");
            var skins = document.Root["skins"] as JArray;
            Checks.Require(skins != null && skinIndex >= 0 && skinIndex < skins.Count, "INVALID_IMPORT", "Source skin index is missing.");
            var skin = skins[skinIndex] as JObject;
            Checks.Require(skin != null, "INVALID_IMPORT", "Source skin must be an object.");
            Checks.Require(skin["extensions"] == null, "UNSUPPORTED_FORMAT", "Skin extensions require a dedicated adapter.");
            var sourceJoints = skin["joints"] as JArray;
            Checks.Require(sourceJoints != null && sourceJoints.Count > 0 && sourceJoints.Count <= nodes.Local.Count,
                "INVALID_IMPORT", "Skin joints must fit the source node budget.");
            var joints = new int[sourceJoints.Count];
            for (int i = 0; i < joints.Length; i++)
                joints[i] = GlbMatrixAccessorReader.Integer(sourceJoints[i], 0, nodes.Local.Count - 1, "Joint node");
            SourceAffine[] matrices = null;
            if (skin["inverseBindMatrices"] != null)
                matrices = GlbMatrixAccessorReader.Read(document, GlbMatrixAccessorReader.Integer(skin["inverseBindMatrices"], 0, int.MaxValue, "Inverse-bind accessor"));
            int? root = skin["skeleton"] == null ? (int?)null : GlbMatrixAccessorReader.Integer(skin["skeleton"], 0, nodes.Local.Count - 1, "Skeleton root");
            return new SourceSkin(nodes, skinIndex, joints, matrices, root);
        }
    }
}
