using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Paint;
using NyaForge.Authoring.Graph;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    public sealed partial class OwnedMeshProjection
    {
        // Transactional material replacement. Geometry and marker ownership remain with the current projection.
        sealed class ColorUpdate : IPreparedProjection
        {
            readonly OwnedMeshProjection owner;
            readonly Prepared target;
            readonly Material[] beforeMaterials,afterMaterials;
            MaterialSurfaceSet before,after;
            bool committed,disposed;
            internal ColorUpdate(OwnedMeshProjection owner,Prepared target,PaintImage image,MaterialParameters parameters=null,GraphMeshValue appearance=null)
            {
                this.owner=owner;this.target=target;before=target.BaseColor;beforeMaterials=target.Renderer.sharedMaterials;
                try
                {
                    after=new MaterialSurfaceSet(beforeMaterials.Length,owner.surface,image,parameters,appearance);
                    afterMaterials=after.Materials;
                }
                catch { after?.Dispose();throw; }
            }
            public void Commit()
            {
                if(disposed || committed || !ReferenceEquals(owner.current,target) || !ReferenceEquals(target.BaseColor,before)) throw new InvalidOperationException("Projection changed before color commit.");
                committed=true;target.Renderer.sharedMaterials=afterMaterials;target.BaseColor=after;
            }
            public void Rollback()
            {
                if(!committed || disposed) return;
                target.Renderer.sharedMaterials=beforeMaterials;target.BaseColor=before;committed=false;
            }
            public void Dispose()
            {
                if(disposed) return;disposed=true;
                if(committed) before?.Dispose();else after?.Dispose();before=null;after=null;
            }
        }
    }
}
