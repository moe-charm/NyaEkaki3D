using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    internal static class PolygonTriangulator
    {
        const double Epsilon = 1e-12;
        readonly struct Point { internal readonly double X, Y; internal Point(double x, double y) { X = x; Y = y; } }
        static double Cross(Point a, Point b, Point c) => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
        internal static int[] Triangulate(PolygonMesh mesh, CageFace face)
        {
            var vertices = face.Corners.Select(c => mesh.Vertices[c.VertexId].Position).ToArray();
            double nx = 0, ny = 0, nz = 0;
            for (int i = 0; i < vertices.Length; i++)
            {
                var a = vertices[i]; var b = vertices[(i + 1) % vertices.Length];
                nx += ((double)a.Y - b.Y) * ((double)a.Z + b.Z);
                ny += ((double)a.Z - b.Z) * ((double)a.X + b.X);
                nz += ((double)a.X - b.X) * ((double)a.Y + b.Y);
            }
            int axis = Math.Abs(nx) >= Math.Abs(ny) && Math.Abs(nx) >= Math.Abs(nz) ? 0 : Math.Abs(ny) >= Math.Abs(nz) ? 1 : 2;
            var points = vertices.Select(v => axis == 0 ? new Point(v.Y, v.Z) : axis == 1 ? new Point(v.Z, v.X) : new Point(v.X, v.Y)).ToArray();
            double minX = points.Min(p => p.X), minY = points.Min(p => p.Y);
            double extent = Math.Max(points.Max(p => p.X) - minX, points.Max(p => p.Y) - minY);
            Checks.Require(extent > 0, "DEGENERATE_FACE", "Face has no projected area.");
            points = points.Select(p => new Point((p.X - minX) / extent, (p.Y - minY) / extent)).ToArray();
            for (int i = 0; i < points.Length; i++)
                for (int j = i + 1; j < points.Length; j++)
                {
                    int ni = (i + 1) % points.Length, nj = (j + 1) % points.Length;
                    if (ni == j || nj == i) continue;
                    Checks.Require(!Intersects(points[i], points[ni], points[j], points[nj]), "SELF_INTERSECTING_FACE", "Face boundary crosses or touches itself in projection.");
                }
            double area = 0;
            for (int i = 0; i < points.Length; i++) { var a = points[i]; var b = points[(i + 1) % points.Length]; area += a.X * b.Y - b.X * a.Y; }
            Checks.Require(Math.Abs(area) > Epsilon, "DEGENERATE_FACE", "Face has no usable projected area.");
            double sign = Math.Sign(area); var remaining = Enumerable.Range(0, points.Length).ToList(); var triangles = new List<int>();
            while (remaining.Count > 3)
            {
                bool found = false;
                for (int i = 0; i < remaining.Count; i++)
                {
                    int a = remaining[(i + remaining.Count - 1) % remaining.Count], b = remaining[i], c = remaining[(i + 1) % remaining.Count];
                    if (sign * Cross(points[a], points[b], points[c]) <= Epsilon) continue;
                    bool occupied = remaining.Any(p => p != a && p != b && p != c &&
                        sign * Cross(points[a], points[b], points[p]) >= -Epsilon && sign * Cross(points[b], points[c], points[p]) >= -Epsilon && sign * Cross(points[c], points[a], points[p]) >= -Epsilon);
                    if (occupied) continue;
                    triangles.AddRange(new[] { a, b, c }); remaining.RemoveAt(i); found = true; break;
                }
                Checks.Require(found, "TRIANGULATION_FAILED", "Face cannot be triangulated without degeneracy.");
            }
            Checks.Require(sign * Cross(points[remaining[0]], points[remaining[1]], points[remaining[2]]) > Epsilon, "DEGENERATE_FACE", "Final triangle is degenerate.");
            triangles.AddRange(remaining); return triangles.ToArray();
        }
        static bool Intersects(Point a, Point b, Point c, Point d)
        {
            double abC = Cross(a, b, c), abD = Cross(a, b, d), cdA = Cross(c, d, a), cdB = Cross(c, d, b);
            if ((abC > Epsilon && abD < -Epsilon || abC < -Epsilon && abD > Epsilon) && (cdA > Epsilon && cdB < -Epsilon || cdA < -Epsilon && cdB > Epsilon)) return true;
            return Math.Abs(abC) <= Epsilon && On(a, b, c) || Math.Abs(abD) <= Epsilon && On(a, b, d) || Math.Abs(cdA) <= Epsilon && On(c, d, a) || Math.Abs(cdB) <= Epsilon && On(c, d, b);
        }
        static bool On(Point a, Point b, Point p) => p.X >= Math.Min(a.X, b.X) - Epsilon && p.X <= Math.Max(a.X, b.X) + Epsilon && p.Y >= Math.Min(a.Y, b.Y) - Epsilon && p.Y <= Math.Max(a.Y, b.Y) + Epsilon;
    }
}
