using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using UnityEditor;

namespace NyaForge.UnityBridge.Editor
{
    public static partial class BridgeBatch
    {
        static void VerifyGeometryUpdates(List<string> checks,List<string> folders)
        {
            string directory=Path.Combine(Path.GetDirectoryName(RequiredArgument(Environment.GetCommandLineArgs(),"--nyaforge-report")),"geometry-update-bake");
            string sourceId=Guid.NewGuid().ToString("D"),assignId=Guid.NewGuid().ToString("D"),outputId=Guid.NewGuid().ToString("D");
            var materialIds=Enumerable.Range(0,3).Select(_=>Guid.NewGuid().ToString("D")).ToArray();
            var nodes=new List<GraphNode> { GraphNode.Source(sourceId,UpdateGeometry(1),new RestTransform(1,new Vec3())),
                GraphNode.AssignMaterials(assignId,new[]{0,1,2}),GraphNode.Output(outputId) };
            var edges=new List<GraphEdge> { new GraphEdge(sourceId,"mesh",assignId,"mesh"),new GraphEdge(assignId,"mesh",outputId,"mesh") };
            for(int i=0;i<3;i++)
            {
                nodes.Add(GraphNode.StandardMaterial(materialIds[i]));
                edges.Add(new GraphEdge(materialIds[i],"material",assignId,GraphNode.MaterialSlotPort(i)));
            }
            var graph=new AuthoringGraph(Guid.NewGuid().ToString("D"),nodes,edges,outputId);
            var workspace=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(workspace);
            commands.Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph)));
            string manifest=MultiMaterialBakeStore.Export(directory,workspace);
            var imported=BakeImporter.ImportMaterials(manifest);folders.Add(imported.AssetDirectory);
            string meshGuid=AssetDatabase.AssetPathToGUID(imported.MeshPath),prefabGuid=AssetDatabase.AssetPathToGUID(imported.PrefabPath);
            string firstMaterialGuid=AssetDatabase.AssetPathToGUID(imported.MaterialPaths[0]);
            string retiredPath=null,retiredGuid=null;
            foreach(int count in new[]{3,2})
            {
                commands.Execute(workspace.NewCommand(AuthoringOperation.UpdateNode(GraphNode.Source(sourceId,UpdateGeometry(count),new RestTransform(1,new Vec3())))));
                manifest=MultiMaterialBakeStore.Export(directory,workspace);
                var preview=BakeUpdatePreview.Materials(manifest,imported.AssetDirectory);
                Require(preview.Conflicts.Count==0 && preview.Plan.Submeshes.Count==count,"Geometry update plan has wrong slot count");
                Require(preview.Plan.Entries.Count(e=>e.Action=="追加")== (count==3 ? 2 : 0),"Geometry update plans unexpected assets");
                var updated=BakeImporter.UpdateMaterials(manifest,imported.AssetDirectory);
                VerifyAssets(updated,MultiMaterialBakeStore.Read(manifest).Geometry,checks,"geometry-update-"+count,true);
                Require(AssetDatabase.AssetPathToGUID(updated.MeshPath)==meshGuid && AssetDatabase.AssetPathToGUID(updated.PrefabPath)==prefabGuid,"Geometry update replaced mesh/prefab GUIDs");
                Require(AssetDatabase.AssetPathToGUID(updated.MaterialPaths[0])==firstMaterialGuid,"Geometry update replaced original material GUID");
                Require(updated.MaterialPaths.SequenceEqual(preview.Plan.Submeshes.Select(b=>b.MaterialPath)),"Geometry update material order differs from plan");
                Require(ImportOwnership.Inspect(updated.AssetDirectory).IsUnchanged,"Geometry update receipt differs");
                if(count==3) { retiredPath=updated.MaterialPaths[2];retiredGuid=AssetDatabase.AssetPathToGUID(retiredPath); }
                else Require(preview.Plan.Entries.Any(e=>e.Path==retiredPath && e.Action=="保持") && AssetDatabase.AssetPathToGUID(retiredPath)==retiredGuid,"Removed submesh lost its prior material asset");
            }
        }

        static MeshData UpdateGeometry(int count)
        {
            // Independent triangles change positions, vertex/index counts and submesh count together.
            var positions=new List<Vec3>();var uv=new List<Vec2>();var submeshes=new List<int[]>();
            for(int i=0;i<count;i++)
            {
                float x=i*.3f,y=count*.02f;
                positions.AddRange(new[]{new Vec3(x,y,0),new Vec3(x,y+.2f,0),new Vec3(x+.2f,y,0)});
                uv.AddRange(new[]{new Vec2(0,0),new Vec2(0,1),new Vec2(1,0)});
                submeshes.Add(new[]{i*3,i*3+1,i*3+2});
            }
            return new MeshData(positions.ToArray(),Array.Empty<Vec3>(),Array.Empty<Vec4>(),uv.ToArray(),submeshes.ToArray());
        }
    }
}
