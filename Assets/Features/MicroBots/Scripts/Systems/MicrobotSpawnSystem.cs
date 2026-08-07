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
            var spawnersToRemove = new NativeList<Entity>(Allocator.Temp);

            foreach (var (spawner, entity) in SystemAPI.Query<RefRO<MicrobotSpawner>>().WithEntityAccess())
            {
                for (var i = 0; i < spawner.ValueRO.SpawnCount; i++)
                {
                    var instance = state.EntityManager.Instantiate(spawner.ValueRO.Prefab);

                    var offset = new float3(
                        random.NextFloat(-0.5f, 0.5f) * spawner.ValueRO.SpawnAreaSize.x,
                        spawner.ValueRO.SpawnHeight,
                        random.NextFloat(-0.5f, 0.5f) * spawner.ValueRO.SpawnAreaSize.z);

                    var spawnPosition = spawner.ValueRO.SpawnCenter + offset;
                    state.EntityManager.SetComponentData(instance, LocalTransform.FromPosition(spawnPosition));

                    var ikTargets = state.EntityManager.GetComponentData<MicrobotIkTargets>(instance);
                    state.EntityManager.SetComponentData(ikTargets.TargetAEntity, LocalTransform.FromPosition(spawnPosition + ikTargets.TargetAOffset));
                    state.EntityManager.SetComponentData(ikTargets.TargetBEntity, LocalTransform.FromPosition(spawnPosition + ikTargets.TargetBOffset));
                }

                spawnersToRemove.Add(entity);
            }

            foreach (var spawnerEntity in spawnersToRemove)
            {
                state.EntityManager.DestroyEntity(spawnerEntity);
            }

            spawnersToRemove.Dispose();
        }
    }
}
