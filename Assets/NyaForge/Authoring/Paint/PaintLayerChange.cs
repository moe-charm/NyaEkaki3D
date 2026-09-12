using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NyaForge.Authoring.Paint
{
    public enum PaintLayerAction { Add, Remove, Move, Appearance, Rename, Mask, Stroke, MaskStroke, PathStroke, MaskPathStroke }

    /// <summary>Immutable intent. Layer order indices are bottom-to-top, after removal for Move.</summary>
    public sealed class PaintLayerChange
    {
        public PaintLayerAction Action { get; }
        public string LayerId { get; }
        public PaintLayer Layer { get; private set; }
        public int Index { get; private set; }
        public float Opacity { get; private set; }
        public bool Visible { get; private set; }
        public string Name { get; private set; } = "";
        public PaintMask Coverage { get; private set; }
        public IReadOnlyList<Vec2> Points { get; private set; } = Array.Empty<Vec2>();
        public float Radius { get; private set; }
        public Rgba32 Color { get; private set; }
        public byte MaskTarget { get; private set; }
        public float MaskStrength { get; private set; }
        public PaintStrokePath Path { get; private set; }
        PaintLayerChange(PaintLayerAction action,string id) { Checks.Id(id);Action=action;LayerId=id; }
        public static PaintLayerChange Add(PaintLayer layer,int index)
        {
            Checks.Require(layer!=null,"INVALID_PAINT_LAYERS","New layer is required.");
            return new PaintLayerChange(PaintLayerAction.Add,layer.Id) { Layer=layer,Index=index };
        }
        public static PaintLayerChange Remove(string id) => new PaintLayerChange(PaintLayerAction.Remove,id);
        public static PaintLayerChange Move(string id,int index) => new PaintLayerChange(PaintLayerAction.Move,id) { Index=index };
        public static PaintLayerChange Appearance(string id,float opacity,bool visible)
        {
            Checks.Finite(opacity);Checks.Require(opacity>=0 && opacity<=1,"INVALID_LAYER_OPACITY","Opacity must be 0..1.");
            return new PaintLayerChange(PaintLayerAction.Appearance,id) { Opacity=opacity,Visible=visible };
        }
        public static PaintLayerChange Rename(string id,string name)
        { Checks.Name(name);return new PaintLayerChange(PaintLayerAction.Rename,id) { Name=name }; }
        public static PaintLayerChange Mask(string id,PaintMask mask) => new PaintLayerChange(PaintLayerAction.Mask,id) { Coverage=mask };
        public static PaintLayerChange Stroke(string id,IEnumerable<Vec2> points,float radius,Rgba32 color)
        {
            Checks.Require(points!=null,"INVALID_STROKE","Stroke points are required.");
            var copy=points.Take(PaintStroke.MaxPoints+1).ToArray();
            Checks.Require(copy.Length>0 && copy.Length<=PaintStroke.MaxPoints,"STROKE_BUDGET_EXCEEDED","Stroke point count exceeds budget.");
            Checks.Finite(radius);
            foreach(var p in copy) { Checks.Finite(p.X);Checks.Finite(p.Y); }
            return new PaintLayerChange(PaintLayerAction.Stroke,id) { Points=Array.AsReadOnly(copy),Radius=radius,Color=color };
        }
        internal void Fingerprint(BinaryWriter writer)
        {
            writer.Write((int)Action);writer.Write(LayerId);writer.Write(Index);writer.Write(Checks.Canonical(Opacity));writer.Write(Visible);writer.Write(Name);
            writer.Write(Layer==null ? "" : Checks.Hash(PaintLayersCodec.Write(new PaintLayers(Layer.Image.Width,Layer.Image.Height,new[]{Layer}),Checks.Hash)));
            writer.Write(Coverage==null ? "" : Checks.Hash(PaintMaskCodec.Write(Coverage)));
            writer.Write(Checks.Canonical(Radius));writer.Write(Color.R);writer.Write(Color.G);writer.Write(Color.B);writer.Write(Color.A);
            writer.Write(Points.Count);foreach(var p in Points) { writer.Write(Checks.Canonical(p.X));writer.Write(Checks.Canonical(p.Y)); }
            if(Action==PaintLayerAction.MaskStroke || Action==PaintLayerAction.MaskPathStroke) { writer.Write(MaskTarget);writer.Write(Checks.Canonical(MaskStrength)); }
            if(Action==PaintLayerAction.PathStroke || Action==PaintLayerAction.MaskPathStroke) Path.Fingerprint(writer);
        }
        public static PaintLayerChange MaskStroke(string id,IEnumerable<Vec2> points,float radius,byte target,float strength)
        {
            Checks.Finite(strength);Checks.Require(strength>=0 && strength<=1,"INVALID_MASK_STRENGTH","Mask strength must be 0..1.");
            var stroke=Stroke(id,points,radius,new Rgba32());
            return new PaintLayerChange(PaintLayerAction.MaskStroke,id) { Points=stroke.Points,Radius=radius,MaskTarget=target,MaskStrength=strength };
        }
        public static PaintLayerChange PathStroke(string id,PaintStrokePath path,float radius,Rgba32 color)
        {
            Checks.Require(path!=null,"INVALID_STROKE","Stroke path is required.");Checks.Finite(radius);
            return new PaintLayerChange(PaintLayerAction.PathStroke,id) { Path=path,Radius=radius,Color=color };
        }
        public static PaintLayerChange MaskPathStroke(string id,PaintStrokePath path,float radius,byte target,float strength)
        {
            Checks.Require(path!=null,"INVALID_STROKE","Stroke path is required.");Checks.Finite(radius);Checks.Finite(strength);
            Checks.Require(strength>=0 && strength<=1,"INVALID_MASK_STRENGTH","Mask strength must be 0..1.");
            return new PaintLayerChange(PaintLayerAction.MaskPathStroke,id) { Path=path,Radius=radius,MaskTarget=target,MaskStrength=strength };
        }
    }
}
