using System;

namespace NyaForge.Authoring.Paint
{
    internal static class PaintBlend
    {
        // Source-over in linear light, with straight alpha. Coverage is linear, not sRGB.
        internal static Rgba32 Over(Rgba32 under,Rgba32 over,float coverage)
        {
            if (coverage <= 0 || over.A == 0) return under;
            double a=over.A/255.0*coverage,b=under.A/255.0*(1-a),alpha=a+b;
            byte Channel(byte old,byte next)=>Encode((Decode(next)*a+Decode(old)*b)/alpha);
            return new Rgba32(Channel(under.R,over.R),Channel(under.G,over.G),Channel(under.B,over.B),(byte)Math.Round(alpha*255));
        }
        internal static double Decode(byte value) { double c=value/255.0;return c<=.04045 ? c/12.92 : Math.Pow((c+.055)/1.055,2.4); }
        internal static byte Encode(double value)
        {
            double c=value<=.0031308 ? 12.92*value : 1.055*Math.Pow(value,1/2.4)-.055;
            return (byte)Math.Max(0,Math.Min(255,Math.Round(c*255)));
        }
    }
}
