using System.Linq;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring
{
    /// <summary>Resolves a committed document to the static Bake profile, never to a stale preview.</summary>
    internal sealed class BakeSource
    {
        internal MeshData Mesh;
        internal RestTransform Transform;
        internal string BaselineHash;
        internal bool PositionsEdited;
        internal GraphImageValue BaseColor;
        internal MaterialParameters Material;
        internal GraphMeshValue SlotOutput;

        internal static BakeSource Capture(AuthoringDocument document) => Capture(document, false);
        internal static BakeSource CaptureSurface(AuthoringDocument document) => Capture(document, true);
        internal static BakeSource CaptureMaterial(AuthoringDocument document)=>Capture(document,true,true);
        internal static BakeSource CaptureMaterials(AuthoringDocument document)=>Capture(document,true,true,true);
        static BakeSource Capture(AuthoringDocument document, bool allowBaseColor,bool allowMaterial=false,bool allowSlots=false)
        {
            Checks.Require(!document.IsEmpty, "NO_EXPORTABLE_OBJECT", "Add a mesh before exporting.");
            var item = document.Objects[0];
            if (item.IsStaticProfile)
                return new BakeSource { Mesh = document.Evaluate(), Transform = item.Transform, BaselineHash = item.BaselineMesh.ContentHash,
                    PositionsEdited = item.LayerEnabled && item.Offsets.Count > 0 };
            var graph = item.Graph; var result = item.EvaluateGraph();
            Checks.Require(result.IsComplete, "GRAPH_INCOMPLETE", "Resolve graph diagnostics before exporting; a previous preview cannot be exported.");
            var output = result.Output;
            Checks.Require(output.Mesh!=null,"NO_RENDERABLE_FACES","Create a face before exporting.");
            Checks.Require(allowSlots || output.SlotMaterials==null,"EXPORT_UNSUPPORTED_FEATURE","Multiple material slots require their own Bake profile.");
            Checks.Require(allowMaterial || output.Material==null,"EXPORT_UNSUPPORTED_FEATURE","Use the material Bake profile to preserve material parameters.");
            Checks.Require(allowBaseColor || output.BaseColor == null,"EXPORT_UNSUPPORTED_FEATURE","This static Bake profile cannot export paint textures. Use the surface profile or keep the native project.");
            string current = graph.OutputNodeId; bool edited = false;
            while (true)
            {
                var node = graph.Nodes[current];
                if (node.TypeId == BuiltinNodes.MeshSource || node.TypeId == BuiltinNodes.Plane || node.TypeId == BuiltinNodes.PolygonSource)
                    return new BakeSource { Mesh = output.Mesh, Transform = output.Transform,
                        BaselineHash = result.MeshOutputs[current].Mesh?.ContentHash ?? NyaForge.Authoring.Topology.PolygonDerivedData.ContentHash(result.MeshOutputs[current].Polygon), PositionsEdited = edited, BaseColor = output.BaseColor,Material=output.Material?.Parameters,SlotOutput=output.SlotMaterials==null ? null : output };
                Checks.Require(node.TypeId == BuiltinNodes.EditMesh || node.TypeId == BuiltinNodes.Output || node.TypeId == BuiltinNodes.PolygonEdit || node.TypeId == BuiltinNodes.Mirror || allowMaterial && node.TypeId==BuiltinNodes.AssignMaterial || allowSlots && node.TypeId==BuiltinNodes.AssignMaterials,
                    "EXPORT_UNSUPPORTED_FEATURE", "This node needs a static Bake provenance adapter.");
                edited |= node.TypeId == BuiltinNodes.EditMesh && node.Enabled && node.Offsets.Count > 0;
                edited |= node.TypeId == BuiltinNodes.PolygonEdit && node.Enabled && node.SourcePolygon != null;
                edited |= node.TypeId == BuiltinNodes.Mirror && node.Enabled;
                var input = graph.Edges.SingleOrDefault(e => e.ToNode == current && e.ToPort == "mesh");
                Checks.Require(input != null, "GRAPH_INCOMPLETE", "Export ancestry is incomplete.");
                current = input.FromNode;
            }
        }
    }
}


