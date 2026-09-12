using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Graph
{
    /// <summary>Conservative downstream dependency query, including disconnected output branches.
    /// Reports retained images potentially affected by an edit, not a prediction of UV equality.</summary>
    public static class PaintDependencies
    {
        public static IReadOnlyList<string> DownstreamImages(AuthoringGraph graph, string editedNode)
        {
            Checks.Require(graph != null && graph.Nodes.ContainsKey(editedNode), "NODE_NOT_FOUND", "Select an existing editing node.");
            var outgoing = graph.Edges.ToLookup(e => e.FromNode, e => e.ToNode, StringComparer.Ordinal);
            var visited = new HashSet<string>(StringComparer.Ordinal) { editedNode };
            var pending = new Queue<string>(); pending.Enqueue(editedNode);
            var affected = new List<string>();
            while (pending.Count != 0)
                foreach (string next in outgoing[pending.Dequeue()])
                {
                    if (!visited.Add(next)) continue;
                    var node = graph.Nodes[next];
                    if ((node.TypeId == BuiltinNodes.Paint || node.TypeId == BuiltinNodes.LayeredPaint) && node.Version == 1 && node.PaintImage != null) affected.Add(next);
                    pending.Enqueue(next);
                }
            affected.Sort(StringComparer.Ordinal);
            return affected.AsReadOnly();
        }
    }
}
