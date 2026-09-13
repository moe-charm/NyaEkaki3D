using System;

namespace NyaForge.Authoring.Graph
{
    /// <summary>Immutable source image bytes kept apart from the editable preview image.</summary>
    public sealed class GraphOriginalImage
    {
        public string PaintNodeId { get; }
        public int Width { get; }
        public int Height { get; }
        public string MimeType { get; }
        /// <summary>Hash of the bounded Paint preview at import time.</summary>
        public string PreviewImageHash { get; }
        public int EncodedByteCount { get { return encodedBytes.Length; } }
        public string ContentHash { get; }
        readonly byte[] encodedBytes;

        public GraphOriginalImage(string paintNodeId, int width, int height, string mimeType, byte[] encodedBytes, string previewImageHash = "")
        {
            Checks.Id(paintNodeId); Checks.Require(width > 0 && height > 0 && width <= 8192 && height <= 8192, "IMAGE_DIMENSION_EXCEEDED", "Original image dimensions must be within 8192px.");
            Checks.Require(mimeType == "image/png" || mimeType == "image/jpeg", "UNSUPPORTED_FORMAT", "Original image must be PNG or JPEG.");
            Checks.Require(encodedBytes != null && encodedBytes.Length > 0 && encodedBytes.Length <= 16 * 1024 * 1024, "IMAGE_BUDGET_EXCEEDED", "Original image exceeds the 16 MiB image budget.");
            if (!string.IsNullOrEmpty(previewImageHash)) Checks.HashText(previewImageHash);
            PaintNodeId = paintNodeId; Width = width; Height = height; MimeType = mimeType; PreviewImageHash = previewImageHash ?? ""; this.encodedBytes = (byte[])encodedBytes.Clone(); ContentHash = Checks.Hash(this.encodedBytes);
        }

        public byte[] CopyEncodedBytes() { return (byte[])encodedBytes.Clone(); }
    }
}
