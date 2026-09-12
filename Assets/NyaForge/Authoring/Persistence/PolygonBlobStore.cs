using NyaForge.Authoring.Topology;

namespace NyaForge.Authoring
{
    public static class PolygonBlobStore
    {
        public static string Write(string directory, PolygonMesh mesh)
        {
            byte[] bytes = PolygonBinaryCodec.Write(mesh); string hash = Checks.Hash(bytes);
            directory = Storage.DirectoryPath(directory);
            using (Storage.Lock(directory)) Storage.WriteBlob(directory, hash, bytes);
            return hash;
        }
        public static PolygonMesh Read(string directory, string hash)
        {
            Checks.HashText(hash);
            return PolygonBinaryCodec.Read(Storage.ReadBlob(Storage.DirectoryPath(directory), hash));
        }
    }
}
