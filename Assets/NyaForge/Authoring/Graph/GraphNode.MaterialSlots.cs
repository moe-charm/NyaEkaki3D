using System;
using System.Collections.Generic;
using System.Linq;
namespace NyaForge.Authoring.Graph
{
    public sealed partial class GraphNode
    {
        public IReadOnlyList<int> MaterialSlots { get; private set; }
        public static string MaterialSlotPort(int slot)=>"material-"+slot.ToString(System.Globalization.CultureInfo.InvariantCulture);
        public static GraphNode AssignMaterials(string id,IEnumerable<int> slots)
        {
            Checks.Require(slots!=null,"INVALID_MATERIAL_SLOTS","Material slots are required.");
            var items=slots.Take(AuthoringLimits.MaxSubmeshes+1).ToArray();
            Checks.Require(items.Length>0 && items.Length<=AuthoringLimits.MaxSubmeshes && items.Distinct().Count()==items.Length && items.All(s=>s>=0 && s<AuthoringLimits.MaxSubmeshes),"INVALID_MATERIAL_SLOTS","Use distinct material slot keys within budget.");
            Array.Sort(items);
            return new GraphNode(id,BuiltinNodes.AssignMaterials,1,null,Identity,0,0,0,true,"","",Empty,"") { MaterialSlots=Array.AsReadOnly(items) };
        }
    }
}
