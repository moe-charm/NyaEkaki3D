using NyaForge.Authoring.Paint;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring
{
    public static partial class CommandWireReader
    {
        static PaintLayerChange ReadLayerDrawing(string kind,JObject value)
        {
            switch(kind)
            {
                case "layers.stroke":
                    Shape(value,"kind layerContext layerId points radius color");
                    return PaintLayerChange.Stroke(Text(value,"layerId"),ReadStrokePoints(value["points"]),Number(value,"radius"),ReadStrokeColor(value["color"]));
                case "layers.mask.stroke":
                    Shape(value,"kind layerContext layerId points radius target strength");
                    return PaintLayerChange.MaskStroke(Text(value,"layerId"),ReadStrokePoints(value["points"]),Number(value,"radius"),ReadByte(value["target"]),Number(value,"strength"));
                case "layers.mask.clear":
                    Shape(value,"kind layerContext layerId");return PaintLayerChange.Mask(Text(value,"layerId"),null);
                case "layers.mask.fill":
                    Shape(value,"kind layerContext layerId width height target");
                    int width=ImageDimension(value,"width"),height=ImageDimension(value,"height");PaintImage.ValidateDimensions(width,height);
                    byte target=ReadByte(value["target"]);var coverage=new byte[checked(width*height)];
                    for(int i=0;i<coverage.Length;i++) coverage[i]=target;
                    return PaintLayerChange.Mask(Text(value,"layerId"),new PaintMask(width,height,coverage));
                default: throw new AuthoringException("UNSUPPORTED_OPERATION","Unknown layer drawing operation.");
            }
        }
    }
}
