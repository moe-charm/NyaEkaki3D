using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NyaForge.Authoring.Rig
{
    /// <summary>Strict NYRP v1 codec for a pose asset. Skeleton membership is checked on binding.</summary>
    public static class PoseCodec
    {
        const int Magic = 0x5059524e; // NYRP
        const int Version = 1;
        static readonly Encoding Utf8 = new UTF8Encoding(false, true);

        public static byte[] Write(PoseSet pose)
        {
            Checks.Require(pose != null, "INVALID_POSE", "Pose is required.");
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream, Utf8))
            {
                writer.Write(Magic); writer.Write(Version); Text(writer, pose.SkeletonHash); writer.Write(pose.Poses.Count);
                foreach (var item in pose.Poses) { Text(writer, item.BoneId); PoseSet.Write(writer, item.Transform); }
                Checks.Require(stream.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Pose asset exceeds capacity."); return stream.ToArray();
            }
        }

        public static PoseSet Read(byte[] bytes, SkeletonDefinition skeleton)
        {
            Checks.Require(skeleton != null, "INVALID_POSE", "Skeleton is required."); return ReadUnbound(bytes).ValidateFor(skeleton);
        }

        internal static PoseSet ReadUnbound(byte[] bytes)
        {
            Checks.Require(bytes != null && bytes.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Pose asset exceeds capacity.");
            try
            {
                using (var stream = new MemoryStream(bytes, false)) using (var reader = new BinaryReader(stream, Utf8))
                {
                    Checks.Require(reader.ReadInt32() == Magic, "INVALID_BLOB", "Not a pose asset."); Checks.Require(reader.ReadInt32() == Version, "UNSUPPORTED_FORMAT", "Unsupported pose wire version.");
                    string hash = Text(reader, 64); int count = Count(reader, SkeletonDefinition.MaxBones); var poses = new List<BonePose>(count);
                    for (int i = 0; i < count; i++) poses.Add(new BonePose(Text(reader, 64), PoseSet.Read(reader)));
                    Checks.Require(stream.Position == stream.Length, "INVALID_BLOB", "Trailing pose asset bytes."); return PoseSet.FromSerialized(hash, poses);
                }
            }
            catch (EndOfStreamException e) { throw new AuthoringException("INVALID_BLOB", e.Message); }
            catch (DecoderFallbackException e) { throw new AuthoringException("INVALID_BLOB", e.Message); }
        }

        static int Count(BinaryReader reader, int max) { int value = reader.ReadInt32(); Checks.Require(value > 0 && value <= max, "BUDGET_EXCEEDED", "Pose count exceeds capacity."); return value; }
        static void Text(BinaryWriter writer, string value) { var bytes = Utf8.GetBytes(value); writer.Write(bytes.Length); writer.Write(bytes); }
        static string Text(BinaryReader reader, int max) { int count = reader.ReadInt32(); Checks.Require(count > 0 && count <= max, "INVALID_BLOB", "Invalid pose text length."); Checks.Require(count <= reader.BaseStream.Length - reader.BaseStream.Position, "INVALID_BLOB", "Truncated pose text."); return Utf8.GetString(reader.ReadBytes(count)); }
    }
}
