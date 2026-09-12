using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace NyaForge.UnityRuntime
{
    /// <summary>Owns only the disposable display projection; the document owns geometry.</summary>
    public sealed partial class OwnedMeshProjection : IGraphAuthoringProjection, IDisposable
    {
        public const int PreviewLayer = 30;
        readonly Transform parent;
        readonly Material surface, point, selectedPoint, selectedFace, finalSurface;
        Prepared current;
        readonly HashSet<int> selected = new HashSet<int>();
        public Mesh DisplayMesh => current?.Mesh;
        public GameObject DisplayObject => current?.Root;
        public int PointBatchCount=>current?.PointMarkers?.BatchCount ?? 0;
        public int SelectedPointCount=>current?.PointMarkers?.SelectedCount ?? 0;
        public Vector3[] Points => current?.Points ?? Array.Empty<Vector3>();
        public string PreviewNodeId { get; set; } = "";
        public int HighlightedTriangleCount => current?.FaceHighlight?.TriangleCount ?? 0;
        public bool ShowFinalResult { get; set; } = true;
        public Mesh FinalMesh => current?.FinalResult?.Mesh;
        public IEnumerable<Vector3> FramingPoints => Points.Concat(current?.FinalResult?.Points ?? Array.Empty<Vector3>());

        public OwnedMeshProjection(Transform parent)
        {
            this.parent = parent;
            var shader = Resources.Load<Shader>("AuthoringSurface");
            if (!shader) throw new InvalidOperationException("Authoring surface shader missing");
            surface = new Material(shader) { color = new Color(.22f, .72f, .69f) };
            finalSurface = new Material(shader) { color = new Color(.48f, .52f, .60f) };
            finalSurface.SetFloat("_DepthBias", 1);
            selectedFace = new Material(shader) { color = new Color(1f, .59f, .16f), renderQueue = 2001 };
            selectedFace.SetFloat("_DepthBias", -1);
            point = MakeMaterial(new Color(.72f, .85f, .9f));
            selectedPoint = MakeMaterial(new Color(1f, .59f, .16f));
        }

        static Material MakeMaterial(Color color)
        {
            var shader = Shader.Find("Viewer/UnlitColor") ?? Shader.Find("Unlit/Color");
            if (!shader) throw new InvalidOperationException("Preview shader missing");
            return new Material(shader) { color = color };
        }

        public IPreparedProjection Prepare(AuthoringDocument document, MeshData evaluated)
        {
            return PrepareMesh(evaluated, document.Transform);
        }

        public IPreparedProjection PrepareGraph(AuthoringDocument document, AuthoringPreview preview)
        {
            if (PreviewNodeId != "" && !document.IsEmpty && !document.Objects[0].IsStaticProfile)
            {
                NyaForge.Authoring.Graph.GraphMeshValue stage = null;
                preview.Evaluation?.MeshOutputs.TryGetValue(PreviewNodeId, out stage);
                var final = ShowFinalResult && preview.IsComplete && preview.Output?.Mesh!=null && stage != null && preview.Output?.SnapshotHash != stage.SnapshotHash ? preview.Output : null;
                return PrepareMesh(stage?.Mesh, stage?.Transform ?? new RestTransform(1, new Vec3()), final, stage?.BaseColor,stage?.Material?.Parameters,stage);
            }
            return PrepareMesh(preview.Output?.Mesh, preview.Output?.Transform ?? new RestTransform(1, new Vec3()), null, preview.Output?.BaseColor,preview.Output?.Material?.Parameters,preview.Output);
        }

        IPreparedProjection PrepareMesh(MeshData evaluated, RestTransform transform, NyaForge.Authoring.Graph.GraphMeshValue final = null, NyaForge.Authoring.Graph.GraphImageValue baseColor = null,NyaForge.Authoring.Graph.MaterialParameters material=null,NyaForge.Authoring.Graph.GraphMeshValue appearance=null)
        {
            if(evaluated!=null && final==null && current?.FinalResult==null && current?.Renderer!=null &&
                current.MeshHash==evaluated.ContentHash && current.Transform.Equals(transform) &&
                ReferenceEquals(current.EditPolygon,PreviewNodeId!="" ? appearance?.Polygon : null))
                return new ColorUpdate(this,current,baseColor?.Image,material,appearance);
            var candidate = new Prepared(this);
            try
            {
                candidate.Root = new GameObject("Authoring mesh") { layer = PreviewLayer };
                candidate.Root.SetActive(false);
                candidate.Root.transform.SetParent(parent, false);
                if (evaluated == null)
                {
                    if(PreviewNodeId!="" && appearance?.Polygon!=null)
                    {
                        candidate.EditPolygon=appearance.Polygon;
                        candidate.Points=NyaForge.Authoring.Topology.PolygonEditPoints.VertexIds(appearance.Polygon).Select(id=>ToUnity(transform.ToAvatarPoint(appearance.Polygon.Vertices[id].Position))).ToArray();
                        candidate.PointMarkers=new EditPointProjection(candidate.Root.transform,candidate.Points,point,selectedPoint,selected);
                    }
                    return candidate;
                }
                candidate.MeshHash=evaluated.ContentHash;candidate.Transform=transform;
                candidate.EditPolygon=PreviewNodeId!="" ? appearance?.Polygon : null;
                candidate.Mesh = CreateMesh(evaluated);
                var meshObject = new GameObject("Evaluated mesh") { layer = PreviewLayer };
                meshObject.transform.SetParent(candidate.Root.transform, false);
                meshObject.transform.localScale = Vector3.one * transform.Scale;
                meshObject.transform.localPosition = ToUnity(transform.Translation);
                meshObject.AddComponent<MeshFilter>().sharedMesh = candidate.Mesh;
                candidate.BaseColor = new MaterialSurfaceSet(evaluated.Submeshes.Count,surface,baseColor?.Image,material,appearance);
                candidate.Renderer=meshObject.AddComponent<MeshRenderer>();candidate.Renderer.sharedMaterials=candidate.BaseColor.Materials;
                candidate.FaceHighlight = new FaceHighlightProjection(meshObject.transform, candidate.Mesh, selectedFace);
                if (final != null) candidate.FinalResult = new FinalResultProjection(candidate.Root.transform, final, finalSurface);
                candidate.Points = evaluated.Positions.Select(p => ToUnity(transform.ToAvatarPoint(p))).ToArray();
                if(PreviewNodeId!="" && appearance?.Polygon!=null)
                    candidate.Points=NyaForge.Authoring.Topology.PolygonEditPoints.VertexIds(appearance.Polygon).Select(id=>ToUnity(transform.ToAvatarPoint(appearance.Polygon.Vertices[id].Position))).ToArray();
                candidate.PointMarkers=new EditPointProjection(candidate.Root.transform,candidate.Points,point,selectedPoint,selected);
                return candidate;
            }
            catch { candidate.DestroyOwned(); throw; }
        }

        public void Select(IEnumerable<int> indices)
        {
            selected.Clear();
            foreach (var index in indices) selected.Add(index);
            if (current == null) return;
            current.PointMarkers?.Select(selected);
        }
        public void ShowPaintPreview(NyaForge.Authoring.Paint.PaintImage image) => current?.BaseColor?.ShowPreview(image);
        public void ShowPaintPreview(NyaForge.Authoring.Paint.PaintImage image,int authoredSlot) => current?.BaseColor?.ShowPreview(image,authoredSlot);
        public void ShowPaintPreviews(NyaForge.Authoring.Paint.PaintImage image,IEnumerable<int> authoredSlots) => current?.BaseColor?.ShowPreviews(image,authoredSlots);
        public bool HasPaintPreview=>current?.BaseColor?.HasPreview ?? false;

        public void SelectFaces(IReadOnlyList<ulong> triangleFaces, ISet<ulong> faceIds, bool faceMode)
        {
            current?.FaceHighlight?.Select(triangleFaces, faceIds);
            current?.PointMarkers?.SetVisible(!faceMode);
        }

        public static Mesh CreateMesh(MeshData data)
        {
            var mesh = new Mesh { name = "NyaForge owned mesh", indexFormat = data.VertexCount > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            try
            {
                mesh.vertices = data.Positions.Select(ToUnity).ToArray();
                if (data.Normals.Count > 0) mesh.normals = data.Normals.Select(ToUnity).ToArray();
                if (data.Tangents.Count > 0) mesh.tangents = data.Tangents.Select(v => new Vector4(v.X, v.Y, v.Z, v.W)).ToArray();
                if (data.Uv0.Count > 0) mesh.uv = data.Uv0.Select(v => new Vector2(v.X, v.Y)).ToArray();
                mesh.subMeshCount = data.Submeshes.Count;
                for (int i = 0; i < data.Submeshes.Count; i++) mesh.SetTriangles(data.Submeshes[i], i, false);
                // Lighting needs display normals even when the authored mesh intentionally omits them.
                DisplayMeshNormals.Ensure(mesh);
                mesh.RecalculateBounds();
                var bounds = mesh.bounds;
                if (!Finite(bounds.center) || !Finite(bounds.size)) throw new InvalidOperationException("Mesh bounds exceed the supported range");
                return mesh;
            }
            catch { UnityEngine.Object.Destroy(mesh); throw; }
        }

        public static Vector3 ToUnity(Vec3 value) => new Vector3(value.X, value.Y, value.Z);
        static bool Finite(Vector3 v) => !float.IsNaN(v.x) && !float.IsInfinity(v.x) && !float.IsNaN(v.y) && !float.IsInfinity(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.z);

        sealed class Prepared : IPreparedProjection
        {
            readonly OwnedMeshProjection owner;
            Prepared previous;
            bool committed;
            public Mesh Mesh;
            public string MeshHash;
            public RestTransform Transform;
            public MeshRenderer Renderer;
            public GameObject Root;
            public FaceHighlightProjection FaceHighlight;
            public FinalResultProjection FinalResult;
            public MaterialSurfaceSet BaseColor;
            public Vector3[] Points = Array.Empty<Vector3>();
            public NyaForge.Authoring.Topology.PolygonMesh EditPolygon;
            public EditPointProjection PointMarkers;
            public Prepared(OwnedMeshProjection owner) { this.owner = owner; }
            public void Commit()
            {
                previous = owner.current;
                // A failure after the first mutation must still restore the previous display.
                committed = true;
                if (previous?.Root != null) previous.Root.SetActive(false);
                Root.SetActive(true);
                owner.current = this;
            }
            public void Rollback()
            {
                if (!committed) return;
                Root.SetActive(false);
                owner.current = previous;
                if (previous?.Root != null) previous.Root.SetActive(true);
                committed = false;
                previous = null;
            }
            public void Dispose()
            {
                if (committed) { previous?.DestroyOwned(); previous = null; }
                else DestroyOwned();
            }
            public void DestroyOwned()
            {
                PointMarkers?.Dispose();PointMarkers=null;
                FaceHighlight?.Dispose(); FaceHighlight = null;
                FinalResult?.Dispose(); FinalResult = null;
                BaseColor?.Dispose(); BaseColor = null;
                if (Root != null) UnityEngine.Object.Destroy(Root);
                if (Mesh != null) UnityEngine.Object.Destroy(Mesh);
                Root = null; Mesh = null;
            }
        }

        public void Dispose()
        {
            EndSpringPreview();
            current?.DestroyOwned(); current = null;
            UnityEngine.Object.Destroy(surface);
            UnityEngine.Object.Destroy(point);
            UnityEngine.Object.Destroy(selectedPoint);
            UnityEngine.Object.Destroy(selectedFace);
            UnityEngine.Object.Destroy(finalSurface);
        }
    }
}


