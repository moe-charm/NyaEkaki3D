namespace NyaForge.Authoring.Import
{
    /// <summary>Source-local collider shape. Null vectors represent information absent from legacy data.</summary>
    public sealed class VrmSpringColliderShape
    {
        public string Kind { get; }
        public Vec3? Offset { get; }
        public float Radius { get; }
        public Vec3? Tail { get; }

        internal VrmSpringColliderShape(string kind, Vec3? offset, float radius, Vec3? tail)
        {
            Checks.Require(kind == "sphere" || kind == "capsule", "INVALID_VRM", "Unknown Spring collider shape.");
            Checks.Finite(radius); Checks.Require(radius >= 0, "INVALID_VRM", "Spring collider radius is negative.");
            if (offset.HasValue) Checks.Finite(offset.Value);
            if (tail.HasValue) Checks.Finite(tail.Value);
            Checks.Require(kind == "capsule" || !tail.HasValue, "INVALID_VRM", "Sphere collider cannot have a tail.");
            Kind = kind; Offset = offset; Radius = radius; Tail = tail;
        }
    }
}
