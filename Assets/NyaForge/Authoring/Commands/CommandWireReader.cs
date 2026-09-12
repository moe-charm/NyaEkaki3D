using System;
using System.Linq;
using NyaForge.Authoring.Graph;
using Newtonsoft.Json.Linq;
namespace NyaForge.Authoring
{
    /// <summary>Explicit transport-to-command conversion. Never supplies current revisions or generates command IDs.</summary>
    public static partial class CommandWireReader
    {
        public static CommandEnvelope Read(JObject value)=>Read(value,null);
        public static CommandEnvelope Read(JObject value,Func<string,string,NyaForge.Authoring.Paint.PaintImage> imageLoader)
        {
            Shape(value,"expectedInstanceId documentId expectedDocumentRevision commandId objectId expectedBaselineHash operations");
            string instance=Text(value,"expectedInstanceId"),document=Text(value,"documentId"),command=Text(value,"commandId"),obj=Text(value,"objectId"),baseline=Text(value,"expectedBaselineHash");
            Checks.Id(instance);Checks.Id(document);Checks.Id(command);if(obj!="") Checks.Id(obj);if(baseline!="") Checks.HashText(baseline);
            Checks.Require(value["expectedDocumentRevision"].Type==JTokenType.Integer && long.TryParse(value["expectedDocumentRevision"].ToString(),out var revision) && revision>=0,"INVALID_COMMAND_WIRE","Expected a nonnegative revision.");
            var operations=value["operations"] as JArray;
            Checks.Require(operations!=null && operations.Count>0 && operations.Count<=64,"INVALID_COMMAND_WIRE","Expected 1..64 operations.");
            return new CommandEnvelope
            {
                ExpectedInstanceId=instance,DocumentId=document,CommandId=command,ObjectId=obj,ExpectedBaselineHash=baseline,
                ExpectedDocumentRevision=(long)value["expectedDocumentRevision"],Operations=operations.Select(o=>imageLoader!=null && o is JObject && (string)o["kind"]=="layers.import" ? ReadImageImport(o as JObject,imageLoader) : ReadOperation(o)).ToArray()
            };
        }
        static AuthoringOperation ReadOperation(JToken operation)
        {
            Checks.Require(operation is JObject,"INVALID_COMMAND_WIRE","Expected an operation object.");var o=(JObject)operation;
            string kind=Text(o,"kind");
            switch(kind)
            {
                case "layers.migrate":
                case "layers.stroke":
                case "layers.mask.stroke":
                case "layers.mask.fill":
                case "layers.mask.clear":
                case "layers.add":
                case "layers.appearance":
                case "layers.remove":
                case "layers.move":
                case "layers.rename": return ReadLayerOperation(kind,o);
                case "paint.stroke": return ReadPaintStroke(o);
                case "polygon.vertices.translate":
                case "polygon.vertices.add":
                case "polygon.faces.create":
                case "polygon.solidify":
                case "polygon.uv.project":
                case "polygon.faces.extrude":
                case "polygon.faces.material":
                case "polygon.faces.delete":
                    return ReadPolygonOperation(kind,o);
                case "graph.vertices.translate":
                    Shape(o,"kind context vertexIds delta");return AuthoringOperation.TranslateGraphVertices(ReadContext(o["context"]),Ids(o["vertexIds"]),Vector(o["delta"]));
                case "object.add_graph":
                    Shape(o,"kind newObjectId graph");return AuthoringOperation.AddGraph(ReadGraph(o["graph"]),Text(o,"newObjectId"));
                case "object.select":
                    Shape(o,"kind objectId");return AuthoringOperation.SelectObject(Text(o,"objectId"));
                case "graph.node.add":
                    Shape(o,"kind node");return AuthoringOperation.AddNode(ReadNode(o["node"]));
                case "graph.node.update":
                    Shape(o,"kind node");return AuthoringOperation.UpdateNode(ReadNode(o["node"]));
                case "graph.node.remove":
                    Shape(o,"kind nodeId");return AuthoringOperation.RemoveNode(Text(o,"nodeId"));
                case "graph.output":
                    Shape(o,"kind nodeId");return AuthoringOperation.SetOutput(Text(o,"nodeId"));
                case "history.undo": Shape(o,"kind");return AuthoringOperation.Undo();
                case "history.redo": Shape(o,"kind");return AuthoringOperation.Redo();
                case "vertices.translate":
                    Shape(o,"kind vertexIds delta");return AuthoringOperation.TranslateVertices(Ids(o["vertexIds"]),Vector(o["delta"]));
                case "graph.disconnect":
                    Shape(o,"kind nodeId inputPort");return AuthoringOperation.Disconnect(Text(o,"nodeId"),Text(o,"inputPort"));
                case "graph.connect":
                    Shape(o,"kind fromNode fromPort toNode toPort");return AuthoringOperation.Connect(new GraphEdge(Text(o,"fromNode"),Text(o,"fromPort"),Text(o,"toNode"),Text(o,"toPort")));
                default: throw new AuthoringException("UNSUPPORTED_OPERATION","Operation is not exposed by the command transport: "+kind);
            }
        }
        static int[] Ids(JToken t)
        {
            Checks.Require(t is JArray && t.Count()>0 && t.Count()<=4096,"INVALID_COMMAND_WIRE","Expected 1..4096 vertex IDs.");
            return t.Select(v=> { Checks.Require(v.Type==JTokenType.Integer && int.TryParse(v.ToString(),out var id) && id>=0,"INVALID_COMMAND_WIRE","Invalid vertex ID.");return (int)v; }).ToArray();
        }
        static Vec3 Vector(JToken t)
        {
            Checks.Require(t is JArray && t.Count()==3,"INVALID_COMMAND_WIRE","Expected delta xyz.");
            var values=t.Select(v=> { Checks.Require(v.Type==JTokenType.Float || v.Type==JTokenType.Integer,"INVALID_COMMAND_WIRE","Expected numeric delta.");float f=(float)v;Checks.Finite(f);return f; }).ToArray();
            return new Vec3(values[0],values[1],values[2]);
        }
        static string Text(JObject o,string key)
        { Checks.Require(o[key]?.Type==JTokenType.String,"INVALID_COMMAND_WIRE","Expected string: "+key);return (string)o[key]; }
        static void Shape(JObject o,string fields)
        { Checks.Require(o!=null && o.Properties().Select(p=>p.Name).OrderBy(n=>n,StringComparer.Ordinal).SequenceEqual(fields.Split(' ').OrderBy(n=>n,StringComparer.Ordinal)),"INVALID_COMMAND_WIRE","Unexpected command fields."); }
    }
}
