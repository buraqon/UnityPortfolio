using Unity.Entities;
using UnityEngine;

namespace HippoLib.MicroBots
{
    public class DockAuthoring : MonoBehaviour
    {
        public Transform pointA;
        public Transform pointB;

        private class Baker : Baker<DockAuthoring>
        {
            public override void Bake(DockAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                DependsOn(authoring.pointA);
                DependsOn(authoring.pointB);

                AddComponent(entity, new Dockable
                {
                    PointA = authoring.pointA.position,
                    PointB = authoring.pointB.position
                });
            }
        }
    }
}
