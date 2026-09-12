using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Evidence;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;
using NyaForge.Authoring.Topology;
using UnityEngine;
namespace NyaForge.UnityRuntime
{
    internal static class EvidenceTexturedVerification
    {
        internal static void Verify(string directory,List<string> checks)
        {
            string Id()=>Guid.NewGuid().ToString("D");
            string source=Id(),paint=Id(),material=Id(),assign=Id(),output=Id();
            var polygon=PolygonSolidify.Apply(PolygonPrimitives.Plane(Id()),.06f);
            var graph=new AuthoringGraph(Id(),new[]{GraphNode.Polygon(source,polygon,new RestTransform(1,new Vec3())),GraphNode.Paint(paint,8,8),GraphNode.StandardMaterial(material,new MaterialParameters(new Vec4(1,1,1,1),0,.8f,new Vec3())),GraphNode.AssignMaterial(assign),GraphNode.Output(output)},
                new[]{new GraphEdge(source,"mesh",paint,"mesh"),new GraphEdge(paint,"image",material,"baseColor"),new GraphEdge(source,"mesh",assign,"mesh"),new GraphEdge(material,"material",assign,"material"),new GraphEdge(assign,"mesh",output,"mesh")},output);
            var bytes=new byte[8*8*4];for(int y=0;y<8;y++) for(int x=0;x<8;x++) { int i=(y*8+x)*4;bytes[i]=(byte)(x<4 ? 240 : 20);bytes[i+1]=30;bytes[i+2]=(byte)(x<4 ? 20 : 240);bytes[i+3]=255; }
            var context=PaintEditing.Context(graph,paint);graph=graph.ReplaceNode(GraphNode.Paint(paint,8,8,PaintImage.FromRgbaBottomLeft(8,8,bytes),context.UvHash,context.MeshDomain));
            var w=AuthoringWorkspace.CreateEmpty();var command=new AuthoringCommandService(w).Execute(w.NewCommand(AuthoringOperation.AddGraph(graph)));
            if(!command.Success) throw new InvalidOperationException(command.Message);
            var snapshot=EvaluatedSnapshot.Acquire(w);var views=EvidenceViewPresets.FiveViews(snapshot,256);var images=new List<EvidenceImage>();
            var names=new[]{"front","back","left","right","oblique"};
            for(int i=0;i<views.Count;i++)
            {
                var image=EvidenceModelCapture.Capture(snapshot,views[i]);images.Add(image);File.WriteAllBytes(Path.Combine(directory,"evidence-texture-"+names[i]+".png"),image.CopyPng());
                var texture=new Texture2D(2,2);
                try
                {
                    if(!texture.LoadImage(image.CopyPng())) throw new InvalidOperationException("Textured evidence decode failed.");
                    var pixels=texture.GetPixels32();int red=pixels.Count(p=>p.r>p.b*1.8f && p.r>50),blue=pixels.Count(p=>p.b>p.r*1.8f && p.b>50);
                    if(red+blue<100 || i<2 && (red<100 || blue<100)) throw new InvalidOperationException("Textured evidence lost visible geometry or front/back texture colors: "+names[i]);
                }
                finally { UnityEngine.Object.Destroy(texture); }
            }
            var set=new EvidenceCaptureSet(images);var path=EvidenceCaptureStore.Save(Path.Combine(directory,"evidence-textured-package"),set);var read=EvidenceCaptureReader.Read(path);
            if(read.Images.Count!=5 || snapshot.Metrics.AssignedMaterialCount!=1 || snapshot.Metrics.ImageHashes.Count!=1 || snapshot.Metrics.PolygonFaceCount!=6) throw new InvalidOperationException("Textured evidence metrics differ.");
            for(int i=0;i<5;i++) if(read.Images[i].PngHash!=images[i].PngHash) throw new InvalidOperationException("Textured evidence image identity differs.");
            checks.Add("Evidence textured solid: five visible views, red/blue texture front/back, material/image metrics and capture package roundtrip");
        }
    }
}
