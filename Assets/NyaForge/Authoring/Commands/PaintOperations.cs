using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;

namespace NyaForge.Authoring
{
    public sealed partial class AuthoringOperation
    {
        public PaintEditContext PaintContext { get; private set; }
        public IReadOnlyList<Vec2> StrokePoints { get; private set; }
        public float BrushRadius { get; private set; }
        public Rgba32 BrushColor { get; private set; }
        public static AuthoringOperation PaintImageStroke(PaintEditContext context,IEnumerable<Vec2> points,float radius,Rgba32 color)
        {
            Checks.Require(context!=null && points!=null,"INVALID_STROKE","Paint context and stroke are required.");
            var copy=points.Take(PaintStroke.MaxPoints+1).ToArray();
            Checks.Require(copy.Length>0 && copy.Length<=PaintStroke.MaxPoints,"STROKE_BUDGET_EXCEEDED","Stroke point count exceeds capacity.");
            Checks.Finite(radius);
            foreach(var point in copy) { Checks.Finite(point.X);Checks.Finite(point.Y); }
            return new AuthoringOperation("graph.paint.stroke",Array.Empty<int>(),new Vec3(),false)
            { PaintContext=context,StrokePoints=Array.AsReadOnly(copy),BrushRadius=radius,BrushColor=color };
        }
        void WritePaintFingerprint(BinaryWriter writer)
        {
            if(PaintContext==null) return;
            writer.Write(PaintContext.GraphId);writer.Write(PaintContext.NodeId);writer.Write(PaintContext.ImageHash);writer.Write(PaintContext.UvHash);writer.Write(PaintContext.MeshDomain);
            writer.Write(Checks.Canonical(BrushRadius));writer.Write(BrushColor.R);writer.Write(BrushColor.G);writer.Write(BrushColor.B);writer.Write(BrushColor.A);
            writer.Write(StrokePoints.Count);foreach(var point in StrokePoints){writer.Write(Checks.Canonical(point.X));writer.Write(Checks.Canonical(point.Y));}
        }
    }
}
