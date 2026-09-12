using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Import
{
    /// <summary>Applies source-space jointWorld × inverseBind palettes to a mesh without authored BoneId assumptions.</summary>
    public static class SourceSkinDeformer
    {
        public static MeshData Apply(MeshData mesh, SourceSkin skin, SourceSkinBinding binding,
            IReadOnlyList<SourceAffine> posedJointWorld)
        {
            Checks.Require(mesh != null && skin != null && binding != null && posedJointWorld != null, "INVALID_SKIN", "Source mesh, skin, binding and palette are required.");
            binding.ValidateFor(mesh, skin);
            Checks.Require(posedJointWorld.Count == skin.Joints.Count, "POSE_JOINT_COUNT", "Source pose palette must match joint slots exactly.");
            var matrices = new SourceAffine[mesh.VertexCount];
            var positions = new Vec3[mesh.VertexCount];
            for (int vertex = 0; vertex < mesh.VertexCount; vertex++)
            {
                var weights = binding.Weights[vertex];
                var candidates = weights.Select(weight => skin.JointMatrix(weight.JointSlot, posedJointWorld[weight.JointSlot])).ToArray();
                var amounts = weights.Select(weight => weight.Weight).ToArray();
                matrices[vertex] = SourceAffine.Blend(candidates, amounts);
                positions[vertex] = matrices[vertex].TransformPoint(mesh.Positions[vertex]);
            }
            var normals = mesh.Normals.Count == 0 ? Array.Empty<Vec3>() : mesh.Normals.Select((normal, i) => matrices[i].TransformNormal(normal)).ToArray();
            var tangents = mesh.Tangents.Count == 0 ? Array.Empty<Vec4>() : mesh.Tangents.Select((tangent, i) => matrices[i].TransformTangent(tangent, mesh.Normals[i])).ToArray();
            return new MeshData(positions, normals, tangents, mesh.Uv0.ToArray(), mesh.Submeshes.ToArray());
        }
    }
}
