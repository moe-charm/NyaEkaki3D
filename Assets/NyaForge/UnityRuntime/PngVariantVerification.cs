using System;
using System.IO;
using NyaForge.Authoring;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    internal static class PngVariantVerification
    {
        [Serializable] sealed class Manifest { public Entry[] entries; }
        [Serializable] sealed class Entry { public string name,rgbaBottomLeft;public int width,height; }
        public static void Verify(string output)
        {
            string directory=Path.Combine(Application.streamingAssetsPath,"NyaForgeVerification","PngVariants");
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Path.Combine(directory,"manifest.json")));
            if(manifest?.entries==null || manifest.entries.Length!=30) throw new InvalidOperationException("PNG variant fixture set is missing.");
            using(var report=new StreamWriter(Path.Combine(output,"png-variants.txt")))
            {
                foreach(var entry in manifest.entries)
                {
                    var image=PaintPngImporter.Read(Path.Combine(directory,entry.name+".png"));
                    var expected=Convert.FromBase64String(entry.rgbaBottomLeft);var actual=image.CopyRgba();
                    if(image.Width!=entry.width || image.Height!=entry.height || actual.Length!=expected.Length)
                        throw new InvalidOperationException(entry.name+": decoded dimensions differ");
                    for(int i=0;i<actual.Length;i++) if(actual[i]!=expected[i])
                        throw new InvalidOperationException(entry.name+": RGBA byte "+i+" expected "+expected[i]+", got "+actual[i]);
                    report.WriteLine("PASS "+entry.name+" exact RGBA, alpha and bottom-left row order");report.Flush();
                }
            }
        }
    }
}
