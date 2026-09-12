using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NyaForge.Authoring.Paint
{
    /// <summary>Immutable disconnected UV polylines belonging to one gesture and one coverage budget.</summary>
    public sealed class PaintStrokePath
    {
        public IReadOnlyList<IReadOnlyList<Vec2>> Sections { get; }
        public int PointCount { get; }
        public PaintStrokePath(IEnumerable<IEnumerable<Vec2>> sections)
        {
            Checks.Require(sections!=null,"INVALID_STROKE","Stroke sections are required.");
            var result=new List<IReadOnlyList<Vec2>>();int count=0;
            foreach(var section in sections.Take(PaintStroke.MaxPoints+1))
            {
                Checks.Require(section!=null,"INVALID_STROKE","Stroke section cannot be null.");
                var points=section.Take(PaintStroke.MaxPoints-count+1).ToArray();
                Checks.Require(points.Length>0 && count+points.Length<=PaintStroke.MaxPoints,"STROKE_BUDGET_EXCEEDED","A gesture needs 1..1024 points total, with no empty sections.");
                foreach(var point in points)
                {
                    Checks.Finite(point.X);Checks.Finite(point.Y);
                    Checks.Require(point.X>=0 && point.X<=1 && point.Y>=0 && point.Y<=1,"INVALID_STROKE","Stroke UV coordinates must be within 0..1.");
                }
                count+=points.Length;result.Add(Array.AsReadOnly(points));
            }
            Checks.Require(count>0,"STROKE_BUDGET_EXCEEDED","A gesture must contain at least one point.");
            Sections=result.AsReadOnly();PointCount=count;
        }
        internal void Fingerprint(BinaryWriter writer)
        {
            writer.Write(Sections.Count);
            foreach(var section in Sections)
            {
                writer.Write(section.Count);
                foreach(var p in section) { writer.Write(Checks.Canonical(p.X));writer.Write(Checks.Canonical(p.Y)); }
            }
        }
    }
}
