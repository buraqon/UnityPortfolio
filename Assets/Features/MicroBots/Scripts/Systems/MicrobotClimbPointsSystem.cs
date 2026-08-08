using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace HippoLib.MicroBots
{
    [BurstCompile]
    [UpdateAfter(typeof(MicrobotStepMovementSystem))]
    public partial struct MicrobotClimbPointsSystem : ISystem
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

            foreach (var (stepState, goal, ikTargets, transform, entity) in SystemAPI
                         .Query<RefRO<MicrobotStepState>, RefRO<MicrobotGoal>, RefRO<MicrobotIkTargets>, RefRO<LocalTransform>>()
                         .WithAll<MicrobotTag>()
                         .WithEntityAccess())
            {
                var stepping = stepState.ValueRO.Initialized && stepState.ValueRO.StepProgress < 1f;
                var isIdle = !goal.ValueRO.HasGoal && !stepping;
                var hasClimbPoints = SystemAPI.HasComponent<MicrobotClimbPoints>(entity);

                if (isIdle)
                {
                    var pointA = ikTargets.ValueRO.TargetAPos;
                    var pointB = ikTargets.ValueRO.TargetBPos;
                    var elbow = transform.ValueRO.Position;

                    if (hasClimbPoints)
                    {
                        SystemAPI.SetComponent(entity, new MicrobotClimbPoints { PointA = pointA, PointB = pointB, Elbow = elbow });
                    }
                    else
                    {
                        ecb.AddComponent(entity, new MicrobotClimbPoints { PointA = pointA, PointB = pointB, Elbow = elbow });
                    }
                }
                else if (hasClimbPoints)
                {
                    ecb.RemoveComponent<MicrobotClimbPoints>(entity);
                }
            }
        }
    }
}
