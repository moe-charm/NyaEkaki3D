using System;
using System.Linq;
using System.Text;
using NyaForge.Authoring;
using NyaForge.Authoring.Evidence;
using NyaForge.Authoring.Inspection;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    static void RunSecondaryMotionCaptureTests()
    {
        Test("secondary-motion capture record is deterministic and bounded", () =>
        {
            string instance = Guid.NewGuid().ToString("D"), document = Guid.NewGuid().ToString("D"), capture = Guid.NewGuid().ToString("D");
            string hash = Hash("authoring");
            var frames = new[]
            {
                new SecondaryMotionCaptureFrame(0, 3, hash, Hash("mesh-0"), Hash("png-0"), 128, 128),
                new SecondaryMotionCaptureFrame(1, 4, Hash("pose-1"), Hash("mesh-1"), Hash("png-1"), 128, 128)
            };
            var record = new SecondaryMotionCaptureRecord(instance, document, 7, hash, hash, Hash("config"), "nyaforge.vrm-spring", "1", "", "vrm1-spring", "2022.3", "test", "bone-pose", 2, hash, "authored-root", "session-colliders", "complete", frames, captureId: capture);
            var bytes = SecondaryMotionCaptureCodec.Write(record); var json = JObject.Parse(Encoding.UTF8.GetString(bytes));
            Equal("nyaforge.secondary-motion.capture", (string)json["kind"]); Equal(capture, (string)json["captureId"]); Equal(2, ((JArray)json["frames"]).Count);
            True(record.ContentHash == Checks.Hash(bytes));
            var same = new SecondaryMotionCaptureRecord(instance, document, 7, hash, hash, Hash("config"), "nyaforge.vrm-spring", "1", "", "vrm1-spring", "2022.3", "test", "bone-pose", 2, hash, "authored-root", "session-colliders", "complete", frames, captureId: capture);
            Equal(record.ContentHash, same.ContentHash);
            Expect("INVALID_CAPTURE_FRAME", () => new SecondaryMotionCaptureFrame(8, 1, hash, hash, hash, 128, 128));
            Expect("INVALID_CAPTURE", () => new SecondaryMotionCaptureRecord(instance, document, 7, hash, hash, hash, "adapter", "1", "", "target", "2022.3", "", "bone-pose", 0, hash, "root", "collider", "failed", Array.Empty<SecondaryMotionCaptureFrame>()));
        });

        Test("secondary-motion capture request rejects unknown fields and enforces pixel budget", () =>
        {
            var request = new JObject { ["frameCount"] = 3, ["warmupSteps"] = 10, ["width"] = 256, ["height"] = 256 };
            var parsed = SecondaryMotionCaptureRequest.Read(request); Equal(3, parsed.FrameCount); Equal(10, parsed.WarmupSteps); Equal(256, parsed.Width);
            request["unknown"] = true; Expect("INVALID_CAPTURE_REQUEST", () => SecondaryMotionCaptureRequest.Read(request));
            request.Remove("unknown"); request["frameCount"] = 8; request["width"] = 512; request["height"] = 512; Expect("CAPTURE_BUDGET", () => SecondaryMotionCaptureRequest.Read(request));
            var envelope = new JObject { ["version"] = 1, ["requestId"] = Guid.NewGuid().ToString("D"), ["expectedInstanceId"] = Guid.NewGuid().ToString("D"), ["method"] = "secondary_motion_capture", ["capture"] = new JObject { ["frameCount"] = 2, ["warmupSteps"] = 0, ["width"] = 128, ["height"] = 128 } };
            var ipc = AuthoringIpcRequest.Parse(Encoding.UTF8.GetBytes(envelope.ToString())); True(ipc.SecondaryCapture != null); Equal(2, ipc.SecondaryCapture.FrameCount);
        });
    }

    static string Hash(string value) => Checks.Hash(Encoding.UTF8.GetBytes(value));
}
