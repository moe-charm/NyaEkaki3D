using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    public static class PolygonFaceSplit
    {
        public static PolygonMesh Split(PolygonMesh mesh,IEnumerable<ulong> vertexIds)
        {
            Checks.Require(mesh!=null && vertexIds!=null,"INVALID_SELECTION","Select two vertices of one face.");
            var ids=vertexIds.Take(3).ToArray();
            Checks.Require(ids.Length==2 && ids[0]!=ids[1],"INVALID_SELECTION","Select two distinct vertices.");
            var matches=mesh.Faces.Where(f=>ids.All(id=>f.Corners.Any(c=>c.VertexId==id))).ToArray();
            Checks.Require(matches.Length==1,"AMBIGUOUS_FACE","Vertices must belong to exactly one common face.");
            var face=matches[0];var corners=face.Corners.ToArray();
            int a=Array.FindIndex(corners,c=>c.VertexId==ids[0]),b=Array.FindIndex(corners,c=>c.VertexId==ids[1]);
            if(a>b) { int swap=a;a=b;b=swap; }
            Checks.Require(b-a>1 && b-a<corners.Length-1,"INVALID_DIAGONAL","Choose nonadjacent vertices.");
            Checks.Require(!mesh.EdgeFaces.ContainsKey(new CageEdgeId(ids[0],ids[1])),"INVALID_DIAGONAL","Diagonal already exists elsewhere in the mesh.");
            var normal=PolygonExtrusion.FaceNormal(mesh,face);var origin=mesh.Vertices[corners[0].VertexId].Position;
            var offsets=corners.Select(c=>mesh.Vertices[c.VertexId].Position-origin).ToArray();
            double extent=Math.Sqrt(offsets.Max(p=>Dot(p,p)));
            Checks.Require(offsets.All(p=>Math.Abs(Dot(normal,p))<=Math.Max(1e-10,extent*1e-6)),"NONPLANAR_SPLIT","This split requires a planar face.");
            Checks.Require(mesh.IdWatermarks.Face<ulong.MaxValue && mesh.IdWatermarks.Corner<=ulong.MaxValue-2,"ELEMENT_ID_EXHAUSTED","No element IDs remain.");
            var first=new CageFace(face.Id,face.Material,corners.Skip(a).Take(b-a+1));
            var secondCorners=corners.Skip(b).Concat(corners.Take(a+1)).ToArray();ulong cornerId=mesh.IdWatermarks.Corner;
            for(int i=0;i<secondCorners.Length;i++)
            {
                var c=secondCorners[i];
                if(c.VertexId==ids[0] || c.VertexId==ids[1]) secondCorners[i]=new CageCorner(++cornerId,c.VertexId,c.Uv0,c.Normal,c.Tangent);
            }
            var second=new CageFace(mesh.IdWatermarks.Face+1,face.Material,secondCorners);
            double originalArea=Area(mesh,face,normal),splitArea=Area(mesh,first,normal)+Area(mesh,second,normal);
            Checks.Require(Math.Abs(splitArea-originalArea)<=originalArea*1e-6,"INVALID_DIAGONAL","Diagonal leaves the original face.");
            var result=new PolygonMesh(mesh.DomainId,mesh.Vertices.Values,mesh.Faces.SelectMany(f=>f.Id==face.Id ? new[]{first,second} : new[]{f}),mesh.IdWatermarks);
            PolygonRenderAdapter.Build(result);return result;
        }
        static double Area(PolygonMesh mesh,CageFace face,Vec3 normal)
        {
            var triangles=PolygonTriangulator.Triangulate(mesh,face);double sum=0;
            for(int i=0;i<triangles.Length;i+=3)
            {
                var p=mesh.Vertices[face.Corners[triangles[i]].VertexId].Position;
                var a=mesh.Vertices[face.Corners[triangles[i+1]].VertexId].Position-p;
                var b=mesh.Vertices[face.Corners[triangles[i+2]].VertexId].Position-p;
                double area=((double)a.Y*b.Z-(double)a.Z*b.Y)*normal.X+((double)a.Z*b.X-(double)a.X*b.Z)*normal.Y+((double)a.X*b.Y-(double)a.Y*b.X)*normal.Z;
                Checks.Require(area>0,"INVALID_DIAGONAL","Diagonal reverses a region or leaves the face.");sum+=area;
            }
            return sum;
        }
        static double Dot(Vec3 a,Vec3 b)=>(double)a.X*b.X+(double)a.Y*b.Y+(double)a.Z*b.Z;
    }
}
