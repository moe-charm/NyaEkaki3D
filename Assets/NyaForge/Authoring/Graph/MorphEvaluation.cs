using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Graph
{
    /// <summary>Typed graph value for a mesh-pinned morph set.</summary>
    public sealed class GraphMorphSetValue
    {
        public MorphSet Morphs { get; }
        internal GraphMorphSetValue(MorphSet morphs)
        {
            Checks.Require(morphs != null, "INVALID_MORPH", "Morph set value is required.");
            Morphs = morphs;
        }
    }
}
