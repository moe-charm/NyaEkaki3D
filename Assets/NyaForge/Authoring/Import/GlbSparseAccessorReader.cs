using System;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring.Import
{
    /// <summary>
    /// Reads a bounded glTF accessor into tightly packed element bytes.  The
    /// base bufferView is optional when a sparse override is present; omitted
    /// base elements are defined as zero and sparse entries replace them.
    /// Keeping this here prevents each importer from implementing a subtly
    /// different sparse-index/range policy.
    /// </summary>
    internal static class GlbSparseAccessorReader
    {
        internal static byte[][] ReadRaw(JObject accessor, JArray views, byte[] bin, int bufferLength, int elementBytes, int expected, string label)
        {
            Checks.Require(accessor != null && views != null && bin != null, "INVALID_IMPORT", label + " accessor inputs are required.");
            Checks.Require(expected >= 1, "INVALID_IMPORT", label + " count must be positive.");
            int count = Integer(accessor["count"], 1, AuthoringLimits.MaxVertices, label + " count");
            Checks.Require(count == expected, "INVALID_IMPORT", label + " count differs from the expected vertex count.");
            Checks.Require(accessor["extensions"] == null, "UNSUPPORTED_FORMAT", label + " accessor extensions require a dedicated adapter.");
            var result = new byte[count][];
            for (int i = 0; i < count; i++) result[i] = new byte[elementBytes];

            var baseViewToken = accessor["bufferView"];
            if (baseViewToken != null)
            {
                int viewId = Integer(baseViewToken, 0, views.Count - 1, label + " bufferView");
                var view = views[viewId] as JObject;
                ReadView(view, views, bin, bufferLength, elementBytes, count, accessor, result, label);
            }
            else
            {
                Checks.Require(accessor["sparse"] != null, "INVALID_IMPORT", label + " requires a bufferView or sparse values.");
            }

            var sparse = accessor["sparse"] as JObject;
            if (sparse == null) return result;
            Checks.Require(sparse.Properties() != null, "INVALID_IMPORT", label + " sparse object is invalid.");
            int sparseCount = Integer(sparse["count"], 1, count, label + " sparse count");
            var indices = sparse["indices"] as JObject;
            var values = sparse["values"] as JObject;
            Checks.Require(indices != null && values != null, "INVALID_IMPORT", label + " sparse indices and values are required.");
            Checks.Require(indices["extensions"] == null && values["extensions"] == null, "UNSUPPORTED_FORMAT", label + " sparse extensions require a dedicated adapter.");
            int indexType = Integer(indices["componentType"], 0, int.MaxValue, label + " sparse index componentType");
            Checks.Require(indexType == 5121 || indexType == 5123 || indexType == 5125, "UNSUPPORTED_FORMAT", label + " sparse index componentType is unsupported.");
            int indexWidth = indexType == 5121 ? 1 : indexType == 5123 ? 2 : 4;
            int indexViewId = Integer(indices["bufferView"], 0, views.Count - 1, label + " sparse index bufferView");
            var indexView = views[indexViewId] as JObject;
            var indexBytes = ReadViewBytes(indexView, bin, bufferLength, indexWidth, sparseCount, OptionalInteger(indices["byteOffset"], label + " sparse index offset"), label + " sparse indices");
            int valueViewId = Integer(values["bufferView"], 0, views.Count - 1, label + " sparse value bufferView");
            var valueView = views[valueViewId] as JObject;
            var valueBytes = ReadViewBytes(valueView, bin, bufferLength, elementBytes, sparseCount, OptionalInteger(values["byteOffset"], label + " sparse value offset"), label + " sparse values");
            var seen = new bool[count];
            for (int i = 0; i < sparseCount; i++)
            {
                int index = indexType == 5121 ? indexBytes[i][0] : indexType == 5123 ? indexBytes[i][0] | (indexBytes[i][1] << 8) : checked((int)BitConverter.ToUInt32(indexBytes[i], 0));
                Checks.Require(index >= 0 && index < count, "INVALID_IMPORT", label + " sparse index is out of range.");
                Checks.Require(!seen[index], "INVALID_IMPORT", label + " sparse indices contain a duplicate.");
                seen[index] = true;
                Buffer.BlockCopy(valueBytes[i], 0, result[index], 0, elementBytes);
            }
            return result;
        }

        static void ReadView(JObject view, JArray views, byte[] bin, int bufferLength, int elementBytes, int count, JObject accessor, byte[][] destination, string label)
        {
            Checks.Require(view != null, "INVALID_IMPORT", label + " bufferView is invalid.");
            Checks.Require(view["extensions"] == null, "UNSUPPORTED_FORMAT", label + " bufferView extensions require a dedicated adapter.");
            Checks.Require(Integer(view["buffer"], 0, 0, label + " buffer") == 0, "INVALID_IMPORT", label + " buffer reference is invalid.");
            int viewOffset = OptionalInteger(view["byteOffset"], label + " view offset");
            int accessorOffset = OptionalInteger(accessor["byteOffset"], label + " accessor offset");
            int stride = view["byteStride"] == null ? elementBytes : Integer(view["byteStride"], elementBytes, 4096, label + " stride");
            int viewLength = Integer(view["byteLength"], 1, AuthoringLimits.MaxGlbImportBytes, label + " view length");
            Checks.Require(viewOffset % 4 == 0, "INVALID_IMPORT", label + " bufferView offset must be 4-byte aligned.");
            int componentType = Integer(accessor["componentType"], 0, int.MaxValue, label + " componentType");
            int componentWidth = componentType == 5121 ? 1 : componentType == 5123 ? 2 : componentType == 5125 || componentType == 5126 ? 4 : 0;
            Checks.Require(componentWidth > 0 && accessorOffset % componentWidth == 0 && ((long)viewOffset + accessorOffset) % 4 == 0 && stride % componentWidth == 0,
                "INVALID_IMPORT", label + " accessor alignment is invalid.");
            ValidateRange(viewOffset, accessorOffset, stride, count, elementBytes, viewLength, bufferLength, label);
            for (int row = 0; row < count; row++) Buffer.BlockCopy(bin, checked(viewOffset + accessorOffset + row * stride), destination[row], 0, elementBytes);
        }

        static byte[][] ReadViewBytes(JObject view, byte[] bin, int bufferLength, int elementBytes, int count, int offset, string label)
        {
            Checks.Require(view != null, "INVALID_IMPORT", label + " bufferView is invalid.");
            Checks.Require(view["extensions"] == null, "UNSUPPORTED_FORMAT", label + " bufferView extensions require a dedicated adapter.");
            Checks.Require(Integer(view["buffer"], 0, 0, label + " buffer") == 0, "INVALID_IMPORT", label + " buffer reference is invalid.");
            int viewOffset = OptionalInteger(view["byteOffset"], label + " view offset");
            int viewLength = Integer(view["byteLength"], 1, AuthoringLimits.MaxGlbImportBytes, label + " view length");
            Checks.Require(viewOffset % 4 == 0, "INVALID_IMPORT", label + " bufferView offset must be 4-byte aligned.");
            ValidateRange(viewOffset, offset, elementBytes, count, elementBytes, viewLength, bufferLength, label);
            var result = new byte[count][];
            for (int row = 0; row < count; row++) { result[row] = new byte[elementBytes]; Buffer.BlockCopy(bin, checked(viewOffset + offset + row * elementBytes), result[row], 0, elementBytes); }
            return result;
        }

        static void ValidateRange(int viewOffset, int accessorOffset, int stride, int count, int elementBytes, int viewLength, int bufferLength, string label)
        {
            Checks.Require(viewOffset >= 0 && accessorOffset >= 0 && viewLength >= 1 && (long)viewOffset + viewLength <= bufferLength &&
                (long)accessorOffset + (long)(count - 1) * stride + elementBytes <= viewLength,
                "INVALID_IMPORT", label + " accessor exceeds its bufferView.");
        }

        static int Integer(JToken token, int minimum, int maximum, string label)
        {
            Checks.Require(token != null && token.Type == JTokenType.Integer, "INVALID_IMPORT", label + " must be an integer.");
            double value = (double)token; Checks.Require(value >= minimum && value <= maximum, "INVALID_IMPORT", label + " is out of range."); return (int)value;
        }

        static int OptionalInteger(JToken token, string label) => token == null ? 0 : Integer(token, 0, AuthoringLimits.MaxGlbImportBytes, label);
    }
}
