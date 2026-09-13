using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Geometry
{
    /// <summary>Nearest-triangle projection over an immutable mesh snapshot.</summary>
    public sealed class MeshSurfaceProjection
    {
        readonly Vec3[] vertices;
        readonly Triangle[] triangles;
        readonly int[] order;
        readonly List<Node> nodes = new List<Node>();
        readonly RestTransform transform;
        readonly int root;

        struct Triangle
        {
            internal int A, B, C;
            internal Vec3 Center, Min, Max;
        }

        struct Node
        {
            internal Vec3 Min, Max;
            internal int Start, Count, Left, Right;
        }

        public MeshSurfaceProjection(MeshData mesh, RestTransform placement)
        {
            Checks.Require(mesh != null, "MESH_MISSING", "A surface projection mesh is required.");
            placement.Validate();
            transform = placement;
            vertices = mesh.Positions.ToArray();
            triangles = new Triangle[mesh.TriangleCount];
            order = new int[triangles.Length];
            int triangle = 0;
            foreach (var indices in mesh.Submeshes)
            {
                for (int i = 0; i < indices.Length; i += 3)
                {
                    int ia = indices[i], ib = indices[i + 1], ic = indices[i + 2];
                    Vec3 a = vertices[ia], b = vertices[ib], c = vertices[ic];
                    triangles[triangle] = new Triangle
                    {
                        A = ia, B = ib, C = ic,
                        Center = (a + b + c) * (1f / 3f),
                        Min = Min(a, Min(b, c)), Max = Max(a, Max(b, c))
                    };
                    order[triangle] = triangle++;
                }
            }
            Checks.Require(triangles.Length > 0, "MESH_MISSING", "A surface projection mesh must contain triangles.");
            root = Build(0, triangles.Length);
        }

        public MeshSurfaceHit FindClosest(Vec3 avatarPoint)
        {
            Checks.Finite(avatarPoint);
            Vec3 local = new Vec3(
                (avatarPoint.X - transform.Translation.X) / transform.Scale,
                (avatarPoint.Y - transform.Translation.Y) / transform.Scale,
                (avatarPoint.Z - transform.Translation.Z) / transform.Scale);
            MeshSurfaceHit best = null;
            double nearest = double.PositiveInfinity;
            void Visit(int nodeIndex)
            {
                var node = nodes[nodeIndex];
                if (DistanceSquared(local, node.Min, node.Max) > nearest) return;
                if (node.Count == 0)
                {
                    double leftDistance = DistanceSquared(local, nodes[node.Left].Min, nodes[node.Left].Max);
                    double rightDistance = DistanceSquared(local, nodes[node.Right].Min, nodes[node.Right].Max);
                    if (leftDistance <= rightDistance) { Visit(node.Left); Visit(node.Right); }
                    else { Visit(node.Right); Visit(node.Left); }
                    return;
                }
                for (int i = node.Start; i < node.Start + node.Count; i++)
                {
                    int id = order[i];
                    var triangle = triangles[id];
                    var candidate = Closest(local, vertices[triangle.A], vertices[triangle.B], vertices[triangle.C]);
                    if (candidate.DistanceSquared < nearest ||
                        candidate.DistanceSquared == nearest && (best == null || id < best.TriangleIndex))
                    {
                        nearest = candidate.DistanceSquared;
                        best = new MeshSurfaceHit(id, triangle.A, triangle.B, triangle.C,
                            candidate.U, candidate.V, candidate.W,
                            candidate.DistanceSquared * transform.Scale * transform.Scale);
                    }
                }
            }
            Visit(root);
            Checks.Require(best != null, "MESH_MISSING", "A surface projection mesh must contain triangles.");
            return best;
        }

        int Build(int start, int count)
        {
            Vec3 min = triangles[order[start]].Min, max = triangles[order[start]].Max;
            for (int i = start + 1; i < start + count; i++)
            {
                min = Min(min, triangles[order[i]].Min);
                max = Max(max, triangles[order[i]].Max);
            }
            int index = nodes.Count;
            nodes.Add(new Node { Min = min, Max = max, Start = start, Count = count, Left = -1, Right = -1 });
            if (count <= 8) return index;
            Vec3 extent = max - min;
            int axis = extent.X >= extent.Y && extent.X >= extent.Z ? 0 : extent.Y >= extent.Z ? 1 : 2;
            Array.Sort(order, start, count, Comparer<int>.Create((a, b) =>
            {
                int result = Axis(triangles[a].Center, axis).CompareTo(Axis(triangles[b].Center, axis));
                return result != 0 ? result : a.CompareTo(b);
            }));
            int half = count / 2;
            int left = Build(start, half);
            int right = Build(start + half, count - half);
            nodes[index] = new Node { Min = min, Max = max, Start = start, Count = 0, Left = left, Right = right };
            return index;
        }

        static float Axis(Vec3 value, int axis) { return axis == 0 ? value.X : axis == 1 ? value.Y : value.Z; }
        static Vec3 Min(Vec3 a, Vec3 b) { return new Vec3(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z)); }
        static Vec3 Max(Vec3 a, Vec3 b) { return new Vec3(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z)); }
        static double DistanceSquared(Vec3 point, Vec3 min, Vec3 max)
        {
            double x = point.X < min.X ? min.X - point.X : point.X > max.X ? point.X - max.X : 0;
            double y = point.Y < min.Y ? min.Y - point.Y : point.Y > max.Y ? point.Y - max.Y : 0;
            double z = point.Z < min.Z ? min.Z - point.Z : point.Z > max.Z ? point.Z - max.Z : 0;
            return x * x + y * y + z * z;
        }

        static ClosestPoint Closest(Vec3 point, Vec3 a, Vec3 b, Vec3 c)
        {
            Vec3 ab = b - a, ac = c - a, ap = point - a;
            double d1 = Dot(ab, ap), d2 = Dot(ac, ap);
            if (d1 <= 0 && d2 <= 0) return Result(point, a, 1, 0, 0);
            Vec3 bp = point - b; double d3 = Dot(ab, bp), d4 = Dot(ac, bp);
            if (d3 >= 0 && d4 <= d3) return Result(point, b, 0, 1, 0);
            double vc = d1 * d4 - d3 * d2;
            if (vc <= 0 && d1 >= 0 && d3 <= 0)
            {
                double v = d1 / (d1 - d3);
                return Result(point, a + ab * (float)v, 1 - v, v, 0);
            }
            Vec3 cp = point - c; double d5 = Dot(ab, cp), d6 = Dot(ac, cp);
            if (d6 >= 0 && d5 <= d6) return Result(point, c, 0, 0, 1);
            double vb = d5 * d2 - d1 * d6;
            if (vb <= 0 && d2 >= 0 && d6 <= 0)
            {
                double w = d2 / (d2 - d6);
                return Result(point, a + ac * (float)w, 1 - w, 0, w);
            }
            double va = d3 * d6 - d5 * d4;
            if (va <= 0 && (d4 - d3) >= 0 && (d5 - d6) >= 0)
            {
                Vec3 bc = c - b;
                double w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                return Result(point, b + bc * (float)w, 0, 1 - w, w);
            }
            double denominator = 1.0 / (va + vb + vc);
            double vFace = vb * denominator, wFace = vc * denominator;
            return Result(point, a + ab * (float)vFace + ac * (float)wFace,
                1 - vFace - wFace, vFace, wFace);
        }

        static ClosestPoint Result(Vec3 point, Vec3 closest, double u, double v, double w)
        {
            Vec3 delta = point - closest;
            return new ClosestPoint { U = u, V = v, W = w, DistanceSquared = Dot(delta, delta) };
        }
        static double Dot(Vec3 a, Vec3 b) { return (double)a.X * b.X + (double)a.Y * b.Y + (double)a.Z * b.Z; }
        struct ClosestPoint { internal double U, V, W, DistanceSquared; }
    }

    public sealed class MeshSurfaceHit
    {
        public int TriangleIndex { get; }
        public int A { get; }
        public int B { get; }
        public int C { get; }
        public double U { get; }
        public double V { get; }
        public double W { get; }
        public double DistanceSquared { get; }
        internal MeshSurfaceHit(int triangle, int a, int b, int c, double u, double v, double w, double distanceSquared)
        {
            TriangleIndex = triangle; A = a; B = b; C = c; U = u; V = v; W = w; DistanceSquared = distanceSquared;
        }
    }
}