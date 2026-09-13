using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyExternalMcp(string probe,string output,Action<string> completed,bool create=false,bool secondary=false,bool glbExport=false)
        {
            Process process=null;string failure=null;
            var previousWorkspace=workspace;string previousPath=savedDirectory;
            bool previousAutomaticTick=springAutomaticTick;
            try
            {
                try
                {
                    probe=Path.GetFullPath(probe);
                    if(!File.Exists(probe) || probe.Contains("\"") || probe.Contains("\n") || probe.Contains("\r")) throw new ArgumentException("Invalid MCP probe path");
                    if(secondary)
                    {
                        var source=Path.Combine(output,"secondary-motion.vrm");
                        File.WriteAllBytes(source,VrmVerificationFixture.Create(false,playback:true));
                        ReplaceWorkspace(NyaForge.Authoring.AuthoringWorkspace.CreateEmpty(),null);
                        ImportModel(source);
                        springAutomaticTick=false;
                    }
                    else ReplaceWorkspace(create ? NyaForge.Authoring.AuthoringWorkspace.CreateEmpty() : NyaForge.Authoring.AuthoringWorkspace.CreateFixture(),null);
                    projectPath.SetValueWithoutNotify(Path.Combine(output,create ? "mcp-created-native" : "mcp-static-native"));
                    if((secondary || glbExport) && !TrySaveProject()) throw new InvalidOperationException("MCP fixture could not be saved before the probe");
                    if(create)
                    {
                        Directory.CreateDirectory(projectPath.value);
                        var image=new NyaForge.Authoring.Paint.PaintImage(8,4,new NyaForge.Authoring.Paint.Rgba32(0,0,255));
                        File.WriteAllBytes(Path.Combine(projectPath.value,"import-fixture.png"),NyaForge.Authoring.Paint.PaintPng.Encode(image));
                    }
                    pipeInstance=workspace.InstanceId;authoringPipe=new Platform.AuthoringPipeServer(pipeInstance);
                    process=Process.Start(new ProcessStartInfo
                    {
                        FileName="dotnet",Arguments="\""+probe+"\" "+(secondary ? "--player-secondary " : (glbExport ? "--player-glb-export " : (create ? "--player-create " : "--player-state ")))+pipeInstance+" "+workspace.Document.DocumentId+" "+workspace.Document.DocumentRevision+" "+workspace.Document.StateHash,
                        UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true
                    });
                }
                catch(Exception e) { failure=e.ToString(); }
                if(failure==null)
                {
                    var stdout=process.StandardOutput.ReadToEndAsync();var stderr=process.StandardError.ReadToEndAsync();
                    var watch=Stopwatch.StartNew();
                    while(!process.HasExited && watch.Elapsed.TotalSeconds<35) yield return null;
                    if(!process.HasExited) { process.Kill();failure="External MCP probe timed out"; }
                    else
                    {
                        while(!stdout.IsCompleted || !stderr.IsCompleted) yield return null;
                        File.WriteAllText(Path.Combine(output,create ? "mcp-create.log" : "mcp-external.log"),stdout.Result+stderr.Result);
                        if(process.ExitCode!=0) failure="External MCP probe failed: "+stderr.Result;
                        else
                        {
                            var reopened=NyaForge.Authoring.ProjectStore.Open(projectPath.value);
                            if(reopened.Document.StateHash!=workspace.Document.StateHash || workspace.IsDirty || savedDirectory!=McpSaveDirectory()) failure="MCP native save/reopen or GUI saved-directory differs";
                            if(glbExport)
                            {
                                var glbs=Directory.GetFiles(Path.Combine(projectPath.value,"exports"),"model.glb",SearchOption.AllDirectories);
                                var reports=Directory.GetFiles(Path.Combine(projectPath.value,"exports"),"export-report.json",SearchOption.AllDirectories);
                                string glbHash=glbs.Length==1 ? NyaForge.Authoring.Checks.Hash(File.ReadAllBytes(glbs[0])) : "";
                                if(glbs.Length!=1 || BitConverter.ToUInt32(File.ReadAllBytes(glbs[0]),0)!=0x46546c67 || reports.Length!=1 || !File.ReadAllText(reports[0]).Contains(workspace.Document.StateHash,StringComparison.Ordinal) || !File.ReadAllText(reports[0]).Contains(glbHash,StringComparison.Ordinal)) failure="MCP GLB export or pinned report is missing or invalid";
                            }
                            else if(!create && !secondary)
                            {
                                var manifests=Directory.GetFiles(Path.Combine(projectPath.value,"exports"),NyaForge.Authoring.BakeStore.ManifestName,SearchOption.AllDirectories);
                                if(manifests.Length!=1 || NyaForge.Authoring.BakeStore.Read(manifests[0]).MeshContentHash!=workspace.Evaluate().ContentHash) failure="MCP export readback differs from current mesh";
                            }
                            else if(create)
                            {
                                var surfaces=Directory.GetFiles(Path.Combine(projectPath.value,"exports"),NyaForge.Authoring.SurfaceBakeStore.ManifestName,SearchOption.AllDirectories);
                                if(surfaces.Length!=1) failure="MCP painted surface export missing";
                                else
                                {
                                    var surface=NyaForge.Authoring.SurfaceBakeStore.Read(surfaces[0]);
                                    var center=surface.BaseColor.GetPixel(32,32);var background=surface.BaseColor.GetPixel(0,0);
                                    if(surface.BaseColor.Width!=64 || surface.BaseColor.Height!=64 || center.R!=255 || center.G!=0 || center.B!=0 || background.R!=255 || background.G!=255 || background.B!=255) failure="MCP painted surface pixels differ";
                                }
                                var manifests=Directory.GetFiles(Path.Combine(projectPath.value,"exports"),NyaForge.Authoring.MaterialBakeStore.ManifestName,SearchOption.AllDirectories);
                                if(manifests.Length!=1) failure="MCP material export missing";
                                else
                                {
                                    var material=NyaForge.Authoring.MaterialBakeStore.Read(manifests[0]).Material;
                                    if(material.BaseColor.X!=.8f || material.BaseColor.Y!=.15f || material.BaseColor.Z!=.3f || material.Metallic!=.25f || material.Roughness!=.6f) failure="MCP material properties lost in export";
                                }
                            }
                        }
                    }
                }
            }
            finally { StopMcp();process?.Dispose();springAutomaticTick=previousAutomaticTick;ReplaceWorkspace(previousWorkspace,previousPath); }
            completed(failure);
        }
    }
}
