using System;
using System.Collections.Generic;

namespace NyaForge.Authoring.Paint
{
    /// <summary>Clips world triangles by camera depth before perspective division.</summary>
    public static class SurfaceScreenProjection
    {
        public static SurfaceScreenCoverage Build(MeshData mesh,RestTransform transform,
            Func<Vec3,float> depth,Func<Vec3,Vec2> project,float near,float far)
        {
            Checks.Require(mesh!=null && depth!=null && project!=null,"INVALID_SCREEN_MESH","Mesh and camera functions required.");
            Checks.Finite(near);Checks.Finite(far);Checks.Require(near>0 && far>near,"INVALID_SCREEN_MESH","Camera depth range must be positive.");transform.Validate();
            var world=new Vec3[mesh.VertexCount];
            for(int i=0;i<world.Length;i++) { world[i]=transform.ToAvatarPoint(mesh.Positions[i]);Checks.Finite(world[i]); }
            IEnumerable<Vec2> Triangles()
            {
                foreach(var indices in mesh.Submeshes) for(int i=0;i<indices.Length;i+=3)
                {
                    var polygon=new List<Vec3> { world[indices[i]],world[indices[i+1]],world[indices[i+2]] };
                    polygon=Clip(polygon,near,true,depth);polygon=Clip(polygon,far,false,depth);
                    if(polygon.Count<3) continue;
                    var a=project(polygon[0]);
                    for(int j=1;j+1<polygon.Count;j++) { yield return a;yield return project(polygon[j]);yield return project(polygon[j+1]); }
                }
            }
            return new SurfaceScreenCoverage(Triangles());
        }
        static List<Vec3> Clip(List<Vec3> source,float plane,bool greater,Func<Vec3,float> depth)
        {
            var result=new List<Vec3>();if(source.Count==0) return result;
            var a=source[source.Count-1];float da=depth(a);Checks.Finite(da);bool insideA=greater ? da>=plane : da<=plane;
            foreach(var b in source)
            {
                float db=depth(b);Checks.Finite(db);bool insideB=greater ? db>=plane : db<=plane;
                if(insideA!=insideB)
                {
                    double t=((double)plane-da)/((double)db-da);
                    result.Add(new Vec3((float)(a.X+((double)b.X-a.X)*t),(float)(a.Y+((double)b.Y-a.Y)*t),(float)(a.Z+((double)b.Z-a.Z)*t)));
                }
                if(insideB) result.Add(b);a=b;da=db;insideA=insideB;
            }
            return result;
        }
    }
}
