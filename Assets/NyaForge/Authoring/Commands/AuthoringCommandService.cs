using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace NyaForge.Authoring
{
    public sealed class AuthoringCommandService
    {
        private readonly AuthoringWorkspace workspace;
        public AuthoringCommandService(AuthoringWorkspace workspace) { this.workspace = workspace ?? throw new ArgumentNullException("workspace"); }
        public CommandResult Execute(CommandEnvelope command, IAuthoringProjection projection = null,CommandTimings timings=null)
        {
            lock (workspace.Gate)
            {
                // Reject before entering finally: a nested rejection must not clear
                // the outer transaction's ownership of the execution guard.
                if (workspace.Executing) return Failure("REENTRANT_COMMAND", "Projection callbacks cannot submit edits.");
                timings?.Begin();
                IPreparedProjection prepared = null; bool committed = false; string fingerprint = null;
                try
                {
                    Checks.Require(command != null, "INVALID_COMMAND", "Missing command.");
                    // Freeze mutable transport fields before hashing/validation. Operations
                    // themselves own their immutable selection and delta payloads.
                    command = new CommandEnvelope { ExpectedInstanceId = command.ExpectedInstanceId, DocumentId = command.DocumentId, ExpectedDocumentRevision = command.ExpectedDocumentRevision, CommandId = command.CommandId, ObjectId = command.ObjectId, ExpectedBaselineHash = command.ExpectedBaselineHash, Operations = command.Operations == null ? null : (AuthoringOperation[])command.Operations.Clone() };
                    Checks.Require(command.ExpectedInstanceId == workspace.InstanceId, "STALE_INSTANCE", "Read the current app instance before editing.");
                    Checks.Id(command.CommandId); Checks.Id(command.DocumentId);
                    if (command.ObjectId != "") Checks.Id(command.ObjectId);
                    if (command.ExpectedBaselineHash != "") Checks.HashText(command.ExpectedBaselineHash);
                    fingerprint = Fingerprint(command);
                    CachedCommand cached;
                    // A successful retry carries the old revision: resolve it first.
                    if (workspace.Commands.TryGetValue(command.CommandId, out cached))
                    {
                        Checks.Require(cached.Fingerprint == fingerprint, "COMMAND_ID_REUSED", "Command ID already belongs to a different payload.");
                        return cached.Result;
                    }
                    Checks.Require(workspace.Commands.Count < AuthoringLimits.MaxCommands, "COMMAND_BUDGET_EXCEEDED", "Reopen the project before submitting additional unique commands.");
                    Checks.Require(command.DocumentId == workspace.Document.DocumentId && command.ObjectId == workspace.Document.ObjectId, "SOURCE_CHANGED", "Document or object identity changed.");
                    Checks.Require(command.ExpectedDocumentRevision == workspace.Document.DocumentRevision, "REVISION_CONFLICT", "Document changed; inspect state again.");
                    Checks.Require(command.ExpectedBaselineHash == workspace.Document.EditSourceHash, "BASE_MESH_CHANGED", "The command targets different source content.");
                    workspace.Executing = true;
                    timings?.Validated();
                    var before = workspace.Document; long revision = checked(before.DocumentRevision + 1);
                    bool undo = command.Operations[0].Kind == "history.undo", redo = command.Operations[0].Kind == "history.redo";
                    AuthoringDocument candidate;
                    if (undo || redo)
                    {
                        Checks.Require(command.Operations.Length == 1, "INVALID_COMMAND", "History commands cannot be batched.");
                        var stack = undo ? workspace.UndoStack : workspace.RedoStack;
                        Checks.Require(stack.Count > 0, "HISTORY_EMPTY", "There is no history in that direction.");
                        candidate = stack[stack.Count - 1].AtRevision(revision);
                    }
                    else
                    {
                        candidate = before;
                        foreach (var operation in command.Operations)
                            candidate = OperationEvaluator.Apply(candidate, operation, revision);
                    }
                    timings?.CandidateBuilt();
                    var preview = AuthoringPreview.Create(candidate, workspace.Preview);
                    timings?.Evaluated();
                    MeshData evaluated = preview.IsComplete ? preview.Output?.Mesh : null;
                    if (projection != null)
                    {
                        if (projection is IGraphAuthoringProjection graphProjection) prepared = graphProjection.PrepareGraph(candidate, preview);
                        else
                        {
                            Checks.Require(candidate.IsEmpty || candidate.Objects[0].IsStaticProfile, "GRAPH_PROJECTION_REQUIRED", "This view cannot display graph authoring state.");
                            prepared = projection.Prepare(candidate, evaluated);
                        }
                        Checks.Require(prepared != null,"PROJECTION_FAILED","Projection did not prepare resources."); prepared.Commit();
                    }
                    timings?.Projected();
                    workspace.Document = candidate; workspace.Preview = preview; committed = true;
                    if (undo) { workspace.UndoStack.RemoveAt(workspace.UndoStack.Count - 1); Push(workspace.RedoStack,before); }
                    else if (redo) { workspace.RedoStack.RemoveAt(workspace.RedoStack.Count - 1); Push(workspace.UndoStack,before); }
                    else { Push(workspace.UndoStack,before); workspace.RedoStack.Clear(); }
                    var result = new CommandResult { Success = true, Code = preview.IsComplete ? "OK" : "COMMITTED_INCOMPLETE", Message = preview.IsComplete ? "Committed." : "Committed; evaluation is incomplete.", DocumentRevision = revision, MeshContentHash = evaluated?.ContentHash, EvaluationComplete = preview.IsComplete, PreviewRevision = preview.OutputRevision };
                    workspace.Commands.Add(command.CommandId,new CachedCommand { Fingerprint = fingerprint, Result = result });
                    timings?.Committed();
                    return result;
                }
                catch (Exception e)
                {
                    if (!committed && prepared != null)
                    {
                        try { prepared.Rollback(); }
                        catch (Exception rollback) { return RememberFailure(command,fingerprint,"PROJECTION_RECOVERY_REQUIRED", "Projection rollback failed: " + rollback.Message); }
                    }
                    return RememberFailure(command,fingerprint,e is AuthoringException ? ((AuthoringException)e).Code : "COMMAND_FAILED",e.Message);
                }
                finally
                {
                    workspace.Executing = false;
                    // Cleanup failure cannot turn a committed command into an apparent retryable failure.
                    if (prepared != null) { try { prepared.Dispose(); } catch { } }
                    timings?.End();
                }
            }
        }
        private CommandResult Failure(string code, string message) { return new CommandResult { Success = false, Code = code, Message = message, DocumentRevision = workspace.Document.DocumentRevision, MeshContentHash = null }; }
        private CommandResult RememberFailure(CommandEnvelope command, string fingerprint, string code, string message)
        {
            var result = Failure(code,message);
            if (command != null && fingerprint != null && workspace.Commands.Count < AuthoringLimits.MaxCommands && !workspace.Commands.ContainsKey(command.CommandId)) workspace.Commands.Add(command.CommandId,new CachedCommand { Fingerprint = fingerprint, Result = result });
            return result;
        }
        private static void Push(List<AuthoringDocument> stack, AuthoringDocument doc) { if (stack.Count >= AuthoringLimits.MaxHistory) stack.RemoveAt(0); stack.Add(doc); }
        private static string Fingerprint(CommandEnvelope command)
        {
            Checks.Require(command.Operations != null && command.Operations.Length > 0 && command.Operations.Length <= AuthoringLimits.MaxOperations,"INVALID_COMMAND","Operation count is invalid.");
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream,Encoding.UTF8))
            {
                writer.Write(command.ExpectedInstanceId ?? ""); writer.Write(command.DocumentId ?? ""); writer.Write(command.ObjectId ?? ""); writer.Write(command.ExpectedDocumentRevision); writer.Write(command.ExpectedBaselineHash ?? ""); writer.Write(command.Operations.Length);
                foreach (var operation in command.Operations)
                {
                    Checks.Require(operation != null && operation.VertexIds.Count <= AuthoringLimits.MaxVertices,"INVALID_COMMAND","Missing or oversized operation.");
                    writer.Write(operation.Kind);
                    if (operation.Kind == "graph.replace") writer.Write(GraphContentIdentity.Hash(operation.Graph));
                    operation.WriteGraphFingerprint(writer);
                    if (operation.Kind == "object.add_mesh")
                    {
                        writer.Write(operation.NewObjectId); writer.Write(operation.Mesh.ContentHash);
                        writer.Write(Checks.Canonical(operation.Transform.Scale)); MeshBinary.Write(writer, operation.Transform.Translation);
                    }
                    writer.Write(operation.Enabled); MeshBinary.Write(writer,operation.Delta); writer.Write(operation.VertexIds.Count); foreach (int index in operation.VertexIds) writer.Write(index);
                }
                return Checks.Hash(stream.ToArray());
            }
        }
    }
}
