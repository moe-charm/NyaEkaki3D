using System;
using System.IO;
using NyaForge.Authoring;
internal static partial class Program
{
    static void RunProjectSaveServiceTests()
    {
        Test("observed revision save rejects stale targets before writing and roundtrips",()=>
        {
            var w=AuthoringWorkspace.CreateFixture();string dir=Dir("save-observed-revision");
            var request=new ProjectSaveRequest(w.InstanceId,w.Document.DocumentId,w.Document.DocumentRevision,dir,0);
            Ok(Execute(w,AuthoringOperation.TranslateVertices(new[]{0,4},new Vec3(.01f,0,0))));
            Expect("REVISION_CONFLICT",()=>ProjectSaveService.Save(w,request));True(!File.Exists(Path.Combine(dir,ProjectStore.ManifestName)));
            request=new ProjectSaveRequest(w.InstanceId,w.Document.DocumentId,w.Document.DocumentRevision,dir,0);
            var saved=ProjectSaveService.Save(w,request);Equal(w.Document.StateHash,saved.StateHash);Equal(1L,saved.SaveVersion);True(!w.IsDirty);
            Equal(saved.StateHash,ProjectStore.Open(dir).Document.StateHash);
            var bytes=File.ReadAllBytes(Path.Combine(dir,ProjectStore.ManifestName));
            Expect("SAVE_CONFLICT",()=>ProjectSaveService.Save(w,request));Equal(Convert.ToBase64String(bytes),Convert.ToBase64String(File.ReadAllBytes(Path.Combine(dir,ProjectStore.ManifestName))));
            Expect("STALE_INSTANCE",()=>ProjectSaveService.Save(w,new ProjectSaveRequest(Guid.NewGuid().ToString("D"),w.Document.DocumentId,w.Document.DocumentRevision,dir,1)));
            Expect("DOCUMENT_CHANGED",()=>ProjectSaveService.Save(w,new ProjectSaveRequest(w.InstanceId,Guid.NewGuid().ToString("D"),w.Document.DocumentRevision,dir,1)));
            Equal(1L,w.SaveVersion);
        });
    }
}
