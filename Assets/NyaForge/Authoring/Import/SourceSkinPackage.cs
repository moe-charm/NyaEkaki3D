using System;
using System.IO;
using System.Linq;
using System.Text;

namespace NyaForge.Authoring.Import
{
    /// <summary>Immutable native payload pair for complete source frames and source-indexed weights.</summary>
    public sealed class SourceSkinPackage
    {
        public SourceSkin Skin { get; }
        public SourceSkinBinding Binding { get; }
        public SourceSkinPackage(SourceSkin skin, SourceSkinBinding binding)
        {
            Checks.Require(skin != null && binding != null && skin.Nodes.SourceHash == binding.SourceHash, "SKIN_SOURCE_CHANGED", "Source skin package identities differ.");
            Checks.Require(binding.Weights.Values.SelectMany(values => values).All(weight => weight.JointSlot < skin.Joints.Count), "INVALID_SKIN", "Source skin package references an unknown joint slot.");
            Skin = skin; Binding = binding;
        }
    }

    public static class SourceSkinPackageCodec
    {
        const int Magic = 0x5053594e; // NYSP
        const int Version = 1;

        public static byte[] Write(SourceSkinPackage package)
        {
            Checks.Require(package != null, "INVALID_SKIN", "Source skin package is required.");
            byte[] skin = SourceSkinCodec.Write(package.Skin), binding = WriteBinding(package.Binding);
            Checks.Require((long)skin.Length + binding.Length + 20 <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Source skin package exceeds capacity.");
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(Magic); writer.Write(Version); writer.Write(skin.Length); writer.Write(binding.Length); writer.Write(skin); writer.Write(binding); return stream.ToArray();
            }
        }

        public static SourceSkinPackage Read(byte[] bytes)
        {
            Checks.Require(bytes != null && bytes.Length > 0 && bytes.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Source skin package exceeds capacity.");
            try
            {
                using (var stream = new MemoryStream(bytes, false)) using (var reader = new BinaryReader(stream))
                {
                    Checks.Require(reader.ReadInt32() == Magic, "INVALID_IMPORT", "Not a source skin package."); Checks.Require(reader.ReadInt32() == Version, "UNSUPPORTED_FORMAT", "Unsupported source skin package version.");
                    int skinLength = Length(reader, bytes.Length), bindingLength = Length(reader, bytes.Length);
                    Checks.Require((long)skinLength + bindingLength == bytes.Length - stream.Position, "INVALID_IMPORT", "Source skin package lengths are inconsistent.");
                    var skin = SourceSkinCodec.Read(reader.ReadBytes(skinLength)); var binding = ReadBinding(reader.ReadBytes(bindingLength), skin); return new SourceSkinPackage(skin, binding);
                }
            }
            catch (EndOfStreamException error) { throw new AuthoringException("INVALID_IMPORT", error.Message); }
        }

        static int Length(BinaryReader reader, int max) { int value = reader.ReadInt32(); Checks.Require(value > 0 && value <= max, "INVALID_IMPORT", "Source skin package section length is invalid."); return value; }

        static byte[] WriteBinding(SourceSkinBinding binding)
        {
            Checks.Require(binding != null, "INVALID_SKIN", "Source skin binding is required.");
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(1); writer.Write(Encoding.ASCII.GetBytes(binding.SourceHash)); writer.Write(Encoding.ASCII.GetBytes(binding.MeshTopologyHash)); writer.Write(binding.VertexCount);
                long count = binding.Weights.Sum(pair => (long)pair.Value.Count); Checks.Require(count > 0 && count <= AuthoringLimits.MaxVertices * (long)SourceSkinBinding.MaxInfluencesPerVertex, "BUDGET_EXCEEDED", "Source weight count exceeds capacity."); writer.Write((int)count);
                foreach (var pair in binding.Weights.OrderBy(pair => pair.Key)) foreach (var item in pair.Value.OrderByDescending(item => item.Weight).ThenBy(item => item.JointSlot)) { writer.Write(item.VertexIndex); writer.Write(item.JointSlot); writer.Write(item.Weight); }
                writer.Flush(); Checks.Require(stream.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Source weight payload exceeds capacity."); return stream.ToArray();
            }
        }

        static SourceSkinBinding ReadBinding(byte[] bytes, SourceSkin skin)
        {
            try
            {
                using (var stream = new MemoryStream(bytes, false)) using (var reader = new BinaryReader(stream))
                {
                    Checks.Require(reader.ReadInt32() == 1, "UNSUPPORTED_FORMAT", "Unsupported source weight payload version.");
                    string sourceHash = Encoding.ASCII.GetString(reader.ReadBytes(64)), topologyHash = Encoding.ASCII.GetString(reader.ReadBytes(64)); Checks.HashText(sourceHash); Checks.HashText(topologyHash);
                    int vertexCount = reader.ReadInt32(); int count = reader.ReadInt32(); Checks.Require(count > 0 && count <= AuthoringLimits.MaxVertices * SourceSkinBinding.MaxInfluencesPerVertex && (long)count * 12 <= stream.Length - stream.Position, "INVALID_IMPORT", "Source weight payload count is invalid.");
                    var values = new SourceSkinWeight[count]; for (int i = 0; i < count; i++) values[i] = new SourceSkinWeight(reader.ReadInt32(), reader.ReadInt32(), reader.ReadSingle());
                    Checks.Require(stream.Position == stream.Length, "INVALID_IMPORT", "Trailing source weight data is not allowed."); return SourceSkinBinding.FromSerialized(sourceHash, topologyHash, vertexCount, skin, values);
                }
            }
            catch (EndOfStreamException error) { throw new AuthoringException("INVALID_IMPORT", error.Message); }
        }
    }
}
