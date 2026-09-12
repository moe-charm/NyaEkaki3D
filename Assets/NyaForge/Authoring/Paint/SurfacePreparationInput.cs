using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Paint
{
    public sealed class SurfacePreparationInput
    {
        public MeshData Mesh { get; }
        public RestTransform Transform { get; }
        public SurfaceCameraSnapshot Camera { get; }
        public IReadOnlyList<ulong> LogicalVertexIds { get; }
        SurfacePreparationInput(SurfacePreparationInput source,SurfaceCameraSnapshot camera)
        {
            if(camera==null) throw new ArgumentNullException(nameof(camera));
            Mesh=source.Mesh;Transform=source.Transform;LogicalVertexIds=source.LogicalVertexIds;Camera=camera;
        }
        public SurfacePreparationInput WithCamera(SurfaceCameraSnapshot camera)=>new SurfacePreparationInput(this,camera);
        public SurfacePreparationInput(MeshData mesh,RestTransform transform,SurfaceCameraSnapshot camera,IEnumerable<ulong> logicalVertexIds=null)
        {
            Checks.Require(mesh!=null && camera!=null,"INVALID_SURFACE_PREPARATION","Mesh and camera snapshot required.");transform.Validate();
            if(logicalVertexIds!=null)
            {
                var ids=logicalVertexIds.Take(mesh.VertexCount+1).ToArray();
                Checks.Require(ids.Length==mesh.VertexCount,"INVALID_VERTEX_MAP","Logical IDs must match mesh vertices.");LogicalVertexIds=Array.AsReadOnly(ids);
            }
            Mesh=mesh;Transform=transform;Camera=camera;
        }
        internal bool SameSurface(SurfacePreparationInput other)=>other!=null && Mesh.ContentHash==other.Mesh.ContentHash && Transform.Equals(other.Transform) &&
            (LogicalVertexIds==null ? other.LogicalVertexIds==null : other.LogicalVertexIds!=null && LogicalVertexIds.SequenceEqual(other.LogicalVertexIds));
    }
    public sealed class PreparedSurface
    {
        public SurfacePaintMesh RayMesh { get; }
        public SurfaceScreenCoverage Coverage { get; }
        internal PreparedSurface(SurfacePaintMesh mesh,SurfaceScreenCoverage coverage) { RayMesh=mesh;Coverage=coverage; }
    }
    public enum SurfacePreparationPhase { Idle,Preparing,Ready,Failed,Disposed }
    public sealed class SurfacePreparationStatus
    {
        public long Generation { get; }
        public SurfacePreparationPhase Phase { get; }
        public PreparedSurface Surface { get; }
        public string Error { get; }
        internal SurfacePreparationStatus(long generation,SurfacePreparationPhase phase,PreparedSurface surface=null,string error=null)
        { Generation=generation;Phase=phase;Surface=surface;Error=error; }
    }
}
