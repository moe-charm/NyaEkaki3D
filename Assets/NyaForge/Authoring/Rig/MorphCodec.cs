using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NyaForge.Authoring.Rig
{
    /// <summary>Strict NYRM codec for mesh-pinned position/normal/tangent morph targets.</summary>
    public static class MorphCodec
    {
        const int Magic = 0x4d524e59; // NYRM
        const int Version = 2;
        static readonly Encoding Utf8 = new UTF8Encoding(false, true);

        public static byte[] Write(MorphSet morphs)
        {
            Checks.Require(morphs != null, "INVALID_MORPH", "Morph set is required.");
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream, Utf8))
            {
                writer.Write(Magic); writer.Write(Version); Text(writer, morphs.MeshTopologyHash); writer.Write(morphs.Targets.Count);
                foreach (var target in morphs.Targets.OrderBy(item => item.TargetId, StringComparer.Ordinal))
                {
                    Text(writer, target.TargetId); Text(writer, target.Name); Text(writer, target.MeshTopologyHash); WriteValues(writer, target.Deltas); WriteValues(writer, target.NormalDeltas); WriteValues(writer, target.TangentDeltas);
                }
                Checks.Require(stream.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Morph asset exceeds capacity."); return stream.ToArray();
            }
        }

        public static MorphSet Read(byte[] bytes, MeshData mesh)
        {
            Checks.Require(mesh != null, "INVALID_MORPH", "Mesh is required.");
            return ReadCore(bytes, mesh, true);
        }

        internal static MorphSet ReadUnbound(byte[] bytes)
        {
            return ReadCore(bytes, null, false);
        }

        static MorphSet ReadCore(byte[] bytes, MeshData mesh, bool bound)
        {
            Checks.Require(bytes != null && bytes.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Morph asset exceeds capacity.");
            try
            {
                using (var stream = new MemoryStream(bytes, false)) using (var reader = new BinaryReader(stream, Utf8))
                {
                    Checks.Require(reader.ReadInt32() == Magic, "INVALID_BLOB", "Not a morph asset."); int version = reader.ReadInt32(); Checks.Require(version == 1 || version == Version, "UNSUPPORTED_FORMAT", "Unsupported morph wire version.");
                    string hash = Text(reader, 64); int count = Count(reader, MorphSet.MaxTargets); var targets = new List<MorphTarget>(count);
                    for (int i = 0; i < count; i++)
                    {
                        string id = Text(reader, 64), name = Text(reader, 128), targetHash = Text(reader, 64);
                        var values = ReadValues(reader); var normals = version == 1 ? null : ReadValues(reader); var tangents = version == 1 ? null : ReadValues(reader);
                        targets.Add(MorphTarget.FromSerialized(id, name, targetHash, values, normals, tangents, AuthoringLimits.MaxVertices));
                    }
                    Checks.Require(stream.Position == stream.Length, "INVALID_BLOB", "Trailing morph asset bytes.");
                    return bound ? MorphSet.FromSerialized(mesh, hash, targets) : MorphSet.FromSerializedUnbound(hash, targets);
                }
            }
            catch (EndOfStreamException e) { throw new AuthoringException("INVALID_BLOB", e.Message); }
            catch (DecoderFallbackException e) { throw new AuthoringException("INVALID_BLOB", e.Message); }
        }

        static int Count(BinaryReader reader, int max) { return Count(reader, max, false); }
        static int Count(BinaryReader reader, int max, bool allowZero) { int value = reader.ReadInt32(); Checks.Require((allowZero ? value >= 0 : value > 0) && value <= max, "BUDGET_EXCEEDED", "Morph count exceeds capacity."); return value; }
        static void WriteValues(BinaryWriter writer, IReadOnlyDictionary<int, Vec3> values)
        { writer.Write(values.Count); foreach (var pair in values.OrderBy(item => item.Key)) { writer.Write(pair.Key); MorphTarget.Write(writer, pair.Value); } }
        static List<MorphDelta> ReadValues(BinaryReader reader)
        { int count = Count(reader, AuthoringLimits.MaxVertices, true); var result = new List<MorphDelta>(count); for (int i = 0; i < count; i++) result.Add(new MorphDelta(reader.ReadInt32(), MorphTarget.Read(reader))); return result; }
        static void Text(BinaryWriter writer, string value) { var bytes = Utf8.GetBytes(value); writer.Write(bytes.Length); writer.Write(bytes); }
        static string Text(BinaryReader reader, int max) { int count = reader.ReadInt32(); Checks.Require(count > 0 && count <= max && count <= reader.BaseStream.Length - reader.BaseStream.Position, "INVALID_BLOB", "Invalid morph text length."); return Utf8.GetString(reader.ReadBytes(count)); }
    }
}
