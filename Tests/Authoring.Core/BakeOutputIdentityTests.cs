using System.IO;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;

internal static partial class Program
{
    static void RunBakeOutputIdentityTests()
    {
        Test("Bake output identity survives edits and native roundtrip and binds exact manifest",()=>
        {
            var f=SlotFixture();var workspace=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(workspace);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.AddGraph(f.graph))));
            string path=MultiMaterialBakeStore.Export(Dir("identity-first"),workspace);
            var first=BakeOutputIdentity.Read(path,MultiMaterialBakeStore.Read(path).Geometry);
            Equal(f.output,first.OutputId);Equal("graph-output",first.OutputKind);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.UpdateNode(GraphNode.StandardMaterial(f.a,new MaterialParameters(new Vec4(.2f,.3f,.4f,1),0,.8f,new Vec3()))))));
            string native=Dir("identity-native");ProjectStore.Save(native,workspace,0);workspace=ProjectStore.Open(native);
            string nextPath=MultiMaterialBakeStore.Export(Dir("identity-next"),workspace);var nextMesh=MultiMaterialBakeStore.Read(nextPath).Geometry;
            var next=BakeOutputIdentity.Read(nextPath,nextMesh);
            Equal(first.OutputId,next.OutputId);Equal(first.DocumentId,next.DocumentId);True(first.ManifestHash!=next.ManifestHash);True(next.Revision>first.Revision);
            string sidecar=nextPath+BakeOutputIdentity.Suffix,text=File.ReadAllText(sidecar);
            File.Copy(path+BakeOutputIdentity.Suffix,sidecar,true);Expect("OUTPUT_IDENTITY_MISMATCH",()=>BakeOutputIdentity.Read(nextPath,nextMesh));
            File.WriteAllText(sidecar,text);File.AppendAllText(nextPath," ");Expect("HASH_MISMATCH",()=>BakeOutputIdentity.Read(nextPath,nextMesh));
            File.Delete(sidecar);True(BakeOutputIdentity.Read(nextPath,nextMesh)==null);
        });
        Test("all Bake profiles publish output identity while legacy geometry reader remains supported",()=>
        {
            var workspace=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(workspace);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.AddMesh(AuthoringFixtures.Panel(1),new RestTransform(1,new Vec3())))));
            string path=BakeStore.Export(Dir("identity-static"),workspace);var mesh=BakeStore.Read(path);var identity=BakeOutputIdentity.Read(path,mesh);
            Equal(mesh.ObjectId,identity.OutputId);Equal("static-object",identity.OutputKind);
            string text=File.ReadAllText(path+BakeOutputIdentity.Suffix);var bad=JObject.Parse(text);bad["outputKind"]="guessed";File.WriteAllText(path+BakeOutputIdentity.Suffix,bad.ToString());
            Expect("INVALID_MANIFEST",()=>BakeOutputIdentity.Read(path,mesh));
            var surface=PaintFixture();workspace=AuthoringWorkspace.CreateEmpty();commands=new AuthoringCommandService(workspace);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.AddGraph(surface.graph))));
            path=SurfaceBakeStore.Export(Dir("identity-surface"),workspace);True(BakeOutputIdentity.Read(path,SurfaceBakeStore.Read(path).Geometry)!=null);
            var material=MaterialFixture();Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.ReplaceGraph(material.graph))));
            path=MaterialBakeStore.Export(Dir("identity-material"),workspace);True(BakeOutputIdentity.Read(path,MaterialBakeStore.Read(path).Geometry)!=null);
        });
    }
}
