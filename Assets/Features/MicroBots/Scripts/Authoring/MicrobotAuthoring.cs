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
        public Transform targetA;
        public Transform targetB;
        public float stepSize = 0.3f;
        public float stepSpeed = 1f;
        public float stepHeight = 0.15f;
        public float turnSpeed = 90f;
        public float turnGate = 30f;
        public float headingEpsilon = 2f;

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
                AddComponent(root, new MicrobotIkState());

                AddComponent(root, new MicrobotStepState
                {
                    StepSize = authoring.stepSize,
                    StepSpeed = authoring.stepSpeed,
                    StepHeight = authoring.stepHeight,
                    TurnSpeed = authoring.turnSpeed,
                    TurnGate = authoring.turnGate,
                    HeadingEpsilon = authoring.headingEpsilon
                });

                DependsOn(authoring.targetA);
                DependsOn(authoring.targetB);
                var targetAEntity = GetEntity(authoring.targetA, TransformUsageFlags.Dynamic);
                var targetBEntity = GetEntity(authoring.targetB, TransformUsageFlags.Dynamic);
                AddComponent(root, new MicrobotIkTargets
                {
                    TargetAEntity = targetAEntity,
                    TargetBEntity = targetBEntity
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
