using System;
using System.Collections.Generic;

namespace NyaForge.UnityBridge.Editor
{
    internal sealed class AssetUpdateTransaction : IDisposable
    {
        readonly UpdateJournal journal;
        readonly List<string> created=new List<string>();
        bool committed;
        internal IEnumerable<string> Created=>created;
        internal AssetUpdateTransaction(string folder,IEnumerable<string> paths,string stagingFolder=null) { journal=UpdateJournal.Create(folder,paths,stagingFolder); }
        internal void RegisterNew(string path) { journal.RegisterNew(path);created.Add(path); }
        internal void ConfirmNew(string path)=>journal.ConfirmNew(path);
        internal void Commit() { journal.Commit();committed=true; }
        public void Dispose()
        {
            journal.Dispose();
            if(!committed) UpdateJournal.Recover(journal.DirectoryPath,true);
        }
    }
}
