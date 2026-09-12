using System;
namespace NyaForge.Authoring.Evidence
{
    public sealed class EvidenceView
    {
        public int Width { get; }
        public int Height { get; }
        public Vec3 Position { get; }
        public Vec3 Target { get; }
        public Vec3 Up { get; }
        public float OrthographicSize { get; }
        public float Near { get; }
        public float Far { get; }
        public EvidenceView(int width,int height,Vec3 position,Vec3 target,Vec3 up,float size,float near=.001f,float far=100)
        {
            Checks.Require(width>=32 && width<=4096 && height>=32 && height<=4096,"CAPTURE_BUDGET","Capture dimensions must be 32..4096.");
            Checks.Finite(position);Checks.Finite(target);Checks.Finite(up);Checks.Finite(size);Checks.Finite(near);Checks.Finite(far);
            double x=(double)target.X-position.X,y=(double)target.Y-position.Y,z=(double)target.Z-position.Z;
            double cx=y*up.Z-z*up.Y,cy=z*up.X-x*up.Z,cz=x*up.Y-y*up.X;
            Checks.Require(cx*cx+cy*cy+cz*cz>1e-20 && size>0 && near>0 && far>near,"INVALID_CAPTURE_VIEW","Camera axes and clipping range must be valid.");
            Width=width;Height=height;Position=position;Target=target;Up=up;OrthographicSize=size;Near=near;Far=far;
        }
    }
    public sealed class EvidenceImage
    {
        readonly byte[] png;
        public EvaluatedSnapshot Snapshot { get; }
        public EvidenceView View { get; }
        public string PngHash { get; }
        public string RenderProfile=>"authoring-surface-orthographic-v1";
        public EvidenceImage(EvaluatedSnapshot snapshot,EvidenceView view,byte[] bytes)
        {
            Checks.Require(snapshot!=null && view!=null && bytes!=null && bytes.Length>8,"INVALID_CAPTURE_IMAGE","Snapshot, view and PNG required.");
            var input=Paint.PaintPngInput.Read(bytes);Checks.Require(input.Width==view.Width && input.Height==view.Height,"INVALID_CAPTURE_IMAGE","PNG dimensions differ from camera profile.");
            Snapshot=snapshot;View=view;png=(byte[])bytes.Clone();PngHash=Checks.Hash(png);
        }
        public byte[] CopyPng()=>(byte[])png.Clone();
    }
}

