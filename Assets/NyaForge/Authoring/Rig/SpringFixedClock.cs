using System;

namespace NyaForge.Authoring.Rig
{
    internal static class SpringFixedClock
    {
        internal const double Interval = 1.0 / 60.0;
        internal const int MaxSteps = 16;

        internal static int Plan(double pending, float elapsed, out double remainder)
        {
            Checks.Finite(elapsed);
            Checks.Require(elapsed >= 0 && elapsed <= SpringBoneSimulator.MaxDeltaTime, "INVALID_DELTA_TIME", "Preview frame time must be between 0 and 0.25 seconds.");
            double total = pending + elapsed;
            // Float frame times accumulate a few nanoseconds of rounding near an exact step.
            int count = (int)Math.Floor((total + 1e-7) / Interval);
            Checks.Require(count <= MaxSteps, "SPRING_STEP_BUDGET", "Preview frame exceeds the fixed-step budget.");
            remainder = Math.Max(0, total - count * Interval);
            return count;
        }
    }
}
