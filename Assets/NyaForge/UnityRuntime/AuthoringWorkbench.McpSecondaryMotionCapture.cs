using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NyaForge.Authoring.Evidence;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Inspection;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        JObject CaptureSecondaryMotionMcp(SecondaryMotionCaptureRequest request)
        {
            if (request == null) throw new InvalidOperationException("Secondary-motion capture settings are required.");
            if (workspace == null || workspace.Document.IsEmpty || importedRigSession == null || importedVrmSpringSession == null)
                throw new InvalidOperationException("対応するVRMモデルを取り込んでください。");
            var authoredState = workspace.Document.StateHash;
            var documentId = workspace.Document.DocumentId;
            var revision = workspace.Document.DocumentRevision;
            var inputHash = authoredState;
            var configHash = Hash(VrmSpringSessionCodec.Write(importedVrmSpringSession));
            var basePoseHash = workspace.Preview.Evaluation?.PoseOutputs.Values.FirstOrDefault()?.Pose.ContentHash ?? Hash(Encoding.UTF8.GetBytes("no-pose"));
            var frames = new List<SecondaryMotionCaptureFrame>();
            var images = new List<JObject>();
            bool previousAutomaticTick = springAutomaticTick;
            try
            {
                springAutomaticTick = false;
                if (springPlayback != null) ClearSpringPlayback(true);
                StartSpringPlayback();
                springPlayback.Pause();
                for (int i = 0; i < request.WarmupSteps; i++) StepSpringPlayback();
                for (int i = 0; i < request.FrameCount; i++)
                {
                    StepSpringPlayback();
                    var value = CurrentSpringPlaybackValue();
                    var png = SecondaryMotionModelCapture.Capture(value, request.Width, request.Height, out var view);
                    var frame = new SecondaryMotionCaptureFrame(i, springPlayback.CompletedSteps, springPlayback.Pose.ContentHash, value.Mesh.ContentHash, Hash(png), request.Width, request.Height);
                    frames.Add(frame);
                    images.Add(new JObject
                    {
                        ["index"] = i, ["mimeType"] = "image/png", ["sha256"] = frame.PngHash,
                        ["width"] = request.Width, ["height"] = request.Height,
                        ["data"] = Convert.ToBase64String(png),
                        ["camera"] = new JObject
                        {
                            ["projection"] = "orthographic",
                            ["position"] = new JArray(view.Position.X, view.Position.Y, view.Position.Z),
                            ["target"] = new JArray(view.Target.X, view.Target.Y, view.Target.Z),
                            ["up"] = new JArray(view.Up.X, view.Up.Y, view.Up.Z),
                            ["size"] = view.OrthographicSize, ["near"] = view.Near, ["far"] = view.Far
                        }
                    });
                }
                if (workspace.Document.StateHash != authoredState || workspace.Document.DocumentRevision != revision)
                    throw new InvalidOperationException("Secondary-motion capture changed authored state.");
                var record = CreateCaptureRecord(request, authoredState, documentId, revision, inputHash, configHash, basePoseHash, "complete", frames);
                var root = new JObject
                {
                    ["success"] = true, ["code"] = "OK", ["record"] = JObject.Parse(Encoding.UTF8.GetString(SecondaryMotionCaptureCodec.Write(record))),
                    ["delivery"] = "inline", ["images"] = new JArray(images)
                };
                EnsureCaptureResponseBudget(root);
                return root;
            }
            catch (Exception error)
            {
                string message = error.GetType().Name + ": " + error.Message;
                if (message.Length > 2048) message = message.Substring(0, 2048);
                var record = CreateCaptureRecord(request, authoredState, documentId, revision, inputHash, configHash, basePoseHash, "failed", frames, message);
                return new JObject
                {
                    ["success"] = false, ["code"] = "SECONDARY_MOTION_CAPTURE_FAILED", ["message"] = error.Message,
                    ["record"] = JObject.Parse(Encoding.UTF8.GetString(SecondaryMotionCaptureCodec.Write(record))), ["delivery"] = "inline", ["images"] = new JArray()
                };
            }
            finally
            {
                try { ClearSpringPlayback(true); } finally { springAutomaticTick = previousAutomaticTick; }
            }
        }

        SecondaryMotionCaptureRecord CreateCaptureRecord(SecondaryMotionCaptureRequest request, string authoredState, string documentId, long revision,
            string inputHash, string configHash, string basePoseHash, string status, IEnumerable<SecondaryMotionCaptureFrame> frames, string error = "")
        {
            return new SecondaryMotionCaptureRecord(workspace.InstanceId, documentId, revision, authoredState, inputHash, configHash,
                "nyaforge.vrm-spring", "1", "", importedVrmSpringSession.Format + "-spring", Application.unityVersion, Application.version,
                "vrm-spring-preview", request.WarmupSteps, basePoseHash, "authored-root", "session-colliders", status, frames, error);
        }

        GraphMeshValue CurrentSpringPlaybackValue()
        {
            if (springPlayback == null || workspace == null) throw new InvalidOperationException("Secondary-motion preview is unavailable.");
            var graph = workspace.Document.Objects[0].Graph;
            var evaluation = GraphEvaluator.Evaluate(graph.ReplaceNode(GraphNode.PoseNode(springPoseNode, springPlayback.Pose)));
            if (!evaluation.IsComplete) throw new InvalidOperationException("揺れ姿勢のgraph評価に失敗しました。");
            return SourceSkinDisplayValue(evaluation, graph) ?? evaluation.Output;
        }

        static string Hash(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        static void EnsureCaptureResponseBudget(JObject root)
        {
            if (Encoding.UTF8.GetByteCount(root.ToString(Newtonsoft.Json.Formatting.None)) > 3 * 1024 * 1024)
                throw new InvalidOperationException("Secondary-motion capture response exceeds the 3 MiB safety budget.");
        }
    }
}
