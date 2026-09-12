using System.Linq;
using NyaForge.Authoring.Graph;
using Newtonsoft.Json.Linq;
namespace NyaForge.Authoring
{
    public static partial class CommandWireReader
    {
        static AuthoringGraph ReadGraph(JToken token)
        {
            var graph=token as JObject;Shape(graph,"graphId outputNodeId nodes edges");
            var nodes=graph["nodes"] as JArray;var edges=graph["edges"] as JArray;
            Checks.Require(nodes!=null && nodes.Count<=AuthoringGraph.MaxNodes && edges!=null && edges.Count<=AuthoringGraph.MaxEdges,"INVALID_COMMAND_WIRE","Graph collections exceed limits.");
            return new AuthoringGraph(Text(graph,"graphId"),nodes.Select(ReadNode),edges.Select(t=>
            {
                var e=t as JObject;Shape(e,"fromNode fromPort toNode toPort");return new GraphEdge(Text(e,"fromNode"),Text(e,"fromPort"),Text(e,"toNode"),Text(e,"toPort"));
            }),Text(graph,"outputNodeId"));
        }
        static GraphNode ReadNode(JToken token)
        {
            var node=token as JObject;Shape(node,"nodeId typeId version parameters");
            Checks.Require(node["version"].Type==JTokenType.Integer && node["version"].ToString()=="1","INVALID_COMMAND_WIRE","Unsupported node version.");
            string id=Text(node,"nodeId"),type=Text(node,"typeId");var parameters=node["parameters"] as JObject;
            switch(type)
            {
                case BuiltinNodes.Paint:
                    Shape(parameters,"width height");return GraphNode.Paint(id,ImageDimension(parameters,"width"),ImageDimension(parameters,"height"));
                case BuiltinNodes.PolygonSource:
                    return ReadPolygon(id,parameters);
                case BuiltinNodes.PolygonEdit:
                    Empty(parameters);return GraphNode.PolygonEdit(id);
                case BuiltinNodes.StandardMaterial:
                    return GraphNode.StandardMaterial(id,ReadMaterial(parameters));
                case BuiltinNodes.AssignMaterial:
                    Empty(parameters);return GraphNode.AssignMaterial(id);
                case BuiltinNodes.AssignMaterials:
                    Shape(parameters,"slots");
                    var slots=parameters["slots"] as JArray;
                    Checks.Require(slots!=null && slots.Count>0,"INVALID_COMMAND_WIRE","Material slots are required.");
                    Checks.Require(slots.All(s=>s.Type==JTokenType.Integer && int.TryParse(s.ToString(),out _)),"INVALID_COMMAND_WIRE","Material slots must be integers.");
                    return GraphNode.AssignMaterials(id,slots.Select(s=>(int)s));
                case BuiltinNodes.Plane:
                    Shape(parameters,"width height");return GraphNode.Plane(id,Number(parameters,"width"),Number(parameters,"height"));
                case BuiltinNodes.EditMesh:
                    Empty(parameters);return GraphNode.Edit(id);
                case BuiltinNodes.Output:
                    Empty(parameters);return GraphNode.Output(id);
                default: throw new AuthoringException("UNSUPPORTED_NODE_WIRE","Node type is not exposed by command transport: "+type);
            }
        }
        static void Empty(JObject o) { Checks.Require(o!=null && !o.Properties().Any(),"INVALID_COMMAND_WIRE","Expected empty parameters."); }
        static float Number(JObject o,string field)
        {
            var value=o[field];Checks.Require(value.Type==JTokenType.Float || value.Type==JTokenType.Integer,"INVALID_COMMAND_WIRE","Expected numeric parameter.");
            float number=(float)value;Checks.Finite(number);return number;
        }
    }
}
