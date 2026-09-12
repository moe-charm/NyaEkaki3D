using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;

internal static partial class Program
{
    static void RunMultiMaterialBakeTests()
    {
        Test("multi-material Bake preserves sparse slots, independent images and native roundtrip",()=>
        {
            var f=SlotFixture();var graph=MaterialSlotPaintEditing.AddBlank(f.graph,3,GraphId(),GraphId());
            var workspace=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(workspace);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph))));
            string state=workspace.Document.StateHash,directory=Dir("multi-material-bake");
            string path=MultiMaterialBakeStore.Export(directory,workspace);var read=MultiMaterialBakeStore.Read(path);
            Equal(state,workspace.Document.StateHash);True(read.SubmeshSlots.SequenceEqual(new[]{3,9}));
            Equal(workspace.Preview.Output.Mesh.ContentHash,read.Geometry.MeshContentHash);
            foreach(var slot in read.Slots)
            {
                var expected=workspace.Preview.Output.SlotMaterials[slot.Slot];
                Equal(expected.MaterialNodeId,slot.MaterialNodeId);Equal(expected.Material.Parameters.ContentHash,slot.Surface.Material.ContentHash);
            }
            True(read.Slots[0].Surface.BaseColor!=null);True(read.Slots[1].Surface.BaseColor==null);
            var png=read.Slots[0].Surface.CopyPng();png[0]=0;Equal((byte)137,read.Slots[0].Surface.CopyPng()[0]);
            string text=File.ReadAllText(path);MultiMaterialBakeStore.Export(directory,workspace);Equal(text,File.ReadAllText(path));
            string native=Dir("multi-material-bake-native");ProjectStore.Save(native,workspace,0);
            var repeat=MultiMaterialBakeStore.Read(MultiMaterialBakeStore.Export(Dir("multi-material-bake-repeat"),ProjectStore.Open(native)));
            Equal(read.Slots[0].Surface.PngHash,repeat.Slots[0].Surface.PngHash);
            foreach(var mutation in new System.Action<JObject>[] {
                j=>j["submeshSlots"]=new JArray(3,3), j=>j["submeshSlots"]=new JArray(3,8),
                j=>j["submeshSlots"]=new JArray(3),j=>j["slots"][1]["slot"]=3,
                j=>j["submeshSlots"]=new JArray("3",9),j=>j["submeshSlots"]=new JArray(3.0,9),
                j=>j["slots"][0]["slot"]=-1,j=>j["slots"][0]=JValue.CreateNull(),
                j=>((JObject)j["slots"][0]).Remove("baseColor"),
                j=>j["slots"][1]["materialNodeId"]=j["slots"][0]["materialNodeId"].DeepClone() })
            {
                var json=JObject.Parse(text);mutation(json);File.WriteAllText(path,json.ToString());
                Expect("INVALID_MANIFEST",()=>MultiMaterialBakeStore.Read(path));
            }
            File.WriteAllText(path,text);
            File.WriteAllBytes(Path.Combine(directory,read.Slots[0].Surface.PngHash+".png"),new byte[]{0});
            Expect("HASH_MISMATCH",()=>MultiMaterialBakeStore.Read(path));
            Expect("HASH_MISMATCH",()=>MultiMaterialBakeStore.Export(directory,workspace));Equal(text,File.ReadAllText(path));
        });
        Test("multi-material Bake preserves shared material UUID and rejects legacy assignment",()=>
        {
            var f=MaterialFixture();var workspace=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(workspace);
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.AddGraph(f.graph))));
            string missing=Path.Combine(Dir("multi-material-required"),"missing");
            Expect("MATERIAL_REQUIRED",()=>MultiMaterialBakeStore.Export(missing,workspace));False(Directory.Exists(missing));
            Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.ReplaceGraph(MaterialSlotEditing.ConvertOutput(f.graph)))));
            var read=MultiMaterialBakeStore.Read(MultiMaterialBakeStore.Export(Dir("multi-material-shared"),workspace));
            True(read.Slots.All(s=>s.MaterialNodeId==f.material));
            True(read.Slots.All(s=>s.Surface.PngHash==read.Slots[0].Surface.PngHash));
        });
    }
}
