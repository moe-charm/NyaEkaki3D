using System;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring
{
    public sealed partial class AuthoringOperation
    {
        public int MaterialSlot { get; private set; }
        public string MaterialNodeId { get; private set; }
        public static AuthoringOperation AssignPolygonMaterial(GraphEditContext context,ulong[] faces,int slot,string materialNodeId="")
        {
            Checks.Require(context!=null && faces!=null && faces.Length<=AuthoringLimits.MaxIndices/3,"INVALID_SELECTION","Bounded face selection is required.");
            if(materialNodeId!="") Checks.Id(materialNodeId);
            return new AuthoringOperation("graph.polygon.material",Array.Empty<int>(),new Vec3(),false)
                { EditContext=context,ElementIds=Array.AsReadOnly((ulong[])faces.Clone()),MaterialSlot=slot,MaterialNodeId=materialNodeId };
        }
    }
}
