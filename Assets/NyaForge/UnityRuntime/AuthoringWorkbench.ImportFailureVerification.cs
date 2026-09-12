using System;
using System.Collections.Generic;
using System.IO;
using NyaForge.Authoring;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void VerifyImportFailureIsolation(string output, List<string> checks)
        {
            string path = Path.Combine(output, "invalid-skin.vrm");
            File.WriteAllBytes(path, VrmVerificationFixture.Create(false, true));
            ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
            VerifyImportUnchanged(() => ImportModel(path));
            string good = Path.Combine(output, "retry-skin.vrm");
            File.WriteAllBytes(good, VrmVerificationFixture.Create(true));
            ImportModel(good);
            Execute(AuthoringOperation.Undo());
            Check(workspace.Document.IsEmpty, "Import did not have one graph Undo entry");
            VerifyImportUnchanged(() => ImportModel(path));
            ImportModel(good);
            Check(!workspace.Document.IsEmpty && importedRigSession != null, "Valid retry failed after rejected import");
            checks.Add("Import failure isolation: invalid skin preserves graph, attachments, sessions, labels, dirty and Undo/Redo; valid retry succeeds");
        }

        void VerifyImportUnchanged(Action action)
        {
            var document = workspace.Document;
            var attachments = workspace.Attachments;
            var rig = importedRigSession; var expressions = importedVrmSession; var springs = importedVrmSpringSession;
            string label = vrmSpringStatus.text, rigLabel = importedRigStatus.text;
            bool dirty = workspace.IsDirty, undo = workspace.CanUndo, redo = workspace.CanRedo;
            bool rejected = false;
            try { action(); }
            catch (AuthoringException) { rejected = true; }
            catch (InvalidOperationException) { rejected = true; }
            Check(rejected, "Invalid import was not rejected");
            Check(ReferenceEquals(document, workspace.Document) && ReferenceEquals(attachments, workspace.Attachments), "Rejected import changed document or attachments");
            Check(ReferenceEquals(rig, importedRigSession) && ReferenceEquals(expressions, importedVrmSession) && ReferenceEquals(springs, importedVrmSpringSession), "Rejected import changed session fields");
            Check(label == vrmSpringStatus.text && rigLabel == importedRigStatus.text, "Rejected import changed metadata labels");
            Check(dirty == workspace.IsDirty && undo == workspace.CanUndo && redo == workspace.CanRedo, "Rejected import changed dirty or history availability");
        }
    }
}
