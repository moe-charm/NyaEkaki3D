using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NyaForge.Authoring.Simulation
{
    public sealed class PhysBonesTargetDocument
    {
        public int WireVersion { get; }
        public bool IsSupported { get; }
        public PhysBonesTargetProfile Profile { get; }
        public IReadOnlyList<byte> RawBytes { get; }
        public string RawHash { get; }
        public string Diagnostic { get; }
        internal PhysBonesTargetDocument(int wireVersion, bool supported, PhysBonesTargetProfile profile, byte[] bytes, string diagnostic) { WireVersion = wireVersion; IsSupported = supported; Profile = profile; RawBytes = Array.AsReadOnly((byte[])bytes.Clone()); RawHash = Checks.Hash(bytes); Diagnostic = diagnostic ?? ""; }
        public PhysBonesTargetProfile RequireSupported() { Checks.Require(IsSupported && Profile != null, "UNSUPPORTED_FORMAT", Diagnostic == "" ? "PhysBones target profile version is unsupported." : Diagnostic); return Profile; }
    }

    /// <summary>Strict bounded NYPP v1 target DTO codec. It is intentionally independent of the VRChat SDK.</summary>
    public static class PhysBonesTargetCodec
    {
        const int Magic = 0x5050594e; // NYPP, little-endian
        const int Version = 1;
        const int MaxTextBytes = 512;
        static readonly Encoding Utf8 = new UTF8Encoding(false, true);

        public static byte[] Write(PhysBonesTargetProfile profile)
        {
            Checks.Require(profile != null, "INVALID_PHYSBONES", "PhysBones target profile is required.");
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream, Utf8, true))
            {
                writer.Write(Magic); writer.Write(Version); Text(writer, profile.TargetId); Text(writer, profile.SdkVersion); Text(writer, profile.PackageVersion); Text(writer, profile.SkeletonHash); Text(writer, profile.SourceSecondaryMotionHash); writer.Write(profile.Chains.Count);
                foreach (var chain in profile.Chains) WriteChain(writer, chain);
                writer.Flush(); Checks.Require(stream.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "PhysBones target profile exceeds capacity."); return stream.ToArray();
            }
        }

        public static PhysBonesTargetProfile Read(byte[] bytes) { return ReadDocument(bytes).RequireSupported(); }
        public static PhysBonesTargetDocument ReadDocument(byte[] bytes)
        {
            Checks.Require(bytes != null && bytes.Length >= 8 && bytes.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "PhysBones target profile exceeds capacity.");
            try
            {
                using (var stream = new MemoryStream(bytes, false)) using (var reader = new BinaryReader(stream, Utf8))
                {
                    Checks.Require(reader.ReadInt32() == Magic, "INVALID_BLOB", "Not a PhysBones target profile."); int wireVersion = reader.ReadInt32();
                    if (wireVersion != Version) return new PhysBonesTargetDocument(wireVersion, false, null, bytes, "PhysBones target profile version " + wireVersion + " is unsupported; raw bytes were retained.");
                    var profile = ReadProfile(reader, stream); Checks.Require(stream.Position == stream.Length, "INVALID_BLOB", "Trailing PhysBones target profile bytes."); return new PhysBonesTargetDocument(wireVersion, true, profile, bytes, "");
                }
            }
            catch (EndOfStreamException error) { throw new AuthoringException("INVALID_BLOB", error.Message); }
            catch (DecoderFallbackException error) { throw new AuthoringException("INVALID_BLOB", error.Message); }
        }

        static void WriteChain(BinaryWriter writer, PhysBonesChain chain)
        {
            Text(writer, chain.Name); Text(writer, chain.RootBoneId); Count(writer, chain.BoneIds.Count); foreach (var id in chain.BoneIds) Text(writer, id); writer.Write((int)chain.EndpointMode); Text(writer, chain.EndBoneId); writer.Write(chain.EndpointPosition.HasValue); if (chain.EndpointPosition.HasValue) Vector(writer, chain.EndpointPosition.Value); writer.Write((int)chain.MultiChildType);
            Count(writer, chain.ExcludedBoneIds.Count); foreach (var id in chain.ExcludedBoneIds) Text(writer, id); Count(writer, chain.Branches.Count); foreach (var branch in chain.Branches) { Text(writer, branch.ParentBoneId); Count(writer, branch.ChildBoneIds.Count); foreach (var id in branch.ChildBoneIds) Text(writer, id); }
            Count(writer, chain.ColliderGroupIndices.Count); foreach (var index in chain.ColliderGroupIndices) writer.Write(index); WriteParameters(writer, chain.Parameters); WriteInteraction(writer, chain.Interaction); Count(writer, chain.Curves.Count); foreach (var curve in chain.Curves) { writer.Write((int)curve.Channel); Count(writer, curve.Keys.Count); foreach (var key in curve.Keys) { writer.Write(Checks.Canonical(key.Time)); writer.Write(Checks.Canonical(key.Value)); } }
        }

        static PhysBonesTargetProfile ReadProfile(BinaryReader reader, Stream stream)
        {
            string target = ReadText(reader, stream, 128), sdk = ReadText(reader, stream, 128), package = ReadText(reader, stream, 128), skeleton = ReadText(reader, stream, 64), source = ReadText(reader, stream, 64); int chainCount = Count(reader, PhysBonesTargetProfile.MaxChains, 4, false); var chains = new List<PhysBonesChain>(); for (int i = 0; i < chainCount; i++) chains.Add(ReadChain(reader, stream)); return new PhysBonesTargetProfile(target, sdk, package, skeleton, source, chains);
        }

        static PhysBonesChain ReadChain(BinaryReader reader, Stream stream)
        {
            string name = ReadText(reader, stream, 128), root = ReadText(reader, stream, 64); int boneCount = Count(reader, PhysBonesChain.MaxBones, 4, false); var bones = new List<string>(); for (int i = 0; i < boneCount; i++) bones.Add(ReadText(reader, stream, 64)); var endpoint = (PhysBonesEndpointMode)reader.ReadInt32(); string endBone = ReadText(reader, stream, 64); Vec3? endpointPosition = reader.ReadBoolean() ? (Vec3?)Vector(reader) : null; var branches = (PhysBonesMultiChildType)reader.ReadInt32(); int excludedCount = Count(reader, PhysBonesChain.MaxExcluded, 4, true); var excluded = new List<string>(); for (int i = 0; i < excludedCount; i++) excluded.Add(ReadText(reader, stream, 64)); int branchCount = Count(reader, PhysBonesChain.MaxBranches, 4, true); var branchValues = new List<PhysBonesBranch>(); for (int i = 0; i < branchCount; i++) { string parent = ReadText(reader, stream, 64); int childCount = Count(reader, 64, 4, false); var children = new List<string>(); for (int j = 0; j < childCount; j++) children.Add(ReadText(reader, stream, 64)); branchValues.Add(new PhysBonesBranch(parent, children)); }
            int colliderCount = Count(reader, 256, 4, true); var colliders = new List<int>(); for (int i = 0; i < colliderCount; i++) colliders.Add(reader.ReadInt32()); var parameters = ReadParameters(reader); var interaction = ReadInteraction(reader, stream); int curveCount = Count(reader, PhysBonesChain.MaxCurves, 8, true); var curves = new List<PhysBonesCurve>(); for (int i = 0; i < curveCount; i++) { var channel = (PhysBonesCurveChannel)reader.ReadInt32(); int keyCount = Count(reader, PhysBonesCurve.MaxKeys, 8, false); var keys = new List<PhysBonesCurveKey>(); for (int j = 0; j < keyCount; j++) keys.Add(new PhysBonesCurveKey(reader.ReadSingle(), reader.ReadSingle())); curves.Add(new PhysBonesCurve(channel, keys)); }
            return new PhysBonesChain(name, root, bones, endpoint, endBone, endpointPosition, branches, excluded, branchValues, colliders, parameters, interaction, curves);
        }

        static void WriteParameters(BinaryWriter writer, PhysBonesParameters p)
        {
            writer.Write((int)p.LimitType); writer.Write(Checks.Canonical(p.MaxAngle)); writer.Write(Checks.Canonical(p.Radius)); writer.Write(Checks.Canonical(p.Stiffness)); writer.Write(Checks.Canonical(p.Pull)); writer.Write(Checks.Canonical(p.Spring)); writer.Write(Checks.Canonical(p.Immobile)); writer.Write(Checks.Canonical(p.Gravity)); writer.Write(Checks.Canonical(p.GravityFalloff)); writer.Write(Checks.Canonical(p.Damping)); writer.Write(Checks.Canonical(p.Elasticity)); writer.Write(Checks.Canonical(p.Inert)); writer.Write(Checks.Canonical(p.Friction)); writer.Write(Checks.Canonical(p.StretchMotion)); writer.Write(Checks.Canonical(p.Squish)); Vector(writer, p.GravityDirection);
        }
        static PhysBonesParameters ReadParameters(BinaryReader reader) { return new PhysBonesParameters((PhysBonesLimitType)reader.ReadInt32(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), Vector(reader)); }
        static void WriteInteraction(BinaryWriter writer, PhysBonesInteraction value) { writer.Write(value.AllowPosing); writer.Write(value.AllowCollision); writer.Write(value.AllowGrabbing); writer.Write(value.SnapToHand); writer.Write(value.ResetWhenDisabled); writer.Write(value.IsAnimated); Text(writer, value.Parameter); }
        static PhysBonesInteraction ReadInteraction(BinaryReader reader, Stream stream) { return new PhysBonesInteraction(reader.ReadBoolean(), reader.ReadBoolean(), reader.ReadBoolean(), reader.ReadBoolean(), reader.ReadBoolean(), reader.ReadBoolean(), ReadText(reader, stream, 128)); }
        static void Count(BinaryWriter writer, int count) { writer.Write(count); }
        static int Count(BinaryReader reader, int maximum, int minimumBytes, bool allowZero) { int count = reader.ReadInt32(); Checks.Require(count >= (allowZero ? 0 : 1) && count <= maximum && (long)count * minimumBytes <= reader.BaseStream.Length - reader.BaseStream.Position, "INVALID_BLOB", "PhysBones count is invalid or exceeds remaining data."); return count; }
        static void Text(BinaryWriter writer, string value) { var bytes = Utf8.GetBytes(value ?? ""); Checks.Require(bytes.Length <= MaxTextBytes, "BUDGET_EXCEEDED", "PhysBones text exceeds capacity."); writer.Write(bytes.Length); writer.Write(bytes); }
        static string ReadText(BinaryReader reader, Stream stream, int maximumCharacters) { int count = reader.ReadInt32(); Checks.Require(count >= 0 && count <= MaxTextBytes && count <= stream.Length - stream.Position, "INVALID_BLOB", "PhysBones text is truncated."); var bytes = reader.ReadBytes(count); Checks.Require(bytes.Length == count, "INVALID_BLOB", "PhysBones text is truncated."); string value = Utf8.GetString(bytes); Checks.Require(value.Length <= maximumCharacters, "INVALID_BLOB", "PhysBones text exceeds capacity."); return value; }
        static void Vector(BinaryWriter writer, Vec3 value) { writer.Write(Checks.Canonical(value.X)); writer.Write(Checks.Canonical(value.Y)); writer.Write(Checks.Canonical(value.Z)); }
        static Vec3 Vector(BinaryReader reader) { return new Vec3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()); }
    }
}
