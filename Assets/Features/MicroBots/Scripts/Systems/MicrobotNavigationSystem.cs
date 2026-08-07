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

            var deltaTime = SystemAPI.Time.DeltaTime;
            var transforms = SystemAPI.GetComponentLookup<LocalTransform>(true);
            var dockableLookup = SystemAPI.GetComponentLookup<Dockable>(true);
            var ikTargetsLookup = SystemAPI.GetComponentLookup<MicrobotIkTargets>(true);
            var ikStateLookup = SystemAPI.GetComponentLookup<MicrobotIkState>(true);
            var stepStateLookup = SystemAPI.GetComponentLookup<MicrobotStepState>(false);

            foreach (var (dockCommand, dockList) in SystemAPI
                         .Query<RefRW<MicrobotDockCommand>, DynamicBuffer<MicrobotDockListElement>>())
            {
                if (dockList.Length == 0)
                    continue;

                if (dockCommand.ValueRO.Docked)
                {
                    dockCommand.ValueRW.RestTimer -= deltaTime;
                    if (dockCommand.ValueRO.RestTimer <= 0f)
                    {
                        dockCommand.ValueRW.CurrentDockIndex = (dockCommand.ValueRO.CurrentDockIndex + 1) % dockList.Length;
                        dockCommand.ValueRW.Docked = false;
                        dockCommand.ValueRW.PointAClaimed = false;
                        dockCommand.ValueRW.PointBClaimed = false;
                    }

                    continue;
                }

                var microbotEntity = dockCommand.ValueRO.MicrobotEntity;
                var dockEntity = dockList[dockCommand.ValueRO.CurrentDockIndex].DockEntity;
                if (!ikTargetsLookup.HasComponent(microbotEntity) || !dockableLookup.HasComponent(dockEntity))
                    continue;

                var dockPoints = dockableLookup[dockEntity];
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
                    dockCommand.ValueRW.RestTimer = dockCommand.ValueRO.RestTime;
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
