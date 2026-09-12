using System;
using System.Runtime.CompilerServices;
using NyaForge.Authoring.Paint;

namespace NyaForge.Authoring.Topology
{
    /// <summary>Derived immutable values owned by polygon snapshot lifetime, never a global content-hash registry.</summary>
    internal static class PolygonDerivedData
    {
        sealed class Entry
        {
            internal readonly Lazy<PolygonRenderMesh> Render;
            internal readonly Lazy<string> ContentHash,UvHash;
            internal Entry(PolygonMesh source)
            {
                Render=new Lazy<PolygonRenderMesh>(()=>PolygonRenderAdapter.BuildUncached(source));
                ContentHash=new Lazy<string>(()=>Checks.Hash(PolygonBinaryCodec.Write(source)));
                UvHash=new Lazy<string>(()=>PaintUvBinding.HashUncached(source));
            }
        }
        static readonly ConditionalWeakTable<PolygonMesh,Entry> entries=new ConditionalWeakTable<PolygonMesh,Entry>();
        static Entry Get(PolygonMesh source)
        {
            if(source==null) throw new ArgumentNullException(nameof(source));
            return entries.GetValue(source,key=>new Entry(key));
        }
        internal static PolygonRenderMesh Render(PolygonMesh source)=>Get(source).Render.Value;
        internal static string ContentHash(PolygonMesh source)=>Get(source).ContentHash.Value;
        internal static string UvHash(PolygonMesh source)=>Get(source).UvHash.Value;
    }
}
