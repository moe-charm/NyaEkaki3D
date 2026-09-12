using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace NyaForge.Authoring
{
    public sealed class AuthoringWorkspace
    {
        internal readonly object Gate = new object();
        internal readonly List<AuthoringDocument> UndoStack = new List<AuthoringDocument>();
        internal readonly List<AuthoringDocument> RedoStack = new List<AuthoringDocument>();
        internal readonly Dictionary<string, CachedCommand> Commands = new Dictionary<string, CachedCommand>();
        internal string SavedStateHash;
        internal string SavedAttachmentsHash = ProjectAttachments.Empty.ContentHash;
        public ProjectAttachments Attachments { get; private set; } = ProjectAttachments.Empty;
        internal string SavedDirectory;
        internal bool Executing;
        public string InstanceId { get; private set; }
        public AuthoringDocument Document { get; internal set; }
        public long SaveVersion { get; internal set; }
        public AuthoringPreview Preview { get; internal set; }
        public bool CanUndo { get { lock (Gate) return UndoStack.Count > 0; } }
        public bool CanRedo { get { lock (Gate) return RedoStack.Count > 0; } }
        public bool IsExecuting { get { lock (Gate) return Executing; } }
        public bool IsDirty { get { lock (Gate) return SavedStateHash != Document.StateHash || SavedAttachmentsHash != Attachments.ContentHash; } }
        public void SetAttachments(ProjectAttachments attachments)
        {
            lock (Gate)
            {
                Checks.Require(!Executing, "REENTRANT_SAVE", "Cannot replace metadata during a command transaction.");
                Checks.Require(attachments != null, "INVALID_ATTACHMENT", "Project metadata is required.");
                Attachments = attachments;
            }
        }
        internal AuthoringWorkspace(AuthoringDocument document) { InstanceId = Guid.NewGuid().ToString("D"); Document = document; Preview = AuthoringPreview.Create(document, null); }
        public static AuthoringWorkspace Create(MeshData mesh, RestTransform transform, string name)
        {
            var doc = new AuthoringDocument(Guid.NewGuid().ToString("D"),Guid.NewGuid().ToString("D"),name,0,transform,mesh,true,new Dictionary<int, Vec3>());
            doc.Evaluate(); return new AuthoringWorkspace(doc);
        }
        public static AuthoringWorkspace CreateFixture(float scale = 1f) { return Create(AuthoringFixtures.Panel(scale),new RestTransform(scale,new Vec3()),"NF-0 reference panel"); }
        public static AuthoringWorkspace CreateEmpty(string name = "Untitled")
        {
            return new AuthoringWorkspace(new AuthoringDocument(Guid.NewGuid().ToString("D"), name, 0, null));
        }
        public MeshData Evaluate() { lock (Gate) return Document.Evaluate(); }
        public CommandEnvelope NewCommand(params AuthoringOperation[] operations)
        {
            lock (Gate) return new CommandEnvelope { ExpectedInstanceId = InstanceId, DocumentId = Document.DocumentId, ExpectedDocumentRevision = Document.DocumentRevision, CommandId = Guid.NewGuid().ToString("D"), ObjectId = Document.ObjectId, ExpectedBaselineHash = Document.EditSourceHash, Operations = operations };
        }
    }
}
