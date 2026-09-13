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

        void RefreshImportedVrmSessionsFromWorkspace()
        {
            importedRigSessions.Clear(); importedVrmSessions.Clear(); importedVrmSpringSessions.Clear();
            if (workspace == null) { importedRigSession = null; importedVrmSession = null; importedVrmSpringSession = null; return; }
            var graphIds = new HashSet<string>(workspace.Document.Objects.Where(item => item.Graph != null).Select(item => item.Graph.GraphId), StringComparer.Ordinal);
            var rigBytes = workspace.Attachments.Read(ProjectAttachments.RigSessions) ?? workspace.Attachments.Read(ProjectAttachments.Rig);
            if (rigBytes != null)
            {
                var decoded = workspace.Attachments.Read(ProjectAttachments.RigSessions) != null
                    ? ImportedRigSessionsCodec.Read(rigBytes)
                    : LegacyRigTable(rigBytes);
                foreach (var item in decoded) if (graphIds.Contains(item.Key)) importedRigSessions[item.Key] = item.Value;
            }
            DecodeExpressionSessions(graphIds);
            DecodeSpringSessions(graphIds);
            string graphId = workspace.Document.ActiveObject?.Graph?.GraphId;
            importedRigSession = graphId != null && importedRigSessions.TryGetValue(graphId, out var rig) ? rig : null;
            importedVrmSession = graphId != null && importedVrmSessions.TryGetValue(graphId, out var expression) ? expression : null;
            importedVrmSpringSession = graphId != null && importedVrmSpringSessions.TryGetValue(graphId, out var spring) ? spring : null;
        }

        static Dictionary<string, ImportedRigSession> LegacyRigTable(byte[] bytes)
        {
            var session = ImportedRigSessionCodec.Read(bytes);
            return new Dictionary<string, ImportedRigSession>(StringComparer.Ordinal) { [session.GraphId] = session };
        }

        void DecodeExpressionSessions(HashSet<string> graphIds)
        {
            var bytes = workspace.Attachments.Read(ProjectAttachments.Expressions); if (bytes == null) return;
            if (VrmExpressionSessionsCodec.IsTable(bytes)) foreach (var item in VrmExpressionSessionsCodec.Read(bytes)) if (graphIds.Contains(item.Key)) importedVrmSessions[item.Key] = item.Value;
            else if (graphIds.Count == 1) { var id = graphIds.First(); importedVrmSessions[id] = VrmExpressionSessionCodec.Read(bytes); }
        }

        void DecodeSpringSessions(HashSet<string> graphIds)
        {
            var bytes = workspace.Attachments.Read(ProjectAttachments.Springs); if (bytes == null) return;
            if (VrmSpringSessionsCodec.IsTable(bytes)) foreach (var item in VrmSpringSessionsCodec.Read(bytes)) if (graphIds.Contains(item.Key)) importedVrmSpringSessions[item.Key] = item.Value;
            else if (graphIds.Count == 1) { var id = graphIds.First(); importedVrmSpringSessions[id] = VrmSpringSessionCodec.Read(bytes); }
        }

        static string LegacyVrmSessionGraphId(AuthoringWorkspace value, ImportedRigSession rig)
        {
            if (rig != null) return rig.GraphId;
            var graphs = value?.Document?.Objects?.Where(item => item.Graph != null).Select(item => item.Graph.GraphId).Distinct().ToArray();
            if (graphs?.Length == 1) return graphs[0];
            // A legacy sidecar has no object identity. Do not infer the
            // selected graph when several graph objects exist; doing so can
            // silently attach avatar A's expressions/Spring to object B.
            return null;
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
