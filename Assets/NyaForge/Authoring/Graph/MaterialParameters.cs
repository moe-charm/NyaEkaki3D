using System;
using System.IO;
using System.Text;

namespace NyaForge.Authoring.Graph
{
    public enum MaterialAlphaMode { Opaque, Cutout, Blend }

    /// <summary>Immutable standard-PBR values. RGB/emission are linear; roughness uses 0=smooth, 1=rough.</summary>
    public sealed class MaterialParameters
    {
        public Vec4 BaseColor { get; }
        public float Metallic { get; }
        public float Roughness { get; }
        public Vec3 Emission { get; }
        public MaterialAlphaMode AlphaMode { get; }
        public float AlphaCutoff { get; }
        public string ContentHash { get; }
        public static MaterialParameters Default { get; }=new MaterialParameters(new Vec4(1,1,1,1),0,.5f,new Vec3());
        public MaterialParameters(Vec4 baseColor,float metallic,float roughness,Vec3 emission,
            MaterialAlphaMode alphaMode=MaterialAlphaMode.Opaque,float alphaCutoff=.5f)
        {
            Unit(baseColor.X);Unit(baseColor.Y);Unit(baseColor.Z);Unit(baseColor.W);Unit(metallic);Unit(roughness);Unit(alphaCutoff);
            Checks.Finite(emission);
            Checks.Require(emission.X>=0 && emission.Y>=0 && emission.Z>=0 && emission.X<=64 && emission.Y<=64 && emission.Z<=64,
                "INVALID_MATERIAL","Emission must be between 0 and 64 in linear RGB.");
            Checks.Require(Enum.IsDefined(typeof(MaterialAlphaMode),alphaMode),"INVALID_MATERIAL","Unknown alpha mode.");
            BaseColor=baseColor;Metallic=metallic;Roughness=roughness;Emission=emission;AlphaMode=alphaMode;AlphaCutoff=alphaCutoff;
            using(var stream=new MemoryStream()) using(var writer=new BinaryWriter(stream,Encoding.UTF8))
            { Write(writer);ContentHash=Checks.Hash(stream.ToArray()); }
        }
        static void Unit(float value)
        { Checks.Require(!float.IsNaN(value) && !float.IsInfinity(value) && value>=0 && value<=1,"INVALID_MATERIAL","Material values must be finite and between 0 and 1."); }
        internal void Write(BinaryWriter writer)
        {
            writer.Write(Checks.Canonical(BaseColor.X));writer.Write(Checks.Canonical(BaseColor.Y));writer.Write(Checks.Canonical(BaseColor.Z));writer.Write(Checks.Canonical(BaseColor.W));
            writer.Write(Checks.Canonical(Metallic));writer.Write(Checks.Canonical(Roughness));MeshBinary.Write(writer,Emission);
            writer.Write((int)AlphaMode);writer.Write(Checks.Canonical(AlphaCutoff));
        }
        internal static MaterialParameters Read(BinaryReader reader)=>new MaterialParameters(
            new Vec4(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle()),reader.ReadSingle(),reader.ReadSingle(),
            MeshBinary.ReadVector(reader),(MaterialAlphaMode)reader.ReadInt32(),reader.ReadSingle());
    }
}
