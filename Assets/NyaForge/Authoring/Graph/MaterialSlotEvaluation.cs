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
        internal static GraphMeshValue Assign(GraphMeshValue mesh,IDictionary<int,MaterialSlotBinding> bindings)
        {
            Checks.Require(mesh.Material==null && mesh.SlotMaterials==null && mesh.BaseColor==null,"MATERIAL_ALREADY_ASSIGNED","Assign materials only once after geometry and Paint.");
            var used=mesh.PolygonRendering?.MaterialSlotMap ?? Enumerable.Range(0,mesh.Mesh.Submeshes.Count).ToArray();
            Checks.Require(used.All(bindings.ContainsKey),"MATERIAL_SLOT_UNASSIGNED","Every used geometry slot needs a material connection.");
            foreach(var binding in bindings.Values) PaintEvaluation.Bind(mesh,binding.Material.BaseColor);
            return new GraphMeshValue(mesh.Mesh,mesh.Transform,mesh.DomainId,mesh.Polygon,mesh.PolygonRendering,null,null,new ReadOnlyDictionary<int,MaterialSlotBinding>(new Dictionary<int,MaterialSlotBinding>(bindings)));
        }
    }
}
