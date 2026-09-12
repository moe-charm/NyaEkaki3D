using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace NyaForge.Authoring.Rig
{
    /// <summary>Immutable sparse morph collection for one mesh topology.</summary>
    public sealed class MorphSet
    {
        public const int MaxTargets = 256;
        public string MeshTopologyHash { get; }
        public IReadOnlyList<MorphTarget> Targets { get; }
        public IReadOnlyDictionary<string, MorphTarget> ById { get; }
        public string ContentHash { get; }

        private MorphSet(string meshTopologyHash, IReadOnlyList<MorphTarget> targets)
        {
            Checks.HashText(meshTopologyHash); MeshTopologyHash = meshTopologyHash; Targets = targets;
            ById = new ReadOnlyDictionary<string, MorphTarget>(targets.ToDictionary(item => item.TargetId, StringComparer.Ordinal));
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(1); writer.Write(MeshTopologyHash); writer.Write(Targets.Count);
                foreach (var target in Targets.OrderBy(item => item.TargetId, StringComparer.Ordinal)) writer.Write(target.ContentHash);
                ContentHash = Checks.Hash(stream.ToArray());
            }
        }

        public static MorphSet Create(MeshData mesh, IEnumerable<MorphTarget> targets)
        {
            Checks.Require(mesh != null && targets != null, "INVALID_MORPH", "Mesh and morph targets are required.");
            var values = targets.ToArray(); Validate(mesh, values);
            return new MorphSet(mesh.TopologyHash, Array.AsReadOnly(values.OrderBy(item => item.TargetId, StringComparer.Ordinal).ToArray()));
        }

        internal static MorphSet FromSerialized(MeshData mesh, string meshTopologyHash, IEnumerable<MorphTarget> targets)
        {
            Checks.Require(mesh != null && targets != null, "INVALID_MORPH", "Mesh and morph targets are required.");
            Checks.Require(mesh.TopologyHash == meshTopologyHash, "MORPH_TOPOLOGY_CHANGED", "Morph set belongs to another mesh topology.");
            var values = targets.ToArray(); Validate(mesh, values);
            return new MorphSet(meshTopologyHash, Array.AsReadOnly(values.OrderBy(item => item.TargetId, StringComparer.Ordinal).ToArray()));
        }

        internal static MorphSet FromSerializedUnbound(string meshTopologyHash, IEnumerable<MorphTarget> targets)
        {
            Checks.Require(targets != null, "INVALID_MORPH", "Morph targets are required.");
            Checks.HashText(meshTopologyHash);
            var values = targets.ToArray(); ValidateIdentity(meshTopologyHash, values);
            return new MorphSet(meshTopologyHash, Array.AsReadOnly(values.OrderBy(item => item.TargetId, StringComparer.Ordinal).ToArray()));
        }

        public MorphSet ValidateFor(MeshData mesh)
        {
            Checks.Require(mesh != null, "INVALID_MORPH", "Mesh is required.");
            Checks.Require(MeshTopologyHash == mesh.TopologyHash, "MORPH_TOPOLOGY_CHANGED", "Morph set belongs to another mesh topology.");
            return Create(mesh, Targets);
        }

        static void Validate(MeshData mesh, MorphTarget[] values)
        {
            ValidateIdentity(mesh.TopologyHash, values);
            foreach (var target in values)
            {
                foreach (var vertex in target.Deltas.Keys)
                    Checks.Require(vertex < mesh.VertexCount, "INVALID_VERTEX", "Morph vertex is outside the mesh domain.");
                foreach (var vertex in target.NormalDeltas.Keys)
                    Checks.Require(mesh.Normals.Count == mesh.VertexCount && vertex < mesh.VertexCount, "UNSUPPORTED_FORMAT", "Normal morph deltas require base normals.");
                foreach (var vertex in target.TangentDeltas.Keys)
                    Checks.Require(mesh.Tangents.Count == mesh.VertexCount && vertex < mesh.VertexCount, "UNSUPPORTED_FORMAT", "Tangent morph deltas require base tangents.");
            }
        }

        static void ValidateIdentity(string meshTopologyHash, MorphTarget[] values)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal); var names = new HashSet<string>(StringComparer.Ordinal);
            Checks.Require(values.Length > 0 && values.Length <= MaxTargets, "BUDGET_EXCEEDED", "Morph target count exceeds capacity.");
            foreach (var target in values)
            {
                Checks.Require(target != null && ids.Add(target.TargetId), "DUPLICATE_MORPH", "Morph target identity must be unique.");
                Checks.Require(names.Add(target.Name), "DUPLICATE_MORPH", "Morph target names must be unique.");
                Checks.Require(target.MeshTopologyHash == meshTopologyHash, "MORPH_TOPOLOGY_CHANGED", "Morph target belongs to another mesh topology.");
            }
        }
    }
}
