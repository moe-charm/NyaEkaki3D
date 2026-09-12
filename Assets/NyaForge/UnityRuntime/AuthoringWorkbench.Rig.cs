using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Foldout rigPanel;
        Label rigStatus;
        Button assignRootWeights;
        DropdownField rigBoneChoice;
        FloatField rigWeightField, rigPoseX, rigPoseY, rigPoseDegrees;
        FloatField rigMoveX, rigMoveY, rigMoveZ;
        Button setWeightButton, setPoseButton, moveBoneButton, rebindRigButton;
        Toggle rigWeightPaint;
        FloatField rigBrushRadius;
        bool rigPainting;
        readonly HashSet<int> rigStrokeVertices = new HashSet<int>();
        readonly List<string> rigBoneIds = new List<string>();

        void BuildRig(VisualElement parent)
        {
            rigPanel = new Foldout { text = "4  Rig / weight編集", value = false, name = "rig-panel" };
            rigStatus = new Label { name = "rig-status" }; rigStatus.style.whiteSpace = WhiteSpace.Normal;
            rigPanel.Add(rigStatus);
            rigBoneChoice = new DropdownField("対象bone", new List<string> { "なし" }, 0) { name = "rig-bone-choice" }; rigPanel.Add(rigBoneChoice);
            rigWeightField = new FloatField("weight (0〜1)") { value = 1f, name = "rig-weight" }; rigWeightField.style.minHeight = 36; rigPanel.Add(rigWeightField);
            setWeightButton = Button("選択頂点へweightを適用", SetSelectedWeight, "rig-set-weight"); rigPanel.Add(setWeightButton);
            rigWeightPaint = new Toggle("weight paintモード") { value = false, name = "rig-weight-paint" }; rigPanel.Add(rigWeightPaint);
            rigBrushRadius = new FloatField("ブラシ半径 (px)") { value = 32, name = "rig-brush-radius" }; rigBrushRadius.style.minHeight = 36; rigPanel.Add(rigBrushRadius);
            rigPoseX = new FloatField("pose X回転 (度)") { value = 0, name = "rig-pose-x" }; rigPoseX.style.minHeight = 36; rigPanel.Add(rigPoseX);
            rigPoseY = new FloatField("pose Y回転 (度)") { value = 0, name = "rig-pose-y" }; rigPoseY.style.minHeight = 36; rigPanel.Add(rigPoseY);
            rigPoseDegrees = new FloatField("pose Z回転 (度)") { value = 0, name = "rig-pose-degrees" }; rigPoseDegrees.style.minHeight = 36; rigPanel.Add(rigPoseDegrees);
            setPoseButton = Button("選択boneのposeを適用", SetSelectedPose, "rig-set-pose"); rigPanel.Add(setPoseButton);
            rigMoveX = new FloatField("bone head/tail移動 X (mm)") { value = 0, name = "rig-move-x" }; rigMoveX.style.minHeight = 36; rigPanel.Add(rigMoveX);
            rigMoveY = new FloatField("bone head/tail移動 Y (mm)") { value = 0, name = "rig-move-y" }; rigMoveY.style.minHeight = 36; rigPanel.Add(rigMoveY);
            rigMoveZ = new FloatField("bone head/tail移動 Z (mm)") { value = 0, name = "rig-move-z" }; rigMoveZ.style.minHeight = 36; rigPanel.Add(rigMoveZ);
            moveBoneButton = Button("選択boneのrest位置を移動", MoveSelectedBone, "rig-move-bone"); rigPanel.Add(moveBoneButton);
            rebindRigButton = Button("新しいrest骨へbinding / poseを再bind", RebindRig, "rig-rebind"); rigPanel.Add(rebindRigButton);
            assignRootWeights = Button("選択頂点をRootへ100%", AssignSelectedToRoot, "rig-assign-root"); rigPanel.Add(assignRootWeights);
            rigPanel.Add(new Label("weightは最大4本・合計1へ正規化。rest骨を動かすと依存assetはstaleになり、再bindが必要です。"));
            parent.Add(rigPanel);
        }

        void RefreshRig()
        {
            if (rigPanel == null) return;
            if (!TryResolveRig(out var bindingNode, out var mesh, out var skeleton))
            {
                rigStatus.text = "Rigサンプルまたはskin-bind nodeを追加するとweightを編集できます。";
                rigBoneIds.Clear(); rigBoneChoice.choices = new List<string> { "なし" }; rigBoneChoice.SetValueWithoutNotify("なし");
                assignRootWeights.SetEnabled(false); setWeightButton.SetEnabled(false); rigWeightPaint.SetEnabled(false); rigWeightPaint.SetValueWithoutNotify(false); rigBrushRadius.SetEnabled(false); setPoseButton.SetEnabled(false); moveBoneButton.SetEnabled(false); rebindRigButton.SetEnabled(false); return;
            }
            rigBoneIds.Clear(); rigBoneIds.AddRange(skeleton.Bones.Select(bone => bone.BoneId));
            var labels = skeleton.Bones.Select(bone => bone.Name + " · " + bone.BoneId.Substring(0, 8)).ToList();
            int selectedBone = rigBoneIds.IndexOf(SelectedBoneId()); if (selectedBone < 0) selectedBone = 0;
            rigBoneChoice.choices = labels; rigBoneChoice.SetValueWithoutNotify(labels[selectedBone]);
            var evaluated = workspace.Preview.Evaluation.SkinBindingOutputs.TryGetValue(bindingNode.NodeId, out var value) ? value.Binding : bindingNode.Binding;
            rigStatus.text = "骨 " + skeleton.Bones.Count + "本 · weight " + evaluated.Weights.Count + "頂点 · 選択 " + selection.Count + "頂点";
            bool weightReady = selection.Count > 0 && mesh != null && skeleton.Bones.Count > 0;
            bool paintReady = mesh != null && skeleton.Bones.Count > 0;
            bool bindingFresh = true; try { bindingNode.Binding.ValidateFor(mesh, skeleton); } catch (AuthoringException) { bindingFresh = false; }
            bool poseAvailable = TryResolvePose(out var poseNode, out var poseSkeleton); bool poseFresh = poseAvailable;
            if (poseAvailable) try { poseNode.Pose.ValidateFor(poseSkeleton); } catch (AuthoringException) { poseFresh = false; }
            if (!bindingFresh || !poseFresh) rigStatus.text += " · stale: 再bindが必要";
            assignRootWeights.SetEnabled(weightReady && bindingFresh); setWeightButton.SetEnabled(weightReady && bindingFresh);
            rigWeightPaint.SetEnabled(paintReady && bindingFresh); if (!bindingFresh) rigWeightPaint.SetValueWithoutNotify(false);
            rigBrushRadius.SetEnabled(paintReady && bindingFresh);
            setPoseButton.SetEnabled(skeleton.Bones.Count > 0 && poseAvailable);
            moveBoneButton.SetEnabled(skeleton.Bones.Count > 0); rebindRigButton.SetEnabled(!bindingFresh || !poseFresh);
        }

        bool TryResolveRig(out GraphNode bindingNode, out MeshData mesh, out SkeletonDefinition skeleton)
        {
            bindingNode = null; mesh = null; skeleton = null;
            if (workspace == null || workspace.Document.IsEmpty) return false;
            var graph = workspace.Document.ActiveObject.Graph;
            bindingNode = graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.SkinBind && node.Binding != null);
            if (bindingNode == null) return false;
            if (!workspace.Preview.Evaluation.MeshInputs.TryGetValue(bindingNode.NodeId, out var meshValue) || meshValue.Mesh == null) return false;
            mesh = meshValue.Mesh;
            string bindingNodeId = bindingNode.NodeId;
            var edge = graph.Edges.FirstOrDefault(item => item.ToNode == bindingNodeId && item.ToPort == "skeleton");
            if (edge == null || !workspace.Preview.Evaluation.SkeletonOutputs.TryGetValue(edge.FromNode, out var skeletonValue)) return false;
            skeleton = skeletonValue.Skeleton; return true;
        }

        void AssignSelectedToRoot()
        {
            Try(AssignSelectedToRootCore);
        }

        void AssignSelectedToRootCore()
        {
            GraphNode bindingNode; MeshData mesh; SkeletonDefinition skeleton;
            if (!TryResolveRig(out bindingNode, out mesh, out skeleton)) throw new InvalidOperationException("skin-bind nodeのmeshとskeleton入力を接続してください。");
            var rootBone = skeleton.Bones.FirstOrDefault(bone => string.IsNullOrEmpty(bone.ParentBoneId));
            if (rootBone == null) throw new InvalidOperationException("Root boneがありません。");
            var changed = SkinBindingEditing.AssignVertices(bindingNode.Binding, mesh, skeleton, selection, rootBone.BoneId);
            Execute(AuthoringOperation.UpdateNode(GraphNode.SkinBindNode(bindingNode.NodeId, changed)));
        }

        string SelectedBoneId()
        {
            int index = rigBoneChoice == null ? -1 : rigBoneChoice.index;
            return index >= 0 && index < rigBoneIds.Count ? rigBoneIds[index] : "";
        }

        void SetSelectedWeight()
        {
            Try(() =>
            {
                GraphNode bindingNode; MeshData mesh; SkeletonDefinition skeleton;
                if (!TryResolveRig(out bindingNode, out mesh, out skeleton)) throw new InvalidOperationException("skin-bind nodeのmeshとskeleton入力を接続してください。");
                string boneId = SelectedBoneId(); if (boneId == "") throw new InvalidOperationException("対象boneを選択してください。");
                var changed = SkinBindingEditing.SetVerticesWeight(bindingNode.Binding, mesh, skeleton, selection, boneId, rigWeightField.value);
                Execute(AuthoringOperation.UpdateNode(GraphNode.SkinBindNode(bindingNode.NodeId, changed)));
            });
        }

        internal bool RigWeightPaintActive => rigWeightPaint != null && rigWeightPaint.value && rigWeightPaint.enabledSelf;

        internal void BeginRigWeightStroke(Vector2 panelPosition)
        {
            rigPainting = true; rigStrokeVertices.Clear(); PaintRigAt(panelPosition);
        }

        internal void UpdateRigWeightStroke(Vector2 panelPosition)
        {
            if (rigPainting) PaintRigAt(panelPosition);
        }

        internal void EndRigWeightStroke()
        {
            if (!rigPainting) return; rigPainting = false;
            if (rigStrokeVertices.Count == 0) return;
            Try(CommitRigWeightStroke);
        }

        void PaintRigAt(Vector2 panelPosition)
        {
            if (projection == null || rigBrushRadius == null) return;
            float radius = Math.Max(4, rigBrushRadius.value); var points = projection.Points;
            for (int i = 0; i < points.Length; i++)
            {
                var projected = camera.WorldToViewportPoint(points[i]);
                if (projected.z > 0 && Vector2.Distance(VertexPanelPoint(points[i]), panelPosition) <= radius) rigStrokeVertices.Add(i);
            }
            rigStatus.text = "weight paint中 · " + rigStrokeVertices.Count + "頂点";
        }

        void CommitRigWeightStroke()
        {
            GraphNode bindingNode; MeshData mesh; SkeletonDefinition skeleton;
            if (!TryResolveRig(out bindingNode, out mesh, out skeleton)) throw new InvalidOperationException("skin-bind nodeのmeshとskeleton入力を接続してください。");
            string boneId = SelectedBoneId(); if (boneId == "") throw new InvalidOperationException("対象boneを選択してください。");
            var changed = SkinBindingEditing.SetVerticesWeight(bindingNode.Binding, mesh, skeleton, rigStrokeVertices, boneId, rigWeightField.value);
            selection.Clear(); selection.UnionWith(rigStrokeVertices);
            Execute(AuthoringOperation.UpdateNode(GraphNode.SkinBindNode(bindingNode.NodeId, changed)));
            rigStrokeVertices.Clear();
        }

        bool TryResolvePose(out GraphNode poseNode, out SkeletonDefinition skeleton)
        {
            poseNode = null; skeleton = null;
            if (workspace == null || workspace.Document.IsEmpty) return false;
            var graph = workspace.Document.ActiveObject.Graph;
            poseNode = graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.Pose && node.Pose != null);
            if (poseNode == null) return false;
            string poseNodeId = poseNode.NodeId;
            var edge = graph.Edges.FirstOrDefault(item => item.ToNode == poseNodeId && item.ToPort == "skeleton");
            return edge != null && workspace.Preview.Evaluation.SkeletonOutputs.TryGetValue(edge.FromNode, out var value) && (skeleton = value.Skeleton) != null;
        }

        void SetSelectedPose()
        {
            Try(() =>
            {
                GraphNode poseNode; SkeletonDefinition skeleton;
                if (!TryResolvePose(out poseNode, out skeleton)) throw new InvalidOperationException("pose nodeへskeletonを接続してください。");
                string boneId = SelectedBoneId(); if (boneId == "") throw new InvalidOperationException("対象boneを選択してください。");
                var changed = PoseEditing.SetRotationEuler(poseNode.Pose, skeleton, boneId, rigPoseX.value, rigPoseY.value, rigPoseDegrees.value);
                Execute(AuthoringOperation.UpdateNode(GraphNode.PoseNode(poseNode.NodeId, changed)));
            });
        }

        void MoveSelectedBone()
        {
            Try(() =>
            {
                GraphNode bindingNode; MeshData mesh; SkeletonDefinition skeleton;
                if (!TryResolveRig(out bindingNode, out mesh, out skeleton)) throw new InvalidOperationException("skin-bind nodeからskeletonを解決できません。");
                string boneId = SelectedBoneId(); if (boneId == "") throw new InvalidOperationException("対象boneを選択してください。");
                var changed = SkeletonEditing.MoveBone(skeleton, boneId, new Vec3(rigMoveX.value / 1000, rigMoveY.value / 1000, rigMoveZ.value / 1000), new Vec3(rigMoveX.value / 1000, rigMoveY.value / 1000, rigMoveZ.value / 1000));
                var graph = workspace.Document.ActiveObject.Graph; string bindingId = bindingNode.NodeId;
                var edge = graph.Edges.FirstOrDefault(item => item.ToNode == bindingId && item.ToPort == "skeleton");
                if (edge == null || !graph.Nodes.TryGetValue(edge.FromNode, out var skeletonNode)) throw new InvalidOperationException("skeleton nodeが見つかりません。");
                Execute(AuthoringOperation.UpdateNode(GraphNode.SkeletonNode(skeletonNode.NodeId, changed)));
            });
        }

        void RebindRig()
        {
            Try(() =>
            {
                GraphNode bindingNode; MeshData mesh; SkeletonDefinition skeleton;
                if (!TryResolveRig(out bindingNode, out mesh, out skeleton)) throw new InvalidOperationException("skin-bind nodeのmeshとskeleton入力を接続してください。");
                var operations = new List<AuthoringOperation> { AuthoringOperation.UpdateNode(GraphNode.SkinBindNode(bindingNode.NodeId, SkinBindingEditing.Rebind(bindingNode.Binding, mesh, skeleton))) };
                GraphNode poseNode; SkeletonDefinition poseSkeleton;
                if (TryResolvePose(out poseNode, out poseSkeleton)) operations.Add(AuthoringOperation.UpdateNode(GraphNode.PoseNode(poseNode.NodeId, PoseEditing.Rebind(poseNode.Pose, skeleton))));
                Execute(operations.ToArray());
            });
        }
    }
}
