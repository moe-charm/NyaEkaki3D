using System;
using System.Collections.Generic;
using NyaForge.Authoring;
using NyaForge.Authoring.Topology;

namespace NyaForge.UnityRuntime
{
    // Synthetic geometry only. Keeps seam, empty-space and occlusion cases reproducible.
    internal static class SurfacePaintFixtures
    {
        internal static PolygonMesh Grid(int cells)
        {
            if(cells<1 || cells>256) throw new ArgumentOutOfRangeException(nameof(cells));
            var vertices=new List<CageVertex>();var faces=new List<CageFace>();ulong corner=0;
            for(int y=0;y<=cells;y++) for(int x=0;x<=cells;x++)
                vertices.Add(new CageVertex((ulong)(y*(cells+1)+x+1),new Vec3(-.1f+.2f*x/cells,-.05f+.1f*y/cells,0)));
            for(int y=0;y<cells;y++) for(int x=0;x<cells;x++)
            {
                ulong a=(ulong)(y*(cells+1)+x+1),b=a+1,c=a+(ulong)cells+1,d=c+1;
                faces.Add(new CageFace((ulong)faces.Count+1,0,new[]{
                    new CageCorner(++corner,a,new Vec2((float)x/cells,(float)y/cells)),
                    new CageCorner(++corner,b,new Vec2((float)(x+1)/cells,(float)y/cells)),
                    new CageCorner(++corner,d,new Vec2((float)(x+1)/cells,(float)(y+1)/cells)),
                    new CageCorner(++corner,c,new Vec2((float)x/cells,(float)(y+1)/cells))}));
            }
            return new PolygonMesh(Guid.NewGuid().ToString("D"),vertices,faces);
        }
        internal static PolygonMesh Create(string kind)
        {
            var vertices=new List<CageVertex>();var faces=new List<CageFace>();
            var ids=new Dictionary<Vec3,ulong>();ulong corner=0;
            void Quad(float left,float right,float z,float u0,float u1)
            {
                var positions=new[]{new Vec3(left,-.05f,z),new Vec3(right,-.05f,z),new Vec3(right,.05f,z),new Vec3(left,.05f,z)};
                var uv=new[]{new Vec2(u0,0),new Vec2(u1,0),new Vec2(u1,1),new Vec2(u0,1)};
                var corners=new CageCorner[4];
                for(int i=0;i<4;i++)
                {
                    if(!ids.TryGetValue(positions[i],out ulong id))
                    { id=(ulong)vertices.Count+1;ids.Add(positions[i],id);vertices.Add(new CageVertex(id,positions[i])); }
                    corners[i]=new CageCorner(++corner,id,uv[i]);
                }
                faces.Add(new CageFace((ulong)faces.Count+1,0,corners));
            }
            switch(kind)
            {
                case "seam": Quad(-.1f,0,0,0,.4f);Quad(0,.1f,0,.6f,1);break;
                case "gap": Quad(-.1f,-.02f,0,0,.4f);Quad(.02f,.1f,0,.6f,1);break;
                case "occluder": Quad(-.1f,.1f,0,0,1);Quad(-.025f,.025f,.01f,2,3);break;
                case "thin-occluder": Quad(-.1f,.1f,0,0,1);Quad(.00123f,.00125f,.01f,2,3);break;
                default: throw new ArgumentException("Unknown surface fixture",nameof(kind));
            }
            return new PolygonMesh(Guid.NewGuid().ToString("D"),vertices,faces);
        }
    }
}
