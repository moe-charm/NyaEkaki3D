using System;
using System.Linq;
using NyaForge.Authoring.Paint;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        SurfacePreparationQueue surfacePreparation=new SurfacePreparationQueue();
        SurfacePreparationInput surfacePreparationInput;
        object surfacePreparationMap;
        string surfacePreparationUv;
        long surfacePreparationGeneration;
        Label surfacePreparationLabel;
        Button surfacePreparationRetry;
        IVisualElementScheduledItem surfacePreparationWatch;

        void BuildSurfacePreparation(VisualElement parent)
        {
            surfacePreparationLabel=new Label { name="surface-preparation-status" };
            surfacePreparationLabel.style.whiteSpace=WhiteSpace.Normal;parent.Add(surfacePreparationLabel);
            surfacePreparationRetry=Button("準備を再試行",()=> { ClearSurfacePreparation();RefreshSurfacePreparation(); },"surface-preparation-retry");
            parent.Add(surfacePreparationRetry);
            surfacePreparationWatch=view.schedule.Execute(RefreshSurfacePreparation).Every(50);
        }
        void ClearSurfacePreparation()
        {
            CancelSurfaceStroke();surfacePreparation.Clear();surfacePreparationInput=null;
            surfacePreparationMap=null;surfacePreparationUv=null;
        }
        void RefreshSurfacePreparation()
        {
            if(surfacePreparationLabel==null) return;
            if(!active || !surfacePaintMode.value || view.panel==null)
            {
                if(surfacePreparationInput!=null) ClearSurfacePreparation();
                surfacePreparationLabel.text="";surfacePreparationRetry.style.display=DisplayStyle.None;return;
            }
            try
            {
                var output=workspace.Preview.Output;var bounds=view.worldBound;
                NyaForge.Authoring.Graph.GraphImageValue image=null;
                workspace.Preview.Evaluation?.ImageOutputs.TryGetValue(selectedPaint,out image);
                if(output==null || image==null || bounds.width<=0 || bounds.height<=0) { ClearSurfacePreparation();return; }
                var captured=SurfaceCameraCapture.Capture(camera,bounds);
                var map=output.PolygonRendering?.RenderVertexMap;
                bool sameSurface=surfacePreparationInput!=null && surfacePreparationInput.Mesh.ContentHash==output.Mesh.ContentHash &&
                    surfacePreparationInput.Transform.Equals(output.Transform) && surfacePreparationUv==image.UvHash && ReferenceEquals(surfacePreparationMap,map);
                if(!sameSurface || !surfacePreparationInput.Camera.Equals(captured))
                {
                    CancelSurfaceStroke();
                    surfacePreparationInput=sameSurface ? surfacePreparationInput.WithCamera(captured) :
                        new SurfacePreparationInput(output.Mesh,output.Transform,captured,map?.Select(v=>v.VertexId));
                    surfacePreparationMap=map;surfacePreparationUv=image.UvHash;
                    surfacePreparationGeneration=surfacePreparation.Request(surfacePreparationInput);
                }
                var state=surfacePreparation.Read();
                surfacePreparationLabel.text=state.Phase==SurfacePreparationPhase.Ready ? "3D描画の準備完了" :
                    state.Phase==SurfacePreparationPhase.Failed ? "3D描画の準備に失敗: "+state.Error : "3D描画を準備中… 完了後に描き始めてください。";
                surfacePreparationRetry.style.display=state.Phase==SurfacePreparationPhase.Failed ? DisplayStyle.Flex : DisplayStyle.None;
            }
            catch(Exception error)
            {
                ClearSurfacePreparation();surfacePreparationLabel.text="3D描画の準備に失敗: "+error.Message;
                surfacePreparationRetry.style.display=DisplayStyle.Flex;
            }
        }
        PreparedSurface ReadySurface()
        {
            RefreshSurfacePreparation();var state=surfacePreparation.Read();
            return surfacePreparationInput!=null && state.Generation==surfacePreparationGeneration && state.Phase==SurfacePreparationPhase.Ready ? state.Surface : null;
        }
    }
}
