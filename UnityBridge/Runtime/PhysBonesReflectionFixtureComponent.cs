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

    // Shape-compatible component whose target members live on an inherited type.
    // This catches SDK versions that keep serialized values in a private base class.
    public abstract class PhysBonesReflectionInheritedBase : MonoBehaviour
    {
        [SerializeField] Transform rootTransform;
        [SerializeField] Vector3 endpointPosition;
        [SerializeField] PhysBonesReflectionChildMode multiChildType;
        [SerializeField] Transform[] ignoreTransforms;
        [SerializeField] Component[] colliders;
        [SerializeField] PhysBonesReflectionLimitMode limitType;
        [SerializeField] float maxAngle, radius, stiffness, pull, spring, immobile, gravity, gravityFalloff, damping, elasticity, inert, friction, stretchMotion, squish;
        [SerializeField] Vector3 gravityDir;
        [SerializeField] bool allowPosing, allowCollision, allowGrabbing, snapToHand;
        [SerializeField] string parameter;

        public Transform RootTransform { get { return rootTransform; } }
        public float Stiffness { get { return stiffness; } }
    }

    [AddComponentMenu("")]
    public sealed class PhysBonesReflectionInheritedFixtureComponent : PhysBonesReflectionInheritedBase
    {
    }
}
#endif
