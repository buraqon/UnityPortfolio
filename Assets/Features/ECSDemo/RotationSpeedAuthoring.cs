using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace HippoLib.ECSDemo
{
    public class RotationSpeedAuthoring : MonoBehaviour
    {
        public float DegreesPerSecond = 90f;

        private class Baker : Baker<RotationSpeedAuthoring>
        {
            public override void Bake(RotationSpeedAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new RotationSpeed
                {
                    RadiansPerSecond = math.radians(authoring.DegreesPerSecond)
                });
            }
        }
    }
}
