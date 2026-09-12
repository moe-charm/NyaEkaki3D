using System;
using System.IO;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring
{
    public static partial class CommandWireReader
    {
        static AuthoringOperation ReadImageImport(JObject value,Func<string,string,PaintImage> loader)
        {
            Shape(value,"kind layerContext layerId name width height index path sourceHash fit");
            var c=value["layerContext"] as JObject;Shape(c,"graphId nodeId stackHash uvHash meshDomain");
            var context=LayerEditContext.FromIdentity(Text(c,"graphId"),Text(c,"nodeId"),Text(c,"stackHash"),Text(c,"uvHash"),Text(c,"meshDomain"));
            string path=Text(value,"path"),hash=Text(value,"sourceHash"),id=Text(value,"layerId"),name=Text(value,"name");
            Checks.Require(Path.IsPathRooted(path),"INVALID_IMPORT_PATH","Import requires an absolute file path.");Checks.HashText(hash);Checks.Id(id);Checks.Name(name);
            int width=ImageDimension(value,"width"),height=ImageDimension(value,"height"),index=ImageDimension(value,"index");PaintImage.ValidateDimensions(width,height);
            Checks.Require(index>=0 && index<PaintLayers.MaxLayers,"INVALID_LAYER_INDEX","Invalid import index.");
            Checks.Require(value["fit"].Type==JTokenType.Boolean,"INVALID_COMMAND_WIRE","Expected boolean fit.");
            return AuthoringOperation.EditPaintLayers(context,PaintLayerImport.Prepare(loader(path,hash),width,height,(bool)value["fit"],id,name,index));
        }
    }
}
