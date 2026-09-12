using System;
using System.Collections.Generic;
using NyaForge.Authoring;
using NyaForge.Authoring.Topology;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    /// <summary>Owned display-only boundary loop. No colliders, mesh edits or document state.</summary>
    sealed class BoundaryHighlightProjection : IDisposable
    {
        readonly GameObject root;
        readonly LineRenderer line;
        readonly Material material;
        readonly bool closed;
        public int VertexCount=>line.enabled ? line.positionCount : 0;
        public Vector3 Position(int index)=>line.GetPosition(index);
        public BoundaryHighlightProjection(Transform parent,Color? tint=null,bool closed=true)
        {
            this.closed=closed;
            var shader=Resources.Load<Shader>("AuthoringSurface");
            if(!shader) throw new InvalidOperationException("Authoring surface shader missing");
            material=new Material(shader) { color=tint ?? new Color(1,.72f,.12f),renderQueue=2002 };
            material.SetFloat("_DepthBias",-2);
            root=new GameObject("Selected boundary") { layer=OwnedMeshProjection.PreviewLayer };
            root.transform.SetParent(parent,false);
            line=root.AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=false;
            line.loop=closed;line.numCornerVertices=2;line.alignment=LineAlignment.View;
            line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.receiveShadows=false;line.enabled=false;
        }
        public void Show(PolygonMesh polygon,RestTransform transform,IReadOnlyList<ulong> vertices)
        {
            if(polygon==null || vertices==null || vertices.Count<(closed ? 3 : 2)) { Clear();return; }
            var points=new Vector3[vertices.Count];
            for(int i=0;i<points.Length;i++)
            {
                if(!polygon.Vertices.TryGetValue(vertices[i],out var vertex)) { Clear();return; }
                var p=transform.ToAvatarPoint(vertex.Position);points[i]=new Vector3(p.X,p.Y,p.Z);
            }
            var bounds=new Bounds(points[0],Vector3.zero);foreach(var p in points) bounds.Encapsulate(p);
            line.positionCount=points.Length;line.SetPositions(points);
            line.widthMultiplier=Mathf.Clamp(bounds.size.magnitude*.008f,.0005f,.02f);line.enabled=true;
        }
        public void Clear() { line.enabled=false;line.positionCount=0; }
        public void Dispose() { UnityEngine.Object.Destroy(root);UnityEngine.Object.Destroy(material); }
    }
}

