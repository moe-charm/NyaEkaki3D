using System;
using System.IO;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;

namespace NyaForge.Authoring
{
    public sealed partial class AuthoringOperation
    {
        public LayerEditContext LayerContext { get; private set; }
        public PaintLayerChange LayerChange { get; private set; }
        public PaintEditContext MigrationContext { get; private set; }
        public string MigrationLayerId { get; private set; }
        public static AuthoringOperation EditPaintLayers(LayerEditContext context,PaintLayerChange change)
        {
            Checks.Require(context!=null && change!=null,"PAINT_CONTEXT_STALE","Layer context and change are required.");
            return new AuthoringOperation("graph.layers.edit",Array.Empty<int>(),new Vec3(),false) { LayerContext=context,LayerChange=change };
        }
        public static AuthoringOperation MigratePaintLayers(PaintEditContext context,string layerId)
        {
            Checks.Require(context!=null,"PAINT_CONTEXT_STALE","Paint context is required.");Checks.Id(layerId);
            return new AuthoringOperation("graph.layers.migrate",Array.Empty<int>(),new Vec3(),false) { MigrationContext=context,MigrationLayerId=layerId };
        }
        void WriteLayerFingerprint(BinaryWriter writer)
        {
            if(LayerContext!=null)
            {
                writer.Write(LayerContext.GraphId);writer.Write(LayerContext.NodeId);writer.Write(LayerContext.StackHash);
                writer.Write(LayerContext.UvHash);writer.Write(LayerContext.MeshDomain);LayerChange.Fingerprint(writer);
            }
            if(MigrationContext!=null)
            {
                writer.Write(MigrationContext.GraphId);writer.Write(MigrationContext.NodeId);writer.Write(MigrationContext.ImageHash);
                writer.Write(MigrationContext.UvHash);writer.Write(MigrationContext.MeshDomain);writer.Write(MigrationLayerId);
            }
        }
    }
}
