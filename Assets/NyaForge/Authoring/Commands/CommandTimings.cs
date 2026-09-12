using System;
using System.Diagnostics;

namespace NyaForge.Authoring
{
    /// <summary>Opt-in, per-call diagnostic data. No callbacks and no effect on command success.</summary>
    [Serializable]
    public sealed class CommandTimings
    {
        public double ValidationMs,CandidateMs,EvaluationMs,ProjectionMs,CommitMs,CleanupMs,TotalMs;
        long start,last;
        internal void Begin() { ValidationMs=CandidateMs=EvaluationMs=ProjectionMs=CommitMs=CleanupMs=TotalMs=0;start=last=Stopwatch.GetTimestamp(); }
        double Lap() { long now=Stopwatch.GetTimestamp();double ms=(now-last)*1000.0/Stopwatch.Frequency;last=now;return ms; }
        internal void Validated()=>ValidationMs=Lap();
        internal void CandidateBuilt()=>CandidateMs=Lap();
        internal void Evaluated()=>EvaluationMs=Lap();
        internal void Projected()=>ProjectionMs=Lap();
        internal void Committed()=>CommitMs=Lap();
        internal void End() { CleanupMs=Lap();TotalMs=(last-start)*1000.0/Stopwatch.Frequency; }
    }
}
