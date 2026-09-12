using System;
using System.Collections.Generic;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;

internal static partial class Program
{
    static void RunVrmSessionTableTests()
    {
        Test("VRM expression and Spring session tables preserve graph identity", () =>
        {
            var metadata = VrmMetadataReader.Read(BuildMappedVrm(false));
            var expression = VrmExpressionSession.Create(metadata, Array.Empty<MappedVrmExpression>());
            var spring = VrmSpringSession.Create(metadata);
            string first = Guid.NewGuid().ToString("D"), second = Guid.NewGuid().ToString("D");
            var expressions = new Dictionary<string, VrmExpressionSession> { [second] = expression, [first] = expression };
            var springs = new Dictionary<string, VrmSpringSession> { [second] = spring, [first] = spring };
            var expressionBytes = VrmExpressionSessionsCodec.Write(expressions);
            var springBytes = VrmSpringSessionsCodec.Write(springs);
            True(VrmExpressionSessionsCodec.IsTable(expressionBytes)); True(VrmSpringSessionsCodec.IsTable(springBytes));
            var expressionRoundTrip = VrmExpressionSessionsCodec.Read(expressionBytes); var springRoundTrip = VrmSpringSessionsCodec.Read(springBytes);
            Equal(2, expressionRoundTrip.Count); Equal(2, springRoundTrip.Count); Equal(expression.SourceHash, expressionRoundTrip[first].SourceHash); Equal(spring.SourceHash, springRoundTrip[second].SourceHash);
            Expect("INVALID_VRM", () => VrmExpressionSessionsCodec.Read(new byte[] { 1, 2, 3, 4 }));
            Expect("INVALID_VRM", () => VrmSpringSessionsCodec.Read(new byte[] { 1, 2, 3, 4 }));
        });
    }
}
