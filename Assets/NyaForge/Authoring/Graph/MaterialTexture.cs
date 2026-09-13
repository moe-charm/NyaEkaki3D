using System;
using System.IO;
using System.Text;

namespace NyaForge.Authoring.Graph
{
    /// <summary>Semantic image maps supported by the Windows v1 PBR adapter.</summary>
    public enum MaterialTextureSemantic
    {
        Normal = 1,
        MetallicRoughness = 2
    }

    /// <summary>glTF sampler values. The numeric values are the glTF constants.</summary>
    public sealed class MaterialTextureSampler
    {
        public const int DefaultWrap = 10497; // REPEAT
        public const int DefaultMinFilter = 9987; // LINEAR_MIPMAP_LINEAR
        public const int DefaultMagFilter = 9729; // LINEAR

        public int WrapS { get; }
        public int WrapT { get; }
        public int MinFilter { get; }
        public int MagFilter { get; }
        public string ContentHash { get; }

        public static MaterialTextureSampler Default { get; } = new MaterialTextureSampler(DefaultWrap, DefaultWrap, DefaultMinFilter, DefaultMagFilter);

        public MaterialTextureSampler(int wrapS = DefaultWrap, int wrapT = DefaultWrap,
            int minFilter = DefaultMinFilter, int magFilter = DefaultMagFilter)
        {
            ValidateWrap(wrapS); ValidateWrap(wrapT); ValidateMinFilter(minFilter); ValidateMagFilter(magFilter);
            WrapS = wrapS; WrapT = wrapT; MinFilter = minFilter; MagFilter = magFilter;
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(WrapS); writer.Write(WrapT); writer.Write(MinFilter); writer.Write(MagFilter);
                ContentHash = Checks.Hash(stream.ToArray());
            }
        }

        static void ValidateWrap(int value)
        { Checks.Require(value == 10497 || value == 33071 || value == 33648, "INVALID_TEXTURE_SAMPLER", "Texture wrap mode is unsupported."); }
        static void ValidateMinFilter(int value)
        { Checks.Require(value == 9728 || value == 9729 || value == 9984 || value == 9985 || value == 9986 || value == 9987, "INVALID_TEXTURE_SAMPLER", "Texture minification filter is unsupported."); }
        static void ValidateMagFilter(int value)
        { Checks.Require(value == 9728 || value == 9729, "INVALID_TEXTURE_SAMPLER", "Texture magnification filter is unsupported."); }
    }

    /// <summary>One encoded local image plus the semantic information needed by an adapter.</summary>
    public sealed class MaterialTextureSlot
    {
        public MaterialTextureSemantic Semantic { get; }
        public string MimeType { get; }
        public int TexCoord { get; }
        public float NormalScale { get; }
        public MaterialTextureSampler Sampler { get; }
        public string ContentHash { get; }
        readonly byte[] encodedBytes;
        public int EncodedByteCount { get { return encodedBytes.Length; } }

        public MaterialTextureSlot(MaterialTextureSemantic semantic, byte[] encodedBytes, string mimeType,
            int texCoord = 0, float normalScale = 1f, MaterialTextureSampler sampler = null)
        {
            Checks.Require(Enum.IsDefined(typeof(MaterialTextureSemantic), semantic), "INVALID_TEXTURE_SLOT", "Texture semantic is unsupported.");
            Checks.Require(encodedBytes != null && encodedBytes.Length > 0 && encodedBytes.Length <= 16 * 1024 * 1024,
                "IMAGE_BUDGET_EXCEEDED", "Texture image exceeds the 16 MiB image budget.");
            Checks.Require(mimeType == "image/png" || mimeType == "image/jpeg", "UNSUPPORTED_FORMAT", "Only PNG and JPEG texture images are supported.");
            Checks.Require(texCoord == 0 || texCoord == 1, "INVALID_TEXTURE_SLOT", "Texture TEXCOORD set must be 0 or 1.");
            Checks.Require(!float.IsNaN(normalScale) && !float.IsInfinity(normalScale) && normalScale >= 0f && normalScale <= 8f,
                "INVALID_TEXTURE_SLOT", "Normal scale must be finite and between 0 and 8.");
            if (semantic != MaterialTextureSemantic.Normal)
                Checks.Require(Math.Abs(normalScale - 1f) < 0.000001f, "INVALID_TEXTURE_SLOT", "Only normal textures may specify normalScale.");
            Semantic = semantic; MimeType = mimeType; TexCoord = texCoord; NormalScale = normalScale;
            Sampler = sampler ?? MaterialTextureSampler.Default;
            this.encodedBytes = (byte[])encodedBytes.Clone();
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write((int)Semantic); writer.Write(TexCoord); writer.Write(Checks.Canonical(NormalScale));
                writer.Write(Sampler.ContentHash); writer.Write(MimeType); writer.Write(this.encodedBytes.Length); writer.Write(this.encodedBytes);
                ContentHash = Checks.Hash(stream.ToArray());
            }
        }

        public byte[] CopyEncodedBytes() { return (byte[])encodedBytes.Clone(); }
    }

    /// <summary>Optional normal and metallic-roughness slots attached to a standard material.</summary>
    public sealed class MaterialTextureSet
    {
        public MaterialTextureSlot Normal { get; }
        public MaterialTextureSlot MetallicRoughness { get; }
        public string ContentHash { get; }

        public MaterialTextureSet(MaterialTextureSlot normal = null, MaterialTextureSlot metallicRoughness = null)
        {
            Checks.Require(normal == null || normal.Semantic == MaterialTextureSemantic.Normal, "INVALID_TEXTURE_SLOT", "Normal slot has the wrong semantic.");
            Checks.Require(metallicRoughness == null || metallicRoughness.Semantic == MaterialTextureSemantic.MetallicRoughness, "INVALID_TEXTURE_SLOT", "Metallic-roughness slot has the wrong semantic.");
            Normal = normal; MetallicRoughness = metallicRoughness;
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(Normal != null); if (Normal != null) writer.Write(Normal.ContentHash);
                writer.Write(MetallicRoughness != null); if (MetallicRoughness != null) writer.Write(MetallicRoughness.ContentHash);
                ContentHash = Checks.Hash(stream.ToArray());
            }
        }

        public bool IsEmpty { get { return Normal == null && MetallicRoughness == null; } }
    }
}
