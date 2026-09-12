using System.IO;
using NyaForge.Authoring;
internal static partial class Program
{
    static void RunProjectExportServiceTests()
    {
        Test("observed export routes mesh and material without changing document",()=>
        {
            var w=AuthoringWorkspace.CreateFixture();string before=w.Document.StateHash;long revision=w.Document.DocumentRevision;bool dirty=w.IsDirty;
            var result=ProjectExportService.Export(w,w.InstanceId,w.Document.DocumentId,revision,Dir("routed-mesh"));Equal(ProjectExportKind.Mesh,result.Kind);Equal(w.Evaluate().ContentHash,BakeStore.Read(result.ManifestPath).MeshContentHash);
            Equal(before,w.Document.StateHash);Equal(revision,w.Document.DocumentRevision);Equal(dirty,w.IsDirty);
            var f=MaterialFixture();var material=AuthoringWorkspace.CreateEmpty();Ok(Execute(material,AuthoringOperation.AddGraph(f.graph)));
            var exported=ProjectExportService.Export(material,material.InstanceId,material.Document.DocumentId,material.Document.DocumentRevision,Dir("routed-material"));Equal(ProjectExportKind.Material,exported.Kind);True(File.Exists(exported.ManifestPath));MaterialBakeStore.Read(exported.ManifestPath);
            string stale=Path.Combine(Root,"stale-export-no-write");
            Expect("REVISION_CONFLICT",()=>ProjectExportService.Export(w,w.InstanceId,w.Document.DocumentId,revision+1,stale));True(!Directory.Exists(stale));
        });
    }
}
