using System;
namespace NyaForge.Authoring
{
    public sealed class ProjectSaveRequest
    {
        public string ExpectedInstanceId { get; }
        public string DocumentId { get; }
        public long ExpectedRevision { get; }
        public string Directory { get; }
        public long ExpectedSaveVersion { get; }
        public ProjectSaveRequest(string instance,string document,long revision,string directory,long version)
        {
            Checks.Id(instance);Checks.Id(document);
            Checks.Require(revision>=0 && version>=0,"INVALID_SAVE_REQUEST","Expected nonnegative revision and save version.");
            Checks.Require(!string.IsNullOrWhiteSpace(directory),"INVALID_SAVE_REQUEST","A destination is required.");
            ExpectedInstanceId=instance;DocumentId=document;ExpectedRevision=revision;Directory=directory;ExpectedSaveVersion=version;
        }
    }
    public sealed class ProjectSaveResult
    {
        public string DocumentId { get; }
        public long Revision { get; }
        public string StateHash { get; }
        public long SaveVersion { get; }
        public string Directory { get; }
        internal ProjectSaveResult(AuthoringWorkspace workspace)
        { DocumentId=workspace.Document.DocumentId;Revision=workspace.Document.DocumentRevision;StateHash=workspace.Document.StateHash;SaveVersion=workspace.SaveVersion;Directory=workspace.SavedDirectory; }
    }
    /// <summary>Save the explicitly observed document revision, using the existing atomic versioned store.</summary>
    public static class ProjectSaveService
    {
        public static ProjectSaveResult Save(AuthoringWorkspace workspace,ProjectSaveRequest request)
        {
            if(workspace==null || request==null) throw new ArgumentNullException(workspace==null ? nameof(workspace) : nameof(request));
            lock(workspace.Gate)
            {
                Checks.Require(!workspace.Executing,"REENTRANT_SAVE","Cannot save during a projection transaction.");
                Checks.Require(request.ExpectedInstanceId==workspace.InstanceId,"STALE_INSTANCE","Save targets another instance.");
                Checks.Require(request.DocumentId==workspace.Document.DocumentId,"DOCUMENT_CHANGED","Save targets another document.");
                Checks.Require(request.ExpectedRevision==workspace.Document.DocumentRevision,"REVISION_CONFLICT","Document changed before save.");
                ProjectStore.Save(request.Directory,workspace,request.ExpectedSaveVersion);
                return new ProjectSaveResult(workspace);
            }
        }
    }
}
