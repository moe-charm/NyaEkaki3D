using System;
using System.Runtime.CompilerServices;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring
{
    /// <summary>Canonical content identity shared by document dirty tracking and graph assets.</summary>
    internal static class GraphContentIdentity
    {
        static readonly ConditionalWeakTable<AuthoringGraph,Lazy<string>> hashes=new ConditionalWeakTable<AuthoringGraph,Lazy<string>>();
        internal static string Hash(AuthoringGraph graph)
        {
            if(graph==null) throw new ArgumentNullException(nameof(graph));
            return hashes.GetValue(graph,key=>new Lazy<string>(()=>Checks.Hash(GraphBinaryCodec.EncodeIdentity(key)))).Value;
        }
    }
}
