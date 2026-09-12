using System.Linq;

namespace NyaForge.Authoring.Import
{
    /// <summary>Cross-checks complete source data against the persisted authored mapping.</summary>
    internal static class ImportedRigSourceValidation
    {
        internal static void Validate(ImportedRigSession session, SourceSkin skin, SourceSkinBinding binding = null)
        {
            Checks.Require(binding == null || skin != null, "INVALID_IMPORT", "Complete source weights require complete source skin.");
            if (skin == null) return;
            Checks.Require(session.SourceHash == skin.Nodes.SourceHash, "IMPORT_SOURCE_CHANGED", "Complete source skin belongs to another input.");
            Checks.Require(skin.Joints.Count == session.NodeToBone.Count && skin.Joints.All(session.NodeToBone.ContainsKey),
                "INVALID_IMPORT", "Complete source joint slots differ from the imported bone mapping.");
            var expected = session.Hierarchy; var actual = skin.Nodes.Hierarchy;
            Checks.Require(expected != null && expected.Parents.SequenceEqual(actual.Parents), "INVALID_IMPORT", "Complete source hierarchy differs from the session.");
            Checks.Require(expected.Origins.SequenceEqual(actual.Origins), "INVALID_IMPORT", "Complete source origins differ from the session.");
            for (int i = 0; i < expected.Children.Count; i++)
                Checks.Require(expected.Children[i].SequenceEqual(actual.Children[i]), "INVALID_IMPORT", "Complete source child order differs from the session.");
            if (binding != null)
            {
                Checks.Require(binding.SourceHash == session.SourceHash && binding.SourceHash == skin.Nodes.SourceHash, "IMPORT_SOURCE_CHANGED", "Complete source weights belong to another input.");
                Checks.Require(binding.Weights.Values.SelectMany(values => values).All(weight => weight.JointSlot >= 0 && weight.JointSlot < skin.Joints.Count), "INVALID_IMPORT", "Complete source weight slot is outside the source skin.");
            }
        }
    }
}
