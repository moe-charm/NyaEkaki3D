using System;
using System.IO;
using System.Text;

namespace NyaForge.Authoring.Paint
{
    /// <summary>Portable deterministic RGBA8 PNG encoder. sRGB, straight alpha, top-first PNG rows.
    /// Stored DEFLATE blocks avoid platform compression differences; native tiles remain the editing source.</summary>
    public static class PaintPng
    {
        public static byte[] Encode(PaintImage image)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            var raw = new byte[image.Height * (1 + image.Width * 4)];
            int at = 0;
            for (int y = image.Height - 1; y >= 0; y--)
            {
                raw[at++] = 0; // PNG filter None
                for (int x = 0; x < image.Width; x++)
                {
                    var p = image.GetPixel(x,y);
                    raw[at++] = p.R; raw[at++] = p.G; raw[at++] = p.B; raw[at++] = p.A;
                }
            }
            using (var png = new MemoryStream())
            {
                png.Write(new byte[]{137,80,78,71,13,10,26,10},0,8);
                using (var header = new MemoryStream())
                {
                    UInt32(header,(uint)image.Width); UInt32(header,(uint)image.Height);
                    header.Write(new byte[]{8,6,0,0,0},0,5);
                    Chunk(png,"IHDR",header.ToArray());
                }
                Chunk(png,"sRGB",new byte[]{0}); // perceptual rendering intent
                Chunk(png,"IDAT",ZlibStored(raw));
                Chunk(png,"IEND",Array.Empty<byte>());
                return png.ToArray();
            }
        }

        static byte[] ZlibStored(byte[] raw)
        {
            using (var stream = new MemoryStream())
            {
                stream.WriteByte(0x78); stream.WriteByte(0x01);
                int offset = 0;
                while (offset < raw.Length)
                {
                    int length = Math.Min(65535,raw.Length-offset);
                    stream.WriteByte((byte)(offset+length == raw.Length ? 1 : 0));
                    stream.WriteByte((byte)length); stream.WriteByte((byte)(length>>8));
                    int inverse = length ^ 65535;
                    stream.WriteByte((byte)inverse); stream.WriteByte((byte)(inverse>>8));
                    stream.Write(raw,offset,length); offset += length;
                }
                uint a = 1, b = 0;
                foreach (byte value in raw) { a = (a+value)%65521; b = (b+a)%65521; }
                UInt32(stream,(b<<16)|a);
                return stream.ToArray();
            }
        }
        static void Chunk(Stream stream,string type,byte[] data)
        {
            var name = Encoding.ASCII.GetBytes(type);
            UInt32(stream,(uint)data.Length); stream.Write(name,0,name.Length); stream.Write(data,0,data.Length);
            uint crc = 0xffffffff;
            foreach (byte value in name) crc = CrcByte(crc,value);
            foreach (byte value in data) crc = CrcByte(crc,value);
            UInt32(stream,crc ^ 0xffffffff);
        }
        static uint CrcByte(uint crc,byte value)
        {
            crc ^= value;
            for (int i = 0; i < 8; i++) crc = (crc>>1) ^ ((crc&1) == 0 ? 0 : 0xedb88320u);
            return crc;
        }
        static void UInt32(Stream stream,uint value)
        {
            stream.WriteByte((byte)(value>>24)); stream.WriteByte((byte)(value>>16));
            stream.WriteByte((byte)(value>>8)); stream.WriteByte((byte)value);
        }
    }
}
