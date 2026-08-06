using Unity.Entities;
using UnityEngine;

namespace HippoLib.ECSDemo
{
    public class CubeSpawnerAuthoring : MonoBehaviour
    {
        public GameObject CubePrefab;
        public int Count = 5;
        public float Spacing = 2f;
        [Tooltip("Seconds between automatic spawns. 0 or less disables the timer trigger.")]
        public float Interval = 0f;

        private class Baker : Baker<CubeSpawnerAuthoring>
        {
            public override void Bake(CubeSpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new CubeSpawner
                {
                    Prefab = GetEntity(authoring.CubePrefab, TransformUsageFlags.Dynamic),
                    Count = authoring.Count,
                    Spacing = authoring.Spacing,
                    Interval = authoring.Interval,
                    NextSpawnTime = 0
                });
                AddBuffer<SpawnedCube>(entity);
            }
        }
    }
}
