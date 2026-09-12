using System;
using System.Collections.Generic;
namespace NyaForge.Authoring.Evidence
{
    public static class EvidenceViewPresets
    {
        /// <summary>Front (+Z), back, left (-X), right, oblique. All share one center and scale.</summary>
        public static IReadOnlyList<EvidenceView> FiveViews(EvaluatedSnapshot snapshot,int resolution=512)
        {
            Checks.Require(snapshot!=null && snapshot.State==EvidenceState.Ready && snapshot.Metrics.BoundsMin.HasValue,"CAPTURE_NOT_READY","A ready mesh is required.");
            var min=snapshot.Metrics.BoundsMin.Value;var max=snapshot.Metrics.BoundsMax.Value;
            var center=new Vec3((float)(((double)min.X+max.X)/2),(float)(((double)min.Y+max.Y)/2),(float)(((double)min.Z+max.Z)/2));
            double x=(double)max.X-min.X,y=(double)max.Y-min.Y,z=(double)max.Z-min.Z;
            float radius=(float)Math.Max(.001,Math.Sqrt(x*x+y*y+z*z)/2),distance=radius*3,size=radius*1.15f;
            var directions=new[]{new Vec3(0,0,1),new Vec3(0,0,-1),new Vec3(-1,0,0),new Vec3(1,0,0),new Vec3(2f/3,1f/3,2f/3)};
            var views=new EvidenceView[directions.Length];
            for(int i=0;i<views.Length;i++) views[i]=new EvidenceView(resolution,resolution,center+directions[i]*distance,center,new Vec3(0,1,0),size,radius*.1f,radius*6);
            return Array.AsReadOnly(views);
        }
    }
}
