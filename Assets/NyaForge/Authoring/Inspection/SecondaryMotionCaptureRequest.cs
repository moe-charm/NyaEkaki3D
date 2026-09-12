using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring.Inspection
{
    /// <summary>Strict bounded request for a transient fixed-step secondary-motion capture.</summary>
    public sealed class SecondaryMotionCaptureRequest
    {
        public int FrameCount { get; }
        public int WarmupSteps { get; }
        public int Width { get; }
        public int Height { get; }

        SecondaryMotionCaptureRequest(int frameCount, int warmupSteps, int width, int height)
        {
            FrameCount = frameCount; WarmupSteps = warmupSteps; Width = width; Height = height;
        }

        public static SecondaryMotionCaptureRequest Read(JObject value)
        {
            Checks.Require(value != null, "INVALID_CAPTURE_REQUEST", "Secondary-motion capture settings are required.");
            var expected = new[] { "frameCount", "height", "warmupSteps", "width" };
            Checks.Require(value.Properties().Select(p => p.Name).OrderBy(name => name, StringComparer.Ordinal).SequenceEqual(expected),
                "INVALID_CAPTURE_REQUEST", "Unexpected secondary-motion capture fields.");
            int frameCount = Integer(value, "frameCount", 1, 8);
            int warmup = Integer(value, "warmupSteps", 0, 120);
            int width = Integer(value, "width", 32, 512);
            int height = Integer(value, "height", 32, 512);
            Checks.Require((long)frameCount * width * height <= 8L * 256L * 256L, "CAPTURE_BUDGET", "Secondary-motion capture pixel budget is exceeded.");
            return new SecondaryMotionCaptureRequest(frameCount, warmup, width, height);
        }

        static int Integer(JObject value, string name, int min, int max)
        {
            var token = value[name];
            int result = 0;
            Checks.Require(token != null && token.Type == JTokenType.Integer && int.TryParse(token.ToString(), out result) && result >= min && result <= max,
                "INVALID_CAPTURE_REQUEST", "Secondary-motion capture field is outside its range: " + name + ".");
            return result;
        }
    }
}
