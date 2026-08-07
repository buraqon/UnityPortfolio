using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace HippoLib.MicroBots
{
    [BurstCompile]
    [UpdateAfter(typeof(MicrobotInputSystem))]
    [UpdateBefore(typeof(MicrobotStepMovementSystem))]
    public partial struct MicrobotNavigationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency();

            var transforms = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var dockPointsLookup = SystemAPI.GetComponentLookup<MicrobotDockPoints>(true);
            var ikTargetsLookup = SystemAPI.GetComponentLookup<MicrobotIkTargets>(true);
            var ikStateLookup = SystemAPI.GetComponentLookup<MicrobotIkState>(true);
            var stepStateLookup = SystemAPI.GetComponentLookup<MicrobotStepState>(false);

            foreach (var dockCommand in SystemAPI.Query<RefRW<MicrobotDockCommand>>())
            {
                if (dockCommand.ValueRO.Docked)
                    continue;

                var microbotEntity = dockCommand.ValueRO.MicrobotEntity;
                if (!ikTargetsLookup.HasComponent(microbotEntity) || !dockPointsLookup.HasComponent(dockCommand.ValueRO.DockEntity))
                    continue;

                var dockPoints = dockPointsLookup[dockCommand.ValueRO.DockEntity];
                var ikTargets = ikTargetsLookup[microbotEntity];

                var posA = transforms[ikTargets.TargetAEntity].Position;
                var posB = transforms[ikTargets.TargetBEntity].Position;
                var tolerance = dockCommand.ValueRO.Tolerance;

                var pointAClaimed = dockCommand.ValueRO.PointAClaimed
                    || math.distance(posA, dockPoints.PointA) <= tolerance
                    || math.distance(posB, dockPoints.PointA) <= tolerance;
                var pointBClaimed = dockCommand.ValueRO.PointBClaimed
                    || math.distance(posA, dockPoints.PointB) <= tolerance
                    || math.distance(posB, dockPoints.PointB) <= tolerance;

                dockCommand.ValueRW.PointAClaimed = pointAClaimed;
                dockCommand.ValueRW.PointBClaimed = pointBClaimed;

                if (pointAClaimed && pointBClaimed)
                {
                    dockCommand.ValueRW.Docked = true;
                    continue;
                }

                var stepState = stepStateLookup[microbotEntity];
                if (stepState.HasGoal)
                    continue;

                var ikState = ikStateLookup[microbotEntity];
                var freeEntity = ikState.BaseIsSegmentB ? ikTargets.TargetAEntity : ikTargets.TargetBEntity;
                var freePos = transforms[freeEntity].Position;

                float3 goal;
                if (!pointAClaimed && !pointBClaimed)
                {
                    goal = math.distance(freePos, dockPoints.PointA) <= math.distance(freePos, dockPoints.PointB)
                        ? dockPoints.PointA
                        : dockPoints.PointB;
                }
                else
                {
                    goal = pointAClaimed ? dockPoints.PointB : dockPoints.PointA;
                }

                stepState.HasGoal = true;
                stepState.GoalPoint = goal;
                stepState.GoalTolerance = tolerance;
                stepStateLookup[microbotEntity] = stepState;
            }
        }
    }
}
