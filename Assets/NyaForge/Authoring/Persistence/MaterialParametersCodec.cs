using System.IO;
using System.Linq;
using System.Text;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring
{
    // Fixed v1 standard-PBR payload: ten floats plus one alpha-mode int. Version belongs to the profile.
    internal static class MaterialParametersCodec
    {
        internal static byte[] Write(MaterialParameters parameters)
        {
            using(var stream=new MemoryStream()) using(var writer=new BinaryWriter(stream,Encoding.UTF8))
            { parameters.Write(writer);return stream.ToArray(); }
        }
        internal static MaterialParameters Read(byte[] bytes)
        {
            Checks.Require(bytes!=null && bytes.Length==44,"INVALID_BLOB","Invalid standard material payload length.");
            using(var stream=new MemoryStream(bytes,false)) using(var reader=new BinaryReader(stream,Encoding.UTF8))
            {
                var parameters=MaterialParameters.Read(reader);
                Checks.Require(Write(parameters).SequenceEqual(bytes),"INVALID_BLOB","Noncanonical standard material payload.");
                return parameters;
            }
        }
    }
}
