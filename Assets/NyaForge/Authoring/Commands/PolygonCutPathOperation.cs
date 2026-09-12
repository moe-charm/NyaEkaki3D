using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;

namespace NyaForge.Authoring
{
    public sealed partial class AuthoringOperation
    {
        public IReadOnlyList<EdgeCutLocation> CutPath { get; private set; }
        public static AuthoringOperation CutPolygonPath(GraphEditContext context,IEnumerable<EdgeCutLocation> path)
        {
            Checks.Require(context!=null && path!=null,"INVALID_SELECTION","Specify an edit context and cut path.");
            var copy=path.Take(257).ToArray();
            Checks.Require(copy.Length>=2 && copy.Length<=256 && copy.All(p=>p!=null),"INVALID_CUT_PATH","A path requires 2..256 edge locations.");
            return new AuthoringOperation("graph.polygon.cut-path",Array.Empty<int>(),new Vec3(),false)
                { EditContext=context,CutPath=Array.AsReadOnly(copy) };
        }
        private void WriteCutPathFingerprint(BinaryWriter writer)
        {
            if(CutPath==null) return;
            writer.Write(CutPath.Count);
            foreach(var point in CutPath)
            { writer.Write(point.Edge.A);writer.Write(point.Edge.B);writer.Write(Checks.Canonical(point.Fraction)); }
        }
    }
}
