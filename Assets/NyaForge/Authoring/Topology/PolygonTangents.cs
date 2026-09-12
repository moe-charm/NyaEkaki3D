using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    public static class PolygonTangents
    {
        public static PolygonMesh Recalculate(PolygonMesh mesh, ISet<ulong> faceIds)
        {
            var faces = mesh.Faces.Select(face =>
            {
                if (!faceIds.Contains(face.Id) || !face.Corners[0].Tangent.HasValue) return face;
                var tangents = new Vec3[face.Corners.Count]; var bitangents = new Vec3[face.Corners.Count];
                var triangles = PolygonTriangulator.Triangulate(mesh, face);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int a = triangles[i], b = triangles[i+1], c = triangles[i+2];
                    var ca = face.Corners[a]; var cb = face.Corners[b]; var cc = face.Corners[c];
                    Checks.Require(ca.Uv0.HasValue && cb.Uv0.HasValue && cc.Uv0.HasValue,"UV_MISSING","Tangent reconstruction requires UVs.");
                    var p = mesh.Vertices[ca.VertexId].Position;
                    var e1 = mesh.Vertices[cb.VertexId].Position-p; var e2 = mesh.Vertices[cc.VertexId].Position-p;
                    double u1 = (double)cb.Uv0.Value.X-ca.Uv0.Value.X, v1 = (double)cb.Uv0.Value.Y-ca.Uv0.Value.Y;
                    double u2 = (double)cc.Uv0.Value.X-ca.Uv0.Value.X, v2 = (double)cc.Uv0.Value.Y-ca.Uv0.Value.Y;
                    double d = u1*v2-u2*v1;
                    Checks.Require(Math.Abs(d)>1e-20,"DEGENERATE_UV","UV triangle has no tangent direction.");
                    var t = e1*(float)(v2/d)-e2*(float)(v1/d);
                    var bt = e2*(float)(u1/d)-e1*(float)(u2/d);
                    foreach(int index in new[]{a,b,c}) { tangents[index]+=t; bitangents[index]+=bt; }
                }
                var geometric = PolygonExtrusion.FaceNormal(mesh,face);
                return new CageFace(face.Id,face.Material,face.Corners.Select((corner,i)=>
                {
                    var n = Unit(corner.Normal ?? geometric); var t = Unit(tangents[i]-n*Dot(n,tangents[i]));
                    float w = Dot(Cross(n,t),bitangents[i]) < 0 ? -1 : 1;
                    return new CageCorner(corner.Id,corner.VertexId,corner.Uv0,corner.Normal,new Vec4(t.X,t.Y,t.Z,w));
                }));
            }).ToArray();
            return new PolygonMesh(mesh.DomainId,mesh.Vertices.Values,faces,mesh.IdWatermarks);
        }
        static float Dot(Vec3 a,Vec3 b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z;
        static Vec3 Cross(Vec3 a,Vec3 b)=>new Vec3(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
        static Vec3 Unit(Vec3 v)
        {
            double length=Math.Sqrt((double)v.X*v.X+(double)v.Y*v.Y+(double)v.Z*v.Z);
            Checks.Require(length>1e-12 && length<=float.MaxValue,"INVALID_UV_TRANSFORM","Invalid tangent frame.");
            return v*(float)(1/length);
        }
    }
}
