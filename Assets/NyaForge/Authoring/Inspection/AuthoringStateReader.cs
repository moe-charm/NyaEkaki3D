using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace NyaForge.Authoring.Inspection
{
    /// <summary>Atomic, detached summary shared by local inspection and protocol adapters.</summary>
    public static class AuthoringStateReader
    {
        public static JObject Read(AuthoringWorkspace workspace,string expectedInstanceId)
        {
            if(workspace==null) throw new ArgumentNullException(nameof(workspace));
            lock(workspace.Gate)
            {
                Checks.Require(expectedInstanceId==workspace.InstanceId,"STALE_INSTANCE","Select the current authoring instance before reading state.");
                Checks.Require(!workspace.Executing,"REENTRANT_STATE","Cannot read state during a command transaction.");
                var doc=workspace.Document;var preview=workspace.Preview;
                return new JObject
                {
                    ["instanceId"]=workspace.InstanceId,["documentId"]=doc.DocumentId,
                    ["revision"]=doc.DocumentRevision,["stateHash"]=doc.StateHash,["attachmentsHash"]=workspace.Attachments.ContentHash,["name"]=doc.Name,["editSourceHash"]=doc.EditSourceHash,
                    ["dirty"]=workspace.IsDirty,["saveVersion"]=workspace.SaveVersion,
                    ["sourceEpoch"]=JValue.CreateNull(),["canUndo"]=workspace.CanUndo,["canRedo"]=workspace.CanRedo,
                    ["evaluation"]=new JObject { ["complete"]=preview.IsComplete,["stale"]=preview.IsStale },
                    ["objects"]=new JArray(doc.Objects.Select(o=>new JObject
                    {
                        ["objectId"]=o.ObjectId,["graphId"]=o.Graph.GraphId,["nodeCount"]=o.Graph.Nodes.Count
                    }))
                };
            }
        }
    }
}
