using System;
using System.Collections.Generic;
namespace NyaForge.Authoring.Geometry
{
    public sealed class MeshRayHit
    {
        public int TriangleIndex { get; }
        public int SubmeshIndex { get; }
        public int A { get; }
        public int B { get; }
        public int C { get; }
        public double U { get; }
        public double V { get; }
        public double Distance { get; }
        public bool IsFrontFace { get; }
        internal MeshRayHit(int triangle,int submesh,int a,int b,int c,double u,double v,double distance,bool front)
        { TriangleIndex=triangle;SubmeshIndex=submesh;A=a;B=b;C=c;U=u;V=v;Distance=distance;IsFrontFace=front; }
    }
    /// <summary>Immutable mesh/transform snapshot with a median BVH. Returns triangle indices and barycentrics without requiring UV attributes.</summary>
    public sealed class MeshRaycast
    {
        readonly MeshData mesh;
        readonly RestTransform transform;
        readonly Triangle[] triangles;
        readonly int[] order;
        readonly List<Node> nodes=new List<Node>();

        public string MeshHash=>mesh.ContentHash;
        public RestTransform Transform=>transform;
        struct Triangle
        {
            internal int A,B,C,Submesh;
            internal RayVector Origin,E1,E2,Center;
            internal RayBounds Bounds;
        }
        struct Node { internal RayBounds Bounds;internal int Start,Count,Left,Right; }
        public MeshRaycast(MeshData source,RestTransform placement)
        {
            Checks.Require(source!=null,"MESH_MISSING","A raycast mesh is required.");
            placement.Validate();mesh=source;transform=placement;
            foreach(var position in source.Positions) Checks.Finite(placement.ToAvatarPoint(position));

            triangles=new Triangle[source.TriangleCount];order=new int[triangles.Length];int triangle=0,submesh=0;
            foreach(var indices in source.Submeshes)
            {
                for(int i=0;i<indices.Length;i+=3)
                {
                    int a=indices[i],b=indices[i+1],c=indices[i+2];var p=new RayVector(source.Positions[a]);var q=new RayVector(source.Positions[b]);var r=new RayVector(source.Positions[c]);
                    triangles[triangle]=new Triangle { A=a,B=b,C=c,Submesh=submesh,Origin=p,E1=q-p,E2=r-p,Center=(p+q+r)*(1.0/3),
                        Bounds=new RayBounds(RayVector.Min(p,RayVector.Min(q,r)),RayVector.Max(p,RayVector.Max(q,r))) };
                    order[triangle]=triangle;triangle++;
                }
                submesh++;
            }
            Build(0,triangles.Length);
        }
        int Build(int start,int count)
        {
            var bounds=triangles[order[start]].Bounds;
            for(int i=start+1;i<start+count;i++) bounds=bounds.Merge(triangles[order[i]].Bounds);
            int index=nodes.Count;nodes.Add(new Node { Bounds=bounds,Start=start,Count=count,Left=-1,Right=-1 });
            if(count<=8) return index;
            var extent=bounds.Max-bounds.Min;int axis=extent.X>=extent.Y && extent.X>=extent.Z ? 0 : extent.Y>=extent.Z ? 1 : 2;
            Array.Sort(order,start,count,Comparer<int>.Create((a,b)=>
            {
                int result=triangles[a].Center.Axis(axis).CompareTo(triangles[b].Center.Axis(axis));return result!=0 ? result : a.CompareTo(b);
            }));
            int half=count/2,left=Build(start,half),right=Build(start+half,count-half);
            nodes[index]=new Node { Bounds=bounds,Start=start,Count=0,Left=left,Right=right };return index;
        }
        public MeshRayHit Raycast(Vec3 origin,Vec3 direction,bool cullBackFaces=false,double maxDistance=double.PositiveInfinity)
        {
            Checks.Finite(origin);Checks.Finite(direction);
            Checks.Require(!double.IsNaN(maxDistance) && maxDistance>=0,"INVALID_PAINT_RAY","Ray distance must be nonnegative.");
            var d=new RayVector(direction);double length=Math.Sqrt(RayVector.Dot(d,d));
            Checks.Require(length>0,"INVALID_PAINT_RAY","Ray direction must be nonzero.");
            d=d*(1/(length*transform.Scale));var o=(new RayVector(origin)-new RayVector(transform.Translation))*(1.0/transform.Scale);
            MeshRayHit best=null;double nearest=maxDistance;
            void Visit(int nodeIndex)
            {
                var node=nodes[nodeIndex];if(!node.Bounds.Intersects(o,d,nearest,out _)) return;
                if(node.Count==0)
                {
                    bool left=nodes[node.Left].Bounds.Intersects(o,d,nearest,out double a),right=nodes[node.Right].Bounds.Intersects(o,d,nearest,out double b);
                    if(left && right) { if(a<=b) { Visit(node.Left);Visit(node.Right); } else { Visit(node.Right);Visit(node.Left); } }
                    else if(left) Visit(node.Left);else if(right) Visit(node.Right);return;
                }
                for(int i=node.Start;i<node.Start+node.Count;i++)
                {
                    int id=order[i];var t=triangles[id];var p=RayVector.Cross(d,t.E2);double det=RayVector.Dot(t.E1,p);
                    double tolerance=Math.Sqrt(RayVector.Dot(t.E1,t.E1)*RayVector.Dot(p,p))*1e-12;
                    if(Math.Abs(det)<=tolerance || cullBackFaces && det<0) continue;
                    var delta=o-t.Origin;double u=RayVector.Dot(delta,p)/det;
                    if(u<0 || u>1) continue;var q=RayVector.Cross(delta,t.E1);double v=RayVector.Dot(d,q)/det;
                    if(v<0 || u+v>1) continue;double distance=RayVector.Dot(t.E2,q)/det;
                    if(distance<0 || distance>nearest || distance==nearest && best!=null && id>=best.TriangleIndex) continue;
                    nearest=distance;best=new MeshRayHit(id,t.Submesh,t.A,t.B,t.C,u,v,distance,det>0);
                }
            }
            Visit(0);return best;
        }
    }
}


