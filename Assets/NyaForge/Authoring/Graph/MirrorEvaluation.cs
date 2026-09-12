using System.Text;
using NyaForge.Authoring.Topology;

namespace NyaForge.Authoring.Graph
{
    internal static class MirrorEvaluation
    {
        internal static GraphMeshValue Apply(GraphNode node, GraphMeshValue input)
        {
            if (!node.Enabled) return input;
            Checks.Require(input.Polygon != null,"EDIT_MODE_UNSUPPORTED","Mirror requires polygon input.");
            var t = input.Transform.Translation;
            float origin = node.MirrorAxis == 0 ? t.X : node.MirrorAxis == 1 ? t.Y : t.Z;
            var polygon = PolygonMirror.Apply(input.Polygon,node.NodeId,node.MirrorAxis,(node.MirrorPlane-origin)/input.Transform.Scale);
            var render = PolygonRenderAdapter.Build(polygon);
            string domain = Checks.Hash(Encoding.UTF8.GetBytes(node.NodeId + ":mirror:" + input.DomainId));
            return new GraphMeshValue(render.Mesh,input.Transform,domain,polygon,render);
        }
    }
}
