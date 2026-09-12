using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring
{
    /// <summary>Current evaluation and explicitly revisioned last successful output.</summary>
    public sealed class AuthoringPreview
    {
        public long DocumentRevision { get; private set; }
        public long? OutputRevision { get; private set; }
        public string ObjectId { get; private set; }
        public GraphEvaluation Evaluation { get; private set; }
        public GraphMeshValue Output { get; private set; }
        public bool IsComplete { get { return Evaluation == null || Evaluation.IsComplete; } }
        public bool IsStale { get { return Output != null && OutputRevision != DocumentRevision; } }

        internal static AuthoringPreview Create(AuthoringDocument doc, AuthoringPreview previous)
        {
            var result = new AuthoringPreview { DocumentRevision = doc.DocumentRevision, ObjectId = doc.ObjectId };
            if (doc.IsEmpty) return result;
            result.Evaluation = doc.Objects[0].EvaluateGraph();
            if (doc.Objects[0].IsStaticProfile && !result.Evaluation.IsComplete)
            {
                var failure = result.Evaluation.Diagnostics[0];
                throw new AuthoringException(failure.Code, failure.Message);
            }
            if (result.Evaluation.IsComplete)
            {
                result.Output = result.Evaluation.Output; result.OutputRevision = doc.DocumentRevision;
            }
            else if (previous != null && previous.ObjectId == doc.ObjectId)
            {
                result.Output = previous.Output; result.OutputRevision = previous.OutputRevision;
            }
            return result;
        }
    }

    public interface IGraphAuthoringProjection : IAuthoringProjection
    {
        IPreparedProjection PrepareGraph(AuthoringDocument candidate, AuthoringPreview preview);
    }
}
