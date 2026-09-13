using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace NyaForge.Authoring.Graph
{
    public sealed class MaterialSlotBinding
    {
        public string MaterialNodeId { get; }
        public GraphMaterialValue Material { get; }
        internal MaterialSlotBinding(string nodeId,GraphMaterialValue material) { MaterialNodeId=nodeId;Material=material; }
    }
    internal static class MaterialSlotEvaluation
    {
        internal static GraphMeshValue Assign(GraphMeshValue mesh,IDictionary<int,MaterialSlotBinding> bindings, IReadOnlyList<int> declaredSlots = null)
        {
            Checks.Require(mesh.Material==null && mesh.SlotMaterials==null && mesh.BaseColor==null,"MATERIAL_ALREADY_ASSIGNED","Assign materials only once after geometry and Paint.");
            // Materialized polygon sources may not carry the optional render
            // adapter, while the assignment node still records sparse authored
            // slot keys. Prefer that declaration over assuming dense slots.
            var used=mesh.PolygonRendering?.MaterialSlotMap ?? declaredSlots ?? Enumerable.Range(0,mesh.Mesh.Submeshes.Count).ToArray();
            Checks.Require(used.All(bindings.ContainsKey),"MATERIAL_SLOT_UNASSIGNED","Every used geometry slot needs a material connection.");
            // Keep only slots that are actually represented by render
            // submeshes. An AssignMaterials node may declare a superset of
            // slots while a polygon currently uses only one of them; carrying
            // the unused binding forward makes GLB export see more materials
            // than submeshes and can silently reintroduce sparse-slot swaps.
            var selected = used.Distinct().ToDictionary(slot => slot, slot => bindings[slot]);
            foreach(var binding in selected.Values) PaintEvaluation.Bind(mesh,binding.Material.BaseColor);
            return new GraphMeshValue(mesh.Mesh,mesh.Transform,mesh.DomainId,mesh.Polygon,mesh.PolygonRendering,null,null,new ReadOnlyDictionary<int,MaterialSlotBinding>(selected));
        }
    }
}
