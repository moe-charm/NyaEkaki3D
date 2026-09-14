using System;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    static void RunMaterialBakeTests()
    {
        foreach(var mode in new[]{MaterialAlphaMode.Opaque,MaterialAlphaMode.Cutout,MaterialAlphaMode.Blend})
            Test("material Bake owns standard parameters and image for "+mode,()=>
            {
                var f=MaterialFixture();var parameters=new MaterialParameters(new Vec4(.2f,.4f,.6f,.8f),.7f,.3f,new Vec3(2,1,0),mode,.45f);
                var graph=f.graph.ReplaceNode(GraphNode.StandardMaterial(f.material,parameters));
                var workspace=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(workspace);
                Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph))));
                string before=workspace.Document.StateHash,directory=Dir("material-bake-"+mode);
                string path=MaterialBakeStore.Export(directory,workspace);var read=MaterialBakeStore.Read(path);
                Equal(before,workspace.Document.StateHash);Equal(parameters.ContentHash,read.Material.ContentHash);
                Equal(workspace.Preview.Output.Mesh.ContentHash,read.Geometry.Mesh.ContentHash);
                True(read.BaseColor.CopyRgba().SequenceEqual(workspace.Preview.Output.BaseColor.Image.CopyRgba()));
                Equal(workspace.Preview.Output.BaseColor.UvHash,read.UvHash);Equal(workspace.Preview.Output.BaseColor.MeshDomain,read.MeshDomain);
                var png=read.CopyPng();png[0]=0;Equal((byte)137,read.CopyPng()[0]);
                byte[] manifest=File.ReadAllBytes(path);Equal(path,MaterialBakeStore.Export(directory,workspace));True(manifest.SequenceEqual(File.ReadAllBytes(path)));
                Expect("INVALID_MANIFEST",()=>SurfaceBakeStore.Read(path));Expect("INVALID_MANIFEST",()=>BakeStore.Read(path));
                string native=Dir("material-bake-native-"+mode);ProjectStore.Save(native,workspace,0);
                var reopened=ProjectStore.Open(native);var repeat=MaterialBakeStore.Read(MaterialBakeStore.Export(Dir("material-bake-reopened-"+mode),reopened));
                Equal(read.Material.ContentHash,repeat.Material.ContentHash);Equal(read.PngHash,repeat.PngHash);
                File.WriteAllBytes(Path.Combine(directory,read.PngHash+".png"),new byte[]{0});
                Expect("HASH_MISMATCH",()=>MaterialBakeStore.Read(path));Expect("HASH_MISMATCH",()=>MaterialBakeStore.Export(directory,workspace));
                True(manifest.SequenceEqual(File.ReadAllBytes(path)));
            });
        Test("material Bake validates profile and canonical parameter payloads and supports no image",()=>
        {
            var f=MaterialFixture();var graph=f.graph.WithEdges(f.graph.Edges.Where(e=>e.ToNode!=f.material));
            var workspace=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(workspace);Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph))));
            string directory=Dir("material-bake-no-image"),path=MaterialBakeStore.Export(directory,workspace);var read=MaterialBakeStore.Read(path);
            True(read.BaseColor==null && read.CopyPng()==null && read.PngHash==null && read.UvHash==null);Equal(MaterialParameters.Default.ContentHash,read.Material.ContentHash);
            string text=File.ReadAllText(path);var manifest=JObject.Parse(text);manifest["profile"]="other-profile";File.WriteAllText(path,manifest.ToString());
            Expect("EXPORT_UNSUPPORTED_FEATURE",()=>MaterialBakeStore.Read(path));File.WriteAllText(path,text);
            manifest=JObject.Parse(text);manifest["schemaVersion"]=2;File.WriteAllText(path,manifest.ToString());Expect("UNSUPPORTED_FORMAT",()=>MaterialBakeStore.Read(path));File.WriteAllText(path,text);
            manifest=JObject.Parse(text);manifest.Remove("baseColor");File.WriteAllText(path,manifest.ToString());Expect("INVALID_MANIFEST",()=>MaterialBakeStore.Read(path));File.WriteAllText(path,text);
            var bytes=MaterialParametersCodec.Write(MaterialParameters.Default);Equal(44,bytes.Length);
            Expect("INVALID_BLOB",()=>MaterialParametersCodec.Read(bytes.Take(43).ToArray()));
            var invalid=(byte[])bytes.Clone();Array.Copy(BitConverter.GetBytes(99),0,invalid,36,4);Expect("INVALID_MATERIAL",()=>MaterialParametersCodec.Read(invalid));
            invalid=(byte[])bytes.Clone();Array.Copy(BitConverter.GetBytes(float.NaN),0,invalid,0,4);Expect("INVALID_MATERIAL",()=>MaterialParametersCodec.Read(invalid));
            invalid=(byte[])bytes.Clone();invalid[19]=128;Expect("INVALID_BLOB",()=>MaterialParametersCodec.Read(invalid)); // metallic negative zero
            string corruptHash=Checks.Hash(invalid);Storage.WriteBlob(directory,corruptHash,invalid);
            manifest=JObject.Parse(text);manifest["materialHash"]=corruptHash;File.WriteAllText(path,manifest.ToString());Expect("INVALID_BLOB",()=>MaterialBakeStore.Read(path));
        });
        Test("material Bake uses compact blob names when a Windows path is long", () =>
        {
            var f = MaterialFixture();
            var graph = f.graph.ReplaceNode(GraphNode.StandardMaterial(f.material, MaterialParameters.Default));
            var workspace = AuthoringWorkspace.CreateEmpty();
            var commands = new AuthoringCommandService(workspace);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph))));
            string directory = Path.Combine(Root, new string('p', 130), "long-material-export");
            string path = MaterialBakeStore.Export(directory, workspace);
            var blobFiles = Directory.GetFiles(Path.Combine(directory, "blobs"));
            True(blobFiles.Any(file => !Path.GetFileName(file).StartsWith(workspace.Preview.Output.Mesh.ContentHash, StringComparison.Ordinal)));
            True(Directory.GetFiles(directory, "*.png").Any(file => !Path.GetFileName(file).StartsWith(workspace.Preview.Output.BaseColor.ImageHash, StringComparison.Ordinal)));
            var read = MaterialBakeStore.Read(path);
            Equal(workspace.Preview.Output.Mesh.ContentHash, read.Geometry.Mesh.ContentHash);
            Equal(MaterialParameters.Default.ContentHash, read.Material.ContentHash);
        });
        Test("material Bake rejects unassigned or incomplete output without creating export directory",()=>
        {
            var workspace=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(workspace);var f=PaintFixture();
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.AddGraph(f.graph))));string path=Path.Combine(Dir("material-bake-rejected"),"missing");
            Expect("MATERIAL_REQUIRED",()=>MaterialBakeStore.Export(path,workspace));False(Directory.Exists(path));
            var material=MaterialFixture();Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.ReplaceGraph(material.graph))));
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.Disconnect(material.assign,"material"))));
            Expect("GRAPH_INCOMPLETE",()=>MaterialBakeStore.Export(path,workspace));False(Directory.Exists(path));
        });
    }
}
