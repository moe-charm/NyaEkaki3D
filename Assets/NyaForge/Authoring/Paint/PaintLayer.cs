namespace NyaForge.Authoring.Paint
{
    public sealed class PaintLayer
    {
        public string Id { get; }
        public string Name { get; }
        public PaintImage Image { get; }
        public PaintMask Mask { get; }
        public float Opacity { get; }
        public bool Visible { get; }
        public PaintLayer(string id,string name,PaintImage image,float opacity=1,bool visible=true,PaintMask mask=null)
        {
            Checks.Id(id); Checks.Name(name); Checks.Finite(opacity);
            Checks.Require(image!=null,"IMAGE_REQUIRED","A layer requires an image.");
            Checks.Require(opacity>=0 && opacity<=1,"INVALID_LAYER_OPACITY","Layer opacity must be 0..1.");
            Checks.Require(mask==null || (mask.Width==image.Width && mask.Height==image.Height),"INVALID_MASK_SIZE","Mask dimensions must match the layer.");
            Id=id; Name=name; Image=image; Opacity=opacity; Visible=visible; Mask=mask;
        }
        public PaintLayer WithImage(PaintImage image) => new PaintLayer(Id,Name,image,Opacity,Visible,Mask);
        public PaintLayer WithMask(PaintMask mask) => new PaintLayer(Id,Name,Image,Opacity,Visible,mask);
        public PaintLayer WithAppearance(float opacity,bool visible) => new PaintLayer(Id,Name,Image,opacity,visible,Mask);
    }
}
