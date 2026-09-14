using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    /// <summary>Owns one complete, replaceable set of submesh materials and their textures.</summary>
    sealed class MaterialSurfaceSet : IDisposable
    {
        readonly Dictionary<int,BaseColorSurface> surfaces=new Dictionary<int,BaseColorSurface>();
        readonly bool multiple;
        public Material[] Materials { get; }
        public bool HasPreview=>surfaces.Values.Any(s=>s.HasPreview);
        public MaterialSurfaceSet(int count,Material template,PaintImage image,MaterialParameters parameters,GraphMeshValue value)
        {
            multiple=value?.SlotMaterials!=null;
            try
            {
                if(multiple)
                {
                    // Skin derivatives use a MeshSource and therefore do not
                    // carry PolygonRenderMesh metadata. In that case the
                    // ordered authored keys are the canonical submesh map.
                    var slots=value.PolygonRendering?.MaterialSlotMap?.ToArray() ??
                        (value.SlotMaterials.Keys.OrderBy(slot=>slot).ToArray());
                    if(slots.Length!=count) throw new InvalidOperationException("Material slot map differs from submesh count.");
                    Materials=new Material[count];
                    for(int i=0;i<count;i++)
                    {
                        var material=value.SlotMaterials[slots[i]].Material;
                        var surface=new BaseColorSurface(material.BaseColor?.Image,template,material.Parameters);
                        surfaces.Add(slots[i],surface);Materials[i]=surface.Material;
                    }
                }
                else
                {
                    var surface=image==null && parameters==null ? null : new BaseColorSurface(image,template,parameters);
                    if(surface!=null) surfaces.Add(0,surface);
                    Materials=Enumerable.Repeat(surface?.Material ?? template,count).ToArray();
                }
            }
            catch { Dispose();throw; }
        }
        public void ShowPreview(PaintImage image,int? slot=null)
        {
            if(image==null && slot==null) { foreach(var surface in surfaces.Values) surface.ShowPreview(null);return; }
            if(multiple && !slot.HasValue) throw new InvalidOperationException("Choose an authored material slot for paint preview.");
            if(surfaces.TryGetValue(multiple ? slot.Value : 0,out var target)) target.ShowPreview(image);
            else if(multiple) throw new InvalidOperationException("The material slot is not displayed.");
        }
        public void Dispose() { foreach(var surface in surfaces.Values) surface.Dispose();surfaces.Clear(); }
        public void ShowPreviews(PaintImage image,IEnumerable<int> slots)
        {
            if(!multiple) throw new InvalidOperationException("Multiple slot preview requires a per-slot material output.");
            if(slots==null) throw new ArgumentNullException(nameof(slots));
            var ids=slots.Distinct().ToArray();
            if(ids.Length==0 || ids.Any(id=>!surfaces.ContainsKey(id))) throw new InvalidOperationException("Choose displayed material slots.");
            var candidates=new Texture2D[ids.Length];
            try { if(image!=null) for(int i=0;i<ids.Length;i++) candidates[i]=PaintTextureAdapter.Create(image); }
            catch { foreach(var texture in candidates) if(texture!=null) UnityEngine.Object.Destroy(texture);throw; }
            // Every allocation is ready before changing any visible material.
            for(int i=0;i<ids.Length;i++) surfaces[ids[i]].SetPreview(candidates[i]);
        }
    }
}
