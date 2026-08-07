using Unity.Entities;
using UnityEngine;

namespace HippoLib.MicroBots
{
    public class MicrobotDockCommandAuthoring : MonoBehaviour
    {
        public GameObject microbot;
        public GameObject dock;
        public float tolerance = 0.15f;

        private class Baker : Baker<MicrobotDockCommandAuthoring>
        {
            public override void Bake(MicrobotDockCommandAuthoring authoring)
            {
                DependsOn(authoring.microbot);
                DependsOn(authoring.dock);

                var entity = GetEntity(TransformUsageFlags.None);
                var microbotEntity = GetEntity(authoring.microbot, TransformUsageFlags.Dynamic);
                var dockEntity = GetEntity(authoring.dock, TransformUsageFlags.None);

                AddComponent(entity, new MicrobotDockCommand
                {
                    MicrobotEntity = microbotEntity,
                    DockEntity = dockEntity,
                    Tolerance = authoring.tolerance
                });
            }
        }
    }
}
