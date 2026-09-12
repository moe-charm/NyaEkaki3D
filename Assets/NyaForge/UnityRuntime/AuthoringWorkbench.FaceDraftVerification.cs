using System;
using System.Collections;
using System.Linq;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyFaceDraftControls(Action<string> completed)
        {
            string failure=null,before=workspace.Document.StateHash;
            facePerimeter.value="";
            foreach(ulong id in new ulong[]{4,1,5})
            {
                Select(DisplayedGraphValue().PolygonRendering.RenderVertexMap.Select((binding,index)=>(binding,index)).Where(p=>p.binding.VertexId==id).Select(p=>p.index));
                yield return null;yield return null;controls.ScrollTo(faceAppendButton);yield return null;yield return null;
                try { PointerProbe.Click(faceAppendButton); }
                catch(Exception e) { failure=e.ToString();break; }
            }
            if(failure==null) try
            {
                Check(ReadFacePerimeter().SequenceEqual(new ulong[]{4,1,5}),"Append did not preserve registration order");
                PointerProbe.Click(faceAppendButton);
                Check(ReadFacePerimeter().SequenceEqual(new ulong[]{4,1,5}),"Duplicate registration changed perimeter");
            }
            catch(Exception e) { failure=e.ToString(); }
            foreach(string name in new[]{"face-reverse","face-reverse","face-pop","face-clear"})
            {
                if(failure!=null) break;
                var button=root.Q<Button>(name);controls.ScrollTo(button);yield return null;yield return null;
                try
                {
                    var old=ReadFacePerimeter();PointerProbe.Click(button);
                    var expected=name=="face-reverse" ? old.Reverse() : name=="face-pop" ? old.Take(old.Length-1) : Enumerable.Empty<ulong>();
                    Check(ReadFacePerimeter().SequenceEqual(expected),"Draft button failed: "+name);
                }
                catch(Exception e) { failure=e.ToString(); }
            }
            if(failure==null) try
            {
                Check(workspace.Document.StateHash==before,"Draft controls changed document");
                facePerimeter.value="4, 1, 5";faceCreatePanel.value=false;
                Check(faceCreateOutline.VertexCount==0,"Collapsed draft outline still visible");
                faceCreatePanel.value=true;Check(faceCreateOutline.VertexCount==3,"Reopened draft outline missing");
                SelectEditStage(0);Check(facePerimeter.value=="" && faceCreateOutline.VertexCount==0,"Leaving edit stage retained draft");
                SelectEditStage(1);facePerimeter.value="4, 1, 5";
            }
            catch(Exception e) { failure=e.ToString(); }
            completed(failure);
        }
    }
}
