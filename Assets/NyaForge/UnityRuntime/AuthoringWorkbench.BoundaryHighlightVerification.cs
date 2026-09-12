using UnityEngine;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void VerifyBoundaryHighlight()
        {
            string original=workspace.Document.StateHash;
            for(int choice=0;choice<boundaryLoops.Length;choice++)
            {
                boundaryChoice.index=choice;
                var value=DisplayedGraphValue();var loop=boundaryLoops[choice];
                Check(boundaryHighlight!=null && boundaryHighlight.VertexCount==loop.Length,"Boundary highlight vertex count differs");
                for(int i=0;i<loop.Length;i++)
                {
                    var p=value.Transform.ToAvatarPoint(value.Polygon.Vertices[loop[i]].Position);
                    Check(Vector3.Distance(boundaryHighlight.Position(i),new Vector3(p.X,p.Y,p.Z))<.000001f,"Boundary highlight does not follow selected loop");
                }
            }
            boundaryPanel.value=false;Check(boundaryHighlight.VertexCount==0,"Collapsed boundary retained highlight");
            boundaryPanel.value=true;Check(boundaryHighlight.VertexCount>0,"Reopened boundary did not restore highlight");
            SelectEditStage(0);Check(boundaryHighlight.VertexCount==0,"Final output retained editable boundary highlight");
            SelectEditStage(1);Check(boundaryHighlight.VertexCount>0,"Edit stage did not restore boundary highlight");
            Check(workspace.Document.StateHash==original,"Boundary display changed the document");
            boundaryChoice.index=1;
        }
    }
}
