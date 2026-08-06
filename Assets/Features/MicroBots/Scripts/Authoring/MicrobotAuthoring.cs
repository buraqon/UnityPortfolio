using Unity.Entities;
using UnityEngine;

namespace HippoLib.MicroBots
{
    public class MicrobotAuthoring : MonoBehaviour
    {
        public Transform segmentA;
        public float segmentALength = 0.4f;
        public Transform segmentB;
        public float segmentBLength = 0.4f;
        public Vector3 destination;
        public Transform target;
        public bool isManualMovement = true;
        public float stepSize = 0.3f;
        public float stepSpeed = 1f;
        public float stepHeight = 0.15f;
        public AnimationCurve stepCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));

        private class Baker : Baker<MicrobotAuthoring>
        {
            public override void Bake(MicrobotAuthoring authoring)
            {
                var root = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<MicrobotTag>(root);
                AddComponent(root, new MicrobotMovementTarget
                {
                    Destination = authoring.destination
                });
                AddComponent(root, new MicrobotIkState
                {
                    IsManualMovement = authoring.isManualMovement
                });

                AddComponentObject(root, new MicrobotStepSettings
                {
                    StepSize = authoring.stepSize,
                    StepSpeed = authoring.stepSpeed,
                    StepHeight = authoring.stepHeight,
                    StepCurve = authoring.stepCurve
                });
                AddComponent(root, new MicrobotStepState());

                DependsOn(authoring.target);
                var targetEntity = GetEntity(authoring.target, TransformUsageFlags.Dynamic);
                AddComponent(root, new MicrobotIkTarget
                {
                    TargetEntity = targetEntity
                });

                DependsOn(authoring.segmentA);
                DependsOn(authoring.segmentB);

                var segmentAEntity = GetEntity(authoring.segmentA, TransformUsageFlags.Dynamic);
                var segmentBEntity = GetEntity(authoring.segmentB, TransformUsageFlags.Dynamic);

                AddComponent(root, new MicrobotSegments
                {
                    SegmentAEntity = segmentAEntity,
                    RotationA = authoring.segmentA.localRotation,
                    LengthA = authoring.segmentALength,
                    SegmentBEntity = segmentBEntity,
                    RotationB = authoring.segmentB.localRotation,
                    LengthB = authoring.segmentBLength
                });
            }
        }
    }
}
