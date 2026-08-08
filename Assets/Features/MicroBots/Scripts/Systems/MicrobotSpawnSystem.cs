using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace HippoLib.MicroBots
{
    [BurstCompile]
    public partial struct MicrobotSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var random = Random.CreateFromIndex((uint)SystemAPI.Time.ElapsedTime + 1);

            var spawnersToProcess = new NativeList<Entity>(Allocator.Temp);

            foreach (var (spawner, entity) in SystemAPI.Query<RefRO<MicrobotSpawner>>().WithEntityAccess())
            {
                if (!spawner.ValueRO.HasSpawned)
                    spawnersToProcess.Add(entity);
            }

            foreach (var spawnerEntity in spawnersToProcess)
            {
                var spawner = state.EntityManager.GetComponentData<MicrobotSpawner>(spawnerEntity);
                var spawnedEntities = new NativeList<Entity>(spawner.SpawnCount, Allocator.Temp);

                for (var i = 0; i < spawner.SpawnCount; i++)
                {
                    var instance = state.EntityManager.Instantiate(spawner.Prefab);

                    var offset = new float3(
                        random.NextFloat(-0.5f, 0.5f) * spawner.SpawnAreaSize.x,
                        spawner.SpawnHeight,
                        random.NextFloat(-0.5f, 0.5f) * spawner.SpawnAreaSize.z);

                    var spawnPosition = spawner.SpawnCenter + offset;
                    state.EntityManager.SetComponentData(instance, LocalTransform.FromPosition(spawnPosition));

                    spawnedEntities.Add(instance);
                }

                var spawnedBuffer = state.EntityManager.GetBuffer<MicrobotSpawnedElement>(spawnerEntity);
                foreach (var spawnedEntity in spawnedEntities)
                {
                    spawnedBuffer.Add(new MicrobotSpawnedElement { Value = spawnedEntity });
                }

                spawnedEntities.Dispose();

                spawner.HasSpawned = true;
                state.EntityManager.SetComponentData(spawnerEntity, spawner);
            }

            spawnersToProcess.Dispose();
        }
    }
}
