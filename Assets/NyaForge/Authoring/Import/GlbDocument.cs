using System;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring.Import
{
    /// <summary>Validated GLB container shared by the mesh and skin adapters.</summary>
    internal sealed class GlbDocument
    {
        public JObject Root { get; }
        public byte[] Bin { get; }
        public string SourceHash { get; }

        internal GlbDocument(JObject root, byte[] bin, string sourceHash)
        {
            Checks.Require(root != null && bin != null, "INVALID_IMPORT", "GLB document payload is required.");
            Checks.HashText(sourceHash); Root = root; Bin = (byte[])bin.Clone(); SourceHash = sourceHash;
        }
    }

    /// <summary>Strict, bounded GLB v2 container reader. Format-specific parsing lives in adapters.</summary>
    internal static class GlbDocumentReader
    {
        const int Magic = 0x46546c67; // glTF
        const int JsonChunk = 0x4e4f534a; // JSON
        const int BinChunk = 0x004e4942; // BIN\0
        const int MaxJsonBytes = 4 * 1024 * 1024;

        internal static GlbDocument Read(byte[] bytes)
        {
            Checks.Require(bytes != null && bytes.Length >= 20 && bytes.Length <= AuthoringLimits.MaxGlbImportBytes, "INVALID_IMPORT", "GLB must fit the 128 MiB import budget.");
            string sourceHash = Checks.Hash(bytes);
            try
            {
                using (var stream = new MemoryStream(bytes, false)) using (var reader = new BinaryReader(stream))
                {
                    Checks.Require(reader.ReadInt32() == Magic, "INVALID_IMPORT", "Not a GLB file.");
                    Checks.Require(reader.ReadInt32() == 2, "UNSUPPORTED_FORMAT", "Only GLB version 2 is supported.");
                    int declaredLength = reader.ReadInt32(); Checks.Require(declaredLength == bytes.Length, "INVALID_IMPORT", "GLB length does not match the file.");
                    byte[] jsonBytes = null, bin = null;
                    while (stream.Position < stream.Length)
                    {
                        int length = reader.ReadInt32(); int kind = reader.ReadInt32();
                        Checks.Require(length >= 0 && length <= stream.Length - stream.Position, "INVALID_IMPORT", "GLB chunk is truncated.");
                        var chunk = reader.ReadBytes(length); Checks.Require(chunk.Length == length, "INVALID_IMPORT", "GLB chunk is truncated.");
                        if (kind == JsonChunk) Checks.Require(jsonBytes == null && length <= MaxJsonBytes, "INVALID_IMPORT", "GLB must contain one bounded JSON chunk.");
                        else if (kind == BinChunk) Checks.Require(bin == null, "INVALID_IMPORT", "GLB must contain at most one BIN chunk.");
                        else throw new AuthoringException("UNSUPPORTED_FORMAT", "This GLB contains an unsupported chunk.");
                        if (kind == JsonChunk) jsonBytes = chunk; else bin = chunk;
                    }
                    Checks.Require(jsonBytes != null && bin != null, "INVALID_IMPORT", "GLB requires JSON and BIN chunks.");
                    var root = JObject.Parse(new UTF8Encoding(false, true).GetString(jsonBytes));
                    return new GlbDocument(root, bin, sourceHash);
                }
            }
            catch (EndOfStreamException e) { throw new AuthoringException("INVALID_IMPORT", e.Message); }
            catch (DecoderFallbackException e) { throw new AuthoringException("INVALID_IMPORT", e.Message); }
            catch (Newtonsoft.Json.JsonException e) { throw new AuthoringException("INVALID_IMPORT", e.Message); }
        }
    }
}
