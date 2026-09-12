using System.Collections.Generic;

namespace NyaForge.Authoring.Rig
{
    /// <summary>Transports both Verlet history samples with center motion without integrating time.</summary>
    public static class SpringCenterMotion
    {
        public static SpringBoneState Transfer(SpringBoneState state, SpringCenterFrame previous, SpringCenterFrame current)
        {
            Checks.Require(state != null && previous != null && current != null, "INVALID_SPRING_CENTER", "State and both center frames are required.");
            previous.ValidateFor(state); current.ValidateFor(state);
            var older = new Dictionary<string, Vec3>(); var newer = new Dictionary<string, Vec3>();
            bool changed = false;
            foreach (var pair in state.CurrentTails)
            {
                var before = previous.ByBoneId[pair.Key]; var after = current.ByBoneId[pair.Key];
                bool same = before.Equals(after); changed |= !same;
                newer.Add(pair.Key, same ? pair.Value : Move(pair.Value, before, after));
                var old = state.PreviousTails[pair.Key];
                older.Add(pair.Key, same ? old : Move(old, before, after));
            }
            return changed ? SpringBoneState.Create(state.SkeletonHash, state.ChainHash, older, newer, state.PreviousDeltaTime) : state;
        }

        static Vec3 Move(Vec3 point, PoseTransform before, PoseTransform after)
        {
            var result = after.TransformPoint(before.InverseTransformPoint(point));
            Checks.Finite(result);
            return result;
        }
    }
}
