using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security.Cryptography;

namespace NyaForge.Authoring
{
    public sealed class AuthoringException : Exception
    {
        public string Code { get; private set; }
        public AuthoringException(string code, string message) : base(message) { Code = code; }
    }

    public struct Vec2
    {
        public readonly float X, Y;
        public Vec2(float x, float y) { X = x; Y = y; }
    }
    public struct Vec3
    {
        public readonly float X, Y, Z;
        public Vec3(float x, float y, float z) { X = x; Y = y; Z = z; }
        public static Vec3 operator +(Vec3 a, Vec3 b) { return new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z); }
        public static Vec3 operator -(Vec3 a, Vec3 b) { return new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z); }
        public static Vec3 operator *(Vec3 a, float b) { return new Vec3(a.X * b, a.Y * b, a.Z * b); }
    }
    public struct Vec4
    {
        public readonly float X, Y, Z, W;
        public Vec4(float x, float y, float z, float w) { X = x; Y = y; Z = z; W = w; }
    }

    // NF-0 deliberately supports only positive uniform scale and translation.
    // Rotation, negative/non-uniform scales, skinning and morphs are not implied.
    public struct RestTransform
    {
        public readonly float Scale;
        public readonly Vec3 Translation;
        public RestTransform(float scale, Vec3 translation)
        {
            Checks.Finite(scale); Checks.Finite(translation);
            Checks.Require(scale >= 0.000001f && scale <= 1000000f, "UNSUPPORTED_TRANSFORM", "Scale must be positive and between 1e-6 and 1e6.");
            Scale = scale; Translation = translation;
        }
        public Vec3 ToAvatarPoint(Vec3 local) { return local * Scale + Translation; }
        public Vec3 ToLocalVector(Vec3 restDelta) { return restDelta * (1f / Scale); }
        internal void Validate() { new RestTransform(Scale, Translation); }
    }

    public static class AuthoringLimits
    {
        public const int MaxVertices = 100000;
        public const int MaxIndices = 600000;
        public const int MaxSubmeshes = 32;
        public const int MaxManifestBytes = 65536;
        public const int MaxBlobBytes = 16 * 1024 * 1024;
        public const int MaxOperations = 64;
        public const int MaxHistory = 128;
        public const int MaxCommands = 10000;
    }

    internal static class Checks
    {
        internal static void Require(bool value, string code, string message) { if (!value) throw new AuthoringException(code, message); }
        internal static void Finite(float f) { Require(!float.IsNaN(f) && !float.IsInfinity(f), "NON_FINITE", "Geometry must contain finite numbers."); }
        internal static void Finite(Vec3 v) { Finite(v.X); Finite(v.Y); Finite(v.Z); }
        internal static void Id(string id) { Guid parsed; Require(Guid.TryParseExact(id, "D", out parsed), "INVALID_ID", "Identity must be a canonical UUID."); }
        internal static void Name(string name) { Require(!string.IsNullOrWhiteSpace(name) && name.Length <= 128 && name.IndexOf('\0') < 0, "INVALID_NAME", "Name must contain 1 to 128 characters."); }
        internal static string Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
        internal static void HashText(string hash)
        {
            Require(hash != null && hash.Length == 64, "INVALID_HASH", "Expected SHA-256 hex.");
            foreach (char c in hash) Require((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f'), "INVALID_HASH", "Expected lowercase SHA-256 hex.");
        }
        internal static float Canonical(float value) { Finite(value); return value == 0f ? 0f : value; }
    }

    public sealed class MeshData
    {
        private readonly Vec3[] positions, normals;
        private readonly Vec4[] tangents;
        private readonly Vec2[] uv0;
        private readonly int[][] submeshes;
        public IReadOnlyList<Vec3> Positions { get; private set; }
        public IReadOnlyList<Vec3> Normals { get; private set; }
        public IReadOnlyList<Vec4> Tangents { get; private set; }
        public IReadOnlyList<Vec2> Uv0 { get; private set; }
        public IReadOnlyList<int[]> Submeshes
        {
            get { var result = new int[submeshes.Length][]; for (int i = 0; i < result.Length; i++) result[i] = (int[])submeshes[i].Clone(); return Array.AsReadOnly(result); }
        }
        public int VertexCount { get { return positions.Length; } }
        public int TriangleCount { get; private set; }
        public string ContentHash { get; private set; }
        public string TopologyHash { get; private set; }

        public MeshData(Vec3[] positions, Vec3[] normals, Vec4[] tangents, Vec2[] uv0, int[][] submeshes)
        {
            Checks.Require(positions != null && positions.Length >= 3 && positions.Length <= AuthoringLimits.MaxVertices, "BUDGET_EXCEEDED", "Mesh vertex budget exceeded or no triangles possible.");
            Checks.Require(normals != null && tangents != null && uv0 != null && submeshes != null, "INVALID_MESH", "Use empty arrays to declare absent attributes.");
            Checks.Require(submeshes.Length >= 1 && submeshes.Length <= AuthoringLimits.MaxSubmeshes, "BUDGET_EXCEEDED", "Submesh budget exceeded.");
            AttributeCount(normals.Length, positions.Length); AttributeCount(tangents.Length, positions.Length); AttributeCount(uv0.Length, positions.Length);
            long indexCount = 0;
            foreach (var indices in submeshes)
            {
                Checks.Require(indices != null && indices.Length > 0 && indices.Length % 3 == 0, "INVALID_MESH", "Each submesh must contain triangles.");
                indexCount += indices.Length;
                Checks.Require(indexCount <= AuthoringLimits.MaxIndices, "BUDGET_EXCEEDED", "Index budget exceeded.");
                foreach (int index in indices) Checks.Require(index >= 0 && index < positions.Length, "INVALID_MESH", "Index is outside the vertex domain.");
                for (int i = 0; i < indices.Length; i += 3)
                {
                    Vec3 a = positions[indices[i]], b = positions[indices[i + 1]], c = positions[indices[i + 2]];
                    double ux = b.X - a.X, uy = b.Y - a.Y, uz = b.Z - a.Z, vx = c.X - a.X, vy = c.Y - a.Y, vz = c.Z - a.Z;
                    double cx = uy * vz - uz * vy, cy = uz * vx - ux * vz, cz = ux * vy - uy * vx;
                    Checks.Require(cx * cx + cy * cy + cz * cz > 0, "DEGENERATE_TRIANGLE", "Triangle has zero area.");
                }
            }
            foreach (var v in positions) Checks.Finite(v);
            foreach (var v in normals) { Checks.Finite(v); Checks.Require((double)v.X * v.X + (double)v.Y * v.Y + (double)v.Z * v.Z > 0, "INVALID_MESH", "Normal must be nonzero."); }
            foreach (var v in tangents) { Checks.Finite(v.X); Checks.Finite(v.Y); Checks.Finite(v.Z); Checks.Finite(v.W); Checks.Require(v.W == 1f || v.W == -1f, "INVALID_MESH", "Tangent handedness must be +/-1."); }
            foreach (var v in uv0) { Checks.Finite(v.X); Checks.Finite(v.Y); }
            this.positions = (Vec3[])positions.Clone(); this.normals = (Vec3[])normals.Clone(); this.tangents = (Vec4[])tangents.Clone(); this.uv0 = (Vec2[])uv0.Clone();
            this.submeshes = new int[submeshes.Length][];
            for (int i = 0; i < submeshes.Length; i++) this.submeshes[i] = (int[])submeshes[i].Clone();
            Positions = Array.AsReadOnly(this.positions); Normals = Array.AsReadOnly(this.normals); Tangents = Array.AsReadOnly(this.tangents); Uv0 = Array.AsReadOnly(this.uv0);
            TriangleCount = (int)indexCount / 3;
            ContentHash = Checks.Hash(MeshBinary.Write(this));
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(1); writer.Write(VertexCount); writer.Write(submeshes.Length);
                foreach (var indices in submeshes) { writer.Write(indices.Length); foreach (int index in indices) writer.Write(index); }
                TopologyHash = Checks.Hash(stream.ToArray());
            }
        }
        private static void AttributeCount(int count, int vertices) { Checks.Require(count == 0 || count == vertices, "INVALID_MESH", "Attribute count must equal vertex count or be absent."); }
        internal MeshData WithPositions(Vec3[] values) { return new MeshData(values, normals, tangents, uv0, submeshes); }
    }

    public static class AuthoringFixtures
    {
        // Original geometry, independent of avatar packs. Duplicate front/back corners
        // deliberately preserve a normal/UV seam and two distinct submeshes.
        public static MeshData Panel(float sourceScale)
        {
            new RestTransform(sourceScale, new Vec3());
            var p = new[] { new Vec3(-.1f,-.05f,-.02f), new Vec3(.1f,-.05f,-.02f), new Vec3(.1f,.05f,-.02f), new Vec3(-.1f,.05f,-.02f), new Vec3(-.1f,-.05f,-.02f), new Vec3(.1f,-.05f,-.02f), new Vec3(.1f,.05f,-.02f), new Vec3(-.1f,.05f,-.02f) };
            for (int i = 0; i < p.Length; i++) p[i] = p[i] * (1f / sourceScale);
            var n = new Vec3[8]; var t = new Vec4[8]; var uv = new Vec2[8];
            for (int i = 0; i < 8; i++) { n[i] = new Vec3(0,0,i < 4 ? -1 : 1); t[i] = new Vec4(1,0,0,i < 4 ? -1 : 1); }
            uv[0] = uv[4] = new Vec2(0,0); uv[1] = uv[5] = new Vec2(1,0); uv[2] = uv[6] = new Vec2(1,1); uv[3] = uv[7] = new Vec2(0,1);
            return new MeshData(p,n,t,uv,new[] { new[] { 0,2,1,0,3,2 }, new[] { 4,5,6,4,6,7 } });
        }
    }
}
