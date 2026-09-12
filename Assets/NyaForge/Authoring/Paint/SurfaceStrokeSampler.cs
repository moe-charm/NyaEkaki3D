using System;
using System.Collections.Generic;

namespace NyaForge.Authoring.Paint
{
    /// <summary>Transient screen-space gesture. Misses, invalid UV and discontinuous edges split sections.</summary>
    public sealed class SurfaceStrokeSampler
    {
        public const int MaxRaySamples=4096;
        readonly SurfacePaintMesh surface;
        readonly Func<Vec2,SurfacePaintHit> resolve;
        readonly float spacing;
        readonly SurfaceScreenCoverage coverage;
        readonly List<List<Vec2>> sections=new List<List<Vec2>>();
        Vec2 previousScreen;
        Vec2 previousSample;
        SurfacePaintHit previousSampleHit;
        SurfacePaintHit previousHit;
        bool started,faulted;
        int samples;
        PaintStrokePath snapshot;
        int snapshotPoints=-1;
        public int PointCount { get; private set; }
        public int RaySampleCount=>samples;
        public SurfaceStrokeSampler(SurfacePaintMesh surface,Func<Vec2,SurfacePaintHit> resolver,float sampleSpacing=2,SurfaceScreenCoverage screenCoverage=null)
        {
            Checks.Require(surface!=null && resolver!=null,"INVALID_STROKE","Surface and screen ray resolver are required.");
            Checks.Finite(sampleSpacing);Checks.Require(sampleSpacing>=.5f && sampleSpacing<=16,"INVALID_STROKE","Screen sample spacing must be 0.5..16 pixels.");
            this.surface=surface;resolve=resolver;spacing=sampleSpacing;coverage=screenCoverage;
        }
        public void Append(Vec2 screenPoint)
        {
            Checks.Require(!faulted,"STROKE_INVALIDATED","Discard the failed gesture before drawing again.");
            try
            {
                Checks.Finite(screenPoint.X);Checks.Finite(screenPoint.Y);
                double dx=(double)screenPoint.X-previousScreen.X,dy=(double)screenPoint.Y-previousScreen.Y;
                double steps=started ? Math.Ceiling(Math.Sqrt(dx*dx+dy*dy)/spacing) : 1;
                Checks.Require(steps<=MaxRaySamples-samples,"STROKE_BUDGET_EXCEEDED","Gesture exceeds ray sample budget.");
                int count=(int)steps;
                var parameters=new SortedSet<double>();for(int i=1;i<=count;i++) parameters.Add((double)i/count);
                if(started && coverage!=null) foreach(double t in coverage.SampleParameters(previousScreen,screenPoint,MaxRaySamples-samples)) parameters.Add(t);
                Checks.Require(parameters.Count<=MaxRaySamples-samples,"STROKE_BUDGET_EXCEEDED","Gesture exceeds ray sample budget.");
                foreach(double t in parameters)
                {
                    var point=!started ? screenPoint : new Vec2((float)(previousScreen.X+dx*t),(float)(previousScreen.Y+dy*t));
                    var hit=Resolve(point);
                    if(started) SurfaceBoundaryRefinement.Append(previousSample,previousSampleHit,point,hit,Resolve,Emit);
                    else Emit(hit);
                    previousSample=point;previousSampleHit=hit;
                }
                previousScreen=screenPoint;started=true;
            }
            catch { faulted=true;throw; }
        }
        SurfacePaintHit Resolve(Vec2 point)
        {
            Checks.Require(samples<MaxRaySamples,"STROKE_BUDGET_EXCEEDED","Gesture exceeds ray sample budget.");
            samples++;var hit=resolve(point);
            Checks.Require(hit==null || surface.Owns(hit),"SURFACE_CONTEXT_STALE","Ray result belongs to a different surface snapshot.");
            return hit;
        }
        void Emit(SurfacePaintHit hit)
        {
            if(hit==null || !hit.IsUvInRange) { previousHit=null;return; }
            bool join=surface.CanJoin(previousHit,hit);
            if(join && previousHit.Uv.Equals(hit.Uv)) { previousHit=hit;return; }
            Checks.Require(PointCount<PaintPathSimplifier.MaxInputPoints,"STROKE_BUDGET_EXCEEDED","Gesture exceeds raw UV sample budget.");
            if(!join) sections.Add(new List<Vec2>());
            sections[sections.Count-1].Add(hit.Uv);PointCount++;previousHit=hit;
        }
        public PaintStrokePath Snapshot()
        {
            Checks.Require(!faulted,"STROKE_INVALIDATED","A failed gesture cannot be committed.");
            if(PointCount==0) return null;
            try
            {
                if(snapshotPoints!=PointCount) { snapshot=PaintPathSimplifier.Reduce(sections);snapshotPoints=PointCount; }
                return snapshot;
            }
            catch { faulted=true;throw; }
        }
    }
}
