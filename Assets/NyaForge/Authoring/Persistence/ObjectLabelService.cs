using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring
{
    /// <summary>Revision and attachment-pinned request for an MCP object display-name change.</summary>
    public sealed class ObjectLabelRequest
    {
        public string ExpectedInstanceId { get; }
        public string DocumentId { get; }
        public long ExpectedRevision { get; }
        public string ExpectedAttachmentsHash { get; }
        public string ObjectId { get; }
        public string DisplayName { get; }

        public ObjectLabelRequest(string instance, string document, long revision, string attachmentsHash, string objectId, string displayName)
        {
            Checks.Id(instance); Checks.Id(document); Checks.Id(objectId);
            Checks.Require(revision >= 0, "INVALID_OBJECT_LABEL_REQUEST", "Expected a nonnegative revision.");
            Checks.HashText(attachmentsHash);
            Checks.Require(displayName != null, "INVALID_OBJECT_LABEL_REQUEST", "A display name is required; use an empty string to clear it.");
            if (displayName.Length > 0) Checks.Name(displayName);
            ExpectedInstanceId = instance; DocumentId = document; ExpectedRevision = revision;
            ExpectedAttachmentsHash = attachmentsHash; ObjectId = objectId; DisplayName = displayName;
        }
    }

    public sealed class ObjectLabelResult
    {
        public string ObjectId { get; }
        public string DisplayName { get; }
        public string AttachmentsHash { get; }
        public long Revision { get; }

        internal ObjectLabelResult(string objectId, string displayName, string attachmentsHash, long revision)
        { ObjectId = objectId; DisplayName = displayName; AttachmentsHash = attachmentsHash; Revision = revision; }
    }

    /// <summary>Applies one metadata-only label change through the workspace history.</summary>
    public static class ObjectLabelService
    {
        public static ObjectLabelResult Set(AuthoringWorkspace workspace, ObjectLabelRequest request)
        {
            if (workspace == null || request == null) throw new ArgumentNullException(workspace == null ? nameof(workspace) : nameof(request));
            lock (workspace.Gate)
            {
                Checks.Require(!workspace.Executing, "REENTRANT_SAVE", "Cannot change metadata during a command transaction.");
                Checks.Require(request.ExpectedInstanceId == workspace.InstanceId, "STALE_INSTANCE", "Display name targets another instance.");
                Checks.Require(request.DocumentId == workspace.Document.DocumentId, "DOCUMENT_CHANGED", "Display name targets another document.");
                Checks.Require(request.ExpectedRevision == workspace.Document.DocumentRevision, "REVISION_CONFLICT", "Document changed before display name update.");
                Checks.Require(request.ExpectedAttachmentsHash == workspace.Attachments.ContentHash, "ATTACHMENTS_CHANGED", "Metadata changed before display name update.");
                Checks.Require(workspace.Document.Objects.Any(item => item.ObjectId == request.ObjectId), "OBJECT_NOT_FOUND", "Display name targets a missing object.");

                var labels = new Dictionary<string, string>(StringComparer.Ordinal);
                var existing = workspace.Attachments.Read(ProjectAttachments.ObjectLabels);
                if (existing != null)
                    foreach (var item in ObjectLabelsCodec.Read(existing)) labels[item.Key] = item.Value;
                if (string.IsNullOrEmpty(request.DisplayName)) labels.Remove(request.ObjectId);
                else labels[request.ObjectId] = request.DisplayName;

                var owned = new Dictionary<string, byte[]>(StringComparer.Ordinal);
                foreach (var key in workspace.Attachments.Hashes.Keys)
                    if (key != ProjectAttachments.ObjectLabels) owned[key] = workspace.Attachments.Read(key);
                if (labels.Count > 0) owned[ProjectAttachments.ObjectLabels] = ObjectLabelsCodec.Write(labels);
                workspace.SetAttachmentsWithHistory(new ProjectAttachments(owned));
                return new ObjectLabelResult(request.ObjectId, request.DisplayName, workspace.Attachments.ContentHash, workspace.Document.DocumentRevision);
            }
        }
    }

    /// <summary>Strict JSON reader for the metadata-only MCP endpoint.</summary>
    public static class ObjectLabelWireReader
    {
        public static ObjectLabelRequest Read(JObject value, string instance)
        {
            Checks.Require(value != null && value.Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal)
                .SequenceEqual(new[] { "documentId", "expectedAttachmentsHash", "expectedRevision", "objectId", "displayName" }.OrderBy(n => n, StringComparer.Ordinal)),
                "INVALID_OBJECT_LABEL_REQUEST", "Unexpected display-name fields.");
            foreach (var field in new[] { "documentId", "expectedAttachmentsHash", "objectId", "displayName" })
                Checks.Require(value[field]?.Type == JTokenType.String, "INVALID_OBJECT_LABEL_REQUEST", "Expected display-name string: " + field);
            long revision = 0;
            Checks.Require(value["expectedRevision"]?.Type == JTokenType.Integer && long.TryParse(value["expectedRevision"].ToString(), out revision) && revision >= 0,
                "INVALID_OBJECT_LABEL_REQUEST", "Expected a nonnegative revision.");
            return new ObjectLabelRequest(instance, (string)value["documentId"], revision, (string)value["expectedAttachmentsHash"],
                (string)value["objectId"], (string)value["displayName"]);
        }
    }
}
