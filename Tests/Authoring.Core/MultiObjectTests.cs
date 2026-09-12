using System;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;

internal static partial class Program
{
    static void RunMultiObjectTests()
    {
        Test("graph project adds, selects, saves, and reopens multiple objects", () =>
        {
            string firstPlane, firstEdit;
            var first = PlaneGraph(out firstPlane, out firstEdit);
            var workspace = AuthoringWorkspace.CreateEmpty("multi graph");
            var commands = new AuthoringCommandService(workspace);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.AddGraph(first))));
            string firstId = workspace.Document.ObjectId;
            string secondId = Guid.NewGuid().ToString("D");
            string secondPlane, secondEdit;
            var second = PlaneGraph(out secondPlane, out secondEdit);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.AddGraph(second, secondId))));
            Equal(2, workspace.Document.Objects.Count); Equal(secondId, workspace.Document.ObjectId);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.SelectObject(firstId))));
            Equal(firstId, workspace.Document.ObjectId); Equal(first.GraphId, workspace.Document.ActiveObject.Graph.GraphId);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.SelectObject(secondId))));
            Equal(secondId, workspace.Document.ObjectId); Equal(second.GraphId, workspace.Document.ActiveObject.Graph.GraphId);
            string directory = Dir("multi-graph"); ProjectStore.Save(directory, workspace, 0);
            var reopened = ProjectStore.Open(directory);
            Equal(2, reopened.Document.Objects.Count); Equal(secondId, reopened.Document.ObjectId);
            Equal(workspace.Document.StateHash, reopened.Document.StateHash);
            Equal(workspace.Evaluate().ContentHash, reopened.Evaluate().ContentHash);
        });
        Test("static project adds, selects, saves, and reopens multiple objects", () =>
        {
            var workspace = AuthoringWorkspace.CreateEmpty("multi static");
            var commands = new AuthoringCommandService(workspace);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.AddMesh(AuthoringFixtures.Panel(1), new RestTransform(1, new Vec3())))));
            string firstId = workspace.Document.ObjectId;
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.AddMesh(AuthoringFixtures.Panel(2), new RestTransform(2, new Vec3(.1f, 0, 0))))));
            string secondId = workspace.Document.ObjectId;
            Equal(2, workspace.Document.Objects.Count); Equal(secondId, workspace.Document.ObjectId); Near(.1f, workspace.Document.Transform.Translation.X);
            string directory = Dir("multi-static"); ProjectStore.Save(directory, workspace, 0);
            var reopened = ProjectStore.Open(directory);
            Equal(2, reopened.Document.Objects.Count); Equal(secondId, reopened.Document.ObjectId);
            Equal(workspace.Document.StateHash, reopened.Document.StateHash);
            Equal(workspace.Evaluate().ContentHash, reopened.Evaluate().ContentHash);
            Near(.1f, reopened.Document.Transform.Translation.X);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.SelectObject(firstId))));
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.TranslateVertices(new[] { 0 }, new Vec3(.01f, 0, 0)))));
            Equal(2, workspace.Document.Objects.Count);
            Equal(firstId, workspace.Document.ObjectId);
            Equal(secondId, workspace.Document.Objects.Single(item => item.ObjectId != firstId).ObjectId);
        });
    }
}
