using System;
using System.Collections.Generic;
using System.IO;

namespace NyaForge.Authoring
{
    internal static class MeshBinary
    {
        internal static byte[] Write(MeshData mesh)
        {
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(0x4d46594e); writer.Write(1); writer.Write(0x01020304);
                writer.Write(mesh.VertexCount); writer.Write(mesh.Normals.Count); writer.Write(mesh.Tangents.Count); writer.Write(mesh.Uv0.Count);
                var submeshes = mesh.Submeshes; writer.Write(submeshes.Count);
                foreach (var submesh in submeshes) writer.Write(submesh.Length);
                foreach (var v in mesh.Positions) Write(writer, v);
                foreach (var v in mesh.Normals) Write(writer, v);
                foreach (var v in mesh.Tangents) { Write(writer, new Vec3(v.X,v.Y,v.Z)); writer.Write(Checks.Canonical(v.W)); }
                foreach (var v in mesh.Uv0) { writer.Write(Checks.Canonical(v.X)); writer.Write(Checks.Canonical(v.Y)); }
                foreach (var submesh in submeshes) foreach (int index in submesh) writer.Write(index);
                Checks.Require(stream.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Mesh blob exceeds budget.");
                return stream.ToArray();
            }
        }
        internal static void Write(BinaryWriter writer, Vec3 value) { writer.Write(Checks.Canonical(value.X)); writer.Write(Checks.Canonical(value.Y)); writer.Write(Checks.Canonical(value.Z)); }
        internal static Vec3 ReadVector(BinaryReader reader) { return new Vec3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle()); }
        internal static MeshData Read(byte[] bytes)
        {
            Checks.Require(bytes.Length >= 36 && bytes.Length <= AuthoringLimits.MaxBlobBytes, "INVALID_BLOB", "Mesh blob length is invalid.");
            try
            {
                using (var stream = new MemoryStream(bytes, false)) using (var reader = new BinaryReader(stream))
                {
                    Checks.Require(reader.ReadInt32() == 0x4d46594e && reader.ReadInt32() == 1 && reader.ReadInt32() == 0x01020304, "UNSUPPORTED_FORMAT", "Unsupported mesh blob header or byte order.");
                    int vertexCount = reader.ReadInt32(), normalCount = reader.ReadInt32(), tangentCount = reader.ReadInt32(), uvCount = reader.ReadInt32(), submeshCount = reader.ReadInt32();
                    Checks.Require(vertexCount >= 3 && vertexCount <= AuthoringLimits.MaxVertices && submeshCount >= 1 && submeshCount <= AuthoringLimits.MaxSubmeshes, "BUDGET_EXCEEDED", "Mesh count exceeds budget.");
                    Checks.Require((normalCount == 0 || normalCount == vertexCount) && (tangentCount == 0 || tangentCount == vertexCount) && (uvCount == 0 || uvCount == vertexCount), "INVALID_BLOB", "Attribute count mismatch.");
                    var counts = new int[submeshCount]; long totalIndices = 0;
                    for (int i = 0; i < counts.Length; i++)
                    {
                        counts[i] = reader.ReadInt32(); totalIndices += counts[i];
                        Checks.Require(counts[i] > 0 && counts[i] % 3 == 0 && totalIndices <= AuthoringLimits.MaxIndices, "BUDGET_EXCEEDED", "Index count exceeds budget or is invalid.");
                    }
                    long required = 32L + submeshCount * 4L + vertexCount * 12L + normalCount * 12L + tangentCount * 16L + uvCount * 8L + totalIndices * 4L;
                    Checks.Require(required == bytes.Length, "INVALID_BLOB", "Declared blob length does not match actual length.");
                    // Allocate only after all counts and exact length have been checked.
                    var p = new Vec3[vertexCount]; var n = new Vec3[normalCount]; var t = new Vec4[tangentCount]; var uv = new Vec2[uvCount]; var submeshes = new int[submeshCount][];
                    for (int i = 0; i < p.Length; i++) p[i] = ReadVector(reader);
                    for (int i = 0; i < n.Length; i++) n[i] = ReadVector(reader);
                    for (int i = 0; i < t.Length; i++) t[i] = new Vec4(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());
                    for (int i = 0; i < uv.Length; i++) uv[i] = new Vec2(reader.ReadSingle(),reader.ReadSingle());
                    for (int i = 0; i < submeshes.Length; i++) { submeshes[i] = new int[counts[i]]; for (int j = 0; j < counts[i]; j++) submeshes[i][j] = reader.ReadInt32(); }
                    return new MeshData(p,n,t,uv,submeshes);
                }
            }
            catch (EndOfStreamException) { throw new AuthoringException("INVALID_BLOB", "Truncated mesh blob."); }
        }
    }

    internal static class DeltaBinary
    {
        internal static byte[] Write(IReadOnlyDictionary<int, Vec3> offsets)
        {
            var keys = new List<int>(offsets.Keys); keys.Sort();
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(0x4446594e); writer.Write(1); writer.Write(0x01020304); writer.Write(keys.Count);
                foreach (int key in keys) { writer.Write(key); MeshBinary.Write(writer, offsets[key]); }
                return stream.ToArray();
            }
        }
        internal static Dictionary<int, Vec3> Read(byte[] bytes, int vertexCount)
        {
            Checks.Require(bytes.Length >= 16 && bytes.Length <= AuthoringLimits.MaxBlobBytes, "INVALID_BLOB", "Delta blob length is invalid.");
            using (var stream = new MemoryStream(bytes, false)) using (var reader = new BinaryReader(stream))
            {
                Checks.Require(reader.ReadInt32() == 0x4446594e && reader.ReadInt32() == 1 && reader.ReadInt32() == 0x01020304, "UNSUPPORTED_FORMAT", "Unsupported delta blob header.");
                int count = reader.ReadInt32();
                Checks.Require(count >= 0 && count <= vertexCount && 16L + count * 16L == bytes.Length, "INVALID_BLOB", "Delta count or length is invalid.");
                var result = new Dictionary<int, Vec3>(); int previous = -1;
                for (int i = 0; i < count; i++)
                {
                    int index = reader.ReadInt32(); Vec3 delta = MeshBinary.ReadVector(reader); Checks.Finite(delta);
                    Checks.Require(index > previous && index < vertexCount, "INVALID_BLOB", "Delta indices must be unique, ordered and in range.");
                    result.Add(index, delta); previous = index;
                }
                return result;
            }
        }
    }
}
