using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NyaForge.Authoring.Simulation
{
    /// <summary>Read result that keeps an unknown wire version as opaque bytes instead of resetting it.</summary>
    public sealed class SecondaryMotionDocument
    {
        public int WireVersion { get; }
        public bool IsSupported { get; }
        public SecondaryMotionAsset Asset { get; }
        public IReadOnlyList<byte> RawBytes { get; }
        public string RawHash { get; }
        public string Diagnostic { get; }

        internal SecondaryMotionDocument(int wireVersion, bool supported, SecondaryMotionAsset asset, byte[] bytes, string diagnostic)
        {
            WireVersion = wireVersion; IsSupported = supported; Asset = asset; RawBytes = Array.AsReadOnly((byte[])bytes.Clone());
            RawHash = Checks.Hash(bytes); Diagnostic = diagnostic ?? "";
        }

        public SecondaryMotionAsset RequireSupported()
        {
            Checks.Require(IsSupported && Asset != null, "UNSUPPORTED_FORMAT", Diagnostic == "" ? "Secondary motion profile version is unsupported." : Diagnostic);
            return Asset;
        }
    }

    /// <summary>Strict bounded wire codec for the simulator-neutral secondary-motion asset.</summary>
    public static class SecondaryMotionCodec
    {
        const int Magic = 0x4d53594e; // NYSM, little-endian
        const int Version = 1;
        const int MaxTextBytes = 512;
        static readonly Encoding Utf8 = new UTF8Encoding(false, true);

        public static byte[] Write(SecondaryMotionAsset asset)
        {
            Checks.Require(asset != null, "INVALID_SIMULATION", "Secondary motion asset is required.");
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream, Utf8, true))
            {
                writer.Write(Magic); writer.Write(Version);
                WriteText(writer, asset.Profile.AdapterId); WriteText(writer, asset.Profile.SimulatorId); writer.Write(asset.Profile.SchemaVersion);
                WriteText(writer, asset.Profile.AdapterVersion); WriteText(writer, asset.Profile.PackageVersion); writer.Write((int)asset.Profile.OutputKind);
                writer.Write(asset.Profile.AdapterPayload.Count); foreach (byte value in asset.Profile.AdapterPayload) writer.Write(value);
                WriteText(writer, asset.SkeletonHash); WriteText(writer, asset.MeshTopologyHash);
                writer.Write(asset.Chains.Count);
                foreach (var chain in asset.Chains)
                {
                    WriteText(writer, chain.Name); writer.Write(chain.BoneIds.Count); foreach (string id in chain.BoneIds) WriteText(writer, id);
                    writer.Write(chain.ColliderGroupIndices.Count); foreach (int index in chain.ColliderGroupIndices) writer.Write(index);
                }
                writer.Write(asset.ColliderGroups.Count);
                foreach (var group in asset.ColliderGroups)
                {
                    WriteText(writer, group.Name); writer.Write(group.Colliders.Count);
                    foreach (var collider in group.Colliders)
                    {
                        WriteText(writer, collider.BoneId); Vector(writer, collider.Center); writer.Write(Checks.Canonical(collider.Radius));
                        writer.Write(collider.Tail.HasValue); if (collider.Tail.HasValue) Vector(writer, collider.Tail.Value);
                    }
                }
                writer.Write(asset.FixedVertexIndices.Count); foreach (int index in asset.FixedVertexIndices) writer.Write(index);
                writer.Flush(); Checks.Require(stream.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Secondary motion asset exceeds capacity.");
                return stream.ToArray();
            }
        }

        public static SecondaryMotionAsset Read(byte[] bytes)
        {
            return ReadDocument(bytes).RequireSupported();
        }

        public static SecondaryMotionDocument ReadDocument(byte[] bytes)
        {
            Checks.Require(bytes != null && bytes.Length >= 8 && bytes.Length <= AuthoringLimits.MaxBlobBytes,
                "BUDGET_EXCEEDED", "Secondary motion asset exceeds capacity.");
            try
            {
                using (var stream = new MemoryStream(bytes, false)) using (var reader = new BinaryReader(stream, Utf8))
                {
                    Checks.Require(reader.ReadInt32() == Magic, "INVALID_BLOB", "Not a secondary motion asset.");
                    int wireVersion = reader.ReadInt32();
                    if (wireVersion != Version)
                        return new SecondaryMotionDocument(wireVersion, false, null, bytes, "Secondary motion asset version " + wireVersion + " is unsupported; raw bytes were retained.");
                    var asset = ReadAsset(reader, stream);
                    Checks.Require(stream.Position == stream.Length, "INVALID_BLOB", "Trailing secondary motion asset bytes.");
                    return new SecondaryMotionDocument(wireVersion, true, asset, bytes, "");
                }
            }
            catch (EndOfStreamException error) { throw new AuthoringException("INVALID_BLOB", error.Message); }
            catch (DecoderFallbackException error) { throw new AuthoringException("INVALID_BLOB", error.Message); }
        }

        static SecondaryMotionAsset ReadAsset(BinaryReader reader, Stream stream)
        {
            string adapterId = ReadText(reader, stream, 128), simulatorId = ReadText(reader, stream, 128); int schemaVersion = reader.ReadInt32();
            string adapterVersion = ReadText(reader, stream, 128), packageVersion = ReadText(reader, stream, 128);
            var kind = (SecondaryMotionOutputKind)reader.ReadInt32(); SecondaryMotionCapabilities.ValidateKind(kind);
            int payloadLength = Count(reader, AuthoringLimits.MaxBlobBytes, 0, true); Checks.Require(payloadLength <= stream.Length - stream.Position, "INVALID_BLOB", "Secondary motion adapter payload is truncated.");
            var payload = reader.ReadBytes(payloadLength); Checks.Require(payload.Length == payloadLength, "INVALID_BLOB", "Secondary motion adapter payload is truncated.");
            string skeletonHash = ReadOptionalHash(reader, stream), meshHash = ReadOptionalHash(reader, stream);
            int chainCount = Count(reader, SecondaryMotionAsset.MaxChains, 4, true); var chains = new List<SecondaryMotionChain>();
            for (int i = 0; i < chainCount; i++)
            {
                string name = ReadText(reader, stream, 128); int boneCount = Count(reader, SecondaryMotionChain.MaxBones, 4, false);
                var bones = new List<string>(); for (int j = 0; j < boneCount; j++) bones.Add(ReadText(reader, stream, 64));
                int groupCount = Count(reader, SecondaryMotionAsset.MaxColliderGroups, 4, true); var groups = new List<int>();
                for (int j = 0; j < groupCount; j++) groups.Add(reader.ReadInt32());
                chains.Add(new SecondaryMotionChain(name, bones, groups));
            }
            int colliderCount = Count(reader, SecondaryMotionAsset.MaxColliderGroups, 4, true); var colliders = new List<SecondaryMotionColliderGroup>();
            for (int i = 0; i < colliderCount; i++)
            {
                string name = ReadText(reader, stream, 128); int count = Count(reader, SecondaryMotionColliderGroup.MaxColliders, 20, true); var values = new List<SecondaryMotionCollider>();
                for (int j = 0; j < count; j++)
                {
                    string bone = ReadText(reader, stream, 64); var center = Vector(reader); float radius = reader.ReadSingle(); bool hasTail = reader.ReadBoolean();
                    values.Add(new SecondaryMotionCollider(bone, center, radius, hasTail ? (Vec3?)Vector(reader) : null));
                }
                colliders.Add(new SecondaryMotionColliderGroup(name, values));
            }
            int fixedCount = Count(reader, SecondaryMotionAsset.MaxFixedVertices, 4, true); var fixedVertices = new List<int>();
            for (int i = 0; i < fixedCount; i++) fixedVertices.Add(reader.ReadInt32());
            var profile = new SecondaryMotionProfile(adapterId, simulatorId, schemaVersion, adapterVersion, packageVersion, kind, payload);
            return new SecondaryMotionAsset(profile, skeletonHash, meshHash, chains, colliders, fixedVertices);
        }

        static int Count(BinaryReader reader, int maximum, int minimumBytes, bool allowZero)
        {
            int count = reader.ReadInt32(); Checks.Require(count >= (allowZero ? 0 : 1) && count <= maximum && (long)count * minimumBytes <= reader.BaseStream.Length - reader.BaseStream.Position,
                "INVALID_BLOB", "Secondary motion count is invalid or exceeds remaining data."); return count;
        }

        static string ReadOptionalHash(BinaryReader reader, Stream stream)
        {
            string value = ReadText(reader, stream, 64); if (value != "") Checks.HashText(value); return value;
        }

        static void WriteText(BinaryWriter writer, string value)
        {
            var bytes = Utf8.GetBytes(value ?? ""); Checks.Require(bytes.Length <= MaxTextBytes, "BUDGET_EXCEEDED", "Secondary motion text exceeds capacity."); writer.Write(bytes.Length); writer.Write(bytes);
        }

        static string ReadText(BinaryReader reader, Stream stream, int maximumCharacters)
        {
            int count = reader.ReadInt32(); Checks.Require(count >= 0 && count <= MaxTextBytes && count <= stream.Length - stream.Position, "INVALID_BLOB", "Secondary motion text is truncated.");
            var bytes = reader.ReadBytes(count); Checks.Require(bytes.Length == count, "INVALID_BLOB", "Secondary motion text is truncated.");
            string value = Utf8.GetString(bytes); Checks.Require(value.Length <= maximumCharacters, "INVALID_BLOB", "Secondary motion text exceeds capacity."); return value;
        }

        static void Vector(BinaryWriter writer, Vec3 value) { writer.Write(Checks.Canonical(value.X)); writer.Write(Checks.Canonical(value.Y)); writer.Write(Checks.Canonical(value.Z)); }
        static Vec3 Vector(BinaryReader reader) { return new Vec3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()); }
    }
}
