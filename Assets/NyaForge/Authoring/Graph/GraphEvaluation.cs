using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Graph
{
    public sealed class GraphMeshValue
    {
        public MeshData Mesh { get; }
        public RestTransform Transform { get; }
        public string DomainId { get; }
        public string SnapshotHash { get; }
        public GraphImageValue BaseColor { get; }
        public GraphMaterialValue Material { get; }
        public IReadOnlyDictionary<int,MaterialSlotBinding> SlotMaterials { get; }
        public NyaForge.Authoring.Topology.PolygonMesh Polygon { get; }
        public NyaForge.Authoring.Topology.PolygonRenderMesh PolygonRendering { get; }
        internal GraphMeshValue(MeshData mesh, RestTransform transform, string domain, NyaForge.Authoring.Topology.PolygonMesh polygon = null, NyaForge.Authoring.Topology.PolygonRenderMesh rendering = null, GraphImageValue baseColor = null, GraphMaterialValue material = null,IReadOnlyDictionary<int,MaterialSlotBinding> slotMaterials=null)
        {
            Checks.Require(mesh!=null || polygon!=null && polygon.Faces.Count==0,"INVALID_MESH","A missing render mesh requires a faceless polygon.");
            foreach (var position in mesh!=null ? mesh.Positions : polygon.Vertices.Values.Select(v=>v.Position)) Checks.Finite(transform.ToAvatarPoint(position));
            Mesh = mesh; Transform = transform; DomainId = domain;
            Polygon = polygon; PolygonRendering = rendering;
            BaseColor = baseColor;Material=material;SlotMaterials=slotMaterials;
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(mesh?.ContentHash ?? "polygon.faceless.v1"); writer.Write(domain);
                writer.Write(Checks.Canonical(transform.Scale)); MeshBinary.Write(writer, transform.Translation);
                if (polygon != null) writer.Write(NyaForge.Authoring.Topology.PolygonDerivedData.ContentHash(polygon));
                if (baseColor != null) { writer.Write(baseColor.ImageHash); writer.Write(baseColor.UvHash); writer.Write(baseColor.MeshDomain); }
                if (material != null) { writer.Write("material.standard.v1");writer.Write(material.Parameters.ContentHash); }
                if(slotMaterials!=null)
                {
                    writer.Write("material.slots.v1");writer.Write(slotMaterials.Count);
                    foreach(var pair in slotMaterials.OrderBy(p=>p.Key))
                    {
                        writer.Write(pair.Key);writer.Write(pair.Value.MaterialNodeId);writer.Write(pair.Value.Material.Parameters.ContentHash);
                        var image=pair.Value.Material.BaseColor;writer.Write(image!=null);
                        if(image!=null) { writer.Write(image.ImageHash);writer.Write(image.UvHash);writer.Write(image.MeshDomain); }
                    }
                }
                SnapshotHash = Checks.Hash(stream.ToArray());
            }
        }
        internal static GraphMeshValue Source(string nodeId, MeshData mesh, RestTransform transform)
        {
            string domain = Checks.Hash(Encoding.UTF8.GetBytes(nodeId + ":" + mesh.TopologyHash));
            return new GraphMeshValue(mesh, transform, domain);
        }

        /// <summary>Creates a value with replaced geometry while retaining graph identity and appearance.</summary>
        internal GraphMeshValue WithMesh(MeshData mesh)
        {
            return new GraphMeshValue(mesh, Transform, DomainId, Polygon, PolygonRendering, BaseColor, Material, SlotMaterials);
        }
    }

    public sealed class GraphDiagnostic
    {
        public string NodeId { get; }
        public string Code { get; }
        public string Message { get; }
        internal GraphDiagnostic(string node, string code, string message) { NodeId = node; Code = code; Message = message; }
    }

    public sealed class GraphEvaluation
    {
        public bool IsComplete { get { return Output != null && Diagnostics.Count == 0; } }
        public GraphMeshValue Output { get; }
        public IReadOnlyDictionary<string, GraphMeshValue> MeshOutputs { get; }
        public IReadOnlyDictionary<string, GraphMeshValue> MeshInputs { get; }
        public IReadOnlyDictionary<string, GraphImageValue> ImageOutputs { get; }
        public IReadOnlyDictionary<string, GraphMaterialValue> MaterialOutputs { get; }
        public IReadOnlyDictionary<string, GraphSkeletonValue> SkeletonOutputs { get; }
        public IReadOnlyDictionary<string, GraphSkinBindingValue> SkinBindingOutputs { get; }
        public IReadOnlyDictionary<string, GraphPoseValue> PoseOutputs { get; }
        public IReadOnlyDictionary<string, GraphMorphSetValue> MorphSetOutputs { get; }
        public IReadOnlyList<GraphDiagnostic> Diagnostics { get; }
        internal GraphEvaluation(GraphMeshValue output, Dictionary<string, GraphMeshValue> outputs,
            Dictionary<string, GraphMeshValue> inputs, List<GraphDiagnostic> diagnostics, Dictionary<string, GraphImageValue> images, Dictionary<string, GraphMaterialValue> materials, Dictionary<string, GraphSkeletonValue> skeletons, Dictionary<string, GraphSkinBindingValue> bindings, Dictionary<string, GraphPoseValue> poses, Dictionary<string, GraphMorphSetValue> morphSets)
        {
            Output = output;
            MeshOutputs = new ReadOnlyDictionary<string, GraphMeshValue>(outputs);
            MeshInputs = new ReadOnlyDictionary<string, GraphMeshValue>(inputs);
            Diagnostics = diagnostics.AsReadOnly();
            ImageOutputs = new ReadOnlyDictionary<string, GraphImageValue>(images);
            MaterialOutputs = new ReadOnlyDictionary<string, GraphMaterialValue>(materials);
            SkeletonOutputs = new ReadOnlyDictionary<string, GraphSkeletonValue>(skeletons);
            SkinBindingOutputs = new ReadOnlyDictionary<string, GraphSkinBindingValue>(bindings);
            PoseOutputs = new ReadOnlyDictionary<string, GraphPoseValue>(poses);
            MorphSetOutputs = new ReadOnlyDictionary<string, GraphMorphSetValue>(morphSets);
        }
    }

    public static class GraphEvaluator
    {
        public static GraphEvaluation Evaluate(AuthoringGraph graph)
        {
            return Evaluate(graph, null);
        }

        /// <summary>Evaluates a graph while replacing selected mesh-producing node outputs.</summary>
        /// <remarks>
        /// Overrides are intentionally applied after the node inputs have been resolved and
        /// validated. This lets import adapters replace a SkinDeform result while preserving
        /// every downstream edit, morph, material and output node in the ordinary evaluator.
        /// </remarks>
        public static GraphEvaluation Evaluate(AuthoringGraph graph, IReadOnlyDictionary<string, GraphMeshValue> meshOverrides)
        {
            return Evaluate(graph, meshOverrides, false);
        }

        internal static GraphEvaluation Evaluate(AuthoringGraph graph, IReadOnlyDictionary<string, GraphMeshValue> meshOverrides, bool rebaseEditSnapshots)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            var order = GraphValidator.Order(graph);
            var outputs = new Dictionary<string, GraphMeshValue>();
            var inputs = new Dictionary<string, GraphMeshValue>();
            var numbers = new Dictionary<string, float>();
            var images = new Dictionary<string, GraphImageValue>();
            var materials = new Dictionary<string, GraphMaterialValue>();
            var skeletons = new Dictionary<string, GraphSkeletonValue>();
            var bindings = new Dictionary<string, GraphSkinBindingValue>();
            var poses = new Dictionary<string, GraphPoseValue>();
            var morphSets = new Dictionary<string, GraphMorphSetValue>();
            var diagnostics = new List<GraphDiagnostic>();
            var links = graph.Edges.ToDictionary(e => e.ToNode + "/" + e.ToPort);
            foreach (string id in order)
            {
                var node = graph.Nodes[id];
                var definition = BuiltinNodes.Find(node);
                if (definition == null) { diagnostics.Add(new GraphDiagnostic(id, "UNKNOWN_NODE", "Node type/version is unavailable; payload is retained.")); continue; }
                try
                {
                    GraphMeshValue input = null;
                    GraphImageValue imageInput = null;
                    GraphMaterialValue materialInput = null;
                    GraphSkeletonValue skeletonInput = null;
                    GraphSkinBindingValue bindingInput = null;
                    GraphPoseValue poseInput = null;
                    GraphMorphSetValue morphInput = null;
                    foreach (var port in definition.Inputs)
                    {
                        GraphEdge edge;
                        if (!links.TryGetValue(id + "/" + port.Id, out edge))
                        {
                            Checks.Require(!port.Required, "INPUT_MISSING", "Required input is not connected: " + port.Id);
                            continue;
                        }
                        bool ready = port.Type == PortType.Mesh ? outputs.ContainsKey(edge.FromNode) : port.Type == PortType.Image ? images.ContainsKey(edge.FromNode) : port.Type == PortType.Material ? materials.ContainsKey(edge.FromNode) : port.Type == PortType.Skeleton ? skeletons.ContainsKey(edge.FromNode) : port.Type == PortType.SkinBinding ? bindings.ContainsKey(edge.FromNode) : port.Type == PortType.Pose ? poses.ContainsKey(edge.FromNode) : port.Type == PortType.MorphSet ? morphSets.ContainsKey(edge.FromNode) : numbers.ContainsKey(edge.FromNode);
                        Checks.Require(ready, "INPUT_UNRESOLVED", "Upstream output is unavailable.");
                        if (port.Type == PortType.Mesh) { input = outputs[edge.FromNode]; inputs[id] = input; }
                        else if (port.Type == PortType.Image) imageInput = images[edge.FromNode];
                        else if (port.Type == PortType.Material) materialInput=materials[edge.FromNode];
                        else if (port.Type == PortType.Skeleton) skeletonInput=skeletons[edge.FromNode];
                        else if (port.Type == PortType.SkinBinding) bindingInput=bindings[edge.FromNode];
                        else if (port.Type == PortType.Pose) poseInput=poses[edge.FromNode];
                        else if (port.Type == PortType.MorphSet) morphInput=morphSets[edge.FromNode];
                    }
                    Checks.Require(input?.Material==null && input?.SlotMaterials==null || node.TypeId==BuiltinNodes.Output || node.TypeId==BuiltinNodes.AssignMaterial || node.TypeId==BuiltinNodes.AssignMaterials,
                        "MATERIAL_ORDER_UNSUPPORTED","Place material assignment after geometry and Paint inputs.");
                    Checks.Require(input==null || input.Mesh!=null || node.TypeId==BuiltinNodes.PolygonEdit || node.TypeId==BuiltinNodes.Output && imageInput==null,"NO_RENDERABLE_FACES","Create a face before using this node.");
                    if (meshOverrides != null && meshOverrides.TryGetValue(id, out var overrideValue))
                    {
                        Checks.Require(node.TypeId == BuiltinNodes.SkinDeform, "INVALID_OVERRIDE", "Mesh overrides are only supported for SkinDeform nodes.");
                        Checks.Require(overrideValue != null && overrideValue.Mesh != null, "INVALID_OVERRIDE", "A mesh override must be renderable.");
                        Checks.Require(input == null || input.Transform.Scale == overrideValue.Transform.Scale && input.DomainId == overrideValue.DomainId,
                            "INVALID_OVERRIDE", "A mesh override must retain the input transform and domain.");
                        outputs[id] = overrideValue;
                    }
                    else if (node.TypeId == BuiltinNodes.MeshSource)
                        outputs[id] = GraphMeshValue.Source(id, node.SourceMesh, node.Transform);
                    else if (node.TypeId == BuiltinNodes.PolygonSource)
                    {
                        var rendered = node.SourcePolygon.Faces.Count==0 ? null : NyaForge.Authoring.Topology.PolygonRenderAdapter.Build(node.SourcePolygon);
                        outputs[id] = new GraphMeshValue(rendered?.Mesh, node.Transform, Checks.Hash(Encoding.UTF8.GetBytes(id + ":" + node.SourcePolygon.DomainId)), node.SourcePolygon, rendered);
                    }
                    else if (node.TypeId == BuiltinNodes.Scalar) numbers[id] = node.Scalar;
                    else if (node.TypeId == BuiltinNodes.StandardMaterial) materials[id]=new GraphMaterialValue(node.Material,imageInput);
                    else if (node.TypeId == BuiltinNodes.Skeleton) skeletons[id] = new GraphSkeletonValue(node.Skeleton);
                    else if (node.TypeId == BuiltinNodes.SkinBind)
                    {
                        Checks.Require(input != null && input.Mesh != null && skeletonInput != null, "INPUT_UNRESOLVED", "Skin binding requires renderable mesh and skeleton inputs.");
                        bindings[id] = new GraphSkinBindingValue(node.Binding.ValidateFor(input.Mesh, skeletonInput.Skeleton));
                    }
                    else if (node.TypeId == BuiltinNodes.Pose)
                    {
                        Checks.Require(skeletonInput != null, "INPUT_UNRESOLVED", "Pose requires a skeleton input.");
                        poses[id] = new GraphPoseValue(node.Pose.ValidateFor(skeletonInput.Skeleton));
                    }
                    else if (node.TypeId == BuiltinNodes.SkinDeform)
                    {
                        Checks.Require(input != null && input.Mesh != null && skeletonInput != null && bindingInput != null && poseInput != null, "INPUT_UNRESOLVED", "Skin deformation requires mesh, skeleton, binding and pose inputs.");
                        var validBinding = bindingInput.Binding.ValidateFor(input.Mesh, skeletonInput.Skeleton);
                        var validPose = poseInput.Pose.ValidateFor(skeletonInput.Skeleton);
                        var deformed = SkinDeformer.Apply(input.Mesh, skeletonInput.Skeleton, validBinding, validPose.Poses);
                        outputs[id] = new GraphMeshValue(deformed, input.Transform, input.DomainId, null, null, input.BaseColor, input.Material, input.SlotMaterials);
                    }
                    else if (node.TypeId == BuiltinNodes.MorphSet)
                        morphSets[id] = new GraphMorphSetValue(node.Morphs);
                    else if (node.TypeId == BuiltinNodes.MorphDeform)
                    {
                        Checks.Require(input != null && input.Mesh != null && morphInput != null, "INPUT_UNRESOLVED", "Morph deformation requires mesh and morph inputs.");
                        var deformed = MorphDeformer.Apply(input.Mesh, morphInput.Morphs, node.MorphWeights);
                        outputs[id] = new GraphMeshValue(deformed, input.Transform, input.DomainId, null, null, input.BaseColor, input.Material, input.SlotMaterials);
                    }
                    else if (node.TypeId == BuiltinNodes.AssignMaterial) outputs[id]=MaterialEvaluation.Assign(input,materialInput);
                    else if(node.TypeId==BuiltinNodes.AssignMaterials) outputs[id]=MaterialSlotEvaluation.Assign(input,node.MaterialSlots.ToDictionary(s=>s,s=>
                    {
                        var edge=links[id+"/"+GraphNode.MaterialSlotPort(s)];return new MaterialSlotBinding(edge.FromNode,materials[edge.FromNode]);
                    }));
                    else if (node.TypeId == BuiltinNodes.Plane)
                    {
                        GraphEdge width, height;
                        float w = links.TryGetValue(id + "/width", out width) ? numbers[width.FromNode] : node.Width;
                        float h = links.TryGetValue(id + "/height", out height) ? numbers[height.FromNode] : node.Height;
                        outputs[id] = GraphMeshValue.Source(id, PrimitiveGeometry.Plane(w,h), new RestTransform(1,new Vec3()));
                    }
                    else if (node.TypeId == BuiltinNodes.Output) outputs[id] = PaintEvaluation.Bind(input,imageInput);
                    else if ((node.TypeId == BuiltinNodes.Paint || node.TypeId == BuiltinNodes.LayeredPaint)) images[id] = PaintEvaluation.Apply(node,input);
                    else if (node.TypeId == BuiltinNodes.Mirror) outputs[id] = MirrorEvaluation.Apply(node,input);
                    else if (node.TypeId == BuiltinNodes.PolygonEdit)
                    {
                        Checks.Require(input.Polygon != null, "EDIT_MODE_UNSUPPORTED", "PolygonEdit requires polygon input.");
                        if (!node.Enabled || node.SourcePolygon == null) outputs[id] = input;
                        else
                        {
                            Checks.Require(node.ExpectedInputSnapshot == input.SnapshotHash && node.ExpectedDomain == input.DomainId, "EDIT_INPUT_CHANGED", "Polygon edit input changed; payload is retained without retargeting.");
                            Checks.Require(node.SourcePolygon.DomainId == input.Polygon.DomainId, "EDIT_DOMAIN_CHANGED", "Polygon payload belongs to another domain.");
                            var render = node.SourcePolygon.Faces.Count==0 ? null : NyaForge.Authoring.Topology.PolygonRenderAdapter.Build(node.SourcePolygon);
                            outputs[id] = new GraphMeshValue(render?.Mesh, input.Transform, input.DomainId, node.SourcePolygon, render);
                        }
                    }
                    else if (node.TypeId == BuiltinNodes.EditMesh) outputs[id] = ApplyEdit(node, input, rebaseEditSnapshots);
                }
                catch (AuthoringException error) { diagnostics.Add(new GraphDiagnostic(id, error.Code, error.Message)); }
            }
            GraphMeshValue output = null;
            if (graph.OutputNodeId != "") outputs.TryGetValue(graph.OutputNodeId, out output);
            else diagnostics.Add(new GraphDiagnostic("", "OUTPUT_MISSING", "Select an Output node."));
            return new GraphEvaluation(output, outputs, inputs, diagnostics, images, materials, skeletons, bindings, poses, morphSets);
        }

        static GraphMeshValue ApplyEdit(GraphNode node, GraphMeshValue input, bool rebaseEditSnapshot = false)
        {
            if (!node.Enabled || node.Offsets.Count == 0) return input;
            Checks.Require(input.Polygon == null, "EDIT_MODE_UNSUPPORTED", "Polygon data requires stable-ID editing; render-index offsets cannot modify it.");
            Checks.Require(node.ExpectedDomain == input.DomainId, "EDIT_DOMAIN_CHANGED", "Edit payload belongs to a different element domain.");
            // A source-skin projection intentionally replaces the upstream SkinDeform
            // value. Its stable topology/domain is unchanged, so the authored offsets
            // remain valid even though the snapshot hash necessarily changes.
            if (!rebaseEditSnapshot)
                Checks.Require(node.ExpectedInputSnapshot == input.SnapshotHash, "EDIT_INPUT_CHANGED", "Upstream geometry changed; rebase or keep the previous source.");
            var positions = input.Mesh.Positions.ToArray();
            foreach (var pair in node.Offsets)
            {
                Checks.Require(pair.Key < positions.Length, "INVALID_VERTEX", "Edit is outside the input domain.");
                positions[pair.Key] += input.Transform.ToLocalVector(pair.Value);
                Checks.Finite(input.Transform.ToAvatarPoint(positions[pair.Key]));
            }
            return new GraphMeshValue(input.Mesh.WithPositions(positions), input.Transform, input.DomainId);
        }
    }
}


