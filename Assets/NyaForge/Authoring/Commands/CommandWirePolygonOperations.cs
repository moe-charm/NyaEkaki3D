using System.Linq;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring
{
    public static partial class CommandWireReader
    {
        static AuthoringOperation ReadPolygonOperation(string kind,JObject value)
        {
            switch(kind)
            {
                case "polygon.faces.material":
                    Shape(value,"kind context elementIds materialSlot materialNodeId");
                    int slot=0;Checks.Require(value["materialSlot"].Type==JTokenType.Integer && int.TryParse(value["materialSlot"].ToString(),out slot),"INVALID_COMMAND_WIRE","Expected integer material slot.");
                    return AuthoringOperation.AssignPolygonMaterial(ReadContext(value["context"]),SelectedElements(value),slot,Text(value,"materialNodeId"));
                case "polygon.vertices.add":
                    Shape(value,"kind context position");return AuthoringOperation.AddPolygonVertex(ReadContext(value["context"]),Vector(value["position"]));
                case "polygon.uv.project":
                    Shape(value,"kind context");return AuthoringOperation.ProjectPolygonUv(ReadContext(value["context"]));
                case "polygon.solidify":
                    Shape(value,"kind context thickness");return AuthoringOperation.SolidifyPolygon(ReadContext(value["context"]),Number(value,"thickness"));
                case "polygon.faces.create":
                    Shape(value,"kind context elementIds materialSlot");
                    Checks.Require(value["materialSlot"].Type==JTokenType.Integer && int.TryParse(value["materialSlot"].ToString(),out _),"INVALID_COMMAND_WIRE","Expected integer material slot.");
                    return AuthoringOperation.CreatePolygonFace(ReadContext(value["context"]),SelectedElements(value),(int)value["materialSlot"]);
            }
            bool delete=kind=="polygon.faces.delete";
            Shape(value,delete ? "kind context elementIds" : "kind context elementIds delta");
            var selected=SelectedElements(value);
            var context=ReadContext(value["context"]);
            if(delete) return AuthoringOperation.DeletePolygonFaces(context,selected);
            var delta=Vector(value["delta"]);
            return kind=="polygon.vertices.translate" ? AuthoringOperation.TranslatePolygonVertices(context,selected,delta) : AuthoringOperation.ExtrudePolygonFaces(context,selected,delta);
        }
        static ulong[] SelectedElements(JObject value)
        {
            var ids=value["elementIds"] as JArray;
            Checks.Require(ids!=null && ids.Count>0 && ids.Count<=4096,"INVALID_COMMAND_WIRE","Expected 1..4096 element IDs.");
            var selected=ids.Select(ElementId).ToArray();
            Checks.Require(selected.Distinct().Count()==selected.Length,"INVALID_SELECTION","Duplicate element IDs.");
            return selected;
        }
    }
}
