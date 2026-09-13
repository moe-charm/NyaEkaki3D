using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Simulation;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        VrmExpressionSession importedVrmSession;
        VrmSpringSession importedVrmSpringSession;
        readonly Dictionary<string, VrmExpressionSession> importedVrmSessions = new Dictionary<string, VrmExpressionSession>(StringComparer.Ordinal);
        readonly Dictionary<string, VrmSpringSession> importedVrmSpringSessions = new Dictionary<string, VrmSpringSession>(StringComparer.Ordinal);
        readonly Dictionary<string, SecondaryMotionAsset> importedSecondaryMotionSessions = new Dictionary<string, SecondaryMotionAsset>(StringComparer.Ordinal);

        void RefreshImportedVrmSessionsForActiveGraph()
        {
            string graphId = workspace?.Document?.ActiveObject?.Graph?.GraphId;
            // Legacy projects may contain a single metadata blob without a graph id.
            // Keep the explicitly decoded session in that case so old projects do not
            // lose their metadata during the post-open refresh.
            if (graphId == null) return;
            importedVrmSession = graphId != null && importedVrmSessions.TryGetValue(graphId, out var expression) ? expression : null;
            importedVrmSpringSession = graphId != null && importedVrmSpringSessions.TryGetValue(graphId, out var spring) ? spring : null;
        }

        static string LegacyVrmSessionGraphId(AuthoringWorkspace value, ImportedRigSession rig)
        {
            if (rig != null) return rig.GraphId;
            var graphs = value?.Document?.Objects?.Where(item => item.Graph != null).Select(item => item.Graph.GraphId).Distinct().ToArray();
            if (graphs?.Length == 1) return graphs[0];
            return value?.Document?.ActiveObject?.Graph?.GraphId;
        }

        void ClearImportedVrmExpressionTable()
        {
            importedVrmSessions.Clear(); importedVrmSession = null;
        }

        void ClearImportedVrmSpringTable()
        {
            importedVrmSpringSessions.Clear(); importedVrmSpringSession = null;
        }

        void ClearImportedSecondaryMotionTable()
        {
            importedSecondaryMotionSessions.Clear(); importedSecondaryMotionDocument = null; importedSecondaryMotionAsset = null;
        }
    }
}
