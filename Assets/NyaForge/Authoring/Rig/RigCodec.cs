using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring.Rig
{
    /// <summary>Strict versioned binary codec for rest skeleton and skin binding assets.</summary>
    public static class RigCodec
    {
        const int Magic = 0x4759524e; // NYRG
        const int SkeletonMagic = 0x5359524e; // NYRS
        const int BindingMagic = 0x4252594e; // NYRB
        const int Version = 1;
        const int MaxNameBytes = 512;
        static readonly Encoding Utf8 = new UTF8Encoding(false, true);

        public static byte[] Write(SkeletonDefinition skeleton, SkinBinding binding)
        {
            Checks.Require(skeleton != null && binding != null, "INVALID_RIG", "Skeleton and binding are required.");
            Checks.Require(binding.SkeletonHash == skeleton.ContentHash, "SKIN_SKELETON_CHANGED", "Binding belongs to another skeleton.");
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream, Utf8))
            {
                writer.Write(Magic); writer.Write(Version); writer.Write(skeleton.Bones.Count);
                foreach (var bone in skeleton.Bones.OrderBy(b => b.BoneId, StringComparer.Ordinal))
                {
                    Text(writer, bone.BoneId); Text(writer, bone.Name); Text(writer, bone.ParentBoneId); Vector(writer, bone.Head); Vector(writer, bone.Tail);
                }
                Text(writer, binding.MeshTopologyHash); Text(writer, binding.SkeletonHash); writer.Write(binding.Weights.Count);
                foreach (var pair in binding.Weights.OrderBy(p => p.Key))
                {
                    writer.Write(pair.Key); writer.Write(pair.Value.Count);
                    foreach (var weight in pair.Value) { Text(writer, weight.BoneId); writer.Write(Checks.Canonical(weight.Weight)); }
                }
                Checks.Require(stream.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Rig asset exceeds capacity.");
                return stream.ToArray();
            }
        }

        public static byte[] WriteSkeleton(SkeletonDefinition skeleton)
        {
            Checks.Require(skeleton != null, "INVALID_SKELETON", "Skeleton is required.");
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream, Utf8))
            {
                writer.Write(SkeletonMagic); writer.Write(Version); writer.Write(skeleton.Bones.Count);
                foreach (var bone in skeleton.Bones.OrderBy(b => b.BoneId, StringComparer.Ordinal))
                {
                    Text(writer, bone.BoneId); Text(writer, bone.Name); Text(writer, bone.ParentBoneId); Vector(writer, bone.Head); Vector(writer, bone.Tail);
                }
                Checks.Require(stream.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Skeleton asset exceeds capacity."); return stream.ToArray();
            }
        }

        /// <summary>Writes a binding independently from its input mesh and skeleton payloads.</summary>
        public static byte[] WriteBinding(SkinBinding binding)
        {
            Checks.Require(binding != null, "INVALID_SKIN", "Binding is required.");
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream, Utf8))
            {
                writer.Write(BindingMagic); writer.Write(Version);
                Text(writer, binding.MeshTopologyHash); Text(writer, binding.SkeletonHash);
                writer.Write(binding.Weights.Count);
                foreach (var pair in binding.Weights.OrderBy(p => p.Key))
                {
                    writer.Write(pair.Key); writer.Write(pair.Value.Count);
                    foreach (var weight in pair.Value)
                    {
                        Text(writer, weight.BoneId); writer.Write(Checks.Canonical(weight.Weight));
                    }
                }
                Checks.Require(stream.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Skin binding exceeds capacity.");
                return stream.ToArray();
            }
        }

        /// <summary>Restores a binding only after its mesh topology and skeleton identity are checked.</summary>
        public static SkinBinding ReadBinding(byte[] bytes, MeshData mesh, SkeletonDefinition skeleton)
        {
            Checks.Require(bytes != null && bytes.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Skin binding exceeds capacity.");
            Checks.Require(mesh != null && skeleton != null, "INVALID_SKIN", "Mesh and skeleton are required.");
            return ReadBindingUnbound(bytes).ValidateFor(mesh, skeleton);
        }

        internal static SkinBinding ReadBindingUnbound(byte[] bytes)
        {
            Checks.Require(bytes != null && bytes.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Skin binding exceeds capacity.");
            try
            {
                using (var stream = new MemoryStream(bytes, false)) using (var reader = new BinaryReader(stream, Utf8))
                {
                    Checks.Require(reader.ReadInt32() == BindingMagic, "INVALID_BLOB", "Not a skin binding asset.");
                    Checks.Require(reader.ReadInt32() == Version, "UNSUPPORTED_FORMAT", "Unsupported skin binding wire version.");
                    string meshHash = Hash(reader, 64), skeletonHash = Hash(reader, 64);
                    int vertexCount = Count(reader, AuthoringLimits.MaxVertices);
                    var seen = new HashSet<int>(); var raw = new List<SkinBinding.VertexWeightInput>();
                    for (int i = 0; i < vertexCount; i++)
                    {
                        int vertex = reader.ReadInt32(); Checks.Require(seen.Add(vertex), "DUPLICATE_VERTEX", "Binding repeats a vertex record.");
                        int influenceCount = Count(reader, SkinBinding.MaxInfluencesPerVertex);
                        Checks.Require(influenceCount > 0, "UNWEIGHTED_VERTEX", "Binding vertex has no influences.");
                        for (int j = 0; j < influenceCount; j++) raw.Add(new SkinBinding.VertexWeightInput(vertex, Text(reader, 64), reader.ReadSingle()));
                    }
                    Checks.Require(stream.Position == stream.Length, "INVALID_BLOB", "Trailing skin binding bytes.");
                    return SkinBinding.FromSerialized(meshHash, skeletonHash, raw);
                }
            }
            catch (EndOfStreamException e) { throw new AuthoringException("INVALID_BLOB", e.Message); }
            catch (DecoderFallbackException e) { throw new AuthoringException("INVALID_BLOB", e.Message); }
        }

        public static SkeletonDefinition ReadSkeleton(byte[] bytes)
        {
            Checks.Require(bytes != null && bytes.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Skeleton asset exceeds capacity.");
            try
            {
                using (var stream = new MemoryStream(bytes, false)) using (var reader = new BinaryReader(stream, Utf8))
                {
                    Checks.Require(reader.ReadInt32() == SkeletonMagic, "INVALID_BLOB", "Not a skeleton asset."); Checks.Require(reader.ReadInt32() == Version, "UNSUPPORTED_FORMAT", "Unsupported skeleton wire version.");
                    int count = Count(reader, SkeletonDefinition.MaxBones); var bones = new List<BoneDefinition>(count);
                    for (int i = 0; i < count; i++) bones.Add(new BoneDefinition(Text(reader, 64), Text(reader, MaxNameBytes), Text(reader, 64), Vector(reader), Vector(reader)));
                    Checks.Require(stream.Position == stream.Length, "INVALID_BLOB", "Trailing skeleton asset bytes."); return new SkeletonDefinition(bones);
                }
            }
            catch (EndOfStreamException e) { throw new AuthoringException("INVALID_BLOB", e.Message); }
            catch (DecoderFallbackException e) { throw new AuthoringException("INVALID_BLOB", e.Message); }
        }

        public static RigAsset Read(byte[] bytes, MeshData mesh)
        {
            Checks.Require(bytes != null && bytes.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Rig asset exceeds capacity.");
            Checks.Require(mesh != null, "INVALID_RIG", "Mesh is required to restore a skin binding.");
            try
            {
                using (var stream = new MemoryStream(bytes, false)) using (var reader = new BinaryReader(stream, Utf8))
                {
                    Checks.Require(reader.ReadInt32() == Magic, "INVALID_BLOB", "Not a rig asset.");
                    Checks.Require(reader.ReadInt32() == Version, "UNSUPPORTED_FORMAT", "Unsupported rig wire version.");
                    int boneCount = Count(reader, SkeletonDefinition.MaxBones); var bones = new List<BoneDefinition>(boneCount);
                    for (int i = 0; i < boneCount; i++)
                        bones.Add(new BoneDefinition(Text(reader, 64), Text(reader, MaxNameBytes), Text(reader, 64), Vector(reader), Vector(reader)));
                    var skeleton = new SkeletonDefinition(bones);
                    string meshHash = Hash(reader, 64), skeletonHash = Hash(reader, 64);
                    Checks.Require(meshHash == mesh.TopologyHash, "SKIN_TOPOLOGY_CHANGED", "Rig asset belongs to another mesh topology.");
                    Checks.Require(skeletonHash == skeleton.ContentHash, "SKIN_SKELETON_CHANGED", "Rig asset skeleton hash does not match its payload.");
                    int vertexCount = Count(reader, mesh.VertexCount); var raw = new List<SkinBinding.VertexWeightInput>();
                    for (int i = 0; i < vertexCount; i++)
                    {
                        int vertex = reader.ReadInt32(); int influenceCount = Count(reader, SkinBinding.MaxInfluencesPerVertex);
                        for (int j = 0; j < influenceCount; j++) raw.Add(new SkinBinding.VertexWeightInput(vertex, Text(reader, 64), reader.ReadSingle()));
                    }
                    Checks.Require(stream.Position == stream.Length, "INVALID_BLOB", "Trailing rig asset bytes.");
                    return new RigAsset(skeleton, SkinBinding.Create(mesh, skeleton, raw));
                }
            }
            catch (EndOfStreamException e) { throw new AuthoringException("INVALID_BLOB", e.Message); }
            catch (DecoderFallbackException e) { throw new AuthoringException("INVALID_BLOB", e.Message); }
        }

        static int Count(BinaryReader reader, int maximum)
        {
            int count = reader.ReadInt32(); Checks.Require(count >= 0 && count <= maximum, "BUDGET_EXCEEDED", "Rig count exceeds capacity."); return count;
        }
        static string Hash(BinaryReader reader, int maximum) { var value = Text(reader, maximum); Checks.HashText(value); return value; }
        static void Text(BinaryWriter writer, string value) { var bytes = Utf8.GetBytes(value); writer.Write(bytes.Length); writer.Write(bytes); }
        static string Text(BinaryReader reader, int maximum)
        {
            int count = Count(reader, maximum); Checks.Require(count <= reader.BaseStream.Length - reader.BaseStream.Position, "INVALID_BLOB", "Truncated rig text."); var bytes = reader.ReadBytes(count); Checks.Require(bytes.Length == count, "INVALID_BLOB", "Truncated rig text."); return Utf8.GetString(bytes);
        }
        static void Vector(BinaryWriter writer, Vec3 value) { writer.Write(Checks.Canonical(value.X)); writer.Write(Checks.Canonical(value.Y)); writer.Write(Checks.Canonical(value.Z)); }
        static Vec3 Vector(BinaryReader reader) { return new Vec3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()); }
    }
}
