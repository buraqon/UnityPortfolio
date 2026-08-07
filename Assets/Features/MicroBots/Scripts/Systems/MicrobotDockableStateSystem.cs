using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace HippoLib.MicroBots
{
    [BurstCompile]
    [UpdateAfter(typeof(MicrobotStepMovementSystem))]
    public partial struct MicrobotDockableStateSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();

            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            var transforms = SystemAPI.GetComponentLookup<LocalTransform>(true);

            foreach (var (stepState, ikTargets, entity) in SystemAPI
                         .Query<RefRO<MicrobotStepState>, RefRO<MicrobotIkTargets>>()
                         .WithAll<MicrobotTag>()
                         .WithEntityAccess())
            {
                var stepping = stepState.ValueRO.Initialized && stepState.ValueRO.StepProgress < 1f;
                var isIdle = !stepState.ValueRO.HasGoal && !stepping;
                var hasDockable = SystemAPI.HasComponent<Dockable>(entity);

                if (isIdle)
                {
                    var pointA = transforms[ikTargets.ValueRO.TargetAEntity].Position;
                    var pointB = transforms[ikTargets.ValueRO.TargetBEntity].Position;

                    if (hasDockable)
                    {
                        SystemAPI.SetComponent(entity, new Dockable { PointA = pointA, PointB = pointB });
                    }
                    else
                    {
                        ecb.AddComponent(entity, new Dockable { PointA = pointA, PointB = pointB });
                    }
                }
                else if (hasDockable)
                {
                    ecb.RemoveComponent<Dockable>(entity);
                }
            }
        }
    }
}
