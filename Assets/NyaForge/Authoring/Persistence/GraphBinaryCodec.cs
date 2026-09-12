using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring
{
    // Explicit binary envelope; no runtime type names or serializer polymorphism.
    internal static class GraphBinaryCodec
    {
        const int Magic = 0x4746594e; // NYFG
        const int Version = 1;
        const int MaxPayloadBytes = 32768;
        static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        static readonly ConditionalWeakTable<GraphNode,Lazy<byte[]>> identityPayloads=new ConditionalWeakTable<GraphNode,Lazy<byte[]>>();

        internal static byte[] Encode(AuthoringGraph graph, Func<byte[], string> addBlob)
            =>EncodeCore(graph,addBlob,false);
        // Identity-only encoding has no persistence callback. Real saves always visit every blob.
        internal static byte[] EncodeIdentity(AuthoringGraph graph)=>EncodeCore(graph,Checks.Hash,true);
        static byte[] EncodeCore(AuthoringGraph graph,Func<byte[],string> addBlob,bool identityOnly)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream, Utf8))
            {
                writer.Write(Magic); writer.Write(Version);
                Text(writer, graph.GraphId); Text(writer, graph.OutputNodeId);
                writer.Write(graph.Nodes.Count); writer.Write(graph.Edges.Count);
                foreach (var node in graph.Nodes.Values.OrderBy(n => n.NodeId, StringComparer.Ordinal))
                {
                    Text(writer, node.NodeId); Text(writer, node.TypeId); writer.Write(node.Version);
                    var payload = identityOnly ? identityPayloads.GetValue(node,key=>new Lazy<byte[]>(()=>EncodeNode(key,Checks.Hash))).Value : EncodeNode(node, addBlob);
                    Checks.Require(payload.Length <= MaxPayloadBytes, "BUDGET_EXCEEDED", "Node payload exceeds capacity.");
                    writer.Write(payload.Length); writer.Write(payload);
                }
                foreach (var edge in graph.Edges.OrderBy(e => e.ToNode, StringComparer.Ordinal).ThenBy(e => e.ToPort, StringComparer.Ordinal))
                {
                    Text(writer, edge.FromNode); Text(writer, edge.FromPort); Text(writer, edge.ToNode); Text(writer, edge.ToPort);
                }
                Checks.Require(stream.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Graph blob exceeds capacity.");
                return stream.ToArray();
            }
        }

        static byte[] EncodeNode(GraphNode node, Func<byte[], string> addBlob)
        {
            if (BuiltinNodes.Find(node) == null) return node.UnknownPayloadBytes.ToArray();
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream, Utf8))
            {
                switch (node.TypeId)
                {
                    case BuiltinNodes.MeshSource:
                        Text(writer, addBlob(MeshBinary.Write(node.SourceMesh)));
                        writer.Write(Checks.Canonical(node.Transform.Scale)); MeshBinary.Write(writer, node.Transform.Translation);
                        break;
                    case BuiltinNodes.PolygonSource:
                        Text(writer, addBlob(PolygonBinaryCodec.Write(node.SourcePolygon)));
                        writer.Write(Checks.Canonical(node.Transform.Scale)); MeshBinary.Write(writer, node.Transform.Translation); break;
                    case BuiltinNodes.PolygonEdit:
                        writer.Write(node.Enabled); Text(writer, node.ExpectedInputSnapshot); Text(writer, node.ExpectedDomain);
                        Text(writer, node.SourcePolygon == null ? "" : addBlob(PolygonBinaryCodec.Write(node.SourcePolygon))); break;
                    case BuiltinNodes.Plane:
                        writer.Write(Checks.Canonical(node.Width)); writer.Write(Checks.Canonical(node.Height)); break;
                    case BuiltinNodes.Scalar:
                        writer.Write(Checks.Canonical(node.Scalar)); break;
                    case BuiltinNodes.EditMesh:
                        writer.Write(node.Enabled); Text(writer, node.ExpectedInputSnapshot); Text(writer, node.ExpectedDomain);
                        Text(writer, addBlob(DeltaBinary.Write(node.Offsets))); break;
                    case BuiltinNodes.Output: break;
                    case BuiltinNodes.StandardMaterial: node.Material.Write(writer);break;
                    case BuiltinNodes.AssignMaterial: break;
                    case BuiltinNodes.AssignMaterials: writer.Write(node.MaterialSlots.Count);foreach(int slot in node.MaterialSlots) writer.Write(slot);break;
                    case BuiltinNodes.Skeleton: Text(writer, addBlob(RigCodec.WriteSkeleton(node.Skeleton))); break;
                    case BuiltinNodes.SkinBind: Text(writer, addBlob(RigCodec.WriteBinding(node.Binding))); break;
                    case BuiltinNodes.Pose: Text(writer, addBlob(PoseCodec.Write(node.Pose))); break;
                    case BuiltinNodes.SkinDeform: break;
                    case BuiltinNodes.Paint:
                        writer.Write(node.PaintWidth); writer.Write(node.PaintHeight); Text(writer,node.PaintUvHash); Text(writer,node.ExpectedDomain);
                        Text(writer,node.PaintImage == null ? "" : addBlob(PaintImageCodec.Write(node.PaintImage))); break;
                    case BuiltinNodes.LayeredPaint:
                        Text(writer,addBlob(PaintLayersCodec.Write(node.LayerStack,addBlob)));
                        Text(writer,node.PaintUvHash);Text(writer,node.ExpectedDomain);break;
                    case BuiltinNodes.Mirror:
                        writer.Write(node.MirrorAxis); writer.Write(Checks.Canonical(node.MirrorPlane)); writer.Write(node.Enabled); break;
                    default: throw new AuthoringException("UNSUPPORTED_NODE", "No codec for registered node.");
                }
                return stream.ToArray();
            }
        }

        internal static AuthoringGraph Decode(byte[] bytes, Func<string, byte[]> readBlob)
        {
            try
            {
                using (var stream = new MemoryStream(bytes, false)) using (var reader = new BinaryReader(stream, Utf8))
                {
                    Checks.Require(reader.ReadInt32() == Magic, "INVALID_BLOB", "Not a graph blob.");
                    Checks.Require(reader.ReadInt32() == Version, "UNSUPPORTED_FORMAT", "Unsupported graph wire version.");
                    string graphId = Text(reader, 36), outputId = Text(reader, 36);
                    int nodeCount = Count(reader, AuthoringGraph.MaxNodes), edgeCount = Count(reader, AuthoringGraph.MaxEdges);
                    var nodes = new List<GraphNode>(); var edges = new List<GraphEdge>();
                    for (int i = 0; i < nodeCount; i++)
                    {
                        string id = Text(reader, 36), type = Text(reader, 512); int version = reader.ReadInt32();
                        int count = Count(reader, MaxPayloadBytes); byte[] payload = Exact(reader, count);
                        nodes.Add(DecodeNode(id, type, version, payload, readBlob));
                    }
                    for (int i = 0; i < edgeCount; i++) edges.Add(new GraphEdge(Text(reader,36), Text(reader,512), Text(reader,36), Text(reader,512)));
                    Checks.Require(stream.Position == stream.Length, "INVALID_BLOB", "Trailing graph bytes.");
                    return new AuthoringGraph(graphId, nodes, edges, outputId);
                }
            }
            catch (EndOfStreamException e) { throw new AuthoringException("INVALID_BLOB", e.Message); }
            catch (DecoderFallbackException e) { throw new AuthoringException("INVALID_BLOB", e.Message); }
        }

        static GraphNode DecodeNode(string id, string type, int version, byte[] payload, Func<string, byte[]> readBlob)
        {
            NodeDefinition definition;
            if (!BuiltinNodes.Definitions.TryGetValue(type, out definition) || definition.Version != version)
                return GraphNode.UnknownBinary(id, type, version, payload);
            using (var stream = new MemoryStream(payload, false)) using (var reader = new BinaryReader(stream, Utf8))
            {
                GraphNode node;
                switch (type)
                {
                    case BuiltinNodes.MeshSource:
                        var mesh = MeshBinary.Read(readBlob(Text(reader,64)));
                        var transform = new RestTransform(reader.ReadSingle(), new Vec3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle()));
                        node = GraphNode.Source(id, mesh, transform); break;
                    case BuiltinNodes.PolygonSource:
                        var polygon = PolygonBinaryCodec.Read(readBlob(Text(reader,64)));
                        node = GraphNode.Polygon(id, polygon, new RestTransform(reader.ReadSingle(), MeshBinary.ReadVector(reader))); break;
                    case BuiltinNodes.PolygonEdit:
                        byte polygonEnabled = reader.ReadByte(); Checks.Require(polygonEnabled <= 1, "INVALID_BLOB", "Invalid boolean encoding.");
                        string polygonSnapshot = Text(reader,64), polygonDomain = Text(reader,64), polygonHash = Text(reader,64);
                        node = GraphNode.PolygonEdit(id, polygonHash == "" ? null : PolygonBinaryCodec.Read(readBlob(polygonHash)), polygonSnapshot, polygonDomain, polygonEnabled != 0); break;
                    case BuiltinNodes.Plane:
                        node = GraphNode.Plane(id, reader.ReadSingle(), reader.ReadSingle()); break;
                    case BuiltinNodes.Scalar:
                        node = GraphNode.Number(id, reader.ReadSingle()); break;
                    case BuiltinNodes.EditMesh:
                        byte flag = reader.ReadByte(); Checks.Require(flag <= 1, "INVALID_BLOB", "Invalid boolean encoding.");
                        string snapshot = Text(reader,64), domain = Text(reader,64);
                        var offsets = DeltaBinary.Read(readBlob(Text(reader,64)), AuthoringLimits.MaxVertices);
                        node = GraphNode.Edit(id,flag == 1,new Dictionary<int, Vec3>(offsets),snapshot,domain); break;
                    case BuiltinNodes.Output: node = GraphNode.Output(id); break;
                    case BuiltinNodes.StandardMaterial: node=GraphNode.StandardMaterial(id,MaterialParameters.Read(reader));break;
                    case BuiltinNodes.AssignMaterial: node=GraphNode.AssignMaterial(id);break;
                    case BuiltinNodes.AssignMaterials:
                        int slotsCount=Count(reader,AuthoringLimits.MaxSubmeshes);var slots=new int[slotsCount];
                        for(int i=0;i<slots.Length;i++) { slots[i]=reader.ReadInt32();Checks.Require(i==0 || slots[i]>slots[i-1],"INVALID_BLOB","Material slots must be canonical sorted keys."); }
                        node=GraphNode.AssignMaterials(id,slots);break;
                    case BuiltinNodes.Skeleton:
                        node=GraphNode.SkeletonNode(id,RigCodec.ReadSkeleton(readBlob(Text(reader,64))));break;
                    case BuiltinNodes.SkinBind:
                        // Binding is checked against graph inputs during evaluation; the blob carries its identities.
                        node=GraphNode.SkinBindNode(id,RigCodec.ReadBindingUnbound(readBlob(Text(reader,64))));break;
                    case BuiltinNodes.Pose:
                        node=GraphNode.PoseNode(id,PoseCodec.ReadUnbound(readBlob(Text(reader,64))));break;
                    case BuiltinNodes.SkinDeform:
                        node=GraphNode.SkinDeformNode(id);break;
                    case BuiltinNodes.Paint:
                        int paintWidth=reader.ReadInt32(),paintHeight=reader.ReadInt32();
                        NyaForge.Authoring.Paint.PaintImage.ValidateDimensions(paintWidth,paintHeight);
                        string uvBinding=Text(reader,64),paintDomain=Text(reader,64),imageHash=Text(reader,64);
                        node=GraphNode.Paint(id,paintWidth,paintHeight,imageHash == "" ? null : PaintImageCodec.Read(readBlob(imageHash)),uvBinding,paintDomain); break;
                    case BuiltinNodes.LayeredPaint:
                        var layers=PaintLayersCodec.Read(readBlob(Text(reader,64)),readBlob);
                        node=GraphNode.LayeredPaint(id,layers,Text(reader,64),Text(reader,64));break;
                    case BuiltinNodes.Mirror:
                        int axis = reader.ReadInt32(); float plane = reader.ReadSingle(); byte mirrorEnabled = reader.ReadByte();
                        Checks.Require(mirrorEnabled <= 1,"INVALID_BLOB","Invalid boolean encoding.");
                        node = GraphNode.Mirror(id,axis,plane,mirrorEnabled != 0); break;
                    default: throw new AuthoringException("UNSUPPORTED_NODE", "No codec for registered node.");
                }
                Checks.Require(stream.Position == stream.Length, "INVALID_BLOB", "Trailing node payload bytes.");
                return node;
            }
        }

        static int Count(BinaryReader reader, int maximum)
        {
            int count = reader.ReadInt32();
            Checks.Require(count >= 0 && count <= maximum, "BUDGET_EXCEEDED", "Binary count exceeds capacity.");
            return count;
        }
        static byte[] Exact(BinaryReader reader, int count)
        {
            Checks.Require(count <= reader.BaseStream.Length - reader.BaseStream.Position, "INVALID_BLOB", "Truncated payload.");
            var bytes = reader.ReadBytes(count);
            Checks.Require(bytes.Length == count, "INVALID_BLOB", "Truncated payload."); return bytes;
        }
        static void Text(BinaryWriter writer, string value)
        { var bytes = Utf8.GetBytes(value); writer.Write(bytes.Length); writer.Write(bytes); }
        static string Text(BinaryReader reader, int maximum) { return Utf8.GetString(Exact(reader,Count(reader,maximum))); }
    }
}
