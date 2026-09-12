using System.IO;
using System.Collections;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Paint;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        // Called with the two-layer masked fixture and 3D mode active. Direct persistence APIs model
        // save/export invoked while pointer capture prevents clicking another UI control.
        IEnumerator VerifySurfaceLifetimes(string directory)
        {
            string state=workspace.Document.StateHash,layerChoice=paintLayerChoice.value,targetChoice=paintTarget.value,brushChoice=maskBrush.value;
            var committed=workspace.Preview.Output.BaseColor.Image;var committedBytes=committed.CopyRgba();
            Vector2 Point()=>VertexPanelPoint(new Vector3(0,0,0));
            void Start()
            {
                PointerProbe.Down(view,Point());
                Check(surfaceStroke!=null && projection.HasPaintPreview,"Lifetime probe did not start a preview");
            }
            void Cancelled(string reason)
            {
                Check(surfaceStroke==null && !projection.HasPaintPreview && workspace.Document.StateHash==state,"Pending paint survived "+reason);
                PointerProbe.Up(view,Point());Check(workspace.Document.StateHash==state,"Late pointer up committed after "+reason);
            }
            try
            {
                yield return WaitSurfaceReady();
                Start();
                string pendingSave=Path.Combine(directory,"surface-pending-save");ProjectStore.Save(pendingSave,workspace,0);
                var reopened=ProjectStore.Open(pendingSave);
                Check(reopened.Document.StateHash==state && reopened.Preview.Output.BaseColor.Image.CopyRgba().SequenceEqual(committedBytes),"Native save included pending paint");
                string png=PaintPngExport.Write(Path.Combine(directory,"surface-pending-png"),workspace,selectedPaint);
                Check(File.ReadAllBytes(png).SequenceEqual(PaintPng.Encode(committed)),"PNG export included pending paint");
                var bake=SurfaceBakeStore.Read(SurfaceBakeStore.Export(Path.Combine(directory,"surface-pending-export"),workspace));
                Check(bake.BaseColor.CopyRgba().SequenceEqual(committedBytes),"Surface export included pending paint");
                Check(surfaceStroke!=null && projection.HasPaintPreview && workspace.Document.StateHash==state,"Persistence mutated the gesture or document");
                CancelSurfaceStroke();Cancelled("explicit cancellation");

                Start();paintTarget.value=paintTarget.choices[0];Cancelled("image/mask target change");paintTarget.value=targetChoice;
                Start();maskBrush.value=maskBrush.choices[maskBrush.index==0 ? 1 : 0];Cancelled("mask brush mode change");maskBrush.value=brushChoice;
                Start();paintLayerChoice.value=paintLayerChoice.choices[0];Cancelled("layer selection");paintLayerChoice.value=layerChoice;paintTarget.value=targetChoice;
                Start();Frame();Cancelled("camera framing");

                var orientation=orbit;var p=Point();var moved=p+new Vector2(14,9);
                PointerProbe.Down(view,p,0,EventModifiers.Alt);PointerProbe.Move(view,moved,0,EventModifiers.Alt);PointerProbe.Up(view,moved,0,EventModifiers.Alt);
                Check(surfaceStroke==null && orbit!=orientation && workspace.Document.StateHash==state,"Alt camera rotation painted or failed");Frame();
                yield return WaitSurfaceReady();
                var oldTarget=target;p=Point();moved=p+new Vector2(14,9);
                Start();PointerProbe.Down(view,p,1);PointerProbe.Move(view,moved,1);PointerProbe.Up(view,moved,1);
                Check(surfaceStroke==null && !projection.HasPaintPreview && target!=oldTarget && workspace.Document.StateHash==state,"Right camera movement retained/committed pending paint");Frame();
            }
            finally
            {
                CancelSurfaceStroke();paintLayerChoice.SetValueWithoutNotify(layerChoice);paintTarget.SetValueWithoutNotify(targetChoice);maskBrush.SetValueWithoutNotify(brushChoice);RefreshPaint();
            }
        }
    }
}
