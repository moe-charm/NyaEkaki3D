using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    /// <summary>Read-only backdrop for inactive authoring objects. The active object keeps the editable projection.</summary>
    sealed class MultiObjectProjection : IDisposable
    {
        sealed class Entry
        {
            internal GameObject Root;
            internal Mesh Mesh;
        }

        readonly Transform parent;
        readonly Material material;
        readonly List<Entry> entries = new List<Entry>();
        bool visible = true;

        internal bool Visible
        {
            get { return visible; }
            set
            {
                visible = value;
                foreach (var entry in entries) if (entry.Root != null) entry.Root.SetActive(value);
            }
        }

        internal IEnumerable<Vector3> FramingPoints => entries.SelectMany(entry =>
            !visible || entry.Root == null || entry.Mesh == null ? Enumerable.Empty<Vector3>() : entry.Mesh.vertices.Select(point => entry.Root.transform.TransformPoint(point)));

        internal int EntryCount => entries.Count;

        internal MultiObjectProjection(Transform parent)
        {
            this.parent = parent;
            var shader = Resources.Load<Shader>("AuthoringSurface");
            if (!shader) throw new InvalidOperationException("Authoring surface shader missing");
            material = new Material(shader) { color = new Color(.16f, .38f, .42f) };
            material.SetFloat("_DepthBias", 2);
        }

        internal void Refresh(AuthoringDocument document, Func<AuthoringObject, PoseTransform?> attachmentResolver = null)
        {
            var next = new List<Entry>();
            try
            {
                if (visible && document != null && document.Objects.Count > 1)
                {
                    foreach (var item in document.Objects)
                    {
                        if (item.ObjectId == document.ActiveObjectId) continue;
                        MeshData data = null;
                        RestTransform transform = new RestTransform(1, new Vec3());
                        if (item.IsStaticProfile) { data = item.Evaluate(); transform = item.Transform; }
                        else
                        {
                            var evaluation = item.EvaluateGraph();
                            if (evaluation.IsComplete && evaluation.Output != null) { data = evaluation.Output.Mesh; transform = evaluation.Output.Transform; }
                        }
                        if (data == null) continue;
                        var attachmentPose = attachmentResolver == null ? (PoseTransform?)null : attachmentResolver(item);
                        var mesh = OwnedMeshProjection.CreateMesh(data);
                        var root = new GameObject("Inactive object " + item.ObjectId) { layer = OwnedMeshProjection.PreviewLayer };
                        root.transform.SetParent(parent, false);
                        root.transform.localScale = Vector3.one * transform.Scale;
                        root.transform.localPosition = OwnedMeshProjection.ToUnity(transform.Translation);
                        if (attachmentPose.HasValue)
                        {
                            root.transform.localPosition = OwnedMeshProjection.ToUnity(attachmentPose.Value.Translation);
                            root.transform.localRotation = Quaternion.LookRotation(OwnedMeshProjection.ToUnity(attachmentPose.Value.ZAxis), OwnedMeshProjection.ToUnity(attachmentPose.Value.YAxis));
                        }
                        var filter = root.AddComponent<MeshFilter>(); filter.sharedMesh = mesh;
                        var renderer = root.AddComponent<MeshRenderer>();
                        renderer.sharedMaterials = Enumerable.Repeat(material, data.Submeshes.Count).ToArray();
                        root.SetActive(visible);
                        next.Add(new Entry { Root = root, Mesh = mesh });
                    }
                }
                ClearEntries();
                entries.AddRange(next);
            }
            catch
            {
                foreach (var entry in next) DestroyEntry(entry);
                throw;
            }
        }

        void ClearEntries()
        {
            foreach (var entry in entries) DestroyEntry(entry);
            entries.Clear();
        }

        static void DestroyEntry(Entry entry)
        {
            if (entry.Root != null) UnityEngine.Object.Destroy(entry.Root);
            if (entry.Mesh != null) UnityEngine.Object.Destroy(entry.Mesh);
        }

        public void Dispose()
        {
            ClearEntries();
            if (material != null) UnityEngine.Object.Destroy(material);
        }
    }
}
