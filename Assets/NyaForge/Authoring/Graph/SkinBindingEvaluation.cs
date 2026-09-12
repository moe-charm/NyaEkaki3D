using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Graph
{
    /// <summary>Typed graph value for a mesh/skeleton skin binding.</summary>
    public sealed class GraphSkinBindingValue
    {
        public SkinBinding Binding { get; }
        internal GraphSkinBindingValue(SkinBinding binding)
        {
            Checks.Require(binding != null, "INVALID_SKIN", "Binding value is required.");
            Binding = binding;
        }
    }
}
