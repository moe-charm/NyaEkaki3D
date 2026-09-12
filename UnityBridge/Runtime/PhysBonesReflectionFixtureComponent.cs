#if UNITY_EDITOR
using UnityEngine;

namespace NyaForge.UnityBridge
{
    // Shape-compatible component used by the package's editor batch verification.
    // It is intentionally hidden from the Add Component menu and has no runtime behaviour.
    public enum PhysBonesReflectionChildMode { Ignore, First, All }
    public enum PhysBonesReflectionLimitMode { None, Angle, Hinge, Polar }

    [AddComponentMenu("")]
    public sealed class PhysBonesReflectionFixtureComponent : MonoBehaviour
    {
        public Transform rootTransform;
        public Vector3 endpointPosition;
        public PhysBonesReflectionChildMode multiChildType;
        public Transform[] ignoreTransforms;
        public Component[] colliders;
        public PhysBonesReflectionLimitMode limitType;
        public float maxAngle, radius, stiffness, pull, spring, immobile, gravity, gravityFalloff, damping, elasticity, inert, friction, stretchMotion, squish;
        public Vector3 gravityDir;
        public bool allowPosing, allowCollision, allowGrabbing, snapToHand;
        public string parameter;
    }
}
#endif
