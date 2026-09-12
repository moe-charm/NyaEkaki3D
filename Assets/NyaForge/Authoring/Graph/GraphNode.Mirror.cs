namespace NyaForge.Authoring.Graph
{
    public sealed partial class GraphNode
    {
        public int MirrorAxis { get; private set; }
        public float MirrorPlane { get; private set; }
        public static GraphNode Mirror(string id, int axis = 0, float plane = 0, bool enabled = true)
        {
            Checks.Require(axis >= 0 && axis <= 2,"INVALID_MIRROR_AXIS","Mirror axis must be X, Y or Z."); Checks.Finite(plane);
            return new GraphNode(id,BuiltinNodes.Mirror,1,null,Identity,0,0,0,enabled,"","",Empty,"") { MirrorAxis = axis, MirrorPlane = plane };
        }
    }
}
