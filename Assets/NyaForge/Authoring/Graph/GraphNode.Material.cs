namespace NyaForge.Authoring.Graph
{
    public sealed partial class GraphNode
    {
        public MaterialParameters Material { get; private set; }
        public static GraphNode StandardMaterial(string id,MaterialParameters parameters=null)=>
            new GraphNode(id,BuiltinNodes.StandardMaterial,1,null,Identity,0,0,0,true,"","",Empty,"") { Material=parameters ?? MaterialParameters.Default };
        public static GraphNode AssignMaterial(string id)=>
            new GraphNode(id,BuiltinNodes.AssignMaterial,1,null,Identity,0,0,0,true,"","",Empty,"");
    }
}
