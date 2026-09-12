using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Graph
{
    internal static class GraphValidator
    {
        // Validate all nodes, including disconnected components, before evaluating any geometry.
        internal static IReadOnlyList<string> Order(AuthoringGraph graph)
        {
            var indegree = graph.Nodes.Keys.ToDictionary(id => id, _ => 0);
            var targets = graph.Nodes.Keys.ToDictionary(id => id, _ => new List<string>());
            var inputs = new HashSet<string>(StringComparer.Ordinal);
            foreach (var edge in graph.Edges)
            {
                Checks.Require(graph.Nodes.ContainsKey(edge.FromNode) && graph.Nodes.ContainsKey(edge.ToNode), "NODE_NOT_FOUND", "Edge references a missing node.");
                Checks.Require(inputs.Add(edge.ToNode + "/" + edge.ToPort), "INPUT_ALREADY_CONNECTED", "An input accepts one connection.");
                var source = BuiltinNodes.Find(graph.Nodes[edge.FromNode]);
                var target = BuiltinNodes.Find(graph.Nodes[edge.ToNode]);
                var output = source?.Outputs.FirstOrDefault(port => port.Id == edge.FromPort);
                var input = target?.Inputs.FirstOrDefault(port => port.Id == edge.ToPort);
                if (source != null) Checks.Require(output != null, "PORT_NOT_FOUND", "Unknown output port.");
                if (target != null) Checks.Require(input != null, "PORT_NOT_FOUND", "Unknown input port.");
                if (source != null && target != null) Checks.Require(output.Type == input.Type, "PORT_TYPE_MISMATCH", "Input and output types differ.");
                indegree[edge.ToNode]++; targets[edge.FromNode].Add(edge.ToNode);
            }
            var ready = new SortedSet<string>(indegree.Where(p => p.Value == 0).Select(p => p.Key), StringComparer.Ordinal);
            var order = new List<string>();
            while (ready.Count != 0)
            {
                string id = ready.Min; ready.Remove(id); order.Add(id);
                foreach (var target in targets[id]) if (--indegree[target] == 0) ready.Add(target);
            }
            Checks.Require(order.Count == graph.Nodes.Count, "GRAPH_CYCLE", "Graph contains a dependency cycle.");
            if (graph.OutputNodeId != "")
            {
                Checks.Require(graph.Nodes.ContainsKey(graph.OutputNodeId), "NODE_NOT_FOUND", "Output binding references a missing node.");
                var selected = graph.Nodes[graph.OutputNodeId];
                if (BuiltinNodes.Find(selected) != null)
                    Checks.Require(selected.TypeId == BuiltinNodes.Output, "INVALID_OUTPUT_BINDING", "Bind an Output node.");
            }
            return order.AsReadOnly();
        }
    }
}
