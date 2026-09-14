using System;
using System.Collections.Generic;

namespace NyaForge.UnityRuntime
{
    /// <summary>
    /// Shared selection state for the active editing domain. The collections
    /// remain exposed during this migration so existing panel and verification
    /// code keeps one source of truth while callers move to explicit methods.
    /// </summary>
    public sealed class SelectionContext
    {
        public HashSet<int> VertexIndices { get; } = new HashSet<int>();
        public HashSet<ulong> FaceIds { get; } = new HashSet<ulong>();
        public string ObjectId { get; private set; } = "";
        public string EditNodeId { get; private set; } = "";
        public long Revision { get; private set; }
        public event Action Changed;

        public void SetObject(string objectId)
        {
            ObjectId = objectId ?? "";
        }

        public void SetEditNode(string nodeId)
        {
            EditNodeId = nodeId ?? "";
        }

        public void ClearGeometry()
        {
            VertexIndices.Clear();
            FaceIds.Clear();
        }

        public void NotifyChanged()
        {
            Revision = checked(Revision + 1);
            Changed?.Invoke();
        }

        public void Reset(string objectId = "", string editNodeId = "")
        {
            ClearGeometry();
            SetObject(objectId);
            SetEditNode(editNodeId);
            NotifyChanged();
        }
    }
}
