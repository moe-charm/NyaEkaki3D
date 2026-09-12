using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring.Topology
{
    public static class PolygonPrimitives
    {
        public static PolygonMesh Plane(string domainId, float width = .2f, float height = .1f)
        {
            var source = PrimitiveGeometry.Plane(width, height);
            var vertices = source.Positions.Select((p, i) => new CageVertex((ulong)i + 1, p));
            var corners = new[] { 0, 3, 2, 1 }.Select((v, i) => new CageCorner((ulong)i + 1, (ulong)v + 1, source.Uv0[v], source.Normals[v], source.Tangents[v]));
            return new PolygonMesh(domainId, vertices, new[] { new CageFace(1, 0, corners) });
        }

        /// <summary>Creates a small, topology-stable torus suitable as a choker/accessory starting point.</summary>
        public static PolygonMesh Choker(string domainId, float radius = .06f, float tubeRadius = .008f,
            int segments = 24, int tubeSegments = 8)
        {
            Checks.Id(domainId);
            Checks.Finite(radius); Checks.Finite(tubeRadius);
            Checks.Require(radius >= .01f && radius <= 10f, "PARAMETER_RANGE", "Choker radius must be between 0.01 and 10 metres.");
            Checks.Require(tubeRadius >= .001f && tubeRadius <= radius * .5f, "PARAMETER_RANGE", "Choker tube radius must be positive and at most half the ring radius.");
            Checks.Require(segments >= 8 && segments <= 128, "PARAMETER_RANGE", "Choker segments must be between 8 and 128.");
            Checks.Require(tubeSegments >= 4 && tubeSegments <= 32, "PARAMETER_RANGE", "Choker tube segments must be between 4 and 32.");

            var vertices = new List<CageVertex>(segments * tubeSegments);
            for (int i = 0; i < segments; i++)
            {
                float u = (float)i / segments;
                double angle = u * Math.PI * 2.0;
                float cosU = (float)Math.Cos(angle), sinU = (float)Math.Sin(angle);
                for (int j = 0; j < tubeSegments; j++)
                {
                    float v = (float)j / tubeSegments;
                    double crossAngle = v * Math.PI * 2.0;
                    float cosV = (float)Math.Cos(crossAngle), sinV = (float)Math.Sin(crossAngle);
                    float ring = radius + tubeRadius * cosV;
                    vertices.Add(new CageVertex((ulong)(i * tubeSegments + j + 1),
                        new Vec3(ring * cosU, tubeRadius * sinV, ring * sinU)));
                }
            }

            var faces = new List<CageFace>(segments * tubeSegments);
            ulong nextCorner = 1;
            for (int i = 0; i < segments; i++)
            {
                int nextI = (i + 1) % segments;
                float u0 = (float)i / segments, u1 = (float)(i + 1) / segments;
                double angle = u0 * Math.PI * 2.0;
                var tangent0 = new Vec4(-(float)Math.Sin(angle), 0, (float)Math.Cos(angle), 1);
                angle = u1 * Math.PI * 2.0;
                var tangent1 = new Vec4(-(float)Math.Sin(angle), 0, (float)Math.Cos(angle), 1);
                for (int j = 0; j < tubeSegments; j++)
                {
                    int nextJ = (j + 1) % tubeSegments;
                    float v0 = (float)j / tubeSegments, v1 = (float)(j + 1) / tubeSegments;
                    var corners = new[]
                    {
                        Corner(i, j, tubeSegments, u0, v0, tangent0, ref nextCorner),
                        Corner(i, nextJ, tubeSegments, u0, v1, tangent0, ref nextCorner),
                        Corner(nextI, nextJ, tubeSegments, u1, v1, tangent1, ref nextCorner),
                        Corner(nextI, j, tubeSegments, u1, v0, tangent1, ref nextCorner)
                    };
                    faces.Add(new CageFace((ulong)(i * tubeSegments + j + 1), 0, corners));
                }
            }
            return new PolygonMesh(domainId, vertices, faces);
        }

        static CageCorner Corner(int ring, int tube, int tubeSegments, float u, float v, Vec4 tangent, ref ulong cornerId)
        {
            float crossAngle = v * (float)(Math.PI * 2.0);
            float cosV = (float)Math.Cos(crossAngle), sinV = (float)Math.Sin(crossAngle);
            float angle = u * (float)(Math.PI * 2.0);
            var normal = new Vec3(cosV * (float)Math.Cos(angle), sinV, cosV * (float)Math.Sin(angle));
            return new CageCorner(cornerId++, (ulong)(ring * tubeSegments + tube + 1), new Vec2(u, v), normal, tangent);
        }
    }
}
