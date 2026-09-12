using System;
using System.Linq;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Import
{
    /// <summary>Atomic geometry/morph conversion for a single affine coordinate change, not skin deformation.</summary>
    public sealed class SourceMeshTransform
    {
        public MeshData Mesh { get; }
        public MorphSet Morphs { get; }
        public string OriginalMeshHash { get; }

        SourceMeshTransform(MeshData mesh, MorphSet morphs, string originalMeshHash)
        { Mesh = mesh; Morphs = morphs; OriginalMeshHash = originalMeshHash; }

        public static SourceMeshTransform Apply(MeshData source, SourceAffine transform, MorphSet morphs = null)
        {
            Checks.Require(source != null && transform != null, "INVALID_IMPORT", "Mesh and affine coordinate transform are required.");
            morphs?.ValidateFor(source);
            Checks.Require(source.Tangents.Count == 0 || source.Normals.Count == source.VertexCount,
                "UNSUPPORTED_FORMAT", "Tangent coordinate conversion requires source normals.");
            var positions = source.Positions.Select(transform.TransformPoint).ToArray();
            var normals = source.Normals.Select(transform.TransformNormal).ToArray();
            var tangents = new Vec4[source.Tangents.Count];
            for (int i = 0; i < tangents.Length; i++) tangents[i] = transform.TransformTangent(source.Tangents[i], source.Normals[i]);
            var submeshes = source.Submeshes.ToArray();
            if (transform.IsMirrored)
                foreach (var indices in submeshes)
                    for (int i = 0; i < indices.Length; i += 3)
                    { int swap = indices[i + 1]; indices[i + 1] = indices[i + 2]; indices[i + 2] = swap; }
            var mesh = new MeshData(positions, normals, tangents, source.Uv0.ToArray(), submeshes);
            MorphSet converted = null;
            if (morphs != null)
                converted = MorphSet.Create(mesh, morphs.Targets.Select(target => MorphTarget.Create(mesh,
                    target.TargetId, target.Name,
                    target.Deltas.Select(delta => new MorphDelta(delta.Key, transform.TransformVector(delta.Value))),
                    target.NormalDeltas.Count == 0 ? null : target.NormalDeltas.Select(delta => new MorphDelta(delta.Key, transform.TransformNormalDelta(delta.Value))),
                    target.TangentDeltas.Count == 0 ? null : target.TangentDeltas.Select(delta => new MorphDelta(delta.Key, transform.TransformVector(delta.Value))))));
            return new SourceMeshTransform(mesh, converted, source.ContentHash);
        }
    }
}
