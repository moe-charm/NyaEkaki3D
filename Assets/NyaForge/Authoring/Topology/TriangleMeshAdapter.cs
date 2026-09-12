using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    public static class TriangleMeshAdapter
    {
        /// <summary>Imports each source index as a distinct vertex. Does not infer welding, quads or seams.</summary>
        public static PolygonMesh Import(string domainId, MeshData source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var vertices = source.Positions.Select((p, i) => new CageVertex((ulong)i + 1, p));
            var faces = new List<CageFace>(); ulong faceId = 1, cornerId = 1;
            var submeshes = source.Submeshes;
            for (int material = 0; material < submeshes.Count; material++)
            {
                var indices = submeshes[material];
                for (int i = 0; i < indices.Length; i += 3)
                {
                    var corners = new CageCorner[3];
                    for (int j = 0; j < 3; j++)
                    {
                        int index = indices[i + j];
                        corners[j] = new CageCorner(cornerId++, (ulong)index + 1,
                            source.Uv0.Count == 0 ? (Vec2?)null : source.Uv0[index],
                            source.Normals.Count == 0 ? (Vec3?)null : source.Normals[index],
                            source.Tangents.Count == 0 ? (Vec4?)null : source.Tangents[index]);
                    }
                    faces.Add(new CageFace(faceId++, material, corners));
                }
            }
            return new PolygonMesh(domainId, vertices, faces);
        }
    }
}
