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

            var spawnersToProcess = new NativeList<MicrobotSpawner>(Allocator.Temp);
            var spawnersToRemove = new NativeList<Entity>(Allocator.Temp);

            foreach (var (spawner, entity) in SystemAPI.Query<RefRO<MicrobotSpawner>>().WithEntityAccess())
            {
                spawnersToProcess.Add(spawner.ValueRO);
                spawnersToRemove.Add(entity);
            }

            foreach (var spawner in spawnersToProcess)
            {
                for (var i = 0; i < spawner.SpawnCount; i++)
                {
                    var instance = state.EntityManager.Instantiate(spawner.Prefab);

                    var offset = new float3(
                        random.NextFloat(-0.5f, 0.5f) * spawner.SpawnAreaSize.x,
                        spawner.SpawnHeight,
                        random.NextFloat(-0.5f, 0.5f) * spawner.SpawnAreaSize.z);

                    var spawnPosition = spawner.SpawnCenter + offset;
                    state.EntityManager.SetComponentData(instance, LocalTransform.FromPosition(spawnPosition));

                    var ikTargets = state.EntityManager.GetComponentData<MicrobotIkTargets>(instance);
                    state.EntityManager.SetComponentData(ikTargets.TargetAEntity, LocalTransform.FromPosition(spawnPosition + ikTargets.TargetAOffset));
                    state.EntityManager.SetComponentData(ikTargets.TargetBEntity, LocalTransform.FromPosition(spawnPosition + ikTargets.TargetBOffset));
                }
            }

            foreach (var spawnerEntity in spawnersToRemove)
            {
                state.EntityManager.DestroyEntity(spawnerEntity);
            }

            spawnersToProcess.Dispose();
            spawnersToRemove.Dispose();
        }
    }
}
