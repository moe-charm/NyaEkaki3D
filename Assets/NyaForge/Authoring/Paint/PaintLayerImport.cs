namespace NyaForge.Authoring.Paint
{
    public static class PaintLayerImport
    {
        public static PaintLayerChange Prepare(PaintImage source,int width,int height,bool fit,string layerId,string name,int index)
        {
            Checks.Require(source!=null,"IMAGE_REQUIRED","An import image is required.");
            PaintImage.ValidateDimensions(width,height);
            Checks.Require(index>=0 && index<PaintLayers.MaxLayers,"INVALID_LAYER_INDEX","Import index is outside the layer budget.");
            var image=fit ? PaintImageFit.Apply(source,width,height) : source;
            Checks.Require(image.Width==width && image.Height==height,"INVALID_IMAGE_SIZE","Image dimensions differ from the canvas. Enable size fitting.");
            return PaintLayerChange.Add(new PaintLayer(layerId,name,image),index);
        }
    }
}
