using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using UnityEditor;
using UnityEngine;

namespace NyaForge.UnityBridge.Editor
{
    public sealed class MaterialUpdatePlan
    {
        public sealed class Entry
        {
            public string Path { get; }
            public string Action { get; }
            internal Entry(string path,string action) { Path=path;Action=action; }
        }
        public sealed class Binding
        {
            public string MaterialPath { get; }
            public string TexturePath { get; }
            internal Binding(string material,string texture) { MaterialPath=material;TexturePath=texture; }
        }
        public IReadOnlyList<Entry> Entries { get; }
        public IReadOnlyList<Binding> Submeshes { get; }
        public string ComparisonKey { get; }
        MaterialUpdatePlan(List<Entry> entries,List<Binding> bindings)
        {
            Entries=entries.AsReadOnly();Submeshes=bindings.AsReadOnly();
            ComparisonKey=string.Join("\n",entries.Select(e=>e.Action+":"+e.Path).Concat(bindings.Select(b=>b.MaterialPath+":"+b.TexturePath)));
        }
        internal static MaterialUpdatePlan Build(MultiMaterialBakeDocument source,string folder)
        {
            var managed=new HashSet<string>(ImportOwnership.ManagedPaths(folder),StringComparer.Ordinal);
            var entries=new Dictionary<string,Entry>(StringComparer.Ordinal);var bindings=new List<Binding>();
            var materials=new Dictionary<string,Binding>();var images=new Dictionary<string,string>();var claimed=new HashSet<string>();
            void Plan(string path)
            {
                if(entries.ContainsKey(path)) return;
                if(!managed.Contains(path) && (File.Exists(path) || File.Exists(path+".meta"))) throw new IOException("Update path is not owned: "+path);
                entries.Add(path,new Entry(path,managed.Contains(path) ? "更新" : "追加"));
            }
            foreach(string name in new[]{"Mesh.asset","StaticMesh.prefab","MaterialSlots.json"})
            {
                if(!managed.Contains(folder+"/"+name)) throw new InvalidOperationException("This import has no multi-material resource layout.");
                Plan(folder+"/"+name);
            }
            foreach(int slotKey in source.SubmeshSlots)
            {
                var slot=source.Slots.Single(s=>s.Slot==slotKey);
                if(materials.TryGetValue(slot.MaterialNodeId,out var shared)) { bindings.Add(shared);continue; }
                string materialPath=folder+"/Material-"+slot.MaterialNodeId+".mat",texturePath=null;
                Plan(materialPath);
                if(slot.Surface.BaseColor!=null && !images.TryGetValue(slot.Surface.PngHash,out texturePath))
                {
                    var material=managed.Contains(materialPath) ? AssetDatabase.LoadAssetAtPath<Material>(materialPath) : null;
                    string previous=material==null ? "" : AssetDatabase.GetAssetPath(material.mainTexture);
                    if(managed.Contains(previous) && claimed.Add(previous)) texturePath=previous;
                    else texturePath=AssetDatabase.GenerateUniqueAssetPath(folder+"/Texture-"+slot.MaterialNodeId+".png");
                    Plan(texturePath);images.Add(slot.Surface.PngHash,texturePath);
                }
                var binding=new Binding(materialPath,texturePath);materials.Add(slot.MaterialNodeId,binding);bindings.Add(binding);
            }
            foreach(string path in managed.Where(p=>!entries.ContainsKey(p)).OrderBy(p=>p,StringComparer.Ordinal)) entries.Add(path,new Entry(path,"保持"));
            return new MaterialUpdatePlan(entries.Values.OrderBy(e=>e.Path,StringComparer.Ordinal).ToList(),bindings);
        }
    }
}
