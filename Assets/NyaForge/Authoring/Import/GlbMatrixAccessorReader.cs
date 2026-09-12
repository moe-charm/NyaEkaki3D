using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring.Import
{
    /// <summary>Bounded dense FLOAT/MAT4 decoding for source inverse-bind data.</summary>
    internal static class GlbMatrixAccessorReader
    {
        internal static int Integer(JToken token, int min, int max, string label)
        {
            Checks.Require(token != null && token.Type == JTokenType.Integer, "INVALID_IMPORT", label + " must be an integer.");
            double value = (double)token;
            Checks.Require(value >= min && value <= max, "INVALID_IMPORT", label + " is outside its budget or index range.");
            return (int)value;
        }

        static int Offset(JObject owner) => owner["byteOffset"] == null ? 0 : Integer(owner["byteOffset"], 0, AuthoringLimits.MaxGlbImportBytes, "byteOffset");

        internal static SourceAffine[] Read(GlbDocument document, int accessorIndex)
        {
            var accessors = document.Root["accessors"] as JArray;
            Checks.Require(accessors != null && accessorIndex >= 0 && accessorIndex < accessors.Count, "INVALID_IMPORT", "Missing matrix accessor.");
            var accessor = accessors[accessorIndex] as JObject;
            Checks.Require(accessor != null && accessor["type"]?.Type == JTokenType.String && (string)accessor["type"] == "MAT4", "INVALID_IMPORT", "Inverse-bind accessor must be MAT4.");
            Integer(accessor["componentType"], 5126, 5126, "Matrix componentType");
            Checks.Require(accessor["normalized"] == null || (accessor["normalized"].Type == JTokenType.Boolean && !(bool)accessor["normalized"]),
                "INVALID_IMPORT", "FLOAT matrices cannot be normalized.");
            Checks.Require(accessor["sparse"] == null, "UNSUPPORTED_FORMAT", "Sparse inverse-bind accessors are not supported yet.");
            Checks.Require(accessor["extensions"] == null, "UNSUPPORTED_FORMAT", "Matrix accessor extensions require a dedicated adapter.");
            int count = Integer(accessor["count"], 1, AuthoringLimits.MaxGlbImportBytes / 64, "Matrix count");
            var views = document.Root["bufferViews"] as JArray;
            Checks.Require(views != null, "INVALID_IMPORT", "Matrix accessor requires bufferViews.");
            int viewIndex = Integer(accessor["bufferView"], 0, views.Count - 1, "Matrix bufferView");
            var view = views[viewIndex] as JObject;
            Checks.Require(view != null, "INVALID_IMPORT", "Matrix bufferView must be an object.");
            // byteStride is for vertex attributes; inverse-bind matrices are tightly packed.
            Checks.Require(view["byteStride"] == null && view["target"] == null, "INVALID_IMPORT", "Inverse-bind bufferView cannot declare vertex stride or target.");
            Checks.Require(view["extensions"] == null, "UNSUPPORTED_FORMAT", "Matrix bufferView extensions require a dedicated adapter.");
            Integer(view["buffer"], 0, 0, "Matrix buffer");
            var buffers = document.Root["buffers"] as JArray;
            Checks.Require(buffers != null && buffers.Count > 0 && buffers[0] is JObject, "INVALID_IMPORT", "Missing embedded GLB buffer.");
            var buffer = (JObject)buffers[0];
            Checks.Require(buffer["uri"] == null && buffer["extensions"] == null, "UNSUPPORTED_FORMAT", "Matrix reader requires the embedded GLB buffer.");
            int bufferLength = Integer(buffer["byteLength"], 1, AuthoringLimits.MaxGlbImportBytes, "Buffer length");
            Checks.Require(bufferLength <= document.Bin.Length && document.Bin.Length - bufferLength <= 3,
                "INVALID_IMPORT", "Embedded buffer length differs from BIN payload.");
            int viewOffset = Offset(view), accessorOffset = Offset(accessor);
            int viewLength = Integer(view["byteLength"], 1, AuthoringLimits.MaxGlbImportBytes, "View length");
            Checks.Require(accessorOffset % 4 == 0 && ((long)viewOffset + accessorOffset) % 4 == 0 &&
                (long)viewOffset + viewLength <= bufferLength && (long)accessorOffset + (long)count * 64 <= viewLength,
                "INVALID_IMPORT", "Matrix data is misaligned or extends beyond its declared buffer range.");
            var result = new SourceAffine[count];
            using (var stream = new MemoryStream(document.Bin, false))
            using (var reader = new BinaryReader(stream))
            {
                stream.Position = (long)viewOffset + accessorOffset;
                for (int i = 0; i < count; i++)
                {
                    var matrix = new double[16];
                    for (int j = 0; j < matrix.Length; j++) matrix[j] = reader.ReadSingle();
                    result[i] = new SourceAffine(matrix);
                }
            }
            return result;
        }
    }
}
