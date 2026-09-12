using System;
using NyaForge.Authoring.Inspection;
using Newtonsoft.Json.Linq;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        JObject DispatchSecondaryMotionMcp(string method, SecondaryMotionCaptureRequest capture = null)
        {
            try
            {
                switch (method)
                {
                    case "secondary_motion_capture": return CaptureSecondaryMotionMcp(capture);
                    case "secondary_motion_play": StartSpringPlayback(); break;
                    case "secondary_motion_pause":
                        if (springPlayback == null) throw new InvalidOperationException("揺れのプレビューは開始されていません。");
                        springPlayback.Pause(); RefreshSpringPlayback(); break;
                    case "secondary_motion_reset": ClearSpringPlayback(true); break;
                    case "secondary_motion_rebuild": RebuildSpringPlayback(); break;
                    case "secondary_motion_step": StepSpringPlayback(); break;
                    case "secondary_motion_state": return SecondaryMotionState("state");
                    default: throw new InvalidOperationException("Unknown secondary-motion method: " + method);
                }
                return SecondaryMotionState(method.Substring("secondary_motion_".Length));
            }
            catch (Exception error)
            {
                return new JObject
                {
                    ["success"] = false,
                    ["code"] = "SECONDARY_MOTION_FAILED",
                    ["message"] = error.Message,
                    ["method"] = method
                };
            }
        }

        JObject SecondaryMotionState(string action)
        {
            bool ready = importedRigSession != null && importedVrmSpringSession != null
                && (importedVrmSpringSession.Format == "vrm1" || (importedVrmSpringSession.Format == "vrm0" && importedRigSession.Hierarchy != null))
                && importedVrmSpringSession.SpringBones.Count > 0;
            return new JObject
            {
                ["success"] = true,
                ["code"] = "OK",
                ["action"] = action,
                ["available"] = ready,
                ["format"] = importedVrmSpringSession?.Format ?? "",
                ["playing"] = springPlayback?.IsPlaying == true,
                ["completedSteps"] = springPlayback?.CompletedSteps ?? 0,
                ["pendingSeconds"] = springPlayback?.PendingSeconds ?? 0,
                ["transient"] = true,
                ["saved"] = false,
                ["message"] = springPlaybackStatus?.text ?? ""
            };
        }
    }
}
