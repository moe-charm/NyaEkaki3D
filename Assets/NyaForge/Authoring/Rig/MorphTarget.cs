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
        public IReadOnlyDictionary<int, Vec3> NormalDeltas { get; }
        public IReadOnlyDictionary<int, Vec3> TangentDeltas { get; }
        public string ContentHash { get; }

        private MorphTarget(string targetId, string name, string meshTopologyHash, IReadOnlyDictionary<int, Vec3> deltas, IReadOnlyDictionary<int, Vec3> normalDeltas, IReadOnlyDictionary<int, Vec3> tangentDeltas)
        {
            Checks.Id(targetId); Checks.Name(name); Checks.HashText(meshTopologyHash);
            TargetId = targetId; Name = name; MeshTopologyHash = meshTopologyHash; Deltas = deltas; NormalDeltas = normalDeltas; TangentDeltas = tangentDeltas;
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(2); writer.Write(TargetId); writer.Write(Name); writer.Write(MeshTopologyHash); WriteValues(writer, Deltas); WriteValues(writer, NormalDeltas); WriteValues(writer, TangentDeltas);
                ContentHash = Checks.Hash(stream.ToArray());
            }
        }

        public static MorphTarget Create(MeshData mesh, string targetId, string name, IEnumerable<MorphDelta> deltas)
            => Create(mesh, targetId, name, deltas, null, null);

        public static MorphTarget Create(MeshData mesh, string targetId, string name, IEnumerable<MorphDelta> deltas, IEnumerable<MorphDelta> normalDeltas, IEnumerable<MorphDelta> tangentDeltas)
        {
            Checks.Require(mesh != null && deltas != null, "INVALID_MORPH", "Mesh and morph deltas are required.");
            Checks.Id(targetId); Checks.Name(name);
            var values = Values(mesh, deltas, "position");
            Checks.Require(normalDeltas == null || mesh.Normals.Count == mesh.VertexCount, "UNSUPPORTED_FORMAT", "Normal morph deltas require base normals.");
            Checks.Require(tangentDeltas == null || mesh.Tangents.Count == mesh.VertexCount, "UNSUPPORTED_FORMAT", "Tangent morph deltas require base tangents.");
            return new MorphTarget(targetId, name, mesh.TopologyHash, values, Values(mesh, normalDeltas, "normal"), Values(mesh, tangentDeltas, "tangent"));
        }

        internal static MorphTarget FromSerialized(string targetId, string name, string meshTopologyHash, IEnumerable<MorphDelta> deltas, int vertexCount)
            => FromSerialized(targetId, name, meshTopologyHash, deltas, null, null, vertexCount);

        /// <summary>Builds an imported morph directly from sparse values.</summary>
        /// <remarks>
        /// GLB import already has one value per source vertex. Going through
        /// millions of short-lived MorphDelta objects before constructing the
        /// dictionaries needlessly increases the peak memory of large avatar
        /// imports, so the importer uses this internal path.
        /// </remarks>
        internal static MorphTarget FromValues(MeshData mesh, string targetId, string name,
            IReadOnlyDictionary<int, Vec3> deltas, IReadOnlyDictionary<int, Vec3> normalDeltas = null,
            IReadOnlyDictionary<int, Vec3> tangentDeltas = null)
        {
            Checks.Require(mesh != null && deltas != null, "INVALID_MORPH", "Mesh and morph values are required.");
            Checks.Id(targetId); Checks.Name(name);
            var values = CopyValues(mesh, deltas, "position");
            Checks.Require(normalDeltas == null || mesh.Normals.Count == mesh.VertexCount, "UNSUPPORTED_FORMAT", "Normal morph deltas require base normals.");
            Checks.Require(tangentDeltas == null || mesh.Tangents.Count == mesh.VertexCount, "UNSUPPORTED_FORMAT", "Tangent morph deltas require base tangents.");
            return new MorphTarget(targetId, name, mesh.TopologyHash, values,
                CopyValues(mesh, normalDeltas, "normal"), CopyValues(mesh, tangentDeltas, "tangent"));
        }

        internal static MorphTarget FromSerialized(string targetId, string name, string meshTopologyHash, IEnumerable<MorphDelta> deltas, IEnumerable<MorphDelta> normalDeltas, IEnumerable<MorphDelta> tangentDeltas, int vertexCount)
        {
            Checks.Require(deltas != null && vertexCount > 0, "INVALID_MORPH", "Serialized morph data is invalid.");
            Checks.Id(targetId); Checks.Name(name); Checks.HashText(meshTopologyHash);
            return new MorphTarget(targetId, name, meshTopologyHash, SerializedValues(deltas, vertexCount), SerializedValues(normalDeltas, vertexCount), SerializedValues(tangentDeltas, vertexCount));
        }

        static IReadOnlyDictionary<int, Vec3> Values(MeshData mesh, IEnumerable<MorphDelta> deltas, string label)
        {
            if (deltas == null) return new System.Collections.ObjectModel.ReadOnlyDictionary<int, Vec3>(new Dictionary<int, Vec3>());
            var values = new Dictionary<int, Vec3>();
            foreach (var delta in deltas)
            {
                Checks.Require(delta != null && delta.VertexIndex < mesh.VertexCount, "INVALID_VERTEX", "Morph " + label + " vertex is outside the mesh domain.");
                Checks.Require(values.TryAdd(delta.VertexIndex, delta.Delta), "DUPLICATE_MORPH", "A morph target cannot list one vertex twice.");
            }
            Checks.Require(values.Count <= mesh.VertexCount, "BUDGET_EXCEEDED", "Morph delta count exceeds the mesh domain.");
            return new System.Collections.ObjectModel.ReadOnlyDictionary<int, Vec3>(values);
        }

        static IReadOnlyDictionary<int, Vec3> CopyValues(MeshData mesh, IReadOnlyDictionary<int, Vec3> values, string label)
        {
            if (values == null) return new System.Collections.ObjectModel.ReadOnlyDictionary<int, Vec3>(new Dictionary<int, Vec3>());
            Checks.Require(values.Count <= mesh.VertexCount, "BUDGET_EXCEEDED", "Morph " + label + " delta count exceeds the mesh domain.");
            var copy = new Dictionary<int, Vec3>(values.Count);
            foreach (var pair in values)
            {
                Checks.Require(pair.Key >= 0 && pair.Key < mesh.VertexCount, "INVALID_VERTEX", "Morph " + label + " vertex is outside the mesh domain.");
                Checks.Finite(pair.Value); Checks.Require(copy.TryAdd(pair.Key, pair.Value), "DUPLICATE_MORPH", "A morph target cannot list one vertex twice.");
            }
            return new System.Collections.ObjectModel.ReadOnlyDictionary<int, Vec3>(copy);
        }

        static IReadOnlyDictionary<int, Vec3> SerializedValues(IEnumerable<MorphDelta> deltas, int vertexCount)
        {
            if (deltas == null) return new System.Collections.ObjectModel.ReadOnlyDictionary<int, Vec3>(new Dictionary<int, Vec3>());
            var values = new Dictionary<int, Vec3>();
            foreach (var delta in deltas)
            {
                Checks.Require(delta != null && delta.VertexIndex >= 0 && delta.VertexIndex < vertexCount, "INVALID_VERTEX", "Serialized morph vertex is outside the mesh domain.");
                Checks.Require(values.TryAdd(delta.VertexIndex, delta.Delta), "DUPLICATE_MORPH", "Serialized morph target contains a duplicate vertex.");
            }
            return new System.Collections.ObjectModel.ReadOnlyDictionary<int, Vec3>(values);
        }

        static void WriteValues(BinaryWriter writer, IReadOnlyDictionary<int, Vec3> values)
        { writer.Write(values.Count); foreach (var pair in values.OrderBy(item => item.Key)) { writer.Write(pair.Key); Write(writer, pair.Value); } }

        internal static void Write(BinaryWriter writer, Vec3 value)
        { writer.Write(Checks.Canonical(value.X)); writer.Write(Checks.Canonical(value.Y)); writer.Write(Checks.Canonical(value.Z)); }
        internal static Vec3 Read(BinaryReader reader) { return new Vec3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()); }
    }
}
