using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace NyaForge.Authoring
{
    /// <summary>Project identity and owned objects; a project may contain no geometry.</summary>
    public sealed class AuthoringDocument
    {
        public string DocumentId { get; private set; }
        public string Name { get; private set; }
        public long DocumentRevision { get; private set; }
        public IReadOnlyList<AuthoringObject> Objects { get; private set; }
        public string ActiveObjectId { get; private set; }
        public AuthoringObject ActiveObject { get; private set; }
        public bool IsEmpty { get { return Objects.Count == 0; } }
        public string StateHash { get; private set; }
        public string EditSourceHash { get { return IsEmpty ? "" : ActiveObject.IsStaticProfile ? BaselineMesh.ContentHash : GraphContentIdentity.Hash(ActiveObject.Graph); } }
        // The current static editing profile evaluates one object. These adapters
        // preserve the v1 API while project creation no longer requires a mesh.
        public string ObjectId { get { return IsEmpty ? "" : ActiveObject.ObjectId; } }
        public RestTransform Transform { get { return IsEmpty ? new RestTransform(1, new Vec3()) : ActiveObject.Transform; } }
        public MeshData BaselineMesh { get { return IsEmpty ? null : ActiveObject.BaselineMesh; } }
        public bool LayerEnabled { get { return !IsEmpty && ActiveObject.LayerEnabled; } }
        public IReadOnlyDictionary<int, Vec3> Offsets { get { return IsEmpty ? EmptyOffsets : ActiveObject.Offsets; } }
        static readonly IReadOnlyDictionary<int, Vec3> EmptyOffsets =
            new ReadOnlyDictionary<int, Vec3>(new Dictionary<int, Vec3>());

        internal AuthoringDocument(string id, string objectId, string name, long revision,
            RestTransform transform, MeshData baseline, bool enabled, IDictionary<int, Vec3> offsets)
            : this(id, name, revision, new AuthoringObject(objectId, transform, baseline, enabled, offsets)) { }

        internal AuthoringDocument(string id, string name, long revision, AuthoringObject item)
            : this(id, name, revision, item == null ? Array.Empty<AuthoringObject>() : new[] { item }, item == null ? "" : item.ObjectId, true) { }

        internal AuthoringDocument(string id, string name, long revision, IEnumerable<AuthoringObject> items, string activeObjectId, bool multipleObjects)
        {
            Checks.Id(id); Checks.Name(name);
            Checks.Require(revision >= 0 && revision < long.MaxValue, "INVALID_REVISION", "Invalid project revision.");
            var values = (items ?? Array.Empty<AuthoringObject>()).ToArray();
            Checks.Require(values.Length <= 64, "OBJECT_BUDGET_EXCEEDED", "Object count exceeds the project capacity.");
            Checks.Require(values.All(item => item != null), "INVALID_DOCUMENT", "Object list contains a null item.");
            Checks.Require(values.Select(item => item.ObjectId).Distinct(StringComparer.Ordinal).Count() == values.Length, "DUPLICATE_OBJECT", "Object identity repeats.");
            Checks.Require(values.Length == 0 || values.All(item => item.IsStaticProfile) || values.All(item => !item.IsStaticProfile), "MIXED_OBJECT_PROFILE", "Static and graph objects cannot share one project.");
            if (values.Length > 0)
            {
                if (string.IsNullOrEmpty(activeObjectId)) activeObjectId = values[0].ObjectId;
                Checks.Require(values.Any(item => item.ObjectId == activeObjectId), "OBJECT_NOT_FOUND", "Active object is not in the project.");
            }
            else activeObjectId = "";
            DocumentId = id; Name = name; DocumentRevision = revision;
            // Keep the active target first so legacy consumers that read Objects[0]
            // automatically follow an explicit object selection. The multi-object
            // state hash below sorts by identity, so this view order is not identity.
            Objects = Array.AsReadOnly(values.OrderBy(item => item.ObjectId == activeObjectId ? 0 : 1).ThenBy(item => item.ObjectId, StringComparer.Ordinal).ToArray());
            ActiveObjectId = activeObjectId;
            ActiveObject = values.FirstOrDefault(item => item.ObjectId == activeObjectId);
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                if (IsEmpty) { writer.Write("NYAF-empty-project-v1"); writer.Write(id); writer.Write(name); }
                else if (values.Length == 1 && !values[0].IsStaticProfile)
                {
                    writer.Write("NYAF-graph-project-v1");
                    writer.Write(id); writer.Write(ObjectId); writer.Write(name);
                    writer.Write(GraphContentIdentity.Hash(values[0].Graph));
                }
                else if (values.Length == 1)
                {
                    // Keep the historical state hash for a single v1 object.
                    writer.Write(id); writer.Write(ObjectId); writer.Write(name);
                    writer.Write(BaselineMesh.ContentHash); writer.Write(Checks.Canonical(Transform.Scale));
                    MeshBinary.Write(writer, Transform.Translation); writer.Write(LayerEnabled);
                    writer.Write(DeltaBinary.Write(Offsets));
                }
                else
                {
                    writer.Write(values[0].IsStaticProfile ? "NYAF-static-project-v2" : "NYAF-graph-project-v2");
                    writer.Write(id); writer.Write(name); writer.Write(ActiveObjectId); writer.Write(values.Length);
                    foreach (var value in values.OrderBy(item => item.ObjectId, StringComparer.Ordinal))
                    {
                        writer.Write(value.ObjectId);
                        if (value.IsStaticProfile)
                        {
                            writer.Write(value.BaselineMesh.ContentHash); writer.Write(Checks.Canonical(value.Transform.Scale));
                            MeshBinary.Write(writer, value.Transform.Translation); writer.Write(value.LayerEnabled); writer.Write(DeltaBinary.Write(value.Offsets));
                        }
                        else writer.Write(GraphContentIdentity.Hash(value.Graph));
                    }
                }
                StateHash = Checks.Hash(stream.ToArray());
            }
        }

        internal AuthoringDocument Changed(long revision, bool enabled, IDictionary<int, Vec3> offsets)
        {
            Checks.Require(!IsEmpty, "NO_EDITABLE_OBJECT", "Add a mesh before editing.");
            var replacement = new AuthoringObject(ObjectId, Transform, BaselineMesh, enabled, offsets);
            var values = Objects.Select(item => item.ObjectId == ActiveObjectId ? replacement : item).ToArray();
            return new AuthoringDocument(DocumentId, Name, revision, values, ActiveObjectId, true);
        }
        internal AuthoringDocument AtRevision(long revision)
        {
            return new AuthoringDocument(DocumentId, Name, revision, Objects, ActiveObjectId, true);
        }
        internal AuthoringDocument WithGraph(NyaForge.Authoring.Graph.AuthoringGraph graph, long revision)
        {
            Checks.Require(!IsEmpty, "NO_EDITABLE_OBJECT", "Add an object before replacing its graph.");
            var values = Objects.Select(item => item.ObjectId == ActiveObjectId ? new AuthoringObject(item.ObjectId, graph) : item).ToArray();
            return new AuthoringDocument(DocumentId, Name, revision, values, ActiveObjectId, true);
        }
        internal AuthoringDocument AddMesh(string objectId, MeshData mesh, RestTransform transform, long revision)
        {
            Checks.Require(IsEmpty || Objects.All(item => item.IsStaticProfile), "MIXED_OBJECT_PROFILE", "Static objects cannot be added to a graph project.");
            var values = Objects.Concat(new[] { new AuthoringObject(objectId, transform, mesh, true, new Dictionary<int, Vec3>()) }).ToArray();
            return new AuthoringDocument(DocumentId, Name, revision, values, objectId, true);
        }
        internal AuthoringDocument AddGraph(string objectId, NyaForge.Authoring.Graph.AuthoringGraph graph, long revision)
        {
            Checks.Require(IsEmpty || Objects.All(item => !item.IsStaticProfile), "MIXED_OBJECT_PROFILE", "Graph objects cannot be added to a static project.");
            var values = Objects.Concat(new[] { new AuthoringObject(objectId, graph) }).ToArray();
            return new AuthoringDocument(DocumentId, Name, revision, values, objectId, true);
        }
        internal AuthoringDocument SelectObject(string objectId, long revision)
        {
            Checks.Require(Objects.Any(item => item.ObjectId == objectId), "OBJECT_NOT_FOUND", "Cannot select a missing object.");
            return new AuthoringDocument(DocumentId, Name, revision, Objects, objectId, true);
        }
        public MeshData Evaluate() { return IsEmpty ? null : ActiveObject.Evaluate(); }
    }
}
