using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace NyaForge.Authoring
{
    public interface IAuthoringProjection { IPreparedProjection Prepare(AuthoringDocument candidate, MeshData evaluatedMesh); }
    public interface IPreparedProjection : IDisposable
    {
        // Prepare builds independent resources. Commit swaps the visible projection.
        // Rollback must restore its old state even if Commit threw. Dispose releases
        // candidate resources after rollback, or old resources after successful commit.
        void Commit();
        void Rollback();
    }
}
