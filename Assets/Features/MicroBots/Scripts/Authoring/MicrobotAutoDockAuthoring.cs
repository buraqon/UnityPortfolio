using Unity.Entities;
using UnityEngine;

namespace HippoLib.MicroBots
{
    public class MicrobotAutoDockAuthoring : MonoBehaviour
    {
        public GameObject dock;
        public float tolerance = 0.15f;
        public float restTime = 1f;

        private class Baker : Baker<MicrobotAutoDockAuthoring>
        {
            public override void Bake(MicrobotAutoDockAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                var dockEntity = Entity.Null;
                if (authoring.dock != null)
                {
                    DependsOn(authoring.dock);
                    dockEntity = GetEntity(authoring.dock, TransformUsageFlags.None);
                }

                AddComponent(entity, new MicrobotAutoDockConfig
                {
                    DockEntity = dockEntity,
                    Tolerance = authoring.tolerance,
                    RestTime = authoring.restTime
                });
            }
        }
    }
}
