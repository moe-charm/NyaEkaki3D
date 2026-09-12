using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NyaForge.Authoring.Topology;

namespace NyaForge.Authoring
{
    internal static class PolygonBinaryCodec
    {
        const int Magic = 0x5046594e, HeaderBytes = 56;
        internal static byte[] Write(PolygonMesh mesh)
        {
            if (mesh == null) throw new ArgumentNullException(nameof(mesh));
            var first = mesh.Faces.FirstOrDefault()?.Corners[0]; int flags = first==null ? 0 : (first.Uv0.HasValue ? 1 : 0) | (first.Normal.HasValue ? 2 : 0) | (first.Tangent.HasValue ? 4 : 0);
            int version=mesh.Faces.Count==0 ? 3 : mesh.IdWatermarks.SameAs(mesh.LiveIdMaxima) ? 1 : 2;
            int headerBytes=HeaderBytes+(version>=2 ? 24 : 0);
            long size = headerBytes + mesh.Vertices.Count * 20L + mesh.Faces.Count * 16L + mesh.Faces.Sum(f => (long)f.Corners.Count) * CornerBytes(flags);
            Checks.Require(size <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Polygon blob exceeds budget.");
            using (var stream = new MemoryStream((int)size)) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Magic); writer.Write(version); writer.Write(Encoding.ASCII.GetBytes(mesh.DomainId));
                writer.Write(mesh.Vertices.Count); writer.Write(mesh.Faces.Count); writer.Write(flags);
                if(version>=2) { writer.Write(mesh.IdWatermarks.Vertex);writer.Write(mesh.IdWatermarks.Face);writer.Write(mesh.IdWatermarks.Corner); }
                foreach (var vertex in mesh.Vertices.Values.OrderBy(v => v.Id)) { writer.Write(vertex.Id); MeshBinary.Write(writer, vertex.Position); }
                foreach (var face in mesh.Faces.OrderBy(f => f.Id))
                {
                    writer.Write(face.Id); writer.Write(face.Material); writer.Write(face.Corners.Count);
                    foreach (var corner in face.Corners)
                    {
                        writer.Write(corner.Id); writer.Write(corner.VertexId);
                        if (corner.Uv0.HasValue) { writer.Write(Checks.Canonical(corner.Uv0.Value.X)); writer.Write(Checks.Canonical(corner.Uv0.Value.Y)); }
                        if (corner.Normal.HasValue) MeshBinary.Write(writer, corner.Normal.Value);
                        if (corner.Tangent.HasValue) { var v = corner.Tangent.Value; writer.Write(Checks.Canonical(v.X)); writer.Write(Checks.Canonical(v.Y)); writer.Write(Checks.Canonical(v.Z)); writer.Write(Checks.Canonical(v.W)); }
                    }
                }
                return stream.ToArray();
            }
        }
        static int CornerBytes(int flags) => 16 + ((flags & 1) != 0 ? 8 : 0) + ((flags & 2) != 0 ? 12 : 0) + ((flags & 4) != 0 ? 16 : 0);
        internal static PolygonMesh Read(byte[] bytes)
        {
            Checks.Require(bytes != null && bytes.Length >= HeaderBytes && bytes.Length <= AuthoringLimits.MaxBlobBytes, "INVALID_BLOB", "Invalid polygon blob length.");
            try
            {
                using (var stream = new MemoryStream(bytes, false)) using (var reader = new BinaryReader(stream))
                {
                    Checks.Require(reader.ReadInt32()==Magic,"UNSUPPORTED_FORMAT","Unsupported polygon blob.");
                    int version=reader.ReadInt32();Checks.Require(version>=1 && version<=3,"UNSUPPORTED_FORMAT","Unsupported polygon blob.");
                    string domain = Encoding.ASCII.GetString(reader.ReadBytes(36)); Checks.Id(domain);
                    int vertices = reader.ReadInt32(), faces = reader.ReadInt32(), flags = reader.ReadInt32();
                    Checks.Require(vertices >= (version==3 ? 0 : 3) && vertices <= AuthoringLimits.MaxVertices && faces >= (version==3 ? 0 : 1) && faces <= AuthoringLimits.MaxIndices / 3,
                        "BUDGET_EXCEEDED", "Polygon counts exceed budget.");
                    Checks.Require(flags >= 0 && flags <= 7, "INVALID_BLOB", "Unknown polygon attribute flags.");
                    Checks.Require(version!=3 || faces==0 && flags==0,"INVALID_BLOB","Version 3 represents faceless polygons without corner attributes.");
                    var watermarks=version>=2 ? new PolygonIdWatermarks(reader.ReadUInt64(),reader.ReadUInt64(),reader.ReadUInt64()) : null;
                    int headerBytes=HeaderBytes+(version>=2 ? 24 : 0);
                    // Validate the complete variable-length envelope before constructing elements.
                    long endVertices = headerBytes + vertices * 20L;
                    Checks.Require(endVertices <= bytes.Length, "INVALID_BLOB", "Truncated polygon vertices.");
                    stream.Position = endVertices; long totalCorners = 0;
                    for (int i = 0; i < faces; i++)
                    {
                        reader.ReadUInt64(); reader.ReadInt32(); int count = reader.ReadInt32(); totalCorners += count;
                        Checks.Require(count >= 3 && count <= 256 && totalCorners <= AuthoringLimits.MaxIndices, "BUDGET_EXCEEDED", "Polygon corner count exceeds budget.");
                        long next = stream.Position + count * (long)CornerBytes(flags);
                        Checks.Require(next <= bytes.Length, "INVALID_BLOB", "Truncated polygon corners."); stream.Position = next;
                    }
                    Checks.Require(stream.Position == bytes.Length, "INVALID_BLOB", "Trailing polygon bytes.");
                    stream.Position = headerBytes;
                    var points = new List<CageVertex>(vertices); var polygons = new List<CageFace>(faces);
                    for (int i = 0; i < vertices; i++) points.Add(new CageVertex(reader.ReadUInt64(), MeshBinary.ReadVector(reader)));
                    for (int i = 0; i < faces; i++)
                    {
                        ulong id = reader.ReadUInt64(); int material = reader.ReadInt32(), count = reader.ReadInt32(); var corners = new CageCorner[count];
                        for (int j = 0; j < count; j++)
                        {
                            ulong corner = reader.ReadUInt64(), vertex = reader.ReadUInt64();
                            Vec2? uv = (flags & 1) == 0 ? (Vec2?)null : new Vec2(reader.ReadSingle(), reader.ReadSingle());
                            Vec3? normal = (flags & 2) == 0 ? (Vec3?)null : MeshBinary.ReadVector(reader);
                            Vec4? tangent = (flags & 4) == 0 ? (Vec4?)null : new Vec4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                            corners[j] = new CageCorner(corner, vertex, uv, normal, tangent);
                        }
                        polygons.Add(new CageFace(id, material, corners));
                    }
                    var mesh=new PolygonMesh(domain,points,polygons,watermarks);
                    Checks.Require(watermarks==null || watermarks.SameAs(mesh.IdWatermarks),"INVALID_BLOB","Polygon allocation history precedes live IDs.");
                    return mesh;
                }
            }
            catch (EndOfStreamException) { throw new AuthoringException("INVALID_BLOB", "Truncated polygon blob."); }
        }
    }
}

