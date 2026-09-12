using System;
using System.Collections.Generic;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    /// <summary>Owned chunked octahedron markers; materials belong to the caller.</summary>
    sealed class EditPointProjection : IDisposable
    {
        const int ChunkSize=2048;
        static readonly Vector3[] Offsets={Vector3.right,Vector3.left,Vector3.up,Vector3.down,Vector3.forward,Vector3.back};
        static readonly int[] Faces={0,2,4,2,1,4,1,3,4,3,0,4,2,0,5,1,2,5,3,1,5,0,3,5};
        readonly List<Chunk> chunks=new List<Chunk>();
        readonly HashSet<int> selection=new HashSet<int>();
        public int PointCount { get; }
        public int BatchCount=>chunks.Count;
        public int SelectedCount=>selection.Count;
        public EditPointProjection(Transform parent,Vector3[] points,Material normal,Material selected,ISet<int> initial)
        {
            PointCount=points.Length;
            try
            {
                for(int start=0;start<points.Length;start+=ChunkSize)
                {
                    int count=Math.Min(ChunkSize,points.Length-start);
                    var chunk=new Chunk { Start=start,Count=count };chunks.Add(chunk);
                    chunk.Root=new GameObject("Edit points "+start) { layer=OwnedMeshProjection.PreviewLayer };chunk.Root.transform.SetParent(parent,false);
                    chunk.Mesh=new Mesh { name="Edit point batch" };chunk.Mesh.MarkDynamic();
                    var vertices=new Vector3[count*6];
                    for(int i=0;i<count;i++) for(int j=0;j<6;j++) vertices[i*6+j]=points[start+i]+Offsets[j]*.00175f;
                    chunk.Mesh.vertices=vertices;chunk.Mesh.subMeshCount=2;
                    chunk.Root.AddComponent<MeshFilter>().sharedMesh=chunk.Mesh;
                    chunk.Renderer=chunk.Root.AddComponent<MeshRenderer>();chunk.Renderer.sharedMaterials=new[]{normal,selected};
                    chunk.Renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;chunk.Renderer.receiveShadows=false;
                }
                UpdateSelection(initial,true);
            }
            catch { Dispose();throw; }
        }
        public void Select(ISet<int> indices)=>UpdateSelection(indices,false);
        void UpdateSelection(ISet<int> indices,bool force)
        {
            var valid=new HashSet<int>();foreach(int i in indices) if(i>=0 && i<PointCount) valid.Add(i);
            if(!force && selection.SetEquals(valid)) return;
            selection.Clear();selection.UnionWith(valid);
            foreach(var chunk in chunks)
            {
                var plain=new List<int>();var highlighted=new List<int>();
                for(int i=0;i<chunk.Count;i++)
                {
                    var target=selection.Contains(chunk.Start+i) ? highlighted : plain;
                    foreach(int corner in Faces) target.Add(i*6+corner);
                }
                chunk.Mesh.SetTriangles(plain,0,false);chunk.Mesh.SetTriangles(highlighted,1,false);chunk.Mesh.RecalculateBounds();
            }
        }
        public void SetVisible(bool visible) { foreach(var chunk in chunks) chunk.Renderer.enabled=visible; }
        public void Dispose()
        {
            foreach(var chunk in chunks) { if(chunk.Root) UnityEngine.Object.Destroy(chunk.Root);if(chunk.Mesh) UnityEngine.Object.Destroy(chunk.Mesh); }
            chunks.Clear();
        }
        sealed class Chunk { internal int Start,Count;internal GameObject Root;internal Mesh Mesh;internal MeshRenderer Renderer; }
    }
}
