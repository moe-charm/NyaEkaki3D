using System;
using System.Linq;
using System.IO;
using UnityEngine;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class GraphCanvas
    {
        // Player verifier invokes the same handlers used by the buttons/ports.
        internal void VerifyEmptyGraphConstruction()
        {
            if (!workspace.Document.IsEmpty) throw new InvalidOperationException("Canvas verification requires empty workspace");
            AddNode("Plane"); AddNode("EditMesh"); AddNode("Output");
            var plane = Graph.Nodes.Values.Single(n => n.TypeId == BuiltinNodes.Plane);
            var editNode = Graph.Nodes.Values.Single(n => n.TypeId == BuiltinNodes.EditMesh);
            var output = Graph.Nodes.Values.Single(n => n.TypeId == BuiltinNodes.Output);
            sourceNode = plane.NodeId; sourcePort = "mesh"; Input(editNode.NodeId, "mesh");
            sourceNode = editNode.NodeId; sourcePort = "mesh"; Input(output.NodeId, "mesh");
            submit(new[] { AuthoringOperation.SetOutput(output.NodeId) });
            if (!workspace.Preview.IsComplete || cards.childCount != 3) throw new InvalidOperationException("Canvas connection handlers did not build graph");
            string state = workspace.Document.StateHash;
            sourceNode = editNode.NodeId; sourcePort = "mesh"; Input(plane.NodeId, "width");
            if (workspace.Document.StateHash != state) throw new InvalidOperationException("Canvas invalid connection changed document");
            sourceNode = sourcePort = null; ShowState(); edit(editNode.NodeId);
            if (this.Q<VisualElement>("graph-node-" + editNode.NodeId) == null) throw new InvalidOperationException("Canvas card missing");
        }

        internal void VerifyLayoutRoundtrip()
        {
            string node = Graph.Nodes.Keys.First(), key = LayoutKey(node), state = workspace.Document.StateHash;
            long revision = workspace.Document.DocumentRevision;
            positions[key] = new Vector2(410, 320); SaveLayout();
            var fresh = new GraphCanvas(); fresh.SetLayoutDirectory(layoutDirectory);
            fresh.Bind(workspace, submit, edit);
            if (fresh.positions[key] != new Vector2(410, 320)) throw new InvalidOperationException("Fresh canvas did not restore layout");
            if (workspace.Document.StateHash != state || workspace.Document.DocumentRevision != revision) throw new InvalidOperationException("Layout mutated authoring state");
            string path = GraphLayoutStore.FilePath(layoutDirectory, workspace.Document.DocumentId, Graph.GraphId);
            byte[] original = File.ReadAllBytes(path);
            try
            {
                File.WriteAllBytes(path, new byte[] { 1, 2, 3 });
                var damaged = new GraphCanvas(); damaged.SetLayoutDirectory(layoutDirectory); damaged.Bind(workspace, submit, edit);
                if (damaged.layoutWarning == null || damaged.cards.childCount != Graph.Nodes.Count)
                    throw new InvalidOperationException("Damaged layout prevented default graph display");
            }
            finally { File.WriteAllBytes(path, original); }
        }
    }
}
