namespace NyaForge.Authoring
{
    internal static class OperationEvaluator
    {
        internal static AuthoringDocument Apply(AuthoringDocument before, AuthoringOperation operation, long revision)
        {
            if (operation.Kind.StartsWith("graph.", System.StringComparison.Ordinal) || operation.Kind == "object.add_graph")
                return GraphOperationEvaluator.Apply(before, operation, revision);
            return StaticOperationEvaluator.Apply(before, operation, revision);
        }
    }
}
