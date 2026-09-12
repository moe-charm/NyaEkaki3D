using System;
using System.Linq;
using NyaForge.Authoring.Graph;
using Newtonsoft.Json.Linq;
namespace NyaForge.Authoring.Inspection
{
    public static class AuthoringReadService
    {
        public static JObject Read(AuthoringWorkspace workspace,string instance,string method)
        {
            if(method=="graph_inspect") return AuthoringGraphReader.Read(workspace,instance);
            // Validate identity using the same atomic state reader for every method.
            var state=AuthoringStateReader.Read(workspace,instance);
            if(method=="get_state") return state;
            if(method=="validate") throw new InvalidOperationException("Validation requires an explicit request payload.");
            Checks.Require(method=="capabilities","UNSUPPORTED_METHOD","Unknown read method.");
            return new JObject
            {
                ["instanceId"]=instance,["schemaVersion"]=1,
                ["remoteMethods"]=new JArray("get_state","capabilities","graph_inspect","vertices_inspect","faces_inspect","validate","import_image","apply","capture","save_project","export","secondary_motion_state","secondary_motion_play","secondary_motion_pause","secondary_motion_reset","secondary_motion_rebuild","secondary_motion_step"),["remoteEditing"]=true,
                ["remoteOperations"]=new JArray("layers.stroke","layers.mask.stroke","layers.mask.fill","layers.mask.clear","layers.migrate","layers.add","layers.appearance","layers.remove","layers.move","layers.rename","paint.stroke","polygon.vertices.add","polygon.faces.create","polygon.solidify","polygon.uv.project","polygon.vertices.translate","polygon.faces.extrude","polygon.faces.delete","polygon.faces.material","vertices.translate","graph.vertices.translate","graph.connect","graph.disconnect","history.undo","history.redo","object.add_graph","object.select","graph.node.add","graph.node.update","graph.node.remove","graph.output"),
                ["remoteNodeTypes"]=new JArray(BuiltinNodes.Paint,BuiltinNodes.PolygonSource,BuiltinNodes.PolygonEdit,BuiltinNodes.AssignMaterials,BuiltinNodes.Plane,BuiltinNodes.EditMesh,BuiltinNodes.Output,BuiltinNodes.StandardMaterial,BuiltinNodes.AssignMaterial,BuiltinNodes.Skeleton),
                ["units"]="meters",["space"]="avatar",
                ["limits"]=new JObject { ["objects"]=1,["graphNodes"]=AuthoringGraph.MaxNodes,["graphEdges"]=AuthoringGraph.MaxEdges },
                ["nodeDefinitions"]=new JArray(BuiltinNodes.Definitions.Values.OrderBy(d=>d.TypeId,StringComparer.Ordinal).Select(d=>new JObject
                {
                    ["typeId"]=d.TypeId,["version"]=d.Version,
                    ["inputs"]=Ports(d.Inputs),["outputs"]=Ports(d.Outputs),
                    ["instanceDependentPorts"]=d.TypeId==BuiltinNodes.AssignMaterials
                }))
            };
        }
        internal static JArray Ports(System.Collections.Generic.IEnumerable<PortDefinition> ports)=>new JArray(ports.Select(p=>new JObject { ["id"]=p.Id,["type"]=p.Type.ToString(),["required"]=p.Required }));
    }
}










