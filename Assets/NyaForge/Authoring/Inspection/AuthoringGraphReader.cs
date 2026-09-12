using System;
using System.Linq;
using NyaForge.Authoring.Graph;
using Newtonsoft.Json.Linq;
namespace NyaForge.Authoring.Inspection
{
    public static class AuthoringGraphReader
    {
        public static JObject Read(AuthoringWorkspace workspace,string instance)
        {
            lock(workspace.Gate)
            {
                var state=AuthoringStateReader.Read(workspace,instance);var doc=workspace.Document;
                var result=new JObject { ["instanceId"]=instance,["documentId"]=doc.DocumentId,["revision"]=doc.DocumentRevision,["stateHash"]=doc.StateHash,["graph"]=JValue.CreateNull() };
                if(doc.IsEmpty) return result;
                var graph=doc.Objects[0].Graph;var evaluation=workspace.Preview.Evaluation;
                result["graph"]=new JObject
                {
                    ["objectId"]=doc.ObjectId,["graphId"]=graph.GraphId,["outputNodeId"]=graph.OutputNodeId,
                    ["evaluationComplete"]=workspace.Preview.IsComplete,["stalePreview"]=workspace.Preview.IsStale,
                    ["nodes"]=new JArray(graph.Nodes.Values.OrderBy(n=>n.NodeId,StringComparer.Ordinal).Select(n=>
                    {
                        var definition=BuiltinNodes.Find(n);
                        evaluation.MeshInputs.TryGetValue(n.NodeId,out var input);evaluation.MeshOutputs.TryGetValue(n.NodeId,out var output);
                        evaluation.ImageOutputs.TryGetValue(n.NodeId,out var image);
                        evaluation.MaterialOutputs.TryGetValue(n.NodeId,out var material);
                        evaluation.SkeletonOutputs.TryGetValue(n.NodeId,out var skeleton);
                        evaluation.SkinBindingOutputs.TryGetValue(n.NodeId,out var binding);
                        evaluation.PoseOutputs.TryGetValue(n.NodeId,out var pose);
                        evaluation.MorphSetOutputs.TryGetValue(n.NodeId,out var morphs);
                        return new JObject
                        {
                            ["nodeId"]=n.NodeId,["typeId"]=n.TypeId,["version"]=n.Version,["supported"]=definition!=null,
                            ["inputs"]=definition==null ? JValue.CreateNull() : (JToken)AuthoringReadService.Ports(definition.Inputs),
                            ["outputs"]=definition==null ? JValue.CreateNull() : (JToken)AuthoringReadService.Ports(definition.Outputs),
                            ["meshInput"]=Mesh(input),["meshOutput"]=Mesh(output),
                            ["layerStack"]=AuthoringLayerReader.Read(graph,n,image),
                            ["paintContext"]=(n.TypeId==BuiltinNodes.Paint && definition!=null && image!=null) ? (JToken)new JObject { ["graphId"]=graph.GraphId,["nodeId"]=n.NodeId,["imageHash"]=image.ImageHash,["uvHash"]=image.UvHash,["meshDomain"]=image.MeshDomain } : JValue.CreateNull(),
                            ["imageOutput"]=image==null ? JValue.CreateNull() : (JToken)new JObject { ["imageHash"]=image.ImageHash,["width"]=image.Image.Width,["height"]=image.Image.Height,["uvHash"]=image.UvHash,["meshDomain"]=image.MeshDomain },
                            ["materialOutput"]=AuthoringMaterialReader.Read(material),
                            ["assignedMaterial"]=AuthoringMaterialReader.Read(output?.Material),
                            ["materialSlots"]=n.MaterialSlots==null ? JValue.CreateNull() : (JToken)new JArray(n.MaterialSlots),
                            ["assignedMaterials"]=output?.SlotMaterials==null ? JValue.CreateNull() : (JToken)new JObject(output.SlotMaterials.OrderBy(p=>p.Key).Select(p=>new JProperty(p.Key.ToString(System.Globalization.CultureInfo.InvariantCulture),AuthoringMaterialReader.Read(p.Value.Material)))),
                            ["skeletonOutput"]=Skeleton(skeleton),
                            ["skinBindingOutput"]=Binding(binding),
                            ["poseOutput"]=Pose(pose),
                            ["morphOutput"]=Morph(morphs),
                            ["editContext"]=(definition!=null && ((n.TypeId==BuiltinNodes.EditMesh && input?.Mesh!=null && input.Polygon==null) || (n.TypeId==BuiltinNodes.PolygonEdit && input?.Polygon!=null))) ? (JToken)new JObject { ["graphId"]=graph.GraphId,["nodeId"]=n.NodeId,["inputSnapshot"]=input.SnapshotHash,["domainId"]=input.DomainId } : JValue.CreateNull()
                        };
                    })),
                    ["edges"]=new JArray(graph.Edges.Select(e=>new JObject { ["fromNode"]=e.FromNode,["fromPort"]=e.FromPort,["toNode"]=e.ToNode,["toPort"]=e.ToPort })),
                    ["diagnostics"]=new JArray(evaluation.Diagnostics.Select(d=>new JObject { ["nodeId"]=d.NodeId,["code"]=d.Code,["message"]=d.Message }))
                };
                return result;
            }
        }
        static JToken Mesh(GraphMeshValue value)=>value==null ? JValue.CreateNull() : (JToken)new JObject
        {
            ["snapshotHash"]=value.SnapshotHash,["domainId"]=value.DomainId,
            ["renderable"]=value.Mesh!=null,["vertexCount"]=value.Mesh?.VertexCount ?? 0,["meshHash"]=value.Mesh==null ? JValue.CreateNull() : new JValue(value.Mesh.ContentHash)
        };
        static JToken Skeleton(GraphSkeletonValue value)=>value==null ? JValue.CreateNull() : (JToken)new JObject
        {
            ["skeletonHash"]=value.Skeleton.ContentHash,["boneCount"]=value.Skeleton.Bones.Count,
            ["bones"]=new JArray(value.Skeleton.Bones.OrderBy(b=>b.BoneId,StringComparer.Ordinal).Select(b=>new JObject { ["boneId"]=b.BoneId,["name"]=b.Name,["parentBoneId"]=b.ParentBoneId,["head"]=new JArray(b.Head.X,b.Head.Y,b.Head.Z),["tail"]=new JArray(b.Tail.X,b.Tail.Y,b.Tail.Z) }))
        };
        static JToken Binding(GraphSkinBindingValue value)=>value==null ? JValue.CreateNull() : (JToken)new JObject
        {
            ["meshTopologyHash"]=value.Binding.MeshTopologyHash,["skeletonHash"]=value.Binding.SkeletonHash,
            ["vertexCount"]=value.Binding.Weights.Count,
            ["influenceCount"]=value.Binding.Weights.Sum(p=>p.Value.Count),
            ["maxInfluencesPerVertex"]=value.Binding.Weights.Count == 0 ? 0 : value.Binding.Weights.Max(p=>p.Value.Count)
        };
        static JToken Pose(GraphPoseValue value)=>value==null ? JValue.CreateNull() : (JToken)new JObject
        {
            ["skeletonHash"]=value.Pose.SkeletonHash,["poseHash"]=value.Pose.ContentHash,["boneCount"]=value.Pose.Poses.Count
        };
        static JToken Morph(GraphMorphSetValue value)=>value==null ? JValue.CreateNull() : (JToken)new JObject
        {
            ["meshTopologyHash"]=value.Morphs.MeshTopologyHash,["morphHash"]=value.Morphs.ContentHash,["targetCount"]=value.Morphs.Targets.Count,
            ["targets"]=new JArray(value.Morphs.Targets.OrderBy(t=>t.TargetId,StringComparer.Ordinal).Select(t=>new JObject { ["targetId"]=t.TargetId,["name"]=t.Name,["deltaCount"]=t.Deltas.Count,["contentHash"]=t.ContentHash }))
        };
    }
}
