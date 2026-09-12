using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class GraphCanvas
    {
        internal IEnumerator VerifyPointerEvents(Action<string> completed)
        {
            string plane = null, editNode = null, output = null, state = null;
            Vector2 origin = default, pointer = default; Label title = null;
            Func<string, VisualElement> card = id => cards.Q<VisualElement>("graph-node-" + id);
            Func<string, string, Button> button = (id, text) => card(id).Query<Button>().ToList().Single(b => b.text == text);
            var steps = new List<Action>
            {
                () => PointerProbe.Click(this.Q<Button>("graph-add-Plane")),
                () => PointerProbe.Click(this.Q<Button>("graph-add-EditMesh")),
                () => PointerProbe.Click(this.Q<Button>("graph-add-Output")),
                () => { plane = Graph.Nodes.Values.Single(n => n.TypeId == BuiltinNodes.Plane).NodeId; editNode = Graph.Nodes.Values.Single(n => n.TypeId == BuiltinNodes.EditMesh).NodeId; output = Graph.Nodes.Values.Single(n => n.TypeId == BuiltinNodes.Output).NodeId; scroller.ScrollTo(ports["out:" + plane + ":mesh"]); },
                () => PointerProbe.Click(ports["out:" + plane + ":mesh"]),
                () => scroller.ScrollTo(ports["in:" + editNode + ":mesh"]),
                () => PointerProbe.Click(ports["in:" + editNode + ":mesh"]),
                () => scroller.ScrollTo(ports["out:" + editNode + ":mesh"]),
                () => PointerProbe.Click(ports["out:" + editNode + ":mesh"]),
                () => scroller.ScrollTo(ports["in:" + output + ":mesh"]),
                () => PointerProbe.Click(ports["in:" + output + ":mesh"]),
                () => scroller.ScrollTo(button(output, "最終出力に指定")),
                () => PointerProbe.Click(button(output, "最終出力に指定")),
                () => { if (!workspace.Preview.IsComplete || Graph.Edges.Count != 2) throw new InvalidOperationException("Pointer connection sequence did not complete graph"); state = workspace.Document.StateHash; card(plane).Query<FloatField>().ToList()[0].value = .35f; scroller.ScrollTo(button(plane, "寸法を適用")); },
                () => { if (workspace.Document.StateHash != state) throw new InvalidOperationException("Field committed before Apply"); PointerProbe.Click(button(plane, "寸法を適用")); },
                () => { if (Math.Abs(Graph.Nodes[plane].Width - .35f) > .00001f) throw new InvalidOperationException("Pointer Apply lost field value"); scroller.ScrollTo(button(editNode, "この段を表示・編集")); },
                () => PointerProbe.Click(button(editNode, "この段を表示・編集")),
                () => { title = card(editNode).Q<Label>("node-title"); scroller.ScrollTo(title); },
                () => { origin = positions[LayoutKey(editNode)]; state = workspace.Document.StateHash; pointer = PointerProbe.Center(title); PointerProbe.Down(title, pointer); },
                () => PointerProbe.Move(title, pointer + new Vector2(40, 25)),
                () => PointerProbe.Up(title, pointer + new Vector2(40, 25)),
                () => { if (positions[LayoutKey(editNode)] != origin + new Vector2(40, 25) || workspace.Document.StateHash != state) throw new InvalidOperationException("Drag position/history mismatch"); var saved = GraphLayoutStore.Load(layoutDirectory, workspace.Document.DocumentId, Graph.GraphId); if (Math.Abs(saved[editNode].X - origin.x - 40) > .01f) throw new InvalidOperationException("Pointer drag did not persist layout"); }
            };
            foreach (var step in steps)
            {
                string failure = null;
                try { step(); } catch (Exception e) { failure = e.ToString(); }
                if (failure != null) { completed(failure); yield break; }
                yield return null; yield return null; yield return null;
            }
            completed(null);
        }
    }
}
