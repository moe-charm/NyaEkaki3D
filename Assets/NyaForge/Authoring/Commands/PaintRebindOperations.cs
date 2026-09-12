using System;
using System.IO;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring
{
    public sealed partial class AuthoringOperation
    {
        public PaintRebindContext PaintRebind { get; private set; }
        public static AuthoringOperation RebindPaintImage(PaintRebindContext context)
        {
            Checks.Require(context != null, "PAINT_CONTEXT_STALE", "A reviewed binding is required.");
            return new AuthoringOperation("graph.paint.rebind", Array.Empty<int>(), new Vec3(), false) { PaintRebind = context };
        }
        void WritePaintRebindFingerprint(BinaryWriter writer)
        {
            if (PaintRebind == null) return;
            writer.Write(PaintRebind.GraphId); writer.Write(PaintRebind.NodeId); writer.Write(PaintRebind.ImageHash);
            writer.Write(PaintRebind.OldUvHash); writer.Write(PaintRebind.OldDomain);
            writer.Write(PaintRebind.NewUvHash); writer.Write(PaintRebind.NewDomain);
            writer.Write(PaintRebind.InputNodeId); writer.Write(PaintRebind.InputSnapshot);
            if (PaintRebind.LayerStackHash != "") writer.Write(PaintRebind.LayerStackHash);
        }
    }
}
