using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring
{
    public static partial class CommandWireReader
    {
        static AuthoringOperation ReadLayerOperation(string kind,JObject value)
        {
            if(kind=="layers.migrate")
            {
                Shape(value,"kind paintContext layerId");return AuthoringOperation.MigratePaintLayers(ReadPaintContext(value["paintContext"]),Text(value,"layerId"));
            }
            var c=value["layerContext"] as JObject;Shape(c,"graphId nodeId stackHash uvHash meshDomain");
            var context=LayerEditContext.FromIdentity(Text(c,"graphId"),Text(c,"nodeId"),Text(c,"stackHash"),Text(c,"uvHash"),Text(c,"meshDomain"));
            PaintLayerChange change;
            switch(kind)
            {
                case "layers.stroke":
                case "layers.mask.stroke":
                case "layers.mask.fill":
                case "layers.mask.clear": change=ReadLayerDrawing(kind,value);break;
                case "layers.add":
                    Shape(value,"kind layerContext layerId name width height index");
                    change=PaintLayerChange.Add(new PaintLayer(Text(value,"layerId"),Text(value,"name"),new PaintImage(ImageDimension(value,"width"),ImageDimension(value,"height"),new Rgba32())),ImageDimension(value,"index"));break;
                case "layers.appearance":
                    Shape(value,"kind layerContext layerId opacity visible");Checks.Require(value["visible"].Type==JTokenType.Boolean,"INVALID_COMMAND_WIRE","Expected boolean visibility.");
                    change=PaintLayerChange.Appearance(Text(value,"layerId"),Number(value,"opacity"),(bool)value["visible"]);break;
                case "layers.remove":
                    Shape(value,"kind layerContext layerId");change=PaintLayerChange.Remove(Text(value,"layerId"));break;
                case "layers.move":
                    Shape(value,"kind layerContext layerId index");change=PaintLayerChange.Move(Text(value,"layerId"),ImageDimension(value,"index"));break;
                case "layers.rename":
                    Shape(value,"kind layerContext layerId name");change=PaintLayerChange.Rename(Text(value,"layerId"),Text(value,"name"));break;
                default: throw new AuthoringException("UNSUPPORTED_OPERATION","Unknown layer operation.");
            }
            return AuthoringOperation.EditPaintLayers(context,change);
        }
    }
}
