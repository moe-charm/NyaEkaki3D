using System.Collections.Generic;

namespace NyaForge.Authoring.Import
{
    /// <summary>Projects an already validated source node tree onto its skin joints.</summary>
    internal static class ImportedJointHierarchy
    {
        internal static Dictionary<int, int> ResolveParents(IReadOnlyList<int> joints, IReadOnlyDictionary<int, int> sourceParents)
        {
            var jointSet = new HashSet<int>(joints);
            var result = new Dictionary<int, int>();
            foreach (var joint in joints)
            {
                int ancestor = joint;
                while (sourceParents.TryGetValue(ancestor, out ancestor))
                {
                    if (!jointSet.Contains(ancestor)) continue;
                    result.Add(joint, ancestor);
                    break;
                }
            }
            return result;
        }
    }
}
