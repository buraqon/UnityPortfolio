using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace HippoLib.MicroBots
{
    [BurstCompile]
    [UpdateBefore(typeof(MicrobotStepMovementSystem))]
    public partial struct MicrobotFollowCommandSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();

            if (!SystemAPI.TryGetSingleton<MicrobotFollowCommand>(out var command))
                return;

            foreach (var (stepState, transform) in SystemAPI
                         .Query<RefRW<MicrobotStepState>, RefRO<LocalTransform>>()
                         .WithAll<MicrobotTag>())
            {
                if (stepState.ValueRO.HasGoal)
                    continue;

                if (math.distance(transform.ValueRO.Position, command.Destination) <= command.Tolerance)
                    continue;

                stepState.ValueRW.HasGoal = true;
                stepState.ValueRW.GoalPoint = command.Destination;
                stepState.ValueRW.GoalTolerance = command.Tolerance;
            }
        }
    }
}
