using System.Collections.Generic;

namespace NyaForge.Authoring
{
    // Pure candidate construction. Publication and history belong to the command service.
    internal static class StaticOperationEvaluator
    {
        internal static AuthoringDocument Apply(AuthoringDocument before, AuthoringOperation operation, long revision)
        {
            if (operation.Kind == "object.add_mesh")
                return before.AddMesh(operation.NewObjectId, operation.Mesh, operation.Transform, revision);
            Checks.Require(!before.IsEmpty, "NO_EDITABLE_OBJECT", "Add a mesh before editing.");
            var offsets = new Dictionary<int, Vec3>(before.Offsets);
            bool enabled = before.LayerEnabled;
            if (operation.Kind == "layer.enabled") enabled = operation.Enabled;
            else if (operation.Kind == "vertices.translate")
            {
                Checks.Require(operation.VertexIds.Count > 0 && operation.VertexIds.Count <= before.BaselineMesh.VertexCount, "INVALID_SELECTION", "Select at least one valid vertex.");
                Checks.Finite(operation.Delta); var seen = new HashSet<int>();
                foreach (int index in operation.VertexIds)
                {
                    Checks.Require(index >= 0 && index < before.BaselineMesh.VertexCount && seen.Add(index), "INVALID_VERTEX", "Vertex indices must be unique and in range.");
                    Vec3 old; offsets.TryGetValue(index, out old); Vec3 next = old + operation.Delta; Checks.Finite(next);
                    if (next.X == 0 && next.Y == 0 && next.Z == 0) offsets.Remove(index); else offsets[index] = next;
                }
            }
            else throw new AuthoringException("UNSUPPORTED_OPERATION", "Unsupported operation or mixed history command.");
            return before.Changed(revision, enabled, offsets);
        }
    }
}
