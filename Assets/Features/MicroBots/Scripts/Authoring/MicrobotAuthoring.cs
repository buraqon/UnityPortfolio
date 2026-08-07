using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
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

                // Targets start wherever their segment's tip already is: root position + the segment's
                // rest direction (root rotation * segment's local rotation, along local forward) * length.
                var rootRotation = authoring.transform.rotation;
                var rootPosition = new float3(authoring.transform.position.x, authoring.transform.position.y, authoring.transform.position.z);

                var tipDirectionA = rootRotation * authoring.segmentA.localRotation * Vector3.forward;
                var targetAOffset = new float3(tipDirectionA.x, tipDirectionA.y, tipDirectionA.z) * authoring.segmentALength;

                var tipDirectionB = rootRotation * authoring.segmentB.localRotation * Vector3.forward;
                var targetBOffset = new float3(tipDirectionB.x, tipDirectionB.y, tipDirectionB.z) * authoring.segmentBLength;

                var targetAEntity = CreateAdditionalEntity(TransformUsageFlags.Dynamic);
                AddComponent(targetAEntity, LocalTransform.FromPosition(rootPosition + targetAOffset));

                var targetBEntity = CreateAdditionalEntity(TransformUsageFlags.Dynamic);
                AddComponent(targetBEntity, LocalTransform.FromPosition(rootPosition + targetBOffset));

                AddComponent(root, new MicrobotIkTargets
                {
                    TargetAEntity = targetAEntity,
                    TargetBEntity = targetBEntity,
                    TargetAOffset = targetAOffset,
                    TargetBOffset = targetBOffset
                });
            }
        }
    }
}
