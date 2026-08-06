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
