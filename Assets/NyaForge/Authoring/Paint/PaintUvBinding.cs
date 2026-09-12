using System.IO;
using System.Linq;
using NyaForge.Authoring.Topology;

namespace NyaForge.Authoring.Paint
{
    public static class PaintUvBinding
    {
        public static string Hash(PolygonMesh mesh)
        {
            Checks.Require(mesh!=null,"UV_MISSING","Painting needs a polygon UV mapping.");
            return PolygonDerivedData.UvHash(mesh);
        }
        internal static string HashUncached(PolygonMesh mesh)
        {
            Checks.Require(mesh != null && mesh.Faces.All(f=>f.Corners.All(c=>c.Uv0.HasValue)),"UV_MISSING","Painting needs a polygon UV mapping.");
            using(var stream=new MemoryStream()) using(var writer=new BinaryWriter(stream))
            {
                writer.Write("paint-uv-v1");writer.Write(mesh.DomainId);writer.Write(mesh.Faces.Count);
                foreach(var face in mesh.Faces.OrderBy(f=>f.Id))
                {
                    writer.Write(face.Id);writer.Write(face.Material);writer.Write(face.Corners.Count);
                    foreach(var corner in face.Corners)
                    {
                        writer.Write(corner.Id);writer.Write(corner.VertexId);
                        writer.Write(Checks.Canonical(corner.Uv0.Value.X));writer.Write(Checks.Canonical(corner.Uv0.Value.Y));
                    }
                }
                return Checks.Hash(stream.ToArray());
            }
        }
        public static void RequireMatch(string expected,PolygonMesh mesh)
        {
            Checks.HashText(expected);
            Checks.Require(expected==Hash(mesh),"PAINT_UV_CHANGED","Paint is bound to a different UV mapping. Keep the old image or explicitly rebind/rebake it.");
        }
    }
}
