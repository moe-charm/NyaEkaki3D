using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NyaForge.Authoring.Paint;

namespace NyaForge.Authoring
{
    internal static class PaintLayersCodec
    {
        const int Magic=0x4c46594e; // NYFL
        const int MaxBytes=16384;
        static readonly UTF8Encoding Utf8=new UTF8Encoding(false,true);
        sealed class Entry
        {
            internal string Id,Name,Image,Mask;
            internal float Opacity;
            internal bool Visible;
        }
        internal static byte[] Write(PaintLayers stack,Func<byte[],string> addBlob)
        {
            using(var stream=new MemoryStream()) using(var writer=new BinaryWriter(stream,Utf8))
            {
                writer.Write(Magic);writer.Write(1);writer.Write(stack.Width);writer.Write(stack.Height);writer.Write(stack.Layers.Count);
                foreach(var layer in stack.Layers)
                {
                    Text(writer,layer.Id);Text(writer,layer.Name);writer.Write(Checks.Canonical(layer.Opacity));writer.Write(layer.Visible);
                    Text(writer,addBlob(PaintImageCodec.Write(layer.Image)));
                    Text(writer,layer.Mask==null ? "" : addBlob(PaintMaskCodec.Write(layer.Mask)));
                }
                Checks.Require(stream.Length<=MaxBytes,"PAINT_LAYER_BUDGET","Layer metadata is too large.");
                return stream.ToArray();
            }
        }
        internal static PaintLayers Read(byte[] bytes,Func<string,byte[]> readBlob)
        {
            Checks.Require(bytes!=null && bytes.Length>=20 && bytes.Length<=MaxBytes,"INVALID_LAYER_BLOB","Invalid layer metadata size.");
            try
            {
                using(var stream=new MemoryStream(bytes,false)) using(var reader=new BinaryReader(stream,Utf8))
                {
                    Checks.Require(reader.ReadInt32()==Magic,"INVALID_LAYER_BLOB","Not a layer stack.");
                    Checks.Require(reader.ReadInt32()==1,"UNSUPPORTED_FORMAT","Unsupported layer stack version.");
                    int width=reader.ReadInt32(),height=reader.ReadInt32(),count=reader.ReadInt32();
                    PaintLayers.ValidateBudget(width,height,count,0);
                    var entries=new List<Entry>();var ids=new HashSet<string>(StringComparer.Ordinal);int masks=0;
                    for(int i=0;i<count;i++)
                    {
                        var entry=new Entry { Id=Text(reader,36),Name=Text(reader,512),Opacity=reader.ReadSingle() };
                        Checks.Id(entry.Id);Checks.Name(entry.Name);Checks.Finite(entry.Opacity);
                        Checks.Require(ids.Add(entry.Id),"INVALID_PAINT_LAYERS","Duplicate layer ID.");
                        Checks.Require(entry.Opacity>=0 && entry.Opacity<=1,"INVALID_LAYER_OPACITY","Invalid layer opacity.");
                        byte visible=reader.ReadByte();Checks.Require(visible<=1,"INVALID_LAYER_BLOB","Invalid visibility flag.");entry.Visible=visible==1;
                        entry.Image=Text(reader,64);entry.Mask=Text(reader,64);Checks.HashText(entry.Image);
                        if(entry.Mask!="") { Checks.HashText(entry.Mask);masks++; }
                        entries.Add(entry);
                    }
                    Checks.Require(stream.Position==stream.Length,"INVALID_LAYER_BLOB","Trailing layer metadata.");
                    PaintLayers.ValidateBudget(width,height,count,masks); // all metadata validated before loading dependencies
                    var layers=new List<PaintLayer>();
                    foreach(var entry in entries)
                        layers.Add(new PaintLayer(entry.Id,entry.Name,PaintImageCodec.Read(readBlob(entry.Image),width,height),entry.Opacity,entry.Visible,
                            entry.Mask=="" ? null : PaintMaskCodec.Read(readBlob(entry.Mask),width,height)));
                    return new PaintLayers(width,height,layers);
                }
            }
            catch(EndOfStreamException) { throw new AuthoringException("INVALID_LAYER_BLOB","Truncated layer metadata."); }
            catch(DecoderFallbackException) { throw new AuthoringException("INVALID_LAYER_BLOB","Invalid UTF8 layer name."); }
        }
        static void Text(BinaryWriter writer,string text)
        { var bytes=Utf8.GetBytes(text);writer.Write(bytes.Length);writer.Write(bytes); }
        static string Text(BinaryReader reader,int max)
        {
            int length=reader.ReadInt32();
            Checks.Require(length>=0 && length<=max && length<=reader.BaseStream.Length-reader.BaseStream.Position,"INVALID_LAYER_BLOB","Invalid string length.");
            return Utf8.GetString(reader.ReadBytes(length));
        }
    }
    public static class PaintLayersStore
    {
        public static string Write(string directory,PaintLayers stack)
        {
            if(stack==null) throw new ArgumentNullException(nameof(stack));
            directory=Storage.DirectoryPath(directory);
            using(Storage.Lock(directory))
            {
                byte[] metadata=PaintLayersCodec.Write(stack,bytes=> { string id=Checks.Hash(bytes);Storage.WriteBlob(directory,id,bytes);return id; });
                string hash=Checks.Hash(metadata);Storage.WriteBlob(directory,hash,metadata);return hash;
            }
        }
        public static PaintLayers Read(string directory,string hash)
        {
            directory=Storage.DirectoryPath(directory);Checks.HashText(hash);
            return PaintLayersCodec.Read(Storage.ReadBlob(directory,hash),id=>Storage.ReadBlob(directory,id));
        }
    }
}
