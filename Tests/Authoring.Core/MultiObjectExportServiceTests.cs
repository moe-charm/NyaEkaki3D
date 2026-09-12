using System.IO;
using System.Linq;
using NyaForge.Authoring;

internal static partial class Program
{
    static void RunMultiObjectExportServiceTests()
    {
        Test("multi-object export packages independently readable object manifests", () =>
        {
            var workspace = AuthoringWorkspace.CreateEmpty("multi export");
            string firstPlane, firstEdit; var first = PlaneGraph(out firstPlane, out firstEdit);
            Ok(Execute(workspace, AuthoringOperation.AddGraph(first)));
            string secondPlane, secondEdit; var second = PlaneGraph(out secondPlane, out secondEdit);
            Ok(Execute(workspace, AuthoringOperation.AddGraph(second)));
            string before = workspace.Document.StateHash; long revision = workspace.Document.DocumentRevision;
            string directory = Path.Combine(Root, "multi-export-output-" + System.Guid.NewGuid().ToString("N"));
            var result = MultiObjectExportService.Export(workspace, workspace.InstanceId, workspace.Document.DocumentId, revision, directory);
            True(File.Exists(result.ManifestPath));
            var package = MultiObjectExportService.Read(result.ManifestPath);
            Equal(2, package.Objects.Count); Equal(workspace.Document.DocumentId, package.DocumentId); Equal(before, package.StateHash);
            True(package.Objects.All(item => File.Exists(item.ManifestPath)));
            True(package.Objects.All(item => BakeStore.Read(item.ManifestPath).ObjectId == item.ObjectId));
            Equal(before, workspace.Document.StateHash); Equal(revision, workspace.Document.DocumentRevision);
            Expect("EXPORT_DESTINATION_EXISTS", () => MultiObjectExportService.Export(workspace, workspace.InstanceId, workspace.Document.DocumentId, revision, directory));
        });
    }
}
