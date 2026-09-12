using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NyaForge.Authoring.Rig
{
    public sealed class MorphDelta
    {
        public int VertexIndex { get; }
        public Vec3 Delta { get; }
        public MorphDelta(int vertexIndex, Vec3 delta)
        {
            Checks.Require(vertexIndex >= 0, "INVALID_VERTEX", "Morph vertex index must be non-negative.");
            Checks.Finite(delta); VertexIndex = vertexIndex; Delta = delta;
        }
    }

    /// <summary>One sparse rest-space morph target pinned to a mesh topology.</summary>
    public sealed class MorphTarget
    {
        public string TargetId { get; }
        public string Name { get; }
        public string MeshTopologyHash { get; }
        public IReadOnlyDictionary<int, Vec3> Deltas { get; }
        public string ContentHash { get; }

        private MorphTarget(string targetId, string name, string meshTopologyHash, IReadOnlyDictionary<int, Vec3> deltas)
        {
            Checks.Id(targetId); Checks.Name(name); Checks.HashText(meshTopologyHash);
            TargetId = targetId; Name = name; MeshTopologyHash = meshTopologyHash; Deltas = deltas;
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(1); writer.Write(TargetId); writer.Write(Name); writer.Write(MeshTopologyHash); writer.Write(Deltas.Count);
                foreach (var pair in Deltas.OrderBy(item => item.Key))
                { writer.Write(pair.Key); Write(writer, pair.Value); }
                ContentHash = Checks.Hash(stream.ToArray());
            }
        }

        public static MorphTarget Create(MeshData mesh, string targetId, string name, IEnumerable<MorphDelta> deltas)
        {
            Checks.Require(mesh != null && deltas != null, "INVALID_MORPH", "Mesh and morph deltas are required.");
            Checks.Id(targetId); Checks.Name(name);
            var values = new Dictionary<int, Vec3>();
            foreach (var delta in deltas)
            {
                Checks.Require(delta != null && delta.VertexIndex < mesh.VertexCount, "INVALID_VERTEX", "Morph vertex is outside the mesh domain.");
                Checks.Require(values.TryAdd(delta.VertexIndex, delta.Delta), "DUPLICATE_MORPH", "A morph target cannot list one vertex twice.");
            }
            Checks.Require(values.Count <= mesh.VertexCount, "BUDGET_EXCEEDED", "Morph delta count exceeds the mesh domain.");
            return new MorphTarget(targetId, name, mesh.TopologyHash, new System.Collections.ObjectModel.ReadOnlyDictionary<int, Vec3>(values));
        }

        internal static MorphTarget FromSerialized(string targetId, string name, string meshTopologyHash, IEnumerable<MorphDelta> deltas, int vertexCount)
        {
            Checks.Require(deltas != null && vertexCount > 0, "INVALID_MORPH", "Serialized morph data is invalid.");
            Checks.Id(targetId); Checks.Name(name); Checks.HashText(meshTopologyHash);
            var values = new Dictionary<int, Vec3>();
            foreach (var delta in deltas)
            {
                Checks.Require(delta != null && delta.VertexIndex >= 0 && delta.VertexIndex < vertexCount, "INVALID_VERTEX", "Serialized morph vertex is outside the mesh domain.");
                Checks.Require(values.TryAdd(delta.VertexIndex, delta.Delta), "DUPLICATE_MORPH", "Serialized morph target contains a duplicate vertex.");
            }
            return new MorphTarget(targetId, name, meshTopologyHash, new System.Collections.ObjectModel.ReadOnlyDictionary<int, Vec3>(values));
        }

        internal static void Write(BinaryWriter writer, Vec3 value)
        { writer.Write(Checks.Canonical(value.X)); writer.Write(Checks.Canonical(value.Y)); writer.Write(Checks.Canonical(value.Z)); }
        internal static Vec3 Read(BinaryReader reader) { return new Vec3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()); }
    }
}
