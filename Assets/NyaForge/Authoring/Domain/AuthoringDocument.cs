using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
        public bool IsEmpty { get { return Objects.Count == 0; } }
        public string StateHash { get; private set; }
        public string EditSourceHash { get { return IsEmpty ? "" : Objects[0].IsStaticProfile ? BaselineMesh.ContentHash : GraphContentIdentity.Hash(Objects[0].Graph); } }
        // The current static editing profile evaluates one object. These adapters
        // preserve the v1 API while project creation no longer requires a mesh.
        public string ObjectId { get { return IsEmpty ? "" : Objects[0].ObjectId; } }
        public RestTransform Transform { get { return IsEmpty ? new RestTransform(1, new Vec3()) : Objects[0].Transform; } }
        public MeshData BaselineMesh { get { return IsEmpty ? null : Objects[0].BaselineMesh; } }
        public bool LayerEnabled { get { return !IsEmpty && Objects[0].LayerEnabled; } }
        public IReadOnlyDictionary<int, Vec3> Offsets { get { return IsEmpty ? EmptyOffsets : Objects[0].Offsets; } }
        static readonly IReadOnlyDictionary<int, Vec3> EmptyOffsets =
            new ReadOnlyDictionary<int, Vec3>(new Dictionary<int, Vec3>());

        internal AuthoringDocument(string id, string objectId, string name, long revision,
            RestTransform transform, MeshData baseline, bool enabled, IDictionary<int, Vec3> offsets)
            : this(id, name, revision, new AuthoringObject(objectId, transform, baseline, enabled, offsets)) { }

        internal AuthoringDocument(string id, string name, long revision, AuthoringObject item)
        {
            Checks.Id(id); Checks.Name(name);
            Checks.Require(revision >= 0 && revision < long.MaxValue, "INVALID_REVISION", "Invalid project revision.");
            DocumentId = id; Name = name; DocumentRevision = revision;
            Objects = Array.AsReadOnly(item == null ? Array.Empty<AuthoringObject>() : new[] { item });
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                if (IsEmpty) { writer.Write("NYAF-empty-project-v1"); writer.Write(id); writer.Write(name); }
                else if (!item.IsStaticProfile)
                {
                    writer.Write("NYAF-graph-project-v1");
                    writer.Write(id); writer.Write(ObjectId); writer.Write(name);
                    writer.Write(GraphContentIdentity.Hash(item.Graph));
                }
                else
                {
                    // Keep the historical state hash for a single v1 object.
                    writer.Write(id); writer.Write(ObjectId); writer.Write(name);
                    writer.Write(BaselineMesh.ContentHash); writer.Write(Checks.Canonical(Transform.Scale));
                    MeshBinary.Write(writer, Transform.Translation); writer.Write(LayerEnabled);
                    writer.Write(DeltaBinary.Write(Offsets));
                }
                StateHash = Checks.Hash(stream.ToArray());
            }
        }

        internal AuthoringDocument Changed(long revision, bool enabled, IDictionary<int, Vec3> offsets)
        {
            Checks.Require(!IsEmpty, "NO_EDITABLE_OBJECT", "Add a mesh before editing.");
            return new AuthoringDocument(DocumentId, ObjectId, Name, revision, Transform, BaselineMesh, enabled, offsets);
        }
        internal AuthoringDocument AtRevision(long revision)
        {
            return new AuthoringDocument(DocumentId, Name, revision, IsEmpty ? null : Objects[0]);
        }
        internal AuthoringDocument WithGraph(NyaForge.Authoring.Graph.AuthoringGraph graph, long revision)
        {
            Checks.Require(!IsEmpty, "NO_EDITABLE_OBJECT", "Add an object before replacing its graph.");
            return new AuthoringDocument(DocumentId, Name, revision, new AuthoringObject(ObjectId, graph));
        }
        internal AuthoringDocument AddMesh(string objectId, MeshData mesh, RestTransform transform, long revision)
        {
            Checks.Require(IsEmpty, "OBJECT_LIMIT", "This editing profile supports one object; create an empty project first.");
            return new AuthoringDocument(DocumentId, objectId: objectId, name: Name, revision: revision,
                transform: transform, baseline: mesh, enabled: true, offsets: new Dictionary<int, Vec3>());
        }
        public MeshData Evaluate() { return IsEmpty ? null : Objects[0].Evaluate(); }
    }
}
