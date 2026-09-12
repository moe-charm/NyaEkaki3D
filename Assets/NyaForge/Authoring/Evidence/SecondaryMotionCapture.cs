using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring.Evidence
{
    /// <summary>One immutable frame in a transient secondary-motion evidence run.</summary>
    public sealed class SecondaryMotionCaptureFrame
    {
        public int Index { get; }
        public long CompletedSteps { get; }
        public string PoseHash { get; }
        public string MeshHash { get; }
        public string PngHash { get; }
        public int Width { get; }
        public int Height { get; }

        public SecondaryMotionCaptureFrame(int index, long completedSteps, string poseHash, string meshHash, string pngHash, int width, int height)
        {
            Checks.Require(index >= 0 && index < SecondaryMotionCaptureRecord.MaxFrames, "INVALID_CAPTURE_FRAME", "Secondary-motion frame index is outside the supported range.");
            Checks.Require(completedSteps >= 0, "INVALID_CAPTURE_FRAME", "Secondary-motion completed steps cannot be negative.");
            Checks.HashText(poseHash); Checks.HashText(meshHash); Checks.HashText(pngHash);
            Checks.Require(width >= 32 && width <= 512 && height >= 32 && height <= 512, "CAPTURE_BUDGET", "Secondary-motion frame dimensions must be 32..512.");
            Index = index; CompletedSteps = completedSteps; PoseHash = poseHash; MeshHash = meshHash; PngHash = pngHash; Width = width; Height = height;
        }
    }

    /// <summary>Bounded, engine-independent evidence metadata for one fixed-step preview run.</summary>
    public sealed class SecondaryMotionCaptureRecord
    {
        public const int SchemaVersion = 1;
        public const int MaxFrames = 8;
        public string CaptureId { get; }
        public string InstanceId { get; }
        public string DocumentId { get; }
        public long DocumentRevision { get; }
        public string AuthoringStateHash { get; }
        public string InputHash { get; }
        public string ConfigHash { get; }
        public string AdapterId { get; }
        public string AdapterVersion { get; }
        public string PackageVersion { get; }
        public string TargetId { get; }
        public string UnityVersion { get; }
        public string BuildId { get; }
        public string SimulationKind { get; }
        public float FixedStepSeconds { get; }
        public int WarmupSteps { get; }
        public string BasePoseHash { get; }
        public string RootCondition { get; }
        public string ColliderCondition { get; }
        public string CaptureStatus { get; }
        public string Error { get; }
        public IReadOnlyList<SecondaryMotionCaptureFrame> Frames { get; }
        public string ContentHash { get; }

        public SecondaryMotionCaptureRecord(string instanceId, string documentId, long documentRevision, string authoringStateHash,
            string inputHash, string configHash, string adapterId, string adapterVersion, string packageVersion, string targetId,
            string unityVersion, string buildId, string simulationKind, int warmupSteps, string basePoseHash,
            string rootCondition, string colliderCondition, string captureStatus, IEnumerable<SecondaryMotionCaptureFrame> frames, string error = "", string captureId = null)
        {
            Checks.Id(instanceId); Checks.Id(documentId); Checks.Require(documentRevision >= 0, "INVALID_CAPTURE", "Document revision cannot be negative.");
            Checks.HashText(authoringStateHash); Checks.HashText(inputHash); Checks.HashText(configHash); Checks.HashText(basePoseHash);
            Text(adapterId, 128, false); Text(adapterVersion, 64, false); Text(packageVersion, 64, true); Text(targetId, 128, false);
            Text(unityVersion, 64, false); Text(buildId, 128, true); Text(simulationKind, 64, false);
            Checks.Require(warmupSteps >= 0 && warmupSteps <= 120, "CAPTURE_BUDGET", "Secondary-motion warmup must be 0..120 steps.");
            Text(rootCondition, 256, false); Text(colliderCondition, 256, false);
            Checks.Require(captureStatus == "complete" || captureStatus == "failed", "INVALID_CAPTURE", "Secondary-motion capture status is invalid.");
            Text(error ?? "", 2048, true);
            var values = (frames ?? Array.Empty<SecondaryMotionCaptureFrame>()).ToArray();
            Checks.Require(values.Length <= MaxFrames, "CAPTURE_BUDGET", "Secondary-motion capture exceeds the frame limit.");
            for (int i = 0; i < values.Length; i++)
            {
                Checks.Require(values[i] != null && values[i].Index == i, "INVALID_CAPTURE_FRAME", "Secondary-motion frame indices must be contiguous.");
                if (i > 0) Checks.Require(values[i].CompletedSteps > values[i - 1].CompletedSteps, "INVALID_CAPTURE_FRAME", "Secondary-motion completed steps must increase per frame.");
                Checks.Require((long)values[i].Width * values[i].Height <= 512L * 512L, "CAPTURE_BUDGET", "Secondary-motion frame area exceeds capacity.");
            }
            Checks.Require(captureStatus != "complete" || values.Length > 0, "INVALID_CAPTURE", "A complete secondary-motion capture needs a frame.");
            if (captureStatus == "failed") Checks.Require(!string.IsNullOrEmpty(error), "INVALID_CAPTURE", "A failed secondary-motion capture needs an error.");
            if (captureId == null) captureId = Guid.NewGuid().ToString("D");
            Checks.Id(captureId);
            CaptureId = captureId; InstanceId = instanceId; DocumentId = documentId; DocumentRevision = documentRevision;
            AuthoringStateHash = authoringStateHash; InputHash = inputHash; ConfigHash = configHash; AdapterId = adapterId; AdapterVersion = adapterVersion;
            PackageVersion = packageVersion ?? ""; TargetId = targetId; UnityVersion = unityVersion; BuildId = buildId ?? ""; SimulationKind = simulationKind;
            FixedStepSeconds = 1f / 60f; WarmupSteps = warmupSteps; BasePoseHash = basePoseHash; RootCondition = rootCondition; ColliderCondition = colliderCondition;
            CaptureStatus = captureStatus; Error = error ?? ""; Frames = Array.AsReadOnly(values);
            ContentHash = Checks.Hash(SecondaryMotionCaptureCodec.Write(this));
        }

        static void Text(string value, int max, bool allowEmpty)
        {
            Checks.Require(value != null && value.Length <= max && (allowEmpty || !string.IsNullOrWhiteSpace(value)) && value.IndexOf('\0') < 0,
                "INVALID_CAPTURE", "Secondary-motion capture text is invalid.");
        }
    }

    /// <summary>Stable JSON metadata codec for fixed-step secondary-motion evidence.</summary>
    public static class SecondaryMotionCaptureCodec
    {
        static readonly Encoding Utf8 = new UTF8Encoding(false, true);

        public static byte[] Write(SecondaryMotionCaptureRecord record)
        {
            Checks.Require(record != null, "INVALID_CAPTURE", "Secondary-motion capture record is required.");
            var root = new JObject
            {
                ["schemaVersion"] = SecondaryMotionCaptureRecord.SchemaVersion,
                ["kind"] = "nyaforge.secondary-motion.capture",
                ["captureId"] = record.CaptureId,
                ["instanceId"] = record.InstanceId,
                ["documentId"] = record.DocumentId,
                ["documentRevision"] = record.DocumentRevision,
                ["authoringStateHash"] = record.AuthoringStateHash,
                ["inputHash"] = record.InputHash,
                ["configHash"] = record.ConfigHash,
                ["adapterId"] = record.AdapterId,
                ["adapterVersion"] = record.AdapterVersion,
                ["packageVersion"] = record.PackageVersion,
                ["targetId"] = record.TargetId,
                ["unityVersion"] = record.UnityVersion,
                ["buildId"] = record.BuildId,
                ["simulationKind"] = record.SimulationKind,
                ["fixedStepSeconds"] = record.FixedStepSeconds,
                ["warmupSteps"] = record.WarmupSteps,
                ["basePoseHash"] = record.BasePoseHash,
                ["rootCondition"] = record.RootCondition,
                ["colliderCondition"] = record.ColliderCondition,
                ["captureStatus"] = record.CaptureStatus,
                ["frames"] = new JArray(record.Frames.Select(frame => new JObject
                {
                    ["index"] = frame.Index,
                    ["completedSteps"] = frame.CompletedSteps,
                    ["poseHash"] = frame.PoseHash,
                    ["meshHash"] = frame.MeshHash,
                    ["pngHash"] = frame.PngHash,
                    ["width"] = frame.Width,
                    ["height"] = frame.Height
                }))
            };
            if (record.Error != "") root["error"] = record.Error;
            var bytes = Utf8.GetBytes(root.ToString(Formatting.Indented) + "\n");
            Checks.Require(bytes.Length <= AuthoringLimits.MaxManifestBytes, "BUDGET_EXCEEDED", "Secondary-motion capture metadata exceeds capacity.");
            return bytes;
        }
    }
}
