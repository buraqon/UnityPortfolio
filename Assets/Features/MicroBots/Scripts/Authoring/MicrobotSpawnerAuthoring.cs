using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace HippoLib.MicroBots
{
    public class MicrobotSpawnerAuthoring : MonoBehaviour
    {
        public GameObject microbotPrefab;
        public int spawnCount = 10;
        public Vector3 spawnAreaSize = new Vector3(10f, 0f, 10f);
        public float spawnHeight = 0f;

        private class Baker : Baker<MicrobotSpawnerAuthoring>
        {
            public override void Bake(MicrobotSpawnerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                DependsOn(authoring.microbotPrefab);
                var prefabEntity = GetEntity(authoring.microbotPrefab, TransformUsageFlags.Dynamic);

                AddComponent(entity, new MicrobotSpawner
                {
                    Prefab = prefabEntity,
                    SpawnCount = authoring.spawnCount,
                    SpawnCenter = authoring.transform.position,
                    SpawnAreaSize = new float3(authoring.spawnAreaSize.x, authoring.spawnAreaSize.y, authoring.spawnAreaSize.z),
                    SpawnHeight = authoring.spawnHeight
                });
            }
        }
    }
}
