using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace HippoLib.MicroBots
{
    public class MicrobotFollowCommandAuthoring : MonoBehaviour
    {
        public Transform destination;
        public float tolerance = 0.1f;

        private class Baker : Baker<MicrobotFollowCommandAuthoring>
        {
            public override void Bake(MicrobotFollowCommandAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                var destinationPosition = authoring.transform.position;
                if (authoring.destination != null)
                {
                    DependsOn(authoring.destination);
                    destinationPosition = authoring.destination.position;
                }

                AddComponent(entity, new MicrobotFollowCommand
                {
                    Destination = destinationPosition,
                    Tolerance = authoring.tolerance
                });
            }
        }
    }
}
