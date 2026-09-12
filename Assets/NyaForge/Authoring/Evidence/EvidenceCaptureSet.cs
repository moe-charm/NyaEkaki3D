using System;
using System.Collections.Generic;
using System.Linq;
namespace NyaForge.Authoring.Evidence
{
    public sealed class EvidenceCaptureSet
    {
        public string CaptureId { get; }=Guid.NewGuid().ToString("D");
        public EvaluatedSnapshot Snapshot { get; }
        public IReadOnlyList<EvidenceImage> Images { get; }
        public EvidenceCaptureSet(IEnumerable<EvidenceImage> images)
        {
            Checks.Require(images!=null,"INVALID_CAPTURE_SET","Capture images required.");var copy=images.Take(9).ToArray();
            Checks.Require(copy.Length>=1 && copy.Length<=8 && copy.All(i=>i!=null),"INVALID_CAPTURE_SET","A capture set requires 1..8 images.");
            Snapshot=copy[0].Snapshot;
            Checks.Require(Snapshot.State==EvidenceState.Ready && copy.All(i=>ReferenceEquals(i.Snapshot,Snapshot)),"CAPTURE_SNAPSHOT_MISMATCH","All images must come from one acquired ready snapshot.");
            Images=Array.AsReadOnly(copy);
        }
    }
}
