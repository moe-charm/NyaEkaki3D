using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Evidence;
using UnityEngine;
namespace NyaForge.UnityRuntime
{
    internal static class EvidenceCaptureVerification
    {
        internal static void Verify(string output,List<string> checks)
        {
            var w=AuthoringWorkspace.CreateFixture();var snapshot=EvaluatedSnapshot.Acquire(w);
            var min=snapshot.Metrics.BoundsMin.Value;var max=snapshot.Metrics.BoundsMax.Value;var center=(min+max)*.5f;
            var view=new EvidenceView(512,512,center+new Vec3(0,0,1),center,new Vec3(0,1,0),.25f);
            var image=EvidenceModelCapture.Capture(snapshot,view);File.WriteAllBytes(Path.Combine(output,"evidence-model.png"),image.CopyPng());
            var texture=new Texture2D(2,2);GameObject foreign=null;
            try
            {
                if(!texture.LoadImage(image.CopyPng())) throw new InvalidOperationException("Evidence PNG could not decode.");
                if(texture.width!=512 || texture.height!=512 || texture.GetPixels32().Distinct().Count()<2) throw new InvalidOperationException("Evidence model image empty.");
                foreign=GameObject.CreatePrimitive(PrimitiveType.Cube);foreign.layer=31;foreign.transform.position=OwnedMeshProjection.ToUnity(center+new Vec3(0,0,.5f));foreign.transform.localScale=Vector3.one*10;
                var result=new AuthoringCommandService(w).Execute(w.NewCommand(AuthoringOperation.TranslateVertices(new[]{0,4},new Vec3(.03f,0,0))));
                if(!result.Success) throw new InvalidOperationException(result.Message);
                var repeat=EvidenceModelCapture.Capture(snapshot,view);
                if(repeat.PngHash!=image.PngHash) throw new InvalidOperationException("Live edit or foreign scene affected captured snapshot.");
                var changed=EvidenceModelCapture.Capture(EvaluatedSnapshot.Acquire(w),view);
                if(changed.PngHash==image.PngHash) throw new InvalidOperationException("Changed model image did not change.");
                var capture=new EvidenceCaptureSet(new[]{image,repeat});var saved=EvidenceCaptureStore.Save(Path.Combine(output,"evidence-package"),capture);var reopened=EvidenceCaptureReader.Read(saved);
                if(reopened.Metadata.SnapshotId!=snapshot.SnapshotId || reopened.Images.Count!=2 || !reopened.Images[0].CopyPng().SequenceEqual(image.CopyPng())) throw new InvalidOperationException("Evidence capture package roundtrip differs.");
                var copied=image.CopyPng();copied[0]=0;if(image.CopyPng()[0]!=137) throw new InvalidOperationException("Evidence PNG ownership leaked.");
                checks.Add("Evidence model capture: isolated scene, decoded PNG, immutable snapshot after live edit, fixed camera changed-model comparison");
            }
            finally { UnityEngine.Object.Destroy(texture);if(foreign) { foreign.SetActive(false);UnityEngine.Object.Destroy(foreign); } }
        }
    }
}



