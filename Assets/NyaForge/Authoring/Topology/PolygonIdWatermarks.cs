using System;

namespace NyaForge.Authoring.Topology
{
    /// <summary>Highest allocated IDs in this polygon snapshot's edit lineage, including deleted elements.</summary>
    public sealed class PolygonIdWatermarks
    {
        public ulong Vertex { get; }
        public ulong Face { get; }
        public ulong Corner { get; }
        public PolygonIdWatermarks(ulong vertex,ulong face,ulong corner) { Vertex=vertex;Face=face;Corner=corner; }
        internal PolygonIdWatermarks Include(PolygonIdWatermarks live)=>new PolygonIdWatermarks(Math.Max(Vertex,live.Vertex),Math.Max(Face,live.Face),Math.Max(Corner,live.Corner));
        internal bool SameAs(PolygonIdWatermarks other)=>Vertex==other.Vertex && Face==other.Face && Corner==other.Corner;
    }
}
