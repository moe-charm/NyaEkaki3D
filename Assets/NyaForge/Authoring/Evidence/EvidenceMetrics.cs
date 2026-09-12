using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Graph;
namespace NyaForge.Authoring.Evidence
{
    /// <summary>Metrics from the acquired final value. Bounds use render positions, or all loose points for a faceless output.</summary>
    public sealed class EvidenceMetrics
    {
        public int RenderVertexCount { get; }
        public int TriangleCount { get; }
        public int RenderSubmeshCount { get; }
        public int? LogicalVertexCount { get; }
        public int? PolygonFaceCount { get; }
        public int AssignedMaterialCount { get; }
        public Vec3? BoundsMin { get; }
        public Vec3? BoundsMax { get; }
        public IReadOnlyList<string> ImageHashes { get; }
        internal EvidenceMetrics(GraphMeshValue value)
        {
            RenderVertexCount=value.Mesh?.VertexCount ?? 0;TriangleCount=value.Mesh?.TriangleCount ?? 0;RenderSubmeshCount=value.Mesh?.Submeshes.Count ?? 0;
            LogicalVertexCount=value.Polygon?.Vertices.Count;PolygonFaceCount=value.Polygon?.Faces.Count;
            AssignedMaterialCount=value.SlotMaterials?.Count ?? (value.Material==null ? 0 : 1);
            var images=new List<string>();
            if(value.BaseColor!=null) images.Add(value.BaseColor.ImageHash);
            if(value.Material?.BaseColor!=null) images.Add(value.Material.BaseColor.ImageHash);
            if(value.SlotMaterials!=null) foreach(var slot in value.SlotMaterials.Values) if(slot.Material.BaseColor!=null) images.Add(slot.Material.BaseColor.ImageHash);
            ImageHashes=Array.AsReadOnly(images.Distinct().OrderBy(h=>h,StringComparer.Ordinal).ToArray());
            var positions=value.Mesh!=null ? value.Mesh.Positions : value.Polygon.Vertices.Values.Select(v=>v.Position);
            bool any=false;Vec3 min=new Vec3(),max=new Vec3();
            foreach(var local in positions)
            {
                var p=value.Transform.ToAvatarPoint(local);
                if(!any) { min=max=p;any=true; }
                else { min=new Vec3(Math.Min(min.X,p.X),Math.Min(min.Y,p.Y),Math.Min(min.Z,p.Z));max=new Vec3(Math.Max(max.X,p.X),Math.Max(max.Y,p.Y),Math.Max(max.Z,p.Z)); }
            }
            if(any) { BoundsMin=min;BoundsMax=max; }
        }
    }
}
