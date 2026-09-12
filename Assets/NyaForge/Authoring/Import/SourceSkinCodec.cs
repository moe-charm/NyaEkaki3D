using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NyaForge.Authoring.Import
{
    /// <summary>Versioned source frames/bind payload. World frames are derived from preserved local matrices.</summary>
    public static class SourceSkinCodec
    {
        const int Magic = 0x5346594e; // NYFS, little-endian
        const int Version = 1;

        public static byte[] Write(SourceSkin skin)
        {
            Checks.Require(skin != null, "INVALID_IMPORT", "Source skin is required.");
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                var nodes = skin.Nodes;
                long size = 8 + 64 + 4 + (long)nodes.Local.Count * 136 + 16 + (long)skin.Joints.Count * 4;
                foreach (var children in nodes.Hierarchy.Children) size += (long)children.Count * 4;
                if (skin.HasExplicitInverseBindMatrices) size += (long)skin.InverseBindMatrices.Count * 128;
                Checks.Require(size <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Source skin payload exceeds capacity.");
                writer.Write(Magic); writer.Write(Version);
                writer.Write(Encoding.ASCII.GetBytes(nodes.SourceHash)); writer.Write(nodes.Local.Count);
                for (int i = 0; i < nodes.Local.Count; i++)
                {
                    writer.Write(nodes.Hierarchy.Parents[i]); WriteMatrix(writer, nodes.Local[i]);
                    writer.Write(nodes.Hierarchy.Children[i].Count);
                    foreach (int child in nodes.Hierarchy.Children[i]) writer.Write(child);
                }
                writer.Write(skin.SkinIndex); writer.Write(skin.SkeletonRoot ?? -1); writer.Write(skin.Joints.Count);
                foreach (int joint in skin.Joints) writer.Write(joint);
                // -1 is source-defined omission; zero is invalid, never an unknown legacy payload.
                writer.Write(skin.HasExplicitInverseBindMatrices ? skin.InverseBindMatrices.Count : -1);
                if (skin.HasExplicitInverseBindMatrices)
                    foreach (var matrix in skin.InverseBindMatrices) WriteMatrix(writer, matrix);
                writer.Flush();
                Checks.Require(stream.Length == size, "INVALID_IMPORT", "Source skin payload length is inconsistent.");
                return stream.ToArray();
            }
        }

        public static SourceSkin Read(byte[] bytes)
        {
            Checks.Require(bytes != null && bytes.Length > 0 && bytes.Length <= AuthoringLimits.MaxBlobBytes,
                "BUDGET_EXCEEDED", "Source skin payload exceeds capacity.");
            try
            {
                using (var stream = new MemoryStream(bytes, false))
                using (var reader = new BinaryReader(stream))
                {
                    Checks.Require(reader.ReadInt32() == Magic, "INVALID_IMPORT", "Not a source skin payload.");
                    Checks.Require(reader.ReadInt32() == Version, "UNSUPPORTED_FORMAT", "Unsupported source skin payload version.");
                    var hashBytes = reader.ReadBytes(64);
                    Checks.Require(hashBytes.Length == 64, "INVALID_IMPORT", "Source hash is truncated.");
                    string hash = Encoding.ASCII.GetString(hashBytes); Checks.HashText(hash);
                    int nodeCount = Count(reader, ImportedSourceHierarchy.MaxNodes, 136);
                    var parents = new int[nodeCount]; var local = new SourceAffine[nodeCount];
                    var children = new IReadOnlyList<int>[nodeCount]; int totalChildren = 0;
                    for (int i = 0; i < nodeCount; i++)
                    {
                        parents[i] = reader.ReadInt32(); local[i] = ReadMatrix(reader);
                        int count = Count(reader, nodeCount - 1, 4, true);
                        totalChildren += count;
                        Checks.Require(totalChildren < nodeCount, "INVALID_IMPORT", "Source forest has too many edges.");
                        var values = new int[count];
                        for (int j = 0; j < count; j++) values[j] = reader.ReadInt32();
                        children[i] = values;
                    }
                    int skinIndex = reader.ReadInt32(), root = reader.ReadInt32();
                    Checks.Require(root >= -1, "INVALID_IMPORT", "Invalid skeleton root sentinel.");
                    int jointCount = Count(reader, nodeCount, 4);
                    var joints = new int[jointCount];
                    for (int i = 0; i < jointCount; i++) joints[i] = reader.ReadInt32();
                    int matrixCount = reader.ReadInt32(); SourceAffine[] matrices = null;
                    if (matrixCount != -1)
                    {
                        Checks.Require(matrixCount >= jointCount && (long)matrixCount * 128 <= stream.Length - stream.Position,
                            "INVALID_IMPORT", "Source inverse-bind entries are incomplete or exceed remaining data.");
                        matrices = new SourceAffine[matrixCount];
                        for (int i = 0; i < matrixCount; i++) matrices[i] = ReadMatrix(reader);
                    }
                    Checks.Require(stream.Position == stream.Length, "INVALID_IMPORT", "Trailing source skin data is not allowed.");
                    var nodes = new SourceNodeTransforms(hash, parents, local, children);
                    return new SourceSkin(nodes, skinIndex, joints, matrices, root == -1 ? (int?)null : root);
                }
            }
            catch (EndOfStreamException error) { throw new AuthoringException("INVALID_IMPORT", error.Message); }
        }

        static int Count(BinaryReader reader, int maximum, int minimumBytes, bool allowZero = false)
        {
            int count = reader.ReadInt32();
            Checks.Require(count >= (allowZero ? 0 : 1) && count <= maximum &&
                (long)count * minimumBytes <= reader.BaseStream.Length - reader.BaseStream.Position,
                "INVALID_IMPORT", "Source list count is invalid or exceeds remaining data.");
            return count;
        }

        static void WriteMatrix(BinaryWriter writer, SourceAffine matrix)
        {
            foreach (double value in matrix.ToColumnMajor()) writer.Write(value);
        }

        static SourceAffine ReadMatrix(BinaryReader reader)
        {
            var values = new double[16];
            for (int i = 0; i < values.Length; i++) values[i] = reader.ReadDouble();
            return new SourceAffine(values);
        }
    }
}
